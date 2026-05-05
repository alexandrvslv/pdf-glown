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

using PdfGlown.Bytes;
using PdfGlown.Documents.Contents.Composition;
using PdfGlown.Documents.Contents.Objects;
using PdfGlown.Documents.Interaction.Annotations;
using PdfGlown.Objects;
using PdfGlown.Util;
using SkiaSharp;
using System;

namespace PdfGlown.Documents.Interaction.Forms
{
    /// <summary>Text field [PDF:1.6:8.6.3].</summary>
    [Pdf(VersionEnum.PDF12)]
    public sealed class TextField : Field
    {
        /// <summary>Creates a new text field within the given document context.</summary>
        public TextField(string name, Widget widget, string value)
            : base(PdfName.Tx, name, widget)
        { Value = value; }

        internal TextField(PdfDirectObject baseObject) : base(baseObject)
        { }

        /// <summary>Gets/Sets whether the field can contain multiple lines of text.</summary>
        public bool IsMultiline
        {
            get => (Flags & FlagsEnum.Multiline) == FlagsEnum.Multiline;
            set => Flags = EnumUtils.Mask(Flags, FlagsEnum.Multiline, value);
        }

        /// <summary>Gets/Sets whether the field is intended for entering a secure password.</summary>
        public bool IsPassword
        {
            get => (Flags & FlagsEnum.Password) == FlagsEnum.Password;
            set => Flags = EnumUtils.Mask(Flags, FlagsEnum.Password, value);
        }

        /// <summary>Gets/Sets the justification to be used in displaying this field's text.</summary>
        public JustificationEnum Justification
        {
            get => (JustificationEnum)DataObject.GetInt(PdfName.Q);
            set => DataObject.Set(PdfName.Q, (int)value);
        }

        /// <summary>Gets/Sets the maximum length of the field's text, in characters.</summary>
        /// <remarks>It corresponds to the maximum integer value in case no explicit limit is defined.</remarks>
        public int MaxLength
        {
            get
            {
                var maxLengthObject = (PdfInteger)DataObject.GetInheritableAttribute(PdfName.MaxLen)?.Resolve(PdfName.MaxLen);
                return maxLengthObject != null ? maxLengthObject.IntValue : Int32.MaxValue;
            }
            set => DataObject.Set(PdfName.MaxLen, value != Int32.MaxValue ? value : null);
        }

        /// <summary>Gets/Sets whether text entered in the field is spell-checked.</summary>
        public bool SpellChecked
        {
            get => (Flags & FlagsEnum.DoNotSpellCheck) != FlagsEnum.DoNotSpellCheck;
            set => Flags = EnumUtils.Mask(Flags, FlagsEnum.DoNotSpellCheck, !value);
        }

        /// <returns>Either a string or an <see cref="IByteStream"/>.</returns>
        public override object Value
        {
            get
            {
                var valueObject = DataObject.GetInheritableAttribute(PdfName.V)?.Resolve(PdfName.V);
                if (valueObject is PdfString pdfString)
                    return pdfString.Value;
                else if (valueObject is PdfStream pdfStream)
                    return pdfStream.GetInputStream();
                else
                    return null;
            }
            set
            {
                if (!(value == null
                    || value is string
                    || value is IByteStream))
                    throw new ArgumentException("Value MUST be either a String or an IByteStream");

                if (value != null)
                {
                    var oldValueObject = DataObject.Get(PdfName.V);
                    IByteStream valueObjectBuffer = null;
                    if (oldValueObject is PdfStream stream)
                    {
                        valueObjectBuffer = stream.GetOutputStream();
                        valueObjectBuffer.SetLength(0);
                    }
                    if (value is string stringValue)
                    {
                        if (valueObjectBuffer != null)
                        { valueObjectBuffer.Write(stringValue); }
                        else
                        { DataObject[PdfName.V] = new PdfTextString(stringValue); }
                    }
                    else if (value is IByteStream inputStream) // IBuffer.
                    {
                        if (valueObjectBuffer != null)
                        { valueObjectBuffer.Write(inputStream); }
                        else
                        { DataObject[PdfName.V] = Document.Register(new PdfStream(inputStream)); }
                    }
                }
                else
                { DataObject[PdfName.V] = null; }

                RefreshAppearance();
            }
        }

