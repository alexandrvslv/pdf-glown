/*
  Copyright 2009-2015 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfGlown.Bytes;
using PdfGlown.Objects;
using PdfGlown.Tokens;
using PdfGlown.Util;
using PdfGlown.Util.Math;
using PdfGlown.Util.Parsers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace PdfGlown.Documents.Contents.Fonts
{
    ///<summary>CMap parser [PDF:1.6:5.6.4;CMAP].</summary>
    internal sealed class CMapParser : PostScriptParser
    {
        private static readonly string BeginCodeSpaceRangeOperator = "begincodespacerange";
        private static readonly string BeginBaseFontCharOperator = "beginbfchar";
        private static readonly string BeginBaseFontRangeOperator = "beginbfrange";
        private static readonly string BeginCIDCharOperator = "begincidchar";
        private static readonly string BeginCIDRangeOperator = "begincidrange";
        private static readonly string DefOperator = "def";
        private static readonly string UseCMapOperator = "usecmap";
        private static readonly string EndCMapOperator = "endcmap";

        private static readonly string CMapName = PdfName.CMapName.StringValue;
        private static readonly string CMapType = PdfName.CMapType.StringValue;
        private static readonly string Registry = PdfName.Registry.StringValue;
        private static readonly string Ordering = PdfName.Ordering.StringValue;
        private static readonly string WMode = PdfName.WMode.StringValue;


        public CMapParser(Stream stream) : this((IInputStream)new StreamContainer(stream))
        { }

        public CMapParser(IInputStream stream) : base(stream)
        { }

        ///<summary>Parses the character-code-to-unicode mapping [PDF:1.6:5.9.1].</summary>
        public CMap Parse()
        {
            Stream.Seek(0);
            var codes = new CMap();
            {
                IList<object> operands = new List<object>();

                while (MoveNext())
                {
                    switch (TokenType)
                    {
                        case TokenTypeEnum.Keyword:
                            {
                                var @operator = CharsToken;
                                if (@operator.Equals(UseCMapOperator, StringComparison.Ordinal))
                                {
                                    var useCMap = CMap.Get((string)operands[0]);
                                    codes.UseCmap(useCMap);
                                }
                                else if (operands.FirstOrDefault() is int)
                                {
                                    if (@operator.Equals(BeginCodeSpaceRangeOperator, StringComparison.Ordinal))
                                    {
                                        ParseCodeSpaceRange(codes, operands);
                                    }
                                    else if (@operator.Equals(BeginBaseFontCharOperator, StringComparison.Ordinal))
                                    {
                                        ParseBFChar(codes, operands);
                                    }
                                    else if (@operator.Equals(BeginCIDCharOperator, StringComparison.Ordinal))
                                    {
                                        ParseCIDChar(codes, operands);
                                    }
                                    else if (@operator.Equals(BeginBaseFontRangeOperator, StringComparison.Ordinal))
                                    {
                                        ParseBFRange(codes, operands);
                                    }
                                    else if (@operator.Equals(BeginCIDRangeOperator, StringComparison.Ordinal))
                                    {
                                        ParseCIDRange(codes, operands);
                                    }
                                }
                                else if (@operator.Equals(DefOperator, StringComparison.Ordinal) && operands.Count != 0)
                                {
                                    if (CMapName.Equals((string)operands[0], StringComparison.Ordinal))
                                    {
                                        codes.CMapName = (string)operands[1];
                                    }
                                    else if (CMapType.Equals((string)operands[0], StringComparison.Ordinal))
                                    {
                                        codes.CMapType = (int)operands[1];
                                    }
                                    else if (Registry.Equals((string)operands[0], StringComparison.Ordinal))
                                    {
                                        codes.Registry = (string)operands[1];
                                    }
                                    else if (Ordering.Equals((string)operands[0], StringComparison.Ordinal))
                                    {
                                        codes.Ordering = (string)operands[1];
                                    }
                                    else if (WMode.Equals((string)operands[0], StringComparison.Ordinal))
                                    {
                                        codes.WMode = (int)operands[1];
                                    }
                                }
                                else if (@operator.Equals(EndCMapOperator, StringComparison.Ordinal))
                                {
                                    return codes;
                                }
                                operands.Clear();
                                break;
                            }
                        case TokenTypeEnum.ArrayBegin:
                            // Skip.
                            while (MoveNext())
                            {
                                if (TokenType == TokenTypeEnum.ArrayEnd)
                                    break;
                            }
                            break;
                        case TokenTypeEnum.DictionaryBegin:
                            // Skip.
                            while (MoveNext())
                            {
                                if (TokenType == TokenTypeEnum.DictionaryEnd)
                                    break;
                            }
                            break;
                        case TokenTypeEnum.Comment:
                            // Skip.
                            break;
                        case TokenTypeEnum.Literal:
                            var mSpan = BytesToken;
                            operands.Add(mSpan.Length > 0
                                    ? Charset.ISO88591.GetString(mSpan)
                                    : string.Empty);
                            break;
                        case TokenTypeEnum.Hex:
                            var hSpan = BytesToken;
                            operands.Add(hSpan.Length > 0
                                    ? ConvertUtils.ByteArrayToHex(hSpan)
                                    : string.Empty);
                            break;
                        case TokenTypeEnum.Name:
                            operands.Add(CharsToken.ToString());
                            break;
                        default:
                            operands.Add(Token);
                            break;
                    }
                }
            }
            return codes;
        }

        private void ParseCIDRange(CMap codes, IList<object> operands)
        {
            for (int itemIndex = 0, itemCount = (int)operands[0]; itemIndex < itemCount; itemIndex++)
            {
                // 1. Beginning input code.
                MoveNext();
                var beginInputCode = ParseInputCode();
                int beginInput = StreamExtensions.ReadIntOffset(beginInputCode);
                // 2. Ending input code.
                MoveNext();
                var endInputCode = ParseInputCode();
                //int entInput = ConvertUtils.ReadIntByLength(endInputCode);
                // 3. Character codes.
                MoveNext();
                var mappedCode = ParseUnicode();

                if (beginInputCode.Length == endInputCode.Length)
                {
                    // some CMaps are using CID ranges to map single values
                    if (beginInputCode.SequenceEqual(endInputCode))
                    {
                        codes.AddCIDMapping(beginInputCode, mappedCode);
                    }
                    else
                    {
                        codes.AddCIDRange(beginInputCode, endInputCode, mappedCode);
                    }
                }
                else
                {
                    throw new IOException("Error : ~cidrange values must not have different byte lengths");
                }
            }
        }

        private void ParseBFRange(CMap codes, IList<object> operands)
        {
            //NOTE: The first and second elements in each line are the beginning and
            //ending valid input codes for the template font; the third element is
            //the beginning character code for the range.
            for (int itemIndex = 0, itemCount = (int)operands[0]; itemIndex < itemCount; itemIndex++)
            {
                // 1. Beginning input code.
                MoveNext();
                var beginInputCode = ParseInputCode();
                int beginInput = StreamExtensions.ReadIntOffset(beginInputCode);
                // 2. Ending input code.
                MoveNext();
                var endInputCode = ParseInputCode();
                int entInput = StreamExtensions.ReadIntOffset(endInputCode);
                // end has to be bigger than start or equal
                if (entInput < beginInput)
                {
                    // PDFBOX-4550: likely corrupt stream
                    break;
                }

                MoveNext();
                switch (TokenType)
                {
                    case TokenTypeEnum.ArrayBegin:
                        {
                            var inputCode = beginInputCode.ToArray();
                            while (MoveNext()
                              && TokenType != TokenTypeEnum.ArrayEnd)
                            {
                                codes.AddCharMapping(inputCode, ParseUnicode());
                                OperationUtils.Increment(inputCode);
                            }
                            break;
                        }
                    default:
                        {
                            var tokenBytes = ParseInputCode().ToArray();
                            var startCode = beginInputCode.ToArray();
                            if (tokenBytes.Length > 0)
                            {
                                // some pdfs use the malformed bfrange <0000> <FFFF> <0000>. Add support by adding a identity
                                // mapping for the whole range instead of cutting it after 255 entries
                                // TODO find a more efficient method to represent all values for a identity mapping
                                if (tokenBytes.Length == 2 && beginInput == 0 && entInput == 0xffff
                                        && tokenBytes[0] == 0 && tokenBytes[1] == 0)
                                {
                                    for (int i = 0; i < 256; i++)
                                    {
                                        startCode[0] = (byte)i;
                                        startCode[1] = 0;
                                        tokenBytes[0] = (byte)i;
                                        tokenBytes[1] = 0;
                                        AddMappingFrombfrange(codes, startCode, 0xff, tokenBytes);
                                    }
                                }
                                else
                                {
                                    AddMappingFrombfrange(codes, startCode, entInput - beginInput + 1, tokenBytes);
                                }
                            }
                            break;
                        }
                }
            }
        }

        private void ParseCodeSpaceRange(CMap codes, IList<object> operands)
        {
            for (int itemIndex = 0, itemCount = (int)operands[0]; itemIndex < itemCount; itemIndex++)
            {
                MoveNext();
                var startRange = ParseInputCode();
                MoveNext();
                var endRange = ParseInputCode();
                codes.AddCodespaceRange(new CodespaceRange(startRange, endRange));
            }
        }

        private void ParseCIDChar(CMap codes, IList<object> operands)
        {
            for (int itemIndex = 0, itemCount = (int)operands[0]; itemIndex < itemCount; itemIndex++)
            {
                MoveNext();
                var inputCode = ParseInputCode();
                //int mappedCID = ConvertUtils.ReadIntByLength(inputCode);
                MoveNext();
                var mappedCode = ParseUnicode();

                codes.AddCIDMapping(inputCode, mappedCode);
            }
        }

        private void ParseBFChar(CMap codes, IList<object> operands)
        {
            //NOTE: The first element on each line is the input code of the template font;
            //the second element is the code or name of the character.
            for (int itemIndex = 0, itemCount = (int)operands[0]; itemIndex < itemCount; itemIndex++)
            {
                MoveNext();
                var inputCode = ParseInputCode();
                MoveNext();
                var unicode = ParseUnicode();
                try
                {
                    codes.AddCharMapping(inputCode, unicode);
                }
                catch (OverflowException)
                { Debug.WriteLine($"WARN: Unable to process Unicode sequence from {codes.CMapName} CMap: {Token}"); }
            }
        }

        ///<summary>Converts the current token into its input code value.</summary>
        private ReadOnlySpan<byte> ParseInputCode()
        {
            switch (TokenType)
            {
                case TokenTypeEnum.Literal:
                case TokenTypeEnum.Hex:
                    return BytesToken.ToArray();
                case TokenTypeEnum.Keyword:
                case TokenTypeEnum.Name:
                    return ConvertUtils.HexToByteArray(CharsToken).AsSpan();
                default:
                    var chars = CharsToken;
                    return ConvertUtils.HexToByteArray(chars.Length > 0
                        ? chars : Token.ToString()).AsSpan();
            }
        }

        ///<summary>Converts the current token into its Unicode value.</summary>
        private int ParseUnicode()
        {
            switch (TokenType)
            {
                case TokenTypeEnum.Integer: // Character code in plain format.
                    return IntegerToken;
                case TokenTypeEnum.Hex: // Character code in hexadecimal format.
                case TokenTypeEnum.Literal:
                    return StreamExtensions.ReadIntOffset(BytesToken);
                case TokenTypeEnum.Name: // Character name.
                    return GlyphMapping.Default.ToUnicode(CharsToken.ToString()).Value;
                default:
                    throw new Exception("Hex string, integer or name expected instead of " + TokenType);
            }
        }

        private void AddMappingFrombfrange(CMap cmap, Span<byte> startCode, List<byte[]> tokenBytesList)
        {
            foreach (byte[] tokenBytes in tokenBytesList)
            {
                var value = StreamExtensions.ReadIntOffset(tokenBytes);
                cmap.AddCharMapping(startCode, value);
                startCode.Increment();
            }
        }

        private void AddMappingFrombfrange(CMap cmap, Span<byte> startCode, int values, Span<byte> tokenBytes)
        {
            for (int i = 0; i < values; i++)
            {
                var value = StreamExtensions.ReadIntOffset(tokenBytes);
                cmap.AddCharMapping(startCode, value);
                if (!tokenBytes.Increment()
                    || !startCode.Increment())
                {
                    // overflow detected -> stop adding further mappings
                    break;
                }
            }
        }

    }



}