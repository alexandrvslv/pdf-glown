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

using PdfGlown.Documents.Contents.XObjects;
using PdfGlown.Objects;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Interaction.Annotations
{
    /// <summary>Sound annotation[PDF:1.6:8.4.5].</summary>
    /// <remarks>When the annotation is activated, the sound is played.</remarks>
    [Pdf(VersionEnum.PDF12)]
    public sealed class Sound : Markup
    {
        /// <summary>Icon to be used in displaying the annotation [PDF:1.6:8.4.5].</summary>
        public enum IconTypeEnum
        {
            /// <summary>Speaker.</summary>
            Speaker,
            /// <summary>Microphone.</summary>
            Microphone
        };

        private static readonly IconTypeEnum DefaultIconType = IconTypeEnum.Speaker;

        private static readonly Dictionary<IconTypeEnum, PdfName> IconTypeEnumCodes;

        static Sound()
        {
            IconTypeEnumCodes = new Dictionary<IconTypeEnum, PdfName>();
            IconTypeEnumCodes[IconTypeEnum.Speaker] = PdfName.Speaker;
            IconTypeEnumCodes[IconTypeEnum.Microphone] = PdfName.Mic;
        }

        /// <summary>Gets the code corresponding to the given value.</summary>
        private static PdfName ToCode(IconTypeEnum value)
        { return IconTypeEnumCodes[value]; }

        /// <summary>Gets the icon type corresponding to the given value.</summary>
        private static IconTypeEnum ToIconTypeEnum(string value)
        {
            if (value == null)
                return DefaultIconType;
            foreach (KeyValuePair<IconTypeEnum, PdfName> iconType in IconTypeEnumCodes)
            {
                if (string.Equals(iconType.Value.StringValue, value, StringComparison.Ordinal))
                    return iconType.Key;
            }
            return DefaultIconType;
        }

        public Sound(PdfPage page, SKRect box, string text, Multimedia.Sound content)
            : base(page, PdfName.Sound, box, text)
        { Content = content; }

        internal Sound(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets/Sets the sound to be played.</summary>
        public Multimedia.Sound Content
        {
            get => Get<Multimedia.Sound>(PdfName.Sound);
            set
            {
                if (value == null)
                    throw new ArgumentException("Content MUST be defined.");

                Set(PdfName.Sound, value);
            }
        }

        /// <summary>Gets/Sets the icon to be used in displaying the annotation.</summary>
        public IconTypeEnum IconType
        {
            get => ToIconTypeEnum(this.GetString(PdfName.Name));
            set => this[PdfName.Name] = value != DefaultIconType ? ToCode(value) : null;
        }

        /// <summary>Popups not supported.</summary>
        public override Popup Popup
        {
            set => throw new NotSupportedException();
        }

        protected override FormXObject GenerateAppearance()
        {
            return null;
        }
    }
}