        private void RefreshAppearance()
        {
            var widget = Widgets[0];
            var normalAppearance = widget.ResetAppearance(out var zeroMatrix);
            PdfName fontName = null;
            double fontSize = 0;
            {
                PdfString defaultAppearanceState = DAString;
                if (defaultAppearanceState == null)
                {
                    var defaultFontName = normalAppearance.GetDefaultFont(out _);
                    DAOperation = new SetFont(defaultFontName, IsMultiline ? 9 : 10);
                }

                // Retrieving the font to use...
                var setFont = DAOperation;
                fontName = setFont.Name;
                fontSize = setFont.Size;
                if (!Catalog.Form.Resources.Fonts.ContainsKey(fontName))
                {
                    
                }
                normalAppearance.Resources.Fonts[fontName] = Catalog.Form.Resources.Fonts[fontName];
            }

            // Refreshing the field appearance...

            // TODO: resources MUST be resolved both through the apperance stream resource dictionary and
            // from the DR-entry acroform resource dictionary
            var baseComposer = new PrimitiveComposer(normalAppearance);
            var composer = new BlockComposer(baseComposer);
            var scanner = composer.Scanner;
            bool textShown = false;
            scanner.OnObjectScanning += OnObjectStarting;
            scanner.Scan();
            scanner.OnObjectScanning -= OnObjectStarting;
            bool OnObjectStarting(ContentObject content, ICompositeObject container, int index)
            {
                if (content is GraphicsMarkedContent markedContent)
                {
                    if (PdfName.Tx.Equals(markedContent.MarkerHeader.Tag))
                    {
                        // Remove old text representation!
                        markedContent.Contents.Clear();
                        // Add new text representation!
                        ShowText(composer, fontName, fontSize);
                        textShown = true;
                    }
                }
                else if (content is GraphicsText && !textShown)
                {
                    container.Contents.RemoveAt(index);
                    return false;
                }

                return true;
            }
            if (!textShown)
            {
                baseComposer.BeginMarkedContent(PdfName.Tx);
                ShowText(composer, fontName, fontSize);
                baseComposer.End();
            }
            baseComposer.Flush();
        }

        private void ShowText(BlockComposer composer, PdfName fontName, double fontSize)
        {
            var baseComposer = composer.BaseComposer;
            var scanner = baseComposer.Scanner;
            SKRect textBox = scanner.Context.Box;
            if (scanner.State.Font == null)
            {
                // NOTE: A zero value for size means that the font is to be auto-sized: its size is computed as
                // a function of the height of the annotation rectangle.
                if (fontSize == 10)
                { fontSize = textBox.Height * 0.6; }
                baseComposer.SetFont(fontName, fontSize);
            }

            string text = (string)Value;

            FlagsEnum flags = Flags;
            if ((flags & FlagsEnum.Comb) == FlagsEnum.Comb
              && (flags & FlagsEnum.FileSelect) == 0
              && (flags & FlagsEnum.Multiline) == 0
              && (flags & FlagsEnum.Password) == 0)
            {
                int maxLength = MaxLength;
                if (maxLength > 0)
                {
                    textBox.Right = textBox.Left + textBox.Width / maxLength;
                    for (int index = 0, length = text.Length; index < length; index++)
                    {
                        composer.Begin(textBox, XAlignmentEnum.Center, YAlignmentEnum.Middle);
                        composer.ShowText(text[index].ToString());
                        composer.End();
                        textBox.Left += textBox.Width;
                        textBox.Right += textBox.Width;
                    }
                    return;
                }
            }

            textBox.Inflate(-2, 0);
            YAlignmentEnum yAlignment;
            if ((flags & FlagsEnum.Multiline) == FlagsEnum.Multiline)
            {
                yAlignment = YAlignmentEnum.Top;
                textBox.Inflate(0, -(float)(fontSize * .35));
            }
            else
            {
                yAlignment = YAlignmentEnum.Middle;
            }
            composer.Begin(textBox, Justification.ToXAlignment(), yAlignment);
            composer.ShowText(text);
            composer.End();
        }
    }
}