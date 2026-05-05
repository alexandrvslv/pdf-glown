/*
  Copyright 2010-2011 Stefano Chizzolini. http://www.pdfclown.org

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
using System.Collections.Generic;
using System.Linq;

namespace PdfGlown.Documents.Contents.ColorSpaces
{
    /// <summary>Special color space that can contain an arbitrary number of color components [PDF:1.6:4.5.5].</summary>
    [Pdf(VersionEnum.PDF13)]
    public sealed class NColorSpace : SpecialDeviceColorSpace
    {
        IList<string> componentNames;
        private Color defaultColor;

        //TODO:IMPL new element constructor!

        internal NColorSpace(List<PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        public override int ComponentCount => ComponentArray.Count;

        private PdfArray ComponentArray
        {
            get => Get<PdfArray>(1);
        }

        public override IList<string> ComponentNames => componentNames ??= GetComponentNames();

        public override Color DefaultColor => defaultColor ??= GetDefaultColor();

        private IList<string> GetComponentNames()
        {
            var componentNames = new List<string>();
            foreach (var nameObject in ComponentArray.GetItems().OfType<IPdfString>())
            {
                componentNames.Add(nameObject.StringValue);
            }

            return componentNames;
        }

        private Color GetDefaultColor()
        {
            var components = new PdfArrayImpl(ComponentCount);
            for (int index = 0, length = components.Capacity; index < length; index++)
            { components.Add(1); }

            return new NColor(this, components);
        }

        public override IColor GetColor(PdfArray components, IContentContext context)
            => components == null ? DefaultColor : new NColor(this, components);

        public override bool IsSpaceColor(IColor color) => color is NColor;
    }
}