/*
  Copyright 2006-2013 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfGlown.Tokens;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using static PdfGlown.Documents.Functions.Type4.BitwiseOperators;
using static PdfGlown.Documents.Functions.Type4.StackOperators;

namespace PdfGlown.Objects
{
    /// <summary>PDF array object, that is a one-dimensional collection of (possibly-heterogeneous)
    /// objects arranged sequentially [PDF:1.7:3.2.5].</summary>
    public abstract class PdfArray : PdfDirectObject
    {
        private static readonly byte[] BeginArrayChunk = BaseEncoding.Pdf.Encode(Keyword.BeginArray);
        private static readonly byte[] EndArrayChunk = BaseEncoding.Pdf.Encode(Keyword.EndArray);
        internal static readonly PdfArray Empty = new PdfArrayImpl(new List<PdfDirectObject>());

        public static bool SequenceEquals(PdfArray oldValue, PdfArray newValue)
        {
            if (oldValue == newValue)
                return true;
            return oldValue != null
                && newValue != null
                && oldValue.items.SequenceEqual(newValue.items);
        }

        internal List<PdfDirectObject> items;
        private PdfObject parent;
        private PdfObjectStatus status;

        public PdfArray()
             : base(PdfObjectStatus.Updateable)
        {
            items = new List<PdfDirectObject>();
        }

        public PdfArray(int capacity)
            : base(PdfObjectStatus.Updateable)

        {
            items = new List<PdfDirectObject>(capacity);
        }

        internal PdfArray(List<PdfDirectObject> items)
            : base(PdfObjectStatus.Updateable)
        {
            this.items = items;
            foreach (var item in items)
                if (item != null)
                    item.ParentObject = this;
        }

        internal PdfArray(PdfDocument context, List<PdfDirectObject> items)
           : this(items)
        {
            context?.Register(this);
        }

        public PdfArray(params PdfDirectObject[] items)
            : this(items.Length)
        {
            Updateable = false;
            AddRange(items);
            Updateable = true;
        }

        public PdfArray(IEnumerable<PdfDirectObject> items)
            : this(items.Count())
        {
            Updateable = false;
            AddRange(items);
            Updateable = true;
        }

        public PdfArray(ICollection<int> items)
            : this(items.Count)
        {
            Updateable = false;
            AddRangeDirect(items.Select(x => PdfInteger.Get(x)));
            Updateable = true;
        }

        public PdfArray(ICollection<float> items)
            : this(items.Count)
        {
            Updateable = false;
            AddRangeDirect(items.Select(x => PdfReal.Get(x)));
            Updateable = true;
        }

        public PdfArray(ICollection<double> items)
            : this(items.Count)
        {
            Updateable = false;
            AddRangeDirect(items.Select(x => PdfReal.Get(x)));
            Updateable = true;
        }

        public PdfArray(ICollection<byte[]> source)
            : this(source.Count)
        {
            Updateable = false;
            AddRangeDirect(source.Select(p => PdfString.Get(p)));
            Updateable = true;
        }

        public PdfArray(ICollection<string> source)
            : this(source.Count)
        {
            Updateable = false;
            AddRangeDirect(source.Select(p => PdfString.Get(p)));
            Updateable = true;
        }

        public virtual PdfName TypeKey { get => null; }

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

        public int Count => items.Count;

        public bool IsReadOnly => false;

        public int Capacity { get => items.Capacity; }

        public override PdfObject Accept(IVisitor visitor, PdfName parentKey, object data) => visitor.Visit(this, parentKey, data);

        public override int CompareTo(PdfDirectObject obj)
        {
            throw new NotImplementedException();
        }

        public override bool Equals(object @object)
        {
            return base.Equals(@object)
              || (@object != null
                && @object.GetType().Equals(GetType())
                && ((PdfArray)@object).items.Equals(items));
        }

        public IPdfNumber GetNumber(int index) => Get<IPdfNumber>(index);

        public float GetFloat(int index, float def = 0) => Get<IPdfNumber>(index)?.FloatValue ?? def;

        public float? GetNFloat(int index, float? def = null) => Get<IPdfNumber>(index)?.FloatValue ?? def;

        public double GetDouble(int index, double def = 0) => Get<IPdfNumber>(index)?.RawValue ?? def;

        public double? GetNDouble(int index, double? def = null) => Get<IPdfNumber>(index)?.RawValue ?? def;

        public int GetInt(int index, int def = 0) => Get<IPdfNumber>(index)?.IntValue ?? def;

        public int? GetNInt(int index, int? def = null) => Get<IPdfNumber>(index)?.IntValue ?? def;

        public bool GetBool(int index, bool def = false) => Get<PdfBoolean>(index)?.RawValue ?? def;

        public bool? GetNBool(int index, bool? def = null) => Get<PdfBoolean>(index)?.RawValue ?? def;

        public string GetString(int index, string def = null) => Get<IPdfString>(index)?.StringValue ?? def;

        public void Set(int index, bool? value) => SetSimple(index, PdfBoolean.Get(value));

        public void Set(int index, float? value) => SetSimple(index, PdfReal.Get(value));

        public void Set(int index, double? value) => SetSimple(index, PdfReal.Get(value));

        public void Set(int index, int? value) => SetSimple(index, PdfInteger.Get(value));

        public void Set(int index, long? value) => SetSimple(index, PdfInteger.Get(value));

        public void Set(int index, bool value) => SetSimple(index, PdfBoolean.Get(value));

        public void Set(int index, float value) => SetSimple(index, PdfReal.Get(value));

        public void Set(int index, double value) => SetSimple(index, PdfReal.Get(value));

        public void Set(int index, int value) => SetSimple(index, PdfInteger.Get(value));

        public void Set(int index, long value) => SetSimple(index, PdfInteger.Get(value));

        public void SetText(int index, string value) => SetSimple(index, PdfTextString.Get(value));

        public void SetName(int index, string value) => SetSimple(index, PdfName.Get(value));

        public void Add(bool? value) => AddSimple(PdfBoolean.Get(value));

        public void Add(float? value) => AddSimple(PdfReal.Get(value));

        public void Add(double? value) => AddSimple(PdfReal.Get(value));

        public void Add(int? value) => AddSimple(PdfInteger.Get(value));

        public void Add(long? value) => AddSimple(PdfInteger.Get(value));

        public void Add(DateTime? value) => AddSimple(PdfDate.Get(value));

        public void Add(bool value) => AddSimple(PdfBoolean.Get(value));

        public void Add(float value) => AddSimple(PdfReal.Get(value));

        public void Add(double value) => AddSimple(PdfReal.Get(value));

        public void Add(int value) => AddSimple(PdfInteger.Get(value));

        public void Add(long value) => AddSimple(PdfInteger.Get(value));

        public void Add(string value) => AddSimple(PdfTextString.Get(value));

        public void Add(byte[] value) => AddSimple(PdfTextString.Get(value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PdfDirectObject Get(int index) => items[index];

        public T Get<T>(int index, PdfName parentKey = null)
            where T : class => Resolve(items[index], parentKey) as T;

        /// <summary>Gets the value corresponding to the given index, forcing its instantiation in case
        /// of missing entry.</summary>
        /// <param name="index">Index of the item to return.</param>
        /// <param name="direct">Whether the item has to be instantiated directly within its container
        /// instead of being referenced through an indirect object.</param>
        public T GetOrCreate<T>(int index, bool direct = true)
            where T : PdfDirectObject, new()
        {
            var item = index < Count ? Get<T>(index) : null;
            return item ?? Create<T>(index, direct);
        }

        private T Create<T>(int index, bool direct)
            where T : PdfDirectObject, new()
        {
            //NOTE: The null-object placeholder MUST NOT perturb the existing structure; therefore:
            //   - it MUST be marked as virtual in order not to unnecessarily serialize it;
            //   - it MUST be put into this array without affecting its update status.
            var item = new T();
            try
            {
                PdfDirectObject toPlace = direct ? item : new PdfIndirectObject(Document, item, new XRefEntry(0, 0)).Reference;
                toPlace.ParentObject = this;

                if (index == Count)
                    items.Add(toPlace);
                else
                    items.Insert(index, toPlace);
                toPlace.Virtual = true;
            }
            catch (Exception e)
            {
                throw new Exception(typeof(T).Name + " failed to instantiate.", e);
            }

            return item;
        }

        public override int GetHashCode() => items.GetHashCode();

        public virtual PdfDirectObject Resolve(PdfDirectObject item, PdfName typeKey = null)
            => item?.Resolve(typeKey ?? TypeKey);

        public override PdfObject Swap(PdfObject other)
        {
            var otherArray = (PdfArray)other;
            var otherItems = otherArray.items;
            // Update the other!
            otherArray.items = items;
            otherArray.Update();
            // Update this one!
            items = otherItems;
            Update();
            return this;
        }

        public override string ToString()
        {
            var buffer = new StringBuilder();
            // Begin.
            buffer.Append('['); buffer.Append(' ');
            // Elements.
            foreach (var item in items)
            {
                buffer.Append(ToString(item)).Append(' ');
            }
            // End.
            buffer.Append(']');
            return buffer.ToString();
        }

        public override void WriteTo(IOutputStream stream, PdfDocument context)
        {
            // Begin.
            stream.Write(BeginArrayChunk);
            // Elements.
            foreach (var item in items)
            {
                if (item != null && item.Virtual)
                    continue;

                WriteTo(stream, context, item);
                stream.Write(Chunk.Space);
            }
            // End.
            stream.Write(EndArrayChunk);
        }

        public int IndexOf(PdfDirectObject item) => items.IndexOf(item);

        public void Insert(int index, PdfDirectObject item)
        {
            items.Insert(index, Include(item));
            Update();
        }

        public virtual void RemoveAt(int index)
        {
            var oldItem = items[index];
            items.RemoveAt(index);
            Exclude(oldItem);
            Update();
        }

        public bool Remove(PdfDirectObject item)
        {
            if (!items.Remove(item))
                return false;

            Exclude(item);
            Update();
            return true;
        }

        internal void SetSimple(int index, PdfDirectObject value)
        {
            var oldItem = items[index];
            items[index] = value;
            Exclude(oldItem);
            Update();
        }

        public void Set(int index, PdfDirectObject value)
        {
            var oldItem = items[index];
            items[index] = Include(value?.RefOrSelf);
            Exclude(oldItem);
            Update();
        }
        
        internal void AddSimple(PdfDirectObject item)
        {
            items.Add(item);
            Update();
        }

        public void Add(PdfDirectObject item)
        {
            items.Add(Include(item));
            Update();
        }

        internal void AddRangeDirect(IEnumerable<PdfDirectObject> source)
        {
            items.AddRange(source);
            Update();
        }

        public void AddRange(IEnumerable<PdfDirectObject> source)
        {
            items.AddRange(source.Select(x => Include(x)));
            Update();
        }

        public void Clear()
        {
            int index = Count;
            while (index-- > 0)
            { RemoveAt(index); }

            //int index = Count;
            //while (items.Count > 0)
            //{ RemoveAt(0); }
        }

        public bool Contains(PdfDirectObject item) => items.Contains(item);

        public void CopyTo(PdfDirectObject[] items, int index) => this.items.CopyTo(items, index);       

        public int[] ToIntArray()
        {
            var newArray = new int[Count];
            {
                for (int i = 0; i < Count; i++)
                {
                    newArray[i] = GetInt(i);
                }
            }
            return newArray;
        }

        public float[] ToFloatArray()
        {
            var newArray = new float[Count];
            {
                for (int i = 0; i < Count; i++)
                {
                    newArray[i] = GetFloat(i);
                }
            }
            return newArray;
        }

        public string[] ToStringArray()
        {
            var newArray = new string[Count];
            {
                for (int i = 0; i < Count; i++)
                {
                    newArray[i] = GetString(i);
                }
            }
            return newArray;
        }

        public List<string> ToStringList()
        {
            var newArray = new List<string>(Count);
            {
                for (int i = 0; i < Count; i++)
                {
                    newArray.Add(GetString(i));
                }
            }
            return newArray;
        }

        public PdfDirectObject TryGet(int i)
        {
            return i < Count ? items[i] : null;
        }

        public virtual IEnumerable<PdfDirectObject> GetItems() => items;
    }    
}