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

using PdfGlown.Objects;
using PdfGlown.Util.Math;
using SkiaSharp;
using System.Collections.Generic;

namespace PdfGlown.Documents.Interaction.Navigation
{
    /// <summary>Article bead [PDF:1.7:8.3.2].</summary>
    [Pdf(VersionEnum.PDF11)]
    public sealed class ArticleElement : PdfDictionary, IPdfObjectWrapper
    {
        private SKRect? box;

        public ArticleElement(PdfPage page, SKRect box)
            : base(page.Document, new Dictionary<PdfName, PdfDirectObject>(1) {
                { PdfName.Type, PdfName.Bead }
            })
        {
            page.ArticleElements.Add(this);
            Box = box;
        }

        internal ArticleElement(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets the thread article this bead belongs to.</summary>
        public Article Article
        {
            get
            {
                var bead = this;
                Article article;
                while ((article = bead.Get<Article>(PdfName.T)) == null)
                { bead = bead.Get<ArticleElement>(PdfName.V); }
                return article;
            }
        }

        /// <summary>Gets/Sets the location on the page in default user space units.</summary>
        public SKRect Box
        {
            get => box ??= GetUserSpaceBox();
            set
            {
                box = null;
                var newValue = Page.InvertRotateMatrix.MapRect(value);
                this[PdfName.R] = newValue.ToPdfRectangle();
            }
        }

        private SKRect GetUserSpaceBox()
        {
            var box = Get<PdfArray>(PdfName.R)?.ToSKRect() ?? SKRect.Empty;
            return Page.RotateMatrix.MapRect(box);
        }

        /// <summary>Deletes this bead removing also its references on the page and its article thread.
        /// </summary>
        public override bool Delete()
        {
            // Shallow removal (references):
            // * thread links
            Article.Elements.Remove(this);
            // * reference on page
            Page.ArticleElements.Remove(this);

            // Deep removal (indirect object).
            return base.Delete();
        }

        /// <summary>Gets whether this is the first bead in its thread.</summary>
        public bool IsHead()
        {
            var thread = Get<PdfDictionary>(PdfName.T);
            return thread != null && Reference.Equals(thread.Get(PdfName.F));
        }

        /// <summary>Gets the next bead.</summary>
        public ArticleElement Next => Get<ArticleElement>(PdfName.N);
        
        /// <summary>Gets the location page.</summary>
        public PdfPage Page => Get<PdfPage>(PdfName.P);

        /// <summary>Gets the previous bead.</summary>
        public ArticleElement Previous => Get<ArticleElement>(PdfName.V);
    }
}