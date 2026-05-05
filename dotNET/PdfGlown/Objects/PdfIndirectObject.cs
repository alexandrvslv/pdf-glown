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
using PdfGlown.Tokens;

using System;
using System.Text;

namespace PdfGlown.Objects
{
    /// <summary>PDF indirect object [PDF:1.6:3.2.9].</summary>
    public class PdfIndirectObject : PdfObject, IPdfIndirectObject
    {
        private static readonly byte[] BeginIndirectObjectChunk = BaseEncoding.Pdf.Encode(Symbol.Space + Keyword.BeginIndirectObject + Symbol.LineFeed);
        private static readonly byte[] EndIndirectObjectChunk = BaseEncoding.Pdf.Encode(Symbol.LineFeed + Keyword.EndIndirectObject + Symbol.LineFeed);
        public static readonly byte[] CapitalRChunk = BaseEncoding.Pdf.Encode(new char[] { Symbol.CapitalR });

        private PdfDirectObject dataObject;
        private PdfDocument document;
        private PdfReference reference;
        private XRefEntry xrefEntry;
        private PdfObjectStatus status;


        /// <param name="document">Associated file.</param>
        /// <param name="dataObject">
        ///   <para>Data object associated to the indirect object. It MUST be</para>
        ///   <list type="bullet">
        ///     <item><code>null</code>, if the indirect object is original or free.</item>
        ///     <item>NOT <code>null</code>, if the indirect object is new and in-use.</item>
        ///   </list>
        /// </param>
        /// <param name="xrefEntry">Cross-reference entry associated to the indirect object. If the
        ///   indirect object is new, its offset field MUST be set to 0.</param>
        internal PdfIndirectObject(PdfDocument document, PdfDirectObject dataObject, XRefEntry xrefEntry)
            : base(PdfObjectStatus.Updateable)
        {
            this.document = document;
            this.dataObject = Include(dataObject);
            this.xrefEntry = xrefEntry;

            IsOriginal = (xrefEntry.Offset >= 0);
            reference = new PdfReference(this);
        }

        public override PdfObject Accept(IVisitor visitor, PdfName parentKey, object data) => visitor.Visit(this, parentKey, data);

        public override PdfDirectObject Resolve(PdfName parentKey) => GetDataObject(parentKey);

        /// <summary>Adds the <see cref="DataObject">data object</see> to the specified object stream
        /// [PDF:1.6:3.4.6].</summary>
        /// <param name="objectStream">Target object stream.</param>
        public void Compress(ObjectStream objectStream)
        {
            // Remove from previous object stream!
            Uncompress();

            if (objectStream != null
               && IsCompressible())
            {
                // Add to the object stream!
                objectStream[xrefEntry.Number] = DataObject;
                // Update its xref entry!
                xrefEntry.Usage = XRefEntry.UsageEnum.InUseCompressed;
                xrefEntry.StreamNumber = objectStream.Reference.Number;
                xrefEntry.Offset = XRefEntry.UndefinedOffset; // Internal object index unknown (to set on object stream serialization -- see ObjectStream).
            }
        }

        public override PdfIndirectObject Container => this;

        public override PdfDocument Document => document;

        // NOTE: As indirect objects are root objects, no parent can be associated.
        public override PdfObject ParentObject
        {
            get => null;
            internal set { }
        }

        public override PdfObjectStatus Status
        {
            get => status;
            protected internal set => status = value;
        }

        public override bool Updated
        {
            get => base.Updated;
            protected internal set
            {
                if (value && IsOriginal)
                {
                    // NOTE: It's expected that DropOriginal() is invoked by IndirectObjects indexer;
                    // such an action is delegated because clients may invoke directly the indexer skipping
                    // this method.
                    document.IndirectObjects.Update(this);
                }
                base.Updated = value;
            }
        }

        public override int GetHashCode() => reference.GetHashCode();

        /// <summary>Gets whether this object is compressed within an object stream [PDF:1.6:3.4.6].
        /// </summary>
        public bool IsCompressed() => xrefEntry.Usage == XRefEntry.UsageEnum.InUseCompressed;

        /// <summary>Gets whether this object can be compressed within an object stream [PDF:1.6:3.4.6].
        /// </summary>
        public bool IsCompressible()
        {
            return !IsCompressed()
              && IsInUse()
              && !(DataObject is PdfStream
                || dataObject is PdfInteger)
              && Reference.Generation == 0;
        }

        /// <summary>Gets whether this object contains a data object.</summary>
        public bool IsInUse() => (xrefEntry.Usage == XRefEntry.UsageEnum.InUse);

