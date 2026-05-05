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

using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Security;
using PdfGlown.Bytes;
using PdfGlown.Files;
using PdfGlown.Objects;
using PdfGlown.Tokens;
using PdfGlown.Util.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace PdfGlown.Documents.Encryption
{
    /// <summary>
    /// A security handler as described in the PDF specifications.
    /// A security handler is responsible of documents protection.
    /// @author Ben Litchfield
    /// @author Benoit Guillon
    /// @author Manuel Kasper
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class SecurityHandler<T> : ISecurityHandler where T : ProtectionPolicy
    {
        private static readonly int DEFAULT_VERSION = 1;

        private static readonly short DEFAULT_KEY_LENGTH = 40;

        // see 7.6.2, page 58, PDF 32000-1:2008
        private static readonly byte[] AES_SALT = { 0x73, 0x41, 0x6c, 0x54 };

        private T protectionPolicy = null;

        /// <summary>The Length in bits of the secret key used to encrypt the document.</summary>
        private short keyLength = DEFAULT_KEY_LENGTH;

        /// <summary>The encryption key that will used to encrypt / decrypt.</summary>
        private byte[] encryptionKey;

        /// <summary>The RC4 implementation used for cryptographic functions.</summary>
        private readonly ConcurrentBag<RC4Cipher> rc4Bag = new();

        /// <summary> indicates if the Metadata have to be decrypted of not.</summary>
        private bool decryptMetadata;

        /// <summary>Can be used to allow stateless AES encryption</summary>
        private SecureRandom customSecureRandom;

        // PDFBOX-4453, PDFBOX-4477: Originally this was just a Set. This failed in rare cases
        // when a decrypted string was identical to an encrypted string.
        // Because PdfString.equals() checks the contents, decryption was then skipped.
        // This solution keeps all different "equal" objects.
        // IdentityHashMap solves this problem and is also faster than a HashMap
        private readonly HashSet<PdfObject> objects = new();

        private bool useAES;

        /// <summary>
        /// The access permission granted to the current user for the document. These
        /// permissions are computed during decryption and are in read only mode.
        /// </summary>
        private AccessPermission currentAccessPermission = null;

        /// <summary> The stream filter name.</summary>
        private PdfName streamFilterName;

        /// <summary>The string filter name.</summary>
        private PdfName stringFilterName;

        protected SecurityHandler()
        { }

        protected SecurityHandler(T protectionPolicy)
        {
            this.protectionPolicy = protectionPolicy;
            keyLength = protectionPolicy.EncryptionKeyLength;
        }

        /// <summary>The whether to decrypt meta data.</summary>
        /// value> decryptMetadata true if meta data has to be decrypted.<//value>
        protected bool DecryptMetadata
        {
            get => this.decryptMetadata;
            set => this.decryptMetadata = value;
        }

        /// <summary>The string filter name.</summary>
        /// <value> stringFilterName the string filter name.</value>
        protected PdfName StringFilterName
        {
            get => stringFilterName;
            set => stringFilterName = value;
        }

        /// <summary>The stream filter name.</summary>
        /// <value>streamFilterName the stream filter name.</value> 
        protected PdfName StreamFilterName
        {
            get => streamFilterName;
            set => streamFilterName = value;
        }

        /// <summary>Set the custom SecureRandom.</summary>
        /// <value> secureRandom the custom SecureRandom for AES encryption</value>
        public SecureRandom CustomSecureRandom
        {
            get => this.customSecureRandom;
            set => customSecureRandom = value;
        }

        /// <summary>Prepare the document for encryption.</summary>
        /// <param name="doc">The document that will be encrypted.</param>
        public abstract void PrepareDocumentForEncryption(PdfDocument doc);

        /// <summary>Prepares everything to decrypt the document.</summary>
        /// <param name="encryption">encryption dictionary, can be retrieved via <see cref="PdfDocument.Encryption"/> </param>
        /// <param name="identifier">document id which is returned via <see cref="PdfDocument.ID"/></param>
        /// <param name="decryptionMaterial">Information used to decrypt the document.</param>
        public abstract void PrepareForDecryption(PdfEncryption encryption, Identifier identifier, DecryptionMaterial decryptionMaterial);

        /// <summary>Encrypt a set of data.</summary>
        /// <param name="objectNumber">The data object number.</param>
        /// <param name="genNumber">The data generation number.</param>
        /// <param name="data">The data to encrypt.</param>
        /// <param name="output">The output to write the encrypted data to.</param>
        /// <returns></returns>
        private bool EncryptData(long objectNumber, long genNumber, Stream data, Stream output)
        {
            // Determine whether we're using Algorithm 1 (for RC4 and AES-128), or 1.A (for AES-256)
            if (useAES && EncryptionKey.Length == 32)
            {
                return EncryptDataAES256(data, output);
            }
            else
            {
                byte[] readonlyKey = CalcFinalKey(objectNumber, genNumber);

                if (useAES)
                {
                    return EncryptDataAESother(readonlyKey, data, output);
                }
                else
                {
                    return EncryptDataRC4(readonlyKey, data, output);
                }
            }
            //output.Flush();
        }

        private bool DecryptData(long objectNumber, long genNumber, Stream data, Stream output)
        {
            // Determine whether we're using Algorithm 1 (for RC4 and AES-128), or 1.A (for AES-256)
            if (useAES && EncryptionKey.Length == 32)
            {
                return DecryptDataAES256(data, output);
            }
            else
            {
                var readonlyKey = CalcFinalKey(objectNumber, genNumber);

                if (useAES)
                {
                    return DecryptDataAESother(readonlyKey, data, output);
                }
                else
                {
                    return EncryptDataRC4(readonlyKey, data, output);
                }
            }
            //output.Flush();
        }

        /// <summary>Calculate the key to be used for RC4 and AES-128.</summary>
        /// <param name="objectNumber">The data object number.</param>
        /// <param name="genNumber">The data generation number.</param>
        /// <returns>the calculated key.</returns>
        private byte[] CalcFinalKey(long objectNumber, long genNumber)
        {
            var newKey = new byte[EncryptionKey.Length + 5];
            EncryptionKey.CopyTo(newKey.AsSpan());
            // PDF 1.4 reference pg 73
            // step 1
            // we have the reference
            // step 2
            newKey[newKey.Length - 5] = (byte)(objectNumber & 0xff);
            newKey[newKey.Length - 4] = (byte)(objectNumber >> 8 & 0xff);
            newKey[newKey.Length - 3] = (byte)(objectNumber >> 16 & 0xff);
            newKey[newKey.Length - 2] = (byte)(genNumber & 0xff);
            newKey[newKey.Length - 1] = (byte)(genNumber >> 8 & 0xff);
            // step 3
#if __BC_HASH__
            var md = new MD5Digest();
#else
            using var md = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
#endif
            md.Update(newKey);
            if (useAES)
            {
                md.Update(AES_SALT);
            }
            byte[] digestedKey = md.Digest();

            // step 4
            int minLength = Math.Min(newKey.Length, 16);

            return digestedKey.AsSpan(0, minLength).ToArray();
        }

        /// <summary>Encrypt or decrypt data with RC4.</summary>
        /// <param name="readonlyKey">The readonly key obtained with via {@link #calcFinalKey(long, long)}.</param>
        /// <param name="input">The data to encrypt.</param>
        /// <param name="output">The output to write the encrypted data to.</param>
        protected bool EncryptDataRC4(ReadOnlySpan<byte> readonlyKey, Stream input, Stream output)
        {
            if (!rc4Bag.TryTake(out var rc4))
                rc4 = new RC4Cipher();
            rc4.SetKey(readonlyKey);
            rc4.Write(input, output);
            rc4Bag.Add(rc4);
            return true;
        }

        /// <summary>Encrypt or decrypt data with RC4.</summary>
        /// <param name="readonlyKey">The readonly key obtained with via {@link #calcFinalKey(long, long)}.</param>
        /// <param name="input">The data to encrypt.</param>
        /// <param name="output">The output to write the encrypted data to.</param>
        protected bool EncryptDataRC4(ReadOnlySpan<byte> readonlyKey, ReadOnlySpan<byte> input, Stream output)
        {
            if (!rc4Bag.TryTake(out var rc4))
                rc4 = new RC4Cipher();
            rc4.SetKey(readonlyKey);
            rc4.Write(input, output);
            rc4Bag.Add(rc4);
            return true;
        }


        /// <summary>Encrypt or decrypt data with AES with key Length other than 256 bits.</summary>
        /// <param name="readonlyKey">The readonly key obtained with via {@link #calcFinalKey(long, long)}.</param>
        /// <param name="data">The data to encrypt.</param>
        /// <param name="output">The output to write the encrypted data to.</param>
        private bool EncryptDataAESother(byte[] readonlyKey, Stream data, Stream output)
        {
            byte[] iv = new byte[16];

            if (!PrepareAESEncryptIV(iv, output))
            {
                return false;
            }

            try
            {
                using var cipher = CreateCipher(readonlyKey, iv);
                using var writer = new CryptoStream(output, cipher.CreateEncryptor(), CryptoStreamMode.Write);
                data.CopyTo(writer);
                writer.FlushFinalBlock();
                return true;
            }
            catch (Exception exception)
            {
                if (exception is not CryptographicException)
                {
                    throw;
                }
                Debug.WriteLine("debug: A CryptographicException occurred when decrypting some stream data " + exception);
            }
            return false;
        }

        private static bool DecryptDataAESother(byte[] readonlyKey, Stream data, Stream output)
        {
            byte[] iv = new byte[16];

            if (!PrepareAESDecryptIV(iv, data))
            {
                return false;
            }

            try
            {
                using var cipher = CreateCipher(readonlyKey, iv);
                using var reader = new CryptoStream(data, cipher.CreateDecryptor(), CryptoStreamMode.Read);
                reader.CopyTo(output);
                return true;
            }
            catch (Exception exception)
            {
                if (exception is not CryptographicException)
                {
                    throw;
                }
                Debug.WriteLine("debug: A CryptographicException occurred when decrypting some stream data " + exception);
            }
            return false;
        }

        /// <summary>Encrypt or decrypt data with AES256.</summary>
        /// <param name="data">The data to encrypt.</param>
        /// <param name="output">The output to write the encrypted data to.</param>
        private bool EncryptDataAES256(Stream data, Stream output)
        {
            byte[] iv = new byte[16];

            if (!PrepareAESEncryptIV(iv, output))
            {
                return false;
            }

            try
            {
                using var cipher = CreateCipher(this.EncryptionKey, iv);
                using var writer = new CryptoStream(output, cipher.CreateEncryptor(), CryptoStreamMode.Write);
                data.CopyTo(writer);
                writer.FlushFinalBlock();
                return true;
            }
            catch (Exception exception)
            {
                // starting with java 8 the JVM wraps an IOException around a GeneralSecurityException
                // it should be safe to swallow a GeneralSecurityException
                if (exception is not CryptographicException)
                {
                    throw;
                }
                Debug.WriteLine("debug: A CryptographicException occurred when decrypting some stream data " + exception);
            }
            return false;
        }

        private bool DecryptDataAES256(Stream data, Stream output)
        {
            byte[] iv = new byte[16];

            if (!PrepareAESDecryptIV(iv, data))
            {
                return false;
            }

            try
            {
                using var cipher = CreateCipher(this.EncryptionKey, iv);
                using var reader = new CryptoStream(data, cipher.CreateDecryptor(), CryptoStreamMode.Read);
                reader.CopyTo(output);
                return true;
            }
            catch (Exception exception)
            {
                // starting with java 8 the JVM wraps an IOException around a GeneralSecurityException
                // it should be safe to swallow a GeneralSecurityException
                if (exception is not CryptographicException)
                {
                    throw;
                }
                Debug.WriteLine("debug: A CryptographicException occurred when decrypting some stream data " + exception);
            }
            return false;
        }

        private static SymmetricAlgorithm CreateCipher(byte[] key, byte[] iv)
        {
            //@SuppressWarnings({ "squid:S4432"}) // PKCS#5 padding is requested by PDF specification
            var cipher = Aes.Create();
            cipher.Mode = CipherMode.CBC;
            cipher.Padding = PaddingMode.PKCS7;
            cipher.Key = key;
            cipher.IV = iv;
            return cipher;
        }

        private bool PrepareAESEncryptIV(byte[] iv, Stream output)
        {
            // generate random IV and write to stream
            SecureRandom rnd = GetSecureRandom();
            rnd.NextBytes(iv);
            output.Write(iv);
            return true;
        }

        /// <summary>
        /// Returns a SecureRandom If customSecureRandom is not defined, instantiate a new SecureRandom
        /// </summary>
        /// <returns>SecureRandom</returns>
        private SecureRandom GetSecureRandom()
        {
            return customSecureRandom ?? new SecureRandom();
        }

        private static bool PrepareAESDecryptIV(byte[] iv, Stream data)
        {
            // read IV from stream
            int ivSize = data.Read(iv, 0, iv.Length);
            if (ivSize == 0)
            {
                return false;
            }
            if (ivSize != iv.Length)
            {
                throw new IOException(
                        "AES initialization vector not fully read: only "
                                + ivSize + " bytes read instead of " + iv.Length);
            }

            return true;
        }

        /// <summary>This will dispatch to the correct method.</summary>
        /// <param name="obj">The object to decrypt.</param>
        /// <param name="objNum">The object number.</param>
        /// <param name="genNum">The object generation Number.</param>
        public void Decrypt(PdfObject obj, long objNum, long genNum)
        {
            // PDFBOX-4477: only cache strings and streams, this improves speed and memory footprint
            if (obj is PdfString pdfString)
            {
                if (objects.Contains(obj))
                {
                    return;
                }
                objects.Add(obj);
                DecryptString(pdfString, objNum, genNum);
            }
            else if (obj is PdfStream stream)
            {
                if (objects.Contains(obj))
                {
                    return;
                }
                objects.Add(obj);
                DecryptStream(stream, objNum, genNum);
            }
            else if (obj is PdfDictionary dictionary)
            {
                DecryptDictionary(dictionary, objNum, genNum);
            }
            else if (obj is PdfArray array)
            {
                DecryptArray(array, objNum, genNum);
            }
        }

        /// <summary>This will decrypt a stream.</summary>
        /// <param name="stream">The stream to decrypt.</param>
        /// <param name="objNum">The object number.</param>
        /// <param name="genNum">The object generation number.</param>
        public void DecryptStream(PdfStream stream, long objNum, long genNum)
        {
            if (stream.encoded == EncodeState.Decoding)
            {
                return;
            }
            if (stream.encoded == EncodeState.Decoded)
            {
                return;
            }
            stream.encoded = EncodeState.Decoding;
            // Stream encrypted with identity filter
            if (PdfName.Identity.Equals(streamFilterName))
            {
                stream.encoded = EncodeState.Identity;
                return;
            }

            var type = stream.Get<PdfName>(PdfName.Type);
            if (!decryptMetadata && PdfName.Metadata.Equals(type))
            {
                stream.encoded = EncodeState.SkipMetadata;
                return;
            }
            // "The cross-reference stream shall not be encrypted"
            if (PdfName.XRef.Equals(type))
            {
                stream.encoded = EncodeState.SkipXRef;
                return;
            }
            if (PdfName.Metadata.Equals(type))
            {
                byte[] buf;
                // PDFBOX-3229 check case where metadata is not encrypted despite /EncryptMetadata missing
                var metadata = stream.GetInputStreamNoDecode();
                buf = new byte[10];
                long isResult = metadata.Read(buf, 0, 10);

                if (isResult.CompareTo(buf.Length) != 0)
                {
                    Debug.WriteLine($"debug: Tried reading {buf.Length} bytes but only {isResult} bytes read");
                }

                if (buf.AsSpan().SequenceEqual(Charset.ISO88591.GetBytes("<?xpacket ").AsSpan()))
                {
                    Debug.WriteLine("warn: Metadata is not encrypted, but was expected to be");
                    Debug.WriteLine("warn: Read PDF specification about EncryptMetadata (default value: true)");
                    return;
                }
            }

            DecryptDictionary(stream, objNum, genNum);
            var encryptedStream = (Stream)stream.GetInputStreamNoDecode();
            if (encryptedStream != null)
            {
                var output = new ByteStream();
                if (DecryptData(objNum, genNum, encryptedStream, output))
                {
                    stream.SetStream(output);
                    stream.encoded = EncodeState.Decoded;
                }
            }
        }

        /// <summary>This will encrypt a stream, but not the dictionary as the dictionary is
        /// encrypted by visitFromString() in PdfWriter and we don't want to encrypt
        /// it twice.</summary>
        /// <param name="stream">The stream to decrypt.</param>
        /// <param name="objNum">The object number.</param>
        /// <param name="genNum">The object generation number.</param>
        public void EncryptStream(PdfStream stream, long objNum, int genNum)
        {
            // empty streams don't need to be encrypted
            if (stream.GetInputStreamNoDecode() is not IInputStream body
                || body.Length == 0)
            {
                return;
            }
            var encryptedStream = (Stream)body;
            var output = new ByteStream();
            if (EncryptData(objNum, genNum, encryptedStream, output))
            {
                stream.SetStream(output);
            }
        }

        /// <summary>This will decrypt a dictionary.</summary>
        /// <param name="dictionary">The dictionary to decrypt.</param>
        /// <param name="objNum">The object number.</param>
        /// <param name="genNum">The object generation number.</param>
        private void DecryptDictionary(PdfDictionary dictionary, long objNum, long genNum)
        {
            if (dictionary.Get(PdfName.CF) != null)
            {
                // PDFBOX-2936: avoid orphan /CF dictionaries found in US govt "I-" files
                return;
            }
            var type = dictionary.Get<PdfName>(PdfName.Type);
            bool isSignature = PdfName.Sig.Equals(type) || PdfName.DocTimeStamp.Equals(type) ||
                    // PDFBOX-4466: /Type is optional, see
                    // https://ec.europa.eu/cefdigital/tracker/browse/DSS-1538
                    (dictionary.Get(PdfName.ByteRange) is PdfArray
                     && dictionary.Get(PdfName.Contents) is PdfString);
            foreach (var entry in dictionary)
            {
                if (isSignature && PdfName.Contents.Equals(entry.Key))
                {
                    // do not decrypt the signature contents string
                    continue;
                }
                var value = entry.Value;
                Decrypt(value, objNum, genNum);
            }
        }

        /// <summary>This will decrypt a string.</summary>
        /// <param name="pdfString">the string to decrypt.</param>
        /// <param name="objNum">The object number.</param>
        /// <param name="genNum">The object generation number.</param>
        private void DecryptString(PdfString pdfString, long objNum, long genNum)
        {
            // String encrypted with identity filter
            if (PdfName.Identity.Equals(stringFilterName))
            {
                return;
            }

            using var data = new ByteStream(pdfString.RawValue);
            using var outputStream = new MemoryStream();
            try
            {
                if (DecryptData(objNum, genNum, data, outputStream))
                {
                    pdfString.SetBuffer(outputStream.AsMemory());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"error: Failed to decrypt PdfString of Length {pdfString.RawValue.Length} in object {objNum}: {ex.Message}", ex);
            }
        }

        /// <summary>This will encrypt a string.</summary>
        /// <param name="pdfString">the string to encrypt.</param>
        /// <param name="objNum">The object number.</param>
        /// <param name="genNum">The object generation number.</param>
        public void EncryptString(PdfString pdfString, long objNum, int genNum)
        {
            using var data = new ByteStream(pdfString.RawValue);
            using var buffer = new MemoryStream();
            if (EncryptData(objNum, genNum, data, buffer))
            {
                pdfString.SetBuffer(buffer.AsMemory());
            }
        }

        /// <summary>This will decrypt an array.</summary>
        /// <param name="array">The array to decrypt.</param>
        /// <param name="objNum">The object number.</param>
        /// <param name="genNum">The object generation number.</param>
        private void DecryptArray(PdfArray array, long objNum, long genNum)
        {
            for (int i = 0; i < array.Count; i++)
            {
                Decrypt(array.Get(i), objNum, genNum);
            }
        }

        /// <summary>Getter of the property keyLength.</summary>
        public short KeyLength
        {
            get => keyLength;
            set => keyLength = value;
        }

        /// <summary>Returns the access permissions that were computed during document decryption.
        /// The returned object is in read only mode.
        /// the access permissions or null if the document was not decrypted.</summary>
        public AccessPermission CurrentAccessPermission
        {
            get => currentAccessPermission;
            set => currentAccessPermission = value;
        }

        /// <summary>True if AES is used for encryption and decryption.</summary>
        public bool IsAES
        {
            get => useAES;
            set => useAES = value;
        }

        public T ProtectionPolicy
        {
            get => protectionPolicy;
            internal set => protectionPolicy = value;
        }

        protected byte[] EncryptionKey
        {
            get => encryptionKey;
            set => encryptionKey = value;
        }

        /// <summary>Returns whether a protection policy has been set.</summary>
        /// <returns>true if a protection policy has been set.</returns>
        public bool HasProtectionPolicy() => protectionPolicy != null;

        /// <summary>
        /// Computes the version number of the StandardSecurityHandler based on the encryption key
        /// length. See PDF Spec 1.6 p 93 and
        /// <a href="https://www.adobe.com/content/dam/acom/en/devnet/pdf/adobe_supplement_iso32000.pdf">PDF
        /// 1.7 Supplement ExtensionLevel: 3</a> and
        /// <a href="http://intranet.pdfa.org/wp-content/uploads/2016/08/ISO_DIS_32000-2-DIS4.pdf">PDF
        /// Spec 2.0</a>.
        /// </summary>
        /// <returns>The computed version number.</returns>
        public int ComputeVersionNumber()
        {
            if (keyLength == 40)
            {
                return DEFAULT_VERSION;
            }
            else if (keyLength == 128 && ProtectionPolicy.IsPreferAES)
            {
                return 4;
            }
            else if (keyLength == 256)
            {
                return 5;
            }

            return 2;
        }
    }
}