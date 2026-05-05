/*
  Copyright 2010-2012 Stefano Chizzolini. http://www.pdfclown.org

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
using System.Linq;

namespace PdfGlown.Objects
{
    public partial class PdfDictionary<TValue> where TValue : PdfDirectObject
    {
        public class ValueCollection: ICollection<TValue>
        {
            private readonly PdfDictionary<TValue> dict;

            public ValueCollection(PdfDictionary<TValue> dict)
            {
                this.dict = dict;
            }

            public int Count => dict.Count;

            public bool IsReadOnly => true;

            public void Add(TValue item) => throw new NotSupportedException();

            public bool Remove(TValue item) => throw new NotSupportedException();

            public void Clear() => throw new NotSupportedException();

            public bool Contains(TValue item) => Enumerable.Contains(this, item);

            public void CopyTo(TValue[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public IEnumerator<TValue> GetEnumerator() => new Enumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private struct Enumerator : IEnumerator<TValue>, IDisposable
            {
                private PdfDictionary<TValue> dict;
                private Dictionary<PdfName, PdfDirectObject>.Enumerator enumer;

                public Enumerator(PdfDictionary<TValue> dict)
                {
                    this.dict = dict;
                    this.enumer = dict.entries.GetEnumerator();
                }

                public TValue Current => (TValue)dict.Resolve(enumer.Current.Key, enumer.Current.Value);

                object IEnumerator.Current => Current;

                public void Dispose() => enumer.Dispose();

                public bool MoveNext() => enumer.MoveNext();

                public void Reset()
                {
                    enumer.Dispose();
                    enumer = dict.entries.GetEnumerator();
                }
            }
        }
    }
}