/*
  Copyright 2011-2015 Stefano Chizzolini. http://www.pdfclown.org

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
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents.Layers
{
    /// <summary>Optional content group collection.</summary>
    [Pdf(VersionEnum.PDF15)]
    public class UILayers : ArrayWrapper<IUILayerNode>
    {
        private delegate int EvaluateNode(int currentNodeIndex, int currentBaseIndex);

        private class ItemWrapper : IEntryWrapper<IUILayerNode>
        {
            private readonly Dictionary<PdfDirectObject, IUILayerNode> cache = new();

            public IUILayerNode Wrap(PdfDirectObject baseObject)
            {
                return baseObject?.Resolve(PdfName.OCG) is PdfDirectObject dataObject
                    ? cache.TryGetValue(baseObject, out var uiLayer) ? uiLayer : cache[baseObject] = Wrap(baseObject, dataObject)
                    : null;
            }

            private static IUILayerNode Wrap(PdfDirectObject baseObject, PdfDirectObject dataObject)
            {
                if (dataObject is Layer layer)
                    return layer;
                else if (dataObject is PdfArray)
                    return new LayerCollection(baseObject);
                else
                    throw new ArgumentException(dataObject.GetType().Name + " is NOT a valid layer node.");
            }
        }

        public UILayers(PdfDocument context)
            : base(context, new ItemWrapper())
        { }

        public UILayers(PdfDirectObject baseObject)
            : base(baseObject, new ItemWrapper())
        { }

        public override int Count => Evaluate(delegate (int currentNodeIndex, int currentBaseIndex)
                                                   {
                                                       if (currentBaseIndex == -1)
                                                           return currentNodeIndex;
                                                       else
                                                           return -1;
                                                   }) + 1;

        public override int IndexOf(IUILayerNode item) => GetNodeIndex(base.IndexOf(item));

        public override void Insert(int index, IUILayerNode item) => base.Insert(GetBaseIndex(index), item);

        public override void RemoveAt(int index)
        {
            int baseIndex = GetBaseIndex(index);
            IUILayerNode removedItem = base[baseIndex];
            base.RemoveAt(baseIndex);
            if (removedItem is Layer
              && baseIndex < base.Count)
            {
                // NOTE: Sublayers MUST be removed as well.
                if (DataObject.Get<PdfArray>(baseIndex) != null)
                {
                    DataObject.RemoveAt(baseIndex);
                }
            }
        }

        public override IUILayerNode this[int index]
        {
            get => base[GetBaseIndex(index)];
            set => base[GetBaseIndex(index)] = value;
        }

        /// <summary>Gets the positional information resulting from the collection evaluation.</summary>
        /// <param name="evaluator">Expression used to evaluate the positional matching.</param>
        private int Evaluate(EvaluateNode evaluateNode)
        {
            // NOTE: Layer hierarchies are represented through a somewhat flatten structure which needs
            // to be evaluated in order to match nodes in their actual place.
            PdfArray baseDataObject = DataObject;
            int nodeIndex = -1;
            bool groupAllowed = true;
            for (int baseIndex = 0, baseLength = base.Count; baseIndex < baseLength; baseIndex++)
            {
                var itemDataObject = baseDataObject.Get<PdfDirectObject>(baseIndex);
                if (itemDataObject is PdfDictionary
                  || (itemDataObject is PdfArray && groupAllowed))
                {
                    nodeIndex++;
                    int evaluation = evaluateNode(nodeIndex, baseIndex);
                    if (evaluation > -1)
                        return evaluation;
                }
                groupAllowed = itemDataObject is not PdfDictionary;
            }
            return evaluateNode(nodeIndex, -1);
        }

        private int GetBaseIndex(int nodeIndex)
        {
            return Evaluate(delegate (int currentNodeIndex, int currentBaseIndex)
            {
                if (currentNodeIndex == nodeIndex)
                    return currentBaseIndex;
                else
                    return -1;
            });
        }

        private int GetNodeIndex(int baseIndex)
        {
            return Evaluate(delegate (int currentNodeIndex, int currentBaseIndex)
            {
                if (currentBaseIndex == baseIndex)
                    return currentNodeIndex;
                else
                    return -1;
            });
        }
    }
}