/*
  Copyright 2007-2010 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfGlown.Documents.Contents.ColorSpaces;
using PdfGlown.Objects;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PdfGlown.Documents.Contents.Objects
{
    /// <summary>Inline image entries (anonymous) operation [PDF:1.6:4.8.6].</summary>
    /// <remarks>This is a figurative operation necessary to constrain the inline image entries section
    /// within the content stream model.</remarks>
    [Pdf(VersionEnum.PDF10)]
    public sealed class InlineImageHeader : Operation, IDictionary<PdfName, PdfDirectObject>
    {
        private float[] decode;
        private ICollection<PdfName> keys;

        // [FIX:0.0.4:2] Null operator.
        public InlineImageHeader(PdfArray operands) 
            : base(String.Empty, operands)
        { }

        public void Add(PdfName key, PdfDirectObject value)
        {
            if (ContainsKey(key))
                throw new ArgumentException("Key '" + key + "' already in use.", nameof(key));

            this[key] = value;
        }

        public int BitsPerComponent
        {
            get => (((IPdfNumber)this[PdfName.BPC]) ?? ((IPdfNumber)this[PdfName.BitsPerComponent]))?.IntValue ?? 8;
            set => this[PdfName.BPC] = new PdfInteger(value);
        }

        public PdfDirectObject ColorSpaceObject
        {
            get => this[PdfName.CS] ?? this[PdfName.ColorSpace];
        }

        public ColorSpace ColorSpace
        {
            get => ColorSpace.Wrap(ColorSpaceObject);
            set => this[PdfName.CS] = value.RefOrSelf;
        }

        public float[] Decode
        {
            get => decode ??= (((PdfArray)this[PdfName.D]) ?? ((PdfArray)this[PdfName.Decode]))?.ToFloatArray();
            set => this[PdfName.D] = new PdfArrayImpl(value.Select(p => PdfInteger.Get((int)p)));
        }

        public PdfDirectObject DecodeParms
        {
            get => this[PdfName.DP] ?? this[PdfName.DecodeParms];
            set => this[PdfName.DP] = value;
        }

        public PdfDirectObject Filter
        {
            get => this[PdfName.F] ?? this[PdfName.Filter];
            set => this[PdfName.F] = value;
        }

        public int Height
        {
            get => (((IPdfNumber)this[PdfName.H]) ?? ((IPdfNumber)this[PdfName.Height]))?.IntValue ?? 0;
            set => this[PdfName.H] = new PdfInteger(value);
        }

        public int Width
        {
            get => (((IPdfNumber)this[PdfName.W]) ?? ((IPdfNumber)this[PdfName.Width]))?.IntValue ?? 0;
            set => this[PdfName.W] = new PdfInteger(value);
        }

        public string ImageMask
        {
            get => (this[PdfName.IM] ?? this[PdfName.ImageMask])?.ToString();
            set => this[PdfName.IM] = PdfName.Get(value);
        }

        public string Interpolate
        {
            get => (this[PdfName.I] ?? this[PdfName.Interpolate])?.ToString();
            set => this[PdfName.I] = PdfName.Get(value);
        }

        public string Intent
        {
            get => this[PdfName.Intent]?.ToString();
            set => this[PdfName.Intent] = PdfName.Get(value);
        }

        public bool ContainsKey(PdfName key) => GetKeyIndex(key) != null;

        public ICollection<PdfName> Keys => keys ??= GetKeys();

        private List<PdfName> GetKeys()
        {
            var keys = new List<PdfName>();
            for (int index = 0, length = operands.Count - 1; index < length; index += 2)
            { keys.Add((PdfName)operands.Get(index)); }

            return keys;
        }

        public bool Remove(PdfName key)
        {
            int? index = GetKeyIndex(key);
            if (!index.HasValue)
                return false;

            operands.RemoveAt(index.Value);
            operands.RemoveAt(index.Value);
            return true;
        }

        public PdfDirectObject this[PdfName key]
        {
            get => GetKeyIndex(key) is int index ? operands.Get(index + 1) : null;
            set
            {
                if (GetKeyIndex(key) is int index)
                {
                    operands.SetSimple(index, key);
                    operands.SetSimple(index + 1, value);                    
                }
                else
                {
                    operands.AddSimple(key);
                    operands.AddSimple(value);
                }
            }
        }

        public bool TryGetValue(PdfName key, out PdfDirectObject value)
        {
            var keyIndex = GetKeyIndex(key);
            value = keyIndex is int index ? operands.Get(index + 1) : null;
            return keyIndex != null;
        }

        public ICollection<PdfDirectObject> Values
        {
            get
            {
                var values = new List<PdfDirectObject>();
                for (int index = 1, length = operands.Count - 1; index < length; index += 2)
                { values.Add(operands.Get(index)); }
                return values;
            }
        }

        void ICollection<KeyValuePair<PdfName, PdfDirectObject>>.Add(KeyValuePair<PdfName, PdfDirectObject> keyValuePair)
        { Add(keyValuePair.Key, keyValuePair.Value); }

        public void Clear() => operands.Clear();

        bool ICollection<KeyValuePair<PdfName, PdfDirectObject>>.Contains(KeyValuePair<PdfName, PdfDirectObject> keyValuePair)
        { return (this[keyValuePair.Key] == keyValuePair.Value); }

        public void CopyTo(KeyValuePair<PdfName, PdfDirectObject>[] keyValuePairs, int index)
        { throw new NotImplementedException(); }

        public int Count => operands.Count / 2;

        public bool IsReadOnly => false;

        public bool Remove(KeyValuePair<PdfName, PdfDirectObject> keyValuePair)
        { throw new NotImplementedException(); }

        IEnumerator<KeyValuePair<PdfName, PdfDirectObject>> IEnumerable<KeyValuePair<PdfName, PdfDirectObject>>.GetEnumerator()
        {
            for (int index = 0, length = operands.Count - 1; index < length; index += 2)
            {
                yield return new KeyValuePair<PdfName, PdfDirectObject>(
                  (PdfName)operands.Get(index),
                  operands.Get(index + 1));
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        { return ((IEnumerable<KeyValuePair<PdfName, PdfDirectObject>>)this).GetEnumerator(); }

        private int? GetKeyIndex(object key)
        {
            for (int index = 0, length = operands.Count - 1; index < length; index += 2)
            {
                if (operands.Get(index).Equals(key))
                    return index;
            }
            return null;
        }
    }
}