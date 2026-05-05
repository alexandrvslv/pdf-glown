/*
  Copyright 2007-2015 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfGlown.Documents.Contents.Fonts;
using PdfGlown.Documents.Contents.Layers;
using PdfGlown.Documents.Contents.Objects;
using PdfGlown.Documents.Contents.XObjects;
using PdfGlown.Documents.Interaction.Annotations;
using PdfGlown.Objects;
using PdfGlown.Util.Math;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using PdfGlown.Documents.Contents.ColorSpaces;
using PdfGlown.Documents.Interaction.Actions;

namespace PdfGlown.Documents.Contents.Composition
{
    /// <summary>
    ///   <para>Content stream primitive composer.</para>
    ///   <para>It provides the basic (primitive) operations described by the PDF specification for
    ///   graphics content composition.</para>
    /// </summary>
    /// <remarks>This class leverages the object-oriented content stream modelling infrastructure, which
    /// encompasses 1st-level content stream objects (operations), 2nd-level content stream objects
    /// (graphics objects) and full graphics state support.</remarks>
    public sealed class PrimitiveComposer
    {
        private const float QuadToQubicKoefficent = 2.0F / 3.0F;
        private ContentScanner scanner;
        private ICompositeObject current;
        private int index;
        private Stack<(ICompositeObject Object, int Index)> stack = new();

        public PrimitiveComposer(ContentScanner scanner)
        {
            this.scanner = scanner;
            scanner.OnObjectScanning += OnObjectStarting;
            scanner.OnObjectScanned += OnObjectFinish;
            current = scanner.Contents;
        }

        public PrimitiveComposer(IContentContext context)
            : this(new ContentScanner(context))
        { }

        /// <summary>Gets/Sets the content stream scanner.</summary>
        public ContentScanner Scanner
        {
            get => scanner;
        }

        /// <summary>Gets the current graphics state [PDF:1.6:4.3].</summary>
        public GraphicsState State => Scanner.State;

        public ICompositeObject Current
        {
            get => current;
        }

        public int Index
        {
            get => index;
            set => index = value;
        }

        private bool OnObjectStarting(ContentObject obj, ICompositeObject container, int index)
        {
            if (container != null)
            {
                if (container == current)
                {
                    this.index = index;
                }
#if DEBUG
                else { Debug.WriteLine("Unexpected scan container"); }
#endif
            }
            if (obj is ICompositeObject objComposite)
            {
                Push(objComposite);
            }
            return true;
        }

        private void OnObjectFinish(ContentObject obj)
        {
            if (obj is ICompositeObject container)
            {
                if (container == current)
                {
                    Pop();
                }
#if DEBUG
                else { Debug.WriteLine("Unexpected scan container"); }
#endif
            }
            this.index += 1;
        }

        private bool Push(ICompositeObject contaner)
        {
            if (current == contaner)
                return false;
            stack.Push((current, index));
            // The new object's children level is the new current level!
            current = contaner;
            index = -1;
            return true;
        }

        private bool Pop()
        {
            if (stack.TryPop(out var result))
            {
                current = result.Object;
                index = result.Index;
                return true;
            }
            return false;
        }

        /// <summary>Adds a content object.</summary>
        /// <returns>The added content object.</returns>
        public T Add<T>(T obj) where T : ContentObject
        {
            Insert(obj);
            obj.Scan(scanner.State, current, index);
            return obj;
        }

        private void Insert<T>(T obj) where T : ContentObject
        {
            if (index < 0)
            { index = 0; }
            current.Contents.Insert(index, obj);
        }

        /// <summary>Adds a composite object beginning it.</summary>
        /// <returns>Added composite object.</returns>
        /// <seealso cref="End()"/>
        public T Begin<T>(T obj)
            where T : CompositeObject
        {
            // Insert the new object at the current level!
            Insert(obj);
            OnObjectStarting(obj, current, index);
            obj.OnScanning(scanner.State);
            return obj;
        }

        /// <summary>Ends the current (innermostly-nested) composite object.</summary>
        /// <seealso cref="Begin(CompositeObject)"/>
        public void End()
        {
            if (current is ContentObject content)
            {
                OnObjectFinish(content);
                if (content is CompositeObject container)
                {
                    container.OnScanned(scanner.State);
                }
            }
        }

        /// <summary>Applies a transformation to the coordinate system from user space to device space
        /// [PDF:1.6:4.3.3].</summary>
        /// <remarks>The transformation is applied to the current transformation matrix (CTM) by
        /// concatenation, i.e. it doesn't replace it.</remarks>
        /// <param name="a">Item 0,0 of the matrix.</param>
        /// <param name="b">Item 0,1 of the matrix.</param>
        /// <param name="c">Item 1,0 of the matrix.</param>
        /// <param name="d">Item 1,1 of the matrix.</param>
        /// <param name="e">Item 2,0 of the matrix.</param>
        /// <param name="f">Item 2,1 of the matrix.</param>
        /// <seealso cref="SetMatrix(double,double,double,double,double,double)"/>
        public void ApplyMatrix(double a, double b, double c, double d, double e, double f) => Add(new ModifyCTM(a, b, c, d, e, f));

        public void ApplyMatrix(SKMatrix matrix) => Add(new ModifyCTM(matrix));

        /// <summary>Applies the specified state parameters [PDF:1.6:4.3.4].</summary>
        /// <param name="name">Resource identifier of the state parameters object.</param>
        public void ApplyState(PdfName name)
        {
            // Doesn't the state exist in the context resources?
            if (!Scanner.Context.Resources.ExtGStates.ContainsKey(name))
                throw new ArgumentException("No state resource associated to the given argument.", "name");

            ApplyState_(name);
        }

        /// <summary>Applies the specified state parameters [PDF:1.6:4.3.4].</summary>
        /// <remarks>The <code>value</code> is checked for presence in the current resource dictionary: if
        /// it isn't available, it's automatically added. If you need to avoid such a behavior, use
        /// <see cref="ApplyState(PdfName)"/>.</remarks>
        /// <param name="state">State parameters object.</param>
        public void ApplyState(ExtGState state) => ApplyState_(GetResourceName(state));

        /// <summary>Begins a new layered-content sequence [PDF:1.6:4.10.2].</summary>
        /// <param name="layer">Layer entity enclosing the layered content.</param>
        /// <returns>Added layered-content sequence.</returns>
        /// <seealso cref="End()"/>
        public GraphicsMarkedContent BeginLayer(LayerEntity layer) => BeginLayer(GetResourceName(layer.Membership));

        /// <summary>Begins a new layered-content sequence [PDF:1.6:4.10.2].</summary>
        /// <param name="layerName">Resource identifier of the {@link LayerEntity} enclosing the layered
        /// content.</param>
        /// <returns>Added layered-content sequence.</returns>
        /// <seealso cref="End()"/>
        public GraphicsMarkedContent BeginLayer(PdfName layerName) => BeginMarkedContent(PdfName.OC, layerName);

        /// <summary>Begins a new nested graphics state context [PDF:1.6:4.3.1].</summary>
        /// <returns>Added local graphics state object.</returns>
        /// <seealso cref="End()"/>
        public GraphicsLocalState BeginLocalState() => Begin(new GraphicsLocalState());

        /// <summary>Begins a new marked-content sequence [PDF:1.6:10.5].</summary>
        /// <param name="tag">Marker indicating the role or significance of the marked content.</param>
        /// <returns>Added marked-content sequence.</returns>
        /// <seealso cref="End()"/>
        public GraphicsMarkedContent BeginMarkedContent(PdfName tag) => BeginMarkedContent(tag, (PdfName)null);

        /// <summary>Begins a new marked-content sequence [PDF:1.6:10.5].</summary>
        /// <param name="tag">Marker indicating the role or significance of the marked content.</param>
        /// <param name="propertyList"><see cref="PropertyList"/> describing the marked content.</param>
        /// <returns>Added marked-content sequence.</returns>
        /// <seealso cref="End()"/>
        public GraphicsMarkedContent BeginMarkedContent(PdfName tag, PropertyList propertyList) => BeginMarkedContent_(tag, GetResourceName(propertyList));

        /// <summary>Begins a new marked-content sequence [PDF:1.6:10.5].</summary>
        /// <param name="tag">Marker indicating the role or significance of the marked content.</param>
        /// <param name="propertyListName">Resource identifier of the <see cref="PropertyList"/> describing
        /// the marked content.</param>
        /// <returns>Added marked-content sequence.</returns>
        /// <seealso cref="End()"/>
        public GraphicsMarkedContent BeginMarkedContent(PdfName tag, PdfName propertyListName)
        {
            // Doesn't the property list exist in the context resources?
            if (propertyListName != null && !Scanner.Context.Resources.PropertyLists.ContainsKey(propertyListName))
                throw new ArgumentException("No property list resource associated to the given argument.", "name");

            return BeginMarkedContent_(tag, propertyListName);
        }

        /// <summary>Modifies the current clipping path by intersecting it with the current path
        /// [PDF:1.6:4.4.1].</summary>
        /// <remarks>It can be validly called only just before painting the current path.</remarks>
        public void Clip()
        {
            Add(ModifyClipPath.NonZero);
            Add(PaintPath.EndNoOp);
        }

        /// <summary>Closes the current subpath by appending a straight line segment from the current point
        /// to the starting point of the subpath [PDF:1.6:4.4.1].</summary>
        public void ClosePath() => Add(CloseSubpath.Value);

        /// <summary>Draws a circular arc.</summary>
        /// <param name="location">Arc location.</param>
        /// <param name="startAngle">Starting angle.</param>
        /// <param name="endAngle">Ending angle.</param>
        /// <seealso cref="Stroke()"/>
        public void DrawArc(SKRect location, double startAngle, double endAngle) => DrawArc(location, startAngle, endAngle, 0, 1);

        /// <summary>Draws an arc.</summary>
        /// <param name="location">Arc location.</param>
        /// <param name="startAngle">Starting angle.</param>
        /// <param name="endAngle">Ending angle.</param>
        /// <param name="branchWidth">Distance between the spiral branches. '0' value degrades to a circular
        /// arc.</param>
        /// <param name="branchRatio">Linear coefficient applied to the branch width. '1' value degrades to
        /// a constant branch width.</param>
        /// <seealso cref="Stroke()"/>
        public void DrawArc(SKRect location, double startAngle, double endAngle, double branchWidth, double branchRatio)
            => DrawArc(location, startAngle, endAngle, branchWidth, branchRatio, true);

        /// <summary>Draws a cubic Bezier curve from the current point [PDF:1.6:4.4.1].</summary>
        /// <param name="endPoint">Ending point.</param>
        /// <param name="startControl">Starting control point.</param>
        /// <param name="endControl">Ending control point.</param>
        /// <seealso cref="Stroke()"/>
        public void DrawCurve(SKPoint endPoint, SKPoint startControl, SKPoint endControl)
        {
            double contextHeight = Scanner.ContextBox.Height;
            Add(new DrawFullCurve(endPoint.X, contextHeight - endPoint.Y,
                startControl.X, contextHeight - startControl.Y,
                endControl.X, contextHeight - endControl.Y));
        }

        /// <summary>Draws a cubic Bezier curve [PDF:1.6:4.4.1].</summary>
        /// <param name="startPoint">Starting point.</param>
        /// <param name="endPoint">Ending point.</param>
        /// <param name="startControl">Starting control point.</param>
        /// <param name="endControl">Ending control point.</param>
        /// <seealso cref="Stroke()"/>
        public void DrawCurve(SKPoint startPoint, SKPoint endPoint, SKPoint startControl, SKPoint endControl)
        {
            StartPath(startPoint);
            DrawCurve(endPoint, startControl, endControl);
        }

        /// <summary>Draws an ellipse.</summary>
        /// <param name="location">Ellipse location.</param>
        /// <seealso cref="Fill()"/>
        /// <seealso cref="FillStroke()"/>
        /// <seealso cref="Stroke()"/>
        public void DrawEllipse(SKRect location) => DrawArc(location, 0, 360);

        /// <summary>Draws an circle by square Ellipce.</summary>
        /// <param name="center">Circle center.</param>
        /// <param name="radius">Circle radius</param>
        /// <seealso cref="Fill()"/>
        /// <seealso cref="FillStroke()"/>
        /// <seealso cref="Stroke()"/>
        public void DrawCircle(SKPoint center, float radius) => DrawEllipse(new SKRect(center.X - radius, center.Y - radius, center.X + radius, center.Y + radius));

        /// <summary>Draws a line from the current point [PDF:1.6:4.4.1].</summary>
        /// <param name="endPoint">Ending point.</param>
        /// <seealso cref="Stroke()"/>
        public void DrawLine(SKPoint endPoint) => Add(new DrawLine(endPoint.X, Scanner.ContextBox.Height - endPoint.Y));

        /// <summary>Draws a line [PDF:1.6:4.4.1].</summary>
        /// <param name="startPoint">Starting point.</param>
        /// <param name="endPoint">Ending point.</param>
        /// <seealso cref="Stroke()"/>
        public void DrawLine(SKPoint startPoint, SKPoint endPoint)
        {
            StartPath(startPoint);
            DrawLine(endPoint);
        }

        /// <summary>Draws a polygon.</summary>
        /// <remarks>A polygon is the same as a multiple line except that it's a closed path.</remarks>
        /// <param name="points">Points.</param>
        /// <seealso cref="Fill()"/>
        /// <seealso cref="FillStroke()"/>
        /// <seealso cref="Stroke()"/>
        public void DrawPolygon(SKPoint[] points)
        {
            DrawPolyline(points);
            ClosePath();
        }

        /// <summary>Draws a path.</summary>
        /// <remarks>Iterate path</remarks>
        /// <param name="path">SKPath.</param>
        /// <seealso cref="Fill()"/>
        /// <seealso cref="FillStroke()"/>
        /// <seealso cref="Stroke()"/>
        public void DrawPath(SKPath path)
        {
            //path.ConicTo(point1,)
            var iterator = path.CreateRawIterator();
            Span<SKPoint> points = stackalloc SKPoint[4];
            SKPathVerb verb;
            while ((verb = iterator.Next(points)) != SKPathVerb.Done)
            {
                switch (verb)
                {
                    case SKPathVerb.Move:
                        StartPath(points[0]);
                        break;
                    case SKPathVerb.Line:
                        DrawLine(points[1]);
                        break;
                    case SKPathVerb.Quad:
                        DrawQuad(points[0], points[1], points[2]);
                        break;
                    case SKPathVerb.Conic:
                        var quadPoints = SKPath.ConvertConicToQuads(points[0], points[1], points[2], iterator.ConicWeight(), 1);
                        DrawQuad(quadPoints[0], quadPoints[1], quadPoints[2]);
                        DrawQuad(quadPoints[2], quadPoints[3], quadPoints[4]);
                        // DrawQuad(quadPoints[6], quadPoints[7], quadPoints[8]);
                        break;
                    case SKPathVerb.Cubic:
                        DrawCurve(points[3], points[1], points[2]);
                        break;
                    case SKPathVerb.Close:
                        ClosePath();
                        break;
                    case SKPathVerb.Done:
                        break;
                }
            }
        }

        public void DrawQuad(SKPoint qp0, SKPoint qp1, SKPoint qp2)
        {
            var controlPoint1 = qp0 + (qp1 - qp0).Multiply(QuadToQubicKoefficent);
            var controlPoint2 = qp2 + (qp1 - qp2).Multiply(QuadToQubicKoefficent);
            DrawCurve(qp2, controlPoint1, controlPoint2);
        }

        /// <summary>Draws a multiple line.</summary>
        /// <param name="points">Points.</param>
        /// <seealso cref="Stroke()"/>
        public void DrawPolyline(SKPoint[] points)
        {
            StartPath(points[0]);
            for (int index = 1, length = points.Length; index < length; index++)
            {
                DrawLine(points[index]);
            }
        }

        /// <summary>Draws a rectangle [PDF:1.6:4.4.1].</summary>
        /// <param name="location">Rectangle location.</param>
        /// <seealso cref="Fill()"/>
        /// <seealso cref="FillStroke()"/>
        /// <seealso cref="Stroke()"/>
        public void DrawRectangle(SKRect location) => DrawRectangle(location, 0);

        public void DrawQuad(SKPoint location, SKPoint vector)
        {
            var leftMiddle = location - vector;
            var topLeft = leftMiddle + leftMiddle.PerpendicularClockwise();
            var bottomLeft = leftMiddle + leftMiddle.PerpendicularCounterClockwise();
            var rightMiddle = location + vector;
            var topRight = rightMiddle + rightMiddle.PerpendicularCounterClockwise();
            var bottomRight = rightMiddle + rightMiddle.PerpendicularClockwise();
            var quad = new Quad(topLeft, topRight, bottomRight, bottomLeft);
            DrawQuad(quad);
        }

        private void DrawQuad(Quad quad)
        {
            StartPath(quad.Point0);
            DrawLine(quad.Point1);
            DrawLine(quad.Point2);
            DrawLine(quad.Point3);
            ClosePath();
        }

        /// <summary>Draws a rounded rectangle.</summary>
        /// <param name="location">Rectangle location.</param>
        /// <param name="radius">Vertex radius, '0' value degrades to squared vertices.</param>
        /// <seealso cref="Fill()"/>
        /// <seealso cref="FillStroke()"/>
        /// <seealso cref="Stroke()"/>
        public void DrawRectangle(SKRect location, double radius)
        {
            if (radius == 0)
            {
                Add(new DrawRectangle(location.Left, Scanner.ContextBox.Height - location.Top - location.Height, location.Width, location.Height));
            }
            else
            {
                double endRadians = Math.PI * 2;
                double quadrantRadians = Math.PI / 2;
                double radians = 0;
                while (radians < endRadians)
                {
                    double radians2 = radians + quadrantRadians;
                    int sin2 = (int)Math.Sin(radians2);
                    int cos2 = (int)Math.Cos(radians2);
                    double x1 = 0, x2 = 0, y1 = 0, y2 = 0;
                    double xArc = 0, yArc = 0;
                    if (cos2 == 0)
                    {
                        if (sin2 == 1)
                        {
                            x1 = x2 = location.Left + location.Width;
                            y1 = location.Top + location.Height - radius;
                            y2 = location.Top + radius;

                            xArc = -radius * 2;
                            yArc = -radius;

                            StartPath(new SKPoint((float)x1, (float)y1));
                        }
                        else
                        {
                            x1 = x2 = location.Left;
                            y1 = location.Top + radius;
                            y2 = location.Top + location.Height - radius;

                            yArc = -radius;
                        }
                    }
                    else if (cos2 == 1)
                    {
                        x1 = location.Left + radius;
                        x2 = location.Left + location.Width - radius;
                        y1 = y2 = location.Top + location.Height;

                        xArc = -radius;
                        yArc = -radius * 2;
                    }
                    else if (cos2 == -1)
                    {
                        x1 = location.Left + location.Width - radius;
                        x2 = location.Left + radius;
                        y1 = y2 = location.Top;

                        xArc = -radius;
                    }
                    DrawLine(new SKPoint((float)x2, (float)y2));
                    DrawArc(
                      SKRect.Create((float)(x2 + xArc), (float)(y2 + yArc), (float)(radius * 2), (float)(radius * 2)),
                      (float)MathUtils.ToDegrees(radians),
                      (float)MathUtils.ToDegrees(radians2),
                      0,
                      1,
                      false);

                    radians = radians2;
                }
            }
        }

        /// <summary>Draws a spiral.</summary>
        /// <param name="center">Spiral center.</param>
        /// <param name="startAngle">Starting angle.</param>
        /// <param name="endAngle">Ending angle.</param>
        /// <param name="branchWidth">Distance between the spiral branches.</param>
        /// <param name="branchRatio">Linear coefficient applied to the branch width.</param>
        /// <seealso cref="Stroke()"/>
        public void DrawSpiral(SKPoint center, double startAngle, double endAngle, double branchWidth, double branchRatio)
            => DrawArc(SKRect.Create(center.X, center.Y, 0.0001f, 0.0001f), startAngle, endAngle, branchWidth, branchRatio);

        /// <summary>Fills the path using the current color [PDF:1.6:4.4.2].</summary>
        /// <seealso cref="SetFillColor(Color)"/>
        public void Fill() => Add(PaintPath.Fill);

        /// <summary>Fills and then strokes the path using the current colors [PDF:1.6:4.4.2].</summary>
        /// <seealso cref="SetFillColor(Color)"/>
        /// <seealso cref="SetStrokeColor(Color)"/>
        public void FillStroke() => Add(PaintPath.FillStroke);

        /// <summary>Serializes the contents into the content stream.</summary>
        public void Flush() => Scanner.Contents.Flush();

        /// <summary>Applies a rotation to the coordinate system from user space to device space
        /// [PDF:1.6:4.2.2].</summary>
        /// <param name="angle">Rotational counterclockwise angle.</param>
        /// <seealso cref="ApplyMatrix(double,double,double,double,double,double)"/>
        public void Rotate(double angle)
        {
            double rad = MathUtils.ToRadians(angle);
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);
            ApplyMatrix(cos, sin, -sin, cos, 0, 0);
        }

        /// <summary>Applies a rotation to the coordinate system from user space to device space
        /// [PDF:1.6:4.2.2].</summary>
        /// <param name="angle">Rotational counterclockwise angle.</param>
        /// <param name="origin">Rotational pivot point; it becomes the new coordinates origin.</param>
        /// <seealso cref="ApplyMatrix(double,double,double,double,double,double)"/>
        public void Rotate(double angle, SKPoint origin)
        {
            // Center to the new origin!
            Translate(origin.X, Scanner.ContextBox.Height - origin.Y);
            // Rotate on the new origin!
            Rotate(angle);
            // Restore the standard vertical coordinates system!
            Translate(0, -Scanner.ContextBox.Height);
        }

        /// <summary>Applies a scaling to the coordinate system from user space to device space
        /// [PDF:1.6:4.2.2].</summary>
        /// <param name="ratioX">Horizontal scaling ratio.</param>
        /// <param name="ratioY">Vertical scaling ratio.</param>
        /// <seealso cref="ApplyMatrix(double,double,double,double,double,double)"/>
        public void Scale(double ratioX, double ratioY) => ApplyMatrix(ratioX, 0, 0, ratioY, 0, 0);

        /// <summary>Sets the character spacing parameter [PDF:1.6:5.2.1].</summary>
        public void SetCharSpace(double value) => Add(new SetCharSpace(value));

        /// <summary>Sets the nonstroking color value [PDF:1.6:4.5.7].</summary>
        /// <seealso cref="SetStrokeColor(Color)"/>
        public void SetFillColor(IColor value)
        {
            if (!State.FillColorSpace.IsSpaceColor(value))
            {
                // Set filling color space!
                Add(new SetFillColorSpace(GetResourceName(value.ColorSpace)));
            }

            Add(new SetFillColorExtended(value));
        }

        /// <summary>Sets the font [PDF:1.6:5.2].</summary>
        /// <param name="name">Resource identifier of the font.</param>
        /// <param name="size">Scaling factor (points).</param>
        public void SetFont(PdfName name, double size)
        {
            // Doesn't the font exist in the context resources?
            if (!Scanner.Context.Resources.Fonts.ContainsKey(name))
            {
                var mapping = FontMappers.Instance.GetTrueTypeFont(name.StringValue, null);
                if (mapping?.Font != null)
                {
                    var fallbackFont = PdfType0Font.Load(Scanner.Context.Resources.Document, mapping.Font, false);
                    SetFont(fallbackFont, size, name);
                    return;
                }
                throw new ArgumentException("No font resource associated to the given argument.", "name");
            }
            SetFont_(name, size);
        }

        /// <summary>Sets the font [PDF:1.6:5.2].</summary>
        /// <remarks>The <paramref cref="value"/> is checked for presence in the current resource
        /// dictionary: if it isn't available, it's automatically added. If you need to avoid such a
        /// behavior, use <see cref="SetFont(PdfName,double)"/>.</remarks>
        /// <param name="value">Font.</param>
        /// <param name="size">Scaling factor (points).</param>
        public void SetFont(PdfFont value, double size, PdfName defaultName = null) => SetFont_(GetResourceName(value, defaultName), size);

        /// <summary>Sets the line cap style [PDF:1.6:4.3.2].</summary>
        public void SetLineCap(LineCapEnum value) => Add(new SetLineCap(value));

        /// <summary>Sets the line dash pattern [PDF:1.6:4.3.2].</summary>
        public void SetLineDash(LineDash value) => Add(new SetLineDash(value));

        /// <summary>Sets the line join style [PDF:1.6:4.3.2].</summary>
        public void SetLineJoin(LineJoinEnum value) => Add(new SetLineJoin(value));

        /// <summary>Sets the line width [PDF:1.6:4.3.2].</summary>
        public void SetLineWidth(double value) => Add(new SetLineWidth(value));

        /// <summary>Sets the transformation of the coordinate system from user space to device space
        /// [PDF:1.6:4.3.3].</summary>
        /// <param name="a">Item 0,0 of the matrix.</param>
        /// <param name="b">Item 0,1 of the matrix.</param>
        /// <param name="c">Item 1,0 of the matrix.</param>
        /// <param name="d">Item 1,1 of the matrix.</param>
        /// <param name="e">Item 2,0 of the matrix.</param>
        /// <param name="f">Item 2,1 of the matrix.</param>
        /// <seealso cref="ApplyMatrix(double,double,double,double,double,double)"/>
        public void SetMatrix(double a, double b, double c, double d, double e, double f)
        {
            // Reset the CTM!
            Add(ModifyCTM.GetResetCTM(State));
            // Apply the transformation!
            Add(new ModifyCTM(a, b, c, d, e, f));
        }

        public void SetMatrix(SKMatrix matrix)
        {
            // Reset the CTM!
            Add(ModifyCTM.GetResetCTM(State));
            // Apply the transformation!
            Add(new ModifyCTM(matrix));
        }

        /// <summary>Sets the miter limit [PDF:1.6:4.3.2].</summary>
        public void SetMiterLimit(double value) => Add(new SetMiterLimit(value));

        /// <summary>Sets the stroking color value [PDF:1.6:4.5.7].</summary>
        /// <seealso cref="SetFillColor(Color)"/>
        public void SetStrokeColor(IColor value)
        {
            if (!State.StrokeColorSpace.IsSpaceColor(value))
            {
                // Set stroking color space!
                Add(new SetStrokeColorSpace(GetResourceName(value.ColorSpace)));
            }

            Add(new SetStrokeColorExtended(value));
        }

        /// <summary>Sets the text leading [PDF:1.6:5.2.4], relative to the current text line height.
        /// </summary>
        public void SetTextLead(double value) => Add(new SetTextLead(value * State.Font.GetLineHeight(State.FontSize)));

        /// <summary>Sets the text rendering mode [PDF:1.6:5.2.5].</summary>
        public void SetTextRenderMode(TextRenderModeEnum value) => Add(new SetTextRenderMode(value));

        /// <summary>Sets the text rise [PDF:1.6:5.2.6].</summary>
        public void SetTextRise(double value) => Add(new SetTextRise(value));

        /// <summary>Sets the text horizontal scaling [PDF:1.6:5.2.3], normalized to 1.</summary>
        public void SetTextScale(double value) => Add(new SetTextScale(value * 100));

        /// <summary>Sets the word spacing [PDF:1.6:5.2.2].</summary>
        public void SetWordSpace(double value) => Add(new SetWordSpace(value));

        /// <summary>Shows the specified text on the page at the current location [PDF:1.6:5.3.2].</summary>
        /// <param name="value">Text to show.</param>
        /// <returns>Bounding box vertices in default user space units.</returns>
        /// <exception cref="EncodeException"/>
        public Quad ShowText(string value) => ShowText(value, new SKPoint(0, 0));

        /// <summary>Shows the link associated to the specified text on the page at the current location.
        /// </summary>
        /// <param name="value">Text to show.</param>
        /// <param name="action">Action to apply when the link is activated.</param>
        /// <returns>Link.</returns>
        /// <exception cref="EncodeException"/>
        public Link ShowText(string value, PdfAction action) => ShowText(value, new SKPoint(0, 0), action);

        /// <summary>Shows the specified text on the page at the specified location [PDF:1.6:5.3.2].
        /// </summary>
        /// <param name="value">Text to show.</param>
        /// <param name="location">Position at which showing the text.</param>
        /// <returns>Bounding box vertices in default user space units.</returns>
        /// <exception cref="EncodeException"/>
        public Quad ShowText(string value, SKPoint location) => ShowText(value, location, XAlignmentEnum.Left, YAlignmentEnum.Top, 0);

        /// <summary>Shows the link associated to the specified text on the page at the specified location.
        /// </summary>
        /// <param name="value">Text to show.</param>
        /// <param name="location">Position at which showing the text.</param>
        /// <param name="action">Action to apply when the link is activated.</param>
        /// <returns>Link.</returns>
        /// <exception cref="EncodeException"/>
        public Link ShowText(string value, SKPoint location, PdfAction action) => ShowText(value, location, XAlignmentEnum.Left, YAlignmentEnum.Top, 0, action);

        /// <summary>Shows the specified text on the page at the specified location [PDF:1.6:5.3.2].
        /// </summary>
        /// <param name="value">Text to show.</param>
        /// <param name="location">Anchor position at which showing the text.</param>
        /// <param name="xAlignment">Horizontal alignment.</param>
        /// <param name="yAlignment">Vertical alignment.</param>
        /// <param name="rotation">Rotational counterclockwise angle.</param>
        /// <returns>Bounding box vertices in default user space units.</returns>
        /// <exception cref="EncodeException"/>
        public Quad ShowText(string value, SKPoint location, XAlignmentEnum xAlignment, YAlignmentEnum yAlignment, double rotation)
        {
            Quad frame = Quad.Empty;

            BeginLocalState();
            try
            {
                // Anchor point positioning.
                double rad = MathUtils.ToRadians(rotation);
                double cos = Math.Cos(rad);
                double sin = Math.Sin(rad);
                ApplyMatrix(cos, sin, -sin, cos, location.X, Scanner.ContextBox.Height - location.Y);

                string[] textLines = value.Split('\n');

                var state = State;
                var font = state.Font;
                double fontSize = state.FontSize;
                double lineHeight = font.GetLineHeight(fontSize);
                double lineSpace = state.Lead < lineHeight ? 0 : state.Lead - lineHeight;
                lineHeight += lineSpace;
                double textHeight = lineHeight * textLines.Length - lineSpace;
                double ascent = font.GetAscent(fontSize);

                // NOTE: Word spacing is automatically applied by viewers only in case of single-byte
                // character code 32 [PDF:1.7:5.2.2]. As several bug reports pointed out, mixed-length
                // encodings aren't properly handled by recent implementations like pdf.js, therefore
                // composite fonts are always treated as multi-byte encodings which require explicit word
                // spacing adjustment.
                double wordSpaceAdjust = font is PdfType0Font ? -state.WordSpace * 1000 * state.Scale / fontSize : 0;

                // Vertical alignment.
                double y;
                switch (yAlignment)
                {
                    case YAlignmentEnum.Top:
                        y = 0 - ascent;
                        break;
                    case YAlignmentEnum.Bottom:
                        y = textHeight - ascent;
                        break;
                    case YAlignmentEnum.Middle:
                        y = textHeight / 2 - ascent;
                        break;
                    default:
                        throw new NotImplementedException();
                }

                // Text placement.
                BeginText();
                try
                {
                    for (int index = 0, length = textLines.Length; index < length; index++)
                    {
                        string textLine = textLines[index];
                        double width = font.GetWidth(textLine, fontSize) * state.Scale; //GetKernedWidth

                        // Horizontal alignment.
                        double x;
                        switch (xAlignment)
                        {
                            case XAlignmentEnum.Left:
                                x = 0;
                                break;
                            case XAlignmentEnum.Right:
                                x = -width;
                                break;
                            case XAlignmentEnum.Center:
                            case XAlignmentEnum.Justify:
                                x = -width / 2;
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                        TranslateText(x, y - lineHeight * index);

                        var showText = (ShowText)null;
                        if (textLine.Length > 0)
                        {
                            if (wordSpaceAdjust == 0 || textLine.IndexOf(' ') == -1) // Simple text.
                            { showText = Add(new ShowSimpleText(font.Encode(textLine))); }
                            else // Adjusted text.
                            {
                                var textParams = new List<PdfDirectObject>();
                                for (int spaceIndex = 0, lastSpaceIndex = -1; spaceIndex > -1; lastSpaceIndex = spaceIndex)
                                {
                                    spaceIndex = textLine.IndexOf(' ', lastSpaceIndex + 1);
                                    // Word space adjustment.
                                    if (lastSpaceIndex > -1)
                                    { textParams.Add(PdfReal.Get(wordSpaceAdjust)); }
                                    // Word.
                                    var l = (spaceIndex > -1 ? spaceIndex + 1 : textLine.Length) - (lastSpaceIndex + 1);
                                    if (l > 0)
                                    {
                                        textParams.Add(new PdfByteString(font.Encode(textLine.AsSpan(lastSpaceIndex + 1, l))));
                                    }
                                }
                                showText = Add(new ShowAdjustedText(textParams));
                            }
                            frame = frame.IsEmpty ? showText.Quad : Quad.Union(frame, showText.Quad);
                        }
                    }
                }
                finally
                { End(); } // Ends the text object.                
            }
            finally
            { End(); } // Ends the local state.

            return frame;
        }

        /// <summary>Shows the link associated to the specified text on the page at the specified location.
        /// </summary>
        /// <param name="value">Text to show.</param>
        /// <param name="location">Anchor position at which showing the text.</param>
        /// <param name="xAlignment">Horizontal alignment.</param>
        /// <param name="yAlignment">Vertical alignment.</param>
        /// <param name="rotation">Rotational counterclockwise angle.</param>
        /// <param name="action">Action to apply when the link is activated.</param>
        /// <returns>Link.</returns>
        /// <exception cref="EncodeException"/>
        public Link ShowText(string value, SKPoint location, XAlignmentEnum xAlignment, YAlignmentEnum yAlignment, double rotation, PdfAction action)
        {
            IContentContext contentContext = Scanner.Context;
            if (contentContext is not PdfPage page)
                throw new Exception("Links can be shown only on page contexts.");

            SKRect linkBox = ShowText(value, location, xAlignment, yAlignment, rotation).GetBounds();

            var link = new Link(page, linkBox, null, action)
            { Layer = GetLayer() };
            page.Annotations.Add(link);
            return link;
        }

        /// <summary>Shows the specified external object [PDF:1.6:4.7].</summary>
        /// <param name="name">Resource identifier of the external object.</param>
        public void ShowXObject(PdfName name) => Add(new PaintXObject(name));

        /// <summary>Shows the specified external object [PDF:1.6:4.7].</summary>
        /// <remarks>The <paramref cref="value"/> is checked for presence in the current resource
        /// dictionary: if it isn't available, it's automatically added. If you need to avoid such a
        /// behavior, use <see cref="ShowXObject(PdfName)"/>.</remarks>
        /// <param name="value">External object.</param>
        public void ShowXObject(XObject value) => ShowXObject(GetResourceName(value));

        /// <summary>Shows the specified external object at the specified position [PDF:1.6:4.7].</summary>
        /// <param name="name">Resource identifier of the external object.</param>
        /// <param name="location">Position at which showing the external object.</param>
        public void ShowXObject(PdfName name, SKPoint location) => ShowXObject(name, location, null);

        /// <summary>Shows the specified external object at the specified position [PDF:1.6:4.7].</summary>
        /// <remarks>The <paramref cref="value"/> is checked for presence in the current resource
        /// dictionary: if it isn't available, it's automatically added. If you need to avoid such a
        /// behavior, use <see cref="ShowXObject(PdfName,SKPoint)"/>.</remarks>
        /// <param name="value">External object.</param>
        /// <param name="location">Position at which showing the external object.</param>
        public void ShowXObject(XObject value, SKPoint location) => ShowXObject(GetResourceName(value), location);

        /// <summary>Shows the specified external object at the specified position [PDF:1.6:4.7].</summary>
        /// <param name="name">Resource identifier of the external object.</param>
        /// <param name="location">Position at which showing the external object.</param>
        /// <param name="size">Size of the external object.</param>
        public void ShowXObject(PdfName name, SKPoint location, SKSize? size) => ShowXObject(name, location, size, XAlignmentEnum.Left, YAlignmentEnum.Top, 0);

        /// <summary>Shows the specified external object at the specified position [PDF:1.6:4.7].</summary>
        /// <remarks>The <paramref cref="value"/> is checked for presence in the current resource
        /// dictionary: if it isn't available, it's automatically added. If you need to avoid such a
        /// behavior, use <see cref="ShowXObject(PdfName,SKPoint,SKSize)"/>.</remarks>
        /// <param name="value">External object.</param>
        /// <param name="location">Position at which showing the external object.</param>
        /// <param name="size">Size of the external object.</param>
        public void ShowXObject(XObject value, SKPoint location, SKSize? size) => ShowXObject(GetResourceName(value), location, size);

        /// <summary>Shows the specified external object at the specified position [PDF:1.6:4.7].</summary>
        /// <param name="name">Resource identifier of the external object.</param>
        /// <param name="location">Position at which showing the external object.</param>
        /// <param name="size">Size of the external object.</param>
        /// <param name="xAlignment">Horizontal alignment.</param>
        /// <param name="yAlignment">Vertical alignment.</param>
        /// <param name="rotation">Rotational counterclockwise angle.</param>
        public void ShowXObject(PdfName name, SKPoint location, SKSize? size, XAlignmentEnum xAlignment, YAlignmentEnum yAlignment, double rotation)
        {
            var xObject = Scanner.Context.Resources.XObjects[name];
            var xObjectSize = xObject.Size;

            if (!size.HasValue)
            { size = xObjectSize; }

            // Scaling.
            var matrix = xObject.Matrix;// SKMatrix.MakeScale(1f / xObjectSize.Width, 1f / xObjectSize.Height);
            double scaleX, scaleY;
            scaleX = size.Value.Width / (xObjectSize.Width * matrix.ScaleX);
            scaleY = size.Value.Height / (xObjectSize.Height * matrix.ScaleY);
            // Alignment.
            var locationOffsetX = xAlignment switch
            {
                XAlignmentEnum.Left => 0,
                XAlignmentEnum.Right => size.Value.Width,
                _ => size.Value.Width / 2,
            };
            var locationOffsetY = yAlignment switch
            {
                YAlignmentEnum.Top => size.Value.Height,
                YAlignmentEnum.Bottom => 0,
                _ => size.Value.Height / 2,
            };
            BeginLocalState();
            try
            {
                Translate(location.X, Scanner.ContextBox.Height - location.Y);
                if (rotation != 0)
                { Rotate(rotation); }
                ApplyMatrix(
                  scaleX, 0, 0,
                  scaleY,
                  -locationOffsetX,
                  -locationOffsetY);
                ShowXObject(name);
            }
            finally
            { End(); } // Ends the local state.
        }

        /// <summary>Shows the specified external object at the specified position [PDF:1.6:4.7].</summary>
        /// <remarks>The <paramref cref="value"/> is checked for presence in the current resource
        /// dictionary: if it isn't available, it's automatically added. If you need to avoid such a
        /// behavior, use <see cref="ShowXObject(PdfName,SKPoint,SKSize,XAlignmentEnum,YAlignmentEnum,double)"/>.
        /// </remarks>
        /// <param name="value">External object.</param>
        /// <param name="location">Position at which showing the external object.</param>
        /// <param name="size">Size of the external object.</param>
        /// <param name="xAlignment">Horizontal alignment.</param>
        /// <param name="yAlignment">Vertical alignment.</param>
        /// <param name="rotation">Rotational counterclockwise angle.</param>
        public void ShowXObject(XObject value, SKPoint location, SKSize? size, XAlignmentEnum xAlignment, YAlignmentEnum yAlignment, double rotation)
            => ShowXObject(GetResourceName(value), location, size, xAlignment, yAlignment, rotation);

        /// <summary>Begins a subpath [PDF:1.6:4.4.1].</summary>
        /// <param name="startPoint">Starting point.</param>
        public void StartPath(SKPoint startPoint) => Add(new BeginSubpath(startPoint.X, Scanner.ContextBox.Height - startPoint.Y));

        /// <summary>Strokes the path using the current color [PDF:1.6:4.4.2].</summary>
        /// <seealso cref="SetStrokeColor(Color)"/>
        public void Stroke() => Add(PaintPath.Stroke);

        /// <summary>Applies a translation to the coordinate system from user space to device space
        /// [PDF:1.6:4.2.2].</summary>
        /// <param name="distanceX">Horizontal distance.</param>
        /// <param name="distanceY">Vertical distance.</param>
        /// <seealso cref="ApplyMatrix(double,double,double,double,double,double)"/>
        public void Translate(double distanceX, double distanceY) => ApplyMatrix(1, 0, 0, 1, distanceX, distanceY);

        private void ApplyState_(PdfName name) => Add(new ApplyExtGState(name));

        private GraphicsMarkedContent BeginMarkedContent_(PdfName tag, PdfName propertyListName)
            => Begin(new GraphicsMarkedContent(new BeginPropertyListMarkedContent(tag, propertyListName)));

        /// <summary>Begins a text object [PDF:1.6:5.3].</summary>
        /// <seealso cref="End()"/>
        private GraphicsText BeginText() => Begin(new GraphicsText());

        //TODO: drawArc MUST seamlessly manage already-begun paths.
        private void DrawArc(SKRect location, double startAngle, double endAngle, double branchWidth, double branchRatio, bool beginPath)
        {
            // NOTE: Strictly speaking, arc drawing is NOT a PDF primitive;
            // it leverages the cubic bezier curve operator (thanks to
            // G. Adam Stanislav, whose article was greatly inspirational:
            // see http://www.whizkidtech.redprince.net/bezier/circle/).

            if (startAngle > endAngle)
            {
                double swap = startAngle;
                startAngle = endAngle;
                endAngle = swap;
            }

            double radiusX = location.Width / 2;
            double radiusY = location.Height / 2;

            var center = new SKPoint((float)(location.Left + radiusX), (float)(location.Top + radiusY));

            var radians1 = MathUtils.ToRadians(startAngle);
            var point1 = new SKPoint((float)(center.X + Math.Cos(radians1) * radiusX), (float)(center.Y - Math.Sin(radians1) * radiusY));

            if (beginPath)
            { StartPath(point1); }

            double endRadians = MathUtils.ToRadians(endAngle);
            double quadrantRadians = Math.PI / 2;
            double radians2 = Math.Min(radians1 + quadrantRadians - radians1 % quadrantRadians, endRadians);
            double kappa = 0.5522847498;
            while (true)
            {
                double segmentX = radiusX * kappa;
                double segmentY = radiusY * kappa;

                // Endpoint 2.
                var point2 = new SKPoint((float)(center.X + Math.Cos(radians2) * radiusX), (float)(center.Y - Math.Sin(radians2) * radiusY));

                // Control point 1.
                double tangentialRadians1 = Math.Atan(-(Math.Pow(radiusY, 2) * (point1.X - center.X)) / (Math.Pow(radiusX, 2) * (point1.Y - center.Y)));
                double segment1 = (
                  segmentY * (1 - Math.Abs(Math.Sin(radians1)))
                    + segmentX * (1 - Math.Abs(Math.Cos(radians1)))
                  ) * (radians2 - radians1) / quadrantRadians; // TODO: control segment calculation is still not so accurate as it should -- verify how to improve it!!!
                var control1 = new SKPoint(
                  (float)(point1.X + Math.Abs(Math.Cos(tangentialRadians1) * segment1) * Math.Sign(-Math.Sin(radians1))),
                  (float)(point1.Y + Math.Abs(Math.Sin(tangentialRadians1) * segment1) * Math.Sign(-Math.Cos(radians1))));

                // Control point 2.
                double tangentialRadians2 = Math.Atan(-(Math.Pow(radiusY, 2) * (point2.X - center.X)) / (Math.Pow(radiusX, 2) * (point2.Y - center.Y)));
                double segment2 = (
                  segmentY * (1 - Math.Abs(Math.Sin(radians2)))
                    + segmentX * (1 - Math.Abs(Math.Cos(radians2)))
                  ) * (radians2 - radians1) / quadrantRadians; // TODO: control segment calculation is still not so accurate as it should -- verify how to improve it!!!
                var control2 = new SKPoint(
                  (float)(point2.X + Math.Abs(Math.Cos(tangentialRadians2) * segment2) * Math.Sign(Math.Sin(radians2))),
                  (float)(point2.Y + Math.Abs(Math.Sin(tangentialRadians2) * segment2) * Math.Sign(Math.Cos(radians2))));

                // Draw the current quadrant curve!
                DrawCurve(point2, control1, control2);

                // Last arc quadrant?
                if (radians2 == endRadians)
                    break;

                // Preparing the next quadrant iteration...
                point1 = point2;
                radians1 = radians2;
                radians2 += quadrantRadians;
                if (radians2 > endRadians)
                { radians2 = endRadians; }

                double quadrantRatio = (radians2 - radians1) / quadrantRadians;
                radiusX += branchWidth * quadrantRatio;
                radiusY += branchWidth * quadrantRatio;

                branchWidth *= branchRatio;
            }
        }

        //TODO: temporary (consolidate stack tracing of marked content blocks!)
        private LayerEntity GetLayer()
        {
            foreach (var parentLevel in stack)
            {
                if (parentLevel.Object is GraphicsMarkedContent markedContent)
                {
                    var marker = markedContent.MarkerHeader;
                    if (PdfName.OC.Equals(marker.Tag))
                        return (LayerEntity)marker.GetProperties(Scanner);
                }
            }
            return null;
        }

        private PdfName GetResourceName<T>(T value, PdfName defaultName = null)
            where T : IPdfDataObject
        {
            if (value is GrayColorSpace)
                return PdfName.DeviceGray;
            else if (value is RGBColorSpace)
                return PdfName.DeviceRGB;
            else if (value is CMYKColorSpace)
                return PdfName.DeviceCMYK;
            else
            {
                // Ensuring that the resource exists within the context resources...
                var biDict = Scanner.Context.Resources.Get(value.GetType());
                // var resourceItemsObject = ((PdfObjectWrapper<PdfDictionary>)biDict).BaseDataObject;
                // Get the key associated to the resource!
                var name = biDict.GetKey(value) as PdfName;
                // No key found?
                if (name == null)
                {
                    if (defaultName == null)
                    {
                        // Insert the resource within the collection!
                        int resourceIndex = biDict.Count;
                        do
                        { name = PdfName.Get($"Res{(++resourceIndex)}", true); }
                        while (biDict.ContainsKey(name));
                    }
                    else
                    {
                        name = defaultName;
                    }
                    biDict[name] = value;
                }
                return name;
            }
        }

        /// <summary>Applies a rotation to the coordinate system from text space to user space
        /// [PDF:1.6:4.2.2].</summary>
        /// <param name="angle">Rotational counterclockwise angle.</param>
        private void RotateText(double angle)
        {
            double rad = MathUtils.ToRadians(angle);
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);

            SetTextMatrix(cos, sin, -sin, cos, 0, 0);
        }

        /// <summary>Applies a scaling to the coordinate system from text space to user space
        /// [PDF:1.6:4.2.2].</summary>
        /// <param name="ratioX">Horizontal scaling ratio.</param>
        /// <param name="ratioY">Vertical scaling ratio.</param>
        private void ScaleText(double ratioX, double ratioY) => SetTextMatrix(ratioX, 0, 0, ratioY, 0, 0);

        private void SetFont_(PdfName name, double size) => Add(new SetFont(name, size));

        /// <summary>Sets the transformation of the coordinate system from text space to user space
        /// [PDF:1.6:5.3.1].</summary>
        /// <remarks>The transformation replaces the current text matrix.</remarks>
        /// <param name="a">Item 0,0 of the matrix.</param>
        /// <param name="b">Item 0,1 of the matrix.</param>
        /// <param name="c">Item 1,0 of the matrix.</param>
        /// <param name="d">Item 1,1 of the matrix.</param>
        /// <param name="e">Item 2,0 of the matrix.</param>
        /// <param name="f">Item 2,1 of the matrix.</param>
        private void SetTextMatrix(double a, double b, double c, double d, double e, double f) => Add(new SetTextMatrix(a, b, c, d, e, f));

        /// <summary>Applies a translation to the coordinate system from text space to user space
        /// [PDF:1.6:4.2.2].</summary>
        /// <param name="distanceX">Horizontal distance.</param>
        /// <param name="distanceY">Vertical distance.</param>
        private void TranslateText(double distanceX, double distanceY) => SetTextMatrix(1, 0, 0, 1, distanceX, distanceY);

        /// <summary>Applies a translation to the coordinate system from text space to user space,
        /// relative to the start of the current line [PDF:1.6:5.3.1].</summary>
        /// <param name="offsetX">Horizontal offset.</param>
        /// <param name="offsetY">Vertical offset.</param>
        private void TranslateTextRelative(double offsetX, double offsetY) => Add(new TranslateTextRelativeNoLead(offsetX, -offsetY));

        /// <summary>Applies a translation to the coordinate system from text space to user space,
        /// moving to the start of the next line [PDF:1.6:5.3.1].</summary>
        private void TranslateTextToNextLine() => Add(Objects.TranslateTextToNextLine.Value);
    }
}