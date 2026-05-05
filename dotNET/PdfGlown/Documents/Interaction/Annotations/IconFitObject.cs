/*
  Copyright 2008-2015 Stefano Chizzolini. http://www.pdfclown.org

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

using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Interaction.Annotations
{
    /// <summary>Icon fit [PDF:1.6:8.6.6].</summary>
    public class IconFitObject : PdfDictionary
    {
        /// <summary>Scaling mode [PDF:1.6:8.6.6].</summary>
        public enum ScaleModeEnum
        {
            /// <summary>Always scale.</summary>
            Always,
            /// <summary>Scale only when the icon is bigger than the annotation box.</summary>
            Bigger,
            /// <summary>Scale only when the icon is smaller than the annotation box.</summary>
            Smaller,
            /// <summary>Never scale.</summary>
            Never
        };

        /// <summary>Scaling type [PDF:1.6:8.6.6].</summary>
        public enum ScaleTypeEnum
        {
            /// <summary>Scale the icon to fill the annotation box exactly,
            /// without regard to its original aspect ratio.</summary>
            Anamorphic,
            /// <summary>Scale the icon to fit the width or height of the annotation box,
            /// while maintaining the icon's original aspect ratio.</summary>
            Proportional
        };

        private static readonly Dictionary<ScaleModeEnum, PdfName> ScaleModeEnumCodes;
        private static readonly Dictionary<ScaleTypeEnum, PdfName> ScaleTypeEnumCodes;

        static IconFitObject()
        {
            ScaleModeEnumCodes = new Dictionary<ScaleModeEnum, PdfName>
            {
                [ScaleModeEnum.Always] = PdfName.A,
                [ScaleModeEnum.Bigger] = PdfName.B,
                [ScaleModeEnum.Smaller] = PdfName.S,
                [ScaleModeEnum.Never] = PdfName.N
            };

            ScaleTypeEnumCodes = new Dictionary<ScaleTypeEnum, PdfName>
            {
                [ScaleTypeEnum.Anamorphic] = PdfName.A,
                [ScaleTypeEnum.Proportional] = PdfName.P
            };
        }

        /// <summary>Gets the code corresponding to the given value.</summary>
        private static PdfName ToCode(ScaleModeEnum value) => ScaleModeEnumCodes[value];

        /// <summary>Gets the code corresponding to the given value.</summary>
        private static PdfName ToCode(ScaleTypeEnum value) => ScaleTypeEnumCodes[value];

        /// <summary>Gets the scaling mode corresponding to the given value.</summary>
        private static ScaleModeEnum ToScaleModeEnum(IPdfString value)
        {
            if (value == null)
                return ScaleModeEnum.Always;
            foreach (KeyValuePair<ScaleModeEnum, PdfName> scaleMode in ScaleModeEnumCodes)
            {
                if (string.Equals(scaleMode.Value.StringValue, value.StringValue, StringComparison.Ordinal))
                    return scaleMode.Key;
            }
            return ScaleModeEnum.Always;
        }

        /// <summary>Gets the scaling type corresponding to the given value.</summary>
        private static ScaleTypeEnum ToScaleTypeEnum(IPdfString value)
        {
            if (value == null)
                return ScaleTypeEnum.Proportional;
            foreach (KeyValuePair<ScaleTypeEnum, PdfName> scaleType in ScaleTypeEnumCodes)
            {
                if (string.Equals(scaleType.Value.StringValue, value.StringValue, StringComparison.Ordinal))
                    return scaleType.Key;
            }
            return ScaleTypeEnum.Proportional;
        }

        public IconFitObject(PdfDocument context)
            : base(context, new())
        { }

        internal IconFitObject(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets/Sets whether not to take into consideration the line width of the border.</summary>
        public bool BorderExcluded
        {
            get => GetBool(PdfName.FB);
            set => Set(PdfName.FB, value);
        }

        /// <summary>Gets/Sets the circumstances under which the icon should be scaled inside the annotation box.</summary>
        public ScaleModeEnum ScaleMode
        {
            get => ToScaleModeEnum(Get<IPdfString>(PdfName.SW));
            set => this[PdfName.SW] = ToCode(value);
        }

        /// <summary>Gets/Sets the type of scaling to use.</summary>
        public ScaleTypeEnum ScaleType
        {
            get => ToScaleTypeEnum(Get<IPdfString>(PdfName.S));
            set => this[PdfName.S] = ToCode(value);
        }

        public PdfArray Alignment
        {
            get => Get<PdfArray>(PdfName.A);
            set => this[PdfName.A] = value;
        }

        /// <summary>Gets/Sets the horizontal alignment of the icon inside the annotation box.</summary>
        public XAlignmentEnum XAlignment
        {
            get
            {
                return (int)Math.Round((Alignment?.GetDouble(0, 0.5D) ?? 0.5D) / .5) switch
                {
                    0 => XAlignmentEnum.Left,
                    2 => XAlignmentEnum.Right,
                    _ => XAlignmentEnum.Center,
                };
            }
            set
            {
                PdfArray alignmentObject = Alignment;
                if (alignmentObject == null)
                {
                    Alignment = alignmentObject = new PdfArrayImpl(2) { 0.5D, 0.5D };
                }

                double objectValue;
                switch (value)
                {
                    case XAlignmentEnum.Left: objectValue = 0; break;
                    case XAlignmentEnum.Right: objectValue = 1; break;
                    default: objectValue = 0.5; break;
                }
                alignmentObject.Set(0, objectValue);
            }
        }

        /// <summary>Gets/Sets the vertical alignment of the icon inside the annotation box.</summary>
        public YAlignmentEnum YAlignment
        {
            get
            {
                return (int)Math.Round((Alignment?.GetDouble(1, 0.5D) ?? 0.5D) / .5) switch
                {
                    0 => YAlignmentEnum.Bottom,
                    2 => YAlignmentEnum.Top,
                    _ => YAlignmentEnum.Middle,
                };
            }
            set
            {
                PdfArray alignmentObject = Alignment;
                if (alignmentObject == null)
                {
                    Alignment = alignmentObject = new PdfArrayImpl(2) { 0.5D, 0.5D };
                }

                double objectValue;
                switch (value)
                {
                    case YAlignmentEnum.Bottom: objectValue = 0; break;
                    case YAlignmentEnum.Top: objectValue = 1; break;
                    default: objectValue = 0.5; break;
                }
                alignmentObject.Set(1, objectValue);
            }
        }
    }

}