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

namespace PdfGlown.Bytes
{
    /// <summary>Buffer interface.</summary>
    public interface IByteStream : IOutputStream
    {
        /// <summary>Appends a byte to the buffer.</summary>
        /// <param name="data">Byte to copy.</param>
        /// <returns>This buffer.</returns>
        IByteStream Append(byte data);

        /// <summary>Appends a byte array to the buffer.</summary>
        /// <param name="data">Byte array to copy.</param>
        /// <returns>This buffer.</returns>
        IByteStream Append(byte[] data);

        /// <summary>Appends a byte range to the buffer.</summary>
        /// <param name="data">Byte array from which the byte range has to be copied.</param>
        /// <param name="offset">Location in the byte array at which copying begins.</param>
        /// <param name="length">Number of bytes to copy.</param>
        /// <returns>This buffer.</returns>
        IByteStream Append(byte[] data, int offset, int length);

        IByteStream Append(ReadOnlySpan<byte> data);

        /// <summary>Gets the allocated buffer size.</summary>
        /// <returns>Allocated buffer size.</returns>
        int Capacity
        {
            get;
        }

        /// <summary>Gets a clone of the buffer.</summary>
        /// <returns>Deep copy of the buffer.</returns>
        IByteStream Clone();



        /// <summary>Deletes a byte chunk from the buffer.</summary>
        /// <param name="index">Location at which deletion has to begin.</param>
        /// <param name="length">Number of bytes to delete.</param>
        void Delete(int index, int length);


        /// <summary>Gets the byte at a specified location.</summary>
        /// <param name="index">A location in the buffer.</param>
        /// <returns>Byte at the specified location.</returns>
        byte GetByte(int index);

        /// <summary>Gets the byte range beginning at a specified location.</summary>
        /// <param name="index">Location at which the byte range has to begin.</param>
        /// <param name="length">Number of bytes to copy.</param>
        /// <returns>Byte range beginning at the specified location.</returns>
        byte[] GetByteArray(int index, int length);

        /// <summary>Gets the byte range beginning at a specified location.</summary>
        /// <param name="index">Location at which the byte range has to begin.</param>
        /// <param name="length">Number of bytes to copy.</param>
        /// <returns>Byte range beginning at the specified location.</returns>
        Span<byte> AsSpan(int index, int length);


        /// <summary>Inserts a byte array into the buffer.</summary>
        /// <param name="index">Location at which the byte array has to be inserted.</param>
        /// <param name="data">Byte array to insert.</param>
        void Insert(int index, byte[] data);

        /// <summary>Inserts a byte range into the buffer.</summary>
        /// <param name="index">Location at which the byte range has to be inserted.</param>
        /// <param name="data">Byte array from which the byte range has to be copied.</param>
        /// <param name="offset">Location in the byte array at which copying begins.</param>
        /// <param name="length">Number of bytes to copy.</param>
        void Insert(int index, byte[] data, int offset, int length);

        /// <summary>Inserts a string into the buffer.</summary>
        /// <param name="index">Location at which the string has to be inserted.</param>
        /// <param name="data">String to insert.</param>
        void Insert(int index, string data);

        /// <summary>Inserts an IInputStream into the buffer.</summary>
        /// <param name="index">Location at which the IInputStream has to be inserted.</param>
        /// <param name="data">Source data to copy.</param>
        void Insert(int index, IInputStream data);

        /// <summary>Replaces the buffer contents with a byte array.</summary>
        /// <param name="index">Location at which the byte array has to be copied.</param>
        /// <param name="data">Byte array to copy.</param>
        void Replace(int index, byte[] data);

        /// <summary>Replaces the buffer contents with a byte range.</summary>
        /// <param name="index">Location at which the byte range has to be copied.</param>
        /// <param name="data">Byte array from which the byte range has to be copied.</param>
        /// <param name="offset">Location in the byte array at which copying begins.</param>
        /// <param name="length">Number of bytes to copy.</param>
        void Replace(int index, byte[] data, int offset, int length);

        /// <summary>Replaces the buffer contents with a string.</summary>
        /// <param name="index">Location at which the string has to be copied.</param>
        /// <param name="data">String to copy.</param>
        void Replace(int index, string data);

        /// <summary>Replaces the buffer contents with an IInputStream.</summary>
        /// <param name="index">Location at which the IInputStream has to be copied.</param>
        /// <param name="data">Source data to copy.</param>
        void Replace(int index, IInputStream data);


        /// <summary>Writes the buffer data to a stream.</summary>
        /// <param name="stream">Target stream.</param>
        void WriteTo(IOutputStream stream);

    }
}