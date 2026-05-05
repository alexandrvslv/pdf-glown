/*
/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using PdfGlown.Objects;
using PdfGlown.Util.Collections;
using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents.Fonts
{
    /// <summary>This will perform the encoding from a dictionary.
    /// @author Ben Litchfield</summary>
    internal class DictionaryEncoding : Encoding
    {
        private readonly PdfDictionary encoding;
        private readonly Encoding baseEncoding;
        private readonly Dictionary<int, string> differences = new();

		 /// <summary>Creates a new DictionaryEncoding for embedding.</summary>
         /// <param name="baseEncoding"></param>
         /// <param name="differences"></param>
        public DictionaryEncoding(PdfName baseEncoding, PdfArray differences)
        {
            encoding = new PdfDictionary
            {
                [PdfName.Name] = PdfName.Encoding,
                [PdfName.Differences] = differences
            };
            if (baseEncoding != PdfName.StandardEncoding)
            {
                encoding[PdfName.BaseEncoding] = baseEncoding;
                this.baseEncoding = Encoding.Get(baseEncoding);
            }
            else
            {
                this.baseEncoding = Encoding.Get(baseEncoding);
            }

            if (this.baseEncoding == null)
            {
                throw new ArgumentException("Invalid encoding: " + baseEncoding);
            }

            CodeToNameMap.AddRange(this.baseEncoding.CodeToNameMap);
            NameToCodeMap.AddRange(this.baseEncoding.NameToCodeMap);
            ApplyDifferences();
        }

        /// <summary>Creates a new DictionaryEncoding for a Type 3 font from a PDF.</summary>
        /// <param name="fontEncoding">The Type 3 encoding dictionary.</param>
        public DictionaryEncoding(PdfDictionary fontEncoding)
        {
            encoding = fontEncoding;
            baseEncoding = null;
            ApplyDifferences();
        }

        /// <summary>Creates a new DictionaryEncoding from a PDF.</summary>
        /// <param name="fontEncoding">The encoding dictionary.</param>
        /// <param name="isNonSymbolic">True if the font is non-symbolic. False for Type 3 fonts.</param>
        /// <param name="builtIn"></param>
        /// <exception cref="ArgumentException"></exception>
        //* @param fontEncoding 
        //* @param isNonSymbolic 
        //* @param builtIn The font's built-in encoding. Null for Type 3 fonts.
        public DictionaryEncoding(PdfDictionary fontEncoding, bool isNonSymbolic, Encoding builtIn)
        {
            encoding = fontEncoding;

            Encoding baseEncoding = null;
            var hasBaseEncoding = encoding.Get(PdfName.BaseEncoding);
            if (hasBaseEncoding is PdfName name)
            {
                baseEncoding = Encoding.Get(name); // null when the name is invalid
            }

            if (baseEncoding == null)
            {
                if (isNonSymbolic)
                {
                    // Otherwise, for a nonsymbolic font, it is StandardEncoding
                    baseEncoding = StandardEncoding.Instance;
                }
                else
                {
                    // and for a symbolic font, it is the font's built-in encoding.
                    if (builtIn != null)
                    {
                        baseEncoding = builtIn;
                    }
                    else
                    {
                        // triggering this error indicates a bug in PDFBox. Every font should always have
                        // a built-in encoding, if not, we parsed it incorrectly.
                        throw new ArgumentException("Symbolic fonts must have a built-in " + "encoding");
                    }
                }
            }
            this.baseEncoding = baseEncoding;

            CodeToNameMap.AddRange(baseEncoding.CodeToNameMap);
            NameToCodeMap.AddRange(baseEncoding.NameToCodeMap);
            ApplyDifferences();
        }

        private void ApplyDifferences()
        {
            // now replace with the differences
            var diffArray = encoding.Get<PdfArray>(PdfName.Differences);
            if (diffArray == null)
            {
                return;
            }
            int currentIndex = -1;
            for (int i = 0; i < diffArray.Count; i++)
            {
                var next = diffArray.Get(i);
                if (next is IPdfNumber number)
                {
                    currentIndex = number.IntValue;
                }
                else if (next is IPdfString name)
                {
                    Overwrite(currentIndex, name.StringValue);
                    this.differences[currentIndex] = name.StringValue;
                    currentIndex++;
                }
            }
        }

        /// <summary>Returns the base encoding. Will be null for Type 3 fonts.</summary>
        public Encoding BaseEncoding
        {
            get => baseEncoding;
        }

        /// <summary>Returns the Differences array.</summary>
        public Dictionary<int, string> Differences
        {
            get => differences;
        }

        public override PdfDirectObject GetPdfObject()
        {
            return encoding;
        }

        public override string EncodingName
        {
            get
            {
                if (baseEncoding == null)
                {
                    // In type 3 the /Differences array shall specify the complete character encoding
                    return "differences";
                }
                return baseEncoding.EncodingName + " with differences";
            }
        }

    }
}
