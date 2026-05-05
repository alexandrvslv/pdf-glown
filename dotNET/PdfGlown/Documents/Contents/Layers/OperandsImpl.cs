/*
  Copyright 2015 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)

  This file should be part of the source code distribution of "PDF Glown library"
  (the Program): see the accompanying README files for more info.

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

using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents.Layers
{
    internal class OperandsImpl : ArrayWrapper<IPdfObjectWrapper>
    {
        private class ItemWrapper : IEntryWrapper<IPdfObjectWrapper>
        {
            private readonly Dictionary<PdfDirectObject, IPdfObjectWrapper> cache = new();

            public IPdfObjectWrapper Wrap(PdfDirectObject baseObject)
            {
                return baseObject?.Resolve(PdfName.OCG) is PdfDirectObject dataObject
                ? cache.TryGetValue(baseObject, out var opers) ? opers : cache[baseObject] = Wrap(baseObject, dataObject)
                : null;
            }

            private static IPdfObjectWrapper Wrap(PdfDirectObject baseObject, PdfDirectObject dataObject)
            {
                if (dataObject is PdfArray)
                    return new VisibilityExpression(baseObject);
                else
                    return (Layer)baseObject;
            }
        }

        public OperandsImpl(PdfDirectObject baseObject)
            : base(baseObject, new ItemWrapper())
        { }

        public override int Count => base.Count - 1;

        public override int IndexOf(IPdfObjectWrapper item)
        {
            int index = base.IndexOf(item);
            return index > 0 ? index - 1 : -1;
        }

        public override void Insert(int index, IPdfObjectWrapper item)
        {
            if (PdfName.Not.Equals(base[0]) && base.Count >= 2)
                throw new ArgumentException("'Not' operator requires only one operand.");

            ValidateItem(item);
            base.Insert(index + 1, item);
        }

        public override void RemoveAt(int index)
        { base.RemoveAt(index + 1); }

        public override IPdfObjectWrapper this[int index]
        {
            get => base[index + 1];
            set
            {
                ValidateItem(value);
                base[index + 1] = value;
            }
        }

        private static void ValidateItem(IPdfObjectWrapper item)
        {
            if (!(item is VisibilityExpression
              || item is Layer))
                throw new ArgumentException("Operand MUST be either VisibilityExpression or Layer");
        }
    }
}

