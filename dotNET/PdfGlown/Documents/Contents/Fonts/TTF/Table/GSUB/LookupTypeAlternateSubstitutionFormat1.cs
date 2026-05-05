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
using PdfGlown.Documents.Contents.Fonts.TTF.Table.Common;
using System;

namespace PdfGlown.Documents.Contents.Fonts.TTF.Table.GSUB
{
    /// <summary>
    /// Lookup Type 3: Alternate Substitution Subtable
    /// as described in OpenType spec: <a href="https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#31-alternate-substitution-format-1">...</a>
    /// </summary>
    public class LookupTypeAlternateSubstitutionFormat1 : LookupSubTable
    {
        private readonly AlternateSetTable[] alternateSetTables;

        public LookupTypeAlternateSubstitutionFormat1(ushort substFormat, CoverageTable coverageTable, AlternateSetTable[] alternateSetTables)
                    : base(substFormat, coverageTable)
        {
            ;
            this.alternateSetTables = alternateSetTables;
        }

        public AlternateSetTable[] AlternateSetTables
        {
            get => alternateSetTables;
        }

        public override ushort DoSubstitution(ushort gid, int coverageIndex)
        {
            throw new Exception("not applicable");
        }
    }
}
