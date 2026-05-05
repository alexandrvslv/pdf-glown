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
using System.IO;
using System;
using System.Buffers;

namespace PdfGlown.Documents.Encryption
{

    /// <summary>
    /// An implementation of the RC4 stream cipher.
    /// @author Ben Litchfield
    /// </summary>
    internal class RC4Cipher
    {
        private readonly int[] salt;
        private int b;
        private int c;

        public RC4Cipher()
        {
            salt = new int[256];
        }

        /**
		 * This will reset the key to be used.
		 *
		 * @param key The RC4 key used during encryption.
		 */
        public void SetKey(ReadOnlySpan<byte> key)
        {
            b = 0;
            c = 0;

            if (key.Length < 1 || key.Length > 32)
            {
                throw new ArgumentException("number of bytes must be between 1 and 32");
            }
            for (int i = 0; i < salt.Length; i++)
            {
                salt[i] = i;
            }

            int keyIndex = 0;
            int saltIndex = 0;
            for (int i = 0; i < salt.Length; i++)
            {
                saltIndex = (key[keyIndex] + salt[i] + saltIndex) % 256;
                Swap(salt, i, saltIndex);
                keyIndex = (keyIndex + 1) % key.Length;
            }

        }

        /**
		 * This will ensure that the value for a byte &gt;=0.
		 *
		 * @param aByte The byte to test against.
		 *
		 * @return A value &gt;=0 and &lt; 256
		 */
        private static int FixByte(byte aByte)
        {
            return aByte < 0 ? 256 + aByte : aByte;
        }

        /**
		 * This will swap two values in an array.
		 *
		 * @param data The array to swap from.
		 * @param firstIndex The index of the first element to swap.
		 * @param secondIndex The index of the second element to swap.
		 */
        private static void Swap(int[] data, int firstIndex, int secondIndex)
        {
            int tmp = data[firstIndex];
            data[firstIndex] = data[secondIndex];
            data[secondIndex] = tmp;
        }

        /**
		 * This will encrypt and write the next byte.
		 *
		 * @param aByte The byte to encrypt.
		 * @param output The stream to write to.
		 *
		 * @throws IOException If there is an error writing to the output stream.
		 */
        public void Write(byte aByte, Stream output)
        {
            b = (b + 1) % 256;
            c = (salt[b] + c) % 256;
            Swap(salt, b, c);
            int saltIndex = (salt[b] + salt[c]) % 256;
            var buffer = (byte)(aByte ^ (byte)salt[saltIndex]);
            output.WriteByte(buffer);
        }

        /**
		 * This will encrypt and write the data.
		 *
		 * @param data The data to encrypt.
		 * @param output The stream to write to.
		 *
		 * @throws IOException If there is an error writing to the output stream.
		 */
        public void Write(ReadOnlySpan<byte> data, Stream output)
        {
            foreach (byte aData in data)
            {
                Write(aData, output);
            }
        }

        /**
		 * This will encrypt and write the data.
		 *
		 * @param data The data to encrypt.
		 * @param output The stream to write to.
		 *
		 * @throws IOException If there is an error writing to the output stream.
		 */
        public void Write(Stream data, Stream output)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(1024);
            int amountRead;
            while ((amountRead = data.Read(buffer, 0, buffer.Length)) > 0)
            {
                Write(buffer, 0, amountRead, output);
            }
            ArrayPool<byte>.Shared.Return(buffer);
        }

        /**
		 * This will encrypt and write the data.
		 *
		 * @param data The data to encrypt.
		 * @param offset The offset into the array to start reading data from.
		 * @param len The number of bytes to attempt to read.
		 * @param output The stream to write to.
		 *
		 * @throws IOException If there is an error writing to the output stream.
		 */
        public void Write(byte[] data, int offset, int len, Stream output)
        {
            for (int i = offset, count = offset + len; i < count; i++)
            {
                Write(data[i], output);
            }
        }
    }
}
