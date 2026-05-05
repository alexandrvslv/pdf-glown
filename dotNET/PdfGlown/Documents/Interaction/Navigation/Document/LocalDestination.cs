/*
  Copyright 2008-2012 Stefano Chizzolini. http://www.pdfclown.org

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

namespace PdfGlown.Documents.Interaction.Navigation
{
    /// <summary>Local interaction target [PDF:1.6:8.2.1].</summary>
    [Pdf(VersionEnum.PDF10)]
    public sealed class LocalDestination : Destination
    {
        public LocalDestination(PdfPage page)
            : this(page, ModeEnum.Fit, null, null)
        { }

        public LocalDestination(PdfPage page, ModeEnum mode, object location, double? zoom)
            : base(page.Document, page, mode, location, zoom)
        { }

        internal LocalDestination(List<PdfDirectObject> baseObject) 
            : base(baseObject)
        { }

        /// <summary>Gets/Sets the target page.</summary>
        public override object Page
        {
            get => Get<PdfPage>(0, PdfName.Page);
            set
            {
                if (value is not PdfPage page)
                    throw new ArgumentException("It MUST be a Page object.");

                Set(0, page);
            }
        }        
    }
}