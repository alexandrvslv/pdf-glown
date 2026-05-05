/*
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */


using System;
using System.Text;

namespace PdfGlown.Documents.Encryption
{

    /**
     * Copied from https://github.com/tombentley/saslprep/blob/master/src/main/java/SaslPrep.java on
     * 30.5.2019, commit 2e30daa.
     *
     * @author Tom Bentley
     */
    internal class SaslPrep
    {

        private SaslPrep()
        {
        }

        /**
		 * Return the {@code SASLPrep}-canonicalised version of the given {@code str} for use as a query
		 * string. This implements the {@code SASLPrep} algorithm defined in
		 * <a href="https://tools.ietf.org/html/rfc4013">RFC 4013</a>.
		 *
		 * @param str The string to canonicalise.
		 * @return The canonicalised string.
		 * @throws IllegalArgumentException if the string contained prohibited codepoints, or broke the
		 * requirements for bidirectional character handling.
		 * @see <a href="https://tools.ietf.org/html/rfc3454#section-7">RFC 3454, Section 7</a> for
		 * discussion of what a query string is.
		 */
        public static string SaslPrepQuery(string str)
        {
            return SaslPrepEnv(str, true);
        }

        /**
		 * Return the {@code SASLPrep}-canonicalised version of the given
		 * @code str} for use as a stored string. This implements the {@code SASLPrep} algorithm defined
		 * in
		 * <a href="https://tools.ietf.org/html/rfc4013">RFC 4013</a>.
		 *
		 * @param str The string to canonicalise.
		 * @return The canonicalised string.
		 * @throws IllegalArgumentException if the string contained prohibited codepoints, or broke the
		 * requirements for bidirectional character handling.
		 * @see <a href="https://tools.ietf.org/html/rfc3454#section-7">RFC 3454, Section 7</a> for
		 * discussion of what a stored string is.
		 */
        public static string SaslPrepStored(string str)
        {
            return SaslPrepEnv(str, false);
        }

        private static string SaslPrepEnv(string str, bool allowUnassigned)
        {
            char[] chars = str.ToCharArray();

            // 1. Map
            // non-ASCII space chars mapped to space
            for (int ii = 0; ii < str.Length; ii++)
            {
                char ch = str[ii];
                if (NonAsciiSpace(ch))
                {
                    chars[ii] = ' ';
                }
            }

            int length = 0;
            for (int ii = 0; ii < str.Length; ii++)
            {
                char ch = chars[ii];
                if (!MappedToNothing(ch))
                {
                    chars[length++] = ch;
                }
            }

            // 2. Normalize
            string normalized = new string(chars, 0, length);
            normalized = normalized.Normalize(NormalizationForm.FormKC);

            bool containsRandALCat = false;
            bool containsLCat = false;
            bool initialRandALCat = false;
            int i = 0;
            while (i < normalized.Length)
            {
                int codepoint = char.ConvertToUtf32(normalized, i);
                var cat = char.GetUnicodeCategory(normalized, i);
                // 3. Prohibit
                if (Prohibited(codepoint))
                {
                    throw new ArgumentException($"Prohibited character 'U+{codepoint:X4}' at position {i}");
                }

                // 4. Check bidi

                bool isRandALcat = HasRandALcat(codepoint);
                containsRandALCat |= isRandALcat;
                containsLCat |= !isRandALcat;

                initialRandALCat |= i == 0 && isRandALcat;
                if (!allowUnassigned && cat == System.Globalization.UnicodeCategory.OtherNotAssigned)//!Character.isDefined(codepoint))
                {
                    throw new ArgumentException("Character at position " + i + " is unassigned");
                }

                i += char.IsSurrogatePair(normalized, i) ? 2 : 1;

                if (initialRandALCat && i >= normalized.Length && !isRandALcat)
                {
                    throw new ArgumentException("First character is RandALCat, but last character is not");
                }
            }
            if (containsRandALCat && containsLCat)
            {
                throw new ArgumentException("Contains both RandALCat characters and LCat characters");
            }
            return normalized;
        }

