/*
  Copyright 2009-2012 Stefano Chizzolini. http://www.pdfclown.org

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

namespace PdfGlown.Documents.Contents.Objects
{
    /// <summary>'Set the specified graphics state parameters' operation [PDF:1.6:4.3.3].</summary>
    [Pdf(VersionEnum.PDF12)]
    public sealed class ApplyExtGState : Operation, IResourceReference<ExtGState>
    {
        public static readonly string OperatorKeyword = "gs";

        public ApplyExtGState(PdfName name) : base(OperatorKeyword, name)
        { }

        public ApplyExtGState(PdfArray operands) : base(OperatorKeyword, operands)
        { }

        public PdfName Name
        {
            get => (PdfName)operands.Get(0);
            set => operands.SetSimple(0, value);
        }

        /// <summary>Gets the<see cref="ExtGState">graphics state parameters</see> resource to be set.
        /// </summary>
        /// <param name="context"> Content context.</param>
        public ExtGState GetResource(ContentScanner context)
        {
            var pcontext = context;
            ExtGState gstate;
            while ((gstate = pcontext.Context.Resources.ExtGStates[Name]) == null
                && (pcontext = pcontext.ResourceParent) != null)
            { }
            return gstate;
        }

        public override void Scan(GraphicsState state) => GetResource(state.Scanner)?.ApplyTo(state);


    }
}