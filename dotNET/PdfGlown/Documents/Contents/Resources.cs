/*
  Copyright 2006-2015 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)

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

using PdfGlown.Documents.Contents.ColorSpaces;
using PdfGlown.Documents.Contents.Fonts;
using PdfGlown.Documents.Contents.Patterns;
using PdfGlown.Documents.Contents.Shadings;
using PdfGlown.Documents.Contents.XObjects;
using PdfGlown.Objects;
using PdfGlown.Tokens;
using PdfGlown.Util;
using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents
{
    /// <summary>Resources collection [PDF:1.6:3.7.2].</summary>
    [Pdf(VersionEnum.PDF10)]
    public sealed class Resources : PdfDictionary, ICompositeDictionary<PdfName>
    {
        public Resources()
            : base()
        { }

        public Resources(PdfDocument context)
            : base(context, new())
        { }

        internal Resources(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        public ColorSpaceResources ColorSpaces
        {
            get => GetOrCreate<ColorSpaceResources>(PdfName.ColorSpace);
            set => SetDirect(PdfName.ColorSpace, value);
        }

        public ExtGStateResources ExtGStates
        {
            get => GetOrCreate<ExtGStateResources>(PdfName.ExtGState);
            set => SetDirect(PdfName.ExtGState, value);
        }

        public FontResources Fonts
        {
            get => GetOrCreate<FontResources>(PdfName.Font);
            set => SetDirect(PdfName.Font, value);
        }

        public PatternResources Patterns
        {
            get => GetOrCreate<PatternResources>(PdfName.Pattern);
            set => SetDirect(PdfName.Pattern, value);
        }

        [Pdf(VersionEnum.PDF12)]
        public PropertyListResources PropertyLists
        {
            get => GetOrCreate<PropertyListResources>(PdfName.Properties);
            set
            {
                Document?.CheckCompatibility(VersionEnum.PDF12);
                SetDirect(PdfName.Properties, value);
            }
        }

        [Pdf(VersionEnum.PDF13)]
        public ShadingResources Shadings
        {
            get => GetOrCreate<ShadingResources>(PdfName.Shading);
            set => SetDirect(PdfName.Shading, value);
        }

        public XObjectResources XObjects
        {
            get => GetOrCreate<XObjectResources>(PdfName.XObject);
            set => SetDirect(PdfName.XObject, value);
        }

        public override PdfName ModifyTypeKey(PdfName key)
            => PdfFactory.MapResKeys.TryGetValue(key, out var resKeys) ? resKeys : key;

        public IBiDictionary Get(Type type)
        {
            if (typeof(ColorSpace).IsAssignableFrom(type))
                return ColorSpaces;
            else if (typeof(ExtGState).IsAssignableFrom(type))
                return ExtGStates;
            else if (typeof(PdfFont).IsAssignableFrom(type))
                return Fonts;
            else if (typeof(Pattern).IsAssignableFrom(type))
                return Patterns;
            else if (typeof(PropertyList).IsAssignableFrom(type))
                return PropertyLists;
            else if (typeof(Shading).IsAssignableFrom(type))
                return Shadings;
            else if (typeof(XObject).IsAssignableFrom(type))
                return XObjects;
            else
                throw new ArgumentException(type.Name + " does NOT represent a valid resource class.");
        }

        public T GetRes<T>(PdfName key)
        {
            return (T)Get(typeof(T))?[key] ?? default(T);
        }
    }
}