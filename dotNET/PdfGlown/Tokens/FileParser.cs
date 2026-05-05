/*
  Copyright 2011-2015 Stefano Chizzolini. http://www.pdfclown.org

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

using Org.BouncyCastle.Pkcs;
using PdfGlown.Bytes;
using PdfGlown.Documents.Contents.ColorSpaces;
using PdfGlown.Documents.Encryption;
using PdfGlown.Documents.Interaction.Navigation;
using PdfGlown.Objects;
using PdfGlown.Util.Parsers;

using System;
using System.Collections.Generic;
using System.IO;

namespace PdfGlown.Tokens
{
    /// <summary>PDF file parser [PDF:1.7:3.2,3.4].</summary>
    public sealed class FileParser : BaseParser
    {
        // private static readonly int EOFMarkerChunkSize = 1024; // [PDF:1.6:H.3.18].
        
        private PdfDocument document;
        private PdfEncryption encryption;
        private Stream keyStoreInputStream;
        private string password;
        private string keyAlias;
        private ISecurityHandler securityHandler;
        private AccessPermission accessPermission;

        public string KeyAlias { get => keyAlias; set => keyAlias = value; }

        internal FileParser(IInputStream stream, PdfDocument document, string password = null, Stream keyStoreInputStream = null)
            : base(stream)
        {
            this.document = document;
            this.password = password;
            this.keyStoreInputStream = keyStoreInputStream;
        }

        protected override PdfDictionary CreatePdfDictionary(Dictionary<PdfName, PdfDirectObject> dictionary, PdfName parentKey, PdfName gparentKey)
        {
            var stream = ReadStream(dictionary);
            var dictType = dictionary.Get<PdfName>(PdfName.Type) ??
                    PdfFactory.DetectDictionaryType(dictionary, parentKey, gparentKey);
            var pdfDictionary = (PdfFactory.Dictionaries.TryGetValue(dictType, out var func))
                ? func(dictionary)
                : stream != null
                    ? new PdfStream(dictionary)
                    : new PdfDictionary(dictionary);

            if (stream != null)
                ((PdfStream)pdfDictionary).SetStream(stream);
            return pdfDictionary;
        }

        protected override PdfArray CreatePdfArray(List<PdfDirectObject> array, PdfName parentKey, PdfName gparentKey)
        {
            if (parentKey != null
                && PdfFactory.Arrays.TryGetValue(parentKey, out var func))
                return func(array);
            if (Destination.IsMatch(array))
                return Destination.Create(array);
            else if (ColorSpace.IsMatch(array))
                return ColorSpace.Create(array);
            return new PdfArrayImpl(array);
        }

        private StreamSegment ReadStream(Dictionary<PdfName, PdfDirectObject> dictionary)
        {
            var stream = Stream;
            int oldOffset = (int)stream.Position;
            MoveNext();
            // Is this dictionary the header of a stream object [PDF:1.6:3.2.7]?
            if (TokenType == TokenTypeEnum.Keyword
                && CharsToken.Equals(Keyword.BeginStream, StringComparison.Ordinal))
            {
                // Keep track of current position!
                // NOTE: Indirect reference resolution is an outbound call which affects the stream pointer position,
                // so we need to recover our current position after it returns.
                long position = stream.Position;
                // Get the stream length!
                int length = dictionary.GetInt(PdfName.Length, 0);
                // Move to the stream data beginning!
                stream.Seek(position);
                SkipEOL();
                ValidateLength(dictionary, stream, ref position, ref length);
                if (length < 0)
                    length = 0;
                // Copy the stream data to the instance!                    
                var bytes = new StreamSegment(stream, length);
                stream.Skip(length);
                MoveNext(); // Postcondition (last token should be 'endstream' keyword).
                return bytes;
            }
            // Stand-alone dictionary.
            // Restores postcondition (last token should be the dictionary end).
            Stream.Seek(oldOffset);
            return null;
        }

        public override PdfDirectObject ParsePdfObject(PdfName parentKey = null)
        {
            if (TokenType == TokenTypeEnum.Reference)
            {
                var reference = ReferenceToken;
                return new PdfReference(reference.ObjectNumber, reference.GenerationNumber, document);
            }

            return base.ParsePdfObject(parentKey);
        }

        private void ValidateLength(Dictionary<PdfName, PdfDirectObject> streamHeader, IInputStream stream, ref long position, ref int length)
        {
            position = stream.Position;
            if (length <= 0)
            {
                length = RepairStreamLength(streamHeader, stream, position);
            }
            else
            {
                stream.Skip(length);
                MoveNext(); // Postcondition (last token should be 'endstream' keyword).
                if (TokenType != TokenTypeEnum.Keyword
                    || !CharsToken.Equals(Keyword.EndStream, StringComparison.Ordinal))
                {
                    stream.Seek(position);
                    length = RepairStreamLength(streamHeader, stream, position);
                }
            }
            stream.Seek(position);
        }

        private int RepairStreamLength(Dictionary<PdfName, PdfDirectObject> streamHeader, IInputStream stream, long position)
        {
            int length;
            System.Diagnostics.Debug.Write($"warning: Repair Stream Object missing {PdfName.Length} header parameter");
            if (SkipKey(Keyword.EndStream))
            {
                length = (int)(stream.Position - position);
                streamHeader[PdfName.Length] = PdfInteger.Get(length);
            }
            else
            {
                throw new Exception($"Pdf Stream Object missing {Keyword.EndStream} Keyword");
            }

            return length;
        }

        public PdfDirectObject ParsePdfObjectWithLock(XRefEntry xrefEntry, PdfName parentKey)
        {
            lock (document.LockObject)
            {
                return ParsePdfObject(xrefEntry, parentKey);
            }
        }

        /// <summary>Parses the specified PDF indirect object [PDF:1.6:3.2.9].</summary>
        /// <param name="xrefEntry">Cross-reference entry of the indirect object to parse.</param>
        public PdfDirectObject ParsePdfObject(XRefEntry xrefEntry, PdfName parentKey)            
        {
            // Go to the beginning of the indirect object!
            Seek(xrefEntry.Offset);
            bool started = false;
            while (MoveNextComplex() && !started)
            {
                if (IsInderectObjectEnd())
                    return default;
                else if (TokenType == TokenTypeEnum.InderectObject
                    || IsInderectObjectBegin())
                {
                    // Skip the indirect-object header!
                    started = true;
                }
                else if (TokenType == TokenTypeEnum.DictionaryBegin
                    || TokenType == TokenTypeEnum.ArrayBegin)
                {
                    break;
                }
            }

            // Get the indirect data object!
            var dataObject = ParsePdfObject(parentKey);
            if (securityHandler != null)
            {
                Stream.Mark();
                securityHandler.Decrypt(dataObject, xrefEntry.Number, xrefEntry.Generation);
                Stream.ResetMark();
            }
            return dataObject;
        }

        private bool IsInderectObjectEnd()
        {
            return TokenType == TokenTypeEnum.Keyword
                && CharsToken.Equals(Keyword.EndIndirectObject, StringComparison.Ordinal);
        }

        private bool IsInderectObjectBegin()
        {
            return TokenType == TokenTypeEnum.Keyword
                && CharsToken.Equals(Keyword.BeginIndirectObject, StringComparison.Ordinal);
        }

        /// <summary>Retrieves the PDF version of the file [PDF:1.6:3.4.1].</summary>
        public string RetrieveVersion()
        {
            IInputStream stream = Stream;
            stream.Seek(0);
            string header = stream.ReadString(10);
            if (!header.StartsWith(Keyword.BOF))
                throw new PostScriptParseException("PDF header not found.", this);

            return header.Substring(Keyword.BOF.Length, 3);
        }

        /// <summary>Retrieves the starting position of the last xref-table section [PDF:1.6:3.4.4].</summary>
        public long RetrieveXRefOffset()
        {
            // [FIX:69] 'startxref' keyword not found (file was corrupted by alien data in the tail).
            IInputStream stream = Stream;
            var streamLength = stream.Length;

            long position = SeekRevers(streamLength, Keyword.StartXRef);
            if (position < 0)
                throw new PostScriptParseException("'" + Keyword.StartXRef + "' keyword not found.", this);

            // Go past the 'startxref' keyword!
            stream.Seek(position); MoveNext();

            // Get the xref offset!
            MoveNext();
            if (TokenType != TokenTypeEnum.Integer)
                throw new PostScriptParseException("'" + Keyword.StartXRef + "' value invalid.", this);
            long xrefPosition = IntegerToken;

            stream.Seek(xrefPosition);
            MoveNextComplex();
            //Repair 
            if (xrefPosition > streamLength
                || (TokenType == TokenTypeEnum.Keyword && !CharsToken.Equals(Keyword.XRef, StringComparison.Ordinal))
                || (TokenType != TokenTypeEnum.InderectObject && TokenType != TokenTypeEnum.Keyword))
            {
                xrefPosition = SeekRevers(streamLength, "\n" + Keyword.XRef);
                if (xrefPosition >= 0)
                    xrefPosition++;
            }
            return xrefPosition;
        }

        private long SeekRevers(long startPosition, string keyWord)
        {
            Seek(startPosition);
            return SkipKeyRevers(keyWord) ? Position : -1;
            //string text = null;
            //long streamLength = stream.Length;
            //long position = startPosition;
            //int chunkSize = (int)Math.Min(streamLength, EOFMarkerChunkSize);
            //int index = -1;

            //while (index < 0 && position > 0)
            //{
            //    /*
            //      NOTE: This condition prevents the keyword from being split by the chunk boundary.
            //    */
            //    if (position < streamLength)
            //    { position += keyWord.Length; }
            //    position -= chunkSize;
            //    if (position < 0)
            //    { position = 0; }
            //    stream.Seek(position);

            //    text = stream.ReadString(chunkSize);
            //    index = text.LastIndexOf(keyWord, StringComparison.Ordinal);
            //}
            //return index < 0 ? -1 : position + index;
        }

        /// <summary>Prepare for decryption.</summary>
        /// <exception cref="IOException"></exception>
        public void PrepareDecryption()
        {
            if (encryption != null)
            {
                return;
            }
            encryption = document.Encryption;
            if (encryption == null)
            {
                return;
            }

            try
            {
                DecryptionMaterial decryptionMaterial;
                if (keyStoreInputStream != null)
                {
                    var builder = new Pkcs12StoreBuilder();
                    var ks = builder.Build();
                    ks.Load(keyStoreInputStream, password.ToCharArray());// KeyStore.getInstance("PKCS12");
                    decryptionMaterial = new PublicKeyDecryptionMaterial(ks, keyAlias, password);
                }
                else
                {
                    decryptionMaterial = new StandardDecryptionMaterial(password);
                }

                securityHandler = encryption.SecurityHandler;
                securityHandler.PrepareForDecryption(encryption, document.ID, decryptionMaterial);
                accessPermission = securityHandler.CurrentAccessPermission;
            }
            catch (IOException)
            {
                throw;
            }
            catch (Org.BouncyCastle.Security.GeneralSecurityException e)
            {
                throw new IOException($"Error ({e.Message}) while creating security handler for decryption", e);
            }
            finally
            {
                keyStoreInputStream?.Dispose();
            }
        }
    }
}