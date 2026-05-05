/*
 * Copyright 2014 The Apache Software Foundation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except input compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to input writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using ICSharpCode.SharpZipLib.Lzw;
using PdfGlown.Objects;
using PdfGlown.Util.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace PdfGlown.Bytes.Filters
{
    /// <summary>
    /// This is the filter used for the LZWDecode filter.
    /// @author Ben Litchfield
    ///  @author Tilman Hausherr
    /// </summary>
    public sealed class LZWFilter : FlateFilter
    {
        /// <summary>The LZW clear table code.</summary>
        public static readonly long CLEAR_TABLE = 256;

        /// <summary>The LZW end of data code.</summary>
        public static readonly long EOD = 257;

        //BEWARE: codeTable must be local to each method, because there is only
        // one instance of each filter

        public  Memory<byte> DecodeStream(IInputStream data, PdfDirectObject parameters, IDictionary<PdfName, PdfDirectObject> header)
        {
            var outputStream = new MemoryStream();
            using (var lzwInputStream = new LzwInputStream((Stream)data))
            {
                Transform(lzwInputStream, outputStream);
                lzwInputStream.Close();
            }

            return DecodePredictor(outputStream, parameters, header);
        }

        public override Memory<byte> Decode(IInputStream data, PdfDirectObject parameters, IDictionary<PdfName, PdfDirectObject> header)
        {
            PdfDictionary decodeParams = (PdfDictionary)parameters;
            int earlyChange = decodeParams?.GetInt(PdfName.EarlyChange, 1) ?? 1;

            if (earlyChange != 0 && earlyChange != 1)
            {
                earlyChange = 1;
            }

            var result = DoLZWDecode(data, earlyChange);
            return DecodePredictor(result, parameters, header);
        }

        private Bytes.ByteStream DoLZWDecode(IInputStream input, int earlyChange)
        {
            List<byte[]> codeTable = new List<byte[]>();
            int chunk = 9;
            var decoded = new ByteStream();
            long nextCommand;
            long prevCommand = -1;
            try
            {
                while ((nextCommand = input.ReadBits(chunk)) != EOD)
                {
                    if (nextCommand == CLEAR_TABLE)
                    {
                        chunk = 9;
                        codeTable = CreateCodeTable();
                        prevCommand = -1;
                    }
                    else
                    {
                        if (nextCommand < codeTable.Count)
                        {
                            byte[] data = codeTable[(int)nextCommand];
                            byte firstByte = data[0];
                            decoded.Write(data);
                            if (prevCommand != -1)
                            {
                                CheckIndexBounds(codeTable, prevCommand, input);
                                data = codeTable[(int)prevCommand];
                                byte[] newData = data.CopyOf(data.Length + 1);
                                newData[data.Length] = firstByte;
                                codeTable.Add(newData);
                            }
                        }
                        else
                        {
                            CheckIndexBounds(codeTable, prevCommand, input);
                            byte[] data = codeTable[(int)prevCommand];
                            byte[] newData = data.CopyOf(data.Length + 1);
                            newData[data.Length] = data[0];
                            decoded.Write(newData);
                            codeTable.Add(newData);
                        }

                        chunk = CalculateChunk(codeTable.Count, earlyChange);
                        prevCommand = nextCommand;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("warn: Premature EOF input LZW stream, EOD code missing " + ex);
            }
            decoded.Seek(0);
            return decoded;
        }

        private void CheckIndexBounds(List<byte[]> codeTable, long index, IInputStream input)
        {
            if (index < 0)
            {
                throw new IOException($"negative array index: {index} near offset {input.Position}");
            }
            if (index >= codeTable.Count)
            {
                throw new IOException($"array index overflow: {index} >= {codeTable.Count} near offset {input.Position}");
            }
        }

        public override Memory<byte> Encode(IInputStream data, PdfDirectObject parameters, IDictionary<PdfName, PdfDirectObject> header)
        {
            List<byte[]> codeTable = CreateCodeTable();
            int chunk = 9;

            byte[] inputPattern = null;
            using (var output = new ByteStream())
            {
                output.WriteBits(CLEAR_TABLE, chunk);
                int foundCode = -1;
                int r;
                while ((r = data.ReadByte()) != -1)
                {
                    byte by = (byte)r;
                    if (inputPattern == null)
                    {
                        inputPattern = new byte[] { by };
                        foundCode = by & 0xff;
                    }
                    else
                    {
                        inputPattern = inputPattern.SubArray(0, inputPattern.Length + 1);
                        inputPattern[inputPattern.Length - 1] = by;
                        int newFoundCode = FindPatternCode(codeTable, inputPattern);
                        if (newFoundCode == -1)
                        {
                            // use previous
                            chunk = CalculateChunk(codeTable.Count - 1, 1);
                            output.WriteBits(foundCode, chunk);
                            // create new table entry
                            codeTable.Add(inputPattern);

                            if (codeTable.Count == 4096)
                            {
                                // code table is full
                                output.WriteBits(CLEAR_TABLE, chunk);
                                codeTable = CreateCodeTable();
                            }

                            inputPattern = new byte[] { by };
                            foundCode = by & 0xff;
                        }
                        else
                        {
                            foundCode = newFoundCode;
                        }
                    }
                }
                if (foundCode != -1)
                {
                    chunk = CalculateChunk(codeTable.Count - 1, 1);
                    output.WriteBits(foundCode, chunk);
                }

                // PPDFBOX-1977: the decoder wouldn't know that the encoder would output
                // an EOD as code, so he would have increased his own code table and
                // possibly adjusted the chunk. Therefore, the encoder must behave as
                // if the code table had just grown and thus it must be checked it is
                // needed to adjust the chunk, based on an increased table size parameter
                chunk = CalculateChunk(codeTable.Count, 1);

                output.WriteBits(EOD, chunk);

                // pad with 0
                output.WriteBits(0, 7);

                // must do or file will be empty :-(
                return output.AsMemory();
            }
        }

        /**
		 * Find the longest matching pattern input the code table.
		 *
		 * @param codeTable The LZW code table.
		 * @param pattern The pattern to be searched for.
		 * @return The index of the longest matching pattern or -1 if nothing is
		 * found.
		 */
        private int FindPatternCode(List<byte[]> codeTable, byte[] pattern)
        {
            int foundCode = -1;
            int foundLen = 0;
            for (int i = codeTable.Count - 1; i >= 0; --i)
            {
                if (i <= EOD)
                {
                    // we're input the single byte area
                    if (foundCode != -1)
                    {
                        // we already found pattern with size > 1
                        return foundCode;
                    }
                    else if (pattern.Length > 1)
                    {
                        // we won't find anything here anyway
                        return -1;
                    }
                }
                byte[] tryPattern = codeTable[i];
                if ((foundCode != -1 || tryPattern.Length > foundLen) && tryPattern.AsSpan().SequenceEqual(pattern.AsSpan()))
                {
                    foundCode = i;
                    foundLen = tryPattern.Length;
                }
            }
            return foundCode;
        }

        /**
		 * Init the code table with 1 byte entries and the EOD and CLEAR_TABLE
		 * markers.
		 */
        private List<byte[]> CreateCodeTable()
        {
            List<byte[]> codeTable = new List<byte[]>(4096);
            for (int i = 0; i < 256; ++i)
            {
                codeTable.Add(new byte[] { (byte)(i & 0xFF) });
            }
            codeTable.Add(null); // 256 EOD
            codeTable.Add(null); // 257 CLEAR_TABLE
            return codeTable;
        }

        /**
		 * Calculate the appropriate chunk size
		 *
		 * @param tabSize the size of the code table
		 * @param earlyChange 0 or 1 for early chunk increase
		 *
		 * @return a value between 9 and 12
		 */
        private int CalculateChunk(int tabSize, int earlyChange)
        {
            if (tabSize >= 2048 - earlyChange)
            {
                return 12;
            }
            if (tabSize >= 1024 - earlyChange)
            {
                return 11;
            }
            if (tabSize >= 512 - earlyChange)
            {
                return 10;
            }
            return 9;
        }
    }
}