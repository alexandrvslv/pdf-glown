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
using PdfGlown.Util;

using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfGlown.Documents.Contents.Layers
{
    /// <summary>Optional content configuration [PDF:1.7:4.10.3].</summary>
    [Pdf(VersionEnum.PDF15)]
    public sealed class LayerConfiguration : PdfDictionary, ILayerConfiguration
    {
        /// <summary>Base state used to initialize the states of all the layers in a document when this
        /// configuration is applied.</summary>
        internal enum BaseStateEnum
        {
            /// <summary>All the layers are visible.</summary>
            On,
            /// <summary>All the layers are invisible.</summary>
            Off,
            /// <summary>All the layers are left unchanged.</summary>
            Unchanged
        }

        private static readonly BiDictionary<BaseStateEnum, string> baseStateCodes = new()
        {
            [BaseStateEnum.On] = PdfName.ON.StringValue,
            [BaseStateEnum.Off] = PdfName.OFF.StringValue,
            [BaseStateEnum.Unchanged] = PdfName.Unchanged.StringValue
        };

        private static readonly BiDictionary<UIModeEnum, string> uiModeCodes = new()
        {
            [UIModeEnum.AllPages] = PdfName.AllPages.StringValue,
            [UIModeEnum.VisiblePages] = PdfName.VisiblePages.StringValue
        };

        public static UIModeEnum GetUIMode(string name)
        {
            if (name == null)
                return UIModeEnum.AllPages;

            UIModeEnum? uiMode = uiModeCodes.GetKey(name);
            if (!uiMode.HasValue)
                throw new NotSupportedException("UI mode unknown: " + name);

            return uiMode.Value;
        }

        public static PdfName GetName(UIModeEnum uiMode) => PdfName.Get(uiModeCodes[uiMode], true);

        internal static BaseStateEnum GetBaseState(string name)
        {
            if (name == null)
                return BaseStateEnum.On;

            BaseStateEnum? baseState = baseStateCodes.GetKey(name);
            if (!baseState.HasValue)
                throw new NotSupportedException("Base state unknown: " + name);

            return baseState.Value;
        }

        internal static BaseStateEnum GetBaseState(bool? enabled) => enabled.HasValue
            ? (enabled.Value ? BaseStateEnum.On : BaseStateEnum.Off)
            : BaseStateEnum.Unchanged;

        internal static PdfName GetName(BaseStateEnum baseState) => PdfName.Get(baseStateCodes[baseState], true);

        internal static bool? IsEnabled(BaseStateEnum baseState)
        {
            return baseState switch
            {
                BaseStateEnum.On => true,
                BaseStateEnum.Off => false,
                BaseStateEnum.Unchanged => null,
                _ => throw new NotSupportedException(),
            };
        }

        private ISet<PdfName> intents;
        private OptionGroups optionGroups;
        private UILayers uiLayers;

        public LayerConfiguration()
            : this((PdfDocument)null)
        { }

        public LayerConfiguration(PdfDocument context)
            : base(context, new())
        { }

        internal LayerConfiguration(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        public string Creator
        {
            get => GetString(PdfName.Creator);
            set => SetText(PdfName.Creator, value);
        }

        public ISet<PdfName> Intents
        {
            get => intents ??= GetIntents();
            set
            {
                PdfDirectObject intentObject = null;
                if (value != null
                  && value.Count > 0)
                {
                    if (value.Count == 1) // Single intent.
                    {
                        intentObject = value.First();
                        if (intentObject.Equals(IntentEnum.View.Name())) // Default.
                        { intentObject = null; }
                    }
                    else // Multiple intents.
                    {
                        var intentArray = new PdfArrayImpl();
                        foreach (PdfName valueItem in value)
                        { intentArray.AddSimple(valueItem); }
                    }
                }
                this[PdfName.Intent] = intentObject;
            }
        }

        private HashSet<PdfName> GetIntents()
        {
            var intents = new HashSet<PdfName>();
            var intentObject = Get<PdfDirectObject>(PdfName.Intent);
            if (intentObject != null)
            {
                if (intentObject is PdfArray array) // Multiple intents.
                {
                    foreach (var intentItem in array.GetItems().Select(x => (PdfName)x.Resolve(PdfName.Intent)))
                    {
                        intents.Add(intentItem);
                    }
                }
                else // Single intent.
                {
                    intents.Add((PdfName)intentObject);
                }
            }
            else
            {
                intents.Add(IntentEnum.View.Name());
            }
            return intents;
        }

        public OptionGroups OptionGroups
        {
            get => optionGroups ??= new OptionGroups(GetOrCreate<PdfArrayImpl>(PdfName.RBGroups));
        }

        public string Title
        {
            get => GetString(PdfName.Name);
            set => SetText(PdfName.Name, value);
        }

        public UILayers UILayers
        {
            get => uiLayers ??= new UILayers(GetOrCreate<PdfArrayImpl>(PdfName.Order));
        }

        public UIModeEnum UIMode
        {
            get => GetUIMode(GetString(PdfName.ListMode));
            set => this[PdfName.ListMode] = GetName(value);
        }

        public bool? Visible
        {
            get => IsEnabled(GetBaseState(GetString(PdfName.BaseState)));
            set
            {
                // NOTE: Base state can be altered only in case of alternate configuration; default ones MUST
                // be set to default state (that is ON).
                if (RefOrSelf.ParentObject is not PdfDictionary) // Not the default configuration?
                {
                    this[PdfName.BaseState] = GetName(GetBaseState(value));
                }
            }
        }

        internal bool IsVisible(Layer layer)
        {
            bool? visible = Visible;
            if (!visible.HasValue || visible.Value)
                return !OffLayersObject.Contains(layer.Reference);
            else
                return OnLayersObject.Contains(layer.Reference);
        }

        /// <summary>Sets the usage application for the specified factors.</summary>
        /// <param name="event">Situation in which this usage application should be used. May be
        ///   <see cref="PdfName.View">View</see>, <see cref="PdfName.Print">Print</see> or <see
        ///   cref="PdfName.Export">Export</see>.</param>
        /// <param name="category">Layer usage entry to consider when managing the states of the layer.
        /// </param>
        /// <param name="layer">Layer which should have its state automatically managed based on its usage
        ///   information.</param>
        /// <param name="retain">Whether this usage application has to be kept or removed.</param>
        internal void SetUsageApplication(PdfName @event, PdfName category, Layer layer, bool retain)
        {
            bool matched = false;
            var usages = GetOrCreate<PdfArrayImpl>(PdfName.AS);
            foreach (var usage in usages)
            {
                var usageDictionary = (PdfDictionary)usage.Resolve(PdfName.AS);
                if (usageDictionary.Get(PdfName.Event).Equals(@event)
                  && usageDictionary.Get<PdfArray>(PdfName.Category).Contains(category))
                {
                    var usageLayers = usageDictionary.GetOrCreate<PdfArrayImpl>(PdfName.OCGs);
                    if (usageLayers.Contains(layer.Reference))
                    {
                        if (!retain)
                            usageLayers.Remove(layer.Reference);
                    }
                    else
                    {
                        if (retain)
                            usageLayers.Add(layer.Reference);
                    }
                    matched = true;
                }
            }
            if (!matched && retain)
            {
                var usageDictionary = new PdfDictionary()
                {
                    [PdfName.Event] = @event
                };
                usageDictionary.GetOrCreate<PdfArrayImpl>(PdfName.Category).Add(category);
                usageDictionary.GetOrCreate<PdfArrayImpl>(PdfName.OCGs).Add(layer.Reference);
                usages.Add(usageDictionary);
            }
        }

        internal void SetVisible(Layer layer, bool value)
        {
            var layerObject = layer.Reference;
            PdfArray offLayersObject = OffLayersObject;
            PdfArray onLayersObject = OnLayersObject;
            bool? visible = Visible;
            if (!visible.HasValue)
            {
                if (value && !onLayersObject.Contains(layerObject))
                {
                    onLayersObject.Add(layerObject);
                    offLayersObject.Remove(layerObject);
                }
                else if (!value && !offLayersObject.Contains(layerObject))
                {
                    offLayersObject.Add(layerObject);
                    onLayersObject.Remove(layerObject);
                }
            }
            else if (!visible.Value)
            {
                if (value && !onLayersObject.Contains(layerObject))
                { onLayersObject.Add(layerObject); }
            }
            else
            {
                if (!value && !offLayersObject.Contains(layerObject))
                { offLayersObject.Add(layerObject); }
            }
        }

        /// <summary>Gets the collection of the layer objects whose state is set to OFF.</summary>
        private PdfArray OffLayersObject => GetOrCreate<PdfArrayImpl>(PdfName.OFF);

        /// <summary>Gets the collection of the layer objects whose state is set to ON.</summary>
        private PdfArray OnLayersObject => GetOrCreate<PdfArrayImpl>(PdfName.ON);
    }
}