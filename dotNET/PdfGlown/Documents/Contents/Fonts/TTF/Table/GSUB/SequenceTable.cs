
using System;
/**
 * This class is a part of the
 * <a href="https://docs.microsoft.com/en-us/typography/opentype/spec/gsub">GSUB — Glyph
 * Substitution Table</a> system of tables in the Open Type Font specs. This is a part of the <a href=
 * "https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#lookuptype-2-multiple-substitution-subtable">LookupType
 * 2: Multiple Substitution Subtable</a>. It specifically models the <a href=
 * "https://learn.microsoft.com/en-us/typography/opentype/spec/gsub#21-multiple-substitution-format-1">Sequence
 * table</a>.
 *
 * @author Tilman Hausherr
 *
 */
namespace PdfGlown.Documents.Contents.Fonts.TTF.Table.GSUB
{
    public class SequenceTable
    {
        private readonly int glyphCount;
        private readonly ushort[] substituteGlyphIDs;

        public SequenceTable(int glyphCount, ushort[] substituteGlyphIDs)
        {
            this.glyphCount = glyphCount;
            this.substituteGlyphIDs = substituteGlyphIDs;
        }

        public int GlyphCount => glyphCount;

        public ushort[] SubstituteGlyphIDs => substituteGlyphIDs;

        public override String ToString()
        {
            return $"SequenceTable{{glyphCount={glyphCount}, substituteGlyphIDs={string.Join(',', substituteGlyphIDs)}}}";
        }
    }
}