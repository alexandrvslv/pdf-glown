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
using System.Collections.Generic;
using System.IO;
using System;
using System.Diagnostics;
using PdfGlown.Bytes;
using PdfGlown.Util.Collections;

namespace PdfGlown.Documents.Contents.Fonts.Type1
{
    /// <summary>
    /// This class represents a converter for a mapping into a Type 1 sequence.
    /// @see "Adobe Type 1 Font Format, Adobe Systems (1999)"
    /// @author Villu Ruusmann
    /// @author John Hewson
    /// </summary>
    public class Type1CharStringParser
    {

        // 1-byte commands
        private static readonly byte CALLSUBR = 10;

        // 2-byte commands
        private static readonly byte TWO_BYTE = 12;
        private static readonly byte CALLOTHERSUBR = 16;
        private static readonly byte POP = 17;

        private readonly string fontName;
        private string currentGlyph;

        /// <summary>Constructs a new Type1CharStringParser object.</summary>
        /// <param name="fontName">font name</param>
        public Type1CharStringParser(string fontName)
        {
            this.fontName = fontName;
        }

        /// <summary>The given byte array will be parsed and converted to a Type1 sequence.</summary>
        /// <param name="bytes">the given mapping as byte array</param>
        /// <param name="subrs">list of local subroutines</param>
        /// <param name="glyphName">name of the current glyph</param>
        /// <returns>the Type1 sequence</returns>
        public List<object> Parse(Memory<byte> bytes, List<Memory<byte>> subrs, string glyphName)
        {
            currentGlyph = glyphName;
            return Parse(bytes, subrs, new List<object>());
        }

        private List<object> Parse(Memory<byte> bytes, List<Memory<byte>> subrs, List<object> sequence)

        {
            var input = new ByteStream(bytes);
            while (input.HasRemaining())
            {
                var b0 = input.ReadUByte();
                if (b0 == CALLSUBR)
                {
                    ProcessCallSubr(subrs, sequence);
                }
                else if (b0 == TWO_BYTE && input.PeekUByte(0) == CALLOTHERSUBR)
                {
                    ProcessCallOtherSubr(input, sequence);
                }
                else if (b0 >= 0 && b0 <= 31)
                {
                    sequence.Add(ReadCommand(input, b0));
                }
                else if (b0 >= 32 && b0 <= 255)
                {
                    sequence.Add(ReadNumber(input, b0));
                }
                else
                {
                    throw new ArgumentException();
                }
            }
            return sequence;
        }

        private void ProcessCallSubr(List<Memory<byte>> subrs, List<object> sequence)
        {
            // callsubr command
            object obj = sequence.RemoveAtValue(sequence.Count - 1);
            if (obj is not int operand)
            {
                Debug.WriteLine("warn: Parameter {} for CALLSUBR is ignored, integer expected in glyph '{}' of font {}",
                        obj, currentGlyph, fontName);
                return;
            }

            if (operand >= 0 && operand < subrs.Count)
            {
                var subrBytes = subrs[operand];
                Parse(subrBytes, subrs, sequence);
                var lastItem = sequence[sequence.Count - 1];
                if (lastItem is CharStringCommand lastCommand
                    && Type1KeyWord.RET == lastCommand.Type1KeyWord)
                {
                    sequence.RemoveAt(sequence.Count - 1); // RemoveAtValue "return" command
                }
            }
            else
            {
                Debug.WriteLine($"warn: CALLSUBR is ignored, operand: {operand}, subrs.Count: {subrs.Count} in glyph '{currentGlyph}' of font {fontName}");
                // RemoveAtValue all parameters (there can be more than one)
                while (sequence[sequence.Count - 1] is int)
                {
                    sequence.RemoveAt(sequence.Count - 1);
                }
            }
        }

        private void ProcessCallOtherSubr(IInputStream input, List<object> sequence)
        {
            // callothersubr command (needed in order to expand Subrs)
            input.ReadByte();

            int othersubrNum = (int)sequence.RemoveAtValue(sequence.Count - 1);
            int numArgs = (int)sequence.RemoveAtValue(sequence.Count - 1);

            // othersubrs 0-3 have their own semantics
            var results = new Stack<int>();
            switch (othersubrNum)
            {
                case 0:
                    results.Push(RemoveInteger(sequence));
                    results.Push(RemoveInteger(sequence));
                    sequence.RemoveAt(sequence.Count - 1);
                    // end flex
                    sequence.Add(0);
                    sequence.Add(CharStringCommand.CALLOTHERSUBR);
                    break;
                case 1:
                    // begin flex
                    sequence.Add(1);
                    sequence.Add(CharStringCommand.CALLOTHERSUBR);
                    break;
                case 3:
                    // allows hint replacement
                    results.Push(RemoveInteger(sequence));
                    break;
                default:
                    // all remaining othersubrs use this fallback mechanism
                    for (int i = 0; i < numArgs; i++)
                    {
                        results.Push(RemoveInteger(sequence));
                    }
                    break;
            }

            // pop must follow immediately
            while (input.PeekUByte(0) == TWO_BYTE && input.PeekUByte(1) == POP)
            {
                input.ReadByte(); // B0_POP
                input.ReadByte(); // B1_POP
                sequence.Add(results.Pop());
            }

            if (results.Count > 0)
            {
                Debug.WriteLine("warn: Value left on the PostScript stack in glyph {currentGlyph} of font {fontName}");
            }
        }

        // this method is a workaround for the fact that Type1CharStringParser assumes that subrs and
        // othersubrs can be unrolled without executing the 'div' operator, which isn't true
        private static int RemoveInteger(List<object> sequence)
        {
            var item = sequence.RemoveAtValue(sequence.Count - 1);
            if (item is int intValue)
            {
                return intValue;
            }
            var command = (CharStringCommand)item;

            // div
            if (Type1KeyWord.DIV == command.Type1KeyWord)
            {
                int a = (int)sequence.RemoveAtValue(sequence.Count - 1);
                int b = (int)sequence.RemoveAtValue(sequence.Count - 1);
                return b / a;
            }
            throw new IOException("Unexpected char string command: " + command.Type1KeyWord);
        }

        private CharStringCommand ReadCommand(IInputStream input, byte b0)
        {
            return b0 == 12 ? CharStringCommand.GetInstance(b0, input.ReadUByte()) 
                            : CharStringCommand.GetInstance(b0);
        }

        private int ReadNumber(IInputStream input, int b0)
        {
            if (b0 >= 32 && b0 <= 246)
            {
                return b0 - 139;
            }
            else if (b0 >= 247 && b0 <= 250)
            {
                int b1 = input.ReadUByte();
                return (b0 - 247) * 256 + b1 + 108;
            }
            else if (b0 >= 251 && b0 <= 254)
            {
                int b1 = input.ReadUByte();
                return -(b0 - 251) * 256 - b1 - 108;
            }
            else if (b0 == 255)
            {
                return input.ReadInt32();
            }
            else
            {
                throw new ArgumentException();
            }
        }
    }


}
