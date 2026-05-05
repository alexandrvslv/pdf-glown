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

namespace PdfGlown.Documents.Contents.Fonts.TTF.Table.Common
{
    /// <summary>
    /// This class models the
    /// <a href="https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2#coverage-format-1">Coverage format 1</a>
    /// in the Open Type layout common tables.
    /// @author Palash Ray
    /// </summary>
    public class CoverageTableFormat1 : CoverageTable
    {

        private readonly ushort[] glyphArray;

        public CoverageTableFormat1(int coverageFormat, ushort[] glyphArray) : base(coverageFormat)
        {
            this.glyphArray = glyphArray;
        }


        public override int GetCoverageIndex(ushort gid)
        {
            return Array.BinarySearch(glyphArray, gid);
        }


        public override ushort GetGlyphId(int index)
        {
            return glyphArray[index];
        }


        public override int Size
        {
            get => glyphArray.Length;
        }

        public ushort[] GlyphArray
        {
            get => glyphArray;
        }


        public override string ToString()
        {
            return $"CoverageTableFormat1[coverageFormat={CoverageFormat},glyphArray={string.Join(", ", glyphArray)}]";
        }

    }
}