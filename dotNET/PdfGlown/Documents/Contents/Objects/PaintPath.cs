/*
  Copyright 2008-2015 Stefano Chizzolini. http://www.pdfclown.org

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

namespace PdfGlown.Documents.Contents.Objects
{
    /// <summary>Path-painting operation [PDF:1.6:4.4.2].</summary>
    [Pdf(VersionEnum.PDF10)]
    public abstract class PaintPath : Operation
    {
        public const string CloseFillStrokeEvenOddOperatorKeyword = "b*";
        public const string CloseFillStrokeOperatorKeyword = "b";
        public const string CloseStrokeOperatorKeyword = "s";
        public const string EndNoOpOperatorKeyword = "n";
        public const string FillEvenOddOperatorKeyword = "f*";
        public const string FillCompatibleOperatorKeyword = "F";
        public const string FillOperatorKeyword = "f";
        public const string FillStrokeEvenOddOperatorKeyword = "B*";
        public const string FillStrokeOperatorKeyword = "B";
        public const string StrokeOperatorKeyword = "S";

        /// <summary>'Close, fill, and then stroke the path, using the nonzero winding number rule to determine
        /// the region to fill' operation.</summary>
        public static readonly PaintPath CloseFillStroke = new PaintPathCloseFillStroke();

        /// <summary>'Close, fill, and then stroke the path, using the even-odd rule to determine the region
        /// to fill' operation.</summary>
        public static readonly PaintPath CloseFillStrokeEvenOdd = new PaintPathCloseFillStrokeEvenOdd();
        
        /// <summary>'Close and stroke the path' operation.</summary>
        public static readonly PaintPath CloseStroke = new PaintPathCloseStroke();
        
        /// <summary>'End the path object without filling or stroking it' operation.</summary>
        public static readonly PaintPath EndNoOp = new PaintPathEndNoOp();
        
        /// <summary>'Fill the path, using the nonzero winding number rule to determine the region to fill' operation.</summary>
        public static readonly PaintPath Fill = new PaintPathFill();
        
        /// <summary>'Fill the path, using the even-odd rule to determine the region to fill' operation.</summary>
        public static readonly PaintPath FillEvenOdd = new PaintPathFillEvenOdd();
        
        /// <summary>'Fill and then stroke the path, using the nonzero winding number rule to determine the region to
        /// fill' operation.</summary>
        public static readonly PaintPath FillStroke = new PaintPathFillStroke();
        
        /// <summary>'Fill and then stroke the path, using the even-odd rule to determine the region to fill' operation.</summary>
        public static readonly PaintPath FillStrokeEvenOdd = new PaintPathFillStrokeEvenOdd();
        
        /// <summary>'Stroke the path' operation.</summary>
        public static readonly PaintPath Stroke = new PaintPathStroke();

        public PaintPath(string @operator) : base(@operator)
        { }        
    }
}