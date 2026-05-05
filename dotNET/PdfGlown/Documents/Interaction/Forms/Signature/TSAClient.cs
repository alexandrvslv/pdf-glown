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
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Tsp;
using System.IO;
using System;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.IO;
using Org.BouncyCastle.Math;
using System.Diagnostics;
using System.Net.Http;
using PdfGlown.Tokens;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using Org.BouncyCastle.Operators.Utilities;

/// <summary>Time Stamping Authority (TSA) Client [RFC 3161].
/// @author Vakhtang Koroghlishvili
/// @author John Hewson</summary>
public class TSAClient
{

    private static readonly DefaultDigestAlgorithmFinder ALGORITHM_OID_FINDER = DefaultDigestAlgorithmFinder.Instance;

    private readonly Uri url;
    private readonly string username;
    private readonly string password;
    private readonly IDigest digest;

    // SecureRandom.getInstanceStrong() would be better, but sometimes blocks on Linux
    private static readonly Random RANDOM = new SecureRandom();

    /**
     *
     * @param url the URL of the TSA service
     * @param username user name of TSA
     * @param password password of TSA
     * @param digest the message digest to use
     */
    public TSAClient(Uri url, string username, string password, IDigest digest)
    {
        this.url = url;
        this.username = username;
        this.password = password;
        this.digest = digest;
    }

    /**
     *
     * @param content
     * @return the time stamp token
     * @throws IOException if there was an error with the connection or data from the TSA server,
     *                     or if the time stamp response could not be validated
     */
    public async Task<TimeStampToken> GetTimeStampToken(Stream content)
    {
        digest.Reset();
        var dis = new DigestStream(content, digest, null);
        while (dis.ReadByte() != -1)
        {
            // do nothing
        }
        byte[] hash = new byte[digest.GetDigestSize()];
        digest.DoFinal(hash, 0);

        // 32-bit cryptographic nonce
        int nonce = RANDOM.Next();

        // generate TSA request
        var tsaGenerator = new TimeStampRequestGenerator();
        tsaGenerator.SetCertReq(true);
        var oid = ALGORITHM_OID_FINDER.Find(digest.AlgorithmName).Algorithm;
        TimeStampRequest request = tsaGenerator.Generate(oid, hash, BigInteger.ValueOf(nonce));

        // get TSA response
        byte[] tsaResponse = await GetTSAResponse(request.GetEncoded());

        TimeStampResponse response;
        try
        {
            response = new TimeStampResponse(tsaResponse);
            response.Validate(request);
        }
        catch (Exception e)
        {
            throw new IOException(e.Message, e);
        }

        TimeStampToken timeStampToken = response.TimeStampToken;
        if (timeStampToken == null)
        {
            // https://www.ietf.org/rfc/rfc3161.html#section-2.4.2
            throw new IOException($"Response from {url} does not have a time stamp token, status: {response.Status} ({response.GetStatusString()})");
        }

        return timeStampToken;
    }

    // gets response data for the given encoded TimeStampRequest data
    // throws IOException if a connection to the TSA cannot be established
    private async Task<byte[]> GetTSAResponse(byte[] request)
    {
        Debug.WriteLine("debug: Opening connection to TSA server");

        // todo: support proxy servers
        var connection = new HttpClient();
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        httpRequest.Headers.Add("Content-Type", "application/timestamp-query");

        Debug.WriteLine("debug: Established connection to TSA server");

        if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
        {
            var base64 = Convert.ToBase64String(Charset.UTF8.GetBytes(username + ":" + password));
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64);
        }
        httpRequest.Content = new ByteArrayContent(request);

        Debug.WriteLine("debug: Waiting for response from TSA server");

        byte[] response = null;

        try
        {
            var responceMessage = await connection.SendAsync(httpRequest);
            {
                response = await responceMessage.Content.ReadAsByteArrayAsync();
            }
        }
        catch (IOException ex)
        {
            Debug.WriteLine($"error: Exception {ex.Message} when reading from {this.url}");
            throw;
        }

        Debug.WriteLine("debug: Received response from TSA server");

        return response;
    }
}
