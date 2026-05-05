/*
 * https://github.com/apache/pdfbox
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
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents.Fonts
{
    /// <summary>
    /// Type 0 CIDFont(CFF).
    /// @author Ben Litchfield
    /// @author John Hewson
    /// </summary>
    public class PdfCIDFontType0 : PdfCIDFont
    {

        public PdfCIDFontType0(PdfDocument document, Dictionary<PdfName, PdfDirectObject> fontObject)
            : base(document, fontObject)
        { }

        /// <summary>Constructor.</summary>
        /// <param name="fontObject">The font dictionary according to the PDF specification.</param>
        internal PdfCIDFontType0(Dictionary<PdfName, PdfDirectObject> fontObject)
            : base(fontObject)
        { }
        
    }
}
