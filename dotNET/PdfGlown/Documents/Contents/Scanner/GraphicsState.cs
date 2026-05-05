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

using PdfGlown.Documents.Contents.ColorSpaces;
using PdfGlown.Documents.Contents.Fonts;
using PdfGlown.Documents.Contents.Patterns;
using PdfGlown.Documents.Contents.XObjects;
using PdfGlown.Documents.Functions;
using SkiaSharp;
using System;

namespace PdfGlown.Documents.Contents
{
    /// <summary>Graphics state [PDF:1.6:4.3].</summary>
    public sealed class GraphicsState : ICloneable
    {
        private BlendModeEnum? blendMode;
        private SKMatrix ctm;
        private IColor fillColor;
        private ColorSpace fillColorSpace;
        private IColor strokeColor;
        private ColorSpace strokeColorSpace;
        private LineCapEnum lineCap;
        private LineDash lineDash;
        private LineJoinEnum lineJoin;
        private float lineWidth;
        private float miterLimit;

        private PdfFont font;
        private float fontSize;

        private TextRenderModeEnum renderMode;
        private float rise;
        private float scale;

        private float charSpace;
        private float wordSpace;
        private float lead;

        private TextGraphicsState textState;
        private ContentScanner scanner;
        private SKPath clipPath;
        private SKMatrix ltm;
        private SoftMask sMask;

        private GraphicsState()
        { }

        internal GraphicsState(ContentScanner scanner)
        {
            this.scanner = scanner;
            Initialize();
        }

        /// <summary>Gets/Sets the current font [PDF:1.6:5.2].</summary>
        public PdfFont Font
        {
            get => font;
            set => font = value;
        }

        /// <summary>Gets/Sets the current font size [PDF:1.6:5.2].</summary>
        public float FontSize
        {
            get => fontSize;
            set => fontSize = value;
        }

        /// <summary>Gets/Sets the current text rendering mode [PDF:1.6:5.2.5].</summary>
        public TextRenderModeEnum RenderMode
        {
            get => renderMode;
            set => renderMode = value;
        }

        /// <summary>Gets/Sets the current text rise [PDF:1.6:5.2.6].</summary>
        public float Rise
        {
            get => rise;
            set => rise = value;
        }

        /// <summary>Gets/Sets the current horizontal scaling [PDF:1.6:5.2.3], normalized to 1.</summary>
        public float Scale
        {
            get => scale;
            set => scale = value;
        }

        /// <summary>Gets/Sets the current character spacing [PDF:1.6:5.2.1].</summary>
        public float CharSpace
        {
            get => charSpace;
            set => charSpace = value;
        }

        /// <summary>Gets/Sets the current word spacing [PDF:1.6:5.2.2].</summary>
        public float WordSpace
        {
            get => wordSpace;
            set => wordSpace = value;
        }

        /// <summary>Gets/Sets the current leading [PDF:1.6:5.2.4].</summary>
        public float Lead
        {
            get => lead;
            set => lead = value;
        }

        public bool RenderModeFill => RenderMode == TextRenderModeEnum.Fill
                    || RenderMode == TextRenderModeEnum.FillStroke
                    || RenderMode == TextRenderModeEnum.FillClip
                    || RenderMode == TextRenderModeEnum.FillStrokeClip;

        public bool RenderModeStroke => RenderMode == TextRenderModeEnum.Stroke
                    || RenderMode == TextRenderModeEnum.FillStroke
                    || RenderMode == TextRenderModeEnum.StrokeClip
                    || RenderMode == TextRenderModeEnum.FillStrokeClip;

        public bool RenderModeClip => RenderMode == TextRenderModeEnum.Clip
                    || RenderMode == TextRenderModeEnum.FillClip
                    || RenderMode == TextRenderModeEnum.StrokeClip
                    || RenderMode == TextRenderModeEnum.FillStrokeClip;

        public TextGraphicsState TextState
        {
            get => textState;
            set => textState = value;
        }

