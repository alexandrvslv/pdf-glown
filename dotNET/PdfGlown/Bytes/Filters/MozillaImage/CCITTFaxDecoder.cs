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
/* Copyright 1996-2003 Glyph & Cog, LLC
 *
 * The CCITT stream implementation contained in this file is a JavaScript port
 * of XPDF"s implementation, made available under the Apache 2.0 open source
 * license.
 */


using System.Diagnostics;
/**
* @typedef {Object} CCITTFaxDecoderSource
* @property {function} next - Method that return one byte of data for decoding,
*   or -1 when EOF is reached.
*/
namespace PdfGlown.Bytes.Filters.CCITT
{

    internal class CCITTFaxDecoder
    {
        const int ccittEOL = -2;
        const int ccittEOF = -1;
        const int twoDimPass = 0;
        const int twoDimHoriz = 1;
        const int twoDimVert0 = 2;
        const int twoDimVertR1 = 3;
        const int twoDimVertL1 = 4;
        const int twoDimVertR2 = 5;
        const int twoDimVertL2 = 6;
        const int twoDimVertR3 = 7;
        const int twoDimVertL3 = 8;

        // prettier-ignore
        static readonly int[][] twoDimTable = new int[][]{
          new []{-1, -1}, new []{-1, -1},                   // 000000x
          new []{7, twoDimVertL3},                    // 0000010
          new []{7, twoDimVertR3},                    // 0000011
          new []{6, twoDimVertL2}, new []{6, twoDimVertL2}, // 000010x
          new []{6, twoDimVertR2}, new []{6, twoDimVertR2}, // 000011x
          new []{4, twoDimPass}, new []{4, twoDimPass},     // 0001xxx
          new []{4, twoDimPass}, new []{4, twoDimPass},
          new []{4, twoDimPass}, new []{4, twoDimPass},
          new []{4, twoDimPass}, new []{4, twoDimPass},
          new []{3, twoDimHoriz}, new []{3, twoDimHoriz},   // 001xxxx
          new []{3, twoDimHoriz}, new []{3, twoDimHoriz},
          new []{3, twoDimHoriz}, new []{3, twoDimHoriz},
          new []{3, twoDimHoriz}, new []{3, twoDimHoriz},
          new []{3, twoDimHoriz}, new []{3, twoDimHoriz},
          new []{3, twoDimHoriz}, new []{3, twoDimHoriz},
          new []{3, twoDimHoriz}, new []{3, twoDimHoriz},
          new []{3, twoDimHoriz}, new []{3, twoDimHoriz},
          new []{3, twoDimVertL1}, new []{3, twoDimVertL1}, // 010xxxx
          new []{3, twoDimVertL1}, new []{3, twoDimVertL1},
          new []{3, twoDimVertL1}, new []{3, twoDimVertL1},
          new []{3, twoDimVertL1}, new []{3, twoDimVertL1},
          new []{3, twoDimVertL1}, new []{3, twoDimVertL1},
          new []{3, twoDimVertL1}, new []{3, twoDimVertL1},
          new []{3, twoDimVertL1}, new []{3, twoDimVertL1},
          new []{3, twoDimVertL1}, new []{3, twoDimVertL1},
          new []{3, twoDimVertR1}, new []{3, twoDimVertR1}, // 011xxxx
          new []{3, twoDimVertR1}, new []{3, twoDimVertR1},
          new []{3, twoDimVertR1}, new []{3, twoDimVertR1},
          new []{3, twoDimVertR1}, new []{3, twoDimVertR1},
          new []{3, twoDimVertR1}, new []{3, twoDimVertR1},
          new []{3, twoDimVertR1}, new []{3, twoDimVertR1},
          new []{3, twoDimVertR1}, new []{3, twoDimVertR1},
          new []{3, twoDimVertR1}, new []{3, twoDimVertR1},
          new []{1, twoDimVert0}, new []{1, twoDimVert0},   // 1xxxxxx
          new []{1, twoDimVert0}, new []{1, twoDimVert0},
          new []{1, twoDimVert0}, new []{1, twoDimVert0},
          new []{1, twoDimVert0}, new []{1, twoDimVert0},
          new []{1, twoDimVert0}, new []{1, twoDimVert0},
          new []{1, twoDimVert0}, new []{1, twoDimVert0},
          new []{1, twoDimVert0}, new []{1, twoDimVert0},
          new []{1, twoDimVert0}, new []{1, twoDimVert0},
          new []{1, twoDimVert0}, new []{1, twoDimVert0},
          new []{1, twoDimVert0}, new []{1, twoDimVert0},
          new []{1, twoDimVert0}, new []{1, twoDimVert0},
          new []{1, twoDimVert0}, new []{1, twoDimVert0},
          new []{1, twoDimVert0}, new []{1, twoDimVert0},
          new []{1, twoDimVert0}, new []{1, twoDimVert0},
          new []{1, twoDimVert0}, new []{1, twoDimVert0},
          new []{1, twoDimVert0}, new []{1, twoDimVert0},
          new []{1, twoDimVert0}, new []{1, twoDimVert0},
          new []{1, twoDimVert0}, new []{1, twoDimVert0},
          new []{1, twoDimVert0}, new []{1, twoDimVert0},
          new []{1, twoDimVert0}, new []{1, twoDimVert0},
          new []{1, twoDimVert0}, new []{1, twoDimVert0},
          new []{1, twoDimVert0}, new []{1, twoDimVert0},
          new []{1, twoDimVert0}, new []{1, twoDimVert0},
          new []{1, twoDimVert0}, new []{1, twoDimVert0},
          new []{1, twoDimVert0}, new []{1, twoDimVert0},
          new []{1, twoDimVert0}, new []{1, twoDimVert0},
          new []{1, twoDimVert0}, new []{1, twoDimVert0},
          new []{1, twoDimVert0}, new []{1, twoDimVert0},
          new []{1, twoDimVert0}, new []{1, twoDimVert0},
          new []{1, twoDimVert0}, new []{1, twoDimVert0},
          new []{1, twoDimVert0}, new []{1, twoDimVert0},
          new []{1, twoDimVert0}, new []{1, twoDimVert0 }
        };

