/*
  Copyright 2006-2012 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfGlown.Tokens;
using PdfGlown.Util;
using System;
using System.Globalization;

namespace PdfGlown.Objects
{
    /// <summary>PDF date object [PDF:1.6:3.8.3].</summary>
    public sealed class PdfDate : PdfString
    {
        private const string FormatString = "yyyyMMddHHmmsszzz";
        private DateTime? date;

        /// <summary>Gets the object equivalent to the given value.</summary>
        public static PdfDate Get(DateTime? value)
        {
            return value.HasValue ? new PdfDate(Trimm(value).Value) : null;
        }

        public static PdfDate Get(Memory<byte> data, DateTime? value)
        {
            return value.HasValue ? new PdfDate(data, value.Value) : null;
        }

        /// <summary>Converts a PDF date literal into its corresponding date.</summary>
        /// <exception cref="PdfGlown.Util.Parsers.ParseException">Thrown when date literal parsing fails.</exception>
        public static bool ToDate(ReadOnlySpan<char> value, out DateTime date)
        {
            date = DateTime.MinValue;
            if (value.IsEmpty)
                return false;
            value = value.Trim();
            if (value.Equals("D:", StringComparison.Ordinal)
                || value.Length < 6)
                return false;
            // 1. Normalization.
            var dateBuilder = new StringStream();
            try
            {
                int length = value.Length;
                // Year (YYYY). 
                dateBuilder.Append(value.Slice(2, 4)); // NOTE: Skips the "D:" prefix; Year is mandatory.
                // Month (MM).
                dateBuilder.Append(length < 8 ? "01" : value.Slice(6, 2));
                // Day (DD).
                dateBuilder.Append(length < 10 ? "01" : value.Slice(8, 2));
                // Hour (HH).
                dateBuilder.Append(length < 12 ? "00" : value.Slice(10, 2));
                // Minute (mm).
                dateBuilder.Append(length < 14 ? "00" : value.Slice(12, 2));
                // Second (SS).
                dateBuilder.Append(length < 16 ? "00" : value.Slice(14, 2));
                // Local time / Universal Time relationship (O).
                dateBuilder.Append(length < 17 || value.Slice(16, 1).Equals("Z", StringComparison.Ordinal) ? "+" : value.Slice(16, 1));
                // UT Hour offset (HH').
                dateBuilder.Append(length < 19 ? "00" : value.Slice(17, 2));
                // UT Minute offset (mm').
                dateBuilder.Append(':');
                dateBuilder.Append(length < 22 ? "00" : value.Slice(20, 2));
            }
            catch
            {
                return false;
            }

            // 2. Parsing.
            return DateTime.TryParseExact(
                dateBuilder.AsSpan(),
                FormatString,
                CultureInfo.InvariantCulture,//("en-US")
                DateTimeStyles.None,
                out date);
        }

        private static string Format(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc)
                value = value.ToLocalTime();
            return ($"D:{value.ToString(FormatString, CultureInfo.InvariantCulture).Replace(':', '\'')}'");
        }

        public PdfDate(DateTime value)
        {
            DateValue = value;
        }

        public PdfDate(Memory<byte> data, DateTime value)
            : base(data)
        {
            date = value;
        }

        public override PdfObject Accept(IVisitor visitor, PdfName parentKey, object data) => visitor.Visit(this, parentKey, data);

        internal static DateTime? Trimm(DateTime? value)
        {
            if (value is DateTime dateTime && dateTime.Millisecond > 0)
            {
                return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second);
            }
            return value;
        }

        public override SerializationModeEnum SerializationMode
        {
            get => base.SerializationMode;
            set
            {/* NOOP: Serialization MUST be kept literal. */}
        }

        public override object Value
        {
            get => DateValue;
            protected set
            {
                if (value is DateTime dateTimeValue)
                {
                    RawValue = BaseEncoding.Pdf.Encode(Format(dateTimeValue));
                    date = dateTimeValue;
                }
            }
        }

        public DateTime? DateValue
        {
            get => date ??= (ToDate(StringValue, out var parsed) ? parsed : null);
            set => Value = date = value;
        }
    }
}
