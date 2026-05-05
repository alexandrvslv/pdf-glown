/*
  Copyright 2015 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)

  This file should be part of the source code distribution of "PDF Glown library" (the
  Program): see the accompanying README files for more info.

  This Program is free software; you can redistribute it and/or modify it under the terms
  of the GNU Lesser General Public License as published by the Free Software Foundation;
  either version 3 of the License, or (at your option) any later version.

  This Program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY,
  either expressed or implied; without even the implied warranty of MERCHANTABILITY or
  FITNESS FOR A PARTICULAR PURPOSE. See the License for more details.

  You should have received a copy of the GNU Lesser General Public License along with this
  Program (see README files); if not, go to the GNU website (http://www.gnu.org/licenses/).

  Redistribution and use, with or without modification, are permitted provided that such
  redistributions retain the above copyright notice, license and disclaimer, along with
  this list of conditions.
*/

using Org.BouncyCastle.Crypto;
using System;
using System.IO;
using System.Security.Cryptography;

namespace PdfGlown.Util.IO
{
    /// <summary>IO utilities.</summary>
    public static class IOUtils
    {
        public static bool Exists(string path)
        { return Directory.Exists(path) || File.Exists(path); }

        public static void Update(this HashAlgorithm digest, byte oneByte)
        {
            Update(digest, new byte[] { oneByte });
        }

        public static void Update(this HashAlgorithm digest, byte[] bytes)
        {
            Update(digest, bytes, 0, bytes.Length);
        }

        public static void Update(this HashAlgorithm digest, byte[] bytes, int offcet, int count)
        {
            digest.TransformBlock(bytes, offcet, count, null, 0);
        }

        public static void Update(this IncrementalHash digest, byte oneByte)
        {
            Update(digest, new byte[] { oneByte });
        }

        public static void Update(this IncrementalHash digest, byte[] bytes)
        {
            Update(digest, bytes, 0, bytes.Length);
        }

        public static void Update(this IncrementalHash digest, byte[] bytes, int offcet, int count)
        {
            digest.AppendData(bytes, offcet, count);
        }

        public static void Update(this IncrementalHash digest, ReadOnlySpan<byte> bytes)
        {
            digest.AppendData(bytes);
        }

        public static void Update(this IDigest digest, byte[] bytes)
        {
            Update(digest, bytes, 0, bytes.Length);
        }

        public static void Update(this IDigest digest, byte[] bytes, int offcet, int count)
        {
            digest.BlockUpdate(bytes, offcet, count);
        }

        public static void Update(this IDigest digest, ReadOnlySpan<byte> bytes)
        {
            digest.BlockUpdate(bytes);
        }

        public static byte[] Digest(this HashAlgorithm digest)
        {
            return Digest(digest, Array.Empty<byte>());
        }

        public static byte[] Digest(this HashAlgorithm digest, byte[] bytes)
        {
            return Digest(digest, bytes, 0, bytes.Length);
        }

        public static byte[] Digest(this HashAlgorithm digest, byte[] bytes, int offcet, int count)
        {
            digest.TransformFinalBlock(bytes, offcet, count);
            return digest.Hash;
        }

        public static byte[] Digest(this IncrementalHash digest)
        {
            return digest.GetHashAndReset();
        }

        public static byte[] Digest(this IncrementalHash digest, byte[] bytes)
        {
            return Digest(digest, bytes, 0, bytes.Length);
        }

        public static byte[] Digest(this IncrementalHash digest, byte[] bytes, int offcet, int count)
        {
            digest.Update(bytes, offcet, count);
            return digest.GetHashAndReset();
        }

        public static byte[] Digest(this IncrementalHash digest, ReadOnlySpan<byte> bytes)
        {
            digest.Update(bytes);
            return digest.GetHashAndReset();
        }

        public static byte[] Digest(this IDigest digest, byte[] bytes, int offset, int count)
        {
            var result = new byte[digest.GetDigestSize()];
            digest.BlockUpdate(bytes, offset, count);
            digest.DoFinal(result, 0);
            return result;
        }

        public static byte[] Digest(this IDigest digest, ReadOnlySpan<byte> bytes)
        {
            var result = new byte[digest.GetDigestSize()];
            digest.BlockUpdate(bytes);
            digest.DoFinal(result, 0);
            return result;
        }

        public static byte[] Digest(this IDigest digest)
        {
            var result = new byte[digest.GetDigestSize()];
            digest.DoFinal(result, 0);
            return result;
        }

        public static void Dispose(this IDigest digest)
        {
            //digest.Finish();
        }

        public static byte[] DoFinal(this ICryptoTransform transform, byte[] bytes)
        {
            return transform.TransformFinalBlock(bytes, 0, bytes.Length);
        }
    }
}

