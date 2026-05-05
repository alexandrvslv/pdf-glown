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
    /// <summary>'Change the view to a specified destination in another PDF file' action
    /// [PDF:1.6:8.5.3].</summary>
    [Pdf(VersionEnum.PDF11)]
    public sealed class GoToRemote : GotoNonLocal<RemoteDestination>
    {
        /// <summary>Creates a new action within the given document context.</summary>
        public GoToRemote(PdfDocument context, FileSpecification destinationFile, RemoteDestination destination)
            : base(context, PdfName.GoToR, destinationFile, destination)
        { }

        internal GoToRemote(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        public override IFileSpecification DestinationFile
        {
            get => base.DestinationFile;
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                base.DestinationFile = value;
            }
        }

        public override string GetDisplayName() => "Go To " + DestinationFile?.FilePath;
    }
}