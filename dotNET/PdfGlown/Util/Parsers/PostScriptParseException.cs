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

using System;

namespace PdfGlown.Util.Parsers
{
    /// <summary>Exception thrown in case of unexpected condition while parsing PostScript-based data.
    /// </summary>
    public class PostScriptParseException : ParseException
    {
        private readonly object token;
        private readonly TokenTypeEnum? tokenType;

        public PostScriptParseException(string message) : this(message, -1)
        { }

        public PostScriptParseException(string message, PostScriptParser parser)
            : this(message, null, parser)
        { }

        public PostScriptParseException(string message, long position)
            : this(message, position, null, null)
        { }

        public PostScriptParseException(string message, long position, object token, TokenTypeEnum? tokenType)
            : this(message, null, position, token, tokenType)
        { }

        public PostScriptParseException(Exception cause)
            : this(null, cause)
        { }

        public PostScriptParseException(string message, Exception cause)
            : this(message, cause, -1)
        { }

        public PostScriptParseException(string message, Exception cause, PostScriptParser parser)
            : this(message, cause, parser.Position, parser.Token, parser.TokenType)
        { }

        public PostScriptParseException(string message, Exception cause, long position)
            : this(message, cause, position, null, null)
        { }

        public PostScriptParseException(string message, Exception cause, long position, object token, TokenTypeEnum? tokenType)
            : base(message, cause, position)
        {
            this.token = token;
            this.tokenType = tokenType;
        }

        /// <summary>Gets the token on which the exception happened.</summary>
        public object Token => token;

        /// <summary>Gets the type of the token on which the exception happened.</summary>
        public TokenTypeEnum? TokenType => tokenType;
    }
}