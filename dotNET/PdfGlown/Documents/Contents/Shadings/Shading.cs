/*
  Copyright 2010-2012 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfGlown.Documents.Functions;
using PdfGlown.Objects;
using PdfGlown.Util.Math;
using SkiaSharp;
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents.Shadings
{
    /// <summary>Shading object [PDF:1.6:4.6.3].</summary>
    [Pdf(VersionEnum.PDF13)]
    public abstract class Shading : PdfStream
    {
        private IColor backgroundColor;
        private SKRect? box;

        internal static Shading Create(Dictionary<PdfName, PdfDirectObject> dictionary)
        {
            var type = dictionary.GetInt(PdfName.ShadingType);
            return type switch
            {
                1 => new FunctionBasedShading(dictionary),
                2 => new AxialShading(dictionary),
                3 => new RadialShading(dictionary),
                4 => new FreeFormShading(dictionary),
                5 => new LatticeFormShading(dictionary),
                6 => new CoonsFormShading(dictionary),
                7 => new TensorProductShading(dictionary),
                _ => new AxialShading(dictionary),
            };
        }

        //TODO:IMPL new element constructor!

        protected Shading(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        protected Shading(PdfDocument context)
            : base(context, new Dictionary<PdfName, PdfDirectObject>())
        { }

        protected Shading()
            : this((PdfDocument)null)
        { }

        public int ShadingType
        {
            get => GetInt(PdfName.ShadingType);
            set => Set(PdfName.ShadingType, value);
        }

        public ColorSpace ColorSpace
        {
            get => ColorSpace.Wrap(Get(PdfName.ColorSpace));
            set => Set(PdfName.ColorSpace, value);
        }

        public PdfArray Background
        {
            get => Get<PdfArray>(PdfName.Background);
            set => SetDirect(PdfName.Background, value);
        }

        public IColor BackgroundColor => backgroundColor ??= Background is PdfArray array ? ColorSpace.GetColor(array, null) : null;

        public Function Function
        {
            get => Function.Wrap(Get(PdfName.Function));
            set => Set(PdfName.Function, value);
        }

        public SKRect? Box
        {
            get => box ??= Get<PdfRectangle>(PdfName.BBox)?.ToSKRect() ?? GetBounds();
            set => GetOrCreate<PdfRectangle>(PdfName.BBox).Update(value.Value);
        }

        public bool AntiAlias
        {
            get => GetBool(PdfName.AntiAlias);
            set => Set(PdfName.AntiAlias, value);
        }

        public virtual SKRect? GetBounds() => null;

        public abstract SKShader GetShader(SKMatrix sKMatrix, GraphicsState state);

        public virtual SKMatrix CalculateMatrix(SKMatrix skMatrix, GraphicsState state)
        {
            if (Box is not SKRect box)
                return skMatrix;
            box = skMatrix.ScaleX > 1 ? box : skMatrix.MapRect(box);
            var pathRect = state.Scanner.Path?.Bounds ?? state.Scanner.Canvas.LocalClipBounds;
            pathRect = state.Ctm.MapRect(pathRect);

            var scale = box.Height > box.Width
                ? pathRect.Height / box.Height
                : pathRect.Width / box.Width;
            var scaleMAtrix = SKMatrix.CreateScale(scale, scale);
            var mBox = scaleMAtrix.MapRect(box);
            if (!mBox.IntersectsWith(pathRect))
            {
                //scaleMAtrix = scaleMAtrix.PreConcat(SKMatrix.CreateTranslation(pathRect.Left - mBox.Left, pathRect.Top - mBox.Top));
            }
            return scaleMAtrix.PreConcat(skMatrix);
        }

        public SKShader GetBackGroundShader()
        {
            return BackgroundColor != null ? SKShader.CreateColor(((Color)BackgroundColor).GetSkColor()) : null;
        }
    }
}
