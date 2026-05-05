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
    /// Represents a CIDSystemInfo.
    /// @author John Hewson
    /// </summary>
    public sealed class CIDSystemInfo : PdfDictionary
    {

        public CIDSystemInfo(PdfDocument context, string registry, string ordering, int supplement)
            : base(context, new(3)
                {
                    { PdfName.Registry, PdfString.Get(registry) },
                    { PdfName.Ordering, PdfString.Get(ordering) },
                    { PdfName.Supplement, PdfInteger.Get(supplement) }
                })
        { }

        internal CIDSystemInfo(Dictionary<PdfName, PdfDirectObject> dictionary) 
            : base(dictionary)
        { }

        public string Registry
        {
            get => GetString(PdfName.Registry);
            set => Set(PdfName.Registry, value);
        }

        public string Ordering
        {
            get => GetString(PdfName.Ordering);
            set => Set(PdfName.Ordering, value);
        }

        public int Supplement
        {
            get => GetInt(PdfName.Supplement, 0);
            set => Set(PdfName.Supplement, value);
        }

        public override string ToString()
        {
            return $"{Registry}-{Ordering}-{Supplement}";
        }
    }
}