        // prettier-ignore
        static readonly int[][] whiteTable1 = new int[][]{
          new []{-1, -1},                               // 00000
          new []{12, ccittEOL},                         // 00001
          new []{-1, -1}, new []{-1, -1},                     // 0001x
          new []{-1, -1}, new []{-1, -1}, new []{-1, -1}, new []{-1, -1}, // 001xx
          new []{-1, -1}, new []{-1, -1}, new []{-1, -1}, new []{-1, -1}, // 010xx
          new []{-1, -1}, new []{-1, -1}, new []{-1, -1}, new []{-1, -1}, // 011xx
          new []{11, 1792}, new []{11, 1792},                 // 1000x
          new []{12, 1984},                             // 10010
          new []{12, 2048},                             // 10011
          new []{12, 2112},                             // 10100
          new []{12, 2176},                             // 10101
          new []{12, 2240},                             // 10110
          new []{12, 2304},                             // 10111
          new []{11, 1856}, new []{11, 1856},                 // 1100x
          new []{11, 1920}, new []{11, 1920},                 // 1101x
          new []{12, 2368},                             // 11100
          new []{12, 2432},                             // 11101
          new []{12, 2496},                             // 11110
          new []{12, 2560}                              // 11111
        };

        // prettier-ignore
        static readonly int[][] whiteTable2 = new int[][]{
            new []{-1, -1}, new []{-1, -1}, new []{-1, -1}, new []{-1, -1},     // 0000000xx
            new []{8, 29}, new []{8, 29},                           // 00000010x
            new []{8, 30}, new []{8, 30},                           // 00000011x
            new []{8, 45}, new []{8, 45},                           // 00000100x         
            new []{8, 46}, new []{8, 46},                           // 00000101x
            new []{7, 22}, new []{7, 22}, new []{7, 22}, new []{7, 22},         // 0000011xx
            new []{7, 23}, new []{7, 23}, new []{7, 23}, new []{7, 23},         // 0000100xx
            new []{8, 47}, new []{8, 47},                           // 00001010x
            new []{8, 48}, new []{8, 48},                           // 00001011x
            new []{6, 13}, new []{6, 13}, new []{6, 13}, new []{6, 13},         // 000011xxx
            new []{6, 13}, new []{6, 13}, new []{6, 13}, new []{6, 13},
            new []{7, 20}, new []{7, 20}, new []{7, 20}, new []{7, 20},         // 0001000xx
            new []{8, 33}, new []{8, 33},                           // 00010010x
            new []{8, 34}, new []{8, 34},                           // 00010011x
            new []{8, 35}, new []{8, 35},                           // 00010100x
            new []{8, 36}, new []{8, 36},                           // 00010101x
            new []{8, 37}, new []{8, 37},                           // 00010110x
            new []{8, 38}, new []{8, 38},                           // 00010111x
            new []{7, 19}, new []{7, 19}, new []{7, 19}, new []{7, 19},         // 0001100xx
            new []{8, 31}, new []{8, 31},                           // 00011010x
            new []{8, 32}, new []{8, 32},                           // 00011011x
            new []{6, 1}, new []{6, 1}, new []{6, 1}, new []{6, 1},             // 000111xxx
            new []{6, 1}, new []{6, 1}, new []{6, 1}, new []{6, 1},
            new []{6, 12}, new []{6, 12}, new []{6, 12}, new []{6, 12},         // 001000xxx
            new []{6, 12}, new []{6, 12}, new []{6, 12}, new []{6, 12},
            new []{8, 53}, new []{8, 53},                           // 00100100x
            new []{8, 54}, new []{8, 54},                           // 00100101x
            new []{7, 26}, new []{7, 26}, new []{7, 26}, new []{7, 26},         // 0010011xx
            new []{8, 39}, new []{8, 39},                           // 00101000x
            new []{8, 40}, new []{8, 40},                           // 00101001x
            new []{8, 41}, new []{8, 41},                           // 00101010x
            new []{8, 42}, new []{8, 42},                           // 00101011x
            new []{8, 43}, new []{8, 43},                           // 00101100x
            new []{8, 44}, new []{8, 44},                           // 00101101x
            new []{7, 21}, new []{7, 21}, new []{7, 21}, new []{7, 21},         // 0010111xx
            new []{7, 28}, new []{7, 28}, new []{7, 28}, new []{7, 28},         // 0011000xx
            new []{8, 61}, new []{8, 61},                           // 00110010x
            new []{8, 62}, new []{8, 62},                           // 00110011x
            new []{8, 63}, new []{8, 63},                           // 00110100x
            new []{8, 0}, new []{8, 0},                             // 00110101x
            new []{8, 320}, new []{8, 320},                         // 00110110x
            new []{8, 384}, new []{8, 384},                         // 00110111x
            new []{5, 10}, new []{5, 10}, new []{5, 10}, new []{5, 10},         // 00111xxxx
            new []{5, 10}, new []{5, 10}, new []{5, 10}, new []{5, 10},
            new []{5, 10}, new []{5, 10}, new []{5, 10}, new []{5, 10},
            new []{5, 10}, new []{5, 10}, new []{5, 10}, new []{5, 10},
            new []{5, 11}, new []{5, 11}, new []{5, 11}, new []{5, 11},         // 01000xxxx
            new []{5, 11}, new []{5, 11}, new []{5, 11}, new []{5, 11},
            new []{5, 11}, new []{5, 11}, new []{5, 11}, new []{5, 11},
            new []{5, 11}, new []{5, 11}, new []{5, 11}, new []{5, 11},
            new []{7, 27}, new []{7, 27}, new []{7, 27}, new []{7, 27},         // 0100100xx
            new []{8, 59}, new []{8, 59},                           // 01001010x
            new []{8, 60}, new []{8, 60},                           // 01001011x
            new []{9, 1472},                                  // 010011000
            new []{9, 1536},                                  // 010011001
            new []{9, 1600},                                  // 010011010
            new []{9, 1728},                                  // 010011011
            new []{7, 18}, new []{7, 18}, new []{7, 18}, new []{7, 18},         // 0100111xx
            new []{7, 24}, new []{7, 24}, new []{7, 24}, new []{7, 24},         // 0101000xx
            new []{8, 49}, new []{8, 49},                           // 01010010x
            new []{8, 50}, new []{8, 50},                           // 01010011x
            new []{8, 51}, new []{8, 51},                           // 01010100x    
            new []{8, 52}, new []{8, 52},                           // 01010101x
            new []{7, 25}, new []{7, 25}, new []{7, 25}, new []{7, 25},         // 0101011xx
            new []{8, 55}, new []{8, 55},                           // 01011000x
            new []{8, 56}, new []{8, 56},                           // 01011001x
            new []{8, 57}, new []{8, 57},                           // 01011010x
            new []{8, 58}, new []{8, 58},                           // 01011011x
            new []{6, 192}, new []{6, 192}, new []{6, 192}, new []{6, 192},     // 010111xxx
            new []{6, 192}, new []{6, 192}, new []{6, 192}, new []{6, 192},
            new []{6, 1664}, new []{6, 1664}, new []{6, 1664}, new []{6, 1664}, // 011000xxx
            new []{6, 1664}, new []{6, 1664}, new []{6, 1664}, new []{6, 1664},
            new []{8, 448}, new []{8, 448},                         // 01100100x
            new []{8, 512}, new []{8, 512},                         // 01100101x
            new []{9, 704},                                   // 011001100
            new []{9, 768},                                   // 011001101
            new []{8, 640}, new []{8, 640},                         // 01100111x
            new []{8, 576}, new []{8, 576},                         // 01101000x
            new []{9, 832},                                   // 011010010
            new []{9, 896},                                   // 011010011
            new []{9, 960},                                   // 011010100
            new []{9, 1024},                                  // 011010101
            new []{9, 1088},                                  // 011010110
            new []{9, 1152},                                  // 011010111
            new []{9, 1216},                                  // 011011000
            new []{9, 1280},                                  // 011011001
            new []{9, 1344},                                  // 011011010
            new []{9, 1408},                                  // 011011011
            new []{7, 256}, new []{7, 256}, new []{7, 256}, new []{7, 256},     // 0110111xx
            new []{4, 2}, new []{4, 2}, new []{4, 2}, new []{4, 2},             // 0111xxxxx
            new []{4, 2}, new []{4, 2}, new []{4, 2}, new []{4, 2},
            new []{4, 2}, new []{4, 2}, new []{4, 2}, new []{4, 2},
            new []{4, 2}, new []{4, 2}, new []{4, 2}, new []{4, 2},
            new []{4, 2}, new []{4, 2}, new []{4, 2}, new []{4, 2},
            new []{4, 2}, new []{4, 2}, new []{4, 2}, new []{4, 2},
            new []{4, 2}, new []{4, 2}, new []{4, 2}, new []{4, 2},
            new []{4, 2}, new []{4, 2}, new []{4, 2}, new []{4, 2},
            new []{4, 3}, new []{4, 3}, new []{4, 3}, new []{4, 3},             // 1000xxxxx
            new []{4, 3}, new []{4, 3}, new []{4, 3}, new []{4, 3},
            new []{4, 3}, new []{4, 3}, new []{4, 3}, new []{4, 3},
            new []{4, 3}, new []{4, 3}, new []{4, 3}, new []{4, 3},
            new []{4, 3}, new []{4, 3}, new []{4, 3}, new []{4, 3},
            new []{4, 3}, new []{4, 3}, new []{4, 3}, new []{4, 3},
            new []{4, 3}, new []{4, 3}, new []{4, 3}, new []{4, 3},
            new []{4, 3}, new []{4, 3}, new []{4, 3}, new []{4, 3},
            new []{5, 128}, new []{5, 128}, new []{5, 128}, new []{5, 128},     // 10010xxxx
            new []{5, 128}, new []{5, 128}, new []{5, 128}, new []{5, 128},
            new []{5, 128}, new []{5, 128}, new []{5, 128}, new []{5, 128},
            new []{5, 128}, new []{5, 128}, new []{5, 128}, new []{5, 128},
            new []{5, 8}, new []{5, 8}, new []{5, 8}, new []{5, 8},             // 10011xxxx
            new []{5, 8}, new []{5, 8}, new []{5, 8}, new []{5, 8},
            new []{5, 8}, new []{5, 8}, new []{5, 8}, new []{5, 8},
            new []{5, 8}, new []{5, 8}, new []{5, 8}, new []{5, 8},
            new []{5, 9}, new []{5, 9}, new []{5, 9}, new []{5, 9},             // 10100xxxx
            new []{5, 9}, new []{5, 9}, new []{5, 9}, new []{5, 9},
            new []{5, 9}, new []{5, 9}, new []{5, 9}, new []{5, 9},
            new []{5, 9}, new []{5, 9}, new []{5, 9}, new []{5, 9},
            new []{6, 16}, new []{6, 16}, new []{6, 16}, new []{6, 16},         // 101010xxx
            new []{6, 16}, new []{6, 16}, new []{6, 16}, new []{6, 16},
            new []{6, 17}, new []{6, 17}, new []{6, 17}, new []{6, 17},         // 101011xxx
            new []{6, 17}, new []{6, 17}, new []{6, 17}, new []{6, 17},
            new []{4, 4}, new []{4, 4}, new []{4, 4}, new []{4, 4},             // 1011xxxxx
            new []{4, 4}, new []{4, 4}, new []{4, 4}, new []{4, 4},
            new []{4, 4}, new []{4, 4}, new []{4, 4}, new []{4, 4},
            new []{4, 4}, new []{4, 4}, new []{4, 4}, new []{4, 4},
            new []{4, 4}, new []{4, 4}, new []{4, 4}, new []{4, 4},
            new []{4, 4}, new []{4, 4}, new []{4, 4}, new []{4, 4},
            new []{4, 4}, new []{4, 4}, new []{4, 4}, new []{4, 4},
            new []{4, 4}, new []{4, 4}, new []{4, 4}, new []{4, 4},
            new []{4, 5}, new []{4, 5}, new []{4, 5}, new []{4, 5},             // 1100xxxxx
            new []{4, 5}, new []{4, 5}, new []{4, 5}, new []{4, 5},
            new []{4, 5}, new []{4, 5}, new []{4, 5}, new []{4, 5},
            new []{4, 5}, new []{4, 5}, new []{4, 5}, new []{4, 5},
            new []{4, 5}, new []{4, 5}, new []{4, 5}, new []{4, 5},
            new []{4, 5}, new []{4, 5}, new []{4, 5}, new []{4, 5},
            new []{4, 5}, new []{4, 5}, new []{4, 5}, new []{4, 5},
            new []{4, 5}, new []{4, 5}, new []{4, 5}, new []{4, 5},
            new []{6, 14}, new []{6, 14}, new []{6, 14}, new []{6, 14},         // 110100xxx
            new []{6, 14}, new []{6, 14}, new []{6, 14}, new []{6, 14},
            new []{6, 15}, new []{6, 15}, new []{6, 15}, new []{6, 15},         // 110101xxx
            new []{6, 15}, new []{6, 15}, new []{6, 15}, new []{6, 15},
            new []{5, 64}, new []{5, 64}, new []{5, 64}, new []{5, 64},         // 11011xxxx
            new []{5, 64}, new []{5, 64}, new []{5, 64}, new []{5, 64},
            new []{5, 64}, new []{5, 64}, new []{5, 64}, new []{5, 64},
            new []{5, 64}, new []{5, 64}, new []{5, 64}, new []{5, 64},
            new []{4, 6}, new []{4, 6}, new []{4, 6}, new []{4, 6},             // 1110xxxxx
            new []{4, 6}, new []{4, 6}, new []{4, 6}, new []{4, 6},
            new []{4, 6}, new []{4, 6}, new []{4, 6}, new []{4, 6},
            new []{4, 6}, new []{4, 6}, new []{4, 6}, new []{4, 6},
            new []{4, 6}, new []{4, 6}, new []{4, 6}, new []{4, 6},
            new []{4, 6}, new []{4, 6}, new []{4, 6}, new []{4, 6},
            new []{4, 6}, new []{4, 6}, new []{4, 6}, new []{4, 6},
            new []{4, 6}, new []{4, 6}, new []{4, 6}, new []{4, 6},
            new []{4, 7}, new []{4, 7}, new []{4, 7}, new []{4, 7},             // 1111xxxxx
            new []{4, 7}, new []{4, 7}, new []{4, 7}, new []{4, 7},
            new []{4, 7}, new []{4, 7}, new []{4, 7}, new []{4, 7},
            new []{4, 7}, new []{4, 7}, new []{4, 7}, new []{4, 7},
            new []{4, 7}, new []{4, 7}, new []{4, 7}, new []{4, 7},
            new []{4, 7}, new []{4, 7}, new []{4, 7}, new []{4, 7},
            new []{4, 7}, new []{4, 7}, new []{4, 7}, new []{4, 7},
            new []{4, 7}, new []{4, 7}, new []{4, 7}, new []{4, 7 }
        };


