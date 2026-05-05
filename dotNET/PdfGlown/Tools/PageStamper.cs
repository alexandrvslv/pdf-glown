/*
  Copyright 2007-2012 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfGlown.Documents.Contents.Composition;
using PdfGlown.Documents.Contents.Objects;
using PdfGlown.Objects;

namespace PdfGlown.Tools
{
    /// <summary>Tool for content insertion into existing pages.</summary>
    public sealed class PageStamper
    {
        private PdfPage page;

        private PrimitiveComposer background;
        private PrimitiveComposer foreground;

        public PageStamper() : this(null)
        { }

        public PageStamper(PdfPage page)
        {
            Page = page;
        }

        public void Flush()
        {
            // Ensuring that there's room for the new content chunks inside the page's content stream...
            // NOTE: This specialized stamper is optimized for content insertion without modifying
            // existing content representations, leveraging the peculiar feature of page structures
            // to express their content streams as arrays of data streams.
            PdfArray streams;
            {
                var contentsObject = page.Get(PdfName.Contents);
                var contentsDataObject = contentsObject?.Resolve();
                // Single data stream?
                if (contentsDataObject is PdfStream)
                {
                    // NOTE: Content stream MUST be expressed as an array of data streams in order to host
                    // background- and foreground-stamped contents.
                    page[PdfName.Contents] = streams = new PdfArrayImpl { contentsObject };
                }
                else
                { streams = (PdfArray)contentsDataObject; }
            }

            // Background.
            // Serialize the content!
            background.Flush();
            // Insert the serialized content into the page's content stream!
            streams.Insert(0, background.Scanner.Contents.RefOrSelf);

            // Foreground.
            // Serialize the content!
            foreground.Flush();
            // Append the serialized content into the page's content stream!
            streams.Add(foreground.Scanner.Contents.RefOrSelf);
        }

        public PrimitiveComposer Background => background;

        public PrimitiveComposer Foreground => foreground;

        public PdfPage Page
        {
            get => page;
            set
            {
                page = value;
                if (page == null)
                {
                    background = null;
                    foreground = null;
                }
                else
                {
                    // Background.
                    background = CreateFilter();
                    // Open the background local state!
                    background.Add(SaveGraphicsState.Value);
                    // Close the background local state!
                    background.Add(RestoreGraphicsState.Value);
                    // Open the middleground local state!
                    background.Add(SaveGraphicsState.Value);
                    // Move into the background!
                    background.Index = 1;

                    // Foregrond.
                    foreground = CreateFilter();
                    // Close the middleground local state!
                    foreground.Add(RestoreGraphicsState.Value);
                }
            }
        }

        private PrimitiveComposer CreateFilter() => new PrimitiveComposer(
            new ContentScanner(page,
                new ContentWrapper(page.Document.Register(
                    new PdfStream()))));
    }
}
