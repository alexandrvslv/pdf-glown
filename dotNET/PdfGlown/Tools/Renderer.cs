/*
  Copyright 2010 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfGlown.Documents;
using PdfGlown.Documents.Contents;
using SkiaSharp;
using System.Collections.Generic;

namespace PdfGlown.Tools
{
    /// <summary>Tool for rendering <see cref="IContentContext">content contexts</see>.</summary>
    /// <remarks>It wraps a page collection for printing purposes.</remarks>
    public sealed class Renderer
    {
        /// <summary>Prints silently the specified document.</summary>
        /// <param name="document">Document to print.</param>
        /// <returns>Whether the print was fulfilled.</returns>
        public bool Print(PdfDocument document)
        {
            return Print(document.Pages);
        }

        /// <summary>Prints the specified document.</summary>
        /// <param name="document">Document to print.</param>
        /// <param name="silent">Whether to avoid showing a print dialog.</param>
        /// <returns>Whether the print was fulfilled.</returns>
        public bool Print(PdfDocument document, bool silent)
        {
            return Print(document.Pages, silent);
        }

        /// <summary>Prints silently the specified page collection.</summary>
        /// <param name="pages">Page collection to print.</param>
        /// <returns>Whether the print was fulfilled.</returns>
        public bool Print(IList<PdfPage> pages)
        {
            return Print(pages, true);
        }

        /// <summary>Prints the specified page collection.</summary>
        /// <param name="pages">Page collection to print.</param>
        /// <param name="silent">Whether to avoid showing a print dialog.</param>
        /// <returns>Whether the print was fulfilled.</returns>
        public bool Print(IList<PdfPage> pages, bool silent)
        {
            using (var stream = new SKFileWStream("print.xps"))
            using (var document = SKDocument.CreateXps(stream))
            {
                foreach (var page in pages)
                {
                    using (var canvas = document.BeginPage(page.Size.Width, page.Size.Height))
                    {
                        page.Render(canvas, page.Box);
                    }
                }
                document.Close();
            }

            return true;
        }

        /// <summary>Renders the specified content context into an image context.</summary>
        /// <param name="contentContext">Source content context.</param>
        /// <param name="size">Image size expressed in device-space units (that is typically pixels).</param>
        /// <returns>Image representing the rendered contents.</returns>
        public SKBitmap Render(IContentContext contentContext, SKSize size)
        {
            return Render(contentContext, size, null);
        }

        /// <summary>Renders the specified content context into an image context.</summary>
        /// <param name="contentContext">Source content context.</param>
        /// <param name="size">Image size expressed in device-space units (that is typically pixels).</param>
        /// <param name="area">Content area to render; <code>null</code> corresponds to the entire
        /// <see cref="IContentContext.Box">content bounding box</see>.</param>
        /// <returns>Image representing the rendered contents.</returns>
        public SKBitmap Render(IContentContext contentContext, SKSize size, SKRect? area)
        {
            //TODO:area!
            var image = new SKBitmap(
              (int)size.Width,
              (int)size.Height,
              SKColorType.Rgba8888,
              SKAlphaType.Opaque
              //PixelFormat.Format24bppRgb
              );
            using (var canvas = new SKCanvas(image))
                contentContext.Render(canvas, SKRect.Create(size));
            return image;
        }
    }
}
