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
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents.Fonts.AFM
{
    /// <summary>
    /// This is the outermost AFM type.This can be created by the afmparser with a valid AFM document.
    /// @author Ben Litchfield
    /// </summary>
    public class FontMetrics
    {
        /// <summary>This is the version of the FontMetrics.</summary>
        private float afmVersion;
        private int metricSets = 0;
        private string fontName;
        private string fullName;
        private string familyName;
        private string weight;
        private SKRect fontBBox;
        private string fontVersion;
        private string notice;
        private string encodingScheme;
        private int mappingScheme = 6;
        private int escChar;
        private string characterSet;
        private int characters;
        private bool isBaseFont = true;
        private float[] vVector;
        private bool? isFixedV;
        private float capHeight;
        private float xHeight;
        private float ascender;
        private float descender;
        private readonly List<string> comments = new();

        private float underlinePosition;
        private float underlineThickness;
        private float italicAngle;
        private float[] charWidth;
        private bool isFixedPitch;
        private float standardHorizontalWidth;
        private float standardVerticalWidth;

        private List<CharMetric> charMetrics = new();
        private Dictionary<string, CharMetric> charMetricsMap = new(StringComparer.Ordinal);
        private List<TrackKern> trackKern = new();
        private List<Composite> composites = new();
        private List<KernPair> kernPairs = new();
        private List<KernPair> kernPairs0 = new();
        private List<KernPair> kernPairs1 = new();

        public FontMetrics()
        { }

        /// <summary>This will get the width of a character.</summary>
        /// <param name="name">The character to get the width for.</param>
        /// <returns>The width of the character.</returns>
        public float GetCharacterWidth(string name) => charMetricsMap.TryGetValue(name, out CharMetric metric) ? metric.Wx : 0;

        /// <summary>This will get the width of a character.</summary>
        /// <param name="name">The character to get the width for.</param>
        /// <returns>The height of the character.</returns>
        public float GetCharacterHeight(string name)
        {
            float result = charMetricsMap.TryGetValue(name, out CharMetric metric) ? metric.Wy : 0;
            return result == 0 ? metric.BoundingBox.Height: result;
        }

        /// <summary>This will get the average width of a character.</summary>
        /// <returns>The width of the character.</returns>
        public float GetAverageCharacterWidth()
        {
            float average = 0;
            float totalWidths = 0;
            float characterCount = 0;
            foreach (CharMetric metric in charMetrics)
            {
                if (metric.Wx > 0)
                {
                    totalWidths += metric.Wx;
                    characterCount += 1;
                }
            }
            if (totalWidths > 0)
            {
                average = totalWidths / characterCount;
            }
            return average;
        }

        /// <summary>This will add a new comment.</summary>
        /// <param name="comment">The comment to add to this metric.</param>
        public void AddComment(string comment)
        {
            comments.Add(comment);
        }

        /// <summary>This will get all comments.</summary>
        public List<string> Comments
        {
            get => comments;
        }

        /// <summary>This will get the version of the AFM document.</summary>
        public float AFMVersion
        {
            get => afmVersion;
            set => afmVersion = value;
        }

        /// <summary>This will get the metricSets attribute.</summary>
        public int MetricSets
        {
            get => metricSets;
            set
            {
                if (value < 0 || value > 2)
                {
                    throw new ArgumentException("The metricSets attribute must be in the "
                            + "set {0,1,2} and not '" + value + "'");
                }
                metricSets = value;
            }
        }

        public string FontName
        {
            get => fontName;
            set => fontName = value;
        }

        public string FullName
        {
            get => fullName;
            set => fullName = value;
        }

        public string FamilyName
        {
            get => familyName;
            set => familyName = value;
        }

        public string Weight
        {
            get => weight;
            set => weight = value;
        }

        public SKRect FontBBox
        {
            get => fontBBox;
            set => fontBBox = value;
        }

        public string Notice
        {
            get => notice;
            set => notice = value;
        }

        public string EncodingScheme
        {
            get => encodingScheme;
            set => encodingScheme = value;
        }

        public int MappingScheme
        {
            get => mappingScheme;
            set => mappingScheme = value;
        }

        public int EscChar
        {
            get => escChar;
            set => escChar = value;
        }

        public string CharacterSet
        {
            get => characterSet;
            set => characterSet = value;
        }

        public int Characters
        {
            get => characters;
            set => characters = value;
        }

        public bool IsBaseFont
        {
            get => isBaseFont;
            set => isBaseFont = value;
        }

        public float[] VVector
        {
            get => vVector;
            set => vVector = value;
        }

        public bool IsFixedV
        {
            get => isFixedV ?? vVector != null;
            set => isFixedV = value;
        }

        public float CapHeight
        {
            get => capHeight;
            set => capHeight = value;
        }

        public float XHeight
        {
            get => xHeight;
            set => xHeight = value;
        }

        public float Ascender
        {
            get => ascender;
            set => ascender = value;
        }

        public float Descender
        {
            get => descender;
            set => descender = value;
        }

        public string FontVersion
        {
            get => fontVersion;
            set => fontVersion = value;
        }

        public float UnderlinePosition
        {
            get => underlinePosition;
            set => underlinePosition = value;
        }

        public float UnderlineThickness
        {
            get => underlineThickness;
            set => underlineThickness = value;
        }

        public float ItalicAngle
        {
            get => italicAngle;
            set => italicAngle = value;
        }

        public float[] CharWidth
        {
            get => charWidth;
            set => charWidth = value;
        }

        public bool IsFixedPitch
        {
            get => isFixedPitch;
            set => isFixedPitch = value;
        }

        public List<CharMetric> CharMetrics
        {
            get => charMetrics;
            set
            {
                charMetrics = value;
                charMetricsMap = new Dictionary<string, CharMetric>(charMetrics.Count, StringComparer.Ordinal);
                foreach (var metric in charMetrics)
                    charMetricsMap[metric.Name] = metric;
            }
        }

        /// <summary>This will add another character metric.</summary>
        /// <param name="metric">The character metric to add.</param>
        public void AddCharMetric(CharMetric metric)
        {
            charMetrics.Add(metric);
            charMetricsMap[metric.Name] = metric;
        }

        public List<TrackKern> TrackKern
        {
            get => trackKern;
            set => trackKern = value;
        }

        /// <summary>This will add another track kern.</summary>
        /// <param name="kern">The track kerning data.</param>
        public void AddTrackKern(TrackKern kern)
        {
            trackKern.Add(kern);
        }

        public List<Composite> Composites
        {
            get => composites;
            set => composites = value;
        }

        /// <summary>This will add a single composite part to the picture.</summary>
        /// <param name="composite">The composite info to add.</param>
        public void AddComposite(Composite composite)
        {
            composites.Add(composite);
        }

        public List<KernPair> KernPairs
        {
            get => kernPairs;
            set => kernPairs = value;
        }

        /// <summary>This will add a kern pair.</summary>
        /// <param name="kernPair">The kern pair to add.</param>
        public void AddKernPair(KernPair kernPair)
        {
            kernPairs.Add(kernPair);
        }

        public List<KernPair> KernPairs0
        {
            get => kernPairs0;
            set => kernPairs0 = value;
        }

        /// <summary>This will add a kern pair.</summary>
        /// <param name="kernPair">The kern pair to add.</param>
        public void AddKernPair0(KernPair kernPair)
        {
            kernPairs0.Add(kernPair);
        }

        public List<KernPair> KernPairs1
        {
            get => kernPairs1;
            set => kernPairs1 = value;
        }

        /// <summary>This will add a kern pair.</summary>
        /// <param name="kernPair">The kern pair to add.</param>
        public void AddKernPair1(KernPair kernPair)
        {
            kernPairs1.Add(kernPair);
        }

        public float StandardHorizontalWidth
        {
            get => standardHorizontalWidth;
            set => standardHorizontalWidth = value;
        }

        public float StandardVerticalWidth
        {
            get => standardVerticalWidth;
            set => standardVerticalWidth = value;
        }

    }
}