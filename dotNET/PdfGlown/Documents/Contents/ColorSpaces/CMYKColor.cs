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
    /// <summary>Device Cyan-Magenta-Yellow-Key color value [PDF:1.6:4.5.3].</summary>
    /// <remarks>The 'Key' component is renamed 'Black' to avoid semantic
    /// ambiguities.</remarks>
    [Pdf(VersionEnum.PDF11)]
    public sealed class CMYKColor : DeviceColor, IEquatable<CMYKColor>
    {
        public static readonly CMYKColor Black = new CMYKColor(0, 0, 0, 1);
        public static readonly CMYKColor White = new CMYKColor(0, 0, 0, 0);

        public static readonly CMYKColor Default = Black;

        public CMYKColor(double c, double m, double y, double k)
            : this(CMYKColorSpace.Default, new PdfArrayImpl(4) {
                      NormalizeComponent(c),
                      NormalizeComponent(m),
                      NormalizeComponent(y),
                      NormalizeComponent(k)
                  })
        { }

        internal CMYKColor(DeviceColorSpace colorSpace, PdfArray components)
            : base(colorSpace, components)
        { }

        /// <summary>Gets/Sets the cyan component.</summary>
        public float C
        {
            get => this[0];
            set => this[0] = value;
        }

        /// <summary>Gets/Sets the magenta component.</summary>
        public float M
        {
            get => this[1];
            set => this[1] = value;
        }

        /// <summary>Gets/Sets the yellow component.</summary>
        public float Y
        {
            get => this[2];
            set => this[2] = value;
        }

        /// <summary>Gets/Sets the black (key) component.</summary>
        public float K
        {
            get => this[3];
            set => this[3] = value;
        }

        public override bool IsZero => Equals(White);

        public bool Equals(CMYKColor other)
        {
            if (other == null)
                return false;
            return C.Equals(other.C)
                && M.Equals(other.M)
                && Y.Equals(other.Y)
                && K.Equals(other.K);
        }

    }
}