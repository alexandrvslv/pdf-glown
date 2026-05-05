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

using PdfGlown.Documents.Contents.XObjects;
using PdfGlown.Objects;
using PdfGlown.Util.Collections;
using System;
using System.Collections;
using System.Collections.Generic;

namespace PdfGlown.Documents.Interaction.Annotations
{
    /// <summary>Appearance states [PDF:1.6:8.4.4].</summary>
    [Pdf(VersionEnum.PDF12)]
    public sealed class AppearanceStates : PdfObjectWrapper<PdfDirectObject>, IDictionary<PdfName, FormXObject>
    {
        public static AppearanceStates Wrap(PdfName statesKey, Appearance appearance)
        {
            return new AppearanceStates(statesKey, appearance);
        }
        private Appearance appearance;

        private PdfName statesKey;

        public AppearanceStates(PdfName statesKey, Appearance appearance)
            : base(appearance.Get(statesKey))
        {
            this.appearance = appearance;
            this.statesKey = statesKey;
        }

        public AppearanceStates(PdfDirectObject baseObject) : base(baseObject)
        { }

        /// <summary>Gets the appearance associated to these states.</summary>
        public Appearance Appearance => appearance;

        //TODO
        /**
          Gets the key associated to a given value.
        */
        //   public PdfName GetKey(
        //     FormXObject value
        //     )
        //   {return BaseDataObject.GetKey(value.BaseObject);}

        public void Add(PdfName key, FormXObject value)
        { EnsureDictionary()[key] = value.RefOrSelf; }

        public bool ContainsKey(PdfName key)
        {
            PdfDirectObject baseDataObject = DataObject;
            if (baseDataObject == null) // No state.
                return false;
            else if (baseDataObject is PdfStream) // Single state.
                return (key == null);
            else // Multiple state.
                return ((PdfDictionary)baseDataObject).ContainsKey(key);
        }

        public ICollection<PdfName> Keys => DataObject is PdfStream ? new SingleItemCollection<PdfName>(null)
            : DataObject is PdfDictionary dict ? dict.Keys : EmptyCollection<PdfName>.Default;

        public bool Remove(PdfName key)
        {
            PdfDirectObject baseDataObject = DataObject;
            if (baseDataObject == null) // No state.
                return false;
            else if (baseDataObject is PdfStream) // Single state.
            {
                if (key == null)
                {
                    RefOrSelf = null;
                    appearance.Remove(statesKey);
                    return true;
                }
                else // Invalid key.
                    return false;
            }
            else // Multiple state.
                return ((PdfDictionary)baseDataObject).Remove(key);
        }

        public FormXObject this[PdfName key]
        {
            get
            {
                PdfDirectObject baseDataObject = DataObject;
                if (baseDataObject == null) // No state.
                    return null;
                else if (key == null)
                {
                    return baseDataObject as FormXObject;
                }
                else // Multiple state.
                    return ((PdfDictionary)baseDataObject).Get<FormXObject>(key);
            }
            set
            {
                if (key == null) // Single state.
                {
                    RefOrSelf = value?.RefOrSelf;
                    appearance.Set(statesKey, RefOrSelf);
                }
                else // Multiple state.
                { EnsureDictionary()[key] = value?.RefOrSelf; }
            }
        }

        public bool TryGetValue(PdfName key, out FormXObject value)
        {
            value = this[key];
            return (value != null || ContainsKey(key));
        }

        public ICollection<FormXObject> Values => throw new NotImplementedException();

        void ICollection<KeyValuePair<PdfName, FormXObject>>.Add(KeyValuePair<PdfName, FormXObject> entry) => Add(entry.Key, entry.Value);

        public void Clear() => EnsureDictionary().Clear();

        bool ICollection<KeyValuePair<PdfName, FormXObject>>.Contains(KeyValuePair<PdfName, FormXObject> entry)
        {
            PdfDirectObject baseDataObject = DataObject;
            if (baseDataObject == null) // No state.
                return false;
            else if (baseDataObject is PdfStream) // Single state.
                return entry.Value.RefOrSelf.Equals(RefOrSelf);
            else // Multiple state.
                return entry.Value.Equals(this[entry.Key]);
        }

        public void CopyTo(KeyValuePair<PdfName, FormXObject>[] entries, int index)
        {
            foreach (var entry in this)
            {
                entries[index++] = entry;
            }
        }

        public int Count
        {
            get => DataObject switch // No state.
            {
                null => 0,
                PdfStream => 1,
                _ => ((PdfDictionary)DataObject).Count,
            };
        }

        public bool IsReadOnly => false;

        public bool Remove(KeyValuePair<PdfName, FormXObject> entry)
        { throw new NotImplementedException(); }

        IEnumerator<KeyValuePair<PdfName, FormXObject>> IEnumerable<KeyValuePair<PdfName, FormXObject>>.GetEnumerator()
        {
            PdfDirectObject baseDataObject = DataObject;
            if (baseDataObject == null) // No state.
            { /* NOOP. */ }
            else if (baseDataObject is FormXObject formX) // Single state.
            {
                yield return new KeyValuePair<PdfName, FormXObject>(null, formX);
            }
            else // Multiple state.
            {
                foreach (var entry in ((PdfDictionary)baseDataObject))
                {
                    yield return new KeyValuePair<PdfName, FormXObject>(
                      entry.Key,
                      (FormXObject)entry.Value.Resolve(PdfName.XObject));
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<KeyValuePair<PdfName, FormXObject>>)this).GetEnumerator();

        private PdfDictionary EnsureDictionary()
        {
            PdfDirectObject baseDataObject = DataObject;
            if (baseDataObject is PdfStream)
            {
                // NOTE: Single states are erased as they have no valid key
                // to be consistently integrated within the dictionary.
                RefOrSelf = baseDataObject = new PdfDictionary();
                appearance.Set(statesKey, baseDataObject.RefOrSelf);
            }
            else if (baseDataObject == null)
            {
                RefOrSelf = baseDataObject = new PdfDictionary();
                appearance.Set(statesKey, baseDataObject.RefOrSelf);
            }
            return (PdfDictionary)baseDataObject;
        }
    }
}