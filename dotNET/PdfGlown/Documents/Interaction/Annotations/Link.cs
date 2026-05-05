/*
  Copyright 2008-2012 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfGlown.Documents.Contents.XObjects;
using PdfGlown.Documents.Interaction.Actions;
using PdfGlown.Documents.Interaction.Navigation;
using PdfGlown.Objects;
using SkiaSharp;
using System.Collections.Generic;

namespace PdfGlown.Documents.Interaction.Annotations
{
    /// <summary>Link annotation [PDF:1.6:8.4.5].</summary>
    /// <remarks>It represents either a hypertext link to a destination elsewhere in the document
    /// or an action to be performed.</remarks>
    [Pdf(VersionEnum.PDF10)]
    public sealed class Link : Annotation, ILink
    {
        public Link(PdfPage page, SKRect box, string text, PdfDirectObject target)
            : base(page, PdfName.Link, box, text)
        { Target = target; }

        public Link(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        public override PdfAction Action
        {
            get => base.Action;
            set
            {
                // NOTE: This entry is not permitted in link annotations if a 'Dest' entry is present.
                if (ContainsKey(PdfName.Dest)
                  && value != null)
                { Remove(PdfName.Dest); }

                base.Action = value;
            }
        }

        public PdfDirectObject Target
        {
            get
            {
                if (ContainsKey(PdfName.Dest))
                    return Destination;
                else if (ContainsKey(PdfName.A))
                    return Action;
                else
                    return null;
            }
            set
            {
                if (value is Destination destination)
                { Destination = destination; }
                else if (value is PdfAction action)
                { Action = action; }
                else
                    throw new System.ArgumentException("It MUST be either a Destination or an Action.");
            }
        }

        private Destination Destination
        {
            get
            {
                return Get(PdfName.Dest) is PdfDirectObject destinationObject
                  ? Catalog.ResolveName<Destination>(destinationObject)
                  : null;
            }
            set
            {
                if (value == null)
                { Remove(PdfName.Dest); }
                else
                {
                    // NOTE: This entry is not permitted in link annotations if an 'A' entry is present.
                    if (ContainsKey(PdfName.A))
                    { Remove(PdfName.A); }

                    Set(PdfName.Dest, value.NamedBaseObject);
                }
            }
        }

        public override SKRect RefreshAppearance(SKCanvas canvas)
        {
            //var color = Color == null ? SKColors.Black : DeviceColorSpace.CalcSKColor(Color, Alpha);
            //using (var paint = new SKPaint { Color = color })
            //{
            //    Border?.Apply(paint, null);
            //}
            return base.RefreshAppearance(canvas);
        }

        protected override FormXObject GenerateAppearance()
        {
            return null;
        }
    }
}