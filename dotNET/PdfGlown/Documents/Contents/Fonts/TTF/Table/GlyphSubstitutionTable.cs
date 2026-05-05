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
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PdfGlown.Documents.Contents.Fonts.TTF.Table.Common;
using PdfGlown.Documents.Contents.Fonts.TTF.Model;
using PdfGlown.Documents.Contents.Fonts.TTF.GSUB;
using System;
using PdfGlown.Documents.Contents.Fonts.TTF.Table.GSUB;
using System.Linq;
using PdfGlown.Bytes;

namespace PdfGlown.Documents.Contents.Fonts.TTF
{
    /// <summary>
    /// A glyph substitution 'GSUB' table in a TrueType or OpenType font.
    /// @author Aaron Madlon-Kay
    /// </summary>
    public class GlyphSubstitutionTable : TTFTable
    {
        //private static readonly Log LOG = LogFactory.getLog(GlyphSubstitutionTable.class);

        public const string TAG = "GSUB";

        private Dictionary<string, ScriptTable> scriptList;
        // featureList and lookupList are not maps because we need to index into them
        private FeatureListTable featureListTable;
        private LookupListTable lookupListTable;

        private readonly Dictionary<int, int> lookupCache = new Dictionary<int, int>();
        private readonly Dictionary<int, int> reverseLookup = new Dictionary<int, int>();

        private string lastUsedSupportedScript;

        private IGsubData gsubData;

        public GlyphSubstitutionTable()
        { }

        public IGsubData GsubData
        {
            get => gsubData;
        }

        //@SuppressWarnings({"squid:S1854"})
        public override void Read(TrueTypeFont ttf, IInputStream data)
        {
            long start = data.Position;
            //@SuppressWarnings({"unused"})
            int majorVersion = data.ReadUInt16();
            int minorVersion = data.ReadUInt16();
            int scriptListOffset = data.ReadUInt16();
            int featureListOffset = data.ReadUInt16();
            int lookupListOffset = data.ReadUInt16();
            //@SuppressWarnings({"unused"})
            long featureVariationsOffset = minorVersion == 1L ? data.ReadUInt32() : -1L;
            scriptList = ReadScriptList(data, start + scriptListOffset);
            featureListTable = ReadFeatureList(data, start + featureListOffset);
            lookupListTable = ReadLookupList(data, start + lookupListOffset);

            var glyphSubstitutionDataExtractor = new GlyphSubstitutionDataExtractor();

            gsubData = glyphSubstitutionDataExtractor.GetGsubData(scriptList, featureListTable, lookupListTable);
            initialized = true;
        }

        private Dictionary<string, ScriptTable> ReadScriptList(IInputStream data, long offset)
        {
            data.Seek(offset);
            int scriptCount = data.ReadUInt16();
            var scriptOffsets = new ushort[scriptCount];
            var scriptTags = new string[scriptCount];
            var resultScriptList = new Dictionary<string, ScriptTable>(scriptCount, StringComparer.Ordinal);
            for (int i = 0; i < scriptCount; i++)
            {
                scriptTags[i] = data.ReadString(4);
                scriptOffsets[i] = data.ReadUInt16();
            }
            for (int i = 0; i < scriptCount; i++)
            {
                var scriptTable = ReadScriptTable(data, offset + scriptOffsets[i]);
                resultScriptList[scriptTags[i]] = scriptTable;
            }
            return resultScriptList;
        }

        private ScriptTable ReadScriptTable(IInputStream data, long offset)
        {
            data.Seek(offset);
            int defaultLangSys = data.ReadUInt16();
            int langSysCount = data.ReadUInt16();
            var langSysTags = new string[langSysCount];
            var langSysOffsets = new ushort[langSysCount];
            for (int i = 0; i < langSysCount; i++)
            {
                langSysTags[i] = data.ReadString(4);
                if (i > 0 && langSysTags[i].CompareTo(langSysTags[i - 1]) <= 0)
                {
                    // PDFBOX-4489: catch corrupt file
                    // https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2#slTbl_sRec
                    Debug.WriteLine($"error: LangSysRecords not alphabetically sorted by LangSys tag: {langSysTags[i]} <= {langSysTags[i - 1]}");
                    return new ScriptTable(null, new Dictionary<string, LangSysTable>(StringComparer.Ordinal));
                }
                langSysOffsets[i] = data.ReadUInt16();
            }

            LangSysTable defaultLangSysTable = null;

            if (defaultLangSys != 0)
            {
                defaultLangSysTable = ReadLangSysTable(data, offset + defaultLangSys);
            }
            var langSysTables = new Dictionary<string, LangSysTable>(langSysCount, StringComparer.Ordinal);
            for (int i = 0; i < langSysCount; i++)
            {
                var langSysTable = ReadLangSysTable(data, offset + langSysOffsets[i]);
                langSysTables[langSysTags[i]] = langSysTable;
            }
            return new ScriptTable(defaultLangSysTable, langSysTables);
        }

