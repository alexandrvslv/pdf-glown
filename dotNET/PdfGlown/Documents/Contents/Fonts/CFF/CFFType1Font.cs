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
using SkiaSharp;
using System;
using System.Collections.Generic;
using PdfGlown.Documents.Contents.Fonts.Type1;

namespace PdfGlown.Documents.Contents.Fonts.CCF
{
    /// <summary>
    /// A Type 1-equivalent font program represented in a CFF file.Thread safe.
    /// @author Villu Ruusmann
    /// @author John Hewson
    /// </summary>
    public class CFFType1Font : CFFFont, IType1CharStringReader, IEncodedFont
    {
        private readonly Dictionary<string, object> privateDict = new(StringComparer.Ordinal);
        private CFFEncoding encoding;

        private readonly Dictionary<int, Type2CharString> charStringCache = new();
        //private readonly PrivateType1CharStringReader reader = new PrivateType1CharStringReader();
        private Type2CharStringParser charStringParser = null;

        private int? defaultWidthX;
        private int? nominalWidthX;
        private Memory<byte>[] localSubrIndex;

        public override SKPath GetPath(string name)
        {
            return GetType1CharString(name).Path;
        }

        public override float GetWidth(string name)
        {
            return GetType1CharString(name).Width;
        }

        public override bool HasGlyph(string name)
        {
            int sid = Charset.GetSID(name);
            int gid = Charset.GetGIDForSID(sid);
            return gid != 0;
        }

        public override List<float> FontMatrix
        {
            get => topDict.TryGetValue("FontMatrix", out var array) ? (List<float>)array : null;
        }

        /// <summary>Returns the Type 1 charstring for the given PostScript glyph name.</summary>
        /// <param name="name">PostScript glyph name</param>
        /// <returns></returns>
        public Type1CharString GetType1CharString(string name)
        {
            // lookup via charset
            int gid = NameToGID(name);

            // lookup in CharStrings INDEX
            return GetType2CharString(gid, name);
        }

        /// <summary>Returns the GID for the given PostScript glyph name.</summary>
        /// <param name="name">a PostScript glyph name.</param>
        /// <returns>GID</returns>
        public int NameToGID(string name)
        {
            // some fonts have glyphs beyond their encoding, so we look up by charset SID
            int sid = Charset.GetSID(name);
            return Charset.GetGIDForSID(sid);
        }

        /// <summary>Returns the Type 1 charstring for the given GID.</summary>
        /// <param name="gid">GID</param>
        public override Type2CharString GetType2CharString(int gid)
        {
            string name = "GID+" + gid; // for debugging only
            return GetType2CharString(gid, name);
        }

        // Returns the Type 2 charstring for the given GID, with name for debugging
        private Type2CharString GetType2CharString(int gid, string name)
        {
            if (!charStringCache.TryGetValue(gid, out Type2CharString type2))
            {
                var bytes = gid < charStrings.Length
                    ? charStrings[gid]
                    : charStrings[0];
                var type2seq = Parser.Parse(bytes, globalSubrIndex, LocalSubrIndex);
                type2 = new Type2CharString(this, Name, name, gid, type2seq, GetDefaultWidthX(), GetNominalWidthX());
                charStringCache[gid] = type2;
            }
            return type2;
        }

        private Type2CharStringParser Parser
        {
            get => charStringParser ??= new Type2CharStringParser(Name);
        }

        public Dictionary<string, object> PrivateDict
        {
            get => privateDict;
        }

        /// <summary>Adds the given key/value pair to the private dictionary.</summary>
        /// <param name="name">the given key</param>
        /// <param name="value">the given value</param>
        public void AddToPrivateDict(string name, object value)
        {
            if (value != null)
            {
                privateDict[name] = value;
            }
        }

        /// <summary>Returns the CFFEncoding of the font.</summary>
        public Encoding Encoding
        {
            get => encoding;
            set => encoding = (CFFEncoding)value;
        }

        private Memory<byte>[] LocalSubrIndex
        {
            get => localSubrIndex ??= privateDict.TryGetValue("Subrs", out var array) ? (Memory<byte>[])array : null;
        }

        // helper for looking up keys/values
        private object GetProperty(string name)
        {
            return topDict.TryGetValue(name, out var topDictValue)
                ? topDictValue
                : privateDict.TryGetValue(name, out var privateDictValue)
                    ? privateDictValue
                    : null;
        }

        private int GetDefaultWidthX()
        {
            return defaultWidthX ??= (int)(float)(GetProperty("defaultWidthX") ?? 1000F);
        }

        private int GetNominalWidthX()
        {
            return nominalWidthX ??= (int)(float)(GetProperty("nominalWidthX") ?? 0F);
        }
    }
}