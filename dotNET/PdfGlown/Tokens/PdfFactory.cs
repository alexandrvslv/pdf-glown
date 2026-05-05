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

using PdfGlown.Documents;
using PdfGlown.Documents.Contents;
using PdfGlown.Documents.Contents.ColorSpaces;
using PdfGlown.Documents.Contents.Fonts;
using PdfGlown.Documents.Contents.Layers;
using PdfGlown.Documents.Contents.Patterns;
using PdfGlown.Documents.Contents.Shadings;
using PdfGlown.Documents.Contents.XObjects;
using PdfGlown.Documents.Encryption;
using PdfGlown.Documents.Files;
using PdfGlown.Documents.Functions;
using PdfGlown.Documents.Interaction.Actions;
using PdfGlown.Documents.Interaction.Annotations;
using PdfGlown.Documents.Interaction.Forms;
using PdfGlown.Documents.Interaction.Forms.Signature;
using PdfGlown.Documents.Interaction.Navigation;
using PdfGlown.Documents.Interaction.Viewer;
using PdfGlown.Documents.Interchange.Metadata;
using PdfGlown.Documents.Multimedia;
using PdfGlown.Documents.Names;
using PdfGlown.Files;
using PdfGlown.Objects;

using System;
using System.Collections.Generic;

namespace PdfGlown.Tokens
{
    public static class PdfFactory
    {
        private static readonly Dictionary<PdfName, PdfName> mapParentKeys = new()
        {
            { PdfName.AA, PdfName.AA },
            { PdfName.A, PdfName.Action },
            { PdfName.AcroForm, PdfName.AcroForm },
            { PdfName.Action, PdfName.Action },
            { PdfName.Annot, PdfName.Annot },
            { PdfName.AP, PdfName.AP },
            { PdfName.Page, PdfName.Page },
            { PdfName.Outlines, PdfName.Outlines },
            { PdfName.Outline, PdfName.Outline },
            { PdfName.Info, PdfName.Info },
            { PdfName.I, PdfName.Info },
            { PdfName.Function, PdfName.Function },
            { PdfName.FontFile, PdfName.FontFile },
            { PdfName.FontFile2, PdfName.FontFile2 },
            { PdfName.FontFile3, PdfName.FontFile3 },
            { PdfName.CIDSystemInfo,PdfName.CIDSystemInfo },
            { PdfName.FontDescriptor, PdfName.FontDescriptor },
            { PdfName.CharProcs, PdfName.CharProcs },
            { PdfName.CharProc, PdfName.CharProc },

            { PdfName.Names, PdfName.Names },
            { PdfName.Style, PdfName.Style },
            { PdfName.PieceInfo, PdfName.PieceInfo },
            { PdfName.AppData, PdfName.AppData },

            { PdfName.ViewerPreferences, PdfName.ViewerPreferences },

            { PdfName.Resources, PdfName.Resources },
            { PdfName.DR, PdfName.Resources },
            { PdfName.ColorSpaces, PdfName.ColorSpaces },
            { PdfName.ExtGStates, PdfName.ExtGStates },
            { PdfName.Fonts, PdfName.Fonts },
            { PdfName.Patterns, PdfName.Patterns },
            { PdfName.Properties, PdfName.Properties },
            { PdfName.Shadings, PdfName.Shadings },
            { PdfName.XObjects, PdfName.XObjects },

            { PdfName.OCProperties, PdfName.OCProperties },
            { PdfName.Config, PdfName.Config },
            { PdfName.SMask, PdfName.Mask },
            { PdfName.StdCF, PdfName.CryptFilter },
            { PdfName.DefaultCryptFilter, PdfName.CryptFilter },
            { PdfName.XObject, PdfName.XObject },
            { PdfName.G, PdfName.XObject },
            { PdfName.Group, PdfName.Group },
            { PdfName.XRef, PdfName.XRef },
            { PdfName.ObjStm, PdfName.ObjStm },
            { PdfName.Encrypt, PdfName.Encrypt },
        };

        public static readonly Dictionary<PdfName, PdfName> MapResKeys = new()
        {
            { PdfName.ColorSpace, PdfName.ColorSpaces },
            { PdfName.ExtGState, PdfName.ExtGStates },
            { PdfName.Font, PdfName.Fonts },
            { PdfName.Pattern, PdfName.Patterns },
            { PdfName.Properties, PdfName.Properties },
            { PdfName.Shading, PdfName.Shadings },
            { PdfName.XObject, PdfName.XObjects },
        };