        private LangSysTable ReadLangSysTable(IInputStream data, long offset)
        {
            data.Seek(offset);
            int lookupOrder = data.ReadUInt16();
            int requiredFeatureIndex = data.ReadUInt16();
            int featureIndexCount = data.ReadUInt16();
            var featureIndices = new int[featureIndexCount];
            for (int i = 0; i < featureIndexCount; i++)
            {
                featureIndices[i] = data.ReadUInt16();
            }
            return new LangSysTable(lookupOrder, requiredFeatureIndex, featureIndexCount, featureIndices);
        }

        private FeatureListTable ReadFeatureList(IInputStream data, long offset)
        {
            data.Seek(offset);
            int featureCount = data.ReadUInt16();
            FeatureRecord[] featureRecords = new FeatureRecord[featureCount];
            int[] featureOffsets = new int[featureCount];
            string[] featureTags = new string[featureCount];
            for (int i = 0; i < featureCount; i++)
            {
                featureTags[i] = data.ReadString(4);
                if (i > 0 && featureTags[i].CompareTo(featureTags[i - 1]) < 0)
                {
                    // catch corrupt file
                    // https://docs.microsoft.com/en-us/typography/opentype/spec/chapter2#flTbl
                    //if (Regex.IsMatch(featureTags[i], "\\w{4}") && Regex.IsMatch(featureTags[i - 1], "\\w{4}"))
                    //{
                    //    // ArialUni.ttf has many warnings but isn't corrupt, so we assume that only
                    //    // strings with trash characters indicate real corruption
                    //    Debug.WriteLine($"debug: FeatureRecord array not alphabetically sorted by FeatureTag: {featureTags[i]} < {featureTags[i - 1]}");
                    //}
                    //else
                    //{
                    //    Debug.WriteLine($"warn: FeatureRecord array not alphabetically sorted by FeatureTag: {featureTags[i]} < {featureTags[i - 1]}");
                    //    return new FeatureListTable(0, new FeatureRecord[0]);
                    //}
                }
                featureOffsets[i] = data.ReadUInt16();
            }
            for (int i = 0; i < featureCount; i++)
            {
                FeatureTable featureTable = ReadFeatureTable(data, offset + featureOffsets[i]);
                featureRecords[i] = new FeatureRecord(featureTags[i], featureTable);
            }
            return new FeatureListTable(featureCount, featureRecords);
        }

        private FeatureTable ReadFeatureTable(IInputStream data, long offset)
        {
            data.Seek(offset);
            int featureParams = data.ReadUInt16();
            int lookupIndexCount = data.ReadUInt16();
            int[] lookupListIndices = new int[lookupIndexCount];
            for (int i = 0; i < lookupIndexCount; i++)
            {
                lookupListIndices[i] = data.ReadUInt16();
            }
            return new FeatureTable(featureParams, lookupIndexCount, lookupListIndices);
        }

        private LookupListTable ReadLookupList(IInputStream data, long offset)
        {
            data.Seek(offset);
            int lookupCount = data.ReadUInt16();
            int[] lookups = new int[lookupCount];
            for (int i = 0; i < lookupCount; i++)
            {
                lookups[i] = data.ReadUInt16();
            }
            LookupTable[] lookupTables = new LookupTable[lookupCount];
            for (int i = 0; i < lookupCount; i++)
            {
                lookupTables[i] = ReadLookupTable(data, offset + lookups[i]);
            }
            return new LookupListTable(lookupCount, lookupTables);
        }

