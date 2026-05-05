/*
  Copyright 2009-2015 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfGlown.Documents.Contents.Fonts;
using PdfGlown.Documents.Contents.XObjects;
using PdfGlown.Documents.Functions;
using PdfGlown.Objects;
using PdfGlown.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfGlown.Documents.Contents
{
    ///<summary>Graphics state parameters [PDF:1.6:4.3.4].</summary>
    [Pdf(VersionEnum.PDF12)]
    public sealed class ExtGState : PdfDictionary
    {
        private static readonly Dictionary<PdfName, Action<ExtGState, GraphicsState>> factory = new(24)
        {
            { PdfName.Font, static (eState, gState) => gState.SetFont(eState)},
            { PdfName.AIS, static (eState, gState) => gState.AlphaIsShape = eState.AlphaShape },
            { PdfName.CA, static (eState, gState) => gState.StrokeAlpha = eState.StrokeAlpha },
            { PdfName.ca, static (eState, gState) => gState.FillAlpha = eState.FillAlpha },
            { PdfName.SA, static (eState, gState) => gState.StrokeAdjustment = eState.StrokeAdjustment },
            { PdfName.OP, static (eState, gState) => gState.StrokeOverprint = eState.StrokeOverprint },
            { PdfName.op, static (eState, gState) => gState.FillOverprint = eState.FillOverprint },
            { PdfName.OPM, static (eState, gState) => gState.OverprintMode = eState.OverprintMode },
            { PdfName.LC, static (eState, gState) => gState.LineCap = eState.LineCap ?? LineCapEnum.Butt },
            { PdfName.LJ, static (eState, gState) => gState.LineJoin = eState.LineJoin ?? LineJoinEnum.Miter },
            { PdfName.LW, static (eState, gState) => gState.LineWidth = eState.LineWidth ?? 0 },
            { PdfName.D, static (eState, gState) => gState.LineDash = eState.LineDash },
            { PdfName.ML, static (eState, gState) => gState.MiterLimit = eState.MiterLimit ?? 0 },
            { PdfName.BM, static (eState, gState) => gState.BlendMode = eState.BlendMode },
            { PdfName.SMask, static (eState, gState) => gState.SMask = eState.SMask },
            { PdfName.TK, static (eState, gState) => gState.Knockout = eState.Knockout },
            { PdfName.BG, static (eState, gState) => gState.Function = eState.BG },
            { PdfName.BG2, static (eState, gState) => gState.Function = eState.BG2 },
            //TODO:extend supported parameters!!!
        };
        private static readonly BiDictionary<BlendModeEnum?, PdfName> blendingCodes = new()
        {
            [BlendModeEnum.Normal] = PdfName.Normal,
            [BlendModeEnum.Multiply] = PdfName.Multiply,
            [BlendModeEnum.Screen] = PdfName.Screen,
            [BlendModeEnum.Overlay] = PdfName.Overlay,
            [BlendModeEnum.Darken] = PdfName.Darken,
            [BlendModeEnum.Lighten] = PdfName.Lighten,
            [BlendModeEnum.ColorDodge] = PdfName.ColorDodge,
            [BlendModeEnum.ColorBurn] = PdfName.ColorBurn,
            [BlendModeEnum.Compatible] = PdfName.Compatible,
            [BlendModeEnum.HardLight] = PdfName.HardLight,
            [BlendModeEnum.SoftLight] = PdfName.SoftLight,
            [BlendModeEnum.Difference] = PdfName.Difference,
            [BlendModeEnum.Exclusion] = PdfName.Exclusion,
            [BlendModeEnum.Hue] = PdfName.Hue,
            [BlendModeEnum.Saturation] = PdfName.Saturation,
            [BlendModeEnum.Color] = PdfName.Color,
            [BlendModeEnum.Luminosity] = PdfName.Luminosity
        };

        public static BlendModeEnum? GetBlendMode(PdfName name) => blendingCodes.GetKey(name);

        public static PdfName GetName(BlendModeEnum blendMode) => blendingCodes[blendMode];

        private LineDash lineDash;
        private PdfFont font;

        public ExtGState(PdfDocument context)
            : base(context, new(){
                { PdfName.Type, PdfName.ExtGState }
            })
        { }

        internal ExtGState(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        ///<summary>Gets/Sets whether the current soft mask and alpha constant are to be interpreted as
        ///shape values instead of opacity values.</summary>
        [Pdf(VersionEnum.PDF14)]
        public bool AlphaShape
        {
            get => GetBool(PdfName.AIS, false);
            set => Set(PdfName.AIS, value);
        }

        public void ApplyTo(GraphicsState state)
        {
            foreach (PdfName parameterName in Keys)
            {
                if (factory.TryGetValue(parameterName, out var func))
                    func(this, state);
            }
        }

        [Pdf(VersionEnum.PDF14)]
        public SoftMask SMask
        {
            get => SoftMask.WrapSoftMask(Get(PdfName.SMask));
            set => Set(PdfName.SMask, value);
        }

        public bool? Knockout
        {
            get => GetNBool(PdfName.TK);
            set => Set(PdfName.TK, value);
        }

        /// <summary>Gets/Sets the blend mode to be used in the transparent imaging model [PDF:1.7:7.2.4].
        /// </summary>
        [Pdf(VersionEnum.PDF14)]
        public BlendModeEnum? BlendMode
        {
            get
            {
                var blendModeObject = Get<PdfDirectObject>(PdfName.BM);
                if (blendModeObject == null)
                    return null;

                if (blendModeObject is PdfName name)
                { return GetBlendMode(name); }
                else if(blendModeObject is PdfArray array)
                {
                    foreach (var item in array.GetItems())
                    {
                        if (item?.Resolve() is PdfName blendModeName
                            && GetBlendMode(blendModeName) is BlendModeEnum blendMode)
                            return blendMode;
                    }
                }
                return null;
            }
            set
            {
                var items = GetOrCreate<PdfArrayImpl>(PdfName.BM);
                items.Clear();
                items.Add(GetName(value.Value));
            }
        }

        ///<summary>Gets/Sets the nonstroking alpha constant, specifying the constant shape or constant
        ///opacity value to be used for nonstroking operations in the transparent imaging model
        ///[PDF:1.7:7.2.6].</summary>
        [Pdf(VersionEnum.PDF14)]
        public float? FillAlpha
        {
            get => GetNFloat(PdfName.ca);
            set => Set(PdfName.ca, value);
        }

        ///<summary>Gets/Sets the stroking alpha constant, specifying the constant shape or constant
        ///opacity value to be used for stroking operations in the transparent imaging model
        ///[PDF:1.7:7.2.6].</summary>
        [Pdf(VersionEnum.PDF14)]
        public float? StrokeAlpha
        {
            get => GetNFloat(PdfName.CA);
            set => Set(PdfName.CA, value);
        }

        public bool FillOverprint
        {
            get => GetBool(PdfName.op);
            set => Set(PdfName.op, value);
        }

        public bool StrokeOverprint
        {
            get => GetBool(PdfName.OP);
            set => Set(PdfName.OP, value);
        }

        public int OverprintMode
        {
            get => GetInt(PdfName.OPM);
            set => Set(PdfName.OPM, value);
        }

        public bool StrokeAdjustment
        {
            get => GetBool(PdfName.SA);
            set => Set(PdfName.SA, value);
        }

        [Pdf(VersionEnum.PDF13)]
        public PdfFont Font
        {
            get => font ??= Get<PdfArray>(PdfName.Font)?.Get<PdfFont>(0);
            set
            {
                font = value;
                var fontObject = Get<PdfArray>(PdfName.Font);
                if (fontObject == null)
                { fontObject = new PdfArrayImpl(2) { value?.Reference, PdfInteger.Default }; }
                else
                { fontObject.Set(0, value); }
                SetDirect(PdfName.Font, fontObject);
            }
        }

        [Pdf(VersionEnum.PDF13)]
        public float? FontSize
        {
            get => Get<PdfArray>(PdfName.Font)?.GetFloat(1);
            set
            {
                var fontObject = Get<PdfArray>(PdfName.Font);
                if (fontObject == null)
                { fontObject = new PdfArrayImpl(2) { (string)null, value }; }
                else
                { fontObject.Set(1, value); }
                SetDirect(PdfName.Font, fontObject);
            }
        }

        [Pdf(VersionEnum.PDF13)]
        public LineCapEnum? LineCap
        {
            get => (LineCapEnum?)GetNInt(PdfName.LC);
            set => Set(PdfName.LC, value.HasValue ? (int)value.Value : null);
        }

        [Pdf(VersionEnum.PDF13)]
        public LineDash LineDash
        {
            get => lineDash ??= Get<PdfArray>(PdfName.D) is PdfArray lineDashObject
                ? LineDash.Get(lineDashObject.Get<PdfArray>(0), lineDashObject.GetNumber(1))
                : null;
            set
            {
                lineDash = value;
                this[PdfName.D] = new PdfArrayImpl
                {
                    new PdfArrayImpl(value.DashArray),
                    value.DashPhase
                };
            }
        }

        [Pdf(VersionEnum.PDF13)]
        public LineJoinEnum? LineJoin
        {
            get => (LineJoinEnum?)GetNInt(PdfName.LJ);
            set => Set(PdfName.LJ, value.HasValue ? (int)value.Value : null);
        }

        [Pdf(VersionEnum.PDF13)]
        public float? LineWidth
        {
            get => GetNFloat(PdfName.LW);
            set => Set(PdfName.LW, value);
        }

        [Pdf(VersionEnum.PDF13)]
        public float? MiterLimit
        {
            get => GetNFloat(PdfName.ML);
            set => Set(PdfName.ML, value);
        }

        public Function BG
        {
            get => Function.Wrap(Get(PdfName.BG));
            set => Set(PdfName.BG, value);
        }

        public Function BG2
        {
            get => Function.Wrap(Get(PdfName.BG2));
            set => Set(PdfName.BG2, value);
        }
    }
}