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
    public class PdfArrayWrapper<T> : PdfObjectWrapper<PdfArray>, IList<T>
        where T : PdfDirectObject
    {
        /// <summary>Wraps a new base array using the specified wrapper for wrapping its items.</summary>
        /// <param name="context">Document context.</param>
        /// <param name="itemWrapper">Item wrapper.</param>
        public PdfArrayWrapper(PdfDocument context)
            : this(context, new PdfArrayImpl())
        { }

        /// <summary>Wraps the specified base array using the specified wrapper for wrapping its items.</summary>
        /// <param name="context">Document context.</param>
        /// <param name="itemWrapper">Item wrapper.</param>
        /// <param name="baseDataObject">Base array.</param>
        public PdfArrayWrapper(PdfDocument context, PdfArray baseDataObject)
            : base(context, baseDataObject)
        { }

        /// <summary>Wraps an existing base array using the specified wrapper for wrapping its items.</summary>
        /// <param name="itemWrapper">Item wrapper.</param>
        /// <param name="baseObject">Base array. MUST be a <see cref="PdfReference">reference</see>
        /// everytime available.</param>
        public PdfArrayWrapper(PdfDirectObject baseObject)
            : base(baseObject)
        { }

        public virtual int Count => DataObject.Count;

        public virtual bool IsReadOnly => false;


        protected virtual T CheckIn(T item) => item;

        protected virtual T CheckOut(T item) => item;

        public virtual int IndexOf(T item) => DataObject.IndexOf(item.RefOrSelf);

        public virtual void Insert(int index, T item) => DataObject.Insert(index, CheckIn(item).RefOrSelf);

        public virtual bool Remove(T item) => DataObject.Remove(CheckOut(item).RefOrSelf);

        public virtual void RemoveAt(int index)
        {
            CheckOut(this[index]);
            DataObject.RemoveAt(index);
        }

        public virtual T this[int index]
        {
            get => DataObject.Get<T>(index);
            set => DataObject.Set(index, CheckIn(value));
        }

        public virtual void Add(T item) => DataObject.Add(CheckIn(item).RefOrSelf);

        public virtual void Clear()
        {
            int index = Count;
            while (index-- > 0)
            { RemoveAt(index); }
        }

        public virtual bool Contains(T item) => DataObject.Contains(item.RefOrSelf);

        public virtual void CopyTo(T[] items, int index)
        {
            foreach (T entry in this)
            {
                items[index++] = entry;
            }
        }

        public virtual Enumerator GetEnumerator() => new(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<T>, IEnumerator, IDisposable
        {
            private PdfName typeKey;
            private List<PdfDirectObject>.Enumerator enumer;

            internal Enumerator(PdfArrayWrapper<T> items)
            {
                this.typeKey = items.DataObject.TypeKey;
                this.enumer = items.DataObject.items.GetEnumerator();
            }

            public T Current => (T)enumer.Current.Resolve(typeKey);

            object IEnumerator.Current => Current;

            public void Dispose() => enumer.Dispose();

            public bool MoveNext() => enumer.MoveNext();

            public void Reset() => ((IEnumerator)enumer).Reset();
        }
    }
}