        /// <summary>Gets/Sets the current blend mode to be used in the transparent imaging model
        /// [PDF:1.6:5.2.1].</summary>
        /// <remarks>The application should use the first blend mode in the list that it recognizes.
        /// </remarks>
        public BlendModeEnum? BlendMode
        {
            get => blendMode;
            set => blendMode = value;
        }

        /// <summary>Gets/Sets the current transformation matrix.</summary>
        public SKMatrix Ctm
        {
            get => ctm;
            set
            {
                ctm = value;
                Scanner?.Canvas?.SetMatrix(ctm);
            }
        }

        /// <summary>Gets/Sets the local transformation matrix.</summary>
        public SKMatrix Ltm
        {
            get => ltm;
            set
            {
                ltm = ltm.PreConcat(value);
                Ctm = ctm.PreConcat(value);
            }
        }

        /// <summary>Gets/Sets the current color for nonstroking operations [PDF:1.6:4.5.1].</summary>
        public IColor FillColor
        {
            get => fillColor;
            set => fillColor = value;
        }

        /// <summary>Gets/Sets the current color space for nonstroking operations [PDF:1.6:4.5.1].</summary>
        public ColorSpace FillColorSpace
        {
            get => fillColorSpace;
            set => fillColorSpace = value;
        }

        /// <summary>Gets/Sets the current line cap style [PDF:1.6:4.3.2].</summary>
        public LineCapEnum LineCap
        {
            get => lineCap;
            set => lineCap = value;
        }

        /// <summary>Gets/Sets the current line dash pattern [PDF:1.6:4.3.2].</summary>
        public LineDash LineDash
        {
            get => lineDash;
            set => lineDash = value;
        }

        /// <summary>Gets/Sets the current line join style [PDF:1.6:4.3.2].</summary>
        public LineJoinEnum LineJoin
        {
            get => lineJoin;
            set => lineJoin = value;
        }

        /// <summary>Gets/Sets the current line width [PDF:1.6:4.3.2].</summary>
        public float LineWidth
        {
            get => lineWidth;
            set => lineWidth = value;
        }

        /// <summary>Gets/Sets the current miter limit [PDF:1.6:4.3.2].</summary>
        public float MiterLimit
        {
            get => miterLimit;
            set => miterLimit = value;
        }

        /// <summary>Gets the scanner associated to this state.</summary>
        public ContentScanner Scanner => scanner;

        /// <summary>Gets/Sets the current color for stroking operations [PDF:1.6:4.5.1].</summary>
        public IColor StrokeColor
        {
            get => strokeColor;
            set => strokeColor = value;
        }

        /// <summary>Gets/Sets the current color space for stroking operations [PDF:1.6:4.5.1].</summary>
        public ColorSpace StrokeColorSpace
        {
            get => strokeColorSpace;
            set => strokeColorSpace = value;
        }

        public float? StrokeAlpha { get; set; }

        public float? FillAlpha { get; set; }

        public bool AlphaIsShape { get; set; }

        public SoftMask SMask
        {
            get => sMask;
            set
            {
                sMask = value;
                if (sMask != null)
                    sMask.InitialMatrix = Ctm;
            }
        }

        public Shadings.Shading Shading { get; internal set; }

        public bool? Knockout { get; internal set; }

        public Function Function { get; internal set; }

        public bool StrokeOverprint { get; internal set; }

        public bool FillOverprint { get; internal set; }

        public int OverprintMode { get; internal set; }

        public bool StrokeAdjustment { get; internal set; }

        public float Flatness { get; internal set; }


        /// <summary>Gets the initial current transformation matrix.</summary>
        public SKMatrix GetInitialCtm()
        {
            return GetInitialMatrix(Scanner.Context, Scanner.CanvasBox.Size);
        }

        public static SKMatrix GetInitialMatrix(IContentContext contentContext, SKSize canvasSize)
        {
            if (contentContext == null)
                return SKMatrix.Identity;
            return GetInitialMatrix(contentContext, canvasSize, contentContext.Box);
        }

