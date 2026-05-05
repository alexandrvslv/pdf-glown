/*
  Copyright 2009-2015 Stefano Chizzolini. http://www.pdfclown.org

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

namespace PdfGlown.Documents.Contents.Fonts
{
    /// <summary>Predefined encodings [PDF:1.6:5.5.5,D].</summary>
    // TODO: This hierarchy is going to be superseded by PdfGlown.Tokens.Encoding.
    public class Encoding
    {
        protected static readonly int CHAR_CODE = 0;
        protected static readonly int CHAR_NAME = 1;
        protected static readonly Dictionary<PdfName, Encoding> Encodings = new();

        public static Encoding Get(PdfName name)
        {
            if (!Encodings.TryGetValue(name, out var encoding))
            {
                if (name.Equals(PdfName.Identity))
                    encoding = IdentityEncoding.Instance;
                else if (name.Equals(PdfName.MacExpertEncoding))
                    encoding = MacExpertEncoding.Instance;
                else if (name.Equals(PdfName.MacRomanEncoding))
                    encoding = MacRomanEncoding.Instance;
                else if (name.Equals(PdfName.StandardEncoding))
                    encoding = StandardEncoding.Instance;
                else if (name.Equals(PdfName.Symbol))
                    encoding = SymbolEncoding.Instance;
                else if (name.Equals(PdfName.WinAnsiEncoding))
                    encoding = WinAnsiEncoding.Instance;
                else if (name.Equals(PdfName.ZapfDingbats))
                    encoding = ZapfDingbatsEncoding.Instance;
            }
            return encoding;
        }
        public Encoding()
        { }

        public Encoding(Dictionary<int, string> codeToName)
        {
            this.codeToName = codeToName;
        }

        private readonly Dictionary<int, string> codeToName = new(256);
        private readonly Dictionary<string, int> nameToCode = new(256, StringComparer.Ordinal);


        public Dictionary<int, string> CodeToNameMap
        {
            get => codeToName;
        }

        public Dictionary<string, int> NameToCodeMap
        {
            get => nameToCode;
        }

        public int? GetCode(string name)
        {
            return nameToCode.TryGetValue(name, out var code) ? code : null;
        }

        public virtual string GetName(int key)
        {
            return codeToName.TryGetValue(key, out var name) ? name : null;
        }

        protected internal void Put(int charCode, string charName)
        {
            codeToName[charCode] = charName;
            if (!nameToCode.ContainsKey(charName))
            {
                nameToCode[charName] = charCode;
            }
        }

        /// <summary>
        /// This will add a character encoding.An already existing mapping is overwritten when creating the reverse mapping.
        /// @see Encoding.Add(int, string)
        /// </summary>
        /// <param name="code">character code</param>
        /// <param name="name">PostScript glyph name</param>
        protected void Overwrite(int code, string name)
        {
            // remove existing reverse mapping first
            if (codeToName.TryGetValue(code, out string oldName))
            {
                if (nameToCode.TryGetValue(oldName, out int oldCode) && oldCode == code)
                {
                    nameToCode.Remove(oldName);
                }
            }
            nameToCode[name] = code;
            codeToName[code] = name;
        }

        /// <summary>Determines if the encoding has a mapping for the given name value.</summary>
        /// <param name="name">PostScript glyph name</param>
        public bool Contains(string name) => nameToCode.ContainsKey(name);

        public virtual PdfDirectObject GetPdfObject() => null;

        public virtual string EncodingName => null;
    }
}