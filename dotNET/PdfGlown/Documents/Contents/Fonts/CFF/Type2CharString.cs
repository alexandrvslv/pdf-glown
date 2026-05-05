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
using PdfGlown.Documents.Contents.Fonts.Type1;
using System;

namespace PdfGlown.Documents.Contents.Fonts.CCF
{
    /// <summary>
    /// Represents a Type 2 CharString by converting it into an equivalent Type 1 CharString.
    /// @author Villu Ruusmann
    /// @author John Hewson
    /// </summary>
    public class Type2CharString : Type1CharString
    {
        private float defWidthX = 0;
        private float nominalWidthX = 0;
        private int pathCount = 0;
        private readonly int gid;

        /// <summary>Constructor.</summary>
        /// <param name="font">Parent CFF font</param>
        /// <param name="fontName">font name</param>
        /// <param name="glyphName">glyph name (or CID as hex string)</param>
        /// <param name="gid">GID</param>
        /// <param name="sequence">Type 2 char string sequence</param>
        /// <param name="defaultWidthX">default width</param>
        /// <param name="nomWidthX">nominal width</param>
        public Type2CharString(IType1CharStringReader font, string fontName, string glyphName, int gid, List<object> sequence,
                               int defaultWidthX, int nomWidthX)
            : base(font, fontName, glyphName)
        {

            this.gid = gid;
            defWidthX = defaultWidthX;
            nominalWidthX = nomWidthX;
            ConvertType1ToType2(sequence);
        }

        /// <summary>Return the GID (glyph id) of this charstring.</summary>
        public int GID
        {
            get => gid;
        }

        /// <summary>Converts a sequence of Type 2 commands into a sequence of Type 1 commands.</summary>
        /// <param name="sequence">the Type 2 char string sequence</param>
        private void ConvertType1ToType2(List<object> sequence)
        {
            pathCount = 0;
            var numbers = new List<float>();
            foreach (var obj in sequence)
            {
                if (obj is CharStringCommand command)
                {
                    var results = ConvertType2Command(numbers, command);
                    numbers.Clear();
                    numbers.AddRange(results);
                }
                else
                {
                    numbers.Add((float)obj);
                }
            }
        }

        protected List<float> ConvertType2Command(List<float> numbers, CharStringCommand command)
        {
            var word = command.Type2KeyWord;
            if (word == null)
            {
                AddCommand(numbers, command);
                return new List<float>(0);
            }
            switch (word)
            {
                case Type2KeyWord.HSTEM:
                case Type2KeyWord.HSTEMHM:
                case Type2KeyWord.VSTEM:
                case Type2KeyWord.VSTEMHM:
                case Type2KeyWord.HINTMASK:
                case Type2KeyWord.CNTRMASK:
                    numbers = ClearStack(numbers, numbers.Count % 2 != 0);
                    ExpandStemHints(numbers, word == Type2KeyWord.HSTEM || word == Type2KeyWord.HSTEMHM);
                    break;
                case Type2KeyWord.VMOVETO:
                case Type2KeyWord.HMOVETO:
                    numbers = ClearStack(numbers, numbers.Count > 1);
                    MarkPath();
                    AddCommand(numbers, command);
                    break;
                case Type2KeyWord.RLINETO:
                    AddCommandList(Split(numbers, 2), command);
                    break;
                case Type2KeyWord.HLINETO:
                case Type2KeyWord.VLINETO:
                    AddAlternatingLine(numbers, word == Type2KeyWord.HLINETO);
                    break;
                case Type2KeyWord.RRCURVETO:
                    AddCommandList(Split(numbers, 6), command);
                    break;
                case Type2KeyWord.ENDCHAR:
                    numbers = ClearStack(numbers, numbers.Count == 5 || numbers.Count == 1);
                    CloseCharString2Path();
                    if (numbers.Count == 4)
                    {
                        // deprecated "seac" operator
                        numbers.Insert(0, 0F);
                        AddCommand(numbers, CharStringCommand.GetInstance(12, 6));
                    }
                    else
                    {
                        AddCommand(numbers, command);
                    }
                    break;
                case Type2KeyWord.RMOVETO:
                    numbers = ClearStack(numbers, numbers.Count > 2);
                    MarkPath();
                    AddCommand(numbers, command);
                    break;
                case Type2KeyWord.VHCURVETO:
                case Type2KeyWord.HVCURVETO:
                    AddAlternatingCurve(numbers, word == Type2KeyWord.HVCURVETO);
                    break;
                case Type2KeyWord.HFLEX:
                    if (numbers.Count >= 7)
                    {
                        var first = new List<float> { numbers[0], 0F, numbers[1], numbers[2], numbers[3], 0F };
                        var second = new List<float> { numbers[4], 0F, numbers[5], -(numbers[2]), numbers[6], 0F };
                        AddCommandList(new() { first, second }, CharStringCommand.RRCURVETO);
                    }
                    break;
                case Type2KeyWord.FLEX:
                    {
                        List<float> first = numbers.GetRange(0, 6);
                        List<float> second = numbers.GetRange(6, 6);
                        AddCommandList(new() { first, second }, CharStringCommand.RRCURVETO);
                        break;
                    }
                case Type2KeyWord.HFLEX1:
                    if (numbers.Count >= 9)
                    {
                        var first = new List<float> { numbers[0], numbers[1], numbers[2], numbers[3], numbers[4], 0F };
                        var second = new List<float> { numbers[5], 0F, numbers[6], numbers[7], numbers[8], 0F };
                        AddCommandList(new() { first, second }, CharStringCommand.RRCURVETO);
                    }
                    break;

                case Type2KeyWord.FLEX1:
                    {
                        int dx = 0;
                        int dy = 0;
                        for (int i = 0; i < 5; i++)
                        {
                            dx += (int)numbers[i * 2];
                            dy += (int)numbers[i * 2 + 1];
                        }
                        var first = numbers.GetRange(0, 6);
                        var dxIsBigger = Math.Abs(dx) > Math.Abs(dy);
                        var second = new List<float>(6)
                        {
                            numbers[6],
                            numbers[7],
                            numbers[8],
                            numbers[9],
                            (dxIsBigger ? numbers[10] : -dx),
                            (dxIsBigger ? -dy : numbers[10])
                        };
                        AddCommandList(new() { first, second }, CharStringCommand.RRCURVETO);
                        break;
                    }
                case Type2KeyWord.RCURVELINE:
                    if (numbers.Count >= 2)
                    {
                        AddCommandList(Split(numbers.GetRange(0, numbers.Count - 2), 6),
                                CharStringCommand.RRCURVETO);
                        AddCommand(numbers.GetRange(numbers.Count - 2, 2),
                                CharStringCommand.RLINETO);
                    }
                    break;
                case Type2KeyWord.RLINECURVE:
                    if (numbers.Count >= 6)
                    {
                        AddCommandList(Split(numbers.GetRange(0, numbers.Count - 6), 2),
                                CharStringCommand.RLINETO);
                        AddCommand(numbers.GetRange(numbers.Count - 6, 6),
                                CharStringCommand.RRCURVETO);
                    }
                    break;
                case Type2KeyWord.HHCURVETO:
                case Type2KeyWord.VVCURVETO:
                    AddCurve(numbers, word == Type2KeyWord.HHCURVETO);
                    break;
                default:
                    AddCommand(numbers, command);
                    break;
            }
            return new List<float>(0);
        }

