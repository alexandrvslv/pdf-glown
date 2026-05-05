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
using System.Diagnostics;


namespace PdfGlown.Documents.Contents.Fonts.TTF
{
    /// <summary>
    /// A table in a true type font.
    /// @author Ben Litchfield
    /// </summary>
    public class OS2WindowsMetricsTable : TTFTable
    {

        /// <summary>Weight class constant.</summary>         
        public const int WEIGHT_CLASS_THIN = 100;

        /// <summary>Weight class constant.</summary>         
        public const int WEIGHT_CLASS_ULTRA_LIGHT = 200;
        
        /// <summary>Weight class constant.</summary>
        public const int WEIGHT_CLASS_LIGHT = 300;
        /**
         * Weight class constant.
         */
        public const int WEIGHT_CLASS_NORMAL = 400;
        /**
         * Weight class constant.
         */
        public const int WEIGHT_CLASS_MEDIUM = 500;
        /**
         * Weight class constant.
         */
        public const int WEIGHT_CLASS_SEMI_BOLD = 600;
        /**
         * Weight class constant.
         */
        public const int WEIGHT_CLASS_BOLD = 700;
        /**
         * Weight class constant.
         */
        public const int WEIGHT_CLASS_EXTRA_BOLD = 800;
        /**
         * Weight class constant.
         */
        public const int WEIGHT_CLASS_BLACK = 900;

        /**
         * Width class constant.
         */
        public const int WIDTH_CLASS_ULTRA_CONDENSED = 1;
        /**
         * Width class constant.
         */
        public const int WIDTH_CLASS_EXTRA_CONDENSED = 2;
        /**
         * Width class constant.
         */
        public const int WIDTH_CLASS_CONDENSED = 3;
        /**
         * Width class constant.
         */
        public const int WIDTH_CLASS_SEMI_CONDENSED = 4;
        /**
         * Width class constant.
         */
        public const int WIDTH_CLASS_MEDIUM = 5;
        /**
         * Width class constant.
         */
        public const int WIDTH_CLASS_SEMI_EXPANDED = 6;
        /**
         * Width class constant.
         */
        public const int WIDTH_CLASS_EXPANDED = 7;
        /**
         * Width class constant.
         */
        public const int WIDTH_CLASS_EXTRA_EXPANDED = 8;
        /**
         * Width class constant.
         */
        public const int WIDTH_CLASS_ULTRA_EXPANDED = 9;

        /**
         * Family class constant.
         */
        public const int FAMILY_CLASS_NO_CLASSIFICATION = 0;
        /**
         * Family class constant.
         */
        public const int FAMILY_CLASS_OLDSTYLE_SERIFS = 1;
        /**
         * Family class constant.
         */
        public const int FAMILY_CLASS_TRANSITIONAL_SERIFS = 2;
        /**
         * Family class constant.
         */
        public const int FAMILY_CLASS_MODERN_SERIFS = 3;
        /**
         * Family class constant.
         */
        public const int FAMILY_CLASS_CLAREDON_SERIFS = 4;
        /**
         * Family class constant.
         */
        public const int FAMILY_CLASS_SLAB_SERIFS = 5;
        /**
         * Family class constant.
         */
        public const int FAMILY_CLASS_FREEFORM_SERIFS = 7;
        /**
         * Family class constant.
         */
        public const int FAMILY_CLASS_SANS_SERIF = 8;
        /**
         * Family class constant.
         */
        public const int FAMILY_CLASS_ORNAMENTALS = 9;
        /**
         * Family class constant.
         */
        public const int FAMILY_CLASS_SCRIPTS = 10;
        /**
         * Family class constant.
         */
        public const int FAMILY_CLASS_SYMBOLIC = 12;

        /**
         * Restricted License embedding: must not be modified, embedded or exchanged in any manner.
         *
         * <p>For Restricted License embedding to take effect, it must be the only level of embedding
         * selected.
         */
        public static readonly short FSTYPE_RESTRICTED = 0x0002;

        /**
         * Preview and Print embedding: the font may be embedded, and temporarily loaded on the
         * remote system. No edits can be applied to the document.
         */
        public static readonly short FSTYPE_PREVIEW_AND_PRINT = 0x0004;

        /**
         * Editable embedding: the font may be embedded but must only be installed temporarily on other
         * systems. Documents may be edited and changes saved.
         */
        public static readonly short FSTYPE_EDITIBLE = 0x0008;

        /**
         * No subsetting: the font must not be subsetted prior to embedding.
         */
        public static readonly short FSTYPE_NO_SUBSETTING = 0x0100;

        /**
         * Bitmap embedding only: only bitmaps contained in the font may be embedded. No outline data
         * may be embedded. Other embedding restrictions specified in bits 0-3 and 8 also apply.
         */
        public static readonly short FSTYPE_BITMAP_ONLY = 0x0200;
        private static readonly byte[] PanoseBytes = new byte[10];

