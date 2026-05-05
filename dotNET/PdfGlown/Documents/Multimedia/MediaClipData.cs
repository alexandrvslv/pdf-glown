/*
  Copyright 2012 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfGlown.Util;

using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Multimedia
{
    /// <summary>Media clip data [PDF:1.7:9.1.3].</summary>
    [Pdf(VersionEnum.PDF15)]
    public sealed class MediaClipData : MediaClip
    {
        internal static readonly BiDictionary<TempFilePermissionEnum, PdfString> permissinCodes = new()
        {
            [TempFilePermissionEnum.Never] = new PdfString("TEMPNEVER"),
            [TempFilePermissionEnum.ContentExtraction] = new PdfString("TEMPEXTRACT"),
            [TempFilePermissionEnum.Accessibility] = new PdfString("TEMPACCESS"),
            [TempFilePermissionEnum.Always] = new PdfString("TEMPALWAYS")
        };

        internal static TempFilePermissionEnum? GetPermissin(PdfString code)
        {
            if (code == null)
                return null;

            TempFilePermissionEnum? tempFilePermission = permissinCodes.GetKey(code);
            if (!tempFilePermission.HasValue)
                throw new NotSupportedException("Operation unknown: " + code);

            return tempFilePermission;
        }

        internal static PdfString GetCode(TempFilePermissionEnum tempFilePermission) => permissinCodes[tempFilePermission];

        private IPdfDataObject data;
        private Viability preferences;
        private Viability requirements;

        /// <summary>Circumstance under which it is acceptable to write a temporary file in order to play
        /// a media clip.</summary>
        public enum TempFilePermissionEnum
        {
            /// <summary>Never allowed.</summary>
            Never,
            /// <summary>Allowed only if the document permissions allow content extraction.</summary>
            ContentExtraction,
            /// <summary>Allowed only if the document permissions allow content extraction, including for
            /// accessibility purposes.</summary>
            Accessibility,
            /// <summary>Always allowed.</summary>
            Always
        }

        /// <summary>Media clip data viability.</summary>
        public class Viability : PdfObjectWrapper<PdfDictionary>
        {
            public Viability(PdfDirectObject baseObject) : base(baseObject)
            { }

            /// <summary>Gets the absolute URL to be used as the base URL in resolving any relative URLs
            /// found within the media data.</summary>
            public Uri BaseURL
            {
                get
                {
                    var baseURLObject = DataObject.GetString(PdfName.BU);
                    return baseURLObject != null ? new Uri(baseURLObject) : null;
                }
                set => DataObject.Set(PdfName.BU, value?.ToString());
            }
        }

        public MediaClipData(IPdfDataObject data, string mimeType)
            : base(data.RefOrSelf.Document, PdfName.MCD)
        {
            Data = data;
            MimeType = mimeType;
            TempFilePermission = TempFilePermissionEnum.Always;
        }

        internal MediaClipData(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        public override IPdfDataObject Data
        {
            get => data ??= Get(PdfName.D) is PdfDirectObject dataObject
                    ? dataObject.Resolve(PdfName.D) is FormXObject formX
                        ? formX
                        : IFileSpecification.Wrap(dataObject)
                    : null;
            set => Set(PdfName.D, (data = value).RefOrSelf);
        }

        /// <summary>Gets/Sets the MIME type of data [RFC 2045].</summary>
        public string MimeType
        {
            get => GetString(PdfName.CT);
            set => Set(PdfName.CT, value);
        }

        /// <summary>Gets/Sets the player rules for playing this media.</summary>
        public MediaPlayers Players
        {
            get => GetOrCreate<MediaPlayers>(PdfName.PL);
            set => Set(PdfName.PL, value);
        }

        /// <summary>Gets/Sets the preferred options the renderer should attempt to honor without affecting its
        /// viability.</summary>
        public Viability Preferences
        {
            get => preferences ??= new(GetOrCreate<PdfDictionary>(PdfName.BE));
            set => Set(PdfName.BE, preferences = value);
        }

        /// <summary>Gets/Sets the minimum requirements the renderer must honor in order to be considered viable.
        /// </summary>
        public Viability Requirements
        {
            get => requirements ??= new(GetOrCreate<PdfDictionary>(PdfName.MH));
            set => Set(PdfName.MH, requirements = value);
        }

        /// <summary>Gets/Sets the circumstance under which it is acceptable to write a temporary file in order
        /// to play this media clip.</summary>
        public TempFilePermissionEnum? TempFilePermission
        {
            get => GetPermissin(GetOrCreate<PdfDictionary>(PdfName.P).Get<PdfString>(PdfName.TF));
            set => GetOrCreate<PdfDictionary>(PdfName.P)[PdfName.TF] = (value.HasValue ? GetCode(value.Value) : null);
        }
    }    
}