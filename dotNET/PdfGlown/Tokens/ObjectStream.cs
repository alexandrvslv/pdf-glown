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

using PdfGlown.Bytes;
using PdfGlown.Objects;

using System;
using System.Collections.Generic;

namespace PdfGlown.Tokens
{
    /// <summary>Object stream containing a sequence of PDF objects [PDF:1.6:3.4.6].</summary>
    /// <remarks>The purpose of object streams is to allow a greater number of PDF objects
    /// to be compressed, thereby substantially reducing the size of PDF files.
    /// The objects in the stream are referred to as compressed objects.</remarks>
    public sealed class ObjectStream : PdfStream
    {
        internal sealed class ObjectEntry
        {
            internal PdfDirectObject dataObject;
            internal int offset;

            private FileParser parser;

            private ObjectEntry(FileParser parser)
            { this.parser = parser; }

            public ObjectEntry(int offset, FileParser parser)
                : this(parser)
            {
                this.dataObject = null;
                this.offset = offset;
            }

            public ObjectEntry(PdfDirectObject dataObject, FileParser parser)
                : this(parser)
            {
                this.dataObject = dataObject;
                this.offset = -1; // Undefined -- to set on stream serialization.
            }

            public PdfDirectObject DataObject
            {
                get => GetDataObject(null);
            }

            public PdfDirectObject GetDataObject(PdfName parentKey)
            {
                if (dataObject == null)
                {
                    parser.Seek(offset); parser.MoveNext();
                    dataObject = parser.ParsePdfObject(parentKey);
                }
                return dataObject;
            }
        }

        /// <summary>Compressed objects map.</summary>
        /// <remarks>This map is initially populated with offset values;
        /// when a compressed object is required, its offset is used to retrieve it.</remarks>
        private Dictionary<int, ObjectEntry> objectEntries;
        private FileParser parser;

        public ObjectStream()
            : base(new Dictionary<PdfName, PdfDirectObject>() {
                { PdfName.Type, PdfName.ObjStm }
            }, new ByteStream())
        { }

        internal ObjectStream(Dictionary<PdfName, PdfDirectObject> header)
            : base(header)
        { }

        internal ObjectStream(Dictionary<PdfName, PdfDirectObject> header, IInputStream body)
            : base(header, body)
        { }

        public override PdfObject Accept(IVisitor visitor, PdfName parentKey, object data) => visitor.Visit(this, parentKey, data);

        /// <summary>Gets/Sets the object stream extended by this one.</summary>
        /// <remarks>Both streams are considered part of a collection of object streams  whose links form
        /// a directed acyclic graph.</remarks>
        public ObjectStream BaseStream
        {
            get => Get<ObjectStream>(PdfName.Extends);
            set => Set(PdfName.Extends, value);
        }

        public override void WriteTo(IOutputStream stream, PdfDocument context)
        {
            if (objectEntries != null)
            { Flush(stream); }

            base.WriteTo(stream, context);
        }

        public void Add(int key, PdfDirectObject value) => Entries.Add(key, new ObjectEntry(value, parser));

        public bool ContainsKey(int key) => Entries.ContainsKey(key);

        public ICollection<int> ObjectKeys => Entries.Keys;

        public bool Remove(int key) => Entries.Remove(key);

        public PdfDirectObject this[int key]
        {
            get => Entries.TryGetValue(key, out var direct) ? direct?.DataObject : null;
            set => Entries[key] = new ObjectEntry(value, parser);
        }

        public bool TryGetValue(int key, out PdfDirectObject value)
        {
            if (Entries.TryGetValue(key, out var direct))
            {
                value = direct?.DataObject;
                return true;
            }
            value = null;
            return false;
        }

        //void ICollection<KeyValuePair<int, PdfDirectObject>>.Add(KeyValuePair<int, PdfDirectObject> entry)
        //{
        //    Add(entry.Key, entry.Value);
        //}

