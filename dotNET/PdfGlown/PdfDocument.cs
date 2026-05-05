/*
  Copyright 2006-2015 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)
    * Kasper Fabaech Brandt (patch contributor [FIX:45], http://sourceforge.net/u/kasperfb/)

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
using PdfGlown.Documents;
using PdfGlown.Documents.Contents.Fonts;
using PdfGlown.Documents.Contents.Fonts.TTF;
using PdfGlown.Documents.Encryption;
using PdfGlown.Documents.Interchange.Metadata;
using PdfGlown.Files;
using PdfGlown.Objects;
using PdfGlown.Tokens;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;

namespace PdfGlown
{
    /// <summary>PDF file representation.</summary>
    public sealed class PdfDocument : IDisposable
    {
        private sealed class ImplicitContainer : PdfIndirectObject
        {
            public ImplicitContainer(PdfDocument context, PdfDirectObject dataObject)
                : base(context, dataObject, new XRefEntry(int.MinValue, int.MinValue))
            { }
        }

        private static readonly Random hashCodeGenerator = new();
        internal Dictionary<PdfDirectObject, IDisposable> Cache = new();
        internal ConcurrentDictionary<FontName, PdfType1Font> Type1FontCache = new();
        internal ConcurrentDictionary<TrueTypeFont, PdfType0Font> Type0FontCache = new();

        private DocumentConfiguration configuration;
        private CatalogConfiguration catalogCfg;
        private readonly PdfCatalog catalog;
        private readonly int hashCode = hashCodeGenerator.Next();
        private readonly IndirectObjects indirectObjects;
        private string path;
        private Reader reader;
        private readonly PdfDictionary trailer;
        private readonly PdfVersion version;
        private Cloner cloner;
        public readonly ManualResetEventSlim LockObject = new(true);

        public PdfDocument()
        {
            Initialize();

            version = PdfVersion.Get(VersionEnum.PDF14);
            trailer = PrepareTrailer(new PdfDictionary());
            indirectObjects = new IndirectObjects(this, null);
            catalog = new PdfCatalog(this);
        }

        public PdfDocument(string path)
            : this(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            this.path = path;
        }

        public PdfDocument(byte[] data)
            : this((IInputStream)new ByteStream(data))
        { }

        public PdfDocument(Stream stream)
            : this((IInputStream)new StreamContainer(stream))
        {
            //if (stream is FileStream fileStream)
            //    path = fileStream.Name;
        }

        public PdfDocument(IInputStream stream)
        {
            Initialize();

            reader = new Reader(stream, this);
            try // [FIX:45] File constructor didn't dispose reader on error.
            {
                var info = reader.ReadInfo();
                version = info.Version;
                trailer = PrepareTrailer(info.Trailer);
                indirectObjects = new IndirectObjects(this, info.XrefEntries);

                reader.PrepareDecryption();

                if (trailer.Get<PdfCatalog>(PdfName.Root) is PdfCatalog rdoc)
                {
                    catalog = rdoc;
                }
                else
                {
                    foreach (var inderectObject in indirectObjects)
                    {
                        var entry = inderectObject.Resolve(PdfName.Root);
                        if (entry is PdfCatalog entryCatalog)
                        {
                            catalog = entryCatalog;
                            break;
                        }
                        else if (entry is PdfDictionary entryDictionary
                            && entryDictionary.Get(PdfName.Pages) != null)
                        {
                            catalog = new PdfCatalog(entryDictionary.entries);
                            break;
                        }
                    }
                }
                Configuration.XRefMode = PdfName.XRef.Equals(trailer.Get<PdfName>(PdfName.Type))
                  ? XRefModeEnum.Compressed
                  : XRefModeEnum.Plain;
            }
            catch (Exception)
            {
                reader.Dispose();
                throw;
            }
        }

        ~PdfDocument()
        {
            Dispose(false);
        }

        public PdfFont LatestFont { get; internal set; }

        /// <summary>Gets the file configuration.</summary>
        public DocumentConfiguration Configuration => configuration;

        /// <summary>Gets/Sets the configuration of this document.</summary>
        public CatalogConfiguration CatalogConfiguration
        {
            get => catalogCfg;
            set => catalogCfg = value;
        }

        /// <summary>Gets the high-level representation of the file content.</summary>
        public PdfCatalog Catalog => catalog;

        /// <summary>Gets/Sets the page collection.</summary>
        public PdfPages Pages
        {
            get => Catalog.Pages;
            set => Catalog.Pages = value;
        }

        public PdfEncryption Encryption
        {
            get => Trailer.Get<PdfEncryption>(PdfName.Encrypt);
            set => trailer.Set(PdfName.Encrypt, value);
        }

        /// <summary>Gets the identifier of this file.</summary>
        public Identifier ID
        {
            get => Trailer.Get<Identifier>(PdfName.ID);
            set => Trailer.SetDirect(PdfName.ID, value);
        }

        /// <summary>Gets/Sets common document metadata.</summary>
        public Information Information
        {
            get => Trailer.GetOrCreateInderect<Information>(PdfName.Info);
            set => Trailer.Set(PdfName.Info, value);
        }

        public DateTime? ModificationDate => Information.ModificationDate;

        /// <summary>Gets the indirect objects collection.</summary>
        public IndirectObjects IndirectObjects => indirectObjects;

        /// <summary>Gets/Sets the file path.</summary>
        public string Path
        {
            get => path;
            set => path = value;
        }

        /// <summary>Gets the data reader backing this file.</summary>
        /// <returns><code>null</code> in case of newly-created file.</returns>
        public Reader Reader => reader;


        /// <summary>Gets the file trailer.</summary>
        public PdfDictionary Trailer => trailer;

        /// <summary>Gets whether the initial state of this file has been modified.</summary>
        public bool Updated => indirectObjects.ModifiedObjects.Count > 0;

        /// <summary>Gets the file header version [PDF:1.6:3.4.1].</summary>
        /// <remarks>This property represents just the original file version; to get the actual version,
        /// use the <see cref="PdfCatalog.Version">Document.Version</see> method.
        /// </remarks>
        public PdfVersion Version => version;

        /// <summary>Registers an <b>internal data object</b>.</summary>
        public PdfReference Register(PdfDirectObject obj)
        {
            return indirectObjects.Add(obj).Reference;
        }

        /// <summary>Serializes the file to the current file-system path using the 
        /// <see cref="SerializationModeEnum.Incremental">incremental for signatures</see> or 
        /// <see cref="SerializationModeEnum.Standard">standard serialization mode</see>.</summary>
        public void Save() => Save(Catalog.HasSignatures ? SerializationModeEnum.Incremental : SerializationModeEnum.Standard);

        /// <summary>Serializes the file to the current file-system path.</summary>
        /// <param name="mode">Serialization mode.</param>
        public void Save(SerializationModeEnum mode)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("No valid source path available.");

            // NOTE: The document file cannot be directly overwritten as it's locked for reading by the
            // open stream; its update is therefore delayed to its disposal, when the temporary file will
            // overwrite it (see Dispose() method).
            Save(TempPath, mode);
            CompleatSave();
        }

        /// <summary>Serializes the file to the specified file system path using the 
        /// <see cref="SerializationModeEnum.Incremental">incremental for signatures</see> or 
        /// <see cref="SerializationModeEnum.Standard">standard serialization mode</see>.</summary>
        /// <param name="path">Target path.</param>
        public void Save(string path) => Save(path, Catalog.HasSignatures ? SerializationModeEnum.Incremental : SerializationModeEnum.Standard);

        /// <summary>Serializes the file to the specified file system path .</summary>
        /// <param name="path">Target path.</param>
        /// <param name="mode">Serialization mode.</param>
        public void Save(string path, SerializationModeEnum mode)
        {
            using (var outputStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                Save((IOutputStream)new StreamContainer(outputStream), mode);
            }
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
        }

        /// <summary>Serializes the file to the specified stream using the 
        /// <see cref="SerializationModeEnum.Incremental">incremental for signatures</see> or 
        /// <see cref="SerializationModeEnum.Standard">standard serialization mode</see>.</summary>
        /// <param name="path">Target path.</param>
        public void Save(Stream stream) => Save(stream, Catalog.HasSignatures ? SerializationModeEnum.Incremental : SerializationModeEnum.Standard);

        /// <summary>Serializes the file to the specified stream.</summary>
        /// <remarks>It's caller responsibility to close the stream after this method ends.</remarks>
        /// <param name="stream">Target stream.</param>
        /// <param name="mode">Serialization mode.</param>
        public void Save(Stream stream, SerializationModeEnum mode) => Save((IOutputStream)new StreamContainer(stream), mode);

        /// <summary>Serializes the file to the specified stream.</summary>
        /// <remarks>It's caller responsibility to close the stream after this method ends.</remarks>
        /// <param name="stream">Target stream.</param>
        /// <param name="mode">Serialization mode.</param>
        public void Save(IOutputStream stream, SerializationModeEnum mode)
        {
            var information = Information;
            if (Reader == null)
            {
                information.CreationDate = DateTime.Now;
                try
                {
                    string assemblyTitle = Assembly.GetExecutingAssembly().GetName().Name;
                    string assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                    information.Producer = assemblyTitle + " " + assemblyVersion;
                }
                catch
                {/* NOOP */}
            }
            else
            { information.ModificationDate = DateTime.Now; }

            var writer = Writer.Get(this, stream);
            writer.Write(mode);
        }

        /// <summary>Unregisters an internal object.</summary>
        public void Unregister(PdfReference reference)
        {
            indirectObjects.RemoveAt(reference.Number);
        }

        /// <summary>Gets/Sets the default cloner.</summary>
        public Cloner Cloner
        {
            get => cloner ??= new Cloner(this);
            set => cloner = value;
        }

        public bool IsDisposed => reader == null;

        public override int GetHashCode()
        {
            return hashCode;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (reader != null)
                {
                    reader.Dispose();
                    reader = null;
                    CompleatSave();
                }
                ClearCache();
                LockObject?.Dispose();
            }
        }

        private void CompleatSave()
        {
            //NOTE: If the temporary file exists (see Save() method), it must overwrite the document file.
            if (File.Exists(TempPath))
            {
                File.Delete(path);
                File.Move(TempPath, path);
            }
        }

        private void Initialize()
        {
            configuration = new DocumentConfiguration(this);
            catalogCfg = new CatalogConfiguration(this);
        }

        private PdfDictionary PrepareTrailer(PdfDictionary trailer)
        {
            return (PdfDictionary)new ImplicitContainer(this, trailer).GetDataObject(PdfName.Trailer);
        }

        private string TempPath => path == null ? null : $"{path}.tmp";

        /// <summary>Checks whether the specified feature is compatible with the
        ///   <see cref="PdfCatalog.Version">document's conformance version</see>.</summary>
        /// <param name="feature">Entity whose compatibility has to be checked. Supported types:
        ///   <list type="bullet">
        ///     <item><see cref="VersionEnum"/></item>
        ///   </list>
        /// </param>
        internal void CheckCompatibility(VersionEnum feature)
        {
            // TODO: Caching!
            var compatibilityMode = CatalogConfiguration.CompatibilityMode;
            if (compatibilityMode == CompatibilityModeEnum.Passthrough) // No check required.
                return;

            //if (feature is Enum)
            //{
            //    Type enumType = feature.GetType();
            //    if (enumType.GetCustomAttributes(typeof(FlagsAttribute), true).Length > 0)
            //    {
            //        int featureEnumValues = Convert.ToInt32(feature);
            //        var featureEnumItems = new List<Enum>();
            //        foreach (int enumValue in Enum.GetValues(enumType))
            //        {
            //            if ((featureEnumValues & enumValue) == enumValue)
            //            { featureEnumItems.Add((Enum)Enum.ToObject(enumType, enumValue)); }
            //        }
            //        if (featureEnumItems.Count > 1)
            //        { feature = featureEnumItems; }
            //    }
            //}
            //if (feature is ICollection)
            //{
            //    foreach (Object featureItem in (ICollection)feature)
            //    { CheckCompatibility(featureItem); }
            //    return;
            //}

            var featureVersion = PdfVersion.Get(feature);
            //if (feature is VersionEnum) // Explicit version.
            //{ featureVersion = ((VersionEnum)feature).GetVersion(); }
            //else // Implicit version (element annotation).
            //{
            //    PDFAttribute annotation;
            //    {
            //        if (feature is string) // Property name.
            //        { feature = GetType().GetProperty((string)feature); }
            //        else if (feature is Enum) // Enum constant.
            //        { feature = feature.GetType().GetField(feature.ToString()); }
            //        if (!(feature is MemberInfo))
            //            throw new ArgumentException("Feature type '" + feature.GetType().Name + "' not supported.");
            //        while (true)
            //        {
            //            var annotations = ((MemberInfo)feature).GetCustomAttributes<PDFAttribute>(true);
            //            if (annotations.Any())
            //            {
            //                annotation = annotations.FirstOrDefault();
            //                break;
            //            }
            //            feature = ((MemberInfo)feature).DeclaringType;
            //            if (feature == null) // Element hierarchy walk complete.
            //                return; // NOTE: As no annotation is available, we assume the feature has no specific compatibility requirements.
            //        }
            //    }
            //    featureVersion = annotation.Value.GetVersion();
            //}
            // Is the feature version compatible?
            if (Catalog.Version.CompareTo(featureVersion) >= 0)
                return;

            // The feature version is NOT compatible: how to solve the conflict?
            switch (compatibilityMode)
            {
                case CompatibilityModeEnum.Loose: // Accepts the feature version.
                                                  // Synchronize the document version!
                    Catalog.Version = featureVersion;
                    break;
                case CompatibilityModeEnum.Strict: // Refuses the feature version.
                                                   // Throw a violation to the document version!
                    throw new Exception("Incompatible feature (version " + featureVersion + " was required against document version " + Catalog.Version);
                default:
                    throw new NotImplementedException("Unhandled compatibility mode: " + compatibilityMode);
            }
        }

        internal void ClearCache()
        {
            foreach (var entry in Cache)
            {
                if (entry.Value is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            Type0FontCache.Clear();
            Type1FontCache.Clear();
            Cache.Clear();
            LatestFont = null;
        }
    }
}
