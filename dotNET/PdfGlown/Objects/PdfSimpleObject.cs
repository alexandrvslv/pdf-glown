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

using System;

namespace PdfGlown.Objects
{
    /// <summary>Abstract PDF simple object.</summary>
    public abstract class PdfSimpleObject<TValue> : PdfDirectObject, IPdfSimpleObject<TValue>
    {
        /// <summary>Gets the object equivalent to the given value.</summary>
        public static PdfDirectObject Get(object value)
        {
            if (value == null)
                return null;

            if (value is int intValue)
                return PdfInteger.Get(intValue);
            else if (value is double doubleValue)
                return PdfReal.Get(doubleValue);
            else if (value is float singleValue)
                return PdfReal.Get(singleValue);
            else if (value is string stringValue)
                return PdfTextString.Get(stringValue);
            else if (value is DateTime dateTimeValue)
                return PdfDate.Get(dateTimeValue);
            else if (value is bool booleanValue)
                return PdfBoolean.Get(booleanValue);
            else if (value is long longValue)
                return PdfInteger.Get((int)longValue);
            else
                throw new NotImplementedException();
        }

        public static double? GetDoubleValue(PdfObject obj)
        {
            object value = GetValue(obj);
            return value switch
            {
                null => null,
                double dValue => dValue,
                _ => Convert.ToDouble(value)
            };
        }

        /// <summary>Gets the value corresponding to the given object.</summary>
        /// <param name="obj">Object to extract the value from.</param>
        public static object GetValue(PdfObject obj) => GetValue(obj, null);

        /// <summary>Gets the value corresponding to the given object.</summary>
        /// <param name="obj">Object to extract the value from.</param>
        /// <param name="defaultValue">Value returned in case the object's one is undefined.</param>
        public static object GetValue(PdfObject obj, object defaultValue)
        {
            return (obj?.Resolve() as IPdfValued)?.Value ?? defaultValue;
        }

        protected TValue v;

        public PdfSimpleObject()
        { }

        // NOTE: As simple objects are immutable, no parent can be associated.
        public sealed override PdfObject ParentObject
        {
            get => null;
            internal set { }
        }

        /// <summary>Gets/Sets the low-level representation of the value.</summary>
        public virtual TValue RawValue
        {
            get => v;
            protected set => v = value;
        }

        public sealed override PdfObjectStatus Status
        {
            get => PdfObjectStatus.None; // NOTE: Simple objects are immutable.
            protected internal set { }// NOOP: As simple objects are immutable, no update can be done.
        }

        /// <summary>Gets/Sets the high-level representation of the value.</summary>
        public virtual object Value
        {
            get => v;
            protected set => v = (TValue)value;
        }

        public override PdfObject Clone(PdfDocument context) => this;  // NOTE: Simple objects are immutable.

        public override bool Equals(object @object)
        {
            return base.Equals(@object)
              || (@object != null
                && @object.GetType().Equals(GetType())
                && ((PdfSimpleObject<TValue>)@object).RawValue.Equals(RawValue));
        }

        public override int GetHashCode() => RawValue.GetHashCode();

        public override PdfObject Swap(PdfObject other) => throw new NotSupportedException("Immutable object");

        public override string ToString() => Value.ToString();

    }
}