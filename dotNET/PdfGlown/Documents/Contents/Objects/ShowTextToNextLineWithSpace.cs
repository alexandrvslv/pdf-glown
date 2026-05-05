/*
  Copyright 2009-2015 Stefano Chizzolini. http://www.pdfclown.org

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

namespace PdfGlown.Documents.Contents.Objects
{
    public sealed class ShowTextToNextLineWithSpace : ShowTextToNextLine
    {
        /// <summary>Specifies the word spacing and the character spacing
        /// (setting the corresponding parameters in the text state).</summary>
        public static readonly string OperatorKeyword = "''";

        /// <param name="text">Text encoded using current font's encoding.</param>
        /// <param name="wordSpace">Word spacing.</param>
        /// <param name="charSpace">Character spacing.</param>
        public ShowTextToNextLineWithSpace(byte[] text, double wordSpace, double charSpace)
            : base(OperatorKeyword, text, wordSpace, charSpace)
        { }

        public ShowTextToNextLineWithSpace(PdfArray operands)
            : base(OperatorKeyword, operands)
        { }

        protected override PdfString TextElement
        {
            get => (PdfString)operands.Get(2);
            set => operands.SetSimple(2, value);
        }

        /// <summary>Gets/Sets the character spacing.</summary>
        public float CharSpace
        {
            get => operands.GetFloat(1);
            set
            {
                EnsureSpaceOperation();
                operands.Set(1, value);
            }
        }

        /// <summary>Gets/Sets the word spacing.</summary>
        public float WordSpace
        {
            get => operands.GetFloat(0);
            set
            {
                EnsureSpaceOperation();
                operands.Set(0, value);
            }
        }

        private void EnsureSpaceOperation()
        {
            if (!OperatorKeyword.Equals(@operator, StringComparison.Ordinal))
            {
                @operator = OperatorKeyword;
                operands.Insert(0, PdfReal.Get(0));
                operands.Insert(1, PdfReal.Get(0));
            }
        }

        public override void Scan(GraphicsState state)
        {
            state.WordSpace = WordSpace;
            state.CharSpace = CharSpace;
            base.Scan(state);
        }
    }
}