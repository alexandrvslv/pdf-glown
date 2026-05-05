/*
  Copyright 2008-2012 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfGlown.Documents.Files;
using PdfGlown.Objects;

using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Interaction.Actions
{
    /// <summary>'Launch an application' action [PDF:1.6:8.5.3].</summary>
    [Pdf(VersionEnum.PDF11)]
    public sealed class Launch : PdfAction
    {
        private IPdfDataObject target;

        /// <summary>Windows-specific launch parameters [PDF:1.6:8.5.3].</summary>
        public class WinTarget : PdfObjectWrapper<PdfDictionary>
        {
            /// <summary>Operation [PDF:1.6:8.5.3].</summary>
            public enum OperationEnum
            {
                /// <summary>Open.</summary>
                Open,
                /// <summary>Print.</summary>
                Print
            };

            private const string ConstOpen = "open";
            private const string ConstPrint = "print";

            private static readonly Dictionary<OperationEnum, PdfString> OperationEnumCodes;

            static WinTarget()
            {
                OperationEnumCodes = new Dictionary<OperationEnum, PdfString>
                {
                    [OperationEnum.Open] = new PdfString(ConstOpen),
                    [OperationEnum.Print] = new PdfString(ConstPrint)
                };
            }

            /// <summary>Gets the code corresponding to the given value.</summary>
            private static PdfString ToCode(OperationEnum value) => OperationEnumCodes[value];

            /// <summary>Gets the operation corresponding to the given value.</summary>
            private static OperationEnum ToOperationEnum(IPdfString value)
            {
                if (value == null)
                    return OperationEnum.Open;
                foreach (KeyValuePair<OperationEnum, PdfString> operation in OperationEnumCodes)
                {
                    if (string.Equals(operation.Value.StringValue, value.StringValue, StringComparison.Ordinal))
                        return operation.Key;
                }
                return OperationEnum.Open;
            }

            public WinTarget(PdfDocument context, string fileName)
                : base(context, new PdfDictionary())
            { FileName = fileName; }

            public WinTarget(PdfDocument context, string fileName, OperationEnum operation)
                : this(context, fileName)
            { Operation = operation; }

            public WinTarget(PdfDocument context, string fileName, string parameterString)
                : this(context, fileName)
            { ParameterString = parameterString; }

            public WinTarget(PdfDirectObject baseObject) : base(baseObject)
            { }

            public override object Clone(PdfDocument context)
            { throw new NotImplementedException(); }

            /// <summary>Gets/Sets the default directory.</summary>
            public string DefaultDirectory
            {
                get => DataObject.GetString(PdfName.D);
                set => DataObject.Set(PdfName.D, value);
            }

            /// <summary>Gets/Sets the file name of the application to be launched
            /// or the document to be opened or printed.</summary>
            public string FileName
            {
                get => DataObject.GetString(PdfName.F);
                set => DataObject.Set(PdfName.F, value);
            }

            /// <summary>Gets/Sets the operation to perform.</summary>
            public OperationEnum Operation
            {
                get => ToOperationEnum(DataObject.Get<IPdfString>(PdfName.O));
                set => DataObject[PdfName.O] = ToCode(value);
            }

            /// <summary>Gets/Sets the parameter string to be passed to the application.</summary>
            public string ParameterString
            {
                get => DataObject.GetString(PdfName.P);
                set => DataObject[PdfName.P] = new PdfString(value);
            }
        }

        /// <summary>Creates a launcher.</summary>
        /// <param name="context">Document context.</param>
        /// <param name="target">Either a <see cref="FileSpecification"/> or a <see cref="WinTarget"/>
        /// representing either an application or a document.</param>
        public Launch(PdfDocument context, IPdfDataObject target) 
            : base(context, PdfName.Launch)
        { Target = target; }

        internal Launch(Dictionary<PdfName, PdfDirectObject> baseObject) 
            : base(baseObject)
        { }

        /// <summary>Gets/Sets the action options.</summary>
        public OptionsEnum Options
        {
            get
            {
                OptionsEnum options = 0;
                if (GetBool(PdfName.NewWindow))
                { options |= OptionsEnum.NewWindow; }
                return options;
            }
            set
            {
                if ((value & OptionsEnum.NewWindow) == OptionsEnum.NewWindow)
                { this[PdfName.NewWindow] = PdfBoolean.True; }
                else if ((value & OptionsEnum.SameWindow) == OptionsEnum.SameWindow)
                { this[PdfName.NewWindow] = PdfBoolean.False; }
                else
                { this.Remove(PdfName.NewWindow); } // NOTE: Forcing the absence of this entry ensures that the viewer application should behave in accordance with the current user preference.
            }
        }

        /// <summary>Gets/Sets the application to be launched or the document to be opened or printed.
        /// </summary>
        public IPdfDataObject Target
        {
            get => target ??= Get(PdfName.F) is PdfDirectObject file
                    ? IFileSpecification.Wrap(file)
                    : Get(PdfName.Win) is PdfDirectObject win
                        ? new WinTarget(win)
                        : null;
            set
            {
                target = value;
                if (value is IFileSpecification specification)
                { Set(PdfName.F, specification.RefOrSelf); }
                else if (value is WinTarget winTarget)
                { Set(PdfName.Win, winTarget); }
                else
                { throw new ArgumentException("MUST be either FileSpecification or WinTarget"); }
            }
        }

        public override string GetDisplayName()
        {
            return "Launch " + (Target is FileSpecification fileSpec
                ? fileSpec.FilePath
                : Target is WinTarget winTarget
                    ? $"{winTarget.Operation} {winTarget.ParameterString} {winTarget.FileName}"
                    : string.Empty);
        }
    }
}