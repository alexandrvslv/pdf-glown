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

using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Multimedia
{

    /// <summary>Media play parameters [PDF:1.7:9.1.4].</summary>
    [Pdf(VersionEnum.PDF15)]
    public sealed class MediaPlayParameters : PdfDictionary
    {
        private Viability rpreferences;
        private Viability requirements;

        /// <summary>Media player parameters viability.</summary>
        public class Viability : PdfObjectWrapper<PdfDictionary>
        {
            public enum FitModeEnum
            {
                /// <summary>The media's width and height are scaled while preserving the aspect ratio so that
                /// the media and play rectangles have the greatest possible intersection while still
                /// displaying all media content. Same as <code>meet</code> value of SMIL's fit attribute.
                /// </summary>
                Meet,
                /// <summary>The media's width and height are scaled while preserving the aspect ratio so that
                /// the play rectangle is entirely filled, and the amount of media content that does not fit
                /// within the play rectangle is minimized. Same as <code>slice</code> value of SMIL's fit
                /// attribute.</summary>
                Slice,
                /// <summary>The media's width and height are scaled independently so that the media and play
                /// rectangles are the same; the aspect ratio is not necessarily preserved. Same as
                /// <code>fill</code> value of SMIL's fit attribute.</summary>
                Fill,
                /// <summary>The media is not scaled. A scrolling user interface is provided if the media
                /// rectangle is wider or taller than the play rectangle. Same as <code>scroll</code> value of
                /// SMIL's fit attribute.</summary>
                Scroll,
                /// <summaryCThe media is not scaled. Only the portions of the media rectangle that intersect
                /// the play rectangle are displayed. Same as <code>hidden</code> value of SMIL's fit attribute.
                /// </summary>
                Hidden,
                /// <summary>Use the player's default setting (author has no preference).</summary>
                Default
            }

            public Viability(PdfDirectObject baseObject)
                : base(baseObject)
            { }

            /// <summary>Gets/Sets whether the media should automatically play when activated.</summary>
            public bool Autoplay
            {
                get => DataObject.GetBool(PdfName.A, true);
                set => DataObject.Set(PdfName.A, value);
            }

            /// <summary>Gets/Sets the temporal duration, corresponding to the notion of simple duration in
            /// SMIL.</summary>
            /// <returns>
            /// <list type="bullet">
            ///   <item><code>Double.NEGATIVE_INFINITY</code>: intrinsic duration of the associated media;
            ///   </item>
            ///   <item><code>Double.POSITIVE_INFINITY</code>: infinite duration;</item>
            ///   <item>non-infinite positive: explicit duration.</item>
            /// </list>
            /// </returns>
            public double Duration
            {
                get => DurationInstance?.Value ?? Double.NegativeInfinity;
                set => DurationInstance.Value = value;
            }

            private MediaDuration DurationInstance
            {
                get => DataObject.GetOrCreate<MediaDuration>(PdfName.D);
                set => DataObject.Set(PdfName.D, value);
            }

            /// <summary>Gets/Sets the manner in which the player should treat a visual media type that does
            /// not exactly fit the rectangle in which it plays.</summary>
            public FitModeEnum? FitMode
            {
                get => (FitModeEnum?)DataObject.GetNInt(PdfName.F);
                set => DataObject.Set(PdfName.F, (int?)value);
            }

            /// <summary>Gets/Sets whether to display a player-specific controller user interface (for
            /// example, play/pause/stop controls) when playing.</summary>
            public bool PlayerSpecificControl
            {
                get => DataObject.GetBool(PdfName.C, false);
                set => DataObject.Set(PdfName.C, value);
            }

            /// <summary>Gets/Sets the number of iterations of the duration to repeat; similar to SMIL's
            /// <code>repeatCount</code> attribute.</summary>
            /// <returns>
            ///   <list type="bullet">
            ///     <item><code>0</code>: repeat forever;</item>
            ///   </list>
            /// </returns>
            public double RepeatCount
            {
                get => DataObject.GetDouble(PdfName.RC, 1d);
                set => DataObject.Set(PdfName.RC, value);
            }

            /// <summary>Gets/Sets the volume level as a percentage of recorded volume level. A zero value
            /// is equivalent to mute.</summary>
            public int Volume
            {
                get => DataObject.GetInt(PdfName.V, 100);
                set
                {
                    if (value < 0)
                    { value = 0; }
                    else if (value > 100)
                    { value = 100; }
                    DataObject.Set(PdfName.V, value);
                }
            }
        }

        public MediaPlayParameters()
            : base(new Dictionary<PdfName, PdfDirectObject>(4) {
                { PdfName.Type, PdfName.MediaPlayParams }
            })
        { }

        public MediaPlayParameters(PdfDocument context)
            : base(context, new Dictionary<PdfName, PdfDirectObject>(4) {
                { PdfName.Type, PdfName.MediaPlayParams }
            })
        { }

        internal MediaPlayParameters(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets/Sets the player rules for playing this media.</summary>
        public MediaPlayers Players
        {
            get => GetOrCreate<MediaPlayers>(PdfName.PL);
            set => Set(PdfName.PL, value);
        }

        /// <summary>Gets/Sets the preferred options the renderer should attempt to honor without affecting
        /// its viability.</summary>
        public Viability Preferences
        {
            get => rpreferences ??= new(GetOrCreate<PdfDictionary>(PdfName.BE));
            set => Set(PdfName.BE, rpreferences = value);
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