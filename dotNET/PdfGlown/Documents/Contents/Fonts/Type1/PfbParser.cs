/*
 * https://github.com/apache/pdfbox
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */


using PdfGlown.Bytes;
using PdfGlown.Tokens;
using System;
using System.Collections.Generic;
using System.IO;

namespace PdfGlown.Documents.Contents.Fonts.Type1
{
    /// <summary>
    /// Parser for a pfb-file.
    /// @author Ben Litchfield
    /// @author Michael Niedermair
    /// </summary>
    public class PfbParser
    {
        /// the pdf header length.
        /// (start-marker (1 byte), ascii-/binary-marker(1 byte), size(4 byte))
        /// 3*6 == 18
        //private static readonly int PFB_HEADER_LENGTH = 18;

        /// the start marker.
        private static readonly int START_MARKER = 0x80;

        /// the ascii marker.
        private static readonly int ASCII_MARKER = 0x01;

        /// the binary marker.
        private static readonly int BINARY_MARKER = 0x02;

        ///the EOF marker.
        private static readonly int EOF_MARKER = 0x03;

        /// the parsed pfb-data.
        private Memory<byte> pfbdata;

        /// the lengths of the records.
        private readonly int[] lengths = new int[3];

        // sample (pfb-file)
        // 00000000 80 01 8b 15  00 00 25 21  50 53 2d 41  64 6f 62 65  
        //          ......%!PS-Adobe

        /// <summary>Create a new object.</summary>
        /// <param name="filename">the file name</param>
        public PfbParser(string filename)
        {
            using var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            ParsePfb(new ByteStream(fileStream));
        }

        /// <summary>Create a new object.</summary>
        /// <param name="input">The input.</param>
        public PfbParser(IInputStream input)
        {
            ParsePfb(input);
        }

        /// <summary>Create a new object.</summary>
        /// <param name="bytes">The input.</param>
        public PfbParser(Memory<byte> bytes)
        {
            ParsePfb(bytes);
        }

        /// <summary>Parse the pfb-array.</summary>
        /// <param name="pfb">The pfb-Array</param>
        private void ParsePfb(Memory<byte> pfb) => ParsePfb(new ByteStream(pfb));

        private void ParsePfb(IInputStream input)
        {
            input.Seek(0);
            // read into segments and keep them
            List<int> typeList = new(3);
            List<Memory<byte>> barrList = new(3);
            int total = 0;
            do
            {
                int r = input.ReadByte();
                if (r == -1 && total > 0)
                {
                    break; // EOF
                }
                if (r != START_MARKER)
                {
                    throw new IOException("Start marker missing");
                }
                int recordType = input.ReadByte();
                if (recordType == EOF_MARKER)
                {
                    break;
                }
                if (recordType != ASCII_MARKER && recordType != BINARY_MARKER)
                {
                    throw new IOException("Incorrect record type: " + recordType);
                }


                int size = StreamExtensions.ReadInt32(input.ReadSpan(4), Bytes.ByteOrderEnum.LittleEndian);

                //Debug.WriteLine($"debug: record type: {recordType}, segment size: {size}");
                var ar = input.ReadMemory(size);
                if (ar.Length != size)
                {
                    throw new IOException("EOF while reading PFB font");
                }
                total += size;
                typeList.Add(recordType);
                barrList.Add(ar);
            }
            while (true);

            // We now have ASCII and binary segments. Lets arrange these so that the ASCII segments
            // come first, then the binary segments, then the last ASCII segment if it is
            // 0000... cleartomark

            pfbdata = new byte[total];
            var pfbdataSpan = pfbdata.Span;
            Memory<byte> cleartomarkSegment = null;
            int dstPos = 0;

            // copy the ASCII segments
            for (int i = 0; i < typeList.Count; ++i)
            {
                if (typeList[i] != ASCII_MARKER)
                {
                    continue;
                }
                var ar = barrList[i];
                if (i == typeList.Count - 1 && ar.Length < 600 && Charset.ASCII.GetString(ar.Span).Contains("cleartomark"))
                {
                    cleartomarkSegment = ar;
                    continue;
                }
                ar.Span.CopyTo(pfbdataSpan.Slice(dstPos, ar.Length));
                dstPos += ar.Length;
            }
            lengths[0] = dstPos;

            // copy the binary segments
            for (int i = 0; i < typeList.Count; ++i)
            {
                if (typeList[i] != BINARY_MARKER)
                {
                    continue;
                }
                var ar = barrList[i];
                ar.Span.CopyTo(pfbdataSpan.Slice(dstPos, ar.Length));
                dstPos += ar.Length;
            }
            lengths[1] = dstPos - lengths[0];

            if (!cleartomarkSegment.IsEmpty)
            {
                cleartomarkSegment.Span.CopyTo(pfbdataSpan.Slice(dstPos, cleartomarkSegment.Length));
                lengths[2] = cleartomarkSegment.Length;
            }


        }

        /// <summary>Read the pdf input.</summary>
        /// <param name="input">The input.</param>
        /// <returns>the pdf-array.</returns>
        private Memory<byte> ReadPfbInput(ByteStream input) => input.AsMemory();

        public int[] Lengths => lengths;

        public Memory<byte> Pfbdata => pfbdata;

        /// <summary>The size of the pfb-data.</summary>
        public int Size => pfbdata.Length;

        /// <returns>Returns the pfb data as stream.</returns>
        public ByteStream GetInputStream()
        {
            return new ByteStream(pfbdata);
        }

        /// <summary>the first segment</summary>
        public Memory<byte> GetSegment1()
        {
            return pfbdata.Slice(0, lengths[0]);
        }

        /// <summary>the second segment</summary>
        public Memory<byte> GetSegment2()
        {
            return pfbdata.Slice(lengths[0], lengths[1]);
        }
    }
}