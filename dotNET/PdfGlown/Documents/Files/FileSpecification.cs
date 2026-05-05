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
using PdfGlown.Files;
using PdfGlown.Objects;
using PdfGlown.Util;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace PdfGlown.Documents.Files
{
    /// <summary>Extended reference to the contents of another file [PDF:1.6:3.10.2].</summary>
    [Pdf(VersionEnum.PDF11)]
    public sealed class FileSpecification : PdfDictionary, IFileSpecification
    {
        private RelatedFiles dependencies;
        private EmbeddedFile embeddedFile;

        ///  <summary>Standard file system.</summary>
        public enum StandardFileSystemEnum
        {
            /// <summary>Generic platform file system.</summary>
            Native,
            /// <summary>Uniform resource locator.</summary>
            URL
        }

        private static readonly BiDictionary<StandardFileSystemEnum, PdfName> stdFScodes = new()
        {
            [StandardFileSystemEnum.Native] = null,
            [StandardFileSystemEnum.URL] = PdfName.URL
        };

        public static StandardFileSystemEnum? GetStdFS(PdfName code) => stdFScodes.GetKey(code);

        public static PdfName GetName(StandardFileSystemEnum standardFileSystem) => stdFScodes[standardFileSystem];

        internal FileSpecification(PdfDocument context, string path)
            : base(context, new Dictionary<PdfName, PdfDirectObject>(3) {
                { PdfName.Type, PdfName.Filespec }
            })
        {
            FilePath = path;
        }

        internal FileSpecification(EmbeddedFile embeddedFile, string filename)
            : this(embeddedFile.Document, filename)
        {
            EmbeddedFile = embeddedFile;
        }

        internal FileSpecification(PdfDocument context, Uri url)
            : this(context, url.ToString())
        {
            FileSystem = StandardFileSystemEnum.URL;
        }

        internal FileSpecification(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets/Sets the identifier of the file.</summary>
        public Identifier ID
        {
            get => Get<Identifier>(PdfName.ID);
            set => SetDirect(PdfName.ID, value);
        }

        public string FilePath
        {
            get => GetString(PdfName.F);
            set => Set(PdfName.F, value);
        }

        /// <summary>Gets/Sets the related files.</summary>
        public RelatedFiles Dependencies
        {
            get => dependencies ??= GetDependencies(PdfName.F);
            set => SetDependencies(PdfName.F, dependencies = value);
        }

        /// <summary>Gets/Sets the description of the file.</summary>
        public string Description
        {
            get => GetString(PdfName.Desc);
            set => SetText(PdfName.Desc, value);
        }

        /// <summary>Gets/Sets the embedded file corresponding to this file.</summary>
        public EmbeddedFile EmbeddedFile
        {
            get => embeddedFile ??= GetEmbeddedFile(PdfName.F);
            set => SetEmbeddedFile(PdfName.F, embeddedFile = value);
        }

        /// <summary>Gets/Sets the file system to be used to interpret this file specification.</summary>
        /// <returns>Either <see cref="StandardFileSystemEnum"/> (standard file system) or
        /// <see cref="String"/> (custom file system).</returns>
        public object FileSystem
        {
            get
            {
                var fileSystemObject = Get<PdfName>(PdfName.FS);
                StandardFileSystemEnum? standardFileSystem = GetStdFS(fileSystemObject);
                return standardFileSystem ?? fileSystemObject.Value;
            }
            set
            {
                this[PdfName.FS] = value switch
                {
                    StandardFileSystemEnum enumValue => GetName(enumValue),
                    string stringValue => PdfName.Get(stringValue),
                    _ => throw new ArgumentException("MUST be either StandardFileSystemEnum (standard file system) or String (custom file system)"),
                };
            }
        }

        /// <summary>Gets/Sets whether the referenced file is volatile (changes frequently with time).
        /// </summary>
        public bool Volatile
        {
            get => GetBool(PdfName.V, false);
            set => Set(PdfName.V, value);
        }

        public PdfString Name => RetrieveName();

        public PdfDirectObject NamedBaseObject => RetrieveNamedBaseObject();

        public IInputStream GetInputStream()
        {
            if (PdfName.URL.Equals(GetString(PdfName.FS))) // Remote resource [PDF:1.7:3.10.4].
            {
                Uri fileUrl;
                try
                { fileUrl = new Uri(FilePath); }
                catch (Exception e)
                { throw new Exception("Failed to instantiate URL for " + FilePath, e); }
                using var webClient = new HttpClient();
                try
                {
                    var stream = webClient.GetStreamAsync(fileUrl).GetAwaiter().GetResult();
                    return new ByteStream(stream);
                }
                catch (Exception e)
                { throw new Exception("Failed to open input stream for " + FilePath, e); }
            }
            else // Local resource [PDF:1.7:3.10.1].
                return ((IFileSpecification)this).GetInputStream();
        }

        public IOutputStream GetOutputStream()
        {
            if (PdfName.URL.Equals(GetString(PdfName.FS))) // Remote resource [PDF:1.7:3.10.4].
            {
                Uri fileUrl;
                try
                { fileUrl = new Uri(FilePath); }
                catch (Exception e)
                { throw new Exception("Failed to instantiate URL for " + FilePath, e); }
                using var webClient = new HttpClient();
                try
                {
                    var tempStream = webClient.GetStreamAsync(fileUrl).GetAwaiter().GetResult();
                    return new StreamContainer(tempStream);
                }
                catch (Exception e)
                { throw new Exception("Failed to open output stream for " + FilePath, e); }
            }
            else // Local resource [PDF:1.7:3.10.1].
                return ((IFileSpecification)this).GetOutputStream();
        }


        /// <summary>Gets the related files associated to the given key.</summary>
        private RelatedFiles GetDependencies(PdfName key)
        {
            var dependenciesObject = Get<PdfDictionary>(PdfName.RF);
            if (dependenciesObject == null)
                return null;

            return new RelatedFiles(dependenciesObject.Get(key));
        }

        /// <see cref="GetDependencies(PdfName)"/>
        private void SetDependencies(PdfName key, RelatedFiles value)
        {
            var dependenciesObject = GetOrCreate<PdfDictionary>(PdfName.RF);

            dependenciesObject[key] = value.RefOrSelf;
        }

        /// <summary>Gets the embedded file associated to the given key.</summary>
        private EmbeddedFile GetEmbeddedFile(PdfName key)
        {
            var embeddedFilesObject = Get<PdfDictionary>(PdfName.EF);
            return embeddedFilesObject?.Get<EmbeddedFile>(key);
        }

        /// <see cref="GetEmbeddedFile(PdfName)"/>
        private void SetEmbeddedFile(PdfName key, EmbeddedFile value)
        {
            var embeddedFilesObject = GetOrCreate<PdfDictionary>(PdfName.EF);

            embeddedFilesObject[key] = value.Reference;
        }

    }
}
