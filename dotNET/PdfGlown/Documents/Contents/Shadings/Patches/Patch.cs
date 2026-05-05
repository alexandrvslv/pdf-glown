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

using SkiaSharp;
using System;

namespace PdfGlown.Documents.Contents.Shadings.Patches
{
    public abstract class Patch
    {
        protected SKPoint[] controlPoints;
        protected SKColor[] cornerColors;


        /// <summary>
        /// level = {levelU, levelV}, levelU defines the patch's u direction edges should be 
        /// divided into 2^levelU parts, level V defines the patch's v direction edges should
        /// be divided into 2^levelV parts
        /// </summary>
        protected int[] level;

        /// <summary>Constructor of Patch.</summary>
        /// <param name="color">4 corner's colors</param>
        public Patch(SKColor[] color)
        {
            cornerColors = color;
        }

        public SKPoint[] ControlPoints => controlPoints;

        public SKColor[] CornerColors => cornerColors;

        /// <summary>Get the implicit edge for flag = 1.</summary>
        public abstract void GetFlag1Edge(Span<SKPoint> points);

        /// <summary>Get the implicit edge for flag = 2.</summary>
        public abstract void GetFlag2Edge(Span<SKPoint> points);

        /// <summary>Get the implicit edge for flag = 3.</summary>
        public abstract void GetFlag3Edge(Span<SKPoint> points);

        /// <summary>Get the implicit color for flag = 1.</summary>
        public void GetFlag1Color(Span<SKColor> colors)
        {
            colors[0] = cornerColors[1];
            colors[1] = cornerColors[2];
        }

        /// <summary>Get implicit color for flag = 2.</summary>
        public void GetFlag2Color(Span<SKColor> colors)
        {
            colors[0] = cornerColors[2];
            colors[1] = cornerColors[3];
        }

        /// <summary>Get implicit color for flag = 3.</summary>
        public void GetFlag3Color(Span<SKColor> colors)
        {
            colors[0] = cornerColors[3];
            colors[1] = cornerColors[0];
        }

        public SKRect GetBound()
        {
            using SKPath path = GeneratePath();
            return path.Bounds;
        }

        public SKPath GeneratePath()
        {
            var path = new SKPath();

            path.MoveTo(controlPoints[0]);

            path.CubicTo(controlPoints[1], controlPoints[2], controlPoints[3]);
            path.CubicTo(controlPoints[4], controlPoints[5], controlPoints[6]);
            path.CubicTo(controlPoints[7], controlPoints[8], controlPoints[9]);
            path.CubicTo(controlPoints[10], controlPoints[11], controlPoints[0]);
            path.Close();
            return path;
        }
    }
}