        private ushort version;
        private short averageCharWidth;
        private ushort weightClass;
        private ushort widthClass;
        private short fsType;
        private short subscriptXSize;
        private short subscriptYSize;
        private short subscriptXOffset;
        private short subscriptYOffset;
        private short superscriptXSize;
        private short superscriptYSize;
        private short superscriptXOffset;
        private short superscriptYOffset;
        private short strikeoutSize;
        private short strikeoutPosition;
        private short familyClass;
        private byte[] panose = PanoseBytes;
        private uint unicodeRange1;
        private uint unicodeRange2;
        private uint unicodeRange3;
        private uint unicodeRange4;
        private string achVendId = "XXXX";
        private ushort fsSelection;
        private ushort firstCharIndex;
        private ushort lastCharIndex;
        private short typoAscender;
        private short typoDescender;
        private short typoLineGap;
        private ushort winAscent;
        private ushort winDescent;
        private uint codePageRange1;
        private uint codePageRange2;
        private short sxHeight;
        private short sCapHeight;
        private ushort usDefaultChar;
        private ushort usBreakChar;
        private ushort usMaxContext;

        public OS2WindowsMetricsTable()
        { }

        /**
         * @return Returns the achVendId.
         */
        public string AchVendId
        {
            get => achVendId;
            set => achVendId = value;
        }

        /**
         * @return Returns the averageCharWidth.
         */
        public short AverageCharWidth
        {
            get => averageCharWidth;
            set => averageCharWidth = value;
        }

        /**
         * @return Returns the codePageRange1.
         */
        public uint CodePageRange1
        {
            get => codePageRange1;
            set => codePageRange1 = value;
        }

        /**
         * @return Returns the codePageRange2.
         */
        public uint CodePageRange2
        {
            get => codePageRange2;
            set => codePageRange2 = value;
        }

        /**
         * @return Returns the familyClass.
         */
        public short FamilyClass
        {
            get => familyClass;
            set => familyClass = value;
        }

        /**
         * @return Returns the firstCharIndex.
         */
        public ushort FirstCharIndex
        {
            get => firstCharIndex;
            set => firstCharIndex = value;
        }

        /**
         * @return Returns the fsSelection.
         */
        public ushort FsSelection
        {
            get => fsSelection;
            set => fsSelection = value;
        }

        /**
         * @return Returns the fsType.
         */
        public short FsType
        {
            get => fsType;
            set => fsType = value;
        }

        /**
         * @return Returns the lastCharIndex.
         */
        public ushort LastCharIndex
        {
            get => lastCharIndex;
            set => lastCharIndex = value;
        }

        /**
         * @return Returns the panose.
         */
        public byte[] Panose
        {
            get => panose;
            set => panose = value;
        }

        /**
         * @return Returns the strikeoutPosition.
         */
        public short StrikeoutPosition
        {
            get => strikeoutPosition;
            set => strikeoutPosition = value;
        }

        /**
         * @return Returns the strikeoutSize.
         */
        public short StrikeoutSize
        {
            get => strikeoutSize;
            set => strikeoutSize = value;
        }

        /**
         * @return Returns the subscriptXOffset.
         */
        public short SubscriptXOffset
        {
            get => subscriptXOffset;
            set => subscriptXOffset = value;
        }

        /**
         * @return Returns the subscriptXSize.
         */
        public short SubscriptXSize
        {
            get => subscriptXSize;
            set => subscriptXSize = value;
        }

        /**
         * @return Returns the subscriptYOffset.
         */
        public short SubscriptYOffset
        {
            get => subscriptYOffset;
            set => subscriptYOffset = value;
        }

        /**
         * @return Returns the subscriptYSize.
         */
        public short SubscriptYSize
        {
            get => subscriptYSize;
            set => subscriptYSize = value;
        }

        /**
         * @return Returns the superscriptXOffset.
         */
        public short SuperscriptXOffset
        {
            get => superscriptXOffset;
            set => superscriptXOffset = value;
        }

        /**
         * @return Returns the superscriptXSize.
         */
        public short SuperscriptXSize
        {
            get => superscriptXSize;
            set => superscriptXSize = value;
        }

        /**
         * @return Returns the superscriptYOffset.
         */
        public short SuperscriptYOffset
        {
            get => superscriptYOffset;
            set => superscriptYOffset = value;
        }

        /**
         * @return Returns the superscriptYSize.
         */
        public short SuperscriptYSize
        {
            get => superscriptYSize;
            set => superscriptYSize = value;
        }

        /**
         * @return Returns the typoLineGap.
         */
        public short TypoLineGap
        {
            get => typoLineGap;
            set => typoLineGap = value;
        }

        /**
         * @return Returns the typoAscender.
         */
        public short TypoAscender
        {
            get => typoAscender;
            set => typoAscender = value;
        }

        /**
         * @return Returns the typoDescender.
         */
        public short TypoDescender
        {
            get => typoDescender;
            set => typoDescender = value;
        }

