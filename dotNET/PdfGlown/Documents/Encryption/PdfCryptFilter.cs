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
using System.Collections.Generic;

namespace PdfGlown.Documents.Encryption
{
    /// <summary>
    /// This class is a specialized view of the crypt filter dictionary of a PDF document.
    /// It contains a low level dictionary (PdfDictionary) and provides the methods to
    /// manage its fields.
    /// </summary>
    public class PdfCryptFilter : PdfDictionary
    {
        /// <summary>creates a new empty crypt filter dictionary.</summary>
        /// <param name="context"></param>
        public PdfCryptFilter()
            : this(new() { { PdfName.Type, PdfName.CryptFilter } }) 
        { }

        /// <summary>
        /// creates a new crypt filter dictionary from the low level dictionary provided.
        /// </summary>
        /// <param name="baseObject">the low level dictionary that will be managed by the newly created object</param>
        internal PdfCryptFilter(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>
        /// This will return the Length entry of the crypt filter dictionary.</br>
        /// The length in <b>bits</b> for the crypt filter algorithm. This will return a multiple of 8.
        /// The length in bits for the encryption algorithm
        /// </summary>
        public int Length
        {
            get => GetInt(PdfName.Length, 0);
            set => Set(PdfName.Length, value);
        }

        /// <summary>
        /// This will return the crypt filter method.
        /// Allowed values are: NONE, V2, AESV2, AESV3
        /// </summary>
        public PdfName CryptFilterMethod
        {
            get => Get<PdfName>(PdfName.CFM);
            set => this[PdfName.CFM] = value;
        }

        /// <summary>
        /// Will get the EncryptMetaData dictionary info.
        /// true if EncryptMetaData is explicitly set (the default is true)
        /// </summary>
        public bool IsEncryptMetaData
        {
            get => GetBool(PdfName.EncryptMetadata, true);
            set => Set(PdfName.EncryptMetadata, value);
        }

        public PdfArray Recipients
        {
            get => Get<PdfArray>(PdfName.Recipients);
            set => SetDirect(PdfName.Recipients, value);
        }
    }
}
