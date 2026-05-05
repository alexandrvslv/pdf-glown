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
    /// <summary>Media player rules [PDF:1.7:9.1.6].</summary>
    [Pdf(VersionEnum.PDF15)]
    public sealed class MediaPlayers : PdfDictionary
    {
        private PdfArrayWrapper<MediaPlayerInfo> allowed;
        private PdfArrayWrapper<MediaPlayerInfo> forbidden;
        private PdfArrayWrapper<MediaPlayerInfo> required;

        public MediaPlayers()
            : this((PdfDocument)null)
        { }

        public MediaPlayers(PdfDocument context)
            : base(context, new())
        { }

        internal MediaPlayers(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets/Sets a set of players, any of which may be used in playing the associated media object.
        /// </summary>
        /// <remarks>This collection is ignored if <see cref="RequiredPlayers"/> is non-empty.</remarks>
        public PdfArrayWrapper<MediaPlayerInfo> AllowedPlayers
        {
            get => allowed ??= new(GetOrCreate<PdfArrayImpl>(PdfName.A));
            set => Set(PdfName.A, allowed = value);
        }

        /// <summary>Gets/Sets a set of players that must NOT be used in playing the associated media object.
        /// </summary>
        /// <remarks>This collection takes priority over <see cref="RequiredPlayers"/>.</remarks>
        public PdfArrayWrapper<MediaPlayerInfo> ForbiddenPlayers
        {
            get => forbidden ??= new(GetOrCreate<PdfArrayImpl>(PdfName.NU));
            set => Set(PdfName.NU, forbidden = value);
        }

        /// <summary>Gets/Sets a set of players, one of which must be used in playing the associated media object.
        /// </summary>
        public PdfArrayWrapper<MediaPlayerInfo> RequiredPlayers
        {
            get => required ??= new(GetOrCreate<PdfArrayImpl>(PdfName.MU));
            set => Set(PdfName.MU, required = value);
        }
    }
}