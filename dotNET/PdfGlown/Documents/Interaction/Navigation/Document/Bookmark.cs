/*
  Copyright 2006-2012 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfGlown.Documents.Contents.ColorSpaces;
using PdfGlown.Documents.Interaction.Actions;
using PdfGlown.Objects;

using System;
using System.Collections;
using System.Collections.Generic;

namespace PdfGlown.Documents.Interaction.Navigation
{
    /// <summary>Outline item [PDF:1.6:8.2.2].</summary>
    [Pdf(VersionEnum.PDF10)]
    public class Bookmark : PdfDictionary, ILink, IList<Bookmark>
    {
        private Destination destination;
        private RGBColor color;

        /// <summary>Bookmark flags [PDF:1.6:8.2.2].</summary>
        [Flags]
        [Pdf(VersionEnum.PDF14)]
        public enum FlagsEnum
        {
            /// <summary>Display the item in italic.</summary>
            Italic = 0x1,
            /// <summary>Display the item in bold.</summary>
            Bold = 0x2
        }

        public Bookmark(PdfDocument context, string title)
            : this(context, new Dictionary<PdfName, PdfDirectObject>() {
                { PdfName.Title, new PdfTextString(title) }
            })
        { }

        protected Bookmark(PdfDocument context, Dictionary<PdfName, PdfDirectObject> data)
            : base(context, data)
        { }

        public Bookmark(PdfDocument context, string title, LocalDestination destination)
            : this(context, title)
        {
            Destination = destination;
        }

        public Bookmark(PdfDocument context, string title, PdfAction action)
            : this(context, title)
        {
            Action = action;
        }

        internal Bookmark(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        public override PdfName ModifyTypeKey(PdfName key) => key == PdfName.Parent
            || key == PdfName.First
            || key == PdfName.Last
            || key == PdfName.Prev ? PdfName.Outline : key;


        /// <summary>Gets/Sets the bookmark text color.</summary>
        [Pdf(VersionEnum.PDF14)]
        public RGBColor Color
        {
            get => color ??= (RGBColor)RGBColorSpace.Default.GetColor(Get<PdfArray>(PdfName.C));
            set
            {
                color = value;
                if (value == null)
                { Remove(PdfName.C); }
                else
                {
                    Document?.CheckCompatibility(VersionEnum.PDF14);
                    SetDirect(PdfName.C, color = value);
                }
            }
        }

        /// <summary>Gets/Sets whether this bookmark's children are displayed.</summary>
        public bool Expanded
        {
            get => GetInt(PdfName.Count) >= 0;
            set
            {
                if (Expanded == value)
                    return;

                // NOTE: Positive Count entry means open, negative Count entry means closed [PDF:1.6:8.2.2].
                Set(PdfName.Count, (value ? 1 : -1) * Math.Abs(GetInt(PdfName.Count)));
            }
        }

        /// <summary>Gets/Sets the bookmark flags.</summary>
        [Pdf(VersionEnum.PDF14)]
        public FlagsEnum Flags
        {
            get => (FlagsEnum)GetInt(PdfName.F);
            set
            {
                if (value == 0)
                { Remove(PdfName.F); }
                else
                {
                    Document?.CheckCompatibility(VersionEnum.PDF14);
                    Set(PdfName.F, (int)value);
                }
            }
        }

        /// <summary>Gets the parent bookmark.</summary>
        public Bookmark Parent
        {
            // Is its parent a bookmark?
            // NOTE: the Title entry can be used as a flag to distinguish bookmark
            //  (outline item) dictionaries from outline (root) dictionaries.
            get => Get<Bookmark>(PdfName.Parent);
        }

        /// <summary>Gets/Sets the text to be displayed for this bookmark.</summary>
        public string Title
        {
            get => GetString(PdfName.Title);
            set => this[PdfName.Title] = new PdfTextString(value);
        }

        public PdfDirectObject Target
        {
            get => ContainsKey(PdfName.Dest)
                    ? Destination
                    : ContainsKey(PdfName.A)
                        ? Action
                        : null;
            set
            {
                if (value is Destination destination)
                    Destination = destination;
                else if (value is PdfAction action)
                    Action = action;
                else
                    throw new ArgumentException("It MUST be either a Destination or an Action.");
            }
        }

        private PdfAction Action
        {
            get => Get<PdfAction>(PdfName.A);
            set
            {
                if (value == null)
                {
                    Remove(PdfName.A);
                }
                else
                {
                    // NOTE: This entry is not permitted in bookmarks if a 'Dest' entry already exists.
                    if (ContainsKey(PdfName.Dest))
                    {
                        Remove(PdfName.Dest);
                    }

                    this[PdfName.A] = value.Reference;
                }
            }
        }

        private Destination Destination
        {
            get
            {
                return destination ??= Get(PdfName.Dest) is PdfDirectObject destinationObject
                  ? Catalog.ResolveName<LocalDestination>(destinationObject)
                  : null;
            }
            set
            {
                destination = value;
                if (value == null)
                { Remove(PdfName.Dest); }
                else
                {
                    // NOTE: This entry is not permitted in bookmarks if an 'A' entry is present.
                    if (ContainsKey(PdfName.A))
                    { Remove(PdfName.A); }

                    this[PdfName.Dest] = value.NamedBaseObject;
                }
            }
        }

        /// <summary>
        /// NOTE: The Count entry may be absent [PDF:1.6:8.2.2].
        /// </summary>
        public new int Count => GetInt(PdfName.Count);

        /// <summary>Gets the count object, forcing its creation if it doesn't
        /// exist.</summary>
        private PdfInteger EnsureCountObject()
        {
            // NOTE: The Count entry may be absent [PDF:1.6:8.2.2].
            var countObject = Get<PdfInteger>(PdfName.Count);
            if (countObject == null)
            {
                Set(PdfName.Count, countObject = PdfInteger.Default);
            }

            return countObject;
        }

        public int IndexOf(Bookmark bookmark) => throw new NotImplementedException();

        public void Insert(int index, Bookmark bookmark) => throw new NotImplementedException();

        public void RemoveAt(int index) => throw new NotImplementedException();

        public bool Remove(Bookmark bookmark) => throw new NotImplementedException();

        public Bookmark this[int index]
        {
            get
            {
                var bookmarkObject = Get<Bookmark>(PdfName.First);
                while (index > 0)
                {
                    bookmarkObject = bookmarkObject.Get<Bookmark>(PdfName.Next);
                    // Did we go past the collection range?
                    if (bookmarkObject == null)
                        throw new ArgumentOutOfRangeException(nameof(index));
                    index--;
                }

                return bookmarkObject;
            }
            set => throw new NotImplementedException();
        }

        public virtual void Add(Bookmark bookmark)
        {
            // NOTE: Bookmarks imported from alien PDF files MUST be cloned
            // before being added.
            bookmark.Set(PdfName.Parent, Reference);

            PdfInteger countObject = EnsureCountObject();
            // Is it the first bookmark?
            if (countObject.RawValue == 0) // First bookmark.
            {
                Set(PdfName.Last, bookmark.Reference);
                Set(PdfName.First, bookmark.Reference);
                Set(PdfName.Count, countObject.IntValue + 1);
            }
            else // Non-first bookmark.
            {
                var oldLastBookmark = Get<Bookmark>(PdfName.Last);
                Set(PdfName.Last, bookmark.Reference); // Added bookmark is the last in the collection...
                oldLastBookmark.Set(PdfName.Next, bookmark.Reference); // ...and the next of the previously-last bookmark.
                bookmark.Set(PdfName.Prev, oldLastBookmark.Reference);

                // NOTE: The Count entry is a relative number (whose sign represents
                // the node open state).
                Set(PdfName.Count, countObject.IntValue + Math.Sign(countObject.IntValue));
            }
        }

        public new void Clear()
        { throw new NotImplementedException(); }

        public bool Contains(Bookmark bookmark)
        { throw new NotImplementedException(); }

        public void CopyTo(Bookmark[] bookmarks, int index)
        { throw new NotImplementedException(); }

        IEnumerator<Bookmark> IEnumerable<Bookmark>.GetEnumerator()
        {
            var bookmarkObject = this.Get<Bookmark>(PdfName.First);
            while (bookmarkObject != null)
            {
                yield return bookmarkObject;

                bookmarkObject = bookmarkObject.Get<Bookmark>(PdfName.Next);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<Bookmark>)this).GetEnumerator();
    }
}