        private List<float> ClearStack(List<float> numbers, bool flag)
        {
            if (IsSequenceEmpty)
            {
                if (flag)
                {
                    AddCommand(new List<float> { 0f, numbers[0] + nominalWidthX },
                            CharStringCommand.HSBW);
                    numbers = numbers.GetRange(1, numbers.Count - 1);
                }
                else
                {
                    AddCommand(new List<float> { 0f, defWidthX }, CharStringCommand.HSBW);
                }
            }
            return numbers;
        }

        private void ExpandStemHints(List<float> numbers, bool horizontal)
        {
            // TODO
        }

        private void MarkPath()
        {
            if (pathCount > 0)
            {
                CloseCharString2Path();
            }
            pathCount++;
        }

        private void CloseCharString2Path()
        {
            var command = pathCount > 0
                ? (CharStringCommand)LastSequenceEntry
                : null;

            if (command != null && command.Type1KeyWord != Type1KeyWord.CLOSEPATH)
            {
                AddCommand(new List<float>(0), CharStringCommand.CLOSEPATH);
            }
        }

        private void AddAlternatingLine(List<float> numbers, bool horizontal)
        {
            while (numbers.Count > 0)
            {
                AddCommand(numbers.GetRange(0, 1), horizontal ? CharStringCommand.HLINETO
                    : CharStringCommand.VLINETO);
                numbers = numbers.GetRange(1, numbers.Count - 1);
                horizontal = !horizontal;
            }
        }

        private void AddAlternatingCurve(List<float> numbers, bool horizontal)
        {
            while (numbers.Count >= 4)
            {
                bool last = numbers.Count == 5;
                if (horizontal)
                {
                    AddCommand(new List<float> { numbers[0], 0, numbers[1], numbers[2], last ? numbers[4] : 0, numbers[3] },
                            CharStringCommand.RRCURVETO);
                }
                else
                {
                    AddCommand(new List<float> { 0, numbers[0], numbers[1], numbers[2], numbers[3], last ? numbers[4] : 0 },
                            CharStringCommand.RRCURVETO);
                }
                numbers = numbers.GetRange(last ? 5 : 4, numbers.Count - (last ? 5 : 4));
                horizontal = !horizontal;
            }
        }

        private void AddCurve(List<float> numbers, bool horizontal)
        {
            while (numbers.Count >= 4)
            {
                bool first = numbers.Count % 4 == 1;

                if (horizontal)
                {
                    AddCommand(new List<float> { numbers[first ? 1 : 0], first ? numbers[0] : 0, numbers[first ? 2 : 1], numbers[first ? 3 : 2], numbers[first ? 4 : 3], 0 },
                             CharStringCommand.RRCURVETO);
                }
                else
                {
                    AddCommand(new List<float> { first ? numbers[0] : 0, numbers[first ? 1 : 0], numbers[first ? 2 : 1], numbers[first ? 3 : 2], 0, numbers[first ? 4 : 3] },
                            CharStringCommand.RRCURVETO);
                }
                numbers = numbers.GetRange(first ? 5 : 4, numbers.Count - (first ? 5 : 4));
            }
        }

        private void AddCommandList(List<List<float>> numbers, CharStringCommand command)
        {
            foreach (var ns in numbers)
                AddCommand(ns, command);
        }

        private static List<List<E>> Split<E>(List<E> list, int size)
        {
            int listSize = list.Count / size;
            var result = new List<List<E>>(listSize);
            for (int i = 0; i < listSize; i++)
            {
                result.Add(list.GetRange(i * size, size));
            }
            return result;
        }
    }
}
