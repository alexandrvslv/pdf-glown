/* Copyright 2012 Mozilla Foundation
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;

namespace PdfGlown.Bytes.Filters
{
    public static class FiltersExtension
    {
        public static sbyte ReadInt8(this IInputStream data)
        {
            return (sbyte)((data.ReadUByte() << 24) >> 24);
        }

        public static sbyte ReadInt8(this byte[] data, int offset)
        {
            return (sbyte)((data[offset] << 24) >> 24);
        }

        public static sbyte ReadInt8(this ReadOnlySpan<byte> data, int offset)
        {
            return (sbyte)((data[offset] << 24) >> 24);
        }


        // Calculate the base 2 logarithm of the number `x`. This differs from the
        // native function in the sense that it returns the ceiling value and that it
        // returns 0 instead of `Infinity`/`NaN` for `x` values smaller than/equal to 0.
        public static int Log2(double x)
        {
            if (x <= 0)
            {
                return 0;
            }
            return (int)Math.Ceiling(Math.Log(x, 2D));
        }

        public static byte ToByte(double v)
        {
            return v > 255 ? (byte)255 : v < 0 ? (byte)0 : (byte)v;
        }

        public static byte ToByte(int v)
        {
            return v > 255 ? (byte)255 : v < 0 ? (byte)0 : (byte)v;
        }

        public static byte ToByte(uint v)
        {
            return v > 255 ? (byte)255 : (byte)v;
        }
    }
}
