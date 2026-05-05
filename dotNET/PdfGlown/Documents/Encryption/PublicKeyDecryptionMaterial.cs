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

using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.X509;
using System;
using System.Linq;

namespace PdfGlown.Documents.Encryption
{

    /**
     * This class holds necessary information to decrypt a PDF document
     * protected by the public key security handler.
     *
     * To decrypt such a document, we need:
     * <ul>
     * <li>a valid X509 certificate which correspond to one of the recipient of the document</li>
     * <li>the private key corresponding to this certificate
     * <li>the password to decrypt the private key if necessary</li>
     * </ul>
     *
     * @author Benoit Guillon
     * 
     */

    public class PublicKeyDecryptionMaterial : DecryptionMaterial
    {
        private string password = null;
        private Pkcs12Store keyStore = null;
        private string alias = null;

        /**
		 * Create a new public key decryption material.
		 *
		 * @param keystore The keystore were the private key and the certificate are
		 * @param a The alias of the private key and the certificate.
		 *   If the keystore contains only 1 entry, this parameter can be left null.
		 * @param pwd The password to extract the private key from the keystore.
		 */

        public PublicKeyDecryptionMaterial(Pkcs12Store keystore, string a, string pwd)
        {
            keyStore = keystore;
            alias = a;
            password = pwd;

        }


        /**
		 * Returns the certificate contained in the keystore.
		 *
		 * @return The certificate that will be used to try to open the document.
		 *
		 * @throws KeyStoreException If there is an error accessing the certificate.
		 */

        public X509Certificate Certificate
        {
            get
            {
                if (keyStore.Count == 1)
                {
                    var aliases = keyStore.Aliases;
                    var keyStoreAlias = aliases.Cast<string>().FirstOrDefault();
                    return keyStore.GetCertificate(keyStoreAlias)?.Certificate;
                }
                else
                {
                    if (keyStore.ContainsAlias(alias))
                    {
                        return keyStore.GetCertificate(alias)?.Certificate;
                    }
                    throw new Exception("the keystore does not contain the given alias");
                }
            }
        }

        /**
		 * Returns the password given by the user and that will be used
		 * to open the private key.
		 *
		 * @return The password.
		 */
        public string Password
        {
            get => password;
        }

        /**
		 * returns The private key that will be used to open the document protection.
		 * @return The private key.
		 * @throws KeyStoreException If there is an error accessing the key.
		 */
        public AsymmetricKeyEntry PrivateKey
        {
            get
            {
                try
                {
                    if (keyStore.Count == 1)
                    {
                        var aliases = keyStore.Aliases;
                        string keyStoreAlias = aliases.Cast<string>().FirstOrDefault();
                        return keyStore.GetKey(keyStoreAlias);//TODO Check, password.toCharArray());
                    }
                    else
                    {
                        if (keyStore.ContainsAlias(alias))
                        {
                            return keyStore.GetKey(alias);//TODO Cehck password.toCharArray());
                        }
                        throw new Exception("the keystore does not contain the given alias");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("the private key is not recoverable", ex);
                    throw new Exception("the algorithm necessary to recover the key is not available", ex);
                }

            }
        }
    }
}
