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

namespace PdfGlown.Objects
{
    /// <summary>Abstract PDF object.</summary>
    public abstract class PdfObject : IVisitable
    {
        protected PdfObject()
        { }

        protected PdfObject(PdfObjectStatus status)
        {
            Status = status;
        }

        public abstract PdfObjectStatus Status { get; protected internal set; }

        public virtual PdfIndirectObject Container => ParentObject?.Container;

        /// <summary>Gets the indirect object containing the data associated to this object.</summary>
        /// <seealso cref="Container"/>
        /// <seealso cref="IndirectObject"/>
        public PdfIndirectObject DataContainer => IndirectObject ?? Container;

        /// <summary>Gets the document containing this object.</summary>
        public virtual PdfDocument Document => DataContainer?.Document;

        /// <summary>Gets the indirect object corresponding to this object.</summary>
        /// <seealso cref="Container"/>
        /// <seealso cref="DataContainer"/>
        public virtual PdfIndirectObject IndirectObject => ParentObject as PdfIndirectObject;

        /// <summary>Gets/Sets the parent of this object.</summary>
        /// <seealso cref="Container"/>
        public abstract PdfObject ParentObject
        {
            get;
            internal set;
        }

        /// <summary>Gets the indirect reference of this object.</summary>
        public virtual PdfReference Reference => IndirectObject?.Reference;

        /// <summary>Creates a shallow copy of this object.</summary>
        public object Clone()
        {
            var clone = (PdfObject)MemberwiseClone();
            clone.ParentObject = null;
            return clone;
        }

        /// <summary>Creates a deep copy of this object using the default cloner of the specified file
        /// context.</summary>
        public virtual PdfObject Clone(PdfDocument context) => Clone(context.Cloner);

        /// <summary>Creates a deep copy of this object using the specified cloner.</summary>
        public virtual PdfObject Clone(Cloner cloner) => Accept(cloner, null, null);

        /// <summary>Removes the object from its file context.</summary>
        /// <remarks>Only indirect objects can be removed through this method; direct objects have to be
        /// explicitly removed from their parent object. The object is no more usable after this method
        /// returns.</remarks>
        /// <returns>Whether the object was removed from its file context.</returns>
        public virtual bool Delete() => IndirectObject?.Delete() ?? false;

        /// <summary>Ensures this object to be resolved into its corresponding data object.</summary>
        /// <seealso cref="Unresolve()"/>
        public virtual PdfDirectObject Resolve(PdfName parentKey = null) => (PdfDirectObject)this;

        /// <summary>Swaps contents between this object and the other one.</summary>
        /// <param name="other">Object whose contents have to be swapped with this one's.</param>
        /// <returns>This object.</returns>
        public abstract PdfObject Swap(PdfObject other);

        /// <summary>Ensures this object to be unresolved into its corresponding indirect reference, if
        /// available.</summary>
        /// <returns><see cref="PdfReference"/>, if available; <code>this</code>, otherwise.</returns>
        /// <seealso cref="Resolve()"/>
        public PdfDirectObject Unresolve()
        {
            return Reference ?? (PdfDirectObject)this;
        }

        ///<summary>Gets/Sets whether the detection of object state changes is enabled.</summary>
        public virtual bool Updateable
        {
            get => (Status & PdfObjectStatus.Updateable) != 0;
            set => Status = value ? (Status | PdfObjectStatus.Updateable) : (Status & ~PdfObjectStatus.Updateable);
        }

        ///<summary>Gets/Sets whether the initial state of this object has been modified.</summary>
        public virtual bool Updated
        {
            get => (Status & PdfObjectStatus.Updated) != 0;
            protected internal set => Status = value ? (Status | PdfObjectStatus.Updated) : (Status & ~PdfObjectStatus.Updated);
        }

        ///<summary>Gets/Sets whether this object acts like a null-object placeholder.</summary>
        public virtual bool Virtual
        {
            get => (Status & PdfObjectStatus.Virtual) != 0;
            protected internal set => Status = value ? (Status | PdfObjectStatus.Virtual) : (Status & ~PdfObjectStatus.Virtual);
        }

        /// <summary>Serializes this object to the specified stream.</summary>
        /// <param name="stream">Target stream.</param>
        /// <param name="context">File context.</param>
        public abstract void WriteTo(IOutputStream stream, PdfDocument context);

        public abstract PdfObject Accept(IVisitor visitor, PdfName parentKey, object data);

        /// <summary>Updates the state of this object.</summary>
        protected internal void Update()
        {
            if (!Updateable || Updated)
                return;

            Updated = true;
            Virtual = false;

            // Propagate the update to the ascendants!
            ParentObject?.Update();
        }

        /// <summary>Ensures that the specified object is decontextualized from this object.</summary>
        /// <param name="obj">Object to decontextualize from this object.</param>
        /// <seealso cref="Include(PdfDirectObject)"/>
        internal void Exclude(PdfDirectObject obj)
        {
            if (obj != null)
            {
                obj.ParentObject = null;
            }
        }

        /// <summary>Ensures that the specified object is contextualized into this object.</summary>
        /// <param name="obj">Object to contextualize into this object; if it is already contextualized
        ///    into another object, it will be cloned to preserve its previous association.</param>
        /// <returns>Contextualized object.</returns>
        /// <seealso cref="Exclude(PdfDirectObject)"/>
        internal PdfDirectObject Include(PdfDirectObject obj)
        {
            if (obj != null)
            {
                if (obj.ParentObject != null)
                { 
                    obj = (PdfDirectObject)obj.Clone(); 
                }
                obj.ParentObject = this;
            }
            return obj;
        }
    }
}