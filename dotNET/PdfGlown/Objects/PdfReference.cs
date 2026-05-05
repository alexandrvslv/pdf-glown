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

namespace PdfGlown.Objects
{
    /// <summary>PDF indirect reference object [PDF:1.6:3.2.9].</summary>
    public sealed class PdfReference : PdfDirectObject, IPdfIndirectObject, IEquatable<PdfReference>, IComparable<PdfReference>
    {
        private const int DelegatedReferenceNumber = -1;

        private readonly int generation;
        private readonly int number;

        private PdfIndirectObject indirectObject;

        private readonly PdfDocument document;
        private PdfObject parent;
        private PdfObjectStatus status;

        internal PdfReference(PdfIndirectObject indirectObject)
        {
            this.number = DelegatedReferenceNumber;
            this.generation = DelegatedReferenceNumber;

            this.indirectObject = indirectObject;
        }

        internal PdfReference(int number, int generation, PdfDocument document)
        {
            this.number = number;
            this.generation = generation;

            this.document = document;
        }

        public override PdfDocument Document => document ?? base.Document;

        /// <summary>Gets the generation number.</summary>
        public int Generation => generation == DelegatedReferenceNumber ? IndirectObject.XrefEntry.Generation : generation;

        /// <summary>Gets the object identifier.</summary>
        /// <remarks>This corresponds to the serialized representation of an object identifier within a PDF file.</remarks>
        public string Id => $"{Number}{Symbol.Space}{Generation}";

        /// <summary>Gets the object reference.</summary>
        /// <remarks>This corresponds to the serialized representation of a reference within a PDF file.</remarks>
        public string IndirectReference => $"{Id}{Symbol.Space}{Symbol.CapitalR}";

        /// <summary>Gets the object number.</summary>
        public int Number => number == DelegatedReferenceNumber ? IndirectObject.XrefEntry.Number : number;

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

        public override bool Updateable
        {
            get => IndirectObject?.Updateable ?? false;
            //NOTE: Fail fast if the referenced indirect object is undefined.
            set => IndirectObject.Updateable = value;
        }

        public override bool Virtual
        {
            get => IndirectObject?.Virtual ?? false;
            //NOTE: Fail fast if the referenced indirect object is undefined.
            protected internal set => IndirectObject.Virtual = value;
        }

        /// <returns><code>null</code>, if the indirect object is undefined.</returns>
        public override PdfIndirectObject IndirectObject => indirectObject ??= document.IndirectObjects[number];

        public override PdfReference Reference => this;

        public override PdfObject Swap(PdfObject other)
        {
            // NOTE: Fail fast if the referenced indirect object is undefined.
            return IndirectObject.Swap(((PdfReference)other).IndirectObject).Reference;
        }

        //NOTE: Uniqueness should be achieved XORring the (local) reference hash-code with the (global)
        //file hash-code.
        public override int GetHashCode() => HashCode.Combine(Document, Number, Generation);

        public override string ToString() => IndirectReference;

        public override void WriteTo(IOutputStream stream, PdfDocument context)
        {            
            stream.WriteAsString(Number);
            stream.Write(Chunk.Space);
            stream.WriteAsString(Generation);
            stream.Write(Chunk.Space);
            stream.Write(PdfIndirectObject.CapitalRChunk);
        }

        public override PdfObject Accept(IVisitor visitor, PdfName parentKey, object data) => visitor.Visit(this, parentKey, data);

        public override PdfDirectObject Resolve(PdfName parentKey = null) => IndirectObject?.GetDataObject(parentKey);

        public override int CompareTo(PdfDirectObject obj)
        {
            if (obj == null)
                return 1;
            if (ReferenceEquals(this, obj))
                return 0;
            return obj is PdfReference reference ? CompareTo(reference) : GetHashCode().CompareTo(obj.GetHashCode());
        }

        public int CompareTo(PdfReference reference)
        {
            var result = Number.CompareTo(reference.Number);
            return result == 0 ? Generation.CompareTo(reference.Generation)
                : result;
        }

        public override bool Equals(object other)
        {
            // NOTE: References are evaluated as "equal" if they are either the same instance or they sport
            // the same identifier within the same file instance.
            if (ReferenceEquals(this, other))
                return true;
            return other is PdfReference otherReference && Equals(otherReference);
        }

        public bool Equals(PdfReference otherReference)
        {
            return otherReference.Document == Document
                && otherReference.Number.Equals(Number)
                && otherReference.Generation.Equals(Generation);
        }
    }
}