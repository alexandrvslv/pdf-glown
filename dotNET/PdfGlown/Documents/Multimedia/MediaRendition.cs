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

using PdfGlown.Objects;
using System.Collections.Generic;

namespace PdfGlown.Documents.Multimedia
{
    /// <summary>Media rendition [PDF:1.7:9.1.2].</summary>
    [Pdf(VersionEnum.PDF15)]
    public sealed class MediaRendition : Rendition
    {
        public MediaRendition(MediaClip clip)
            : base(clip.Document, PdfName.MR)
        {
            Clip = clip;
        }

        internal MediaRendition(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets/Sets the content to be played.</summary>
        public MediaClip Clip
        {
            get => Get<MediaClip>(PdfName.C);
            set => Set(PdfName.C, value);
        }

        /// <summary>Gets/Sets the parameters that specify how this media rendition should be played.
        /// </summary>
        public MediaPlayParameters PlayParameters
        {
            get => GetOrCreate<MediaPlayParameters>(PdfName.P);
            set => Set(PdfName.P, value);
        }

        /// <summary>Gets/Sets the parameters that specify where the media rendition object should be
        /// played.<summary>
        public MediaScreenParameters ScreenParameters
        {
            get => GetOrCreate<MediaScreenParameters>(PdfName.SP);
            set => Set(PdfName.SP, value);
        }
    }
}