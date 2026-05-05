/*
  Copyright 2015 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)

  This file should be part of the source code distribution of "PDF Glown library"
  (the Program): see the accompanying README files for more info.

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

using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Interchange.Metadata
{
    /// <summary>A page-piece dictionary used to hold private application data [PDF:1.7:10.4].</summary>
    [Pdf(VersionEnum.PDF13)]
    public sealed class AppDataCollection : PdfDictionary<AppData>
    {
        private IAppDataHolder holder;

        public AppDataCollection()
            : base(new Dictionary<PdfName, PdfDirectObject>())
        { }

        internal AppDataCollection(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        public override PdfName ModifyTypeKey(PdfName key) => PdfName.AppData;

        public AppData Ensure(PdfName key)
        {
            AppData appData = this[key];
            if (appData == null)
            {
                Set(key, appData = new AppData(Document));
                holder?.Touch(key);
            }
            return appData;
        }

        public override AppData this[PdfName key]
        {
            get => base[key];
            set => throw new NotSupportedException();
        }

        public override void Add(PdfName key, AppData value) => throw new NotSupportedException();

        internal AppDataCollection WithHolder(IAppDataHolder holder)
        {
            this.holder = holder;
            return this;
        }
    }


}

