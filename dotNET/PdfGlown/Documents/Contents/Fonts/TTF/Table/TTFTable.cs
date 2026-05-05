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

namespace PdfGlown.Documents.Contents.Fonts.TTF
{
    /// <summary>
    /// A table in a true type font.
    /// @author Ben Litchfield
    /// </summary>
    public class TTFTable
    {
        private string tag;
        private long checkSum;
        private long offset;
        private long length;

        /// <summary>Indicates if the table is initialized or not.</summary>
        protected bool initialized;

        public TTFTable()
        { }

        /// <summary>The checkSum.</summary>
        public long CheckSum
        {
            get => checkSum;
            set => checkSum = value;
        }

        /// <summary>The length.</summary>
        public long Length
        {
            get => length;
            set => length = value;
        }

        public long Offset
        {
            get => offset;
            set => offset = value;
        }

        public string Tag
        {
            get => tag;
            set => tag = value;
        }

        /// <summary>Indicates if the table is already initialized.</summary>
        public bool Initialized
        {
            get => initialized;
        }

        /// <summary>This will read the required data from the stream.</summary>
        /// <param name="ttf">The font that is being read.</param>
        /// <param name="data">The stream to read the data from.</param>
        public virtual void Read(TrueTypeFont ttf, IInputStream data)
        {
        }
    }
}