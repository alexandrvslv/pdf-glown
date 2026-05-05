/*
  Copyright 2008-2015 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfGlown.Bytes;
using PdfGlown.Objects;
using System.Collections.Generic;

namespace PdfGlown.Documents.Interaction.Actions
{
    /// <summary>'Cause a script to be compiled and executed by the JavaScript interpreter'
    /// action [PDF:1.6:8.6.4].</summary>
    [Pdf(VersionEnum.PDF13)]
    public sealed class JavaScript : PdfAction
    {
        private string script;

        /// <summary>Gets the Javascript script from the specified base data object.</summary>
        internal static string GetScript(PdfDictionary baseDataObject, PdfName key)
        {
            var scriptObject = baseDataObject.Get<PdfDirectObject>(key);
            if (scriptObject is PdfTextString pdfString)
            {
                return pdfString.StringValue;
            }
            else if (scriptObject is PdfStream pdfSteam)
            {
                var scriptBuffer = pdfSteam.GetInputStream();
                return scriptBuffer.ReadString(0, (int)scriptBuffer.Length);
            }
            return null;
        }

        /// <summary>Sets the Javascript script into the specified base data object.</summary>
        internal static void SetScript(PdfDictionary baseDataObject, PdfName key, string value)
        {
            var scriptObject = baseDataObject.Get<PdfDirectObject>(key);
            if (scriptObject is not PdfStream && value.Length > 256)
            {
                baseDataObject.Set(key, baseDataObject.Document.Register(scriptObject = new PdfStream()));
            }
            // Insert the script!
            if (scriptObject is PdfStream stream)
            {
                var scriptBuffer = stream.GetOutputStream();
                scriptBuffer.SetLength(0);
                scriptBuffer.Write(value);
            }
            else
            {
                baseDataObject[key] = new PdfTextString(value);
            }
        }

        /// <summary>Creates a new action within the given document context.</summary>
        public JavaScript(PdfDocument context, string script)
            : base(context, PdfName.JavaScript)
        { Script = script; }

        internal JavaScript(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets/Sets the JavaScript script to be executed.</summary>
        public string Script
        {
            get => script ??= GetScript(this, PdfName.JS);
            set => SetScript(this, PdfName.JS, script = value);
        }

        public PdfString Name => RetrieveName();

        public PdfDirectObject NamedBaseObject => RetrieveNamedBaseObject();
    }
}