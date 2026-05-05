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

using PdfGlown.Documents.Functions;
using PdfGlown.Objects;
using PdfGlown.Util.Math;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents.Shadings
{
    public class RadialShading : Shading
    {
        private SKPoint3[] coords;
        private float[] domain;
        private bool[] extend;

        internal RadialShading(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        public RadialShading(PdfDocument context)
            : base(context)
        {
            ShadingType = 3;
        }

        public SKPoint3[] Coords
        {
            get => coords ??= this.Get<PdfArray>(PdfName.Coords) is PdfArray array
                    ? new SKPoint3[]
                    {
                        new(array.GetFloat(0), array.GetFloat(1), array.GetFloat(2)),
                        new(array.GetFloat(3), array.GetFloat(4), array.GetFloat(5)),
                    }
                    : null;
            set
            {
                coords = value;
                this[PdfName.Coords] = new PdfArrayImpl(6)
                {
                    value[0].X, value[0].Y, value[0].Z,
                    value[1].X, value[1].Y, value[1].Z
                };
            }
        }

        public float[] Domain
        {
            get => domain ??= Get<PdfArray>(PdfName.Domain) is PdfArray array
                    ? new float[] { array.GetFloat(0), array.GetFloat(1) }
                    : new float[] { 0F, 1F };
            set
            {
                domain = value;
                this[PdfName.Domain] = new PdfArrayImpl(2)
                {
                    value[0],
                    value[1]
                };
            }
        }

        public bool[] Extend
        {
            get => extend ??= Get<PdfArray>(PdfName.Extend) is PdfArray array
                    ? new bool[] { array.GetBool(0), array.GetBool(1) }
                    : new bool[] { false, false };
            set
            {
                extend = value;
                this[PdfName.Domain] = new PdfArrayImpl(2)
                {
                    value[0],
                    value[1]
                };
            }
        }

        //public override SKRect? GetBounds()
        //{
        //    var point1 = Coords[0];
        //    var point2 = Coords[1];
        //    var rect = new SKRect(point1.X, point1.Y, point2.X, point2.Y);
        //    rect.Add(new SKRect(point1.X - point1.Z, point1.Y - point1.Z, point1.X + point1.Z, point1.Y + point1.Z));
        //    rect.Add(new SKRect(point2.X - point2.Z, point2.Y - point2.Z, point2.X + point2.Z, point2.Y + point2.Z));
        //    return SKRect.Inflate(rect, 1, 1);
        //}

        public override SKShader GetShader(SKMatrix sKMatrix, GraphicsState state)
        {
            var coords = Coords;
            var colorSpace = ColorSpace;
            var compCount = colorSpace.ComponentCount;
            var colors = new SKColor[2];
            //var background = Background;
            var domain = Domain;
            Span<float> components = stackalloc float[compCount];
            for (int i = 0; i < domain.Length; i++)
            {
                components[0] = domain[i];
                var result = Function.Calculate(components);
                colors[i] = colorSpace.GetSKColor(result, null);
                components.Clear();
            }
            var mode = Extend[0] && Extend[1] ? SKShaderTileMode.Clamp
                : Extend[0] && !Extend[1] ? SKShaderTileMode.Mirror
                : !Extend[0] && Extend[1] ? SKShaderTileMode.Mirror
                : SKShaderTileMode.Decal;
            return SKShader.CreateTwoPointConicalGradient(new SKPoint(coords[0].X, coords[0].Y), coords[0].Z,
                                                          new SKPoint(coords[1].X, coords[1].Y), coords[1].Z,
                                                          colors, domain, mode, sKMatrix);
        }
    }
}
