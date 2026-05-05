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

using System;

namespace PdfGlown.Util
{
    public class StringStream : BuffedStream<char>
    {
        public StringStream()
        {
        }

        public StringStream(int capacity) : base(capacity)
        {
        }

        public StringStream AppendNumber(int intValue)
        {
            var lenth = intValue == 0 ? 1 : (intValue > 0 ? 1 : 2) + (int)System.Math.Log10(System.Math.Abs((double)intValue));
            Span<char> chars = stackalloc char[lenth];
            intValue.TryFormat(chars, out var written);
            Append(chars.Slice(0, written));
            return this;
        }

        public StringStream AppendNumber(uint intValue)
        {
            var lenth = intValue == 0 ? 1 : 1 + (int)System.Math.Log10(System.Math.Abs((double)intValue));
            Span<char> chars = stackalloc char[lenth];
            intValue.TryFormat(chars, out var written);
            Append(chars.Slice(0, written));
            return this;
        }

        public override string ToString() => new string(ToArray());
    }
}