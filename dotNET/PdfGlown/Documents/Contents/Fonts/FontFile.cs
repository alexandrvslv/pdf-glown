/*
  Copyright 2006-2015 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)
    * Manuel Guilbault (code contributor [FIX:27], manuel.guilbault at gmail.com)

  This file should be part of the source code distribution of "PDF Glown library" (the
  Program): see the accompanying README files for more info.

  This Program is free software; you can redistribute it and/or modify it under the terms
  of the GNU Lesser General Public License as published by the Free Software Foundation;
  either version 3 of the L
  Redistribution and use, with or without modification, are permitted provided that such
  redistributions retain the above copyright notice, license and disclaimer, along with
  this list of conditions.
*/

using PdfGlown.Bytes;
using PdfGlown.Objects;
using System.Collections.Generic;


namespace PdfGlown.Documents.Contents.Fonts
{
    public class FontFile : PdfStream
    {
        public FontFile(PdfDocument context, IInputStream stream)
            : base(context, stream)
        {
            Length1 = (int)stream.Length;
        }

        internal FontFile(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        internal FontFile(Dictionary<PdfName, PdfDirectObject> baseObject, IInputStream stream)
            : base(baseObject, stream)
        { }

        public string Subtype
        {
            get => GetString(PdfName.Subtype);
            set => SetName(PdfName.Subtype, value);
        }

        public int Length1
        {
            get => GetInt(PdfName.Length1);
            set => Set(PdfName.Length1, value);
        }

        public int Length2
        {
            get => GetInt(PdfName.Length2);
            set => Set(PdfName.Length2, value);
        }

        public int Length3
        {
            get => GetInt(PdfName.Length3);
            set => Set(PdfName.Length3, value);
        }

    }
}