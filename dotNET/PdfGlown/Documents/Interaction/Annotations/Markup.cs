/*
  Copyright 2013-2015 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfGlown.Documents.Contents.Composition;
using PdfGlown.Objects;
using PdfGlown.Util;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Interaction.Annotations
{
    /// <summary>Markup annotation [PDF:1.6:8.4.5].</summary>
    /// <remarks>It represents text-based annotations used primarily to mark up documents.</remarks>
    [Pdf(VersionEnum.PDF11)]
    public abstract class Markup : Annotation
    {
        private static readonly BiDictionary<ReplyTypeEnum, string> replyTypeCodes = new()
        {
            [ReplyTypeEnum.Thread] = PdfName.R.StringValue,
            [ReplyTypeEnum.Group] = PdfName.Group.StringValue
        };

        private static readonly BiDictionary<MarkupIntent, string> intentCodes = new()
        {
            [MarkupIntent.Text] = PdfName.Text.StringValue,
            [MarkupIntent.FreeText] = PdfName.FreeText.StringValue,
            [MarkupIntent.FreeTextCallout] = PdfName.FreeTextCallout.StringValue,
            [MarkupIntent.FreeTextTypeWriter] = PdfName.FreeTextTypeWriter.StringValue,

            [MarkupIntent.Line] = PdfName.Line.StringValue,
            [MarkupIntent.LineArrow] = PdfName.LineArrow.StringValue,
            [MarkupIntent.LineDimension] = PdfName.LineDimension.StringValue,

            [MarkupIntent.Polygon] = PdfName.Polygon.StringValue,
            [MarkupIntent.PolygonCloud] = PdfName.PolygonCloud.StringValue,
            [MarkupIntent.PolygonDimension] = PdfName.PolygonDimension.StringValue,
            [MarkupIntent.PolyLine] = PdfName.PolyLine.StringValue,
            [MarkupIntent.PolyLineDimension] = PdfName.PolyLineDimension.StringValue
        };

        private static readonly BiDictionary<MarkupState, string> stateCodes = new()
        {
            [MarkupState.None] = PdfName.None.StringValue,
            [MarkupState.Unmarked] = PdfName.Unmarked.StringValue,
            [MarkupState.Accepted] = PdfName.Accepted.StringValue,
            [MarkupState.Rejected] = PdfName.Rejected.StringValue,
            [MarkupState.Cancelled] = PdfName.Cancelled.StringValue,
            [MarkupState.Completed] = PdfName.Completed.StringValue,
        };

        private static readonly BiDictionary<MarkupStateModel, string> stateModelCodes = new()
        {
            [MarkupStateModel.Marked] = PdfName.Marked.StringValue,
            [MarkupStateModel.Review] = PdfName.Review.StringValue
        };

        public static MarkupStateModel? GetStateModel(IPdfString name) => name == null ? null : GetStateModel(name.StringValue);

        public static MarkupStateModel? GetStateModel(string name) => name == null ? null : stateModelCodes.GetKey(name);

        public static string GetCode(MarkupStateModel? intent) => intent == null ? null : stateModelCodes[intent.Value];

        public static MarkupState? GetMarkupState(IPdfString name) => name == null ? null : GetMarkupState(name.StringValue);

        public static MarkupState? GetMarkupState(string name) => name == null ? null : stateCodes.GetKey(name);

        public static string GetMarkupStateCode(MarkupState? intent) => intent == null ? null : stateCodes[intent.Value];

        public static MarkupIntent? GetMarkupIntent(string name) => name == null ? null : intentCodes.GetKey(name);

        public static PdfName GetName(MarkupIntent? intent) => intent == null ? null : PdfName.Get(intentCodes[intent.Value], true);

        public static ReplyTypeEnum? GetReplyType(string name) => name == null ? ReplyTypeEnum.Thread : replyTypeCodes.GetKey(name);

        public static PdfName GetName(ReplyTypeEnum? replyType) => replyType == null ? null : PdfName.Get(replyTypeCodes[replyType.Value], true);

        private BorderEffect borderEffect;
        private Annotation replyTo;
        private DeviceColor interColor;

        protected Markup(PdfPage page, PdfName subtype, SKRect box, string text)
            : base(page, subtype, box, text)
        {
            CreationDate = DateTime.Now;
        }

        protected Markup(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets/Sets the annotation editor. It is displayed as a text label in the title bar of
        /// the annotation's pop-up window when open and active. By convention, it identifies the user who
        /// added the annotation.</summary>
        [Pdf(VersionEnum.PDF11)]
        public override string Author
        {
            get => GetString(PdfName.T);
            set
            {
                var oldValue = Author;
                if (!string.Equals(oldValue, value, StringComparison.Ordinal))
                {
                    SetText(PdfName.T, value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /// <summary>Gets/Sets the date and time when the annotation was created.</summary>
        [Pdf(VersionEnum.PDF15)]
        public override DateTime? CreationDate
        {
            get => GetNDate(PdfName.CreationDate);
            set
            {
                var oldValue = CreationDate;
                if (oldValue != PdfDate.Trimm(value))
                {
                    Set(PdfName.CreationDate, value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /// <summary>Gets/Sets the annotation that this one is in reply to. Both annotations must be on the
        /// same page of the document.</summary>
        /// <remarks>The relationship between the two annotations is specified by the
        /// <see cref="ReplyType"/> property.</remarks>
        [Pdf(VersionEnum.PDF15)]
        public virtual Annotation ReplyTo
        {
            get => replyTo ??= Get<Annotation>(PdfName.IRT);
            set
            {
                var oldValue = ReplyTo;
                if (oldValue != value)
                {
                    Set(PdfName.IRT, replyTo = value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /// <summary>Gets/Sets the pop-up annotation associated with this one.</summary>
        /// <exception cref="InvalidOperationException">If pop-up annotations can't be associated with
        /// this markup.</exception>
        [Pdf(VersionEnum.PDF13)]
        public virtual Popup Popup
        {
            get => Get<Popup>(PdfName.Popup);
            set
            {
                var oldValue = Popup;
                if (oldValue != value)
                {
                    if (value != null)
                    {
                        value.Parent = this;
                    }
                    Set(PdfName.Popup, value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /// <summary>Gets/Sets the relationship between this annotation and one specified by
        /// <see cref="ReplyTo"/> property.</summary>
        [Pdf(VersionEnum.PDF16)]
        public virtual ReplyTypeEnum? ReplyType
        {
            get => GetReplyType(GetString(PdfName.RT));
            set
            {
                var oldValue = ReplyType;
                if (oldValue != value)
                {
                    this[PdfName.RT] = GetName(value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        [Pdf(VersionEnum.PDF15)]
        public string RichContents
        {
            get => GetString(PdfName.RC);
            set
            {
                var oldValue = RichContents;
                if (!string.Equals(oldValue, value, StringComparison.Ordinal))
                {
                    SetText(PdfName.RC, value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        public string DefaultStyle
        {
            get => GetString(PdfName.DS);
            set
            {
                var oldValue = DefaultStyle;
                if (!string.Equals(oldValue, value, StringComparison.Ordinal))
                {
                    SetText(PdfName.DS, value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }


        [Pdf(VersionEnum.PDF16)]
        public MarkupIntent? Intent
        {
            get => GetMarkupIntent(GetString(PdfName.IT));
            set
            {
                var oldValue = Intent;
                if (oldValue != value)
                {
                    this[PdfName.IT] = GetName(value.Value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /// <summary>Gets/Sets the color with which to fill the interior of following markups: Line Ending, Circle, Square.</summary>
        public DeviceColor InteriorColor
        {
            get => interColor ??= DeviceColor.Get(Get<PdfArray>(PdfName.IC));
            set
            {
                var oldValue = InteriorColor;
                if (oldValue != value)
                {
                    this[PdfName.IC] = (PdfArray)value?.DataObject;
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        public SKColor? InteriorSKColor
        {
            get => InteriorColor == null ? (SKColor?)null : DeviceColorSpace.CalcSKColor(InteriorColor, Alpha);
            set
            {
                var oldValue = InteriorSKColor;
                if (oldValue != value)
                {
                    InteriorColor = RGBColor.Get(value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /// <summary>Gets/Sets the border effect.</summary>
        [Pdf(VersionEnum.PDF15)]
        public BorderEffect BorderEffect
        {
            get => borderEffect ??= Get(PdfName.BE) is PdfDirectObject be
                ? new BorderEffect(be)
                : null;
            set
            {
                var oldValue = BorderEffect;
                if (!(oldValue?.Equals(value) ?? value == null))
                {
                    this[PdfName.BE] = (borderEffect = value)?.RefOrSelf;
                    QueueRefreshAppearance();
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        public SKPath ApplyBorderAndEffect(PrimitiveComposer composer, SKPath path)
        {
            ApplyBorder(composer);
            return BorderEffect?.Apply(composer, path) ?? path;
        }

        protected void ApplyBorder(PrimitiveComposer composer)
        {
            if (Border != null) Border.Apply(composer);
            else composer.SetLineWidth(1);
        }

        public virtual void ApplyBorderAndEffect(ref SKRect rect)
        {
            ApplyBorder(ref rect);
            BorderEffect?.Apply(ref rect);
        }

        protected void ApplyBorder(ref SKRect rect)
        {
            if (Border == null)
                rect.Inflate(1, 1);
            else
                Border.Apply(ref rect);
        }

        public virtual void InvertBorderAndEffect(ref SKRect rect)
        {
            InvertBorder(ref rect);
            BorderEffect?.Invert(ref rect);
        }

        protected void InvertBorder(ref SKRect rect)
        {
            if (Border == null)
            {
                if (rect.Width > 2 && rect.Height > 2)
                    rect.Inflate(-1, -1);
            }
            else
                Border.Invert(ref rect);
        }
    }

    // <summary>Annotation relationship [PDF:1.6:8.4.5].</summary>
    [Pdf(VersionEnum.PDF16)]
    public enum ReplyTypeEnum
    {
        Thread,
        Group
    }

}