/*
  Copyright 2010-2015 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)

  This file should be part of the source code distribution of "PDF Glown library" (the
  Program): see the accompanying README files for more info.

  This Program is free software; you can redistribute it and/or modify it under the terms
  of the GNU Lesser General Public License as published by the Free Software Foundation;
  either version 3 of the License, or (at your option) any later version.

  This Program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY,
  either expressed or implied; without even the implied warranty of MERCHANTABILITY or
  FITNESS FOR A PARTICULAR PURPOSE. See the License for more details.

  You should have received a copy of the GNU Lesser General Public License along with this
  Program (see README files); if not, go to the GNU website (http://www.gnu.org/licenses/).

  Redistribution and use, with or without modification, are permitted provided that such
  redistributions retain the above copyright notice, license and disclaimer, along with
  this list of conditions.
*/

using PdfGlown.Bytes;
using PdfGlown.Objects;
using PdfGlown.Util.Math;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PdfGlown.Documents.Contents.Fonts
{
    /// <summary>Type 3 font [PDF:1.6:5.5.4].</summary>
    [Pdf(VersionEnum.PDF10)]
    public sealed class PdfType3Font : PdfSimpleFont
    {
        internal PdfType3Font(PdfDocument context)
            : base(context)
        { }

        internal PdfType3Font(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        public override string Name
        {
            get => GetString(PdfName.Name);
        }

        internal override void AfterParse()
        {
            base.AfterParse();
            ReadEncoding();
        }

        protected override void ReadEncoding()
        {
            var encodingBase = EncodingData;
            if (encodingBase is PdfName encodingName)
            {
                encoding = Encoding.Get(encodingName);
                if (encoding == null)
                {
                    Debug.WriteLine($"warn: Unknown encoding: {encodingName}");
                }
            }
            else if (encodingBase is PdfDictionary dictionary)
            {
                encoding = new DictionaryEncoding(dictionary);
            }
            glyphList = GlyphMapping.Default;
        }

        protected override Encoding ReadEncodingFromFont()
        {
            // Type 3 fonts do not have a built-in encoding
            throw new NotSupportedException("not supported for Type 3 fonts");
        }

        protected override bool? FontSymbolic
        {
            get => false;
        }

        public override BaseFont Font
        {
            // Type 3 fonts do not use FontBox fonts
            get => throw new NotSupportedException("not supported for Type 3 fonts");
        }

        public override bool IsEmbedded
        {
            get => true;
        }

        public override bool IsDamaged
        {
            // there's no font file to load
            get => false;
        }

        public override bool IsStandard14
        {
            get => false;
        }

        /// <summary>Returns the optional resources of the type3 stream.
        /// return the resources bound to be used when parsing the type3 stream
        /// </summary>
        public Resources Resources
        {
            get => Get<Resources>(PdfName.Resources);
        }

        /// <summary>Returns the dictionary containing all streams to be used to render 
        /// the glyphs.</summary>
        public Type3CharProcs CharProcs
        {
            get => GetOrCreate<Type3CharProcs>(PdfName.CharProcs).WithFont(this);
        }

        public override float ScalingFactor => FontMatrix.ScaleX;

        public override SKPath GetPath(int code)
        {
            throw new NotSupportedException("not supported for Type 3 fonts");
        }

        public override SKPath GetPath(string name)
        {
            // Type 3 fonts do not use vector paths
            throw new NotSupportedException("not supported for Type 3 fonts");
        }

        public override SKPath GetNormalizedPath(int code)
        {
            throw new NotSupportedException("not supported for Type 3 fonts");
        }

        public override bool HasGlyph(int code)
        {
            string name = Encoding.GetName(code);
            return GetCharProc(PdfName.Get(name)) != null;
        }

        public override bool HasGlyph(string name)
        {
            return GetCharProc(PdfName.Get(name)) != null;
        }

        public override SKPoint GetDisplacement(int code)
        {
            return FontMatrix.MapVector(base.GetWidth(code), 0);
        }

        protected override float GetAscent() => FontBBox.Bottom;

        protected override float GetDescent() => FontBBox.Top;

        public override float GetWidth(int code)
        {
            int firstChar = FirstChar ?? -1;
            int lastChar = LastChar ?? -1;
            if (Widths.Count > 0 && code >= firstChar && code <= lastChar)
            {
                return Widths.GetNFloat(code - firstChar) ?? 0F;
            }
            else
            {
                return FontDescriptor?.MissingWidth ?? GetWidthFromFont(code);
            }
        }

        public override float GetWidthFromFont(int code)
        {
            Type3CharProc charProc = GetCharProc(code);
            if (charProc == null
                || (charProc.Contents?.Count ?? 0) == 0)
            {
                return 0;
            }
            return charProc.Width ?? 0;
        }

        public override float GetHeight(int code)
        {
            var desc = FontDescriptor;
            if (desc != null)
            {
                // the following values are all more or less accurate at least all are average
                // values. Maybe we'll find another way to get those value for every single glyph
                // in the future if needed
                var bbox = desc.FontBBox;
                float retval = 0;
                if (bbox != null)
                {
                    retval = (float)bbox.Height / 2;
                }
                if (retval.CompareTo(0) == 0)
                {
                    retval = desc.CapHeight ?? 0;
                }
                if (retval.CompareTo(0) == 0)
                {
                    retval = desc.Ascent;
                }
                if (retval.CompareTo(0) == 0)
                {
                    retval = desc.XHeight ?? 0;
                    if (retval > 0)
                    {
                        retval -= desc.Descent;
                    }
                }
                return retval;
            }
            return 0;
        }

        public override int GetBytesCount(int code) => 1;

        public override void Encode(Span<byte> bytes, int unicode)
        {
            throw new NotSupportedException("Not implemented: Type3");
        }

        public override int ReadCode(IInputStream input, out ReadOnlySpan<byte> bytes)
        {
            bytes = input.ReadSpan(1);
            return ReadCode(bytes);
        }

        public override int ReadCode(ReadOnlySpan<byte> bytes)
        {
            return bytes[0];
        }

        protected override SKMatrix GenerateFontMatrix()
        {
            return Get<PdfArray>(PdfName.FontMatrix) is PdfArray array && array.Count > 5
                ? new SKMatrix(
                    array.GetFloat(0), array.GetFloat(1), array.GetFloat(4),
                    array.GetFloat(2), array.GetFloat(3), array.GetFloat(5),
                    0, 0, 1)
                : base.GenerateFontMatrix();
        }

        protected override SKRect GenerateBBox()
        {
            var pdfRect = Get<PdfRectangle>(PdfName.FontBBox);
            var rect = pdfRect?.ToSKRect() ?? SKRect.Empty;
            if (rect.Width == 0 || rect.Height == 0)
            {
                // Plan B: get the max bounding box of the glyphs
                var cp = CharProcs;
                foreach (PdfName name in cp.Keys)
                {
                    var charProc = GetCharProc(name);
                    if (charProc != null)
                    {
                        try
                        {
                            var glyphBBox = charProc.GlyphBox;
                            if (glyphBBox == null)
                            {
                                continue;
                            }
                            rect.Left = Math.Min(rect.Left, glyphBBox.Value.Left);
                            rect.Top = Math.Min(rect.Top, glyphBBox.Value.Top);
                            rect.Right = Math.Max(rect.Right, glyphBBox.Value.Right);
                            rect.Bottom = Math.Max(rect.Bottom, glyphBBox.Value.Bottom);
                        }
                        catch (Exception ex)
                        {
                            // ignore
                            Debug.WriteLine($"debug: error getting the glyph bounding box - font bounding box will be used {ex}");
                        }
                    }
                }
            }
            return rect;
        }

        /// <summary>Returns the stream of the glyph for the given character code</summary>
        /// <param name="code">character code</param>
        /// <returns>the stream to be used to render the glyph</returns>
        public Type3CharProc GetCharProc(int code)
        {
            if (Encoding == null)
                return null;
            string name = Encoding.GetName(code);
            return GetCharProc(PdfName.Get(name));
        }

        private Type3CharProc GetCharProc(PdfName name)
        {
            return CharProcs.TryGetValue(name, out var proc) ? proc: null;
        }

        public override SKPath DrawChar(SKCanvas context, SKPaint fill, SKPaint stroke, char textChar, int code)
        {
            var proc = GetCharProc(code);
            if (proc == null)
            {
                Debug.WriteLine($"info: no Glyph for Code: {code}  Char: '{textChar}'");
                return null;
            }
            var picture = proc.Render();
            context.DrawPicture(picture, fill ?? stroke);
            return null;
        }
    }
}