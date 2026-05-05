/*
  Copyright 2010-2012 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfGlown.Util.Metadata;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PdfGlown
{
    /// <summary>Generic PDF version number [PDF:1.6:H.1].</summary>
    /// <seealso cref="VersionEnum"/>
    public sealed partial class PdfVersion : IVersion
    {
        [GeneratedRegex("^(\\d+)\\.(\\d+)$", RegexOptions.CultureInvariant)]
        private static partial Regex GetVersionPattern();

        private static readonly Dictionary<string, PdfVersion> versions = new(8, StringComparer.Ordinal);
        private static readonly Dictionary<VersionEnum, PdfVersion> versionEnums = new(8)
        {
            { VersionEnum.PDF10, Get("1.0") },
            { VersionEnum.PDF11, Get("1.1") },
            { VersionEnum.PDF12, Get("1.2") },
            { VersionEnum.PDF13, Get("1.3") },
            { VersionEnum.PDF14, Get("1.4") },
            { VersionEnum.PDF15, Get("1.5") },
            { VersionEnum.PDF16, Get("1.6") },
            { VersionEnum.PDF17, Get("1.7") },
        };

        public static PdfVersion Get(VersionEnum version) => versionEnums[version];

        public static PdfVersion Get(PdfName version) => Get(version.RawValue);

        public static PdfVersion Get(string version)
        {
            var versionPattern = GetVersionPattern();
            if (!versions.ContainsKey(version))
            {
                Match versionMatch = versionPattern.Match(version);
                if (!versionMatch.Success)
                    throw new Exception("Invalid PDF version format: '" + versionPattern + "' pattern expected.");

                var versionObject = new PdfVersion(Int32.Parse(versionMatch.Groups[1].Value), Int32.Parse(versionMatch.Groups[2].Value));
                versions[version] = versionObject;
            }
            return versions[version];
        }

        private readonly int major;
        private readonly int minor;

        internal PdfVersion(int major, int minor)
        {
            this.major = major;
            this.minor = minor;
        }

        public int Major => major;

        public int Minor => minor;

        public override string ToString() => VersionUtils.ToString(this);

        public IList<int> Numbers => new List<int> { major, minor };

        public int CompareTo(IVersion value) => VersionUtils.CompareTo(this, value);

        public float GetFloat() => float.Parse($"{Major}.{Minor}", CultureInfo.InvariantCulture);
    }
}