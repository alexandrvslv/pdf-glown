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
using PdfGlown.Bytes;
using PdfGlown.Documents.Contents.Fonts.CCF;
using System;

namespace PdfGlown.Documents.Contents.Fonts.TTF
{
    /// <summary>PostScript font program(compact font format).</summary>
    public class CFFTable : TTFTable
    {
        /// <summary>A tag that identifies this table type.</summary>
        public const string TAG = "CFF ";

        private CFFFont cffFont;

        public CFFTable()
        { }

        /// <summary>This will read the required data from the stream.</summary>
        /// <param name="ttf">The font that is being read</param>
        /// <param name="data">The stream to read the data from</param>
        public override void Read(TrueTypeFont ttf, IInputStream data)
        {
            var bytes = data.ReadMemory((int)Length);

            var parser = new CFFParser();
            cffFont = parser.Parse(bytes, new CFFByteSource(ttf))[0];

            initialized = true;
        }

        /// <summary>Returns the CFF font, which is a compact representation of a PostScript Type 1, or CIDFont</summary>
        public CFFFont Font
        {
            get => cffFont;
        }

        /// <summary>Allows bytes to be re-read later by CFFParser.</summary>
        internal class CFFByteSource : CFFParser.IByteSource
        {
            private readonly TrueTypeFont ttf;

            public CFFByteSource(TrueTypeFont ttf)
            {
                this.ttf = ttf;
            }

            public Memory<byte> GetBytes()
            {
                return ttf.GetTableBytes(ttf.TableMap.TryGetValue(CFFTable.TAG, out var ccfTable) ? ccfTable : null);
            }
        }
    }
}
