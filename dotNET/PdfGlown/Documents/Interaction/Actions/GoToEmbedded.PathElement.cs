/*
  Copyright 2008-2012 Stefano Chizzolini. http://www.pdfclown.org

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

namespace PdfGlown.Documents.Interaction.Actions
{
    public sealed partial class GoToEmbedded
    {
        /// <summary>Path information to the target document [PDF:1.6:8.5.3].</summary>
        public class PathElement : PdfObjectWrapper<PdfDictionary>
        {
            /// <summary>Relationship between the target and the current document [PDF:1.6:8.5.3].</summary>
            public enum RelationEnum
            {
                ///<summary>Parent.</summary>
                Parent,
                ///<summary>Child.</summary>
                Child
            };

            private static readonly Dictionary<RelationEnum, PdfName> RelationEnumCodes = new(2)
            {
                [RelationEnum.Parent] = PdfName.P,
                [RelationEnum.Child] = PdfName.C
            };
            private PathElement next;

            /// <summary>Gets the code corresponding to the given value.</summary>
            private static PdfName ToCode(RelationEnum value) => RelationEnumCodes[value];

            /// <summary>Gets the relation corresponding to the given value.</summary>
            private static RelationEnum ToRelationEnum(IPdfString value)
            {
                if (value == null)
                    new Exception("'null' doesn't represent a valid relation.");
                foreach (KeyValuePair<RelationEnum, PdfName> relation in RelationEnumCodes)
                {
                    if (string.Equals(relation.Value.StringValue, value.StringValue, StringComparison.Ordinal))
                        return relation.Key;
                }
                throw new Exception("'" + value?.StringValue + "' doesn't represent a valid relation.");
            }

            /// <summary>Creates a new path element representing the parent of the document.</summary>
            public PathElement(PdfDocument context, PathElement next)
                : this(context, RelationEnum.Parent, null, null, null, next)
            { }

            /// <summary>Creates a new path element located in the embedded files collection of the document.</summary>
            public PathElement(PdfDocument context, string embeddedFileName, PathElement next)
                : this(context, RelationEnum.Child, embeddedFileName, null, null, next)
            { }

            /// <summary>Creates a new path element associated with a file attachment annotation.</summary>
            public PathElement(PdfDocument context, object annotationPageRef, object annotationRef, PathElement next)
                : this(context, RelationEnum.Child, null, annotationPageRef, annotationRef, next)
            { }

            /// <summary>Creates a new path element.</summary>
            private PathElement(PdfDocument context, RelationEnum relation, string embeddedFileName, object annotationPageRef, object annotationRef, PathElement next)
                : base(context, new PdfDictionary())
            {
                Relation = relation;
                EmbeddedFileName = embeddedFileName;
                AnnotationPageRef = annotationPageRef;
                AnnotationRef = annotationRef;
                Next = next;
            }

            /// <summary>Instantiates an existing path element.</summary>
            public PathElement(PdfDirectObject baseObject) : base(baseObject)
            { }

            /// <summary>Gets/Sets the page reference to the file attachment annotation.</summary>
            /// <returns>Either the (zero-based) number of the page in the current document containing the file attachment annotation,
            /// or the name of a destination in the current document that provides the page number of the file attachment annotation.</returns>
            public object AnnotationPageRef
            {
                get
                {
                    var pageRefObject = DataObject.Get<PdfDirectObject>(PdfName.P);
                    return pageRefObject switch
                    {
                        null => null,
                        PdfInteger pdfInteger => pdfInteger.Value,
                        _ => ((IPdfString)pageRefObject).StringValue
                    };
                }
                set
                {
                    if (value == null)
                    { DataObject.Remove(PdfName.P); }
                    else
                    {
                        PdfDirectObject refObject = null;
                        switch (value)
                        {
                            case int intValue: refObject = PdfInteger.Get(intValue); break;
                            case string stringValue: refObject = new PdfString(stringValue); break;
                            default: throw new ArgumentException("Wrong argument type: it MUST be either a page number Integer or a named destination String.");
                        };
                        DataObject[PdfName.P] = refObject;
                    }
                }
            }

            /// <summary>Gets/Sets the reference to the file attachment annotation.</summary>
            /// <returns>Either the (zero-based) index of the annotation in the list of annotations
            /// associated to the page specified by the annotationPageRef property, or the name of the annotation.</returns>
            public object AnnotationRef
            {
                get
                {
                    var annotationRefObject = DataObject.Get<PdfDirectObject>(PdfName.A);
                    return annotationRefObject switch
                    {
                        null => null,
                        PdfInteger pdfInteger => pdfInteger.Value,
                        _ => ((IPdfString)annotationRefObject).StringValue
                    };
                }
                set
                {
                    if (value == null)
                    { DataObject.Remove(PdfName.A); }
                    else
                    {
                        PdfDirectObject refObject = null;
                        switch (value)
                        {
                            case int intValue: refObject = PdfInteger.Get(intValue); break;
                            case string stringValue: refObject = new PdfTextString(stringValue); break;
                            default: throw new ArgumentException("Wrong argument type: it MUST be either a page number Integer or a named destination String.");
                        };
                        DataObject[PdfName.A] = refObject;
                    }
                }
            }

            /// <summary>Gets/Sets the embedded file name.</summary>
            public string EmbeddedFileName
            {
                get => DataObject.GetString(PdfName.N);
                set => DataObject.Set(PdfName.N, value);
            }

            /// <summary>Gets/Sets the relationship between the target and the current document.</summary>
            public RelationEnum Relation
            {
                get => ToRelationEnum(DataObject.Get<IPdfString>(PdfName.R));
                set => DataObject.Set(PdfName.R, ToCode(value));
            }

            /// <summary>Gets/Sets a further path information to the target document.</summary>
            public PathElement Next
            {
                get => next ??= new(DataObject.Get(PdfName.T));
                set => DataObject.Set(PdfName.T, next = value);
            }
        }
    }
}