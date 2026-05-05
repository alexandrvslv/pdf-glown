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

using PdfGlown.Tokens;

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace PdfGlown.Bytes
{
    /// <summary>Byte buffer.</summary>
    public class ByteStream : Stream, IByteStream
    {
        public static ByteStream Empty => new ByteStream(Array.Empty<byte>());
        /// <summary>Default buffer capacity.</summary>
        private const int DefaultCapacity = 1 << 8;

        /// <summary>Inner buffer where data are stored.</summary>
        private ArraySegment<byte> data;

        /// <summary>Number of bytes actually used in the buffer.</summary>
        private int length;

        /// <summary>Pointer position within the buffer.</summary>
        private int position = 0;
        private ByteOrderEnum byteOrder = ByteOrderEnum.BigEndian;
        private bool dirty;
        private long mark;
        private int bitShift = -1;
        private byte currentByte;

        public ByteStream() : this(DefaultCapacity)
        { }

        public ByteStream(int capacity)
        {
            if (capacity < 1)
            {
                data = Array.Empty<byte>();
            }
            else
            {
                data = new byte[capacity];
            }
        }

        public ByteStream(Memory<byte> data)
        {
            SetBuffer(data);
        }

        public ByteStream(byte[] data, int start, int end) : this(data.AsMemory(start, end - start))
        { }

        public ByteStream(Stream data) : this((int)data.Length)
        {
            this.Write(data);
        }

        public ByteStream(IInputStream data) : this((int)data.Length)
        {
            this.Write(data);
        }

        public ByteStream(IDataWrapper data) : this(data.AsMemory())
        { }

        public ByteStream(string data) : this(data.Length)
        {
            this.Write(data);
        }

        public ByteStream(IInputStream data, int begin, int len)
        {
            data.Seek(begin);
            SetBuffer(data.ReadMemory(len));
        }

        public long Available { get => length - position; }

        public bool IsAvailable => length > position;

        public override long Length
        {
            get => length;
        }

        public int Capacity => data.Count;

        public bool Dirty
        {
            get => dirty;
            set => dirty = value;
        }

        public ByteOrderEnum ByteOrder
        {
            get => byteOrder;
            set => byteOrder = value;
        }

        /* int GetHashCode() uses inherited implementation. */
        public override long Position
        {
            get => position;
            set => position = (int)value;
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public IByteStream Append(byte data)
        {
            EnsureCapacity(1);
            this.data[length++] = data;
            NotifyChange();
            return this;
        }

        public IByteStream Append(byte[] data) => Append(data.AsSpan(0, data.Length));

        public IByteStream Append(byte[] data, int offset, int length) => Append(data.AsSpan(offset, length));

        public IByteStream Append(ReadOnlySpan<byte> data)
        {
            EnsureCapacity(data.Length);
            data.CopyTo(AsSpan(length, data.Length));
            length += data.Length;
            NotifyChange();
            return this;
        }

        public IByteStream Clone()
        {
            var clone = new ByteStream(length);
            clone.Append(AsSpan(0, length));
            return clone;
        }

        public void Delete(int index, int length)
        {
            var leftLength = this.length - (index + length);
            // Shift left the trailing data block to override the deleted data!
            //Array.Copy(data, index + length, this.data, index, leftLength);

            AsSpan(index + length, leftLength).CopyTo(AsSpan(index, leftLength));
            this.length -= length;
            NotifyChange();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetByte(int index) => data[index];

        public byte[] GetByteArray(int index, int length)
        {
            if ((index + length) > this.length)
            {
                length = this.length - index;
            }
            byte[] data = new byte[length];
            AsSpan(index, length).CopyTo(data.AsSpan());
            return data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> AsSpan(int index, int length) => data.AsSpan(index, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> AsSpan(int index) => AsSpan(index, length - index);

        public void Insert(int index, byte[] data) => Insert(index, data, 0, data.Length);

        public void Insert(int index, byte[] data, int offset, int length) => Insert(index, data.AsSpan(offset, length));

        public void Insert(int index, ReadOnlySpan<byte> data)
        {
            EnsureCapacity(length);
            var leftLength = this.length - index;
            // Shift right the existing data block to make room for new data!
            //Array.Copy(this.data, index, this.data, index + length, leftLength);
            AsSpan(index, leftLength).CopyTo(AsSpan(index + length, leftLength));

            // Insert additional data!
            //Array.Copy(data, offset, this.data, index, length);
            data.CopyTo(AsSpan(index, length));
            this.length += length;
            NotifyChange();
        }

        public void Insert(int index, string data) => Insert(index, BaseEncoding.Pdf.Encode(data));

        public void Insert(int index, IInputStream data) => Insert(index, data.AsMemory().Span);

        public void Replace(int index, byte[] data) => Replace(index, data, 0, data.Length);

        public void Replace(int index, byte[] data, int offset, int length) => Replace(index, data.AsSpan(offset, length));

        public void Replace(int index, ReadOnlySpan<byte> data)
        {
            //Array.Copy(data, offset, this.data, index, data.Length);
            data.CopyTo(AsSpan(index, data.Length));
            NotifyChange();
        }

        public void Replace(int index, string data) => Replace(index, BaseEncoding.Pdf.Encode(data));

        public void Replace(int index, IInputStream data) => Replace(index, data.AsMemory().Span);

        public override void SetLength(long value) => SetLength((int)value);

        public void SetLength(int value)
        {
            if (length != value)
            {
                if (Capacity < value)
                {
                    EnsureCapacity(value - Capacity);
                }
                length = value;
                if (position > length)
                    position = length;
                NotifyChange();
            }
        }

        public void WriteTo(IOutputStream stream) => stream.Write(AsSpan(0, length));

        public int Read(byte[] data) => Read(data, 0, data.Length);

        public override int Read(byte[] data, int offset, int length) => Read(data.AsSpan(offset, length));

        public override int Read(Span<byte> data)
        {
            var length = data.Length;
            if (position + length > Length)
            {
                length = (int)(Length - position);
            }
            AsSpan(position, length).CopyTo(data);
            position += length;
            return length;
        }

        public byte[] ReadNullTermitaded()
        {
            var start = position;
            var length = 0;
            while (ReadByte() > 0) { length++; }
            return GetByteArray(start, length);
        }

        public Span<byte> ReadNullTermitadedSpan()
        {
            var start = position;
            var length = 0;
            while (ReadByte() > 0) { length++; }
            return AsSpan(start, length);
        }

        public Span<byte> ReadSpan(int length)
        {
            if (position + length > this.length)
            {
                length = this.length - position;
            }
            var start = position;
            position += length;
            return AsSpan(start, length);
        }

        public Memory<byte> ReadMemory(int length)
        {
            if (position + length > this.length)
            {
                length = this.length - position;
            }
            var start = position;
            position += length;
            return data.Slice(start, length);
        }

        public override int ReadByte()
        {
            if (position >= length)
                return -1;

            return GetByte(position++);
        }        

        public int PeekByte()
        {
            if (position >= length)
                return -1;
            return GetByte(position);
        }

        public byte PeekUByte(int offset)
        {
            if (position + offset >= length)
                throw new EndOfStreamException();
            return GetByte(position + offset);
        }

        public short ReadInt16()
        {
            short value = StreamExtensions.ReadInt16(AsSpan(position, sizeof(short)), byteOrder);
            position += sizeof(short);
            return value;
        }

        public ushort ReadUInt16()
        {
            ushort value = StreamExtensions.ReadUInt16(AsSpan(position, sizeof(ushort)), byteOrder);
            position += sizeof(ushort);
            return value;
        }

        public int ReadInt32()
        {
            int value = StreamExtensions.ReadInt32(AsSpan(position, sizeof(int)), byteOrder);
            position += sizeof(int);
            return value;
        }

        public uint ReadUInt32()
        {
            var value = StreamExtensions.ReadUInt32(AsSpan(position, sizeof(uint)), byteOrder);
            position += sizeof(uint);
            return value;
        }

        public int ReadInt(int length)
        {
            int value = StreamExtensions.ReadIntOffset(AsSpan(position, length), byteOrder);
            position += length;
            return value;
        }

        public long ReadInt64()
        {
            var value = StreamExtensions.ReadInt64(AsSpan(position, sizeof(long)), byteOrder);
            position += sizeof(long);
            return value;
        }

        public ulong ReadUInt64()
        {
            var value = StreamExtensions.ReadUInt64(AsSpan(position, sizeof(ulong)), byteOrder);
            position += sizeof(ulong);
            return value;
        }

        public string ReadLine()
        {
            if (position >= length)
                throw new EndOfStreamException();

            var buffer = new StringBuilder();
            while (position < length)
            {
                int c = GetByte(position++);
                if (c == '\r'
                  || c == '\n')
                    break;

                buffer.Append((char)c);
            }
            return buffer.ToString();
        }

        public void ByteAlign()
        {
            this.bitShift = -1;
        }

        public int ReadBit()
        {
            if (bitShift < 0)
            {
                currentByte = this.ReadUByte();
                bitShift = 7;
            }
            var bit = (currentByte >> bitShift) & 1;
            bitShift--;
            return bit;
        }

        public uint ReadBits(int count)
        {
            var result = (uint)0;
            for (int i = count - 1; i >= 0; i--)
            {
                result |= (uint)(ReadBit() << i);
            }
            return result;
        }

        public long Seek(long position)
        {
            if (position < 0)
            { position = 0; }
            else if (position > length)
            { position = length; }

            return this.position = (int)position;
        }

        public long Skip(long offset) => Seek(position + offset);

        public int Mark() => (int)(mark = position);

        public int Mark(long position) => (int)(mark = this.position + position);

        public void ResetMark() => Seek(mark);

        public byte[] ToArray() => data.Slice(0, length).ToArray();

        public Memory<byte> AsMemory() => data.Slice(0, length);

        public Span<byte> AsSpan() => data.AsSpan(0, length);

        public byte[] GetArrayBuffer() => data.Array;

        public void SetBuffer(Memory<byte> data)
        {
            this.data = Unsafe.As<Memory<byte>, ArraySegment<byte>>(ref data);
            length = data.Length;
            position = 0;
        }

        public void Clear() => SetLength(0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void WriteByte(byte data) => Append(data);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(int data, int length)
        {
            Span<byte> result = stackalloc byte[length];
            StreamExtensions.WriteIntOffset(result, data, byteOrder);
            Write(result);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(byte[] data) => Append(data);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Write(ReadOnlySpan<byte> data) => Append(data);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Write(byte[] data, int offset, int length) => Append(data, offset, length);

        public int WriteBits(long data, int count)
        {
            throw new NotImplementedException();
        }

        /// <summary>Check whether the buffer has sufficient room for
        /// adding data.</summary>
        private void EnsureCapacity(int additionalLength)
        {
            int minCapacity = length + additionalLength;
            // Is additional data within the buffer capacity?
            if (minCapacity <= data.Count)
                return;

            // Additional data exceed buffer capacity.
            // Reallocate the buffer!
            var newBuffer = new byte[Math.Max(data.Count << 1, minCapacity)];
            AsSpan().CopyTo(newBuffer);
            data = newBuffer;
        }

        private void NotifyChange()
        {
            dirty = true;
        }

        public override void Flush() { }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return origin switch
            {
                SeekOrigin.Current => Skip(offset),
                SeekOrigin.End => Seek(Length - offset),
                _ => Seek(offset),
            };
        }

    }


}