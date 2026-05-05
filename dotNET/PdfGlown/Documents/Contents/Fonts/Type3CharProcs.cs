/*
  Copyright 2010-2015 Stefano Chizzolini. http://www.pdfclown.org

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
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents.Fonts
{
    public class Type3CharProcs : PdfDictionary<Type3CharProc>
    {
        private PdfType3Font font;

        public Type3CharProcs()
            : base((PdfDocument)null)
        { }

        internal Type3CharProcs(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        public override PdfName ModifyTypeKey(PdfName key) => PdfName.CharProc;

        public override PdfDirectObject Resolve(PdfName key, PdfDirectObject value)
        {
            var item = base.Resolve(key, value);
            if (item is Type3CharProc charProc)
            {
                charProc.Font = font;
            }
            return item;
        }

        internal Type3CharProcs WithFont(PdfType3Font fontType3)
        {
            font = fontType3;
            return this;
        }
    }
}