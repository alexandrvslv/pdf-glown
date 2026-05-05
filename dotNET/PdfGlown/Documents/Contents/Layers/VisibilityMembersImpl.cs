/*
  Copyright 2011-2015 Stefano Chizzolini. http://www.pdfclown.org

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
using System.Collections;
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents.Layers
{
    /// <summary>Layers whose states determine the visibility of content controlled by a membership.</summary>
    internal class VisibilityMembersImpl : PdfObjectWrapper<PdfDirectObject>, IList<Layer>
    {
        private LayerMembership membership;

        internal VisibilityMembersImpl(LayerMembership membership)
            : base(membership.Get(PdfName.OCGs))
        { this.membership = membership; }

        public int IndexOf(Layer item)
        {
            var baseDataObject = DataObject;
            if (baseDataObject == null) // No layer.
                return -1;
            else if (baseDataObject is Layer) // Single layer.
                return item.Reference.Equals(RefOrSelf) ? 0 : -1;
            else // Multiple layers.
                return ((PdfArray)baseDataObject).IndexOf(item.Reference);
        }

        public void Insert(int index, Layer item) => EnsureArray().Insert(index, item.Reference);

        public void RemoveAt(int index) => EnsureArray().RemoveAt(index);

        public Layer this[int index]
        {
            get
            {
                PdfDirectObject baseDataObject = DataObject;
                if (baseDataObject == null) // No layer.
                    return null;
                else if (baseDataObject is Layer layer) // Single layer.
                {
                    if (index != 0)
                        throw new IndexOutOfRangeException();

                    return layer;
                }
                else // Multiple layers.
                    return ((PdfArray)baseDataObject).Get<Layer>(index);
            }
            set => EnsureArray().Set(index, value);
        }

        public void Add(Layer item) => EnsureArray().Add(item.Reference);

        public void Clear() => EnsureArray().Clear();

        public bool Contains(Layer item)
        {
            var dataObject = DataObject;
            if (dataObject == null) // No layer.
                return false;
            else if (dataObject is Layer) // Single layer.
                return item.Reference.Equals(RefOrSelf);
            else // Multiple layers.
                return ((PdfArray)dataObject).Contains(item.Reference);
        }

        public void CopyTo(Layer[] items, int index)
        { throw new NotImplementedException(); }

        public int Count
        {
            get
            {
                var dataObject = DataObject;
                if (dataObject == null) // No layer.
                    return 0;
                else if (dataObject is PdfDictionary) // Single layer.
                    return 1;
                else // Multiple layers.
                    return ((PdfArray)dataObject).Count;
            }
        }

        public bool IsReadOnly => false;

        public bool Remove(Layer item) => EnsureArray().Remove(item.Reference);

        public IEnumerator<Layer> GetEnumerator()
        {
            for (int index = 0, length = Count; index < length; index++)
            { yield return this[index]; }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        private PdfArray EnsureArray()
        {
            var baseDataObject = DataObject;
            if (baseDataObject is not PdfArray)
            {
                var array = new PdfArrayImpl();
                if (baseDataObject != null)
                { array.Add(baseDataObject); }
                RefOrSelf = baseDataObject = array;
                membership[PdfName.OCGs] = RefOrSelf;
            }
            return (PdfArray)baseDataObject;
        }
    }
}