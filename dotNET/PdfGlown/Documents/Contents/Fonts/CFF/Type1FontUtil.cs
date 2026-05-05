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

using System;

namespace PdfGlown.Documents.Contents.Fonts.CCF
{
	/**
     * This class contains some helper methods handling Type1-Fonts.
     *
     * @author Villu Ruusmann
     */
	public sealed class Type1FontUtil
	{

		public Type1FontUtil()
		{
		}

		/**
		 * Encrypt eexec.
		 * @param buffer the given data
		 * @return the encrypted data
		 */
		public static byte[] eexecEncrypt(byte[] buffer)
		{
			return encrypt(buffer, 55665, 4);
		}

		/**
		 * Encrypt charstring.
		 * @param buffer the given data
		 * @param n blocksize?
		 * @return the encrypted data
		 */
		public static byte[] charstringEncrypt(byte[] buffer, int n)
		{
			return encrypt(buffer, 4330, n);
		}

		private static byte[] encrypt(byte[] plaintextBytes, int r, int n)
		{
			byte[] buffer = new byte[plaintextBytes.Length + n];

			Array.Copy(plaintextBytes, 0, buffer, n, buffer.Length - n);

			int c1 = 52845;
			int c2 = 22719;

			byte[] ciphertextBytes = new byte[buffer.Length];

			for (int i = 0; i < buffer.Length; i++)
			{
				int plain = buffer[i] & 0xff;
				int cipher = plain ^ r >> 8;

				ciphertextBytes[i] = (byte)cipher;

				r = (cipher + r) * c1 + c2 & 0xffff;
			}

			return ciphertextBytes;
		}

		/**
		 * Decrypt eexec.
		 * @param buffer the given encrypted data
		 * @return the decrypted data
		 */
		public static byte[] eexecDecrypt(byte[] buffer)
		{
			return decrypt(buffer, 55665, 4);
		}

		/**
		 * Decrypt charstring.
		 * @param buffer the given encrypted data
		 * @param n blocksize?
		 * @return the decrypted data
		 */
		public static byte[] charstringDecrypt(byte[] buffer, int n)
		{
			return decrypt(buffer, 4330, n);
		}

		private static byte[] decrypt(byte[] ciphertextBytes, int r, int n)
		{
			byte[] buffer = new byte[ciphertextBytes.Length];

			int c1 = 52845;
			int c2 = 22719;

			for (int i = 0; i < ciphertextBytes.Length; i++)
			{
				int cipher = ciphertextBytes[i] & 0xff;
				int plain = cipher ^ r >> 8;

				buffer[i] = (byte)plain;

				r = (cipher + r) * c1 + c2 & 0xffff;
			}

			byte[] plaintextBytes = new byte[ciphertextBytes.Length - n];
			Array.Copy(buffer, n, plaintextBytes, 0, plaintextBytes.Length);

			return plaintextBytes;
		}
	}
}