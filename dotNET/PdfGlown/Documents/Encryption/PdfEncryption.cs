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

using PdfGlown.Objects;
using System;
using System.Collections.Generic;
using System.IO;

namespace PdfGlown.Documents.Encryption
{

    /// <summary>
    /// This class is a specialized view of the encryption Dictionary of a PDF document.
    /// It contains a low level Dictionary (PdfDictionary) and provides the methods to
    /// manage its fields.
    /// </summary>
    /// <remarks>
    /// The available fields are the ones who are involved by standard security handler
    ///  and public key security handler.    
    /// @author Ben Litchfield
    /// @author Benoit Guillon
    /// </remarks>
    public class PdfEncryption : PdfDictionary
    {
        /// <summary>See PDF Reference 1.4 Table 3.13.</summary>
        public static readonly int VERSION0_UNDOCUMENTED_UNSUPPORTED = 0;
        /// <summary>See PDF Reference 1.4 Table 3.13.</summary>
        public static readonly int VERSION1_40_BIT_ALGORITHM = 1;
        /// <summary>See PDF Reference 1.4 Table 3.13.</summary>
        public static readonly int VERSION2_VARIABLE_LENGTH_ALGORITHM = 2;
        /// <summary>See PDF Reference 1.4 Table 3.13.</summary>
        public static readonly int VERSION3_UNPUBLISHED_ALGORITHM = 3;
        /// <summary>See PDF Reference 1.4 Table 3.13.</summary>
        public static readonly int VERSION4_SECURITY_HANDLER = 4;

        /// <summary>The default security handler.</summary>
        public static readonly string DEFAULT_NAME = "Standard";

        /// <summary>The default length for the encryption key.</summary>
        public static readonly int DEFAULT_LENGTH = 40;

        /// <summary>The default version, according to the PDF Reference.</summary>
        public static readonly int DEFAULT_VERSION = VERSION0_UNDOCUMENTED_UNSUPPORTED;

        private ISecurityHandler securityHandler;
        private PdfCryptFilter std;
        private PdfCryptFilter df;

        /// <summary>creates a new empty encryption Dictionary.</summary>
        /// <param name="context">File</param>
        public PdfEncryption(PdfDocument context)
            : base(context, new())
        { }