        private LookupTable ReadLookupTable(IInputStream data, long offset)
        {
            data.Seek(offset);
            int lookupType = data.ReadUInt16();
            int lookupFlag = data.ReadUInt16();
            int subTableCount = data.ReadUInt16();
            var subTableOffsets = new int[subTableCount];
            for (int i = 0; i < subTableCount; i++)
            {
                subTableOffsets[i] = data.ReadUInt16();
            }

            int markFilteringSet;
            if ((lookupFlag & 0x0010) != 0)
            {
                markFilteringSet = data.ReadUInt16();
            }
            else
            {
                markFilteringSet = 0;
            }
            LookupSubTable[] subTables = new LookupSubTable[subTableCount];
            switch (lookupType)
            {
                case 1:
                case 2:
                case 4:
                    for (int i = 0; i < subTableCount; i++)
                    {
                        subTables[i] = ReadLookupSubtable(data, offset + subTableOffsets[i], lookupType);
                    }
                    break;
                case 7:
                    // Extension Substitution
                    // https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#ES
                    for (int i = 0; i < subTableCount; i++)
                    {
                        long baseOffset = data.Position;
                        int substFormat = data.ReadUInt16(); // always 1
                        if (substFormat != 1)
                        {
                            Debug.WriteLine($"error: The expected SubstFormat for ExtensionSubstFormat1 subtable is {substFormat} but should be 1");
                            continue;
                        }
                        int extensionLookupType = data.ReadUInt16();
                        long extensionOffset = data.ReadUInt32();
                        subTables[i] = ReadLookupSubtable(data, baseOffset + extensionOffset, extensionLookupType);
                    }
                    break;
                default:
                    // Other lookup types are not supported
                    Debug.WriteLine($"debug: Type {lookupType} GSUB lookup table is not supported and will be ignored");
                    break;
            }
            return new LookupTable(lookupType, lookupFlag, markFilteringSet, subTables);
        }

        private LookupSubTable ReadLookupSubtable(IInputStream data, long offset, int lookupType)
        {
            switch (lookupType)
            {
                case 1:
                    // Single Substitution Subtable
                    // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#SS
                    return ReadSingleLookupSubTable(data, offset);
                case 2:
                    // Multiple Substitution Subtable
                    // https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookuptype-2-multiple-substitution-subtable
                    return ReadMultipleSubstitutionSubtable(data, offset);
                case 3:
                    // Alternate Substitution Subtable
                    // https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookuptype-3-alternate-substitution-subtable
                    return ReadAlternateSubstitutionSubtable(data, offset);
                case 4:
                    // Ligature Substitution Subtable
                    // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#LS
                    return ReadLigatureSubstitutionSubtable(data, offset);

                // when creating a new LookupSubTable derived type, don't forget to add a "switch"
                // in readLookupTable() and add the type in GlyphSubstitutionDataExtractor.extractData()

                default:
                    // Other lookup types are not supported
                    //Debug.WriteLine($"debug: Type {lookupType} GSUB lookup table is not supported and will be ignored");
                    return null;
                    //TODO next: implement type 6
                    // https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookuptype-6-chained-contexts-substitution-subtable
                    // see e.g. readChainedContextualSubTable in Apache FOP
                    // https://github.com/apache/xmlgraphics-fop/blob/1323c2e3511eb23c7dd9b8fb74463af707fa972d/fop-core/src/main/java/org/apache/fop/complexscripts/fonts/OTFAdvancedTypographicTableReader.java#L898
            }
        }

        private LookupSubTable ReadSingleLookupSubTable(IInputStream data, long offset)
        {
            data.Seek(offset);
            var substFormat = data.ReadUInt16();
            switch (substFormat)
            {
                case 1:
                    {
                        // LookupType 1: Single Substitution Subtable
                        // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#11-single-substitution-format-1
                        int coverageOffset = data.ReadUInt16();
                        var deltaGlyphID = data.ReadInt16();
                        CoverageTable coverageTable = ReadCoverageTable(data, offset + coverageOffset);
                        return new LookupTypeSingleSubstFormat1(substFormat, coverageTable, deltaGlyphID);
                    }
                case 2:
                    {
                        // Single Substitution Format 2
                        // https://docs.microsoft.com/en-us/typography/opentype/spec/gsub#12-single-substitution-format-2
                        int coverageOffset = data.ReadUInt16();
                        int glyphCount = data.ReadUInt16();
                        var substituteGlyphIDs = new ushort[glyphCount];
                        for (int i = 0; i < glyphCount; i++)
                        {
                            substituteGlyphIDs[i] = data.ReadUInt16();
                        }
                        CoverageTable coverageTable = ReadCoverageTable(data, offset + coverageOffset);
                        return new LookupTypeSingleSubstFormat2(substFormat, coverageTable, substituteGlyphIDs);
                    }
                default:
                    Debug.WriteLine($"warn: Unknown substFormat: {substFormat}");
                    return null;
            }
        }

