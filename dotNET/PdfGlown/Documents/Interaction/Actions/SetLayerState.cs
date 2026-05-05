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

using PdfGlown.Documents.Contents.Layers;
using PdfGlown.Objects;
using PdfGlown.Util;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PdfGlown.Documents.Interaction.Actions
{
    /// <summary>'Set the state of one or more optional content groups' action [PDF:1.6:8.5.3].</summary>
    [Pdf(VersionEnum.PDF15)]
    public sealed class SetLayerState : PdfAction
    {
        private LayerStates states;

        public enum StateModeEnum
        {
            On,
            Off,
            Toggle
        }

        public class LayerState
        {
            internal class LayersImpl : Collection<Layer>
            {
                internal LayerState parentState;

                protected override void ClearItems()
                {
                    // Low-level definition.
                    LayerStates baseStates = BaseStates;
                    if (baseStates != null)
                    {
                        int itemIndex = baseStates.GetBaseIndex(parentState)
                          + 1; // Name object offset.
                        for (int count = Count; count > 0; count--)
                        { baseStates.DataObject.RemoveAt(itemIndex); }
                    }
                    // High-level definition.
                    base.ClearItems();
                }

                protected override void InsertItem(int index, Layer item)
                {
                    // High-level definition.
                    base.InsertItem(index, item);
                    // Low-level definition.
                    LayerStates baseStates = BaseStates;
                    if (baseStates != null)
                    {
                        int baseIndex = baseStates.GetBaseIndex(parentState);
                        int itemIndex = baseIndex
                          + 1 // Name object offset.
                          + index; // Layer object offset.
                        baseStates.DataObject.Set(itemIndex, item.RefOrSelf);
                    }
                }

                protected override void RemoveItem(int index)
                {
                    // High-level definition.
                    base.RemoveItem(index);
                    // Low-level definition.
                    LayerStates baseStates = BaseStates;
                    if (baseStates != null)
                    {
                        int baseIndex = baseStates.GetBaseIndex(parentState);
                        int itemIndex = baseIndex
                          + 1 // Name object offset.
                          + index; // Layer object offset.
                        baseStates.DataObject.RemoveAt(itemIndex);
                    }
                }

                protected override void SetItem(int index, Layer item)
                {
                    RemoveItem(index);
                    InsertItem(index, item);
                }

                private LayerStates BaseStates => parentState?.baseStates;
            }

            private readonly LayersImpl layers;
            private StateModeEnum mode;

            private LayerStates baseStates;

            public LayerState(StateModeEnum mode, params Layer[] layers)
                : this(mode, new LayersImpl(), null)
            {
                foreach (var layer in layers)
                { this.layers.Add(layer); }
            }

            internal LayerState(StateModeEnum mode, LayersImpl layers, LayerStates baseStates)
            {
                this.mode = mode;
                this.layers = layers;
                this.layers.parentState = this;
                Attach(baseStates);
            }

            public override bool Equals(object obj)
            {
                if (!(obj is LayerState))
                    return false;

                LayerState state = (LayerState)obj;
                if (!state.Mode.Equals(Mode)
                  || state.Layers.Count != Layers.Count)
                    return false;

                IEnumerator<Layer> layerIterator = Layers.GetEnumerator();
                IEnumerator<Layer> stateLayerIterator = state.Layers.GetEnumerator();
                while (layerIterator.MoveNext())
                {
                    stateLayerIterator.MoveNext();
                    if (!layerIterator.Current.Equals(stateLayerIterator.Current))
                        return false;
                }
                return true;
            }

            public IList<Layer> Layers => layers;

            public StateModeEnum Mode
            {
                get => mode;
                set
                {
                    mode = value;

                    if (baseStates != null)
                    {
                        int baseIndex = baseStates.GetBaseIndex(this);
                        baseStates.DataObject.Set(baseIndex, GetName(value));
                    }
                }
            }

            public override int GetHashCode() => HashCode.Combine(layers, mode);

            internal void Attach(LayerStates baseStates) => this.baseStates = baseStates;

            internal void Detach() => baseStates = null;
        }

        public class LayerStates : PdfObjectWrapper<PdfArray>, IList<LayerState>
        {
            private List<LayerState> items;

            public LayerStates()
                : base(new PdfArrayImpl())
            { }

            public LayerStates(PdfDirectObject baseObject) : base(baseObject)
            { Initialize(); }

            public int IndexOf(LayerState item)
            { return items.IndexOf(item); }

            public void Insert(int index, LayerState item)
            {
                int baseIndex = GetBaseIndex(index);
                if (baseIndex == -1)
                { Add(item); }
                else
                {
                    PdfArray baseDataObject = DataObject;
                    // Low-level definition.
                    baseDataObject.Insert(baseIndex++, GetName(item.Mode));
                    foreach (Layer layer in item.Layers)
                    { baseDataObject.Insert(baseIndex++, layer.RefOrSelf); }
                    // High-level definition.
                    items.Insert(index, item);
                    item.Attach(this);
                }
            }

            public void RemoveAt(int index)
            {
                LayerState layerState;
                // Low-level definition.
                {
                    int baseIndex = GetBaseIndex(index);
                    if (baseIndex == -1)
                        throw new IndexOutOfRangeException();

                    PdfArray baseDataObject = DataObject;
                    bool done = false;
                    for (int baseCount = baseDataObject.Count; baseIndex < baseCount;)
                    {
                        if (baseDataObject.Get(baseIndex) is PdfName)
                        {
                            if (done)
                                break;

                            done = true;
                        }
                        baseDataObject.RemoveAt(baseIndex);
                    }
                }
                // High-level definition.
                {
                    layerState = items[index];
                    items.RemoveAt(index);
                    layerState.Detach();
                }
            }

            public LayerState this[int index]
            {
                get => items[index];
                set
                {
                    RemoveAt(index);
                    Insert(index, value);
                }
            }

            public void Add(LayerState item)
            {
                var baseDataObject = DataObject;
                // Low-level definition.
                baseDataObject.AddSimple(GetName(item.Mode));
                foreach (Layer layer in item.Layers)
                { baseDataObject.Add(layer.RefOrSelf); }
                // High-level definition.
                items.Add(item);
                item.Attach(this);
            }

            public void Clear()
            {
                // Low-level definition.
                DataObject.Clear();
                // High-level definition.
                foreach (LayerState item in items)
                { item.Detach(); }
                items.Clear();
            }

            public bool Contains(LayerState item) => items.Contains(item);

            public void CopyTo(LayerState[] entries, int index)
            {
                foreach (var entry in this)
                {
                    entries[index++] = entry;
                }
            }

            public int Count => items.Count;

            public bool IsReadOnly => false;

            public bool Remove(LayerState item)
            {
                int index = IndexOf(item);
                if (index == -1)
                    return false;

                RemoveAt(index);
                return true;
            }

            public List<LayerState>.Enumerator GetEnumerator() => items.GetEnumerator();

            IEnumerator<LayerState> IEnumerable<LayerState>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            /// <summary>Gets the position of the initial base item corresponding to the specified layer
            /// state index.</summary>
            /// <param name="index">Layer state index.</param>
            /// <returns>-1, in case <code>index</code> is outside the available range.</returns>
            internal int GetBaseIndex(int index)
            {
                int baseIndex = -1;
                {
                    PdfArray baseDataObject = DataObject;
                    int layerStateIndex = -1;
                    for (int baseItemIndex = 0, baseItemCount = baseDataObject.Count; baseItemIndex < baseItemCount; baseItemIndex++)
                    {
                        if (baseDataObject.Get(baseItemIndex) is PdfName)
                        {
                            layerStateIndex++;
                            if (layerStateIndex == index)
                            {
                                baseIndex = baseItemIndex;
                                break;
                            }
                        }
                    }
                }
                return baseIndex;
            }

            /// <summary>Gets the position of the initial base item corresponding to the specified layer
            /// state.</summary>
            /// <param name="item">Layer state.</param>
            /// <returns>-1, in case <code>item</code> has no match.</returns>
            internal int GetBaseIndex(LayerState item)
            {
                int baseIndex = -1;
                {
                    PdfArray baseDataObject = DataObject;
                    for (int baseItemIndex = 0, baseItemCount = baseDataObject.Count; baseItemIndex < baseItemCount; baseItemIndex++)
                    {
                        var baseItem = baseDataObject.Get(baseItemIndex);
                        if (baseItem is PdfName baseName
                          && baseName.Equals(GetName(item.Mode)))
                        {
                            foreach (Layer layer in item.Layers)
                            {
                                if (++baseItemIndex >= baseItemCount)
                                    break;

                                baseItem = baseDataObject.Get(baseItemIndex);
                                if (baseItem is PdfName
                                  || !baseItem.Equals(layer.RefOrSelf))
                                    break;
                            }
                        }
                    }
                }
                return baseIndex;
            }

            private void Initialize()
            {
                items = new List<LayerState>();
                PdfArray baseDataObject = DataObject;
                StateModeEnum? mode = null;
                LayerState.LayersImpl layers = null;
                for (int baseIndex = 0, baseCount = baseDataObject.Count; baseIndex < baseCount; baseIndex++)
                {
                    var baseObject = baseDataObject.Get(baseIndex);
                    if (baseObject is PdfName)
                    {
                        if (mode.HasValue)
                        { items.Add(new LayerState(mode.Value, layers, this)); }
                        mode = GetStateMode((PdfName)baseObject);
                        layers = new LayerState.LayersImpl();
                    }
                    else
                    { layers.Add((Layer)baseObject.Resolve(PdfName.OCG)); }
                }
                if (mode.HasValue)
                { items.Add(new LayerState(mode.Value, layers, this)); }
            }
        }

        private static readonly BiDictionary<StateModeEnum, PdfName> stateModeCodes = new()
        {
            [StateModeEnum.On] = PdfName.ON,
            [StateModeEnum.Off] = PdfName.OFF,
            [StateModeEnum.Toggle] = PdfName.Toggle
        };

        public static StateModeEnum GetStateMode(PdfName name)
        {
            if (name == null)
                throw new ArgumentNullException("name");

            StateModeEnum? stateMode = stateModeCodes.GetKey(name);
            if (!stateMode.HasValue)
                throw new NotSupportedException("State mode unknown: " + name);

            return stateMode.Value;
        }

        public static PdfName GetName(StateModeEnum stateMode) => stateModeCodes[stateMode];

        /// <summary>Creates a new action within the given document context.</summary>
        public SetLayerState(PdfDocument context)
            : this(context, (LayerState)null)
        { }

        /// <summary>Creates a new action within the given document context.</summary>
        public SetLayerState(PdfDocument context, params LayerState[] states)
            : base(context, PdfName.SetOCGState)
        {
            States = new LayerStates();
            if (states != null && states.Length > 0)
            {
                var layerStates = States;
                foreach (var state in states)
                { layerStates.Add(state); }
            }
        }

        internal SetLayerState(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        public LayerStates States
        {
            get => states ??= new LayerStates(Get(PdfName.State));
            set => Set(PdfName.State, value);
        }

        public override string GetDisplayName() => "Set Layer State";
    }


}