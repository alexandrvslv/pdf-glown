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

using PdfGlown.Documents.Contents.ColorSpaces;
using PdfGlown.Objects;
using System.Collections.Generic;

namespace PdfGlown.Documents.Multimedia
{

    /// <summary>Media screen parameters [PDF:1.7:9.1.5].</summary>
    [Pdf(VersionEnum.PDF15)]
    public sealed class MediaScreenParameters : PdfDictionary
    {
        private Viability preferences;
        private Viability requirements;

        /// <summary>Media screen parameters viability.</summary>
        public class Viability : PdfObjectWrapper<PdfDictionary>
        {
            private RGBColor backgroundColor;
            public enum WindowTypeEnum
            {
                ///<summary>A floating window.</summary>
                Floating,
                ///<summary>A full-screen window that obscures all other windows.</summary>
                FullScreen,
                ///<summary>A hidden window.</summary>
                Hidden,
                ///<summary>The rectangle occupied by the {@link Screen screen annotation} associated with
                ///the media rendition.</summary>
                Annotation
            }

            public Viability(PdfDirectObject baseObject) : base(baseObject)
            { }

            /// <summary>Gets/Sets the background color for the rectangle in which the media is being played.
            /// </summary>
            /// <remarks>This color is used if the media object does not entirely cover the rectangle or if
            /// it has transparent sections.</remarks>
            public RGBColor BackgroundColor
            {
                get => backgroundColor ??= (RGBColor)RGBColorSpace.Default.GetColor(DataObject.Get<PdfArray>(PdfName.B), null);
                set => DataObject.Set(PdfName.B, backgroundColor = value);
            }

            /// <summary>Gets/Sets the opacity of the background color.</summary>
            /// <returns>A number in the range 0 to 1, where 0 means full transparency and 1 full opacity.
            /// </returns>
            public double BackgroundOpacity
            {
                get => DataObject.GetDouble(PdfName.O, 1d);
                set
                {
                    if (value < 0)
                    { value = 0; }
                    else if (value > 1)
                    { value = 1; }
                    DataObject.Set(PdfName.O, value);
                }
            }

            /// <summary>Gets/Sets the options used in displaying floating windows.</summary>
            public FloatingWindowParameters FloatingWindowParameters
            {
                get => DataObject.GetOrCreate<FloatingWindowParameters>(PdfName.F);
                set => DataObject.Set(PdfName.F, value);
            }

            /// <summary>Gets/Sets which monitor in a multi-monitor system a floating or full-screen window
            /// should appear on.</summary>
            public MonitorSpecifierEnum? MonitorSpecifier
            {
                get => (MonitorSpecifierEnum?)DataObject.GetNInt(PdfName.M);
                set => DataObject.Set(PdfName.M, (int?)value);
            }

            /// <summary>Gets/Sets the type of window that the media object should play in.</summary>
            public WindowTypeEnum? WindowType
            {
                get => (WindowTypeEnum?)DataObject.GetNInt(PdfName.W);
                set => DataObject.Set(PdfName.W, (int?)value);
            }
        }

        public MediaScreenParameters()
           : base(new Dictionary<PdfName, PdfDirectObject> {
                { PdfName.Type, PdfName.MediaScreenParams }
            })
        { }

        public MediaScreenParameters(PdfDocument context)
            : base(context, new Dictionary<PdfName, PdfDirectObject> {
                { PdfName.Type, PdfName.MediaScreenParams }
            })
        { }

        internal MediaScreenParameters(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets/Sets the preferred options the renderer should attempt to honor without affecting
        /// its viability.</summary>
        public Viability Preferences
        {
            get => preferences ??= new(GetOrCreate<PdfDictionary>(PdfName.BE));
            set => Set(PdfName.BE, preferences = value);
        }

        /// <summary>Gets/Sets the minimum requirements the renderer must honor in order to be considered
        /// viable.</summary>
        public Viability Requirements
        {
            get => requirements ??= new(GetOrCreate<PdfDictionary>(PdfName.MH));
            set => Set(PdfName.MH, requirements = value);
        }
    }


}