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

namespace PdfGlown.Documents.Interaction.Forms
{
    /// <summary>Field option [PDF:1.6:8.6.3].</summary>
    [Pdf(VersionEnum.PDF12)]
    public sealed class ChoiceItem : PdfObjectWrapper<PdfDirectObject>
    {
        private ChoiceItems items;

        public ChoiceItem(string value)
            : base(new PdfTextString(value))
        { }

        public ChoiceItem(PdfDocument context, string value, string text)
            : base(context, new PdfArrayImpl(2) { new PdfTextString(value), new PdfTextString(text) })
        { }

        internal ChoiceItem(PdfDirectObject baseObject, ChoiceItems items)
            : base(baseObject)
        { Items = items; }

        //TODO:make the class immutable (to avoid needing wiring it up to its collection...)!!!
        /// <summary>Gets/Sets the displayed text.</summary>
        public string Text
        {
            get
            {
                PdfDirectObject baseDataObject = DataObject;
                if (baseDataObject is PdfArray array) // <value,text> pair.
                    return array.GetString(1);
                else // Single text string.
                    return ((IPdfString)baseDataObject).StringValue;
            }
            set
            {
                PdfDirectObject baseDataObject = DataObject;
                if (baseDataObject is PdfTextString pdfString)
                {
                    RefOrSelf = baseDataObject = new PdfArrayImpl(2) { pdfString, PdfTextString.Default };

                    if (items != null)
                    {
                        // Force list update!
                        /*
                          NOTE: This operation is necessary in order to substitute
                          the previous base object with the new one within the list.
                        */
                        PdfArray itemsObject = items.DataObject;
                        itemsObject.Set(itemsObject.IndexOf(pdfString), baseDataObject);
                    }
                }
                ((PdfArray)baseDataObject).SetText(1, value);
            }
        }

        /// <summary>Gets/Sets the export value.</summary>
        public string Value
        {
            get
            {
                var baseDataObject = DataObject;
                if (DataObject is PdfArray array) // <value,text> pair.
                    return array.GetString(0);
                else // Single text string.
                    return ((IPdfString)baseDataObject).StringValue;
            }
            set
            {
                var baseDataObject = DataObject;
                if (baseDataObject is PdfArray array) // <value,text> pair.
                { array.SetText(0, value); }
                else // Single text string.
                { RefOrSelf = new PdfTextString(value); }
            }
        }

        internal ChoiceItems Items
        {
            set
            {
                if (items != null)
                    throw new ArgumentException("Item already associated to another choice field.");

                items = value;
            }
        }
    }
}