/* Copyright 2014 Mozilla Foundation
 *
 * Licensed under the Apache License, Version 2.0 (the 'License');
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an 'AS IS' BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using PdfGlown.Util.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PdfGlown.Bytes.Filters.Jpeg
{
    internal class JpegError : Exception
    {
        public JpegError(string msg) : base($"JPEG error: {msg}")
        { }
    }
    internal class DNLMarkerError : Exception
    {
        internal ushort scanLines;

        public DNLMarkerError(string message, ushort scanLines) : base(message)
        {
            this.scanLines = scanLines;
        }
    }

    internal class EOIMarkerError : Exception
    {
        public EOIMarkerError(string message) : base(message)
        { }
    }

    /// <summary>
    /// This code was forked from https://github.com/notmasteryet/jpgjs.
    /// The original version was created by GitHub user notmasteryet.
    /// - The JPEG specification can be found in the ITU CCITT Recommendation T.81
    ///   (www.w3.org/Graphics/JPEG/itu-t81.pdf)
    /// - The JFIF specification can be found in the JPEG File Interchange Format
    ///   (www.w3.org/Graphics/JPEG/jfif3.pdf)
    /// - The Adobe Application-Specific JPEG markers in the
    ///   Supporting the DCT Filters in PostScript Level 2, Technical Note #5116
    ///   (partners.adobe.com/public/developer/en/ps/sdk/5116.DCT_Filter.pdf)
    /// </summary>
    internal class JpegImage
    {
        // prettier-ignore
        static readonly byte[] DctZigZag = [
            0,
            1,  8,
            16,  9,  2,
            3, 10, 17, 24,
            32, 25, 18, 11, 4,
            5, 12, 19, 26, 33, 40,
            48, 41, 34, 27, 20, 13,  6,
            7, 14, 21, 28, 35, 42, 49, 56,
            57, 50, 43, 36, 29, 22, 15,
            23, 30, 37, 44, 51, 58,
            59, 52, 45, 38, 31,
            39, 46, 53, 60,
            61, 54, 47,
            55, 62,
            63
        ];

        static readonly int DctCos1 = 4017; // cos(pi/16)
        static readonly int DctSin1 = 799; // sin(pi/16)
        static readonly int DctCos3 = 3406; // cos(3*pi/16)
        static readonly int DctSin3 = 2276; // sin(3*pi/16)
        static readonly int DctCos6 = 1567; // cos(6*pi/16)
        static readonly int DctSin6 = 3784; // sin(6*pi/16)
        static readonly int DctSqrt2 = 5793; // sqrt(2)
        static readonly int DctSqrt1d2 = 2896; // sqrt(2) / 2

        private ushort width;
        private ushort height;
        private JFIF jfif;
        private Adobe adobe;
        private List<ImageComponent> components;
        private int numComponents;
        private int[] _decodeTransform;
        private int _colorTransform;

        // eslint-disable-next-line no-shadow
        public JpegImage(JpegOptions options)
        {
            _decodeTransform = options.DecodeTransform;
            _colorTransform = options.ColorTransform ?? -1;
        }

        public static Dictionary<int, object> BuildHuffmanTable(byte[] codeLengths, byte[] values)
        {
            var k = 0;
            var code = new List<Node>();
            int i, j, length = 16;
            while (length > 0 && codeLengths[length - 1] == 0)
            {
                length--;
            }
            code.Add(new Node(children: new Dictionary<int, object>(), index: 0));
            var p = code[0];
            Node q;
            for (i = 0; i < length; i++)
            {
                for (j = 0; j < codeLengths[i]; j++)
                {
                    p = code.RemoveAtValue(code.Count - 1);
                    p.Children[p.Index] = values[k];
                    while (p.Index > 0)
                    {
                        p = code.RemoveAtValue(code.Count - 1);
                    }
                    p.Index++;
                    code.Add(p);
                    while (code.Count <= i)
                    {
                        code.Add((q = new Node(children: new Dictionary<int, object>(), index: 0)));
                        p.Children[p.Index] = q.Children;
                        p = q;
                    }
                    k++;
                }
                if (i + 1 < length)
                {
                    // p here points to last code
                    code.Add((q = new Node(children: new Dictionary<int, object>(), index: 0)));
                    p.Children[p.Index] = q.Children;
                    p = q;
                }
            }
            return code[0].Children;
        }

        public static int GetBlockBufferOffset(Component component, int row, int col)
        {
            return 64 * ((component.BlocksPerLine + 1) * row + col);
        }

        public static int DecodeScan(
          IInputStream data,
          Frame frame,
          List<Component> components,
          ushort? resetInterval,
          int spectralStart,
          int spectralEnd,
          int successivePrev,
          int successive,
          bool parseDNLMarker = false
        )
        {
            var mcusPerLine = frame.McusPerLine;
            var progressive = frame.Progressive;

            var startOffset = data.Position;
            int bitsData = 0,
              bitsCount = 0;
            var blockRow = 0;
            var eobrun = 0;
            int successiveACState = 0,
              successiveACNextValue = 0;
            var componentsLength = components.Count;

            int ReadBit()
            {
                if (bitsCount > 0)
                {
                    bitsCount--;
                    return (bitsData >> bitsCount) & 1;
                }
                bitsData = data.ReadUByte();
                if (bitsData == 0xff)
                {
                    var nextByte = data.ReadUByte();
                    if (nextByte != 0)
                    {
                        if (nextByte == /* DNL = */ 0xdc && parseDNLMarker)
                        {
                            data.Skip(2); // Skip marker length.

                            var scanLines = data.ReadUInt16();
                            if (scanLines > 0 && scanLines != frame.ScanLines)
                            {
                                throw new DNLMarkerError(
                                  "Found DNL marker (0xFFDC) while parsing scan data",
                                  scanLines
                                );
                            }
                        }
                        else if (nextByte == /* EOI = */ 0xd9)
                        {
                            if (parseDNLMarker)
                            {
                                // NOTE: only 8-bit JPEG images are supported in this decoder.
                                var maybeScanLines = (ushort)(blockRow * (frame.Precision == 8 ? 8 : 0));
                                // Heuristic to attempt to handle corrupt JPEG images with too
                                // large "scanLines" parameter, by falling back to the currently
                                // parsed number of scanLines when it's at least one order of
                                // one "half" order of magnitude smaller than expected (fixes
                                // issue10880.pdf, issue10989.pdf, issue15492.pdf).
                                if (maybeScanLines > 0
                                    && Math.Round(frame.ScanLines / (double)maybeScanLines) >= 5)
                                {
                                    throw new DNLMarkerError(
                                      $"Found EOI marker (0xFFD9) while parsing scan data, possibly caused by incorrect {frame.ScanLines} parameter",
                                      maybeScanLines
                                    );
                                }
                            }
                            throw new EOIMarkerError("Found EOI marker (0xFFD9) while parsing scan data");
                        }
                        throw new JpegError($"unexpected marker {Convert.ToString((bitsData << 8) | nextByte, 16)}");
                    }
                    // unstuff 0
                }
                bitsCount = 7;
                return (int)(((uint)bitsData) >> 7);
            }

            byte DecodeHuffman(Dictionary<int, object> tree)
            {
                var node = tree;
                while (true)
                {
                    if (node.TryGetValue(ReadBit(), out var item))
                    {
                        if (item is byte byteValue)
                            return byteValue;
                        else
                            node = (Dictionary<int, object>)item;
                    }
                    else
                    {
                        throw new JpegError("invalid huffman sequence");
                    }
                }
            }

            int Receive(int length)
            {
                var n = 0;
                while (length > 0)
                {
                    n = (n << 1) | ReadBit();
                    length--;
                }
                return n;
            }

            int ReceiveAndExtend(int length)
            {
                if (length == 1)
                {
                    return ReadBit() == 1 ? 1 : -1;
                }
                var n = Receive(length);
                if (n >= 1 << (length - 1))
                {
                    return n;
                }
                return n + (-1 << length) + 1;
            }

            void DecodeBaseline(Component component, int blockOffset)
            {
                var t = DecodeHuffman(component.HuffmanTableDC);
                var diff = t == 0 ? 0 : ReceiveAndExtend(t);
                component.BlockData[blockOffset] = (short)(component.Pred += diff);
                var k = 1;
                while (k < 64)
                {
                    var rs = DecodeHuffman(component.HuffmanTableAC);
                    var s = rs & 15;
                    var r = rs >> 4;
                    if (s == 0)
                    {
                        if (r < 15)
                        {
                            break;
                        }
                        k += 16;
                        continue;
                    }
                    k += r;
                    var z = DctZigZag[k];
                    component.BlockData[blockOffset + z] = (short)(ReceiveAndExtend(s));
                    k++;
                }
            }

            void DecodeDCFirst(Component component, int blockOffset)
            {
                var t = DecodeHuffman(component.HuffmanTableDC);
                var diff = t == 0 ? 0 : ReceiveAndExtend(t) << successive;
                component.BlockData[blockOffset] = (short)(component.Pred += diff);
            }

            void DecodeDCSuccessive(Component component, int blockOffset)
            {
                component.BlockData[blockOffset] |= (short)(ReadBit() << successive);
            }

            void DecodeACFirst(Component component, int blockOffset)
            {
                if (eobrun > 0)
                {
                    eobrun--;
                    return;
                }
                int k = spectralStart,
                  e = spectralEnd;
                while (k <= e)
                {
                    var rs = DecodeHuffman(component.HuffmanTableAC);
                    var s = rs & 15;
                    var r = rs >> 4;
                    if (s == 0)
                    {
                        if (r < 15)
                        {
                            eobrun = Receive(r) + (1 << r) - 1;
                            break;
                        }
                        k += 16;
                        continue;
                    }
                    k += r;
                    var z = DctZigZag[k];
                    component.BlockData[blockOffset + z] = (short)(ReceiveAndExtend(s) * (1 << successive));
                    k++;
                }
            }

            void DecodeACSuccessive(Component component, int blockOffset)
            {
                var k = spectralStart;
                var e = spectralEnd;
                var r = 0;
                var s = 0;
                var rs = 0;
                while (k <= e)
                {
                    var offsetZ = blockOffset + DctZigZag[k];
                    var sign = component.BlockData[offsetZ] < 0 ? -1 : 1;
                    switch (successiveACState)
                    {
                        case 0: // initial state
                            rs = DecodeHuffman(component.HuffmanTableAC);
                            s = rs & 15;
                            r = rs >> 4;
                            if (s == 0)
                            {
                                if (r < 15)
                                {
                                    eobrun = Receive(r) + (1 << r);
                                    successiveACState = 4;
                                }
                                else
                                {
                                    r = 16;
                                    successiveACState = 1;
                                }
                            }
                            else
                            {
                                if (s != 1)
                                {
                                    throw new JpegError("invalid ACn encoding");
                                }
                                successiveACNextValue = ReceiveAndExtend(s);
                                successiveACState = r != 0 ? 2 : 3;
                            }
                            continue;
                        case 1: // skipping r zero items
                        case 2:
                            if (component.BlockData[offsetZ] != 0)
                            {
                                component.BlockData[offsetZ] += (short)(sign * (ReadBit() << successive));
                            }
                            else
                            {
                                r--;
                                if (r == 0)
                                {
                                    successiveACState = successiveACState == 2 ? 3 : 0;
                                }
                            }
                            break;
                        case 3: // set value for a zero item
                            if (component.BlockData[offsetZ] != 0)
                            {
                                component.BlockData[offsetZ] += (short)(sign * (ReadBit() << successive));
                            }
                            else
                            {
                                component.BlockData[offsetZ] = (short)(successiveACNextValue << successive);
                                successiveACState = 0;
                            }
                            break;
                        case 4: // eob
                            if (component.BlockData[offsetZ] != 0)
                            {
                                component.BlockData[offsetZ] += (short)(sign * (ReadBit() << successive));
                            }
                            break;
                    }
                    k++;
                }
                if (successiveACState == 4)
                {
                    eobrun--;
                    if (eobrun == 0)
                    {
                        successiveACState = 0;
                    }
                }
            }

            void decodeMcu(Component component, Action<Component, int> decode, int mcu, int row, int col)
            {
                var mcuRow = (mcu / mcusPerLine) | 0;
                var mcuCol = mcu % mcusPerLine;
                blockRow = mcuRow * component.V + row;
                var blockCol = mcuCol * component.H + col;
                var blockOffset = GetBlockBufferOffset(component, blockRow, blockCol);
                decode(component, blockOffset);
            }

            void decodeBlock(Component component, Action<Component, int> decode, int mcu)
            {
                blockRow = (mcu / component.BlocksPerLine) | 0;
                var blockCol = mcu % component.BlocksPerLine;
                var blockOffset = GetBlockBufferOffset(component, blockRow, blockCol);
                decode(component, blockOffset);
            }

            //Component component;
            int i, j;
            Action<Component, int> decodeFn;
            int mcuv = 0;
            FileMarker fileMarker = null;
            int h, v;

            if (progressive)
            {
                if (spectralStart == 0)
                {
                    decodeFn = successivePrev == 0 ? (Action<Component, int>)DecodeDCFirst : (Action<Component, int>)DecodeDCSuccessive;
                }
                else
                {
                    decodeFn = successivePrev == 0 ? (Action<Component, int>)DecodeACFirst : (Action<Component, int>)DecodeACSuccessive;
                }
            }
            else
            {
                decodeFn = DecodeBaseline;
            }

            int mcuExpected = componentsLength == 1
                ? components[0].BlocksPerLine * components[0].BlocksPerColumn
                : mcusPerLine * frame.McusPerColumn;
            while (mcuv <= mcuExpected)
            {
                // reset interval stuff
                var mcuToRead = resetInterval != null && resetInterval != 0
                  ? Math.Min(mcuExpected - mcuv, (int)resetInterval)
                  : mcuExpected;

                // The "mcuToRead == 0" case should only occur when all of the expected
                // MCU data has been already parsed, i.e. when "mcu == mcuExpected", but
                // some corrupt JPEG images contain more data than intended and we thus
                // want to skip over any extra RSTx markers below (fixes issue11794.pdf).
                if (mcuToRead > 0)
                {
                    for (i = 0; i < componentsLength; i++)
                    {
                        components[i].Pred = 0;
                    }
                    eobrun = 0;

                    if (componentsLength == 1)
                    {
                        var component = components[0];
                        for (var n = 0; n < mcuToRead; n++)
                        {
                            decodeBlock(component, decodeFn, mcuv);
                            mcuv++;
                        }
                    }
                    else
                    {
                        for (var n = 0; n < mcuToRead; n++)
                        {
                            for (i = 0; i < componentsLength; i++)
                            {
                                var component = components[i];
                                h = component.H;
                                v = component.V;
                                for (j = 0; j < v; j++)
                                {
                                    for (var k = 0; k < h; k++)
                                    {
                                        decodeMcu(component, decodeFn, mcuv, j, k);
                                    }
                                }
                            }
                            mcuv++;
                        }
                    }
                }

                // find marker
                bitsCount = 0;
                fileMarker = FindNextFileMarker(data.AsSpan(), (int)data.Position, (int)data.Position);
                if (fileMarker == null)
                {
                    break; // Reached the end of the image data without finding any marker.
                }
                if (fileMarker.Invalid != null)
                {
                    // Some bad images seem to pad Scan blocks with e.g. zero bytes, skip
                    // past those to attempt to find a valid marker (fixes issue4090.pdf).
                    var partialMsg = mcuToRead > 0 ? "unexpected" : "excessive";
                    Debug.WriteLine($"warn: decodeScan - {partialMsg} MCU data, current marker is: {fileMarker.Invalid}");
                    data.Seek(fileMarker.Offset);
                }
                if (fileMarker.Marker >= 0xffd0 && fileMarker.Marker <= 0xffd7)
                {
                    // RSTx
                    data.Skip(2);
                }
                else
                {
                    break;
                }
            }

            return (int)(data.Position - startOffset);
        }

        // A port of poppler's IDCT method which in turn is taken from:
        //   Christoph Loeffler, Adriaan Ligtenberg, George S. Moschytz,
        //   'Practical Fast 1-D DCT Algorithms with 11 Multiplications',
        //   IEEE Intl. Conf. on Acoustics, Speech & Signal Processing, 1989,
        //   988-991.
        public static void QuantizeAndInverse(Component component, int blockBufferOffset, short[] p)
        {
            var qt = component.QuantizationTable;
            var blockData = component.BlockData;
            int v0, v1, v2, v3, v4, v5, v6, v7;
            int p0, p1, p2, p3, p4, p5, p6, p7;
            short t;

            if (qt == null)
            {
                throw new JpegError("missing required Quantization Table.");
            }

            // inverse DCT on rows
            for (var row = 0; row < 64; row += 8)
            {
                // gather block data
                p0 = blockData[blockBufferOffset + row];
                p1 = blockData[blockBufferOffset + row + 1];
                p2 = blockData[blockBufferOffset + row + 2];
                p3 = blockData[blockBufferOffset + row + 3];
                p4 = blockData[blockBufferOffset + row + 4];
                p5 = blockData[blockBufferOffset + row + 5];
                p6 = blockData[blockBufferOffset + row + 6];
                p7 = blockData[blockBufferOffset + row + 7];

                // dequant p0
                p0 *= qt[row];

                // check for all-zero AC coefficients
                if ((p1 | p2 | p3 | p4 | p5 | p6 | p7) == 0)
                {
                    t = (short)((DctSqrt2 * p0 + 512) >> 10);
                    p[row] = t;
                    p[row + 1] = t;
                    p[row + 2] = t;
                    p[row + 3] = t;
                    p[row + 4] = t;
                    p[row + 5] = t;
                    p[row + 6] = t;
                    p[row + 7] = t;
                    continue;
                }
                // dequant p1 ... p7
                p1 *= qt[row + 1];
                p2 *= qt[row + 2];
                p3 *= qt[row + 3];
                p4 *= qt[row + 4];
                p5 *= qt[row + 5];
                p6 *= qt[row + 6];
                p7 *= qt[row + 7];

                // stage 4
                v0 = (DctSqrt2 * p0 + 128) >> 8;
                v1 = (DctSqrt2 * p4 + 128) >> 8;
                v2 = p2;
                v3 = p6;
                v4 = (DctSqrt1d2 * (p1 - p7) + 128) >> 8;
                v7 = (DctSqrt1d2 * (p1 + p7) + 128) >> 8;
                v5 = p3 << 4;
                v6 = p5 << 4;

                // stage 3
                v0 = (v0 + v1 + 1) >> 1;
                v1 = v0 - v1;
                t = (short)((v2 * DctSin6 + v3 * DctCos6 + 128) >> 8);
                v2 = (v2 * DctCos6 - v3 * DctSin6 + 128) >> 8;
                v3 = t;
                v4 = (v4 + v6 + 1) >> 1;
                v6 = v4 - v6;
                v7 = (v7 + v5 + 1) >> 1;
                v5 = v7 - v5;

                // stage 2
                v0 = (v0 + v3 + 1) >> 1;
                v3 = v0 - v3;
                v1 = (v1 + v2 + 1) >> 1;
                v2 = v1 - v2;
                t = (short)((v4 * DctSin3 + v7 * DctCos3 + 2048) >> 12);
                v4 = (v4 * DctCos3 - v7 * DctSin3 + 2048) >> 12;
                v7 = t;
                t = (short)((v5 * DctSin1 + v6 * DctCos1 + 2048) >> 12);
                v5 = (v5 * DctCos1 - v6 * DctSin1 + 2048) >> 12;
                v6 = t;

                // stage 1
                p[row] = (short)(v0 + v7);
                p[row + 7] = (short)(v0 - v7);
                p[row + 1] = (short)(v1 + v6);
                p[row + 6] = (short)(v1 - v6);
                p[row + 2] = (short)(v2 + v5);
                p[row + 5] = (short)(v2 - v5);
                p[row + 3] = (short)(v3 + v4);
                p[row + 4] = (short)(v3 - v4);
            }

            // inverse DCT on columns
            for (var col = 0; col < 8; ++col)
            {
                p0 = p[col];
                p1 = p[col + 8];
                p2 = p[col + 16];
                p3 = p[col + 24];
                p4 = p[col + 32];
                p5 = p[col + 40];
                p6 = p[col + 48];
                p7 = p[col + 56];

                // check for all-zero AC coefficients
                if ((p1 | p2 | p3 | p4 | p5 | p6 | p7) == 0)
                {
                    t = (short)((DctSqrt2 * p0 + 8192) >> 14);
                    // Convert to 8-bit.
                    if (t < -2040)
                    {
                        t = 0;
                    }
                    else if (t >= 2024)
                    {
                        t = 255;
                    }
                    else
                    {
                        t = (short)((t + 2056) >> 4);
                    }
                    blockData[blockBufferOffset + col] = t;
                    blockData[blockBufferOffset + col + 8] = t;
                    blockData[blockBufferOffset + col + 16] = t;
                    blockData[blockBufferOffset + col + 24] = t;
                    blockData[blockBufferOffset + col + 32] = t;
                    blockData[blockBufferOffset + col + 40] = t;
                    blockData[blockBufferOffset + col + 48] = t;
                    blockData[blockBufferOffset + col + 56] = t;
                    continue;
                }

                // stage 4
                v0 = (DctSqrt2 * p0 + 2048) >> 12;
                v1 = (DctSqrt2 * p4 + 2048) >> 12;
                v2 = p2;
                v3 = p6;
                v4 = (DctSqrt1d2 * (p1 - p7) + 2048) >> 12;
                v7 = (DctSqrt1d2 * (p1 + p7) + 2048) >> 12;
                v5 = p3;
                v6 = p5;

                // stage 3
                // Shift v0 by 128.5 << 5 here, so we don't need to shift p0...p7 when
                // converting to UInt8 range later.
                v0 = ((v0 + v1 + 1) >> 1) + 4112;
                v1 = v0 - v1;
                t = (short)((v2 * DctSin6 + v3 * DctCos6 + 2048) >> 12);
                v2 = (v2 * DctCos6 - v3 * DctSin6 + 2048) >> 12;
                v3 = t;
                v4 = (v4 + v6 + 1) >> 1;
                v6 = v4 - v6;
                v7 = (v7 + v5 + 1) >> 1;
                v5 = v7 - v5;

                // stage 2
                v0 = (v0 + v3 + 1) >> 1;
                v3 = v0 - v3;
                v1 = (v1 + v2 + 1) >> 1;
                v2 = v1 - v2;
                t = (short)((v4 * DctSin3 + v7 * DctCos3 + 2048) >> 12);
                v4 = (v4 * DctCos3 - v7 * DctSin3 + 2048) >> 12;
                v7 = t;
                t = (short)((v5 * DctSin1 + v6 * DctCos1 + 2048) >> 12);
                v5 = (v5 * DctCos1 - v6 * DctSin1 + 2048) >> 12;
                v6 = t;

                // stage 1
                p0 = v0 + v7;
                p7 = v0 - v7;
                p1 = v1 + v6;
                p6 = v1 - v6;
                p2 = v2 + v5;
                p5 = v2 - v5;
                p3 = v3 + v4;
                p4 = v3 - v4;

                // Convert to 8-bit integers.
                if (p0 < 16)
                {
                    p0 = 0;
                }
                else if (p0 >= 4080)
                {
                    p0 = 255;
                }
                else
                {
                    p0 >>= 4;
                }
                if (p1 < 16)
                {
                    p1 = 0;
                }
                else if (p1 >= 4080)
                {
                    p1 = 255;
                }
                else
                {
                    p1 >>= 4;
                }
                if (p2 < 16)
                {
                    p2 = 0;
                }
                else if (p2 >= 4080)
                {
                    p2 = 255;
                }
                else
                {
                    p2 >>= 4;
                }
                if (p3 < 16)
                {
                    p3 = 0;
                }
                else if (p3 >= 4080)
                {
                    p3 = 255;
                }
                else
                {
                    p3 >>= 4;
                }
                if (p4 < 16)
                {
                    p4 = 0;
                }
                else if (p4 >= 4080)
                {
                    p4 = 255;
                }
                else
                {
                    p4 >>= 4;
                }
                if (p5 < 16)
                {
                    p5 = 0;
                }
                else if (p5 >= 4080)
                {
                    p5 = 255;
                }
                else
                {
                    p5 >>= 4;
                }
                if (p6 < 16)
                {
                    p6 = 0;
                }
                else if (p6 >= 4080)
                {
                    p6 = 255;
                }
                else
                {
                    p6 >>= 4;
                }
                if (p7 < 16)
                {
                    p7 = 0;
                }
                else if (p7 >= 4080)
                {
                    p7 = 255;
                }
                else
                {
                    p7 >>= 4;
                }

                // store block data
                blockData[blockBufferOffset + col] = (short)(p0);
                blockData[blockBufferOffset + col + 8] = (short)(p1);
                blockData[blockBufferOffset + col + 16] = (short)(p2);
                blockData[blockBufferOffset + col + 24] = (short)(p3);
                blockData[blockBufferOffset + col + 32] = (short)(p4);
                blockData[blockBufferOffset + col + 40] = (short)(p5);
                blockData[blockBufferOffset + col + 48] = (short)(p6);
                blockData[blockBufferOffset + col + 56] = (short)(p7);
            }
        }

        public static short[] BuildComponentData(Frame frame, Component component)
        {
            var blocksPerLine = component.BlocksPerLine;
            var blocksPerColumn = component.BlocksPerColumn;
            var computationBuffer = new short[64];

            for (var blockRow = 0; blockRow < blocksPerColumn; blockRow++)
            {
                for (var blockCol = 0; blockCol < blocksPerLine; blockCol++)
                {
                    var offset = GetBlockBufferOffset(component, blockRow, blockCol);
                    QuantizeAndInverse(component, offset, computationBuffer);
                }
            }
            return component.BlockData;
        }

        public static FileMarker FindNextFileMarker(ReadOnlySpan<byte> data, int currentPos, int startPos)// = currentPos
        {
            var maxPos = data.Length - 1;
            var newPos = startPos < currentPos ? startPos : currentPos;

            if (currentPos >= maxPos)
            {
                return null; // Don't attempt to read non-existent data and just return.
            }
            var currentMarker = data.ReadUInt16(currentPos);
            if (currentMarker >= 0xffc0 && currentMarker <= 0xfffe)
            {
                return new FileMarker(
                  invalid: null,
                  marker: currentMarker,
                  offset: currentPos
                );
            }
            var newMarker = data.ReadUInt16(newPos);
            while (!(newMarker >= 0xffc0 && newMarker <= 0xfffe))
            {
                if (++newPos >= maxPos)
                {
                    return null; // Don't attempt to read non-existent data and just return.
                }
                newMarker = data.ReadUInt16(newPos);
            }
            return new FileMarker(
              invalid: Convert.ToString(currentMarker, 16),
              marker: newMarker,
              offset: newPos
            );
        }

        void PrepareComponents(Frame f)
        {
            var mcusPerLine = (int)Math.Ceiling((double)f.SamplesPerLine / 8 / f.MaxH);
            var mcusPerColumn = (int)Math.Ceiling((double)f.ScanLines / 8 / f.MaxV);
            foreach (var component in f.Components)
            {
                var blocksPerLine = (int)Math.Ceiling((Math.Ceiling((double)f.SamplesPerLine / 8) * component.H) / f.MaxH);
                var blocksPerColumn = (int)Math.Ceiling((Math.Ceiling((double)f.ScanLines / 8) * component.V) / f.MaxV);
                var blocksPerLineForMcu = mcusPerLine * component.H;
                var blocksPerColumnForMcu = mcusPerColumn * component.V;

                var blocksBufferSize =
                  64 * blocksPerColumnForMcu * (blocksPerLineForMcu + 1);
                component.BlockData = new short[blocksBufferSize];
                component.BlocksPerLine = blocksPerLine;
                component.BlocksPerColumn = blocksPerColumn;
            }
            f.McusPerLine = mcusPerLine;
            f.McusPerColumn = mcusPerColumn;
        }

        public void Parse(IInputStream data, ushort? dnlScanLines = null)
        {
            JFIF jfif = null;
            Adobe adobe = null;
            Frame frame = null;
            ushort? resetInterval = null;
            var numSOSMarkers = 0;
            var quantizationTables = new Dictionary<int, ushort[]>();
            var huffmanTablesAC = new Dictionary<int, Dictionary<int, object>>();
            var huffmanTablesDC = new Dictionary<int, Dictionary<int, object>>();
            var codeLengths = new byte[16];

            Memory<byte> readDataBlock()
            {
                var length = data.ReadUInt16();
                var position = (int)data.Position;
                var endOffset = position + length - 2;


                var marker = FindNextFileMarker(data.AsSpan(), endOffset, position);
                if (marker?.Invalid != null)
                {
                    Debug.WriteLine("warn: readDataBlock - incorrect length, current marker is: " + marker.Invalid);
                    endOffset = marker.Offset;
                }

                return data.ReadMemory(endOffset - position);
            }


            // Some images may contain 'junk' before the SOI (start-of-image) marker.
            // Note: this seems to mainly affect inline images.
            while (data.Position < data.Length)
            {
                // Find the first byte of the SOI marker (0xFFD8).
                if (data.ReadUByte() == 0xff)
                {
                    data.Skip(-1);
                    break;
                }
            }

            var fileMarker = data.ReadUInt16();
            if (fileMarker != /* SOI (Start of Image) = */ 0xffd8)
            {
                throw new JpegError("SOI not found");
            }
            fileMarker = data.ReadUInt16();

            while (fileMarker != /* EOI (End of Image) = */ 0xffd9)
            {
                int i, j, l;
                switch (fileMarker)
                {
                    case 0xffe0: // APP0 (Application Specific)
                    case 0xffe1: // APP1
                    case 0xffe2: // APP2
                    case 0xffe3: // APP3
                    case 0xffe4: // APP4
                    case 0xffe5: // APP5
                    case 0xffe6: // APP6
                    case 0xffe7: // APP7
                    case 0xffe8: // APP8
                    case 0xffe9: // APP9
                    case 0xffea: // APP10
                    case 0xffeb: // APP11
                    case 0xffec: // APP12
                    case 0xffed: // APP13
                    case 0xffee: // APP14
                    case 0xffef: // APP15
                    case 0xfffe: // COM (Comment)
                        var block = readDataBlock();
                        var appData = block.Span;
                        if (fileMarker == 0xffe0)
                        {
                            // 'JFIF\x00'
                            if (
                              appData[0] == 0x4a &&
                              appData[1] == 0x46 &&
                              appData[2] == 0x49 &&
                              appData[3] == 0x46 &&
                              appData[4] == 0
                            )
                            {
                                jfif = new JFIF(
                                  version: new PdfVersion(major: appData[5],
                                    minor: appData[6]),
                                    densityUnits: appData[7],
                                    xDensity: (appData[8] << 8) | appData[9],
                                    yDensity: (appData[10] << 8) | appData[11],
                                    thumbWidth: appData[12],
                                    thumbHeight: appData[13],
                                    thumbData: block.Slice(
                                        14,
                                        3 * appData[12] * appData[13]
                                  )
                                );
                            }
                        }
                        // TODO APP1 - Exif
                        if (fileMarker == 0xffee)
                        {
                            // 'Adobe'
                            if (
                              appData[0] == 0x41 &&
                              appData[1] == 0x64 &&
                              appData[2] == 0x6f &&
                              appData[3] == 0x62 &&
                              appData[4] == 0x65
                            )
                            {
                                adobe = new Adobe(
                                  version: (appData[5] << 8) | appData[6],
                                  flags0: (appData[7] << 8) | appData[8],
                                  flags1: (appData[9] << 8) | appData[10],
                                  transformCode: appData[11]
                                );
                            }
                        }
                        break;

                    case 0xffdb: // DQT (Define Quantization Tables)
                        var quantizationTablesLength = data.ReadUInt16();
                        var quantizationTablesEnd = quantizationTablesLength + (int)data.Position - 2;
                        int z;
                        while (data.Position < quantizationTablesEnd)
                        {
                            var quantizationTableSpec = data.ReadUByte();
                            var tableData = new ushort[64];
                            if (quantizationTableSpec >> 4 == 0)
                            {
                                // 8 bit values
                                for (j = 0; j < 64; j++)
                                {
                                    z = DctZigZag[j];
                                    tableData[z] = data.ReadUByte();
                                }
                            }
                            else if (quantizationTableSpec >> 4 == 1)
                            {
                                // 16 bit values
                                for (j = 0; j < 64; j++)
                                {
                                    z = DctZigZag[j];
                                    tableData[z] = data.ReadUInt16();
                                }
                            }
                            else
                            {
                                throw new JpegError("DQT - invalid table spec");
                            }
                            quantizationTables[quantizationTableSpec & 15] = tableData;
                        }
                        break;

                    case 0xffc0: // SOF0 (Start of Frame, Baseline DCT)
                    case 0xffc1: // SOF1 (Start of Frame, Extended DCT)
                    case 0xffc2: // SOF2 (Start of Frame, Progressive DCT)
                        if (frame != null)
                        {
                            throw new JpegError("Only single frame JPEGs supported");
                        }
                        data.Skip(2); // Skip marker length.

                        frame = new Frame();
                        frame.Extended = fileMarker == 0xffc1;
                        frame.Progressive = fileMarker == 0xffc2;
                        frame.Precision = data.ReadUByte();
                        var sofScanLines = data.ReadUInt16();
                        frame.ScanLines = dnlScanLines ?? sofScanLines;
                        frame.SamplesPerLine = data.ReadUInt16();
                        frame.Components = new List<Component>();
                        frame.ComponentIds = new Dictionary<byte, int> { };
                        var componentsCount = data.ReadUByte();
                        byte componentId;
                        int maxH = 0, maxV = 0;
                        for (i = 0; i < componentsCount; i++)
                        {
                            componentId = data.ReadUByte();
                            var subId = data.ReadUByte();
                            var h = subId >> 4;
                            var v = subId & 15;
                            if (maxH < h)
                            {
                                maxH = h;
                            }
                            if (maxV < v)
                            {
                                maxV = v;
                            }
                            var qId = data.ReadUByte();
                            l = frame.Components.Count();
                            frame.Components.Add(new Component(
                              h,
                              v,
                              quantizationId: qId,
                              quantizationTable: null // See comment below.
                            ));
                            frame.ComponentIds[componentId] = l;
                        }
                        frame.MaxH = maxH;
                        frame.MaxV = maxV;
                        PrepareComponents(frame);
                        break;

                    case 0xffc4: // DHT (Define Huffman Tables)
                        var huffmanLength = data.ReadUInt16();
                        for (i = 2; i < huffmanLength;)
                        {
                            var huffmanTableSpec = data.ReadUByte();
                            data.Read(codeLengths);
                            var codeLengthSum = 0;
                            for (j = 0; j < 16; j++)
                            {
                                codeLengthSum += codeLengths[j];
                            }
                            var huffmanValues = new byte[codeLengthSum];
                            data.Read(huffmanValues);
                            i += 17 + codeLengthSum;

                            var huffmanTables = huffmanTableSpec >> 4 == 0 ? huffmanTablesDC : huffmanTablesAC;
                            huffmanTables[huffmanTableSpec & 15] = BuildHuffmanTable(codeLengths, huffmanValues);
                        }
                        break;

                    case 0xffdd: // DRI (Define Restart Interval)
                        data.Skip(2); // Skip marker length.

                        resetInterval = data.ReadUInt16();
                        break;

                    case 0xffda: // SOS (Start of Scan)
                                 // A DNL marker (0xFFDC), if it exists, is only allowed at the end
                                 // of the first scan segment and may only occur once in an image.
                                 // Furthermore, to prevent an infinite loop, do *not* attempt to
                                 // parse DNL markers during re-parsing of the JPEG scan data.
                        var parseDNLMarker = ++numSOSMarkers == 1 && dnlScanLines == null;

                        data.Skip(2); // Skip marker length.

                        var selectorsCount = data.ReadUByte();
                        var components = new List<Component>();
                        Component component;
                        for (i = 0; i < selectorsCount; i++)
                        {
                            var index = data.ReadUByte();
                            var componentIndex = frame.ComponentIds[index];
                            component = frame.Components[componentIndex];
                            component.Index = index;
                            var tableSpec = data.ReadUByte();
                            component.HuffmanTableDC = huffmanTablesDC.TryGetValue(tableSpec >> 4, out var dc) ? dc : null;
                            component.HuffmanTableAC = huffmanTablesAC.TryGetValue(tableSpec & 15, out var ac) ? ac : null;
                            components.Add(component);
                        }
                        var spectralStart = data.ReadUByte();
                        var spectralEnd = data.ReadUByte();
                        var successiveApproximation = data.ReadUByte();
                        try
                        {
                            var processed = DecodeScan(
                              data,
                              frame,
                              components,
                              resetInterval,
                              spectralStart,
                              spectralEnd,
                              successiveApproximation >> 4,
                              successiveApproximation & 15,
                              parseDNLMarker
                            );
                        }
                        catch (Exception ex)
                        {
                            if (ex is DNLMarkerError dNLMarkerError)
                            {
                                Debug.WriteLine($"warn:{ex.Message} -- attempting to re-parse the JPEG image.");
                                frame = null;
                                ReParse(data, dnlScanLines: dNLMarkerError.scanLines);
                                return;
                            }
                            else if (ex is EOIMarkerError)
                            {
                                Debug.WriteLine($"warn{ex.Message} -- ignoring the rest of the image data.");
                                goto markerLoop;
                            }
                            throw;
                        }
                        break;

                    case 0xffdc: // DNL (Define Number of Lines)
                                 // Ignore the marker, since it's being handled in "decodeScan".
                        data.Skip(4);
                        break;

                    case 0xffff: // Fill bytes
                        if (data.PeekUByte(0) != 0xff)
                        {
                            // Avoid skipping a valid marker.
                            data.Skip(-1);
                        }
                        break;

                    default:
                        // Could be incorrect encoding -- the last 0xFF byte of the previous
                        // block could have been eaten by the encoder, hence we fallback to
                        // "startPos = offset - 3" when looking for the next valid marker.
                        var nextFileMarker = FindNextFileMarker(
                          data.AsSpan(),
                          (int)data.Position - 2,
                          (int)data.Position - 3
                        );
                        if (nextFileMarker?.Invalid != null)
                        {
                            Debug.WriteLine("warn: JpegImage.parse - unexpected data, current marker is: " + nextFileMarker.Invalid);
                            data.Seek(nextFileMarker.Offset);
                            break;
                        }
                        if (nextFileMarker == null || data.Position >= data.Length - 1)
                        {
                            Debug.WriteLine("warn: JpegImage.parse - reached the end of the image data " + "without finding an EOI marker (0xFFD9).");
                            goto markerLoop;
                        }
                        throw new JpegError("JpegImage.parse - unknown marker: " + Convert.ToString(fileMarker, 16));
                }
                fileMarker = data.ReadUInt16();
            }
        markerLoop:
            width = frame.SamplesPerLine;
            height = frame.ScanLines;
            this.jfif = jfif;
            this.adobe = adobe;
            components = new List<ImageComponent>();
            foreach (var component in frame.Components)
            {
                // Prevent errors when DQT markers are placed after SOF{n} markers,
                // by assigning the "quantizationTable" entry after the entire image
                // has been parsed (fixes issue7406.pdf).
                if (quantizationTables.TryGetValue(component.QuantizationId, out var quantizationTable))
                {
                    component.QuantizationTable = quantizationTable;
                }

                components.Add(new ImageComponent(
                    index: component.Index,
                    output: BuildComponentData(frame, component),
                    scaleX: (double)component.H / frame.MaxH,
                    scaleY: (double)component.V / frame.MaxV,
                    blocksPerLine: component.BlocksPerLine,
                    blocksPerColumn: component.BlocksPerColumn
                    ));
            }
            numComponents = components.Count;
            //return null;
        }

        private void ReParse(IInputStream data, ushort dnlScanLines)
        {
            components = null;
            GC.Collect();
            data.Seek(0);
            Parse(data, dnlScanLines);
        }

        Memory<byte> GetLinearizedBlockData(int width, int height, bool isSourcePDF = false)
        {
            double scaleX = this.width / (double)width,
              scaleY = this.height / (double)height;

            ImageComponent component;
            double componentScaleX, componentScaleY, lastComponentScaleX = 0;
            uint x, i, j, y, k, blocksPerScanline;
            uint index;
            uint offset = 0;
            short[] output;
            uint numComponents = (uint)components.Count;
            var dataLength = width * height * numComponents;
            var data = new byte[dataLength];
            var xScaleBlockOffset = new uint[width];
            uint mask3LSB = 0xfffffff8; // used to clear the 3 LSBs

            for (i = 0; i < numComponents; i++)
            {
                component = components[(int)i];
                componentScaleX = component.ScaleX * scaleX;
                componentScaleY = component.ScaleY * scaleY;
                offset = i;
                output = component.Output;
                blocksPerScanline = (uint)((component.BlocksPerLine + 1) << 3);
                // Precalculate the "xScaleBlockOffset". Since it doesn't depend on the
                // component data, that's only necessary when "componentScaleX" changes.
                if (componentScaleX != lastComponentScaleX)
                {
                    for (x = 0; x < width; x++)
                    {
                        j = 0 | (uint)(x * componentScaleX);
                        xScaleBlockOffset[x] = (((j & mask3LSB) << 3) | (j & 7));
                    }
                    lastComponentScaleX = componentScaleX;
                }
                // linearize the blocks of the component
                for (y = 0; y < height; y++)
                {
                    j = 0 | (uint)(y * componentScaleY);
                    index = ((blocksPerScanline * (j & mask3LSB)) | ((j & 7) << 3));
                    for (x = 0; x < width; x++)
                    {
                        data[offset] = FiltersExtension.ToByte(output[index + xScaleBlockOffset[x]]);
                        offset += numComponents;
                    }
                }
            }

            // decodeTransform contains pairs of multiplier (-256..256) and additive
            var transform = _decodeTransform;

            // In PDF files, JPEG images with CMYK colour spaces are usually inverted
            // (this can be observed by extracting the raw image data).
            // Since the conversion algorithms (see below) were written primarily for
            // the PDF use-cases, attempting to use "JpegImage" to parse standalone
            // JPEG (CMYK) images may thus result in inverted images (see issue 9513).
            //
            // Unfortunately it's not (always) possible to tell, from the image data
            // alone, if it needs to be inverted. Thus in an attempt to provide better
            // out-of-box behaviour when "JpegImage" is used standalone, default to
            // inverting JPEG (CMYK) images if and only if the image data does *not*
            // come from a PDF file and no "decodeTransform" was passed by the user.
            if (!isSourcePDF && numComponents == 4 && transform == null)
            {
                // prettier-ignore
                transform = new int[] { -256, 255, -256, 255, -256, 255, -256, 255 };
            }

            if (transform != null)
            {
                for (i = 0; i < dataLength;)
                {
                    for (j = 0, k = 0; j < numComponents; j++, i++, k += 2)
                    {
                        data[i] = FiltersExtension.ToByte(((data[i] * transform[k]) >> 8) + transform[k + 1]);
                    }
                }
            }
            return data;
        }

        bool IsColorConversionNeeded
        {
            get
            {
                if (adobe != null)
                {
                    // The adobe transform marker overrides any previous setting.
                    return adobe.TransformCode != 0;
                }
                if (numComponents == 3)
                {
                    if (_colorTransform == 0)
                    {
                        // If the Adobe transform marker is not present and the image
                        // dictionary has a 'ColorTransform' entry, explicitly set to "0",
                        // then the colours should *not* be transformed.
                        return false;
                    }
                    else if (components[0].Index == /* "R" = */ 0x52
                          && components[1].Index == /* "G" = */ 0x47
                          && components[2].Index == /* "B" = */ 0x42)
                    {
                        // If the three components are indexed as RGB in ASCII
                        // then the colours should *not* be transformed.
                        return false;
                    }
                    return true;
                }
                // "this.numComponents != 3"
                if (_colorTransform == 1)
                {
                    // If the Adobe transform marker is not present and the image
                    // dictionary has a 'ColorTransform' entry, explicitly set to "1",
                    // then the colours should be transformed.
                    return true;
                }
                return false;
            }
        }

        Memory<byte> ConvertYccToRgb(Memory<byte> mdata)
        {
            var data = mdata.Span;
            byte Y, Cb, Cr;
            for (int i = 0, length = data.Length; i < length; i += 3)
            {
                Y = data[i];
                Cb = data[i + 1];
                Cr = data[i + 2];
                data[i] = FiltersExtension.ToByte(Y - 179.456 + 1.402 * Cr);
                data[i + 1] = FiltersExtension.ToByte(Y + 135.459 - 0.344 * Cb - 0.714 * Cr);
                data[i + 2] = FiltersExtension.ToByte(Y - 226.816 + 1.772 * Cb);
            }
            return mdata;
        }

        Memory<byte> ConvertYcckToRgb(Memory<byte> mdata)
        {
            var data = mdata.Span;
            byte Y, Cb, Cr, k;
            var offset = 0;
            for (int i = 0, length = data.Length; i < length; i += 4)
            {
                Y = data[i];
                Cb = data[i + 1];
                Cr = data[i + 2];
                k = data[i + 3];

                data[offset++] = FiltersExtension.ToByte(
                  -122.67195406894 +
                  Cb *
                    (-6.60635669420364e-5 * Cb +
                      0.000437130475926232 * Cr -
                      5.4080610064599e-5 * Y +
                      0.00048449797120281 * k -
                      0.154362151871126) +
                  Cr *
                    (-0.000957964378445773 * Cr +
                      0.000817076911346625 * Y -
                      0.00477271405408747 * k +
                      1.53380253221734) +
                  Y *
                    (0.000961250184130688 * Y -
                      0.00266257332283933 * k +
                      0.48357088451265) +
                  k * (-0.000336197177618394 * k + 0.484791561490776));

                data[offset++] = FiltersExtension.ToByte(
                  107.268039397724 +
                  Cb *
                    (2.19927104525741e-5 * Cb -
                      0.000640992018297945 * Cr +
                      0.000659397001245577 * Y +
                      0.000426105652938837 * k -
                      0.176491792462875) +
                  Cr *
                    (-0.000778269941513683 * Cr +
                      0.00130872261408275 * Y +
                      0.000770482631801132 * k -
                      0.151051492775562) +
                  Y *
                    (0.00126935368114843 * Y -
                      0.00265090189010898 * k +
                      0.25802910206845) +
                  k * (-0.000318913117588328 * k - 0.213742400323665));

                data[offset++] = FiltersExtension.ToByte(
                  -20.810012546947 +
                  Cb *
                    (-0.000570115196973677 * Cb -
                      2.63409051004589e-5 * Cr +
                      0.0020741088115012 * Y -
                      0.00288260236853442 * k +
                      0.814272968359295) +
                  Cr *
                    (-1.53496057440975e-5 * Cr -
                      0.000132689043961446 * Y +
                      0.000560833691242812 * k -
                      0.195152027534049) +
                  Y *
                    (0.00174418132927582 * Y -
                      0.00255243321439347 * k +
                      0.116935020465145) +
                  k * (-0.000343531996510555 * k + 0.24165260232407));
            }
            // Ensure that only the converted RGB data is returned.
            return mdata.Slice(0, offset);
        }

        Memory<byte> ConvertYcckToCmyk(Memory<byte> mdata)
        {
            var data = mdata.Span;
            byte Y, Cb, Cr;
            for (int i = 0, length = data.Length; i < length; i += 4)
            {
                Y = data[i];
                Cb = data[i + 1];
                Cr = data[i + 2];
                data[i] = FiltersExtension.ToByte(434.456 - Y - 1.402 * Cr);
                data[i + 1] = FiltersExtension.ToByte(119.541 - Y + 0.344 * Cb + 0.714 * Cr);
                data[i + 2] = FiltersExtension.ToByte(481.816 - Y - 1.772 * Cb);
                // K in data[i + 3] is unchanged
            }
            return mdata;
        }

        Memory<byte> ConvertCmykToRgb(Memory<byte> mdata)
        {
            var data = mdata.Span;
            byte c, m, y, k;
            var offset = 0;
            for (int i = 0, length = data.Length; i < length; i += 4)
            {
                c = data[i];
                m = data[i + 1];
                y = data[i + 2];
                k = data[i + 3];

                data[offset++] = FiltersExtension.ToByte(
                  255 +
                  c *
                    (-0.00006747147073602441 * c +
                      0.0008379262121013727 * m +
                      0.0002894718188643294 * y +
                      0.003264231057537806 * k -
                      1.1185611867203937) +
                  m *
                    (0.000026374107616089405 * m -
                      0.00008626949158638572 * y -
                      0.0002748769067499491 * k -
                      0.02155688794978967) +
                  y *
                    (-0.00003878099212869363 * y -
                      0.0003267808279485286 * k +
                      0.0686742238595345) -
                  k * (0.0003361971776183937 * k + 0.7430659151342254));

                data[offset++] = FiltersExtension.ToByte(
                  255 +
                  c *
                    (0.00013596372813588848 * c +
                      0.000924537132573585 * m +
                      0.00010567359618683593 * y +
                      0.0004791864687436512 * k -
                      0.3109689587515875) +
                  m *
                    (-0.00023545346108370344 * m +
                      0.0002702845253534714 * y +
                      0.0020200308977307156 * k -
                      0.7488052167015494) +
                  y *
                    (0.00006834815998235662 * y +
                      0.00015168452363460973 * k -
                      0.09751927774728933) -
                  k * (0.00031891311758832814 * k + 0.7364883807733168));

                data[offset++] = FiltersExtension.ToByte(
                  255 +
                  c *
                    (0.000013598650411385307 * c +
                      0.00012423956175490851 * m +
                      0.0004751985097583589 * y -
                      0.0000036729317476630422 * k -
                      0.05562186980264034) +
                  m *
                    (0.00016141380598724676 * m +
                      0.0009692239130725186 * y +
                      0.0007782692450036253 * k -
                      0.44015232367526463) +
                  y *
                    (5.068882914068769e-7 * y +
                      0.0017778369011375071 * k -
                      0.7591454649749609) -
                  k * (0.0003435319965105553 * k + 0.7063770186160144));
            }
            // Ensure that only the converted RGB data is returned.
            return mdata.Slice(0, offset);
        }

        public Memory<byte> GetData(int width, int height, bool forceRGB = false, bool isSourcePDF = true)
        {
            //if (
            //  typeof PDFJSDev == "undefined" ||
            //  PDFJSDev.test("!PRODUCTION || TESTING")
            //)
            //{
            //    assert(
            //      isSourcePDF == true,
            //      'JpegImage.getData: Unexpected "isSourcePDF" value for PDF files.'
            //    );
            //}
            if (numComponents > 4)
            {
                throw new JpegError("Unsupported color mode");
            }
            // Type of data: byte[](width * height * numComponents)
            var data = GetLinearizedBlockData(width, height, isSourcePDF);
            var span = data.Span;
            if (numComponents == 1 && forceRGB)
            {
                var dataLength = data.Length;
                var rgbData = new byte[dataLength * 3];
                var offset = 0;
                for (var i = 0; i < dataLength; i++)
                {
                    var grayColor = span[i];
                    rgbData[offset++] = grayColor;
                    rgbData[offset++] = grayColor;
                    rgbData[offset++] = grayColor;
                }
                return rgbData;
            }
            else if (numComponents == 3 && IsColorConversionNeeded)
            {
                return ConvertYccToRgb(data);
            }
            else if (numComponents == 4)
            {
                if (IsColorConversionNeeded)
                {
                    if (forceRGB)
                    {
                        return ConvertYcckToRgb(data);
                    }
                    return ConvertYcckToCmyk(data);
                }
                else if (forceRGB)
                {
                    return ConvertCmykToRgb(data);
                }
            }
            return data;
        }
    }

    internal class JpegOptions
    {
        internal int[] DecodeTransform;
        internal int? ColorTransform;

        public JpegOptions(int[] decodeTransform, int? colorTransform)
        {
            DecodeTransform = decodeTransform;
            ColorTransform = colorTransform;
        }
    }

    internal class ImageComponent
    {
        internal byte Index;
        internal short[] Output;
        internal double ScaleX;
        internal double ScaleY;
        internal int BlocksPerLine;
        internal int BlocksPerColumn;

        public ImageComponent(byte index, short[] output, double scaleX, double scaleY, int blocksPerLine, int blocksPerColumn)
        {
            Index = index;
            Output = output;
            ScaleX = scaleX;
            ScaleY = scaleY;
            BlocksPerLine = blocksPerLine;
            BlocksPerColumn = blocksPerColumn;
        }
    }

    internal class Frame
    {
        internal bool Extended;
        internal bool Progressive;
        internal byte Precision;
        internal ushort ScanLines;
        internal ushort SamplesPerLine;
        internal List<Component> Components;
        internal Dictionary<byte, int> ComponentIds;
        internal int MaxH;
        internal int MaxV;
        internal int McusPerLine;
        internal int McusPerColumn;

        public Frame()
        {
        }
    }

    internal class Node
    {
        internal Dictionary<int, object> Children;
        internal int Index;

        public Node(Dictionary<int, object> children, int index)
        {
            Children = children;
            Index = index;
        }
    }

    internal class Component
    {
        internal int H;
        internal int V;
        internal byte QuantizationId;
        internal ushort[] QuantizationTable;
        internal Dictionary<int, object> HuffmanTableDC;
        internal Dictionary<int, object> HuffmanTableAC;
        internal short[] BlockData;
        internal int BlocksPerLine;
        internal int BlocksPerColumn;
        internal int Pred;
        internal byte Index;

        public Component(int h, int v, byte quantizationId, ushort[] quantizationTable)
        {
            H = h;
            V = v;
            QuantizationId = quantizationId;
            QuantizationTable = quantizationTable;
        }
    }

    internal class FileMarker
    {
        internal string Invalid;
        internal ushort Marker;
        internal int Offset;

        public FileMarker(string invalid, ushort marker, int offset)
        {
            Invalid = invalid;
            Marker = marker;
            Offset = offset;
        }
    }

    internal class JFIF
    {
        internal PdfVersion Version;
        internal byte DensityUnits;
        internal int XDensity;
        internal int YDensity;
        internal byte ThumbWidth;
        internal byte ThumbHeight;
        internal Memory<byte> ThumbData;

        public JFIF(PdfVersion version, byte densityUnits, int xDensity, int yDensity, byte thumbWidth, byte thumbHeight, Memory<byte> thumbData)
        {
            Version = version;
            DensityUnits = densityUnits;
            XDensity = xDensity;
            YDensity = yDensity;
            ThumbWidth = thumbWidth;
            ThumbHeight = thumbHeight;
            ThumbData = thumbData;
        }
    }

    internal class Adobe
    {
        internal int Version;
        internal int Flags0;
        internal int Flags1;
        internal byte TransformCode;

        public Adobe(int version, int flags0, int flags1, byte transformCode)
        {
            Version = version;
            Flags0 = flags0;
            Flags1 = flags1;
            TransformCode = transformCode;
        }
    }
}