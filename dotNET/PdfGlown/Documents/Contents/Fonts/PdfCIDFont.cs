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
using PdfGlown.Bytes;
using PdfGlown.Objects;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents.Fonts
{
    /// <summary>
    /// A CIDFont.A CIDFont is a PDF object that contains information about a CIDFont program.Although
    /// its Type value is Font, a CIDFont is not actually a font.
    /// <p>It is not usually necessary to use this class directly, prefer <see cref="PdfType0Font"/>.</p>
    /// @author Ben Litchfield
    /// </summary>
    public class PdfCIDFont : PdfFont
    {
        private static readonly string UnsupportedMessage = "Use PdfCIDFontWrappers Instread";
        public PdfCIDFont(PdfDocument context)
            : this(context, new() { { PdfName.Type, PdfName.Font } })
        { }

        public PdfCIDFont(PdfDocument context, Dictionary<PdfName, PdfDirectObject> fontObject)
            : base(context, fontObject)
        { }

        internal PdfCIDFont(Dictionary<PdfName, PdfDirectObject> fontObject)
            : base(fontObject)
        { }

        /// <summary>The PostScript name of the font.</summary>
        public CIDSystemInfo CIDSystemInfo
        {
            get => Get<CIDSystemInfo>(PdfName.CIDSystemInfo);
            set => SetDirect(PdfName.CIDSystemInfo, value);
        }

        public override PdfArray Widths
        {
            get => Get<PdfArray>(PdfName.W);
            set => Set(PdfName.W, value);
        }

        public PdfArray VerticalDefaultWidth
        {
            get => Get<PdfArray>(PdfName.DW2);
            set => Set(PdfName.DW2, value);
        }

        public PdfArray VerticaltWidths
        {
            get => Get<PdfArray>(PdfName.W2);
            set => Set(PdfName.W2, value);
        }

        public PdfDirectObject CIDToGIDMap
        {
            get => Get<PdfDirectObject>(PdfName.CIDToGIDMap);
            set => Set(PdfName.CIDToGIDMap, value);
        }

        public override bool WillBeSubset => false;        
       

        public override void AddToSubset(int codePoint)
        {
            throw new NotSupportedException(UnsupportedMessage);
        }

        public override void Encode(Span<byte> bytes, int unicode)
        {
            throw new NotSupportedException(UnsupportedMessage);
        }

        public override int GetBytesCount(int code)
        {
            throw new NotSupportedException(UnsupportedMessage);
        }

        public override float GetHeight(int code)
        {
            throw new NotSupportedException(UnsupportedMessage);
        }

        public override SKPath GetNormalizedPath(int code)
        {
            throw new NotSupportedException(UnsupportedMessage);
        }

        public override SKPath GetPath(int code)
        {
            throw new NotSupportedException(UnsupportedMessage);
        }

        public override float GetWidthFromFont(int code)
        {
            throw new NotSupportedException(UnsupportedMessage);
        }

        public override bool HasExplicitWidth(int code)
        {
            throw new NotSupportedException(UnsupportedMessage);
        }

        public override bool HasGlyph(int code)
        {
            throw new NotSupportedException(UnsupportedMessage);
        }

        public override int ReadCode(IInputStream input, out ReadOnlySpan<byte> bytes)
        {
            throw new NotSupportedException(UnsupportedMessage);
        }

        public override int ReadCode(ReadOnlySpan<byte> bytes)
        {
            throw new NotSupportedException(UnsupportedMessage);
        }

        public override void Subset()
        {
            throw new NotSupportedException(UnsupportedMessage);
        }

        protected override SKRect GenerateBBox()
        {
            throw new NotSupportedException(UnsupportedMessage);
        }

        protected override float GetStandard14Width(int code)
        {
            throw new NotSupportedException(UnsupportedMessage);
        }
    }
}