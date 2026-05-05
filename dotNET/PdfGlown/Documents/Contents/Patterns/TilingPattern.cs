/*
  Copyright 2010-2011 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfGlown.Objects;

using System;
using SkiaSharp;
using System.Collections.Generic;
using PdfGlown.Documents.Contents.Composition;
using PdfGlown.Documents.Contents.Objects;
using PdfGlown.Documents.Contents.XObjects;
using PdfGlown.Documents.Interchange.Metadata;
using PdfGlown.Documents.Contents.ColorSpaces;
using PdfGlown.Documents.Contents.Scanner;
using PdfGlown.Util.Math;

namespace PdfGlown.Documents.Contents.Patterns
{
    /// <summary>Pattern consisting of a small graphical figure called<i>pattern cell</i> [PDF:1.6:4.6.2].</summary>
    /// <remarks>Painting with the pattern replicates the cell at fixed horizontal and vertical intervals
    /// to fill an area.</remarks>
    [Pdf(VersionEnum.PDF12)]
    public class TilingPattern : Pattern, IContentContext
    {
        private SKPicture picture;
        private SKRect? box;
        private List<ITextBlock> textBlocks;
        private ContentWrapper contents;

        internal TilingPattern(PatternColorSpace colorSpace, Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(colorSpace, baseObject)
        { }

        internal TilingPattern(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets the colorized representation of this pattern.</summary>
        /// <param name="color"> Color to be applied to the pattern.</param>
        public ColorizedTilingPattern Colorize(IColor color)
        {
            if (PaintType != TilingPaintTypeEnum.Uncolored)
                throw new NotSupportedException("Only uncolored tiling patterns can be colorized.");

            return new ColorizedTilingPattern(this, color);
        }

        /// <summary>Gets the pattern cell's bounding box (expressed in the pattern coordinate system)
        /// used to clip the pattern cell.</summary>
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

        /// <summary>Gets how the color of the pattern cell is to be specified.</summary>
        public TilingPaintTypeEnum PaintType
        {
            get => (TilingPaintTypeEnum)GetInt(PdfName.PaintType);
            set => Set(PdfName.PaintType, (int)value);
        }

        /// <summary>Gets the named resources required by the pattern's content stream.</summary>
        public Resources Resources => Get<Resources>(PdfName.Resources);

        /// <summary>Gets how to adjust the spacing of tiles relative to the device pixel grid.</summary>
        public TilingTypeEnum TilingType
        {
            get => (TilingTypeEnum)GetInt(PdfName.TilingType);
            set => Set(PdfName.TilingType, (int)value);
        }

        /// <summary>Gets the horizontal spacing between pattern cells (expressed in the pattern coordinate system).</summary>
        public float XStep
        {
            get => GetFloat(PdfName.XStep);
            set => Set(PdfName.XStep, value);
        }

        /// <summary>Gets the vertical spacing between pattern cells (expressed in the pattern coordinate system).</summary>
        public float YStep
        {
            get => GetFloat(PdfName.YStep);
            set => Set(PdfName.YStep, value);
        }

        public ContentWrapper Contents => contents ??= new ContentWrapper(RefOrSelf);


        public RotationEnum Rotation => RotationEnum.Downward;

        public int Rotate => 0;

        public SKMatrix RotateMatrix => SKMatrix.Identity;

        public AppDataCollection AppData => throw new NotImplementedException();

        public DateTime? ModificationDate => GetDate(PdfName.LastModified);

        public TransparencyXObject Group => Get<TransparencyXObject>(PdfName.Group);

        public List<ITextBlock> TextBlocks => textBlocks ??= new List<ITextBlock>();

        IList<ContentObject> ICompositeObject.Contents => Contents;

        public SKPicture GetPicture(GraphicsState state)
        {
            if (picture != null)
                return picture;
            var box = Matrix.MapRect(Box);
            using var recorder = new SKPictureRecorder();// SKBitmap((int)box.Width, (int)box.Height);
#if NET9_0_OR_GREATER
            using var canvas = recorder.BeginRecording(box);
#else
            using var canvas = recorder.BeginRecording(box);
#endif
            Render(canvas, box, null, state.Scanner);
            return picture = recorder.EndRecording();
        }

        public override SKShader GetShader(GraphicsState state)
        {
            //var tile = Matrix.MapRect(SKRect.Create(Math.Abs(XStep), Math.Abs(YStep)));
            return SKShader.CreatePicture(GetPicture(state), SKShaderTileMode.Repeat, SKShaderTileMode.Repeat);//, tile);//, Matrix, rect);
        }

        public void Render(SKCanvas canvas, SKRect box, SKColor? clearColor = null)
        {
            Render(canvas, box, clearColor, null);
        }

        public void Render(SKCanvas canvas, SKRect box, SKColor? clearColor, ContentScanner resurceScanner)
        {
            var scanner = new ContentScanner(this, canvas, box, clearColor)
            {
                ResourceParent = resurceScanner
            };
            scanner.Scan();
        }

        public AppData GetAppData(PdfName appName) => throw new NotImplementedException();

        public void Touch(PdfName appName) => throw new NotImplementedException();

        public void Touch(PdfName appName, DateTime modificationDate) => throw new NotImplementedException();

        public ContentObject ToInlineObject(PrimitiveComposer composer) => throw new NotImplementedException();

        public XObject ToXObject(PdfDocument context) => throw new NotImplementedException();


    }
}