        // prettier-ignore
        static readonly int[][] blackTable1 = new int[][]{
          new []{-1, -1}, new []{-1, -1},                             // 000000000000x
          new []{12, ccittEOL}, new []{12, ccittEOL},                 // 000000000001x
          new []{-1, -1}, new []{-1, -1}, new []{-1, -1}, new []{-1, -1},         // 00000000001xx
          new []{-1, -1}, new []{-1, -1}, new []{-1, -1}, new []{-1, -1},         // 00000000010xx
          new []{-1, -1}, new []{-1, -1}, new []{-1, -1}, new []{-1, -1},         // 00000000011xx
          new []{-1, -1}, new []{-1, -1}, new []{-1, -1}, new []{-1, -1},         // 00000000100xx
          new []{-1, -1}, new []{-1, -1}, new []{-1, -1}, new []{-1, -1},         // 00000000101xx
          new []{-1, -1}, new []{-1, -1}, new []{-1, -1}, new []{-1, -1},         // 00000000110xx
          new []{-1, -1}, new []{-1, -1}, new []{-1, -1}, new []{-1, -1},         // 00000000111xx
          new []{11, 1792}, new []{11, 1792}, new []{11, 1792}, new []{11, 1792}, // 00000001000xx
          new []{12, 1984}, new []{12, 1984},                         // 000000010010x
          new []{12, 2048}, new []{12, 2048},                         // 000000010011x
          new []{12, 2112}, new []{12, 2112},                         // 000000010100x
          new []{12, 2176}, new []{12, 2176},                         // 000000010101x
          new []{12, 2240}, new []{12, 2240},                         // 000000010110x
          new []{12, 2304}, new []{12, 2304},                         // 000000010111x
          new []{11, 1856}, new []{11, 1856}, new []{11, 1856}, new []{11, 1856}, // 00000001100xx
          new []{11, 1920}, new []{11, 1920}, new []{11, 1920}, new []{11, 1920}, // 00000001101xx
          new []{12, 2368}, new []{12, 2368},                         // 000000011100x
          new []{12, 2432}, new []{12, 2432},                         // 000000011101x
          new []{12, 2496}, new []{12, 2496},                         // 000000011110x
          new []{12, 2560}, new []{12, 2560},                         // 000000011111x
          new []{10, 18}, new []{10, 18}, new []{10, 18}, new []{10, 18},         // 0000001000xxx
          new []{10, 18}, new []{10, 18}, new []{10, 18}, new []{10, 18},
          new []{12, 52}, new []{12, 52},                             // 000000100100x
          new []{13, 640},                                      // 0000001001010
          new []{13, 704},                                      // 0000001001011
          new []{13, 768},                                      // 0000001001100
          new []{13, 832},                                      // 0000001001101
          new []{12, 55}, new []{12, 55},                             // 000000100111x
          new []{12, 56}, new []{12, 56},                             // 000000101000x
          new []{13, 1280},                                     // 0000001010010
          new []{13, 1344},                                     // 0000001010011
          new []{13, 1408},                                     // 0000001010100
          new []{13, 1472},                                     // 0000001010101
          new []{12, 59}, new []{12, 59},                             // 000000101011x
          new []{12, 60}, new []{12, 60},                             // 000000101100x
          new []{13, 1536},                                     // 0000001011010
          new []{13, 1600},                                     // 0000001011011
          new []{11, 24}, new []{11, 24}, new []{11, 24}, new []{11, 24},         // 00000010111xx
          new []{11, 25}, new []{11, 25}, new []{11, 25}, new []{11, 25},         // 00000011000xx
          new []{13, 1664},                                     // 0000001100100
          new []{13, 1728},                                     // 0000001100101
          new []{12, 320}, new []{12, 320},                           // 000000110011x
          new []{12, 384}, new []{12, 384},                           // 000000110100x
          new []{12, 448}, new []{12, 448},                           // 000000110101x
          new []{13, 512},                                      // 0000001101100
          new []{13, 576},                                      // 0000001101101
          new []{12, 53}, new []{12, 53},                             // 000000110111x
          new []{12, 54}, new []{12, 54},                             // 000000111000x
          new []{13, 896},                                      // 0000001110010
          new []{13, 960},                                      // 0000001110011
          new []{13, 1024},                                     // 0000001110100
          new []{13, 1088},                                     // 0000001110101
          new []{13, 1152},                                     // 0000001110110
          new []{13, 1216},                                     // 0000001110111
          new []{10, 64}, new []{10, 64}, new []{10, 64}, new []{10, 64},         // 0000001111xxx
          new []{10, 64}, new []{10, 64}, new []{10, 64}, new []{10, 64 }
        };

