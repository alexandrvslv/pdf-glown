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
using PdfGlown.Documents.Contents.ColorSpaces;
using PdfGlown.Documents.Functions;
using PdfGlown.Objects;
using PdfGlown.Util.Collections;
using PdfGlown.Util.Math;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PdfGlown.Documents.Contents.Shadings
{
    public class FreeFormShading : Shading
    {
        private float[] decode;
        private IList<Interval<float>> decodes;
        private int numberOfColorComponents = -1;
        private Vertices vertices;
        private SKVertices skVerteces;

        internal FreeFormShading(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        public FreeFormShading(PdfDocument context)
            : base(context)
        {
            ShadingType = 4;
        }

        public int BitsPerCoordinate
        {
            get => GetInt(PdfName.BitsPerCoordinate);
            set => Set(PdfName.BitsPerCoordinate, value);
        }

        public int BitsPerComponent
        {
            get => GetInt(PdfName.BitsPerComponent);
            set => Set(PdfName.BitsPerComponent, value);
        }

        public int BitsPerFlag
        {
            get => GetInt(PdfName.BitsPerFlag);
            set => Set(PdfName.BitsPerFlag, value);
        }

        public float[] Decode
        {
            get => decode ??= Get<PdfArray>(PdfName.Decode) is PdfArray array
                        ? array.ToFloatArray()
                        : GenerateDecode();
            set => Set(PdfName.Domain, new PdfArrayImpl(decode = value));
        }

        public IList<Interval<float>> Decodes => decodes ??= Decode.GetIntervals();

        public int NumberOfColorComponents
        {
            get
            {
                if (numberOfColorComponents == -1)
                {
                    numberOfColorComponents = Function != null ? 1
                            : ColorSpace.ComponentCount;
                }
                return numberOfColorComponents;
            }
        }

        public Vertices Vertices => vertices ??= LoadTriangles();

        public SKVertices SKVertices => skVerteces ??= Vertices != null
            ? SKVertices.CreateCopy(SKVertexMode.Triangles, Vertices.Points.ToArray(), Vertices.Colors.ToArray())
            : null;

        public override SKShader GetShader(SKMatrix skMatrix, GraphicsState state)
        {
            var calcMatrix = CalculateMatrix(skMatrix, state);
            using var picture = Render(SKMatrix.Identity);
            var cullRect = picture?.CullRect ?? SKRect.Empty;
            return picture == null ? null : SKShader.CreatePicture(picture, SKShaderTileMode.Decal, SKShaderTileMode.Decal, calcMatrix, cullRect);//, SKMatrix.Identity, paint.CullRect);
        }       

        protected virtual SKPicture Render(SKMatrix spaceMatrix)
        {
            var skVertices = SKVertices;
            if (skVertices == null)
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
            canvas.DrawVertices(skVerteces, SKBlendMode.Modulate, paint);
            return recorder.EndRecording();
        }

        public override SKRect? GetBounds()
        {
            var vertices = Vertices;
            if (vertices == null)
                return null;
            var box = SKRect.Create(vertices.Points.First(), SKSize.Empty);
            box.Add(vertices.Points);
            return SKRect.Inflate(box, 1, 1);
        }

        private float[] GenerateDecode()
        {
            long maxSrcCoord = (long)Math.Pow(2, BitsPerCoordinate) - 1;
            long maxSrcColor = (long)Math.Pow(2, BitsPerComponent) - 1;
            var array = new float[4 + NumberOfColorComponents];
            array[0] = -maxSrcCoord;
            array[1] = maxSrcCoord;
            array[2] = -maxSrcCoord;
            array[3] = maxSrcCoord;
            for (int i = 4; i < array.Length - 4; i++)
            {
                array[i] = -maxSrcColor;
                array[++i] = maxSrcColor;
            }
            return array;
        }

        /**
        * Calculate the interpolation, see p.345 pdf spec 1.7.
        *
        * @param src src value
        * @param srcMax max src value (2^bits-1)
        * @param dstMin min dst value
        * @param dstMax max dst value
        * @return interpolated value
        */
        protected static float Interpolate(float src, long srcMax, float dstMin, float dstMax)
        {
            return dstMin + src * (dstMax - dstMin) / srcMax;
        }

        /**
        * Read a vertex from the bit input stream performs interpolations.
        *
        * @param input bit input stream
        * @param maxSrcCoord max value for source coordinate (2^bits-1)
        * @param maxSrcColor max value for source color (2^bits-1)
        * @param rangeX dest range for X
        * @param rangeY dest range for Y
        * @param colRangeTab dest range array for colors
        * @param matrix the pattern matrix concatenated with that of the parent content stream
        * @param xform the affine transformation
        * @return a new vertex with the flag and the interpolated values
        * @throws IOException if something went wrong
        */
        protected (SKPoint, SKColor) ReadVertex(IInputStream input, long maxSrcCoord, long maxSrcColor)
        {
            Span<float> colorComponentTab = stackalloc float[NumberOfColorComponents];
            var rangeX = Decodes[0];
            var rangeY = Decodes[1];
            long x = input.ReadBits(BitsPerCoordinate);
            long y = input.ReadBits(BitsPerCoordinate);
            float dstX = Interpolate(x, maxSrcCoord, rangeX.Low, rangeX.High);
            float dstY = Interpolate(y, maxSrcCoord, rangeY.Low, rangeY.High);
            var p = new SKPoint(dstX, dstY);

            for (int n = 0; n < colorComponentTab.Length; ++n)
            {
                int color = (int)input.ReadBits(BitsPerComponent);
                var range = Decodes[n + 2];
                colorComponentTab[n] = Interpolate(color, maxSrcColor, range.Low, range.High);
            }

            // "Each set of vertex data shall occupy a whole number of bytes.
            // If the total number of bits required is not divisible by 8, the last data byte
            // for each vertex is padded at the end with extra bits, which shall be ignored."
            input.ByteAlign();
            SKColor skColor = GetSKColor(colorComponentTab);
            return (p, skColor);
        }

        protected SKColor GetSKColor(ReadOnlySpan<float> colorComponents)
        {
            if (Function != null)
            {
                colorComponents = Function.Calculate(colorComponents);
            }
            return ColorSpace.GetSKColor(colorComponents);
        }

        protected virtual Vertices LoadTriangles()
        {
            int bitsPerFlag = BitsPerFlag;
            var dict = this;
            if (dict is not PdfStream stream)
            {
                return null;
            }
            var points = new List<SKPoint>();
            var colors = new List<SKColor>();
            long maxSrcCoord = (long)Math.Pow(2, BitsPerCoordinate) - 1;
            long maxSrcColor = (long)Math.Pow(2, BitsPerComponent) - 1;
            var vertextBitLength = GetVertextBitLength();
            var triangleBitLength = GetTriangleBitLength(vertextBitLength);
            var vertextLength = vertextBitLength / 8;
            var triangleLength = triangleBitLength / 8;

            var input = stream.GetInputStream();
            byte flag;
            try
            {
                flag = (byte)(input.ReadBits(bitsPerFlag) & 3);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }

            while (true)
            {
                (SKPoint point, SKColor color) p0;
                (SKPoint point, SKColor color) p1;
                (SKPoint point, SKColor color) p2;
                int index;
                try
                {
                    switch (flag)
                    {
                        case 0:
                            if (input.Position + triangleLength > input.Length)
                                return new Vertices(points, colors);
                            p0 = ReadVertex(input, maxSrcCoord, maxSrcColor);
                            flag = (byte)(input.ReadBits(bitsPerFlag) & 3);
                            if (flag != 0)
                            {
                                Debug.WriteLine($"error: bad triangle: {flag}");
                            }
                            p1 = ReadVertex(input, maxSrcCoord, maxSrcColor);
                            flag = (byte)(input.ReadBits(bitsPerFlag) & 3);
                            if (flag != 0)
                            {
                                Debug.WriteLine($"error: bad triangle: {flag}");
                            }
                            p2 = ReadVertex(input, maxSrcCoord, maxSrcColor);

                            break;
                        case 1:
                        case 2:
                            index = flag == 1 ? points.Count - 2 : points.Count - 3;
                            if (index < 0)
                            {
                                Debug.WriteLine($"error: broken data stream");
                                return new Vertices(points, colors);
                            }
                            else
                            {
                                if (input.Position + vertextLength > input.Length)
                                    return new Vertices(points, colors);

                                p0 = (points[index], colors[index]);
                                p1 = (points[points.Count - 1], colors[points.Count - 1]);
                                p2 = ReadVertex(input, maxSrcCoord, maxSrcColor);
                            }
                            break;
                        default:
                            Debug.WriteLine($"warn: bad flag: {flag}");
                            return new Vertices(points, colors);
                    }
                    points.Add(p0.point);
                    colors.Add(p0.color);
                    points.Add(p1.point);
                    colors.Add(p1.color);
                    points.Add(p2.point);
                    colors.Add(p2.color);
                    if (input.Position >= input.Length)
                        break;
                    flag = (byte)(input.ReadBits(bitsPerFlag) & 3);
                }
                catch
                {
                    break;
                }
            }
            return new Vertices(points, colors);
        }

        private int GetTriangleBitLength(int vertextBitLength)
        {
            return vertextBitLength * 3 + 2 * BitsPerFlag;
        }

        protected int GetVertextBitLength()
        {
            return 2 * BitsPerCoordinate + NumberOfColorComponents * BitsPerComponent;
        }

    }

    public class Vertices
    {
        private List<SKPoint> points;
        private List<SKColor> colors;

        public Vertices(List<SKPoint> points, List<SKColor> colors)
        {
            Points = points;
            Colors = colors;
        }

        public List<SKPoint> Points { get => points; set => points = value; }

        public List<SKColor> Colors { get => colors; set => colors = value; }

    }
}
