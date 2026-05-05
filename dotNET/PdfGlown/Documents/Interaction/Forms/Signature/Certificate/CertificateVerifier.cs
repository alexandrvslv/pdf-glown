
/**
* Licensed to the Apache Software Foundation (ASF) under one
* or more contributor license agreements. See the NOTICE file
* distributed with this work for additional information
* regarding copyright ownership. The ASF licenses this file
* to you under the Apache License, Version 2.0 (the
* "License"); you may not use this file except in compliance
* with the License. You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing,
* software distributed under the License is distributed on an
* "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
* KIND, either express or implied. See the License for the
* specific language governing permissions and limitations
* under the License.
*//**
* Copied from Apache CXF 2.4.9, initial version:
* https://svn.apache.org/repos/asf/cxf/tags/cxf-2.4.9/distribution/src/main/release/samples/sts_issue_operation/src/main/java/demo/sts/provider/cert/
* 
*/


using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Ocsp;
using Org.BouncyCastle.Pkix;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Security.Certificates;
using Org.BouncyCastle.Utilities.Collections;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Store;
using PdfGlown.Tokens;
using PdfGlown.Util.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace PdfGlown.Documents.Interaction.Forms.Signature.Sertificate
{
    public static class CertificateVerifier
    {

        static CertificateVerifier()
        {

        }

        /**
         * Attempts to build a certification chain for given certificate and to
         * verify it. Relies on a set of root CA certificates and intermediate
         * certificates that will be used for building the certification chain. The
         * verification process assumes that all self-signed certificates in the set
         * are trusted root CA certificates and all other certificates in the set
         * are intermediate certificates.
         *
         * @param cert - certificate for validation
         * @param additionalCerts - set of trusted root CA certificates that will be
         * used as "trust anchors" and intermediate CA certificates that will be
         * used as part of the certification chain. All self-signed certificates are
         * considered to be trusted root CA certificates. All the rest are
         * considered to be intermediate CA certificates.
         * @param verifySelfSignedCert true if a self-signed certificate is accepted, false if not.
         * @param signDate the date when the signing took place
         * @return the certification chain (if verification is successful)
         * @ - if the certification is not
         * successful (e.g. certification path cannot be built or some certificate
         * in the chain is expired or CRL checks are failed)
         */
        public static async Task<PkixCertPathBuilderResult> VerifyCertificate(
                X509Certificate cert, IEnumerable<X509Certificate> additionalCerts,
                bool verifySelfSignedCert, DateTime signDate)
        {
            try
            {
                // Check for self-signed certificate
                if (!verifySelfSignedCert && IsSelfSigned(cert))
                {
                    throw new CertificateVerificationException("The certificate is self-signed.");
                }

                HashSet<X509Certificate> certSet = new(additionalCerts);

                // Download extra certificates. However, each downloaded certificate can lead to
                // more extra certificates, e.g. with the file from PDFBOX-4091, which has
                // an incomplete chain.
                // You can skip this block if you know that the certificate chain is complete
                HashSet<X509Certificate> certsToTrySet = new();
                certsToTrySet.Add(cert);
                certsToTrySet.AddRange(additionalCerts);
                int downloadSize = 0;
                while (certsToTrySet.Any())
                {
                    HashSet<X509Certificate> nextCertsToTrySet = new();
                    foreach (X509Certificate tryCert in certsToTrySet)
                    {
                        HashSet<X509Certificate> downloadedExtraCertificatesSet = await DownloadExtraCertificates(tryCert);
                        foreach (var downloadedCertificate in downloadedExtraCertificatesSet)
                        {
                            if (!certSet.Contains(downloadedCertificate))
                            {
                                nextCertsToTrySet.Add(downloadedCertificate);
                                certSet.Add(downloadedCertificate);
                                downloadSize++;
                            }
                        }
                    }
                    certsToTrySet = nextCertsToTrySet;
                }
                if (downloadSize > 0)
                {
                    Debug.WriteLine($"CA issuers: {downloadSize} downloaded certificate(s) are new");
                }

                // Prepare a set of trust anchors (set of root CA certificates)
                // and a set of intermediate certificates
                List<X509Certificate> intermediateCerts = new();
                HashSet<TrustAnchor> trustAnchors = new();
                foreach (X509Certificate additionalCert in certSet)
                {
                    if (IsSelfSigned(additionalCert))
                    {
                        trustAnchors.Add(new TrustAnchor(additionalCert, null));
                    }
                    else
                    {
                        intermediateCerts.Add(additionalCert);
                    }
                }

                if (trustAnchors.Count == 0)
                {
                    throw new CertificateVerificationException("No root certificate in the chain");
                }

                // Attempt to build the certification chain and verify it
                PkixCertPathBuilderResult verifiedCertChain = VerifyCertificate(cert, trustAnchors, intermediateCerts, signDate);

                Debug.WriteLine($"info: Certification chain verified successfully up to this root: {verifiedCertChain.TrustAnchor.TrustedCert.SubjectDN}");

                await CheckRevocations(cert, certSet, signDate);

                return verifiedCertChain;
            }
            catch (PkixCertPathBuilderException certPathEx)
            {
                throw new CertificateVerificationException($"error: building certification path: " + cert.SubjectDN, certPathEx);
            }
            catch (CertificateVerificationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new CertificateVerificationException($"error: verifying the certificate: " + cert.SubjectDN, ex);
            }
        }

        private static async Task CheckRevocations(X509Certificate cert,
                                             HashSet<X509Certificate> additionalCerts,
                                             DateTime signDate)
        {
            if (IsSelfSigned(cert))
            {
                // root, we're done
                return;
            }
            foreach (X509Certificate additionalCert in additionalCerts)
            {
                try
                {
                    cert.Verify(additionalCert.GetPublicKey());
                    await CheckRevocationsWithIssuer(cert, additionalCert, additionalCerts, signDate);
                    // there can be several issuers
                }
                catch (GeneralSecurityException)
                {
                    // not the issuer
                }
            }
        }

        private static async Task CheckRevocationsWithIssuer(X509Certificate cert, X509Certificate issuerCert,
                HashSet<X509Certificate> additionalCerts, DateTime signDate)
        {
            // Try checking the certificate through OCSP (faster than CRL)
            string ocspURL = ExtractOCSPURL(cert);
            if (ocspURL != null)
            {
                var ocspHelper = new OcspHelper(cert, signDate, issuerCert, additionalCerts, ocspURL);
                try
                {
                    await VerifyOCSP(ocspHelper, additionalCerts);
                }
                catch (Exception ex)
                {
                    // IOException happens with 021496.pdf because OCSP responder no longer exists
                    // OCSPException happens with QV_RCA1_RCA3_CPCPS_V4_11.pdf
                    Debug.WriteLine("warn: Exception trying OCSP, will try CRL", ex);
                    Debug.WriteLine("warn: Certificate# to check: {}", cert.SerialNumber.ToString(16));
                    await CRLVerifier.VerifyCertificateCRLs(cert, signDate, additionalCerts);
                }
            }
            else
            {
                Debug.WriteLine("info:  OCSP not available, will try CRL");

                // Check whether the certificate is revoked by the CRL
                // given in its CRL distribution point extension
                await CRLVerifier.VerifyCertificateCRLs(cert, signDate, additionalCerts);
            }

            // now check the issuer
            await CheckRevocations(issuerCert, additionalCerts, signDate);
        }

        /**
         * Checks whether given X.509 certificate is self-signed.
         * @param cert The X.509 certificate to check.
         * @return true if the certificate is self-signed, false if not.
         * @throws java.security.GeneralSecurityException 
         */
        public static bool IsSelfSigned(X509Certificate cert)
        {
            try
            {
                // Try to verify certificate signature with its own public key
                var key = cert.GetPublicKey();
                cert.Verify(key);
                return true;
            }
            catch (Exception ex)
            {
                // Invalid signature --> not self-signed
                Debug.WriteLine($"warn: Couldn't get signature information - returning false, {ex.Message}");
                return false;
            }
        }

        /**
         * Download extra certificates from the URI mentioned in id-ad-caIssuers in the "authority
         * information access" extension. The method is lenient, i.e. catches all exceptions.
         *
         * @param ext an X509 object that can have extensions.
         *
         * @return a certificate set, never null.
         */
        public static async Task<HashSet<X509Certificate>> DownloadExtraCertificates(IX509Extension ext)
        {
            // https://tools.ietf.org/html/rfc2459#section-4.2.2.1
            // https://tools.ietf.org/html/rfc3280#section-4.2.2.1
            // https://tools.ietf.org/html/rfc4325
            HashSet<X509Certificate> resultSet = new();
            var authorityExtensionValue = ext.GetExtensionValue(X509Extensions.AuthorityInfoAccess);
            if (authorityExtensionValue == null)
            {
                return resultSet;
            }

            var asn1Seq = Asn1Sequence.GetInstance(authorityExtensionValue);
            foreach (Asn1Encodable encodable in asn1Seq)
            {
                // AccessDescription
                var obj = Asn1Sequence.GetInstance(encodable);
                var oid = obj[0];
                if (!X509ObjectIdentifiers.IdADCAIssuers.Equals(oid))
                {
                    continue;
                }
                var location = Asn1TaggedObject.GetInstance(obj[1]);
                var uri = (Asn1OctetString)location.GetExplicitBaseObject().ToAsn1Object();
                string urlString = Charset.UTF8.GetString(uri.GetOctets());
                Debug.WriteLine($"info: CA issuers URL: {urlString}");
                try
                {
                    using (var input = await SigUtils.OpenURL(urlString))
                    {
                        X509CertificateParser certFactory = new X509CertificateParser();
                        var altCerts = certFactory.ReadCertificates(input);
                        resultSet.AddRange(altCerts.Cast<X509Certificate>());
                        Debug.WriteLine($"info: CA issuers URL: {altCerts.Count} certificate(s) downloaded");
                    }
                }
                catch (Exception exception) when (exception is IOException | exception is HttpRequestException)
                {
                    Debug.WriteLine($"warn: {urlString} failure: {exception.Message}");
                }
                catch (CertificateException ex)
                {
                    Debug.WriteLine($"warn: {ex.Message}");
                }
            }
            Debug.WriteLine($"info: CA issuers: Downloaded {resultSet.Count} certificate(s) total");
            return resultSet;
        }

        /**
         * Attempts to build a certification chain for given certificate and to
         * verify it. Relies on a set of root CA certificates (trust anchors) and a
         * set of intermediate certificates (to be used as part of the chain).
         *
         * @param cert - certificate for validation
         * @param trustAnchors - set of trust anchors
         * @param intermediateCerts - set of intermediate certificates
         * @param signDate the date when the signing took place
         * @return the certification chain (if verification is successful)
         * @ - if the verification is not successful
         * (e.g. certification path cannot be built or some certificate in the chain
         * is expired)
         */
        private static PkixCertPathBuilderResult VerifyCertificate(X509Certificate cert, HashSet<TrustAnchor> trustAnchors,
                List<X509Certificate> intermediateCerts, DateTime signDate)
        {
            // Create the selector that specifies the starting certificate
            var selector = new X509CertStoreSelector();
            selector.Certificate = cert;

            // Configure the PKIX certificate builder algorithm parameters
            var pkixParams = new PkixBuilderParameters(trustAnchors, selector);

            // Disable CRL checks (this is done manually as additional step)
            pkixParams.IsRevocationEnabled = false;

            // not doing this brings
            // "SunCertPathBuilderException: unable to find valid certification path to requested target"
            // (when using -Djava.security.debug=certpath: "critical policy qualifiers present in certificate")
            // for files like 021496.pdf that have the "Adobe CDS Certificate Policy" 1.2.840.113583.1.2.1
            // CDS = "Certified Document Services"
            // https://www.adobe.com/misc/pdfs/Adobe_CDS_CP.pdf
            pkixParams.IsPolicyQualifiersRejected = false;
            // However, maybe there is still work to do:
            // "If the policyQualifiersRejected flag is set to false, it is up to the application
            // to validate all policy qualifiers in this manner in order to be PKIX compliant."

            pkixParams.Date = signDate;

            // Specify a list of intermediate certificates
            var intermediateCertStore = CollectionUtilities.CreateStore(intermediateCerts);
            pkixParams.AddStoreCert(intermediateCertStore);

            // Build and verify the certification chain
            // If this doesn't work although it should, it can be debugged
            // by starting java with -Djava.security.debug=certpath
            // see also
            // https://docs.oracle.com/javase/8/docs/technotes/guides/security/troubleshooting-security.html
            var builder = new PkixCertPathBuilder();
            return builder.Build(pkixParams);
        }

        /**
         * Extract the OCSP URL from an X.509 certificate if available.
         *
         * @param cert X.509 certificate
         * @return the URL of the OCSP validation service
         * @throws IOException 
         */
        private static string ExtractOCSPURL(X509Certificate cert)
        {
            var authorityExtensionValue = cert.GetExtensionValue(X509Extensions.AuthorityInfoAccess);
            if (authorityExtensionValue != null)
            {
                // copied from CertInformationHelper.getAuthorityInfoExtensionValue()
                // DRY refactor should be done some day
                var asn1Seq = Asn1Sequence.GetInstance(authorityExtensionValue);
                foreach (Asn1Encodable encodable in asn1Seq)
                {
                    // AccessDescription
                    var obj = Asn1Sequence.GetInstance(encodable);
                    var oid = obj[0];
                    // accessLocation
                    var location = Asn1TaggedObject.GetInstance(obj[1]);
                    if (X509ObjectIdentifiers.IdADOcsp.Equals(oid)
                        && location.TagNo == GeneralName.UniformResourceIdentifier)
                    {
                        var url = (Asn1OctetString)location.GetExplicitBaseObject().ToAsn1Object();
                        string ocspURL = Charset.UTF8.GetString(url.GetOctets());
                        Debug.WriteLine($"info: OCSP URL: {ocspURL}");
                        return ocspURL;
                    }
                }
            }
            return null;
        }

        /**
         * Verify whether the certificate has been revoked at signing date, and verify whether
         * the certificate of the responder has been revoked now.
         *
         * @param ocspHelper the OCSP helper.
         * @param additionalCerts
         * @throws RevokedCertificateException
         * @throws IOException
         * @throws URISyntaxException
         * @throws OCSPException
         * @
         */
        private static async Task VerifyOCSP(OcspHelper ocspHelper, HashSet<X509Certificate> additionalCerts)
        {
            DateTime now = DateTime.Now;
            OcspResp ocspResponse;
            ocspResponse = await ocspHelper.GetResponseOcsp();
            if (ocspResponse.Status != OcspResponseStatus.Successful)
            {
                throw new CertificateVerificationException($"OCSP check not successful, status: {ocspResponse.Status}");
            }
            Debug.WriteLine("info:  OCSP check successful");

            var basicResponse = (BasicOcspResp)ocspResponse.GetResponseObject();
            X509Certificate ocspResponderCertificate = ocspHelper.GetOcspResponderCertificate();
            if (ocspResponderCertificate.GetExtensionValue(OcspObjectIdentifiers.PkixOcspNocheck) != null)
            {
                // https://tools.ietf.org/html/rfc6960#section-4.2.2.2.1
                // A CA may specify that an OCSP client can trust a responder for the
                // lifetime of the responder's certificate.  The CA does so by
                // including the extension id-pkix-ocsp-nocheck.
                Debug.WriteLine("info:  Revocation check of OCSP responder certificate skipped (id-pkix-ocsp-nocheck is set)");
                return;
            }

            if (ocspHelper.CertificateToCheck.Equals(ocspResponderCertificate))
            {
                Debug.WriteLine("info:  OCSP responder certificate is identical to certificate to check");
                return;
            }

            Debug.WriteLine("info:  Check of OCSP responder certificate");
            HashSet<X509Certificate> additionalCerts2 = new(additionalCerts);
            foreach (X509Certificate cert in basicResponse.GetCerts())
            {
                try
                {
                    if (!ocspResponderCertificate.Equals(cert))
                    {
                        additionalCerts2.Add(cert);
                    }
                }
                catch (CertificateException ex)
                {
                    // unlikely to happen because the certificate existed as an object
                    Debug.WriteLine("error: " + ex.Message);
                }
            }
            await CertificateVerifier.VerifyCertificate(ocspResponderCertificate, additionalCerts2, true, now);
            Debug.WriteLine("info:  Check of OCSP responder certificate done");
        }


    }
}
