/*
  Copyright 2012 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfGlown.Util;

using System;
using System.Collections;
using System.Collections.Generic;

namespace PdfGlown.Documents.Interaction.Navigation
{
    /// <summary>Article bead [PDF:1.7:8.3.2].</summary>
    [Pdf(VersionEnum.PDF11)]
    public sealed class ArticleElements : PdfObjectWrapper<PdfDictionary>, IList<ArticleElement>
    {
        private sealed class ElementCounter : ElementEvaluator
        {
            public int Count => index + 1;
        }

        private class ElementEvaluator //: IPredicate
        {
            // Current position.
            protected int index = -1;

            public virtual bool Evaluate(ArticleElement @object)
            {
                index++;
                return false;
            }
        }

        private sealed class ElementGetter : ElementEvaluator
        {
            private ArticleElement bead;
            private readonly int beadIndex;

            public ElementGetter(int beadIndex)
            { this.beadIndex = beadIndex; }

            public override bool Evaluate(ArticleElement @object)
            {
                base.Evaluate(@object);
                if (index == beadIndex)
                {
                    bead = @object;
                    return true;
                }
                return false;
            }

            public ArticleElement Bead => bead;
        }

        private sealed class ElementIndexer : ElementEvaluator
        {
            private readonly PdfDictionary searchedBead;

            public ElementIndexer(PdfDictionary searchedBead)
            { this.searchedBead = searchedBead; }

            public override bool Evaluate(ArticleElement @object)
            {
                base.Evaluate(@object);
                return @object.Equals(searchedBead);
            }

            public int Index => index;
        }

        private sealed class ElementListBuilder : ElementEvaluator
        {
            public IList<ArticleElement> elements = new List<ArticleElement>();

            public override bool Evaluate(ArticleElement @object)
            {
                elements.Add(@object);
                return false;
            }

            public IList<ArticleElement> Elements => elements;
        }

        private class Enumerator : IEnumerator<ArticleElement>
        {
            private ArticleElement currentObject;
            private readonly ArticleElement firstObject;
            private ArticleElement nextObject;

            internal Enumerator(ArticleElements elements)
            {
                nextObject = firstObject = elements.DataObject.Get<ArticleElement>(PdfName.F);
            }

            ArticleElement IEnumerator<ArticleElement>.Current => currentObject;

            public object Current => ((IEnumerator<ArticleElement>)this).Current;

            public bool MoveNext()
            {
                if (nextObject == null)
                    return false;

                currentObject = nextObject;
                nextObject = currentObject.Get<ArticleElement>(PdfName.N);
                if (nextObject == firstObject) // Looping back.
                { nextObject = null; }
                return true;
            }

            public void Reset()
            { throw new NotSupportedException(); }

            public void Dispose()
            { }
        }

        public ArticleElements(PdfDirectObject baseObject) : base(baseObject)
        { }

        public int IndexOf(ArticleElement @object)
        {
            if (@object == null)
                return -1; // NOTE: By definition, no bead can be null.

            var indexer = new ElementIndexer(@object);
            Iterate(indexer);
            return indexer.Index;
        }

        public void Insert(int index, ArticleElement @object)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException();

            var getter = new ElementGetter(index);
            Iterate(getter);
            PdfDictionary bead = getter.Bead;
            if (bead == null)
            { Add(@object); }
            else
            { Link(@object, bead); }
        }

        public void RemoveAt(int index) => Unlink(this[index]);

        public ArticleElement this[int index]
        {
            get
            {
                if (index < 0)
                    throw new ArgumentOutOfRangeException();

                var getter = new ElementGetter(index);
                Iterate(getter);
                var bead = getter.Bead;
                if (bead == null)
                    throw new ArgumentOutOfRangeException();

                return bead;
            }
            set => throw new NotImplementedException();
        }

        public void Add(ArticleElement @object)
        {
            var itemBead = @object;
            var firstBead = FirstBead;
            if (firstBead != null) // Non-empty list.
            { Link(itemBead, firstBead); }
            else // Empty list.
            {
                FirstBead = itemBead;
                Link(itemBead, itemBead);
            }
        }

        public void Clear()
        { throw new NotImplementedException(); }

        public bool Contains(ArticleElement @object) => IndexOf(@object) >= 0;

        public void CopyTo(ArticleElement[] objects, int index)
        { throw new NotImplementedException(); }

        public int Count
        {
            get
            {
                ElementCounter counter = new ElementCounter();
                Iterate(counter);
                return counter.Count;
            }
        }

        public bool IsReadOnly => false;

        public bool Remove(ArticleElement @object)
        {
            if (!Contains(@object))
                return false;

            Unlink(@object);
            return true;
        }

        public IEnumerator<ArticleElement> GetEnumerator() => new Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<ArticleElement>)this).GetEnumerator();

        private ArticleElement FirstBead
        {
            get => DataObject.Get<ArticleElement>(PdfName.F);
            set
            {
                var oldValue = FirstBead;
                DataObject[PdfName.F] = value?.Unresolve();
                if (value != null)
                { value[PdfName.T] = RefOrSelf; }
                if (oldValue != null)
                { oldValue.Remove(PdfName.T); }
            }
        }

        private void Iterate(ElementEvaluator predicate)
        {
            var firstBead = FirstBead;
            var bead = firstBead;
            while (bead != null)
            {
                if (predicate.Evaluate(bead))
                    break;

                bead = bead.Get<ArticleElement>(PdfName.N);
                if (bead == firstBead)
                    break;
            }
        }

        /// <summary>Links the given item.</summary>
        private void Link(PdfDictionary item, PdfDictionary next)
        {
            var previous = next.Get<PdfDictionary>(PdfName.V);
            if (previous == null)
            { previous = next; }

            item[PdfName.N] = next.Reference;
            next[PdfName.V] = item.Reference;
            if (previous != item)
            {
                item[PdfName.V] = previous.Reference;
                previous[PdfName.N] = item.Reference;
            }
        }

        /// <summary>Unlinks the given item.</summary>
        /// <remarks>It assumes the item is contained in this list.</remarks>
        private void Unlink(PdfDictionary item)
        {
            var prevBead = item.Get<ArticleElement>(PdfName.V);
            item.Remove(PdfName.V);
            var nextBead = item.Get<ArticleElement>(PdfName.N);
            item.Remove(PdfName.N);
            if (prevBead != item) // Still some elements.
            {
                prevBead[PdfName.N] = nextBead.Reference;
                nextBead[PdfName.V] = prevBead.Reference;
                if (item == FirstBead)
                { FirstBead = nextBead; }
            }
            else // No more elements.
            { FirstBead = null; }
        }
    }
}