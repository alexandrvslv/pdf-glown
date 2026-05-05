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

using PdfGlown.Objects;

using System;
using System.Collections;
using System.Collections.Generic;

namespace PdfGlown.Documents.Interchange.Metadata
{
    /// <summary>Document information [PDF:1.6:10.2.1].</summary>
    [Pdf(VersionEnum.PDF10)]
    public sealed class Information : PdfDictionary, IDictionary<PdfName, object>
    {
        public Information()
            : this((PdfDocument)null)
        { }

        public Information(PdfDocument context)
            : base(context, new())
        { }

        internal Information(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        public string Author
        {
            get => GetString(PdfName.Author);
            set => SetText(PdfName.Author, value);
        }

        public DateTime? CreationDate
        {
            get => GetDate(PdfName.CreationDate);
            set => Set(PdfName.CreationDate, value);
        }

        public string Creator
        {
            get => GetString(PdfName.Creator);
            set => SetText(PdfName.Creator, value);
        }

        [Pdf(VersionEnum.PDF11)]
        public string Keywords
        {
            get => GetString(PdfName.Keywords);
            set => SetText(PdfName.Keywords, value);
        }

        [Pdf(VersionEnum.PDF11)]
        public DateTime? ModificationDate
        {
            get => GetDate(PdfName.ModDate);
            set => Set(PdfName.ModDate, value);
        }

        public string Producer
        {
            get => GetString(PdfName.Producer);
            set => SetText(PdfName.Producer, value);
        }

        [Pdf(VersionEnum.PDF11)]
        public string Subject
        {
            get => GetString(PdfName.Subject);
            set => SetText(PdfName.Subject, value);
        }

        [Pdf(VersionEnum.PDF11)]
        public string Title
        {
            get => GetString(PdfName.Title);
            set => SetText(PdfName.Title, value);
        }

        void IDictionary<PdfName, object>.Add(PdfName key, object value) => Add(key, PdfSimpleObject<object>.Get(value));

        bool IDictionary<PdfName, object>.ContainsKey(PdfName key) => ContainsKey(key);

        object IDictionary<PdfName, object>.this[PdfName key]
        {
            get => PdfSimpleObject<object>.GetValue(Get(key));
            set => this[key] = PdfSimpleObject<object>.Get(value);
        }

        bool IDictionary<PdfName, object>.TryGetValue(PdfName key, out object value)
        {
            PdfDirectObject valueObject;
            if (TryGetValue(key, out valueObject))
            {
                value = PdfSimpleObject<object>.GetValue(valueObject);
                return true;
            }
            else
                value = null;
            return false;
        }

        ICollection<PdfName> IDictionary<PdfName, object>.Keys => base.Keys;

        ICollection<object> IDictionary<PdfName, object>.Values
        {
            get
            {
                IList<object> values = new List<object>();
                foreach (PdfDirectObject item in Values)
                { values.Add(PdfSimpleObject<object>.GetValue(item)); }
                return values;
            }
        }

        void ICollection<KeyValuePair<PdfName, object>>.Add(KeyValuePair<PdfName, object> entry) => Add(entry.Key, PdfSimpleObject<object>.Get(entry.Value));

        bool ICollection<KeyValuePair<PdfName, object>>.Contains(KeyValuePair<PdfName, object> entry) => entry.Value.Equals(Get(entry.Key));

        public void CopyTo(KeyValuePair<PdfName, object>[] entries, int index) => throw new NotImplementedException();

        public bool Remove(KeyValuePair<PdfName, object> entry) => throw new NotImplementedException();

        IEnumerator<KeyValuePair<PdfName, object>> IEnumerable<KeyValuePair<PdfName, object>>.GetEnumerator()
        {
            foreach (var entry in this)
            {
                yield return new KeyValuePair<PdfName, object>(
                  entry.Key,
                  PdfSimpleObject<object>.GetValue(entry.Value));
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<KeyValuePair<PdfName, object>>)this).GetEnumerator();
    }
}