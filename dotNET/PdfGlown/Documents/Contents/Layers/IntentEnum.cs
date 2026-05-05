/*
  Copyright 2015 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfGlown.Objects;
using PdfGlown.Util;

using System;

namespace PdfGlown.Documents.Contents.Layers
{
    /// <summary>Intended use of layers [PDF:1.7:4.10.1].</summary>
    public enum IntentEnum
    {
        /// <summary>Intended for interactive use by document consumers.</summary>
        View,
        /// <summary>Intended to represent a document designer's structural organization of artwork.
        /// </summary>
        Design,
        /// <summary>Set of all intents (valid for <see cref="ILayerConfiguration.Intents"/> only).</summary>
        All
    }

    public static class IntentEnumExtension
    {
        private static readonly BiDictionary<IntentEnum, PdfName> codes = new()
        {
            [IntentEnum.View] = PdfName.View,
            [IntentEnum.Design] = PdfName.Design,
            [IntentEnum.All] = PdfName.All
        };

        public static IntentEnum Get(PdfName name)
        {
            if (name == null)
                return IntentEnum.View;

            IntentEnum? intent = codes.GetKey(name);
            if (!intent.HasValue)
                throw new NotSupportedException("Intent unknown: " + name);

            return intent.Value;
        }

        public static PdfName Name(this IntentEnum intent) => codes[intent];
    }
}
