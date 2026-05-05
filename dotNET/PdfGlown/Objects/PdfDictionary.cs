/*
  Copyright 2006-2015 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfGlown.Bytes;
using PdfGlown.Documents;
using PdfGlown.Tokens;
using PdfGlown.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace PdfGlown.Objects
{
    /// <summary>PDF dictionary object [PDF:1.6:3.2.6].</summary>
    public class PdfDictionary : PdfDirectObject, IDictionary<PdfName, PdfDirectObject>, IBiDictionary<PdfName, PdfDirectObject>
    {
        private static readonly byte[] BeginDictionaryChunk = BaseEncoding.Pdf.Encode(Keyword.BeginDictionary);
        private static readonly byte[] EndDictionaryChunk = BaseEncoding.Pdf.Encode(Keyword.EndDictionary);

        internal Dictionary<PdfName, PdfDirectObject> entries;

        private PdfObject parent;
        private PdfObjectStatus status;

        /// <summary>Creates a new empty dictionary object with the default initial capacity.</summary>
        public PdfDictionary()
            : base(PdfObjectStatus.Updateable)
        {
            entries = new Dictionary<PdfName, PdfDirectObject>();
        }

        /// <summary>Creates a new empty dictionary object with the specified initial capacity.</summary>
        /// <param name="capacity">Initial capacity.</param>
        public PdfDictionary(int capacity)
            : base(PdfObjectStatus.Updateable)
        {
            entries = new Dictionary<PdfName, PdfDirectObject>(capacity);
        }

        /// <summary>Creates a new dictionary object with the specified entries.</summary>
        /// <param name="keys">Entry keys to add to this dictionary.</param>
        /// <param name="values">Entry values to add to this dictionary; their position and number must
        /// match the <code>keys</code> argument.</param>
        public PdfDictionary(PdfName[] keys, PdfDirectObject[] values)
            : this(values.Length)
        {
            Updateable = false;
            for (int index = 0; index < values.Length; index++)
            { this[keys[index]] = values[index]; }
            Updateable = true;
        }

        /// <summary>Creates a new dictionary object with the specified entries.</summary>
        /// <param name="objects">Sequence of key/value-paired objects (where key is a <see
        /// cref="PdfName"/> and value is a <see cref="PdfDirectObject"/>).</param>
        public PdfDictionary(params PdfDirectObject[] objects)
            : this(objects.Length / 2)
        {
            Updateable = false;
            for (int index = 0; index < objects.Length;)
            { this[(PdfName)objects[index++]] = objects[index++]; }
            Updateable = true;
        }

        /// <summary>Creates a new dictionary object with the specified entries.</summary>
        /// <param name="entries">Map whose entries have to be added to this dictionary.</param>
        internal PdfDictionary(Dictionary<PdfName, PdfDirectObject> entries)
            : base(PdfObjectStatus.Updateable)
        {
            this.entries = entries;
            foreach (var value in entries.Values)
                if (value != null)
                    value.ParentObject = this;
        }

        internal PdfDictionary(PdfDocument context, Dictionary<PdfName, PdfDirectObject> entries)
            : this(entries)
        {
            context?.Register(this);
        }

        public virtual PdfName ModifyTypeKey(PdfName key) => key;

        public PdfCatalog Catalog => Document?.Catalog;

        public override PdfObject ParentObject
        {
            get => parent;
            internal set => parent = value;
        }

        public override PdfObjectStatus Status
        {
            get => status;
            protected internal set => status = value;
        }

        public Dictionary<PdfName, PdfDirectObject>.KeyCollection Keys => entries.Keys;

        ICollection<PdfName> IDictionary<PdfName, PdfDirectObject>.Keys => Keys;

        public Dictionary<PdfName, PdfDirectObject>.ValueCollection Values => entries.Values;

        ICollection<PdfDirectObject> IDictionary<PdfName, PdfDirectObject>.Values => Values;

        public int Count => entries.Count;

        public bool IsReadOnly => false;

        public override PdfObject Accept(IVisitor visitor, PdfName parentKey, object data) => visitor.Visit(this, parentKey, data);

        public override int CompareTo(PdfDirectObject obj)
        {
            throw new NotImplementedException();
        }

        public T Get<T>(PdfName key)
            where T : class
        {
            return entries.TryGetValue(key, out var value)
                ? Resolve(key, value) as T
                : default;
        }

        public T Get<T>(PdfName key, T deaultValue)
            where T : class
        {
            return entries.TryGetValue(key, out var value)
                ? Resolve(key, value) as T ?? deaultValue
                : deaultValue;
        }

        /// <summary>Gets the value corresponding to the given key, forcing its instantiation in case of
        /// missing entry.</summary>
        /// <param name="key">Key whose associated value is to be returned.</param>
        /// <param name="direct">Whether the item has to be instantiated directly within its container
        /// instead of being referenced through an indirect object.</param>
        public T GetOrCreateInderect<T>(PdfName key)
            where T : PdfDirectObject, new()
        {
            return Get<T>(key) ?? Create<T>(key, false);
        }

        public T GetOrCreate<T>(PdfName key)
            where T : PdfDirectObject, new()
        {
            return Get<T>(key) ?? Create<T>(key, true);
        }

        public PdfDirectObject GetAnyOrCreate<T>(PdfName key, bool direct = false)
            where T : PdfDirectObject, new()
        {
            return Get(key) ?? Create<T>(key, direct).RefOrSelf;
        }

        public T Create<T>(PdfName key, bool direct)
            where T : PdfDirectObject, new()
        {
            T value;
            //NOTE: The null-object placeholder MUST NOT perturb the existing structure; therefore:
            //    - it MUST be marked as virtual in order not to unnecessarily serialize it;
            //    - it MUST be put into this dictionary without affecting its update status.
            try
            {
                value = new T();
                if (!direct)
                {
                    var reference = new PdfIndirectObject(Document, value, new XRefEntry(0, 0)).Reference;
                    reference.ParentObject = this;
                    entries[key] = reference;
                    reference.Virtual = true;
                }
                else
                {
                    value.ParentObject = this;
                    entries[key] = value;
                    value.Virtual = true;
                }
            }
            catch (Exception e)
            { throw new Exception(typeof(T).Name + " failed to instantiate.", e); }

            return value;
        }

        public override bool Equals(object @object)
        {
            return base.Equals(@object)
              || (@object != null
                && @object.GetType().Equals(GetType())
                && ((PdfDictionary)@object).entries.Equals(entries));
        }

        public override int GetHashCode() => entries.GetHashCode();

        object IBiDictionary.GetKey(object value) => value is PdfDirectObject tValue ? GetKey(tValue) : default(PdfName);

        /// <summary>Gets the key associated to the specified value.</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public PdfName GetKey(PdfDirectObject value)
        {
            // NOTE: Current PdfDictionary implementation doesn't support bidirectional maps, to say that
            //  the only currently-available way to retrieve a key from a value is to iterate the whole map
            //  (really poor performance!).
            foreach (KeyValuePair<PdfName, PdfDirectObject> entry in entries)
            {
                if (entry.Value.Equals(value))
                    return entry.Key;
            }
            return null;
        }

        public float GetFloat(PdfName key, float def = 0) => Get<IPdfNumber>(key)?.FloatValue ?? def;

        public float? GetNFloat(PdfName key, float? def = null) => Get<IPdfNumber>(key)?.FloatValue ?? def;

        public double GetDouble(PdfName key, double def = 0) => Get<IPdfNumber>(key)?.DoubleValue ?? def;

        public double? GetNDouble(PdfName key, double? def = null) => Get<IPdfNumber>(key)?.DoubleValue ?? def;

        public int GetInt(PdfName key, int def = 0) => Get<IPdfNumber>(key)?.IntValue ?? def;

        public int? GetNInt(PdfName key, int? def = null) => Get<IPdfNumber>(key)?.IntValue ?? def;

        public IPdfNumber GetNumber(PdfName key) => Get<IPdfNumber>(key);

        public bool GetBool(PdfName key, bool def = false) => Get<PdfBoolean>(key)?.RawValue ?? def;

        public bool? GetNBool(PdfName key, bool? def = null) => Get<PdfBoolean>(key)?.RawValue ?? def;

        public DateTime? GetDate(PdfName key) => Get<PdfDate>(key)?.DateValue;

        public DateTime? GetNDate(PdfName key) => Get<PdfDate>(key)?.DateValue ?? null;

        public Memory<byte> GetTextBytes(PdfName key) => Get<PdfString>(key)?.RawValue ?? Memory<byte>.Empty;

        public string GetString(PdfName key, string def = null) => Get<IPdfString>(key)?.StringValue ?? def;

        public void Set(PdfName key, double? value) => SetSimple(key, PdfReal.Get(value));

        public void Set(PdfName key, float? value) => SetSimple(key, PdfReal.Get(value));

        public void Set(PdfName key, int? value) => SetSimple(key, PdfInteger.Get(value));

        public void Set(PdfName key, long? value) => SetSimple(key, PdfInteger.Get(value));

        public void Set(PdfName key, bool? value) => SetSimple(key, PdfBoolean.Get(value));

        public void Set(PdfName key, DateTime? value) => SetSimple(key, PdfDate.Get(value));

        public void Set(PdfName key, double value) => SetSimple(key, PdfReal.Get(value));

        public void Set(PdfName key, float value) => SetSimple(key, PdfReal.Get(value));

        public void Set(PdfName key, int value) => SetSimple(key, PdfInteger.Get(value));

        public void Set(PdfName key, long value) => SetSimple(key, PdfInteger.Get(value));

        public void Set(PdfName key, bool value) => SetSimple(key, PdfBoolean.Get(value));

        public void Set(PdfName key, Memory<byte> data) => SetSimple(key, PdfString.Get(data));

        public void Set(PdfName key, string value) => SetSimple(key, PdfString.Get(value));

        public void SetName(PdfName key, string value) => SetSimple(key, PdfName.Get(value));

        public void SetText(PdfName key, string value) => SetSimple(key, PdfTextString.Get(value));

        public void Set(PdfName key, IPdfObjectWrapper value) => Set(key, value?.RefOrSelf);

        public void SetDirect(PdfName key, IPdfObjectWrapper value) => Set(key, value?.RefOrSelf?.Resolve());

        //public void Set(PdfName key, IPdfDataObject value) => Set(key, value?.RefOrSelf);

        //public void SetDirect(PdfName key, IPdfDataObject value) => this[key] = Resolve(key, value.RefOrSelf);

        public void Set(PdfName key, PdfDictionary value) => Set(key, value?.RefOrSelf);

        public void SetDirect(PdfName key, PdfDictionary value) => Set(key, value);

        public void Set(PdfName key, PdfArray value) => Set(key, value?.RefOrSelf);

        public void SetDirect(PdfName key, PdfArray value) => Set(key, value);

        public void Add(PdfName key, double? value) => AddSimple(key, PdfReal.Get(value));

        public void Add(PdfName key, float? value) => AddSimple(key, PdfReal.Get(value));

        public void Add(PdfName key, int? value) => AddSimple(key, PdfInteger.Get(value));

        public void Add(PdfName key, long? value) => AddSimple(key, PdfInteger.Get(value));

        public void Add(PdfName key, bool? value) => AddSimple(key, PdfBoolean.Get(value));

        public void Add(PdfName key, DateTime? value) => AddSimple(key, PdfDate.Get(value));

        public void Add(PdfName key, double value) => AddSimple(key, PdfReal.Get(value));

        public void Add(PdfName key, float value) => AddSimple(key, PdfReal.Get(value));

        public void Add(PdfName key, int value) => AddSimple(key, PdfInteger.Get(value));

        public void Add(PdfName key, long value) => AddSimple(key, PdfInteger.Get(value));

        public void Add(PdfName key, bool value) => AddSimple(key, PdfBoolean.Get(value));

        public void Add(PdfName key, Memory<byte> data) => AddSimple(key, PdfString.Get(data));

        public void Add(PdfName key, string value) => AddSimple(key, PdfString.Get(value));


        /// <summary>Gets the dereferenced value corresponding to the given key.</summary>
        /// <remarks>This method takes care to resolve the value returned by <see cref="this[PdfName]">
        /// this[PdfName]</see>.</remarks>
        /// <param name="key">Key whose associated value is to be returned.</param>
        /// <returns>null, if the map contains no mapping for this key.</returns>
        public virtual PdfDirectObject Resolve(PdfName key, PdfDirectObject value) => value?.Resolve(ModifyTypeKey(key));

        public override PdfObject Swap(PdfObject other)
        {
            var otherDictionary = (PdfDictionary)other;
            var otherEntries = otherDictionary.entries;
            // Update the other!
            otherDictionary.entries = this.entries;
            otherDictionary.Update();
            // Update this one!
            this.entries = otherEntries;
            this.Update();
            return this;
        }

        public override string ToString()
        {
            var buffer = new StringBuilder();
            {
                // Begin.
                buffer.Append("<< ");
                // Entries.
                foreach (KeyValuePair<PdfName, PdfDirectObject> entry in entries)
                {
                    // Entry...
                    // ...key.
                    buffer.Append(entry.Key.StringValue).Append(' ');
                    // ...value.
                    buffer.Append(ToString(entry.Value)).Append(' ');
                }
                // End.
                buffer.Append(">>");
            }
            return buffer.ToString();
        }

        public override void WriteTo(IOutputStream stream, PdfDocument context)
        {
            // Begin.
            stream.Write(BeginDictionaryChunk);
            // Entries.
            foreach (KeyValuePair<PdfName, PdfDirectObject> entry in entries)
            {
                PdfDirectObject value = entry.Value;
                if (value != null && value.Virtual)
                    continue;

                // Entry...
                // ...key.
                entry.Key.WriteTo(stream, context); stream.Write(Chunk.Space);
                // ...value.
                WriteTo(stream, context, value); stream.Write(Chunk.Space);
            }
            // End.
            stream.Write(EndDictionaryChunk);
        }

        public void AddSimple(PdfName key, PdfDirectObject value)
        {
            entries.Add(key, value);
            Update();
        }

        public void Add(PdfName key, PdfDirectObject value)
        {
            entries.Add(key, Include(value));
            Update();
        }

        public bool ContainsKey(object key) => ContainsKey((PdfName)key);

        public bool ContainsKey(PdfName key)
        {
            return entries.ContainsKey(key);
        }

        public bool Remove(PdfName key)
        {
            if (entries.Remove(key, out var oldValue))
            {
                Exclude(oldValue);
                Update();
                return true;
            }
            return false;
        }

        public void SetSimple(PdfName key, PdfDirectObject value)
        {
            if (value == null)
            { Remove(key); }
            else
            {
                PdfDirectObject oldValue = Get(key);
                entries[key] = value;
                Exclude(oldValue);
                Update();
            }
        }

        public void Set(PdfName key, PdfDirectObject value)
        {
            if (value == null)
            { Remove(key); }
            else
            {
                PdfDirectObject oldValue = Get(key);
                entries[key] = Include(value);
                Exclude(oldValue);
                Update();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PdfDirectObject Get(PdfName key) => entries.TryGetValue(key, out var value) ? value : null;

        public PdfDirectObject this[PdfName key]
        {
            get => Get(key);
            set => Set(key, value);
        }        

        object IBiDictionary.this[object key]
        {
            get => Get((PdfName)key);
            set => Set((PdfName)key, (PdfDirectObject)value);
        }

        public bool TryGetValue(PdfName key, out PdfDirectObject value) => entries.TryGetValue(key, out value);

        void ICollection<KeyValuePair<PdfName, PdfDirectObject>>.Add(KeyValuePair<PdfName, PdfDirectObject> entry) => Add(entry.Key, entry.Value);

        public void Clear()
        {
            foreach (PdfName key in entries.Keys.ToList())
            {
                Remove(key);
            }
        }

        bool ICollection<KeyValuePair<PdfName, PdfDirectObject>>.Contains(KeyValuePair<PdfName, PdfDirectObject> entry)
        {
            return entries.TryGetValue(entry.Key, out var value) && value.Equals(entry.Value);
        }

        public void CopyTo(KeyValuePair<PdfName, PdfDirectObject>[] entries, int index)
        {
            foreach (var entry in this)
            {
                entries[index++] = entry;
            }
        }

        public bool Remove(KeyValuePair<PdfName, PdfDirectObject> entry)
        {
            if (entry.Value.Equals(Get(entry.Key)))
                return Remove(entry.Key);
            else
                return false;
        }

        public Dictionary<PdfName, PdfDirectObject>.Enumerator GetEnumerator() => entries.GetEnumerator();

        IEnumerator<KeyValuePair<PdfName, PdfDirectObject>> IEnumerable<KeyValuePair<PdfName, PdfDirectObject>>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    }
}