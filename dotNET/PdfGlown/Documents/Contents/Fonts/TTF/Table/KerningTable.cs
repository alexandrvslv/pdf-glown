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
using System.Diagnostics;
using PdfGlown.Bytes;

namespace PdfGlown.Documents.Contents.Fonts.TTF
{
    /// <summary>
    /// A 'kern' table in a true type font.
    /// @author Glenn Adams
    /// </summary>
    public class KerningTable : TTFTable
    {
        /// <summary>Tag to identify this table.</summary>
        public const string TAG = "kern";

        private KerningSubtable[] subtables;

        public KerningTable()
        { }

        /// <summary>This will read the required data from the stream.</summary>
        /// <param name="ttf">The font that is being read.</param>
        /// <param name="data">The stream to read the data from.</param>
        public override void Read(TrueTypeFont ttf, IInputStream data)
        {
            int version = data.ReadUInt16();
            if (version != 0)
            {
                version = (version << 16) | data.ReadUInt16();
            }
            int numSubtables = 0;
            switch (version)
            {
                case 0:
                    numSubtables = data.ReadUInt16();
                    break;
                case 1:
                    numSubtables = (int)data.ReadUInt32();
                    break;
                default:
                    Debug.WriteLine($"debug: Skipped kerning table due to an unsupported kerning table version: {version}");
                    break;
            }
            if (numSubtables > 0)
            {
                subtables = new KerningSubtable[numSubtables];
                for (int i = 0; i < numSubtables; ++i)
                {
                    var subtable = new KerningSubtable();
                    subtable.Read(data, version);
                    subtables[i] = subtable;
                }
            }
            initialized = true;
        }

        /// <summary>Obtain first subtable that supports non-cross-stream horizontal kerning.</summary>
        /// <returns>first matching subtable or null if none found</returns>
        public KerningSubtable GetHorizontalKerningSubtable()
        {
            return GetHorizontalKerningSubtable(false);
        }

        /// <summary>
        /// Obtain first subtable that supports horizontal kerning with specified cross stream.
        /// </summary>
        /// <param name="cross">cross true if requesting cross stream horizontal kerning</param>
        /// <returns>first matching subtable or null if none found</returns>
        public KerningSubtable GetHorizontalKerningSubtable(bool cross)
        {
            if (subtables != null)
            {
                foreach (KerningSubtable s in subtables)
                {
                    if (s.IsHorizontalKerning(cross))
                    {
                        return s;
                    }
                }
            }
            return null;
        }
    }
}