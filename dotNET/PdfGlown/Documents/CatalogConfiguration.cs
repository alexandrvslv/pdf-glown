/*
  Copyright 2015 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it)

  This file should be part of the source code distribution of "PDF Glown library"
  (the Program): see the accompanying README files for more info.

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

using PdfGlown.Documents.Contents.XObjects;
using PdfGlown.Documents.Interaction.Annotations;
using PdfGlown.Objects;
using PdfGlown.Util.IO;
using PdfGlown.Util.Math;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace PdfGlown.Documents
{
    /// <summary>Document configuration.</summary>
    public sealed class CatalogConfiguration
    {
        private CompatibilityModeEnum compatibilityMode = CompatibilityModeEnum.Loose;
        private EncodingFallbackEnum encodingFallback = EncodingFallbackEnum.Substitution;
        private string stampPath;

        private PdfDocument document;

        private IDictionary<StandardStampEnum, FormXObject> importedStamps;

        internal CatalogConfiguration(PdfDocument document)
        {
            this.document = document;
        }

        /// <summary>Gets/Sets the document's version compatibility mode.</summary>
        public CompatibilityModeEnum CompatibilityMode
        {
            get => compatibilityMode;
            set => compatibilityMode = value;
        }

        /// <summary>Gets the document associated with this configuration.</summary>
        public PdfDocument Document => document;

        /// <summary>Gets/Sets the encoding behavior in case of missing character mapping.</summary>
        public EncodingFallbackEnum EncodingFallback
        {
            get => encodingFallback;
            set => encodingFallback = value;
        }

        /// <summary>Gets the stamp appearance corresponding to the specified stamp type.</summary>
        /// <remarks>The stamp appearance is retrieved from the <see cref="StampPath">standard stamps
        /// path</see> and embedded in the document.</remarks>
        /// <param name="type">Predefined stamp type whose appearance has to be retrieved.</param>
        public FormXObject GetStamp(StandardStampEnum? type)
        {
            if (!type.HasValue
              || stampPath == null)
                return null;

            FormXObject stamp = null;
            if (importedStamps != null)
            {
                importedStamps.TryGetValue(type.Value, out stamp);
            }
            else
            {
                importedStamps = new Dictionary<StandardStampEnum, FormXObject>();
            }
            if (stamp == null)
            {
                if (File.GetAttributes(stampPath).HasFlag(FileAttributes.Directory)) // Acrobat standard stamps directory.
                {
                    string stampFileName;
                    switch (type.Value)
                    {
                        case StandardStampEnum.Approved:
                        case StandardStampEnum.AsIs:
                        case StandardStampEnum.Confidential:
                        case StandardStampEnum.Departmental:
                        case StandardStampEnum.Draft:
                        case StandardStampEnum.Experimental:
                        case StandardStampEnum.Expired:
                        case StandardStampEnum.Final:
                        case StandardStampEnum.ForComment:
                        case StandardStampEnum.ForPublicRelease:
                        case StandardStampEnum.NotApproved:
                        case StandardStampEnum.NotForPublicRelease:
                        case StandardStampEnum.Sold:
                        case StandardStampEnum.TopSecret:
                            stampFileName = "Standard.pdf";
                            break;
                        case StandardStampEnum.BusinessApproved:
                        case StandardStampEnum.BusinessConfidential:
                        case StandardStampEnum.BusinessDraft:
                        case StandardStampEnum.BusinessFinal:
                        case StandardStampEnum.BusinessForComment:
                        case StandardStampEnum.BusinessForPublicRelease:
                        case StandardStampEnum.BusinessNotApproved:
                        case StandardStampEnum.BusinessNotForPublicRelease:
                        case StandardStampEnum.BusinessCompleted:
                        case StandardStampEnum.BusinessVoid:
                        case StandardStampEnum.BusinessPreliminaryResults:
                        case StandardStampEnum.BusinessInformationOnly:
                            stampFileName = "StandardBusiness.pdf";
                            break;
                        case StandardStampEnum.Rejected:
                        case StandardStampEnum.Accepted:
                        case StandardStampEnum.InitialHere:
                        case StandardStampEnum.SignHere:
                        case StandardStampEnum.Witness:
                            stampFileName = "SignHere.pdf";
                            break;
                        default:
                            throw new NotSupportedException("Unknown stamp type");
                    }
                    var path = Path.Combine(stampPath, stampFileName);
                    if (File.Exists(path))
                    {
                        using (var stampFile = new PdfDocument(path))
                        {
                            var stampPageKey = new PdfString(type.Value.GetName().StringValue + "=" + String.Join(" ", Regex.Split(type.Value.GetName().StringValue.Substring(2), "(?!^)(?=\\p{Lu})")));
                            var stampPage = stampFile.Catalog.Names.Pages[stampPageKey];
                            importedStamps[type.Value] = (stamp = (FormXObject)stampPage.ToXObject(Document));
                            stamp.Box = stampPage.ArtBox.ToSKRect();
                        }
                    }
                }
                else // Standard stamps template (std-stamps.pdf).
                {
                    using var stampFile = new PdfDocument(stampPath);
                    var stampXObject = stampFile.Pages[0].Resources.XObjects[type.Value.GetName()] as FormXObject;
                    importedStamps[type.Value] = stamp = (FormXObject)stampXObject.RefOrSelf.Clone(Document).Resolve(PdfName.XObject);
                }
            }
            return stamp;
        }

        /// <summary>Gets/Sets the path (either Acrobat's standard stamps installation directory or PDF
        /// Glown's standard stamps collection (std-stamps.pdf)) where standard stamp templates are
        /// located.</summary>
        /// <remarks>In order to ensure consistent and predictable rendering across the systems, the
        /// <see cref="Stamp.#ctor(Page, SKRect, string,
        /// PdfGlown.Documents.Interaction.Annotations.Stamp.StandardTypeEnum)">standard stamp annotations
        /// </see> require their appearance to be embedded from the corresponding standard stamp files
        /// (Standard.pdf, StandardBusiness.pdf, SignHere.pdf, ...) shipped with Acrobat: defining this
        /// property activates the automatic embedding of such appearances.</remarks>
        public string StampPath
        {
            get => stampPath;
            set
            {
                if (!IOUtils.Exists(value))
                    throw new ArgumentException(null, new FileNotFoundException());

                stampPath = value;
            }
        }
    }
}

