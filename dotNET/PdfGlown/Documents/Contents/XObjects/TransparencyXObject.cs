/*
  Copyright 2006-2015 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)
    * Alexandr

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
using PdfGlown.Objects;
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents.XObjects
{
    public sealed class TransparencyXObject : GroupXObject
    {
        private ColorSpace colorSpace;

        public TransparencyXObject(PdfDocument context)
            : base(context)
        {
            SubType = PdfName.Transparency;
        }

        internal TransparencyXObject(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        public ColorSpace ColorSpace
        {
            get => colorSpace ??= ColorSpace.Wrap(Get(PdfName.CS));
            set => Set(PdfName.CS, value = colorSpace);
        }

        public bool Isolated
        {
            get => GetBool(PdfName.I);
            set => Set(PdfName.I, value);
        }

        public bool Knockout
        {
            get => GetBool(PdfName.K);
            set => Set(PdfName.K, value);
        }
    }
}