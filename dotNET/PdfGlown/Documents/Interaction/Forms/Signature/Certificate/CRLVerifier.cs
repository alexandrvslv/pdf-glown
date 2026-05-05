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
 */
/**
 * Copied from Apache CXF 2.4.9, initial version:
 * https://svn.apache.org/repos/asf/cxf/tags/cxf-2.4.9/distribution/src/main/release/samples/sts_issue_operation/src/main/java/demo/sts/provider/cert/
 * 
 */

using System;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System.Collections.Generic;
using System.Diagnostics;
using PdfGlown.Util.Collections;
using System.Threading.Tasks;
using System.DirectoryServices.Protocols;

namespace PdfGlown.Documents.Interaction.Forms.Signature.Sertificate
{
    public static class CRLVerifier
    {
        private const string LdapRevocationAttributeFilter = "certificateRevocationList;binary";

        static CRLVerifier()
        {
        }

        /**
         * Extracts the CRL distribution points from the certificate (if available)
         * and checks the certificate revocation status against the CRLs coming from
         * the distribution points. Supports HTTP, HTTPS, FTP and LDAP based URLs.
         *
         * @param cert the certificate to be checked for revocation
         * @param signDate the date when the signing took place
         * @param additionalCerts set of trusted root CA certificates that will be
         * used as "trust anchors" and intermediate CA certificates that will be
         * used as part of the certification chain.
         * @throws CertificateVerificationException if the certificate could not be verified
         * @ if the certificate is revoked
         */
        // nested exception needed to try several distribution points
        public static async Task VerifyCertificateCRLs(X509Certificate cert, DateTime signDate, HashSet<X509Certificate> additionalCerts)
        {
            try
            {
                DateTime now = DateTime.Now;
                Exception firstException = null;
                List<string> crlDistributionPointsURLs = GetCrlDistributionPoints(cert);
                foreach (string crlDistributionPointsURL in crlDistributionPointsURLs)
                {
                    Debug.WriteLine($"info: Checking distribution point URL: {crlDistributionPointsURL}");

                    X509Crl crl;
                    try
                    {
                        crl = await DownloadCRL(crlDistributionPointsURL);
                    }
                    catch (Exception ex)
                    {
                        // e.g. LDAP behind corporate proxy
                        // but couldn't get LDAP to work at all, see e.g. file from PDFBOX-1452
                        Debug.WriteLine($"warn:Caught {ex.GetType().Name} downloading CRL, will try next distribution point if available");
                        if (firstException == null)
                        {
                            firstException = ex;
                        }
                        continue;
                    }

                    var mergedCertSet = await CertificateVerifier.DownloadExtraCertificates(crl);
                    mergedCertSet.AddRange(additionalCerts);

                    // Verify CRL, see wikipedia:
                    // "To validate a specific CRL prior to relying on it,
                    //  the certificate of its corresponding CA is needed"
                    X509Certificate crlIssuerCert = null;
                    foreach (X509Certificate possibleCert in mergedCertSet)
                    {
                        try
                        {
                            cert.Verify(possibleCert.GetPublicKey());
                            crlIssuerCert = possibleCert;
                            break;
                        }
                        catch (GeneralSecurityException)
                        {
                            // not the issuer
                        }
                    }
                    if (crlIssuerCert == null)
                    {
                        throw new CertificateVerificationException(
                                "Certificate for " + crl.IssuerDN +
                                "not found in certificate chain, so the CRL at " +
                                crlDistributionPointsURL + " could not be verified");
                    }
                    crl.Verify(crlIssuerCert.GetPublicKey());
                    //TODO these should be exceptions, but for that we need a test case where
                    // a PDF has a broken OCSP and a working CRL
                    if (crl.ThisUpdate > now)
                    {
                        Debug.WriteLine($"error: CRL not yet valid, thisUpdate is {crl.ThisUpdate}");
                    }
                    if (crl.NextUpdate < now)
                    {
                        Debug.WriteLine($"error: CRL no longer valid, nextUpdate is {crl.NextUpdate}");
                    }

                    if (!crl.IssuerDN.Equals(cert.IssuerDN))
                    {
                        Debug.WriteLine("info:CRL issuer certificate is not identical to cert issuer, check needed");
                        await CertificateVerifier.VerifyCertificate(crlIssuerCert, mergedCertSet, true, now);
                        Debug.WriteLine("info:CRL issuer certificate checked successfully");
                    }
                    else
                    {
                        Debug.WriteLine("info:CRL issuer certificate is identical to cert issuer, no extra check needed");
                    }

                    CheckRevocation(crl, cert, signDate, crlDistributionPointsURL);

                    // https://tools.ietf.org/html/rfc5280#section-4.2.1.13
                    // If the DistributionPointName contains multiple values,
                    // each name describes a different mechanism to obtain the same
                    // CRL.  For example, the same CRL could be available for
                    // retrieval through both LDAP and HTTP.
                    //
                    // => thus no need to check several protocols
                    return;
                }
                if (firstException != null)
                {
                    throw firstException;
                }
            }
            catch (Exception exception) when (exception is RevokedCertificateException | exception is CertificateVerificationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new CertificateVerificationException("Cannot verify CRL for certificate: " + cert.SubjectDN, ex);
            }
        }