        // prettier-ignore
        static readonly int[][] blackTable2 = new int[][]{
          new []{8, 13}, new []{8, 13}, new []{8, 13}, new []{8, 13},     // 00000100xxxx
          new []{8, 13}, new []{8, 13}, new []{8, 13}, new []{8, 13},
          new []{8, 13}, new []{8, 13}, new []{8, 13}, new []{8, 13},
          new []{8, 13}, new []{8, 13}, new []{8, 13}, new []{8, 13},
          new []{11, 23}, new []{11, 23},                     // 00000101000x
          new []{12, 50},                               // 000001010010
          new []{12, 51},                               // 000001010011
          new []{12, 44},                               // 000001010100
          new []{12, 45},                               // 000001010101
          new []{12, 46},                               // 000001010110
          new []{12, 47},                               // 000001010111
          new []{12, 57},                               // 000001011000
          new []{12, 58},                               // 000001011001
          new []{12, 61},                               // 000001011010
          new []{12, 256},                              // 000001011011
          new []{10, 16}, new []{10, 16}, new []{10, 16}, new []{10, 16}, // 0000010111xx
          new []{10, 17}, new []{10, 17}, new []{10, 17}, new []{10, 17}, // 0000011000xx
          new []{12, 48},                               // 000001100100
          new []{12, 49},                               // 000001100101
          new []{12, 62},                               // 000001100110
          new []{12, 63},                               // 000001100111
          new []{12, 30},                               // 000001101000
          new []{12, 31},                               // 000001101001
          new []{12, 32},                               // 000001101010
          new []{12, 33},                               // 000001101011
          new []{12, 40},                               // 000001101100
          new []{12, 41},                               // 000001101101
          new []{11, 22}, new []{11, 22},                     // 00000110111x
          new []{8, 14}, new []{8, 14}, new []{8, 14}, new []{8, 14},     // 00000111xxxx
          new []{8, 14}, new []{8, 14}, new []{8, 14}, new []{8, 14},
          new []{8, 14}, new []{8, 14}, new []{8, 14}, new []{8, 14},
          new []{8, 14}, new []{8, 14}, new []{8, 14}, new []{8, 14},
          new []{7, 10}, new []{7, 10}, new []{7, 10}, new []{7, 10},     // 0000100xxxxx
          new []{7, 10}, new []{7, 10}, new []{7, 10}, new []{7, 10},
          new []{7, 10}, new []{7, 10}, new []{7, 10}, new []{7, 10},
          new []{7, 10}, new []{7, 10}, new []{7, 10}, new []{7, 10},
          new []{7, 10}, new []{7, 10}, new []{7, 10}, new []{7, 10},
          new []{7, 10}, new []{7, 10}, new []{7, 10}, new []{7, 10},
          new []{7, 10}, new []{7, 10}, new []{7, 10}, new []{7, 10},
          new []{7, 10}, new []{7, 10}, new []{7, 10}, new []{7, 10},
          new []{7, 11}, new []{7, 11}, new []{7, 11}, new []{7, 11},     // 0000101xxxxx
          new []{7, 11}, new []{7, 11}, new []{7, 11}, new []{7, 11},
          new []{7, 11}, new []{7, 11}, new []{7, 11}, new []{7, 11},
          new []{7, 11}, new []{7, 11}, new []{7, 11}, new []{7, 11},
          new []{7, 11}, new []{7, 11}, new []{7, 11}, new []{7, 11},
          new []{7, 11}, new []{7, 11}, new []{7, 11}, new []{7, 11},
          new []{7, 11}, new []{7, 11}, new []{7, 11}, new []{7, 11},
          new []{7, 11}, new []{7, 11}, new []{7, 11}, new []{7, 11},
          new []{9, 15}, new []{9, 15}, new []{9, 15}, new []{9, 15},     // 000011000xxx
          new []{9, 15}, new []{9, 15}, new []{9, 15}, new []{9, 15},
          new []{12, 128},                              // 000011001000
          new []{12, 192},                              // 000011001001
          new []{12, 26},                               // 000011001010
          new []{12, 27},                               // 000011001011
          new []{12, 28},                               // 000011001100
          new []{12, 29},                               // 000011001101
          new []{11, 19}, new []{11, 19},                     // 00001100111x
          new []{11, 20}, new []{11, 20},                     // 00001101000x
          new []{12, 34},                               // 000011010010
          new []{12, 35},                               // 000011010011
          new []{12, 36},                               // 000011010100
          new []{12, 37},                               // 000011010101
          new []{12, 38},                               // 000011010110
          new []{12, 39},                               // 000011010111
          new []{11, 21}, new []{11, 21},                     // 00001101100x
          new []{12, 42},                               // 000011011010
          new []{12, 43},                               // 000011011011
          new []{10, 0}, new []{10, 0}, new []{10, 0}, new []{10, 0},     // 0000110111xx
          new []{7, 12}, new []{7, 12}, new []{7, 12}, new []{7, 12},     // 0000111xxxxx
          new []{7, 12}, new []{7, 12}, new []{7, 12}, new []{7, 12},
          new []{7, 12}, new []{7, 12}, new []{7, 12}, new []{7, 12},
          new []{7, 12}, new []{7, 12}, new []{7, 12}, new []{7, 12},
          new []{7, 12}, new []{7, 12}, new []{7, 12}, new []{7, 12},
          new []{7, 12}, new []{7, 12}, new []{7, 12}, new []{7, 12},
          new []{7, 12}, new []{7, 12}, new []{7, 12}, new []{7, 12},
          new []{7, 12}, new []{7, 12}, new []{7, 12}, new []{7, 12 }
        };

