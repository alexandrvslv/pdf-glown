/*
  Copyright 2010-2012 Stefano Chizzolini. http://www.pdfclown.org

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
using System.Collections.Generic;

namespace PdfGlown.Documents.Interaction.Actions
{
    /// <summary>Abstract 'go to non-local destination' action.</summary>
    [Pdf(VersionEnum.PDF11)]
    public abstract class GotoNonLocal<T> : GoToDestination<T>
      where T : Destination
    {
        private IFileSpecification fileSpec;

        protected GotoNonLocal(PdfDocument context, PdfName actionType, FileSpecification destinationFile, T destination)
            : base(context, actionType, destination)
        { DestinationFile = destinationFile; }

        protected GotoNonLocal(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets/Sets the file in which the destination is located.</summary>
        public virtual IFileSpecification DestinationFile
        {
            get => fileSpec ??= IFileSpecification.Wrap(Get(PdfName.F));
            set => Set(PdfName.F, (fileSpec = value)?.RefOrSelf);
        }

        /// <summary>Gets/Sets the action options.</summary>
        public OptionsEnum Options
        {
            get
            {
                OptionsEnum options = 0;
                if (this.GetBool(PdfName.NewWindow))
                { options |= OptionsEnum.NewWindow; }
                return options;
            }
            set
            {
                if ((value & OptionsEnum.NewWindow) == OptionsEnum.NewWindow)
                { this[PdfName.NewWindow] = PdfBoolean.True; }
                else if ((value & OptionsEnum.SameWindow) == OptionsEnum.SameWindow)
                { this[PdfName.NewWindow] = PdfBoolean.False; }
                else
                { this.Remove(PdfName.NewWindow); } // NOTE: Forcing the absence of this entry ensures that the viewer application should behave in accordance with the current user preference.
            }
        }
    }
}