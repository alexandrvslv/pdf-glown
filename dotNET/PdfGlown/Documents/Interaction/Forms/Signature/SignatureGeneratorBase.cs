/*
 * Copyright 2015 The Apache Software Foundation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Utilities.Collections;
using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
//using System.Security.Cryptography.X509Certificates;

namespace PdfGlown.Documents.Interaction.Forms.Signature
{
    public abstract class CreateSignatureBase
    {
        private AsymmetricKeyParameter privateKey;
        private X509Certificate cert;
        private string tsaUrl;

        /// <summary>Initialize the signature creator with a keystore (pkcs12) and pin that should be used for the
        /// signature.</summary>
        /// <param name="cert"></param>
        /// <exception cref="IOException"></exception>
        public CreateSignatureBase(X509Certificate cert)
        {
            this.cert = cert;

            if (cert != null)
            {
                // avoid expired certificate
                cert.CheckValidity();

                SigUtils.CheckCertificateUsage(cert);
            }

            if (cert == null)
            {
                throw new IOException("Could not find certificate");
            }
        }

        public AsymmetricKeyParameter PrivateKey
        {
            get => privateKey;
            set => privateKey = value;
        }

        public X509Certificate Certificate
        {
            get => cert;
            set => cert = value;
        }

        public string TsaUrl
        {
            get => tsaUrl;
            set => tsaUrl = value;
        }

        /// <summary>
        /// SignatureInterface sample implementation.
        /// <p>This method will be called from inside of the pdfbox and create the PKCS #7 signature.
        /// The given InputStream contains the bytes that are given by the byte range./// </p>
        /// <p>This method is for internal use only.</p>
        /// <p>Use your favorite cryptographic library to implement PKCS #7 signature creation.
        /// If you want to create the hash and the signature separately (e.g. to transfer only the hash
        /// to an external application), read <a href="https://stackoverflow.com/questions/41767351">this
        /// answer</a> or <a href="https://stackoverflow.com/questions/56867465">this answer</a>.
        /// </p>
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        /// <exception cref="IOException"></exception>

        public async Task<byte[]> Sign(Stream content)
        {
            // cannot be done private (interface)
            try
            {
                var gen = new CmsSignedDataGenerator();
                gen.AddCertificates(CollectionUtilities.CreateStore(new List<X509Certificate>() { cert }));
                gen.AddSigner(privateKey, cert, CmsSignedDataGenerator.EncryptionRsa, CmsSignedDataGenerator.DigestSha256);
                var msg = new CmsProcessableInputStream(content);
                var signedData = gen.Generate(msg, false);
                if (!string.IsNullOrEmpty(tsaUrl))
                {
                    var validation = new ValidationTimeStamp(tsaUrl);
                    signedData = await validation.AddSignedTimeStamp(signedData);
                }
                return signedData.GetEncoded();
            }
            catch (Exception e)
            {
                throw new IOException("Sign Fail", e);
            }
        }


    }
}