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

using System;

namespace PdfGlown.Documents.Contents.Fonts
{

    /**
     * Represents a 10-byte <a href="http://monotype.de/services/pan2">PANOSE classification</a>.
     *
     * @author John Hewson
     */
    public class PanoseClassification
    {
        public static readonly int PanoseLength = 10;

        private readonly Memory<byte> bytes;

        public PanoseClassification(Memory<byte> bytes)
        {
            this.bytes = bytes;
        }

        public Span<byte> Span => bytes.Span;

        public int FamilyKind
        {
            get => Span[0];
        }

        public int SerifStyle
        {
            get => Span[1];
        }

        public int Weight
        {
            get => Span[2];
        }

        public int Proportion
        {
            get => Span[3];
        }

        public int Contrast
        {
            get => Span[4];
        }

        public int StrokeVariation
        {
            get => Span[5];
        }

        public int ArmStyle
        {
            get => Span[6];
        }

        public int Letterform
        {
            get => Span[7];
        }

        public int Midline
        {
            get => Span[8];
        }

        public int XHeight
        {
            get => Span[9];
        }

        public Memory<byte> AsMemory() => bytes;


        public override string ToString()
        {
            return "{ FamilyKind = " + FamilyKind + ", " +
                     "SerifStyle = " + SerifStyle + ", " +
                     "Weight = " + Weight + ", " +
                     "Proportion = " + Proportion + ", " +
                     "Contrast = " + Contrast + ", " +
                     "StrokeVariation = " + StrokeVariation + ", " +
                     "ArmStyle = " + ArmStyle + ", " +
                     "Letterform = " + Letterform + ", " +
                     "Midline = " + Midline + ", " +
                     "XHeight = " + XHeight + "}";
        }
    }
}