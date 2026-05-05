/*
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
using System;

namespace PdfGlown.Documents.Contents.Fonts
{

    /**
     * This represents a single entry in the codespace range.
     *
     * @author Ben Litchfield
     */
    public class CodespaceRange
    {
        private readonly byte[] start;
        private readonly byte[] end;
        private readonly int codeLength;

        /**
     * Creates a new instance of CodespaceRange. The length of both arrays has to be the same.<br>
     * For one byte ranges startBytes and endBytes define a linear range of values. Double byte values define a
     * rectangular range not a linear range. Examples: <br>
     * &lt;00&gt; &lt;20&gt; defines a linear range from 0x00 up to 0x20.<br>
     * &lt;8140&gt; to &lt;9FFC&gt; defines a rectangular range. The high byte has to be within 0x81 and 0x9F and the
     * low byte has to be within 0x40 and 0xFC
     * 
     * @param startBytes start of the range
     * @param endBytes start of the range
     */
        public CodespaceRange(ReadOnlySpan<byte> startBytes, ReadOnlySpan<byte> endBytes)
        {
            var correctedStartBytes = startBytes;
            if (startBytes.Length != endBytes.Length && startBytes.Length == 1 && startBytes[0] == 0)
            {
                correctedStartBytes = new byte[endBytes.Length];
            }
            else if (startBytes.Length != endBytes.Length)
            {
                throw new ArgumentException("The start and the end values must not have different lengths.");
            }
            start = correctedStartBytes.ToArray();
            end = endBytes.ToArray();
            codeLength = endBytes.Length;
        }

        /**
         * Returns the length of the codes of the codespace.
         * 
         * @return the code length
         */
        public int CodeLength => codeLength;

        /**
         * Returns true if the given code bytes match this codespace range.
         */
        public bool Matches(ReadOnlySpan<byte> code)
        {
            return IsFullMatch(code, code.Length);
        }

        /**
         * Returns true if the given number of code bytes match this codespace range.
         */
        public bool IsFullMatch(ReadOnlySpan<byte> code, int codeLen)
        {
            // code must be the same length as the bounding codes
            if (codeLength != codeLen
                || code.Length < codeLength)
            {
                return false;
            }
            for (int i = 0; i < codeLength; i++)
            {
                var codeAsInt = code[i];
                if (codeAsInt < start[i] || codeAsInt > end[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
