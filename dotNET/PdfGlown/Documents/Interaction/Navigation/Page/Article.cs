/*
  Copyright 2012 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfGlown.Documents.Interchange.Metadata;
using PdfGlown.Objects;
using System.Collections.Generic;

namespace PdfGlown.Documents.Interaction.Navigation
{
    /// <summary>Article thread [PDF:1.7:8.3.2].</summary>
    [Pdf(VersionEnum.PDF11)]
    public sealed class Article : PdfDictionary
    {
        private ArticleElements elements;

        public Article(PdfDocument context)
            : base(context, new Dictionary<PdfName, PdfDirectObject>(1) { 
                { PdfName.Type, PdfName.Thread } 
            })
        {
            context.Catalog.Articles.Add(this);
        }

        internal Article(Dictionary<PdfName, PdfDirectObject> baseObject) 
            : base(baseObject)
        { }

        /// <summary>Deletes this thread removing also its reference in the document's collection.</summary>
        public override bool Delete()
        {
            // Shallow removal (references):
            // * reference in document
            Catalog.Articles.Remove(this);

            // Deep removal (indirect object).
            return base.Delete();
        }

        /// <summary>Gets the beads associated to this thread.</summary>
        public ArticleElements Elements
        {
            get => elements ??= new ArticleElements(this);
        }

        /// <summary>Gets/Sets common article metadata.</summary>
        public Information Information
        {
            get => GetOrCreate<Information>(PdfName.I);
            set => Set(PdfName.I, value);
        }
    }
}