        /**
         * Check whether the certificate was revoked at signing time.
         *
         * @param crl certificate revocation list
         * @param cert certificate to be checked
         * @param signDate date the certificate was used for signing
         * @param crlDistributionPointsURL URL for log message or exception text
         * @ if the certificate was revoked at signing time
         */
        public static void CheckRevocation(X509Crl crl, X509Certificate cert, DateTime signDate, string crlDistributionPointsURL)
        {
            X509CrlEntry revokedCRLEntry = crl.GetRevokedCertificate(cert.SerialNumber);
            if (revokedCRLEntry != null &&
                    revokedCRLEntry.RevocationDate.CompareTo(signDate) <= 0)
            {
                throw new RevokedCertificateException($"The certificate was revoked by CRL {crlDistributionPointsURL} on {revokedCRLEntry.RevocationDate}",
                        revokedCRLEntry.RevocationDate);
            }
            else if (revokedCRLEntry != null)
            {
                Debug.WriteLine($"info: The certificate was revoked after signing by CRL {crlDistributionPointsURL} on {revokedCRLEntry.RevocationDate}");
            }
            else
            {
                Debug.WriteLine($"info: The certificate was not revoked by CRL {crlDistributionPointsURL}");
            }
        }

        /**
         * Downloads CRL from given URL. Supports http, https, ftp and ldap based URLs.
         */
        private static Task<X509Crl> DownloadCRL(string crlURL)
        {
            if (crlURL.StartsWith("http://") || crlURL.StartsWith("https://")
                    || crlURL.StartsWith("ftp://"))
            {
                return downloadCRLFromWeb(crlURL);
            }
            else if (crlURL.StartsWith("ldap://"))
            {
                return downloadCRLFromLDAP(crlURL);
            }
            else
            {
                throw new CertificateVerificationException(
                        "Can not download CRL from certificate "
                        + "distribution point: " + crlURL);
            }
        }

        /**
         * Downloads a CRL from given LDAP url, e.g.
         * ldap://ldap.infonotary.com/dc=identity-ca,dc=infonotary,dc=com
         */
        private static async Task<X509Crl> downloadCRLFromLDAP(string ldapURL)
        {
            var conn = new LdapConnection(ldapURL);
            // don't wait forever behind corporate proxy
            conn.Timeout = TimeSpan.FromMilliseconds(1000);
            // https://docs.oracle.com/javase/jndi/tutorial/ldap/connect/create.html
            var request = new SearchRequest();
            request.Filter = "(&(certificateRevocationList=*))";
            request.Attributes.Add(LdapRevocationAttributeFilter);
            var asyncResult = conn.BeginSendRequest(request, PartialResultProcessing.NoPartialResultSupport, null, null);
            var result = await Task<SearchResponse>.Factory.FromAsync(asyncResult, x => (SearchResponse)conn.EndSendRequest(x));

            if (result.Entries.Count == 0
                || !result.Entries[0].Attributes.Contains(LdapRevocationAttributeFilter)
                || !(result.Entries[0].Attributes[LdapRevocationAttributeFilter].GetValues(typeof(byte[])) is byte[][] resultArray))
            {
                throw new CertificateVerificationException("Can not download CRL from: " + ldapURL);
            }
            else
            {
                ;
                return new X509CrlParser().ReadCrl(resultArray[0]);
                //return (X509Crl)CertificateFactory.getInstance("X.509").generateCRL(new ByteStream(val));
            }
        }

        /**
         * Downloads a CRL from given HTTP/HTTPS/FTP URL, e.g.
         * http://crl.infonotary.com/crl/identity-ca.crl
         */
        public static async Task<X509Crl> downloadCRLFromWeb(string crlURL)
        {
            using (var crlStream = await SigUtils.OpenURL(crlURL))
            {
                return new X509CrlParser().ReadCrl(crlStream);
                //return (X509Crl)CertificateFactory.getInstance("X.509").generateCRL(crlStream);
            }
        }

        /**
         * Extracts all CRL distribution point URLs from the "CRL Distribution
         * Point" extension in a X.509 certificate. If CRL distribution point
         * extension is unavailable, returns an empty list.
         * @param cert
         * @return List of CRL distribution point URLs.
         * @throws java.io.IOException
         */
        public static List<string> GetCrlDistributionPoints(X509Certificate cert)

        {
            var dosCrlDP = cert.GetExtensionValue(X509Extensions.CrlDistributionPoints);
            if (dosCrlDP == null)
            {
                return new List<string>();
            }
            byte[] crldpExtOctets = dosCrlDP.GetOctets();
            Asn1Object derObj2;
            using (var oAsnInStream2 = new Asn1InputStream(crldpExtOctets))
            {
                derObj2 = oAsnInStream2.ReadObject();
            }
            var distPoint = CrlDistPoint.GetInstance(derObj2);
            List<string> crlUrls = new();
            foreach (DistributionPoint dp in distPoint.GetDistributionPoints())
            {
                var dpn = dp.DistributionPointName;
                // Look for URIs in fullName
                if (dpn != null && dpn.Type == DistributionPointName.FullName)
                {
                    // Look for an URI
                    foreach (GeneralName genName in GeneralNames.GetInstance(dpn.Name).GetNames())
                    {
                        if (genName.TagNo == GeneralName.UniformResourceIdentifier)
                        {
                            string url = DerIA5String.GetInstance(genName.Name).GetString();
                            crlUrls.Add(url);
                        }
                    }
                }
            }
            return crlUrls;
        }
    }
}