        private LookupSubTable ReadMultipleSubstitutionSubtable(IInputStream data, long offset)
        {
            data.Seek(offset);
            var substFormat = data.ReadUInt16();

            if (substFormat != 1)
            {
                throw new IOException("The expected SubstFormat for LigatureSubstitutionTable is 1");
            }

            var coverage = data.ReadUInt16();
            var sequenceCount = data.ReadUInt16();
            var sequenceOffsets = new int[sequenceCount];
            for (int i = 0; i < sequenceCount; i++)
            {
                sequenceOffsets[i] = data.ReadUInt16();
            }

            var coverageTable = ReadCoverageTable(data, offset + coverage);

            if (sequenceCount != coverageTable.Size)
            {
                throw new IOException(
                        "According to the OpenTypeFont specifications, the coverage count should be equal to the no. of SequenceTables");
            }

            var sequenceTables = new SequenceTable[sequenceCount];
            for (int i = 0; i < sequenceCount; i++)
            {
                data.Seek(offset + sequenceOffsets[i]);
                var glyphCount = data.ReadUInt16();
                var substituteGlyphIDs = data.ReadUShortArray(glyphCount);
                sequenceTables[i] = new SequenceTable(glyphCount, substituteGlyphIDs);
            }

            return new LookupTypeMultipleSubstitutionFormat1(substFormat, coverageTable, sequenceTables);
        }

        private LookupSubTable ReadAlternateSubstitutionSubtable(IInputStream data, long offset)
        {
            data.Seek(offset);
            var substFormat = data.ReadUInt16();

            if (substFormat != 1)
            {
                throw new IOException(
                        "The expected SubstFormat for AlternateSubstitutionTable is 1");
            }

            var coverage = data.ReadUInt16();
            var altSetCount = data.ReadUInt16();

            int[] alternateOffsets = new int[altSetCount];

            for (int i = 0; i < altSetCount; i++)
            {
                alternateOffsets[i] = data.ReadUInt16();
            }

            var coverageTable = ReadCoverageTable(data, offset + coverage);

            if (altSetCount != coverageTable.Size)
            {
                throw new IOException(
                        "According to the OpenTypeFont specifications, the coverage count should be equal to the no. of AlternateSetTable");
            }

            var alternateSetTables = new AlternateSetTable[altSetCount];

            for (int i = 0; i < altSetCount; i++)
            {
                data.Seek(offset + alternateOffsets[i]);
                var glyphCount = data.ReadUInt16();
                var alternateGlyphIDs = data.ReadUShortArray(glyphCount);
                alternateSetTables[i] = new AlternateSetTable(glyphCount, alternateGlyphIDs);
            }

            return new LookupTypeAlternateSubstitutionFormat1(substFormat, coverageTable,
                    alternateSetTables);
        }


        private LookupSubTable ReadLigatureSubstitutionSubtable(IInputStream data, long offset)
        {
            data.Seek(offset);
            var substFormat = data.ReadUInt16();

            if (substFormat != 1)
            {
                throw new IOException("The expected SubstFormat for LigatureSubstitutionTable is 1");
            }

            int coverage = data.ReadUInt16();
            int ligSetCount = data.ReadUInt16();

            int[] ligatureOffsets = new int[ligSetCount];

            for (int i = 0; i < ligSetCount; i++)
            {
                ligatureOffsets[i] = data.ReadUInt16();
            }

            var coverageTable = ReadCoverageTable(data, offset + coverage);

            if (ligSetCount != coverageTable.Size)
            {
                throw new IOException(
                        "According to the OpenTypeFont specifications, the coverage count should be equal to the no. of LigatureSetTables");
            }

            var ligatureSetTables = new LigatureSetTable[ligSetCount];

            for (int i = 0; i < ligSetCount; i++)
            {

                var coverageGlyphId = coverageTable.GetGlyphId(i);

                ligatureSetTables[i] = ReadLigatureSetTable(data,
                        offset + ligatureOffsets[i], coverageGlyphId);
            }

            return new LookupTypeLigatureSubstitutionSubstFormat1(substFormat, coverageTable,
                    ligatureSetTables);
        }

