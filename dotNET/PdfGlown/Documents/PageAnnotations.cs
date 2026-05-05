/*
  Copyright 2008-2013 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfGlown.Documents.Interaction.Annotations;
using PdfGlown.Objects;

using System;
using System.Collections.Generic;

namespace PdfGlown.Documents
{
    /// <summary>Page annotations [PDF:1.6:3.6.2].</summary>
    [Pdf(VersionEnum.PDF10)]
    public sealed class PageAnnotations : PageElements<Annotation>
    {
        private Dictionary<string, Annotation> nameIndex;

        public PageAnnotations()
            : this((PdfDocument)null)
        { }

        public PageAnnotations(PdfDocument document)
            : base(document)
        { }

        internal PageAnnotations(List<PdfDirectObject> baseObject)
            : base(baseObject)
        {
            
        }

        public override PdfName TypeKey => PdfName.Annot;

        private Dictionary<string, Annotation> NameIndex => nameIndex ?? RefreshCache();

        public Annotation this[string name] => NameIndex.TryGetValue(name, out var annotation) ? annotation : null;

        private Dictionary<string, Annotation> RefreshCache()
        {
            if (nameIndex != null)
                return nameIndex;
            nameIndex = new Dictionary<string, Annotation>(StringComparer.Ordinal);
            for (int i = 0, length = Count; i < length; i++)
            {
                var item = this[i];
                if (item == null)
                {
                    RemoveAt(i);
                    length--;
                    i--;
                    continue;
                }
                Recover(item);
                AddIndex(item);
            }
            return nameIndex;
        }

        private void Recover(Annotation item)
        {
            if (item is Markup markup
                && markup.Popup != null
                && !Contains(markup.Popup))
            {
                Add(markup.Popup);
            }
            if (item is Popup popup
                && popup.Parent != null
                && !Contains(popup.Parent))
            {
                Add(popup.Parent);
            }
        }

        private void AddIndex(Annotation annotation)
        {
            if (annotation is Markup
                || annotation is Widget)
            {
                //Recovery
                if (annotation.Page == null)
                {
                    LinkPageNoUpdate(annotation);
                }

                if (string.IsNullOrEmpty(annotation.Name)
                    || (NameIndex.TryGetValue(annotation.Name, out var existing)
                    && existing != annotation))
                {
                    annotation.GenerateExistingName();
                }
                NameIndex[annotation.Name] = annotation;
            }
            else if (annotation.Page == null)
            {
                annotation.page = Page;
            }
        }

        protected override Annotation CheckIn(Annotation item)
        {
            item = base.CheckIn(item);
            AddIndex(item);
            if (item.IsQueueRefreshAppearance)
            {
                item.RefreshAppearance();
            }
            return item;
        }

        protected override Annotation CheckOut(Annotation item)
        {
            if (item != null)
                NameIndex.Remove(item.Name);
            return base.CheckOut(item);
        }

        internal PageAnnotations WithPage(PdfPage pdfPage)
        {
            Page = pdfPage;
            RefreshCache();
            return this;
        }

    }
}
