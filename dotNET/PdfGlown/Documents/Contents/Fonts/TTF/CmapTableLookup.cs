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
namespace PdfGlown.Documents.Contents.Fonts.TTF
{

    using System.Collections.Generic;

    public class CmapTableLookup : ICmapLookup
    {
        private List<ICmapLookup> cmapSubtables = new List<ICmapLookup>();
        private readonly CmapTable cmapTable;

        public CmapTableLookup(CmapTable cmapTable, GlyphSubstitutionTable gsub, List<string> enabledGsubFeatures)
        {
            this.cmapTable = cmapTable;

            var cmap = cmapTable.GetSubtable(CmapTable.PLATFORM_UNICODE, CmapTable.ENCODING_UNICODE_2_0_FULL);
            if (cmap != null)
                cmapSubtables.Add(cmap);

            cmap = cmapTable.GetSubtable(CmapTable.PLATFORM_WINDOWS, CmapTable.ENCODING_WIN_UNICODE_FULL);
            if (cmap != null)
                cmapSubtables.Add(cmap);

            cmap = cmapTable.GetSubtable(CmapTable.PLATFORM_UNICODE, CmapTable.ENCODING_UNICODE_2_0_BMP);
            if (cmap != null)
                cmapSubtables.Add(cmap);

            cmap = cmapTable.GetSubtable(CmapTable.PLATFORM_WINDOWS, CmapTable.ENCODING_WIN_UNICODE_BMP);
            if (cmap != null)
                cmapSubtables.Add(cmap);

            cmap = cmapTable.GetSubtable(CmapTable.PLATFORM_MACINTOSH, CmapTable.ENCODING_UNICODE_1_0);
            if (cmap != null)
                cmapSubtables.Add(cmap);

            cmap = cmapTable.GetSubtable(CmapTable.PLATFORM_WINDOWS, CmapTable.ENCODING_WIN_SYMBOL);
            if (cmap != null)
                cmapSubtables.Add(cmap);

            foreach (var subTable in this.cmapTable.Cmaps)
                if (!cmapSubtables.Contains(subTable))
                    cmapSubtables.Add(subTable);

            if (enabledGsubFeatures.Count > 0 && gsub != null)
            {
                cmapSubtables.Insert(0, new SubstitutingCmapLookup((CmapSubtable)cmapSubtables[0], gsub, enabledGsubFeatures));
            }

        }


        public List<int> GetCharCodes(int gid)
        {
            foreach (var cmap in cmapSubtables)
            {
                var codes = cmap.GetCharCodes(gid);
                if (codes != null)
                    return codes;
            }
            return null;
        }

        public int GetGlyphId(int codePointAt)
        {
            foreach (var cmap in cmapSubtables)
            {
                var glyphId = cmap.GetGlyphId(codePointAt);
                if (glyphId != 0)
                    return glyphId;
            }
            return 0;
        }
    }
}