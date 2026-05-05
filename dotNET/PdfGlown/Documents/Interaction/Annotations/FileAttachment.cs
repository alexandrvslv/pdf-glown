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
using PdfGlown.Documents.Files;
using PdfGlown.Objects;
using PdfGlown.Tools;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Interaction.Annotations
{
    /// <summary>File attachment annotation [PDF:1.6:8.4.5].</summary>
    /// <remarks>It represents a reference to a file, which typically is embedded in the PDF file.
    /// </remarks>
    [Pdf(VersionEnum.PDF13)]
    public sealed class FileAttachment : Markup, IFileResource
    {
        private static readonly Dictionary<FileAttachmentImageType, PdfName> IconTypeEnumCodes = new()
        {
            [FileAttachmentImageType.Graph] = PdfName.Graph,
            [FileAttachmentImageType.PaperClip] = PdfName.Paperclip,
            [FileAttachmentImageType.PushPin] = PdfName.PushPin,
            [FileAttachmentImageType.Tag] = PdfName.Tag
        };

        private static readonly FileAttachmentImageType DefaultIconType = FileAttachmentImageType.PushPin;
        private IFileSpecification fs;

        /// <summary>Gets the code corresponding to the given value.</summary>
        private static PdfName ToCode(FileAttachmentImageType value)
        {
            return IconTypeEnumCodes[value];
        }

        /// <summary>Gets the icon type corresponding to the given value.</summary>
        private static FileAttachmentImageType ToImageTypeEnum(IPdfString value)
        {
            if (value == null)
                return DefaultIconType;
            foreach (KeyValuePair<FileAttachmentImageType, PdfName> iconType in IconTypeEnumCodes)
            {
                if (string.Equals(iconType.Value.StringValue, value.StringValue, StringComparison.Ordinal))
                    return iconType.Key;
            }
            return DefaultIconType;
        }

        public FileAttachment(PdfPage page, SKRect box, string text, IFileSpecification dataFile)
            : base(page, PdfName.FileAttachment, box, text)
        {
            DataFile = dataFile;
        }

        public FileAttachment(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets/Sets the icon to be used in displaying the annotation.</summary>
        public FileAttachmentImageType AttachmentName
        {
            get => ToImageTypeEnum(Get<IPdfString>(PdfName.Name));
            set
            {
                var oldvalue = AttachmentName;
                if (oldvalue != value)
                {
                    this[PdfName.Name] = value != DefaultIconType ? ToCode(value) : null;
                    OnPropertyChanged(oldvalue, value);
                }
            }
        }

        public IFileSpecification DataFile
        {
            get => fs ??= IFileSpecification.Wrap(Get(PdfName.FS));
            set
            {
                var oldValue = DataFile;
                if (oldValue != value)
                {
                    Set(PdfName.FS, (fs = value).RefOrSelf);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        public override bool AllowSize => false;

        public override SKRect RefreshAppearance(SKCanvas canvas)
        {
            var bounds = Box;
            var color = SKColor;
            SvgImage.DrawImage(canvas, AttachmentName.ToString(), color, bounds, 1);
            return bounds;
        }

        protected override FormXObject GenerateAppearance()
        {
            return null;
        }
    }

    /// <summary>Icon to be used in displaying the annotation [PDF:1.6:8.4.5].</summary>
    public enum FileAttachmentImageType
    {
        /// <summary>Graph.</summary>
        Graph,
        /// <summary>Paper clip.</summary>
        PaperClip,
        /// <summary>Push pin.</summary>
        PushPin,
        /// <summary>Tag.</summary>
        Tag
    };

}