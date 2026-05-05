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

using PdfGlown.Files;
using PdfGlown.Objects;

namespace PdfGlown.Documents.Encryption
{
    public interface ISecurityHandler
    {
        AccessPermission CurrentAccessPermission { get; set; }
        bool IsAES { get; set; }
        short KeyLength { get; set; }

        int ComputeVersionNumber();
        void Decrypt(PdfObject obj, long objNum, long genNum);
        void DecryptStream(PdfStream stream, long objNum, long genNum);
        void EncryptStream(PdfStream stream, long objNum, int genNum);
        void EncryptString(PdfString pdfString, long objNum, int genNum);
        bool HasProtectionPolicy();
        void PrepareDocumentForEncryption(PdfDocument doc);
        void PrepareForDecryption(PdfEncryption encryption, Identifier identifier, DecryptionMaterial decryptionMaterial);
    }
}