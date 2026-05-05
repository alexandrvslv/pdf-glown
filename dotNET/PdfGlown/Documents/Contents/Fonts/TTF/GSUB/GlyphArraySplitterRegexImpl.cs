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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfGlown.Documents.Contents.Fonts.TTF.GSUB
{

    /// <summary>
    /// This is an in-efficient implementation based on regex, which helps split the array.
    /// @author Palash Ray
    /// </summary>
    public class GlyphArraySplitterRegexImpl : IGlyphArraySplitter
    {
        private static readonly char[] GLYPH_ID_SEPARATOR = new[] { '_' };

        private readonly CompoundCharacterTokenizer compoundCharacterTokenizer;

        public GlyphArraySplitterRegexImpl(ICollection<HashList<ushort>> matchers)
        {
            compoundCharacterTokenizer = new CompoundCharacterTokenizer(GetMatchersAsStrings(matchers));
        }

        public List<HashList<ushort>> Split(HashList<ushort> glyphIds)
        {
            string originalGlyphsAsText = ConvertGlyphIdsToString(glyphIds);
            List<string> tokens = compoundCharacterTokenizer.Tokenize(originalGlyphsAsText);

            var modifiedGlyphs = new List<HashList<ushort>>();
            foreach (var token in tokens) modifiedGlyphs.Add(ConvertGlyphIdsToList(token));
            return modifiedGlyphs;
        }

        private ISet<string> GetMatchersAsStrings(ICollection<HashList<ushort>> matchers)
        {
            ISet<string> stringMatchers = new SortedSet<string>(StrangeStringComparer.Instance);
            foreach (var glyphIds in matchers) stringMatchers.Add(ConvertGlyphIdsToString(glyphIds));
            return stringMatchers;
        }

        private class StrangeStringComparer : IComparer<string>
        {
            public static readonly StrangeStringComparer Instance = new StrangeStringComparer();
            public int Compare(string s1, string s2)
            {
                // comparator to ensure that strings with the same beginning
                // put the larger string first        
                if (s1.Length == s2.Length)
                {
                    return string.Compare(s2, s1, StringComparison.Ordinal);
                }
                return s2.Length.CompareTo(s1.Length);
            }
        }

        private string ConvertGlyphIdsToString(HashList<ushort> glyphIds)
        {
            var sb = new StringBuilder(20);
            sb.Append(GLYPH_ID_SEPARATOR);
            foreach (var glyphId in glyphIds.Span) 
                sb.Append(glyphId).Append(GLYPH_ID_SEPARATOR);
            return sb.ToString();
        }

        private HashList<ushort> ConvertGlyphIdsToList(string glyphIdsAsString)
        {
            var array = glyphIdsAsString.Split(GLYPH_ID_SEPARATOR, StringSplitOptions.RemoveEmptyEntries)
                                                        .Select(x => x.Trim())
                                                        .Where(x => x.Length > 0)
                                                        .Select (x =>ushort.Parse(x))
                                                        .ToArray();
            return (HashList<ushort>)array;
        }

    }
}