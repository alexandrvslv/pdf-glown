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

using PdfGlown.Documents.Contents.ColorSpaces;
using PdfGlown.Documents.Contents.XObjects;
using PdfGlown.Objects;
using System.Collections.Generic;

namespace PdfGlown.Documents.Interaction.Annotations
{

    /// <summary>Appearance characteristics [PDF:1.6:8.4.5].</summary>
    [Pdf(VersionEnum.PDF12)]
    public sealed class AppearanceCharacteristics : PdfDictionary
    {
        private DeviceColor backgroundColor;
        private DeviceColor borderColor;

        /// <summary>Annotation orientation [PDF:1.6:8.4.5].</summary>
        public enum OrientationEnum
        {
            /// <summary>Upward.</summary>
            Up = 0,
            /// <summary>Leftward.</summary>
            Left = 90,
            /// <summary>Downward.</summary>
            Down = 180,
            /// <summary>Rightward.</summary>
            Right = 270
        };

        public AppearanceCharacteristics()
            : this((PdfDocument)null)
        { }

        public AppearanceCharacteristics(PdfDocument context)
            : base(context, new())
        { }

        internal AppearanceCharacteristics(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets/Sets the widget annotation's alternate (down) caption,
        /// displayed when the mouse button is pressed within its active area
        /// (Pushbutton fields only).</summary>
        public string AlternateCaption
        {
            get => GetString(PdfName.AC);
            set => SetText(PdfName.AC, value);
        }

        /// <summary>Gets/Sets the widget annotation's alternate (down) icon definition,
        /// displayed when the mouse button is pressed within its active area
        /// (Pushbutton fields only).</summary>
        public FormXObject AlternateIcon
        {
            get => Get<FormXObject>(PdfName.IX);
            set => this[PdfName.IX] = value.RefOrSelf;
        }

        /// <summary>Gets/Sets the widget annotation's background color.</summary>
        public DeviceColor BackgroundColor
        {
            get => backgroundColor ??= GetColor(PdfName.BG);
            set => SetColor(PdfName.BG, backgroundColor = value);
        }

        /// <summary>Gets/Sets the widget annotation's border color.</summary>
        public DeviceColor BorderColor
        {
            get => borderColor ??= GetColor(PdfName.BC);
            set => SetColor(PdfName.BC, borderColor = value);
        }

        /// <summary>Gets/Sets the position of the caption relative to its icon (Pushbutton fields only).</summary>
        public AppearanceCaptionPosition CaptionPosition
        {
            get => (AppearanceCaptionPosition)GetInt(PdfName.TP);
            set => Set(PdfName.TP, (int)value);
        }

        /// <summary>Gets/Sets the icon fit specifying how to display the widget annotation's icon
        /// within its annotation box (Pushbutton fields only).
        /// If present, the icon fit applies to all of the annotation's icons
        /// (normal, rollover, and alternate).</summary>
        public IconFitObject IconFit
        {
            get => Get<IconFitObject>(PdfName.IF);
            set => Set(PdfName.IF, value);
        }

        /// <summary>Gets/Sets the widget annotation's normal caption,
        /// displayed when it is not interacting with the user (Button fields only).</summary>
        public string NormalCaption
        {
            get => GetString(PdfName.CA);
            set => SetText(PdfName.CA, value);
        }

        /// <summary>Gets/Sets the widget annotation's normal icon definition,
        /// displayed when it is not interacting with the user (Pushbutton fields only).</summary>
        public FormXObject NormalIcon
        {
            get => Get<FormXObject>(PdfName.I);
            set => this[PdfName.I] = value?.RefOrSelf;
        }

        /// <summary>Gets/Sets the widget annotation's orientation.</summary>
        public OrientationEnum Orientation
        {
            get => (OrientationEnum)GetInt(PdfName.R);
            set => Set(PdfName.R, (int)value);
        }

        /// <summary>Gets/Sets the widget annotation's rollover caption,
        /// displayed when the user rolls the cursor into its active area
        /// without pressing the mouse button (Pushbutton fields only).</summary>
        public string RolloverCaption
        {
            get => GetString(PdfName.RC);
            set => SetText(PdfName.RC, value);
        }

        /// <summary>Gets/Sets the widget annotation's rollover icon definition,
        /// displayed when the user rolls the cursor into its active area
        /// without pressing the mouse button (Pushbutton fields only).</summary>
        public FormXObject RolloverIcon
        {
            get => Get<FormXObject>(PdfName.RI);
            set => Set(PdfName.RI, value);
        }

        private DeviceColor GetColor(PdfName key) => DeviceColor.Get(Get<PdfArray>(key));

        private void SetColor(PdfName key, DeviceColor value) => Set(key, value);
    }

    /// <summary>Caption position relative to its icon [PDF:1.6:8.4.5].</summary>
    public enum AppearanceCaptionPosition
    {
        /// <summary>Caption only (no icon).</summary>
        CaptionOnly = 0,
        /// <summary>No caption (icon only).</summary>
        NoCaption = 1,
        /// <summary>Caption below the icon.</summary>
        Below = 2,
        /// <summary>Caption above the icon.</summary>
        Above = 3,
        /// <summary>Caption to the right of the icon.</summary>
        Right = 4,
        /// <summary>Caption to the left of the icon.</summary>
        Left = 5,
        /// <summary>Caption overlaid directly on the icon.</summary>
        Overlaid = 6
    };

}