        public static readonly Dictionary<PdfName, Func<List<PdfDirectObject>, PdfArray>> Arrays = new()
        {
            { PdfName.ID, static arr => new Identifier(arr) },
            { PdfName.Dest, static arr => Destination.Create(arr) },
            { PdfName.ColorSpace, static arr => ColorSpace.Create(arr) },
            { PdfName.Annots, static arr => new PageAnnotations(arr) },
            { PdfName.Threads, static arr => new Articles(arr) },
            { PdfName.Functions, static arr => new Functions(arr) },
            { PdfName.Configs, static arr => new LayerConfigurations(arr) },
            { PdfName.Rect, static arr => new PdfRectangle(arr) },
            { PdfName.ArtBox, static arr => new PdfRectangle(arr) },
            { PdfName.BleedBox, static arr => new PdfRectangle(arr) },
            { PdfName.CropBox, static arr => new PdfRectangle(arr) },
            { PdfName.MediaBox, static arr => new PdfRectangle(arr) },
            { PdfName.TrimBox, static arr => new PdfRectangle(arr) },
            { PdfName.BBox, static arr => new PdfRectangle(arr) },
            { PdfName.FontBBox, static arr => new PdfRectangle(arr) },

        };

        public static readonly Dictionary<PdfName, Func<Dictionary<PdfName, PdfDirectObject>, PdfDictionary>> Dictionaries = new()
        {
            { PdfName.Catalog, static dict => new PdfCatalog(dict) },
            { PdfName.ViewerPreferences, static dict => new ViewerPreferences(dict) },
            { PdfName.Pages, static dict => new PdfPages(dict) },
            { PdfName.Page, static dict => new PdfPage(dict) },
            { PdfName.AcroForm, static dict => new AcroForm(dict) },
            { PdfName.Names, static dict => new NamedResources(dict) },
            { PdfName.PageLabel, static dict => new PageLabel(dict) },
            
            { PdfName.ExtGState, static dict => new ExtGState(dict) },

            { PdfName.Group, static dict => GroupXObject.Create(dict) },
            { PdfName.Mask, static dict => new SoftMask(dict) },

            { PdfName.Filespec, static dict => new FileSpecification(dict) },

            { PdfName.Action, static dict => PdfAction.Create(dict) },
            { PdfName.AA, static dict => new AdditionalActions(dict) },

            { PdfName.Annot, static dict => Annotation.Create(dict) },
            { PdfName.Border, static dict => new Border(dict) },
            { PdfName.FB, static dict => new IconFitObject(dict) },
            { PdfName.AC, static dict => new AppearanceCharacteristics(dict) },
            { PdfName.AP, static dict => new Appearance(dict) },

            { PdfName.Outlines, static dict => new Bookmarks(dict) },
            { PdfName.Outline, static dict => new Bookmark(dict) },
            { PdfName.Thread, static dict => new Article(dict) },
            { PdfName.Bead, static dict => new ArticleElement(dict) },
            { PdfName.Trans, static dict => new Transition(dict) },

            { PdfName.FWParams, static dict => new FloatingWindowParameters(dict) },
            { PdfName.MediaClip, static dict => MediaClip.Create(dict) },
            { PdfName.MediaDuration, static dict => new MediaDuration(dict) },
            { PdfName.MediaOffset, static dict => MediaOffset.Create(dict) },
            { PdfName.PL, static dict => new MediaPlayers(dict) },
            { PdfName.MediaPlayerInfo, static dict => new MediaPlayerInfo(dict) },
            { PdfName.MediaPlayParams, static dict => new MediaPlayParameters(dict) },
            { PdfName.MediaScreenParams, static dict => new MediaScreenParameters(dict) },
            { PdfName.Rendition, static dict => Rendition.Create(dict) },
            { PdfName.Timespan, static dict => new Timespan(dict) },
            { PdfName.SoftwareIdentifier, static dict => new SoftwareIdentifier(dict) },
            { PdfName.Sound, static dict => new Documents.Multimedia.Sound(dict) },

            { PdfName.Font, static dict => PdfFont.Create(dict) },
            { PdfName.FontDescriptor, static dict => new FontDescriptor(dict) },
            { PdfName.Style, static dict => new FontStyle(dict) },
            { PdfName.CIDSystemInfo, static dict => new CIDSystemInfo(dict) },
            { PdfName.FontFile, static dict => new FontFile(dict) },
            { PdfName.FontFile2, static dict => new FontFile(dict) },
            { PdfName.FontFile3, static dict => new FontFile(dict) },
            { PdfName.CharProcs, static dict => new Type3CharProcs(dict) },
            { PdfName.CharProc, static dict => new Type3CharProc(dict) },

            { PdfName.Function, static dict => Function.Create(dict) },
            { PdfName.Pattern, static dict => Pattern.Create(dict) },
            { PdfName.Shading, static dict => Shading.Create(dict) },

            { PdfName.Resources, static dict => new Resources(dict) },
            { PdfName.ColorSpaces, static dict => new ColorSpaceResources(dict) },
            { PdfName.ExtGStates, static dict => new ExtGStateResources(dict) },
            { PdfName.Fonts, static dict => new FontResources(dict) },
            { PdfName.Patterns, static dict => new PatternResources(dict) },
            { PdfName.Properties, static dict => new PropertyListResources(dict) },
            { PdfName.Shadings, static dict => new ShadingResources(dict) },
            { PdfName.XObjects, static dict => new XObjectResources(dict) },

            { PdfName.ObjStm, static dict => new ObjectStream(dict) },
            { PdfName.XRef, static dict => new XRefStream(dict) },
            { PdfName.XObject, static dict => XObject.Create(dict) },
            { PdfName.EmbeddedFile, static dict => new EmbeddedFile(dict) },
            { PdfName.Metadata, static dict => new PdfMetadata(dict) },
            { PdfName.Info, static dict => new Information(dict) },
            { PdfName.PieceInfo, static dict => new AppDataCollection(dict) },
            { PdfName.AppData, static dict => new AppData(dict) },

            { PdfName.OCProperties, static dict => new LayerDefinition(dict) },
            { PdfName.Config, static dict => new LayerConfiguration(dict) },

            { PdfName.Sig, static dict => new SignatureDictionary(dict) },
            { PdfName.Encrypt, static dict => new PdfEncryption(dict) },
            { PdfName.CryptFilter, static dict => new PdfCryptFilter(dict) },
        };