        // prettier-ignore
        private static readonly int[][] blackTable3 = new int[][]{
          new []{-1, -1}, new []{-1, -1}, new []{-1, -1}, new []{-1, -1}, // 0000xx
          new []{6, 9},                                 // 000100
          new []{6, 8},                                 // 000101
          new []{5, 7}, new []{5, 7},                         // 00011x
          new []{4, 6}, new []{4, 6}, new []{4, 6}, new []{4, 6},         // 0010xx
          new []{4, 5}, new []{4, 5}, new []{4, 5}, new []{4, 5},         // 0011xx
          new []{3, 1}, new []{3, 1}, new []{3, 1}, new []{3, 1},         // 010xxx
          new []{3, 1}, new []{3, 1}, new []{3, 1}, new []{3, 1},
          new []{3, 4}, new []{3, 4}, new []{3, 4}, new []{3, 4},         // 011xxx
          new []{3, 4}, new []{3, 4}, new []{3, 4}, new []{3, 4},
          new []{2, 3}, new []{2, 3}, new []{2, 3}, new []{2, 3},         // 10xxxx
          new []{2, 3}, new []{2, 3}, new []{2, 3}, new []{2, 3},
          new []{2, 3}, new []{2, 3}, new []{2, 3}, new []{2, 3},
          new []{2, 3}, new []{2, 3}, new []{2, 3}, new []{2, 3},
          new []{2, 2}, new []{2, 2}, new []{2, 2}, new []{2, 2},         // 11xxxx
          new []{2, 2}, new []{2, 2}, new []{2, 2}, new []{2, 2},
          new []{2, 2}, new []{2, 2}, new []{2, 2}, new []{2, 2},
          new []{2, 2}, new []{2, 2}, new []{2, 2}, new []{2, 2 }
        };

