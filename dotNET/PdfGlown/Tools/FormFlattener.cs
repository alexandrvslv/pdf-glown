/*
  Copyright 2015 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfGlown.Documents.Contents.XObjects;
using PdfGlown.Documents.Interaction.Annotations;
using PdfGlown.Documents.Interaction.Forms;
using PdfGlown.Objects;

using System.Collections.Generic;
using SkiaSharp;

namespace PdfGlown.Tools
{
    /// <summary>Tool to flatten Acroforms.</summary>
    public sealed class FormFlattener
    {
        private bool hiddenRendered;
        private bool nonPrintableRendered;

        /// <summary>Replaces the Acroform fields with their corresponding graphics representation.</summary>
        /// <param name="document">Document to flatten.</param>
        public void Flatten(PdfDocument document)
        {
            var pageStampers = new Dictionary<PdfPage, PageStamper>();
            AcroForm form = document.Catalog.Form;
            Fields formFields = form.Fields;
            foreach (Field field in formFields.Values)
            {
                foreach (Widget widget in field.Widgets)
                {
                    var widgetPage = widget.Page;
                    AnnotationFlagsEnum flags = widget.Flags;
                    // Is the widget to be rendered?
                    if (((flags & AnnotationFlagsEnum.Hidden) == 0 || hiddenRendered)
                      && ((flags & AnnotationFlagsEnum.Print) > 0 || nonPrintableRendered))
                    {
                        // Stamping the current state appearance of the widget...
                        var widgetCurrentState = widget.Get<PdfName>(PdfName.AS);
                        FormXObject widgetCurrentAppearance = widget.Appearance.Normal[widgetCurrentState];
                        if (widgetCurrentAppearance != null)
                        {
                            PageStamper widgetStamper;
                            if (!pageStampers.TryGetValue(widgetPage, out widgetStamper))
                            { pageStampers[widgetPage] = widgetStamper = new PageStamper(widgetPage); }

                            SKRect widgetBox = widget.GetViewBounds();
                            widgetStamper.Foreground.ShowXObject(widgetCurrentAppearance, widgetBox.Location, widgetBox.Size);
                        }
                    }

                    // Removing the widget from the page annotations...
                    PageAnnotations widgetPageAnnotations = widgetPage.Annotations;
                    widgetPageAnnotations.Remove(widget);
                    if (widgetPageAnnotations.Count == 0)
                    {
                        widgetPage.Annotations = null;
                        widgetPageAnnotations.Delete();
                    }

                    // Removing the field references relating the widget...
                    PdfDictionary fieldPartDictionary = widget;
                    while (fieldPartDictionary != null)
                    {
                        var parentFieldPartDictionary = fieldPartDictionary.Get<PdfDictionary>(PdfName.Parent);

                        PdfArray kidsArray;
                        if (parentFieldPartDictionary != null)
                        { kidsArray = parentFieldPartDictionary.Get<PdfArray>(PdfName.Kids); }
                        else
                        { kidsArray = formFields.DataObject; }

                        kidsArray.Remove(fieldPartDictionary.Reference);
                        fieldPartDictionary.Delete();
                        if (kidsArray.Count > 0)
                            break;

                        fieldPartDictionary = parentFieldPartDictionary;
                    }
                }
            }
            if (formFields.Count == 0)
            {
                // Removing the form root...
                document.Catalog.Form = null;
                form.Delete();
            }
            foreach (PageStamper pageStamper in pageStampers.Values)
            { pageStamper.Flush(); }
        }

        /// <summary>Gets/Sets whether hidden fields have to be rendered.</summary>
        public bool HiddenRendered
        {
            get => hiddenRendered;
            set => hiddenRendered = value;
        }

        /// <summary>Gets/Sets whether non-printable fields have to be rendered.</summary>
        public bool NonPrintableRendered
        {
            get => nonPrintableRendered;
            set => nonPrintableRendered = value;
        }
    }
}