        public static PdfName DetectDictionaryType(Dictionary<PdfName, PdfDirectObject> dictionary, PdfName pKey, PdfName gpKey)
        {
            if ((PdfName.Resources.Equals(gpKey)
                || PdfName.DR.Equals(gpKey))
                && MapResKeys.TryGetValue(pKey, out var resType))
            {
                return resType;
            }
            if (pKey != null
                && mapParentKeys.TryGetValue(pKey, out var mapped))
            {
                return mapped;
            }

            //if (dictionary.ContainsKey(PdfName.Fields))
            //    return PdfName.Fields;
            //if (dictionary.ContainsKey(PdfName.Panose))
            //    return PdfName.Style;            
            //if (dictionary.ContainsKey(PdfName.Length1))
            //    return PdfName.FontFile;
            //if (dictionary.ContainsKey(PdfName.Length2))
            //    return PdfName.FontFile2;
            //if (dictionary.ContainsKey(PdfName.Length3))
            //    return PdfName.FontFile3;
            //if (dictionary.ContainsKey(PdfName.FontName))
            //    return PdfName.FontDescriptor;
            //if (dictionary.ContainsKey(PdfName.Registry))
            //    return PdfName.CIDSystemInfo;
            if (dictionary.ContainsKey(PdfName.PatternType))
                return PdfName.Pattern;
            if (dictionary.ContainsKey(PdfName.TilingType))
                return PdfName.Pattern;
            if (dictionary.ContainsKey(PdfName.ShadingType))
                return PdfName.Shading;
            if (dictionary.ContainsKey(PdfName.FunctionType))
                return PdfName.Function;
            if (dictionary.ContainsKey(PdfName.FormType))
                return PdfName.XObject;

            //if (dictionary.ContainsKey(PdfName.Private))
            //    return PdfName.Private;
            return PdfName.None;
        }


    }
}