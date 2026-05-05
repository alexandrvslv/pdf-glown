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
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents.Layers
{
    /// <summary>Optional content properties [PDF:1.7:4.10.3].</summary>
    [Pdf(VersionEnum.PDF15)]
    public sealed class LayerDefinition : PdfDictionary, ILayerConfiguration
    {
        private Layers layers;

        public LayerDefinition()
            : base()
        { }

        public LayerDefinition(PdfDocument context)
            : base(context, new())
        { }

        internal LayerDefinition(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }
        public override PdfName ModifyTypeKey(PdfName key) => key == PdfName.D ? PdfName.Config : key;

        /// <summary>Gets the layer configurations used under particular circumstances.</summary>
        public LayerConfigurations AlternateConfigurations
        {
            get => GetOrCreate<LayerConfigurations>(PdfName.Configs);
            set => Set(PdfName.Configs, value);
        }

        /// <summary>Gets the default layer configuration, that is the initial state of the optional
        /// content groups when a document is first opened.</summary>
        public LayerConfiguration DefaultConfiguration
        {
            get => GetOrCreate<LayerConfiguration>(PdfName.D);
            set => Set(PdfName.D, value);
        }

        /// <summary>Gets the collection of all the layers existing in the document.</summary>
        public Layers Layers
        {
            get => layers ??= new(GetOrCreate<PdfArrayImpl>(PdfName.OCGs));
            set => Set(PdfName.OCGs, layers = value);
        }

        public string Creator
        {
            get => DefaultConfiguration.Creator;
            set => DefaultConfiguration.Creator = value;
        }

        public ISet<PdfName> Intents
        {
            get => DefaultConfiguration.Intents;
            set => DefaultConfiguration.Intents = value;
        }

        public OptionGroups OptionGroups => DefaultConfiguration.OptionGroups;

        public string Title
        {
            get => DefaultConfiguration.Title;
            set => DefaultConfiguration.Title = value;
        }

        public UILayers UILayers => DefaultConfiguration.UILayers;

        public UIModeEnum UIMode
        {
            get => DefaultConfiguration.UIMode;
            set => DefaultConfiguration.UIMode = value;
        }

        public bool? Visible
        {
            get => DefaultConfiguration.Visible;
            set => DefaultConfiguration.Visible = value;
        }
    }
}

