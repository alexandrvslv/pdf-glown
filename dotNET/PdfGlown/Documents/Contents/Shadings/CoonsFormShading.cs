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

using PdfGlown.Bytes;
using PdfGlown.Documents.Contents.Shadings.Patches;
using PdfGlown.Objects;
using PdfGlown.Util.Math;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace PdfGlown.Documents.Contents.Shadings
{
    public class CoonsFormShading : FreeFormShading
    {
        protected int controlPoints = 12;
        private List<Patch> patches;

        public CoonsFormShading(PdfDocument context)
            : base(context)
        {
            ShadingType = 6;
        }

        internal CoonsFormShading(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        public List<Patch> Patches => patches ??= LoadPatches();

        protected override SKPicture Render(SKMatrix spaceMatrix)
        {
            var patches = Patches;
            if (patches == null || patches.Count == 0)
                return null;
            var box = spaceMatrix.MapRect(Box.Value);
            using var recorder = new SKPictureRecorder();
#if NET9_0_OR_GREATER
            using var canvas = recorder.BeginRecording(box);
#else
            using var canvas = recorder.BeginRecording(box);
#endif
            canvas.SetMatrix(spaceMatrix);
            using var paint = new SKPaint { IsAntialias = AntiAlias };
            foreach (var patch in patches)
            {
                canvas.DrawPatch(patch.ControlPoints, patch.CornerColors, null, paint);
            }
            var picture = recorder.EndRecording();
            //using var temp = new FileStream("Shading" + Reference.ToString() + ".png", FileMode.Create, FileAccess.Write);
            //SKImage.FromPicture(picture, box.Size.ToSizeI(), SKMatrix.CreateTranslation(-box.Left, -box.Top)).Encode().SaveTo(temp);
            //temp.Flush();
            return picture;
        }

        public override SKRect? GetBounds()
        {
            var patches = Patches;
            if (patches == null)
                return null;
            var rect = patches.First().GetBound();
            rect.Add(patches.Skip(1).Select(x => x.GetBound()));
            return SKRect.Inflate(rect, 1, 1);
        }

        List<Patch> LoadPatches()
        {
            var dict = this;
            if (dict is not PdfStream stream
                || Decodes == null
                || Decodes.Count != 2 + NumberOfColorComponents)
            {
                return null;
            }
            int bitsPerFlag = BitsPerFlag;
            var list = new List<Patch>();
            long maxSrcCoord = (long)Math.Pow(2, BitsPerCoordinate) - 1;
            long maxSrcColor = (long)Math.Pow(2, BitsPerComponent) - 1;

            var input = stream.GetInputStream();
            Span<SKPoint> implicitEdge = stackalloc SKPoint[4];
            Span<SKColor> implicitCornerColor = stackalloc SKColor[2];
            Span<float> colorBuffer = stackalloc float[NumberOfColorComponents];
            byte flag = 0;
            Patch current = null;

            while (input.IsAvailable)
            {
                try
                {
                    flag = (byte)(input.ReadBits(bitsPerFlag) & 3);
                    if (current != null)
                    {
                        switch (flag)
                        {
                            case 0:
                                break;
                            case 1:
                                current.GetFlag1Edge(implicitEdge);
                                current.GetFlag1Color(implicitCornerColor);
                                break;
                            case 2:
                                current.GetFlag2Edge(implicitEdge);
                                current.GetFlag2Color(implicitCornerColor);
                                break;
                            case 3:
                                current.GetFlag3Edge(implicitEdge);
                                current.GetFlag3Color(implicitCornerColor);
                                break;
                            default:
                                return list;
                        }
                    }
                    current = ReadPatch(input, flag == 0, colorBuffer, implicitEdge, implicitCornerColor,
                            maxSrcCoord, maxSrcColor);
                    if (current == null)
                    {
                        break;
                    }
                    list.Add(current);
                }
                catch (Exception)
                {
                    break;
                }
            }
            return list;
        }

        /**
         * Read a single patch from a data stream, a patch contains information of its coordinates and color parameters.
         *
         * @param input the image source data stream
         * @param isFree whether this is a free patch
         * @param implicitEdge implicit edge when a patch is not free, otherwise it's not used
         * @param implicitCornerColor implicit colors when a patch is not free, otherwise it's not used
         * @param maxSrcCoord the maximum coordinate value calculated from source data
         * @param maxSrcColor the maximum color value calculated from source data
         * @param rangeX range for coordinate x
         * @param rangeY range for coordinate y
         * @param colRange range for color
         * @param matrix the pattern matrix concatenated with that of the parent content stream
         * @param xform transformation for user to device space
         * @param controlPoints number of control points, 12 for type 6 shading and 16 for type 7 shading
         * @return a single patch
         * @throws IOException when something went wrong
         */
        protected Patch ReadPatch(IInputStream input, bool isFree, Span<float> colorBuffer, Span<SKPoint> implicitEdge,
                Span<SKColor> implicitCornerColor, long maxSrcCoord, long maxSrcColor)
        {
            var color = new SKColor[4];
            var points = new SKPoint[controlPoints];
            var rangeX = Decodes[0];
            var rangeY = Decodes[1];
            int pStart = 4;
            int cStart = 2;
            if (isFree)
            {
                pStart = 0;
                cStart = 0;
            }
            else
            {
                points[0] = implicitEdge[0];
                points[1] = implicitEdge[1];
                points[2] = implicitEdge[2];
                points[3] = implicitEdge[3];

                color[0] = implicitCornerColor[0];
                color[1] = implicitCornerColor[1];
            }

            try
            {
                for (int i = pStart; i < controlPoints; i++)
                {
                    long x = input.ReadBits(BitsPerCoordinate);
                    long y = input.ReadBits(BitsPerCoordinate);
                    float px = Interpolate(x, maxSrcCoord, rangeX.Low, rangeX.High);
                    float py = Interpolate(y, maxSrcCoord, rangeY.Low, rangeY.High);
                    points[i] = new SKPoint(px, py);
                }

                for (int i = cStart; i < 4; i++)
                {
                    for (int j = 0; j < colorBuffer.Length; j++)
                    {
                        long c = input.ReadBits(BitsPerComponent);
                        var range = Decodes[j + 2];
                        colorBuffer[j] = Interpolate(c, maxSrcColor, range.Low, range.High);
                    }
                    color[i] = GetSKColor(colorBuffer);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("EOF " + ex);
                return null;
            }
            return GeneratePatch(points, color);
        }

        protected virtual Patch GeneratePatch(SKPoint[] points, SKColor[] colors) => new CoonsPatch(points, colors);
    }
}
