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
    public class MediaDuration : PdfDictionary
    {
        public MediaDuration()
            : base(new Dictionary<PdfName, PdfDirectObject>(2) {
                { PdfName.Type, PdfName.MediaDuration }
            })
        { }

        public MediaDuration(double value)
            : this()
        {
            Value = value;
        }

        internal MediaDuration(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets/Sets the temporal duration.</summary>
        /// <returns>
        ///   <list type="bullet">
        ///     <item><code>Double.NEGATIVE_INFINITY</code>: intrinsic duration of the associated media;
        ///     </item>
        ///     <item><code>Double.POSITIVE_INFINITY</code>: infinite duration;</item>
        ///     <item>non-infinite positive: explicit duration.</item>
        ///   </list>
        /// </returns>
        public double Value
        {
            get
            {
                var durationSubtype = Get<PdfName>(PdfName.S);
                if (PdfName.I.Equals(durationSubtype))
                    return Double.NegativeInfinity;
                else if (PdfName.F.Equals(durationSubtype))
                    return Double.PositiveInfinity;
                else if (PdfName.T.Equals(durationSubtype))
                    return Get<Timespan>(PdfName.T).Time;
                else
                    throw new NotSupportedException("Duration subtype '" + durationSubtype + "'");
            }
            set
            {
                if (Double.IsNegativeInfinity(value))
                {
                    SetSimple(PdfName.S, PdfName.I);
                    Remove(PdfName.T);
                }
                else if (Double.IsPositiveInfinity(value))
                {
                    SetSimple(PdfName.S, PdfName.F);
                    Remove(PdfName.T);
                }
                else
                {
                    SetSimple(PdfName.S, PdfName.T);
                    GetOrCreate<Timespan>(PdfName.T).Time = value;
                }
            }
        }
    }
}