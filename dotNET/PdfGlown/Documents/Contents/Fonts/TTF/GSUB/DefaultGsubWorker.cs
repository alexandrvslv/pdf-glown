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

namespace PdfGlown.Documents.Contents.Fonts.TTF.GSUB
{
    /// <summary>
    /// A default implementation of { @link GsubWorker }
    /// that actually does not transform the glyphs yet allows to correctly
    /// {@linkplain GlyphSubstitutionTable#getGsubData(String) load} GSUB table data even from fonts for which a complete
    /// glyph substitution is not implemented.
    /// @author Vladimir Plizga
    /// </summary>
    class DefaultGsubWorker : IGsubWorker
    {

        public HashList<ushort> ApplyTransforms(HashList<ushort> originalGlyphIds)
        {
            Debug.Write($"warn: {nameof(DefaultGsubWorker)} class does not perform actual GSUB substitutions. Perhaps the selected language is not yet supported by the FontBox library.");
            // Make the result read-only to prevent accidental modifications of the source list
            return originalGlyphIds;
        }
    }
}