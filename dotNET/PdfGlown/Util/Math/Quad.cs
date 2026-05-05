/*
  Copyright 2011-2015 Stefano Chizzolini. http://www.pdfclown.org

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

namespace PdfGlown.Util.Math
{
    /// <summary>Quadrilateral shape.</summary>
    public struct Quad : IEquatable<Quad>
    {
        private static float Min(float lengthTopLeft, float lengthTopRight, float lengthBottomLeft, float lengthBottomRight)
        {
            var top = lengthTopLeft < lengthTopRight ? lengthTopLeft : lengthTopRight;
            var bottom = lengthBottomLeft < lengthBottomRight ? lengthBottomLeft : lengthBottomRight;
            return top < bottom ? top : bottom;
        }

        private static float Max(float lengthTopLeft, float lengthTopRight, float lengthBottomLeft, float lengthBottomRight)
        {
            var top = lengthTopLeft > lengthTopRight ? lengthTopLeft : lengthTopRight;
            var bottom = lengthBottomLeft > lengthBottomRight ? lengthBottomLeft : lengthBottomRight;
            return top > bottom ? top : bottom;
        }

        public static readonly Quad Empty = new Quad(SKPoint.Empty, SKPoint.Empty, SKPoint.Empty, SKPoint.Empty);

        public static bool operator ==(Quad value, Quad value2) => value.Equals(value2);

        public static bool operator !=(Quad value, Quad value2) => !value.Equals(value2);

        public static Quad Union(Quad value, Quad value2)
        {
            return value.Add(value2);
        }

        public static Quad Inflate(Quad value, float valueX, float valueY)
        {
            return value.Inflate(valueX, valueY);
        }

        public static Quad Transform(Quad quad, ref SKMatrix matrix)
        {
            var temp = new Quad(quad);
            return temp.Transform(ref matrix);
        }

        private SKPoint point0;
        private SKPoint point1;
        private SKPoint point2;
        private SKPoint point3;

        public Quad(SKRect rectangle)
            : this(new SKPoint(rectangle.Left, rectangle.Top),
                  new SKPoint(rectangle.Right, rectangle.Top),
                  new SKPoint(rectangle.Right, rectangle.Bottom),
                  new SKPoint(rectangle.Left, rectangle.Bottom))
        { }

        public Quad(Quad quad)
            : this(quad.point0,
                  quad.point1,
                  quad.point2,
                  quad.point3)
        { }

        public Quad(SKPoint pointTopLeft, SKPoint pointTopRight, SKPoint pointBottomRight, SKPoint pointBottomLeft)
        {
            point0 = pointTopLeft;
            point1 = pointTopRight;
            point2 = pointBottomRight;
            point3 = pointBottomLeft;
        }

        public Quad(float left, float top, float right, float bottom)
        {
            point0 = new SKPoint(left, top);
            point1 = new SKPoint(right, top);
            point2 = new SKPoint(right, bottom);
            point3 = new SKPoint(left, bottom);
        }

        public bool IsEmpty => Equals(Empty);

        public SKPoint Point0 => point0;

        public SKPoint Point1 => point1;

        public SKPoint Point2 => point2;

        public SKPoint Point3 => point3;

        public SKPoint? Middle => SKLine.FindIntersection(new SKLine(point0, point2), new SKLine(point3, point1), false);

        public float Width => SKPoint.Distance(point0, point1);

        public float HorizontalLength => System.Math.Abs(MaxX - MinX);

        public float Height => SKPoint.Distance(point1, point2);

        public float VerticalLenght => System.Math.Abs(MaxY - MinY);

        public float MinY => Min(point0.Y, point1.Y, point2.Y, point3.Y);

        public float MinX => Min(point0.X, point1.X, point2.X, point3.X);

        public float MaxX => Max(point0.X, point1.X, point2.X, point3.X);

        public float MaxY => Max(point0.Y, point1.Y, point2.Y, point3.Y);

        public SKPoint[] GetPoints() => new SKPoint[4] { point0, point1, point2, point3 };

        public SKPoint[] GetClosedPoints() => new SKPoint[5] { point0, point1, point2, point3, point0 };

        public SKPath GetPath()
        {
            var points = new SKPoint[4] { point0, point1, point2, point3 };
            var path = new SKPath();//FillMode.Alternate
            path.AddPoly(points);
            return path;
        }

        public SKRect GetBounds()
        {
            return new SKRect(MinX, MinY, MaxX, MaxY);
        }

        /// <summary>Expands the size of this quad stretching around its center.</summary>
        /// <param name="value">Expansion extent.</param>
        /// <returns>This quad.</returns>
        public Quad Inflate(float value)
        {
            return Inflate(value, value);
        }

        /// <summary>Expands the size of this quad stretching around its center.</summary>
        /// <param name="valueX">Expansion's horizontal extent.</param>
        /// <param name="valueY">Expansion's vertical extent.</param>
        /// <returns>This quad.</returns>
        public Quad Inflate(float valueX, float valueY)
        {
            point0 = point0 + (SKPoint.Normalize(point0 - point3) + SKPoint.Normalize(point0 - point1)).Scale(valueX, valueY);
            point1 = point1 + (SKPoint.Normalize(point1 - point0) + SKPoint.Normalize(point1 - point2)).Scale(valueX, valueY);
            point2 = point2 + (SKPoint.Normalize(point2 - point1) + SKPoint.Normalize(point2 - point3)).Scale(valueX, valueY);
            point3 = point3 + (SKPoint.Normalize(point3 - point2) + SKPoint.Normalize(point3 - point0)).Scale(valueX, valueY);
            return this;
        }

        private Quad MatrixInflate(float valueX, float valueY)
        {
            SKRect oldBounds = GetBounds();
            var matrix = SKMatrix.CreateTranslation(oldBounds.MidX, oldBounds.MidY);
            matrix = matrix.PreConcat(SKMatrix.CreateScale(1 + valueX * 2 / oldBounds.Width, 1 + valueY * 2 / oldBounds.Height));
            matrix = matrix.PreConcat(SKMatrix.CreateTranslation(-oldBounds.MidX, -oldBounds.MidY));
            return Transform(ref matrix);
        }

        public Quad Transform(ref SKMatrix matrix)
        {
#if NET9_0_OR_GREATER
            Span<SKPoint> points = stackalloc SKPoint[4] { point0, point1, point2, point3 };
            matrix.MapPoints(points, points);
#else
            var points = new SKPoint[4] { point0, point1, point2, point3 };
            matrix.MapPoints(points, points);
#endif
            point0 = points[0];
            point1 = points[1];
            point2 = points[2];
            point3 = points[3];
            return this;
        }

        public bool Contains(SKPoint p)
        {
            return p.ContainsInTriangle(point0, point1, point3)
                || p.ContainsInTriangle(point1, point2, point3);
        }

        public bool Contains(float x, float y)
        {
            return Contains(new SKPoint(x, y));
        }

        public bool IntersectsWith(Quad value)
        {
            return new SKLine(point0, point1).FindIntersection(value) != null
                || new SKLine(point1, point2).FindIntersection(value) != null
                || new SKLine(point2, point3).FindIntersection(value) != null
                || new SKLine(point3, point0).FindIntersection(value) != null;
        }

        public bool IntersectsWith(SKRect value)
        {
            return new SKLine(value.Left, value.Top, value.Right, value.Top).FindIntersection(this) != null
                || new SKLine(value.Right, value.Top, value.Right, value.Bottom).FindIntersection(this) != null
                || new SKLine(value.Right, value.Bottom, value.Left, value.Bottom).FindIntersection(this) != null
                || new SKLine(value.Left, value.Bottom, value.Left, value.Top).FindIntersection(this) != null;
        }

        public bool ContainsOrIntersects(Quad quad)
        {
            return Contains(quad.point0)
                || Contains(quad.point1)
                || Contains(quad.point2)
                || Contains(quad.point3)
                || quad.Contains(point0)
                || quad.Contains(point1)
                || quad.Contains(point2)
                || quad.Contains(point3)
                || IntersectsWith(quad);
        }

        public bool ContainsOrIntersects(SKLine line)
        {
            return Contains(line.A)
                || Contains(line.B)
                || line.FindIntersection(this) != null;
        }

        public bool ContainsOrIntersects(SKRect rect)
        {
            return rect.Contains(point0)
                || rect.Contains(point1)
                || rect.Contains(point2)
                || rect.Contains(point3)
                || Contains(new SKPoint(rect.Left, rect.Top))
                || Contains(new SKPoint(rect.Right, rect.Top))
                || Contains(new SKPoint(rect.Right, rect.Bottom))
                || Contains(new SKPoint(rect.Left, rect.Bottom))
                || IntersectsWith(rect);
        }

        public bool Contains(Quad quad)
        {
            return Contains(quad.point0)
                && Contains(quad.point1)
                && Contains(quad.point2)
                && Contains(quad.point3);
        }

        public bool Contains(SKLine line)
        {
            return Contains(line.A)
                && Contains(line.B);
        }

        public bool Contains(SKRect rect)
        {
            return Contains(new SKPoint(rect.Left, rect.Top))
                && Contains(new SKPoint(rect.Right, rect.Top))
                && Contains(new SKPoint(rect.Right, rect.Bottom))
                && Contains(new SKPoint(rect.Left, rect.Bottom));
        }

        public Quad Add(Quad value)
        {
            Add(value.Point0);
            Add(value.Point1);
            Add(value.Point2);
            Add(value.Point3);
            return this;
        }

        enum AddMode
        {
            P0, P1, P2, P3,
        }

        public void Add(SKPoint point)
        {
            if (Contains(point))
                return;
            var vector0 = point - point0;
            var vector1 = point - point1;
            var vector2 = point - point2;
            var vector3 = point - point3;
            var length0 = vector0.Length;
            var length1 = vector1.Length;
            var length2 = vector2.Length;
            var length3 = vector3.Length;
            var min = Min(length0, length1, length2, length3);

            if (min < 0.01)
                return;
            var addMode = min == length0 ? AddMode.P0
                : min == length1 ? AddMode.P1
                : min == length2 ? AddMode.P2
                : AddMode.P3;

            switch (addMode)
            {
                case AddMode.P0:
                    if (SKLine.FindIntersection(point0, point1, point, point3, true) != null)
                    {
                        var line = new SKLine(point0 + vector0, point1 + vector0);
                        point0 = SKLine.FindIntersection(line, new SKLine(point0, point3), false) ?? point0;
                        point1 = SKLine.FindIntersection(line, new SKLine(point1, point2), false) ?? point1;
                    }
                    else if (SKLine.FindIntersection(point0, point3, point, point1, true) != null)
                    {
                        var line = new SKLine(point0 + vector0, point3 + vector0);
                        point0 = SKLine.FindIntersection(line, new SKLine(point0, point1), false) ?? point0;
                        point3 = SKLine.FindIntersection(line, new SKLine(point2, point3), false) ?? point3;
                    }
                    else if (SKLine.FindIntersection(point1, point2, point, point0, true) != null)
                    {
                        goto case AddMode.P2;
                    }
                    else if (SKLine.FindIntersection(point2, point3, point, point0, true) != null)
                    {
                        goto case AddMode.P3;
                    }
                    else
                    {
                        point1 = SKLine.FindIntersection(point0 + vector0, point1 + vector0, point1, point2, false) ?? point1;
                        point3 = SKLine.FindIntersection(point0 + vector0, point3 + vector0, point2, point3, false) ?? point3;
                        point0 = point;
                    }
                    break;
                case AddMode.P1:
                    if (SKLine.FindIntersection(point0, point1, point, point2, true) != null)
                    {
                        var line = new SKLine(point0 + vector1, point1 + vector1);
                        point1 = SKLine.FindIntersection(line, new SKLine(point1, point2), false) ?? point1;
                        point0 = SKLine.FindIntersection(line, new SKLine(point0, point3), false) ?? point0;
                    }
                    else if (SKLine.FindIntersection(point1, point2, point, point0, true) != null)
                    {
                        var line = new SKLine(point1 + vector1, point2 + vector1);
                        point1 = SKLine.FindIntersection(line, new SKLine(point0, point1), false) ?? point1;
                        point2 = SKLine.FindIntersection(line, new SKLine(point2, point3), false) ?? point2;
                    }
                    else if (SKLine.FindIntersection(point3, point0, point, point1, true) != null)
                    {
                        goto case AddMode.P0;
                    }
                    else if (SKLine.FindIntersection(point2, point3, point, point1, true) != null)
                    {
                        goto case AddMode.P2;
                    }
                    else
                    {
                        point0 = SKLine.FindIntersection(point0 + vector1, point1 + vector1, point0, point3, false) ?? point1;
                        point2 = SKLine.FindIntersection(point1 + vector1, point2 + vector1, point2, point3, false) ?? point2;
                        point1 = point;
                    }
                    break;
                case AddMode.P2:
                    if (SKLine.FindIntersection(point1, point2, point, point3, true) != null)
                    {
                        var line = new SKLine(point1 + vector2, point2 + vector2);
                        point2 = SKLine.FindIntersection(line, new SKLine(point2, point3), false) ?? point2;
                        point1 = SKLine.FindIntersection(line, new SKLine(point0, point1), false) ?? point1;
                    }
                    else if (SKLine.FindIntersection(point2, point3, point, point1, true) != null)
                    {
                        var line = new SKLine(point2 + vector2, point3 + vector2);
                        point2 = SKLine.FindIntersection(line, new SKLine(point1, point2), false) ?? point2;
                        point3 = SKLine.FindIntersection(line, new SKLine(point0, point3), false) ?? point3;
                    }
                    else if (SKLine.FindIntersection(point0, point1, point, point2, true) != null)
                    {
                        goto case AddMode.P1;
                    }
                    else if (SKLine.FindIntersection(point3, point0, point, point2, true) != null)
                    {
                        goto case AddMode.P3;
                    }
                    else
                    {
                        point1 = SKLine.FindIntersection(point1 + vector2, point2 + vector2, point0, point1, false) ?? point1;
                        point3 = SKLine.FindIntersection(point2 + vector2, point3 + vector2, point0, point3, false) ?? point3;
                        point2 = point;
                    }
                    break;
                case AddMode.P3:
                    if (SKLine.FindIntersection(point3, point0, point, point2, true) != null)
                    {
                        var line = new SKLine(point3 + vector3, point0 + vector3);
                        point3 = SKLine.FindIntersection(line, new SKLine(point2, point3), false) ?? point3;
                        point0 = SKLine.FindIntersection(line, new SKLine(point0, point1), false) ?? point0;
                    }
                    else if (SKLine.FindIntersection(point2, point3, point, point0, true) != null)
                    {
                        var line = new SKLine(point2 + vector3, point3 + vector3);
                        point3 = SKLine.FindIntersection(line, new SKLine(point0, point3), false) ?? point3;
                        point2 = SKLine.FindIntersection(line, new SKLine(point1, point2), false) ?? point2;
                    }
                    else if (SKLine.FindIntersection(point0, point1, point, point3, true) != null)
                    {
                        goto case AddMode.P0;
                    }
                    else if (SKLine.FindIntersection(point1, point2, point, point3, true) != null)
                    {
                        goto case AddMode.P2;
                    }
                    else
                    {
                        point0 = SKLine.FindIntersection(point3 + vector3, point0 + vector3, point0, point1, false) ?? point0;
                        point2 = SKLine.FindIntersection(point2 + vector3, point3 + vector3, point1, point2, false) ?? point2;
                        point3 = point;
                    }
                    break;
            }           
            
        }

        public override bool Equals(object other) => other is Quad quad ? Equals(quad) : false;

        public bool Equals(Quad other)
        {
            return point0.Equals(other.point0)
                && point1.Equals(other.point1)
                && point2.Equals(other.point2)
                && point3.Equals(other.point3);
        }

        public override string ToString()
        {
            return GetBounds().ToString();
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(point0, point1, point2, point3);
        }

        public bool ContainsOrIntersect(SKLine selectionLine)
        {
            throw new NotImplementedException();
        }
    }
}