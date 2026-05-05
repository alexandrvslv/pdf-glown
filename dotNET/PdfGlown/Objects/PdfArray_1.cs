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
    public abstract class PdfArray<TItem> : PdfArray, IList<TItem>
        where TItem : PdfDirectObject
    {
        /// <summary>Wraps a new base array using the default wrapper for wrapping its items.</summary>
        /// <param name="context">Document context.</param>
        public PdfArray(PdfDocument context)
            : this(context, new ())
        { }

        /// <summary>Wraps the specified base array using the default wrapper for wrapping its items.</summary>
        /// <param name="context">Document context.</param>
        /// <param name="baseDataObject">Base array.</param>
        public PdfArray(PdfDocument context, List<PdfDirectObject> baseDataObject)
            : base(context, baseDataObject)
        { }

        /// <summary>Wraps an existing base array using the default wrapper for wrapping its items.</summary>
        /// <param name="baseObject">Base array. MUST be a <see cref="PdfReference">reference</see>
        /// everytime available.</param>
        internal PdfArray(List<PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        protected virtual TItem CheckIn(TItem item) => item;

        protected virtual TItem CheckOut(TItem item) => item;

        public int IndexOf(TItem item) => base.IndexOf(item.RefOrSelf);

        public void Insert(int index, TItem item) => base.Insert(index, CheckIn(item).RefOrSelf);

        public TItem this[int index]
        {
            get => Get<TItem>(index, TypeKey);
            set => Set(index, CheckIn(value));
        }

        public void Add(TItem item) => base.Add(CheckIn(item).RefOrSelf);

        public bool Contains(TItem item) => base.Contains(item.RefOrSelf);

        public virtual void CopyTo(TItem[] items, int index)
        {
            foreach (TItem entry in this)
            {
                items[index++] = entry;
            }
        }

        public virtual bool Remove(TItem item) => base.Remove(CheckOut(item).RefOrSelf);

        public override void RemoveAt(int index)
        {
            CheckOut(this[index]);
            base.RemoveAt(index);
        }

        public Enumerator GetEnumerator() => new (this);

        IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public struct Enumerator : IEnumerator<TItem>, IEnumerator, IDisposable
        {
            private List<PdfDirectObject>.Enumerator enumer;
            private PdfName typeKey;

            internal Enumerator(PdfArray<TItem> array)
            {
                this.enumer = array.items.GetEnumerator();
                this.typeKey = array.TypeKey;
            }

            public TItem Current => enumer.Current?.Resolve(typeKey) as TItem;

            object IEnumerator.Current => Current;

            public void Dispose() => enumer.Dispose();

            public bool MoveNext() => enumer.MoveNext();

            public void Reset() => ((IEnumerator)enumer).Reset();
        }
    }
}