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

using PdfGlown.Objects;
using System;

namespace PdfGlown.Documents.Interaction.Forms.Signature
{
    public class PropBuildDataDictionary : PdfObjectWrapper<PdfDictionary>
    {
        public PropBuildDataDictionary(PdfDocument doc)
            : base(doc, new PdfDictionary())
        { }

        public PropBuildDataDictionary(PdfDirectObject obj)
            : base(obj)
        { }

        public string Name
        {
            get => DataObject.GetString(PdfName.Name);
            set => DataObject.SetName(PdfName.Name, value);
        }

        public DateTime? Date
        {
            get => DataObject.GetDate(PdfName.Date);
            set => DataObject.Set(PdfName.Date, value);
        }

        public string Version
        {
            get => DataObject.GetString(PdfName.REx);
            set => DataObject.SetName(PdfName.REx, value);
        }

        public int Revision
        {
            get => DataObject.GetInt(PdfName.R);
            set => DataObject.Set(PdfName.R, value);
        }

        public bool PrePelease
        {
            get => DataObject.GetBool(PdfName.PreRelease);
            set => DataObject.Set(PdfName.PreRelease, value);
        }

        public string OS
        {
            get => DataObject.Get(PdfName.REx) is PdfDirectObject directObject
                ? directObject is PdfArray array 
                    ? array.GetString(0) 
                    : directObject is IPdfString pdfString 
                        ? pdfString.StringValue 
                        : null
                : null;
            set
            {
                var array = DataObject.GetOrCreateInderect<PdfArrayImpl>(PdfName.REx);
                array.SetName(0, value);
            }
        }

        public bool NonEFontNoWarn
        {
            get => DataObject.GetBool(PdfName.NonEFontNoWarn, true);
            set => DataObject.Set(PdfName.NonEFontNoWarn, value);
        }

        public bool TrustedMode
        {
            get => DataObject.GetBool(PdfName.TrueType, false);
            set => DataObject.Set(PdfName.TrustedMode, value);
        }
    }
}