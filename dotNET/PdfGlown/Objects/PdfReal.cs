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

using PdfGlown.Bytes;
using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace PdfGlown.Objects
{
    /// <summary>PDF real number object [PDF:1.6:3.2.2].</summary>
    public sealed class PdfReal : PdfSimpleObject<double>, IPdfNumber
    {
        private static readonly NumberFormatInfo formatInfo;

        public static readonly PdfReal Zero = new PdfReal(0D);

        static PdfReal()
        {
            formatInfo = new NumberFormatInfo();
            formatInfo.NumberDecimalSeparator = ".";
            formatInfo.NegativeSign = "-";
        }

        /// <summary>Gets the object equivalent to the given value.</summary>
        public static PdfReal Get(double? value, int roundDigit = 5)
        {
            if (!value.HasValue)
                return null;

            return Get(value.Value, roundDigit);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PdfReal Get(double value, int roundDigit = 5)
        {
            return Double.IsNaN(value) ? null : new PdfReal(Math.Round(value, roundDigit));
        }

        public PdfReal(double value) => RawValue = value;

        public override PdfObject Accept(IVisitor visitor, PdfName parentKey, object data) => visitor.Visit(this, parentKey, data);

        public override int CompareTo(PdfDirectObject obj) => PdfNumber.Compare(this, obj);

        public int CompareTo(object obj) => PdfNumber.Compare(this, obj);

        public override bool Equals(object obj) => PdfNumber.Equal(this, obj);

        public override int GetHashCode() => PdfNumber.GetHashCode(this);

        public override void WriteTo(IOutputStream stream, PdfDocument context) => stream.WriteAsString(RawValue, context.Configuration.RealFormat, formatInfo);

        public double DoubleValue => RawValue;

        public float FloatValue => (float)RawValue;

        public int IntValue => (int)Math.Round(RawValue);

        public long LongValue => (long)Math.Round(RawValue);

        public T GetValue<T>() where T : struct
        {
            if (typeof(T) == typeof(double))
                return Unsafe.As<double, T>(ref v);
            else if (typeof(T) == typeof(float))
            {
                var value = FloatValue;
                return Unsafe.As<float, T>(ref value);
            }
            else if (typeof(T) == typeof(int))
            {
                var value = IntValue;
                return Unsafe.As<int, T>(ref value);
            }
            else if (typeof(T) == typeof(long))
            {
                var value = LongValue;
                return Unsafe.As<long, T>(ref value);
            }
            throw new Exception($"TODO support {typeof(T)}");
        }
    }
}