/*
  Copyright 2010-2012 Stefano Chizzolini. http://www.pdfclown.org

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

namespace PdfGlown.Documents.Contents.ColorSpaces
{
    /// <summary>Color value defined by numeric-level components.</summary>
    public abstract class LeveledColor : Color
    {
        protected LeveledColor(ColorSpace colorSpace, PdfDirectObject baseObject) 
            : base(colorSpace, baseObject)
        { }

        public sealed override PdfArray Components => (PdfArray)DataObject;

        public override bool Equals(object obj)
        {
            if (obj == null
              || !obj.GetType().Equals(GetType()))
                return false;

            return PdfArray.SequenceEquals(Components, ((LeveledColor)obj).Components);
        }

        public sealed override int GetHashCode()
        {
            int hashCode = 0;
            foreach (var component in Components.GetItems())
            {
                hashCode ^= component.GetHashCode();
            }
            return hashCode;
        }

        /// <summary>Gets the specified color component.</summary>
        /// <param name="index">Component index.</param>
        protected float this[int index]
        {
            get => Components.GetFloat(index);
            set => Components.Set(index, NormalizeComponent(value));
        }
    }
}