/*
  Copyright 2011-2013 Stefano Chizzolini. http://www.pdfclown.org

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

using System;
using System.Collections;
using System.Collections.Generic;

namespace PdfGlown.Objects
{
    /// <summary>Collection of sequentially-arranged object wrappers.</summary>
    public class ArrayWrapper<TItem> : PdfObjectWrapper<PdfArray>, IList<TItem>
        where TItem : IPdfObjectWrapper
    {
        protected IEntryWrapper<TItem> itemWrapper;

        /// <summary>Wraps a new base array using the specified wrapper for wrapping its items.</summary>
        /// <param name="context">Document context.</param>
        /// <param name="itemWrapper">Item wrapper.</param>
        public ArrayWrapper(PdfDocument context, IEntryWrapper<TItem> itemWrapper)
            : this(context, new PdfArrayImpl(), itemWrapper)
        { }

        /// <summary>Wraps the specified base array using the specified wrapper for wrapping its items.</summary>
        /// <param name="context">Document context.</param>
        /// <param name="itemWrapper">Item wrapper.</param>
        /// <param name="baseDataObject">Base array.</param>
        public ArrayWrapper(PdfDocument context, PdfArray baseDataObject, IEntryWrapper<TItem> itemWrapper)
            : base(context, baseDataObject)
        {
            this.itemWrapper = itemWrapper;
        }

        /// <summary>Wraps an existing base array using the specified wrapper for wrapping its items.</summary>
        /// <param name="itemWrapper">Item wrapper.</param>
        /// <param name="baseObject">Base array. MUST be a <see cref="PdfReference">reference</see>
        /// everytime available.</param>
        public ArrayWrapper(PdfDirectObject baseObject, IEntryWrapper<TItem> itemWrapper)
            : base(baseObject)
        {
            this.itemWrapper = itemWrapper;
        }

        public virtual int IndexOf(TItem item) => DataObject.IndexOf(item.RefOrSelf);

        public virtual void Insert(int index, TItem item) => DataObject.Insert(index, item.RefOrSelf);

        public virtual void RemoveAt(int index) => DataObject.RemoveAt(index);

        public virtual TItem this[int index]
        {
            get => itemWrapper.Wrap(DataObject.Get(index));
            set => DataObject.Set(index, value.RefOrSelf);
        }

        public virtual void Add(TItem item) => DataObject.Add(item.RefOrSelf);

        public virtual void Clear()
        {
            int index = Count;
            while (index-- > 0)
            { RemoveAt(index); }
        }

        public virtual bool Contains(TItem item) => DataObject.Contains(item.RefOrSelf);

        public virtual void CopyTo(TItem[] items, int index)
        {
            foreach (TItem entry in this)
            {
                items[index++] = entry;
            }
        }

        public virtual int Count => DataObject.Count;

        public virtual bool IsReadOnly => false;

        public virtual bool Remove(TItem item) => DataObject.Remove(item.RefOrSelf);

        public virtual Enumerator GetEnumerator() => new(this);

        IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<TItem>, IEnumerator, IDisposable
        {
            private List<PdfDirectObject>.Enumerator enumer;
            private IEntryWrapper<TItem> wrapper;

            internal Enumerator(ArrayWrapper<TItem> items)
            {
                this.wrapper = items.itemWrapper;
                this.enumer = items.DataObject.items.GetEnumerator();
            }

            public TItem Current => wrapper.Wrap(enumer.Current);

            object IEnumerator.Current => Current;

            public void Dispose() => enumer.Dispose();

            public bool MoveNext() => enumer.MoveNext();

            public void Reset() => ((IEnumerator)enumer).Reset();
        }
    }
}