using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;


namespace PdfGlown.Tools
{
    //https://github.com/mono/SkiaSharp.Extended/blob/master/SkiaSharp.Extended.Svg/source/SkiaSharp.Extended.Svg.Shared/SKSvg.cs
    public class SvgImage
    {
        public static readonly Dictionary<string, SvgImage> SvgCache = new Dictionary<string, SvgImage>(StringComparer.Ordinal);
        private static readonly char[] WS = new char[] { ' ', '\t', '\n', '\r' };
        private static readonly Regex unitRe = new Regex("px|pt|em|ex|pc|cm|mm|in");
        private static readonly Regex percRe = new Regex("%");
        private static readonly Regex urlRe = new Regex(@"url\s*\(\s*#([^\)]+)\)");
        private static readonly float PixelsPerInch = 160f;

        public static SvgImage GetCache(string imageName)
        {
            return GetCache(typeof(SvgImage).Assembly, imageName);
        }


        public static SvgImage GetCache(Assembly assembly, string imageName)
        {
            if (string.IsNullOrEmpty(imageName))
                return null;
            if (!SvgCache.TryGetValue(imageName, out var svg))
            {
                var keyName = $"{assembly.GetName().Name}.Assets.{imageName}.svg";
                using (Stream stream = assembly.GetManifestResourceStream(keyName))
                {
                    if (stream != null)
                    {
                        svg = new SvgImage();
                        svg.Load(stream);
                        SvgCache[imageName] = svg;
                    }
                    else
                    {
                        SvgCache[imageName] = null;
                    }

                }
            }
            return svg;
        }


        public static SKMatrix GetMatrix(SvgImage svg, SKRect bound, float indent, float rotate = 0)
        {
            bound.Inflate(-indent, -indent);

            return GetMatrix(svg, bound.Left, bound.Top, bound.Width, bound.Height, rotate);
        }

        public static SKMatrix GetMatrix(SvgImage svg, float left, float top, float widthR, float heightR, float rotate = 0)
        {
            float canvasMin = Math.Min(widthR, heightR);
            // get the size of the picture
            float svgMax = Math.Max(svg.ViewBox.Width, svg.ViewBox.Height);
            // get the scale to fill the screen
            float scale = canvasMin / svgMax;
            var width = svg.ViewBox.Width * scale;
            var height = svg.ViewBox.Height * scale;

            var matrix = SKMatrix.Identity;

            if (rotate > 0)
            {
                matrix = matrix.PreConcat(SKMatrix.CreateRotationDegrees(rotate, (left + widthR / 2), (top + heightR / 2)));
            }

            matrix = matrix.PreConcat(SKMatrix.CreateTranslation(left + (widthR - width) / 2F, top + (heightR - height) / 2F));
            matrix = matrix.PreConcat(SKMatrix.CreateScale(scale, scale));

            return matrix;
        }

        public static void DrawImage(SKCanvas canvas, string imageName, SKColor color, SKRect bounds, float indent = 2)
        {
            DrawImage(canvas, typeof(SvgImage).Assembly, imageName, color, bounds, indent);
        }

        public static void DrawImage(SKCanvas canvas, Assembly assembly, string imageName, SKColor color, SKRect bounds, float indent = 2)
        {
            var svg = GetCache(assembly, imageName);
            if (svg != null)
            {
                using var paint = new SKPaint { Color = color };
                DrawImage(canvas, svg, paint, bounds, indent);
            }
        }

        public static void DrawImage(SKCanvas canvas, SvgImage svg, SKPaint paint, SKRect bounds, float indent = 2)
        {
            var matrix = GetMatrix(svg, bounds, indent);
            canvas.Save();
#if NET9_0_OR_GREATER
            canvas.Concat(in matrix);
#else
            canvas.Concat(ref matrix);
#endif
            canvas.DrawPath(svg.Path, paint);
            canvas.Restore();
        }

        public SvgImage()
        {
            Path = new SKPath();
        }

        public SKPath Path { get; private set; }

        public SKRect ViewBox { get; private set; }

        public SKSize CanvasSize { get; private set; }


