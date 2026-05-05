/*
  Copyright 2006-2015 Stefano Chizzolini. http://www.pdfclown.org

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

using System.Collections.Generic;

namespace PdfGlown.Objects
{
    public static class DictionaryExtensions
    {
        public static T Get<T>(this Dictionary<PdfName, PdfDirectObject> entries, PdfName key)
            where T : class
        {
            return entries.TryGetValue(key, out var value)
                ? value?.Resolve(key) as T
                : default;
        }

        public static T Get<T>(this Dictionary<PdfName, PdfDirectObject> entries, PdfName key, T deaultValue)
            where T : class
        {
            return entries.TryGetValue(key, out var value)
                ? value?.Resolve(key) as T ?? deaultValue
                : deaultValue;
        }

        public static int GetInt(this Dictionary<PdfName, PdfDirectObject> entries, PdfName key, int deaultValue = 0)
        {
            return entries.Get<IPdfNumber>(key)?.IntValue ?? deaultValue;
        }

        public static T Get<T>(this List<PdfDirectObject> entries, int key)
            where T : class
        {
            var value = entries.Count > key ? entries[key] : null;
            return value?.Resolve() as T;
        }

        public static int GetInt(this List<PdfDirectObject> entries, int index, int defaultValue = 0)
        {
            var value = entries.Count > index ? entries[index] : null;
            return (value?.Resolve() as IPdfNumber)?.IntValue ?? defaultValue;
        }

        public static double GetDouble(this List<PdfDirectObject> entries, int index, double deaultValue = 0)
        {
            var value = entries.Count > index ? entries[index] : null;
            return (value?.Resolve() as IPdfNumber)?.DoubleValue ?? deaultValue;
        }

        public static IPdfNumber GetNumber(this List<PdfDirectObject> entries, int index)
        {
            var value = entries.Count > index ? entries[index] : null;
            return value?.Resolve() as IPdfNumber;
        }

        /// <summary>Gets the attribute value corresponding to the specified key, possibly recurring to
        /// its ancestor nodes in the page tree.</summary>
        /// <param name="start">PdfDictionary object.</param>
        /// <param name="key">Attribute key.</param>
        public static PdfDirectObject GetInheritableAttribute(this PdfDictionary start, PdfName key, PdfDirectObject defaultValue = null)
        {
            // NOTE: It moves upward until it finds the inherited attribute.
            var dictionary = start;
            while (dictionary != null)
            {
                if (dictionary.TryGetValue(key, out var entry)
                    && entry != null)
                    return entry;
                dictionary = dictionary.Get<PdfDictionary>(PdfName.Parent);
            }
            return defaultValue;
        }       
    }
}