        private LigatureSetTable ReadLigatureSetTable(IInputStream data, long ligatureSetTableLocation,
                ushort coverageGlyphId)
        {
            data.Seek(ligatureSetTableLocation);

            int ligatureCount = data.ReadUInt16();

            int[] ligatureOffsets = new int[ligatureCount];
            LigatureTable[] ligatureTables = new LigatureTable[ligatureCount];

            for (int i = 0; i < ligatureOffsets.Length; i++)
            {
                ligatureOffsets[i] = data.ReadUInt16();
            }

            for (int i = 0; i < ligatureOffsets.Length; i++)
            {
                int ligatureOffset = ligatureOffsets[i];
                ligatureTables[i] = ReadLigatureTable(data,
                        ligatureSetTableLocation + ligatureOffset, coverageGlyphId);
            }

            return new LigatureSetTable(ligatureCount, ligatureTables);
        }

        private LigatureTable ReadLigatureTable(IInputStream data, long ligatureTableLocation,
                ushort coverageGlyphId)
        {
            data.Seek(ligatureTableLocation);

            var ligatureGlyph = data.ReadUInt16();

            int componentCount = data.ReadUInt16();

            var componentGlyphIDs = new ushort[componentCount];

            if (componentCount > 0)
            {
                componentGlyphIDs[0] = coverageGlyphId;
            }

            for (int i = 1; i <= componentCount - 1; i++)
            {
                componentGlyphIDs[i] = data.ReadUInt16();
            }

            return new LigatureTable(ligatureGlyph, componentCount, componentGlyphIDs);

        }

        private CoverageTable ReadCoverageTable(IInputStream data, long offset)
        {
            data.Seek(offset);
            int coverageFormat = data.ReadUInt16();
            switch (coverageFormat)
            {
                case 1:
                    {
                        int glyphCount = data.ReadUInt16();
                        var glyphArray = new ushort[glyphCount];
                        for (int i = 0; i < glyphCount; i++)
                        {
                            glyphArray[i] = data.ReadUInt16();
                        }
                        return new CoverageTableFormat1(coverageFormat, glyphArray);
                    }
                case 2:
                    {
                        int rangeCount = data.ReadUInt16();
                        var rangeRecords = new RangeRecord[rangeCount];


                        for (int i = 0; i < rangeCount; i++)
                        {
                            rangeRecords[i] = ReadRangeRecord(data);
                        }

                        return new CoverageTableFormat2(coverageFormat, rangeRecords);
                    }
                default:
                    // Should not happen (the spec indicates only format 1 and format 2)
                    throw new IOException($"Unknown coverage format: {coverageFormat}");
            }
        }

        /// <summary>
        /// Choose from one of the supplied OpenType script tags, depending on what the font supports and potentially on
        /// context.
        /// </summary>
        /// <param name="tags"></param>
        /// <returns>The best OpenType script tag</returns>
        private string SelectScriptTag(string[] tags)
        {
            if (tags.Length == 1)
            {
                string tag = tags[0];
                if (OpenTypeScript.INHERITED.Equals(tag, StringComparison.Ordinal)
                        || (OpenTypeScript.TAG_DEFAULT.Equals(tag, StringComparison.Ordinal)
                            && !scriptList.ContainsKey(tag)))
                {
                    // We don't know what script this should be.
                    if (lastUsedSupportedScript == null)
                    {
                        // We have no past context and (currently) no way to get future context so we guess.
                        lastUsedSupportedScript = scriptList.Keys.FirstOrDefault();
                    }
                    // else use past context

                    return lastUsedSupportedScript;
                }
            }
            foreach (string tag in tags)
            {
                if (scriptList.ContainsKey(tag))
                {
                    // Use the first recognized tag. We assume a single font only recognizes one version ("ver. 2")
                    // of a single script, or if it recognizes more than one that it prefers the latest one.
                    lastUsedSupportedScript = tag;
                    return lastUsedSupportedScript;
                }
            }
            return tags[0];
        }