        public static SKMatrix GetInitialMatrix(IContentContext contentContext, SKSize canvasSize, SKRect contentBox)
        {
            SKMatrix initialCtm;
            var rotation = contentContext.Rotation;
            if (contentContext is FormXObject xObject)
            {
                return xObject.InitialMatrix;
            }
            else if (contentContext is TilingPattern tiling)
            {
                return tiling.Matrix;
            }
            else if (contentContext is Type3CharProc charProc)
            {
                return SKMatrix.Identity;
            }
            else
            {
                // Axes orientation.
                initialCtm = GetRotationMatrix(canvasSize, rotation);
            }

            // Scaling.
            SKSize rotatedCanvasSize = rotation.Transform(canvasSize);
            initialCtm = initialCtm.PreConcat(SKMatrix.CreateScale(
               rotatedCanvasSize.Width / contentBox.Width,
               rotatedCanvasSize.Height / contentBox.Height));

            // Origin alignment.
            initialCtm = initialCtm.PreConcat(SKMatrix.CreateTranslation(-contentBox.Left, -contentBox.Top)); //TODO: verify minimum coordinates!
            return initialCtm;
        }
        public SKColor? GetStrokeColor()
        {
            return StrokeColorSpace?.GetSKColor(StrokeColor, AlphaIsShape ? null : StrokeAlpha);
        }

        public SKPaint CreateStrokePaint()
        {
            var paint = StrokeColorSpace?.GetPaint(StrokeColor, SKPaintStyle.Stroke, AlphaIsShape ? null : StrokeAlpha, this);
            if (paint != null)
            {
                //paint.TextSize = (float)FontSize;
                //paint.TextScaleX = (float)Scale;

                paint.StrokeWidth = LineWidth < 1 ? 0 : LineWidth;
                paint.StrokeCap = LineCap.ToSkia();
                paint.StrokeJoin = LineJoin.ToSkia();
                paint.StrokeMiter = MiterLimit;
                LineDash?.Apply(paint);
                if (BlendMode is BlendModeEnum blend)
                    ApplyBlend(paint, blend);
                SMask?.Apply(paint, scanner);
            }
            return paint;
        }

        public SKColor? GetFillColor()
        {
            return FillColorSpace?.GetSKColor(FillColor, AlphaIsShape ? null : FillAlpha);
        }

        public SKPaint CreateFillPaint()
        {
            var paint = FillColorSpace?.GetPaint(FillColor, SKPaintStyle.Fill, AlphaIsShape ? null : FillAlpha, this);
            if (paint != null)
            {
                //paint.TextSize = (float)FontSize;
                //paint.TextScaleX = (float)Scale;

                if (BlendMode is BlendModeEnum blend)
                    ApplyBlend(paint, blend);
                SMask?.Apply(paint, scanner);
            }
            return paint;
        }

        public SKPath GetClipPath()
        {
            return RenderModeClip
                ? clipPath ??= new SKPath()
                : null;
        }

        public void ApplyClipPath()
        {
            if (clipPath != null)
            {
                scanner.Canvas?.ClipPath(clipPath, SKClipOperation.Intersect, true);
                clipPath?.Dispose();
                clipPath = null;
            }
        }

        private static void ApplyBlend(SKPaint paint, BlendModeEnum mode)
        {
            switch (mode)
            {
                case BlendModeEnum.Multiply:
                    paint.BlendMode = SKBlendMode.Multiply;
                    break;
                case BlendModeEnum.Lighten:
                    paint.BlendMode = SKBlendMode.Lighten;
                    break;
                case BlendModeEnum.Luminosity:
                    paint.BlendMode = SKBlendMode.Luminosity;
                    break;
                case BlendModeEnum.Overlay:
                    paint.BlendMode = SKBlendMode.Overlay;
                    break;
                case BlendModeEnum.Normal:
                    paint.BlendMode = SKBlendMode.SrcOver;
                    break;
                case BlendModeEnum.ColorBurn:
                    paint.BlendMode = SKBlendMode.ColorBurn;
                    break;
                case BlendModeEnum.Screen:
                    paint.BlendMode = SKBlendMode.Screen;
                    break;
                case BlendModeEnum.Darken:
                    paint.BlendMode = SKBlendMode.Darken;
                    break;
                case BlendModeEnum.ColorDodge:
                    paint.BlendMode = SKBlendMode.ColorDodge;
                    break;
                case BlendModeEnum.Compatible:
                    paint.BlendMode = SKBlendMode.SrcOver;
                    break;
                case BlendModeEnum.HardLight:
                    paint.BlendMode = SKBlendMode.HardLight;
                    break;
                case BlendModeEnum.SoftLight:
                    paint.BlendMode = SKBlendMode.SoftLight;
                    break;
                case BlendModeEnum.Difference:
                    paint.BlendMode = SKBlendMode.Difference;
                    break;
                case BlendModeEnum.Exclusion:
                    paint.BlendMode = SKBlendMode.Exclusion;
                    break;
                case BlendModeEnum.Hue:
                    paint.BlendMode = SKBlendMode.Hue;
                    break;
                case BlendModeEnum.Saturation:
                    paint.BlendMode = SKBlendMode.Saturation;
                    break;
                case BlendModeEnum.Color:
                    paint.BlendMode = SKBlendMode.Color;
                    break;
            }
        }

