/*
  Copyright 2010-2015 Stefano Chizzolini. http://www.pdfclown.org

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

using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfGlown.Tokens
{
    /// <summary>PDF file writer implementing compressed cross-reference stream [PDF:1.6:3.4.7].</summary>
    internal sealed class CompressedWriter : Writer
    {
        /// <summary>Maximum number of objects in individual object streams [PDF:1.7:H:19].</summary>
        private static readonly int ObjectStreamMaxEntryCount = 100;

        internal CompressedWriter(PdfDocument document, IOutputStream stream) 
            : base(document, stream)
        { }

        protected override void WriteIncremental()
        {
            // 1. Original content (header, body and previous trailer).
            var parser = document.Reader.Parser;
            stream.Write(parser.Stream);

            // 2. Body update (modified indirect objects insertion).
            XRefEntry xrefStreamEntry;
            {
                // Create the xref stream!
                // NOTE: Incremental xref information structure comprises multiple sections; this update adds
                // a new section.
                var xrefStream = new XRefStream(document);

                // 2.1. Indirect objects.
                var indirectObjects = document.IndirectObjects;

                // 2.1.1. Modified indirect objects serialization.
                XRefEntry prevFreeEntry = null;
                // NOTE: Any uncompressed indirect object will be compressed.
                ObjectStream objectStream = null;
                // NOTE: Any previously-compressed indirect object will have its original object stream
                // updated through a new extension object stream.
                var extensionObjectStreams = new Dictionary<int, ObjectStream>();
                int indirectObjectsPrecompressCount = indirectObjects.Count;
                foreach (var indirectObject in indirectObjects.ModifiedObjects.OrderBy(x => x.Key).Select(x => x.Value).ToList())
                {
                    if (indirectObject.IsCompressible())
                    {
                        if (objectStream == null
                          || objectStream.Count >= ObjectStreamMaxEntryCount)
                        { document.Register(objectStream = new ObjectStream()); }

                        indirectObject.Compress(objectStream);
                    }

                    prevFreeEntry = AddXRefEntry(
                      indirectObject,
                      xrefStream,
                      prevFreeEntry,
                      extensionObjectStreams);
                }
                // 2.1.2. Additional object streams serialization.
                for (int index = indirectObjectsPrecompressCount, limit = indirectObjects.Count; index < limit; index++)
                {
                    prevFreeEntry = AddXRefEntry(
                      indirectObjects[index],
                      xrefStream,
                      prevFreeEntry,
                      null);
                }
                if (prevFreeEntry != null)
                {
                    prevFreeEntry.Offset = 0; // Links back to the first free object. NOTE: The first entry in the table (object number 0) is always free.
                }

                // 2.2. XRef stream.
                UpdateTrailer(xrefStream, stream);
                xrefStream.Set(PdfName.Prev, (int)parser.RetrieveXRefOffset());
                AddXRefEntry(
                  // NOTE: This xref stream indirect object is purposely temporary (i.e. not registered into
                  // the file's indirect objects collection).
                  new PdfIndirectObject(
                      document,
                      xrefStream,
                      xrefStreamEntry = new XRefEntry(indirectObjects.Count, 0, (int)stream.Length, XRefEntry.UsageEnum.InUse)),
                  xrefStream,
                  null,
                  null);
            }

            // 3. Tail.
            WriteTail(xrefStreamEntry.Offset);
        }

        protected override void WriteLinearized()
        { throw new NotImplementedException(); }

        protected override void WriteStandard()
        {
            // 1. Header [PDF:1.6:3.4.1].
            WriteHeader();

            // 2. Body [PDF:1.6:3.4.2,3,7].
            XRefEntry xrefStreamEntry;
            {
                // Create the xref stream!
                // NOTE: Standard xref information structure comprises just one section; the xref stream is
                // generated on-the-fly and kept volatile not to interfere with the existing file structure.
                var xrefStream = new XRefStream(document);

                // 2.1. Indirect objects.
                var indirectObjects = document.IndirectObjects;

                // Indirect objects serialization.
                XRefEntry prevFreeEntry = null;
                ObjectStream objectStream = null;
                foreach (var indirectObject in indirectObjects)
                {
                    if (indirectObject.IsCompressible())
                    {
                        if (objectStream == null
                          || objectStream.Count >= ObjectStreamMaxEntryCount)
                        { document.Register(objectStream = new ObjectStream()); }

                        indirectObject.Compress(objectStream);
                    }

                    prevFreeEntry = AddXRefEntry(indirectObject, xrefStream, prevFreeEntry, null);
                }
                prevFreeEntry.Offset = 0; // Links back to the first free object. NOTE: The first entry in the table (object number 0) is always free.

                // 2.2. XRef stream.
                UpdateTrailer(xrefStream, stream);
                xrefStreamEntry = new XRefEntry(indirectObjects.Count, 0, (int)stream.Length, XRefEntry.UsageEnum.InUse);
                // NOTE: This xref stream indirect object is purposely temporary (i.e. not registered into
                // the file's indirect objects collection).
                AddXRefEntry(new PdfIndirectObject(document, xrefStream, xrefStreamEntry), xrefStream, null, null);
            }

            // 3. Tail.
            WriteTail(xrefStreamEntry.Offset);
        }

        /// <summary>Adds an indirect object entry to the specified xref stream.</summary>
        /// <param name="indirectObject">Indirect object.</param>
        /// <param name="xrefStream">XRef stream.</param>
        /// <param name="prevFreeEntry">Previous free xref entry.</param>
        /// <param name="extensionObjectStreams">Object streams used in incremental updates to extend
        ///   modified ones.</param>
        /// <returns>Current free xref entry.</returns>
        private XRefEntry AddXRefEntry(PdfIndirectObject indirectObject, XRefStream xrefStream, XRefEntry prevFreeEntry, Dictionary<int, ObjectStream> extensionObjectStreams)
        {
            var xrefEntry = indirectObject.XrefEntry;

            // Add the entry to the xref stream!
            xrefStream[xrefEntry.Number] = xrefEntry;

            // Serialize the entry contents!
            switch (xrefEntry.Usage)
            {
                case XRefEntry.UsageEnum.InUse:
                    {
                        int offset = (int)stream.Length;
                        // Add entry content!
                        indirectObject.WriteTo(stream, document);
                        // Set entry content's offset!
                        xrefEntry.Offset = offset;
                    }
                    break;
                case XRefEntry.UsageEnum.InUseCompressed:
                    // NOTE: Serialization is delegated to the containing object stream.
                    if (extensionObjectStreams != null) // Incremental update.
                    {
                        int baseStreamNumber = xrefEntry.StreamNumber;
                        var baseStreamIndirectObject = document.IndirectObjects[baseStreamNumber];
                        if (baseStreamIndirectObject.IsOriginal) // Extension stream needed in order to preserve the original object stream.
                        {
                            // Get the extension object stream associated to the original object stream!
                            if (!extensionObjectStreams.TryGetValue(baseStreamNumber, out var extensionObjectStream))
                            {
                                document.Register(extensionObjectStream = new ObjectStream());
                                // Link the extension to the base object stream!
                                extensionObjectStream.BaseStream = (ObjectStream)baseStreamIndirectObject.DataObject;
                                extensionObjectStreams[baseStreamNumber] = extensionObjectStream;
                            }
                            // Insert the data object into the extension object stream!
                            extensionObjectStream[xrefEntry.Number] = indirectObject.DataObject;
                            // Update the data object's xref entry!
                            xrefEntry.StreamNumber = extensionObjectStream.Reference.Number;
                            xrefEntry.Offset = XRefEntry.UndefinedOffset; // Internal object index unknown (to set on object stream serialization -- see ObjectStream).
                        }
                    }
                    break;
                case XRefEntry.UsageEnum.Free:
                    if (prevFreeEntry != null)
                    { prevFreeEntry.Offset = xrefEntry.Number; } // Object number of the next free object.

                    prevFreeEntry = xrefEntry;
                    break;
                default:
                    throw new NotSupportedException();
            }
            return prevFreeEntry;
        }
    }
}