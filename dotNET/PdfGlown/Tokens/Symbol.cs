/*
  Copyright 2010 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)

  This file should be part of the source code distribution of "PDF Glown library"
  (the Program): see the accompanying README files for more info.

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

using System.Collections.Generic;

namespace PdfGlown.Tokens
{
    /// <summary>PDF symbols.</summary>
    public static class Symbol
    {
        public const char CapitalR = 'R';
        public const char CarriageReturn = '\r';
        public const char CloseAngleBracket = '>';
        public const char CloseRoundBracket = ')';
        public const char CloseSquareBracket = ']';
        public const char LineFeed = '\n';
        public const char OpenAngleBracket = '<';
        public const char OpenRoundBracket = '(';
        public const char OpenSquareBracket = '[';
        public const char Percent = '%';
        public const char Slash = '/';
        public const char Space = ' ';
        public const char HorizontalTabulation = '\t';
        public const char FormFeed = '\f';
        public const char Null = '\u0000';

        private static readonly HashSet<int> whitespaces = new(6) 
        {
            Space, 
            Null, 
            HorizontalTabulation, 
            LineFeed, 
            FormFeed, 
            CarriageReturn
        };

        private static readonly HashSet<int> delimeters = new(8)
        {
            OpenRoundBracket,
            CloseRoundBracket,
            OpenAngleBracket,
            CloseAngleBracket,
            OpenSquareBracket,
            CloseSquareBracket,
            Slash,
            Percent
        };

        private static readonly HashSet<int> delimetersAndWhitespaces = new(14)
        {
            OpenRoundBracket,
            CloseRoundBracket,
            OpenAngleBracket,
            CloseAngleBracket,
            OpenSquareBracket,
            CloseSquareBracket,
            Slash,
            Percent,
            Space,
            Null,
            HorizontalTabulation,
            LineFeed,
            FormFeed,
            CarriageReturn
        };



        /// <summary>Evaluate whether a character is a delimiter.</summary>
        public static bool IsDelimiter(int c) => delimeters.Contains(c);

        /// <summary>Evaluate whether a character is an EOL marker.</summary>
        public static bool IsEOL(int c) => (c == 10 || c == 13);

        /// <summary>Evaluate whether a character is a white-space.</summary>
        public static bool IsWhitespace(int c)// => whitespaces.Contains(c);
            => c == 32 || c == 10 || c == 13 || c == 0 || c == 9 || c == 12;

        public static bool IsDelimiterOrWhitespace(int c) => delimetersAndWhitespaces.Contains(c);

    }
}