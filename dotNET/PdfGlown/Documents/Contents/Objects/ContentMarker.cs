/*
  Copyright 2010-2015 Stefano Chizzolini. http://www.pdfclown.org

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

namespace PdfGlown.Documents.Contents.Objects
{
    /// <summary>Abstract content marker [PDF:1.6:10.5].</summary>
    [Pdf(VersionEnum.PDF12)]
    public abstract class ContentMarker : Operation, IResourceReference<PropertyList>
    {
        protected ContentMarker(string @operator, PdfName tag) : this(@operator, tag, null)
        { }

        protected ContentMarker(string @operator, PdfName tag, PdfDirectObject properties) : base(@operator, tag)
        {
            if (properties != null)
            {
                operands.Add(properties);
            }
        }

        protected ContentMarker(string @operator, PdfArray operands) : base(@operator, operands)
        { }

        public PropertyList GetResource(ContentScanner scanner) => GetProperties(scanner);

        /// <summary>Gets the private information meaningful to the program (application or plugin extension)
        /// creating the marked content.</summary>
        /// <param name="scanner">Content context.</param>
        public PropertyList GetProperties(ContentScanner scanner)
        {
            object properties = Properties;
            if (properties is PropertyList list)
                return list;
            if (properties == null)
                return null;
            var name = (PdfName)properties;
            var pscanner = scanner;

            while ((list = pscanner.Context.Resources.PropertyLists[name]) == null
                && (pscanner = pscanner.ResourceParent) != null)
            { }
            return list;
        }

        /// <summary>Gets/Sets the private information meaningful to the program (application or plugin
        /// extension) creating the marked content. It can be either an inline <see cref="PropertyList"/>
        /// or the <see cref="PdfName">name</see> of an external PropertyList resource.</summary>
        public PdfDirectObject Properties
        {
            get
            {
                var propertiesObject = operands.Count > 1 ? operands.Get(1) : null;
                if (propertiesObject == null)
                    return null;
                else if (propertiesObject is PdfName)
                    return propertiesObject;
                else if (propertiesObject is PropertyList pList)
                    return pList;
                else if (propertiesObject is PdfDictionary dictionary)
                {
                    var lst = new PropertyList(dictionary.entries);
                    operands.Set(1, lst);
                    return lst;
                }
                else
                    throw new NotSupportedException("Property list type unknown: " + propertiesObject.GetType().Name);
            }
            set
            {
                if (value == null)
                {
                    if (operands.Count > 1)
                    { operands.RemoveAt(1); }
                }
                else
                {
                    PdfDirectObject operand;
                    if (value is PdfName pdfName)
                    { operand = pdfName; }
                    else if (value is PropertyList propertyList)
                    { operand = propertyList; }
                    else
                        throw new ArgumentException("value MUST be a PdfName or a PropertyList.");

                    if (operands.Count > 1)
                    { operands.Set(1, operand); }
                    else
                    { operands.Add(operand); }
                }
            }
        }

        /// <summary>Gets/Sets the marker indicating the role or significance of the marked content.</summary>
        public PdfName Tag
        {
            get => (PdfName)operands.Get(0);
            set => operands.SetSimple(0, value);
        }

        public PdfName Name
        {
            get => Properties is PdfName name ? name : null;
            set => Properties = value;
        }        
    }
}