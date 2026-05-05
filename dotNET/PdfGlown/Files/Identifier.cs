/*
  Copyright 2012 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfGlown.Objects;
using PdfGlown.Tokens;
using PdfGlown.Util.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace PdfGlown.Files
{
    /// <summary>File identifier [PDF:1.7:10.3].</summary>
    public sealed class Identifier : PdfArray
    {
        private static void Digest(BinaryWriter buffer, object value)
        {
            buffer.Write(value.ToString());
        }

        private static List<PdfDirectObject> CreateRawData()
        {
            return new (2) { PdfString.Default, PdfString.Default };
        }

        /// <summary>Creates a new direct file identifier.</summary>
        public Identifier() 
            : this(CreateRawData())
        { }

        /// <summary>Creates a new indirect file identifier.</summary>
        public Identifier(PdfDocument context) 
            : base(context, CreateRawData())
        { }

        /// <summary>Instantiates an existing file identifier.</summary>
        /// <param nme="baseObject">Base object.</param>
        internal Identifier(List<PdfDirectObject> baseObject) 
            : base(baseObject)
        { }

        /// <summary>Gets the permanent identifier based on the contents of the file at the time it was
        /// originally created.</summary>
        public PdfString BaseID
        {
            get => Get<PdfString>(0);
            set => Set(0, value);
        }

        /// <summary>Gets the changing identifier based on the file's contents at the time it was last
        /// updated.</summary>
        public PdfString VersionID
        {
            get => Get<PdfString>(1);
            set => Set(1, value);
        }

        /// <summary>Computes a new version identifier based on the file's contents.</summary>
        /// <remarks>This method is typically invoked internally during file serialization.</remarks>
        /// <param name="writer">File serializer.</param>
        public void Update(Writer writer)
        {
            // NOTE: To help ensure the uniqueness of file identifiers, it is recommended that they are
            // computed by means of a message digest algorithm such as MD5 [PDF:1.7:10.3].
            using var md5 = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
            using var buffer = new BinaryWriter(new MemoryStream(), Charset.ISO88591, false);
            var file = writer.Document;
            try
            {
                // File identifier computation is fulfilled with this information:
                // a) Current time.
                Digest(buffer, DateTime.Now.Ticks);

                // b) File location.
                if (file.Path != null)
                { Digest(buffer, file.Path); }

                // c) File size.
                Digest(buffer, writer.Stream.Length);

                // d) Entries in the document information dictionary.
                foreach (var informationObjectEntry in ((IDictionary<PdfName, object>)file.Information))
                {
                    Digest(buffer, informationObjectEntry.Key);
                    Digest(buffer, informationObjectEntry.Value);
                }
            }
            catch (Exception e)
            { throw new Exception("File identifier digest failed.", e); }


            //NOTE: File identifier is an array of two byte strings [PDF:1.7:10.3]:
            //1) a permanent identifier based on the contents of the file at the time it was
            // originally created. It does not change when the file is incrementally updated;
            //2) a changing identifier based on the file's contents at the time it was last updated.
            // When a file is first written, both identifiers are set to the same value. If both
            // identifiers match when a file reference is resolved, it is very likely that the correct
            // file has been found. If only the first identifier matches, a different version of the
            // correct file has been found.

            var versionID = new PdfString(
              md5.Digest(((MemoryStream)buffer.BaseStream).AsSpan()),
              PdfString.SerializationModeEnum.Hex);
            Set(1, versionID);
            if (Get(0).Equals(PdfString.Default))
            { Set(0, versionID); }
        }
    }
}