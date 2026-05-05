/*
 * https://github.com/apache/pdfbox
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

namespace PdfGlown.Documents.Contents.Fonts.CCF
{
    /// <summary>
    /// This class represents a CFF operator.
    /// @author Villu Ruusmann
    /// </summary>
    public sealed class CFFOperator
    {
        private static void Register(int b0, int b1, string name) => Register(CalculateKey(b0, b1), name);

        private static void Register(int key, string name)
        {
            keyMap[key] = name;
            nameMap[name] = key;
        }

        /// <summary>Returns the operator corresponding to the given key.</summary>
        /// <param name="key">the given key</param>
        /// <returns>the corresponding operator</returns>
        public static string GetOperator(int key)
        {
            return keyMap.TryGetValue(key, out var cFFOperator) ? cFFOperator : null;
        }

        public static string GetOperator(int b0, int b1) => GetOperator(CalculateKey(b0, b1));

        private static int CalculateKey(int b0, int b1) => (b0 << 8) | b1;

        /// <summary>Returns the operator corresponding to the given name.</summary>
        /// <param name="name">the given name</param>
        /// <returns>the corresponding operator</returns>
        public static int? GetOperator(string name)
        {
            return nameMap.TryGetValue(name, out var cFFOperator) ? cFFOperator : null;
        }

        /// <summary>
        /// @author Villu Ruusmann
        /// This class is a holder for a key value.It consists of one or two bytes.
        /// </summary>
        private static Dictionary<int, string> keyMap = new(52);
        private static Dictionary<string, int> nameMap = new(52, StringComparer.Ordinal);

        static CFFOperator()
        {
            // Top DICT
            Register(0, "version");
            Register(1, "Notice");
            Register(12, 0, "Copyright");
            Register(2, "FullName");
            Register(3, "FamilyName");
            Register(4, "Weight");
            Register(12, 1, "isFixedPitch");
            Register(12, 2, "ItalicAngle");
            Register(12, 3, "UnderlinePosition");
            Register(12, 4, "UnderlineThickness");
            Register(12, 5, "PaintType");
            Register(12, 6, "CharstringType");
            Register(12, 7, "FontMatrix");
            Register(13, "UniqueID");
            Register(5, "FontBBox");
            Register(12, 8, "StrokeWidth");
            Register(14, "XUID");
            Register(15, "charset");
            Register(16, "Encoding");
            Register(17, "CharStrings");
            Register(18, "Private");
            Register(12, 20, "SyntheticBase");
            Register(12, 21, "PostScript");
            Register(12, 22, "BaseFontName");
            Register(12, 23, "BaseFontBlend");
            Register(12, 30, "ROS");
            Register(12, 31, "CIDFontVersion");
            Register(12, 32, "CIDFontRevision");
            Register(12, 33, "CIDFontType");
            Register(12, 34, "CIDCount");
            Register(12, 35, "UIDBase");
            Register(12, 36, "FDArray");
            Register(12, 37, "FDSelect");
            Register(12, 38, "FontName");

            // Private DICT
            Register(6, "BlueValues");
            Register(7, "OtherBlues");
            Register(8, "FamilyBlues");
            Register(9, "FamilyOtherBlues");
            Register(12, 9, "BlueScale");
            Register(12, 10, "BlueShift");
            Register(12, 11, "BlueFuzz");
            Register(10, "StdHW");
            Register(11, "StdVW");
            Register(12, 12, "StemSnapH");
            Register(12, 13, "StemSnapV");
            Register(12, 14, "ForceBold");
            Register(12, 15, "LanguageGroup");
            Register(12, 16, "ExpansionFactor");
            Register(12, 17, "initialRandomSeed");
            Register(19, "Subrs");
            Register(20, "defaultWidthX");
            Register(21, "nominalWidthX");
        }
    }
}
