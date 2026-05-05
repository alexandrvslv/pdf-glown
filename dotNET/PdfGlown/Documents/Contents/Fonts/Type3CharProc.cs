/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using PdfGlown.Documents.Contents.Composition;
using PdfGlown.Documents.Contents.Objects;
using PdfGlown.Documents.Contents.Scanner;
using PdfGlown.Documents.Contents.XObjects;
using PdfGlown.Documents.Interchange.Metadata;
using PdfGlown.Objects;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PdfGlown.Documents.Contents.Fonts
{
    /// <summary>
    /// A Type 3 character procedure.This is a standalone PDF content stream.
    /// @author John Hewson
    /// </summary>
    public sealed class Type3CharProc : PdfStream, IContentContext
    {
        private PdfType3Font font;
        private SKPicture picture;
        private ContentWrapper contents;
        private Resources resources;

        public Type3CharProc(PdfDocument document)
            : base(document, new Dictionary<PdfName, PdfDirectObject>())
        { }

        internal Type3CharProc(Dictionary<PdfName, PdfDirectObject> charStream)
            : base(charStream)
        { }

        public PdfType3Font Font
        {
            get => font;
            set => font = value;
        }

        public ContentWrapper Contents => contents ??= new ContentWrapper(RefOrSelf);

        IList<ContentObject> ICompositeObject.Contents => Contents;

        public SKMatrix Matrix
        {
            get => font.FontMatrix;
        }

        public Resources Resources
        {
            get => resources ??= GetResources();
        }

        private Resources GetResources()
        {
            if (Get<Resources>(PdfName.Resources) is Resources resourceDictionary)
            {
                // PDFBOX-5294
                Debug.WriteLine("warn: Using resources dictionary found in charproc entry");
                Debug.WriteLine("warn: This should have been in the font or in the page dictionary");
                return resourceDictionary;
            }
            return font.Resources;
        }

        public SKRect FontBBox
        {
            get => font.FontBBox;
        }

        /// <summary>
        /// Calculate the bounding box of this glyph.This will work only if the first operator in the
        /// stream is d1.
        /// </summary>
        public SKRect Box
        {
            get => GlyphBox ?? FontBBox;
        }

        public SKRect? GlyphBox
        {
            get => Contents.OfType<CharProcBBox>().FirstOrDefault()?.BBox;
        }

        /// <summary>
        /// Get the width from a type3 charproc stream.
        /// </summary>
        public float? Width
        {
            get => (float?)(Contents.OfType<CharProcWidth>().FirstOrDefault()?.WX ?? Contents.OfType<CharProcBBox>().FirstOrDefault()?.WX);
        }

        public RotationEnum Rotation => RotationEnum.Downward;

        public int Rotate => 0;

        public SKMatrix RotateMatrix
        {
            get => SKMatrix.Identity;
        }

        public List<ITextBlock> TextBlocks { get; set; }

        public TransparencyXObject Group { get => null; }

        public AppDataCollection AppData => null;

        public DateTime? ModificationDate => null;

        public SKPicture Render()
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
            Render(canvas, box, null);
            return picture = recorder.EndRecording();
        }

        public void Render(SKCanvas canvas, SKRect box, SKColor? clearColor = null)
        {
            var scanner = new ContentScanner(this, canvas, box, clearColor);
            scanner.Scan();
        }

        public AppData GetAppData(PdfName appName) => throw new NotSupportedException();

        public void Touch(PdfName appName) => throw new NotSupportedException();

        public void Touch(PdfName appName, DateTime modificationDate) => throw new NotSupportedException();

        public ContentObject ToInlineObject(PrimitiveComposer composer) => throw new NotImplementedException();

        public XObject ToXObject(PdfDocument context) => throw new NotImplementedException();
    }
}
