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
    /// <summary>Optional content membership [PDF:1.7:4.10.1].</summary>
    [Pdf(VersionEnum.PDF15)]
    public sealed class LayerMembership : LayerEntity
    {       

        public static readonly PdfName TypeName = PdfName.OCMD;
        private VisibilityExpression visibilityExpression;
        private VisibilityMembersImpl visibilityMembers;

        public LayerMembership(PdfDocument context)
            : base(context, TypeName)
        { }

        internal LayerMembership(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        public override LayerEntity Membership => this;

        public override VisibilityExpression VisibilityExpression
        {
            get => visibilityExpression ??= new VisibilityExpression(Get(PdfName.VE));
            set => Set(PdfName.VE, visibilityExpression = value);
        }

        public override IList<Layer> VisibilityMembers
        {
            get => visibilityMembers ??= new VisibilityMembersImpl(this);
            set
            {
                var visibilityMembers = this.VisibilityMembers;
                visibilityMembers.Clear();
                foreach (var layer in value)
                { visibilityMembers.Add(layer); }
            }
        }

        public override VisibilityPolicyEnum VisibilityPolicy
        {
            get => GetVPE(GetString(PdfName.P));
            set => this[PdfName.P] = GetName(value);
        }
    }
}