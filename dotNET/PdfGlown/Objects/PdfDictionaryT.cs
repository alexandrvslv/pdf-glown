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

using PdfGlown.Util;
using System;
using System.Collections;
using System.Collections.Generic;

namespace PdfGlown.Objects
{
    public partial class PdfDictionary<TValue> : PdfDictionary, IDictionary<PdfName, TValue>, IDictionary, IBiDictionary<PdfName, TValue>
        where TValue : PdfDirectObject
    {
        private ValueCollection values;
        protected PdfDictionary(PdfDocument context)
            : base(context, new())
        { }

        public PdfDictionary(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        ICollection<PdfName> IDictionary<PdfName, TValue>.Keys => base.Keys;

        public new ICollection<TValue> Values => values ??= new ValueCollection(this);        

        bool IDictionary.IsFixedSize => false;

        ICollection IDictionary.Keys => (ICollection)Keys;

        ICollection IDictionary.Values => (ICollection)Values;

        bool ICollection.IsSynchronized => true;

        object ICollection.SyncRoot => entries;

        object IBiDictionary.GetKey(object value) => value is TValue tValue ? GetKey(tValue) : null;

        ///Gets the key associated to a given value.
        public PdfName GetKey(TValue value) => base.GetKey(value.RefOrSelf);

        public virtual void Add(PdfName key, TValue value) => base.Add(key, value?.RefOrSelf);

        public new virtual TValue this[PdfName key]
        {
            get => base.Get<TValue>(key);
            set => base.Set(key, value?.RefOrSelf);
        }

        object IDictionary.this[object key]
        {
            get => this[(PdfName)key];
            set => this[(PdfName)key] = (TValue)value;
        }

        object IBiDictionary.this[object key]
        {
            get => this[(PdfName)key];
            set => this[(PdfName)key] = (TValue)value;
        }

        public bool TryGetValue(PdfName key, out TValue value)
        {
            var result = base.TryGetValue(key, out var bValue);
            value = result ? Resolve(key, bValue) as TValue : null;
            return result;
        }

        void ICollection<KeyValuePair<PdfName, TValue>>.Add(KeyValuePair<PdfName, TValue> entry)
            => Add(entry.Key, entry.Value);

        bool ICollection<KeyValuePair<PdfName, TValue>>.Contains(KeyValuePair<PdfName, TValue> entry)
            => entry.Value.RefOrSelf.Equals(Get(entry.Key));

        public void CopyTo(KeyValuePair<PdfName, TValue>[] entries, int index)
        {
            foreach (var entry in this)
            {
                entries[index++] = entry;
            }
        }

        public bool Remove(KeyValuePair<PdfName, TValue> entry)
            => base.Remove(new KeyValuePair<PdfName, PdfDirectObject>(entry.Key, entry.Value.RefOrSelf));

        public new DictionaryEnumerator GetEnumerator() => new(this);

        IEnumerator<KeyValuePair<PdfName, TValue>> IEnumerable<KeyValuePair<PdfName, TValue>>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        IDictionaryEnumerator IDictionary.GetEnumerator() => GetEnumerator();

        void IDictionary.Add(object key, object value) => Add((PdfName)key, (TValue)value);

        bool IDictionary.Contains(object key) => ContainsKey((PdfName)key);

        void IDictionary.Remove(object key) => Remove((PdfName)key);

        void ICollection.CopyTo(Array array, int index)
        {
            int lIndex = index;
            foreach (var entry in this)
            {
                array.SetValue(entry, lIndex++);
            }
        }

        public struct DictionaryEnumerator : IEnumerator<KeyValuePair<PdfName, TValue>>, IDisposable, IEnumerator, IDictionaryEnumerator
        {
            private PdfName key;
            private TValue value;
            private Dictionary<PdfName, PdfDirectObject>.Enumerator enumer;
            private PdfDictionary<TValue> dictionary;
            internal DictionaryEnumerator(PdfDictionary<TValue> dictionary)
            {
                key = null;
                value = default;
                this.dictionary = dictionary;
                enumer = dictionary.entries.GetEnumerator();
            }

            public DictionaryEntry Entry => new(key, value);

            public object Key => Key;

            public object Value => Value;

            public KeyValuePair<PdfName, TValue> Current => new(key, value);

            object IEnumerator.Current => Entry;

            public bool MoveNext()
            {
                if (!enumer.MoveNext())
                {
                    key = null;
                    value = default;
                    return false;
                }
                var dictEntry = enumer.Current;
                if (PdfName.Metadata.Equals(dictEntry.Key))
                    return MoveNext();
                key = dictEntry.Key;
                value = dictionary.Resolve(key, dictEntry.Value) as TValue;
                return true;
            }

            public void Reset() => ((IEnumerator)enumer).Reset();

            public void Dispose() => enumer.Dispose();

        }
    }
}