        public void ClearObjects()
        {
            if (objectEntries == null)
            { objectEntries = new Dictionary<int, ObjectEntry>(); }
            else
            { objectEntries.Clear(); }
        }

        //bool ICollection<KeyValuePair<int, PdfDirectObject>>.Contains(KeyValuePair<int, PdfDirectObject> entry)
        //{
        //    return ((ICollection<KeyValuePair<int, PdfDirectObject>>)Entries).Contains(entry);
        //}

        public void CopyTo(KeyValuePair<int, PdfDirectObject>[] entries, int index)
        {
            throw new NotImplementedException();
        }

        public int ObjectsCount => Entries.Count;

        public bool Remove(KeyValuePair<int, PdfDirectObject> entry)
        {
            PdfDirectObject value;
            if (TryGetValue(entry.Key, out value)
              && value.Equals(entry.Value))
                return Entries.Remove(entry.Key);
            else
                return false;
        }

        //IEnumerator<KeyValuePair<int, PdfDirectObject>> IEnumerable<KeyValuePair<int, PdfDirectObject>>.GetEnumerator()
        //{
        //    foreach (int key in Keys)
        //    { yield return new KeyValuePair<int, PdfDirectObject>(key, this[key]); }
        //}

        //IEnumerator IEnumerable.GetEnumerator()
        //{ return ((IEnumerable<KeyValuePair<int, PdfDirectObject>>)this).GetEnumerator(); }

        internal Dictionary<int, ObjectEntry> Entries
        {
            get
            {
                if (objectEntries == null)
                {
                    objectEntries = new Dictionary<int, ObjectEntry>();

                    var body = GetInputStream();
                    if (body.Length > 0)
                    {
                        parser = new FileParser(body, Document);
                        int baseOffset = GetInt(PdfName.First);
                        for (int index = 0, length = GetInt(PdfName.N); index < length; index++)
                        {
                            int objectNumber = ((PdfInteger)parser.ParseNextPdfObject(null)).RawValue;
                            int objectOffset = baseOffset + ((PdfInteger)parser.ParseNextPdfObject(null)).RawValue;
                            objectEntries[objectNumber] = new ObjectEntry(objectOffset, parser);
                        }
                    }
                }
                return objectEntries;
            }
        }

        /// <summary>Serializes the object stream entries into the stream body.</summary>
        private void Flush(IOutputStream stream)
        {
            // 1. Body.
            int dataByteOffset;
            {
                // Serializing the entries into the stream buffer...
                IByteStream indexBuffer = new ByteStream();
                IByteStream dataBuffer = new ByteStream();
                var indirectObjects = Document.IndirectObjects;
                int objectIndex = -1;
                var context = Document;
                foreach (KeyValuePair<int, ObjectEntry> entry in Entries)
                {
                    int objectNumber = entry.Key;

                    // Update the xref entry!
                    var xrefEntry = indirectObjects[objectNumber].XrefEntry;
                    xrefEntry.Offset = ++objectIndex;

                    // NOTE: The entry offset MUST be updated only after its serialization, in order not to
                    // interfere with its possible data-object retrieval from the old serialization.
                    int entryValueOffset = (int)dataBuffer.Length;

                    // Index.
                    indexBuffer.Write(objectNumber.ToString());
                    indexBuffer.Write(Chunk.Space);
                    indexBuffer.Write(entryValueOffset.ToString());
                    indexBuffer.Write(Chunk.Space);

                    // Data.
                    entry.Value.DataObject.WriteTo(dataBuffer, context);
                    entry.Value.offset = entryValueOffset;
                }

                // Get the stream buffer!
                var body = GetOutputStream();

                // Delete the old entries!
                body.SetLength(0);

                // Add the new entries!
                body.Write(indexBuffer);
                dataByteOffset = (int)body.Length;
                body.Write(dataBuffer);
            }

            // 2. Header.
            {
                Set(PdfName.N, Entries.Count);
                Set(PdfName.First, dataByteOffset);
            }
        }
    }
}