        static bool HasRandALcat(int c)
        {
            int hasRandALCat = 0;
            if (c >= 0x5BE && c <= 0x10B7F)
            {
                if (c <= 0x85E)
                {
                    if (c == 0x5BE) hasRandALCat = 1;
                    else if (c == 0x5C0) hasRandALCat = 1;
                    else if (c == 0x5C3) hasRandALCat = 1;
                    else if (c == 0x5C6) hasRandALCat = 1;
                    else if (0x5D0 <= c && c <= 0x5EA) hasRandALCat = 1;
                    else if (0x5F0 <= c && c <= 0x5F4) hasRandALCat = 1;
                    else if (c == 0x608) hasRandALCat = 1;
                    else if (c == 0x60B) hasRandALCat = 1;
                    else if (c == 0x60D) hasRandALCat = 1;
                    else if (c == 0x61B) hasRandALCat = 1;
                    else if (0x61E <= c && c <= 0x64A) hasRandALCat = 1;
                    else if (0x66D <= c && c <= 0x66F) hasRandALCat = 1;
                    else if (0x671 <= c && c <= 0x6D5) hasRandALCat = 1;
                    else if (0x6E5 <= c && c <= 0x6E6) hasRandALCat = 1;
                    else if (0x6EE <= c && c <= 0x6EF) hasRandALCat = 1;
                    else if (0x6FA <= c && c <= 0x70D) hasRandALCat = 1;
                    else if (c == 0x710) hasRandALCat = 1;
                    else if (0x712 <= c && c <= 0x72F) hasRandALCat = 1;
                    else if (0x74D <= c && c <= 0x7A5) hasRandALCat = 1;
                    else if (c == 0x7B1) hasRandALCat = 1;
                    else if (0x7C0 <= c && c <= 0x7EA) hasRandALCat = 1;
                    else if (0x7F4 <= c && c <= 0x7F5) hasRandALCat = 1;
                    else if (c == 0x7FA) hasRandALCat = 1;
                    else if (0x800 <= c && c <= 0x815) hasRandALCat = 1;
                    else if (c == 0x81A) hasRandALCat = 1;
                    else if (c == 0x824) hasRandALCat = 1;
                    else if (c == 0x828) hasRandALCat = 1;
                    else if (0x830 <= c && c <= 0x83E) hasRandALCat = 1;
                    else if (0x840 <= c && c <= 0x858) hasRandALCat = 1;
                    else if (c == 0x85E) hasRandALCat = 1;
                }
                else if (c == 0x200F) hasRandALCat = 1;
                else if (c >= 0xFB1D)
                {
                    if (c == 0xFB1D) hasRandALCat = 1;
                    else if (0xFB1F <= c && c <= 0xFB28) hasRandALCat = 1;
                    else if (0xFB2A <= c && c <= 0xFB36) hasRandALCat = 1;
                    else if (0xFB38 <= c && c <= 0xFB3C) hasRandALCat = 1;
                    else if (c == 0xFB3E) hasRandALCat = 1;
                    else if (0xFB40 <= c && c <= 0xFB41) hasRandALCat = 1;
                    else if (0xFB43 <= c && c <= 0xFB44) hasRandALCat = 1;
                    else if (0xFB46 <= c && c <= 0xFBC1) hasRandALCat = 1;
                    else if (0xFBD3 <= c && c <= 0xFD3D) hasRandALCat = 1;
                    else if (0xFD50 <= c && c <= 0xFD8F) hasRandALCat = 1;
                    else if (0xFD92 <= c && c <= 0xFDC7) hasRandALCat = 1;
                    else if (0xFDF0 <= c && c <= 0xFDFC) hasRandALCat = 1;
                    else if (0xFE70 <= c && c <= 0xFE74) hasRandALCat = 1;
                    else if (0xFE76 <= c && c <= 0xFEFC) hasRandALCat = 1;
                    else if (0x10800 <= c && c <= 0x10805) hasRandALCat = 1;
                    else if (c == 0x10808) hasRandALCat = 1;
                    else if (0x1080A <= c && c <= 0x10835) hasRandALCat = 1;
                    else if (0x10837 <= c && c <= 0x10838) hasRandALCat = 1;
                    else if (c == 0x1083C) hasRandALCat = 1;
                    else if (0x1083F <= c && c <= 0x10855) hasRandALCat = 1;
                    else if (0x10857 <= c && c <= 0x1085F) hasRandALCat = 1;
                    else if (0x10900 <= c && c <= 0x1091B) hasRandALCat = 1;
                    else if (0x10920 <= c && c <= 0x10939) hasRandALCat = 1;
                    else if (c == 0x1093F) hasRandALCat = 1;
                    else if (c == 0x10A00) hasRandALCat = 1;
                    else if (0x10A10 <= c && c <= 0x10A13) hasRandALCat = 1;
                    else if (0x10A15 <= c && c <= 0x10A17) hasRandALCat = 1;
                    else if (0x10A19 <= c && c <= 0x10A33) hasRandALCat = 1;
                    else if (0x10A40 <= c && c <= 0x10A47) hasRandALCat = 1;
                    else if (0x10A50 <= c && c <= 0x10A58) hasRandALCat = 1;
                    else if (0x10A60 <= c && c <= 0x10A7F) hasRandALCat = 1;
                    else if (0x10B00 <= c && c <= 0x10B35) hasRandALCat = 1;
                    else if (0x10B40 <= c && c <= 0x10B55) hasRandALCat = 1;
                    else if (0x10B58 <= c && c <= 0x10B72) hasRandALCat = 1;
                    else if (0x10B78 <= c && c <= 0x10B7F) hasRandALCat = 1;
                }
            }

            return hasRandALCat == 1;
        }