        private ICollection<LangSysTable> GetLangSysTables(string scriptTag)
        {
            ICollection<LangSysTable> result = new List<LangSysTable>(0);
            if (scriptList.TryGetValue(scriptTag, out ScriptTable scriptTable))
            {
                if (scriptTable.DefaultLangSysTable == null)
                {
                    result = scriptTable.LangSysTables.Values;
                }
                else
                {
                    result = new List<LangSysTable>(scriptTable.LangSysTables.Values);
                    result.Add(scriptTable.DefaultLangSysTable);
                }
            }
            return result;
        }

        /// <summary>
        /// Get a list of {@code FeatureRecord}s from a collection of {@code LangSysTable}s. Optionally
        /// filter the returned features by supplying a list of allowed feature tags in
        /// {@code enabledFeatures}.
        /// Note that features listed as required ({@code LangSysTable#requiredFeatureIndex}) will be
        /// included even if not explicitly enabled.
        /// </summary>
        /// <param name="langSysTables">The {@code LangSysTable}s indicating {@code FeatureRecord}s to search for</param>
        /// <param name="enabledFeatures">An optional whitelist of feature tags ({@code null} to allow all)</param>
        /// <returns>The indicated {@code FeatureRecord}s</returns>
        private List<FeatureRecord> GetFeatureRecords(ICollection<LangSysTable> langSysTables,
                List<string> enabledFeatures)
        {
            if (langSysTables.Count == 0)
            {
                return new List<FeatureRecord>(0);
            }
            List<FeatureRecord> result = new List<FeatureRecord>();
            foreach (var langSysTable in langSysTables)
            {
                int required = langSysTable.RequiredFeatureIndex;
                FeatureRecord[] featureRecords = featureListTable.FeatureRecords;
                if (required != 0xffff && required < featureRecords.Length) // if no required features = 0xFFFF
                {
                    result.Add(featureRecords[required]);
                }
                foreach (int featureIndex in langSysTable.FeatureIndices)
                {
                    if (featureIndex < featureRecords.Length
                        && (enabledFeatures == null
                        || enabledFeatures.Contains(featureRecords[featureIndex].FeatureTag)))
                    {
                        result.Add(featureRecords[featureIndex]);
                    }
                }
            }

            // 'vrt2' supersedes 'vert' and they should not be used together
            // https://www.microsoft.com/typography/otspec/features_uz.htm
            if (ContainsFeature(result, "vrt2"))
            {
                RemoveFeature(result, "vert");
            }

            if (enabledFeatures != null && result.Count > 1)
            {
                result.Sort((o1, o2) => enabledFeatures.IndexOf(o1.FeatureTag).CompareTo(enabledFeatures.IndexOf(o2.FeatureTag)));
            }

            return result;
        }

        private bool ContainsFeature(List<FeatureRecord> featureRecords, string featureTag)
        {
            return featureRecords.Any(featureRecord => featureRecord.FeatureTag.Equals(featureTag, StringComparison.Ordinal));
        }

