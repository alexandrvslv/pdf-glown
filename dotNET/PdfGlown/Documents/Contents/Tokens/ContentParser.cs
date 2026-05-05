/*
  Copyright 2011-2015 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)
    * Stephen Cleary (bug reporter [FIX:51], https://sourceforge.net/u/stephencleary/)

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
using PdfGlown.Documents.Contents.ColorSpaces;
using PdfGlown.Documents.Contents.Objects;
using PdfGlown.Objects;
using PdfGlown.Tokens;
using PdfGlown.Util.Parsers;
using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents.Tokens
{
    /// <summary>Content stream parser [PDF:1.6:3.7.1].</summary>
    public sealed class ContentParser : BaseParser
    {
        internal ContentParser(IInputStream stream) : base(stream)
        { }

        public ContentParser(Memory<byte> data) : base(data)
        { }

        /// <summary>Parses the next content object [PDF:1.6:4.1].</summary>
        public ContentObject ParseContentObject()
        {
            Operation operation = ParseOperation();
            switch (operation) // External object.
            {
                case BeginSubpath:
                case DrawRectangle:
                case ModifyClipPath:
                    return ParsePath(operation);
                case BeginText:
                    return new GraphicsText(ParseContentObjects());
                //case SaveGraphicsState:
                //    return new GraphicsLocalState(ParseContentObjects());
                case BeginMarkedContent beginMarkedContent:
                    return new GraphicsMarkedContent(beginMarkedContent, ParseContentObjects(new List<ContentObject> { beginMarkedContent }));
                case BeginCompatibilityState:
                    return new GraphicsCompatibilityState(ParseContentObjects());
                case BeginInlineImage:
                    return ParseInlineImage();
                default:
                    return operation;
            }
        }

        /// <summary>Parses the next content objects.</summary>
        public List<ContentObject> ParseContentObjects()
        {
            var contentObjects = new List<ContentObject>();
            return ParseContentObjects(contentObjects);
        }

        private List<ContentObject> ParseContentObjects(List<ContentObject> contentObjects)
        {
            while (MoveNext())
            {
                ContentObject contentObject = ParseContentObject();
                // Multiple-operation graphics object end?
                if (contentObject is EndText // Text.
                                             //|| contentObject is RestoreGraphicsState // Local graphics state.
                  || contentObject is EndMarkedContent // End marked-content sequence.
                  || contentObject is EndCompatibilityState // compatibility state.
                  || contentObject is EndInlineImage // Inline image.
                  )
                    return contentObjects;

                contentObjects.Add(contentObject);
            }
            return contentObjects;
        }

        /// <summary>Parses the next operation.</summary>
        public Operation ParseOperation()
        {
            string @operator = null;
            PdfArray operands = null;
            // Parsing the operation parts...
            do
            {
                if (Position == 0 && TokenType == TokenTypeEnum.ArrayEnd)
                    MoveNext();
                switch (TokenType)
                {
                    case TokenTypeEnum.Keyword:
                        @operator = StringBuffer.ToString();
                        break;
                    default:
                        operands ??= new PdfArrayImpl();
                        operands.AddSimple(ParsePdfObject());
                        break;
                }
            } while (@operator == null && MoveNext());
            return @operator == null ? null : Operation.Get(@operator, operands);
        }

        public override PdfDirectObject ParsePdfObject(PdfName parentKey = null)
        {
            if (TokenType == TokenTypeEnum.Literal
                || TokenType == TokenTypeEnum.Hex)
            {
                return new PdfByteString(BytesToken.ToArray());
            }
            return base.ParsePdfObject(parentKey);
        }

        protected override PdfArray CreatePdfArray(List<PdfDirectObject> array, PdfName parentKey, PdfName gPKey)
        {
            return ColorSpace.IsMatch(array) 
                ? ColorSpace.Create(array)
                : base.CreatePdfArray(array, parentKey, gPKey);
        }

        private GraphicsInlineImage ParseInlineImage()
        {
            //NOTE: Inline images use a peculiar syntax that's an exception to the usual rule
            //that the data in a content stream is interpreted according to the standard PDF syntax
            //for objects.
            InlineImageHeader header;
            {
                var operands = new PdfArrayImpl();
                // Parsing the image entries...
                while (MoveNext() && TokenType != TokenTypeEnum.Keyword) // Not keyword (i.e. end at image data beginning (ID operator)).
                {
                    operands.AddSimple(ParsePdfObject());
                }
                header = new InlineImageHeader(operands);
            }

            // [FIX:51,74] Wrong 'EI' token handling on inline image parsing.
            IInputStream stream = Stream;
            stream.ReadByte(); // Should be the whitespace following the 'ID' token.
            var startSegment = stream.Position;
            //var data = new ByteStream();
            while (true)
            {
                int curByte = stream.ReadByte();
                if (((char)curByte == 'E' && (char)stream.PeekByte() == 'I'))
                {
                    stream.ReadByte();
                    break;
                }
                //data.WriteByte((byte)curByte);
            }
            var length = (stream.Position - startSegment) - 2;
            var data = new StreamSegment(stream, startSegment, length);
            var body = new InlineImageBody(data);

            return new GraphicsInlineImage(header, body);
        }

        private GraphicsPath ParsePath(Operation beginOperation)
        {
            //NOTE: Paths do not have an explicit end operation, so we must infer it
            //looking for the first non-painting operation.
            var operations = new List<ContentObject>();
            if (beginOperation is not ModifyClipPath)
                operations.Add(beginOperation);
            long position = Position;
            bool closeable = false;
            while (MoveNext())
            {
                var operation = ParseOperation();
                // Multiple-operation graphics object closeable?
                if (operation is PaintPath) // Painting operation.
                { closeable = true; }
                else if (closeable) // Past end (first non-painting operation).
                {
                    Seek(position); // Rolls back to the last path-related operation.

                    break;
                }

                operations.Add(operation);
                position = Position;
            }
            return beginOperation is ModifyClipPath modifyClipPath
                ? new GraphicsPathPreClip(modifyClipPath, operations)
                : new GraphicsPath(operations);
        }

        public override bool MoveNextComplex() => base.MoveNext();
    }
}