        private IInputStream source;
        private bool eof;
        private int encoding;
        private bool eoline;
        private bool byteAlign;
        private uint columns;
        private uint rows;
        private bool eoblock;
        private bool black;
        private uint[] codingLine;
        private uint[] refLine;
        private int codingPos;
        private int row;
        private bool nextLine2D;
        private int inputBits;
        private int inputBuf;
        private int outputBits;
        private bool rowsDone;
        private bool err;

        /**
         * @param {CCITTFaxDecoderSource} source - The data which should be decoded.
         * @param {Object} [options] - Decoding options.
         */
        public CCITTFaxDecoder(IInputStream source, CCITTFaxParams options)
        {
            this.source = source;
            eof = false;

            encoding = options?.K ?? 0;
            eoline = options?.EndOfLine ?? false;
            byteAlign = options?.EncodedByteAlign ?? false;
            columns = (uint)(options?.Columns ?? 1728);
            rows = (uint)(options?.Rows ?? 0);
            var eoblock = options?.EndOfBlock ?? true;
            this.eoblock = eoblock;
            black = options?.BlackIs1 ?? false;

            codingLine = new uint[columns + 1];
            refLine = new uint[columns + 2];

            codingLine[0] = columns;
            codingPos = 0;

            row = 0;
            nextLine2D = encoding < 0;
            inputBits = 0;
            inputBuf = 0;
            outputBits = 0;
            rowsDone = false;

            int code1;
            while ((code1 = LookBits(12)) == 0)
            {
                EatBits(1);
            }
            if (code1 == 1)
            {
                EatBits(12);
            }
            if (encoding > 0)
            {
                nextLine2D = LookBits(1) == 0;
                EatBits(1);
            }
        }

