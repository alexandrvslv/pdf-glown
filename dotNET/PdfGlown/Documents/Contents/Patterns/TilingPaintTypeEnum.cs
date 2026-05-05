/*
  Copyright 2010-2011 Stefano Chizzolini. http://www.pdfclown.org

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

namespace PdfGlown.Documents.Contents.Patterns
{
    ///<summary>Pattern cell color mode.</summary>
    public enum TilingPaintTypeEnum
    {
        /**
          <summary>The pattern's content stream specifies the colors used to paint the pattern cell.</summary>
          <remarks>When the content stream begins execution, the current color is the one
          that was initially in effect in the pattern's parent content stream.</remarks>
        */
        Colored = 1,
        /**
          <summary>The pattern's content stream does NOT specify any color information.</summary>
          <remarks>
            <para>Instead, the entire pattern cell is painted with a separately specified color
            each time the pattern is used; essentially, the content stream describes a stencil
            through which the current color is to be poured.</para>
            <para>The content stream must not invoke operators that specify colors
            or other color-related parameters in the graphics state.</para>
          </remarks>
        */
        Uncolored = 2
    }
}