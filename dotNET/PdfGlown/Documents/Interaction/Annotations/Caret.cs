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

using PdfGlown.Documents.Contents.XObjects;
using PdfGlown.Objects;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Interaction.Annotations
{
    /// <summary>Caret annotation [PDF:1.6:8.4.5].</summary>
    /// <remarks>It displays a visual symbol that indicates the presence of text edits.</remarks>
    [Pdf(VersionEnum.PDF15)]
    public sealed class Caret : Markup
    {
        /// <summary>Symbol type [PDF:1.6:8.4.5].</summary>
        public enum SymbolTypeEnum
        {
            /// <summary>None.</summary>
            None,
            /// <summary>New paragraph.</summary>
            NewParagraph
        };

        private static readonly SymbolTypeEnum DefaultSymbolType = SymbolTypeEnum.None;

        private static readonly Dictionary<SymbolTypeEnum, PdfName> SymbolTypeEnumCodes = new()
        {
            [SymbolTypeEnum.NewParagraph] = PdfName.P,
            [SymbolTypeEnum.None] = PdfName.None
        };

        /// <summary>Gets the code corresponding to the given value.</summary>
        private static PdfName ToCode(SymbolTypeEnum value)
        {
            return SymbolTypeEnumCodes[value];
        }

        /// <summary>Gets the symbol type corresponding to the given value.</summary>
        private static SymbolTypeEnum ToSymbolTypeEnum(IPdfString value)
        {
            if (value == null)
                return DefaultSymbolType;
            foreach (KeyValuePair<SymbolTypeEnum, PdfName> symbolType in SymbolTypeEnumCodes)
            {
                if (string.Equals(symbolType.Value.StringValue, value.StringValue, StringComparison.Ordinal))
                    return symbolType.Key;
            }
            return DefaultSymbolType;
        }

        public Caret(PdfPage page, SKRect box, string text)
            : base(page, PdfName.Caret, box, text)
        { }

        internal Caret(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets/Sets the symbol to be used in displaying the annotation.</summary>
        public SymbolTypeEnum SymbolType
        {
            get => ToSymbolTypeEnum(Get<IPdfString>(PdfName.Sy));
            set => this[PdfName.Sy] = value != DefaultSymbolType ? ToCode(value) : null;
        }

        public override bool AllowSize => false;

        protected override FormXObject GenerateAppearance()
        {
            return null;
        }
    }
}