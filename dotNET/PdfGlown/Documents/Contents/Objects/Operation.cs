/*
  Copyright 2006-2015 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfGlown.Tokens;

using System;
using System.Collections.Generic;
using System.Text;

namespace PdfGlown.Documents.Contents.Objects
{
    /// <summary>Content stream instruction [PDF:1.6:3.7.1].</summary>
    [Pdf(VersionEnum.PDF10)]
    public abstract class Operation : ContentObject
    {
        private static readonly Dictionary<string, Func<PdfArray, Operation>> cache = new(128, StringComparer.Ordinal)
        {
            { SaveGraphicsState.OperatorKeyword, static operands => SaveGraphicsState.Value },
            { SetFont.OperatorKeyword, static operands => new SetFont(operands) },
            { SetStrokeColorBase.OperatorKeyword, static operands => new SetStrokeColorBase(operands) },
            { SetStrokeColorExtended.OperatorKeyword, static operands => new SetStrokeColorExtended(operands) },
            { SetStrokeColorSpace.OperatorKeyword, static operands => new SetStrokeColorSpace(operands) },
            { SetFillColorBase.OperatorKeyword, static operands => new SetFillColorBase(operands) },
            { SetFillColorExtended.OperatorKeyword, static operands => new SetFillColorExtended(operands) },
            { SetFillColorSpace.OperatorKeyword, static operands => new SetFillColorSpace(operands) },
            { SetGrayStrokeColor.OperatorKeyword, static operands => new SetGrayStrokeColor(operands) },
            { SetGrayFillColor.OperatorKeyword, static operands => new SetGrayFillColor(operands) },
            { SetRGBStrokeColor.OperatorKeyword, static operands => new SetRGBStrokeColor(operands) },
            { SetRGBFillColor.OperatorKeyword, static operands => new SetRGBFillColor(operands) },
            { SetCMYKStrokeColor.OperatorKeyword, static operands => new SetCMYKStrokeColor(operands) },
            { SetCMYKFillColor.OperatorKeyword, static operands => new SetCMYKFillColor(operands) },
            { RestoreGraphicsState.OperatorKeyword, static operands => RestoreGraphicsState.Value },
            { BeginSubpath.OperatorKeyword, static operands => new BeginSubpath(operands) },
            { CloseSubpath.OperatorKeyword, static operands => CloseSubpath.Value },
            { PaintPath.CloseStrokeOperatorKeyword, static operands => PaintPath.CloseStroke },
            { PaintPath.FillOperatorKeyword, static operands => PaintPath.Fill },
            { PaintPath.FillCompatibleOperatorKeyword, static operands => PaintPath.Fill },
            { PaintPath.FillEvenOddOperatorKeyword, static operands => PaintPath.FillEvenOdd },
            { PaintPath.StrokeOperatorKeyword, static operands => PaintPath.Stroke },
            { PaintPath.FillStrokeOperatorKeyword, static operands => PaintPath.FillStroke },
            { PaintPath.FillStrokeEvenOddOperatorKeyword, static operands => PaintPath.FillStrokeEvenOdd },
            { PaintPath.CloseFillStrokeOperatorKeyword, static operands => PaintPath.CloseFillStroke },
            { PaintPath.CloseFillStrokeEvenOddOperatorKeyword, static operands => PaintPath.CloseFillStrokeEvenOdd },
            { PaintPath.EndNoOpOperatorKeyword, static operands => PaintPath.EndNoOp },
            { ModifyClipPathNonZero.OperatorKeyword, static operands => ModifyClipPath.NonZero },
            { ModifyClipPathEvenOdd.OperatorKeyword, static operands => ModifyClipPath.EvenOdd },
            { TranslateTextToNextLine.OperatorKeyword, static operands => TranslateTextToNextLine.Value },
            { ShowSimpleText.OperatorKeyword, static operands => new ShowSimpleText(operands) },
            { ShowTextToNextLineNoSpace.OperatorKeyword, static operands => new ShowTextToNextLineNoSpace(operands) },
            { ShowTextToNextLineWithSpace.OperatorKeyword, static operands => new ShowTextToNextLineWithSpace(operands) },
            { ShowAdjustedText.OperatorKeyword, static operands => new ShowAdjustedText(operands) },
            { TranslateTextRelativeNoLead.OperatorKeyword, static operands => new TranslateTextRelativeNoLead(operands) },
            { TranslateTextRelativeWithLead.OperatorKeyword, static operands => new TranslateTextRelativeWithLead(operands) },
            { SetTextMatrix.OperatorKeyword, static operands => new SetTextMatrix(operands) },
            { ModifyCTM.OperatorKeyword, static operands => new ModifyCTM(operands) },
            { PaintXObject.OperatorKeyword, static operands => new PaintXObject(operands) },
            { PaintShading.OperatorKeyword, static operands => new PaintShading(operands) },
            { SetCharSpace.OperatorKeyword, static operands => new SetCharSpace(operands) },
            { SetLineCap.OperatorKeyword, static operands => new SetLineCap(operands) },
            { SetLineDash.OperatorKeyword, static operands => new SetLineDash(operands) },
            { SetLineJoin.OperatorKeyword, static operands => new SetLineJoin(operands) },
            { SetLineWidth.OperatorKeyword, static operands => new SetLineWidth(operands) },
            { SetMiterLimit.OperatorKeyword, static operands => new SetMiterLimit(operands) },
            { SetTextLead.OperatorKeyword, static operands => new SetTextLead(operands) },
            { SetTextRise.OperatorKeyword, static operands => new SetTextRise(operands) },
            { SetTextScale.OperatorKeyword, static operands => new SetTextScale(operands) },
            { SetTextRenderMode.OperatorKeyword, static operands => new SetTextRenderMode(operands) },
            { SetWordSpace.OperatorKeyword, static operands => new SetWordSpace(operands) },
            { DrawLine.OperatorKeyword, static operands => new DrawLine(operands) },
            { DrawRectangle.OperatorKeyword, static operands => new DrawRectangle(operands) },
            { DrawCurve.InitialOperatorKeyword, static operands => new DrawInitialCurve(operands) },
            { DrawCurve.FullOperatorKeyword, static operands => new DrawFullCurve(operands) },
            { DrawCurve.FinalOperatorKeyword, static operands => new DrawFinalCurve(operands) },
            { BeginText.OperatorKeyword, static operands => BeginText.Value },
            { EndText.OperatorKeyword, static operands => EndText.Value },
            { BeginNamedMarkedContent.SimpleOperatorKeyword, static operands => new BeginNamedMarkedContent(operands) },
            { BeginPropertyListMarkedContent.PropertyListOperatorKeyword, static operands => new BeginPropertyListMarkedContent(operands) },
            { EndMarkedContent.OperatorKeyword, static operands => EndMarkedContent.Value },
            { MarkedNamedContentPoint.OperatorKeyword, static operands => new MarkedNamedContentPoint(operands) },
            { MarkedPropertyListContentPoint.OperatorKeyword, static operands => new MarkedPropertyListContentPoint(operands) },
            { BeginInlineImage.OperatorKeyword, static operands => BeginInlineImage.Value },
            { EndInlineImage.OperatorKeyword, static operands => EndInlineImage.Value },
            { ApplyExtGState.OperatorKeyword, static operands => new ApplyExtGState(operands) },
            { CharProcWidth.OperatorKeyword, static operands => new CharProcWidth(operands) },
            { CharProcBBox.OperatorKeyword, static operands => new CharProcBBox(operands) },
            { Flatness.OperatorKeyword, static operands => new Flatness(operands) },
            { BeginCompatibilityState.OperatorKeyword, static operands => BeginCompatibilityState.Value },
            { EndCompatibilityState.OperatorKeyword, static operands => EndCompatibilityState.Value },
        };

        /// <summary>Gets an operation.</summary>
        /// <param name="@operator">Operator.</param>
        /// <param name="operands">List of operands.</param>
        public static Operation Get(string @operator, PdfArray operands)
        {
            if (string.IsNullOrEmpty(@operator))
                return null;
            if (cache.TryGetValue(@operator, out var func))
                return func(operands);
            else // No explicit operation implementation available.
                return new GenericOperation(@operator, operands);
        }

        protected string @operator;
        protected PdfArray operands;

        protected Operation(string @operator)
        { this.@operator = @operator; }

        protected Operation(string @operator, PdfDirectObject operand)
        {
            this.@operator = @operator;
            operands = new PdfArrayImpl(1) { operand };
        }

        protected Operation(string @operator, params PdfDirectObject[] operands)
        {
            this.@operator = @operator;
            this.operands = new PdfArrayImpl(operands);
        }

        protected Operation(string @operator, PdfArray operands)
        {
            this.@operator = @operator;
            this.operands = operands;
        }

        public string Operator => @operator;

        public PdfArray Operands => operands;

        public override string ToString()
        {
            var buffer = new StringBuilder();

            // Begin.
            buffer.Append('{');

            // Operator.
            buffer.Append(@operator);

            // Operands.
            if (operands != null)
            {
                buffer.Append(' ').Append('[');
                for (int i = 0, count = operands.Count; i < count; i++)
                {
                    if (i > 0)
                    { buffer.Append(',').Append(' '); }

                    buffer.Append(operands.Get(i).ToString());
                }
                buffer.Append(']');
            }

            // End.
            buffer.Append('}');

            return buffer.ToString();
        }

        public override void WriteTo(IOutputStream stream, PdfDocument context)
        {
            if (operands != null)
            {
                foreach (var operand in operands.GetItems())
                {
                    operand.WriteTo(stream, context); 
                    stream.Write(Chunk.Space);
                }
            }
            stream.Write(@operator); stream.Write(Chunk.LineFeed);
        }
    }
}