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

namespace PdfGlown.Documents.Contents.Fonts.TTF
{
    /// <summary>
    /// A table in a true type font.
    /// @author Ben Litchfield
    /// </summary>
    public class MaximumProfileTable : TTFTable
    {
        /// <summary>A tag that identifies this table type.</summary>
        public const string TAG = "maxp";

        private float version;
        private ushort numGlyphs;
        private ushort maxPoints;
        private ushort maxContours;
        private ushort maxCompositePoints;
        private ushort maxCompositeContours;
        private ushort maxZones;
        private ushort maxTwilightPoints;
        private ushort maxStorage;
        private ushort maxFunctionDefs;
        private ushort maxInstructionDefs;
        private ushort maxStackElements;
        private ushort maxSizeOfInstructions;
        private ushort maxComponentElements;
        private ushort maxComponentDepth;

        public MaximumProfileTable()
        { }

        public ushort MaxComponentDepth
        {
            get => maxComponentDepth;
            set => maxComponentDepth = value;
        }

        public ushort MaxComponentElements
        {
            get => maxComponentElements;
            set => maxComponentElements = value;
        }

        public ushort MaxCompositeContours
        {
            get => maxCompositeContours;
            set => maxCompositeContours = value;
        }

        public ushort MaxCompositePoints
        {
            get => maxCompositePoints;
            set => maxCompositePoints = value;
        }

        public ushort MaxContours
        {
            get => maxContours;
            set => maxContours = value;
        }

        public ushort MaxFunctionDefs
        {
            get => maxFunctionDefs;
            set => maxFunctionDefs = value;
        }

        public ushort MaxInstructionDefs
        {
            get => maxInstructionDefs;
            set => maxInstructionDefs = value;
        }

        public ushort MaxPoints
        {
            get => maxPoints;
            set => maxPoints = value;
        }

        public ushort MaxSizeOfInstructions
        {
            get => maxSizeOfInstructions;
            set => maxSizeOfInstructions = value;
        }

        public ushort MaxStackElements
        {
            get => maxStackElements;
            set => maxStackElements = value;
        }

        public ushort MaxStorage
        {
            get => maxStorage;
            set => maxStorage = value;
        }

        public ushort MaxTwilightPoints
        {
            get => maxTwilightPoints;
            set => maxTwilightPoints = value;
        }

        public ushort MaxZones
        {
            get => maxZones;
            set => maxZones = value;
        }

        public ushort NumGlyphs
        {
            get => numGlyphs;
            set => numGlyphs = value;
        }

        public float Version
        {
            get => version;
            set => version = value;
        }

        /// <summary>This will read the required data from the stream.</summary>
        /// <param name="ttf">The font that is being read.</param>
        /// <param name="data">The stream to read the data from.</param>
        public override void Read(TrueTypeFont ttf, IInputStream data)
        {
            version = data.Read32Fixed();
            numGlyphs = data.ReadUInt16();
            if (version >= 1.0f)
            {
                maxPoints = data.ReadUInt16();
                maxContours = data.ReadUInt16();
                maxCompositePoints = data.ReadUInt16();
                maxCompositeContours = data.ReadUInt16();
                maxZones = data.ReadUInt16();
                maxTwilightPoints = data.ReadUInt16();
                maxStorage = data.ReadUInt16();
                maxFunctionDefs = data.ReadUInt16();
                maxInstructionDefs = data.ReadUInt16();
                maxStackElements = data.ReadUInt16();
                maxSizeOfInstructions = data.ReadUInt16();
                maxComponentElements = data.ReadUInt16();
                maxComponentDepth = data.ReadUInt16();
            }
            initialized = true;
        }
    }
}