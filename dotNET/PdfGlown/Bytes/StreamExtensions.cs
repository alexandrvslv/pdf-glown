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

using PdfGlown.Bytes;
using PdfGlown.Tokens;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace PdfGlown.Bytes
{
    public static class StreamExtensions
    {
        private const int BufferSize = 4 * 1024;
        private const int MaxStackAllockSize = 256;

        public static void Reset(this MemoryStream stream)
        {
            stream.SetLength(0);
        }

        public static void CopyTo(this IInputStream target, IOutputStream output) => target.CopyTo((Stream)output);

        public static void CopyTo(this IInputStream target, IOutputStream output, int bufferSize) => target.CopyTo((Stream)output, bufferSize);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32(this byte[] data, int index = 0, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
        {
            var buffer = data.AsSpan(index, sizeof(int));
            return ReadInt32(buffer, byteOrder);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32(this ReadOnlySpan<byte> buffer, int offset, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
           => ReadInt32(buffer.Slice(offset, sizeof(int)), byteOrder);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt32(ReadOnlySpan<byte> buffer, ByteOrderEnum byteOrder)
        {
            return byteOrder == ByteOrderEnum.BigEndian
                ? BinaryPrimitives.ReadInt32BigEndian(buffer)
                : BinaryPrimitives.ReadInt32LittleEndian(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt32(Span<byte> buffer, int value, ByteOrderEnum byteOrder)
        {
            if (byteOrder == ByteOrderEnum.BigEndian)
                BinaryPrimitives.WriteInt32BigEndian(buffer, value);
            else
                BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUInt32(this byte[] data, int index = 0, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
        {
            var buffer = data.AsSpan(index, sizeof(uint));
            return ReadUInt32(buffer, byteOrder);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUInt32(this ReadOnlySpan<byte> buffer, int offset, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
           => ReadUInt32(buffer.Slice(offset, sizeof(uint)), byteOrder);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUInt32(ReadOnlySpan<byte> buffer, ByteOrderEnum byteOrder)
        {
            return byteOrder == ByteOrderEnum.BigEndian
                ? BinaryPrimitives.ReadUInt32BigEndian(buffer)
                : BinaryPrimitives.ReadUInt32LittleEndian(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt32(Span<byte> buffer, uint value, ByteOrderEnum byteOrder)
        {
            if (byteOrder == ByteOrderEnum.BigEndian)
                BinaryPrimitives.WriteUInt32BigEndian(buffer, value);
            else
                BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadInt16(this byte[] data, int index = 0, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
        {
            var buffer = data.AsSpan(index, sizeof(short));
            return ReadInt16(buffer, byteOrder);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadInt16(this ReadOnlySpan<byte> buffer, int offset, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
            => ReadInt16(buffer.Slice(offset, sizeof(short)), byteOrder);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadInt16(ReadOnlySpan<byte> buffer, ByteOrderEnum byteOrder)
        {
            return byteOrder == ByteOrderEnum.BigEndian
                ? BinaryPrimitives.ReadInt16BigEndian(buffer)
                : BinaryPrimitives.ReadInt16LittleEndian(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt16(Span<byte> buffer, short value, ByteOrderEnum byteOrder)
        {
            if (byteOrder == ByteOrderEnum.BigEndian)
                BinaryPrimitives.WriteInt16BigEndian(buffer, value);
            else
                BinaryPrimitives.WriteInt16LittleEndian(buffer, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUInt16(this byte[] data, int index = 0, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
        {
            var buffer = data.AsSpan(index, sizeof(ushort));
            return ReadUInt16(buffer, byteOrder);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUInt16(this ReadOnlySpan<byte> buffer, int offset, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
            => ReadUInt16(buffer.Slice(offset, sizeof(ushort)), byteOrder);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUInt16(ReadOnlySpan<byte> buffer, ByteOrderEnum byteOrder)
        {
            return byteOrder == ByteOrderEnum.BigEndian
                ? BinaryPrimitives.ReadUInt16BigEndian(buffer)
                : BinaryPrimitives.ReadUInt16LittleEndian(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt16(Span<byte> buffer, ushort value, ByteOrderEnum byteOrder)
        {
            if (byteOrder == ByteOrderEnum.BigEndian)
                BinaryPrimitives.WriteUInt16BigEndian(buffer, value);
            else
                BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadInt64(this byte[] data, int position = 0, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
        {
            var buffer = data.AsSpan(position, sizeof(long));
            return ReadInt64(buffer, byteOrder);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadInt64(ReadOnlySpan<byte> buffer, ByteOrderEnum byteOrder)
        {
            return byteOrder == ByteOrderEnum.BigEndian
                ? BinaryPrimitives.ReadInt64BigEndian(buffer)
                : BinaryPrimitives.ReadInt64LittleEndian(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt64(Span<byte> buffer, long value, ByteOrderEnum byteOrder)
        {
            if (byteOrder == ByteOrderEnum.BigEndian)
                BinaryPrimitives.WriteInt64BigEndian(buffer, value);
            else
                BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReadUint64(this byte[] data, int position = 0, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
        {
            var buffer = data.AsSpan(position, sizeof(ulong));
            return ReadUInt64(buffer, byteOrder);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReadUInt64(Span<byte> buffer, ByteOrderEnum byteOrder)
        {
            return byteOrder == ByteOrderEnum.BigEndian
                ? BinaryPrimitives.ReadUInt64BigEndian(buffer)
                : BinaryPrimitives.ReadUInt64LittleEndian(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt64(Span<byte> buffer, ulong value, ByteOrderEnum byteOrder)
        {
            if (byteOrder == ByteOrderEnum.BigEndian)
                BinaryPrimitives.WriteUInt64BigEndian(buffer, value);
            else
                BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
        }



        public static byte[] IntToByteArray(int data, bool compact = false)
        {
            if (compact)
            {
                if (data < 1 << 8)
                {
                    return new byte[] { (byte)data };
                }
                else if (data < 1 << 16)
                {
                    return new byte[] { (byte)(data >> 8), (byte)data };
                }
                else if (data < 1 << 24)
                {
                    return new byte[] { (byte)(data >> 16), (byte)(data >> 8), (byte)data };
                }
            }
            return new byte[] { (byte)(data >> 24), (byte)(data >> 16), (byte)(data >> 8), (byte)data };
        }

        public static int ReadIntOffset(byte[] data, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian) => ReadIntOffset(data, 0, data.Length, byteOrder);

        public static int ReadIntOffset(byte[] data, int index, int length, ByteOrderEnum byteOrder)
        {
            int value = 0;
            length = (int)System.Math.Min(length, data.Length - index);
            for (int i = index, endIndex = index + length; i < endIndex; i++)
            { value |= (data[i] & 0xff) << 8 * (byteOrder == ByteOrderEnum.LittleEndian ? i - index : endIndex - i - 1); }
            return value;
        }

        public static int ReadIntOffset(this ReadOnlySpan<byte> data, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
        {
            int value = 0;
            var length = data.Length;
            for (int i = 0, endIndex = length; i < endIndex; i++)
            { value |= (data[i] & 0xff) << 8 * (byteOrder == ByteOrderEnum.LittleEndian ? i : endIndex - i - 1); }
            return value;
        }

        //public static void WriteInt(Span<byte> result, int data, ByteOrderEnum byteOrder)
        //{
        //    switch (result.Length)
        //    {
        //        case 1: result[0] = (byte)data; break;
        //        case 2: WriteUInt16(result, (ushort)data, byteOrder); break;
        //        case 4: WriteInt32(result, data, byteOrder); break;
        //        case 8: WriteInt64(result, (long)data, byteOrder); break;
        //        default: WriteIntByLength(result, data, byteOrder); break;
        //    }
        //}

        public static byte[] WriteIntOffset(int data, int length, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
        {
            byte[] result = new byte[length];
            for (int index = 0; index < length; index++)
            { result[index] = (byte)(data >> 8 * (byteOrder == ByteOrderEnum.LittleEndian ? index : length - index - 1)); }
            return result;
        }

        public static void WriteIntOffset(Span<byte> result, int data, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
        {
            var length = result.Length;
            for (int index = 0; index < length; index++)
            { result[index] = (byte)(data >> 8 * (byteOrder == ByteOrderEnum.LittleEndian ? index : length - index - 1)); }
        }

        /// <summary>Reads a signed byte integer.</summary>
        /// <remarks>This operation causes the stream pointer to advance after the read data.</remarks>
        public static sbyte ReadSByte(this IInputStream target) => unchecked((sbyte)target.ReadByte());

        /// <summary>Reads a unsigned byte integer.</summary>
        /// <remarks>This operation causes the stream pointer to advance after the read data.</remarks>
        public static byte ReadUByte(this IInputStream target) => unchecked((byte)target.ReadByte());

        public static byte ReadUByteWithThrow(this IInputStream target)
        {
            if (target.Position >= target.Length)
                throw new EndOfStreamException();

            return (byte)target.ReadByte();
        }

        public static sbyte ReadSByteWithThrow(this IInputStream target)
        {
            if (target.Position >= target.Length)
                throw new EndOfStreamException();

            return unchecked((sbyte)target.ReadByte());
        }

        /// <summary>Reads a string.</summary>
        /// <remarks>This operation causes the stream pointer to advance after the read data.</remarks>
        /// <param name="length">Number of bytes to read.</param>
        public static string ReadString(this IInputStream target, int length) => target.ReadString(length, Charset.ISO88591);

        public static string ReadString(this IInputStream target, int length, Encoding encoding)
        {
            var span = target.ReadSpan(length);
            return encoding.GetString(span);
        }

        public static string ReadString(this IInputStream target, int position, int length)
        {
            var temp = target.Position;
            target.Position = position;
            var result = target.ReadString(length);
            target.Position = temp;
            return result;
        }

        public static ReadOnlySpan<char> ReadROS(this IInputStream target, int length, System.Text.Encoding encoding)
        {
            Span<byte> bytes = target.ReadSpan(length);
            Span<char> chars = new char[encoding.GetCharCount(bytes)];
            encoding.GetChars(bytes, chars);
            return chars;
        }

        public static float ReadFixed32(this IInputStream target)
        {
            return target.ReadInt16() // Signed Fixed-point mantissa (16 bits).
               + target.ReadUInt16() / 16384f; // Fixed-point fraction (16 bits).
        }

        public static float ReadUnsignedFixed32(this IInputStream target)
        {
            return target.ReadUInt16() // Fixed-point mantissa (16 bits).
               + target.ReadUInt16() / 16384f; // Fixed-point fraction (16 bits).
        }

        public static float ReadFixed16(this IInputStream target)
        {
            return target.ReadSByte() // Fixed-point mantissa (8 bits).
               + target.ReadByte() / 64f; // Fixed-point fraction (8 bits).
        }

        public static float ReadUnsignedFixed16(this IInputStream target)
        {
            return (byte)target.ReadByte() // Fixed-point mantissa (8 bits).
               + target.ReadByte() / 64f; // Fixed-point fraction (8 bits).
        }

        public static float Read32Fixed(this IInputStream target)
        {
            float retval = 0;
            retval = target.ReadInt16();
            retval += (target.ReadUInt16() / 65536.0F);
            return retval;
        }

        public static string ReadTag(this IInputStream target)
        {
            Span<byte> buffer = stackalloc byte[4];
            target.Read(buffer);
            return Charset.ASCII.GetString(buffer);
        }

        public static DateTime ReadInternationalDate(this IInputStream target)
        {
            try
            {
                var secondsSince1904 = target.ReadInt64();
                var cal = new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                return cal + TimeSpan.FromSeconds(secondsSince1904);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"error: ReadInternationalDate {ex} ");
                return DateTime.UtcNow;
            }
        }

        public static int Read(this IInputStream target, sbyte[] data) => target.Read(data, 0, data.Length);

        public static int Read(this IInputStream target, sbyte[] data, int offset, int length)
        {
            return target.Read((byte[])(Array)data, offset, length);
        }

        public static ushort[] ReadUShortArray(this IInputStream input, int length)
        {
            var array = new ushort[length];
            for (int i = 0; i < length; i++)
            {
                array[i] = input.ReadUInt16();
            }
            return array;
        }

        public static short[] ReadSShortArray(this IInputStream input, int length)
        {
            var array = new short[length];
            for (int i = 0; i < length; i++)
            {
                array[i] = input.ReadInt16();
            }
            return array;
        }

        /**
		 * Read the offset from the buffer.
		 * @param offSize the given offsize
		 * @return the offset
		 * @throws IOException if an error occurs during reading
		 */
        public static int ReadOffset(this IInputStream input, int offSize)
        {
            Span<byte> bytes = stackalloc byte[offSize];
            input.Read(bytes);
            int value = 0;
            for (int i = 0; i < offSize; i++)
            {
                value = value << 8 | bytes[i];
            }
            return value;
        }

        public static void Write(this IOutputStream target, IInputStream data)
        {
            byte[] baseData = ArrayPool<byte>.Shared.Rent(BufferSize);
            data.Seek(0);
            int count;
            while ((count = data.Read(baseData, 0, baseData.Length)) > 0)
            {
                target.Write(baseData, 0, count);
            }
            ArrayPool<byte>.Shared.Return(baseData);
        }

        public static void Write(this IOutputStream target, Stream data)
        {
            byte[] baseData = ArrayPool<byte>.Shared.Rent(BufferSize);
            data.Position = 0;
            int count;
            while ((count = data.Read(baseData, 0, baseData.Length)) > 0)
            {
                target.Write(baseData, 0, count);
            }
            ArrayPool<byte>.Shared.Return(baseData);
        }

        public static void Write(this IOutputStream target, short value, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
        {
            Span<byte> buffer = stackalloc byte[sizeof(short)];
            WriteInt16(buffer, value, byteOrder);
            target.Write(buffer);
        }

        public static void Write(this IOutputStream target, ushort value, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ushort)];
            WriteUInt16(buffer, value, byteOrder);
            target.Write(buffer);
        }

        public static void Write(this IOutputStream target, int value, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            WriteInt32(buffer, value, byteOrder);
            target.Write(buffer);
        }

        public static void Write(this IOutputStream target, uint value, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
        {
            Span<byte> buffer = stackalloc byte[sizeof(uint)];
            WriteUInt32(buffer, value, byteOrder);
            target.Write(buffer);
        }

        public static void Write(this IOutputStream target, long value, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
        {
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            WriteInt64(buffer, value, byteOrder);
            target.Write(buffer);
        }

        public static void Write(this IOutputStream target, ulong value, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ulong)];
            WriteUInt64(buffer, value, byteOrder);
            target.Write(buffer);
        }

        public static void WriteFixed(this IOutputStream target, double f, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
        {
            double ip = Math.Floor(f);
            double fp = (f - ip) * 65536.0;
            target.Write((short)ip, byteOrder);
            target.Write((short)fp, byteOrder);
        }

        public static void WriteLongDateTime(this IOutputStream target, DateTime calendar, ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian)
        {
            // inverse operation of IInputStream.readInternationalDate()
            DateTime cal = new DateTime(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            long secondsSince1904 = (long)(calendar - cal).TotalSeconds;
            target.Write(secondsSince1904, byteOrder);
        }

        public static void WriteAsString(this IOutputStream target, int value) => target.WriteAsString(value, Charset.ISO88591);

        public static void WriteAsString(this IOutputStream target, int value, Encoding encoding)
        {
            Span<char> chars = stackalloc char[12];
            value.TryFormat(chars, out var lenth, ReadOnlySpan<char>.Empty, CultureInfo.InvariantCulture);
            target.Write(chars[..lenth], encoding);
        }

        public static void WriteAsString(this IOutputStream target, double value, string format, IFormatProvider provider) => target.WriteAsString(value, format, provider, Charset.ISO88591);

        public static void WriteAsString(this IOutputStream target, double value, string format, IFormatProvider provider, Encoding encoding)
        {
            Span<char> chars = stackalloc char[22];
            value.TryFormat(chars, out var lenth, format, provider);
            target.Write(chars[..lenth], encoding);
        }

        public static void Write(this IOutputStream target, ReadOnlySpan<char> data) => target.Write(data, Charset.ISO88591);

        public static void Write(this IOutputStream target, ReadOnlySpan<char> data, Encoding encoding)
        {
            var length = encoding.GetByteCount(data);
            Span<byte> buffer = length <= MaxStackAllockSize
                ? stackalloc byte[length]
                : new byte[length];
            encoding.GetBytes(data, buffer);
            target.Write(buffer);
        }

        public static Memory<byte> AsMemory(this Stream stream) => stream is MemoryStream memoryStream
            ? memoryStream.AsMemory()
            : stream is IDataWrapper dataWrapper
                ? dataWrapper.AsMemory()
                : throw new Exception("new BuffedStream(stream).GetMemoryBuffer()");

        public static Memory<byte> AsMemory(this IDataWrapper stream) => stream.AsMemory();

        public static Memory<byte> AsMemory(this MemoryStream stream) => new Memory<byte>(stream.GetBuffer(), 0, (int)stream.Length);

        public static Span<byte> AsSpan(this MemoryStream stream) => new Span<byte>(stream.GetBuffer(), 0, (int)stream.Length);

        /**
        * Determines if there are any bytes left to read or not. 
        * @return true if there are any bytes left to read
        */
        public static bool HasRemaining(this IInputStream input)
        {
            return input.Length > input.Position;
        }

        /**
		 * Peeks one single signed byte from the buffer.
		 * @return the signed byte as int
		 * @throws IOException if an error occurs during reading
		 */
        public static sbyte PeekSignedByte(this IInputStream input, int offset)
        {
            try
            {
                return unchecked((sbyte)input.PeekUByte(offset));
            }
            catch (Exception re)
            {
                Debug.WriteLine("debug: An error occurred peeking at offset " + offset + " - returning -1", re);
                throw new EndOfStreamException();
            }
        }
        
    }
}