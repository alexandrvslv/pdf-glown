/*
  Copyright 2007-2015 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfGlown.Objects;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace PdfGlown.Documents.Contents
{
    /// <summary>Content stream wrapper.</summary>
    internal class ContentStream : Stream, IInputStream
    {
        private readonly PdfDirectObject dataObject;

        // Current stream base position (cumulative size of preceding streams).
        private long basePosition;
        private long? length;
        // Current stream.
        private IInputStream stream;
        // Current stream index.
        private int streamIndex = -1;
        private long mark;

        public ContentStream(PdfDirectObject dataObject)
        {
            this.dataObject = dataObject;
            MoveNextStream();
        }

        public ByteOrderEnum ByteOrder
        {
            get => GetStream().ByteOrder;
            set => throw new NotSupportedException();
        }

        public bool IsAvailable => Length > Position;

        public override long Length => length ??= GetLength();

        public override long Position
        {
            get => basePosition + (stream?.Position ?? 0);
            set => Seek(value);
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public bool Dirty
        {
            get => false;
            set { }
        }

        public int Read(byte[] data) => Read(data, 0, data.Length);

        public override int Read(byte[] data, int offset, int length)
        {
            int total = 0;
            while (length > 0 && EnsureStream())
            {
                int readLength = Math.Min(length, (int)(stream.Length - stream.Position));
                total += stream.Read(data, offset, readLength);
                offset += readLength;
                length -= readLength;
            }
            return total;
        }

        public override int ReadByte() => GetStream()?.ReadByte() ?? -1;

        public int PeekByte() => GetStream()?.PeekByte() ?? -1;

        public int ReadInt32() => GetStream().ReadInt32();

        public uint ReadUInt32() => GetStream().ReadUInt32();

        public int ReadInt(int length) => GetStream().ReadInt(length);

        public string ReadLine() => GetStream().ReadLine();

        public short ReadInt16() => GetStream().ReadInt16();

        public ushort ReadUInt16() => GetStream().ReadUInt16();

        public sbyte ReadSByte() => GetStream().ReadSByte();

        public long ReadInt64() => GetStream().ReadInt64();

        public ulong ReadUInt64() => GetStream().ReadUInt64();

        public string ReadString(int length)
        {
            var builder = new StringBuilder();
            while (length > 0 && EnsureStream())
            {
                int readLength = Math.Min(length, (int)(stream.Length - stream.Position));
                builder.Append(stream.ReadString(readLength));
                length -= readLength;
            }
            return builder.ToString();
        }

        public Span<byte> ReadSpan(int length)
        {
            return GetStream().ReadSpan(length);
        }

        public Memory<byte> ReadMemory(int length)
        {
            return GetStream().ReadMemory(length);
        }

        private long GetLength()
        {
            if (dataObject is PdfStream pdfStream) // Single stream.
                return pdfStream.GetInputStream().Length;
            else // Array of streams.
            {
                long length = 0;
                foreach (var reference in ((PdfArrayImpl)dataObject).OfType<PdfReference>())
                {
                    length += ((PdfStream)reference.Resolve(PdfName.Contents)).GetInputStream().Length;
                }
                return length;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin: Seek(offset); break;
                case SeekOrigin.Current: Seek(Position + offset); break;
                case SeekOrigin.End: Seek(Length - offset); break;
            }
            return Position;
        }

        public long Seek(long position)
        {
            if (position < 0)
                throw new ArgumentException("Negative positions cannot be sought.");

            while (true)
            {
                if (position < basePosition) //Before current stream.
                { MovePreviousStream(); }
                else if (position > basePosition + stream.Length) // After current stream.
                {
                    if (!MoveNextStream())
                        throw new EndOfStreamException();
                }
                else // At current stream.
                {
                    stream.Seek(position - basePosition);
                    break;
                }
            }
            return Position;
        }

        public long Skip(long offset)
        {
            var newPosition = Position + offset;
            Seek(newPosition);
            return newPosition;
        }

        public byte[] ToArray()
        {
            return GetStream()?.ToArray();
        }

        public Memory<byte> AsMemory()
        {
            return GetStream()?.AsMemory() ?? Memory<byte>.Empty;
        }

        public Span<byte> AsSpan()
        {
            return GetStream().AsSpan();
        }

        public byte[] GetArrayBuffer()
        {
            return GetStream()?.GetArrayBuffer();
        }

        public void SetBuffer(byte[] data)
        {
            GetStream().SetBuffer(data);
        }

        public void SetBuffer(Memory<byte> data)
        {
            GetStream().SetBuffer(data);
        }

        /// <summary>Ensures stream availability, moving to the next stream in case the current 
        /// one hasrun out of data.</summary>
        private bool EnsureStream()
        {
            return !(stream == null
                || !stream.IsAvailable)
                || MoveNextStream();
        }

        private IInputStream GetStream() => EnsureStream() ? stream : null;

        private bool MoveNextStream()
        {
            streamIndex++;
            basePosition = streamIndex == 0 ? 0 : basePosition + (stream?.Length ?? 0);
            // Is the content stream just a single stream?
            // NOTE: A content stream may be made up of multiple streams [PDF:1.6:3.6.2].
            if (dataObject is PdfStream pdfStream) // Single stream.
            {
                stream = streamIndex < 1
                    ? pdfStream.GetInputStream()
                    : null;
            }
            else if (dataObject is PdfArray streams) // Multiple streams.
            {
                stream = streamIndex < streams.Count
                    ? streams.Get<PdfStream>(streamIndex).GetInputStream()
                    : null;
            }
            if (stream == null)
                return false;

            stream.Seek(0);
            return true;
        }

        private bool MovePreviousStream()
        {
            if (streamIndex == 0)
            {
                streamIndex--;
                stream = null;
            }
            if (streamIndex == -1)
                return false;

            streamIndex--;
            /* NOTE: A content stream may be made up of multiple streams [PDF:1.6:3.6.2]. */
            // Is the content stream just a single stream?
            if (dataObject is PdfStream pdfStream) // Single stream.
            {
                stream = pdfStream.GetInputStream();
                basePosition = 0;
            }
            else // Array of streams.
            {
                var streams = (PdfArray)dataObject;

                stream = streams.Get<PdfStream>(streamIndex).GetInputStream();
                basePosition -= stream.Length;
            }

            return true;
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public byte PeekUByte(int offset) => GetStream().PeekUByte(offset);

        public void ByteAlign() => GetStream().ByteAlign();

        public int ReadBit() => GetStream().ReadBit();

        public uint ReadBits(int count) => GetStream().ReadBits(count);

        public int Mark() => (int)(mark = Position);

        public int Mark(long position) => (int)(mark = Position + position);

        public void ResetMark() => Seek(mark);
    }
}
