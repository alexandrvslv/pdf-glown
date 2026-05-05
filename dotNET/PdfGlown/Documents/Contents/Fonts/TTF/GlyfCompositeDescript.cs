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

using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PdfGlown.Bytes;

namespace PdfGlown.Documents.Contents.Fonts.TTF
{
    /// <summary>
    /// Glyph description for composite glyphs. Composite glyphs are made up of one
    /// or more simple glyphs, usually with some sort of transformation applied to
    /// each.
    /// This class is based on code from Apache Batik a subproject of Apache
    /// XMLGraphics. see http://xmlgraphics.apache.org/batik/ for further details.
    /// </summary>
    public class GlyfCompositeDescript : GlyfDescript
    {

        private readonly List<GlyfCompositeComp> components = new();
        private readonly Dictionary<int, IGlyphDescription> descriptions = new();
        private GlyphTable glyphTable = null;
        private bool beingResolved = false;
        private bool resolved = false;
        private int pointCount = -1;
        private int contourCount = -1;

        /// <summary>Constructor.</summary>
        /// <param name="bais">the stream to be read</param>
        /// <param name="glyphTable">the Glyphtable containing all glyphs</param>
        /// <param name="level">current level</param>
        public GlyfCompositeDescript(IInputStream bais, GlyphTable glyphTable, int level)
             : base((short)-1)
        {
            this.glyphTable = glyphTable;

            // Get all of the composite components
            GlyfCompositeComp comp;
            do
            {
                comp = new GlyfCompositeComp(bais);
                components.Add(comp);
            }
            while ((comp.Flags & GlyfCompositeComp.MORE_COMPONENTS) != 0);

            // Are there hinting instructions to read?
            if ((comp.Flags & GlyfCompositeComp.WE_HAVE_INSTRUCTIONS) != 0)
            {
                ReadInstructions(bais, (bais.ReadUInt16()));
            }
            InitDescriptions(level);
        }

        public override void Resolve()
        {
            if (resolved)
            {
                return;
            }
            if (beingResolved)
            {
                Debug.WriteLine("error: Circular reference in GlyfCompositeDesc");
                return;
            }
            beingResolved = true;

            int firstIndex = 0;
            int firstContour = 0;

            foreach (GlyfCompositeComp comp in components)
            {
                comp.FirstIndex = firstIndex;
                comp.FirstContour = firstContour;

                if (descriptions.TryGetValue(comp.GlyphIndex, out IGlyphDescription desc))
                {
                    desc.Resolve();
                    firstIndex += desc.PointCount;
                    firstContour += desc.ContourCount;
                }
            }
            resolved = true;
            beingResolved = false;
        }

        public override int GetEndPtOfContours(int i)
        {
            GlyfCompositeComp c = GetCompositeCompEndPt(i);
            if (c != null)
            {
                descriptions.TryGetValue(c.GlyphIndex, out IGlyphDescription gd);
                return gd.GetEndPtOfContours(i - c.FirstContour) + c.FirstIndex;
            }
            return 0;
        }

        public override byte GetFlags(int i)
        {
            GlyfCompositeComp c = GetCompositeComp(i);
            if (c != null)
            {
                descriptions.TryGetValue(c.GlyphIndex, out IGlyphDescription gd);
                return gd.GetFlags(i - c.FirstIndex);
            }
            return 0;
        }

        public override short GetXCoordinate(int i)
        {
            GlyfCompositeComp c = GetCompositeComp(i);
            if (c != null && descriptions.TryGetValue(c.GlyphIndex, out IGlyphDescription gd))
            {
                int n = i - c.FirstIndex;
                int x = gd.GetXCoordinate(n);
                int y = gd.GetYCoordinate(n);
                return (short)(c.ScaleX(x, y) + c.XTranslate);
            }
            return 0;
        }

        public override short GetYCoordinate(int i)
        {
            GlyfCompositeComp c = GetCompositeComp(i);
            if (c != null && descriptions.TryGetValue(c.GlyphIndex, out IGlyphDescription gd))
            {
                int n = i - c.FirstIndex;
                int x = gd.GetXCoordinate(n);
                int y = gd.GetYCoordinate(n);
                return (short)(c.ScaleY(x, y) + c.YTranslate);
            }
            return 0;
        }

        public override bool IsComposite
        {
            get => true;
        }

        public override int PointCount
        {
            get
            {
                if (!resolved)
                {
                    Debug.WriteLine("error: getPointCount called on unresolved GlyfCompositeDescript");
                }
                if (pointCount < 0)
                {
                    GlyfCompositeComp c = components[components.Count - 1];
                    if (!descriptions.TryGetValue(c.GlyphIndex, out IGlyphDescription gd))
                    {
                        Debug.WriteLine($"error: GlyphDescription for index {c.GlyphIndex} is null, returning 0");
                        pointCount = 0;
                    }
                    else
                    {
                        pointCount = c.FirstIndex + gd.PointCount;
                    }
                }
                return pointCount;
            }
        }

        public override int ContourCount
        {
            get
            {
                if (!resolved)
                {
                    Debug.WriteLine("error: getContourCount called on unresolved GlyfCompositeDescript");
                }
                if (contourCount < 0)
                {
                    GlyfCompositeComp c = components[components.Count - 1];
                    if (!descriptions.TryGetValue(c.GlyphIndex, out IGlyphDescription gd))
                    {
                        Debug.WriteLine("error: missing glyph description for index " + c.GlyphIndex);
                        contourCount = 0;
                    }
                    else
                    {
                        contourCount = c.FirstContour + gd.ContourCount;
                    }
                }
                return contourCount;
            }
        }

        /// <summary>Get number of components.</summary>
        public int ComponentCount
        {
            get => components.Count;
        }

        /// <summary>
        /// Gets a view to the composite components.
        /// unmodifiable list of this composite glyph's <see cref="GlyfCompositeComp"> components}
        /// </summary>
        public List<GlyfCompositeComp> Components
        {
            get => components;
        }

        private GlyfCompositeComp GetCompositeComp(int i)
        {
            foreach (GlyfCompositeComp c in components)
            {
                if (c.FirstIndex <= i
                    && descriptions.TryGetValue(c.GlyphIndex, out IGlyphDescription gd)
                    && i < (c.FirstIndex + gd.PointCount))
                {
                    return c;
                }
            }
            return null;
        }

        private GlyfCompositeComp GetCompositeCompEndPt(int i)
        {
            foreach (GlyfCompositeComp c in components)
            {
                ;
                if (c.FirstContour <= i
                    && descriptions.TryGetValue(c.GlyphIndex, out IGlyphDescription gd)
                    && i < (c.FirstContour + gd.ContourCount))
                {
                    return c;
                }
            }
            return null;
        }

        private void InitDescriptions(int level)
        {
            foreach (GlyfCompositeComp component in components)
            {
                try
                {
                    int index = component.GlyphIndex;
                    var glyph = glyphTable.GetGlyph(index, level);
                    if (glyph != null)
                    {
                        descriptions[index] = glyph.Description;
                    }
                }
                catch (IOException e)
                {
                    Debug.WriteLine($"error: {e}");
                }
            }
        }
    }
}
