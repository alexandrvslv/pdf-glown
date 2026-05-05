/*
  Copyright 2008-2012 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfGlown.Documents.Interaction.Annotations;
using PdfGlown.Objects;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PdfGlown.Documents.Interaction.Forms
{
    /// <summary>Interactive form fields [PDF:1.6:8.6.1].</summary>
    [Pdf(VersionEnum.PDF12)]
    public sealed class Fields : PdfObjectWrapper<PdfArray>, IDictionary<string, Field>, IEnumerable<Field>
    {
        private Dictionary<string, Field> nameCache;
        private readonly Dictionary<PdfReference, Field> refCache = new();

        public Fields(PdfDocument context) 
            : base(context, new PdfArrayImpl())
        { }

        public Fields(PdfDirectObject baseObject) 
            : base(baseObject)
        { }

        internal Dictionary<string, Field> NameCache => nameCache ??= RefreshCache();
        internal Dictionary<PdfReference, Field> RefCache => refCache;

        public ICollection<string> Keys => NameCache.Keys;

        public ICollection<Field> Values => NameCache.Values;

        public int Count => NameCache.Count;

        public bool IsReadOnly => false;

        public Field this[string key]
        {
            get => TryGetValue(key, out var field) ? field : null;
            set => Add(key, value);
        }

        private Dictionary<string, Field> RefreshCache()
        {
            var cache = new Dictionary<string, Field>();

            foreach (var field in RetrieveValues(DataObject))
            {
                cache[field.FullName] = field;
            }
            return cache;
        }

        public void Add(Field value) => Add(value.FullName, value);

        public void Add(string key, Field value)
        {
            NameCache[key] = value;
            RefCache[(PdfReference)value.RefOrSelf] = value;
            var fieldObjects = GetHolderArray(value);
            fieldObjects.Add(value.RefOrSelf);
        }

        public bool ContainsKey(string key) => NameCache.ContainsKey(key);

        public bool Remove(string key) => NameCache.Remove(key, out var field) && RemoveFromArray(field);

        public bool Remove(string key, out Field field) => NameCache.Remove(key, out field) && RemoveFromArray(field);

        public bool Remove(Field field)
        {
            NameCache.Remove(field.FullName);
            return RemoveFromArray(field);
        }

        private bool RemoveFromArray(Field field)
        {
            var fieldObjects = GetHolderArray(field);
            return fieldObjects?.Remove(field.RefOrSelf) ?? false;
        }

        private PdfArray GetHolderArray(Field field)
        {
            PdfArray fieldObjects;
            {
                var fieldParentReference = field.DataObject.Get(PdfName.Parent);
                if (fieldParentReference == null)
                { fieldObjects = DataObject; }
                else
                { fieldObjects = ((PdfDictionary)fieldParentReference.Resolve(null)).Get<PdfArray>(PdfName.Kids); }
            }

            return fieldObjects;
        }

        public bool TryGetValue(string key, out Field value) => NameCache.TryGetValue(key, out value);

        public bool TryGetValue(PdfReference key, out Field value) => RefCache.TryGetValue(key, out value);

        void ICollection<KeyValuePair<string, Field>>.Add(KeyValuePair<string, Field> entry) => Add(entry.Key, entry.Value);

        public void Clear()
        {
            NameCache.Clear();
            DataObject.Clear();
        }

        bool ICollection<KeyValuePair<string, Field>>.Contains(KeyValuePair<string, Field> entry) => NameCache.Contains(entry);

        public void CopyTo(KeyValuePair<string, Field>[] entries, int index)
        {
            foreach (var entry in this)
            {
                entries[index++] = entry;
            }
        }

        public bool Remove(KeyValuePair<string, Field> entry) => throw new NotImplementedException();

        public Dictionary<string, Field>.Enumerator GetEnumerator() => NameCache.GetEnumerator();

        IEnumerator<KeyValuePair<string, Field>> IEnumerable<KeyValuePair<string, Field>>.GetEnumerator() => GetEnumerator();

        IEnumerator<Field> IEnumerable<Field>.GetEnumerator() => Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private IList<Field> RetrieveValues(PdfArray fieldObjects)
        {
            var list = new List<Field>();
            RetrieveValues(fieldObjects, list);
            return list;
        }

        public Field Wrap(PdfReference reference)
        {
            if (reference == null)
                return null;
            if (TryGetValue(reference, out var field))
                return field;
            return RefCache[reference] = Field.Wrap(reference);
        }

        private void RetrieveValues(PdfArray fieldObjects, IList<Field> values)
        {
            foreach (var fieldReference in fieldObjects.GetItems().OfType<PdfReference>())
            {
                var kidReferences = ((PdfDictionary)fieldReference.Resolve(null)).Get<PdfArray>(PdfName.Kids);
                var kidObject = kidReferences?.Get<PdfDictionary>(0);
                // Terminal field?
                if (kidObject == null // Merged single widget annotation.
                  || (!kidObject.ContainsKey(PdfName.FT) // Multiple widget annotations.
                    && kidObject is Widget))
                {
                    values.Add(Wrap(fieldReference));
                }
                else // Non-terminal field.
                {
                    RetrieveValues(kidReferences, values);
                }
            }
        }
    }
}