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
using PdfGlown.Documents.Contents.Composition;
using PdfGlown.Documents.Contents.Fonts;
using PdfGlown.Documents.Contents.Objects;
using PdfGlown.Documents.Contents.Scanner;
using PdfGlown.Documents.Interchange.Metadata;
using PdfGlown.Objects;
using PdfGlown.Util.Math;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents.XObjects
{
    /// <summary>Form external object [PDF:1.6:4.9].</summary>
    [Pdf(VersionEnum.PDF10)]
    public sealed class FormXObject : XObject, IContentContext
    {
        private SKPicture picture;
        private SKMatrix? matrix;
        private SKRect? box;
        private ContentWrapper contents;

        ///<summary>Creates a new form within the specified document context.</summary>
        ///<param name="context">Document where to place this form.</param>
        ///<param name="size">Form size.</param>
        public FormXObject(PdfDocument context, SKSize size)
            : this(context, SKRect.Create(size))
        { }

        ///<summary>Creates a new form within the specified document context.</summary>
        ///<param name="context">Document where to place this form.</param>
        ///<param name="box">Form box.</param>
        public FormXObject(PdfDocument context, SKRect box)
            : base(context)
        {
            this[PdfName.Subtype] = PdfName.Form;
            Box = box;
        }

        internal FormXObject(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        internal FormXObject(Dictionary<PdfName, PdfDirectObject> baseObject, IInputStream stream)
            : base(baseObject, stream)
        { }

        public override SKMatrix Matrix
        {
            //NOTE: Form-space-to-user-space matrix is identity [1 0 0 1 0 0] by default,
            //but may be adjusted by setting the matrix entry in the form dictionary [PDF:1.6:4.9].
            get => matrix ??= Get<PdfArray>(PdfName.Matrix)?.ToSkMatrix() ?? SKMatrix.Identity;
            set
            {
                matrix = value;
                this[PdfName.Matrix] = value.ToPdfArray();
            }
        }

        public TransparencyXObject Group => Get<TransparencyXObject>(PdfName.Group);

        public override SKSize Size
        {
            get
            {
                var box = BBox;
                return new SKSize((float)box.Width, (float)box.Height);
            }
            set
            {
                var boxObject = Get<PdfRectangle>(PdfName.BBox);
                boxObject.Set(2, Math.Abs(value.Width) + boxObject.GetFloat(0));
                boxObject.Set(3, Math.Abs(value.Height) + boxObject.GetFloat(1));
                box = null;
            }
        }

        public PdfRectangle BBox
        {
            get => GetOrCreate<PdfRectangle>(PdfName.BBox);
            set
            {
                SetDirect(PdfName.BBox, value);
                box = null;
            }
        }

        public SKRect Box
        {
            get => box ??= BBox.ToSKRect();
            set
            {
                var newValue = value.Round();
                if (Box != newValue)
                {
                    box = newValue;
                    BBox.Update(newValue);
                }
            }
        }

        public ContentWrapper Contents => contents ??= new ContentWrapper(this);

        public Resources Resources
        {
            get => GetOrCreateInderect<Resources>(PdfName.Resources);
            set => Set(PdfName.Resources, value);
        }

        public RotationEnum Rotation => RotationEnum.Downward;

        public int Rotate => 0;

        public SKMatrix RotateMatrix => SKMatrix.Identity;

        //public SKMatrix TextMatrix => SKMatrix.Identity;

        public List<ITextBlock> TextBlocks { get; } = new List<ITextBlock>();

        public AppDataCollection AppData
        {
            get => GetOrCreate<AppDataCollection>(PdfName.PieceInfo).WithHolder(this);
        }

        public DateTime? ModificationDate => GetNDate(PdfName.LastModified);

        public SKMatrix InitialMatrix { get; internal set; } = SKMatrix.Identity;

        IList<ContentObject> ICompositeObject.Contents => Contents;

        public void ReloadContents()
        {
            if (contents != null)
            {
                Contents.Load();
            }
            InvalidatePicture();
        }

        public SKPicture Render(ContentScanner resourceScanner, SKColor? clearColor = null)
        {
            if (picture != null)
                return picture;

            var box = Box;
            using var recorder = new SKPictureRecorder();
#if NET9_0_OR_GREATER
            using var canvas = recorder.BeginRecording(box);
#else
            using var canvas = recorder.BeginRecording(box);
#endif
            Render(canvas, box, clearColor, resourceScanner);
            return picture = recorder.EndRecording();
        }

        public void Render(SKCanvas canvas, SKRect box, SKColor? clearColor = null)
            => Render(canvas, box, clearColor, null);

        public void Render(SKCanvas canvas, SKRect box, SKColor? clearColor = null, ContentScanner resourseScanner = null)
        {
            ReloadContents();
            var scanner = new ContentScanner(this, canvas, box, clearColor)
            {
                ResourceParent = resourseScanner
            };
            scanner.Scan();
        }



        public AppData GetAppData(PdfName appName) => AppData.Ensure(appName);

        public void Touch(PdfName appName) => Touch(appName, DateTime.Now);

        public void Touch(PdfName appName, DateTime modificationDate)
        {
            GetAppData(appName).ModificationDate = modificationDate;
            Set(PdfName.LastModified, modificationDate);
        }

        public ContentObject ToInlineObject(PrimitiveComposer composer)
        {
            throw new NotImplementedException();
        }

        public XObject ToXObject(PdfDocument context) => (XObject)RefOrSelf.Clone(context).Resolve(PdfName.XObject);

        internal void InvalidatePicture()
        {
            picture?.Dispose();
            picture = null;
        }

        public PdfName GetDefaultFont(out PdfFont defaultFont, FontName fontName = FontName.Helvetica)
        {
            // Retrieving the font to define the default appearance...
            PdfName defaultFontName = null;
            defaultFont = null;
            defaultFontName = null;
            {
                // Field fonts.
                var normalAppearanceFonts = Resources.Fonts;
                foreach (KeyValuePair<PdfName, PdfFont> entry in normalAppearanceFonts)
                {
                    if (!entry.Value.Symbolic)
                    {
                        defaultFont = entry.Value;
                        defaultFontName = entry.Key;
                        break;
                    }
                }
                if (defaultFontName == null)
                {
                    // Common fonts.
                    var formFonts = Document.Catalog.Form.Resources.Fonts;
                    foreach (KeyValuePair<PdfName, PdfFont> entry in formFonts)
                    {
                        if (!entry.Value.Symbolic && !entry.Value.IsStandard14)
                        {
                            defaultFont = entry.Value;
                            defaultFontName = entry.Key;
                            break;
                        }
                    }
                    if (defaultFontName == null)
                    {
                        //TODO:manage name collision!
                        formFonts[defaultFontName = PdfName.Get("defaultTTF")] = defaultFont = PdfType0Font.Load(Document, fontName);
                    }
                    normalAppearanceFonts[defaultFontName] = defaultFont;
                }
            }

            return defaultFontName;
        }
    }
}