        /// <summary>Gets whether this object comes intact from an existing file.</summary>
        public bool IsOriginal
        {
            get => (Status & PdfObjectStatus.Original) != 0;
            internal set => Status = value ? (Status | PdfObjectStatus.Original) : (Status & ~PdfObjectStatus.Original);
        }

        public override PdfObject Swap(PdfObject other)
        {
            PdfIndirectObject otherObject = (PdfIndirectObject)other;
            PdfDirectObject otherDataObject = otherObject.dataObject;
            // Update the other!
            otherObject.DataObject = dataObject;
            // Update this one!
            this.DataObject = otherDataObject;
            return this;
        }

        /// <summary>Removes the <see cref="DataObject">data object</see> from its object stream [PDF:1.6:3.4.6].</summary>
        public void Uncompress()
        {
            if (!IsCompressed())
                return;

            // Remove from its object stream!
            var oldObjectStream = (ObjectStream)document.IndirectObjects[xrefEntry.StreamNumber].Resolve(PdfName.ObjStm);
            oldObjectStream.Remove(xrefEntry.Number);
            // Update its xref entry!
            xrefEntry.Usage = XRefEntry.UsageEnum.InUse;
            xrefEntry.StreamNumber = XRefEntry.UndefinedStreamNumber; // No object stream.
            xrefEntry.Offset = XRefEntry.UndefinedOffset; // Offset unknown (to set on file serialization -- see CompressedWriter).
        }

        public override void WriteTo(IOutputStream stream, PdfDocument context)
        {
            // Header.
            stream.WriteAsString(reference.Number);
            stream.Write(Chunk.Space);
            stream.WriteAsString(reference.Generation);
            stream.Write(BeginIndirectObjectChunk);
            // Body.
            DataObject.WriteTo(stream, context);
            // Tail.
            stream.Write(EndIndirectObjectChunk);
        }

        public XRefEntry XrefEntry => xrefEntry;

        public PdfDirectObject DataObject
        {
            get => GetDataObject(null);
            set
            {
                if (xrefEntry.Generation == XRefEntry.GenerationUnreusable)
                    throw new Exception("Unreusable entry.");

                Exclude(dataObject);
                dataObject = Include(value);
                xrefEntry.Usage = XRefEntry.UsageEnum.InUse;
                Update();
            }
        }

        public PdfDirectObject GetDataObject(PdfName parentKey)
        {
            if (dataObject == null)
            {
                switch (xrefEntry.Usage)
                {
                    // Free entry (no data object at all).
                    case XRefEntry.UsageEnum.Free:
                        break;
                    // In-use entry (late-bound data object).
                    case XRefEntry.UsageEnum.InUse:
                        {
                            // Get the indirect data object!
                            dataObject = Include(document.Reader.Parser.ParsePdfObjectWithLock(xrefEntry, parentKey));
                            break;
                        }
                    case XRefEntry.UsageEnum.InUseCompressed:
                        {
                            // Get the object stream where its data object is stored!
                            var objectStream = (ObjectStream)document.IndirectObjects[xrefEntry.StreamNumber].GetDataObject(PdfName.ObjStm);
                            if (objectStream.Entries.TryGetValue(xrefEntry.Number, out var entry))
                                // Get the indirect data object!
                                dataObject = Include(entry.GetDataObject(parentKey));
                            break;
                        }
                }
                dataObject?.AfterParse();
            }
            return dataObject;
        }

        public override bool Delete()
        {
            // NOTE: It's expected that DropFile() is invoked by IndirectObjects.Remove() method;
            // such an action is delegated because clients may invoke directly Remove() method,
            // skipping this method.
            document?.IndirectObjects.RemoveAt(xrefEntry.Number);
            return true;
        }

        public sealed override PdfIndirectObject IndirectObject => this;

        public sealed override PdfReference Reference => reference;

        public override string ToString()
        {
            var buffer = new StringBuilder();
            {
                // Header.
                buffer.Append(reference.Number)
                    .Append(' ')
                    .Append(reference.Generation)
                    .Append(" obj")
                    .Append(Symbol.LineFeed);
                // Body.
                buffer.Append(DataObject);
            }
            return buffer.ToString();
        }

        public override bool Virtual
        {
            get => base.Virtual;
            protected internal set
            {
                if (Virtual && !value)
                {
                    //NOTE: When a virtual indirect object becomes concrete it must be registered.
                    document.IndirectObjects.AddVirtual(this);
                    base.Virtual = false;
                    Reference.Update();
                }
                else
                { base.Virtual = value; }
                dataObject.Virtual = Virtual;
            }
        }

        internal void DropFile()
        {
            Uncompress();
            document = null;
        }
    }
}
