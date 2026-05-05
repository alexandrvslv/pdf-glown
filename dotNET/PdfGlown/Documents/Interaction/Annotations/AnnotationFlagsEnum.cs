/*
  Copyright 2008-2015 Stefano Chizzolini. http://www.pdfclown.org

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

using System;
//using System.Diagnostics;

namespace PdfGlown.Documents.Interaction.Annotations
{
    /**
      <summary>Annotation flags [PDF:1.6:8.4.2].</summary>
    */
    [Flags]
    public enum AnnotationFlagsEnum
    {
        /**
          <summary>Hide the annotation, both on screen and on print,
          if it does not belong to one of the standard annotation types
          and no annotation handler is available.</summary>
        */
        Invisible = 0x1,
        /**
          <summary>Hide the annotation, both on screen and on print
          (regardless of its annotation type or whether an annotation handler is available).</summary>
        */
        Hidden = 0x2,
        /**
          <summary>Print the annotation when the page is printed.</summary>
        */
        Print = 0x4,
        /**
          <summary>Do not scale the annotation's appearance to match the magnification of the page.</summary>
        */
        NoZoom = 0x8,
        /**
          <summary>Do not rotate the annotation's appearance to match the rotation of the page.</summary>
        */
        NoRotate = 0x10,
        /**
          <summary>Hide the annotation on the screen.</summary>
        */
        NoView = 0x20,
        /**
          <summary>Do not allow the annotation to interact with the user.</summary>
        */
        ReadOnly = 0x40,
        /**
          <summary>Do not allow the annotation to be deleted or its properties to be modified by the user.</summary>
        */
        Locked = 0x80,
        /**
          <summary>Invert the interpretation of the NoView flag.</summary>
        */
        ToggleNoView = 0x100,
        /**
          <summary>Do not allow the contents of the annotation to be modified by the user.</summary>
        */
        LockedContents = 0x200
    }

}