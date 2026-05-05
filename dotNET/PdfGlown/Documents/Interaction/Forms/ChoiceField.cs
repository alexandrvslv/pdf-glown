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

using PdfGlown.Documents.Interaction.Annotations;
using PdfGlown.Objects;
using PdfGlown.Util;

using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfGlown.Documents.Interaction.Forms
{
    /// <summary>Choice field [PDF:1.6:8.6.3].</summary>
    [Pdf(VersionEnum.PDF12)]
    public abstract class ChoiceField : Field
    {
        private ChoiceItems items;

        /// <summary>Creates a new choice field within the given document context.</summary>
        protected ChoiceField(string name, Widget widget) : base(PdfName.Ch, name, widget)
        { }

        protected ChoiceField(PdfDirectObject baseObject) : base(baseObject)
        { }

        public ChoiceItems Items
        {
            get => items ??= new(DataObject.GetOrCreate<PdfArrayImpl>(PdfName.Opt));
            set => DataObject.Set(PdfName.Opt, items = value);
        }

        /// <summary>Gets/Sets whether more than one of the field's items may be selected simultaneously.
        /// </summary>
        public bool MultiSelect
        {
            get => (Flags & FlagsEnum.MultiSelect) == FlagsEnum.MultiSelect;
            set => Flags = EnumUtils.Mask(Flags, FlagsEnum.MultiSelect, value);
        }

        /// <summary>Gets/Sets whether validation action is triggered as soon as a selection is made,
        /// without requiring the user to exit the field.</summary>
        public bool ValidatedOnChange
        {
            get => (Flags & FlagsEnum.CommitOnSelChange) == FlagsEnum.CommitOnSelChange;
            set => Flags = EnumUtils.Mask(Flags, FlagsEnum.CommitOnSelChange, value);
        }

        /// <returns>Either a string (single-selection) or a list of strings (multi-selection).</returns>
        /// <seealso cref="MultiSelect"/>
        public override object Value
        {
            get
            {
                var valueObject = DataObject.GetInheritableAttribute(PdfName.V)?.Resolve(PdfName.V);
                if (MultiSelect)
                {
                    var values = new List<string>();
                    if (valueObject != null)
                    {
                        if (valueObject is PdfArray array)
                        {
                            foreach (var valueItemObject in array.GetItems().OfType<IPdfString>())
                            {
                                values.Add(valueItemObject.StringValue);
                            }
                        }
                        else
                        { values.Add(((IPdfString)valueObject).StringValue); }
                    }
                    return values;
                }
                else
                    return valueObject != null ? ((IPdfString)valueObject).StringValue : null;
            }
            set
            {
                if (value is string vstr)
                {
                    DataObject[PdfName.V] = new PdfTextString(vstr);
                }
                else if (value is IList<string> list)
                {
                    if (!MultiSelect)
                        throw new ArgumentException("IList<string> value is only allowed when MultiSelect flag is active.");

                    var oldValueObject = DataObject.Get<PdfDirectObject>(PdfName.V);
                    PdfArray valuesObject;
                    if (oldValueObject is PdfArray array)
                    {
                        valuesObject = array;
                        valuesObject.Clear();
                    }
                    else
                    { valuesObject = new PdfArrayImpl(); }

                    foreach (string valueItem in list)
                    { valuesObject.Add(valueItem); }

                    if (valuesObject != oldValueObject)
                    { DataObject[PdfName.V] = valuesObject; }
                }
                else if (value == null)
                { DataObject[PdfName.V] = null; }
                else
                    throw new ArgumentException("Value MUST be either a string or an IList<string>");
            }
        }
    }
}