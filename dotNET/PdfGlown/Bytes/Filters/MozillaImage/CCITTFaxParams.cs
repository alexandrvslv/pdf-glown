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

namespace PdfGlown.Bytes.Filters
{
    internal class CCITTFaxParams
    {
        internal int K;
        internal int Columns;
        internal int Rows;
        internal bool BlackIs1;
        internal bool EndOfBlock;
        internal bool EndOfLine;
        internal bool EncodedByteAlign;

        public CCITTFaxParams(int K, int columns, int rows, bool blackIs1, bool endOfBlock)
        {
            this.K = K;
            this.Columns = columns;
            this.Rows = rows;
            this.BlackIs1 = blackIs1;
            this.EndOfBlock = endOfBlock;
        }

        public CCITTFaxParams(int K, bool endOfLine, bool encodedByteAlign, int columns, int rows, bool endOfBlock, bool blackIs1)
        {
            this.K = K;
            this.EndOfLine = endOfLine;
            this.EncodedByteAlign = encodedByteAlign;
            this.Columns = columns;
            this.Rows = rows;
            this.EndOfBlock = endOfBlock;
            this.BlackIs1 = blackIs1;
        }
    }
}
