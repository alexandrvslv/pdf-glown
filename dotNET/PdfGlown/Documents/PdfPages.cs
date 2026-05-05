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

using PdfGlown.Documents.Contents;
using PdfGlown.Objects;
using PdfGlown.Util.Collections;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PdfGlown.Documents
{
    /// <summary>Document pages collection [PDF:1.6:3.6.2].</summary>
    [Pdf(VersionEnum.PDF10)]
    public sealed class PdfPages : PdfDictionary, IExtList<PdfPage>, IList<PdfPage>, IResourceProvider
    {
        public struct Enumerator : IEnumerator<PdfPage>
        {
            /// <summary>Collection size.</summary>
            private int count;
            /// <summary>Index of the next item.</summary>
            private int index;

            /// <summary>Current page.</summary>
            private PdfPage current;

            /// <summary>Current level index.</summary>
            private int levelIndex;

            /// <summary>Stacked level indexes.</summary>
            private Stack<int> levelIndexes;

            private HashSet<PdfArray> containers;

            /// <summary>Current child tree nodes.</summary>
            private PdfArray kids;
            /// <summary>Current parent tree node.</summary>
            private PdfDictionary parent;

            internal Enumerator(PdfPages pages)
            {
                index = 0;
                current = null;
                levelIndex = 0;
                levelIndexes = new Stack<int>();
                containers = new HashSet<PdfArray>();
                count = pages.Count;
                parent = pages;
                kids = parent.Get<PdfArray>(PdfName.Kids);
            }

            public PdfPage Current => current;

            object IEnumerator.Current => current;

            public bool MoveNext()
            {
                if (index == count)
                    return false;

                //NOTE: As stated in [PDF:1.6:3.6.2], page retrieval is a matter of diving
                //  inside a B - tree.
                //  This is a special adaptation of the get() algorithm necessary to keep
                //  a low overhead throughout the page tree scan(using the get() method
                //  would have implied a nonlinear computational cost).

                //NOTE: Algorithm:
                //  1. [Vertical, down] We have to go downward the page tree till we reach
                //a page(leaf node).
                //  2. [Horizontal] Then we iterate across the page collection it belongs to,
                //  repeating step 1 whenever we find a subtree.
                //  3. [Vertical, up] When leaf-nodes scan is complete, we go upward solving
                //  parent nodes, repeating step 2.

                while (true)
                {
                    // Did we complete current page-tree-branch level?
                    if (kids.Count == levelIndex) // Page subtree complete.
                    {
                        // 3. Go upward one level.
                        // Restore node index at the current level!
                        levelIndex = levelIndexes.Pop() + 1; // Next node (partially scanned level).
                                                             // Move upward!
                        parent = parent.Get<PdfDictionary>(PdfName.Parent);
                        kids = parent.Get<PdfArray>(PdfName.Kids);
                    }
                    else // Page subtree incomplete.
                    {
                        var dict = kids.Get<PdfDictionary>(levelIndex);
                        // Is current kid a page object?
                        if (dict is PdfPage page)
                        {
                            // 2. Page found.
                            index++; // Absolute page index.
                            levelIndex++; // Current level node index.

                            current = page;
                            return true;
                        }
                        // Page tree node.
                        if (dict is PdfDictionary kid)
                        {
                            // 1. Go downward one level.
                            // Save node index at the current level!
                            levelIndexes.Push(levelIndex);
                            // Move downward!
                            parent = kid;
                            kids = parent.Get<PdfArray>(PdfName.Kids);
                            if (kids == null || containers.Contains(kids))
                                return false;
                            containers.Add(kids);
                            levelIndex = 0; // First node (new level).
                        }
                        else
                        {
                            return false;
                            //throw new Exception($"TODO Support type {kidObject.GetType()} in page enumeration!");
                        }
                    }
                }
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            public void Dispose()
            { }
        }

        /*
          TODO:IMPL A B-tree algorithm should be implemented to optimize the inner layout
          of the page tree (better insertion/deletion performance). In this case, it would
          be necessary to keep track of the modified tree nodes for incremental update.
        */
        public PdfPages(PdfDocument context)
            : base(context, new(3)
            {
                { PdfName.Type, PdfName.Pages },
                { PdfName.Kids, new PdfArrayImpl() },
                { PdfName.Count, PdfInteger.Default }
            })
        { }

        internal PdfPages(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets/Sets the default resource collection [PDF:1.6:3.6.2].</summary>
        /// <remarks>The default resource collection is used as last resort by every page that doesn't
        /// reference one explicitly (and doesn't reference an intermediate one implicitly).</remarks>
        public Resources Resources
        {
            get => GetOrCreate<Resources>(PdfName.Resources);
            set => Set(PdfName.Resources, value);
        }

        /// <summary>Gets the default media box.</summary>
        //NOTE: Document media box MUST be associated with the page-tree root node in order to be
        //inheritable by all the pages.
        public PdfRectangle MediaBox => GetOrCreate<PdfRectangle>(PdfName.MediaBox);

        public IList<PdfPage> GetRange(int index, int count)
        {
            return GetSlice(index, index + count);
        }

        public IList<PdfPage> GetSlice(int fromIndex, int toIndex)
        {
            var pages = new List<PdfPage>(toIndex - fromIndex);
            int i = fromIndex;
            while (i < toIndex)
            { pages.Add(this[i++]); }

            return pages;
        }

        public void InsertAll<TVar>(int index, ICollection<TVar> pages)
          where TVar : PdfPage
        {
            CommonAddAll(index, pages);
        }

        public void AddAll<TVar>(ICollection<TVar> pages)
          where TVar : PdfPage
        {
            CommonAddAll(-1, pages);
        }

        public void RemoveAll<TVar>(ICollection<TVar> pages)
          where TVar : PdfPage
        {
            //NOTE: The interface contract doesn't prescribe any relation among the removing-collection's
            //items, so we cannot adopt the optimized approach of the add* (...) methods family,
            //where adding-collection's items are explicitly ordered.

            foreach (PdfPage page in pages)
            { Remove(page); }
        }

        public int RemoveAll(Predicate<PdfPage> match)
        {
            //NOTE: Removal is indirectly fulfilled through an intermediate collection
            //in order not to interfere with the enumerator execution.

            var removingPages = new List<PdfPage>();
            foreach (PdfPage page in this)
            {
                if (match(page))
                { removingPages.Add(page); }
            }

            RemoveAll(removingPages);

            return removingPages.Count;
        }

        public int IndexOf(PdfPage page) => page.Index;

        public void Insert(int index, PdfPage page)
        {
            CommonAddAll(index, new PdfPage[] { page });
        }

        public void RemoveAt(int index) => Remove(this[index]);

        public PdfPage this[int index]
        {
            get
            {
                //NOTE: As stated in [PDF:1.6:3.6.2], to retrieve pages is a matter of diving
                //inside a B - tree.To keep it as efficient as possible, this implementation
                //does NOT adopt recursion to deepen its search, opting for an iterative
                //strategy instead.

                int pageOffset = 0;
                PdfDictionary parent = this;
                var kids = parent.Get<PdfArray>(PdfName.Kids);
                for (int i = 0; i < kids.Count; i++)
                {
                    var kid = kids.Get<PdfDictionary>(i);
                    // Is current kid a page object?
                    if (kid is PdfPage page) // Page object.
                    {
                        // Did we reach the searched position?
                        if (pageOffset == index) // Vertical scan (we finished).
                                                 // We got it!
                            return page;
                        else // Horizontal scan (go past).
                             // Cumulate current page object count!
                            pageOffset++;
                    }
                    else // Page tree node.
                    {
                        // Does the current subtree contain the searched page?
                        var count = kid.GetInt(PdfName.Count);
                        if (count + pageOffset > index) // Vertical scan (deepen the search).
                        {
                            // Go down one level!
                            parent = kid;
                            kids = parent.Get<PdfArray>(PdfName.Kids);
                            i = -1;
                        }
                        else // Horizontal scan (go past).
                        {
                            // Cumulate current subtree count!
                            pageOffset += count;
                        }
                    }
                }

                return null;
            }
            set
            {
                RemoveAt(index);
                Insert(index, value);
            }
        }

        public void Add(PdfPage page) => CommonAddAll(-1, new PdfPage[] { page });

        public new void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(PdfPage page) => ((IEnumerable<PdfPage>)this).Contains(page);

        public void CopyTo(PdfPage[] entires, int index)
        {
            foreach (PdfPage entry in this)
            {
                entires[index++] = entry;
            }
        }

        public new int Count => GetInt(PdfName.Count);

        public bool Remove(PdfPage page)
        {
            // Get the parent tree node!
            var parent = page.Get<PdfDictionary>(PdfName.Parent);
            // Get the parent's page collection!
            var kids = parent.Get<PdfArray>(PdfName.Kids);
            // Remove the page!
            kids.Remove(page.Reference);

            // Unbind the page from its parent!
            page[PdfName.Parent] = null;

            // Decrementing the pages counters...
            do
            {
                // Get the page collection counter!
                var count = parent.GetInt(PdfName.Count);
                // Decrement the counter at the current level!
                parent.Set(PdfName.Count, count - 1);

                // Iterate upward!
                parent = page.Get<PdfDictionary>(PdfName.Parent);
            } while (parent != null);

            return true;
        }

        public new Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<PdfPage> IEnumerable<PdfPage>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// Add a collection of pages at the specified position.
        /// <param name="index">Addition position. To append, use value -1.</param>
        /// <param name="pages">Collection of pages to add.</param>
        private void CommonAddAll<TPage>(int index, ICollection<TPage> pages) where TPage : PdfPage
        {
            PdfDictionary parent;
            PdfArray kids;
            int offset;
            // Append operation?
            if (index == -1) // Append operation.
            {
                // Get the parent tree node!
                parent = this;
                // Get the parent's page collection!
                kids = parent.Get<PdfArray>(PdfName.Kids);
                offset = 0; // Not used.
            }
            else // Insert operation.
            {
                // Get the page currently at the specified position!
                var pivotPage = this[index];
                // Get the parent tree node!
                parent = pivotPage.Get<PdfDictionary>(PdfName.Parent);
                // Get the parent's page collection!
                kids = parent.Get<PdfArray>(PdfName.Kids);
                // Get the insertion's relative position within the parent's page collection!
                offset = kids.IndexOf(pivotPage.Reference);
            }

            // Adding the pages...
            foreach (var page in pages)
            {
                // Append?
                if (index == -1) // Append.
                {
                    // Append the page to the collection!
                    kids.Add(page.Reference);
                }
                else // Insert.
                {
                    // Insert the page into the collection!
                    kids.Insert(offset++, page.Reference);
                }
                // Bind the page to the collection!
                page[PdfName.Parent] = parent.Reference;
            }

            // Incrementing the pages counters...
            do
            {
                // Get the page collection counter!
                var count = parent.GetInt(PdfName.Count);
                // Increment the counter at the current level!
                parent.Set(PdfName.Count, count + pages.Count);

                // Iterate upward!
                parent = parent.Get<PdfDictionary>(PdfName.Parent);
            } while (parent != null);
        }
    }
}