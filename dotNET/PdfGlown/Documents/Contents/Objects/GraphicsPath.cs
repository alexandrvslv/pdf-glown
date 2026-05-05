/*
  Copyright 2008-2010 Stefano Chizzolini. http://www.pdfclown.org

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

using SkiaSharp;
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents.Objects
{
    /// <summary>Path object [PDF:1.6:4.4].</summary>
    [Pdf(VersionEnum.PDF10)]
    public  class GraphicsPath : CompositeObject, IBoxed
    {
        public GraphicsPath()
        { }

        public GraphicsPath(IList<ContentObject> operations) : base(operations)
        { }

        /// <summary>Creates the rendering object corresponding to this container.</summary>
        private static SKPath CreatePath() => new();

        public override void Scan(GraphicsState state)
        {
            // Render the inner elements!
            using var path = CreatePath();
            Scan(state, path);
            PostScan(state);
        }

        private void Scan(GraphicsState state, SKPath path)
        {
            state.Scanner.Path = path;
            base.Scan(state);           
        }

        protected virtual void PostScan(GraphicsState state)
        {
            state.Scanner.Path?.Dispose();
            state.Scanner.Path = null;
        }

        public SKRect GetBox(GraphicsState state)
        {
            using var path = CreatePath();
            Scan(state, path);
            return state.Ctm.MapRect(path.Bounds);
        }
    }
}
