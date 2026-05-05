/*

   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for additional information regarding copyright ownership.
   The ASF licenses this file to You under the Apache License, Version 2.0
   (the "License"); you may not use this file except in compliance with
   the License.  You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.

 */
using PdfGlown.Bytes;

namespace PdfGlown.Documents.Contents.Fonts.TTF
{
    /// <summary>
    /// This class is based on code from Apache Batik a subproject of Apache XMLGraphics.see
    /// http://xmlgraphics.apache.org/batik/ for further details.
    /// </summary>
    public class GlyfSimpleDescript : GlyfDescript
    {

        /// <summary>Constructor for an empty description.</summary>
        public GlyfSimpleDescript()
            : base(0)
        {
            pointCount = 0;
        }

        private ushort[] endPtsOfContours;
        private byte[] flags;
        private short[] xCoordinates;
        private short[] yCoordinates;
        private readonly int pointCount;

        /// <summary>Constructor.</summary>
        /// <param name="numberOfContours">number of contours</param>
        /// <param name="bais">the stream to be read</param>
        /// <param name="x0">the initial X-position</param>
        public GlyfSimpleDescript(short numberOfContours, IInputStream bais, short x0)
            : base(numberOfContours)
        {

            /*
             * https://developer.apple.com/fonts/TTRefMan/RM06/Chap6glyf.html
             * "If a glyph has zero contours, it need not have any glyph data." set the pointCount to zero to initialize
             * attributes and avoid nullpointer but maybe there shouldn't have GlyphDescript in the GlyphData?
             */
            if (numberOfContours == 0)
            {
                pointCount = 0;
                return;
            }

            // Simple glyph description
            endPtsOfContours = bais.ReadUShortArray(numberOfContours);

            int lastEndPt = endPtsOfContours[numberOfContours - 1];
            if (numberOfContours == 1 && lastEndPt == 65535)
            {
                // PDFBOX-2939: assume an empty glyph
                pointCount = 0;
                return;
            }
            // The last end point index reveals the total number of points
            pointCount = lastEndPt + 1;

            flags = new byte[pointCount];
            xCoordinates = new short[pointCount];
            yCoordinates = new short[pointCount];

            int instructionCount = bais.ReadUInt16();
            ReadInstructions(bais, instructionCount);
            ReadFlags(pointCount, bais);
            ReadCoords(pointCount, bais, x0);
        }

        public override int GetEndPtOfContours(int i)
        {
            return endPtsOfContours[i];
        }

        public override byte GetFlags(int i)
        {
            return flags[i];
        }

        public override short GetXCoordinate(int i)
        {
            return xCoordinates[i];
        }

        public override short GetYCoordinate(int i)
        {
            return yCoordinates[i];
        }

        public override bool IsComposite
        {
            get => false;
        }

        public override int PointCount
        {
            get => pointCount;
        }

        /// <summary>The table is stored as relative values, but we'll store them as absolutes.</summary>
        private void ReadCoords(int count, IInputStream bais, short x0)
        {
            short x = x0;
            short y = 0;
            for (int i = 0; i < count; i++)
            {
                if ((flags[i] & X_DUAL) != 0)
                {
                    if ((flags[i] & X_SHORT_VECTOR) != 0)
                    {
                        x += (short)bais.ReadByte();
                    }
                }
                else
                {
                    if ((flags[i] & X_SHORT_VECTOR) != 0)
                    {
                        x -= (short)bais.ReadByte();
                    }
                    else
                    {
                        x += bais.ReadInt16();
                    }
                }
                xCoordinates[i] = x;
            }

            for (int i = 0; i < count; i++)
            {
                if ((flags[i] & Y_DUAL) != 0)
                {
                    if ((flags[i] & Y_SHORT_VECTOR) != 0)
                    {
                        y += (short)bais.ReadByte();
                    }
                }
                else
                {
                    if ((flags[i] & Y_SHORT_VECTOR) != 0)
                    {
                        y -= bais.ReadUByte();
                    }
                    else
                    {
                        y += bais.ReadInt16();
                    }
                }
                yCoordinates[i] = y;
            }
        }

        /// <summary>The flags are run-length encoded.</summary>
        private void ReadFlags(int flagCount, IInputStream bais)
        {
            for (int index = 0; index < flagCount; index++)
            {
                flags[index] = (byte)bais.ReadByte();
                if ((flags[index] & REPEAT) != 0)
                {
                    int repeats = bais.ReadByte();
                    for (int i = 1; i <= repeats && index + i < flags.Length; i++)
                    {
                        flags[index + i] = flags[index];
                    }
                    index += repeats;
                }
            }
        }
    }
}
