/*
  Copyright 2015 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfGlown.Objects;
using PdfGlown.Util;

using System;

namespace PdfGlown.Documents.Contents.Layers
{
    /// <summary>Visibility expression, used to compute visibility of content based on a set of layers
    /// [PDF:1.7:4.10.1].</summary>
    [Pdf(VersionEnum.PDF16)]
    public class VisibilityExpression : PdfObjectWrapper<PdfArray>
    {
        public enum OperatorEnum
        {
            And,
            Or,
            Not
        }

        private static readonly BiDictionary<OperatorEnum, string> operatorCodes = new()
        {
            [OperatorEnum.And] = PdfName.And.StringValue,
            [OperatorEnum.Not] = PdfName.Not.StringValue,
            [OperatorEnum.Or] = PdfName.Or.StringValue
        };

        public static OperatorEnum GetOperator(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            OperatorEnum? @operator = operatorCodes.GetKey(name);
            if (!@operator.HasValue)
                throw new NotSupportedException("Operator unknown: " + name);

            return @operator.Value;
        }

        public static string GetOperatorName(OperatorEnum @operator) => operatorCodes[@operator];

        private OperandsImpl operands;

        public VisibilityExpression(PdfDocument context, OperatorEnum @operator, params IPdfObjectWrapper[] operands)
            : base(context, new PdfArrayImpl(operands?.Length ?? 1) { (PdfDirectObject)null })
        {
            Operator = @operator;
            var operands_ = Operands;
            foreach (var operand in operands)
            { operands_.Add(operand); }
        }

        public VisibilityExpression(PdfDirectObject baseObject) : base(baseObject)
        { }

        public ArrayWrapper<IPdfObjectWrapper> Operands => operands ??= new OperandsImpl(RefOrSelf);

        public OperatorEnum Operator
        {
            get => GetOperator(DataObject.GetString(0));
            set
            {
                if (value == OperatorEnum.Not && DataObject.Count > 2)
                    throw new ArgumentException("'Not' operator requires only one operand.");

                DataObject.SetName(0, GetOperatorName(value));
            }
        }
    }
}

