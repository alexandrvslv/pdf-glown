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

using System;
using System.IO;

namespace PdfGlown.Bytes
{
    public class StreamSegment : StreamContainer
    {
        private Memory<byte>? buffer;

        public StreamSegment(IInputStream stream) : this(stream, stream.Position, stream.Length - stream.Position)
        { }

        public StreamSegment(IInputStream stream, long length) : this(stream, stream.Position, length)
        { }

        public StreamSegment(IInputStream stream, long startSegment, long lengthSegment)
            : base((Stream)stream)
        {
            StartSegment = startSegment;
            LengthSegment = lengthSegment;
            EndSegment = startSegment + lengthSegment;
        }

        public long StartSegment { get; }
        public long LengthSegment { get; }
        public long EndSegment { get; }

        public override long Position
        {
            get => base.Position - StartSegment;
            set => base.Position = value + StartSegment;
        }

        public override long Length => LengthSegment;

        public override long Seek(long offset, SeekOrigin origin)
        {
            return origin switch
            {
                SeekOrigin.Begin => base.Seek(StartSegment + offset, origin),
                SeekOrigin.End => base.Seek(EndSegment - offset, SeekOrigin.Begin),
                _ => base.Seek(offset, origin),
            };
        }

        public override int Read(byte[] data, int offset, int count)
        {
            if ((Position + count) > Length)
                count = (int)(Length - Position);
            return base.Read(data, offset, count);
        }

        public override int Read(Span<byte> data)
        {
            if ((Position + data.Length) > Length)
                data = data.Slice(0, (int)(Length - Position));
            return base.Read(data);
        }

        private Memory<byte> Buffer()
        {
            Mark();
            Position = 0;
            var result = base.ReadMemory((int)Length);
            ResetMark();
            return result;
        }

        public override Memory<byte> AsMemory()
        {
            return buffer ??= Buffer();
        }

        public override Span<byte> AsSpan()
        {
            return AsMemory().Span;
        }

        private void EnsurePosition()
        {
            var basePosition = base.Position;
            if (basePosition < StartSegment
                || basePosition > EndSegment)
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        protected override void Dispose(bool disposing)
        {
        }
    }
}