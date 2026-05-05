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

using PdfGlown.Documents;
using PdfGlown.Documents.Interchange.Metadata;
using PdfGlown.Util;
using System;

namespace PdfGlown.Objects
{

    /// <summary>Base high-level representation of a weakly-typed PDF object.</summary>
    public abstract class PdfObjectWrapper : IPdfObjectWrapper, IPdfDataObject
    {
        private PdfDirectObject baseObject;

        /// <summary>Instantiates an empty wrapper.</summary>
        protected PdfObjectWrapper()
        { }

        /// <summary>Instantiates a wrapper from the specified base object.</summary>
        /// <param name="baseObject">PDF object backing this wrapper. MUST be a <see cref="PdfReference"/>
        /// every time available.</param>
        public PdfObjectWrapper(PdfDirectObject baseObject)
        {
            RefOrSelf = baseObject;
        }

        /// <summary>Gets the indirect object containing the base object.</summary>
        public PdfIndirectObject Container => baseObject.Container;

        /// <summary>Gets the indirect object containing the base data object.</summary>
        public PdfIndirectObject DataContainer => baseObject.DataContainer;

        /// <summary>Gets/Sets the metadata associated to this object.</summary>
        /// <returns><code>null</code>, if base data object's type isn't suitable (only
        /// <see cref="PdfDictionary"/> and <see cref="PdfStream"/> objects are allowed).</returns>
        /// <throws>NotSupportedException If base data object's type isn't suitable (only
        /// <see cref="PdfDictionary"/> and <see cref="PdfStream"/> objects are allowed).</throws>
        public virtual PdfMetadata Metadata
        {
            get => DataObject is PdfDictionary dictionary ? dictionary.Get<PdfMetadata>(PdfName.Metadata) : null;
            set
            {
                if (DataObject is not PdfDictionary dictionary)
                    throw new NotSupportedException("Metadata can be attached only to PdfDictionary/PdfStream base data objects.");

                dictionary.Set(PdfName.Metadata, value);
            }
        }

        /// <summary>Removes the object from its document context.</summary>
        /// <remarks>Only indirect objects can be removed through this method; direct objects have to be
        /// explicitly removed from their parent object. The object is no more usable after this method
        /// returns.</remarks>
        /// <returns>Whether the object was removed from its document context.</returns>
        public virtual bool Delete() => baseObject.Delete();

        /// <summary>Gets the document context.</summary>
        public PdfCatalog Catalog => Document?.Catalog;

        /// <summary>Gets the document context.</summary>
        public PdfDocument Document => baseObject.Document;

        public virtual PdfDirectObject RefOrSelf
        {
            get => baseObject;
            protected set => baseObject = value;
        }

        /// <summary>Gets the underlying data object.</summary>
        public PdfDirectObject DataObject => RefOrSelf?.Resolve();

        /// <summary>Gets whether the underlying data object is concrete.</summary>
        public bool Virtual => DataObject.Virtual;

        /// <summary>Gets a clone of the object, registered inside the specified document context using
        /// the default object cloner.</summary>
        public virtual object Clone(PdfDocument context) => Clone(context.Cloner);

        /// <summary>Gets a clone of the object, registered using the specified object cloner.</summary>
        public virtual object Clone(Cloner cloner)
        {
            var clone = (PdfObjectWrapper)base.MemberwiseClone();
            clone.RefOrSelf = (PdfDirectObject)RefOrSelf.Clone(cloner);
            return clone;
        }

        public override bool Equals(object other)
        {
            return other != null
              && other.GetType().Equals(GetType())
              && ((PdfObjectWrapper)other).baseObject.Equals(baseObject);
        }

        public override int GetHashCode() => baseObject.GetHashCode();

        public override string ToString()
        {
            return $"{GetType().Name} {{{(RefOrSelf is PdfReference ? (PdfObject)RefOrSelf.DataContainer : RefOrSelf)}}}";
        }

        /// <summary>Retrieves the name possibly associated to this object, walking through the document's
        /// name dictionary.</summary>
        protected virtual PdfString RetrieveName()
        {
            return Catalog.Names.Get(GetType()) is IBiDictionary biDictionary
                ? biDictionary.GetKey(this) as PdfString
                : null;
        }

        ///<summary>Retrieves the object name, if available; otherwise, behaves like
        ///<see cref="PdfObjectWrapper.RefOrSelf"/>.</summary>
        protected PdfDirectObject RetrieveNamedBaseObject()
        {
            return RetrieveName() ?? RefOrSelf;
        }

        protected static PdfDirectObject GetBaseObject(object value)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>High-level representation of a strongly-typed PDF object.</summary>    
    public abstract class PdfObjectWrapper<TDataObject> : PdfObjectWrapper
      where TDataObject : PdfDirectObject
    {
        /// <summary>Instantiates an empty wrapper.</summary>
        protected PdfObjectWrapper()
        { }

        /// <summary>Instantiates a wrapper from the specified base object.</summary>
        /// <param name="baseObject">PDF object backing this wrapper. It MUST be a <see cref="PdfReference"/>
        /// every time available.</param>
        public PdfObjectWrapper(PdfDirectObject baseObject) 
            : base(baseObject)
        { }

        /// <summary>Instantiates a wrapper registering the specified base data object into the specified
        /// file context.</summary>
        /// <param name="context">File context into which the specified data object has to be registered.
        /// </param>
        /// <param name="baseDataObject">PDF data object backing this wrapper.</param>
        /// <seealso cref="PdfObjectWrapper(PdfCatalog, PdfDirectObject)"/>
        protected PdfObjectWrapper(PdfDocument context, TDataObject baseDataObject)
            : this(context != null ? context.Register(baseDataObject) : baseDataObject)
        { }

        /// <summary>Gets the underlying data object.</summary>
        public new TDataObject DataObject => RefOrSelf?.Resolve() as TDataObject;
    }
}