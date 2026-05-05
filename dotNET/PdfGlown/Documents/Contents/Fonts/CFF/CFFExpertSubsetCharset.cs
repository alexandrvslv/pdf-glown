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
namespace PdfGlown.Documents.Contents.Fonts.CCF
{

    /**
     * This is specialized CFFCharset. It's used if the CharsetId of a font is set to 2.
     * 
     * @author Villu Ruusmann
     */
    public sealed class CFFExpertSubsetCharset : CFFCharsetType1
    {
        private CFFExpertSubsetCharset()
        {
            int gid = 0;
            AddSID(gid++, 0, ".notdef");
            AddSID(gid++, 1, "space");
            AddSID(gid++, 231, "dollaroldstyle");
            AddSID(gid++, 232, "dollarsuperior");
            AddSID(gid++, 235, "parenleftsuperior");
            AddSID(gid++, 236, "parenrightsuperior");
            AddSID(gid++, 237, "twodotenleader");
            AddSID(gid++, 238, "onedotenleader");
            AddSID(gid++, 13, "comma");
            AddSID(gid++, 14, "hyphen");
            AddSID(gid++, 15, "period");
            AddSID(gid++, 99, "fraction");
            AddSID(gid++, 239, "zerooldstyle");
            AddSID(gid++, 240, "oneoldstyle");
            AddSID(gid++, 241, "twooldstyle");
            AddSID(gid++, 242, "threeoldstyle");
            AddSID(gid++, 243, "fouroldstyle");
            AddSID(gid++, 244, "fiveoldstyle");
            AddSID(gid++, 245, "sixoldstyle");
            AddSID(gid++, 246, "sevenoldstyle");
            AddSID(gid++, 247, "eightoldstyle");
            AddSID(gid++, 248, "nineoldstyle");
            AddSID(gid++, 27, "colon");
            AddSID(gid++, 28, "semicolon");
            AddSID(gid++, 249, "commasuperior");
            AddSID(gid++, 250, "threequartersemdash");
            AddSID(gid++, 251, "periodsuperior");
            AddSID(gid++, 253, "asuperior");
            AddSID(gid++, 254, "bsuperior");
            AddSID(gid++, 255, "centsuperior");
            AddSID(gid++, 256, "dsuperior");
            AddSID(gid++, 257, "esuperior");
            AddSID(gid++, 258, "isuperior");
            AddSID(gid++, 259, "lsuperior");
            AddSID(gid++, 260, "msuperior");
            AddSID(gid++, 261, "nsuperior");
            AddSID(gid++, 262, "osuperior");
            AddSID(gid++, 263, "rsuperior");
            AddSID(gid++, 264, "ssuperior");
            AddSID(gid++, 265, "tsuperior");
            AddSID(gid++, 266, "ff");
            AddSID(gid++, 109, "fi");
            AddSID(gid++, 110, "fl");
            AddSID(gid++, 267, "ffi");
            AddSID(gid++, 268, "ffl");
            AddSID(gid++, 269, "parenleftinferior");
            AddSID(gid++, 270, "parenrightinferior");
            AddSID(gid++, 272, "hyphensuperior");
            AddSID(gid++, 300, "colonmonetary");
            AddSID(gid++, 301, "onefitted");
            AddSID(gid++, 302, "rupiah");
            AddSID(gid++, 305, "centoldstyle");
            AddSID(gid++, 314, "figuredash");
            AddSID(gid++, 315, "hypheninferior");
            AddSID(gid++, 158, "onequarter");
            AddSID(gid++, 155, "onehalf");
            AddSID(gid++, 163, "threequarters");
            AddSID(gid++, 320, "oneeighth");
            AddSID(gid++, 321, "threeeighths");
            AddSID(gid++, 322, "fiveeighths");
            AddSID(gid++, 323, "seveneighths");
            AddSID(gid++, 324, "onethird");
            AddSID(gid++, 325, "twothirds");
            AddSID(gid++, 326, "zerosuperior");
            AddSID(gid++, 150, "onesuperior");
            AddSID(gid++, 164, "twosuperior");
            AddSID(gid++, 169, "threesuperior");
            AddSID(gid++, 327, "foursuperior");
            AddSID(gid++, 328, "fivesuperior");
            AddSID(gid++, 329, "sixsuperior");
            AddSID(gid++, 330, "sevensuperior");
            AddSID(gid++, 331, "eightsuperior");
            AddSID(gid++, 332, "ninesuperior");
            AddSID(gid++, 333, "zeroinferior");
            AddSID(gid++, 334, "oneinferior");
            AddSID(gid++, 335, "twoinferior");
            AddSID(gid++, 336, "threeinferior");
            AddSID(gid++, 337, "fourinferior");
            AddSID(gid++, 338, "fiveinferior");
            AddSID(gid++, 339, "sixinferior");
            AddSID(gid++, 340, "seveninferior");
            AddSID(gid++, 341, "eightinferior");
            AddSID(gid++, 342, "nineinferior");
            AddSID(gid++, 343, "centinferior");
            AddSID(gid++, 344, "dollarinferior");
            AddSID(gid++, 345, "periodinferior");
            AddSID(gid++, 346, "commainferior");
        }

        public static readonly CFFExpertSubsetCharset Instance = new CFFExpertSubsetCharset();

        static CFFExpertSubsetCharset()
        {
            
        }
    }
}