        public int ReadNextChar()
        {
            if (eof)
            {
                return -1;
            }
            var refLine = this.refLine;
            var codingLine = this.codingLine;
            var columns = this.columns;

            int refPos, blackPixels, i;
            int bits;

            if (outputBits == 0)
            {
                if (rowsDone)
                {
                    eof = true;
                }
                if (eof)
                {
                    return -1;
                }
                err = false;

                int code1, code2, code3;
                if (nextLine2D)
                {
                    for (i = 0; codingLine[i] < columns; ++i)
                    {
                        refLine[i] = codingLine[i];
                    }
                    refLine[i++] = columns;
                    refLine[i] = columns;
                    codingLine[0] = 0;
                    codingPos = 0;
                    refPos = 0;
                    blackPixels = 0;

                    while (codingLine[codingPos] < columns)
                    {
                        code1 = GetTwoDimCode();
                        switch (code1)
                        {
                            case twoDimPass:
                                AddPixels(refLine[refPos + 1], blackPixels);
                                if (refLine[refPos + 1] < columns)
                                {
                                    refPos += 2;
                                }
                                break;
                            case twoDimHoriz:
                                code1 = code2 = 0;
                                if (blackPixels != 0)
                                {
                                    do
                                    {
                                        code1 += code3 = GetBlackCode();
                                    } while (code3 >= 64);
                                    do
                                    {
                                        code2 += code3 = GetWhiteCode();
                                    } while (code3 >= 64);
                                }
                                else
                                {
                                    do
                                    {
                                        code1 += code3 = GetWhiteCode();
                                    } while (code3 >= 64);
                                    do
                                    {
                                        code2 += code3 = GetBlackCode();
                                    } while (code3 >= 64);
                                }
                                AddPixels((uint)(codingLine[codingPos] + code1), blackPixels);
                                if (codingLine[codingPos] < columns)
                                {
                                    AddPixels((uint)(codingLine[codingPos] + code2), blackPixels ^ 1);
                                }
                                while (refLine[refPos] <= codingLine[codingPos] && refLine[refPos] < columns)
                                {
                                    refPos += 2;
                                }
                                break;
                            case twoDimVertR3:
                                AddPixels(refLine[refPos] + 3, blackPixels);
                                blackPixels ^= 1;
                                if (codingLine[codingPos] < columns)
                                {
                                    ++refPos;
                                    while (
                                      refLine[refPos] <= codingLine[codingPos] &&
                                      refLine[refPos] < columns
                                    )
                                    {
                                        refPos += 2;
                                    }
                                }
                                break;
                            case twoDimVertR2:
                                AddPixels(refLine[refPos] + 2, blackPixels);
                                blackPixels ^= 1;
                                if (codingLine[codingPos] < columns)
                                {
                                    ++refPos;
                                    while (
                                      refLine[refPos] <= codingLine[codingPos] &&
                                      refLine[refPos] < columns
                                    )
                                    {
                                        refPos += 2;
                                    }
                                }
                                break;
                            case twoDimVertR1:
                                AddPixels(refLine[refPos] + 1, blackPixels);
                                blackPixels ^= 1;
                                if (codingLine[codingPos] < columns)
                                {
                                    ++refPos;
                                    while (
                                      refLine[refPos] <= codingLine[codingPos] &&
                                      refLine[refPos] < columns
                                    )
                                    {
                                        refPos += 2;
                                    }
                                }
                                break;
                            case twoDimVert0:
                                AddPixels(refLine[refPos], blackPixels);
                                blackPixels ^= 1;
                                if (codingLine[codingPos] < columns)
                                {
                                    ++refPos;
                                    while (
                                      refLine[refPos] <= codingLine[codingPos] &&
                                      refLine[refPos] < columns
                                    )
                                    {
                                        refPos += 2;
                                    }
                                }
                                break;
                            case twoDimVertL3:
                                AddPixelsNeg(refLine[refPos] - 3, blackPixels);
                                blackPixels ^= 1;
                                if (codingLine[codingPos] < columns)
                                {
                                    if (refPos > 0)
                                    {
                                        --refPos;
                                    }
                                    else
                                    {
                                        ++refPos;
                                    }
                                    while (
                                      refLine[refPos] <= codingLine[codingPos] &&
                                      refLine[refPos] < columns
                                    )
                                    {
                                        refPos += 2;
                                    }
                                }
                                break;
                            case twoDimVertL2:
                                AddPixelsNeg(refLine[refPos] - 2, blackPixels);
                                blackPixels ^= 1;
                                if (codingLine[codingPos] < columns)
                                {
                                    if (refPos > 0)
                                    {
                                        --refPos;
                                    }
                                    else
                                    {
                                        ++refPos;
                                    }
                                    while (
                                      refLine[refPos] <= codingLine[codingPos] &&
                                      refLine[refPos] < columns
                                    )
                                    {
                                        refPos += 2;
                                    }
                                }
                                break;
                            case twoDimVertL1:
                                AddPixelsNeg(refLine[refPos] - 1, blackPixels);
                                blackPixels ^= 1;
                                if (codingLine[codingPos] < columns)
                                {
                                    if (refPos > 0)
                                    {
                                        --refPos;
                                    }
                                    else
                                    {
                                        ++refPos;
                                    }
                                    while (
                                      refLine[refPos] <= codingLine[codingPos] &&
                                      refLine[refPos] < columns
                                    )
                                    {
                                        refPos += 2;
                                    }
                                }
                                break;
                            case ccittEOF:
                                AddPixels(columns, 0);
                                eof = true;
                                break;
                            default:
                                Debug.WriteLine("info: bad 2d code");
                                AddPixels(columns, 0);
                                err = true;
                                break;
                        }
                    }
                }
                else
                {
                    codingLine[0] = 0;
                    codingPos = 0;
                    blackPixels = 0;
                    while (codingLine[codingPos] < columns)
                    {
                        code1 = 0;
                        if (blackPixels != 0)
                        {
                            do
                            {
                                code1 += code3 = GetBlackCode();
                            } while (code3 >= 64);
                        }
                        else
                        {
                            do
                            {
                                code1 += code3 = GetWhiteCode();
                            } while (code3 >= 64);
                        }
                        AddPixels((uint)(codingLine[codingPos] + code1), blackPixels);
                        blackPixels ^= 1;
                    }
                }

                var gotEOL = false;

                if (byteAlign)
                {
                    inputBits &= ~7;
                }

                if (!eoblock && row == rows - 1)
                {
                    rowsDone = true;
                }
                else
                {
                    code1 = LookBits(12);
                    if (eoline)
                    {
                        while (code1 != ccittEOF && code1 != 1)
                        {
                            EatBits(1);
                            code1 = LookBits(12);
                        }
                    }
                    else
                    {
                        while (code1 == 0)
                        {
                            EatBits(1);
                            code1 = LookBits(12);
                        }
                    }
                    if (code1 == 1)
                    {
                        EatBits(12);
                        gotEOL = true;
                    }
                    else if (code1 == ccittEOF)
                    {
                        eof = true;
                    }
                }

                if (!eof && encoding > 0 && !rowsDone)
                {
                    nextLine2D = LookBits(1) == 0;
                    EatBits(1);
                }

                if (eoblock && gotEOL && byteAlign)
                {
                    code1 = LookBits(12);
                    if (code1 == 1)
                    {
                        EatBits(12);
                        if (encoding > 0)
                        {
                            LookBits(1);
                            EatBits(1);
                        }
                        if (encoding >= 0)
                        {
                            for (i = 0; i < 4; ++i)
                            {
                                code1 = LookBits(12);
                                if (code1 != 1)
                                {
                                    Debug.WriteLine("info: bad rtc code: " + code1);
                                }
                                EatBits(12);
                                if (encoding > 0)
                                {
                                    LookBits(1);
                                    EatBits(1);
                                }
                            }
                        }
                        eof = true;
                    }
                }
                else if (err && eoline)
                {
                    while (true)
                    {
                        code1 = LookBits(13);
                        if (code1 == ccittEOF)
                        {
                            eof = true;
                            return -1;
                        }
                        if (code1 >> 1 == 1)
                        {
                            break;
                        }
                        EatBits(1);
                    }
                    EatBits(12);
                    if (encoding > 0)
                    {
                        EatBits(1);
                        nextLine2D = (code1 & 1) == 0;
                    }
                }

                if (codingLine[0] > 0)
                {
                    outputBits = (int)codingLine[(codingPos = 0)];
                }
                else
                {
                    outputBits = (int)codingLine[(codingPos = 1)];
                }
                row++;
            }

            uint c;
            if (outputBits >= 8)
            {
                c = (codingPos & 1) != 0 ? 0 : (uint)0xff;
                outputBits -= 8;
                if (outputBits == 0 && codingLine[codingPos] < columns)
                {
                    codingPos++;
                    outputBits = (int)(codingLine[codingPos] - codingLine[codingPos - 1]);
                }
            }
            else
            {
                bits = 8;
                c = 0;
                do
                {
                    if (outputBits > bits)
                    {
                        c <<= bits;
                        if ((codingPos & 1) == 0)
                        {
                            c |= (uint)0xff >> (8 - bits);
                        }
                        outputBits -= bits;
                        bits = 0;
                    }
                    else
                    {
                        c <<= outputBits;
                        if ((codingPos & 1) == 0)
                        {
                            c |= (uint)0xff >> (8 - outputBits);
                        }
                        bits -= outputBits;
                        outputBits = 0;
                        if (codingLine[codingPos] < columns)
                        {
                            codingPos++;
                            outputBits = (int)(codingLine[codingPos] - codingLine[codingPos - 1]);
                        }
                        else if (bits > 0)
                        {
                            c <<= bits;
                            bits = 0;
                        }
                    }
                } while (bits != 0);
            }
            if (black)
            {
                c ^= 0xff;
            }
            return (int)c;
        }

