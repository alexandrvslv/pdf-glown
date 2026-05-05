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
     * Represents the "Panose" entry of a FontDescriptor's Style dictionary. This is a sequence of 12
     * bytes which contain both the TTF sFamilyClass and PANOSE classification bytes. 
     *
     * @author John Hewson
     */
    public class Panose
    {
        public static readonly int PanoseLength = 12;

        private readonly Memory<byte> bytes;
        private PanoseClassification classification;

        public Panose(Memory<byte> bytes)
        {
            this.bytes = bytes;
        }

        /**
         * The font family class and subclass ID bytes, given in the sFamilyClass field of the
         * “OS/2” table in a TrueType font.
         * 
         * @see <a href="http://www.microsoft.com/typography/otspec/ibmfc.htm">http://www.microsoft.com/typography/otspec/ibmfc.htm</a>
         */
        public int FamilyClass
        {
            get => (bytes.Span[0] & 0xff) << 8 | (bytes.Span[1] & 0xff);
        }

        /**
         * Ten bytes for the PANOSE classification number for the font.
         * 
         * @see <a href="http://www.monotype.com/services/pan1">http://www.monotype.com/services/pan1</a>
         */
        public PanoseClassification PanoseClassification
        {
            get => classification ??= new PanoseClassification(bytes.Slice(2, 10));
        }

        public Memory<byte> Bytes
        {
            get => bytes;
        }
    }
}