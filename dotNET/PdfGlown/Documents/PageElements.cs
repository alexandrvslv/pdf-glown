/*
  Copyright 2012-2015 Stefano Chizzolini. http://www.pdfclown.org

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

namespace PdfGlown.Documents
{
    /// <summary>Page elements.</summary>
    public abstract class PageElements<TItem> : PdfArray<TItem>
        where TItem : PdfDictionary
    {
        private PdfPage page;

        protected PageElements(PdfDocument document)
            : base(document)
        { }

        protected PageElements(List<PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets the page associated to these elements.</summary>
        public PdfPage Page
        {
            get => page;
            set => page = value;
        }

        protected override TItem CheckIn(TItem item)
        {
            LinkPage(item);
            return base.CheckIn(item);
        }

        protected override TItem CheckOut(TItem item)
        {
            UnlinkPage(item);
            return base.CheckOut(item);
        }

        protected internal void LinkPage(TItem item)
        {
            // Link the element to its page!
            item[PdfName.P] = page.Reference;
        }

        protected internal void LinkPageNoUpdate(TItem item)
        {
            // Link the element to its page!
            var temp = item.Updateable;
            item.Updateable = false;
            LinkPage(item);
            item.Updateable = temp;
        }

        protected void UnlinkPage(TItem item)
        {
            // Unlink the element from its page!
            item?.Remove(PdfName.P);
        }
    }
}