/*
  Copyright 2006-2015 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfGlown.Bytes;
using PdfGlown.Files;
using PdfGlown.Objects;

using System;

namespace PdfGlown.Tokens
{
    /// <summary>PDF file writer.</summary>
    public abstract class Writer
    {
        private static readonly byte[] BOFChunk = BaseEncoding.Pdf.Encode(Keyword.BOF);
        private static readonly byte[] EOFChunk = BaseEncoding.Pdf.Encode(Symbol.LineFeed + Keyword.EOF + Symbol.CarriageReturn + Symbol.LineFeed);
        private static readonly byte[] HeaderBinaryHintChunk = new byte[] { (byte)Symbol.LineFeed, (byte)Symbol.Percent, 0x80, 0x80, 0x80, 0x80, (byte)Symbol.LineFeed }; // NOTE: Arbitrary binary characters (code >= 128) for ensuring proper behavior of file transfer applications [PDF:1.6:3.4.1].
        private static readonly byte[] StartXRefChunk = BaseEncoding.Pdf.Encode(Keyword.StartXRef + Symbol.LineFeed);

        /// <summary>Gets a new writer instance for the specified file.</summary>
        /// <param name="document">File to serialize.</param>
        /// <param name="stream">Target stream.</param>
        public static Writer Get(PdfDocument document, IOutputStream stream)
        {
            // Which cross-reference table mode?
            switch (document.Configuration.XRefMode)
            {
                case XRefModeEnum.Plain:
                    return new PlainWriter(document, stream);
                case XRefModeEnum.Compressed:
                    return new CompressedWriter(document, stream);
                default:
                    throw new NotSupportedException();
            }
        }

        protected readonly PdfDocument document;
        protected readonly IOutputStream stream;

        protected Writer(PdfDocument document, IOutputStream stream)
        {
            this.document = document;
            this.stream = stream;
        }

        /// <summary>Gets the file to serialize.</summary>
        public PdfDocument Document => document;

        /// <summary>Gets the target stream.</summary>
        public IOutputStream Stream => stream;

        /// <summary>Serializes the <see cref="Document">file</see> to the <see cref="Stream">target stream</see>.</summary>
        /// <param name="mode">Serialization mode.</param>
        public void Write(SerializationModeEnum mode)
        {
            switch (mode)
            {
                case SerializationModeEnum.Incremental:
                    if (document.Reader == null)
                        goto case SerializationModeEnum.Standard;

                    WriteIncremental();
                    break;
                case SerializationModeEnum.Standard:
                    WriteStandard();
                    break;
                case SerializationModeEnum.Linearized:
                    WriteLinearized();
                    break;
            }
        }

        /// <summary>Updates the specified trailer.</summary>
        /// <remarks>This method has to be called just before serializing the trailer object.</remarks>
        protected void UpdateTrailer(PdfDictionary trailer, IOutputStream stream)
        {
            // File identifier update.
            var identifier = trailer.GetOrCreate<Identifier>(PdfName.ID);            
            identifier.Update(this);
        }

        /// <summary>Serializes the beginning of the file [PDF:1.6:3.4.1].</summary>
        protected void WriteHeader()
        {
            stream.Write(BOFChunk);
            stream.Write(document.Catalog.Version.ToString()); // NOTE: Document version represents the actual (possibly-overridden) file version.
            stream.Write(HeaderBinaryHintChunk);
        }

        /// <summary>Serializes the PDF file as incremental update [PDF:1.6:3.4.5].</summary>
        protected abstract void WriteIncremental();

        /// <summary>Serializes the PDF file linearized [PDF:1.6:F].</summary>
        protected abstract void WriteLinearized();

        /// <summary>Serializes the PDF file compactly [PDF:1.6:3.4].</summary>
        protected abstract void WriteStandard();

        /// <summary>Serializes the end of the file [PDF:1.6:3.4.4].</summary>
        /// <param name="startxref">Byte offset from the beginning of the file to the beginning
        ///  of the last cross-reference section.</param>
        protected void WriteTail(long startxref)
        {
            stream.Write(StartXRefChunk);
            stream.Write(startxref.ToString());
            stream.Write(EOFChunk);
        }
    }
}
