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
using System;
using System.Collections.Generic;
using System.IO;

namespace PdfGlown.Documents.Contents.Fonts.TTF
{

    /// <summary>
    /// A TrueType Collection, now more properly known as a "Font Collection" as it may contain either
    /// TrueType or OpenType fonts.
    /// @author John Hewson
    /// </summary>
    public class TrueTypeCollection : IDisposable
    {
        public static readonly string TAG = "ttcf";
        private readonly IInputStream stream;
        private int numFonts;
        private long[] fontOffsets;
        private readonly Dictionary<int, TrueTypeFont> fontCache = new Dictionary<int, TrueTypeFont>();

        /// <summary>Creates a new TrueTypeCollection from a.ttc file.</summary>
        /// <param name="file">The TTC file.</param>
        public TrueTypeCollection(FileInfo file)
            : this(new ByteStream(new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
        { }

        /// <summary>Creates a new TrueTypeCollection from a TTC stream.</summary>
        /// <param name="stream">The TTF file.</param>
        public TrueTypeCollection(IInputStream stream)
        {
            this.stream = stream;
            Initialize();
        }

        private void Initialize()
        {
            // TTC header
            string tag = stream.ReadTag();
            if (!tag.Equals("ttcf", StringComparison.Ordinal))
            {
                throw new IOException("Missing TTC header");
            }
            float version = stream.Read32Fixed();
            numFonts = (int)stream.ReadUInt32();
            if (numFonts <= 0 || numFonts > 1024)
            {
                throw new IOException($"Invalid number of fonts {numFonts}");
            }
            fontOffsets = new long[numFonts];
            for (int i = 0; i < numFonts; i++)
            {
                fontOffsets[i] = stream.ReadUInt32();
            }
            if (version >= 2)
            {
                // not used at this time
                int ulDsigTag = stream.ReadUInt16();
                int ulDsigLength = stream.ReadUInt16();
                int ulDsigOffset = stream.ReadUInt16();
            }
        }

        /// <summary>Run the callback for each TT font in the collection.</summary>
        /// <param name="trueTypeFontProcessor">the object with the callback method.</param>
        /// <param name="tag"></param>
        public void ProcessAllFonts(ITrueTypeFontProcessor trueTypeFontProcessor, object tag)
        {
            for (int i = 0; i < numFonts; i++)
            {
                TrueTypeFont font = GetFontAtIndex(i);
                trueTypeFontProcessor(font, tag);
            }
        }

        public TrueTypeFont GetFontAtIndex(int idx)
        {
            if (!fontCache.TryGetValue(idx, out var font))
            {
                stream.Seek(fontOffsets[idx]);
                TTFParser parser;
                if (stream.ReadTag().Equals("OTTO", StringComparison.Ordinal))
                {
                    parser = new OTFParser(false);
                }
                else
                {
                    parser = new TTFParser(false);
                }
                stream.Seek(fontOffsets[idx]);
                font = parser.Parse(stream);
            }
            return font;
        }

        /// <summary>Get a TT font from a collection.</summary>
        /// <param name="name">The postscript name of the font.</param>
        /// <returns>The found font, nor null if none is found.</returns>
        public TrueTypeFont GetFontByName(string name)
        {
            for (int i = 0; i < numFonts; i++)
            {
                TrueTypeFont font = GetFontAtIndex(i);
                if (font.Name.Equals(name, StringComparison.Ordinal))
                {
                    return font;
                }
            }
            return null;
        }

        /// <summary>Implement the callback method to 
        /// call {@link TrueTypeCollection#processAllFonts(TrueTypeFontProcessor)}.
        /// </summary>
        public delegate void ITrueTypeFontProcessor(TrueTypeFont ttf, object tag);

        public void Dispose()
        {
            stream.Dispose();
        }
    }
}
