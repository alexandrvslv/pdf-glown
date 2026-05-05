/*
  Copyright 2007-2015 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfGlown.Documents.Contents.Fonts;
using PdfGlown.Documents.Contents.Scanner;
using PdfGlown.Objects;
using PdfGlown.Util.Math;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfGlown.Documents.Contents.Objects
{
    /// <summary>Abstract 'show a text string' operation [PDF:1.6:5.3.2].</summary>
    [Pdf(VersionEnum.PDF10)]
    public abstract class ShowText : Operation, ITextString
    {
        public interface IScanner
        {
            /// <summary>Notifies the scanner about a text character.</summary>
            /// <param name="textChar">Scanned character.</param>
            /// <param name="textCharBox">Bounding box of the scanned character.</param>
            void ScanChar(char textChar, Quad textCharBox);
        }

        public class ShowTextScanner : IScanner
        {
            ITextString wrapper;

            internal ShowTextScanner(ITextString wrapper)
            {
                this.wrapper = wrapper;
                wrapper.Chars.Clear();
                wrapper.Invalidate();
            }

            public void ScanChar(char textChar, Quad textCharQuad)
            {
                wrapper.Chars.Add(new TextChar(textChar, textCharQuad));
            }
        }

        private TextStyle style;
        private List<TextChar> chars;
        private string text;
        private Quad? quad;

        protected ShowText(string @operator) : base(@operator)
        { }

        protected ShowText(string @operator, PdfDirectObject operand) : base(@operator, operand)
        { }

        protected ShowText(string @operator, PdfArray operands) : base(@operator, operands) { }

        /// <summary>Gets/Sets the encoded text.</summary>
        /// <remarks>Text is expressed in native encoding: to resolve it to Unicode, pass it
        /// to the decode method of the corresponding font.</remarks>
        public abstract Memory<byte> TextBytes { get; set; }

        /// <summary>Gets/Sets the encoded text elements along with their adjustments.</summary>
        /// <remarks>Text is expressed in native encoding: to resolve it to Unicode, pass it
        /// to the decode method of the corresponding font.</remarks>
        /// <returns>Each element can be either a byte array or a number:
        ///  <list type="bullet">
        ///   <item>if it's a byte array (encoded text), the operator shows text glyphs;</item>
        ///   <item>if it's a number (glyph adjustment), the operator inversely adjusts the next glyph position
        ///   by that amount (that is: a positive value reduces the distance between consecutive glyphs).</item>
        /// </list>
        /// </returns>
        public abstract IEnumerable<PdfDirectObject> TextElements { get; set; }

        public Quad Quad
        {
            get
            {
                if (quad == null)
                {
                    var result = new Quad();
                    foreach (TextChar textChar in chars)
                    {
                        if (textChar.Quad.IsEmpty)
                            continue;
                        if (result.IsEmpty)
                        { result = textChar.Quad; }
                        else
                        { result.Add(textChar.Quad); }
                    }
                    quad = result;
                }
                return quad.Value;
            }
        }

        /// <summary>Gets the text style.</summary>
        public TextStyle Style => style;

        public string Text
        {
            get
            {
                if (text == null)
                {
                    var textBuilder = new StringBuilder();
                    foreach (TextChar textChar in chars)
                    {
                        textBuilder.Append(textChar.Value);
                    }
                    text = textBuilder.ToString();
                }
                return text;
            }
        }

        public List<TextChar> Chars => chars;

        public override void Scan(GraphicsState state)
        {
            if (chars == null)
            {
                state.TextState.TextBlock.Add(this);
                chars = new List<TextChar>();
            }
            style = new TextStyle(
              state.Font,
              state.FontSize * state.TextState.Tm.ScaleY,
              state.RenderMode,
              state.StrokeColor,
              state.StrokeColorSpace,
              state.FillColor,
              state.FillColorSpace,
              state.Scale * state.TextState.Tm.ScaleX,
              state.TextState.Tm.ScaleY);

            Scan(state, new ShowTextScanner(this));
        }

        private int CalculateCount()
        {
            return Math.Max(2, TextElements.OfType<PdfString>().Sum(x => x.RawValue.Length) / 2);
        }

        public SKRect GetBox(GraphicsState state)
        {
            if (chars == null)
            {
                Scan(state);
            }
            return Quad.GetBounds();
        }

        /// <summary>Executes scanning on this operation.</summary>
        /// <param name="state">Graphics state context.</param>
        /// <param name="textScanner">Scanner to be notified about text contents.
        /// In case it's null, the operation is applied to the graphics state context.</param>
        public virtual void Scan(GraphicsState state, IScanner textScanner)
        {
            //TODO: I really dislike this solution -- it's a temporary hack until the event-driven
            //parsing mechanism is implemented...
            PdfFont font = state.Font ?? state.Scanner.Contents.Document?.LatestFont;
            if (font == null)
                return;
            state.Scanner.Canvas?.Save();
            state.TextState.Tm = font.IsVertical
                ? DrawVertical(font, state, textScanner)
                : DrawHorizontal(font, state, textScanner);
            state.Scanner.Canvas?.Restore();
            state.ApplyClipPath();
        }

        private SKMatrix DrawHorizontal(PdfFont font, GraphicsState state, IScanner textScanner)
        {
            bool wordSpaceSupported = !(font is PdfType0Font);

            var fontSize = state.FontSize;
            var horizontalScaling = state.Scale;
            var wordSpace = wordSpaceSupported ? state.WordSpace : 0;
            var charSpace = state.CharSpace;
            var charAscent = (float)font.GetAscent(fontSize);
            var charDescent = (float)font.GetDescent(fontSize);
            var charHeight = (float)font.GetLineHeight(fontSize);
            SKMatrix fm = font.FontMatrix;
            SKMatrix ctm = state.Ctm;
            SKMatrix tm = state.TextState.Tm;
            var canvas = state.Scanner.Canvas;

            // put the text state parameters into matrix form
            var parameters = new SKMatrix(
                fontSize * horizontalScaling, 0f, 0f,
                0f, fontSize, state.Rise,
                0f, 0f, 1f)
                .PreConcat(fm);
            var uparameters = new SKMatrix(
                1f, 0f, 0f,
                0f, -1f, state.Rise,
                0f, 0f, 1f);

            var clip = state.GetClipPath();
            using var fill = canvas != null && state.RenderModeFill ? state.CreateFillPaint() : null;
            using var stroke = canvas != null && state.RenderModeStroke ? state.CreateStrokePaint() : null;

            var buffer = new ByteStream(0);
            foreach (var textElement in TextElements)
            {
                if (textElement is PdfString pdfString) // Text string.
                {
                    buffer.SetBuffer(pdfString.RawValue);
                    while (buffer.Position < buffer.Length)
                    {
                        var code = font.ReadCode(buffer, out var codeBytes);
                        var textCode = font.ToUnicode(code);
                        if (textCode == null)
                        {
                            // Missing character.
                            textCode = '?';// font.MissingCharacter(byteElement, code);
                        }
                        var textChar = (char)textCode;
                        // Word spacing shall be applied to every occurrence of the single-byte character code
                        // 32 in a string when using a simple font or a composite font that defines code 32 as
                        // a single-byte code.
                        double wordSpacing = 0;
                        if (codeBytes.Length == 1 && code == 32)
                        {
                            wordSpacing = wordSpace;
                        }
                        //NOTE: The text rendering matrix is recomputed before each glyph is painted
                        // during a text-showing operation.
                        SKMatrix trm = parameters.PostConcat(tm).PostConcat(ctm);
                        SKMatrix utm = uparameters.PostConcat(tm).PostConcat(ctm);

                        if (canvas != null && !(codeBytes.Length == 1 && textChar == ' '))
                        {
                            canvas.SetMatrix(trm);
                            var path = font.DrawChar(canvas, fill, stroke, textChar, code);
                            if (clip != null && path != null)
                            {
#if NET9_0_OR_GREATER
                                clip.AddPath(path, in trm);
#else
                                clip.AddPath(path, ref trm);
#endif
                            }
                        }

                        var w = font.GetDisplacement(code);
                        // NOTE: After the glyph is painted, the text matrix is updated
                        // according to the glyph displacement and any applicable spacing parameter.
                        // calculate the combined displacements
                        float tx = (float)((w.X * fontSize + charSpace + wordSpacing) * horizontalScaling);
                        float ty = 0;

                        //Text Scanner                            
                        var quad = new Quad(0, -charAscent, tx, -charDescent);
                        quad.Transform(ref utm);
                        textScanner.ScanChar(textChar, quad);

                        tm = tm.PreConcat(SKMatrix.CreateTranslation(tx, ty));
                    }
                }
                else if (textElement is IPdfNumber pdfNumber)
                {
                    // calculate the combined displacements
                    var tj = -pdfNumber.DoubleValue;
                    float tx = (float)(tj / 1000 * fontSize * horizontalScaling);
                    float ty = 0;

                    //SKMatrix utm = uparameters.PostConcat(tm).PostConcat(ctm);

                    //var quad = new Quad(0, -charAscent, tx, -charDescent);
                    //quad.Transform(ref utm);
                    //textScanner.ScanChar(' ', quad);

                    tm = tm.PreConcat(SKMatrix.CreateTranslation(tx, ty));
                }
            }
            return tm;
        }

        private SKMatrix DrawHorizontalCalcQuad(PdfFont font, GraphicsState state, IScanner textScanner)
        {
            bool wordSpaceSupported = !(font is PdfType0Font);

            var fontSize = state.FontSize;
            var horizontalScaling = state.Scale;
            var wordSpace = wordSpaceSupported ? state.WordSpace : 0;
            var charSpace = state.CharSpace;
            var charAscent = font.Ascent;//.GetAscent(fontSize);
            var charDescent = font.Descent;//.GetDescent(fontSize);
            var ctm = state.Ctm;
            var tm = state.TextState.Tm;
            var canvas = state.Scanner.Canvas;

            // put the text state parameters into matrix form
            var parameters = new SKMatrix(
                fontSize * horizontalScaling, 0f, 0f,
                0f, fontSize, state.Rise,
                0f, 0f, 1f).PreConcat(font.FontMatrix);

            var clip = state.GetClipPath();
            using var fill = canvas != null && state.RenderModeFill ? state.CreateFillPaint() : null;
            using var stroke = canvas != null && state.RenderModeStroke ? state.CreateStrokePaint() : null;

            var buffer = new ByteStream(0);
            foreach (var textElement in TextElements)
            {
                if (textElement is PdfString pdfString) // Text string.
                {
                    buffer.SetBuffer(pdfString.RawValue);
                    while (buffer.Position < buffer.Length)
                    {
                        var code = font.ReadCode(buffer, out var codeBytes);
                        var textCode = font.ToUnicode(code);
                        if (textCode == null)
                        {
                            // Missing character.
                            textCode = '?';// font.MissingCharacter(byteElement, code);
                        }
                        var textChar = (char)textCode;
                        // Word spacing shall be applied to every occurrence of the single-byte character code
                        // 32 in a string when using a simple font or a composite font that defines code 32 as
                        // a single-byte code.
                        var wordSpacing = codeBytes.Length == 1 && code == 32 ? wordSpace : 0F;

                        //NOTE: The text rendering matrix is recomputed before each glyph is painted
                        // during a text-showing operation.
                        SKMatrix trm = parameters.PostConcat(tm).PostConcat(ctm);

                        if (canvas != null && !(codeBytes.Length == 1 && textChar == ' '))
                        {
                            canvas.SetMatrix(trm);
                            var path = font.DrawChar(canvas, fill, stroke, textChar, code);
                            if (clip != null && path != null)
                            {
#if NET10_0_OR_GREATER
                                clip.AddPath(path, in trm);
#else
                                clip.AddPath(path, ref trm);
#endif
                            }
                        }

                        var w = font.GetWidth(code);
                        // NOTE: After the glyph is painted, the text matrix is updated
                        // according to the glyph displacement and any applicable spacing parameter.
                        // calculate the combined displacements
                        float tx = (w * 0.001F * fontSize + charSpace + wordSpacing) * horizontalScaling;
                        float ty = 0F;

                        //Text Scanner
                        var quad = new Quad(0, charDescent, w + charSpace, charAscent);
                        quad.Transform(ref trm);
                        textScanner.ScanChar(textChar, quad);

                        tm = tm.PreConcat(SKMatrix.CreateTranslation(tx, ty));
                    }
                }
                else if (textElement is IPdfNumber pdfNumber)
                {
                    // calculate the combined displacements
                    var tj = -pdfNumber.FloatValue;
                    float tx = tj / 1000F * fontSize * horizontalScaling;
                    float ty = 0;

                    SKMatrix trm = parameters.PostConcat(tm).PostConcat(ctm);

                    var quad = new Quad(0, charDescent, tj, charAscent);
                    quad.Transform(ref trm);
                    textScanner.ScanChar(' ', quad);

                    tm = tm.PreConcat(SKMatrix.CreateTranslation(tx, ty));
                }
            }
            return tm;
        }

        private SKMatrix DrawVertical(PdfFont font, GraphicsState state, IScanner textScanner)
        {
            bool wordSpaceSupported = !(font is PdfType0Font);

            var fontSize = state.FontSize;
            var horizontalScaling = state.Scale;
            var wordSpace = wordSpaceSupported ? state.WordSpace : 0;
            var charSpace = state.CharSpace;
            SKMatrix fm = font.FontMatrix;
            SKMatrix ctm = state.Ctm;
            SKMatrix tm = state.TextState.Tm;
            var canvas = state.Scanner.Canvas;

            // put the text state parameters into matrix form
            var parameters = new SKMatrix(
                fontSize * horizontalScaling, 0f, 0f,
                0f, fontSize, state.Rise,
                0f, 0f, 1f);
            var uparameters = new SKMatrix(
                1f, 0f, 0f,
                0f, -1f, state.Rise,
                0f, 0f, 1f);

            var clip = state.GetClipPath();
            using var fill = canvas != null && state.RenderModeFill ? state.CreateFillPaint() : null;
            using var stroke = canvas != null && state.RenderModeStroke ? state.CreateStrokePaint() : null;

            var buffer = new ByteStream(0);
            foreach (var textElement in TextElements)
            {
                if (textElement is PdfString pdfString) // Text string.
                {
                    buffer.SetBuffer(pdfString.RawValue);
                    while (buffer.Position < buffer.Length)
                    {
                        var code = font.ReadCode(buffer, out var codeBytes);
                        var textCode = font.ToUnicode(code);
                        if (textCode == null)
                        {
                            // Missing character.
                            textCode = '?';// font.MissingCharacter(byteElement, code);
                        }
                        var textChar = (char)textCode;
                        // Word spacing shall be applied to every occurrence of the single-byte character code
                        // 32 in a string when using a simple font or a composite font that defines code 32 as
                        // a single-byte code.
                        double wordSpacing = 0;
                        if (codeBytes.Length == 1 && code == 32)
                        {
                            wordSpacing = wordSpace;
                        }
                        //NOTE: The text rendering matrix is recomputed before each glyph is painted
                        // during a text-showing operation.
                        SKMatrix trm = parameters.PostConcat(tm).PostConcat(ctm);
                        SKMatrix utm = uparameters.PostConcat(tm).PostConcat(ctm);
                        // get glyph's position vector if this is vertical text
                        // changes to vertical text should be tested with PDFBOX-2294 and PDFBOX-1422
                        // position vector, in text space
                        var v = font.GetPositionVector(code);
                        // apply the position vector to the horizontal origin to get the vertical origin
                        trm = trm.PreConcat(SKMatrix.CreateTranslation(v.X, v.Y));
                        trm = trm.PreConcat(fm);

                        if (canvas != null && !(codeBytes.Length == 1 && textChar == ' '))
                        {
#if NET10_0_OR_GREATER
                            canvas.SetMatrix(in trm);
#else
                            canvas.SetMatrix(trm);
#endif
                            var path = font.DrawChar(canvas, fill, stroke, textChar, code);
                            if (clip != null && path != null)
                            {
#if NET10_0_OR_GREATER
                                clip.AddPath(path, in trm);
#else
                                clip.AddPath(path, ref trm);
#endif
                            }
                        }

                        var w = font.GetDisplacement(code);
                        // NOTE: After the glyph is painted, the text matrix is updated
                        // according to the glyph displacement and any applicable spacing parameter.
                        // calculate the combined displacements
                        float tx = 0;
                        float ty = (float)(w.Y * fontSize + charSpace + wordSpacing);
                        var fw = font.GetWidth(code) / 200;

                        //Text Scanner
                        var quad = new Quad(-fw, 0, fw, -ty);
                        quad.Transform(ref utm);
                        textScanner.ScanChar(textChar, quad);

                        tm = tm.PreConcat(SKMatrix.CreateTranslation(tx, ty));
                    }
                }
                else if (textElement is IPdfNumber pdfNumber)
                {
                    // calculate the combined displacements
                    var tj = -pdfNumber.DoubleValue;
                    float tx = 0;
                    float ty = (float)(tj / 1000 * fontSize);

                    tm = tm.PreConcat(SKMatrix.CreateTranslation(tx, ty));
                }
            }
            return tm;
        }

        private float GetVerticalWidth(PdfFont font, int code)
        {
            var ttf = font is PdfTrueTypeFont ftt
                ? ftt.TrueTypeFont
                : font is PdfType0Font ft0
                    ? ft0.DescendantFont is PdfCIDFontType2Wrapper fCID2
                        ? fCID2.TrueTypeFont
                        : null
                    : null;
            return font.GetWidth(code) / (ttf?.UnitsPerEm ?? 1000);
        }

        public void Invalidate()
        {
            quad = null;
            text = null;
        }
    }
}