        /**
		 * Return true if the given {@code codepoint} is a prohibited character
		 * as defined by
		 * <a href="https://tools.ietf.org/html/rfc4013#section-2.3">RFC 4013,
		 * Section 2.3</a>.
		 */
        static bool Prohibited(int codepoint)
        {
            return NonAsciiSpace((char)codepoint)
                    || AsciiControl((char)codepoint)
                    || NonAsciiControl(codepoint)
                    || PrivateUse(codepoint)
                    || NonCharacterCodePoint(codepoint)
                    || SurrogateCodePoint(codepoint)
                    || InappropriateForPlainText(codepoint)
                    || InappropriateForCanonical(codepoint)
                    || ChangeDisplayProperties(codepoint)
                    || Tagging(codepoint);
        }

        /**
		 * Return true if the given {@code codepoint} is a tagging character
		 * as defined by
		 * <a href="https://tools.ietf.org/html/rfc3454#appendix-C.9">RFC 3454,
		 * Appendix C.9</a>.
		 */
        private static bool Tagging(int codepoint)
        {
            return codepoint == 0xE0001
                    || 0xE0020 <= codepoint && codepoint <= 0xE007F;
        }

        /**
		 * Return true if the given {@code codepoint} is change display properties
		 * or deprecated characters as defined by
		 * <a href="https://tools.ietf.org/html/rfc3454#appendix-C.8">RFC 3454,
		 * Appendix C.8</a>.
		 */
        private static bool ChangeDisplayProperties(int codepoint)
        {
            return codepoint == 0x0340
                    || codepoint == 0x0341
                    || codepoint == 0x200E
                    || codepoint == 0x200F
                    || codepoint == 0x202A
                    || codepoint == 0x202B
                    || codepoint == 0x202C
                    || codepoint == 0x202D
                    || codepoint == 0x202E
                    || codepoint == 0x206A
                    || codepoint == 0x206B
                    || codepoint == 0x206C
                    || codepoint == 0x206D
                    || codepoint == 0x206E
                    || codepoint == 0x206F
                    ;
        }

        /**
		 * Return true if the given {@code codepoint} is inappropriate for
		 * canonical representation characters as defined by
		 * <a href="https://tools.ietf.org/html/rfc3454#appendix-C.7">RFC 3454,
		 * Appendix C.7</a>.
		 */
        private static bool InappropriateForCanonical(int codepoint)
        {
            return 0x2FF0 <= codepoint && codepoint <= 0x2FFB;
        }

        /**
		 * Return true if the given {@code codepoint} is inappropriate for plain
		 * text characters as defined by
		 * <a href="https://tools.ietf.org/html/rfc3454#appendix-C.6">RFC 3454,
		 * Appendix C.6</a>.
		 */
        private static bool InappropriateForPlainText(int codepoint)
        {
            return codepoint == 0xFFF9
                    || codepoint == 0xFFFA
                    || codepoint == 0xFFFB
                    || codepoint == 0xFFFC
                    || codepoint == 0xFFFD
                    ;
        }

        /**
		 * Return true if the given {@code codepoint} is a surrogate
		 * code point as defined by
		 * <a href="https://tools.ietf.org/html/rfc3454#appendix-C.5">RFC 3454,
		 * Appendix C.5</a>.
		 */
        private static bool SurrogateCodePoint(int codepoint)
        {
            return 0xD800 <= codepoint && codepoint <= 0xDFFF;
        }

