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

using PdfGlown.Documents.Files;
using PdfGlown.Documents.Interaction.Navigation;
using PdfGlown.Objects;

using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Interaction.Actions
{
    /// <summary>'Change the view to a specified destination in a PDF file embedded in another PDF file'
    /// action [PDF:1.6:8.5.3].</summary>
    [Pdf(VersionEnum.PDF11)]
    public sealed partial class GoToEmbedded : GotoNonLocal<Destination>
    {
        private PathElement destinationPath;

        /// <summary>Creates a new instance within the specified document context, pointing to a
        /// destination within an embedded document.</summary>
        /// <param name="context">Document context.</param>
        /// <param name="destinationPath">Path information to the target document within the destination
        /// file.</param>
        /// <param name="destination">Destination within the target document.</param>
        public GoToEmbedded(PdfDocument context, PathElement destinationPath, Destination destination)
            : this(context, null, destinationPath, destination)
        { }

        /// <summary>Creates a new instance within the specified document context, pointing to a
        /// destination within another document.</summary>
        /// <param name="context">Document context.</param>
        /// <param name="destinationFile">File in which the destination is located.</param>
        /// <param name="destination">Destination within the target document.</param>
        public GoToEmbedded(PdfDocument context, FileSpecification destinationFile, Destination destination)
            : this(context, destinationFile, null, destination)
        { }

        /// <summary>Creates a new instance within the specified document context.</summary>
        /// <param name="context">Document context.</param>
        /// <param name="destinationFile">File in which the destination is located.</param>
        /// <param name="destinationPath">Path information to the target document within the destination
        /// file.</param>
        /// <param name="destination">Destination within the target document.</param>
        public GoToEmbedded(PdfDocument context, FileSpecification destinationFile, PathElement destinationPath, Destination destination)
            : base(context, PdfName.GoToE, destinationFile, destination)
        { DestinationPath = destinationPath; }

        internal GoToEmbedded(Dictionary<PdfName, PdfDirectObject> baseObject) : base(baseObject)
        { }

        /// <summary>Gets/Sets the path information to the target document.</summary>
        public PathElement DestinationPath
        {
            get => destinationPath ??= new PathElement(PdfName.T);
            set => Set(PdfName.T, destinationPath = value);
        }

        public override string GetDisplayName() => "Go To Embedded";
    }
}