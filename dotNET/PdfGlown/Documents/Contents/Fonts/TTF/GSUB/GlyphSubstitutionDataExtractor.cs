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
using PdfGlown.Documents.Contents.Fonts.TTF.Model;
using PdfGlown.Documents.Contents.Fonts.TTF.Table.Common;
using PdfGlown.Documents.Contents.Fonts.TTF.Table.GSUB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PdfGlown.Documents.Contents.Fonts.TTF.GSUB
{

    /// <summary>
    /// This class has utility methods to extract meaningful data from the highly obfuscated GSUB Tables.This data is then
    /// used to determine which combination of Glyphs or words have to be replaced.
    /// @author Palash Ray
    /// </summary>
    public class GlyphSubstitutionDataExtractor
    {

        public IGsubData GetGsubData(Dictionary<string, ScriptTable> scriptList, FeatureListTable featureListTable, LookupListTable lookupListTable)
        {
            var scriptTableDetails = GetSupportedLanguage(scriptList);

            if (scriptTableDetails == null)
            {
                return DefaultGsubData.NO_DATA_FOUND;
            }

            return BuildMapBackedGsubData(featureListTable, lookupListTable, scriptTableDetails);
        }

        /**
         * Unlike {@link #getGsubData(Map, FeatureListTable, LookupListTable)}, this method doesn't iterate over supported
         * {@link Language}'s searching for the first match with the scripts of the font. Instead, it unconditionally
         * creates {@link ScriptTableDetails} instance with language left {@linkplain Language#UNSPECIFIED unspecified}.
         * 
         * @return {@link GsubData} instance built especially for the given {@code scriptName}
         */
        public IGsubData GetGsubData(string scriptName, ScriptTable scriptTable,
                FeatureListTable featureListTable, LookupListTable lookupListTable)
        {
            var scriptTableDetails = new ScriptTableDetails(Language.UNSPECIFIED,
                    scriptName, scriptTable);

            return BuildMapBackedGsubData(featureListTable, lookupListTable, scriptTableDetails);
        }

        private MapBackedGsubData BuildMapBackedGsubData(FeatureListTable featureListTable,
                LookupListTable lookupListTable, ScriptTableDetails scriptTableDetails)
        {
            var scriptTable = scriptTableDetails.ScriptTable;

            var gsubData = new Dictionary<string, Dictionary<HashList<ushort>, HashList<ushort>>>(StringComparer.Ordinal);
            // the starting point is really the scriptTags
            if (scriptTable.DefaultLangSysTable != null)
            {
                PopulateGsubData(gsubData, scriptTable.DefaultLangSysTable, featureListTable,
                        lookupListTable);
            }
            foreach (LangSysTable langSysTable in scriptTable.LangSysTables.Values)
            {
                PopulateGsubData(gsubData, langSysTable, featureListTable, lookupListTable);
            }

            return new MapBackedGsubData(scriptTableDetails.Language,
                    scriptTableDetails.FeatureName, gsubData);
        }

        private ScriptTableDetails GetSupportedLanguage(Dictionary<string, ScriptTable> scriptList)
        {
            foreach (Language lang in Enum.GetValues<Language>())
            {
                foreach (var scriptName in lang.GetScriptNames())
                {
                    if (scriptList.TryGetValue(scriptName, out var scriptTable))
                    {
                        return new ScriptTableDetails(lang, scriptName, scriptTable);
                    }
                }
            }
            return null;
        }

        private void PopulateGsubData(Dictionary<string, Dictionary<HashList<ushort>, HashList<ushort>>> gsubData,
                LangSysTable langSysTable, FeatureListTable featureListTable,
                LookupListTable lookupListTable)
        {
            var featureRecords = featureListTable.FeatureRecords;
            foreach (int featureIndex in langSysTable.FeatureIndices)
            {
                if (featureIndex < featureRecords.Length)
                {
                    PopulateGsubData(gsubData, featureRecords[featureIndex], lookupListTable);
                }
            }
        }

        private void PopulateGsubData(Dictionary<string, Dictionary<HashList<ushort>, HashList<ushort>>> gsubData,
                FeatureRecord featureRecord, LookupListTable lookupListTable)
        {
            var lookups = lookupListTable.Lookups;
            var glyphSubstitutionMap = new Dictionary<HashList<ushort>, HashList<ushort>>();
            foreach (int lookupIndex in featureRecord.FeatureTable.LookupListIndices)
            {
                if (lookupIndex < lookups.Length)
                {
                    ExtractData(glyphSubstitutionMap, lookups[lookupIndex]);
                }
            }

            gsubData[featureRecord.FeatureTag] = glyphSubstitutionMap;
        }

        private void ExtractData(Dictionary<HashList<ushort>, HashList<ushort>> glyphSubstitutionMap, LookupTable lookupTable)
        {
            foreach (LookupSubTable lookupSubTable in lookupTable.SubTables)
            {
                switch (lookupSubTable)
                {
                    case LookupTypeLigatureSubstitutionSubstFormat1 subtitution:
                        ExtractDataFromLigatureSubstitutionSubstFormat1Table(glyphSubstitutionMap, subtitution);
                        break;
                    case LookupTypeAlternateSubstitutionFormat1 altsubtitution1:
                        ExtractDataFromAlternateSubstitutionSubstFormat1Table(glyphSubstitutionMap, altsubtitution1);
                        break;
                    case LookupTypeSingleSubstFormat1 substFormat1:
                        ExtractDataFromSingleSubstTableFormat1Table(glyphSubstitutionMap, substFormat1);
                        break;
                    case LookupTypeSingleSubstFormat2 substFormat2:
                        ExtractDataFromSingleSubstTableFormat2Table(glyphSubstitutionMap, substFormat2);
                        break;
                    case LookupTypeMultipleSubstitutionFormat1 msubstFormat1:
                        ExtractDataFromMultipleSubstitutionFormat1Table(glyphSubstitutionMap, msubstFormat1);
                        break;
                    default:
                        break;
                }
            }

        }

        private void ExtractDataFromSingleSubstTableFormat1Table(Dictionary<HashList<ushort>, HashList<ushort>> glyphSubstitutionMap,
            LookupTypeSingleSubstFormat1 singleSubstTableFormat1)
        {
            var coverageTable = singleSubstTableFormat1.CoverageTable;
            for (int i = 0; i < coverageTable.Size; i++)
            {
                var coverageGlyphId = coverageTable.GetGlyphId(i);
                var substituteGlyphId = (coverageGlyphId + singleSubstTableFormat1.DeltaGlyphID);
                PutNewSubstitutionEntry(glyphSubstitutionMap, (HashList<ushort>)substituteGlyphId,
                        (HashList<ushort>)coverageGlyphId);
            }
        }

        private void ExtractDataFromSingleSubstTableFormat2Table(
                Dictionary<HashList<ushort>, HashList<ushort>> glyphSubstitutionMap,
                LookupTypeSingleSubstFormat2 singleSubstTableFormat2)
        {

            var coverageTable = singleSubstTableFormat2.CoverageTable;

            if (coverageTable.Size != singleSubstTableFormat2.SubstituteGlyphIDs.Length)
            {
                Debug.WriteLine("warn: The no. coverage table entries should be the same as the size of the substituteGlyphIDs");
                return;
            }

            for (int i = 0; i < coverageTable.Size; i++)
            {
                var coverageGlyphId = coverageTable.GetGlyphId(i);
                var substituteGlyphId = singleSubstTableFormat2.SubstituteGlyphIDs[i];
                PutNewSubstitutionEntry(glyphSubstitutionMap, (HashList<ushort>)substituteGlyphId,
                        (HashList<ushort>)coverageGlyphId);
            }
        }

        private void ExtractDataFromMultipleSubstitutionFormat1Table(Dictionary<HashList<ushort>, HashList<ushort>> glyphSubstitutionMap,
            LookupTypeMultipleSubstitutionFormat1 multipleSubstFormat1Subtable)
        {
            var coverageTable = multipleSubstFormat1Subtable.CoverageTable;

            if (coverageTable.Size != multipleSubstFormat1Subtable.SequenceTables.Length)
            {
                Debug.WriteLine("warn: The no. coverage table entries should be the same as the size of the sequencce tables");
                return;
            }

            for (int i = 0; i < coverageTable.Size; i++)
            {
                var coverageGlyphId = coverageTable.GetGlyphId(i);
                var sequenceTable = multipleSubstFormat1Subtable.SequenceTables[i];

                var substituteGlyphIDArray = sequenceTable.SubstituteGlyphIDs;

                PutNewSubstitutionEntry(glyphSubstitutionMap,
                        (HashList<ushort>)substituteGlyphIDArray,
                        (HashList<ushort>)coverageGlyphId);
            }

        }

        private void ExtractDataFromLigatureSubstitutionSubstFormat1Table(Dictionary<HashList<ushort>, HashList<ushort>> glyphSubstitutionMap,
                LookupTypeLigatureSubstitutionSubstFormat1 ligatureSubstitutionTable)
        {
            foreach (LigatureSetTable ligatureSetTable in ligatureSubstitutionTable.LigatureSetTables)
            {
                foreach (LigatureTable ligatureTable in ligatureSetTable.getLigatureTables())
                {
                    ExtractDataFromLigatureTable(glyphSubstitutionMap, ligatureTable);
                }

            }
        }

        //   *
        //* Extracts data from the AlternateSubstitutionFormat1(lookuptype) 3 table and puts it in the
        //* glyphSubstitutionMap.
        //*
        //* @param glyphSubstitutionMap         the map to store the substitution data
        //* @param alternateSubstitutionFormat1 the alternate substitution format 1 table


        private void ExtractDataFromAlternateSubstitutionSubstFormat1Table(Dictionary<HashList<ushort>, HashList<ushort>> glyphSubstitutionMap,
                LookupTypeAlternateSubstitutionFormat1 alternateSubstitutionFormat1)
        {

            var coverageTable = alternateSubstitutionFormat1.CoverageTable;

            if (coverageTable.Size != alternateSubstitutionFormat1.AlternateSetTables.Length)
            {
                Debug.WriteLine($"warn:The coverage table size ({coverageTable.Size}) should be the same as the count of the alternate set tables ({alternateSubstitutionFormat1.AlternateSetTables.Length})");
                return;
            }

            for (int i = 0; i < coverageTable.Size; i++)
            {
                var coverageGlyphId = coverageTable.GetGlyphId(i);
                var sequenceTable = alternateSubstitutionFormat1.AlternateSetTables[i];

                // Loop through the substitute glyphs and pick the first one that is not the same as the coverage glyph
                foreach (var alternateGlyphId in sequenceTable.AlternateGlyphIDs)
                {
                    if (alternateGlyphId != coverageGlyphId)
                    {
                        PutNewSubstitutionEntry(glyphSubstitutionMap, (HashList<ushort>)alternateGlyphId,
                                (HashList<ushort>)coverageGlyphId);
                        break;
                    }
                }
            }
        }

        private void ExtractDataFromLigatureTable(Dictionary<HashList<ushort>, HashList<ushort>> glyphSubstitutionMap,
                LigatureTable ligatureTable)
        {
            PutNewSubstitutionEntry(glyphSubstitutionMap, (HashList<ushort>)ligatureTable.LigatureGlyph, (HashList<ushort>)ligatureTable.ComponentGlyphIDs);
        }

        private void PutNewSubstitutionEntry(Dictionary<HashList<ushort>, HashList<ushort>> glyphSubstitutionMap,
                HashList<ushort> newGlyphList, HashList<ushort> glyphsToBeSubstituted)
        {
            if (glyphSubstitutionMap.TryGetValue(glyphsToBeSubstituted, out var oldValue))
            {
                Debug.WriteLine($"warning: For the newGlyph: {newGlyphList}, newValue: {glyphsToBeSubstituted} is trying to override the oldValue: {oldValue}");
            }
            glyphSubstitutionMap[glyphsToBeSubstituted] = newGlyphList;
        }

        private class ScriptTableDetails
        {
            private readonly Language language;
            private readonly string featureName;
            private readonly ScriptTable scriptTable;

            public ScriptTableDetails(Language language, string featureName, ScriptTable scriptTable)
            {
                this.language = language;
                this.featureName = featureName;
                this.scriptTable = scriptTable;
            }

            public Language Language
            {
                get => language;
            }

            public string FeatureName
            {
                get => featureName;
            }

            public ScriptTable ScriptTable
            {
                get => scriptTable;
            }

        }

    }

    public class ListComparer<T> : IEqualityComparer<List<T>>
    {
        public bool Equals(List<T> x, List<T> y)
        {
            return x.SequenceEqual(y);
        }

        public int GetHashCode(List<T> obj)
        {
            int hashcode = 0;
            foreach (T t in obj)
            {
                hashcode ^= t.GetHashCode();
            }
            return hashcode;
        }
    }

    public class ArrayComparer<T> : IEqualityComparer<T[]>
    {
        public bool Equals(T[] x, T[] y)
        {
            return x.AsSpan().SequenceEqual(y);
        }

        public int GetHashCode(T[] obj)
        {
            int hashcode = 0;
            foreach (T t in obj)
            {
                hashcode ^= t.GetHashCode();
            }
            return hashcode;
        }
    }
}
