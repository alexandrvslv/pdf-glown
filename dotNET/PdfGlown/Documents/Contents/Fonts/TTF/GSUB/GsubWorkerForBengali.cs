/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using PdfGlown.Documents.Contents.Fonts.TTF.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PdfGlown.Documents.Contents.Fonts.TTF.GSUB
{
    /// <summary>
    /// Bengali-specific implementation of GSUB system
    /// @author Palash Ray
    /// </summary>
    public class GsubWorkerForBengali : IGsubWorker
    {
        private static readonly string INIT_FEATURE = "init";

        // This sequence is very important.This has been taken from
        // <a href="https://docs.microsoft.com/en-us/typography/script-development/bengali"> https://docs.microsoft.com/en-us/typography/script-development/bengali</a>
        private static readonly List<string> FEATURES_IN_ORDER = ["locl", "nukt", "akhn",
                "rphf", "blwf", "pstf", "half", "vatu", "cjct", INIT_FEATURE, "pres", "abvs", "blws",
                "psts", "haln", "calt" ];

        private static readonly char[] BEFORE_HALF_CHARS = ['\u09BF', '\u09C7', '\u09C8'];
        private static readonly BeforeAndAfterSpanComponent[] BEFORE_AND_AFTER_SPAN_CHARS = [
            new BeforeAndAfterSpanComponent('\u09CB', '\u09C7', '\u09BE'),
            new BeforeAndAfterSpanComponent('\u09CC', '\u09C7', '\u09D7') ];

        private readonly ICmapLookup cmapLookup;
        private readonly IGsubData gsubData;

        private readonly HashList<ushort> beforeHalfGlyphIds;
        private readonly Dictionary<int, BeforeAndAfterSpanComponent> beforeAndAfterSpanGlyphIds;


        public GsubWorkerForBengali(ICmapLookup cmapLookup, IGsubData gsubData)
        {
            this.cmapLookup = cmapLookup;
            this.gsubData = gsubData;
            beforeHalfGlyphIds = GetBeforeHalfGlyphIds();
            beforeAndAfterSpanGlyphIds = GetBeforeAndAfterSpanGlyphIds();
        }

        public HashList<ushort> ApplyTransforms(HashList<ushort> originalGlyphIds)
        {
            var intermediateGlyphsFromGsub = originalGlyphIds;

            foreach (string feature in FEATURES_IN_ORDER)
            {
                if (!gsubData.IsFeatureSupported(feature))
                {
                    Debug.WriteLine($"info: the feature {feature} was not found");
                    continue;
                }

                Debug.WriteLine($"info: applying the feature {feature}");

                IScriptFeature scriptFeature = gsubData.GetFeature(feature);

                intermediateGlyphsFromGsub = ApplyGsubFeature(scriptFeature, intermediateGlyphsFromGsub);
            }

            return RepositionGlyphs(intermediateGlyphsFromGsub);
        }

        private HashList<ushort> RepositionGlyphs(HashList<ushort> originalGlyphIds)
        {
            var glyphsRepositionedByBeforeHalf = RepositionBeforeHalfGlyphIds(originalGlyphIds);
            return RepositionBeforeAndAfterSpanGlyphIds(glyphsRepositionedByBeforeHalf);
        }

        private HashList<ushort> RepositionBeforeHalfGlyphIds(HashList<ushort> originalGlyphIds)
        {
            var span = originalGlyphIds.Span;
            var repositionedGlyphIds = new ushort[span.Length];
            span.CopyTo(repositionedGlyphIds);
            for (int index = 1; index < repositionedGlyphIds.Length; index++)
            {
                var glyphId = span[index];
                if (beforeHalfGlyphIds.Span.Contains(glyphId))
                {
                    var previousGlyphId = span[index - 1];
                    repositionedGlyphIds[index] = previousGlyphId;
                    repositionedGlyphIds[index - 1] = glyphId;
                }
            }
            return (HashList<ushort>)repositionedGlyphIds;
        }

        private HashList<ushort> RepositionBeforeAndAfterSpanGlyphIds(HashList<ushort> originalGlyphIds)
        {
            var span = originalGlyphIds.Span;
            var repositionedGlyphIds = new ushort[span.Length];
            span.CopyTo(repositionedGlyphIds);
            for (int index = 1; index < repositionedGlyphIds.Length; index++)
            {
                var glyphId = span[index];
                if (beforeAndAfterSpanGlyphIds.TryGetValue(glyphId, out BeforeAndAfterSpanComponent component))
                {
                    var previousGlyphId = span[index - 1];
                    repositionedGlyphIds[index] = previousGlyphId;
                    repositionedGlyphIds[index - 1] = (ushort)GetGlyphId(component.beforeComponentCharacter);
                    repositionedGlyphIds[index + 1] = (ushort)GetGlyphId(component.afterComponentCharacter);
                }
            }
            return (HashList<ushort>)repositionedGlyphIds;
        }

        private HashList<ushort> ApplyGsubFeature(IScriptFeature scriptFeature, HashList<ushort> originalGlyphs)
        {
            var allGlyphIdsForSubstitution = scriptFeature.AllGlyphIdsForSubstitution;
            if (allGlyphIdsForSubstitution.Count == 0)
            {
                // not stopping here results in really weird output, the regex goes wild
                Debug.WriteLine($"debug: getAllGlyphIdsForSubstitution() for {scriptFeature.Name} is empty");
                return originalGlyphs;
            }
            IGlyphArraySplitter glyphArraySplitter = new GlyphArraySplitterRegexImpl(scriptFeature.AllGlyphIdsForSubstitution);

            var tokens = glyphArraySplitter.Split(originalGlyphs);

            var gsubProcessedGlyphs = new List<ushort>(tokens.Count);

            foreach (var chunk in tokens)
            {
                if (scriptFeature.CanReplaceGlyphs(chunk))
                {
                    // gsub system kicks in, you get the glyphId directly
                    var replacement = scriptFeature.GetReplacementForGlyphs(chunk);
                    foreach (var glyphId in replacement.Span)
                        gsubProcessedGlyphs.Add(glyphId);
                }
                else
                {
                    foreach (var glyphId in chunk.Span)
                        gsubProcessedGlyphs.Add(glyphId);
                }
            }

            return (HashList<ushort>)gsubProcessedGlyphs.ToArray();
        }

        private HashList<ushort> GetBeforeHalfGlyphIds()
        {
            var glyphIds = new List<ushort>(BEFORE_HALF_CHARS.Length);

            foreach (char character in BEFORE_HALF_CHARS)
            {
                glyphIds.Add((ushort)GetGlyphId(character));
            }

            if (gsubData.IsFeatureSupported(INIT_FEATURE))
            {
                IScriptFeature feature = gsubData.GetFeature(INIT_FEATURE);
                foreach (var glyphCluster in feature.AllGlyphIdsForSubstitution)
                {
                    var replacement = feature.GetReplacementForGlyphs(glyphCluster);
                    foreach (var glyphId in replacement.Span)
                        glyphIds.Add(glyphId);
                }
            }

            return (HashList<ushort>)glyphIds.ToArray();

        }

        private int GetGlyphId(char character)
        {
            return cmapLookup.GetGlyphId(character);
        }

        private Dictionary<int, BeforeAndAfterSpanComponent> GetBeforeAndAfterSpanGlyphIds()
        {
            var result = new Dictionary<int, BeforeAndAfterSpanComponent>(BEFORE_AND_AFTER_SPAN_CHARS.Length);

            foreach (BeforeAndAfterSpanComponent beforeAndAfterSpanComponent in BEFORE_AND_AFTER_SPAN_CHARS)
            {
                result[GetGlyphId(beforeAndAfterSpanComponent.originalCharacter)] = beforeAndAfterSpanComponent;
            }

            return result;
        }

        /// <summary>
        /// Models characters like O-kar (\u09CB) and OU-kar (\u09CC). Since these 2 characters is
        /// represented by 2 components, one before and one after the Vyanjan Varna on which this is
        /// used, this glyph has to be replaced by these 2 glyphs. For O-kar, it has to be replaced by
        /// E-kar (\u09C7) and AA-kar (\u09BE). For OU-kar, it has be replaced by E-kar (\u09C7) and
        /// \u09D7.
        /// </summary>
        private class BeforeAndAfterSpanComponent
        {
            internal readonly char originalCharacter;
            internal readonly char beforeComponentCharacter;
            internal readonly char afterComponentCharacter;

            public BeforeAndAfterSpanComponent(char originalCharacter, char beforeComponentCharacter, char afterComponentCharacter)
            {
                this.originalCharacter = originalCharacter;
                this.beforeComponentCharacter = beforeComponentCharacter;
                this.afterComponentCharacter = afterComponentCharacter;
            }

        }

    }
}