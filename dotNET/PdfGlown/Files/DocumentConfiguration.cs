/*
  Copyright 2015 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)

  This file should be part of the source code distribution of "PDF Glown library"
  (the Program): see the accompanying README files for more info.

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

namespace PdfGlown.Files
{
    /// <summary>File configuration.</summary>
    public sealed class DocumentConfiguration
    {
        private string realFormat;
        private bool streamFilterEnabled;
        private XRefModeEnum xrefMode = XRefModeEnum.Plain;

        private readonly PdfDocument document;

        internal DocumentConfiguration(PdfDocument document)
        {
            this.document = document;

            RealPrecision = 0;
            StreamFilterEnabled = true;
        }

        /// <summary>Gets the file associated with this configuration.</summary>
        public PdfDocument Document => document;

        /// <summary>Gets/Sets the number of decimal places applied to real numbers' serialization.</summary>
        public int RealPrecision
        {
            get => realFormat.Length - realFormat.IndexOf('.') - 1;
            set => realFormat = "0." + new string('#', value <= 0 ? 5 : value);
        }

        /// <summary>Gets/Sets whether PDF stream objects have to be filtered for compression.</summary>
        public bool StreamFilterEnabled
        {
            get => streamFilterEnabled;
            set => streamFilterEnabled = value;
        }

        /// <summary>Gets the document's cross-reference mode.</summary>
        public XRefModeEnum XRefMode
        {
            get => xrefMode;
            set
            {
                xrefMode = value;
                document.CheckCompatibility(xrefMode == XRefModeEnum.Compressed ? VersionEnum.PDF15 : VersionEnum.PDF10);
            }
        }

        internal string RealFormat => realFormat;
    }
}
