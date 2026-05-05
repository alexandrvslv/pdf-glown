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
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Tsp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

/**
 * This class wraps the TSAClient and the work that has to be done with it. Like Adding Signed
 * TimeStamps to a signature, or creating a CMS timestamp attribute (with a signed timestamp)
 *
 * @author Others
 * @author Alexis Suter
 */
public class ValidationTimeStamp
{
    private TSAClient tsaClient;

    /**
     * @param tsaUrl The url where TS-Request will be done.
     * @throws NoSuchAlgorithmException
     * @throws MalformedURLException
     * @throws java.net.URISyntaxException
     */
    public ValidationTimeStamp(string tsaUrl)
    {
        if (tsaUrl != null)
        {
            var digest = new Sha256Digest();
            this.tsaClient = new TSAClient(new Uri(tsaUrl), null, null, digest);
        }
    }

    /**
     * Creates a signed timestamp token by the given input stream.
     * 
     * @param content InputStream of the content to sign
     * @return the byte[] of the timestamp token
     * @throws IOException
     */
    public async Task<byte[]> GetTimeStampToken(Stream content)
    {
        TimeStampToken timeStampToken = await tsaClient.GetTimeStampToken(content);
        return timeStampToken.GetEncoded();
    }

    /**
     * Extend cms signed data with TimeStamp first or to all signers
     *
     * @param signedData Generated CMS signed data
     * @return CMSSignedData Extended CMS signed data
     * @throws IOException
     */
    public async Task<CmsSignedData> AddSignedTimeStamp(CmsSignedData signedData)
    {
        var signerStore = signedData.GetSignerInfos();
        List<SignerInformation> newSigners = new();

        foreach (SignerInformation signer in signerStore.GetSigners())
        {
            // This adds a timestamp to every signer (into his unsigned attributes) in the signature.
            newSigners.Add(await SignTimeStamp(signer));
        }

        // Because new SignerInformation is created, new SignerInfoStore has to be created 
        // and also be replaced in signedData. Which creates a new signedData object.
        return CmsSignedData.ReplaceSigners(signedData, new SignerInformationStore(newSigners));
    }

    /**
     * Extend CMS Signer Information with the TimeStampToken into the unsigned Attributes.
     *
     * @param signer information about signer
     * @return information about SignerInformation
     * @throws IOException
     */
    private async Task<SignerInformation> SignTimeStamp(SignerInformation signer)
    {
        AttributeTable unsignedAttributes = signer.UnsignedAttributes;

        var vector = unsignedAttributes?.ToAsn1EncodableVector() ?? new Asn1EncodableVector();

        var timeStampToken = await tsaClient.GetTimeStampToken(new MemoryStream(signer.GetSignature()));
        byte[] token = timeStampToken.GetEncoded();
        var oid = PkcsObjectIdentifiers.IdAASignatureTimeStampToken;
        var signatureTimeStamp = new Org.BouncyCastle.Asn1.Cms.Attribute(oid, new DerSet(Asn1Null.FromByteArray(token)));

        vector.Add(signatureTimeStamp);
        var signedAttributes = new Attributes(vector);

        // There is no other way changing the unsigned attributes of the signer information.
        // result is never null, new SignerInformation always returned, 
        // see source code of replaceUnsignedAttributes
        return SignerInformation.ReplaceUnsignedAttributes(signer, new AttributeTable(signedAttributes));
    }
}
