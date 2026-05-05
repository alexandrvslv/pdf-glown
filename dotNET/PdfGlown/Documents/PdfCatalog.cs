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

using PdfGlown.Documents.Contents;
using PdfGlown.Documents.Contents.Fonts.TTF;
using PdfGlown.Documents.Contents.Layers;
using PdfGlown.Documents.Interaction.Actions;
using PdfGlown.Documents.Interaction.Annotations;
using PdfGlown.Documents.Interaction.Forms;
using PdfGlown.Documents.Interaction.Forms.Signature;
using PdfGlown.Documents.Interaction.Navigation;
using PdfGlown.Documents.Interaction.Viewer;
using PdfGlown.Documents.Interchange.Metadata;
using PdfGlown.Documents.Names;
using PdfGlown.Objects;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfGlown.Documents
{
    /// <summary>PDF document [PDF:1.6::3.6.1].</summary>
    [Pdf(VersionEnum.PDF10)]
    public sealed class PdfCatalog : PdfDictionary, IAppDataHolder
    {
        public static T Resolve<T>(PdfDirectObject baseObject)
            where T : class
        {
            if (typeof(Destination).IsAssignableFrom(typeof(T)))
                return baseObject.Resolve(PdfName.D) as T;
            else
                throw new NotSupportedException("Type '" + typeof(T).Name + "' wrapping is not supported.");
        }

        private PdfVersion version;
        private PageLabels pageLabels;
        private PdfPages pages;
        internal PdfCatalog(PdfDocument context)
            : base(context, new(4) { { PdfName.Type, PdfName.Catalog } })
        {
            // Attach the document catalog to the file trailer!
            context.Trailer[PdfName.Root] = Reference;

            // Pages collection.
            Pages = new PdfPages(Document);

            // Default page size.
            PageSize = PageFormat.GetSize();

            // Default resources collection.
            Resources = new Resources(context);
        }

        internal PdfCatalog(Dictionary<PdfName, PdfDirectObject> baseObject)// Catalog.
            : base(baseObject)
        { }

        /// <summary>Gets/Sets the document's behavior in response to trigger events.</summary>
        [Pdf(VersionEnum.PDF14)]
        public AdditionalActions Actions
        {
            get => GetOrCreate<AdditionalActions>(PdfName.AA);
            set => Set(PdfName.AA, value);
        }

        /// <summary>Gets the article threads.</summary>
        [Pdf(VersionEnum.PDF11)]
        public Articles Articles
        {
            get => GetOrCreateInderect<Articles>(PdfName.Threads);
            set => SetDirect(PdfName.Threads, value);
        }

        /// <summary>Gets/Sets the bookmark collection.</summary>
        public Bookmarks Bookmarks
        {
            get => GetOrCreateInderect<Bookmarks>(PdfName.Outlines);
            set => Set(PdfName.Outlines, value);
        }

        /// <summary>Deletes the object from this document context.</summary>
        public void Exclude(PdfObject obj)
        {
            if (obj.Document != Document)
                return;

            obj.Delete();
        }

        /// <summary>Deletes the objects from this document context.</summary>
        public void Exclude<T>(ICollection<T> objs) where T : PdfObject
        {
            foreach (T obj in objs)
            {
                Exclude(obj);
            }
        }

        /// <summary>Gets/Sets the interactive form (AcroForm).</summary>
        [Pdf(VersionEnum.PDF12)]
        public AcroForm Form
        {
            get => GetOrCreate<AcroForm>(PdfName.AcroForm);
            set => Set(PdfName.AcroForm, value);
        }

        /// <summary>Gets/Sets the optional content properties.</summary>
        [Pdf(VersionEnum.PDF15)]
        public LayerDefinition Layers
        {
            get => GetOrCreate<LayerDefinition>(PdfName.OCProperties);
            set
            {
                Document?.CheckCompatibility(VersionEnum.PDF15);
                Set(PdfName.OCProperties, value);
            }
        }

        /// <summary>Gets/Sets the name dictionary.</summary>
        [Pdf(VersionEnum.PDF12)]
        public NamedResources Names
        {
            get => GetOrCreate<NamedResources>(PdfName.Names);
            set => Set(PdfName.Names, value);
        }

        /// <summary>Gets/Sets the page label ranges.</summary>
        [Pdf(VersionEnum.PDF13)]
        public PageLabels PageLabels
        {
            get => pageLabels ??= new(GetOrCreate<PdfDictionary>(PdfName.PageLabels));
            set
            {
                Document?.CheckCompatibility(VersionEnum.PDF13);
                this[PdfName.PageLabels] = (pageLabels = value)?.RefOrSelf;
            }
        }

        /// <summary>Gets/Sets the page collection.</summary>
        public PdfPages Pages
        {
            get => pages ??= Get<PdfPages>(PdfName.Pages);
            set => Set(PdfName.Pages, pages = value);
        }

        /// <summary>Gets/Sets the default page size [PDF:1.6:3.6.2].</summary>
        public SKSize? PageSize
        {
            get
            {
                PdfArray mediaBox = MediaBox;
                return mediaBox != null
                  ? new SKSize(
                    (int)mediaBox.GetDouble(2),
                    (int)mediaBox.GetDouble(3))
                  : null;
            }
            set
            {
                PdfArray mediaBox = MediaBox;
                if (mediaBox == null)
                {
                    // Create default media box!
                    mediaBox = new PdfRectangle(0, 0, 0, 0);
                    // Assign the media box to the document!
                    Get<PdfDictionary>(PdfName.Pages)[PdfName.MediaBox] = mediaBox;
                }
                mediaBox.Set(2, value.Value.Width);
                mediaBox.Set(3, value.Value.Height);
            }
        }

        /// <summary>Gets the document size, that is the maximum page dimensions across the whole document.
        /// <seealso cref="PageSize"/>
        /// </summary>
        public SKSize GetSize()
        {
            float height = 0, width = 0;
            foreach (var page in Pages)
            {
                SKSize pageSize = page.Size;
                height = Math.Max(height, pageSize.Height);
                width = Math.Max(width, pageSize.Width);
            }
            return new SKSize(width, height);
        }

        /// <summary>Clones the object within this document context.</summary>
        public PdfObject Include(PdfObject obj)
        {
            if (obj.Document == Document)
                return obj;

            return obj.Clone(Document);
        }

        /// <summary>Clones the collection objects within this document context.</summary>
        public List<T> Include<T>(ICollection<T> objs) where T : PdfDirectObject
        {
            var includedObjects = new List<T>(objs.Count);
            foreach (T obj in objs)
            { includedObjects.Add((T)Include(obj.RefOrSelf).Resolve()); }

            return includedObjects;
        }

        /// <summary>Registers a named object.</summary>
        /// <param name="name">Object name.</param>
        /// <param name="object">Named object.</param>
        /// <returns>Registered named object.</returns>
        public T Register<T>(PdfString name, T @object) where T : class
        {
            var namedObjects = Names.Get(@object.GetType());
            namedObjects[name] = @object;
            return @object;
        }

        /// <summary>Forces a named base object to be expressed as its corresponding high-level
        /// representation.</summary>
        public T ResolveName<T>(PdfDirectObject namedBaseObject)
            where T : class
        {
            if (namedBaseObject is PdfString name) // Named object.
                return Names.GetRes<T>(name);
            else // Explicit object.
                return Resolve<T>(namedBaseObject);
        }

        /// <summary>Gets/Sets the default resource collection [PDF:1.6:3.6.2].</summary>
        /// <remarks>The default resource collection is used as last resort by every page that doesn't
        /// reference one explicitly (and doesn't reference an intermediate one implicitly).</remarks>
        public Resources Resources
        {
            get => Pages.Resources;
            set => Pages.Resources = value;
        }

        /// <summary>Gets/Sets the version of the PDF specification this document conforms to.</summary>
        [Pdf(VersionEnum.PDF14)]
        public PdfVersion Version
        {
            get => version ??= GetVersion();
            set => this[PdfName.Version] = PdfName.Get(version = value);
        }

        private PdfVersion GetVersion()
        {
            //NOTE: If the header specifies a later version, or if this entry is absent, the document
            //conforms to the version specified in the header.
            var fileVersion = Document.Version;

            var versionObject = Get<PdfName>(PdfName.Version);
            if (versionObject == null)
                return fileVersion;

            var version = PdfVersion.Get(versionObject);
            if (Document.Reader == null)
                return version;

            return (version.CompareTo(fileVersion) > 0 ? version : fileVersion);
        }

        /// <summary>Gets the way the document is to be presented.</summary>
        public ViewerPreferences ViewerPreferences
        {
            get => GetOrCreate<ViewerPreferences>(PdfName.ViewerPreferences);
            set => Set(PdfName.ViewerPreferences, value);
        }

        /// <summary>Gets/Sets the destination to be displayed or the action to be performed
        /// after opening the document.</summary>
        public PdfDirectObject OnOpen
        {
            get => Get<Destination>(PdfName.OpenAction)
                ?? (PdfDirectObject)Get<PdfAction>(PdfName.OpenAction);
            set
            {
                if (!(value is PdfAction
                  || value is LocalDestination))
                    throw new System.ArgumentException("Value MUST be either an Action or a LocalDestination.");

                Set(PdfName.OpenAction, value);
            }
        }

        public AppDataCollection AppData => GetOrCreate<AppDataCollection>(PdfName.PieceInfo).WithHolder(this);

        /// <summary>Gets the default media box.</summary>
        //NOTE: Document media box MUST be associated with the page-tree root node in order to be
        //inheritable by all the pages.        
        private PdfRectangle MediaBox => Pages.MediaBox;

        public AppData GetAppData(PdfName appName) => AppData.Ensure(appName);

        public DateTime? ModificationDate => Document.ModificationDate;

        public void Touch(PdfName appName) => Touch(appName, DateTime.Now);

        public void Touch(PdfName appName, DateTime modificationDate)
        {
            GetAppData(appName).ModificationDate = modificationDate;
            Document.Information.ModificationDate = modificationDate;
        }

        internal void RegisterTrueTypeFontForClosing(TrueTypeFont ttf)
        {
            throw new NotImplementedException();
        }

        public bool HasSignatures => ContainsKey(PdfName.AcroForm) && Form.Fields.OfType<SignatureField>().Any();

        public bool HasSignatureDictionaries => GetSignatureDictionaries().Any();

        public float? PageAlpha { get; set; }

        internal IEnumerable<SignatureDictionary> GetSignatureDictionaries()
        {
            if (!ContainsKey(PdfName.AcroForm))
                yield break;
            foreach (var sigField in Form.Fields.OfType<SignatureField>())
            {
                if (sigField.SignatureDictionary != null)
                    yield return sigField.SignatureDictionary;
            }
        }

        public Annotation FindAnnotation(string name, int? pageIndex = null)
        {
            var annotation = (Annotation)null;
            if (pageIndex == null)
            {
                foreach (var page in Pages)
                {
                    annotation = page.Annotations[name];
                    if (annotation != null)
                        return annotation;
                }
            }
            else
            {
                var page = Pages[(int)pageIndex];
                return page?.Annotations[name];
            }
            return null;
        }
    }
}