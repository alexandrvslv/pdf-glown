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

using PdfGlown.Documents.Files;
using PdfGlown.Documents.Interaction.Actions;
using PdfGlown.Documents.Interaction.Navigation;
using PdfGlown.Documents.Multimedia;
using PdfGlown.Objects;
using PdfGlown.Util;
using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Names
{
    /// <summary>Name dictionary [PDF:1.6:3.6.3].</summary>
    [Pdf(VersionEnum.PDF12)]
    public sealed class NamedResources : PdfDictionary, ICompositeDictionary<PdfString>
    {
        private NamedDestinations destinations;
        private NamedEmbeddedFiles embeddedFiles;
        private NamedJavaScripts javaScripts;
        private NamedPages pages;
        private NamedRenditions renditions;

        public NamedResources()
            : this((PdfDocument)null)
        { }

        public NamedResources(PdfDocument context)
            : base(context, new())
        { }

        internal NamedResources(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets/Sets the named destinations.</summary>
        [Pdf(VersionEnum.PDF12)]
        public NamedDestinations Destinations
        {
            get => destinations ??= new(GetOrCreate<PdfDictionary>(PdfName.Dests));
            set => Set(PdfName.Dests, destinations = value);
        }

        /// <summary>Gets/Sets the named embedded files.</summary>
        [Pdf(VersionEnum.PDF14)]
        public NamedEmbeddedFiles EmbeddedFiles
        {
            get => embeddedFiles ??= new(GetOrCreate<PdfDictionary>(PdfName.EmbeddedFiles));
            set => Set(PdfName.EmbeddedFiles, embeddedFiles = value);
        }

        /// <summary>Gets/Sets the named JavaScript actions.</summary>
        [Pdf(VersionEnum.PDF13)]
        public NamedJavaScripts JavaScripts
        {
            get => javaScripts ??= new(GetOrCreate<PdfDictionary>(PdfName.JavaScript));
            set => Set(PdfName.JavaScript, javaScripts = value);
        }

        /// <summary>Gets/Sets the named pages.</summary>
        [Pdf(VersionEnum.PDF13)]
        public NamedPages Pages
        {
            get => pages ??= new(GetOrCreate<PdfDictionary>(PdfName.Pages));
            set => Set(PdfName.Pages, pages = value);
        }

        /// <summary>Gets/Sets the named renditions.</summary>
        [Pdf(VersionEnum.PDF15)]
        public NamedRenditions Renditions
        {
            get => renditions ??= new(GetOrCreate<PdfDictionary>(PdfName.Renditions));
            set => Set(PdfName.Renditions, renditions = value);
        }

        public IBiDictionary Get(Type type)
        {
            if (typeof(Destination).IsAssignableFrom(type))
                return Destinations;
            else if (typeof(FileSpecification).IsAssignableFrom(type))
                return EmbeddedFiles;
            else if (typeof(JavaScript).IsAssignableFrom(type))
                return JavaScripts;
            else if (typeof(PdfPage).IsAssignableFrom(type))
                return Pages;
            else if (typeof(Rendition).IsAssignableFrom(type))
                return Renditions;
            else
                return null;
        }

        public T GetRes<T>(PdfString key)
        {
            return (T)Get(typeof(T))?[key];
        }
    }
}