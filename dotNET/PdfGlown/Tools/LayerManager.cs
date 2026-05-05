/*
  Copyright 2015 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfGlown.Documents;
using PdfGlown.Documents.Contents;
using PdfGlown.Documents.Contents.Layers;
using PdfGlown.Documents.Contents.Objects;
using PdfGlown.Documents.Contents.XObjects;
using PdfGlown.Objects;
using System.Collections.Generic;

namespace PdfGlown.Tools
{
    ///<summary>Tool to manage layers (aka OCGs).</summary>
    public class LayerManager
    {
        /// <summary>Removes the specified layers from the document.</summary>
        /// <param name="layers">Layers to remove (they MUST belong to the same document).</param>
        public void Remove(params Layer[] layers) => Remove(false, layers);

        /// <summary>Removes the specified layers from the document.</summary>
        /// <param name="preserveContent">Whether the layer contents have to be flattened only.</param>
        /// <param name="layers">Layers to remove (they MUST belong to the same document).</param>
        public void Remove(bool preserveContent, params Layer[] layers)
        {
            var catalog = layers[0].Catalog;

            // 1. Page contents.
            var removedLayers = new HashSet<Layer>(layers);
            var layerEntities = new HashSet<LayerEntity>(removedLayers);
            var layerXObjects = new HashSet<XObject>();
            foreach (var page in catalog.Pages)
            { RemoveLayerContents(page, removedLayers, layerEntities, layerXObjects, preserveContent); }

            // 2. Layer definitions.
            var removedLayerReferences = new HashSet<PdfReference>();
            foreach (var removedLayer in removedLayers)
            { removedLayerReferences.Add((PdfReference)removedLayer.RefOrSelf); }
            var layerDefinition = catalog.Layers;
            // 2.1. Clean default layer configuration!
            RemoveLayerReferences(layerDefinition.DefaultConfiguration, removedLayerReferences);
            // 2.2. Clean alternate layer configurations!
            foreach (var layerConfiguration in layerDefinition.AlternateConfigurations)
            { RemoveLayerReferences(layerConfiguration, removedLayerReferences); }
            // 2.3. Clean global layer collection!
            RemoveLayerReferences(layerDefinition.Layers.DataObject, removedLayerReferences);

            // 3. Entities.
            // 3.1. Clean the xobjects!
            foreach (var xObject in layerXObjects)
            {
                if (preserveContent)
                { xObject.Layer = null; }
                else
                { xObject.Delete(); }
            }
            // 3.2. Clean the layer entities!
            foreach (var layerEntity in layerEntities)
            { layerEntity.Delete(); }

            // 4. Reference cleanup.
            Optimizer.RemoveOrphanedObjects(catalog.Document);
        }

        private void RemoveLayerContents(PdfPage page, ICollection<Layer> removedLayers, ICollection<LayerEntity> layerEntities, ICollection<XObject> layerXObjects, bool preserveContent)
        {
            var pageResources = page.Resources;

            // Collect the page's layer entities containing the layers!
            var layerEntityNames = new HashSet<PdfName>();
            var pagePropertyLists = pageResources.PropertyLists;
            foreach (var propertyListEntry in pagePropertyLists)
            {
                if (propertyListEntry.Value is not LayerEntity layerEntity)
                    continue;

                if (layerEntities.Contains(layerEntity))
                {
                    layerEntityNames.Add(propertyListEntry.Key);
                }
                else
                {
                    var members = layerEntity.VisibilityMembers;
                    foreach (var removedLayer in removedLayers)
                    {
                        if (members.Contains(removedLayer))
                        {
                            layerEntityNames.Add(propertyListEntry.Key);
                            layerEntities.Add(layerEntity);
                            break;
                        }
                    }
                }
            }

            // Collect the page's xobjects associated to the layers!
            var layerXObjectNames = new HashSet<PdfName>();
            var pageXObjects = pageResources.XObjects;
            foreach (var xObjectEntry in pageXObjects)
            {
                if (layerXObjects.Contains(xObjectEntry.Value))
                { layerXObjectNames.Add(xObjectEntry.Key); }
                else
                {
                    if (layerEntities.Contains(xObjectEntry.Value.Layer))
                    {
                        layerXObjectNames.Add(xObjectEntry.Key);
                        layerXObjects.Add(xObjectEntry.Value);
                        break;
                    }
                }
            }

            // 1.1. Remove the layered contents from the page!
            if (layerEntityNames.Count > 0 || (!preserveContent && layerXObjectNames.Count > 0))
            {
                var scanner = new ContentScanner(page);
                RemoveLayerContents(scanner, layerEntityNames, layerXObjectNames, preserveContent);
                scanner.Contents.Flush();
            }

            // 1.2. Clean the page's layer entities from the purged references!
            foreach (var layerEntityName in layerEntityNames)
            { pagePropertyLists.Remove(layerEntityName); }

            // 1.3. Clean the page's xobjects from the purged references!
            if (!preserveContent)
            {
                foreach (var layerXObjectName in layerXObjectNames)
                { pageXObjects.Remove(layerXObjectName); }
            }

            // 1.4. Clean the page's annotations!
            {
                var pageAnnotations = page.Annotations;
                for (int index = pageAnnotations.Count - 1; index >= 0; index--)
                {
                    var annotation = pageAnnotations[index];
                    if (layerEntities.Contains(annotation.Layer))
                    {
                        if (preserveContent)
                        { annotation.Layer = null; }
                        else
                        { annotation.Delete(); }
                    }
                }
            }
        }

        private void RemoveLayerContents(ContentScanner level, HashSet<PdfName> layerEntityNames, HashSet<PdfName> layerXObjectNames, bool preserveContent)
        {
            if (level == null)
                return;
            level.OnObjectScanning += OnStart;
            level.Scan();
            level.OnObjectScanning -= OnStart;
            bool OnStart(ContentObject content, ICompositeObject container, int index)
            {
                if (content is GraphicsMarkedContent markedContent)
                {
                    var marker = (ContentMarker)markedContent.Header;
                    if (PdfName.OC.Equals(marker.Tag) // NOTE: /OC tag identifies layer (aka optional content) markers.
                      && layerEntityNames.Contains(marker.Name))
                    {
                        if (preserveContent)
                        {
                            container.Contents[index] = new ContentPlaceholder(markedContent.Contents); // Replaces the layer marked content block with an anonymous container, preserving its contents.
                        }
                        else
                        {
                            container.Contents.RemoveAt(index); // Removes the layer marked content block along with its contents.
                        }
                        return false;
                    }
                }
                else if (!preserveContent && content is PaintXObject xObject)
                {
                    if (layerXObjectNames.Contains(xObject.Name))
                    {
                        container.Contents.RemoveAt(index);
                        return false;
                    }
                }
                return true;
            }
        }

        private static void RemoveLayerReferences(LayerConfiguration layerConfiguration, ICollection<PdfReference> layerReferences)
        {
            if (layerConfiguration == null)
                return;

            var usageArrayObject = layerConfiguration.Get<PdfArray>(PdfName.AS);
            if (usageArrayObject != null)
            {
                foreach (var usageItemObject in usageArrayObject.GetItems())
                {
                    RemoveLayerReferences((PdfDictionary)usageItemObject.Resolve(PdfName.AS), PdfName.OCGs, layerReferences);
                }
            }
            RemoveLayerReferences(layerConfiguration, PdfName.Locked, layerReferences);
            RemoveLayerReferences(layerConfiguration, PdfName.OFF, layerReferences);
            RemoveLayerReferences(layerConfiguration, PdfName.ON, layerReferences);
            RemoveLayerReferences(layerConfiguration, PdfName.Order, layerReferences);
            RemoveLayerReferences(layerConfiguration, PdfName.RBGroups, layerReferences);
        }

        private static void RemoveLayerReferences(PdfDictionary dictionaryObject, PdfName key, ICollection<PdfReference> layerReferences)
        {
            if (dictionaryObject == null)
                return;

            RemoveLayerReferences(dictionaryObject.Get<PdfArray>(key), layerReferences);
        }

        private static void RemoveLayerReferences(PdfArray arrayObject, ICollection<PdfReference> layerReferences)
        {
            if (arrayObject == null)
                return;

            for (int index = arrayObject.Count - 1; index >= 0; index--)
            {
                var itemObject = arrayObject.Get(index);
                if (itemObject is PdfReference pdfReference)
                {
                    if (layerReferences.Contains(pdfReference))
                    {
                        arrayObject.RemoveAt(index);

                        if (index < arrayObject.Count)
                        {
                            if (arrayObject.Get<PdfArray>(index) != null) // Children array.
                            {
                                arrayObject.RemoveAt(index);
                            }
                        }
                        continue;
                    }
                    else
                    { itemObject = itemObject.Resolve(); }
                }
                if (itemObject is PdfArray pdfArray)
                {
                    RemoveLayerReferences(pdfArray, layerReferences);
                }
            }
        }
    }
}

