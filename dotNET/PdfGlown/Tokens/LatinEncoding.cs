/*
  Copyright 2012 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)

  This file should be part of the source code distribution of "PDF Glown library"
  (the Program): see the accompanying README files for more info.

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

namespace PdfGlown.Tokens
{
    /// <summary>Adobe standard Latin character set [PDF:1.7:D].</summary>
    public class LatinEncoding : BaseEncoding
    {
        /// <summary>Code-to-Unicode map.</summary>
        protected BiDictionary<int, char> chars;

        public override string Decode(byte[] value) => Decode(value, 0, value.Length);

        public override string Decode(byte[] value, int index, int length)
        {
            char[] stringChars = new char[length];
            for (int decodeIndex = index, decodeLength = length + index; decodeIndex < decodeLength; decodeIndex++)
            { stringChars[decodeIndex - index] = chars[value[decodeIndex] & 0xff]; }
            return new string(stringChars);
        }

        public override string Decode(ReadOnlySpan<byte> value)
        {
            var index = 0;
            char[] stringChars = new char[value.Length];
            for (int decodeIndex = index, decodeLength = value.Length + index; decodeIndex < decodeLength; decodeIndex++)
            { stringChars[decodeIndex - index] = chars[value[decodeIndex] & 0xff]; }
            return new string(stringChars);
        }

        public override byte[] Encode(string value)
        {
            byte[] stringBytes = new byte[value.Length];
            for (int index = 0, length = value.Length; index < length; index++)
            {
                int code = chars.GetKey(value[index]);
                if (code == 0) //TODO: verify whether 0 collides with valid code values.
                    return null;

                stringBytes[index] = (byte)code;
            }
            return stringBytes;
        }

        public override byte[] Encode(char[] value)
        {
            byte[] stringBytes = new byte[value.Length];
            for (int index = 0, length = value.Length; index < length; index++)
            {
                int code = chars.GetKey(value[index]);
                if (code == 0) //TODO: verify whether 0 collides with valid code values.
                    return null;

                stringBytes[index] = (byte)code;
            }
            return stringBytes;
        }

        public override void Encode(ReadOnlySpan<char> value, Span<byte> stringBytes)
        {
            for (int index = 0, length = value.Length; index < length; index++)
            {
                int code = chars.GetKey(value[index]);
                if (code == 0) //TODO: verify whether 0 collides with valid code values.
                    throw new Exception();

                stringBytes[index] = (byte)code;
            }
        }
    }
}