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
using PdfGlown.Documents.Contents.Fonts.AFM;
using PdfGlown.Documents.Contents.Fonts.Type1;
using PdfGlown.Objects;
using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents.Fonts
{
    /// <summary>Embedded PDType1Font builder.Helper class to populate a PDType1Font from a PFB and AFM.
    /// @author Michael Niedermair
    /// </summary>
    internal class FontType1Embedder
    {
        private readonly Encoding fontEncoding;
        private readonly Type1Font type1;

        /// <summary>This will load a PFB to be embedded into a document.</summary>
        /// <param name="context">The PDF document that will hold the embedded font.</param>
        /// <param name="dict">The Font dictionary to write to.</param>
        /// <param name="pfbStream">The pfb input</param>
        /// <param name="encoding"></param>
        public FontType1Embedder(PdfDocument context, PdfFont dict, Bytes.IInputStream pfbStream, Encoding encoding)
        {
            dict[PdfName.Subtype] = PdfName.Type1;

            // read the pfb
            var pfbBytes = pfbStream.AsMemory();
            var pfbParser = new PfbParser(pfbBytes);
            type1 = Type1Font.CreateWithPFB(pfbBytes, pfbParser);

            if (encoding == null)
            {
                fontEncoding = Type1Encoding.FromFontBox(type1.Encoding);
            }
            else
            {
                fontEncoding = encoding;
            }

            // build font descriptor
            var fd = BuildFontDescriptor(type1, context);

            var fontStream = new FontFile(context, pfbParser.GetInputStream());
            fontStream.Set(PdfName.Length, pfbParser.Size);
            for (int i = 0; i < pfbParser.Lengths.Length; i++)
            {
                fontStream.Set(PdfName.Get("Length" + (i + 1), true), pfbParser.Lengths[i]);
            }
            fd.FontFile = fontStream;

            // set the values
            dict[PdfName.FontDescriptor] = fd.Reference;
            dict[PdfName.BaseFont] = PdfName.Get(type1.Name);

            // widths
            var widths = new PdfArrayImpl(256);
            for (int code = 0; code <= 255; code++)
            {
                string name = fontEncoding.GetName(code);
                int width = (int)Math.Round(type1.GetWidth(name));
                widths.Add(width);
            }

            dict.Set(PdfName.FirstChar, 0);
            dict.Set(PdfName.LastChar, 255);
            dict[PdfName.Widths] = widths;
            dict[PdfName.Encoding] = encoding.GetPdfObject();
        }

        /// <summary>Returns a FontDescriptor for the given PFB.</summary>
        /// <param name="type1"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static FontDescriptor BuildFontDescriptor(Type1Font type1, PdfDocument context)
        {
            bool isSymbolic = type1.Encoding is BuiltInEncoding;

            var fd = new FontDescriptor(context)
            {
                FontName = type1.Name,
                FontFamily = type1.FamilyName,
                NonSymbolic = !isSymbolic,
                Symbolic = isSymbolic,
                FontBBox = new PdfRectangle(type1.FontBBox),
                ItalicAngle = type1.ItalicAngle,
                Ascent = type1.FontBBox.Top,
                Descent = type1.FontBBox.Bottom,
                CapHeight = type1.BlueValues[2],
                StemV = 0 // for PDF/A
            };
            return fd;
        }

        /// <summary>Returns a FontDescriptor for the given AFM.Used only for Standard 14 fonts.</summary>
        /// <param name="metrics">AFM</param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static FontDescriptor BuildFontDescriptor(FontMetrics metrics, PdfDocument context)
        {
            bool isSymbolic = metrics.EncodingScheme.Equals("FontSpecific", StringComparison.Ordinal);

            var fd = new FontDescriptor(context)
            {
                FontName = metrics.FontName,
                FontFamily = metrics.FamilyName,
                NonSymbolic = !isSymbolic,
                Symbolic = isSymbolic,
                FontBBox = new PdfRectangle(metrics.FontBBox),
                ItalicAngle = metrics.ItalicAngle,
                Ascent = metrics.Ascender,
                Descent = metrics.Descender,
                CapHeight = metrics.CapHeight,
                XHeight = metrics.XHeight,
                AvgWidth = metrics.GetAverageCharacterWidth(),
                CharSet = metrics.CharacterSet,
                StemV = 0 // for PDF/A
            };
            return fd;
        }

        /// <summary>Returns the font's encoding.</summary>
        public Encoding FontEncoding
        {
            get => fontEncoding;
        }

        /// <summary>Returns the font's glyph list.</summary>
        public GlyphMapping GlyphList
        {
            get => GlyphMapping.Default;
        }

        /// <summary>Returns the Type 1 font.</summary>
        public Type1Font Type1Font
        {
            get => type1;
        }
    }
}
