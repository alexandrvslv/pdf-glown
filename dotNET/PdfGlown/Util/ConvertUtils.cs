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

using PdfGlown.Util.IO;

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace PdfGlown.Util
{
    /// <summary>Data convertion utility.</summary>
    /// <remarks>This class is a specialized adaptation from the original <a href="http://commons.apache.org/codec/">
    /// Apache Commons Codec</a> project, licensed under the <a href="http://www.apache.org/licenses/LICENSE-2.0">
    /// Apache License, Version 2.0</a>.</remarks>
    public static class ConvertUtils
    {
        private static readonly char[] HexDigits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
        public static readonly string HexAlphabet = "0123456789ABCDEF";

        public static int GetHex(int c)
        {
            if (c >= '0' && c <= '9')
                return (c - '0');
            else if (c >= 'A' && c <= 'F')
                return (c - 'A' + 10);
            else if (c >= 'a' && c <= 'f')
                return (c - 'a' + 10);
            else
                return -1;
        }

        public static readonly int[] HexValue = new int[] {
            0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F };

        public static string ByteArrayToHex(ReadOnlySpan<byte> data)
        {
            int dataLength = data.Length;
            char[] result = new char[dataLength * 2];
            for (int dataIndex = 0, resultIndex = 0; dataIndex < dataLength; dataIndex++)
            {
                result[resultIndex++] = HexDigits[(0xF0 & data[dataIndex]) >> 4];
                result[resultIndex++] = HexDigits[0x0F & data[dataIndex]];
            }
            return new string(result);
        }

        public static byte[] HexToByteArray(ReadOnlySpan<char> data)
        {
            byte[] result;
            {
                int dataLength = data.Length;
                if ((dataLength % 2) != 0)
                    throw new Exception("Odd number of characters.");

                result = new byte[dataLength / 2];
                for (int resultIndex = 0, dataIndex = 0; dataIndex < dataLength; resultIndex++, dataIndex += 2)
                {
                    result[resultIndex] = byte.Parse(data.Slice(dataIndex, 2), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo);
                }
            }
            return result;
        }

        //https://stackoverflow.com/a/5919521/4682355
        public static string ByteArrayToHexString(ReadOnlySpan<byte> bytes)
        {
            var Result = new StringBuilder(bytes.Length * 2);

            foreach (byte B in bytes)
            {
                Result.Append(HexAlphabet[B >> 4]);
                Result.Append(HexAlphabet[B & 0xF]);
            }

            return Result.ToString();
        }

        public static byte[] HexStringToByteArray(string Hex)
        {
            byte[] Bytes = new byte[Hex.Length / 2];


            for (int x = 0, i = 0; i < Hex.Length; i += 2, x += 1)
            {
                Bytes[x] = ReadHexByte(Hex[i + 0], Hex[i + 1]);
            }

            return Bytes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ReadHexByte(char c1, char c2)
        {
            return (byte)(HexValue[Char.ToUpper(c1) - '0'] << 4 |
                          HexValue[Char.ToUpper(c2) - '0']);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ReadHexByte(Span<byte> span)
        {
            return (byte)(HexValue[Char.ToUpper((char)span[0]) - '0'] << 4 |
                          HexValue[Char.ToUpper((char)span[1]) - '0']);
        }

        //public static int ReadInt(this byte[] data, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
        //{
        //    return data.Length switch
        //    {
        //        1 => data[0],
        //        2 => ReadUInt16(data.AsSpan(), byteOrder),
        //        4 => ReadUInt32(data.AsSpan(), byteOrder),
        //        8 => (int)ReadUInt64(data.AsSpan(), byteOrder),
        //        _ => ReadIntByLength(data.AsSpan(), byteOrder),
        //    };
        //}

        //public static int ReadInt(Span<byte> data, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
        //{
        //    return data.Length switch
        //    {
        //        1 => data[0],
        //        2 => ReadUInt16(data, byteOrder),
        //        4 => ReadUInt32(data, byteOrder),
        //        8 => (int)ReadUInt64(data, byteOrder),
        //        _ => ReadIntByLength(data, byteOrder),
        //    };
        //}

        public static int ParseAsIntInvariant(string value) => (int)ParseFloatInvariant(value);

        public static int ParseAsIntInvariant(ReadOnlySpan<char> value) => (int)ParseFloatInvariant(value);

        public static double ParseDoubleInvariant(string value) => Double.Parse(value, NumberStyles.Float, NumberFormatInfo.InvariantInfo);

        public static double ParseDoubleInvariant(ReadOnlySpan<char> value) => Double.Parse(value, NumberStyles.Float, NumberFormatInfo.InvariantInfo);

        public static float ParseFloatInvariant(string value) => Single.Parse(value, NumberStyles.Float, NumberFormatInfo.InvariantInfo);

        public static float ParseFloatInvariant(ReadOnlySpan<char> value) => Single.Parse(value, NumberStyles.Float, NumberFormatInfo.InvariantInfo);

        public static int ParseIntInvariant(string value) => Int32.Parse(value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo);

        public static int ParseIntInvariant(ReadOnlySpan<char> value) => Int32.Parse(value, NumberStyles.Integer, NumberFormatInfo.InvariantInfo);

        public static float[] ToFloatArray(double[] array)
        {
            float[] result = new float[array.Length];
            for (int index = 0, length = array.Length; index < length; index++)
            { result[index] = (float)array[index]; }
            return result;
        }
    }
}