        /// <summary>creates a new encryption Dictionary from the low level Dictionary provided.</summary>
        /// <param name="baseObject">a PDF encryption Dictionary</param>
        public PdfEncryption(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        {
            securityHandler = SecurityHandlerFactory.INSTANCE.NewSecurityHandlerForFilter(Filter);
        }

        /// <summary>Returns the security handler specified in the Dictionary's Filter entry.</summary>
        /// <value> a security handler instance</value>
        public ISecurityHandler SecurityHandler
        {
            get
            {
                if (securityHandler == null)
                {
                    throw new IOException("No security handler for filter " + Filter);
                }
                return securityHandler;
            }
            set => securityHandler = value;// TODO set Filter (currently this is done by the security handlers)
        }

        /// <summary>Returns true if the security handler specified in the Dictionary's Filter is available.</summary>
        /// <value> true if the security handler is available </value> 
        public bool HasSecurityHandler
        {
            get => securityHandler == null;
        }

        /// <summary>Get the name of the filter.</summary>
        /// <value>The filter name contained in this encryption Dictionary.</value>>
        public string Filter
        {
            get => GetString(PdfName.Filter);
            set => SetName(PdfName.Filter, value);

        }

        /// <summary>Get the name of the subfilter.</summary>
        /// <value>The subfilter name contained in this encryption Dictionary.</value>
        public string SubFilter
        {
            get => GetString(PdfName.SubFilter);
            set => SetName(PdfName.SubFilter, value);
        }

        /// <summary> This will return the V entry of the encryption Dictionary.<br></br>
        /// See PDF Reference 1.4 Table 3.13.  <br></br>
        /// <b>Note: This value is used to decrypt the pdf document.  If you change this when
        /// the document is encrypted then decryption will fail!.</b>
        /// </summary>
        /// <value>The encryption version to use.</value>
        public int Version
        {
            get => GetInt(PdfName.V, 0);
            set => Set(PdfName.V, value);
        }


        /// <summary>
        /// This will return the Length entry of the encryption Dictionary.<br></br>
        /// The length in <b>bits</b> for the encryption algorithm.  This will return a multiple of 8.
        /// </summary>
        /// <value>The length in bits for the encryption algorithm</value>
        public int Length
        {
            get => GetInt(PdfName.Length, 0);
            set => Set(PdfName.Length, value);
        }

        /// <summary>
        /// This will return the R entry of the encryption Dictionary.<br></br>
        /// See PDF Reference 1.4 Table 3.14.  <br></br>
        /// <b>Note: This value is used to decrypt the pdf document.  If you change this when
        /// the document is encrypted then decryption will fail!.</b>
        ///</summary> 
        /// <value> The encryption revision to use. </value>
        public int Revision
        {
            get => GetInt(PdfName.R, DEFAULT_VERSION);
            set => Set(PdfName.R, value);
        }

        /// <summary>
        /// This will get the O entry in the standard encryption Dictionary.
        /// A 32 byte array or null if there is no owner key.
        /// </summary>
        public Memory<byte> OwnerKey
        {
            get => GetTextBytes(PdfName.O);
            set => Set(PdfName.O, value);
        }

        /// <summary>
        /// This will get the U entry in the standard encryption Dictionary.
        /// A 32 byte array or null if there is no user key.
        /// </summary>
        public Memory<byte> UserKey
        {
            get => GetTextBytes(PdfName.U);
            set => Set(PdfName.U, value);
        }

        /// <summary>
        /// This will get the OE entry in the standard encryption Dictionary.
        /// A 32 byte array or null if there is no owner encryption key.
        /// </summary>
        public Memory<byte> OwnerEncryptionKey
        {
            get => GetTextBytes(PdfName.OE);
            set => Set(PdfName.OE, value);
        }

        /// <summary>
        /// This will get the UE entry in the standard encryption Dictionary.
        /// A 32 byte array or null if there is no user encryption key.
        /// </summary>
        public Memory<byte> UserEncryptionKey
        {
            get => GetTextBytes(PdfName.UE);
            set => Set(PdfName.UE, value);
        }

        /// <summary>
        /// This will get the permissions bit mask.
        /// The permissions bit mask.
        /// </summary>
        public int Permissions
        {
            get => GetInt(PdfName.P, 0);
            set => Set(PdfName.P, value);
        }

        /// <summary>
        /// Will get the EncryptMetaData Dictionary info.
        /// true if EncryptMetaData is explicitly set to false (the default is true)
        /// </summary>
        public bool IsEncryptMetaData
        {
            // default is true (see 7.6.3.2 Standard Encryption Dictionary PDF 32000-1:2008)
            get => GetBool(PdfName.EncryptMetadata, true);
        }

        /// <summary>This will set the Recipients field of the Dictionary.This field contains an array 
        /// of string.</summary>
        /// <param name="recipients">the array of bytes arrays to put in the Recipients field.</param>
        public void SetRecipients(byte[][] recipients)
        {
            SetDirect(PdfName.Recipients, new PdfArrayImpl(recipients));
        }

        /// <summary>
        /// Returns the number of recipients contained in the Recipients field of the Dictionary.
        /// </summary>
        public int RecipientsLength
        {
            get => Get<PdfArray>(PdfName.Recipients).Count;
        }

        /// <summary>
        /// Returns the PdfString contained in the Recipients field at position i.
        /// </summary>
        /// <param name="i">the position in the Recipients field array.</param>
        /// <returns>a PdfString object containing information about the recipient number i.</returns>
        public PdfString GetRecipientStringAt(int i)
        {
            return Get<PdfArray>(PdfName.Recipients).Get<PdfString>(i);
        }

        /// <summary>
        /// Returns the standard crypt filter.
        /// </summary>
        public PdfCryptFilter StdCryptFilter
        {
            get => std ??= GetCryptFilterDictionary(PdfName.StdCF);
            set => SetCryptFilterDictionary(PdfName.StdCF, std = value);
        }

        /// <summary>
        /// Returns the default crypt filter(for public-key security handler).
        /// </summary>
        public PdfCryptFilter DefaultCryptFilter
        {
            get => df ??= GetCryptFilterDictionary(PdfName.DefaultCryptFilter);
            set => SetCryptFilterDictionary(PdfName.DefaultCryptFilter, df = value);
        }

        /// <summary>Returns the crypt filter with the given name.</summary>
        /// <param name="cryptFilterName">the name of the crypt filter</param>
        /// <returns>the crypt filter with the given name if available</returns>
        public PdfCryptFilter GetCryptFilterDictionary(PdfName cryptFilterName)
        {
            // See CF in "Table 20 – Entries common to all encryption dictionaries"            
            return Get<PdfDictionary>(PdfName.CF)?.Get<PdfCryptFilter>(cryptFilterName);
        }

        /// <summary>Sets the crypt filter with the given name.</summary>
        /// <param name="cryptFilterName">the name of the crypt filter</param>
        /// <param name="cryptFilterDictionary">the crypt filter to set</param>
        public void SetCryptFilterDictionary(PdfName cryptFilterName, PdfCryptFilter cryptFilterDictionary)
        {
            var cfDictionary = GetOrCreate<PdfDictionary>(PdfName.CF);
            //cfDictionary.setDirect(true); // PDFBOX-4436 direct obj needed for Adobe Reader on Android
            cfDictionary.SetDirect(cryptFilterName, cryptFilterDictionary);
        }

        /// <summary>
        /// Returns the name of the filter which is used for de/encrypting streams.
        /// Default value is "Identity".
        /// </summary>
        public PdfName StreamFilterName
        {
            get => Get(PdfName.StmF, PdfName.Identity);
            set => this[PdfName.StmF] = value;
        }

        /// <summary>
        /// Returns the name of the filter which is used for de/encrypting strings.
        /// Default value is "Identity".
        /// </summary>
        public PdfName StringFilterName
        {
            get => Get(PdfName.StrF, PdfName.Identity);
            set => this[PdfName.StrF] = value;
        }

        /// <summary>
        /// Get the Perms entry in the encryption Dictionary.
        /// A 16 byte array or null if there is no Perms entry.
        /// </summary>
        public Memory<byte> Perms
        {
            get => GetTextBytes(PdfName.Perms);
            set => this[PdfName.Perms] = new PdfString(value);
        }

        /// <summary>remove CF, StmF, and StrF entries.This is to be called if V is not 4 or 5.</summary>
        public void RemoveV45filters()
        {
            this[PdfName.CF] = null;
            this[PdfName.StmF] = null;
            this[PdfName.StrF] = null;
        }
    }
}