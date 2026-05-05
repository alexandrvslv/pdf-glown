/*
  Copyright 2006-2012 Stefano Chizzolini. http://www.pdfclown.org

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
using System;

namespace PdfGlown.Documents.Contents.ColorSpaces
{
    /// <summary>Device Gray color value [PDF:1.6:4.5.3].</summary>
    [Pdf(VersionEnum.PDF11)]
    public sealed class GrayColor : DeviceColor, IEquatable<GrayColor>
    {
        public static readonly GrayColor Black = new GrayColor(0);
        public static readonly GrayColor White = new GrayColor(1);

        public static readonly GrayColor Default = Black;

        /// <summary>Gets the color corresponding to the specified components.</summary>
        /// <param name="components">Color components to convert.</param>
        public static new GrayColor Get(PdfArray components)
        {
            return components != null
                ? new GrayColor(GrayColorSpace.Default, components)
                : Default;
        }

        public GrayColor(float g)
            : this(GrayColorSpace.Default, new PdfArrayImpl(1) { NormalizeComponent(g) })
        { }

        internal GrayColor(DeviceColorSpace colorSpace, PdfArray components)
            : base(colorSpace, components)
        { }


        /// <summary>Gets/Sets the gray component.</summary>
        public float G
        {
            get => this[0];
            set => this[0] = value;
        }

        public override bool IsZero => Equals(Black);

        public bool Equals(GrayColor other)
        {
            if (other == null)
                return false;
            return G.Equals(other.G);
        }

    }
}