        private void RemoveFeature(List<FeatureRecord> featureRecords, string featureTag)
        {
            for (int i = 0; i < featureRecords.Count;)
            {
                var item = featureRecords[i];
                if (item.FeatureTag.Equals(featureTag, StringComparison.Ordinal))
                {
                    featureRecords.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }

        private ushort ApplyFeature(FeatureRecord featureRecord, ushort gid)
        {
            var lookupResult = gid;
            foreach (int lookupListIndex in featureRecord.FeatureTable.LookupListIndices)
            {
                LookupTable lookupTable = lookupListTable.Lookups[lookupListIndex];
                if (lookupTable.LookupType != 1)
                {
                    Debug.WriteLine($"warn: Skipping GSUB feature '{featureRecord.FeatureTag}' because it requires unsupported lookup table type {lookupTable.LookupType}");
                    continue;
                }
                lookupResult = DoLookup(lookupTable, lookupResult);
            }
            return lookupResult;
        }

        private ushort DoLookup(LookupTable lookupTable, ushort gid)
        {
            foreach (LookupSubTable lookupSubtable in lookupTable.SubTables)
            {
                int coverageIndex = lookupSubtable.CoverageTable.GetCoverageIndex(gid);
                if (coverageIndex >= 0)
                {
                    return lookupSubtable.DoSubstitution(gid, coverageIndex);
                }
            }
            return gid;
        }

        /// <summary>
        /// Apply glyph substitutions to the supplied gid. The applicable substitutions are determined by
        /// the {@code scriptTags} which indicate the language of the gid, and by the
        /// {@code enabledFeatures} which acts as a whitelist.
        /// To ensure that a single gid isn't mapped to multiple substitutions, subsequent invocations
        /// with the same gid will return the same result as the first, regardless of script or enabled
        /// features.
        /// </summary>
        /// <param name="gid">GID</param>
        /// <param name="scriptTags">Script tags applicable to the gid (see {@link OpenTypeScript})</param>
        /// <param name="enabledFeatures">Whitelist of features to apply</param>
        public int GetSubstitution(int gid, string[] scriptTags, List<string> enabledFeatures)
        {
            if (gid == -1)
            {
                return -1;
            }
            if (lookupCache.TryGetValue(gid, out int cached))
            {
                // Because script detection for indeterminate scripts (COMMON, INHERIT, etc.) depends on context,
                // it is possible to return a different substitution for the same input. However we don't want that,
                // as we need a one-to-one mapping.
                return cached;
            }
            var scriptTag = SelectScriptTag(scriptTags);
            var langSysTables = GetLangSysTables(scriptTag);
            var featureRecords = GetFeatureRecords(langSysTables, enabledFeatures);
            var sgid = (ushort)gid;
            foreach (FeatureRecord featureRecord in featureRecords)
            {
                sgid = ApplyFeature(featureRecord, sgid);
            }
            lookupCache[gid] = sgid;
            reverseLookup[sgid] = gid;
            return sgid;
        }

        /// <summary>
        /// For a substitute-gid (obtained from {@link #getSubstitution(int, string[], List)}), retrieve
        /// the original gid.
        /// Only gids previously substituted by this instance can be un-substituted. If you are trying to
        /// unsubstitute before you substitute, something is wrong.
        /// </summary>
        /// <param name="sgid">Substitute GID</param>
        public int GetUnsubstitution(int sgid)
        {
            if (!reverseLookup.TryGetValue(sgid, out int gid))
            {
                Debug.WriteLine("warn: Trying to un-substitute a never-before-seen gid: " + sgid);
                return sgid;
            }
            return gid;
        }

        /// <summary>
        /// Builds a new <see cref="IGsubData"/>
        /// instance for given script tag.In contrast to neighbour
        /// {@link #GetGsubData()}
        ///   method, this one does not try to find the first supported language and load GSUB data for it.Instead, it fetches
        ///   the data for the given { @code scriptTag }
        ///   (if it's supported by the font) leaving the language unspecified. It
        ///   means that even after successful reading of GSUB data, the actual glyph substitution may not work if there is no
        ///   corresponding
        ///  { @link GsubWorker }
        ///   implementation for it.
        ///   @implNote This method performs searching on every invocation(no results are cached)
        /// </summary>
        /// <param name="scriptTag">a <a href="https://learn.microsoft.com/en-us/typography/opentype/spec/scripttags">script tag</a>
        /// for which the data is needed</param>
        /// <returns>GSUB data for the given script or { @code null} if no such script in the font</returns>
        public IGsubData GetGsubData(string scriptTag)
        {
            if (!scriptList.TryGetValue(scriptTag, out ScriptTable scriptTable))
            {
                return null;
            }
            return new GlyphSubstitutionDataExtractor().GetGsubData(scriptTag, scriptTable,
                    featureListTable, lookupListTable);
        }

        /// <summary>
        /// A read-only view of the
        /// <a href="https://learn.microsoft.com/en-us/typography/opentype/spec/scripttags">script tags</a> for which this
        /// GSUB table has records
        /// </summary>
        public IReadOnlyCollection<string> SupportedScriptTags
        {
            get => scriptList.Keys;
        }

        private RangeRecord ReadRangeRecord(IInputStream data)
        {
            var startGlyphID = data.ReadUInt16();
            var endGlyphID = data.ReadUInt16();
            int startCoverageIndex = data.ReadUInt16();
            return new RangeRecord(startGlyphID, endGlyphID, startCoverageIndex);
        }

    }
}
