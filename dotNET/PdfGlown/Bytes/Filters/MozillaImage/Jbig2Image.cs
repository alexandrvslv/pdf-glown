/* Copyright 2012 Mozilla Foundation
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using PdfGlown.Bytes.Filters.CCITT;
using PdfGlown.Util.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfGlown.Bytes.Filters.JBig
{

    internal class Jbig2Image
    {
        // 7.3 Segment types
        static readonly string[] SegmentTypes = [
           "SymbolDictionary",
            null,
            null,
            null,
            "IntermediateTextRegion",
            null,
            "ImmediateTextRegion",
            "ImmediateLosslessTextRegion",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            "PatternDictionary",
            null,
            null,
            null,
            "IntermediateHalftoneRegion",
            null,
            "ImmediateHalftoneRegion",
            "ImmediateLosslessHalftoneRegion",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            "IntermediateGenericRegion",
            null,
            "ImmediateGenericRegion",
            "ImmediateLosslessGenericRegion",
            "IntermediateGenericRefinementRegion",
            null,
            "ImmediateGenericRefinementRegion",
            "ImmediateLosslessGenericRefinementRegion",
            null,
            null,
            null,
            null,
            "PageInformation",
            "EndOfPage",
            "EndOfStripe",
            "EndOfFile",
            "Profiles",
            "Tables",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            "Extension"
        ];

        static readonly List<Point[]> CodingTemplates =
        [
            [
                new Point(x: -1, y: -2),
                new Point(x: 0, y: -2),
                new Point(x: 1, y: -2),
                new Point(x: -2, y: -1),
                new Point(x: -1, y: -1),
                new Point(x: 0, y: -1),
                new Point(x: 1, y: -1),
                new Point(x: 2, y: -1),
                new Point(x: -4, y: 0),
                new Point(x: -3, y: 0),
                new Point(x: -2, y: 0),
                new Point(x: -1, y: 0),
            ],
            [
                new Point(x: -1, y: -2),
                new Point(x: 0, y: -2),
                new Point(x: 1, y: -2),
                new Point(x: 2, y: -2),
                new Point(x: -2, y: -1),
                new Point(x: -1, y: -1),
                new Point(x: 0, y: -1),
                new Point(x: 1, y: -1),
                new Point(x: 2, y: -1),
                new Point(x: -3, y: 0),
                new Point(x: -2, y: 0),
                new Point(x: -1, y: 0),
            ],
            [
                new Point(x: -1, y: -2),
                new Point(x: 0, y: -2),
                new Point(x: 1, y: -2),
                new Point(x: -2, y: -1),
                new Point(x: -1, y: -1),
                new Point(x: 0, y: -1),
                new Point(x: 1, y: -1),
                new Point(x: -2, y: 0),
                new Point(x: -1, y: 0),
            ],
            [
                new Point(x: -3, y: -1),
                new Point(x: -2, y: -1),
                new Point(x: -1, y: -1),
                new Point(x: 0, y: -1),
                new Point(x: 1, y: -1),
                new Point(x: -4, y: 0),
                new Point(x: -3, y: 0),
                new Point(x: -2, y: 0),
                new Point(x: -1, y: 0)
            ]
        ];

        static readonly Refinement[] RefinementTemplates =
        [
            new Refinement(
                coding: [
                    new Point(x: 0, y: -1),
                    new Point(x: 1, y: -1),
                    new Point(x: -1, y: 0),
                ],
                reference: [
                    new Point(x: 0, y: -1),
                    new Point(x: 1, y: -1),
                    new Point(x: -1, y: 0),
                    new Point(x: 0, y: 0),
                    new Point(x: 1, y: 0),
                    new Point(x: -1, y: 1),
                    new Point(x: 0, y: 1),
                    new Point(x: 1, y: 1),
                ]),
            new Refinement(
                coding: [
                    new Point(x: -1, y: -1),
                    new Point(x: 0, y: -1),
                    new Point(x: 1, y: -1),
                    new Point(x: -1, y: 0),
                ],
                reference: [
                    new Point(x: 0, y: -1),
                    new Point(x: -1, y: 0),
                    new Point(x: 0, y: 0),
                    new Point(x: 1, y: 0),
                    new Point(x: 0, y: 1),
                    new Point(x: 1, y: 1),
                ]),
        ];

        // See 6.2.5.7 Decoding the bitmap.
        static readonly int[] ReusedContexts = [
            0x9b25, // 10011 0110010 0101
            0x0795, // 0011 110010 101
            0x00e5, // 001 11001 01
            0x0195, // 011001 0101
        ];

        static readonly int[] RefinementReusedContexts = [
            0x0020, // '000' + '0' (coding) + '00010000' + '0' (reference)
            0x0008, // '0000' + '001000'
        ];

        static readonly Dictionary<int, HuffmanTable> StandardTablesCache = new();
        public static readonly int RegionSegmentInformationFieldLength = 17;
        private int width;
        private int height;

        // Utility data structures
        internal class ContextCache : Dictionary<string, sbyte[]>
        {
            public ContextCache() : base(StringComparer.Ordinal)
            { }

            public sbyte[] GetContexts(string id)
            {
                if (TryGetValue(id, out var context))
                    return context;

                return this[id] = new sbyte[1 << 16];
            }
        }

        internal class DecodingContext
        {
            private ContextCache cache;
            private ArithmeticDecoder decoder;
            internal Memory<byte> Data;

            public DecodingContext(Memory<byte> data)
            {
                Data = data;
            }

            public ArithmeticDecoder Decoder => decoder ??= new ArithmeticDecoder(Data);

            public ContextCache ContextCache => cache ??= new ContextCache();
        }

        // Annex A. Arithmetic Integer Decoding Procedure
        // A.2 Procedure for decoding values
        static int? DecodeInteger(ContextCache contextCache, string procedure, ArithmeticDecoder decoder)
        {
            var contexts = contextCache.GetContexts(procedure);
            var prev = 1;

            int ReadBits(int Length)
            {
                var v = 0;
                for (var i = 0; i < Length; i++)
                {
                    var bit = decoder.ReadBit(contexts, prev);
                    prev = prev < 256
                        ? (prev << 1) | bit
                        : (((prev << 1) | bit) & 511) | 256;
                    v = (v << 1) | bit;
                }
                return (int)((uint)v >> 0);
            }

            var sign = ReadBits(1);
            // prettier-ignore
            /* eslint-disable no-nested-ternary */
            var value = ReadBits(1) != 0 ?
                          (ReadBits(1) != 0 ?
                            (ReadBits(1) != 0 ?
                              (ReadBits(1) != 0 ?
                                (ReadBits(1) != 0 ?
                                  (ReadBits(32) + 4436) :
                                ReadBits(12) + 340) :
                              ReadBits(8) + 84) :
                            ReadBits(6) + 20) :
                          ReadBits(4) + 4) :
                        ReadBits(2);
            /* eslint-enable no-nested-ternary */
            if (sign == 0)
            {
                return value;
            }
            else if (value > 0)
            {
                return -value;
            }
            return null;
        }

        // A.3 The IAID decoding procedure
        static int DecodeIAID(ContextCache contextCache, ArithmeticDecoder decoder, int codeLength)
        {
            var contexts = contextCache.GetContexts("IAID");

            var prev = 1;
            for (var i = 0; i < codeLength; i++)
            {
                var bit = decoder.ReadBit(contexts, prev);
                prev = (prev << 1) | bit;
            }
            if (codeLength < 31)
            {
                return prev & ((1 << codeLength) - 1);
            }
            return prev & 0x7fffffff;
        }

        static Dictionary<int, byte[]> DecodeBitmapTemplate0(int width, int height, DecodingContext decodingContext)
        {
            var decoder = decodingContext.Decoder;
            var contexts = decodingContext.ContextCache.GetContexts("GB");
            int contextLabel;
            int i, j, pixel;
            byte[] row, row1, row2;
            var bitmap = new Dictionary<int, byte[]>();

            // ...ooooo....
            // ..ooooooo... Context template for current pixel (X)
            // .ooooX...... (concatenate values of 'o'-pixels to get contextLabel)
            var OLD_PIXEL_MASK = 0x7bf7; // 01111 0111111 0111

            for (i = 0; i < height; i++)
            {
                row = bitmap[i] = new byte[width];
                row1 = i < 1 ? row : bitmap[i - 1];
                row2 = i < 2 ? row : bitmap[i - 2];

                // At the beginning of each row:
                // Fill contextLabel with pixels that are above/right of (X)
                contextLabel =
                  (row2[0] << 13) |
                  ((row2.Length > 1 ? row2[1] : 0) << 12) |
                  ((row2.Length > 2 ? row2[2] : 0) << 11) |
                  (row1[0] << 7) |
                  ((row1.Length > 1 ? row1[1] : 0) << 6) |
                  ((row1.Length > 2 ? row1[2] : 0) << 5) |
                  ((row1.Length > 3 ? row1[3] : 0) << 4);

                for (j = 0; j < width; j++)
                {
                    pixel = decoder.ReadBit(contexts, contextLabel);
                    row[j] = FiltersExtension.ToByte(pixel);
                    // At each pixel: Clear contextLabel pixels that are shifted
                    // out of the context, then add new ones.
                    contextLabel =
                      ((contextLabel & OLD_PIXEL_MASK) << 1) |
                      (j + 3 < width ? row2[j + 3] << 11 : 0) |
                      (j + 4 < width ? row1[j + 4] << 4 : 0) |
                      pixel;
                }
            }

            return bitmap;
        }

        // 6.2 Generic Region Decoding Procedure
        static Dictionary<int, byte[]> DecodeBitmap(bool mmr, int width, int height,
          int templateIndex, bool prediction, Dictionary<int, byte[]> skip, List<Point> at,
          DecodingContext decodingContext)
        {
            if (mmr)
            {
                var input = new ByteStream(decodingContext.Data);
                return DecodeMMRBitmap(input, width, height, false);
            }

            // Use optimized version for the most common case
            if (
              templateIndex == 0 &&
              skip == null &&
              !prediction &&
              at.Count == 4 &&
              at[0].X == 3 &&
              at[0].Y == -1 &&
              at[1].X == -3 &&
              at[1].Y == -1 &&
              at[2].X == 2 &&
              at[2].Y == -2 &&
              at[3].X == -2 &&
              at[3].Y == -2
            )
            {
                return DecodeBitmapTemplate0(width, height, decodingContext);
            }

            var useskip = skip != null;
            var template = CodingTemplates[templateIndex].Concat(at).ToList();

            // Sorting is non-standard, and it is not required. But sorting increases
            // the number of template bits that can be reused from the previous
            // contextLabel in the main loop.
            template.Sort((a, b) =>
            {
                var y = a.Y.CompareTo(b.Y);
                return y == 0 ? a.X.CompareTo(b.X) : y;
            });

            var templateLength = template.Count;
            var templateX = new sbyte[templateLength];
            var templateY = new sbyte[templateLength];
            var changingTemplateEntries = new List<int>();
            int reuseMask = 0, minX = 0, maxX = 0, minY = 0;
            int c, k;

            for (k = 0; k < templateLength; k++)
            {
                templateX[k] = template[k].X;
                templateY[k] = template[k].Y;
                minX = Math.Min(minX, template[k].X);
                maxX = Math.Max(maxX, template[k].X);
                minY = Math.Min(minY, template[k].Y);
                // Check if the template pixel appears in two consecutive context labels,
                // so it can be reused. Otherwise, we add it to the list of changing
                // template entries.
                if (k < templateLength - 1 &&
                    template[k].Y == template[k + 1].Y &&
                    template[k].X == template[k + 1].X - 1)
                {
                    reuseMask |= 1 << (templateLength - 1 - k);
                }
                else
                {
                    changingTemplateEntries.Add(k);
                }
            }
            var changingEntriesLength = changingTemplateEntries.Count;

            var changingTemplateX = new sbyte[changingEntriesLength];
            var changingTemplateY = new sbyte[changingEntriesLength];
            var changingTemplateBit = new ushort[changingEntriesLength];
            for (c = 0; c < changingEntriesLength; c++)
            {
                k = changingTemplateEntries[c];
                changingTemplateX[c] = template[k].X;
                changingTemplateY[c] = template[k].Y;
                changingTemplateBit[c] = (ushort)(1 << (templateLength - 1 - k));
            }

            // Get the safe bounding box edges from the width, height, minX, maxX, minY
            var sbb_left = -minX;
            var sbb_top = -minY;
            var sbb_right = width - maxX;

            var pseudoPixelContext = ReusedContexts[templateIndex];
            var row = new byte[width];
            var bitmap = new Dictionary<int, byte[]>();

            var decoder = decodingContext.Decoder;
            var contexts = decodingContext.ContextCache.GetContexts("GB");

            int ltp = 0, j, i0, j0, contextLabel = 0, bit, shift;
            for (var i = 0; i < height; i++)
            {
                if (prediction)
                {
                    var sltp = decoder.ReadBit(contexts, pseudoPixelContext);
                    ltp ^= sltp;
                    if (ltp != 0)
                    {
                        bitmap.Add(bitmap.Count, row); // duplicate previous row
                        continue;
                    }
                }
                row = (byte[])row.Clone();
                bitmap.Add(bitmap.Count, row);
                for (j = 0; j < width; j++)
                {
                    if (useskip && skip[i][j] != 0)
                    {
                        row[j] = 0;
                        continue;
                    }
                    // Are we in the middle of a scanline, so we can reuse contextLabel
                    // bits?
                    if (j >= sbb_left && j < sbb_right && i >= sbb_top)
                    {
                        // If yes, we can just shift the bits that are reusable and only
                        // fetch the remaining ones.
                        contextLabel = (contextLabel << 1) & reuseMask;
                        for (k = 0; k < changingEntriesLength; k++)
                        {
                            i0 = i + changingTemplateY[k];
                            j0 = j + changingTemplateX[k];

                            if (bitmap.TryGetValue(i0, out var bits) && j0 < bits.Length && (bit = bits[j0]) != 0)
                            {
                                bit = changingTemplateBit[k];
                                contextLabel |= bit;
                            }
                        }
                    }
                    else
                    {
                        // compute the contextLabel from scratch
                        contextLabel = 0;
                        shift = templateLength - 1;
                        for (k = 0; k < templateLength; k++, shift--)
                        {
                            j0 = j + templateX[k];
                            if (j0 >= 0 && j0 < width)
                            {
                                i0 = i + templateY[k];
                                if (i0 >= 0)
                                {
                                    if (bitmap.TryGetValue(i0, out var bits) && j0 < bits.Length && (bit = bits[j0]) != 0)
                                    {
                                        contextLabel |= bit << shift;
                                    }
                                }
                            }
                        }
                    }
                    var pixel = decoder.ReadBit(contexts, contextLabel);
                    row[j] = FiltersExtension.ToByte(pixel);
                }
            }
            return bitmap;
        }

        // 6.3.2 Generic Refinement Region Decoding Procedure
        static Dictionary<int, byte[]> DecodeRefinement(int width, int height, int templateIndex,
            Dictionary<int, byte[]> referenceBitmap, int offsetX, int offsetY,
            bool prediction, List<Point> at, DecodingContext decodingContext)
        {
            var codingTemplate = RefinementTemplates[templateIndex].Coding;
            if (templateIndex == 0)
            {
                codingTemplate = codingTemplate.Concat(new Point[] { at[0] }).ToArray();
            }
            var codingTemplateLength = codingTemplate.Length;
            var codingTemplateX = new int[codingTemplateLength];
            var codingTemplateY = new int[codingTemplateLength];
            int k;
            for (k = 0; k < codingTemplateLength; k++)
            {
                codingTemplateX[k] = codingTemplate[k].X;
                codingTemplateY[k] = codingTemplate[k].Y;
            }

            var referenceTemplate = RefinementTemplates[templateIndex].Reference;
            if (templateIndex == 0)
            {
                referenceTemplate = referenceTemplate.Concat(new Point[] { at[1] }).ToArray();
            }
            var referenceTemplateLength = referenceTemplate.Length;
            var referenceTemplateX = new int[referenceTemplateLength];
            var referenceTemplateY = new int[referenceTemplateLength];
            for (k = 0; k < referenceTemplateLength; k++)
            {
                referenceTemplateX[k] = referenceTemplate[k].X;
                referenceTemplateY[k] = referenceTemplate[k].Y;
            }
            var referenceWidth = referenceBitmap[0].Length;
            var referenceHeight = referenceBitmap.Count;

            var pseudoPixelContext = RefinementReusedContexts[templateIndex];
            var bitmap = new Dictionary<int, byte[]>();

            var decoder = decodingContext.Decoder;
            var contexts = decodingContext.ContextCache.GetContexts("GR");

            var ltp = 0;
            for (var i = 0; i < height; i++)
            {
                if (prediction)
                {
                    var sltp = decoder.ReadBit(contexts, pseudoPixelContext);
                    ltp ^= sltp;
                    if (ltp != 0)
                    {
                        throw new Jbig2Error("prediction is not supported");
                    }
                }
                var row = new byte[width];
                bitmap.Add(bitmap.Count, row);
                for (var j = 0; j < width; j++)
                {
                    int i0, j0;
                    var contextLabel = 0;
                    for (k = 0; k < codingTemplateLength; k++)
                    {
                        i0 = i + codingTemplateY[k];
                        j0 = j + codingTemplateX[k];
                        if (i0 < 0 || j0 < 0 || j0 >= width)
                        {
                            contextLabel <<= 1; // out of bound pixel
                        }
                        else
                        {
                            contextLabel = (contextLabel << 1) | bitmap[i0][j0];
                        }
                    }
                    for (k = 0; k < referenceTemplateLength; k++)
                    {
                        i0 = i + referenceTemplateY[k] - offsetY;
                        j0 = j + referenceTemplateX[k] - offsetX;
                        if (i0 < 0 || i0 >= referenceHeight || j0 < 0 || j0 >= referenceWidth)
                        {
                            contextLabel <<= 1; // out of bound pixel
                        }
                        else
                        {
                            contextLabel = (contextLabel << 1) | referenceBitmap[i0][j0];
                        }
                    }
                    var pixel = decoder.ReadBit(contexts, contextLabel);
                    row[j] = FiltersExtension.ToByte(pixel);
                }
            }

            return bitmap;
        }

        // 6.5.5 Decoding the symbol dictionary
        static List<Dictionary<int, byte[]>> DecodeSymbolDictionary(bool huffman, bool refinement, List<Dictionary<int, byte[]>> symbols, int numberOfNewSymbols,
           int numberOfExportedSymbols, HuffmanTables huffmanTables, int templateIndex, List<Point> at, int refinementTemplateIndex,
           List<Point> refinementAt, DecodingContext decodingContext, ByteStream huffmanInput)
        {
            if (huffman && refinement)
            {
                throw new Jbig2Error("symbol refinement with Huffman is not supported");
            }

            var newSymbols = new List<Dictionary<int, byte[]>>();
            var currentHeight = 0;
            var symbolCodeLength = FiltersExtension.Log2(symbols.Count + numberOfNewSymbols);
            int i = 0;
            var decoder = decodingContext.Decoder;
            var contextCache = decodingContext.ContextCache;
            HuffmanTable tableB1 = null;
            List<int> symbolWidths = null;
            if (huffman)
            {
                tableB1 = GetStandardTable(1); // standard table B.1
                symbolWidths = new List<int>();
                symbolCodeLength = Math.Max(symbolCodeLength, 1); // 6.5.8.2.3
            }

            while (newSymbols.Count < numberOfNewSymbols)
            {
                var deltaHeight = (int)(huffman
                  ? huffmanTables.TableDeltaHeight.Decode(huffmanInput)
                  : DecodeInteger(contextCache, "IADH", decoder)); // 6.5.6
                currentHeight += deltaHeight;
                int currentWidth = 0,
                  totalWidth = 0;
                var firstSymbol = huffman ? symbolWidths.Count : 0;
                while (true)
                {
                    var deltaWidth = huffman
                      ? huffmanTables.TableDeltaWidth.Decode(huffmanInput)
                      : DecodeInteger(contextCache, "IADW", decoder); // 6.5.7
                    if (deltaWidth == null)
                    {
                        break; // OOB
                    }
                    currentWidth += (int)deltaWidth;
                    totalWidth += currentWidth;
                    Dictionary<int, byte[]> bitmap;
                    if (refinement)
                    {
                        // 6.5.8.2 Refinement/aggregate-coded symbol bitmap
                        var numberOfInstances = DecodeInteger(contextCache, "IAAI", decoder);
                        if (numberOfInstances > 1)
                        {
                            bitmap = DecodeTextRegion(
                              huffman,
                              refinement,
                              currentWidth,
                              currentHeight,
                              0,
                              numberOfInstances,
                              1, // strip size
                              symbols.Concat(newSymbols).ToList(),
                              symbolCodeLength,
                              false, // transposed
                              0, // ds offset
                              1, // top left 7.4.3.1.1
                              0, // OR operator
                              huffmanTables,
                              refinementTemplateIndex,
                              refinementAt,
                              decodingContext,
                              0,
                              huffmanInput
                            );
                        }
                        else
                        {
                            var symbolId = DecodeIAID(contextCache, decoder, symbolCodeLength);
                            var rdx = (int)DecodeInteger(contextCache, "IARDX", decoder); // 6.4.11.3
                            var rdy = (int)DecodeInteger(contextCache, "IARDY", decoder); // 6.4.11.4
                            var symbol =
                              symbolId < symbols.Count
                                ? symbols[symbolId]
                                : newSymbols[symbolId - symbols.Count];
                            bitmap = DecodeRefinement(currentWidth, currentHeight, refinementTemplateIndex, symbol, rdx, rdy, false, refinementAt, decodingContext);
                        }
                        newSymbols.Add(bitmap);
                    }
                    else if (huffman)
                    {
                        // Store only symbol width and decode a collective bitmap when the
                        // height class is done.
                        symbolWidths.Add(currentWidth);
                    }
                    else
                    {
                        // 6.5.8.1 Direct-coded symbol bitmap
                        bitmap = DecodeBitmap(false, currentWidth, currentHeight, templateIndex, false, null, at, decodingContext);
                        newSymbols.Add(bitmap);
                    }
                }
                if (huffman && !refinement)
                {
                    // 6.5.9 Height class collective bitmap
                    var bitmapSize = huffmanTables.TableBitmapSize.Decode(huffmanInput);
                    huffmanInput.ByteAlign();
                    Dictionary<int, byte[]> collectiveBitmap;
                    if (bitmapSize == null || bitmapSize == 0)
                    {
                        // Uncompressed collective bitmap
                        collectiveBitmap = ReadUncompressedBitmap(huffmanInput, totalWidth, currentHeight);
                    }
                    else
                    {
                        // MMR collective bitmap
                        var originalL = (int)huffmanInput.Length;
                        var bitmapL = (int)bitmapSize;
                        huffmanInput.SetLength(bitmapL);
                        collectiveBitmap = DecodeMMRBitmap(huffmanInput, totalWidth, currentHeight, false);
                        huffmanInput.SetLength(originalL);
                        huffmanInput.Position += bitmapL;
                    }
                    var numberOfSymbolsDecoded = symbolWidths.Count;
                    if (firstSymbol == numberOfSymbolsDecoded - 1)
                    {
                        // collectiveBitmap is a single symbol.
                        newSymbols.Add(collectiveBitmap);
                    }
                    else
                    {
                        // Divide collectiveBitmap into symbols.
                        int y, xMin = 0, xMax, bitmapWidth;
                        Dictionary<int, byte[]> symbolBitmap = null;
                        for (i = firstSymbol; i < numberOfSymbolsDecoded; i++)
                        {
                            bitmapWidth = symbolWidths[i];
                            xMax = xMin + bitmapWidth;
                            symbolBitmap = new Dictionary<int, byte[]>();
                            for (y = 0; y < currentHeight; y++)
                            {
                                symbolBitmap.Add(symbolBitmap.Count, collectiveBitmap[y].SubArray(xMin, xMax));
                            }
                            newSymbols.Add(symbolBitmap);
                            xMin = xMax;
                        }
                    }
                }
            }

            // 6.5.10 Exported symbols
            var exportedSymbols = new List<Dictionary<int, byte[]>>();
            var flags = new List<bool>();
            var currentFlag = false;
            var totalSymbolsLength = symbols.Count + numberOfNewSymbols;
            while (flags.Count < totalSymbolsLength)
            {
                var runLength = (int)(huffman
                  ? tableB1.Decode(huffmanInput)
                  : DecodeInteger(contextCache, "IAEX", decoder));
                while (runLength-- != 0)
                {
                    flags.Add(currentFlag);
                }
                currentFlag = !currentFlag;
            }

            for (var ii = symbols.Count; i < ii; i++)
            {
                if (flags[i])
                {
                    exportedSymbols.Add(symbols[i]);
                }
            }
            for (var j = 0; j < numberOfNewSymbols; i++, j++)
            {
                if (flags[i])
                {
                    exportedSymbols.Add(newSymbols[j]);
                }
            }
            return exportedSymbols;
        }

        static Dictionary<int, byte[]> DecodeTextRegion(bool huffman, bool refinement, int width, int height,
          byte defaultPixelValue, int? numberOfSymbolInstances, int stripSize, List<Dictionary<int, byte[]>> inputSymbols, int symbolCodeLength,
          bool transposed, int dsOffset, int referenceCorner, int combinationOperator, HuffmanTables huffmanTables,
          int refinementTemplateIndex, List<Point> refinementAt, DecodingContext decodingContext, int logStripSize, ByteStream huffmanInput)
        {
            if (huffman && refinement)
            {
                throw new Jbig2Error("refinement with Huffman is not supported");
            }

            // Prepare bitmap
            var bitmap = new Dictionary<int, byte[]>();
            int i;
            byte[] row;
            for (i = 0; i < height; i++)
            {
                row = new byte[width];
                if (defaultPixelValue != 0)
                {
                    Array.Fill(row, defaultPixelValue);
                }
                bitmap.Add(bitmap.Count, row);
            }

            var decoder = decodingContext.Decoder;
            var contextCache = decodingContext.ContextCache;

            var stripT = (int)(huffman
              ? -huffmanTables.tableDeltaT.Decode(huffmanInput)
              : -DecodeInteger(contextCache, "IADT", decoder)); // 6.4.6
            var firstS = 0;
            i = 0;
            while (i < numberOfSymbolInstances)
            {
                var deltaT = (int)(huffman
                  ? huffmanTables.tableDeltaT.Decode(huffmanInput)
                  : DecodeInteger(contextCache, "IADT", decoder)); // 6.4.6
                stripT += deltaT;

                var deltaFirstS = (int)(huffman
                  ? huffmanTables.TableFirstS.Decode(huffmanInput)
                  : DecodeInteger(contextCache, "IAFS", decoder)); // 6.4.7
                firstS += deltaFirstS;
                var currentS = firstS;
                do
                {
                    var currentT = 0; // 6.4.9
                    if (stripSize > 1)
                    {
                        currentT = (int)(huffman
                          ? (int)huffmanInput.ReadBits(logStripSize)
                          : DecodeInteger(contextCache, "IAIT", decoder));
                    }
                    var t = stripSize * stripT + currentT;
                    var symbolId = (int)(huffman
                      ? huffmanTables.SymbolIDTable.Decode(huffmanInput)
                      : DecodeIAID(contextCache, decoder, symbolCodeLength));
                    var applyRefinement =
                      refinement &&
                      (huffman
                        ? huffmanInput.ReadBit()
                        : DecodeInteger(contextCache, "IARI", decoder)) != 0;
                    var symbolBitmap = inputSymbols[symbolId];
                    var symbolWidth = symbolBitmap[0].Length;
                    var symbolHeight = symbolBitmap.Count;
                    if (applyRefinement)
                    {
                        var rdw = (int)DecodeInteger(contextCache, "IARDW", decoder); // 6.4.11.1
                        var rdh = (int)DecodeInteger(contextCache, "IARDH", decoder); // 6.4.11.2
                        var rdx = (int)DecodeInteger(contextCache, "IARDX", decoder); // 6.4.11.3
                        var rdy = (int)DecodeInteger(contextCache, "IARDY", decoder); // 6.4.11.4
                        symbolWidth += rdw;
                        symbolHeight += rdh;
                        symbolBitmap = DecodeRefinement(
                          symbolWidth,
                          symbolHeight,
                          refinementTemplateIndex,
                          symbolBitmap,
                          (rdw >> 1) + rdx,
                          (rdh >> 1) + rdy,
                          false,
                          refinementAt,
                          decodingContext
                        );
                    }
                    var offsetT = t - ((referenceCorner & 1) != 0 ? 0 : symbolHeight - 1);
                    var offsetS = currentS - ((referenceCorner & 2) != 0 ? symbolWidth - 1 : 0);
                    int s2, t2;
                    byte[] symbolRow = null;
                    if (transposed)
                    {
                        // Place Symbol Bitmap from T1,S1
                        for (s2 = 0; s2 < symbolHeight; s2++)
                        {
                            if (!bitmap.TryGetValue(offsetS + s2, out row))
                            {
                                continue;
                            }
                            symbolRow = symbolBitmap[s2];
                            // To ignore Parts of Symbol bitmap which goes
                            // outside bitmap region
                            var maxWidth = Math.Min(width - offsetT, symbolWidth);
                            switch (combinationOperator)
                            {
                                case 0: // OR
                                    for (t2 = 0; t2 < maxWidth; t2++)
                                    {
                                        row[offsetT + t2] |= symbolRow[t2];
                                    }
                                    break;
                                case 2: // XOR
                                    for (t2 = 0; t2 < maxWidth; t2++)
                                    {
                                        row[offsetT + t2] ^= symbolRow[t2];
                                    }
                                    break;
                                default:
                                    throw new Jbig2Error(
                                      "operator ${combinationOperator} is not supported"
                                    );
                            }
                        }
                        currentS += symbolHeight - 1;
                    }
                    else
                    {
                        for (t2 = 0; t2 < symbolHeight; t2++)
                        {
                            if (!bitmap.TryGetValue(offsetT + t2, out row))
                            {
                                continue;
                            }
                            symbolRow = symbolBitmap[t2];
                            switch (combinationOperator)
                            {
                                case 0: // OR
                                    for (s2 = 0; s2 < symbolWidth; s2++)
                                    {
                                        row[offsetS + s2] |= symbolRow[s2];
                                    }
                                    break;
                                case 2: // XOR
                                    for (s2 = 0; s2 < symbolWidth; s2++)
                                    {
                                        row[offsetS + s2] ^= symbolRow[s2];
                                    }
                                    break;
                                default:
                                    throw new Jbig2Error(
                                      "operator ${combinationOperator} is not supported"
                                    );
                            }
                        }
                        currentS += symbolWidth - 1;
                    }
                    i++;
                    var deltaS = huffman
                      ? huffmanTables.TableDeltaS.Decode(huffmanInput)
                      : DecodeInteger(contextCache, "IADS", decoder); // 6.4.8
                    if (deltaS == null)
                    {
                        break; // OOB
                    }
                    currentS += (int)deltaS + dsOffset;
                } while (true);
            }
            return bitmap;
        }

        static List<Dictionary<int, byte[]>> DecodePatternDictionary(bool mmr, int patternWidth, int patternHeight, int maxPatternIndex, int template, DecodingContext decodingContext)
        {
            var at = new List<Point>();
            if (!mmr)
            {
                at.Add(new Point(x: (sbyte)-patternWidth, y: 0));
                if (template == 0)
                {
                    at.Add(new Point(x: -3, y: -1));
                    at.Add(new Point(x: 2, y: -2));
                    at.Add(new Point(x: -2, y: -2));
                }
            }
            var collectiveWidth = (maxPatternIndex + 1) * patternWidth;
            var collectiveBitmap = DecodeBitmap(mmr, collectiveWidth, patternHeight, template, false, null, at, decodingContext);
            // Divide collective bitmap into patterns.
            var patterns = new List<Dictionary<int, byte[]>>();
            for (var i = 0; i <= maxPatternIndex; i++)
            {
                var patternBitmap = new Dictionary<int, byte[]>();
                var xMin = patternWidth * i;
                var xMax = xMin + patternWidth;
                for (var y = 0; y < patternHeight; y++)
                {
                    patternBitmap.Add(patternBitmap.Count, collectiveBitmap[y].SubArray(xMin, xMax));
                }
                patterns.Add(patternBitmap);
            }
            return patterns;
        }

        static Dictionary<int, byte[]> DecodeHalftoneRegion(bool mmr, List<Dictionary<int, byte[]>> patterns, int template, int regionWidth,
           int regionHeight, byte defaultPixelValue, bool enableSkip, int combinationOperator, int gridWidth,
           int gridHeight, int gridOffsetX, int gridOffsetY, int gridVectorX, int gridVectorY,
           DecodingContext decodingContext)
        {
            Dictionary<int, byte[]> skip = null;
            if (enableSkip)
            {
                throw new Jbig2Error("skip is not supported");
            }
            if (combinationOperator != 0)
            {
                throw new Jbig2Error(
                  "operator " +
                    combinationOperator +
                    " is not supported in halftone region"
                );
            }

            // Prepare bitmap.
            var regionBitmap = new Dictionary<int, byte[]>();
            int i, j;
            byte[] row;
            for (i = 0; i < regionHeight; i++)
            {
                row = new byte[regionWidth];
                if (defaultPixelValue != 0)
                {
                    for (j = 0; j < regionWidth; j++)
                    {
                        row[j] = defaultPixelValue;
                    }
                }
                regionBitmap.Add(regionBitmap.Count, row);
            }

            var numberOfPatterns = patterns.Count;
            var pattern0 = patterns[0];
            int patternWidth = pattern0[0].Length,
              patternHeight = pattern0.Count;
            var bitsPerValue = FiltersExtension.Log2(numberOfPatterns);
            var at = new List<Point>();
            if (!mmr)
            {
                at.Add(new Point(x: (sbyte)(template <= 1 ? 3 : 2), y: -1));
                if (template == 0)
                {
                    at.Add(new Point(x: -3, y: -1));
                    at.Add(new Point(x: 2, y: -2));
                    at.Add(new Point(x: -2, y: -2));
                }
            }
            // Annex C. Gray-scale Image Decoding Procedure.
            var grayScaleBitPlanes = new Dictionary<int, Dictionary<int, byte[]>>();
            ByteStream mmrInput = null;
            Dictionary<int, byte[]> bitmap;
            if (mmr)
            {
                // MMR bit planes are in one continuous stream. Only EOFB codes indicate
                // the end of each bitmap, so EOFBs must be decoded.
                mmrInput = new ByteStream(decodingContext.Data);
            }
            for (i = bitsPerValue - 1; i >= 0; i--)
            {
                if (mmr)
                {
                    bitmap = DecodeMMRBitmap(mmrInput, gridWidth, gridHeight, true);
                }
                else
                {
                    bitmap = DecodeBitmap(false, gridWidth, gridHeight, template, false, skip, at, decodingContext);
                }
                grayScaleBitPlanes[i] = bitmap;
            }
            // 6.6.5.2 Rendering the patterns.
            int mg, ng, bit, patternIndex, x, y;
            byte[] patternRow, regionRow;
            Dictionary<int, byte[]> patternBitmap;
            for (mg = 0; mg < gridHeight; mg++)
            {
                for (ng = 0; ng < gridWidth; ng++)
                {
                    bit = 0;
                    patternIndex = 0;
                    for (j = bitsPerValue - 1; j >= 0; j--)
                    {
                        bit ^= grayScaleBitPlanes[j][mg][ng]; // Gray decoding
                        patternIndex |= bit << j;
                    }
                    patternBitmap = patterns[patternIndex];
                    x = (gridOffsetX + mg * gridVectorY + ng * gridVectorX) >> 8;
                    y = (gridOffsetY + mg * gridVectorX - ng * gridVectorY) >> 8;
                    // Draw patternBitmap at (x, y).
                    if (x >= 0 &&
                      x + patternWidth <= regionWidth &&
                      y >= 0 &&
                      y + patternHeight <= regionHeight)
                    {
                        for (i = 0; i < patternHeight; i++)
                        {
                            regionRow = regionBitmap[y + i];
                            patternRow = patternBitmap[i];
                            for (j = 0; j < patternWidth; j++)
                            {
                                regionRow[x + j] |= patternRow[j];
                            }
                        }
                    }
                    else
                    {
                        int regionX, regionY;
                        for (i = 0; i < patternHeight; i++)
                        {
                            regionY = y + i;
                            if (regionY < 0 || regionY >= regionHeight)
                            {
                                continue;
                            }
                            regionRow = regionBitmap[regionY];
                            patternRow = patternBitmap[i];
                            for (j = 0; j < patternWidth; j++)
                            {
                                regionX = x + j;
                                if (regionX >= 0 && regionX < regionWidth)
                                {
                                    regionRow[regionX] |= patternRow[j];
                                }
                            }
                        }
                    }
                }
            }
            return regionBitmap;
        }

        static SegmentHeader ReadSegmentHeader(IInputStream data)
        {
            var segmentHeader = new SegmentHeader();
            segmentHeader.Number = (int)data.ReadUInt32();
            var flags = data.ReadUByte();
            var segmentType = flags & 0x3f;
            if (SegmentTypes[segmentType] == null)
            {
                throw new Jbig2Error("invalid segment type: " + segmentType);
            }
            segmentHeader.Type = segmentType;
            segmentHeader.TypeName = SegmentTypes[segmentType];
            segmentHeader.DeferredNonRetain = (flags & 0x80) != 0;

            var pageAssociationFieldSize = (flags & 0x40) != 0;
            var referredFlags = data.ReadUByte();
            long referredToCount = (referredFlags >> 5) & 7;
            var retainBits = new List<int> { referredFlags & 31 };
            //var position = start + 6;
            if (referredFlags == 7)
            {
                data.Skip(-1);
                referredToCount = data.ReadUInt32() & 0x1fffffff;
                var bytes = (referredToCount + 7) >> 3;
                retainBits[0] = data.ReadUByte();
                while (--bytes > 0)
                {
                    retainBits.Add(data.ReadUByte());
                }
            }
            else if (referredFlags == 5 || referredFlags == 6)
            {
                throw new Jbig2Error("invalid referred-to flags");
            }

            segmentHeader.RetainBits = retainBits;

            var referredToSegmentNumberSize = 4;
            if (segmentHeader.Number <= 256)
            {
                referredToSegmentNumberSize = 1;
            }
            else if (segmentHeader.Number <= 65536)
            {
                referredToSegmentNumberSize = 2;
            }
            var referredTo = new List<int>();
            int i, ii;
            for (i = 0; i < referredToCount; i++)
            {
                int number;
                if (referredToSegmentNumberSize == 1)
                {
                    number = data.ReadUByte();
                }
                else if (referredToSegmentNumberSize == 2)
                {
                    number = data.ReadUInt16();
                }
                else
                {
                    number = (int)data.ReadUInt32();
                }
                referredTo.Add(number);                
            }
            segmentHeader.ReferredTo = referredTo;
            if (!pageAssociationFieldSize)
            {
                segmentHeader.PageAssociation = data.ReadUByte();
            }
            else
            {
                segmentHeader.PageAssociation = (int)data.ReadUInt32();
            }
            segmentHeader.Length = data.ReadUInt32();

            if (segmentHeader.Length == 0xffffffff)
            {
                // 7.2.7 Segment data Length, unknown segment Length
                if (segmentType == 38)
                {
                    var position = (int)data.Position;
                    // ImmediateGenericRegion
                    var genericRegionInfo = ReadRegionSegmentInformation(data);
                    var genericRegionSegmentFlags = data.ReadUByte();
                    var genericRegionMmr = (genericRegionSegmentFlags & 1) != 0;
                    // searching for the segment end
                    var searchPatternLength = 6;
                    var searchPattern = new byte[searchPatternLength];
                    if (!genericRegionMmr)
                    {
                        searchPattern[0] = 0xff;
                        searchPattern[1] = 0xac;
                    }
                    searchPattern[2] = FiltersExtension.ToByte((int)(((uint)genericRegionInfo.Height >> 24) & 0xff));
                    searchPattern[3] = FiltersExtension.ToByte((genericRegionInfo.Height >> 16) & 0xff);
                    searchPattern[4] = FiltersExtension.ToByte((genericRegionInfo.Height >> 8) & 0xff);
                    searchPattern[5] = FiltersExtension.ToByte(genericRegionInfo.Height & 0xff);
                    for (i = 0, ii = (int)data.Length - position; i < ii; i++)
                    {
                        var j = 0;
                        while (j < searchPatternLength && searchPattern[j] == data.PeekUByte(i + j))
                        {
                            j++;
                        }
                        if (j == searchPatternLength)
                        {
                            segmentHeader.Length = (uint)(i + searchPatternLength + position);
                            break;
                        }
                    }
                    if (segmentHeader.Length == 0xffffffff)
                    {
                        throw new Jbig2Error("segment end was not found");
                    }
                    data.Position = position;
                }
                else
                {
                    throw new Jbig2Error("invalid unknown segment Length");
                }
            }
            return segmentHeader;
        }

        static List<Segment> ReadSegments(SegmentHeader header, IInputStream data)
        {
            var segments = new List<Segment>();
            while (data.Position < data.Length)
            {
                var segmentHeader = ReadSegmentHeader(data);
                var segment = new Segment(header: segmentHeader);
                if (!header.RandomAccess)
                {
                    segment.Data = data.ReadMemory((int)segmentHeader.Length);
                }
                segments.Add(segment);
                if (segmentHeader.Type == 51)
                {
                    break; // end of file is found
                }
            }
            if (header.RandomAccess)
            {
                for (int i = 0, ii = segments.Count; i < ii; i++)
                {
                    var segment = segments[i];
                    segment.Data = data.ReadMemory((int)segment.Header.Length);
                }
            }
            return segments;
        }

        // 7.4.1 Region segment information field
        static RegionSegmentInformation ReadRegionSegmentInformation(IInputStream data)
        {
            return new RegionSegmentInformation(
                width: data.ReadUInt32(),
                height: data.ReadUInt32(),
                x: data.ReadUInt32(),
                y: data.ReadUInt32(),
                combinationOperator: data.ReadUByte() & 7
                );
        }

        static void ProcessSegment(Segment segment, SimpleSegmentVisitor visitor)
        {
            var header = segment.Header;
            var data = new ByteStream(segment.Data);
            var end = data.Length;
            List<Point> at;
            int i, atLength;
            switch (header.Type)
            {
                case 0: // SymbolDictionary
                        // 7.4.2 Symbol dictionary segment syntax
                    var dictionary = new SymbolDictionary();
                    var dictionaryFlags = data.ReadUInt16(); // 7.4.2.1.1
                    dictionary.Huffman = (dictionaryFlags & 1) != 0;
                    dictionary.Refinement = (dictionaryFlags & 2) != 0;
                    dictionary.HuffmanDHSelector = (dictionaryFlags >> 2) & 3;
                    dictionary.HuffmanDWSelector = (dictionaryFlags >> 4) & 3;
                    dictionary.BitmapSizeSelector = (dictionaryFlags >> 6) & 1;
                    dictionary.AggregationInstancesSelector = (dictionaryFlags >> 7) & 1;
                    dictionary.BitmapCodingContextUsed = (dictionaryFlags & 256) != 0;
                    dictionary.BitmapCodingContextRetained = (dictionaryFlags & 512) != 0;
                    dictionary.Template = (dictionaryFlags >> 10) & 3;
                    dictionary.RefinementTemplate = (dictionaryFlags >> 12) & 1;
                    if (!dictionary.Huffman)
                    {
                        atLength = dictionary.Template == 0 ? 4 : 1;
                        at = new List<Point>();
                        for (i = 0; i < atLength; i++)
                        {
                            at.Add(new Point(x: data.ReadInt8(), y: data.ReadInt8()));
                        }
                        dictionary.At = at;
                    }
                    if (dictionary.Refinement && dictionary.RefinementTemplate == 0)
                    {
                        at = new List<Point>();
                        for (i = 0; i < 2; i++)
                        {
                            at.Add(new Point(x: data.ReadInt8(), y: data.ReadInt8()));
                        }
                        dictionary.RefinementAt = at;
                    }
                    dictionary.NumberOfExportedSymbols = (int)data.ReadUInt32();
                    dictionary.NumberOfNewSymbols = (int)data.ReadUInt32();
                    visitor.OnSymbolDictionary(dictionary, header.Number, header.ReferredTo, segment.Data.Slice((int)data.Position));
                    break;
                case 6: // ImmediateTextRegion
                case 7: // ImmediateLosslessTextRegion
                    var textRegion = new TextRegion();
                    textRegion.Info = ReadRegionSegmentInformation(data);
                    var textRegionSegmentFlags = data.ReadUInt16();
                    textRegion.Huffman = (textRegionSegmentFlags & 1) != 0;
                    textRegion.Refinement = (textRegionSegmentFlags & 2) != 0;
                    textRegion.LogStripSize = (textRegionSegmentFlags >> 2) & 3;
                    textRegion.StripSize = 1 << textRegion.LogStripSize;
                    textRegion.ReferenceCorner = (textRegionSegmentFlags >> 4) & 3;
                    textRegion.Transposed = (textRegionSegmentFlags & 64) != 0;
                    textRegion.CombinationOperator = (textRegionSegmentFlags >> 7) & 3;
                    textRegion.DefaultPixelValue = FiltersExtension.ToByte((textRegionSegmentFlags >> 9) & 1);
                    textRegion.DsOffset = (textRegionSegmentFlags << 17) >> 27;
                    textRegion.RefinementTemplate = (textRegionSegmentFlags >> 15) & 1;
                    if (textRegion.Huffman)
                    {
                        var textRegionHuffmanFlags = data.ReadUInt16();
                        textRegion.HuffmanFS = textRegionHuffmanFlags & 3;
                        textRegion.HuffmanDS = (textRegionHuffmanFlags >> 2) & 3;
                        textRegion.HuffmanDT = (textRegionHuffmanFlags >> 4) & 3;
                        textRegion.HuffmanRefinementDW = (textRegionHuffmanFlags >> 6) & 3;
                        textRegion.HuffmanRefinementDH = (textRegionHuffmanFlags >> 8) & 3;
                        textRegion.HuffmanRefinementDX = (textRegionHuffmanFlags >> 10) & 3;
                        textRegion.HuffmanRefinementDY = (textRegionHuffmanFlags >> 12) & 3;
                        textRegion.huffmanRefinementSizeSelector = (textRegionHuffmanFlags & 0x4000) != 0;
                    }
                    if (textRegion.Refinement && textRegion.RefinementTemplate == 0)
                    {
                        at = new List<Point>();
                        for (i = 0; i < 2; i++)
                        {
                            at.Add(new Point(x: data.ReadInt8(), y: data.ReadInt8()));
                        }
                        textRegion.refinementAt = at;
                    }
                    textRegion.numberOfSymbolInstances = (int)data.ReadUInt32();
                    visitor.OnImmediateTextRegion(textRegion, header.ReferredTo, segment.Data.Slice((int)data.Position));

                    break;
                case 16: // PatternDictionary
                         // 7.4.4. Pattern dictionary segment syntax
                    var patternDictionary = new PatternDictionary();
                    var patternDictionaryFlags = data.ReadUByte();
                    patternDictionary.Mmr = (patternDictionaryFlags & 1) != 0;
                    patternDictionary.Template = (patternDictionaryFlags >> 1) & 3;
                    patternDictionary.PatternWidth = data.ReadUByte();
                    patternDictionary.PatternHeight = data.ReadUByte();
                    patternDictionary.MaxPatternIndex = (int)data.ReadUInt32();
                    visitor.OnPatternDictionary(patternDictionary, header.Number, segment.Data.Slice((int)data.Position));
                    break;
                case 22: // ImmediateHalftoneRegion
                case 23: // ImmediateLosslessHalftoneRegion
                         // 7.4.5 Halftone region segment syntax
                    var halftoneRegion = new HalftoneRegion();
                    halftoneRegion.Info = ReadRegionSegmentInformation(data);
                    var halftoneRegionFlags = data.ReadUByte();
                    halftoneRegion.Mmr = (halftoneRegionFlags & 1) != 0;
                    halftoneRegion.Template = (halftoneRegionFlags >> 1) & 3;
                    halftoneRegion.EnableSkip = (halftoneRegionFlags & 8) != 0;
                    halftoneRegion.CombinationOperator = (halftoneRegionFlags >> 4) & 7;
                    halftoneRegion.DefaultPixelValue = FiltersExtension.ToByte((halftoneRegionFlags >> 7) & 1);
                    halftoneRegion.GridWidth = (int)data.ReadUInt32();
                    halftoneRegion.GridHeight = (int)data.ReadUInt32();
                    halftoneRegion.GridOffsetX = (int)(data.ReadUInt32() & 0xffffffff);
                    halftoneRegion.GridOffsetY = (int)(data.ReadUInt32() & 0xffffffff);
                    halftoneRegion.GridVectorX = (int)data.ReadUInt16();
                    halftoneRegion.GridVectorY = (int)data.ReadUInt16();
                    visitor.OnImmediateHalftoneRegion(halftoneRegion, header.ReferredTo, segment.Data.Slice((int)data.Position));
                    break;
                case 38: // ImmediateGenericRegion
                case 39: // ImmediateLosslessGenericRegion
                    var genericRegion = new GenericRegion();
                    genericRegion.Info = ReadRegionSegmentInformation(data);
                    var genericRegionSegmentFlags = data.ReadUByte();
                    genericRegion.Mmr = (genericRegionSegmentFlags & 1) != 0;
                    genericRegion.Template = (genericRegionSegmentFlags >> 1) & 3;
                    genericRegion.Prediction = (genericRegionSegmentFlags & 8) != 0;
                    if (!genericRegion.Mmr)
                    {
                        atLength = genericRegion.Template == 0 ? 4 : 1;
                        at = new List<Point>();
                        for (i = 0; i < atLength; i++)
                        {
                            at.Add(new Point(x: data.ReadInt8(), y: data.ReadInt8()));
                        }
                        genericRegion.At = at;
                    }
                    visitor.OnImmediateGenericRegion(genericRegion, segment.Data.Slice((int)data.Position));
                    break;
                case 48: // PageInformation
                    var pos = data.Position;
                    var pageInfo = new PageInformation(
                        width: data.ReadUInt32(),
                        height: data.ReadUInt32(),
                        resolutionX: data.ReadUInt32(),
                        resolutionY: data.ReadUInt32());
                    if (pageInfo.Height == 0xffffffff)
                    {
                        pageInfo.Height = 0;
                        //????delete pageInfo.height;
                    }
                    var pageSegmentFlags = data.ReadUByte();
                    data.ReadUInt16(); // pageStripingInformation
                    pageInfo.Lossless = (pageSegmentFlags & 1) != 0;
                    pageInfo.Refinement = (pageSegmentFlags & 2) != 0;
                    pageInfo.DefaultPixelValue = (pageSegmentFlags >> 2) & 1;
                    pageInfo.CombinationOperator = (pageSegmentFlags >> 3) & 3;
                    pageInfo.RequiresBuffer = (pageSegmentFlags & 32) != 0;
                    pageInfo.CombinationOperatorOverride = (pageSegmentFlags & 64) != 0;
                    visitor.OnPageInformation(pageInfo);
                    break;
                case 49: // EndOfPage
                    break;
                case 50: // EndOfStripe
                    break;
                case 51: // EndOfFile
                    break;
                case 53: // Tables
                    visitor.OnTables(header.Number, segment.Data.Slice((int)data.Position));
                    break;
                case 62: // 7.4.15 defines 2 extension types which
                         // are comments and can be ignored.
                    break;
                default:
                    throw new Jbig2Error($"segment type {header.TypeName}({header.Type}) is not implemented");
            }
        }

        static void ProcessSegments(List<Segment> segments, SimpleSegmentVisitor visitor)
        {
            for (int i = 0, ii = segments.Count; i < ii; i++)
            {
                ProcessSegment(segments[i], visitor);
            }
        }

        static byte[] ParseJbig2Chunks(List<ImageChunk> chunks)
        {
            var visitor = new SimpleSegmentVisitor();
            for (int i = 0, ii = chunks.Count; i < ii; i++)
            {
                var chunk = chunks[i];
                var segments = ReadSegments(new SegmentHeader(), chunk.Data);
                ProcessSegments(segments, visitor);
            }
            return visitor.Buffer;
        }

        static ImageData ParseJbig2(IInputStream data)
        {
            var end = data.Length;

            if (
              data.ReadUByte() != 0x97 ||
              data.ReadUByte() != 0x4a ||
              data.ReadUByte() != 0x42 ||
              data.ReadUByte() != 0x32 ||
              data.ReadUByte() != 0x0d ||
              data.ReadUByte() != 0x0a ||
              data.ReadUByte() != 0x1a ||
              data.ReadUByte() != 0x0a
            )
            {
                throw new Jbig2Error("parseJbig2 - invalid header.");
            }

            var header = new SegmentHeader();
            var flags = data.ReadUByte();
            header.RandomAccess = (flags & 1) == 0;
            if ((flags & 2) == 0)
            {
                header.NumberOfPages = (int)data.ReadUInt32();
            }

            var segments = ReadSegments(header, data);
            var visitor = new SimpleSegmentVisitor();
            ProcessSegments(segments, visitor);

            var width = visitor.CurrentPageInfo.Width;
            var height = visitor.CurrentPageInfo.Height;
            var bitPacked = visitor.Buffer;
            var imgData = new byte[width * height];
            int q = 0, k = 0;
            for (var i = 0; i < height; i++)
            {
                int mask = 0;
                byte buffer = 0;
                for (var j = 0; j < width; j++)
                {
                    if (mask == 0)
                    {
                        mask = 128;
                        buffer = bitPacked[k++];
                    }
                    imgData[q++] = (buffer & mask) != 0 ? (byte)0 : (byte)255;
                    mask >>= 1;
                }
            }

            return new ImageData(imgData, (int)width, (int)height);
        }

        internal class SimpleSegmentVisitor
        {
            internal Dictionary<int, HuffmanTable> CustomTables;
            internal PageInformation CurrentPageInfo;
            internal byte[] Buffer;
            private Dictionary<int, List<Dictionary<int, byte[]>>> symbols;
            private Dictionary<int, List<Dictionary<int, byte[]>>> patterns;

            public void OnPageInformation(PageInformation info)
            {
                CurrentPageInfo = info;
                var rowSize = (info.Width + 7) >> 3;
                var buffer = new byte[rowSize * info.Height];
                // The contents of ArrayBuffers are initialized to 0.
                // Fill the buffer with 0xFF only if info.defaultPixelValue is set
                if (info.DefaultPixelValue != 0)
                {
                    Array.Fill(buffer, (byte)0xff);
                }
                Buffer = buffer;
            }

            void DrawBitmap(RegionSegmentInformation regionInfo, Dictionary<int, byte[]> bitmap)
            {
                var pageInfo = CurrentPageInfo;
                uint width = regionInfo.Width,
                  height = regionInfo.Height;
                var rowSize = (pageInfo.Width + 7) >> 3;
                var combinationOperator = pageInfo.CombinationOperatorOverride
                  ? regionInfo.CombinationOperator
                  : pageInfo.CombinationOperator;
                var buffer = Buffer;
                var mask0 = 128 >> (int)(regionInfo.X & 7);
                var offset0 = (uint)(regionInfo.Y * rowSize + (regionInfo.X >> 3));
                int i, j;
                int mask;
                uint offset;
                switch (combinationOperator)
                {
                    case 0: // OR
                        for (i = 0; i < height; i++)
                        {
                            mask = mask0;
                            offset = offset0;
                            var exist = bitmap.TryGetValue(i, out var bits);
                            for (j = 0; j < width; j++)
                            {
                                if (exist && j < bits.Length && bits[j] != 0)
                                {
                                    buffer[offset] = FiltersExtension.ToByte(buffer[offset] | mask);
                                }
                                mask >>= 1;
                                if (mask == 0)
                                {
                                    mask = 128;
                                    offset++;
                                }
                            }
                            offset0 += rowSize;
                        }
                        break;
                    case 2: // XOR
                        for (i = 0; i < height; i++)
                        {
                            mask = mask0;
                            offset = offset0;
                            var exist = bitmap.TryGetValue(i, out var bits);
                            for (j = 0; j < width; j++)
                            {
                                if (exist && j < bits.Length && bits[j] != 0)
                                {
                                    buffer[offset] = FiltersExtension.ToByte(buffer[offset] ^ mask);
                                }
                                mask >>= 1;
                                if (mask == 0)
                                {
                                    mask = 128;
                                    offset++;
                                }
                            }
                            offset0 += rowSize;
                        }
                        break;
                    default:
                        throw new Jbig2Error($"operator {combinationOperator} is not supported");
                }
            }

            public void OnImmediateGenericRegion(GenericRegion region, Memory<byte> data)
            {
                var regionInfo = region.Info;
                var decodingContext = new DecodingContext(data);
                var bitmap = DecodeBitmap(
                  region.Mmr,
                  (int)regionInfo.Width,
                  (int)regionInfo.Height,
                  region.Template,
                  region.Prediction,
                  null,
                  region.At,
                  decodingContext
                );
                DrawBitmap(regionInfo, bitmap);
            }

            public void OnSymbolDictionary(SymbolDictionary dictionary, int currentSegment, List<int> referredSegments, Memory<byte> data)
            {
                HuffmanTables huffmanTables = null;
                ByteStream huffmanInput = null;
                if (dictionary.Huffman)
                {
                    huffmanTables = GetSymbolDictionaryHuffmanTables(dictionary, referredSegments, CustomTables);
                    huffmanInput = new ByteStream(data);
                }

                // Combines exported symbols from all referred segments
                var symbols = this.symbols;
                if (symbols == null)
                {
                    this.symbols = symbols = new Dictionary<int, List<Dictionary<int, byte[]>>>();
                }

                var inputSymbols = new List<Dictionary<int, byte[]>>();
                foreach (var referredSegment in referredSegments)
                {
                    // referredSymbols is undefined when we have a reference to a Tables
                    // segment instead of a SymbolDictionary.
                    if (symbols.TryGetValue(referredSegment, out var referredSymbols))
                    {
                        inputSymbols.AddRange(referredSymbols);
                    }
                }

                var decodingContext = new DecodingContext(data);
                symbols[currentSegment] = DecodeSymbolDictionary(dictionary.Huffman, dictionary.Refinement, inputSymbols,
                  dictionary.NumberOfNewSymbols, dictionary.NumberOfExportedSymbols, huffmanTables, dictionary.Template,
                  dictionary.At, dictionary.RefinementTemplate, dictionary.RefinementAt, decodingContext,
                  huffmanInput
                );
            }

            public void OnImmediateTextRegion(TextRegion region, List<int> referredSegments, Memory<byte> data)
            {
                var regionInfo = region.Info;
                HuffmanTables huffmanTables = null;
                ByteStream huffmanInput = null;

                // Combines exported symbols from all referred segments
                var symbols = this.symbols;
                var inputSymbols = new List<Dictionary<int, byte[]>>();
                foreach (var referredSegment in referredSegments)
                {
                    // referredSymbols is undefined when we have a reference to a Tables
                    // segment instead of a SymbolDictionary.
                    if (symbols.TryGetValue(referredSegment, out var referredSymbols))
                    {
                        inputSymbols.AddRange(referredSymbols);
                    }
                }
                var symbolCodeLength = FiltersExtension.Log2(inputSymbols.Count);
                if (region.Huffman)
                {
                    huffmanInput = new ByteStream(data);
                    huffmanTables = GetTextRegionHuffmanTables(
                      region,
                      referredSegments,
                      CustomTables,
                      inputSymbols.Count,
                      huffmanInput
                    );
                }

                var decodingContext = new DecodingContext(data);
                var bitmap = DecodeTextRegion(region.Huffman, region.Refinement, (int)regionInfo.Width,
                  (int)regionInfo.Height, region.DefaultPixelValue, region.numberOfSymbolInstances, region.StripSize,
                  inputSymbols, symbolCodeLength, region.Transposed, region.DsOffset,
                  region.ReferenceCorner, region.CombinationOperator, huffmanTables, region.RefinementTemplate,
                  region.refinementAt, decodingContext, region.LogStripSize, huffmanInput
                );
                DrawBitmap(regionInfo, bitmap);
            }

            public void OnPatternDictionary(PatternDictionary dictionary, int currentSegment, Memory<byte> data)
            {
                var patterns = this.patterns;
                if (patterns == null)
                {
                    this.patterns = patterns = new Dictionary<int, List<Dictionary<int, byte[]>>>();
                }
                var decodingContext = new DecodingContext(data);
                patterns[currentSegment] = DecodePatternDictionary(dictionary.Mmr, dictionary.PatternWidth, dictionary.PatternHeight,
                    dictionary.MaxPatternIndex, dictionary.Template, decodingContext);
            }

            public void OnImmediateHalftoneRegion(HalftoneRegion region, List<int> referredSegments, Memory<byte> data)
            {
                // HalftoneRegion refers to exactly one PatternDictionary.
                var patterns = this.patterns[referredSegments[0]];
                var regionInfo = region.Info;
                var decodingContext = new DecodingContext(data);
                var bitmap = DecodeHalftoneRegion(
                  region.Mmr,
                  patterns,
                  region.Template,
                  (int)regionInfo.Width,
                  (int)regionInfo.Height,
                  region.DefaultPixelValue,
                  region.EnableSkip,
                  region.CombinationOperator,
                  region.GridWidth,
                  region.GridHeight,
                  region.GridOffsetX,
                  region.GridOffsetY,
                  region.GridVectorX,
                  region.GridVectorY,
                  decodingContext
                );
                DrawBitmap(regionInfo, bitmap);
            }

            public void OnTables(int currentSegment, Memory<byte> data)
            {
                var customTables = CustomTables;
                if (customTables == null)
                {
                    CustomTables = customTables = new Dictionary<int, HuffmanTable> { };
                }
                customTables[currentSegment] = DecodeTablesSegment(data);
            }
        }

        internal class HuffmanLine
        {
            internal bool IsOOB;
            internal int RangeLow;
            internal int PrefixLength;
            internal int RangeLength;
            internal int PrefixCode;
            public bool IsLowerRange;

            public HuffmanLine(int prefixLength, int prefixCode)
            {
                // OOB line.
                IsOOB = true;
                RangeLow = 0;
                PrefixLength = prefixLength;
                RangeLength = 0;
                PrefixCode = prefixCode;
                IsLowerRange = false;
            }
            public HuffmanLine(int rangeLow, int prefixLength, int rangeLength, int prefixCode, bool isLower = false)
            {
                // Normal, upper range or lower range line.
                // Upper range lines are processed like normal lines.
                IsOOB = false;
                RangeLow = rangeLow;
                PrefixLength = prefixLength;
                RangeLength = rangeLength;
                PrefixCode = prefixCode;
                IsLowerRange = isLower;
            }
        }

        internal class HuffmanTreeNode
        {
            internal bool IsLeaf;
            internal int RangeLength;
            internal int RangeLow;
            internal bool IsLowerRange;
            internal bool IsOOB;
            internal Dictionary<int, HuffmanTreeNode> Children;

            public HuffmanTreeNode(HuffmanLine line)
            {
                Children = new Dictionary<int, HuffmanTreeNode>();
                if (line != null)
                {
                    // Leaf node
                    IsLeaf = true;
                    RangeLength = line.RangeLength;
                    RangeLow = line.RangeLow;
                    IsLowerRange = line.IsLowerRange;
                    IsOOB = line.IsOOB;
                }
                else
                {
                    // Intermediate or root node
                    IsLeaf = false;
                }
            }


            public void BuildTree(HuffmanLine line, int shift)
            {
                var bit = (line.PrefixCode >> shift) & 1;
                if (shift <= 0)
                {
                    // Create a leaf node.
                    Children[bit] = new HuffmanTreeNode(line);
                }
                else
                {
                    // Create an intermediate node and continue recursively.
                    if (!Children.TryGetValue(bit, out var node))
                    {
                        Children[bit] = node = new HuffmanTreeNode(null);
                    }
                    node.BuildTree(line, shift - 1);
                }
            }

            public int? DecodeNode(ByteStream reader)
            {
                if (IsLeaf)
                {
                    if (IsOOB)
                    {
                        return null;
                    }
                    var htOffset = (int)reader.ReadBits(RangeLength);
                    return RangeLow + (IsLowerRange ? -htOffset : htOffset);
                }
                var node = Children[reader.ReadBit()];
                if (node == null)
                {
                    throw new Jbig2Error("invalid Huffman data");
                }
                return node.DecodeNode(reader);
            }
        }

        internal class HuffmanTable
        {
            private HuffmanTreeNode rootNode;

            public HuffmanTable(List<HuffmanLine> lines, bool prefixCodesDone)
            {
                if (!prefixCodesDone)
                {
                    AssignPrefixCodes(lines);
                }
                // Create Huffman tree.
                rootNode = new HuffmanTreeNode(null);
                for (int i = 0, ii = lines.Count; i < ii; i++)
                {
                    var line = lines[i];
                    if (line.PrefixLength > 0)
                    {
                        rootNode.BuildTree(line, line.PrefixLength - 1);
                    }
                }
            }

            public int? Decode(ByteStream reader)
            {
                return rootNode.DecodeNode(reader);
            }

            void AssignPrefixCodes(List<HuffmanLine> lines)
            {
                // Annex B.3 Assigning the prefix codes.
                var linesLength = lines.Count;
                var prefixLengthMax = 0;
                for (var i = 0; i < linesLength; i++)
                {
                    prefixLengthMax = Math.Max(prefixLengthMax, lines[i].PrefixLength);
                }

                var histogram = new uint[prefixLengthMax + 1];
                for (var i = 0; i < linesLength; i++)
                {
                    histogram[lines[i].PrefixLength]++;
                }
                int currentLength = 1,
                  firstCode = 0,
                  currentCode,
                  currentTemp;
                HuffmanLine line;
                histogram[0] = 0;

                while (currentLength <= prefixLengthMax)
                {
                    firstCode = (int)((firstCode + histogram[currentLength - 1]) << 1);
                    currentCode = firstCode;
                    currentTemp = 0;
                    while (currentTemp < linesLength)
                    {
                        line = lines[currentTemp];
                        if (line.PrefixLength == currentLength)
                        {
                            line.PrefixCode = currentCode;
                            currentCode++;
                        }
                        currentTemp++;
                    }
                    currentLength++;
                }
            }
        }

        static HuffmanTable DecodeTablesSegment(Memory<byte> data)
        {
            // Decodes a Tables segment, i.e., a custom Huffman table.
            // Annex B.2 Code table structure.
            var reader = new ByteStream(data);
            var flags = reader.ReadUByte();
            var lowestValue = reader.ReadUInt32() & 0xffffffff;
            var highestValue = reader.ReadUInt32() & 0xffffffff;


            var prefixSizeBits = ((flags >> 1) & 7) + 1;
            var rangeSizeBits = ((flags >> 4) & 7) + 1;
            var lines = new List<HuffmanLine>();
            int prefixLength,
              rangeLength,
              currentRangeLow = (int)lowestValue;

            // Normal table lines
            do
            {
                prefixLength = (int)reader.ReadBits(prefixSizeBits);
                rangeLength = (int)reader.ReadBits(rangeSizeBits);
                lines.Add(new HuffmanLine(currentRangeLow, prefixLength, rangeLength, 0));
                currentRangeLow += 1 << rangeLength;
            } while (currentRangeLow < highestValue);

            // Lower range table line
            prefixLength = (int)reader.ReadBits(prefixSizeBits);
            lines.Add(new HuffmanLine((int)lowestValue - 1, prefixLength, 32, 0, true));

            // Upper range table line
            prefixLength = (int)reader.ReadBits(prefixSizeBits);
            lines.Add(new HuffmanLine((int)highestValue, prefixLength, 32, 0));

            if ((flags & 1) != 0)
            {
                // Out-of-band table line
                prefixLength = (int)reader.ReadBits(prefixSizeBits);
                lines.Add(new HuffmanLine(prefixLength, 0));
            }

            return new HuffmanTable(lines, false);
        }

        static HuffmanTable GetStandardTable(int number)
        {
            // Annex B.5 Standard Huffman tables.
            if (StandardTablesCache.TryGetValue(number, out var table))
            {
                return table;
            }
            List<HuffmanLine> lines;
            switch (number)
            {
                case 1:
                    lines = new List<HuffmanLine> {
                        new HuffmanLine(0, 1, 4, 0x0 ),
                        new HuffmanLine(16, 2, 8, 0x2 ),
                        new HuffmanLine(272, 3, 16, 0x6 ),
                        new HuffmanLine(65808, 3, 32, 0x7 ) // upper
                    };
                    break;
                case 2:
                    lines = new List<HuffmanLine> {
                       new HuffmanLine(0, 1, 0, 0x0 ),
                       new HuffmanLine(1, 2, 0, 0x2 ),
                       new HuffmanLine(2, 3, 0, 0x6 ),
                       new HuffmanLine(3, 4, 3, 0xe ),
                       new HuffmanLine(11, 5, 6, 0x1e ),
                       new HuffmanLine(75, 6, 32, 0x3e ), // upper
                       new HuffmanLine(6, 0x3f ), // OOB
                      };
                    break;
                case 3:
                    lines = new List<HuffmanLine> {
                        new HuffmanLine(-256, 8, 8, 0xfe ),
                        new HuffmanLine(0, 1, 0, 0x0 ),
                        new HuffmanLine(1, 2, 0, 0x2 ),
                        new HuffmanLine(2, 3, 0, 0x6 ),
                        new HuffmanLine(3, 4, 3, 0xe ),
                        new HuffmanLine(11, 5, 6, 0x1e ),
                        new HuffmanLine(-257, 8, 32, 0xff, true),
                        new HuffmanLine(75, 7, 32, 0x7e ), // upper
                        new HuffmanLine(6, 0x3e ), // OOB
                    };
                    break;
                case 4:
                    lines = new List<HuffmanLine> {
                      new HuffmanLine(1, 1, 0, 0x0 ),
                      new HuffmanLine(2, 2, 0, 0x2 ),
                      new HuffmanLine(3, 3, 0, 0x6 ),
                      new HuffmanLine(4, 4, 3, 0xe ),
                      new HuffmanLine(12, 5, 6, 0x1e ),
                      new HuffmanLine(76, 5, 32, 0x1f ), // upper
                    };
                    break;
                case 5:
                    lines = new List<HuffmanLine> {
                      new HuffmanLine(-255, 7, 8, 0x7e ),
                      new HuffmanLine(1, 1, 0, 0x0 ),
                      new HuffmanLine(2, 2, 0, 0x2 ),
                      new HuffmanLine(3, 3, 0, 0x6 ),
                      new HuffmanLine(4, 4, 3, 0xe ),
                      new HuffmanLine(12, 5, 6, 0x1e ),
                      new HuffmanLine(-256, 7, 32, 0x7f, true),
                      new HuffmanLine(76, 6, 32, 0x3e ), // upper
                    };
                    break;
                case 6:
                    lines = new List<HuffmanLine> {
                        new HuffmanLine(-2048, 5, 10, 0x1c),
                        new HuffmanLine(-1024, 4, 9, 0x8),
                        new HuffmanLine(-512, 4, 8, 0x9),
                        new HuffmanLine(-256, 4, 7, 0xa),
                        new HuffmanLine(-128, 5, 6, 0x1d),
                        new HuffmanLine(-64, 5, 5, 0x1e),
                        new HuffmanLine(-32, 4, 5, 0xb),
                        new HuffmanLine(0, 2, 7, 0x0),
                        new HuffmanLine(128, 3, 7, 0x2),
                        new HuffmanLine(256, 3, 8, 0x3),
                        new HuffmanLine(512, 4, 9, 0xc),
                        new HuffmanLine(1024, 4, 10, 0xd),
                        new HuffmanLine(-2049, 6, 32, 0x3e, true),
                        new HuffmanLine(2048, 6, 32, 0x3f), // upper
                    };
                    break;
                case 7:
                    lines = new List<HuffmanLine> {
                        new HuffmanLine( -1024, 4, 9, 0x8),
                        new HuffmanLine(-512, 3, 8, 0x0),
                        new HuffmanLine(-256, 4, 7, 0x9),
                        new HuffmanLine(-128, 5, 6, 0x1a),
                        new HuffmanLine(-64, 5, 5, 0x1b),
                        new HuffmanLine(-32, 4, 5, 0xa),
                        new HuffmanLine(0, 4, 5, 0xb),
                        new HuffmanLine(32, 5, 5, 0x1c),
                        new HuffmanLine(64, 5, 6, 0x1d),
                        new HuffmanLine(128, 4, 7, 0xc),
                        new HuffmanLine(256, 3, 8, 0x1),
                        new HuffmanLine(512, 3, 9, 0x2),
                        new HuffmanLine(1024, 3, 10, 0x3),
                        new HuffmanLine(-1025, 5, 32, 0x1e, true),
                        new HuffmanLine(2048, 5, 32, 0x1f), // upper
                    };
                    break;
                case 8:
                    lines = new List<HuffmanLine> {
                        new HuffmanLine(-15, 8, 3, 0xfc),
                        new HuffmanLine(-7, 9, 1, 0x1fc),
                        new HuffmanLine(-5, 8, 1, 0xfd),
                        new HuffmanLine(-3, 9, 0, 0x1fd),
                        new HuffmanLine(-2, 7, 0, 0x7c),
                        new HuffmanLine(-1, 4, 0, 0xa),
                        new HuffmanLine(0, 2, 1, 0x0),
                        new HuffmanLine(2, 5, 0, 0x1a),
                        new HuffmanLine(3, 6, 0, 0x3a),
                        new HuffmanLine(4, 3, 4, 0x4),
                        new HuffmanLine(20, 6, 1, 0x3b),
                        new HuffmanLine(22, 4, 4, 0xb),
                        new HuffmanLine(38, 4, 5, 0xc),
                        new HuffmanLine(70, 5, 6, 0x1b),
                        new HuffmanLine(134, 5, 7, 0x1c),
                        new HuffmanLine(262, 6, 7, 0x3c),
                        new HuffmanLine(390, 7, 8, 0x7d),
                        new HuffmanLine(646, 6, 10, 0x3d),
                        new HuffmanLine(-16, 9, 32, 0x1fe, true),
                        new HuffmanLine(1670, 9, 32, 0x1ff), // upper
                        new HuffmanLine(2, 0x1), // OOB
                    };
                    break;
                case 9:
                    lines = new List<HuffmanLine> {
                        new HuffmanLine(-31, 8, 4, 0xfc),
                        new HuffmanLine(-15, 9, 2, 0x1fc),
                        new HuffmanLine(-11, 8, 2, 0xfd),
                        new HuffmanLine(-7, 9, 1, 0x1fd),
                        new HuffmanLine(-5, 7, 1, 0x7c),
                        new HuffmanLine(-3, 4, 1, 0xa),
                        new HuffmanLine(-1, 3, 1, 0x2),
                        new HuffmanLine(1, 3, 1, 0x3),
                        new HuffmanLine(3, 5, 1, 0x1a),
                        new HuffmanLine(5, 6, 1, 0x3a),
                        new HuffmanLine(7, 3, 5, 0x4),
                        new HuffmanLine(39, 6, 2, 0x3b),
                        new HuffmanLine(43, 4, 5, 0xb),
                        new HuffmanLine(75, 4, 6, 0xc),
                        new HuffmanLine(139, 5, 7, 0x1b),
                        new HuffmanLine(267, 5, 8, 0x1c),
                        new HuffmanLine(523, 6, 8, 0x3c),
                        new HuffmanLine(779, 7, 9, 0x7d),
                        new HuffmanLine(1291, 6, 11, 0x3d),
                        new HuffmanLine(-32, 9, 32, 0x1fe, true),
                        new HuffmanLine(3339, 9, 32, 0x1ff), // upper
                        new HuffmanLine(2, 0x0), // OOB
                    };
                    break;
                case 10:
                    lines = new List<HuffmanLine> {
                      new HuffmanLine(-21, 7, 4, 0x7a),
                        new HuffmanLine(-5, 8, 0, 0xfc),
                        new HuffmanLine(-4, 7, 0, 0x7b),
                        new HuffmanLine(-3, 5, 0, 0x18),
                        new HuffmanLine(-2, 2, 2, 0x0),
                        new HuffmanLine(2, 5, 0, 0x19),
                        new HuffmanLine(3, 6, 0, 0x36),
                        new HuffmanLine(4, 7, 0, 0x7c),
                        new HuffmanLine(5, 8, 0, 0xfd),
                        new HuffmanLine(6, 2, 6, 0x1),
                        new HuffmanLine(70, 5, 5, 0x1a),
                        new HuffmanLine(102, 6, 5, 0x37),
                        new HuffmanLine(134, 6, 6, 0x38),
                        new HuffmanLine(198, 6, 7, 0x39),
                        new HuffmanLine(326, 6, 8, 0x3a),
                        new HuffmanLine(582, 6, 9, 0x3b),
                        new HuffmanLine(1094, 6, 10, 0x3c),
                        new HuffmanLine(2118, 7, 11, 0x7d),
                        new HuffmanLine(-22, 8, 32, 0xfe, true),
                        new HuffmanLine(4166, 8, 32, 0xff), // upper
                        new HuffmanLine(2, 0x2), // OOB
                    };
                    break;
                case 11:
                    lines = new List<HuffmanLine> {
                        new HuffmanLine(1, 1, 0, 0x0),
                        new HuffmanLine(2, 2, 1, 0x2),
                        new HuffmanLine(4, 4, 0, 0xc),
                        new HuffmanLine(5, 4, 1, 0xd),
                        new HuffmanLine(7, 5, 1, 0x1c),
                        new HuffmanLine(9, 5, 2, 0x1d),
                        new HuffmanLine(13, 6, 2, 0x3c),
                        new HuffmanLine(17, 7, 2, 0x7a),
                        new HuffmanLine(21, 7, 3, 0x7b),
                        new HuffmanLine(29, 7, 4, 0x7c),
                        new HuffmanLine(45, 7, 5, 0x7d),
                        new HuffmanLine(77, 7, 6, 0x7e),
                        new HuffmanLine(141, 7, 32, 0x7f), // upper
                    };
                    break;
                case 12:
                    lines = new List<HuffmanLine> {
                        new HuffmanLine(1, 1, 0, 0x0),
                        new HuffmanLine(2, 2, 0, 0x2),
                        new HuffmanLine(3, 3, 1, 0x6),
                        new HuffmanLine(5, 5, 0, 0x1c),
                        new HuffmanLine(6, 5, 1, 0x1d),
                        new HuffmanLine(8, 6, 1, 0x3c),
                        new HuffmanLine(10, 7, 0, 0x7a),
                        new HuffmanLine(11, 7, 1, 0x7b),
                        new HuffmanLine(13, 7, 2, 0x7c),
                        new HuffmanLine(17, 7, 3, 0x7d),
                        new HuffmanLine(25, 7, 4, 0x7e),
                        new HuffmanLine(41, 8, 5, 0xfe),
                        new HuffmanLine(73, 8, 32, 0xff), // upper
                    };
                    break;
                case 13:
                    lines = new List<HuffmanLine> {
                        new HuffmanLine(1, 1, 0, 0x0),
                        new HuffmanLine(2, 3, 0, 0x4),
                        new HuffmanLine(3, 4, 0, 0xc),
                        new HuffmanLine(4, 5, 0, 0x1c),
                        new HuffmanLine(5, 4, 1, 0xd),
                        new HuffmanLine(7, 3, 3, 0x5),
                        new HuffmanLine(15, 6, 1, 0x3a),
                        new HuffmanLine(17, 6, 2, 0x3b),
                        new HuffmanLine(21, 6, 3, 0x3c),
                        new HuffmanLine(29, 6, 4, 0x3d),
                        new HuffmanLine(45, 6, 5, 0x3e),
                        new HuffmanLine(77, 7, 6, 0x7e),
                        new HuffmanLine(141, 7, 32, 0x7f), // upper
                    };
                    break;
                case 14:
                    lines = new List<HuffmanLine> {
                        new HuffmanLine(-2, 3, 0, 0x4),
                        new HuffmanLine(-1, 3, 0, 0x5),
                        new HuffmanLine(0, 1, 0, 0x0),
                        new HuffmanLine(1, 3, 0, 0x6),
                        new HuffmanLine(2, 3, 0, 0x7),
                    };
                    break;
                case 15:
                    lines = new List<HuffmanLine> {
                        new HuffmanLine(-24, 7, 4, 0x7c),
                        new HuffmanLine(-8, 6, 2, 0x3c),
                        new HuffmanLine(-4, 5, 1, 0x1c),
                        new HuffmanLine(-2, 4, 0, 0xc),
                        new HuffmanLine(-1, 3, 0, 0x4),
                        new HuffmanLine(0, 1, 0, 0x0),
                        new HuffmanLine(1, 3, 0, 0x5),
                        new HuffmanLine(2, 4, 0, 0xd),
                        new HuffmanLine(3, 5, 1, 0x1d),
                        new HuffmanLine(5, 6, 2, 0x3d),
                        new HuffmanLine(9, 7, 4, 0x7d),
                        new HuffmanLine(-25, 7, 32, 0x7e, true),
                        new HuffmanLine(25, 7, 32, 0x7f), // upper
                    };
                    break;
                default:
                    throw new Jbig2Error("standard table B.${number} does not exist");
            }

            table = new HuffmanTable(lines, true);
            StandardTablesCache[number] = table;
            return table;
        }

        static HuffmanTable GetCustomHuffmanTable(int index, List<int> referredTo, Dictionary<int, HuffmanTable> customTables)
        {
            // Returns a Tables segment that has been earlier decoded.
            // See 7.4.2.1.6 (symbol dictionary) or 7.4.3.1.6 (text region).
            var currentIndex = 0;
            for (int i = 0, ii = referredTo.Count; i < ii; i++)
            {
                if (customTables.TryGetValue(referredTo[i], out var table))
                {
                    if (index == currentIndex)
                    {
                        return table;
                    }
                    currentIndex++;
                }
            }
            throw new Jbig2Error("can't find custom Huffman table");
        }

        static HuffmanTables GetTextRegionHuffmanTables(TextRegion textRegion, List<int> referredTo, Dictionary<int, HuffmanTable> customTables, int numberOfSymbols, ByteStream reader)
        {
            // 7.4.3.1.7 Symbol ID Huffman table decoding

            // Read code lengths for RUNCODEs 0...34.
            var codes = new List<HuffmanLine>();
            for (var i = 0; i <= 34; i++)
            {
                var codeLength = (int)reader.ReadBits(4);
                codes.Add(new HuffmanLine(i, codeLength, 0, 0));
            }
            // Assign Huffman codes for RUNCODEs.
            var runCodesTable = new HuffmanTable(codes, false);

            // Read a Huffman code using the assignment above.
            // Interpret the RUNCODE codes and the additional bits (if any).
            codes = new List<HuffmanLine>();
            for (var i = 0; i < numberOfSymbols;)
            {
                var codeLength = (int)runCodesTable.Decode(reader);
                if (codeLength >= 32)
                {
                    int repeatedLength, numberOfRepeats, j;
                    switch (codeLength)
                    {
                        case 32:
                            if (i == 0)
                            {
                                throw new Jbig2Error("no previous value in symbol ID table");
                            }
                            numberOfRepeats = (int)reader.ReadBits(2) + 3;
                            repeatedLength = codes[i - 1].PrefixLength;
                            break;
                        case 33:
                            numberOfRepeats = (int)reader.ReadBits(3) + 3;
                            repeatedLength = 0;
                            break;
                        case 34:
                            numberOfRepeats = (int)reader.ReadBits(7) + 11;
                            repeatedLength = 0;
                            break;
                        default:
                            throw new Jbig2Error("invalid code Length in symbol ID table");
                    }
                    for (j = 0; j < numberOfRepeats; j++)
                    {
                        codes.Add(new HuffmanLine(i, repeatedLength, 0, 0));
                        i++;
                    }
                }
                else
                {
                    codes.Add(new HuffmanLine(i, codeLength, 0, 0));
                    i++;
                }
            }
            reader.ByteAlign();
            var symbolIDTable = new HuffmanTable(codes, false);

            // 7.4.3.1.6 Text region segment Huffman table selection

            int customIndex = 0;
            HuffmanTable tableFirstS, tableDeltaS, tableDeltaT;

            switch (textRegion.HuffmanFS)
            {
                case 0:
                case 1:
                    tableFirstS = GetStandardTable(textRegion.HuffmanFS + 6);
                    break;
                case 3:
                    tableFirstS = GetCustomHuffmanTable(customIndex, referredTo, customTables);
                    customIndex++;
                    break;
                default:
                    throw new Jbig2Error("invalid Huffman FS selector");
            }

            switch (textRegion.HuffmanDS)
            {
                case 0:
                case 1:
                case 2:
                    tableDeltaS = GetStandardTable(textRegion.HuffmanDS + 8);
                    break;
                case 3:
                    tableDeltaS = GetCustomHuffmanTable(customIndex, referredTo, customTables);
                    customIndex++;
                    break;
                default:
                    throw new Jbig2Error("invalid Huffman DS selector");
            }

            switch (textRegion.HuffmanDT)
            {
                case 0:
                case 1:
                case 2:
                    tableDeltaT = GetStandardTable(textRegion.HuffmanDT + 11);
                    break;
                case 3:
                    tableDeltaT = GetCustomHuffmanTable(customIndex, referredTo, customTables);
                    customIndex++;
                    break;
                default:
                    throw new Jbig2Error("invalid Huffman DT selector");
            }

            if (textRegion.Refinement)
            {
                // Load tables RDW, RDH, RDX and RDY.
                throw new Jbig2Error("refinement with Huffman is not supported");
            }

            return HuffmanTables.CreateSymbol(symbolIDTable, tableFirstS, tableDeltaS, tableDeltaT);
        }

        static HuffmanTables GetSymbolDictionaryHuffmanTables(SymbolDictionary dictionary, List<int> referredTo, Dictionary<int, HuffmanTable> customTables)
        {
            // 7.4.2.1.6 Symbol dictionary segment Huffman table selection

            int customIndex = 0;
            HuffmanTable tableDeltaHeight, tableDeltaWidth;
            switch (dictionary.HuffmanDHSelector)
            {
                case 0:
                case 1:
                    tableDeltaHeight = GetStandardTable(dictionary.HuffmanDHSelector + 4);
                    break;
                case 3:
                    tableDeltaHeight = GetCustomHuffmanTable(customIndex, referredTo, customTables);
                    customIndex++;
                    break;
                default:
                    throw new Jbig2Error("invalid Huffman DH selector");
            }

            switch (dictionary.HuffmanDWSelector)
            {
                case 0:
                case 1:
                    tableDeltaWidth = GetStandardTable(dictionary.HuffmanDWSelector + 2);
                    break;
                case 3:
                    tableDeltaWidth = GetCustomHuffmanTable(customIndex, referredTo, customTables);
                    customIndex++;
                    break;
                default:
                    throw new Jbig2Error("invalid Huffman DW selector");
            }

            HuffmanTable tableBitmapSize, tableAggregateInstances;
            if (dictionary.BitmapSizeSelector != 0)
            {
                tableBitmapSize = GetCustomHuffmanTable(customIndex, referredTo, customTables);
                customIndex++;
            }
            else
            {
                tableBitmapSize = GetStandardTable(1);
            }

            if (dictionary.AggregationInstancesSelector != 0)
            {
                tableAggregateInstances = GetCustomHuffmanTable(customIndex, referredTo, customTables);
            }
            else
            {
                tableAggregateInstances = GetStandardTable(1);
            }

            return HuffmanTables.CreateDelta(tableDeltaHeight, tableDeltaWidth, tableBitmapSize, tableAggregateInstances);
        }

        static Dictionary<int, byte[]> ReadUncompressedBitmap(ByteStream reader, int width, int height)
        {
            var bitmap = new Dictionary<int, byte[]>();
            for (var y = 0; y < height; y++)
            {
                var row = new byte[width];
                bitmap.Add(bitmap.Count, row);
                for (var x = 0; x < width; x++)
                {
                    row[x] = FiltersExtension.ToByte(reader.ReadBit());
                }
                reader.ByteAlign();
            }
            return bitmap;
        }

        static Dictionary<int, byte[]> DecodeMMRBitmap(ByteStream input, int width, int height, bool endOfBlock)
        {
            // MMR is the same compression algorithm as the PDF filter
            // CCITTFaxDecode with /K -1.
            var paramss = new CCITTFaxParams(K: -1, columns: width, rows: height, blackIs1: true, endOfBlock: endOfBlock);
            var decoder = new CCITTFaxDecoder(input, paramss);
            var bitmap = new Dictionary<int, byte[]>();
            int currentByte = 0;
            var eof = false;

            for (var y = 0; y < height; y++)
            {
                var row = new byte[width];
                bitmap.Add(y, row);
                var shift = -1;
                for (var x = 0; x < width; x++)
                {
                    if (shift < 0)
                    {
                        currentByte = decoder.ReadNextChar();
                        if (currentByte == -1)
                        {
                            // Set the rest of the bits to zero.
                            currentByte = 0;
                            eof = true;
                        }
                        shift = 7;
                    }
                    row[x] = FiltersExtension.ToByte((currentByte >> shift) & 1);
                    shift--;
                }
            }

            if (endOfBlock && !eof)
            {
                // Read until EOFB has been consumed.
                var lookForEOFLimit = 5;
                for (var i = 0; i < lookForEOFLimit; i++)
                {
                    if (decoder.ReadNextChar() == -1)
                    {
                        break;
                    }
                }
            }

            return bitmap;
        }

        // eslint-disable-next-line no-shadow

        public byte[] ParseChunks(List<ImageChunk> chunks)
        {
            return ParseJbig2Chunks(chunks);
        }

        public ImageData Parse(IInputStream data)
        {
            var imgData = ParseJbig2(data);
            width = imgData.Width;
            height = imgData.Height;
            return imgData;
        }

        internal class HuffmanTables
        {
            internal HuffmanTable TableDeltaHeight;
            internal HuffmanTable TableDeltaWidth;
            internal HuffmanTable TableBitmapSize;
            internal HuffmanTable TableAggregateInstances;
            internal HuffmanTable SymbolIDTable;
            internal HuffmanTable TableFirstS;
            internal HuffmanTable TableDeltaS;
            internal HuffmanTable tableDeltaT;

            public static HuffmanTables CreateDelta(HuffmanTable tableDeltaHeight, HuffmanTable tableDeltaWidth, HuffmanTable tableBitmapSize, HuffmanTable tableAggregateInstances)
            {
                return new HuffmanTables
                {
                    TableDeltaHeight = tableDeltaHeight,
                    TableDeltaWidth = tableDeltaWidth,
                    TableBitmapSize = tableBitmapSize,
                    TableAggregateInstances = tableAggregateInstances
                };
            }

            public static HuffmanTables CreateSymbol(HuffmanTable symbolIDTable, HuffmanTable tableFirstS, HuffmanTable tableDeltaS, HuffmanTable tableDeltaT)
            {
                return new HuffmanTables
                {
                    SymbolIDTable = symbolIDTable,
                    TableFirstS = tableFirstS,
                    TableDeltaS = tableDeltaS,
                    tableDeltaT = tableDeltaT
                };
            }
        }
    }

    internal class GenericRegion
    {
        internal RegionSegmentInformation Info;
        internal bool Mmr;
        internal int Template;
        internal bool Prediction;
        internal List<Point> At;

        public GenericRegion()
        {
        }
    }

    internal class HalftoneRegion
    {
        internal RegionSegmentInformation Info;
        internal bool Mmr;
        internal int Template;
        internal bool EnableSkip;
        internal int CombinationOperator;
        internal byte DefaultPixelValue;
        internal int GridWidth;
        internal int GridHeight;
        internal int GridOffsetX;
        internal int GridOffsetY;
        internal int GridVectorX;
        internal int GridVectorY;

        public HalftoneRegion()
        {
        }
    }

    internal class PatternDictionary
    {
        internal bool Mmr;
        internal int Template;
        internal byte PatternWidth;
        internal byte PatternHeight;
        internal int MaxPatternIndex;

        public PatternDictionary()
        {
        }
    }

    internal class TextRegion
    {
        internal RegionSegmentInformation Info;
        internal bool Huffman;
        internal bool Refinement;
        internal int LogStripSize;
        internal int StripSize;
        internal int ReferenceCorner;
        internal bool Transposed;
        internal int CombinationOperator;
        internal byte DefaultPixelValue;
        internal int DsOffset;
        internal int RefinementTemplate;
        internal int HuffmanFS;
        internal int HuffmanDS;
        internal int HuffmanDT;
        internal int HuffmanRefinementDW;
        internal int HuffmanRefinementDH;
        internal int HuffmanRefinementDX;
        internal int HuffmanRefinementDY;
        internal bool huffmanRefinementSizeSelector;
        internal List<Point> refinementAt;
        internal int numberOfSymbolInstances;

        public TextRegion()
        {
        }
    }

    internal class SymbolDictionary
    {
        internal bool Huffman;
        internal bool Refinement;
        internal int HuffmanDHSelector;
        internal int HuffmanDWSelector;
        internal int BitmapSizeSelector;
        internal int AggregationInstancesSelector;
        internal bool BitmapCodingContextUsed;
        internal bool BitmapCodingContextRetained;
        internal int Template;
        internal int RefinementTemplate;
        internal List<Point> At;
        internal List<Point> RefinementAt;
        internal int NumberOfExportedSymbols;
        internal int NumberOfNewSymbols;

        public SymbolDictionary()
        {
        }
    }

    internal class Segment
    {
        internal SegmentHeader Header;
        internal Memory<byte> Data;

        public Segment(SegmentHeader header)
        {
            Header = header;
        }
    }

    internal class PageInformation
    {
        internal uint Width;
        internal uint Height;
        internal uint ResolutionX;
        internal uint ResolutionY;
        internal bool Lossless;
        internal bool Refinement;
        internal int DefaultPixelValue;
        internal int CombinationOperator;
        internal bool RequiresBuffer;
        internal bool CombinationOperatorOverride;

        public PageInformation(uint width, uint height, uint resolutionX, uint resolutionY)
        {
            Width = width;
            Height = height;
            ResolutionX = resolutionX;
            ResolutionY = resolutionY;
        }
    }

    internal class RegionSegmentInformation
    {
        internal uint Width;
        internal uint Height;
        internal uint X;
        internal uint Y;
        internal int CombinationOperator;

        public RegionSegmentInformation(uint width, uint height, uint x, uint y, int combinationOperator)
        {
            Width = width;
            Height = height;
            X = x;
            Y = y;
            CombinationOperator = combinationOperator;
        }
    }

    internal class SegmentHeader
    {
        internal int Number;
        internal int Type;
        internal string TypeName;
        internal bool DeferredNonRetain;
        internal List<int> ReferredTo;
        internal int PageAssociation;
        internal uint Length;
        internal List<int> RetainBits;
        internal bool RandomAccess;
        internal int NumberOfPages;

        public SegmentHeader()
        {
        }
    }

    internal class ImageData
    {
        internal byte[] ImgData;
        internal int Width;
        internal int Height;

        public ImageData(byte[] imgData, int width, int height)
        {
            ImgData = imgData;
            Width = width;
            Height = height;
        }
    }

    internal class Refinement
    {
        internal Point[] Coding;
        internal Point[] Reference;

        public Refinement(Point[] coding, Point[] reference)
        {
            Coding = coding;
            Reference = reference;
        }
    }

    internal struct Point
    {
        internal sbyte X;
        internal sbyte Y;

        public Point(sbyte x, sbyte y)
        {
            X = x;
            Y = y;
        }
    }
}


