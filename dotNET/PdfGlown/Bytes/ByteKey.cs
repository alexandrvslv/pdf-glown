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
using System.Text;

namespace PdfGlown.Bytes
{
    /// <summary>Byte array.</summary>
    // NOTE: This class is useful when applied as key for dictionaries using the default IEqualityComparer.
    public readonly struct ByteKey : IComparable<ByteKey>, IEquatable<ByteKey>
    {
        public static implicit operator int(ByteKey value) => value.Data;

        public readonly int Data;

        public int Length
        {
            get => Data > 255 ? 2 : 1;
        }

        public ByteKey(int b0, int b1) : this(b0 << 8 | b1)
        { }

        public ByteKey(int data)
        {
            Data = data;
        }
        //TODO Check remove copy{ Array.Copy(data, this.Data = new byte[data.Length], data.Length); }

        public int CompareTo(ByteKey other) => Data.CompareTo(other.Data);

        public override bool Equals(object obj) => obj is ByteKey other
              && Equals(other);

        public bool Equals(ByteKey other) => Data.Equals(other.Data);

        public override int GetHashCode() => Data.GetHashCode();

        public override string ToString()
        {
            var builder = new StringBuilder('[');
            var bytes = ToArray();
            foreach (byte datum in bytes)
            {
                if (builder.Length > 1)
                { builder.Append(','); }

                builder.Append(datum & 0xFF);
            }
            builder.Append(']');
            return builder.ToString();
        }

        public byte[] ToArray()
        {
            return StreamExtensions.WriteIntOffset(Data, Length);
        }
    }
}