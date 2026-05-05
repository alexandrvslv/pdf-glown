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
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Ocsp;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Security.Certificates;
using Org.BouncyCastle.X509;
using PdfGlown.Util.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace PdfGlown.Documents.Interaction.Forms.Signature.Sertificate
{

    /**
     * Helper Class for OCSP-Operations with bouncy castle.
     * 
     * @author Alexis Suter
     */
    public class OcspHelper
    {

        private readonly X509Certificate issuerCertificate;
        private readonly DateTime signDate;
        private readonly X509Certificate certificateToCheck;
        private readonly HashSet<X509Certificate> additionalCerts;
        private readonly string ocspUrl;
        private DerOctetString encodedNonce;
        private X509Certificate ocspResponderCertificate;

        // SecureRandom.getInstanceStrong() would be better, but sometimes blocks on Linux
        private static readonly Random RANDOM = new SecureRandom();

        /**
         * @param checkCertificate Certificate to be OCSP-checked
         * @param signDate the date when the signing took place
         * @param issuerCertificate Certificate of the issuer
         * @param additionalCerts Set of trusted root CA certificates that will be used as "trust
         * anchors" and intermediate CA certificates that will be used as part of the certification
         * chain. All self-signed certificates are considered to be trusted root CA certificates. All
         * the rest are considered to be intermediate CA certificates.
         * @param ocspUrl where to fetch for OCSP
         */
        public OcspHelper(X509Certificate checkCertificate, DateTime signDate, X509Certificate issuerCertificate,
                HashSet<X509Certificate> additionalCerts, string ocspUrl)
        {
            this.certificateToCheck = checkCertificate;
            this.signDate = signDate;
            this.issuerCertificate = issuerCertificate;
            this.additionalCerts = additionalCerts;
            this.ocspUrl = ocspUrl;
        }

        /**
         * Get the certificate to be OCSP-checked.
         * 
         * @return The certificate to be OCSP-checked.
         */
        public X509Certificate CertificateToCheck
        {
            get => certificateToCheck;
        }

        /**
         * Performs and verifies the OCSP-Request
         *
         * @return the OcspResp, when the request was successful, else a corresponding exception will be
         * thrown. Never returns null.
         *
         * @
         * @throws OcspException
         * @throws RevokedCertificateException
         * @throws URISyntaxException
         */
        public async Task<OcspResp> GetResponseOcsp()
        {
            var ocspResponse = await PerformRequest(ocspUrl);
            VerifyOcspResponse(ocspResponse);
            return ocspResponse;
        }

        /**
         * Get responder certificate. This is available after {@link #getResponseOcsp()} has been
         * called. This method should be used instead of {@code basicResponse.getCerts()[0]}
         *
         * @return The certificate of the responder.
         */
        public X509Certificate GetOcspResponderCertificate()
        {
            return ocspResponderCertificate;
        }

        /**
         * Verifies the status and the response itself (including nonce), but not the signature.
         * 
         * @param ocspResponse to be verified
         * @throws OcspException
         * @throws RevokedCertificateException
         * @ if the default security provider can't be instantiated
         */
        private void VerifyOcspResponse(OcspResp ocspResponse)
        {
            VerifyRespStatus(ocspResponse);

            var basicResponse = (BasicOcspResp)ocspResponse.GetResponseObject();
            if (basicResponse != null)
            {
                var responderID = basicResponse.ResponderId.ToAsn1Object();
                // https://tools.ietf.org/html/rfc6960#section-4.2.2.3
                // The basic response type contains:
                // (...)
                // either the name of the responder or a hash of the responder's
                // public key as the ResponderID
                // (...)
                // The responder MAY include certificates in the certs field of
                // BasicOCSPResponse that help the OCSP client verify the responder's
                // signature.
                var name = responderID.Name;
                if (name != null)
                {
                    FindResponderCertificateByName(basicResponse, name);
                }
                else
                {
                    byte[] keyHash = responderID.GetKeyHash();
                    if (keyHash != null)
                    {
                        FindResponderCertificateByKeyHash(basicResponse, keyHash);
                    }
                    else
                    {
                        throw new OcspException("OCSP: basic response must provide name or key hash");
                    }
                }

                if (ocspResponderCertificate == null)
                {
                    throw new OcspException("OCSP: certificate for responder " + name + " not found");
                }

                try
                {
                    SigUtils.CheckResponderCertificateUsage(ocspResponderCertificate);
                }
                catch (Exception ex)
                {
                    // unlikely to happen because the certificate existed as an object
                    Debug.WriteLine($"error:{ex.Message}");
                }
                CheckOcspSignature(ocspResponderCertificate, basicResponse);

                bool nonceChecked = CheckNonce(basicResponse);

                var responses = basicResponse.Responses;
                if (responses.Length != 1)
                {
                    throw new Exception($"OCSP: Received {responses.Length} responses instead of 1!");
                }

                var resp = responses[0];
                Object status = resp.GetCertStatus();

                if (!nonceChecked)
                {
                    // https://tools.ietf.org/html/rfc5019
                    // fall back to validating the OCSPResponse based on time
                    CheckOcspResponseFresh(resp);
                }

                if (status is RevokedStatus revokedStatus)
                {
                    if (revokedStatus.RevocationTime.CompareTo(signDate) <= 0)
                    {
                        throw new RevokedCertificateException($"OCSP: Certificate is revoked since {revokedStatus.RevocationTime}", revokedStatus.RevocationTime);
                    }
                    Debug.WriteLine($"info: The certificate was revoked after signing by OCSP {ocspUrl} on {revokedStatus.RevocationTime}");
                }
                else if (status != CertificateStatus.Good)
                {
                    throw new OcspException("OCSP: Status of Cert is unknown");
                }
            }
        }

        private byte[] GetKeyHashFromCert(X509Certificate certHolder)
        {
            // https://tools.ietf.org/html/rfc2560#section-4.2.1
            // KeyHash ::= OCTET STRING -- SHA-1 hash of responder's public key
            //         -- (i.e., the SHA-1 hash of the value of the
            //         -- BIT STRING subjectPublicKey [excluding
            //         -- the tag, length, and number of unused
            //         -- bits] in the responder's certificate)

            // code below inspired by org.bouncycastle.cert.ocsp.CertificateID.createCertID()
            // tested with SO52757037-Signed3-OCSP-with-KeyHash.pdf
            var info = certHolder.CertificateStructure.SubjectPublicKeyInfo;
            try
            {
#if __BC_HASH__
                var hash = new Sha1Digest();
#else
                using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
#endif
                return hash.Digest(info.PublicKey.GetBytes());
            }
            catch (Exception ex)
            {
                // should not happen
                Debug.WriteLine("error: SHA-1 Algorithm not found", ex);
                return new byte[0];
            }
        }

        private void FindResponderCertificateByKeyHash(BasicOcspResp basicResponse, byte[] keyHash)

        {
            X509Certificate[] certs = basicResponse.GetCerts();
            foreach (X509Certificate cert in certs)
            {
                try
                {
                    byte[] digest = GetKeyHashFromCert(cert);
                    if (MemoryExtensions.SequenceEqual(keyHash.AsSpan(), digest))
                    {
                        ocspResponderCertificate = cert;
                        break;
                    }
                }
                catch (CertificateEncodingException ex)
                {
                    // unlikely to happen because the certificate existed as an object
                    Debug.WriteLine($"error: {ex.Message}");
                }
            }

            // DO NOT use the certificate found in additionalCerts first. One file had a
            // responder certificate in the PDF itself with SHA1withRSA algorithm, but
            // the responder delivered a different (newer, more secure) certificate
            // with SHA256withRSA (tried with QV_RCA1_RCA3_CPCPS_V4_11.pdf)
            // https://www.quovadisglobal.com/~/media/Files/Repository/QV_RCA1_RCA3_CPCPS_V4_11.ashx
            foreach (X509Certificate cert in additionalCerts)
            {
                try
                {
                    byte[] digest = GetKeyHashFromCert(new X509Certificate(cert.GetEncoded()));
                    if (MemoryExtensions.SequenceEqual(keyHash.AsSpan(), digest))
                    {
                        ocspResponderCertificate = cert;
                        return;
                    }
                }
                catch (CertificateEncodingException ex)
                {
                    // unlikely to happen because the certificate existed as an object
                    Debug.WriteLine($"error: {ex.Message}");
                }
            }
        }

        private void FindResponderCertificateByName(BasicOcspResp basicResponse, X509Name name)
        {
            X509Certificate[] certs = basicResponse.GetCerts();
            foreach (X509Certificate cert in certs)
            {
                if (cert.SubjectDN.Equals(name))
                {
                    ocspResponderCertificate = cert;
                }
            }

            // DO NOT use the certificate found in additionalCerts first. One file had a
            // responder certificate in the PDF itself with SHA1withRSA algorithm, but
            // the responder delivered a different (newer, more secure) certificate
            // with SHA256withRSA (tried with QV_RCA1_RCA3_CPCPS_V4_11.pdf)
            // https://www.quovadisglobal.com/~/media/Files/Repository/QV_RCA1_RCA3_CPCPS_V4_11.ashx
            foreach (X509Certificate cert in additionalCerts)
            {
                if (cert.SubjectDN.Equals(name))
                {
                    ocspResponderCertificate = cert;
                    return;
                }
            }
        }

        private void CheckOcspResponseFresh(SingleResp resp)
        {
            // https://tools.ietf.org/html/rfc5019
            // Clients MUST check for the existence of the nextUpdate field and MUST
            // ensure the current time, expressed in GMT time as described in
            // Section 2.2.4, falls between the thisUpdate and nextUpdate times.  If
            // the nextUpdate field is absent, the client MUST reject the response.

            var curDate = DateTime.Now;

            var thisUpdate = resp.ThisUpdate;
            if (thisUpdate == DateTime.MinValue)
            {
                throw new OcspException("OCSP: thisUpdate field is missing in response (RFC 5019 2.2.4.)");
            }
            var nextUpdate = resp.NextUpdate;
            if (nextUpdate == null)
            {
                throw new OcspException("OCSP: nextUpdate field is missing in response (RFC 5019 2.2.4.)");
            }
            if (curDate.CompareTo(thisUpdate) < 0)
            {
                Debug.WriteLine($"error: {curDate} < {thisUpdate}");
                throw new OcspException("OCSP: current date < thisUpdate field (RFC 5019 2.2.4.)");
            }
            if (curDate.CompareTo(nextUpdate.Value) > 0)
            {
                Debug.WriteLine($"error: {curDate} > {nextUpdate}");
                throw new OcspException("OCSP: current date > nextUpdate field (RFC 5019 2.2.4.)");
            }
            Debug.WriteLine("info: OCSP response is fresh");
        }

        /**
         * Checks whether the OCSP response is signed by the given certificate.
         * 
         * @param certificate the certificate to check the signature
         * @param basicResponse OCSP response containing the signature
         * @throws OcspException when the signature is invalid or could not be checked
         * @ if the default security provider can't be instantiated
         */
        private void CheckOcspSignature(X509Certificate certificate, BasicOcspResp basicResponse)
        {
            try
            {
                //ContentVerifierProvider verifier = new JcaContentVerifierProviderBuilder()
                //        .setProvider(SecurityProvider.getProvider()).build(certificate);

                if (!basicResponse.Verify(certificate.GetPublicKey()))
                {
                    throw new OcspException("OCSP-Signature is not valid!");
                }
            }
            catch (Exception e)
            {
                throw new OcspException("Error checking Ocsp-Signature", e);
            }
        }

        /**
         * Checks if the nonce in the response matches.
         * 
         * @param basicResponse Response to be checked
         * @return true if the nonce is present and matches, false if nonce is missing.
         * @throws OcspException if the nonce is different
         */
        private bool CheckNonce(BasicOcspResp basicResponse)
        {
            var responseNonceString = basicResponse.GetExtensionValue(OcspObjectIdentifiers.PkixOcspNonce);
            if (responseNonceString != null)
            {
                if (!responseNonceString.Equals(encodedNonce))
                {
                    throw new OcspException("Different nonce found in response!");
                }
                else
                {
                    Debug.WriteLine("info: Nonce is good");
                    return true;
                }
            }
            // https://tools.ietf.org/html/rfc5019
            // Clients that opt to include a nonce in the
            // request SHOULD NOT reject a corresponding OCSPResponse solely on the
            // basis of the nonexistent expected nonce, but MUST fall back to
            // validating the OCSPResponse based on time.
            return false;
        }

        /**
         * Performs the OCSP-Request, with given data.
         *
         * @param urlString URL of OCSP service.
         * @return the OcspResp, that has been fetched from the ocspUrl
         * @
         * @throws OcspException
         * @throws URISyntaxException
         */
        private async Task<OcspResp> PerformRequest(string urlString)
        {
            OcspReq request = GenerateOCSPRequest();
            var url = new Uri(urlString);
            var httpConnection = new HttpClient() { BaseAddress = url };
            try
            {

                var httpRequest = new HttpRequestMessage();
                httpRequest.Options.Set(new HttpRequestOptionsKey<string>("Content-Type"), "application/ocsp-request");
                httpRequest.Options.Set(new HttpRequestOptionsKey<string>("Accept"), "application/ocsp-response");
                httpRequest.Method = HttpMethod.Post;

                httpRequest.Content = new ReadOnlyMemoryContent(request.GetEncoded());

                var responce = await httpConnection.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);

                if (responce.StatusCode == System.Net.HttpStatusCode.Moved ||
                    responce.StatusCode == System.Net.HttpStatusCode.MovedPermanently ||
                    responce.StatusCode == System.Net.HttpStatusCode.SeeOther)
                {
                    var location = responce.Headers.Location;
                    if (url.Scheme == "http" &&
                        location.Scheme == "https" &&
                        url.PathAndQuery == location.PathAndQuery)
                    {
                        // redirection from http:// to https://
                        // change this code if you want to be more flexible (but think about security!)
                        Debug.WriteLine($"info: redirection to {location} followed");
                        return await PerformRequest(location.OriginalString);
                    }
                    else
                    {
                        Debug.WriteLine($"info: redirection to {location} ignored");
                    }
                }
                if (responce.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new IOException($"OCSP: Could not access url, ResponseCode {responce.StatusCode}: {responce}");
                }
                // Get response
                var input = await responce.Content.ReadAsByteArrayAsync();
                return new OcspResp(input);
            }
            finally
            {
                httpConnection.Dispose();
            }
        }

        /**
         * Helper method to verify response status.
         * 
         * @param resp OCSP response
         * @throws OcspException if the response status is not ok
         */
        public void VerifyRespStatus(OcspResp resp)
        {
            string statusInfo = "";
            if (resp != null)
            {
                int status = resp.Status;
                switch (status)
                {
                    case OcspResponseStatus.InternalError:
                        statusInfo = "INTERNAL_ERROR";
                        Debug.WriteLine("error: An internal error occurred in the OCSP Server!");
                        break;
                    case OcspResponseStatus.MalformedRequest:
                        // This happened when the "critical" flag was used for extensions
                        // on a responder known by the committer of this comment.
                        statusInfo = "MALFORMED_REQUEST";
                        Debug.WriteLine("error: Your request did not fit the RFC 2560 syntax!");
                        break;
                    case OcspResponseStatus.SignatureRequired:
                        statusInfo = "SIG_REQUIRED";
                        Debug.WriteLine("error: Your request was not signed!");
                        break;
                    case OcspResponseStatus.TryLater:
                        statusInfo = "TRY_LATER";
                        Debug.WriteLine("error: The server was too busy to answer you!");
                        break;
                    case OcspResponseStatus.Unauthorized:
                        statusInfo = "UNAUTHORIZED";
                        Debug.WriteLine("error: The server could not authenticate you!");
                        break;
                    case OcspResponseStatus.Successful:
                        break;
                    default:
                        statusInfo = "UNKNOWN";
                        Debug.WriteLine("error: Unknown OCSPResponse status code! {}", status);
                        break;
                }
            }
            if (resp == null || resp.Status != OcspResponseStatus.Successful)
            {
                throw new OcspException("OCSP response unsuccessful, status: " + statusInfo);
            }
        }

        /**
         * Generates an OCSP request and generates the <code>CertificateID</code>.
         *
         * @return OCSP request, ready to fetch data
         * @throws OcspException
         * @
         */
        private OcspReq GenerateOCSPRequest()
        {
            //Security.addProvider(SecurityProvider.getProvider());

            // Generate the ID for the certificate we are looking for
            CertificateID certId;
            try
            {
                certId = new CertificateID(CertificateID.DigestSha1, issuerCertificate, certificateToCheck.SerialNumber);
            }
            catch (CertificateEncodingException e)
            {
                throw new IOException("Error creating CertificateID with the Certificate encoding", e);
            }

            // https://tools.ietf.org/html/rfc2560#section-4.1.2
            // Support for any specific extension is OPTIONAL. The critical flag
            // SHOULD NOT be set for any of them.

            var oids = new List<DerObjectIdentifier>
            {
                OcspObjectIdentifiers.PkixOcspResponse ,
                OcspObjectIdentifiers.PkixOcspNonce
            };
            var exts = new List<X509Extension>
            {
                new X509Extension(false, new DerOctetString(new DerSequence(OcspObjectIdentifiers.PkixOcspBasic).GetEncoded())),
                new X509Extension(false, encodedNonce = new DerOctetString(Create16BytesNonce()))
            };

            var builder = new OcspReqGenerator();
            builder.SetRequestExtensions(new X509Extensions(oids, exts));
            builder.AddRequest(certId);
            return builder.Generate();
        }

        private byte[] Create16BytesNonce()
        {
            byte[] nonce = new byte[16];
            RANDOM.NextBytes(nonce);
            return nonce;
        }
    }
}

