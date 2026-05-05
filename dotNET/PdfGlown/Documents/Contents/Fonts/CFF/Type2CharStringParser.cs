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
using PdfGlown.Documents.Contents.Fonts.Type1;
using PdfGlown.Util.Collections;
using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents.Fonts.CCF
{
    /// <summary>
    /// This class represents a converter for a mapping into a Type2-sequence.
    /// @author Villu Ruusmann
    /// </summary>
    public class Type2CharStringParser
    {
        // 1-byte commands
        private static readonly int CALLSUBR = 10;
        private static readonly int CALLGSUBR = 29;

        private readonly string fontName;

        /// <summary>Constructs a new Type1CharStringParser object for a Type 1-equivalent font.</summary>
        /// <param name="fontName">font name</param>
        public Type2CharStringParser(string fontName)
        {
            this.fontName = fontName;
        }

        /// <summary>The given byte array will be parsed and converted to a Type2 sequence.</summary>
        /// <param name="bytes">the given mapping as byte array</param>
        /// <param name="globalSubrIndex">array containing all global subroutines</param>
        /// <param name="localSubrIndex">array containing all local subroutines</param>
        /// <returns>the Type2 sequence</returns>
        public List<Object> Parse(Memory<byte> bytes, Memory<byte>[] globalSubrIndex, Memory<byte>[] localSubrIndex)
        {
            // reset values if the parser is used multiple times
            var glyphData = new GlyphData();
            // create a new list as it is used as return value
            ParseSequence(bytes, globalSubrIndex, localSubrIndex, glyphData);
            return glyphData.Sequence;
        }

        private void ParseSequence(Memory<byte> bytes, Memory<byte>[] globalSubrIndex, Memory<byte>[] localSubrIndex, GlyphData glyphData)
        {
            var input = new ByteStream(bytes);

            while (input.HasRemaining())
            {
                var b0 = input.ReadUByte();
                if (b0 == CALLSUBR)
                {
                    ProcessCallSubr(globalSubrIndex, localSubrIndex, glyphData);
                }
                else if (b0 == CALLGSUBR)
                {
                    ProcessCallGSubr(globalSubrIndex, localSubrIndex, glyphData);
                }
                else if ((b0 >= 0 && b0 <= 27) || (b0 >= 29 && b0 <= 31))
                {
                    glyphData.Sequence.Add(ReadCommand(b0, input, glyphData));
                }
                else if (b0 == 28 || (b0 >= 32 && b0 <= 255))
                {
                    glyphData.Sequence.Add(ReadNumber(b0, input));
                }
                else
                {
                    throw new ArgumentException();
                }
            }
        }

        private Memory<byte> GetSubrBytes(Memory<byte>[] subrIndex, GlyphData glyphData)
        {
            int subrNumber = CalculateSubrNumber(
                    (int)glyphData.Sequence.RemoveAtValue<float>(glyphData.Sequence.Count - 1),
                    subrIndex.Length);
            return subrNumber < subrIndex.Length ? subrIndex[subrNumber] : null;
        }

        private void ProcessCallSubr(Memory<byte>[] globalSubrIndex, Memory<byte>[] localSubrIndex, GlyphData glyphData)
        {
            if (localSubrIndex != null && localSubrIndex.Length > 0)
            {
                var subrBytes = GetSubrBytes(localSubrIndex, glyphData);
                ProcessSubr(globalSubrIndex, localSubrIndex, subrBytes, glyphData);
            }
        }

        private void ProcessCallGSubr(Memory<byte>[] globalSubrIndex, Memory<byte>[] localSubrIndex,
            GlyphData glyphData)
        {
            if (globalSubrIndex != null && globalSubrIndex.Length > 0)
            {
                var subrBytes = GetSubrBytes(globalSubrIndex, glyphData);
                ProcessSubr(globalSubrIndex, localSubrIndex, subrBytes, glyphData);
            }
        }


        private void ProcessSubr(Memory<byte>[] globalSubrIndex, Memory<byte>[] localSubrIndex, Memory<byte> subrBytes,
            GlyphData glyphData)
        {
            ParseSequence(subrBytes, globalSubrIndex, localSubrIndex, glyphData);
            var lastItem = glyphData.Sequence[glyphData.Sequence.Count - 1];
            if (lastItem is CharStringCommand command
                && Type2KeyWord.RET == command.Type2KeyWord)
            {
                // remove "return" command
                glyphData.Sequence.RemoveAt(glyphData.Sequence.Count - 1);
            }
        }

        private int CalculateSubrNumber(int operand, int subrIndexlength)
        {
            if (subrIndexlength < 1240)
            {
                return 107 + operand;
            }
            if (subrIndexlength < 33900)
            {
                return 1131 + operand;
            }
            return 32768 + operand;
        }

        private CharStringCommand ReadCommand(byte b0, IInputStream input, GlyphData glyphData)
        {
            switch (b0)
            {
                case 1:
                case 18:
                    glyphData.HStemCount += CountNumbers(glyphData.Sequence) / 2;
                    return CharStringCommand.GetInstance(b0);
                case 3:
                case 23:
                    glyphData.VStemCount += CountNumbers(glyphData.Sequence) / 2;
                    return CharStringCommand.GetInstance(b0);
                case 12:
                    return CharStringCommand.GetInstance(b0, input.ReadUByte());
                case 19:
                case 20:
                    glyphData.VStemCount += CountNumbers(glyphData.Sequence) / 2;
                    byte[] value = new byte[1 + GetMaskLength(glyphData.HStemCount, glyphData.VStemCount)];
                    value[0] = b0;

                    for (int i = 1; i < value.Length; i++)
                    {
                        value[i] = input.ReadUByte();
                    }

                    return CharStringCommand.GetInstance(value);
            }

            return CharStringCommand.GetInstance(b0);
        }

        private float ReadNumber(int b0, IInputStream input)
        {
            switch (b0)
            {
                case 28:
                    return input.ReadInt16();
                case >= 32 and <= 246:
                    return b0 - 139;
                case >= 247 and <= 250:
                    {
                        int b1 = input.ReadUByte();
                        return (b0 - 247) * 256 + b1 + 108;
                    }
                case >= 251 and <= 254:
                    {
                        int b1 = input.ReadUByte();
                        return -(b0 - 251) * 256 - b1 - 108;
                    }
                case 255:
                    short value = input.ReadInt16();
                    // The lower bytes are representing the digits after the decimal point
                    float fraction = input.ReadUInt16() / 65535f;
                    return value + fraction;
                default:
                    throw new ArgumentException();
            }
        }

        private int GetMaskLength(int hstemCount, int vstemCount)
        {
            int hintCount = hstemCount + vstemCount;
            int Length = hintCount / 8;
            if (hintCount % 8 > 0)
            {
                Length++;
            }
            return Length;
        }

        private int CountNumbers(List<Object> sequence)
        {
            int count = 0;
            for (int i = sequence.Count - 1; i > -1; i--)
            {
                if (!(sequence[i] is float))
                {
                    return count;
                }
                count++;
            }
            return count;
        }

        public override string ToString()
        {
            return fontName;
        }

        private class GlyphData
        {
            public readonly List<Object> Sequence = new();
            public int HStemCount = 0;
            public int VStemCount = 0;
        }
    }
}
