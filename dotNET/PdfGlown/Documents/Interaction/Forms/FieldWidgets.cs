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

using PdfGlown.Documents.Interaction.Annotations;
using PdfGlown.Objects;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PdfGlown.Documents.Interaction.Forms
{
    /// <summary>Field widget annotations [PDF:1.6:8.6].</summary>
    [Pdf(VersionEnum.PDF12)]
    public sealed class FieldWidgets : PdfObjectWrapper<PdfDirectObject>, IList<Widget>
    {
        // NOTE: Widget annotations may be singular (either merged to their field or within an array)
        // or multiple (within an array).
        // This implementation hides such a complexity to the user, smoothly exposing just the most
        // general case (array) yet preserving its internal state.
        private Field field;

        internal FieldWidgets(PdfDirectObject baseObject, Field field)
            : base(baseObject)
        {
            this.field = field;
        }

        public override object Clone(PdfDocument context)
        { throw new NotImplementedException(); } // TODO:verify field reference.

        /// <summary>Gets the field associated to these widgets.</summary>
        public Field Field => this.field;

        public int IndexOf(Widget value)
        {
            var baseDataObject = DataObject;
            if (baseDataObject is PdfDictionary) // Single annotation.
            {
                if (value.Reference.Equals(RefOrSelf))
                    return 0;
                else
                    return -1;
            }

            return ((PdfArray)baseDataObject).IndexOf(value.Reference);
        }

        public void Insert(int index, Widget value) => EnsureArray().Insert(index, value.Reference);

        public void RemoveAt(int index) => EnsureArray().RemoveAt(index);

        public Widget this[int index]
        {
            get
            {
                var baseDataObject = DataObject;
                if (baseDataObject is Widget widget) // Single annotation.
                {
                    if (index != 0)
                        throw new ArgumentException("Index: " + index + ", Size: 1");

                    return widget;
                }

                return ((PdfArray)baseDataObject).Get<Widget>(index, PdfName.Action);
            }
            set => EnsureArray().Set(index, value);
        }

        public void Add(Widget value)
        {
            value[PdfName.Parent] = Field.RefOrSelf;
            EnsureAnnotations(value);
            EnsureArray().Add(value.Reference);
        }

        private static void EnsureAnnotations(Widget value)
        {
            if (!(value.Page?.Annotations.Contains(value) ?? true))
                value.Page.Annotations.Add(value);
        }

        public void Clear() => EnsureArray().Clear();

        public bool Contains(Widget value)
        {
            PdfDirectObject baseDataObject = DataObject;
            if (baseDataObject is PdfDictionary) // Single annotation.
                return value.Reference.Equals(RefOrSelf);

            return ((PdfArray)baseDataObject).Contains(value.Reference);
        }

        public void CopyTo(Widget[] values, int index)
        {
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = this[index + i];
            }
        }

        public int Count
        {
            get
            {
                PdfDirectObject baseDataObject = DataObject;
                if (baseDataObject is PdfDictionary) // Single annotation.
                    return 1;

                return ((PdfArray)baseDataObject).Count;
            }
        }

        public bool IsReadOnly => false;

        public bool Remove(Widget value) => EnsureArray().Remove(value.Reference);

        IEnumerator<Widget> IEnumerable<Widget>.GetEnumerator()
        {
            for (int index = 0, length = Count; index < length; index++)
            { yield return this[index]; }
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<Widget>)this).GetEnumerator();

        private PdfArray EnsureArray()
        {
            var baseDataObject = DataObject;
            if (baseDataObject is Widget mergedDictionary) // Merged annotation.
            {
                var widgetsArray = new PdfArrayImpl();
                {
                    var separetedDictionary = new Widget(mergedDictionary.Page);

                    // Remove the field from the page annotations (as the widget annotation is decoupled from it)!
                    var pageAnnotationsArray = mergedDictionary.Page.Annotations;
                    pageAnnotationsArray.Remove(mergedDictionary);

                    // Add the widget to the page annotations!
                    pageAnnotationsArray.Add(separetedDictionary);
                    // Add the widget to the field widgets!
                    widgetsArray.Add(separetedDictionary.Reference);
                    // Associate the field to the widget!
                    separetedDictionary[PdfName.Parent] = Field.RefOrSelf;
                    // Extracting widget entries from the field...
                    foreach (PdfName key in mergedDictionary.Keys.ToList())
                    {
                        // Is it a widget entry?
                        if (key.Equals(PdfName.Type)
                          || key.Equals(PdfName.Subtype)
                          || key.Equals(PdfName.Rect)
                          || key.Equals(PdfName.Contents)
                          || key.Equals(PdfName.P)
                          || key.Equals(PdfName.NM)
                          || key.Equals(PdfName.M)
                          || key.Equals(PdfName.F)
                          || key.Equals(PdfName.BS)
                          || key.Equals(PdfName.AP)
                          || key.Equals(PdfName.AS)
                          || key.Equals(PdfName.Border)
                          || key.Equals(PdfName.C)
                          || key.Equals(PdfName.A)
                          || key.Equals(PdfName.AA)
                          || key.Equals(PdfName.StructParent)
                          || key.Equals(PdfName.OC)
                          || key.Equals(PdfName.H)
                          || key.Equals(PdfName.MK))
                        {

                            // Transfer the entry from the field to the widget!
                            separetedDictionary.Set(key, mergedDictionary.Get(key));
                            mergedDictionary.Remove(key);
                        }
                    }
                }
                RefOrSelf = widgetsArray;
                Field.DataObject[PdfName.Kids] = widgetsArray;

                baseDataObject = widgetsArray;
            }

            return (PdfArray)baseDataObject;
        }

    }
}