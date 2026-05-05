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
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents.Fonts.TTF
{
    /// <summary>
    /// An interface that abstracts the cid 
    /// codepoint lookup functionality of cmap.
    /// @author Aaron Madlon-Kay
    /// </summary>
    public interface ICmapLookup
    {
        /// <summary>Returns the GlyphId linked with the given character code.</summary>
        /// <param name="codePointAt">the given character code to be mapped</param>
        /// <returns>glyphId the corresponding glyph id for the given character code</returns>
        int GetGlyphId(int codePointAt);

        /// <summary>Returns all possible character codes for the given gid, or null if there is none.</summary>
        /// <param name="gid">glyph id</param>
        /// <returns>a list with all character codes the given gid maps to</returns>
        List<int> GetCharCodes(int gid);

    }
}