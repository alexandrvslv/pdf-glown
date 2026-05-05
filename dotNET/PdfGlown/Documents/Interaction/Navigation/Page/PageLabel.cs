/*
  Copyright 2012 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfGlown.Util;

using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Interaction.Navigation
{
    /// <summary>Page label range [PDF:1.7:8.3.1].</summary>
    /// <remarks>It represents a series of consecutive pages' visual identifiers using the same
    /// numbering system.</remarks>
    [Pdf(VersionEnum.PDF13)]
    public sealed class PageLabel : PdfDictionary, IPdfObjectWrapper
    {
        public enum NumberStyleEnum
        {
            /// <summary>Decimal arabic numerals.</summary>
            ArabicNumber,
            /// <summary>Upper-case roman numerals.</summary>
            UCaseRomanNumber,
            /// <summary>Lower-case roman numerals.</summary>
            LCaseRomanNumber,
            /// <summary>Upper-case letters (A to Z for the first 26 pages, AA to ZZ for the next 26, and so
            /// on).</summary>
            UCaseLetter,
            /// <summary>Lower-case letters (a to z for the first 26 pages, aa to zz for the next 26, and so
            /// on).</summary>
            LCaseLetter
        };

        private static readonly int DefaultNumberBase = 1;
        private static readonly BiDictionary<NumberStyleEnum, string> styleCodes = new()
        {
            [NumberStyleEnum.ArabicNumber] = PdfName.D.StringValue,
            [NumberStyleEnum.UCaseRomanNumber] = PdfName.R.StringValue,
            [NumberStyleEnum.LCaseRomanNumber] = PdfName.r.StringValue,
            [NumberStyleEnum.UCaseLetter] = PdfName.A.StringValue,
            [NumberStyleEnum.LCaseLetter] = PdfName.a.StringValue
        };

        public static NumberStyleEnum GetStyle(string name)
        {
            if (name == null)
                throw new ArgumentNullException();

            NumberStyleEnum? numberStyle = styleCodes.GetKey(name);
            if (!numberStyle.HasValue)
                throw new NotSupportedException("Page layout unknown: " + name);

            return numberStyle.Value;
        }

        public static PdfName GetCode(NumberStyleEnum numberStyle) => PdfName.Get(styleCodes[numberStyle], true);

        /// <summary>Gets an existing page label range.</summary>
        /// <param name="baseObject">Base object to wrap.</param>

        public PageLabel(PdfDocument context, NumberStyleEnum numberStyle)
            : this(context, null, numberStyle, DefaultNumberBase)
        { }

        public PageLabel(PdfDocument context, String prefix, NumberStyleEnum numberStyle, int numberBase)
            : base(context, new Dictionary<PdfName, PdfDirectObject>(1) {
                { PdfName.Type, PdfName.PageLabel }
            })
        {
            Prefix = prefix;
            NumberStyle = numberStyle;
            NumberBase = numberBase;
        }

        internal PageLabel(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets/Sets the value of the numeric suffix for the first page label in this range.
        /// Subsequent pages are numbered sequentially from this value.</summary>
        public int NumberBase
        {
            get => GetInt(PdfName.St, DefaultNumberBase);
            set => Set(PdfName.St, value <= DefaultNumberBase ? null : value);
        }

        /// <summary>Gets/Sets the numbering style to be used for the numeric suffix of each page label in
        /// this range.</summary>
        /// <remarks>If no style is defined, the numeric suffix isn't displayed at all.</remarks>
        public NumberStyleEnum NumberStyle
        {
            get => GetStyle(GetString(PdfName.S));
            set => this[PdfName.S] = GetCode(value);
        }

        /// <summary>Gets/Sets the label prefix for page labels in this range.</summary>
        public string Prefix
        {
            get => GetString(PdfName.P);
            set => SetText(PdfName.P, value);
        }
    }
}