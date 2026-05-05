/*
  Copyright 2012 Stefano Chizzolini. http://www.pdfclown.org

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
using SkiaSharp;
using System.Collections.Generic;

namespace PdfGlown.Documents.Multimedia
{
    public class FloatingWindowParameters : PdfDictionary
    {
        public enum LocationEnum
        {
            ///<summary>Upper-left corner.</summary>
            UpperLeft,
            ///<summary>Upper center.</summary>
            UpperCenter,
            ///<summary>Upper-right corner.</summary>
            UpperRight,
            ///<summary>Center left.</summary>
            CenterLeft,
            ///<summary>Center.</summary>
            Center,
            ///<summary>Center right.</summary>
            CenterRight,
            ///<summary>Lower-left corner.</summary>
            LowerLeft,
            ///<summary>Lower center.</summary>
            LowerCenter,
            ///<summary>Lower-right corner.</summary>
            LowerRight
        }

        public enum OffscreenBehaviorEnum
        {
            ///<summary>Take no special action.</summary>
            None,
            ///<summary>Move and/or resize the window so that it is on-screen.</summary>
            Adapt,
            ///<summary>Consider the object to be non-viable.</summary>
            NonViable
        }

        public enum RelatedWindowEnum
        {
            ///<summary>The document window.</summary>
            Document,
            ///<summary>The application window.</summary>
            Application,
            ///<summary>The full virtual desktop.</summary>
            Desktop,
            ///<summary>The monitor specified by <see cref="MediaScreenParameters.Viability.MonitorSpecifier"/>.</summary>
            Custom
        }

        public enum ResizeBehaviorEnum
        {
            ///<summary>Not resizable.</summary>
            None,
            ///<summary>Resizable preserving its aspect ratio.</summary>
            AspectRatioLocked,
            ///<summary>Resizable without preserving its aspect ratio.</summary>
            Free
        }

        public FloatingWindowParameters()
            : base(new Dictionary<PdfName, PdfDirectObject>(7) {
                { PdfName.Type, PdfName.FWParams }
            })
        { }

        public FloatingWindowParameters(SKSize size)
            : this()
        {
            Size = size;
        }

        internal FloatingWindowParameters(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets/Sets the location where the floating window should be positioned relative to
        /// the related window.</summary>
        public LocationEnum? Location
        {
            get => (LocationEnum?)GetNInt(PdfName.P);
            set => Set(PdfName.P, (int?)value);
        }

        /// <summary>Gets/Sets what should occur if the floating window is positioned totally or
        /// partially offscreen (that is, not visible on any physical monitor).</summary>
        public OffscreenBehaviorEnum? OffscreenBehavior
        {
            get => (OffscreenBehaviorEnum?)GetNInt(PdfName.O);
            set => Set(PdfName.O, (int?)value);
        }

        /// <summary>Gets/Sets the window relative to which the floating window should be positioned.
        /// </summary>
        public RelatedWindowEnum? RelatedWindow
        {
            get => (RelatedWindowEnum?)GetNInt(PdfName.RT);
            set => Set(PdfName.RT, (int?)value);
        }

        /// <summary>Gets/Sets how the floating window may be resized by a user.</summary>
        public ResizeBehaviorEnum? ResizeBehavior
        {
            get => (ResizeBehaviorEnum?)GetNInt(PdfName.R);
            set => Set(PdfName.R, (int?)value);
        }

        /// <summary>Gets/Sets the floating window's width and height, in pixels.</summary>
        /// <remarks>These values correspond to the dimensions of the rectangle in which the media
        /// will play, not including such items as title bar and resizing handles.</remarks>
        public SKSize Size
        {
            get
            {
                var sizeObject = Get<PdfArray>(PdfName.D);
                return new SKSize(sizeObject.GetInt(0), sizeObject.GetInt(1));
            }
            set => this[PdfName.D] = new PdfArrayImpl(2) { (int)value.Width, (int)value.Height };
        }

        /// <summary>Gets/Sets whether the floating window should include user interface elements that
        /// allow a user to close it.</summary>
        /// <remarks>Meaningful only if <see cref="TitleBarVisible"/> is true.</remarks>
        public bool Closeable
        {
            get => GetBool(PdfName.UC, true);
            set => Set(PdfName.UC, value);
        }

        /// <summary>Gets/Sets whether the floating window should have a title bar.</summary>
        public bool TitleBarVisible
        {
            get => GetBool(PdfName.T, true);
            set => Set(PdfName.T, value);
        }

        //TODO: TT entry!
    }


}