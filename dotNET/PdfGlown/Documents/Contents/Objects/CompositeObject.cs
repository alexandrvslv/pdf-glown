/*
  Copyright 2007-2015 Stefano Chizzolini. http://www.pdfclown.org

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

using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents.Objects
{
    /// <summary>Composite object. It is made up of multiple content objects.</summary>
    [Pdf(VersionEnum.PDF10)]
    public abstract class CompositeObject : ContentObject, ICompositeObject
    {
        protected IList<ContentObject> objects;

        protected CompositeObject()
        {
            objects = new List<ContentObject>();
        }

        protected CompositeObject(ContentObject obj) : this()
        {
            objects.Add(obj);
        }

        protected CompositeObject(params ContentObject[] objects) : this()
        {
            foreach (ContentObject obj in objects)
            {
                this.objects.Add(obj);
            }
        }

        protected CompositeObject(IList<ContentObject> objects)
        {
            this.objects = objects;
        }

        /// <summary>Gets/Sets the object header.</summary>
        public virtual Operation Header
        {
            get => null;
            set => throw new NotSupportedException();
        }

        /// <summary>Gets the list of inner objects.</summary>
        public IList<ContentObject> Contents => objects;        

        public override void Scan(GraphicsState state)
        {
            OnScanning(state);
            for (int i = 0; i < objects.Count; i++)
            {
                objects[i].Scan(state, this, i);
            }
            OnScanned(state);
        }

        public virtual void OnScanning(GraphicsState state)
        { }

        public virtual void OnScanned(GraphicsState state)
        { }

        public override string ToString()
        {
            return "{" + GetType().Name + " " + objects.ToString() + "}";
        }

        public override void WriteTo(IOutputStream stream, PdfDocument context)
        {
            foreach (var obj in objects)
            {
                obj.WriteTo(stream, context);
            }
        }
    }

}