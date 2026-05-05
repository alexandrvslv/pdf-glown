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

using PdfGlown.Documents.Contents.ColorSpaces;
using PdfGlown.Objects;
using System.Linq;

namespace PdfGlown.Documents.Contents.Objects
{
    /// <summary>'Set the color to use for nonstroking operations' operation [PDF:1.6:4.5.7].</summary>
    [Pdf(VersionEnum.PDF12)]
    public abstract class SetFillColor : SetColor
    {
        public SetFillColor(string @operator, PdfArray operands)
            : base(@operator, operands)
        { }

        protected SetFillColor(string @operator, IColor value)
            : base(@operator, value.Components)
        { }

        /// <param name="operator">Graphics operator.</param>
        /// <param name="name">Name of the color resource entry (see <see cref="Patterns"/>).</param>
        protected SetFillColor(string @operator, PdfName name)
            : this(@operator, name, null)
        { }

        /// <param name="operator">Graphics operator.</param>
        /// <param name="name">Name of the color resource entry (see <see cref="Patterns"/>).</param>
        /// <param name="underlyingColor">Color used to colorize the pattern.</param>
        protected SetFillColor(string @operator, PdfName name, IColor underlyingColor)
            : base(@operator, underlyingColor?.Components ?? PdfArray.Empty)
        {
            operands.AddSimple(name);
        }

        public override void Scan(GraphicsState state)
        {
            state.FillColor = state.FillColorSpace.GetColor(operands, state.Scanner.Context);
        }
    }
}