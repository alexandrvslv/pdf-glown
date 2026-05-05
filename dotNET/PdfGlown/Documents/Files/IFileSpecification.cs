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
using System;
using System.IO;

namespace PdfGlown.Documents.Files
{
    /// <summary>Reference to the contents of another file (file specification) [PDF:1.6:3.10.2].</summary>
    [Pdf(VersionEnum.PDF11)]
    public interface IFileSpecification : IPdfNamedObject
    {
        /// <summary>Creates a new reference to an external file.</summary>
        /// <param name="context">Document context.</param>
        /// <param name="path">File path.</param>
        public static SimpleFileSpecification Get(PdfDocument context, string path)
        {
            return new SimpleFileSpecification(context, path);
        }

        /// <summary>Creates a new reference to a file.</summary>
        /// <param name="context">Document context.</param>
        /// <param name="path">File path.</param>
        /// <param name="full">Whether the reference is able to support extended dependencies.</param>
        public static FileSpecification GetFull(PdfDocument context, string path)
        {
            return new FileSpecification(context, path);
        }

        /// <summary>Creates a new reference to an embedded file.</summary>
        /// <param name="embeddedFile">Embedded file corresponding to the reference.</param>
        /// <param name="filename">Name corresponding to the reference.</param>
        public static FileSpecification Get(EmbeddedFile embeddedFile, string filename)
        {
            return new FileSpecification(embeddedFile, filename);
        }

        /// <summary>Creates a new reference to a remote file.</summary>
        /// <param name="context">Document context.</param>
        /// <param name="url">Remote file location.</param>
        public static FileSpecification Get(PdfDocument context, Uri url)
        {
            return new FileSpecification(context, url);
        }

        /// <summary>Instantiates an existing file reference.</summary>
        /// <param name="baseObject">Base object.</param>
        public static IFileSpecification Wrap(PdfDirectObject baseObject)
        {
            if (baseObject == null)
                return null;

            var baseDataObject = baseObject.Resolve(PdfName.Filespec);
            if (baseDataObject is PdfString)
                return new SimpleFileSpecification(baseObject);
            else if (baseDataObject is FileSpecification fullFile)
                return fullFile;
            else
                return null;
        }

        /// <summary>Gets the file absolute path.</summary>
        public string GetAbsolutePath()
        {
            var path = FilePath;
            if (!Path.IsPathRooted(path)) // Path needs to be resolved.
            {
                string basePath = Document.Path;
                if (basePath != null)
                { path = Path.Combine(Path.GetDirectoryName(basePath), path); }
            }
            return path;
        }

        /// <summary>Gets an input stream to read from the file.</summary>
        public virtual IInputStream GetInputStream()
        {
            return new StreamContainer(
              new FileStream(GetAbsolutePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        }

        /// <summary>Gets an output stream to write into the file.</summary>
        public virtual IOutputStream GetOutputStream()
        {
            return new StreamContainer(
              new FileStream(GetAbsolutePath(), FileMode.Create, FileAccess.Write, FileShare.ReadWrite));
        }

        /// <summary>Gets the file path.</summary>
        public string FilePath { get; set; }

        public PdfDocument Document { get; }
    }
}

