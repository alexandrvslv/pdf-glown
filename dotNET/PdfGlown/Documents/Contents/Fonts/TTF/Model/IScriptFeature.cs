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

namespace PdfGlown.Documents.Contents.Fonts.TTF.Model
{
    /// <summary>
    /// Models a {@link FeatureRecord}
    /// @author Palash Ray
    /// </summary>
    public interface IScriptFeature
    {
        string Name { get; }

        ICollection<HashList<ushort>> AllGlyphIdsForSubstitution { get; }

        bool CanReplaceGlyphs(HashList<ushort> glyphIds);

        HashList<ushort> GetReplacementForGlyphs(HashList<ushort> glyphIds);

    }
}
