/*
  Copyright 2008-2012 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)

  This file should be part of the source code distribution of "PDF Clown library" (the
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

using PdfClown.Documents.Interaction.Annotations;
using PdfClown.Documents.Interaction.Forms;
using PdfClown.Objects;

using System;
using System.Collections.Generic;

namespace PdfClown.Documents.Interaction.Actions
{
    /// <summary>'Toggle the visibility of one or more annotations on the screen' action [PDF:1.6:8.5.3].</summary>
    [PDF(VersionEnum.PDF12)]
    public sealed class ToggleVisibility : PdfAction
    {
        /// <summary>Creates a new action within the given document context.</summary>
        public ToggleVisibility(PdfDocument context, ICollection<IPdfDataObject> objects, bool visible)
            : base(context, PdfName.Hide)
        {
            Objects = objects;
            Visible = visible;
        }

        internal ToggleVisibility(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets/Sets the annotations (or associated form fields) to be affected.</summary>
        public ICollection<IPdfDataObject> Objects
        {
            get
            {
                var objects = new List<IPdfDataObject>();
                {
                    var objectsObject = Get(PdfName.T);
                    FillObjects(objectsObject, objects);
                }
                return objects;
            }
            set
            {
                var objectsDataObject = new PdfArrayImpl();
                foreach (var item in value)
                {
                    if (item is Annotation annotation)
                    {
                        objectsDataObject.Add(annotation.Reference);
                    }
                    else if (item is Field fieldItem)
                    {
                        objectsDataObject.Add(fieldItem.FullName);
                    }
                    else
                    {
                        throw new ArgumentException(
                          "Invalid 'Hide' action target type (" + item.GetType().Name + ").\n"
                            + "It MUST be either an annotation or a form field."
                          );
                    }
                }
                this[PdfName.T] = objectsDataObject;
            }
        }

        /// <summary>Gets/Sets whether to show the annotations.</summary>
        public bool Visible
        {
            get => !GetBool(PdfName.H);
            set => Set(PdfName.H, !value);
        }

        private void FillObjects(PdfDirectObject objectObject, ICollection<IPdfDataObject> objects)
        {
            var objectDataObject = objectObject?.Resolve(null);
            if (objectDataObject is PdfArray pdfArray) // Multiple objects.
            {
                foreach (var itemObject in pdfArray.GetItems())
                {
                    FillObjects(itemObject, objects);
                }
            }
            else // Single object.
            {
                if (objectDataObject is Annotation annotation) // Annotation.
                {
                    objects.Add(annotation);
                }
                else if (objectDataObject is PdfTextString pdfString) // Form field (associated to widget annotations).
                {
                    objects.Add(Catalog.Form.Fields[pdfString.StringValue]);
                }
                else // Invalid object type.
                {
                    throw new Exception(
                      "Invalid 'Hide' action target type (" + objectDataObject.GetType().Name + ").\n"
                        + "It should be either an annotation or a form field."
                      );
                }
            }
        }

        public override string GetDisplayName() => "Toggle Visibility";
    }
}