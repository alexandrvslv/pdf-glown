/*
  Copyright 2006-2015 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)
    * Andreas Pinter (bug reporter [FIX:53], https://sourceforge.net/u/drunal/)

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
using PdfGlown.Documents.Contents;
using PdfGlown.Documents.Contents.Composition;
using PdfGlown.Documents.Contents.Objects;
using PdfGlown.Documents.Contents.Scanner;
using PdfGlown.Documents.Contents.XObjects;
using PdfGlown.Documents.Interaction.Actions;
using PdfGlown.Documents.Interaction.Navigation;
using PdfGlown.Documents.Interchange.Metadata;
using PdfGlown.Objects;
using PdfGlown.Util.Math;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace PdfGlown.Documents
{
    /// <summary>Document page [PDF:1.6:3.6.2].</summary>
    [Pdf(VersionEnum.PDF10)]
    public class PdfPage : PdfDictionary, IContentContext, IResourceProvider
    {
        // NOTE: Inheritable attributes are NOT early-collected, as they are NOT part
        // of the explicit representation of a page. They are retrieved every time
        // clients call.

        /// <summary>Annotations tab order [PDF:1.6:3.6.2].</summary>
        [Pdf(VersionEnum.PDF15)]
        public enum TabOrderEnum
        {
            /// <summary>Row order.</summary>
            Row,
            /// <summary>Column order.</summary>
            Column,
            /// <summary>Structure order.</summary>
            Structure
        };

        public static readonly HashSet<PdfName> InheritableAttributeKeys = new()
        {
            PdfName.Resources,
            PdfName.MediaBox,
            PdfName.CropBox,
            PdfName.Rotate
        };

        private static readonly Dictionary<TabOrderEnum, PdfName> TabOrderEnumCodes = new()
        {
            [TabOrderEnum.Row] = PdfName.R,
            [TabOrderEnum.Column] = PdfName.C,
            [TabOrderEnum.Structure] = PdfName.S
        };

        private SKMatrix? rotateMatrix;
        private SKMatrix? invertRotateMatrix;
        private SKRect? box;
        internal int? index;
        private ContentWrapper contents;
        private Resources resources;
        private PageArticleElements articleElements;

        /// <summary>Gets the code corresponding to the given value.</summary>
        private static PdfName ToCode(TabOrderEnum value)
        {
            return TabOrderEnumCodes[value];
        }

        /// <summary>Gets the tab order corresponding to the given value.</summary>
        private static TabOrderEnum ToTabOrderEnum(string value)
        {
            if (value == null)
                return TabOrderEnum.Row;
            foreach (KeyValuePair<TabOrderEnum, PdfName> tabOrder in TabOrderEnumCodes)
            {
                if (string.Equals(tabOrder.Value.StringValue, value, StringComparison.Ordinal))
                    return tabOrder.Key;
            }
            return TabOrderEnum.Row;
        }

        /// <summary>Creates a new page within the specified document context, using the default size.
        /// </summary>
        /// <param name="context">Document where to place this page.</param>
        public PdfPage(PdfDocument context)
            : this(context, null)
        { }

        /// <summary>Creates a new page within the specified document context.</summary>
        /// <param name="context">Document where to place this page.</param>
        /// <param name="size">Page size. In case of <code>null</code>, uses the default SKSize.</param>
        public PdfPage(PdfDocument context, SKSize? size)
            : base(context, new Dictionary<PdfName, PdfDirectObject>()
            {
                { PdfName.Type, PdfName.Page },
                { PdfName.Contents, context.Register(new PdfStream(new ByteStream())) }
            })
        {
            if (size.HasValue)
            { Size = size.Value; }
        }

        internal PdfPage(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets/Sets the page's behavior in response to trigger events.</summary>
        [Pdf(VersionEnum.PDF12)]
        public AdditionalActions Actions
        {
            get => GetOrCreate<AdditionalActions>(PdfName.AA);
            set => Set(PdfName.AA, value);
        }

        /// <summary>Gets/Sets the annotations associated to the page.</summary>
        public PageAnnotations Annotations
        {
            get => GetOrCreate<PageAnnotations>(PdfName.Annots).WithPage(this);
            set => Set(PdfName.Annots, value);
        }

        /// <summary>Gets/Sets the extent of the page's meaningful content (including potential white space)
        /// as intended by the page's creator [PDF:1.7:10.10.1].</summary>
        /// <seealso cref="CropBox"/>
        [Pdf(VersionEnum.PDF13)]
        public PdfRectangle ArtBox
        {
            //NOTE: The default value is the page's crop box.
            get => (PdfRectangle)this.GetInheritableAttribute(PdfName.ArtBox)?.Resolve(PdfName.ArtBox) ?? CropBox;
            set => SetDirect(PdfName.ArtBox, value);
        }

        /// <summary>Gets the page article beads.</summary>
        public PageArticleElements ArticleElements
        {
            get => articleElements ??= new(GetOrCreate<PdfArrayImpl>(PdfName.B)) { Page = this };
        }

        /// <summary>Gets/Sets the region to which the contents of the page should be clipped when output
        /// in a production environment [PDF:1.7:10.10.1].</summary>
        /// <remarks>
        ///   <para>This may include any extra bleed area needed to accommodate the physical limitations of
        ///   cutting, folding, and trimming equipment. The actual printed page may include printing marks
        ///   that fall outside the bleed box.</para>
        /// </remarks>
        /// <seealso cref="CropBox"/>
        [Pdf(VersionEnum.PDF13)]
        public PdfRectangle BleedBox
        {
            // NOTE: The default value is the page's crop box.
            get => (PdfRectangle)this.GetInheritableAttribute(PdfName.BleedBox)?.Resolve(PdfName.BleedBox) ?? CropBox;
            set => SetDirect(PdfName.BleedBox, value);
        }

        /// <summary>Gets/Sets the region to which the contents of the page are to be clipped (cropped)
        /// when displayed or printed [PDF:1.7:10.10.1].</summary>
        /// <remarks>
        ///   <para>Unlike the other boxes, the crop box has no defined meaning in terms of physical page
        ///   geometry or intended use; it merely imposes clipping on the page contents. However, in the
        ///   absence of additional information, the crop box determines how the page's contents are to be
        ///   positioned on the output medium.</para>
        /// </remarks>
        /// <seealso cref="Box"/>
        public PdfRectangle CropBox
        {
            //NOTE: The default value is the page's media box.
            get => (PdfRectangle)this.GetInheritableAttribute(PdfName.CropBox)?.Resolve(PdfName.CropBox) ?? MediaBox;
            set => SetDirect(PdfName.CropBox, value);
        }

        /// <summary>Gets/Sets the page's display duration.</summary>
        /// <remarks>
        ///   <para>The page's display duration (also called its advance timing)
        ///   is the maximum length of time, in seconds, that the page is displayed
        ///   during presentations before the viewer application automatically advances
        ///   to the next page.</para>
        ///   <para>By default, the viewer does not advance automatically.</para>
        /// </remarks>
        [Pdf(VersionEnum.PDF11)]
        public double Duration
        {
            get => GetDouble(PdfName.Dur);
            set => Set(PdfName.Dur, value > 0 ? value : null);
        }

        ///  <summary>Gets the index of this page.</summary>
        public int Index
        {
            get => index ??= CalcIndex();
            set => index = value;
        }

        private int CalcIndex()
        {
            //NOTE: We'll scan sequentially each page-tree level above this page object
            //collecting page counts.At each level we'll scan the kids array from the
            //lower - indexed item to the ancestor of this page object at that level.
            var ancestorKidReference = Reference;
            var parent = Get<PdfDictionary>(PdfName.Parent);
            var kids = parent.Get<PdfArray>(PdfName.Kids);
            if (kids == null)
                return 0;
            int index = 0;
            for (int i = 0; true; i++)
            {
                var kidReference = (PdfReference)kids.Get(i);
                // Is the current-level counting complete?
                // NOTE: It's complete when it reaches the ancestor at this level.
                if (kidReference.Equals(ancestorKidReference)) // Ancestor node.
                {
                    // Does the current level correspond to the page-tree root node?
                    if (!parent.ContainsKey(PdfName.Parent))
                    {
                        // We reached the top: counting's finished.
                        return index;
                    }
                    // Set the ancestor at the next level!
                    ancestorKidReference = parent.Reference;
                    // Move up one level!
                    parent = parent.Get<PdfDictionary>(PdfName.Parent);
                    kids = parent.Get<PdfArray>(PdfName.Kids);
                    i = -1;
                }
                else // Intermediate node.
                {
                    var kid = (PdfDictionary)kidReference.Resolve();
                    if (kid is PdfPage)
                        index++;
                    else
                        index += kid.GetInt(PdfName.Count);
                }
            }
        }

        /// <summary>Gets the page number.</summary>
        public int Number => Index + 1;

        public TransparencyXObject Group
        {
            get => Get<TransparencyXObject>(PdfName.Group);
        }

        /// <summary>Gets/Sets the page size.</summary>
        public SKSize Size
        {
            get => Box.Size;
            set
            {
                SKRect box;
                try
                { box = Box; }
                catch
                { box = SKRect.Create(0, 0, 0, 0); }
                box.Size = value;
                Box = box;
            }
        }

        /// <summary>Gets/Sets the tab order to be used for annotations on the page.</summary>
        [Pdf(VersionEnum.PDF15)]
        public TabOrderEnum TabOrder
        {
            get => ToTabOrderEnum(GetString(PdfName.Tabs));
            set => this[PdfName.Tabs] = ToCode(value);
        }

        /// <summary>Gets the transition effect to be used
        /// when displaying the page during presentations.</summary>
        [Pdf(VersionEnum.PDF11)]
        public Transition Transition
        {
            get => Get<Transition>(PdfName.Trans);
            set => Set(PdfName.Trans, value);
        }

        /// <summary>Gets/Sets the intended dimensions of the finished page after trimming
        /// [PDF:1.7:10.10.1].</summary>
        /// <remarks>
        ///   <para>It may be smaller than the media box to allow for production-related content, such as
        ///   printing instructions, cut marks, or color bars.</para>
        /// </remarks>
        /// <seealso cref="CropBox"/>
        [Pdf(VersionEnum.PDF13)]
        public PdfRectangle TrimBox
        {
            // NOTE: The default value is the page's crop box.
            get => (PdfRectangle)this.GetInheritableAttribute(PdfName.TrimBox)?.Resolve(PdfName.TrimBox) ?? CropBox;
            set => SetDirect(PdfName.TrimBox, value);
        }

        // NOTE: Mandatory.
        public PdfRectangle MediaBox
        {
            get => (PdfRectangle)this.GetInheritableAttribute(PdfName.MediaBox)?.Resolve(PdfName.MediaBox);
            set => SetDirect(PdfName.MediaBox, value);
        }

        public SKRect Box
        {
            get => box ??= BleedBox?.ToSKRect() ?? SKRect.Empty;
            set => MediaBox = new PdfRectangle((box = value).Value);
        }

        public SKRect RotatedBox => Box.RotateRect(Rotate);

        public ContentWrapper Contents => contents ??= new ContentWrapper(GetAnyOrCreate<PdfStream>(PdfName.Contents));

        public void Render(SKCanvas canvas, SKRect box, SKColor? clearColor = null)
        {
            var scanner = new ContentScanner(this, canvas, box, clearColor);
            scanner.Scan();
        }

        public Resources Resources
        {
            get => resources ??= this.GetInheritableAttribute(PdfName.Resources)?.Resolve(PdfName.Resources) as Resources
                    ?? GetOrCreate<Resources>(PdfName.Resources);
        }

        public RotationEnum Rotation
        {
            get => RotationEnumExtension.Get((IPdfNumber)this.GetInheritableAttribute(PdfName.Rotate));
            set => Set(PdfName.Rotate, (int)value);
        }

        public int Rotate
        {
            get => ((IPdfNumber)this.GetInheritableAttribute(PdfName.Rotate))?.IntValue ?? 0;
            set
            {
                Set(PdfName.Rotate, value);
                box = null;
                rotateMatrix = null;
                invertRotateMatrix = null;
            }
        }

        public SKMatrix RotateMatrix
        {
            get => rotateMatrix ??= GraphicsState.GetRotationLeftBottomMatrix(Box, Rotate);
            set
            {
                rotateMatrix = value;
                invertRotateMatrix = null;
            }
        }

        public SKMatrix InvertRotateMatrix
        {
            get
            {
                if (invertRotateMatrix == null)
                {
                    RotateMatrix.TryInvert(out var invert);
                    invertRotateMatrix = invert;
                }
                return invertRotateMatrix.Value;
            }
        }

        public AppDataCollection AppData => GetOrCreate<AppDataCollection>(PdfName.PieceInfo).WithHolder(this);

        public DateTime? ModificationDate => GetDate(PdfName.LastModified);

        public List<ITextBlock> TextBlocks { get; } = new List<ITextBlock>();

        IList<ContentObject> ICompositeObject.Contents => Contents;

        public PdfPage Parent
        {
            get => Get<PdfPage>(PdfName.Parent);
            set => Set(PdfName.Parent, value);
        }

        public AppData GetAppData(PdfName appName)
        {
            return AppData.Ensure(appName);
        }

        public void Touch(PdfName appName)
        {
            Touch(appName, DateTime.Now);
        }

        public void Touch(PdfName appName, DateTime modificationDate)
        {
            GetAppData(appName).ModificationDate = modificationDate;
            Set(PdfName.LastModified, modificationDate);
        }

        public ContentObject ToInlineObject(PrimitiveComposer composer)
        { throw new NotImplementedException(); }

        public XObject ToXObject(PdfDocument context)
        {
            var form = new FormXObject(context, Box);
            form.Resources = (Resources)(
              context == Document  // [FIX:53] Ambiguous context identity.
                ? Resources // Same document: reuses the existing resources.
                : Resources.RefOrSelf.Clone(context).Resolve()); // Alien document: clones the resources.

            // Body (contents).
            var formBody = form.GetOutputStream();
            var contentsDataObject = Get<PdfDirectObject>(PdfName.Contents);
            if (contentsDataObject is PdfStream stream)
            {
                formBody.Write(stream.GetInputStream());
            }
            else if (contentsDataObject is PdfArray array)
            {
                foreach (var contentStreamObject in array.GetItems())
                {
                    formBody.Write(((PdfStream)contentStreamObject.Resolve(PdfName.Contents)).GetInputStream());
                }
            }
            return form;
        }

    }
}