        private void Load(Stream stream)
        {
            var matrix = SKMatrix.CreateIdentity();
            Dictionary<int, SKMatrix> stack = new Dictionary<int, SKMatrix>();
            using (var reader = XmlReader.Create(stream))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.EndElement)
                    {
                        if (stack.TryGetValue(reader.Depth, out var stackMatrix))
                        {
                            stack.Remove(reader.Depth);
                            matrix = stackMatrix;
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (string.Equals(reader.Name, "svg", StringComparison.Ordinal))
                        {
                            var preserveAspectRatio = reader["preserveAspectRatio"];
                            // get the SVG dimensions
                            var viewBoxA = reader["viewBox"] ?? reader["viewPort"];
                            if (viewBoxA != null)
                            {
                                ViewBox = ReadRectangle(viewBoxA);
                            }

                            var widthA = reader["width"];
                            var heightA = reader["height"];
                            var width = ReadNumber(widthA);
                            var height = ReadNumber(heightA);
                            var size = new SKSize(width, height);

                            if (widthA == null)
                            {
                                size.Width = ViewBox.Width;
                            }
                            else if (widthA.IndexOf('%') > -1)
                            {
                                size.Width *= ViewBox.Width;
                            }
                            if (heightA == null)
                            {
                                size.Height = ViewBox.Height;
                            }
                            else if (heightA != null && heightA.IndexOf('%') > -1)
                            {
                                size.Height *= ViewBox.Height;
                            }
                            CanvasSize = size;

                            if (!ViewBox.IsEmpty && (ViewBox.Width != CanvasSize.Width || ViewBox.Height != CanvasSize.Height))
                            {
                                if (preserveAspectRatio == "none")
                                {
                                    matrix = matrix.PostConcat(SKMatrix.CreateScale(CanvasSize.Width / ViewBox.Width, CanvasSize.Height / ViewBox.Height));
                                }
                                else
                                {
                                    // TODO: just center scale for now
                                    var scale = Math.Min(CanvasSize.Width / ViewBox.Width, CanvasSize.Height / ViewBox.Height);
                                    var centered = SKRect.Create(CanvasSize).AspectFit(ViewBox.Size);
                                    matrix = matrix.PostConcat(SKMatrix.CreateTranslation(centered.Left, centered.Top))
                                        .PostConcat(SKMatrix.CreateScale(scale, scale));
                                }
                            }

                            // translate the canvas by the viewBox origin
                            matrix = matrix.PostConcat(SKMatrix.CreateTranslation(-ViewBox.Left, -ViewBox.Top));
                        }
                        else
                        {
                            var transform = reader["transform"];
                            if (transform != null)
                            {
                                stack[reader.Depth] = matrix;
                                var trMatrix = ReadTransform(transform);
                                matrix = matrix.PostConcat(trMatrix);
                            }

                            if (string.Equals(reader.Name, "path", StringComparison.Ordinal))
                            {
                                var pathData = reader["d"];
                                using var path = SKPath.ParseSvgPathData(pathData);
#if NET9_0_OR_GREATER
                                Path.AddPath(path, in matrix);
#else
                                Path.AddPath(path, ref matrix);
#endif
                            }
                            else if (string.Equals(reader.Name, "polyline", StringComparison.Ordinal))
                            {
                                var pathData = "M" + reader["points"];
                                using var path = SKPath.ParseSvgPathData(pathData);
#if NET9_0_OR_GREATER
                                Path.AddPath(path, in matrix);
#else
                                Path.AddPath(path, ref matrix);
#endif
                            }
                            else if (string.Equals(reader.Name, "polygon", StringComparison.Ordinal))
                            {
                                var pathData = "M" + reader["points"] + " Z";
                                using var path = SKPath.ParseSvgPathData(pathData);
#if NET9_0_OR_GREATER
                                Path.AddPath(path, in matrix);
#else
                                Path.AddPath(path, ref matrix);
#endif
                            }
                            else if (string.Equals(reader.Name, "line", StringComparison.Ordinal))
                            {
                                var x1 = ReadNumber(reader["x1"]);
                                var x2 = ReadNumber(reader["x2"]);
                                var y1 = ReadNumber(reader["y1"]);
                                var y2 = ReadNumber(reader["y2"]);
                                using var path = new SKPath();
                                path.MoveTo(x1, y1);
                                path.LineTo(x2, y2);
#if NET9_0_OR_GREATER
                                Path.AddPath(path, in matrix);
#else
                                Path.AddPath(path, ref matrix);
#endif
                            }
                            else if (string.Equals(reader.Name, "circle", StringComparison.Ordinal))
                            {
                                var cx = ReadNumber(reader["cx"]);
                                var cy = ReadNumber(reader["cy"]);
                                var rr = ReadNumber(reader["r"]);
                                using var path = new SKPath();
                                path.AddCircle(cx, cy, rr);
#if NET9_0_OR_GREATER
                                Path.AddPath(path, in matrix);
#else
                                Path.AddPath(path, ref matrix);
#endif
                            }
                            else if (string.Equals(reader.Name, "ellipse", StringComparison.Ordinal))
                            {
                                var cx = ReadNumber(reader["cx"]);
                                var cy = ReadNumber(reader["cy"]);
                                var rx = ReadNumber(reader["rx"]);
                                var ry = ReadNumber(reader["ry"]);
                                using var path = new SKPath();
                                path.AddOval(new SKRect(cx, cy, rx, ry));
#if NET9_0_OR_GREATER
                                Path.AddPath(path, in matrix);
#else
                                Path.AddPath(path, ref matrix);
#endif
                            }
                            else if (string.Equals(reader.Name, "rect", StringComparison.Ordinal))
                            {
                                var x = ReadNumber(reader["x"]);
                                var y = ReadNumber(reader["y"]);
                                var width = ReadNumber(reader["width"]);
                                var height = ReadNumber(reader["height"]);
                                var rx = ReadOptionalNumber(reader["rx"]);
                                var ry = ReadOptionalNumber(reader["ry"]);
                                var rect = SKRect.Create(x, y, width, height);
                                using var path = new SKPath();
                                if (rx != null)
                                {
                                    path.AddRoundRect(rect, rx ?? 0, ry ?? 0);
                                }
                                else
                                {
                                    path.AddRect(rect);
                                }
#if NET9_0_OR_GREATER
                                Path.AddPath(path, in matrix);
#else
                                Path.AddPath(path, ref matrix);
#endif
                            }
                        }
                    }
                }
            }
        }

        private static float ReadNumber(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return 0;

            var s = raw.Trim();
            var m = 1.0f;

            if (unitRe.IsMatch(s))
            {
                if (s.EndsWith("in", StringComparison.Ordinal))
                {
                    m = PixelsPerInch;
                }
                else if (s.EndsWith("cm", StringComparison.Ordinal))
                {
                    m = PixelsPerInch / 2.54f;
                }
                else if (s.EndsWith("mm", StringComparison.Ordinal))
                {
                    m = PixelsPerInch / 25.4f;
                }
                else if (s.EndsWith("pt", StringComparison.Ordinal))
                {
                    m = PixelsPerInch / 72.0f;
                }
                else if (s.EndsWith("pc", StringComparison.Ordinal))
                {
                    m = PixelsPerInch / 6.0f;
                }
                s = s.Substring(0, s.Length - 2);
            }
            else if (percRe.IsMatch(s))
            {
                s = s.Substring(0, s.Length - 1);
                m = 0.01f;
            }

            if (!float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
            {
                v = 0;
            }

            return m * v;
        }

        private static float ReadNumber(string a, float defaultValue) =>
            a == null ? defaultValue : ReadNumber(a);

        private static float? ReadOptionalNumber(string a) =>
            a == null ? (float?)null : ReadNumber(a);

        private static SKRect ReadRectangle(string s)
        {
            var r = new SKRect();
            var p = s.Split(WS, StringSplitOptions.RemoveEmptyEntries);
            if (p.Length > 0)
                r.Left = ReadNumber(p[0]);
            if (p.Length > 1)
                r.Top = ReadNumber(p[1]);
            if (p.Length > 2)
                r.Right = r.Left + ReadNumber(p[2]);
            if (p.Length > 3)
                r.Bottom = r.Top + ReadNumber(p[3]);
            return r;
        }

        private static SKMatrix ReadTransform(string raw)
        {
            var t = SKMatrix.Identity;

            if (string.IsNullOrWhiteSpace(raw))
            {
                return t;
            }

            var calls = raw.Trim().Split(new[] { ')' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var c in calls)
            {
                var args = c.Split(new[] { '(', ',', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var nt = SKMatrix.Identity;
                switch (args[0])
                {
                    case "matrix":
                        if (args.Length == 7)
                        {
                            nt.Values = new float[]
                            {
                                ReadNumber(args[1]), ReadNumber(args[3]), ReadNumber(args[5]),
                                ReadNumber(args[2]), ReadNumber(args[4]), ReadNumber(args[6]),
                                0, 0, 1
                            };
                        }
                        else
                        {
                            Debug.WriteLine($"Matrices are expected to have 6 elements, this one has {args.Length - 1}");
                        }
                        break;
                    case "translate":
                        if (args.Length >= 3)
                        {
                            nt = SKMatrix.CreateTranslation(ReadNumber(args[1]), ReadNumber(args[2]));
                        }
                        else if (args.Length >= 2)
                        {
                            nt = SKMatrix.CreateTranslation(ReadNumber(args[1]), 0);
                        }
                        break;
                    case "scale":
                        if (args.Length >= 3)
                        {
                            nt = SKMatrix.CreateScale(ReadNumber(args[1]), ReadNumber(args[2]));
                        }
                        else if (args.Length >= 2)
                        {
                            var sx = ReadNumber(args[1]);
                            nt = SKMatrix.CreateScale(sx, sx);
                        }
                        break;
                    case "rotate":
                        var a = ReadNumber(args[1]);
                        if (args.Length >= 4)
                        {
                            var x = ReadNumber(args[2]);
                            var y = ReadNumber(args[3]);
                            var t1 = SKMatrix.CreateTranslation(x, y);
                            var t2 = SKMatrix.CreateRotationDegrees(a);
                            var t3 = SKMatrix.CreateTranslation(-x, -y);
#if NET9_0_OR_GREATER
                            SKMatrix.Concat(ref nt, t1, t2);
                            SKMatrix.Concat(ref nt, nt, t3);
#else
                            SKMatrix.Concat(ref nt, ref t1, ref t2);
                            SKMatrix.Concat(ref nt, ref nt, ref t3);
#endif
                        }
                        else
                        {
                            nt = SKMatrix.CreateRotationDegrees(a);
                        }
                        break;
                    default:
                        Debug.WriteLine($"Can't transform {args[0]}");
                        break;
                }
#if NET9_0_OR_GREATER
                SKMatrix.Concat(ref t, t, nt);
#else
                SKMatrix.Concat(ref t, ref t, ref nt);
#endif
            }

            return t;
        }

    }

}

