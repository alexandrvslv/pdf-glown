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

using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Cms;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using PdfGlown.Bytes;
using PdfGlown.Files;
using PdfGlown.Objects;
using PdfGlown.Util.IO;
using System;
using System.IO;
using System.Security.Cryptography;
//using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace PdfGlown.Documents.Encryption
{
    /// <summary>
    /// This class implements the public key security handler described in the PDF specification.
    /// <see cref="PublicKeyProtectionPolicy"> to see how to protect document with this security handler.
    /// @author Benoit Guillon
    /// </summary>
    // Just Translated, TODO Check & Debug
    public sealed class PublicKeySecurityHandler : SecurityHandler<PublicKeyProtectionPolicy>
    {
        /** The filter name. */
        public const string FILTER = "Adobe.PubSec";

        private static readonly string SUBFILTER4 = "adbe.pkcs7.s4";
        private static readonly string SUBFILTER5 = "adbe.pkcs7.s5";
        private static readonly byte[] foreBytes = new byte[] { 0xff, 0xff, 0xff, 0xff };

        public PublicKeySecurityHandler()
        { }

        /// <summary>Constructor used for encryption.</summary>
        /// <param name="policy">The protection policy.</param>
        public PublicKeySecurityHandler(PublicKeyProtectionPolicy policy)
            : base(policy)
        {
            KeyLength = policy.EncryptionKeyLength;
        }

        /// <summary>Prepares everything to decrypt the document.</summary>
        /// <param name="encryption">encryption dictionary, can be retrieved via <see cref="PdfDocument.Encryption"> </param>
        /// <param name="identifier">document id which is returned via <see cref="PdfDocument.ID"> (not used by this handler)</param>
        /// <param name="decryptionMaterial">Information used to decrypt the document.</param>
        /// <exception cref="IOException">If there is an error accessing data. If verbose mode
        /// is enabled, the exception message will provide more details why the
        /// match wasn't successful.</exception>
        public override void PrepareForDecryption(PdfEncryption encryption, Identifier identifier, DecryptionMaterial decryptionMaterial)
        {
            if (!(decryptionMaterial is PublicKeyDecryptionMaterial material))
            {
                throw new IOException(
                        "Provided decryption material is not compatible with the document");
            }

            var defaultCryptFilterDictionary = encryption.DefaultCryptFilter;
            if (defaultCryptFilterDictionary != null && defaultCryptFilterDictionary.Length != 0)
            {
                KeyLength = (short)defaultCryptFilterDictionary.Length;
                DecryptMetadata = defaultCryptFilterDictionary.IsEncryptMetaData;
            }
            else
            {
                KeyLength = (short)(encryption.Length != 0
                    ? encryption.Length
                    : 40);
                DecryptMetadata = encryption.IsEncryptMetaData;
            }

            try
            {
                bool foundRecipient = false;
                //Org.BouncyCastle.X509.Extension.
                X509Certificate certificate = material.Certificate;
                X509CertificateEntry materialCert = null;
                if (certificate != null)
                {
                    materialCert = new X509CertificateEntry(certificate);
                }

                // the decrypted content of the enveloped data that match
                // the certificate in the decryption material provided
                byte[] envelopedData = null;

                // the bytes of each recipient in the recipients array
                var array = encryption.Get<PdfArray>(PdfName.Recipients)
                    ?? defaultCryptFilterDictionary.Recipients
                    ?? throw new IOException("/Recipients entry is missing in encryption dictionary");

                Memory<byte>[] recipientFieldsBytes = new Memory<byte>[array.Count];
                //TODO encryption.getRecipientsLength() and getRecipientStringAt() should be deprecated

                int recipientFieldsLength = 0;
                var extraInfo = new StringBuilder();
                for (int i = 0; i < array.Count; i++)
                {
                    var recipientFieldString = array.Get<PdfString>(i);
                    var recipientBytes = recipientFieldString.RawValue;
                    var stream = new ByteStream(recipientBytes);
                    var data = new CmsEnvelopedData(stream);
                    var recipCertificatesIt = data.GetRecipientInfos().GetRecipients();
                    int j = 0;
                    foreach (RecipientInformation ri in recipCertificatesIt)
                    {
                        // Impl: if a matching certificate was previously found it is an error,
                        // here we just don't care about it
                        RecipientID rid = ri.RecipientID;
                        if (!foundRecipient && rid.Match(certificate))
                        {
                            foundRecipient = true;
                            var privateKey = material.PrivateKey;
                            // might need to call setContentProvider() if we use PKI token, see
                            // http://bouncy-castle.1462172.n4.nabble.com/CMSException-exception-unwrapping-key-key-invalid-unknown-key-type-passed-to-RSA-td4658109.html
                            //DotNetUtilities.GetKeyPair(ri.AlgorithmIdentifier)
                            envelopedData = ri.GetContent(privateKey.Key);
                            break;
                        }
                        j++;
                        if (certificate != null)
                        {
                            extraInfo.Append('\n');
                            extraInfo.Append(j);
                            extraInfo.Append(": ");
                            if (ri is KeyTransRecipientInformation recipientInfo)
                            {
                                AppendCertInfo(extraInfo, recipientInfo, certificate, materialCert);
                            }
                        }

                    }
                    recipientFieldsBytes[i] = recipientBytes;
                    recipientFieldsLength += recipientBytes.Length;
                }
                if (!foundRecipient || envelopedData == null)
                {
                    throw new IOException("The certificate matches none of " + array.Count
                            + " recipient entries" + extraInfo.ToString());
                }
                if (envelopedData.Length != 24)
                {
                    throw new IOException("The enveloped data does not contain 24 bytes");
                }
                // now envelopedData contains:
                // - the 20 bytes seed
                // - the 4 bytes of permission for the current user

                byte[] accessBytes = new byte[4];
                Array.Copy(envelopedData, 20, accessBytes, 0, 4);

                var currentAccessPermission = new AccessPermission(accessBytes)
                {
                    IsReadOnly = true
                };
                CurrentAccessPermission = currentAccessPermission;

                // what we will put in the SHA1 = the seed + each byte contained in the recipients array
                byte[] sha1Input = new byte[recipientFieldsLength + 20];

                // put the seed in the sha1 input
                Array.Copy(envelopedData, 0, sha1Input, 0, 20);

                // put each bytes of the recipients array in the sha1 input
                int sha1InputOffset = 20;
                foreach (var recipientFieldsByte in recipientFieldsBytes)
                {
                    recipientFieldsByte.Span.CopyTo(sha1Input.AsSpan(sha1InputOffset, recipientFieldsByte.Length));
                    sha1InputOffset += recipientFieldsByte.Length;
                }

                byte[] mdResult;
                if (encryption.Version == 4 || encryption.Version == 5)
                {
                    if (!DecryptMetadata)
                    {
                        // "4 bytes with the value 0xFF if the key being generated is intended for use in
                        // document-level encryption and the document metadata is being left as plaintext"
                        Array.Resize(ref sha1Input, sha1Input.Length + 4);
                        foreBytes.CopyTo(sha1Input.AsSpan(sha1Input.Length - 4, 4));
                    }
                    if (encryption.Version == 4)
                    {
                        mdResult = SHA1.Create().Digest(sha1Input);
                    }
                    else
                    {
                        mdResult = SHA256.Create().Digest(sha1Input);
                    }
                    // detect whether AES encryption is used. This assumes that the encryption algo is 
                    // stored in the PDCryptFilterDictionary
                    // However, crypt filters are used only when V is 4 or 5.
                    if (defaultCryptFilterDictionary != null)
                    {
                        var cryptFilterMethod = defaultCryptFilterDictionary.CryptFilterMethod;
                        IsAES = PdfName.AESV2.Equals(cryptFilterMethod) || PdfName.AESV3.Equals(cryptFilterMethod);
                    }
                }
                else
                {
                    mdResult = SHA1.Create().Digest(sha1Input);
                }

                // we have the encryption key ...
                EncryptionKey = new byte[KeyLength / 8];
                Array.Copy(mdResult, 0, EncryptionKey, 0, KeyLength / 8);
            }
            catch (Exception e)
            {
                throw new IOException("", e);
            }
        }

        private void AppendCertInfo(StringBuilder extraInfo, KeyTransRecipientInformation ktRid, X509Certificate certificate, X509CertificateEntry materialCert)
        {

            BigInteger ridSerialNumber = null;// TODO ktRid.GetSerialNumber();
            if (ridSerialNumber != null)
            {
                string certSerial = "unknown";
                BigInteger certSerialNumber = certificate.SerialNumber;
                if (certSerialNumber != null)
                {
                    certSerial = certSerialNumber.ToString(16);
                }
                extraInfo.Append("serial-#: rid ");
                extraInfo.Append(ridSerialNumber.ToString(16));
                extraInfo.Append(" vs. cert ");
                extraInfo.Append(certSerial);
                extraInfo.Append(" issuer: rid \'");
                // TODO extraInfo.Append(ktRid.Issuer);
                extraInfo.Append("\' vs. cert \'");
                extraInfo.Append(materialCert == null ? "null" : certificate.IssuerDN.ToString());
                extraInfo.Append("\' ");
            }
        }

        /// <summary>Prepare the document for encryption.</summary>
        /// <param name="doc">The document that will be encrypted.</param>
        /// <exception cref="IOException">If there is an error while encrypting.</exception>
        public override void PrepareDocumentForEncryption(PdfDocument doc)
        {
            try
            {
                PdfEncryption dictionary = doc.Encryption;
                if (dictionary == null)
                {
                    dictionary = new PdfEncryption(doc);
                }

                dictionary.Filter = FILTER;
                dictionary.Length = KeyLength;
                int version = ComputeVersionNumber();
                dictionary.Version = version;

                // remove CF, StmF, and StrF entries that may be left from a previous encryption
                dictionary.RemoveV45filters();

                // create the 20 bytes seed
                byte[] seed = new byte[20];

                CipherKeyGenerator key;
                try
                {
                    key = GeneratorUtilities.GetKeyGenerator("AES");
                }
                catch (Exception e)
                {
                    // should never happen
                    throw new Exception("AES Key Generator", e);
                }

                key.Init(new KeyGenerationParameters(new SecureRandom(), 192));
                var sk = key.GenerateKey();

                // create the 20 bytes seed
                Array.Copy(sk, 0, seed, 0, 20);

                byte[][] recipientsFields = ComputeRecipientsField(seed);

                int shaInputLength = seed.Length;

                foreach (byte[] field in recipientsFields)
                {
                    shaInputLength += field.Length;
                }

                byte[] shaInput = new byte[shaInputLength];

                Array.Copy(seed, 0, shaInput, 0, 20);

                int shaInputOffset = 20;

                foreach (byte[] recipientsField in recipientsFields)
                {
                    Array.Copy(recipientsField, 0, shaInput, shaInputOffset, recipientsField.Length);
                    shaInputOffset += recipientsField.Length;
                }

                byte[] mdResult;
                switch (version)
                {
                    case 4:
                        dictionary.SubFilter = SUBFILTER5;
                        mdResult = SHA1.Create().Digest(shaInput);
                        PrepareEncryptionDictAES(dictionary, PdfName.AESV2, recipientsFields);
                        break;
                    case 5:
                        dictionary.SubFilter = SUBFILTER5;
                        mdResult = SHA256.Create().Digest(shaInput);
                        PrepareEncryptionDictAES(dictionary, PdfName.AESV3, recipientsFields);
                        break;
                    default:
                        dictionary.SubFilter = SUBFILTER4;
                        mdResult = SHA1.Create().Digest(shaInput);
                        dictionary.SetRecipients(recipientsFields);
                        break;
                }

                this.EncryptionKey = new byte[KeyLength / 8];
                Array.Copy(mdResult, 0, this.EncryptionKey, 0, KeyLength / 8);

                doc.Encryption = dictionary;
            }
            catch (Exception e)
            {
                throw new IOException("", e);
            }
        }

        private void PrepareEncryptionDictAES(PdfEncryption encryptionDictionary, PdfName aesVName, byte[][] recipients)
        {
            var cryptFilterDictionary = new PdfCryptFilter()
            {
                CryptFilterMethod = aesVName,
                Length = KeyLength,
                Recipients = new PdfArrayImpl(recipients)
            };
            //array.setDirect(true);
            encryptionDictionary.DefaultCryptFilter = cryptFilterDictionary;
            encryptionDictionary.StreamFilterName = PdfName.DefaultCryptFilter;
            encryptionDictionary.StringFilterName = PdfName.DefaultCryptFilter;
            //cryptFilterDictionary.getCOSObject().setDirect(true);
            IsAES = true;
        }

        private byte[][] ComputeRecipientsField(byte[] seed)
        {
            byte[][] recipientsField = new byte[ProtectionPolicy.NumberOfRecipients][];
            var it = ProtectionPolicy.RecipientsIterator;
            int i = 0;

            while (it.MoveNext())
            {
                PublicKeyRecipient recipient = it.Current;
                X509Certificate certificate = recipient.X509;
                int permission = recipient.Permission.PermissionBytesForPublicKey;

                byte[] pkcs7input = new byte[24];
                byte one = (byte)(permission);
                byte two = (byte)((uint)permission >> 8);
                byte three = (byte)((uint)permission >> 16);
                byte four = (byte)((uint)permission >> 24);

                // put this seed in the pkcs7 input
                Array.Copy(seed, 0, pkcs7input, 0, 20);

                pkcs7input[20] = four;
                pkcs7input[21] = three;
                pkcs7input[22] = two;
                pkcs7input[23] = one;

                var obj = CreateDERForRecipient(pkcs7input, certificate);
                var baos = new ByteStream();
                obj.EncodeTo(baos, "DER");

                recipientsField[i] = obj.GetDerEncoded();

                i++;
            }
            return recipientsField;
        }

        private Asn1Encodable CreateDERForRecipient(byte[] inp, X509Certificate cert)
        {
            string algorithm = PkcsObjectIdentifiers.RC2Cbc.Id;
            PbeParametersGenerator apg;
            CipherKeyGenerator keygen;
            IBufferedCipher cipher;
            try
            {
                apg = new Pkcs12ParametersGenerator(new Sha1Digest());//TODO Check
                keygen = GeneratorUtilities.GetKeyGenerator(algorithm);
                cipher = CipherUtilities.GetCipher(algorithm);
            }
            catch (Exception e)
            {
                // happens when using the command line app .jar file
                throw new IOException("Could not find a suitable javax.crypto provider for algorithm " +
                        algorithm + "; possible reason: using an unsigned .jar file", e);
            }

            //apg.Init(PbeParametersGenerator.Pkcs12PasswordToBytes(password.ToC), salt, iCount);

            var parameters = apg.GenerateDerivedParameters(algorithm, inp.Length * 8);

            Asn1Encodable obj = null;
            //TODO 
            //using (Asn1InputStream input = new Asn1InputStream(parameters.getEncoded("ASN.1")))
            //{
            //    obj = input.ReadObject();
            //}

            keygen.Init(new KeyGenerationParameters(new SecureRandom(), 128));
            var secretkey = keygen.GenerateKey();

            //TODO cipher.Init(true, secretkey, parameters);
            byte[] bytes = cipher.DoFinal(inp);

            var recipientInfo = ComputeRecipientInfo(cert, secretkey);
            var set = new DerSet(new RecipientInfo(recipientInfo));

            var algorithmId = new AlgorithmIdentifier(new DerObjectIdentifier(algorithm), obj);
            var encryptedInfo = new EncryptedContentInfo(PkcsObjectIdentifiers.Data, algorithmId, new DerOctetString(bytes));
            var enveloped = new EnvelopedData(null, set, encryptedInfo, (Asn1Set)null);

            var contentInfo = new Org.BouncyCastle.Asn1.Cms.ContentInfo(PkcsObjectIdentifiers.EnvelopedData, enveloped);
            return contentInfo.Content;
        }

        private static RsassaPssParameters CreatePssParams(
            AlgorithmIdentifier hashAlgId,
            int saltSize)
        {
            return new RsassaPssParameters(
                hashAlgId,
                new AlgorithmIdentifier(PkcsObjectIdentifiers.IdMgf1, hashAlgId),
                new DerInteger(saltSize),
                new DerInteger(1));
        }

        private KeyTransRecipientInfo ComputeRecipientInfo(X509Certificate x509certificate, byte[] abyte0)
        {
            TbsCertificateStructure certificate;
            using (Asn1InputStream input = new Asn1InputStream(x509certificate.GetTbsCertificate()))
            {
                certificate = TbsCertificateStructure.GetInstance(input.ReadObject());
            }

            AlgorithmIdentifier algorithmId = certificate.SubjectPublicKeyInfo.Algorithm;

            var serial = new Org.BouncyCastle.Asn1.Cms.IssuerAndSerialNumber(certificate.Issuer, certificate.SerialNumber.Value);
            IBufferedCipher cipher;
            try
            {
                cipher = CipherUtilities.GetCipher(algorithmId.Algorithm.Id);
            }
            catch (Exception e)
            {
                // should never happen, if this happens throw IOException instead
                throw new Exception("Could not find a suitable javax.crypto provider", e);
            }

            cipher.Init(true, x509certificate.GetPublicKey());

            DerOctetString octets = new DerOctetString(cipher.DoFinal(abyte0));
            RecipientIdentifier recipientId = new RecipientIdentifier(serial);
            return new KeyTransRecipientInfo(recipientId, algorithmId, octets);
        }

    }
}