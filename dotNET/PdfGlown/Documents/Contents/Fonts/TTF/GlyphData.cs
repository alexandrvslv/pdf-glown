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
using SkiaSharp;

namespace PdfGlown.Documents.Contents.Fonts.TTF
{
    /// <summary>
    /// A glyph data record in the glyf table.
    /// @author Ben Litchfield
    /// </summary>
    public class GlyphData
    {
        private short xMin;
        private short yMin;
        private short xMax;
        private short yMax;
        private SKRect? boundingBox = null;
        private short numberOfContours;
        private GlyfDescript glyphDescription = null;
        private GlyphRenderer renderer;

        /// <summary>This will read the required data from the stream.</summary>
        /// <param name="glyphTable">The glyph table this glyph belongs to.</param>
        /// <param name="data">The stream to read the data from.</param>
        /// <param name="leftSideBearing">The left side bearing for this glyph.</param>
        public void InitData(GlyphTable glyphTable, IInputStream data, int leftSideBearing, int level)
        {
            numberOfContours = data.ReadInt16();
            xMin = data.ReadInt16();
            yMin = data.ReadInt16();
            xMax = data.ReadInt16();
            yMax = data.ReadInt16();
            boundingBox = new SKRect(xMin, yMin, xMax, yMax);

            if (numberOfContours >= 0)
            {
                // create a simple glyph
                short x0 = (short)(leftSideBearing - xMin);
                glyphDescription = new GlyfSimpleDescript(numberOfContours, data, x0);
            }
            else
            {
                // create a composite glyph
                glyphDescription = new GlyfCompositeDescript(data, glyphTable, level + 1);
            }
        }

        /// <summary>Initialize an empty glyph record.</summary>
        public void InitEmptyData()
        {
            glyphDescription = new GlyfSimpleDescript();
            boundingBox = SKRect.Empty;
        }

        public SKRect BoundingBox
        {
            get => boundingBox ?? SKRect.Empty;
            set => boundingBox = value;
        }

        public short NumberOfContours
        {
            get => numberOfContours;
            set => numberOfContours = value;
        }

        public IGlyphDescription Description
        {
            get => glyphDescription;
        }

        /// <summary>Returns the path of the glyph.</summary>
        public SKPath GetPath()
        {
            return (renderer ??= new GlyphRenderer(glyphDescription)).GetPath();
        }
        
        public short XMaximum
        {
            get => xMax;
        }

        public short XMinimum
        {
            get => xMin;
        }

        public short YMaximum
        {
            get => yMax;
        }

        public short YMinimum
        {
            get => yMin;
        }
    }

}