/*
  Copyright 2008-2015 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfGlown.Documents.Contents.ColorSpaces;
using PdfGlown.Documents.Contents.Composition;
using PdfGlown.Documents.Contents.XObjects;
using PdfGlown.Documents.Interaction.Annotations.ControlPoints;
using PdfGlown.Objects;
using SkiaSharp;
using System.Collections.Generic;

namespace PdfGlown.Documents.Interaction.Annotations
{
    /// <summary>Abstract shape annotation.</summary>
    [Pdf(VersionEnum.PDF13)]
    public abstract class Shape : Markup
    {
        protected Shape(PdfPage page, SKRect box, string text, PdfName subtype)
            : base(page, subtype, box, text)
        { }

        protected Shape(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        public void DrawPath(SKCanvas canvas, SKPath path)
        {
            if (InteriorColor != null)
            {
                var fillColor = InteriorSKColor.Value;
                using var paint = new SKPaint { Color = fillColor, Style = SKPaintStyle.Fill };
                var cloudPath = BorderEffect?.Apply(paint, path) ?? path;
                canvas.DrawPath(cloudPath, paint);
                if (cloudPath != path)
                    cloudPath.Dispose();
            }
            if (Border != null && Color != null && !Color.IsZero)
            {
                var color = Color == null ? SKColors.Black : DeviceColorSpace.CalcSKColor(Color, Alpha);
                using var paint = new SKPaint { Color = color };
                Border?.Apply(paint, BorderEffect);
                canvas.DrawPath(path, paint);
            }
        }

        public void DrawPath(PrimitiveComposer canvas, SKPath path)
        {
            if (InteriorColor != null)
            {
                canvas.SetFillColor(InteriorColor);
            }
            if (Border != null)
            {
                canvas.SetStrokeColor(Color ?? RGBColor.Default);
                Border.Apply(canvas);
            }
            var effectPath = BorderEffect?.Apply(canvas, path) ?? path;
            canvas.DrawPath(effectPath);
            if (effectPath != path)
            {
                effectPath.Dispose();
            }
            if (InteriorColor != null && Border != null)
            {
                canvas.FillStroke();
            }
            else if (InteriorColor != null)
            {
                canvas.Fill();
            }
            else
            {
                canvas.Stroke();
            }
        }

        public abstract SKPath GetPath(SKMatrix sKMatrix);

        protected override FormXObject GenerateAppearance()
        {
            var appearance = ResetAppearance(out var zeroMatrix);

            var canvas = new PrimitiveComposer(appearance);
            {
                using var path = GetPath(zeroMatrix);
                if (path != null && !path.IsEmpty)
                {
                    DrawPath(canvas, path);
                }
            }
            canvas.Flush();
            return appearance;
        }

        public override void MoveTo(SKRect newBox)
        {
            var oldBox = Box;
            if (oldBox.Width != newBox.Width
                || oldBox.Height != newBox.Height)
            {
                QueueRefreshAppearance();
            }

            base.MoveTo(newBox);
        }

        public override IEnumerable<ControlPoint> GetControlPoints()
        {
            foreach (var cpBase in GetDefaultControlPoint())
            {
                yield return cpBase;
            }
        }
    }
}