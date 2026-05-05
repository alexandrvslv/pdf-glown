/*
  Copyright 2012-2013 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfGlown.Bytes;
using PdfGlown.Documents;
using PdfGlown.Documents.Interaction.Navigation;
using PdfGlown.Tokens;

using System;
using System.Collections.Generic;

namespace PdfGlown.Objects
{
    /// <summary>Object cloner.</summary>
    public class Cloner : Visitor
    {
        public class Filter
        {
            private readonly String name;

            public Filter(String name)
            {
                this.name = name;
            }

            /// <summary>Notifies a complete clone operation on an object.</summary>
            /// <param name="cloner">Object cloner.</param>
            /// <param name="source">Source object.</param>
            /// <param name="clone">Clone object.</param>
            public virtual void AfterClone(Cloner cloner, PdfObject source, PdfObject clone)
            { }

            /// <summary>Notifies a complete clone operation on a dictionary entry.</summary>
            /// <param name="cloner">Object cloner.</param>
            /// <param name="source">Parent source object.</param>
            /// <param name="clone">Parent clone object.</param>
            /// <param name="key">Entry key within the parent.</param>
            /// <param name="value">Clone value.</param>
            public virtual void AfterClone(Cloner cloner, PdfDictionary source, PdfDictionary clone, PdfName key, PdfDirectObject value)
            { }

            /// <summary>Notifies a complete clone operation on an array item.</summary>
            /// <param name="cloner">Object cloner.</param>
            /// <param name="source">Parent source object.</param>
            /// <param name="clone">Parent clone object.</param>
            /// <param name="index">Item index within the parent.</param>
            /// <param name="item">Clone item.</param>
            public virtual void AfterClone(Cloner cloner, PdfArray source, PdfArray clone, int index, PdfDirectObject item)
            { }

            /// <summary>Notifies a starting clone operation on a dictionary entry.</summary>
            /// <param name="cloner">Object cloner.</param>
            /// <param name="source">Parent source object.</param>
            /// <param name="clone">Parent clone object.</param>
            /// <param name="key">Entry key within the parent.</param>
            /// <param name="value">Source value.</param>
            /// <returns>Whether the clone operation can be fulfilled.</returns>
            public virtual bool BeforeClone(Cloner cloner, PdfDictionary source, PdfDictionary clone, PdfName key, PdfDirectObject value)
            {
                return true;
            }

            /// <summary>Notifies a starting clone operation on an array item.</summary>
            /// <param name="cloner">Object cloner.</param>
            /// <param name="source">Parent source object.</param>
            /// <param name="clone">Parent clone object.</param>
            /// <param name="index">Item index within the parent.</param>
            /// <param name="item">Source item.</param>
            /// <returns>Whether the clone operation can be fulfilled.</returns>
            public virtual bool BeforeClone(Cloner cloner, PdfArray source, PdfArray clone, int index, PdfDirectObject item)
            {
                return true;
            }

            /// <summary>Gets whether this filter can deal with the given object.</summary>
            /// <param name="cloner">Object cloner.</param>
            /// <param name="source">Source object.</param>
            public virtual bool Matches(Cloner cloner, PdfObject source)
            {
                return true;
            }

            public string Name => name;

            protected static void CloneNamedObject<T>(Cloner cloner, PdfDirectObject source, PdfString name)
                where T : PdfObject
            {
                // Resolve the named object source!
                T namedObjectSource = source.Document.Catalog.ResolveName<T>(name);
                if (namedObjectSource == null)
                    return;

                // Clone the named object source into the target document!
                cloner.context.Catalog.Register(name, (T)namedObjectSource.Clone(cloner));
            }
        }

        private class ActionFilter : Filter
        {
            public ActionFilter() : base("Action")
            { }

            public override void AfterClone(Cloner cloner, PdfDictionary source, PdfDictionary clone, PdfName key, PdfDirectObject value)
            {
                if (PdfName.D.Equals(key))
                {
                    PdfDirectObject destObject = clone.Get(PdfName.D);
                    if (destObject is PdfString dstr) // Named destination.
                    {
                        CloneNamedObject<Destination>(cloner, source, dstr);
                    }
                }
            }

            public override bool Matches(Cloner cloner, PdfObject source)
            {
                if (source is PdfDictionary dictionary)
                {
                    return dictionary.ContainsKey(PdfName.S)
                      && (!dictionary.ContainsKey(PdfName.Type)
                      || PdfName.Action.Equals(dictionary.Get<PdfName>(PdfName.Type)));
                }
                return false;
            }
        }

        private class AnnotationsFilter : Filter
        {
            public AnnotationsFilter() : base("Annots")
            { }

            public override void AfterClone(Cloner cloner, PdfArray source, PdfArray clone, int index, PdfDirectObject item)
            {
                var annotation = (PdfDictionary)item.Resolve(PdfName.Annot);
                if (annotation.ContainsKey(PdfName.FT))
                {
                    cloner.context.Catalog.Form.Fields.Add(cloner.context.Catalog.Form.Fields.Wrap(annotation.Reference));
                }
                else if (annotation.ContainsKey(PdfName.Dest))
                {
                    var destObject = annotation.Get(PdfName.Dest);
                    if (destObject is PdfString destString) // Named destination.
                    { CloneNamedObject<Destination>(cloner, source, destString); }
                }
            }

            public override bool Matches(Cloner cloner, PdfObject source)
            {
                if (source is PdfArray array
                    && array.Count > 0
                    && array.Get<PdfDictionary>(0) is PdfDictionary arrayItemDictionary)
                {
                    return arrayItemDictionary.ContainsKey(PdfName.Subtype)
                      && arrayItemDictionary.ContainsKey(PdfName.Rect);
                }
                return false;
            }
        }

        private class AnnotationFilter : Filter
        {
            public AnnotationFilter() : base("Annot")
            { }

            public override bool BeforeClone(Cloner cloner, PdfDictionary source, PdfDictionary clone, PdfName key, PdfDirectObject value)
            {
                if (key.Equals(PdfName.P))
                    return false;
                return true;
            }

            public override bool Matches(Cloner cloner, PdfObject source)
            {
                if (source is PdfDictionary dictionary
                    && PdfName.Annot.Equals(dictionary.Get<PdfName>(PdfName.Type)))
                {
                    return true;
                }
                return false;
            }
        }

        private class PageFilter : Filter
        {
            public PageFilter() : base("Page")
            { }

            public override void AfterClone(Cloner cloner, PdfObject source, PdfObject clone)
            {
                // NOTE: Inheritable attributes have to be consolidated into the cloned page dictionary in
                // order to ensure its consistency.
                var cloneDictionary = (PdfDictionary)clone;
                var sourceDictionary = (PdfDictionary)source;
                foreach (PdfName key in PdfPage.InheritableAttributeKeys)
                {
                    if (!sourceDictionary.ContainsKey(key))
                    {
                        var sourceValue = sourceDictionary.GetInheritableAttribute(key);
                        if (sourceValue != null)
                        { cloneDictionary[key] = (PdfDirectObject)sourceValue.Accept(cloner, key, null); }
                    }
                }
            }

            public override bool BeforeClone(Cloner cloner, PdfDictionary source, PdfDictionary clone, PdfName key, PdfDirectObject value)
            {
                return base.BeforeClone(cloner, source, clone, key, value)
                    && !PdfName.Parent.Equals(key);
            }

            public override bool Matches(Cloner cloner, PdfObject source)
            {
                return source is PdfDictionary dictionary
                  && PdfName.Page.Equals(dictionary.Get<PdfName>(PdfName.Type));
            }
        }

        private static readonly Filter NullFilter = new("Default");

        private static readonly List<Filter> commonFilters = new()
        {
            new PageFilter(),
            new ActionFilter(),
            new AnnotationFilter(),
            new AnnotationsFilter(),
        };

        private PdfDocument context;

        private readonly List<Filter> filters = new(commonFilters);

        public Cloner(PdfDocument context)
        {
            Context = context;
        }

        public PdfDocument Context
        {
            get => context;
            set => context = value ?? throw new ArgumentException("value required");
        }

        public List<Filter> Filters => filters;

        public override PdfObject Visit(ObjectStream obj, PdfName parentKey, object data)
        {
            throw new NotSupportedException();
        }

        public override PdfObject Visit(PdfArray obj, PdfName parentKey, object data)
        {
            var cloneFilter = MatchFilter(obj);
            var clone = (PdfArray)obj.Clone();
            {
                clone.items = new List<PdfDirectObject>();
                var sourceItems = obj.items;
                for (int index = 0, length = sourceItems.Count; index < length; index++)
                {
                    var sourceItem = sourceItems[index];
                    if (cloneFilter.BeforeClone(this, obj, clone, index, sourceItem))
                    {
                        PdfDirectObject cloneItem;
                        clone.Add(cloneItem = (PdfDirectObject)(sourceItem?.Accept(this, obj.TypeKey ?? parentKey, null)));
                        cloneFilter.AfterClone(this, obj, clone, index, cloneItem);
                    }
                }
            }
            cloneFilter.AfterClone(this, obj, clone);
            return clone;
        }

        public override PdfObject Visit(PdfDictionary obj, PdfName parentKey, object data)
        {
            var cloneFilter = MatchFilter(obj);
            var clone = (PdfDictionary)obj.Clone();
            {
                clone.entries = new Dictionary<PdfName, PdfDirectObject>();
                foreach (KeyValuePair<PdfName, PdfDirectObject> entry in obj.entries)
                {

                    var sourceValue = entry.Value;
                    if (cloneFilter.BeforeClone(this, obj, clone, entry.Key, sourceValue))
                    {
                        PdfDirectObject cloneValue;
                        clone[entry.Key] = cloneValue = (PdfDirectObject)(sourceValue?.Accept(this, obj.ModifyTypeKey(entry.Key), null));
                        cloneFilter.AfterClone(this, obj, clone, entry.Key, cloneValue);
                    }
                }
            }
            cloneFilter.AfterClone(this, obj, clone);
            return clone;
        }

        public override PdfObject Visit(PdfIndirectObject obj, PdfName parentKey, object data)
        {
            return context.IndirectObjects.AddExternal(obj, this, parentKey);
        }

        public override PdfObject Visit(PdfReference obj, PdfName parentKey, object data)
        {
            return context == obj.Document
              ? (PdfReference)obj.Clone() // Local clone.
              : Visit(obj.IndirectObject, parentKey, data).Reference; // Alien clone.
        }

        public override PdfObject Visit(PdfStream obj, PdfName parentKey, object data)
        {
            var clone = (PdfStream)Visit((PdfDictionary)obj, parentKey, data);
            if (obj.GetInputStreamNoDecode() is IInputStream stream)
                clone.SetStream(new ByteStream(stream));
            return clone;
        }

        public override PdfObject Visit(XRefStream obj, PdfName parentKey, object data)
        {
            throw new NotSupportedException();
        }

        private Filter MatchFilter(PdfObject obj)
        {
            Filter cloneFilter = NullFilter;
            foreach (Filter filter in filters)
            {
                if (filter.Matches(this, obj))
                {
                    cloneFilter = filter;
                    break;
                }
            }
            return cloneFilter;
        }
    }
}