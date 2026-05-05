/*
 * Copyright 2017 The Apache Software Foundation.
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




using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Tsp;
using Org.BouncyCastle.Utilities.Collections;
using Org.BouncyCastle.X509;
using PdfGlown.Bytes;
using PdfGlown.Documents.Interaction.Forms.Signature.Sertificate;
using PdfGlown.Objects;
using PdfGlown.Tokens;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace PdfGlown.Documents.Interaction.Forms.Signature
{
    /// <summary>
    /// Utility class for the signature / timestamp examples.
    /// <br/>  * @author Tilman Hausherr
    /// </summary>
    public static class SigUtils
    {
        public static readonly DerObjectIdentifier PurposePdfSigning = new DerObjectIdentifier("1.2.840.113583.1.1.5");
        public static readonly DerObjectIdentifier MSDocumentSigning = new DerObjectIdentifier("1.3.6.1.4.1.311.10.3.12");
        static SigUtils()
        {
        }

        /// <summary>
        /// Get the access permissions granted for this document in the DocMDP transform parameters
        /// dictionary. Details are described in the table "Entries in the DocMDP transform parameters
        /// dictionary" in the PDF specification.
        /// </summary>
        /// <param name="doc">document</param>
        /// <returns>
        /// the permission value. 0 means no DocMDP transform parameters dictionary exists. Other
        /// return values are 1, 2 or 3. 2 is also returned if the DocMDP transform parameters dictionary
        /// is found but did not contain a /P entry, or if the value is outside the valid range.
        /// </returns>
        public static int GetMDPPermission(PdfCatalog doc)
        {
            var permsDict = doc.Get<PdfDictionary>(PdfName.Perms);
            if (permsDict != null
                && permsDict.Get<PdfDictionary>(PdfName.DocMDP) is PdfDictionary signatureDict
                && signatureDict.Get<PdfArray>(PdfName.Reference) is PdfArray refArray)
            {
                for (int i = 0; i < refArray.Count; ++i)
                {
                    if (refArray.Get<PdfDictionary>(i) is PdfDictionary sigRefDict)
                    {
                        if (PdfName.DocMDP.Equals(sigRefDict.Get<PdfName>(PdfName.TransformMethod)))
                        {
                            if (sigRefDict.Get<PdfDictionary>(PdfName.TransformParams) is PdfDictionary transformDict)
                            {
                                int accessPermissions = transformDict.GetInt(PdfName.P, 2);
                                if (accessPermissions < 1 || accessPermissions > 3)
                                {
                                    accessPermissions = 2;
                                }
                                return accessPermissions;
                            }
                        }
                    }
                }
            }
            return 0;
        }

        /// <summary>
        /// Set the "modification detection and prevention" permissions granted for this document in the
        /// DocMDP transform parameters dictionary. Details are described in the table "Entries in the
        /// DocMDP transform parameters dictionary" in the PDF specification.
        /// </summary>
        /// <param name="doc">The document</param>
        /// <param name="signature">The signature object</param>
        /// <param name="accessPermissions">The permission value (1, 2 or 3)</param>
        /// <exception cref="Exception"></exception>
        public static void SetMDPPermission(PdfCatalog doc, SignatureDictionary signature, int accessPermissions)
        {
            foreach (var sig in doc.GetSignatureDictionaries())
            {
                // "Approval signatures shall follow the certification signature if one is present"
                // thus we don't care about timestamp signatures
                if (PdfName.DocTimeStamp.Equals(sig.Get<PdfName>(PdfName.Type)))
                {
                    continue;
                }
                if (sig.ContainsKey(PdfName.Contents))
                {
                    throw new Exception("DocMDP transform method not allowed if an approval signature exists");
                }
            }

            // DocMDP specific stuff
            // all values in the signature dictionary shall be direct objects
            var transformParameters = new PdfDictionary();
            transformParameters[PdfName.Type] = PdfName.TransformParams;
            transformParameters.Set(PdfName.P, accessPermissions);
            transformParameters.SetName(PdfName.V, "1.2");

            var referenceDict = new PdfDictionary
            {
                [PdfName.Type] = PdfName.SigRef,
                [PdfName.TransformMethod] = PdfName.DocMDP,
                [PdfName.DigestMethod] = PdfName.Get("SHA1"),
                [PdfName.TransformParams] = transformParameters
            };

            var referenceArray = new PdfArrayImpl { referenceDict };
            signature[PdfName.Reference] = referenceArray;

            // Catalog
            var permsDict = new PdfDictionary
            {
                [PdfName.DocMDP] = signature
            };
            doc[PdfName.Perms] = permsDict;
        }

        /// <summary>
        /// Log if the certificate is not valid for signature usage. Doing this
        /// anyway results in Adobe Reader failing to validate the PDF.
        /// </summary>
        /// <param name="x509Certificate"></param>
        public static void CheckCertificateUsage(X509Certificate x509Certificate)
        {
            // Check whether signer certificate is "valid for usage"
            // https://stackoverflow.com/a/52765021/535646
            // https://www.adobe.com/devnet-docs/acrobatetk/tools/DigSig/changes.html#id1
            bool[] keyUsage = x509Certificate.GetKeyUsage();
            if (keyUsage != null && !keyUsage[0] && !keyUsage[1])
            {
                // (unclear what "signTransaction" is)
                // https://tools.ietf.org/html/rfc5280#section-4.2.1.3
                Debug.WriteLine("Certificate key usage does not include digitalSignature nor nonRepudiation");
            }
            var extendedKeyUsage = x509Certificate.GetExtendedKeyUsage();
            if (extendedKeyUsage != null &&
                !extendedKeyUsage.Contains(KeyPurposeID.id_kp_emailProtection) &&
                !extendedKeyUsage.Contains(KeyPurposeID.id_kp_codeSigning) &&
                !extendedKeyUsage.Contains(KeyPurposeID.AnyExtendedKeyUsage) &&
                !extendedKeyUsage.Contains(PurposePdfSigning) &&
                // not mentioned in Adobe document, but tolerated in practice
                !extendedKeyUsage.Contains(MSDocumentSigning))
            {
                Debug.WriteLine("Certificate extended key usage does not include " +
                        "emailProtection, nor codeSigning, nor anyExtendedKeyUsage, " +
                        "nor 'Adobe Authentic Documents Trust'");
            }
        }

        /// <summary>
        /// Log if the certificate is not valid for timestamping.
        /// </summary>
        /// <param name="x509Certificate"></param>
        public static void CheckTimeStampCertificateUsage(X509Certificate x509Certificate)
        {
            var extendedKeyUsage = x509Certificate.GetExtendedKeyUsage();
            // https://tools.ietf.org/html/rfc5280#section-4.2.1.12
            if (extendedKeyUsage != null &&
                !extendedKeyUsage.Contains(KeyPurposeID.id_kp_timeStamping))
            {
                Debug.WriteLine("Certificate extended key usage does not include timeStamping");
            }
        }

        /// <summary>
        /// Log if the certificate is not valid for responding.
        /// </summary>
        /// <param name="x509Certificate"></param>
        public static void CheckResponderCertificateUsage(X509Certificate x509Certificate)
        {
            var extendedKeyUsage = x509Certificate.GetExtendedKeyUsage();
            // https://tools.ietf.org/html/rfc5280#section-4.2.1.12
            if (extendedKeyUsage != null &&
                !extendedKeyUsage.Contains(KeyPurposeID.id_kp_OCSPSigning))
            {
                Debug.WriteLine("Certificate extended key usage does not include OCSP responding");
            }
        }

        /// <summary>
        /// Gets the last relevant signature in the document, i.e. the one with the highest offset.
        /// </summary>
        /// <param name="document">document to get its last signature</param>
        /// <returns>last signature or null when none found</returns>
        public static SignatureDictionary GetLastRelevantSignature(PdfCatalog document)
        {
            // we can't use getLastSignatureDictionary() because this will fail (see PDFBOX-3978) 
            // if a signature is assigned to a pre-defined empty signature field that isn't the last.
            // we get the last in time by looking at the offset in the PDF file.
            var lastSignature = document.GetSignatureDictionaries().
                    OrderByDescending(x => x.ByteRange[1]).
                    FirstOrDefault();
            if (lastSignature != null)
            {
                var type = lastSignature.Type;
                if (type == null || PdfName.Sig.Equals(type) || PdfName.DocTimeStamp.Equals(type))
                {
                    return lastSignature;
                }
            }
            return null;
        }

        public static TimeStampToken ExtractTimeStampTokenFromSignerInformation(SignerInformation signerInformation)
        {
            if (signerInformation.UnsignedAttributes is not Org.BouncyCastle.Asn1.Cms.AttributeTable unsignedAttributes)
            {
                return null;
            }
            // https://stackoverflow.com/questions/1647759/how-to-validate-if-a-signed-jar-contains-a-timestamp
            var attribute = unsignedAttributes[PkcsObjectIdentifiers.IdAASignatureTimeStampToken];
            if (attribute == null)
            {
                return null;
            }
            var obj = (Asn1Object)attribute.AttrValues[0];
            var signedTSTData = new CmsSignedData(obj.GetEncoded());
            return new TimeStampToken(signedTSTData);
        }

        public static void ValidateTimestampToken(TimeStampToken timeStampToken)
        {
            // https://stackoverflow.com/questions/42114742/
            var tstMatches = timeStampToken.GetCertificates().EnumerateMatches(timeStampToken.SignerID);
            var certificate = tstMatches.Cast<X509Certificate>().FirstOrDefault();
            //SignerInformationVerifier siv = new CmsSignerInfoVerifierBuilder().setProvider(SecurityProvider.getProvider()).build(certificate);
            timeStampToken.Validate(certificate);
        }

        /// <summary>
        /// Verify the certificate chain up to the root, including OCSP or CRL. However this does not
        /// test whether the root certificate is in a trusted list.<br/>
        /// Please post bad PDF files that succeed and good PDF files that fail in
        /// <a href="https://issues.apache.org/jira/browse/PDFBOX-3017">PDFBOX-3017</a>.
        /// </summary>
        /// <param name="certificatesStore"></param>
        /// <param name="certFromSignedData"></param>
        /// <param name="signDate"></param>
        /// <returns></returns>
        public static async Task VerifyCertificateChain(IStore<X509Certificate> certificatesStore, X509Certificate certFromSignedData, DateTime signDate)
        {
            var certificates = certificatesStore.EnumerateMatches(null);
            HashSet<X509Certificate> additionalCerts = new();
            foreach (X509Certificate certificate in certificates)
            {
                if (!certificate.Equals(certFromSignedData))
                {
                    additionalCerts.Add(certificate);
                }
            }
            await CertificateVerifier.VerifyCertificate(certFromSignedData, additionalCerts, true, signDate);
            //TODO check whether the root certificate is in our trusted list.
            // For the EU, get a list here:
            // https://ec.europa.eu/digital-single-market/en/eu-trusted-lists-trust-service-providers
            // ( getRootCertificates() is not helpful because these are SSL certificates)
        }

        /// <summary>
        /// Get certificate of a TSA.
        /// </summary>
        /// <param name="tsaUrl">URL</param>
        /// <returns>the X.509 certificate</returns>
        public static async Task<X509Certificate> GetTsaCertificate(string tsaUrl)
        {
            var digest = new Sha256Digest();//"SHA-256"
            var tsaClient = new TSAClient(new Uri(tsaUrl), null, null, digest);
            var emptyStream = new ByteStream(new byte[0]);
            TimeStampToken timeStampToken = await tsaClient.GetTimeStampToken(emptyStream);
            return GetCertificateFromTimeStampToken(timeStampToken);
        }

        /// <summary>
        /// Extract X.509 certificate from a timestamp
        /// </summary>
        /// <param name="timeStampToken"></param>
        /// <returns>the X.509 certificate</returns>
        public static X509Certificate GetCertificateFromTimeStampToken(TimeStampToken timeStampToken)
        {
            var tstMatches = timeStampToken.GetCertificates().EnumerateMatches(timeStampToken.SignerID);
            return tstMatches.Cast<X509Certificate>().FirstOrDefault();
        }

        /// <summary>
        /// Look for gaps in the cross reference table and display warnings if any found. See also
        /// <a href="https://stackoverflow.com/questions/71267471/">here</a>.
        /// </summary>
        /// <param name="doc">document</param>
        public static void CheckCrossReferenceTable(PdfCatalog doc)
        {
            List<XRefEntry> set = new(doc.Document.IndirectObjects.Select(x => x.XrefEntry));
            if (set.Count != set.Last().Number)
            {
                long n = 0;
                foreach (var key in set)
                {
                    ++n;
                    while (n < key.Number)
                    {
                        Debug.WriteLine($"Object {n} missing, signature verification may fail in Adobe Reader, see https://stackoverflow.com/questions/71267471/");
                        ++n;
                    }
                }
            }
        }

        /// <summary>
        /// Like {@link URL#openStream()} but will follow redirection from http to https.
        /// </summary>
        /// <param name="urlString"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task<Stream> OpenURL(string urlString)
        {
            var url = new Uri(urlString);
            if (!urlString.StartsWith("http"))
            {
                throw new Exception("so that ftp is still supported");
            }
            var con = new HttpClient() { BaseAddress = url };
            var response = await con.GetAsync(url);
            Debug.WriteLine($"{response.StatusCode} {response}");
            if (response.StatusCode == System.Net.HttpStatusCode.Moved ||
                response.StatusCode == System.Net.HttpStatusCode.MovedPermanently ||
                response.StatusCode == System.Net.HttpStatusCode.SeeOther)
            {
                var location = response.Headers.Location;
                if (url.Scheme == "http"
                    && location.Scheme == "https"
                    && url.PathAndQuery.Equals(location.PathAndQuery))
                {
                    // redirection from http:// to https://
                    // change this code if you want to be more flexible (but think about security!)
                    Debug.WriteLine($"redirection to {location} followed");
                    response = await con.GetAsync(location);
                }
                else
                {
                    Debug.WriteLine($"redirection to {location} ignored");
                }
            }
            var stream = await response.Content.ReadAsStreamAsync();
            return new StreamContainer(stream);
        }
    }
}
