/*
  Copyright 2008-2012 Stefano Chizzolini. http://www.pdfclown.org

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

using System;

namespace PdfGlown.Objects
{
    /**
      <summary>PDF text string object [PDF:1.6:3.8.1].</summary>
      <remarks>Text strings are meaningful only as part of the document hierarchy; they cannot appear
      within content streams. They represent information that is intended to be human-readable.</remarks>
    */
    public sealed class PdfTextString : PdfString
    {
        /*
          NOTE: Text strings are string objects encoded in either PdfDocEncoding (superset of the ISO
          Latin 1 encoding [PDF:1.6:D]) or 16-bit big-endian Unicode character encoding (see [UCS:4]).
        */
        public static readonly new PdfTextString Default = new(string.Empty);

        /// <summary>Gets the object equivalent to the given value.</summary>
        public static new PdfTextString Get(string value) => value != null ? new PdfTextString(value) : null;

        private bool unicoded;

        public PdfTextString(Memory<byte> rawValue, SerializationModeEnum serializationMode = SerializationModeEnum.Literal)
           : base(rawValue, serializationMode)
        { }

        public PdfTextString(string value, SerializationModeEnum serializationMode = SerializationModeEnum.Literal)
            : base(value, serializationMode)
        { }

        public override PdfObject Accept(IVisitor visitor, PdfName parentKey, object data) => visitor.Visit(this, parentKey, data);

        public override Memory<byte> RawValue
        {
            protected set
            {
                unicoded = value.Length >= 2 && value.Span[0] == 254 && value.Span[1] == 255;
                base.RawValue = value;
            }
        }

        public override object Value
        {
            get
            {
                if (stringValue != null)
                    return stringValue;
                if (unicoded)//SerializationMode == SerializationModeEnum.Literal && 
                {
                    var valueBytes = RawValue;
                    return stringValue = Charset.UTF16BE.GetString(valueBytes.Span.Slice(2));
                }
                else
                    // FIXME: proper call to base.StringValue could NOT be done due to an unexpected Mono runtime SIGSEGV (TOO BAD).
                    //          return base.StringValue;
                    return (string)base.Value;
            }
            protected set
            {
                stringValue = (string)value;
                switch (SerializationMode)
                {
                    case SerializationModeEnum.Literal:
                        {

                            byte[] valueBytes = PdfDocEncoding.Get().Encode(stringValue);
                            if (valueBytes == null)
                            {
                                unicoded = true;
                                valueBytes = new byte[Charset.UTF16BE.GetByteCount(stringValue) + 2];
                                Charset.UTF16BE.GetBytes(stringValue, 0, stringValue.Length, valueBytes, 2);
                                // Prepending UTF marker...
                                valueBytes[0] = 254; valueBytes[1] = 255;
                            }
                            RawValue = valueBytes;
                        }
                        break;
                    case SerializationModeEnum.Hex:
                        base.Value = value;
                        break;
                }
            }
        }
    }
}