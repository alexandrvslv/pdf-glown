/*
  Copyright 2008-2015 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfGlown.Documents.Contents.ColorSpaces;
using PdfGlown.Documents.Contents.Composition;
using PdfGlown.Documents.Contents.Fonts;
using PdfGlown.Documents.Interaction.Annotations;
using PdfGlown.Documents.Interaction.Forms.Signature;
using PdfGlown.Objects;
using SkiaSharp;
using System;
using System.Linq;

namespace PdfGlown.Documents.Interaction.Forms
{
    /// <summary>Signature field [PDF:1.6:8.6.3].</summary>
    [Pdf(VersionEnum.PDF13)]
    public sealed class SignatureField : Field
    {
        //workaround of cross thread error while reading
        private SignatureDictionary initialSD;

        ///<summary>Creates a new signature field within the given document context.</summary>
        public SignatureField(string name, Widget widget) : base(PdfName.Sig, name, widget)
        { }

        internal SignatureField(PdfDirectObject baseObject) : base(baseObject)
        {
            initialSD = DataObject.Get<SignatureDictionary>(PdfName.V);
        }

        /// <returns>A <see cref="PdfDictionary"/>.</returns>
        public override object Value
        {
            get => SignatureDictionary;
            set => SignatureDictionary = (SignatureDictionary)value;
        }

        public SignatureDictionary SignatureDictionary
        {
            get => DataObject.Get<SignatureDictionary>(PdfName.V);
            set => DataObject.SetDirect(PdfName.V, value);
        }

        public void RefreshAppearence(string text)
        {
            var nameArray = (SignatureDictionary?.Name ?? "Sign Here").Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var widget = Widgets[0];
            var rect = widget.Box;

            var normalAppearanceState = widget.ResetAppearance(out var zeroMatrix);

            var box = zeroMatrix.MapRect(rect);

            var font = PdfType0Font.Load(Document, FontName.TimesRoman);

            var horizontal = box.Width > box.Height;
            var maxSize = nameArray.Select(x => font.GetWidth(x, 1)).Max();
            var availible = horizontal ? (box.Width / 2) - 4 : box.Width - 4;
            var headerFontSize = availible / maxSize;
            var composer = new PrimitiveComposer(normalAppearanceState);

            composer.BeginLocalState();
            composer.ApplyMatrix(GraphicsState.GetRotationMatrix(box, widget.Page.Rotate));
            composer.SetFillColor(RGBColor.Black);
            composer.SetFont(font, headerFontSize);
            composer.ShowText(string.Join('\n', nameArray),
                horizontal
                    ? new SKPoint(box.Left, box.Height / 2)
                    : new SKPoint(box.Left, box.Height / 4),
                XAlignmentEnum.Left,
                YAlignmentEnum.Middle, 0);
            composer.End();


            composer.BeginLocalState();
            var blockComp = new BlockComposer(composer)
            {
                Hyphenation = true
            };
            blockComp.Begin(horizontal
                    ? new SKRect(box.MidX, box.Top, box.Right, box.Bottom)
                    : new SKRect(box.Left, box.MidY, box.Right, box.Bottom),
                XAlignmentEnum.Left,
                YAlignmentEnum.Middle);
            composer.SetFillColor(RGBColor.Black);
            composer.SetFont(font, headerFontSize / 2.5);
            blockComp.ShowText(text);
            blockComp.End();
            composer.End();
            composer.Flush();
        }
    }
}