/*
  Copyright 2010-2011 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfGlown.Objects;
using SkiaSharp;

namespace PdfGlown.Documents.Contents.Patterns
{
    /// <summary>Uncolored tiling pattern ("stencil") associated to a color.</summary>
    public sealed class ColorizedTilingPattern : Color, IPattern
    {
        private readonly TilingPattern tiling;
        private readonly IColor color;

        internal ColorizedTilingPattern(TilingPattern tiling, IColor color)
            : base(null)
        {
            this.tiling = tiling;
            this.color = color;
        }

        /// <summary>Gets the color applied to the stencil.</summary>
        public IColor Color => color;

        public override ColorSpace ColorSpace => tiling.ColorSpace;

        public override PdfArray Components => ((Color)color).Components;

        public override PdfDirectObject RefOrSelf
        {
            get => tiling.RefOrSelf;
            protected set => base.RefOrSelf = value;
        }

        public SKMatrix Matrix
        {
            get => tiling.Matrix;
            set => tiling.Matrix = value;
        }

        public SKShader GetShader(GraphicsState state)
        {
            var shader = tiling.GetShader(state);
            var skColor = ((Color)color).GetSkColor();
            return SKShader.CreateCompose(SKShader.CreateColor(skColor), shader, SKBlendMode.DstIn);
        }

    }
}