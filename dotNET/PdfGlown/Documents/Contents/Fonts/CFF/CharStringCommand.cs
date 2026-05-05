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
using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents.Fonts.Type1
{
    /// <summary>
    /// This class represents a CharStringCommand.
    /// @author Villu Ruusmann
    /// </summary>
    public class CharStringCommand
    {
        private static readonly Dictionary<KeyWord, CharStringCommand> CHAR_STRING_COMMANDS = new(64)
        {
            { KeyWord.HSTEM, new CharStringCommand(KeyWord.HSTEM) },
            { KeyWord.VSTEM, new CharStringCommand(KeyWord.VSTEM) },
            { KeyWord.VMOVETO, new CharStringCommand(KeyWord.VMOVETO) },
            { KeyWord.RLINETO, new CharStringCommand(KeyWord.RLINETO) },
            { KeyWord.HLINETO, new CharStringCommand(KeyWord.HLINETO) },
            { KeyWord.VLINETO, new CharStringCommand(KeyWord.VLINETO) },
            { KeyWord.RRCURVETO, new CharStringCommand(KeyWord.RRCURVETO) },
            { KeyWord.CLOSEPATH, new CharStringCommand(KeyWord.CLOSEPATH) },
            { KeyWord.CALLSUBR, new CharStringCommand(KeyWord.CALLSUBR) },
            { KeyWord.RET, new CharStringCommand(KeyWord.RET) },
            { KeyWord.ESCAPE, new CharStringCommand(KeyWord.ESCAPE) },

            { KeyWord.HSBW, new CharStringCommand(KeyWord.HSBW) },
            { KeyWord.ENDCHAR, new CharStringCommand(KeyWord.ENDCHAR) },
            { KeyWord.HSTEMHM, new CharStringCommand(KeyWord.HSTEMHM) },
            { KeyWord.HINTMASK, new CharStringCommand(KeyWord.HINTMASK) },
            { KeyWord.CNTRMASK, new CharStringCommand(KeyWord.CNTRMASK) },
            { KeyWord.RMOVETO, new CharStringCommand(KeyWord.RMOVETO) },
            { KeyWord.HMOVETO, new CharStringCommand(KeyWord.HMOVETO) },
            { KeyWord.VSTEMHM, new CharStringCommand(KeyWord.VSTEMHM) },
            { KeyWord.RCURVELINE, new CharStringCommand(KeyWord.RCURVELINE) },
            { KeyWord.RLINECURVE, new CharStringCommand(KeyWord.RLINECURVE) },
            { KeyWord.VVCURVETO, new CharStringCommand(KeyWord.VVCURVETO) },
            { KeyWord.HHCURVETO, new CharStringCommand(KeyWord.HHCURVETO) },
            { KeyWord.SHORTINT, new CharStringCommand(KeyWord.SHORTINT) },
            { KeyWord.CALLGSUBR, new CharStringCommand(KeyWord.CALLGSUBR) },
            { KeyWord.VHCURVETO, new CharStringCommand(KeyWord.VHCURVETO) },
            { KeyWord.HVCURVETO, new CharStringCommand(KeyWord.HVCURVETO) },

            // two byte commands
            { KeyWord.DOTSECTION, new CharStringCommand(KeyWord.DOTSECTION) },
            { KeyWord.VSTEM3, new CharStringCommand(KeyWord.VSTEM3) },
            { KeyWord.HSTEM3, new CharStringCommand(KeyWord.HSTEM3) },
            { KeyWord.AND, new CharStringCommand(KeyWord.AND) },
            { KeyWord.OR, new CharStringCommand(KeyWord.OR) },
            { KeyWord.NOT, new CharStringCommand(KeyWord.NOT) },
            { KeyWord.SEAC, new CharStringCommand(KeyWord.SEAC) },
            { KeyWord.SBW, new CharStringCommand(KeyWord.SBW) },
            { KeyWord.ABS, new CharStringCommand(KeyWord.ABS) },
            { KeyWord.ADD, new CharStringCommand(KeyWord.ADD) },
            { KeyWord.SUB, new CharStringCommand(KeyWord.SUB) },
            { KeyWord.DIV, new CharStringCommand(KeyWord.DIV) },
            { KeyWord.NEG, new CharStringCommand(KeyWord.NEG) },
            { KeyWord.EQ, new CharStringCommand(KeyWord.EQ) },
            { KeyWord.CALLOTHERSUBR, new CharStringCommand(KeyWord.CALLOTHERSUBR) },
            { KeyWord.POP, new CharStringCommand(KeyWord.POP) },
            { KeyWord.DROP, new CharStringCommand(KeyWord.DROP) },
            { KeyWord.PUT, new CharStringCommand(KeyWord.PUT) },
            { KeyWord.GET, new CharStringCommand(KeyWord.GET) },
            { KeyWord.IFELSE, new CharStringCommand(KeyWord.IFELSE) },
            { KeyWord.RANDOM, new CharStringCommand(KeyWord.RANDOM) },
            { KeyWord.MUL, new CharStringCommand(KeyWord.MUL) },
            { KeyWord.SQRT, new CharStringCommand(KeyWord.SQRT) },
            { KeyWord.DUP, new CharStringCommand(KeyWord.DUP) },
            { KeyWord.EXCH, new CharStringCommand(KeyWord.EXCH) },
            { KeyWord.INDEX, new CharStringCommand(KeyWord.INDEX) },
            { KeyWord.ROLL, new CharStringCommand(KeyWord.ROLL) },
            { KeyWord.SETCURRENTPOINT, new CharStringCommand(KeyWord.SETCURRENTPOINT) },
            { KeyWord.HFLEX, new CharStringCommand(KeyWord.HFLEX) },
            { KeyWord.FLEX, new CharStringCommand(KeyWord.FLEX) },
            { KeyWord.HFLEX1, new CharStringCommand(KeyWord.HFLEX1) },
            { KeyWord.FLEX1, new CharStringCommand(KeyWord.FLEX1) },
        };

        public static readonly CharStringCommand CLOSEPATH = GetInstance(KeyWord.CLOSEPATH);
        public static readonly CharStringCommand RLINETO = GetInstance(KeyWord.RLINETO);
        public static readonly CharStringCommand HLINETO = GetInstance(KeyWord.HLINETO);
        public static readonly CharStringCommand VLINETO = GetInstance(KeyWord.VLINETO);
        public static readonly CharStringCommand RRCURVETO = GetInstance(KeyWord.RRCURVETO);
        public static readonly CharStringCommand HSBW = GetInstance(KeyWord.HSBW);
        public static readonly CharStringCommand CALLOTHERSUBR = GetInstance(KeyWord.CALLOTHERSUBR);

        private static readonly byte KEY_UNKNOWN = 99;
        public static readonly CharStringCommand UNKNOWN = new CharStringCommand(KEY_UNKNOWN, 0);

        /// <summary>
        /// Get an instance of the CharStringCommand represented by the given value.
        /// </summary>
        /// <param name="keyword">value</param>
        /// <returns>CharStringCommand represented by the given value</returns>
        public static CharStringCommand GetInstance(KeyWord keyword)
        {
            return CHAR_STRING_COMMANDS.TryGetValue(keyword, out CharStringCommand command) ? command : UNKNOWN;
        }

        public static CharStringCommand GetInstance(int b0)
        {
            return CHAR_STRING_COMMANDS.TryGetValue((KeyWord)b0, out CharStringCommand command) ? command : UNKNOWN;
        }

        public static CharStringCommand GetInstance(int b0, int b1)
        {
            return CHAR_STRING_COMMANDS.TryGetValue((KeyWord)((b0 << 8) | b1), out CharStringCommand command) ? command : UNKNOWN;
        }

        private readonly KeyWord keyWord;

        /// <summary>Get an instance of the CharStringCommand represented by the given array.</summary>
        /// <param name="values">array of values</param>
        /// <returns>CharStringCommand represented by the given values</returns>
        public static CharStringCommand GetInstance(byte[] values)
        {
            if (values.Length == 1)
            {
                return GetInstance(values[0]);
            }
            else if (values.Length == 2)
            {
                return GetInstance(values[0], values[1]);
            }
            return UNKNOWN;
        }

        /// <summary>Constructor with the CharStringCommand key as value.</summary>
        /// <param name="key">the key of the char string command</param>
        private CharStringCommand(KeyWord key)
        {
            keyWord = (KeyWord)((int)key);
        }

        /// <summary>Constructor with two values.</summary>
        /// <param name="b0">value1</param>
        /// <param name="b1">value2</param>
        private CharStringCommand(int b0, int b1)
        {
            keyWord = (KeyWord)((b0 << 8) | b1);
        }


        public Type1KeyWord? Type1KeyWord
        {
            get => Enum.IsDefined((Type1KeyWord)keyWord) ? (Type1KeyWord)keyWord : null;
        }

        public Type2KeyWord? Type2KeyWord
        {
            get => Enum.IsDefined((Type2KeyWord)keyWord) ? (Type2KeyWord)keyWord : null;
        }

        public override string ToString()
        {
            string str = Enum.GetName(typeof(KeyWord), keyWord);
            if (str == null)
            {
                return ((int)keyWord).ToString() + '|';
            }
            return str + '|';
        }

        public override int GetHashCode()
        {
            return keyWord.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is CharStringCommand that)
            {
                return keyWord == that.keyWord;
            }
            return false;
        }

    }

    /// <summary>Enum of all valid type1 key words</summary>
    public enum Type1KeyWord
    {
        HSTEM = KeyWord.HSTEM,
        VSTEM = KeyWord.VSTEM,
        VMOVETO = KeyWord.VMOVETO,
        RLINETO = KeyWord.RLINETO, //
        HLINETO = KeyWord.HLINETO,
        VLINETO = KeyWord.VLINETO,
        RRCURVETO = KeyWord.RRCURVETO, //
        CLOSEPATH = KeyWord.CLOSEPATH,
        CALLSUBR = KeyWord.CALLSUBR,
        RET = KeyWord.RET, //
        ESCAPE = KeyWord.ESCAPE,
        DOTSECTION = KeyWord.DOTSECTION, //
        VSTEM3 = KeyWord.VSTEM3,
        HSTEM3 = KeyWord.HSTEM3,
        SEAC = KeyWord.SEAC,
        SBW = KeyWord.SBW, //
        DIV = KeyWord.DIV,
        CALLOTHERSUBR = KeyWord.CALLOTHERSUBR,
        POP = KeyWord.POP, //
        SETCURRENTPOINT = KeyWord.SETCURRENTPOINT,
        HSBW = KeyWord.HSBW,
        ENDCHAR = KeyWord.ENDCHAR, //
        RMOVETO = KeyWord.RMOVETO,
        HMOVETO = KeyWord.HMOVETO,
        VHCURVETO = KeyWord.VHCURVETO, //
        HVCURVETO = KeyWord.HVCURVETO
    }


    /// <summary>Enum of all valid type2 key words</summary>
    public enum Type2KeyWord
    {
        HSTEM = KeyWord.HSTEM,
        VSTEM = KeyWord.VSTEM,
        VMOVETO = KeyWord.VMOVETO,
        RLINETO = KeyWord.RLINETO, //
        HLINETO = KeyWord.HLINETO,
        VLINETO = KeyWord.VLINETO,
        RRCURVETO = KeyWord.RRCURVETO,
        CALLSUBR = KeyWord.CALLSUBR, //
        RET = KeyWord.RET,
        ESCAPE = KeyWord.ESCAPE,
        AND = KeyWord.AND,
        OR = KeyWord.OR, //
        NOT = KeyWord.NOT, ABS = KeyWord.ABS, ADD = KeyWord.ADD, SUB = KeyWord.SUB, //
        DIV = KeyWord.DIV, NEG = KeyWord.NEG, EQ = KeyWord.EQ, DROP = KeyWord.DROP, //
        PUT = KeyWord.PUT, GET = KeyWord.GET, IFELSE = KeyWord.IFELSE, //
        RANDOM = KeyWord.RANDOM, MUL = KeyWord.MUL, SQRT = KeyWord.SQRT, DUP = KeyWord.DUP, //
        EXCH = KeyWord.EXCH, INDEX = KeyWord.INDEX, ROLL = KeyWord.ROLL, //
        HFLEX = KeyWord.HFLEX, FLEX = KeyWord.FLEX, HFLEX1 = KeyWord.HFLEX1, //
        FLEX1 = KeyWord.FLEX1,
        ENDCHAR = KeyWord.ENDCHAR,
        HSTEMHM = KeyWord.HSTEMHM,
        HINTMASK = KeyWord.HINTMASK, //
        CNTRMASK = KeyWord.CNTRMASK,
        RMOVETO = KeyWord.RMOVETO,
        HMOVETO = KeyWord.HMOVETO,
        VSTEMHM = KeyWord.VSTEMHM, //
        RCURVELINE = KeyWord.RCURVELINE,
        RLINECURVE = KeyWord.RLINECURVE,
        VVCURVETO = KeyWord.VVCURVETO, //
        HHCURVETO = KeyWord.HHCURVETO,
        SHORTINT = KeyWord.SHORTINT,
        CALLGSUBR = KeyWord.CALLGSUBR, //
        VHCURVETO = KeyWord.VHCURVETO,
        HVCURVETO = KeyWord.HVCURVETO
    }


    public enum KeyWord
    {
        HSTEM = 1,
        VSTEM = 3,
        VMOVETO = 4,
        RLINETO = 5, //
        HLINETO = 6,
        VLINETO = 7,
        RRCURVETO = 8,
        CLOSEPATH = 9,
        CALLSUBR = 10, //
        RET = 11,
        ESCAPE = 12,
        DOTSECTION = (12 << 8) | 0,
        VSTEM3 = (12 << 8) | 1,
        HSTEM3 = (12 << 8) | 2, //
        AND = (12 << 8) | 3,
        OR = (12 << 8) | 4,
        NOT = (12 << 8) | 5,
        SEAC = (12 << 8) | 6,
        SBW = (12 << 8) | 7, //
        ABS = (12 << 8) | 9,
        ADD = (12 << 8) | 10,
        SUB = (12 << 8) | 11,
        DIV = (12 << 8) | 12,
        NEG = (12 << 8) | 14,
        EQ = (12 << 8) | 15, //
        CALLOTHERSUBR = (12 << 8) | 16,
        POP = (12 << 8) | 17,
        DROP = (12 << 8) | 18, //
        PUT = (12 << 8) | 20,
        GET = (12 << 8) | 21,
        IFELSE = (12 << 8) | 22, //
        RANDOM = (12 << 8) | 23,
        MUL = (12 << 8) | 24,
        SQRT = (12 << 8) | 26,
        DUP = (12 << 8) | 27, //
        EXCH = (12 << 8) | 28,
        INDEX = (12 << 8) | 29,
        ROLL = (12 << 8) | 30,
        SETCURRENTPOINT = (12 << 8) | 33, //
        HFLEX = (12 << 8) | 34,
        FLEX = (12 << 8) | 35,
        HFLEX1 = (12 << 8) | 36,
        FLEX1 = (12 << 8) | 37, //
        HSBW = 13,
        ENDCHAR = 14,
        HSTEMHM = 18,
        HINTMASK = 19, //
        CNTRMASK = 20,
        RMOVETO = 21,
        HMOVETO = 22,
        VSTEMHM = 23, //
        RCURVELINE = 24,
        RLINECURVE = 25,
        VVCURVETO = 26, //
        HHCURVETO = 27,
        SHORTINT = 28,
        CALLGSUBR = 29, //
        VHCURVETO = 30,
        HVCURVETO = 31
    }

}
