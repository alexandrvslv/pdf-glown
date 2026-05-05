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
    /// <summary>This is specialized CFFCharset.It's used if the CharsetId of a font is set to 0.
    /// @author Villu Ruusmann</summary>
    public sealed class CFFISOAdobeCharset : CFFCharsetType1
    {

        private CFFISOAdobeCharset()
        {
            int gid = 0;
            AddSID(gid++, 0, ".notdef");
            AddSID(gid++, 1, "space");
            AddSID(gid++, 2, "exclam");
            AddSID(gid++, 3, "quotedbl");
            AddSID(gid++, 4, "numbersign");
            AddSID(gid++, 5, "dollar");
            AddSID(gid++, 6, "percent");
            AddSID(gid++, 7, "ampersand");
            AddSID(gid++, 8, "quoteright");
            AddSID(gid++, 9, "parenleft");
            AddSID(gid++, 10, "parenright");
            AddSID(gid++, 11, "asterisk");
            AddSID(gid++, 12, "plus");
            AddSID(gid++, 13, "comma");
            AddSID(gid++, 14, "hyphen");
            AddSID(gid++, 15, "period");
            AddSID(gid++, 16, "slash");
            AddSID(gid++, 17, "zero");
            AddSID(gid++, 18, "one");
            AddSID(gid++, 19, "two");
            AddSID(gid++, 20, "three");
            AddSID(gid++, 21, "four");
            AddSID(gid++, 22, "five");
            AddSID(gid++, 23, "six");
            AddSID(gid++, 24, "seven");
            AddSID(gid++, 25, "eight");
            AddSID(gid++, 26, "nine");
            AddSID(gid++, 27, "colon");
            AddSID(gid++, 28, "semicolon");
            AddSID(gid++, 29, "less");
            AddSID(gid++, 30, "equal");
            AddSID(gid++, 31, "greater");
            AddSID(gid++, 32, "question");
            AddSID(gid++, 33, "at");
            AddSID(gid++, 34, "A");
            AddSID(gid++, 35, "B");
            AddSID(gid++, 36, "C");
            AddSID(gid++, 37, "D");
            AddSID(gid++, 38, "E");
            AddSID(gid++, 39, "F");
            AddSID(gid++, 40, "G");
            AddSID(gid++, 41, "H");
            AddSID(gid++, 42, "I");
            AddSID(gid++, 43, "J");
            AddSID(gid++, 44, "K");
            AddSID(gid++, 45, "L");
            AddSID(gid++, 46, "M");
            AddSID(gid++, 47, "N");
            AddSID(gid++, 48, "O");
            AddSID(gid++, 49, "P");
            AddSID(gid++, 50, "Q");
            AddSID(gid++, 51, "R");
            AddSID(gid++, 52, "S");
            AddSID(gid++, 53, "T");
            AddSID(gid++, 54, "U");
            AddSID(gid++, 55, "V");
            AddSID(gid++, 56, "W");
            AddSID(gid++, 57, "X");
            AddSID(gid++, 58, "Y");
            AddSID(gid++, 59, "Z");
            AddSID(gid++, 60, "bracketleft");
            AddSID(gid++, 61, "backslash");
            AddSID(gid++, 62, "bracketright");
            AddSID(gid++, 63, "asciicircum");
            AddSID(gid++, 64, "underscore");
            AddSID(gid++, 65, "quoteleft");
            AddSID(gid++, 66, "a");
            AddSID(gid++, 67, "b");
            AddSID(gid++, 68, "c");
            AddSID(gid++, 69, "d");
            AddSID(gid++, 70, "e");
            AddSID(gid++, 71, "f");
            AddSID(gid++, 72, "g");
            AddSID(gid++, 73, "h");
            AddSID(gid++, 74, "i");
            AddSID(gid++, 75, "j");
            AddSID(gid++, 76, "k");
            AddSID(gid++, 77, "l");
            AddSID(gid++, 78, "m");
            AddSID(gid++, 79, "n");
            AddSID(gid++, 80, "o");
            AddSID(gid++, 81, "p");
            AddSID(gid++, 82, "q");
            AddSID(gid++, 83, "r");
            AddSID(gid++, 84, "s");
            AddSID(gid++, 85, "t");
            AddSID(gid++, 86, "u");
            AddSID(gid++, 87, "v");
            AddSID(gid++, 88, "w");
            AddSID(gid++, 89, "x");
            AddSID(gid++, 90, "y");
            AddSID(gid++, 91, "z");
            AddSID(gid++, 92, "braceleft");
            AddSID(gid++, 93, "bar");
            AddSID(gid++, 94, "braceright");
            AddSID(gid++, 95, "asciitilde");
            AddSID(gid++, 96, "exclamdown");
            AddSID(gid++, 97, "cent");
            AddSID(gid++, 98, "sterling");
            AddSID(gid++, 99, "fraction");
            AddSID(gid++, 100, "yen");
            AddSID(gid++, 101, "florin");
            AddSID(gid++, 102, "section");
            AddSID(gid++, 103, "currency");
            AddSID(gid++, 104, "quotesingle");
            AddSID(gid++, 105, "quotedblleft");
            AddSID(gid++, 106, "guillemotleft");
            AddSID(gid++, 107, "guilsinglleft");
            AddSID(gid++, 108, "guilsinglright");
            AddSID(gid++, 109, "fi");
            AddSID(gid++, 110, "fl");
            AddSID(gid++, 111, "endash");
            AddSID(gid++, 112, "dagger");
            AddSID(gid++, 113, "daggerdbl");
            AddSID(gid++, 114, "periodcentered");
            AddSID(gid++, 115, "paragraph");
            AddSID(gid++, 116, "bullet");
            AddSID(gid++, 117, "quotesinglbase");
            AddSID(gid++, 118, "quotedblbase");
            AddSID(gid++, 119, "quotedblright");
            AddSID(gid++, 120, "guillemotright");
            AddSID(gid++, 121, "ellipsis");
            AddSID(gid++, 122, "perthousand");
            AddSID(gid++, 123, "questiondown");
            AddSID(gid++, 124, "grave");
            AddSID(gid++, 125, "acute");
            AddSID(gid++, 126, "circumflex");
            AddSID(gid++, 127, "tilde");
            AddSID(gid++, 128, "macron");
            AddSID(gid++, 129, "breve");
            AddSID(gid++, 130, "dotaccent");
            AddSID(gid++, 131, "dieresis");
            AddSID(gid++, 132, "ring");
            AddSID(gid++, 133, "cedilla");
            AddSID(gid++, 134, "hungarumlaut");
            AddSID(gid++, 135, "ogonek");
            AddSID(gid++, 136, "caron");
            AddSID(gid++, 137, "emdash");
            AddSID(gid++, 138, "AE");
            AddSID(gid++, 139, "ordfeminine");
            AddSID(gid++, 140, "Lslash");
            AddSID(gid++, 141, "Oslash");
            AddSID(gid++, 142, "OE");
            AddSID(gid++, 143, "ordmasculine");
            AddSID(gid++, 144, "ae");
            AddSID(gid++, 145, "dotlessi");
            AddSID(gid++, 146, "lslash");
            AddSID(gid++, 147, "oslash");
            AddSID(gid++, 148, "oe");
            AddSID(gid++, 149, "germandbls");
            AddSID(gid++, 150, "onesuperior");
            AddSID(gid++, 151, "logicalnot");
            AddSID(gid++, 152, "mu");
            AddSID(gid++, 153, "trademark");
            AddSID(gid++, 154, "Eth");
            AddSID(gid++, 155, "onehalf");
            AddSID(gid++, 156, "plusminus");
            AddSID(gid++, 157, "Thorn");
            AddSID(gid++, 158, "onequarter");
            AddSID(gid++, 159, "divide");
            AddSID(gid++, 160, "brokenbar");
            AddSID(gid++, 161, "degree");
            AddSID(gid++, 162, "thorn");
            AddSID(gid++, 163, "threequarters");
            AddSID(gid++, 164, "twosuperior");
            AddSID(gid++, 165, "registered");
            AddSID(gid++, 166, "minus");
            AddSID(gid++, 167, "eth");
            AddSID(gid++, 168, "multiply");
            AddSID(gid++, 169, "threesuperior");
            AddSID(gid++, 170, "copyright");
            AddSID(gid++, 171, "Aacute");
            AddSID(gid++, 172, "Acircumflex");
            AddSID(gid++, 173, "Adieresis");
            AddSID(gid++, 174, "Agrave");
            AddSID(gid++, 175, "Aring");
            AddSID(gid++, 176, "Atilde");
            AddSID(gid++, 177, "Ccedilla");
            AddSID(gid++, 178, "Eacute");
            AddSID(gid++, 179, "Ecircumflex");
            AddSID(gid++, 180, "Edieresis");
            AddSID(gid++, 181, "Egrave");
            AddSID(gid++, 182, "Iacute");
            AddSID(gid++, 183, "Icircumflex");
            AddSID(gid++, 184, "Idieresis");
            AddSID(gid++, 185, "Igrave");
            AddSID(gid++, 186, "Ntilde");
            AddSID(gid++, 187, "Oacute");
            AddSID(gid++, 188, "Ocircumflex");
            AddSID(gid++, 189, "Odieresis");
            AddSID(gid++, 190, "Ograve");
            AddSID(gid++, 191, "Otilde");
            AddSID(gid++, 192, "Scaron");
            AddSID(gid++, 193, "Uacute");
            AddSID(gid++, 194, "Ucircumflex");
            AddSID(gid++, 195, "Udieresis");
            AddSID(gid++, 196, "Ugrave");
            AddSID(gid++, 197, "Yacute");
            AddSID(gid++, 198, "Ydieresis");
            AddSID(gid++, 199, "Zcaron");
            AddSID(gid++, 200, "aacute");
            AddSID(gid++, 201, "acircumflex");
            AddSID(gid++, 202, "adieresis");
            AddSID(gid++, 203, "agrave");
            AddSID(gid++, 204, "aring");
            AddSID(gid++, 205, "atilde");
            AddSID(gid++, 206, "ccedilla");
            AddSID(gid++, 207, "eacute");
            AddSID(gid++, 208, "ecircumflex");
            AddSID(gid++, 209, "edieresis");
            AddSID(gid++, 210, "egrave");
            AddSID(gid++, 211, "iacute");
            AddSID(gid++, 212, "icircumflex");
            AddSID(gid++, 213, "idieresis");
            AddSID(gid++, 214, "igrave");
            AddSID(gid++, 215, "ntilde");
            AddSID(gid++, 216, "oacute");
            AddSID(gid++, 217, "ocircumflex");
            AddSID(gid++, 218, "odieresis");
            AddSID(gid++, 219, "ograve");
            AddSID(gid++, 220, "otilde");
            AddSID(gid++, 221, "scaron");
            AddSID(gid++, 222, "uacute");
            AddSID(gid++, 223, "ucircumflex");
            AddSID(gid++, 224, "udieresis");
            AddSID(gid++, 225, "ugrave");
            AddSID(gid++, 226, "yacute");
            AddSID(gid++, 227, "ydieresis");
            AddSID(gid++, 228, "zcaron");
        }

        public static readonly CFFISOAdobeCharset Instance = new();

        static CFFISOAdobeCharset()
        {
            
        }
    }
}