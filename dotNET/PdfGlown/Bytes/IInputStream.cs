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
    /// <summary>Input stream interface.</summary>
    public interface IInputStream : IStream, IDataWrapper
    {
        /// <summary>Gets/Sets the byte order.</summary>
        ByteOrderEnum ByteOrder { get; set; }

        bool IsAvailable { get; }

        /// <summary>Gets the hash representation of the sequence.</summary>
        int GetHashCode();

        /// <summary>Gets the pointer position.</summary>
        long Position { get; set; }

        /// <summary>Gets/Sets whether this buffer has changed.</summary>
        bool Dirty { get; set; }

        void CopyTo(Stream destination);
        void CopyTo(Stream destination, int bufferSize);

        /// <summary>Reads a sequence of bytes.</summary>
        /// <remarks>This operation causes the stream pointer to advance after the read data.</remarks>
        /// <param name="data">Target byte array.</param>
        int Read(byte[] data);

        /// <summary>Reads a sequence of bytes.</summary>
        /// <remarks>This operation causes the stream pointer to advance after the read data.</remarks>
        /// <param name="data">Target byte array.</param>
        int Read(Span<byte> data);

        /// <summary>Reads a sequence of bytes.</summary>
        /// <remarks>This operation causes the stream pointer to advance after the read data.</remarks>
        /// <param name="data">Target byte array.</param>
        /// <param name="offset">Location in the byte array at which storing begins.</param>
        /// <param name="length">Number of bytes to read.</param>
        int Read(byte[] data, int offset, int length);

        /// <summary>Reads a byte.</summary>
        /// <remarks>This operation causes the stream pointer to advance after the read data.</remarks>
        int ReadByte();

        int PeekByte();

        byte PeekUByte(int offset);

        /// <summary>Reads an integer.</summary>
        /// <remarks>This operation causes the stream pointer to advance after the read data.</remarks>
        int ReadInt32();

        /// <summary>Reads an unsigned integer.</summary>
        /// <remarks>This operation causes the stream pointer to advance after the read data.</remarks>
        uint ReadUInt32();

        /// <summary>Reads a variable-length integer.</summary>
        /// <remarks>This operation causes the stream pointer to advance after the read data.</remarks>
        /// <param name="length">Number of bytes to read.</param>
        int ReadInt(int length);

        /// <summary>Reads the next line of text.</summary>
        /// <remarks>This operation causes the stream pointer to advance after the read data.</remarks>
        string ReadLine();

        /// <summary>Reads a short integer.</summary>
        /// <remarks>This operation causes the stream pointer to advance after the read data.</remarks>
        short ReadInt16();

        /// <summary>Reads an unsigned short integer.</summary>
        /// <remarks>This operation causes the stream pointer to advance after the read data.</remarks>
        ushort ReadUInt16();

        /// <summary>Reads a bit integer.</summary>
        /// <remarks>This operation causes the stream pointer to advance after the read data.</remarks>
        long ReadInt64();

        /// <summary>Reads an unsigned bit integer.</summary>
        /// <remarks>This operation causes the stream pointer to advance after the read data.</remarks>
        ulong ReadUInt64();

        
        Span<byte> ReadSpan(int length);

        Memory<byte> ReadMemory(int length);

        void ByteAlign();

        int ReadBit();

        uint ReadBits(int count);

        /// <summary>Sets the pointer absolute position.</summary>
        long Seek(long position);

        /// <summary>Sets the pointer relative position.</summary>
        long Skip(long offset);

        int Mark();

        int Mark(long position);

        void ResetMark();
    }
}