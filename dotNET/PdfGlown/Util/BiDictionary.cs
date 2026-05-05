/*
  Copyright 2009-2012 Stefano Chizzolini. http://www.pdfclown.org

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

namespace PdfGlown.Util
{
    /// <summary>Bidirectional bijective map.</summary>
    public class BiDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IBiDictionary<TKey, TValue>
    {
        private static IEqualityComparer<T> GetEqualityComparer<T>()
        {
            if (typeof(T) == typeof(string))
                return (IEqualityComparer<T>)(object)StringComparer.Ordinal;
            return EqualityComparer<T>.Default;
        }

        private readonly Dictionary<TKey, TValue> dictionary;
        private readonly Dictionary<TValue, TKey> inverseDictionary;

        public BiDictionary()
            : this(GetEqualityComparer<TKey>(), GetEqualityComparer<TValue>())
        {
            dictionary = new Dictionary<TKey, TValue>();
            inverseDictionary = new Dictionary<TValue, TKey>();
        }

        public BiDictionary(IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer)
        {
        }

        public BiDictionary(int capacity)
        {
            dictionary = new Dictionary<TKey, TValue>(capacity);
            inverseDictionary = new Dictionary<TValue, TKey>(capacity);
        }

        public BiDictionary(IDictionary<TKey, TValue> dictionary)
        {
            this.dictionary = new Dictionary<TKey, TValue>(dictionary);
            //TODO: key duplicate collisions to resolve!
            //       inverseDictionary = this.dictionary.ToDictionary(entry => entry.Value, entry => entry.Key);
            inverseDictionary = new Dictionary<TValue, TKey>();
            foreach (KeyValuePair<TKey, TValue> entry in this.dictionary)
            { inverseDictionary[entry.Value] = entry.Key; }
        }

        public bool ContainsValue(TValue value) => inverseDictionary.ContainsKey(value);

        public object GetKey(object value) => value is TValue tvalue ? GetKey(tvalue) : default(TKey);

        public virtual TKey GetKey(TValue value) => inverseDictionary.TryGetValue(value, out var key) ? key : default;

        public void Add(TKey key, TValue value)
        {
            dictionary.Add(key, value); // Adds the entry.
            try
            { inverseDictionary.Add(value, key); } // Adds the inverse entry.
            catch (Exception)
            {
                dictionary.Remove(key); // Reverts the entry addition.
                throw;
            }
        }

        public bool ContainsKey(object key) => ContainsKey((TKey)key);

        public bool ContainsKey(TKey key) => dictionary.ContainsKey(key);

        public ICollection<TKey> Keys => dictionary.Keys;

        public bool Remove(TKey key)
        {
            TValue value;
            if (dictionary.Remove(key, out value))
            {
                inverseDictionary.Remove(value);
                return true;
            }
            return false;
        }

        public virtual TValue this[TKey key]
        {
            get
            {
                /*
                  NOTE: This is an intentional violation of the official .NET Framework Class Library
                  prescription.
                  My loose implementation emphasizes coding smoothness and concision, against ugly
                  TryGetValue() invocations: unfound keys are happily dealt with returning a default (null)
                  value.
                  If the user is interested in verifying whether such result represents a non-existing key
                  or an actual null object, it suffices to query ContainsKey() method.
                */
                return dictionary.TryGetValue(key, out var value) ? value : default(TValue);
            }
            set
            {
                TValue oldValue;
                dictionary.TryGetValue(key, out oldValue);
                dictionary[key] = value; // Sets the entry.
                if (oldValue != null)
                { inverseDictionary.Remove(oldValue); }
                inverseDictionary[value] = key; // Sets the inverse entry.
            }
        }

        object IBiDictionary.this[object key]
        {
            get => this[(TKey)key];
            set => this[(TKey)key] = (TValue)value;
        }

        public bool TryGetValue(TKey key, out TValue value) => dictionary.TryGetValue(key, out value);

        public ICollection<TValue> Values => dictionary.Values;

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> keyValuePair) => Add(keyValuePair.Key, keyValuePair.Value);

        public void Clear()
        {
            dictionary.Clear();
            inverseDictionary.Clear();
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> keyValuePair) => dictionary.Contains(keyValuePair);

        public void CopyTo(KeyValuePair<TKey, TValue>[] keyValuePairs, int index)
        { throw new NotImplementedException(); }

        public virtual int Count => dictionary.Count;

        public bool IsReadOnly => false;

        public bool Remove(KeyValuePair<TKey, TValue> keyValuePair)
        {
            if (!((ICollection<KeyValuePair<TKey, TValue>>)dictionary).Remove(keyValuePair))
                return false;

            inverseDictionary.Remove(keyValuePair.Value);
            return true;
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => dictionary.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<KeyValuePair<TKey, TValue>>)this).GetEnumerator();
    }
}