        /**
         * @return Returns the unicodeRange1.
         */
        public uint UnicodeRange1
        {
            get => unicodeRange1;
            set => unicodeRange1 = value;
        }

        /**
         * @return Returns the unicodeRange2.
         */
        public uint UnicodeRange2
        {
            get => unicodeRange2;
            set => unicodeRange2 = value;
        }

        /**
         * @return Returns the unicodeRange3.
         */
        public uint UnicodeRange3
        {
            get => unicodeRange3;
            set => unicodeRange3 = value;
        }

        /**
         * @return Returns the unicodeRange4.
         */
        public uint UnicodeRange4
        {
            get => unicodeRange4;
            set => unicodeRange4 = value;
        }

        /**
         * @return Returns the version.
         */
        public ushort Version
        {
            get => version;
            set => version = value;
        }

        /**
         * @return Returns the weightClass.
         */
        public ushort WeightClass
        {
            get => weightClass;
            set => weightClass = value;
        }

        /**
         * @return Returns the widthClass.
         */
        public ushort WidthClass
        {
            get => widthClass;
            set => widthClass = value;
        }

        /**
         * @return Returns the winAscent.
         */
        public ushort WinAscent
        {
            get => winAscent;
            set => winAscent = value;
        }

        /**
         * @return Returns the winDescent.
         */
        public ushort WinDescent
        {
            get => winDescent;
            set => winDescent = value;
        }

        /**
         * Returns the sxHeight.
         */
        public short Height
        {
            get => sxHeight;
            set => sxHeight = value;
        }

        /**
         * Returns the sCapHeight.
         */
        public short CapHeight
        {
            get => sCapHeight;
        }

        /**
         * Returns the usDefaultChar.
         */
        public ushort DefaultChar
        {
            get => usDefaultChar;
        }

        /**
         * Returns the usBreakChar.
         */
        public ushort BreakChar
        {
            get => usBreakChar;
        }

        /**
         * Returns the usMaxContext.
         */
        public ushort MaxContext
        {
            get => usMaxContext;
        }

        /**
         * A tag that identifies this table type.
         */
        public const string TAG = "OS/2";

        /**
         * This will read the required data from the stream.
         * 
         * @param ttf The font that is being read.
         * @param data The stream to read the data from.
         * @ If there is an error reading the data.
         */
        public override void Read(TrueTypeFont ttf, IInputStream data)
        {
            var limit = Offset + Length;
            if (limit > data.Length)
                limit = data.Length;
            version = data.ReadUInt16();
            averageCharWidth = data.ReadInt16();
            weightClass = data.ReadUInt16();
            widthClass = data.ReadUInt16();
            fsType = data.ReadInt16();
            subscriptXSize = data.ReadInt16();
            subscriptYSize = data.ReadInt16();
            subscriptXOffset = data.ReadInt16();
            subscriptYOffset = data.ReadInt16();
            superscriptXSize = data.ReadInt16();
            superscriptYSize = data.ReadInt16();
            superscriptXOffset = data.ReadInt16();
            superscriptYOffset = data.ReadInt16();
            strikeoutSize = data.ReadInt16();
            strikeoutPosition = data.ReadInt16();
            familyClass = data.ReadInt16();
            panose = data.ReadSpan(10).ToArray();
            unicodeRange1 = data.ReadUInt32();
            unicodeRange2 = data.ReadUInt32();
            unicodeRange3 = data.ReadUInt32();
            unicodeRange4 = data.ReadUInt32();
            achVendId = data.ReadString(4);
            fsSelection = data.ReadUInt16();
            firstCharIndex = data.ReadUInt16();
            lastCharIndex = data.ReadUInt16();
            if ((limit - data.Position) >= 10)
            {
                typoAscender = data.ReadInt16();
                typoDescender = data.ReadInt16();
                typoLineGap = data.ReadInt16();
                winAscent = data.ReadUInt16();
                winDescent = data.ReadUInt16();
            }
            else
            {
                Debug.WriteLine("warn: EOF, probably some legacy TrueType font ");
                initialized = true;
                return;
            }
            if (version >= 1)
            {
                if ((limit - data.Position) >= 4)
                {
                    codePageRange1 = data.ReadUInt32();
                    codePageRange2 = data.ReadUInt32();
                }
                else
                {
                    version = 0;
                    Debug.WriteLine("warn: Could not read all expected parts of version >= 1, setting version to 0 ");
                }
            }
            if (version >= 2)
            {
                if ((limit - data.Position) >= 10)
                {
                    sxHeight = data.ReadInt16();
                    sCapHeight = data.ReadInt16();
                    usDefaultChar = data.ReadUInt16();
                    usBreakChar = data.ReadUInt16();
                    usMaxContext = data.ReadUInt16();
                }
                else
                {
                    version = 1;
                    Debug.WriteLine("warn: Could not read all expected parts of version >= 2, setting version to 1 ");
                }
            }
            initialized = true;
        }
    }
}