        /**
		 * Return true if the given {@code codepoint} is a non-character
		 * code point as defined by
		 * <a href="https://tools.ietf.org/html/rfc3454#appendix-C.4">RFC 3454,
		 * Appendix C.4</a>.
		 */
        private static bool NonCharacterCodePoint(int codepoint)
        {
            return 0xFDD0 <= codepoint && codepoint <= 0xFDEF
                    || 0xFFFE <= codepoint && codepoint <= 0xFFFF
                    || 0x1FFFE <= codepoint && codepoint <= 0x1FFFF
                    || 0x2FFFE <= codepoint && codepoint <= 0x2FFFF
                    || 0x3FFFE <= codepoint && codepoint <= 0x3FFFF
                    || 0x4FFFE <= codepoint && codepoint <= 0x4FFFF
                    || 0x5FFFE <= codepoint && codepoint <= 0x5FFFF
                    || 0x6FFFE <= codepoint && codepoint <= 0x6FFFF
                    || 0x7FFFE <= codepoint && codepoint <= 0x7FFFF
                    || 0x8FFFE <= codepoint && codepoint <= 0x8FFFF
                    || 0x9FFFE <= codepoint && codepoint <= 0x9FFFF
                    || 0xAFFFE <= codepoint && codepoint <= 0xAFFFF
                    || 0xBFFFE <= codepoint && codepoint <= 0xBFFFF
                    || 0xCFFFE <= codepoint && codepoint <= 0xCFFFF
                    || 0xDFFFE <= codepoint && codepoint <= 0xDFFFF
                    || 0xEFFFE <= codepoint && codepoint <= 0xEFFFF
                    || 0xFFFFE <= codepoint && codepoint <= 0xFFFFF
                    || 0x10FFFE <= codepoint && codepoint <= 0x10FFFF
                    ;
        }

        /**
		 * Return true if the given {@code codepoint} is a private use character
		 * as defined by <a href="https://tools.ietf.org/html/rfc3454#appendix-C.3">RFC 3454,
		 * Appendix C.3</a>.
		 */
        private static bool PrivateUse(int codepoint)
        {
            return 0xE000 <= codepoint && codepoint <= 0xF8FF
                    || 0xF0000 <= codepoint && codepoint <= 0xFFFFD
                    || 0x100000 <= codepoint && codepoint <= 0x10FFFD;
        }

        /**
		 * Return true if the given {@code ch} is a non-ASCII control character
		 * as defined by <a href="https://tools.ietf.org/html/rfc3454#appendix-C.2.2">RFC 3454,
		 * Appendix C.2.2</a>.
		 */
        private static bool NonAsciiControl(int codepoint)
        {
            return 0x0080 <= codepoint && codepoint <= 0x009F
                    || codepoint == 0x06DD
                    || codepoint == 0x070F
                    || codepoint == 0x180E
                    || codepoint == 0x200C
                    || codepoint == 0x200D
                    || codepoint == 0x2028
                    || codepoint == 0x2029
                    || codepoint == 0x2060
                    || codepoint == 0x2061
                    || codepoint == 0x2062
                    || codepoint == 0x2063
                    || 0x206A <= codepoint && codepoint <= 0x206F
                    || codepoint == 0xFEFF
                    || 0xFFF9 <= codepoint && codepoint <= 0xFFFC
                    || 0x1D173 <= codepoint && codepoint <= 0x1D17A;
        }

        /**
		 * Return true if the given {@code ch} is an ASCII control character
		 * as defined by <a href="https://tools.ietf.org/html/rfc3454#appendix-C.2.1">RFC 3454,
		 * Appendix C.2.1</a>.
		 */
        private static bool AsciiControl(char ch)
        {
            return '\u0000' <= ch && ch <= '\u001F' || ch == '\u007F';
        }

        /**
		 * Return true if the given {@code ch} is a non-ASCII space character
		 * as defined by <a href="https://tools.ietf.org/html/rfc3454#appendix-C.1.2">RFC 3454,
		 * Appendix C.1.2</a>.
		 */
        private static bool NonAsciiSpace(char ch)
        {
            return ch == '\u00A0'
                    || ch == '\u1680'
                    || '\u2000' <= ch && ch <= '\u200B'
                    || ch == '\u202F'
                    || ch == '\u205F'
                    || ch == '\u3000';
        }

        /**
		 * Return true if the given {@code ch} is a "commonly mapped to nothing" character
		 * as defined by <a href="https://tools.ietf.org/html/rfc3454#appendix-B.1">RFC 3454,
		 * Appendix B.1</a>.
		 */
        private static bool MappedToNothing(char ch)
        {
            return ch == '\u00AD'
                    || ch == '\u034F'
                    || ch == '\u1806'
                    || ch == '\u180B'
                    || ch == '\u180C'
                    || ch == '\u180D'
                    || ch == '\u200B'
                    || ch == '\u200C'
                    || ch == '\u200D'
                    || ch == '\u2060'
                    || '\uFE00' <= ch && ch <= '\uFE0F'
                    || ch == '\uFEFF';
        }
    }
}