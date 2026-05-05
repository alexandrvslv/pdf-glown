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

using PdfGlown.Documents.Contents.Objects;
using PdfGlown.Documents.Contents.Scanner;
using PdfGlown.Documents.Contents.XObjects;
using PdfGlown.Documents.Interchange.Metadata;

using SkiaSharp;
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents
{
    /// <summary>Content stream context.</summary>
    public interface IContentContext : IAppDataHolder, IContentEntity, ICompositeObject, IResourceProvider
    {
        ///<summary>Gets the bounding box associated with this content context either explicitly
        ///(directly associated to the object) or (if not explicitly available) implicitly (inherited
        ///from a higher level object), expressed in default user-space units.</summary>
        SKRect Box { get; }

        ///<summary>Gets the contents collection representing the content stream associated
        ///with this content context.</summary>
        new ContentWrapper Contents { get; }

        /// <summary>
        /// Renders this content context into the specified rendering context.
        /// </summary>
        /// <param name="context">Rendering context</param>
        /// <param name="box">Rendering canvas size</param>
        /// <param name="clearColor"></param>
        /// <remarks> @since 0.1.0</remarks> 
        void Render(SKCanvas context, SKRect box, SKColor? clearColor = null);

        /// <summary>Gets the rendering rotation of this content context.</summary>
        RotationEnum Rotation { get; }

        int Rotate { get; }

        SKMatrix RotateMatrix { get; }

        List<ITextBlock> TextBlocks { get; }

        TransparencyXObject Group { get; }

    }
}