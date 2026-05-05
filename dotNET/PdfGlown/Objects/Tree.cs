/*
  Copyright 2007-2015 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfGlown.Util;
using System;
using System.Collections;
using System.Collections.Generic;

namespace PdfGlown.Objects
{
    /// <summary>Abstract tree [PDF:1.7:3.8.5].</summary>
    [Pdf(VersionEnum.PDF10)]
    public abstract class Tree<TKey, TValue> : PdfObjectWrapper<PdfDictionary>, IDictionary<TKey, TValue>, IDictionary, IBiDictionary<TKey, TValue>
        where TKey : PdfDirectObject, IPdfSimpleObject
        where TValue : IPdfDataObject
    {
        // NOTE: This implementation is an adaptation of the B-tree algorithm described in "Introduction
        // to Algorithms" [1], 2nd ed (Cormen, Leiserson, Rivest, Stein) published by MIT Press/McGraw-Hill.
        // PDF trees represent a special subset of B-trees whereas actual keys are concentrated in leaf
        // nodes and proxied by boundary limits across their paths. This simplifies some handling but
        // requires keeping node limits updated whenever a change occurs in the leaf nodes composition.
        // [1] http://en.wikipedia.org/wiki/Introduction_to_Algorithms

        /// <summary> Node children.</summary>
        private sealed class Children
        {
            public sealed class InfoImpl
            {
                private static readonly InfoImpl KidsInfo = new(1, TreeLowOrder);
                private static readonly InfoImpl PairsInfo = new(2, TreeLowOrder); // NOTE: Paired children are combinations of 2 contiguous items.

                public static InfoImpl Get(PdfName typeName)
                { return typeName.Equals(PdfName.Kids) ? KidsInfo : PairsInfo; }

                /// <summary> Number of (contiguous) children defining an item.</summary>
                public int ItemCount;
                /// <summary> Maximum number of children.</summary>
                public int MaxCount;
                /// <summary> Minimum number of children.</summary>
                public int MinCount;

                public InfoImpl(int itemCount, int lowOrder)
                {
                    ItemCount = itemCount;
                    MinCount = itemCount * lowOrder;
                    MaxCount = MinCount * 2;
                }
            }

            /// <summary>Gets the given node's children.</summary>
            /// <param name="node">Parent node.</param>
            /// <param name="pairs">Pairs key.</param>
            public static Children Get(PdfDictionary node, PdfName pairsKey)
            {
                PdfName childrenTypeName;
                if (node.ContainsKey(PdfName.Kids))
                { childrenTypeName = PdfName.Kids; }
                else if (node.ContainsKey(pairsKey))
                { childrenTypeName = pairsKey; }
                else
                    throw new Exception("Malformed tree node.");

                return new Children(node, childrenTypeName);
            }

            private InfoImpl info;
            private PdfArray items;
            private PdfDictionary parent;
            private PdfName typeName;

            private Children(PdfDictionary parent, PdfName typeName)
            {
                this.parent = parent;
                TypeName = typeName;
            }

            /// <summary>Gets the node's children info.</summary>
            public InfoImpl Info => info;

            /// <summary>Gets whether the collection size has reached its maximum.</summary>
            public bool IsFull() => Items.Count >= Info.MaxCount;

            /// <summary>Gets whether this collection represents a leaf node.</summary>
            public bool IsLeaf() => !TypeName.Equals(PdfName.Kids);

            /// <summary>Gets whether the collection size is more than its maximum.</summary>
            public bool IsOversized() => Items.Count > Info.MaxCount;

            /// <summary>Gets whether the collection size is less than its minimum.</summary>
            public bool IsUndersized() => Items.Count < Info.MinCount;

            /// <summary>Gets whether the collection size is within the order limits.</summary>
            public bool IsValid() => !(IsUndersized() || IsOversized());

            /// <summary>Gets the node's children collection.</summary>
            public PdfArray Items => items;

            /// <summary>Gets the node.</summary>
            public PdfDictionary Parent => parent;

            /// <summary>Gets/Sets the node's children type.</summary>
            public PdfName TypeName
            {
                get => typeName;
                set
                {
                    typeName = value;
                    items = parent.Get<PdfArray>(typeName);
                    info = InfoImpl.Get(typeName);
                }
            }

            public PdfDictionary BinaySearch(TKey key)
            {
                int low = 0, high = Items.Count - Info.ItemCount;
                while (true)
                {
                    if (low > high)
                        return null;

                    int mid = (low + high) / 2;
                    var kid = Items.Get<PdfDictionary>(mid);
                    var limits = kid.Get<PdfArray>(PdfName.Limits);
                    if (key.CompareTo(limits.Get(0)) < 0)
                    { high = mid - 1; }
                    else if (key.CompareTo(limits.Get(1)) > 0)
                    { low = mid + 1; }
                    else
                    {
                        // Go down one level!
                        return kid;
                    }
                }
            }

            public TValue BinarySearchLeaf(TKey key, Tree<TKey, TValue> tree)
            {
                int low = 0, high = Items.Count - Info.ItemCount;
                while (true)
                {
                    if (low > high)
                        return default;

                    int mid = (mid = ((low + high) / 2)) - (mid % 2);
                    int comparison = key.CompareTo(Items.Get(mid));
                    if (comparison < 0)
                    { high = mid - 2; }
                    else if (comparison > 0)
                    { low = mid + 2; }
                    else
                    {
                        // We got it!
                        return tree.WrapValue(Items.Get(mid + 1));
                    }
                }
            }
        }

        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
        {
            /// <summary>Current named object.</summary>
            private KeyValuePair<TKey, TValue>? current;

            /// <summary>Current level index.</summary>
            private int levelIndex;

            /// <summary>Stacked levels.</summary>
            private Stack<object[]> levels;

            /// <summary>Current child tree nodes.</summary>
            private PdfArray kids;

            /// <summary>Current names.</summary>
            private PdfArray names;

            /// <summary>Current container.</summary>
            private PdfIndirectObject container;

            /// <summary>Name tree.</summary>
            private Tree<TKey, TValue> tree;

            internal Enumerator(Tree<TKey, TValue> tree)
            {
                current = null;
                names = null;
                kids = null;
                container = null;
                levelIndex = 0;
                levels = new Stack<object[]>();
                this.tree = tree;

                Reset();
            }

            KeyValuePair<TKey, TValue> IEnumerator<KeyValuePair<TKey, TValue>>.Current => current.Value;

            public object Current => current.Value;

            public bool MoveNext() => (current = GetNext()) != null;

            public void Reset()
            {
                container = tree.Container;
                MoveNext(tree.DataObject);
            }

            private void MoveNext(PdfDictionary node)
            {
                var kidsObject = node.Get(PdfName.Kids);
                if (kidsObject == null) // Leaf node.
                {
                    var namesObject = node.Get(tree.pairsKey);
                    if (namesObject is PdfReference reference)
                    { container = reference.IndirectObject; }
                    names = (PdfArray)namesObject.Resolve();
                }
                else // Intermediate node.
                {
                    if (kidsObject is PdfReference kidsRef)
                    { container = kidsRef.IndirectObject; }
                    kids = (PdfArray)kidsObject.Resolve();
                }
            }

            public void Dispose()
            { }

            private KeyValuePair<TKey, TValue>? GetNext()
            {
                // NOTE: Algorithm:
                //  1. [Vertical, down] We have to go downward the name tree till we reach
                //  a names collection (leaf node).
                //  2. [Horizontal] Then we iterate across the names collection.
                //  3. [Vertical, up] When leaf-nodes scan is complete, we go upward solving
                //  parent nodes, repeating step 1.
                while (true)
                {
                    if (names == null)
                    {
                        if (kids == null
                          || kids.Count == levelIndex) // Kids subtree complete.
                        {
                            if (levels.Count == 0)
                                return null;

                            // 3. Go upward one level.
                            // Restore current level!
                            object[] level = levels.Pop();
                            container = (PdfIndirectObject)level[0];
                            kids = (PdfArray)level[1];
                            levelIndex = ((int)level[2]) + 1; // Next node (partially scanned level).
                        }
                        else // Kids subtree incomplete.
                        {
                            // 1. Go downward one level.
                            // Save current level!
                            levels.Push(new object[] { container, kids, levelIndex });

                            // Move downward!
                            var kidReference = (PdfReference)kids.Get(levelIndex);
                            container = kidReference.IndirectObject;
                            MoveNext((PdfDictionary)kidReference.Resolve());
                            levelIndex = 0; // First node (new level).
                        }
                    }
                    else
                    {
                        if (names.Count == levelIndex) // Names complete.
                        { names = null; }
                        else // Names incomplete.
                        {
                            // 2. Object found.
                            TKey key = (TKey)names.Get(levelIndex);
                            TValue value = tree.WrapValue(names.Get(levelIndex + 1));
                            levelIndex += 2;

                            return new KeyValuePair<TKey, TValue>(key, value);
                        }
                    }
                }
            }
        }

        private interface IFiller<TObject>
        {
            void Add(PdfArray names, int offset);

            ICollection<TObject> Collection { get; }
        }

        private class KeysFiller : IFiller<TKey>
        {
            private List<TKey> keys = new();

            public void Add(PdfArray names, int offset)
            { keys.Add((TKey)names.Get(offset)); }

            public ICollection<TKey> Collection => keys;
        }

        private class ValuesFiller : IFiller<TValue>
        {
            private Tree<TKey, TValue> tree;
            private List<TValue> values = new();

            internal ValuesFiller(Tree<TKey, TValue> tree)
            { this.tree = tree; }

            public void Add(PdfArray names, int offset)
            { values.Add(tree.WrapValue(names.Get(offset + 1))); }

            public ICollection<TValue> Collection => values;
        }

        /// <summary>
        /// Minimum number of items in non-root nodes.
        /// Note that the tree (high) order is assumed twice as much (<see cref="Children.Info.Info(int, int)"/>.
        /// </summary>
        private static readonly int TreeLowOrder = 5;

        private PdfName pairsKey;

        public Tree(PdfDocument context)
            : base(context, new PdfDictionary())
        { Initialize(); }

        public Tree(PdfDirectObject baseObject)
            : base(baseObject)
        { Initialize(); }

        ///<summary>Gets the name of the key-value pairs entries.</summary>
        protected abstract PdfName PairsKey { get; }

        public bool IsFixedSize => false;

        ICollection IDictionary.Keys => (ICollection)Keys;

        ICollection IDictionary.Values => (ICollection)Values;

        public bool IsSynchronized => true;

        public object SyncRoot => throw new NotImplementedException();

        public object GetKey(object value) => value is TValue tValue ? GetKey(tValue) : default(TKey);

        /// <summary>Gets the key associated to the specified value.</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public TKey GetKey(TValue value)
        {
            // NOTE: Current implementation doesn't support bidirectional maps, to say that the only
            // currently-available way to retrieve a key from a value is to iterate the whole map (really
            // poor performance!).
            foreach (KeyValuePair<TKey, TValue> entry in this)
            {
                if (entry.Value.Equals(value))
                    return entry.Key;
            }
            return null;
        }

        public virtual void Add(TKey key, TValue value) => Add(key, value, false);

        public bool ContainsKey(object key) => ContainsKey((TKey)key);
        public virtual bool ContainsKey(TKey key)
        {
            // NOTE: Here we assume that any named entry has a non-null value.
            return this[key] != null;
        }

        public virtual ICollection<TKey> Keys
        {
            get
            {
                var filler = new KeysFiller();
                Fill(filler, DataObject);

                return filler.Collection;
            }
        }

        public virtual bool Remove(TKey key)
        {
            PdfDictionary node = DataObject;
            var nodeReferenceStack = new Stack<PdfReference>();
            while (true)
            {
                Children nodeChildren = Children.Get(node, pairsKey);
                if (nodeChildren.IsLeaf()) // Leaf node.
                {
                    int low = 0, high = nodeChildren.Items.Count - nodeChildren.Info.ItemCount;
                    while (true)
                    {
                        if (low > high) // No match.
                            return false;

                        int mid = (mid = ((low + high) / 2)) - (mid % 2);
                        int comparison = key.CompareTo(nodeChildren.Items.Get(mid));
                        if (comparison < 0) // Key before.
                        { high = mid - 2; }
                        else if (comparison > 0) // Key after.
                        { low = mid + 2; }
                        else // Key matched.
                        {
                            // We got it!
                            nodeChildren.Items.RemoveAt(mid + 1); // Removes value.
                            nodeChildren.Items.RemoveAt(mid); // Removes key.
                            if (mid == 0 || mid == nodeChildren.Items.Count) // Limits changed.
                            {
                                // Update key limits!
                                UpdateNodeLimits(nodeChildren);

                                // Updating key limits on ascendants...
                                var rootReference = (PdfReference)RefOrSelf;
                                PdfReference nodeReference;
                                while (nodeReferenceStack.Count > 0 && !(nodeReference = nodeReferenceStack.Pop()).Equals(rootReference))
                                {
                                    var parentChildren = (PdfArray)nodeReference.ParentObject;
                                    int nodeIndex = parentChildren.IndexOf(nodeReference);
                                    if (nodeIndex == 0 || nodeIndex == parentChildren.Count - 1)
                                    {
                                        var parent = (PdfDictionary)parentChildren.ParentObject;
                                        UpdateNodeLimits(parent, parentChildren, PdfName.Kids);
                                    }
                                    else
                                        break;
                                }
                            }
                            return true;
                        }
                    }
                }
                else // Intermediate node.
                {
                    int low = 0, high = nodeChildren.Items.Count - nodeChildren.Info.ItemCount;
                    while (true)
                    {
                        if (low > high) // Outside the limit range.
                            return false;

                        int mid = (low + high) / 2;
                        var kidReference = (PdfReference)nodeChildren.Items.Get(mid);
                        var kid = (PdfDictionary)kidReference.Resolve();
                        var limits = kid.Get<PdfArray>(PdfName.Limits);
                        if (key.CompareTo(limits.Get(0)) < 0) // Before the lower limit.
                        { high = mid - 1; }
                        else if (key.CompareTo(limits.Get(1)) > 0) // After the upper limit.
                        { low = mid + 1; }
                        else // Limit range matched.
                        {
                            Children kidChildren = Children.Get(kid, pairsKey);
                            if (kidChildren.IsUndersized())
                            {
                                // NOTE: Rebalancing is required as minimum node size invariant is violated.
                                PdfDictionary leftSibling = null;
                                Children leftSiblingChildren = null;
                                if (mid > 0)
                                {
                                    leftSibling = nodeChildren.Items.Get<PdfDictionary>(mid - 1);
                                    leftSiblingChildren = Children.Get(leftSibling, pairsKey);
                                }
                                PdfDictionary rightSibling = null;
                                Children rightSiblingChildren = null;
                                if (mid < nodeChildren.Items.Count - 1)
                                {
                                    rightSibling = nodeChildren.Items.Get<PdfDictionary>(mid + 1);
                                    rightSiblingChildren = Children.Get(rightSibling, pairsKey);
                                }

                                if (leftSiblingChildren != null && !leftSiblingChildren.IsUndersized())
                                {
                                    // Move the last child subtree of the left sibling to be the first child subtree of the kid!
                                    for (int index = 0, endIndex = leftSiblingChildren.Info.ItemCount; index < endIndex; index++)
                                    {
                                        int itemIndex = leftSiblingChildren.Items.Count - 1;
                                        PdfDirectObject item = leftSiblingChildren.Items.Get(itemIndex);
                                        leftSiblingChildren.Items.RemoveAt(itemIndex);
                                        kidChildren.Items.Insert(0, item);
                                    }
                                    // Update left sibling's key limits!
                                    UpdateNodeLimits(leftSiblingChildren);
                                }
                                else if (rightSiblingChildren != null && !rightSiblingChildren.IsUndersized())
                                {
                                    // Move the first child subtree of the right sibling to be the last child subtree of the kid!
                                    for (int index = 0, endIndex = rightSiblingChildren.Info.ItemCount; index < endIndex; index++)
                                    {
                                        int itemIndex = 0;
                                        PdfDirectObject item = rightSiblingChildren.Items.Get(itemIndex);
                                        rightSiblingChildren.Items.RemoveAt(itemIndex);
                                        kidChildren.Items.Add(item);
                                    }
                                    // Update right sibling's key limits!
                                    UpdateNodeLimits(rightSiblingChildren);
                                }
                                else
                                {
                                    if (leftSibling != null)
                                    {
                                        // Merging with the left sibling...
                                        for (int index = leftSiblingChildren.Items.Count; index-- > 0;)
                                        {
                                            PdfDirectObject item = leftSiblingChildren.Items.Get(index);
                                            leftSiblingChildren.Items.RemoveAt(index);
                                            kidChildren.Items.Insert(0, item);
                                        }
                                        nodeChildren.Items.RemoveAt(mid - 1);
                                        leftSibling.Delete();
                                    }
                                    else if (rightSibling != null)
                                    {
                                        // Merging with the right sibling...
                                        for (int index = rightSiblingChildren.Items.Count; index-- > 0;)
                                        {
                                            int itemIndex = 0;
                                            PdfDirectObject item = rightSiblingChildren.Items.Get(itemIndex);
                                            rightSiblingChildren.Items.RemoveAt(itemIndex);
                                            kidChildren.Items.Add(item);
                                        }
                                        nodeChildren.Items.RemoveAt(mid + 1);
                                        rightSibling.Delete();
                                    }
                                    if (nodeChildren.Items.Count == 1)
                                    {
                                        // Collapsing node...
                                        // Remove the lonely intermediate node from the parent!
                                        nodeChildren.Items.RemoveAt(0);
                                        if (node == DataObject) // Root node [FIX:50].
                                        {
                                            /*
                                              NOTE: In case of root collapse, Kids entry must be converted to
                                              key-value-pairs entry, as no more intermediate nodes are available.
                                            */
                                            node[pairsKey] = node.Get(PdfName.Kids);
                                            node.Remove(PdfName.Kids);
                                            nodeChildren.TypeName = pairsKey;
                                        }
                                        // Populate the parent with the lonely intermediate node's children!
                                        for (int index = kidChildren.Items.Count; index-- > 0;)
                                        {
                                            const int RemovedItemIndex = 0;
                                            PdfDirectObject item = kidChildren.Items.Get(RemovedItemIndex);
                                            kidChildren.Items.RemoveAt(RemovedItemIndex);
                                            nodeChildren.Items.Add(item);
                                        }
                                        kid.Delete();
                                        kid = node;
                                        kidReference = kid.Reference;
                                        kidChildren = nodeChildren;
                                    }
                                }
                                // Update key limits!
                                UpdateNodeLimits(kidChildren);
                            }
                            // Go down one level!
                            nodeReferenceStack.Push(kidReference);
                            node = kid;
                            break;
                        }
                    }
                }
            }
        }

        public virtual TValue this[TKey key]
        {
            get
            {
                PdfDictionary parent = DataObject;
                while (parent != null)
                {
                    Children children = Children.Get(parent, pairsKey);
                    if (children.IsLeaf()) // Leaf node.
                    {
                        return children.BinarySearchLeaf(key, this);
                    }
                    else // Intermediate node.
                    {
                        parent = children.BinaySearch(key);
                    }
                }
                return default;
            }
            set => Add(key, value, true);
        }

        public virtual bool TryGetValue(TKey key, out TValue value)
        {
            value = this[key];
            return value != null;
        }

        public virtual ICollection<TValue> Values
        {
            get
            {
                ValuesFiller filler = new ValuesFiller(this);
                Fill(filler, DataObject);
                return filler.Collection;
            }
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> keyValuePair)
            => Add(keyValuePair.Key, keyValuePair.Value);

        public virtual void Clear() => Clear(DataObject);

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> keyValuePair) =>
            keyValuePair.Value.Equals(this[keyValuePair.Key]);

        public virtual void CopyTo(KeyValuePair<TKey, TValue>[] keyValuePairs, int index)
        { throw new NotImplementedException(); }

        public virtual int Count => GetCount(DataObject);

        public virtual bool IsReadOnly => false;

        public virtual bool Remove(KeyValuePair<TKey, TValue> keyValuePair)
        { throw new NotSupportedException(); }

        public virtual Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public object this[object key] { get => this[(TKey)key]; set => this[(TKey)key] = (TValue)value; }

        /// <summary>Wraps a base object within its corresponding high-level representation.</summary>
        protected abstract TValue WrapValue(PdfDirectObject baseObject);

        /// <summary>Adds an entry into the tree.</summary>
        /// <param name="key">New entry's key.</param>
        /// <param name="value">New entry's value.</param>
        /// <param name="overwrite">Whether the entry is allowed to replace an existing one having the same
        /// key.</param>
        private void Add(TKey key, TValue value, bool overwrite)
        {
            // Get the root node!
            PdfDictionary root = DataObject;

            // Ensuring the root node isn't full...
            {
                Children rootChildren = Children.Get(root, pairsKey);
                if (rootChildren.IsFull())
                {
                    // Transfer the root contents into the new leaf!
                    var leaf = (PdfDictionary)new PdfDictionary().Swap(root);
                    var rootChildrenObject = new PdfArrayImpl(1) { Document.Register(leaf) };
                    root[PdfName.Kids] = rootChildrenObject;
                    // Split the leaf!
                    SplitFullNode(
                      rootChildrenObject,
                      0, // Old root's position within new root's kids.
                      rootChildren.TypeName);
                }
            }

            // Set the entry under the root node!
            Add(key, value, overwrite, root);
        }

        /// <summary>Adds an entry under the given tree node.</summary>
        /// <param name="key">New entry's key.</param>
        /// <param name="value">New entry's value.</param>
        /// <param name="overwrite">Whether the entry is allowed to replace an existing one having the same
        /// key.</param>
        /// <param name="nodeReference">Current node reference.</param>
        private void Add(TKey key, TValue value, bool overwrite, PdfDictionary node)
        {
            Children children = Children.Get(node, pairsKey);
            if (children.IsLeaf()) // Leaf node.
            {
                int childrenSize = children.Items.Count;
                int low = 0, high = childrenSize - children.Info.ItemCount;
                while (true)
                {
                    if (low > high)
                    {
                        // Insert the entry!
                        children.Items.Insert(low, key);
                        children.Items.Insert(++low, value.RefOrSelf);
                        break;
                    }

                    int mid = (mid = ((low + high) / 2)) - (mid % 2);
                    if (mid >= childrenSize)
                    {
                        // Append the entry!
                        children.Items.AddSimple(key);
                        children.Items.Add(value.RefOrSelf);
                        break;
                    }

                    int comparison = key.CompareTo(children.Items.Get(mid));
                    if (comparison < 0) // Before.
                    { high = mid - 2; }
                    else if (comparison > 0) // After.
                    { low = mid + 2; }
                    else // Matching entry.
                    {
                        if (!overwrite)
                            throw new ArgumentException("Key '" + key + "' already exists.", nameof(key));

                        // Overwrite the entry!
                        children.Items.Set(mid, key);
                        children.Items.Set(++mid, value.RefOrSelf);
                        break;
                    }
                }

                // Update the key limits!
                UpdateNodeLimits(children);
            }
            else // Intermediate node.
            {
                int low = 0, high = children.Items.Count - children.Info.ItemCount;
                while (true)
                {
                    bool matched = false;
                    int mid = (low + high) / 2;
                    var kidReference = children.Items.Get(mid);
                    var kid = (PdfDictionary)kidReference.Resolve();
                    var limits = kid.Get<PdfArray>(PdfName.Limits);
                    if (key.CompareTo(limits.Get(0)) < 0) // Before the lower limit.
                    { high = mid - 1; }
                    else if (key.CompareTo(limits.Get(1)) > 0) // After the upper limit.
                    { low = mid + 1; }
                    else // Limit range matched.
                    { matched = true; }

                    if (matched // Limit range matched.
                      || low > high) // No limit range match.
                    {
                        Children kidChildren = Children.Get(kid, pairsKey);
                        if (kidChildren.IsFull())
                        {
                            // Split the node!
                            SplitFullNode(
                              children.Items,
                              mid,
                              kidChildren.TypeName);
                            // Is the key before the split node?
                            if (key.CompareTo(kid.Get<PdfArray>(PdfName.Limits).Get(0)) < 0)
                            {
                                kidReference = children.Items.Get(mid);
                                kid = (PdfDictionary)kidReference.Resolve();
                            }
                        }

                        Add(key, value, overwrite, kid);
                        // Update the key limits!
                        UpdateNodeLimits(children);
                        break;
                    }
                }
            }
        }

        /// <summary>Removes all the given node's children.</summary>
        /// <remarks>
        ///   <para>As this method doesn't apply balancing, it's suitable for clearing root nodes only.
        ///   </para>
        ///   <para>Removal affects only tree nodes: referenced objects are preserved to avoid inadvertently
        ///   breaking possible references to them from somewhere else.</para>
        /// </remarks>
        /// <param name="node">Current node.</param>
        private void Clear(PdfDictionary node)
        {
            var children = Children.Get(node, pairsKey);
            if (!children.IsLeaf())
            {
                foreach (PdfReference child in children.Items.GetItems())
                {
                    Clear((PdfDictionary)child.Resolve());
                    Document.Unregister(child);
                }
                node.Set(pairsKey, node.Get(children.TypeName));
                node.Remove(children.TypeName); // Recycles the array as the intermediate node transforms to leaf.
            }
            children.Items.Clear();
            node.Remove(PdfName.Limits);
        }

        private void Fill<TObject>(IFiller<TObject> filler, PdfDictionary node)
        {
            var kidsObject = node.Get<PdfArray>(PdfName.Kids);
            if (kidsObject == null) // Leaf node.
            {
                var namesObject = node.Get<PdfArray>(pairsKey);
                var length = namesObject.Count;
                for (int index = 0; index < length; index += 2)
                { filler.Add(namesObject, index); }
            }
            else // Intermediate node.
            {
                foreach (var kidObject in kidsObject.GetItems())
                {
                    Fill(filler, (PdfDictionary)kidObject.Resolve());
                }
            }
        }

        /// <summary>Gets the given node's entries count.</summary>
        /// <param name="node">Current node.</param>
        private int GetCount(PdfDictionary node)
        {
            var children = node.Get<PdfArray>(pairsKey);
            if (children != null) // Leaf node.
            { return (children.Count / 2); }
            else // Intermediate node.
            {
                children = node.Get<PdfArray>(PdfName.Kids);
                int count = 0;
                foreach (var child in children.GetItems())
                {
                    count += GetCount((PdfDictionary)child.Resolve());
                }
                return count;
            }
        }

        private void Initialize()
        {
            pairsKey = PairsKey;

            PdfDictionary baseDataObject = DataObject;
            if (baseDataObject.Count == 0)
            {
                baseDataObject.Updateable = false;
                baseDataObject[pairsKey] = new PdfArrayImpl(); // NOTE: Initial root is by definition a leaf node.
                baseDataObject.Updateable = true;
            }
        }

        /// <summary>Splits a full node.</summary>
        /// <remarks>A new node is inserted at the full node's position, receiving the lower half of its
        /// children.</remarks>
        /// <param name="nodes">Parent nodes.</param>
        /// <param name="fullNodeIndex">Full node's position among the parent nodes.</param>
        /// <param name="childrenTypeName">Full node's children type.</param>
        private void SplitFullNode(PdfArray nodes, int fullNodeIndex, PdfName childrenTypeName)
        {
            // Get the full node!
            var fullNode = nodes.Get<PdfDictionary>(fullNodeIndex);
            var fullNodeChildren = fullNode.Get<PdfArray>(childrenTypeName);

            // Create a new (sibling) node!
            var newNode = new PdfDictionary();
            var newNodeChildren = new PdfArrayImpl();
            newNode[childrenTypeName] = newNodeChildren;
            // Insert the new node just before the full!
            nodes.Insert(fullNodeIndex, Document.Register(newNode)); // NOTE: Nodes MUST be indirect objects.

            // Transferring exceeding children to the new node...
            for (int index = 0, length = Children.InfoImpl.Get(childrenTypeName).MinCount; index < length; index++)
            {
                var removedChild = fullNodeChildren.Get(0);
                fullNodeChildren.RemoveAt(0);
                newNodeChildren.Add(removedChild);
            }

            // Update the key limits!
            UpdateNodeLimits(newNode, newNodeChildren, childrenTypeName);
            UpdateNodeLimits(fullNode, fullNodeChildren, childrenTypeName);
        }

        /// <summary>Sets the key limits of the given node.</summary>
        /// <param name="children">Node children.</param>
        private void UpdateNodeLimits(Children children) => UpdateNodeLimits(children.Parent, children.Items, children.TypeName);

        /// <summary>Sets the key limits of the given node.</summary>
        /// <param name="node">Node to update.</param>
        /// <param name="children">Node children.</param>
        /// <param name="childrenTypeName">Node's children type.</param>
        private void UpdateNodeLimits(PdfDictionary node, PdfArray children, PdfName childrenTypeName)
        {
            // Root node?
            if (node == DataObject)
                return; // NOTE: Root nodes DO NOT specify limits.

            PdfDirectObject lowLimit, highLimit;
            if (childrenTypeName.Equals(PdfName.Kids))
            {
                lowLimit = children.Get<PdfDictionary>(0).Get<PdfArray>(PdfName.Limits).Get(0);
                highLimit = children.Get<PdfDictionary>(children.Count - 1).Get<PdfArray>(PdfName.Limits).Get(1);
            }
            else if (childrenTypeName.Equals(pairsKey))
            {
                lowLimit = children.Get(0);
                highLimit = children.Get(children.Count - 2);
            }
            else // NOTE: Should NEVER happen.
                throw new NotSupportedException(childrenTypeName + " is NOT a supported child type.");

            var limits = node.Get<PdfArray>(PdfName.Limits);
            if (limits != null)
            {
                limits.Set(0, lowLimit);
                limits.Set(1, highLimit);
            }
            else
            {
                node[PdfName.Limits] = new PdfArrayImpl(2) { lowLimit, highLimit };
            }
        }

        void IDictionary.Add(object key, object value) => Add((TKey)key, (TValue)value);

        bool IDictionary.Contains(object key) => ContainsKey((TKey)key);

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        void IDictionary.Remove(object key) => Remove((TKey)key);

        void ICollection.CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }
    }
}