        private void AddPixels(uint a1, int blackPixels)
        {
            var codingLine = this.codingLine;
            var codingPos = this.codingPos;

            if (a1 > codingLine[codingPos])
            {
                if (a1 > columns)
                {
                    Debug.WriteLine("info: row is wrong length");
                    err = true;
                    a1 = columns;
                }
                if (((codingPos & 1) ^ blackPixels) != 0)
                {
                    ++codingPos;
                }

                codingLine[codingPos] = a1;
            }
            this.codingPos = codingPos;
        }

        private void AddPixelsNeg(uint a1, int blackPixels)
        {
            var codingLine = this.codingLine;
            var codingPos = this.codingPos;

            if (a1 > codingLine[codingPos])
            {
                if (a1 > columns)
                {
                    Debug.WriteLine("info: row is wrong length");
                    err = true;
                    a1 = columns;
                }
                if (((codingPos & 1) ^ blackPixels) != 0)
                {
                    ++codingPos;
                }

                codingLine[codingPos] = a1;
            }
            else if (a1 < codingLine[codingPos])
            {
                if (a1 < 0)
                {
                    Debug.WriteLine("info: invalid code");
                    err = true;
                    a1 = 0;
                }
                while (codingPos > 0 && a1 < codingLine[codingPos - 1])
                {
                    --codingPos;
                }
                codingLine[codingPos] = a1;
            }

            this.codingPos = codingPos;
        }

        /**
         * This function returns the code found from the table.
         * The start and end parameters set the boundaries for searching the table.
         * The limit parameter is optional. Function returns an array with three
         * values. The first array element indicates whether a valid code is being
         * returned. The second array element is the actual code. The third array
         * element indicates whether EOF was reached.
         */
        private TableCode FindTableCode(int start, int end, int[][] table, int? limit = null)
        {
            var limitValue = limit ?? 0;
            for (var i = start; i <= end; ++i)
            {
                var code = LookBits(i);
                if (code == ccittEOF)
                {
                    return new TableCode(true, 1, false);
                }
                if (i < end)
                {
                    code <<= end - i;
                }
                if (limitValue == 0 || code >= limitValue)
                {
                    var p = table[code - limitValue];
                    if (p[0] == i)
                    {
                        EatBits(i);
                        return new TableCode(true, p[1], true);
                    }
                }
            }
            return new TableCode(false, 0, false);
        }

        private int GetTwoDimCode()
        {
            var code = 0;
            int[] p;
            if (eoblock)
            {
                code = LookBits(7);
                if (code >= 0 && code < twoDimTable.Length)
                {
                    p = twoDimTable[code];
                    if (p[0] > 0)
                    {
                        EatBits(p[0]);
                        return p[1];
                    }
                }
            }
            else
            {
                var result = FindTableCode(1, 7, twoDimTable);
                if (result.v1 && result.v3)
                {
                    return result.v2;
                }
            }
            Debug.WriteLine("info: Bad two dim code");
            return ccittEOF;
        }

        private int GetWhiteCode()
        {
            var code = 0;
            int[] p;
            if (eoblock)
            {
                code = LookBits(12);
                if (code == ccittEOF)
                {
                    return 1;
                }

                if (code >> 5 == 0)
                {
                    p = whiteTable1[code];
                }
                else
                {
                    p = whiteTable2[code >> 3];
                }

                if (p[0] > 0)
                {
                    EatBits(p[0]);
                    return p[1];
                }
            }
            else
            {
                var result = FindTableCode(1, 9, whiteTable2);
                if (result.v1)
                {
                    return result.v2;
                }

                result = FindTableCode(11, 12, whiteTable1);
                if (result.v1)
                {
                    return result.v2;
                }
            }
            Debug.WriteLine("info: bad white code");
            EatBits(1);
            return 1;
        }

        private int GetBlackCode()
        {
            int code;
            int[] p;
            if (eoblock)
            {
                code = LookBits(13);
                if (code == ccittEOF)
                {
                    return 1;
                }
                if (code >> 7 == 0)
                {
                    p = blackTable1[code];
                }
                else if (code >> 9 == 0 && code >> 7 != 0)
                {
                    p = blackTable2[(code >> 1) - 64];
                }
                else
                {
                    p = blackTable3[code >> 7];
                }

                if (p[0] > 0)
                {
                    EatBits(p[0]);
                    return p[1];
                }
            }
            else
            {
                var result = FindTableCode(2, 6, blackTable3);
                if (result.v1)
                {
                    return result.v2;
                }

                result = FindTableCode(7, 12, blackTable2, 64);
                if (result.v1)
                {
                    return result.v2;
                }

                result = FindTableCode(10, 13, blackTable1);
                if (result.v1)
                {
                    return result.v2;
                }
            }
            Debug.WriteLine("info: bad black code");
            EatBits(1);
            return 1;
        }

        private int LookBits(int n)
        {
            int c;
            while (inputBits < n)
            {
                if ((c = source.ReadByte()) == -1)
                {
                    if (inputBits == 0)
                    {
                        return ccittEOF;
                    }
                    return (inputBuf << (n - inputBits)) & (0xffff >> (16 - n));
                }
                inputBuf = (inputBuf << 8) | c;
                inputBits += 8;
            }
            return (inputBuf >> (inputBits - n)) & (0xffff >> (16 - n));
        }

        private void EatBits(int n)
        {
            if ((inputBits -= n) < 0)
            {
                inputBits = 0;
            }
        }
    }

    internal struct TableCode
    {
        internal bool v1;
        internal int v2;
        internal bool v3;

        public TableCode(bool v1, int v2, bool v3)
        {
            this.v1 = v1;
            this.v2 = v2;
            this.v3 = v3;
        }
    }
}


