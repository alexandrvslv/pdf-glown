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

using System;

namespace PdfGlown.Documents.Encryption
{

    /**
     * This class represents the protection policy to apply to a document.
     *
     * Objects implementing this abstract class can be passed to the protect method of PDDocument
     * to protect a document.
     *
     * @see org.apache.pdfbox.pdmodel.PDDocument#protect(ProtectionPolicy)
     *
     * @author Benoit Guillon (benoit.guillon@snv.jussieu.fr)
     */
    public abstract class ProtectionPolicy
    {

        private static readonly short DEFAULT_KEY_LENGTH = 40;

        private short encryptionKeyLength = DEFAULT_KEY_LENGTH;

        private bool preferAES = false;
        /**
        * Get the length of the secrete key that will be used to encrypt
        * document data.
        *
        * @return The length (in bits) of the encryption key.
        */
        /**
         * set the length in (bits) of the secret key that will be
         * used to encrypt document data.
         * The default value is 40 bits, which provides a low security level
         * but is compatible with old versions of Acrobat Reader.
         *
         * @param l the length in bits (must be 40, 128 or 256)
         */
        public short EncryptionKeyLength
        {
            get => encryptionKeyLength;
            set
            {
                if (value != 40 && value != 128 && value != 256)
                {
                    throw new ArgumentException("Invalid key length '" + value + "' value must be 40, 128 or 256!");
                }
                encryptionKeyLength = value;
            }
        }

        /**
        * Tell whether AES encryption is preferred when several encryption methods are available for
        * the chosen key length. The default is false. This setting is only relevant if the key length
        * is 128 bits.
        *
        * @return true if AES encryption is preferred
        */
        /**
         * Set whether AES encryption is preferred when several encryption methods are available for the
         * chosen key length. The default is false. This setting is only relevant if the key length is
         * 128 bits.
         *
         * @param preferAES
         */
        public bool IsPreferAES
        {
            get => preferAES;
            set => preferAES = value;
        }

    }
}