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

namespace PdfGlown.Documents.Contents.Layers
{
    /// <summary>Layer entity.</summary>
    [Pdf(VersionEnum.PDF15)]
    public abstract class LayerEntity : PropertyList
    {
        /// <summary>Membership visibility policy [PDF:1.7:4.10.1].</summary>
        public enum VisibilityPolicyEnum
        {
            /// <summary>Visible only if all of the visibility layers are ON.</summary>
            AllOn,
            /// <summary>Visible if any of the visibility layers are ON.</summary>
            AnyOn,
            /// <summary>Visible if any of the visibility layers are OFF.</summary>
            AnyOff,
            /// <summary>Visible only if all of the visibility layers are OFF.</summary>
            AllOff
        }

        protected static readonly BiDictionary<VisibilityPolicyEnum, string> vpeCodes = new()
        {
            [VisibilityPolicyEnum.AllOn] = PdfName.AllOn.StringValue,
            [VisibilityPolicyEnum.AnyOn] = PdfName.AnyOn.StringValue,
            [VisibilityPolicyEnum.AnyOff] = PdfName.AnyOff.StringValue,
            [VisibilityPolicyEnum.AllOff] = PdfName.AllOff.StringValue
        };

        public static VisibilityPolicyEnum GetVPE(string name)
        {
            if (name == null)
                return VisibilityPolicyEnum.AnyOn;

            VisibilityPolicyEnum? visibilityPolicy = vpeCodes.GetKey(name);
            if (!visibilityPolicy.HasValue)
                throw new NotSupportedException("Visibility policy unknown: " + name);

            return visibilityPolicy.Value;
        }

        public static PdfName GetName(VisibilityPolicyEnum visibilityPolicy) => PdfName.Get(vpeCodes[visibilityPolicy], true);

        protected LayerEntity(PdfDocument context, PdfName typeName)
            : base(context, new(1) {
                { PdfName.Type, typeName }
            })
        { }

        protected LayerEntity(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets the default membership, corresponding to the hierarchical relation between this
        /// layer entity and its ascendants; top-level layers return themselves.</summary>
        public abstract LayerEntity Membership { get; }

        /// <summary>Gets the visibility expression.</summary>
        /// <remarks><see cref="VisibilityExpression"/> should be preferred to <see cref="VisibilityPolicy"/>
        /// and<see cref="VisibilityMembers"/> as a more advanced alternative. However, for compatibility
        /// purposes, PDF creators should also provide the latters to approximate the behavior in older
        /// consumer software.</remarks>
        public abstract VisibilityExpression VisibilityExpression { get; set; }

        /// <summary>Gets the layers whose states determine the visibility of content controlled by this
        /// entity.</summary>
        public abstract IList<Layer> VisibilityMembers { get; set; }

        /// <summary>Gets/Sets the visibility policy of this entity.</summary>
        public abstract VisibilityPolicyEnum VisibilityPolicy { get; set; }
    }

}