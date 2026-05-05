/*
  Copyright 2015 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfGlown.Util;

using System;

namespace PdfGlown.Objects
{
    /**
      <summary>PDF byte string object [PDF:1.7:3.8.1].</summary>
      <remarks>The byte string type is used for binary data represented as a series of 8-bit bytes,
      where each byte can be any value representable in 8 bits. This string may represent characters
      whose encoding is implicit, or may contain non-textual data; in any case, its information isn't
      intended to be human-readable.</remarks>
    */
    public sealed class PdfByteString : PdfString
    {
        public PdfByteString(Memory<byte> rawValue, SerializationModeEnum serializationMode = SerializationModeEnum.Hex)
            : base(rawValue, serializationMode)
        { }

        /**
          <param name="value">Hexadecimal representation of this byte string.</param>
        */
        public PdfByteString(string value, SerializationModeEnum serializationMode = SerializationModeEnum.Hex)
            : base(value, serializationMode)
        { }

        public override object Value => stringValue ??= ConvertUtils.ByteArrayToHex(RawValue.Span);
    }
}