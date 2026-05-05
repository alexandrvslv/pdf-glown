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
using System.Diagnostics;
using System.IO;

namespace PdfGlown.Documents.Contents.Fonts
{
    /**
    * Creates the appropriate font subtype based on information in the dictionary.
    * @author Ben Litchfield
*/
    public sealed class PDFontFactory
    {
        private PDFontFactory()
        {
        }

        /**
         * Creates a new PDFont instance with the appropriate subclass.
         *
         * @param dictionary a font dictionary
         * @return a PDFont instance, based on the SubType entry of the dictionary
         * @throws IOException if something goes wrong
         */
        public static Font CreateFont(PdfDictionary dictionary)
        {
            return CreateFont(dictionary, null);
        }

        /**
         * Creates a new PDFont instance with the appropriate subclass.
         *
         * @param dictionary a font dictionary
         * @param resourceCache resource cache, only useful for type 3 fonts, can be null
         * @return a PDFont instance, based on the SubType entry of the dictionary
         * @throws IOException if something goes wrong
         */
        public static Font CreateFont(PdfDictionary dictionary, FontResources resourceCache)
        {
            var type = dictionary.GetName(PdfName.Type, PdfName.Font);
            if (!PdfName.Font.Equals(type))
            {
                Debug.WriteLine("error: Expected 'Font' dictionary but found '" + type.StringValue + "'");
            }

            PdfName subType = dictionary.GetName(PdfName.Subtype);
            if (PdfName.Type1.Equals(subType))
            {
                PdfDictionary fd = dictionary.GetDictionary(PdfName.FontDescriptor);
                if (fd != null && fd.ContainsKey(PdfName.FontFile3))
                {
                    return new FontType1C(dictionary);
                }
                return new FontType1(dictionary);
            }
            else if (PdfName.MMType1.Equals(subType))
            {
                PdfDictionary fd = dictionary.GetDictionary(PdfName.FontDescriptor);
                if (fd != null && fd.ContainsKey(PdfName.FontFile3))
                {
                    return new FontType1C(dictionary);
                }
                return new FontMMType1(dictionary);
            }
            else if (PdfName.TrueType.Equals(subType))
            {
                return new FontTrueType(dictionary);
            }
            else if (PdfName.Type3.Equals(subType))
            {
                return new FontType3(dictionary);
            }
            else if (PdfName.Type0.Equals(subType))
            {
                return new FontType0(dictionary);
            }
            else if (PdfName.CIDFontType0.Equals(subType))
            {
                throw new IOException("Type 0 descendant font not allowed");
            }
            else if (PdfName.CIDFontType2.Equals(subType))
            {
                throw new IOException("Type 2 descendant font not allowed");
            }
            else
            {
                // assuming Type 1 font (see PDFBOX-1988) because it seems that Adobe Reader does this
                // however, we may need more sophisticated logic perhaps looking at the FontFile
                Debug.WriteLine("warn: Invalid font subtype '" + subType + "'");
                return new FontType1(dictionary);
            }
        }

        /**
         * Creates a new PDCIDFont instance with the appropriate subclass.
         *
         * @param dictionary descendant font dictionary
         * @return a PDCIDFont instance, based on the SubType entry of the dictionary
         * @throws IOException if something goes wrong
         */
        public static FontCID CreateDescendantFont(PdfDictionary dictionary, FontType0 parent)
        {
            PdfName type = dictionary.GetName(PdfName.Type, PdfName.Font);
            if (!PdfName.Font.Equals(type))
            {
                throw new IOException("Expected 'Font' dictionary but found '" + type.StringValue + "'");
            }

            PdfName subType = dictionary.GetName(PdfName.Subtype);
            if (PdfName.CIDFontType0.Equals(subType))
            {
                return new FontCIDType0(dictionary, parent);
            }
            else if (PdfName.CIDFontType0.Equals(subType))
            {
                return new FontCIDType2(dictionary, parent);
            }
            else
            {
                throw new IOException("Invalid font type: " + type);
            }
        }
    }
}