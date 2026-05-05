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
namespace PdfGlown.Documents.Contents.Fonts.CCF
{
    /// <summary>
    /// A CFF charset.A charset is an array of SIDs/CIDs for all glyphs in the font.
    /// todo: split this into two? CFFCharsetType1 and CFFCharsetCID?
    /// @author John Hewson
    /// </summary>
    public abstract class CFFCharset
    {
        public abstract bool IsCIDFont { get; }

        /// <summary>Adds a new GID/SID/name combination to the charset.</summary>
        /// <param name="gid">GID</param>
        /// <param name="sid">SID</param>
        /// <param name="name"></param>
        public abstract void AddSID(int gid, int sid, string name);

        /// <summary>Adds a new GID/CID combination to the charset.</summary>
        /// <param name="gid">GID</param>
        /// <param name="cid">CID</param>
        public abstract void AddCID(int gid, int cid);

        /// <summary>Returns the SID for a given GID.SIDs are internal to the font and are not public.</summary>
        /// <param name="gid">GID</param>
        /// <returns>SID</returns>
        public abstract int GetSIDForGID(int gid);

        /// <summary>Returns the GID for the given SID.SIDs are internal to the font and are not public.</summary>
        /// <param name="sid">SID</param>
        /// <returns>GID</returns>
        public abstract int GetGIDForSID(int sid);

        /// <summary>Returns the GID for a given CID.Returns 0 if the CID is missing.</summary>
        /// <param name="cid">CID</param>
        /// <returns>GID</returns>
        public abstract int GetGIDForCID(int cid);

        /// <summary>Returns the SID for a given PostScript name, you would think this is not needed,
        /// but some fonts have glyphs beyond their encoding with charset SID names.</summary>
        /// <param name="name">PostScript glyph name</param>
        /// <returns>SID</returns>
        public abstract int GetSID(string name);

        /// <summary>Returns the PostScript glyph name for the given GID.</summary>
        /// <param name="gid">GID</param>
        /// <returns>PostScript glyph name</returns>
        public abstract string GetNameForGID(int gid);

        /// <summary>Returns the CID for the given GID.</summary>
        /// <param name="gid"GID></param>
        /// <returns>CID</returns>
        public abstract int GetCIDForGID(int gid);
    }
}