        public static SKMatrix GetRotationMatrix(SKSize canvasSize, RotationEnum rotation)
        {
            switch (rotation)
            {
                case RotationEnum.Downward:
                    return new SKMatrix(1, 0, 0, 0, -1, canvasSize.Height, 0, 0, 1);
                case RotationEnum.Leftward:
                    return new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1);
                case RotationEnum.Upward:
                    return new SKMatrix(-1, 0, canvasSize.Width, 0, 1, 0, 0, 0, 1);
                case RotationEnum.Rightward:
                    return new SKMatrix(0, -1, canvasSize.Width, -1, 0, canvasSize.Height, 0, 0, 1);
                default:
                    throw new NotImplementedException();
            }
        }

        public static SKMatrix GetRotationLeftBottomMatrix(SKRect box, int degrees)
        {
            var matrix = SKMatrix.CreateRotationDegrees(degrees);
            matrix = matrix.PreConcat(SKMatrix.CreateScale(1, -1));
            var mappedBox = matrix.MapRect(box);
            matrix = matrix.PostConcat(SKMatrix.CreateTranslation(-mappedBox.Left, -mappedBox.Top));
            return matrix;
        }

        public static SKMatrix GetRotationMatrix(SKRect box, int degrees)
        {
            var matrix = SKMatrix.CreateRotationDegrees(degrees);
            var mappedBox = matrix.MapRect(box);
            matrix = matrix.PostConcat(SKMatrix.CreateTranslation(-mappedBox.Left, -mappedBox.Top));
            return matrix;
        }

        /// <summary>Gets the text-to-device space transformation matrix [PDF:1.6:5.3.3].</summary>
        /// <param name="topDown">Whether the y-axis orientation has to be adjusted to common top-down
        /// orientation rather than standard PDF coordinate system (bottom-up).</param>
        public SKMatrix GetTextToDeviceMatrix(IContentContext context)
        {
            /*
              NOTE: The text rendering matrix (trm) is obtained from the concatenation of the current
              transformation matrix (ctm) and the text matrix (tm).
            */
            var matrix = GetUserToDeviceMatrix(context);
            return matrix.PostConcat(textState.Tm);
        }

        /// <summary>Gets the user-to-device space transformation matrix [PDF:1.6:4.2.3].</summary>
        /// <param name="topDown">Whether the y-axis orientation has to be adjusted to common top-down
        /// orientation rather than standard PDF coordinate system (bottom-up).</param>
        public SKMatrix GetUserToDeviceMatrix(IContentContext context)
        {
            var matrix = context.RotateMatrix;
            return matrix.PreConcat(ctm);
        }

        public TextGraphicsState PushTextState()
        {
            Scanner.TextStateStack.Push(TextState);
            return TextState = new TextGraphicsState();
        }

        public TextGraphicsState PopTextState()
        {
            return Scanner.TextStateStack.TryPop(out var poped)
                ? TextState = poped
                : null;
        }

        public void Save()
        {
            Scanner.Canvas?.Save();
            var stack = Scanner.StateStack;
            var cloned = (GraphicsState)Clone();
            stack.Push(cloned);
        }

