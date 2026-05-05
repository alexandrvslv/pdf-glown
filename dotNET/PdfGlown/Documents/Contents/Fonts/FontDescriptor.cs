/*
  Copyright 2006-2015 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)
    * Manuel Guilbault (code contributor [FIX:27], manuel.guilbault at gmail.com)

  This file should be part of the source code distribution of "PDF Glown library" (the
  Program): see the accompanying README files for more info.

  This Program is free software; you can redistribute it and/or modify it under the terms
  of the GNU Lesser General Public License as published by the Free Software Foundation;
  either version 3 of the License, or (at your option) any later version.

  This Program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY,
  either expressed or implied; without even the implied warranty of MERCHANTABILITY or
  FITNESS FOR A PARTICULAR PURPOSE. See the License for more details.

  You should have received a copy of the GNU Lesser General Public License along with this
  Program (see README files); if not, go to the GNU website (http://www.gnu.org/licenses/).

  Redistribution and use, with or without modification, are permitted provided that such
  redistributions retain the above copyright notice, license and disclaimer, along with
  this list of conditions.
*/

using PdfGlown.Objects;
using System.Collections.Generic;


namespace PdfGlown.Documents.Contents.Fonts
{
    public class FontDescriptor : PdfDictionary
    {
        public FontDescriptor(PdfDocument context)
            : base(context, new() {
                { PdfName.Type, PdfName.FontDescriptor }
            })
        { }

        internal FontDescriptor(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        public string FontName
        {
            get => GetString(PdfName.FontName);
            set => SetName(PdfName.FontName, value);
        }

        public string FontFamily
        {
            get => GetString(PdfName.FontFamily);
            set => Set(PdfName.FontFamily, value);
        }

        public float? FontStretch
        {
            get => GetFloat(PdfName.FontStretch);
            set => Set(PdfName.FontStretch, value);
        }

        public float? FontWeight
        {
            get => GetNFloat(PdfName.FontWeight);
            set => Set(PdfName.FontWeight, value);
        }

        public FlagsEnum Flags
        {
            get => (FlagsEnum)GetInt(PdfName.Flags);
            set => Set(PdfName.Flags, (int)value);
        }

        public bool HasFlags => ContainsKey(PdfName.Flags);

        public bool NonSymbolic
        {
            get => (Flags & FlagsEnum.Nonsymbolic) == FlagsEnum.Nonsymbolic;
            set
            {
                if (value)
                    Flags |= FlagsEnum.Nonsymbolic;
                else
                    Flags &= ~FlagsEnum.Nonsymbolic;
            }
        }

        public bool Symbolic
        {
            get => (Flags & FlagsEnum.Symbolic) == FlagsEnum.Symbolic;
            set
            {
                if (value)
                    Flags |= FlagsEnum.Symbolic;
                else
                    Flags &= ~FlagsEnum.Symbolic;
            }
        }

        public PdfRectangle FontBBox
        {
            get => Get<PdfRectangle>(PdfName.FontBBox);
            set => SetDirect(PdfName.FontBBox, value);
        }

        public float ItalicAngle
        {
            get => GetFloat(PdfName.ItalicAngle, 0F);
            set => Set(PdfName.ItalicAngle, value);
        }

        public float Ascent
        {
            get => GetFloat(PdfName.Ascent, 750F);
            set => Set(PdfName.Ascent, value);
        }

        public float Descent
        {
            get => GetFloat(PdfName.Descent, -250F);
            set => Set(PdfName.Descent, value);
        }

        public float? Leading
        {
            get => GetNFloat(PdfName.Leading);
            set => Set(PdfName.Leading, value);
        }

        public float? CapHeight
        {
            get => GetNFloat(PdfName.CapHeight);
            set => Set(PdfName.CapHeight, value);
        }

        public float? XHeight
        {
            get => GetNFloat(PdfName.XHeight);
            set => Set(PdfName.XHeight, value);
        }

        public float StemV
        {
            get => GetFloat(PdfName.StemV, 0F);
            set => Set(PdfName.StemV, value);
        }

        public float StemH
        {
            get => GetFloat(PdfName.StemH, 0F);
            set => Set(PdfName.StemH, value);
        }

        public float? AvgWidth
        {
            get => GetNFloat(PdfName.AvgWidth);
            set => Set(PdfName.AvgWidth, value);
        }

        public float? MaxWidth
        {
            get => GetNFloat(PdfName.MaxWidth);
            set => Set(PdfName.MaxWidth, value);
        }

        public float? MissingWidth
        {
            get => GetNFloat(PdfName.MissingWidth);
            set => Set(PdfName.MissingWidth, value);
        }

        public FontFile FontFile
        {
            get => Get<FontFile>(PdfName.FontFile);
            set => Set(PdfName.FontFile, value);
        }

        public FontFile FontFile2
        {
            get => Get<FontFile>(PdfName.FontFile2);
            set => Set(PdfName.FontFile2, value);
        }

        public FontFile FontFile3
        {
            get => Get<FontFile>(PdfName.FontFile3);
            set => Set(PdfName.FontFile3, value);
        }

        public string CharSet
        {
            get => GetString(PdfName.CharSet);
            set => Set(PdfName.CharSet, value);
        }

        //CID Font Specific
        public string Lang
        {
            get => GetString(PdfName.Lang);
            set => SetName(PdfName.Lang, value);
        }

        public FontStyle Style
        {
            get => Get<FontStyle>(PdfName.Style);
            set => SetDirect(PdfName.Style, value);
        }

        public PdfStream CIDSet
        {
            get => Get<PdfStream>(PdfName.CIDSet);
            set => Set(PdfName.CIDSet, value);
        }

    }
}