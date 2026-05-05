/*
  Copyright 2009-2011 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * J. James Jack, Ph.D., Senior Consultant at Symyx Technologies UK Ltd. (original
      code developer, james{dot}jack{at}symyx{dot}com)
    * Stefano Chizzolini (source code normalization to PDF Glown's conventions,
      http://www.stefanochizzolini.it)

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

using PdfGlown.Objects;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PdfGlown.Bytes.Filters
{
    /// <summary>ASCII base-85 filter [PDF:1.6:3.3.2].</summary>
    [Pdf(VersionEnum.PDF10)]
    public sealed class ASCII85Filter : Filter
    {
        /// <summary>Prefix mark that identifies an encoded ASCII85 string.</summary>
        private const string PrefixMark = "<~";

        /// <summary>Suffix mark that identifies an encoded ASCII85 string.</summary>
        private const string SuffixMark = "~>";

        /// <summary>Add the Prefix and Suffix marks when encoding, and enforce their presence for decoding.</summary>
        private const bool EnforceMarks = true;

        /// <summary>Maximum line length for encoded ASCII85 string; set to zero for one unbroken line.</summary>
        private const int LineLength = 75;

        private const int AsciiOffset = 33;

        private static readonly uint[] Pow85 = { 85 * 85 * 85 * 85, 85 * 85 * 85, 85 * 85, 85, 1 };

        private static void AppendChar(StringBuilder buffer, char data, ref int linePos)
        {
            buffer.Append(data);
            linePos++;
            if (LineLength > 0
              && linePos >= LineLength)
            {
                linePos = 0;
                buffer.Append('\n');
            }
        }

        private static void AppendString(StringBuilder buffer, string data, ref int linePos)
        {
            if (LineLength > 0 && linePos + data.Length > LineLength)
            {
                linePos = 0;
                buffer.Append('\n');
            }
            else
            {
                linePos += data.Length;
            }
            buffer.Append(data);
        }

        private static void DecodeBlock(Span<byte> decodedBlock, ref uint tuple) => DecodeBlock(decodedBlock, decodedBlock.Length, ref tuple);

        private static void DecodeBlock(Span<byte> decodedBlock, int count, ref uint tuple)
        {
            for (int i = 0; i < count; i++)
            {
                decodedBlock[i] = (byte)(tuple >> 24 - (i * 8));
            }
        }

        private static void EncodeBlock(byte[] encodedBlock, StringBuilder buffer, ref uint tuple, ref int linePos) => EncodeBlock(encodedBlock, encodedBlock.Length, buffer, ref tuple, ref linePos);

        private static void EncodeBlock(byte[] encodedBlock, int count, StringBuilder buffer, ref uint tuple, ref int linePos)
        {
            for (int i = encodedBlock.Length - 1; i >= 0; i--)
            {
                encodedBlock[i] = (byte)((tuple % 85) + AsciiOffset);
                tuple /= 85;
            }

            for (int i = 0; i < count; i++)
            {
                AppendChar(buffer, (char)encodedBlock[i], ref linePos);
            }
        }

        internal ASCII85Filter()
        { }

        public override Memory<byte> Decode(IInputStream data, PdfDirectObject parameters, IDictionary<PdfName, PdfDirectObject> header)
        {
            Span<byte> decodedBlock = stackalloc byte[4];
            Span<byte> byteBlock = stackalloc byte[1];
            Span<char> charBlock = stackalloc char[1];

            var encodedBlockLength = 5;
            var stream = new MemoryStream();
            uint tuple = 0;
            int count = 0, read = 0;
            bool processChar = false;
            while (data.Read(byteBlock) > 0)
            {
                Encoding.ASCII.GetChars(byteBlock, charBlock);
                char dataChar = charBlock[0];
                switch (dataChar)
                {
                    case 'z':
                        if (count != 0)
                            throw new Exception("The character 'z' is invalid inside an ASCII85 block.");

                        decodedBlock[0] = 0;
                        decodedBlock[1] = 0;
                        decodedBlock[2] = 0;
                        decodedBlock[3] = 0;
                        stream.Write(decodedBlock);
                        processChar = false;
                        break;
                    case '\n':
                    case '\r':
                    case '\t':
                    case '\0':
                    case '\f':
                    case '\b':
                        processChar = false;
                        break;
                    default:
                        if (dataChar == '>' && data.Position + 2 > data.Length
                            || dataChar == '<' && read == 0
                            || dataChar == '~' && (read == 0 || data.Position + 3 > data.Length))
                        {
                            processChar = false;
                            break;
                        }
                        if (dataChar < '!' || dataChar > 'u')
                            throw new Exception("Bad character '" + dataChar + "' found. ASCII85 only allows characters '!' to 'u'.");
                        read++;
                        processChar = true;
                        break;
                }

                if (processChar)
                {
                    tuple += ((uint)(dataChar - AsciiOffset) * Pow85[count]);
                    count++;
                    if (count == encodedBlockLength)
                    {
                        DecodeBlock(decodedBlock, ref tuple);
                        stream.Write(decodedBlock);
                        tuple = 0;
                        count = 0;
                    }
                }
            }

            // Bytes left over at the end?
            if (count != 0)
            {
                if (count == 1)
                    throw new Exception("The last block of ASCII85 data cannot be a single byte.");

                count--;
                tuple += Pow85[count];
                DecodeBlock(decodedBlock, count, ref tuple);
                for (int i = 0; i < count; i++)
                { stream.WriteByte(decodedBlock[i]); }
            }

            return stream.AsMemory();
        }

        public override Memory<byte> Encode(IInputStream data, PdfDirectObject parameters, IDictionary<PdfName, PdfDirectObject> header)
        {
            byte[] decodedBlock = new byte[4];
            byte[] encodedBlock = new byte[5];

            var buffer = new StringBuilder((int)(data.Length * (encodedBlock.Length / decodedBlock.Length)));
            int linePos = 0;

            if (EnforceMarks)
            { AppendString(buffer, PrefixMark, ref linePos); }

            int count = 0;
            uint tuple = 0;
            foreach (byte dataByte in data.AsMemory().Span)
            {
                if (count >= decodedBlock.Length - 1)
                {
                    tuple |= dataByte;
                    if (tuple == 0)
                    { AppendChar(buffer, 'z', ref linePos); }
                    else
                    { EncodeBlock(encodedBlock, buffer, ref tuple, ref linePos); }
                    tuple = 0;
                    count = 0;
                }
                else
                {
                    tuple |= (uint)(dataByte << (24 - (count * 8)));
                    count++;
                }
            }

            // if we have some bytes left over at the end..
            if (count > 0)
            { EncodeBlock(encodedBlock, count + 1, buffer, ref tuple, ref linePos); }

            if (EnforceMarks)
            { AppendString(buffer, SuffixMark, ref linePos); }

            return ASCIIEncoding.UTF8.GetBytes(buffer.ToString());
        }
    }
}