        public void Restore()
        {
            Scanner.Canvas?.Restore();
            var stack = Scanner.StateStack;
            if (stack.Count > 0)
            {
                var poped = stack.Pop();
                poped.CopyTo(this);
            }
        }

        internal GraphicsState Clone(ContentScanner scanner)
        {
            GraphicsState state = (GraphicsState)Clone();
            state.scanner = scanner;
            return state;
        }

        internal void Initialize()
        {
            // State parameters initialization.
            blendMode = BlendModeEnum.Normal;
            Ctm = GetInitialCtm();
            ltm = SKMatrix.Identity;
            fillColor = GrayColor.Default;
            fillColorSpace = GrayColorSpace.Default;
            strokeColor = GrayColor.Default;
            strokeColorSpace = GrayColorSpace.Default;
            lineCap = LineCapEnum.Butt;
            lineDash = new LineDash();
            lineJoin = LineJoinEnum.Miter;
            lineWidth = 1;
            miterLimit = 10;
            font = null;
            fontSize = 0;
            rise = 0;
            scale = 1;
            charSpace = 0;
            wordSpace = 0;
            lead = 0;
            renderMode = TextRenderModeEnum.Fill;
            TextState = new TextGraphicsState();
            SMask = null;
            StrokeAlpha = null;
            FillAlpha = null;
        }

        /// <summary>Gets a deep copy of the graphics state object.</summary>
        public object Clone()
        {
            var clone = new GraphicsState
            {
                scanner = scanner,
                //Text
                font = font,
                fontSize = fontSize,
                renderMode = renderMode,
                rise = rise,
                scale = scale,
                charSpace = charSpace,
                wordSpace = wordSpace,
                lead = lead,
                //Paint
                blendMode = blendMode,
                ctm = ctm,
                ltm = ltm,
                fillColor = fillColor,
                fillColorSpace = fillColorSpace,
                strokeColor = strokeColor,
                strokeColorSpace = strokeColorSpace,
                lineCap = lineCap,
                lineDash = lineDash,// != null ? new LineDash(lineDash.DashArray, lineDash.DashPhase),
                lineJoin = lineJoin,
                lineWidth = lineWidth,
                miterLimit = miterLimit,
                SMask = SMask,
                AlphaIsShape = AlphaIsShape,
                StrokeAlpha = StrokeAlpha,
                FillAlpha = FillAlpha,
                FillOverprint = FillOverprint,
                StrokeOverprint = StrokeOverprint,
                OverprintMode = OverprintMode,
                Flatness = Flatness,
                TextState = new TextGraphicsState
                {
                    Tm = textState.Tm,
                    Tlm = textState.Tlm
                }
            };
            return clone;
        }

        /// <summary>Copies this graphics state into the specified one.</summary>
        /// <param name="state">Target graphics state object.</param>
        public void CopyTo(GraphicsState state)
        {
            state.ctm = ctm;
            state.ltm = ltm;
            //Text
            state.font = font;
            state.fontSize = fontSize;
            state.renderMode = renderMode;
            state.rise = rise;
            state.scale = scale;
            state.charSpace = charSpace;
            state.wordSpace = wordSpace;
            state.lead = lead;
            //Paint
            state.blendMode = blendMode;
            state.fillColor = fillColor;
            state.fillColorSpace = fillColorSpace;
            state.strokeColor = strokeColor;
            state.strokeColorSpace = strokeColorSpace;
            state.lineCap = lineCap;
            state.lineDash = lineDash;
            state.lineJoin = lineJoin;
            state.lineWidth = lineWidth;
            state.miterLimit = miterLimit;
            state.TextState = textState;
            state.SMask = SMask;
            state.AlphaIsShape = AlphaIsShape;
            state.FillAlpha = FillAlpha;
            state.StrokeAlpha = StrokeAlpha;
            state.FillOverprint = FillOverprint;
            state.StrokeOverprint = StrokeOverprint;
            state.OverprintMode = OverprintMode;
            state.Flatness = Flatness;
        }

        internal void SetFont(ExtGState eState)
        {
            Font = eState.Font;
            FontSize = eState.FontSize.Value;
        }
    }
}
