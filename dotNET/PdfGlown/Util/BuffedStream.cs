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
using System.IO;
using System.Runtime.CompilerServices;

namespace PdfGlown.Util
{
    public class BuffedStream<T>
    {
        private const int DefaultCapacity = 1 << 3;

        private T[] data;
        private int length;
        private int position = 0;
        private int mark;
        private int capacity;

        public BuffedStream() : this(0)
        { }

        public BuffedStream(int capacity)
        {
            if (capacity < 1)
            { capacity = DefaultCapacity; }

            Capacity = capacity;
        }

        public BuffedStream(Memory<T> data)
        {
            SetBuffer(data);
        }

        public BuffedStream(T[] data, int start, int end)
        {
            //unsafe mode;
            this.data = data;
            this.position = start;
            this.length = end - start;
            this.capacity = data.Length;
        }

        public long Available { get => length - position; }

        public long Length
        {
            get => length;
        }

        public int Capacity
        {
            get => capacity;
            set
            {
                capacity = value;
                // Additional data exceed buffer capacity.
                // Reallocate the buffer!
                var newBuffer = new T[capacity];
                //Array.Copy(this.data, 0, data, 0, this.length);
                data.AsSpan().CopyTo(newBuffer);
                data = newBuffer;
            }
        }

        public int Position
        {
            get => position;
            set => position = value;
        }

        public void Append(T value)
        {
            EnsureCapacity();
            data[length++] = value;
        }

        public void Append(T[] beffer) => Append(beffer, 0, beffer.Length);

        public void Append(T[] buffer, int offset, int count)
        {
            EnsureCapacity(count);
            Array.Copy(buffer, offset, data, length, count);
            length += count;
        }

        public void Append(ReadOnlySpan<T> data)
        {
            EnsureCapacity(data.Length);
            data.CopyTo(AsSpan(length, data.Length));
            length += data.Length;
        }

        public BuffedStream<T> Clone()
        {
            var clone = new BuffedStream<T>(length);
            clone.Append(data);
            return clone;
        }

        public void Delete(int index, int length)
        {
            var leftLength = this.length - (index + length);
            // Shift left the trailing data block to override the deleted data!
            //Array.Copy(data, index + length, this.data, index, leftLength);

            AsSpan(index + length, leftLength).CopyTo(AsSpan(index, leftLength));
            this.length -= length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get(int index) => data[index];

        public T[] GetArray(int index, int length)
        {
            if ((index + length) > this.length)
            {
                length = this.length - index;
            }
            var data = new T[length];
            AsSpan(index, length).CopyTo(data.AsSpan());
            return data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan(int index, int length) => data.AsSpan(index, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan(int index) => data.AsSpan(index, length - index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan() => data.AsSpan(0, length);

        public void Insert(int index, T[] data) => Insert(index, data, 0, data.Length);

        public void Insert(int index, T[] data, int offset, int length) => Insert(index, data.AsSpan(offset, length));

        public void Insert(int index, ReadOnlySpan<T> data)
        {
            EnsureCapacity(length);
            var leftLength = length - index;
            // Shift right the existing data block to make room for new data!
            //Array.Copy(this.data, index, this.data, index + length, leftLength);
            AsSpan(index, leftLength).CopyTo(AsSpan(index + length, leftLength));

            // Insert additional data!
            //Array.Copy(data, offset, this.data, index, length);
            data.CopyTo(AsSpan(index, length));
            this.length += length;
        }

        public void Replace(int index, T[] data) => Replace(index, data, 0, data.Length);

        public void Replace(int index, T[] data, int offset, int length) => Replace(index, data.AsSpan(offset, length));

        public void Replace(int index, ReadOnlySpan<T> data)
        {
            //Array.Copy(data, offset, this.data, index, data.Length);
            data.CopyTo(AsSpan(index, data.Length));
        }

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
            }
        }

        public int Read(T[] buffer) => Read(buffer, 0, buffer.Length);

        public int Read(T[] buffer, int offset, int count)
        {
            if (position + count > length)
            {
                count = (int)(length - position);
            }
            Array.Copy(buffer, offset, data, position, count);
            position += count;
            return count;
        }

        public int Read(Span<T> span)
        {
            var count = span.Length;
            if (position + count > length)
            {
                count = (int)(length - position);
            }
            AsSpan(position, count).CopyTo(span);
            position += count;
            return count;
        }

        public Span<T> ReadSpan(int length)
        {
            if (position + length > Length)
            {
                length = (int)(Length - position);
            }
            var start = position;
            position += length;
            return AsSpan(start, length);
        }

        public T[] ReadBytes(int length)
        {
            if (position + length > Length)
            {
                length = (int)(Length - position);
            }
            var start = position;
            position += length;
            return GetArray(start, length);
        }

        public T Read()
        {
            if (position >= length)
                return default(T);

            return Get(position++);
        }

        public T Peek()
        {
            if (position >= length)
                return default(T);
            return Get(position);
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

        public long Seek(int offset, SeekOrigin origin)
        {
            return origin switch
            {
                SeekOrigin.Current => Skip(offset),
                SeekOrigin.End => Seek(Length - offset),
                _ => Position = offset,
            };
        }

        public int Mark() => mark = position;

        public void ResetMark() => Seek(mark);

        public void Reset() => SetLength(0);

        public T[] ToArray() => data.AsSpan(0, length).ToArray();

        public Memory<T> GetMemoryBuffer() => data.AsMemory(0, length);

        public T[] GetArrayBuffer() => data;

        public void SetBuffer(T[] data) => SetBuffer(data, data.Length);

        public void SetBuffer(T[] data, int newLength)
        {
            this.data = data;
            capacity = data.Length;
            length = newLength;
            position = 0;
        }

        public void SetBuffer(Memory<T> data)
        {
            var aSegment = Unsafe.As<Memory<T>, ArraySegment<T>>(ref data);
            SetBuffer(aSegment.Array, aSegment.Array.Length);
        }

        public void Clear() => SetLength(0);

        /// <summary>Check whether the buffer has sufficient room for
        /// adding data.</summary>
        private void EnsureCapacity(int additionalLength)
        {
            int minCapacity = length + additionalLength + 1;
            // Is additional data within the buffer capacity?
            if (minCapacity < capacity)
                return;
            Capacity = System.Math.Max(capacity << 1, minCapacity - 1);
        }

        private void EnsureCapacity()
        {
            int minCapacity = length + 2;
            // Is additional data within the buffer capacity?
            if (minCapacity < capacity)
                return;
            Capacity = System.Math.Max(capacity << 1, minCapacity - 1);
        }

    }
}