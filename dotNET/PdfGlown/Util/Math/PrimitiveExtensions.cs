/*
  Copyright 2015 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfGlown.Documents.Contents.Composition;
using PdfGlown.Objects;
using SkiaSharp;
using System.Collections.Generic;

namespace PdfGlown.Util.Math
{
    public static class PrimitiveExtensions
    {
        public static SKRect ToSKRect(this PdfPadding padding)
        {
            return new SKRect((float)padding.Left, (float)padding.Bottom, (float)padding.Right, (float)padding.Top);
        }

        public static SKRect ToSKRect(this PdfRectangle rectangle)
        {
            return new SKRect((float)rectangle.Left, (float)rectangle.Bottom, (float)rectangle.Right, (float)rectangle.Top);
        }

        public static SKRect ToSKRect(this PdfArray array)
        {
            PdfRectangle.Normalize(array.items);
            return new SKRect(array.GetFloat(0), array.GetFloat(1), array.GetFloat(2), array.GetFloat(3));
        }

        public static SKPoint ToSKPoint(this PdfArray array)
        {
            return new SKPoint(array.GetFloat(0), array.GetFloat(1));
        }

        public static SKMatrix ToSkMatrix(this PdfArray array)
        {
            return new SKMatrix
            {
                ScaleX = array.GetFloat(0),
                SkewY = array.GetFloat(1),
                SkewX = array.GetFloat(2),
                ScaleY = array.GetFloat(3),
                TransX = array.GetFloat(4),
                TransY = array.GetFloat(5),
                Persp2 = 1
            };
        }

        public static SKPath ToSKPath(this PdfArray array)
        {
            var path = new SKPath();
            var pointLength = array.Count;
            for (int pointIndex = 0; pointIndex < pointLength; pointIndex += 2)
            {
                var point = GetPagePoint(array, pointIndex);

                if (path.IsEmpty)
                {
                    path.MoveTo(point);
                }
                //else if (pointIndex + 3 < pointLength)
                //{
                //    var nextPoint = GetPagePoint(pathObject, pointIndex + 2);                        
                //    path.QuadTo(, point);
                //}
                else
                {
                    path.LineTo(point);
                }
            }
            //path.Close();
            return path;

            static SKPoint GetPagePoint(PdfArray pathObject, int pointIndex)
            {
                return new SKPoint(
                    pathObject.GetFloat(pointIndex),
                    pathObject.GetFloat(pointIndex + 1));
            }
        }

        public static PdfRectangle ToPdfRectangle(this SKRect rect)
        {
            return new PdfRectangle(rect);
        }

        public static PdfArray ToPdfArray(this SKPoint point)
        {
            return new PdfArrayImpl(2) { point.X, point.Y };
        }

        public static PdfArray ToPdfArray(this SKMatrix value)
        {
            return new PdfArrayImpl(6)
            {
                value.ScaleX,
                value.SkewY,
                value.SkewX,
                value.ScaleY,
                value.TransX,
                value.TransY
            };
        }

        public static PdfArray ToPdfArray(this ICollection<SKPoint> points, ref SKRect box)
        {
            var array = new PdfArrayImpl(points.Count * 2);
            foreach (SKPoint point in points)
            {
                if (box == SKRect.Empty)
                { box = SKRect.Create(point.X, point.Y, 0, 0); }
                else
                { box.Add(point); }
                array.Add(point.X); // x.
                array.Add(point.Y); // y.
            }
            return array;
        }

        public static void UpdatePdfArray(this SKMatrix value, PdfArray array)
        {
            array.Set(0, value.ScaleX);
            array.Set(1, value.SkewY);
            array.Set(2, value.SkewX);
            array.Set(3, value.ScaleY);
            array.Set(4, value.TransX);
            array.Set(5, value.TransY);
        }

        public static void Update(this PdfRectangle rectangle, SKRect skRect)
        {
            rectangle.Left = skRect.Left;
            rectangle.Bottom = skRect.Top;
            rectangle.Right = skRect.Right;
            rectangle.Top = skRect.Bottom;
        }

        public static float Cross(this SKPoint u, SKPoint v)
        {
            return u.X * v.Y - u.Y * v.X;
        }

        public static bool ContainsInTriangle(this SKPoint point, SKPoint point1, SKPoint point2, SKPoint point3)
        {
            //Calculate barycentric coordinates
            var denominator1 = (point2.Y - point1.Y) * (point3.X - point1.X) + (point1.X - point2.X) * (point3.Y - point1.Y);
            var alpha1 = ((point2.Y - point1.Y) * (point.X - point1.X) + (point1.X - point2.X) * (point.Y - point1.Y)) / denominator1;
            var beta1 = ((point1.Y - point3.Y) * (point.X - point1.X) + (point3.X - point1.X) * (point.Y - point1.Y)) / denominator1;
            var gamma1 = 1 - alpha1 - beta1;

            return alpha1 >= 0 && beta1 >= 0 && gamma1 >= 0;
        }

        public static bool Contains(this SKRect rect, Quad quad)
        {
            return rect.Contains(quad.Point0)
                && rect.Contains(quad.Point1)
                && rect.Contains(quad.Point2)
                && rect.Contains(quad.Point3); 
        }

        public static bool ContainsAny(this SKRect rect, Quad quad)
        {
            return rect.Contains(quad.Point0)
                || rect.Contains(quad.Point1)
                || rect.Contains(quad.Point2)
                || rect.Contains(quad.Point3);
        }

        public static SKPoint Invert(this SKPoint p)
        {
            return new SKPoint(p.X * -1, p.Y * -1);
        }

        public static SKPoint Multiply(this SKPoint p, float v)
        {
            return new SKPoint(p.X * v, p.Y * v);
        }

        public static SKPoint Multiply(this SKPoint p, SKPoint v)
        {
            return new SKPoint(p.X * v.X, p.Y * v.Y);
        }

        public static SKPoint PerpendicularClockwise(this SKPoint vector2)
        {
            return new SKPoint(vector2.Y, -vector2.X);
        }

        public static SKPoint PerpendicularCounterClockwise(this SKPoint vector2)
        {
            return new SKPoint(-vector2.Y, vector2.X);
        }

        public static SKPoint GetPerp(this SKPoint a, float v, bool xbasis = true)
        {
            var b = xbasis
                ? SKPoint.Normalize(new SKPoint(a.Y == 0 ? 0 : v, a.Y == 0 ? v : -(a.X * v) / a.Y))
                : SKPoint.Normalize(new SKPoint(a.X == 0 ? v : -(a.Y * v) / a.X, a.X == 0 ? 0 : v));
            var abs = System.Math.Abs(v);
            return new SKPoint(b.X * abs, b.Y * abs);
        }

        public static void AddOpenArrow(this SKPath path, SKPoint point, SKPoint normal)
        {
            var matrix1 = SKMatrix.CreateRotationDegrees(35);
            var rotated1 = matrix1.MapVector(normal.X, normal.Y);
            var matrix2 = SKMatrix.CreateRotationDegrees(-35);
            var rotated2 = matrix2.MapVector(normal.X, normal.Y);

            path.MoveTo(point + new SKPoint(rotated1.X * 8, rotated1.Y * 8));
            path.LineTo(point);
            path.LineTo(point + new SKPoint(rotated2.X * 8, rotated2.Y * 8));
        }

        public static void AddOpenArrow(this PrimitiveComposer path, SKPoint point, SKPoint normal)
        {
            var matrix1 = SKMatrix.CreateRotationDegrees(35);
            var rotated1 = matrix1.MapVector(normal.X, normal.Y);
            var matrix2 = SKMatrix.CreateRotationDegrees(-35);
            var rotated2 = matrix2.MapVector(normal.X, normal.Y);

            path.StartPath(point + new SKPoint(rotated1.X * 8, rotated1.Y * 8));
            path.DrawLine(point);
            path.DrawLine(point + new SKPoint(rotated2.X * 8, rotated2.Y * 8));
        }

        public static void AddCloseArrow(this SKPath path, SKPoint point, SKPoint normal)
        {
            var matrix1 = SKMatrix.CreateRotationDegrees(35);
            var rotated1 = matrix1.MapVector(normal.X, normal.Y);
            var matrix2 = SKMatrix.CreateRotationDegrees(-35);
            var rotated2 = matrix2.MapVector(normal.X, normal.Y);

            path.MoveTo(point + new SKPoint(rotated1.X * 8, rotated1.Y * 8));
            path.LineTo(point);
            path.LineTo(point + new SKPoint(rotated2.X * 8, rotated2.Y * 8));
            path.Close();
        }

        public static void AddClosedArrow(this PrimitiveComposer path, SKPoint point, SKPoint normal)
        {
            var matrix1 = SKMatrix.CreateRotationDegrees(35);
            var rotated1 = matrix1.MapVector(normal.X, normal.Y);
            var matrix2 = SKMatrix.CreateRotationDegrees(-35);
            var rotated2 = matrix2.MapVector(normal.X, normal.Y);

            path.StartPath(point + new SKPoint(rotated1.X * 8, rotated1.Y * 8));
            path.DrawLine(point);
            path.DrawLine(point + new SKPoint(rotated2.X * 8, rotated2.Y * 8));
            path.ClosePath();
        }

        public static void Add(this ref SKRect rectangle, IEnumerable<SKPoint> points)
        {
            foreach (var point in points)
            {
                rectangle.Add(point);
            }
        }

        public static void Add(this ref SKRect rectangle, SKPoint[] points)
        {
            foreach (var point in points)
            {
                rectangle.Add(point);
            }
        }

        public static void Add(this ref SKRect rectangle, IEnumerable<SKRect> rects)
        {
            foreach (var rect in rects)
            {
                rectangle.Add(rect);
            }
        }
        public static void Add(this ref SKRect rectangle, Quad quad)
        {
            rectangle.Add(quad.Point0);
            rectangle.Add(quad.Point1);
            rectangle.Add(quad.Point2);
            rectangle.Add(quad.Point3);
        }

        public static void Add(this ref SKRect rectangle, SKRect rect)
        {
            rectangle.Add(new SKPoint(rect.Left, rect.Top));
            rectangle.Add(new SKPoint(rect.Right, rect.Bottom));
        }

        public static void Add(this ref SKRect rectangle, SKPoint point)
        {
            if (point.X < rectangle.Left)
            {
                rectangle.Left = point.X;
            }
            if (point.X > rectangle.Right)
            {
                rectangle.Right = point.X;
            }
            if (point.Y < rectangle.Top)
            {
                rectangle.Top = point.Y;
            }
            if (point.Y > rectangle.Bottom)
            {
                rectangle.Bottom = point.Y;
            }
        }

        public static SKPoint Center(this SKRect rectangle)
        {
            return new SKPoint(rectangle.CenterX(), rectangle.CenterY());
        }

        public static float CenterX(this SKRect rectangle)
        {
            return rectangle.Left + rectangle.Width / 2;
        }

        public static float CenterY(this SKRect rectangle)
        {
            return rectangle.Top + rectangle.Height / 2;
        }

        public static void Normalize(this ref SKRect rectangle)
        {
            if (rectangle.Left > rectangle.Right)
            {
                var temp = rectangle.Left;
                rectangle.Left = rectangle.Right;
                rectangle.Right = temp;
            }
            if (rectangle.Bottom > rectangle.Top)
            {
                var temp = rectangle.Bottom;
                rectangle.Bottom = rectangle.Top;
                rectangle.Top = temp;
            }
        }

        public static SKRect Normalize(SKRect rectangle)
        {
            var left = rectangle.Left;
            var right = rectangle.Right;
            var top = rectangle.Top;
            var bottom = rectangle.Bottom;
            if (rectangle.Left > rectangle.Right)
            {
                left = rectangle.Right;
                right = rectangle.Left;
            }
            if (rectangle.Top > rectangle.Bottom)
            {
                top = rectangle.Bottom;
                bottom = rectangle.Top;
            }
            return new SKRect(left, top, right, bottom);
        }

        public static SKPath ToPath(this SKRect rectangle)
        {
            var path = new SKPath();
            path.AddRect(rectangle);
            return path;
        }

        public static SKRect Round(this SKRect value, int precision = 5)
        {
            return new SKRect(
                (float)System.Math.Round(value.Left, precision),
                (float)System.Math.Round(value.Top, precision),
                (float)System.Math.Round(value.Right, precision),
                (float)System.Math.Round(value.Bottom, precision));
        }

        public static SKPoint Round(this SKPoint value, int precision = 5)
        {
            return new SKPoint(
                (float)System.Math.Round(value.X, precision),
                (float)System.Math.Round(value.Y, precision));
        }

        //public static SKPoint Transform(
        //  this SKMatrix matrix,
        //  SKPoint point
        //  )
        //{
        //  var points = new SKPoint[]{point};
        //  matrix.MapPoints(points);
        //  return points[0];
        //}

        public static SKRect Align(this SKRect rectangle, SKPoint anchor, SKPoint alignment)
        {
            return SKRect.Create(
              anchor.X - rectangle.Width * (1 - alignment.X.CompareTo(0)) / 2,
              anchor.Y - rectangle.Height * (1 - alignment.Y.CompareTo(0)) / 2,
              rectangle.Width,
              rectangle.Height);
        }

        /// <summary>Gets the size scaled to the specified limit.</summary>
        /// <remarks>In particular, the limit matches the largest dimension and proportionally scales the
        /// other one; for example, a limit 300 applied to size Dimension2D(100, 200) returns
        /// Dimension2D(150, 300).</remarks>
        /// <param name="size"> Size to scale.</param>
        /// <param name="limit"> Scale limit.</param>
        /// <returns>Scaled size.</returns>
        public static SKSize Scale(this SKSize size, float limit)
        {
            if (limit == 0)
                return size;
            else
            {
                float sizeRatio = size.Width / size.Height;
                return sizeRatio > 1
                  ? new SKSize(limit, limit / sizeRatio)
                  : new SKSize(limit * sizeRatio, limit);
            }
        }

        /// <summary>Gets the size scaled to the specified limit.</summary>
        /// <remarks>In particular, implicit (zero-valued) limit dimensions correspond to proportional
        /// dimensions; for example, a limit Dimension2D(0, 300) means 300 high and proportionally wide.
        /// </remarks>
        /// <param name="size"> Size to scale.</param>
        /// <param name="limit"> Scale limit.</param>
        /// <returns>Scaled size.</returns>
        public static SKSize Scale(this SKSize size, SKSize limit)
        {
            if (limit.Width == 0)
            {
                if (limit.Height == 0)
                    return size;
                else
                    return new SKSize(limit.Height * size.Width / size.Height, limit.Height);
            }
            else if (limit.Height == 0)
                return new SKSize(limit.Width, limit.Width * size.Height / size.Width);
            else
                return limit;
        }

        public static SKPoint Scale(this SKPoint point, float scale)
        {
            return point.Scale(scale, scale);
        }

        public static SKPoint Scale(this SKPoint point, float scaleX, float scaleY)
        {
            return new SKPoint(point.X * scaleX, point.Y * scaleY);
        }

        public static SKPoint UnScale(this SKPoint point, float scale)
        {
            return point.UnScale(scale, scale);
        }

        public static SKPoint UnScale(this SKPoint point, float scaleX, float scaleY)
        {
            return new SKPoint(point.X / scaleX, point.Y / scaleY);
        }
    }
}

