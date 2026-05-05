/*
  Copyright 2008-2011 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfGlown.Objects;

using System;
using System.Collections;
using System.Collections.Generic;

namespace PdfGlown.Documents.Interaction.Actions
{
    ///<summary>Chained actions [PDF:1.6:8.5.1].</summary>
    [Pdf(VersionEnum.PDF12)]
    public sealed class ChainedActions : PdfObjectWrapper<PdfDirectObject>, IList<PdfAction>
    {
        //NOTE: Chained actions may be either singular or multiple (within an array).
        //This implementation hides such a complexity to the user, smoothly exposing
        //just the most general case (array) yet preserving its internal state.

        public static ChainedActions Wrap(PdfDirectObject baseObject, PdfAction parent)
            => baseObject == null ? null : new ChainedActions(baseObject, parent);

        ///Parent action.
        private PdfAction parent;

        public ChainedActions(PdfDirectObject baseObject, PdfAction parent)
            : base(baseObject)
        { this.parent = parent; }

        ///<summary>Gets the parent action.</summary>
        public PdfAction Parent => parent;

        public int Count
        {
            get
            {
                PdfDirectObject baseDataObject = DataObject;
                if (baseDataObject is PdfDictionary) // Single action.
                    return 1;
                else // Multiple actions.
                    return ((PdfArray)baseDataObject).Count;
            }
        }

        public bool IsReadOnly => false;

        public int IndexOf(PdfAction value)
        {
            PdfDirectObject baseDataObject = DataObject;
            if (baseDataObject is PdfDictionary) // Single action.
                return value.Reference.Equals(RefOrSelf) ? 0 : -1;
            else // Multiple actions.
                return ((PdfArray)baseDataObject).IndexOf(value.Reference);
        }

        public void Insert(int index, PdfAction value) => EnsureArray().Insert(index, value.Reference);

        public void RemoveAt(int index) => EnsureArray().RemoveAt(index);

        public PdfAction this[int index]
        {
            get
            {
                PdfDirectObject baseDataObject = DataObject;
                if (baseDataObject is PdfAction action) // Single action.
                {
                    if (index != 0)
                        throw new ArgumentException("Index: " + index + ", Size: 1");

                    return action;
                }
                else // Multiple actions.
                    return ((PdfArray)baseDataObject).Get<PdfAction>(index, PdfName.Action);
            }
            set => EnsureArray().Set(index, value);
        }

        public void Add(PdfAction value) => EnsureArray().Add(value.Reference);

        public void Clear() => EnsureArray().Clear();

        public bool Contains(PdfAction value)
        {
            PdfDirectObject baseDataObject = DataObject;
            if (baseDataObject is PdfDictionary) // Single action.
                return value.Reference.Equals(RefOrSelf);
            else // Multiple actions.
                return ((PdfArray)baseDataObject).Contains(value.Reference);
        }

        public void CopyTo(PdfAction[] entries, int index)
        {
            foreach (var entry in this)
            {
                entries[index++] = entry;
            }
        }

        public bool Remove(PdfAction value) => EnsureArray().Remove(value.Reference);

        IEnumerator<PdfAction> IEnumerable<PdfAction>.GetEnumerator()
        {
            for (int index = 0, length = Count; index < length; index++)
            { yield return this[index]; }
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<PdfAction>)this).GetEnumerator();

        private PdfArray EnsureArray()
        {
            var baseDataObject = DataObject;
            if (baseDataObject is PdfDictionary) // Single action.
            {
                var actionsArray = new PdfArrayImpl { RefOrSelf };
                RefOrSelf = actionsArray;
                parent[PdfName.Next] = actionsArray;

                baseDataObject = actionsArray;
            }
            return (PdfArray)baseDataObject;
        }
    }
}