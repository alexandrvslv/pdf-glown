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
using PdfGlown.Util.Math;
using PdfGlown.Util.Metadata;

using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfGlown.Documents.Multimedia
{
    /// <summary>Software identifier [PDF:1.7:9.1.6].</summary>
    [Pdf(VersionEnum.PDF15)]
    public sealed class SoftwareIdentifier : PdfDictionary
    {
        private IList<string> oSes;
        private Uri url;
        private Interval<VersionObject> version;

        /// <summary>Software version number [PDF:1.7:9.1.6].</summary>
        public sealed class VersionObject : PdfObjectWrapper<PdfArray>, IVersion
        {
            private int[] numbers;

            public VersionObject(params int[] numbers) : base(new PdfArrayImpl(numbers))
            { }

            public VersionObject(PdfDirectObject baseObject) : base(baseObject)
            { }

            public int CompareTo(IVersion value)
            { return VersionUtils.CompareTo(this, value); }

            public IList<int> Numbers
            {
                get => numbers ??= DataObject.ToIntArray();
                set
                {
                    DataObject.Clear();
                    DataObject.AddRangeDirect(value.Select(x => PdfInteger.Get(x)));
                }
            }

            public override string ToString()
            { return VersionUtils.ToString(this); }
        }

        public SoftwareIdentifier(PdfDocument context)
            : base(context, new(){
                { PdfName.Type, PdfName.SoftwareIdentifier}
            })
        { }

        internal SoftwareIdentifier(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets the operating system identifiers that indicate which operating systems this
        /// object applies to.</summary>
        /// <remarks>The defined values are the same as those defined for SMIL 2.0's systemOperatingSystem
        /// attribute.An empty list is considered to represent all operating systems.</remarks>
        public IList<string> OSes
        {
            get => oSes ??= Get<PdfArray>(PdfName.OS)?.ToStringArray();
            set => this[PdfName.OS] = new PdfArrayImpl(oSes = value);
        }

        /// <summary>Gets the URI that identifies a piece of software.</summary>
        /// <remarks>It is interpreted according to its scheme; the only presently defined scheme is
        /// vnd.adobe.swname.The scheme name is case-insensitive; if is not recognized by the viewer
        /// application, the software must be considered a non-match.The syntax of URIs of this scheme is
        /// "vnd.adobe.swname:" software_name where software_name is equivalent to reg_name as defined in
        /// Internet RFC 2396, Uniform Resource Identifiers (URI): Generic Syntax.</remarks>
        public Uri URI
        {
            get => url ??= GetString(PdfName.U) is string stringValue ? new Uri(stringValue) : null;
            set => Set(PdfName.U, value?.ToString());
        }

        /// <summary>Gets the software version bounds.</summary>
        public Interval<VersionObject> Version
        {
            get => version ??= new Interval<VersionObject>(
                  new VersionObject(Get(PdfName.L)),
                  new VersionObject(Get(PdfName.H)),
                  GetBool(PdfName.LI, true),
                  GetBool(PdfName.HI, true));
            set
            {
                version = value;
                Set(PdfName.L, value.Low.DataObject);
                Set(PdfName.H, value.High.DataObject);
                Set(PdfName.LI, value.LowInclusive);
                Set(PdfName.HI, value.HighInclusive);
            }
        }
    }
}