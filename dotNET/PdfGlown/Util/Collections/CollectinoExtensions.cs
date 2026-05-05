/*
  Copyright 2010 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfGlown.Util.Math;
using System;
using System.Collections;
using System.Collections.Generic;

namespace PdfGlown.Util.Collections
{
    public delegate IList<Interval<T>> DefaultIntervalsCallback<T>(IList<Interval<T>> intervals) where T : IComparable<T>;

    public static class CollectinoExtensions
    {

        public static void AddRange<T>(this Stack<T> collection, IEnumerable<T> enumerable)
        {
            foreach (T item in enumerable)
            { collection.Push(item); }
        }

        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> enumerable)
        {
            foreach (T item in enumerable)
            { collection.Add(item); }
        }

        public static void RemoveAll<T>(this ICollection<T> collection, IEnumerable<T> enumerable)
        {
            foreach (T item in enumerable)
            { collection.Remove(item); }
        }

        /**
          <summary>Sets all the specified entries into this dictionary.</summary>
          <remarks>The effect of this call is equivalent to that of calling the indexer on this dictionary
          once for each entry in the specified enumerable.</remarks>
        */
        public static void SetAll<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, IEnumerable<KeyValuePair<TKey, TValue>> enumerable)
        {
            foreach (KeyValuePair<TKey, TValue> entry in enumerable)
            { dictionary[entry.Key] = entry.Value; }
        }

        public static T RemoveAtValue<T>(this List<T> list, int index)
        {
            var item = list[index];
            list.RemoveAt(index);
            return item;
        }

        public static T RemoveAtValue<T>(this IList list, int index)
        {
            var item = list[index];
            list.RemoveAt(index);
            return (T)item;
        }

        public static void Fill<T>(this T[] list, int offset, int length, T value)
        {
            var max = System.Math.Min(list.Length, offset + length);
            for (int i = offset; i < max; i++)
            {
                list[i] = value;
            }
        }

        public static T[] SubArray<T>(this T[] src, int start, int end)
        {
            var len = end - start;
            var dest = new T[len];
            Array.Copy(src, start, dest, 0, len);
            return dest;
        }

        public static T[] CopyOf<T>(this T[] src, int length)
        {
            var minLength = System.Math.Min(length, src.Length);
            var dest = new T[length];
            Array.Copy(src, 0, dest, 0, minLength);
            return dest;
        }

        public static IList<Interval<T>> GetIntervals<T>(this PdfArray intervalsObject, DefaultIntervalsCallback<T> defaultIntervalsCallback = null)
            where T : struct, IComparable<T>
        {
            IList<Interval<T>> intervals;
            {
                if (intervalsObject == null)
                {
                    intervals = defaultIntervalsCallback == null
                      ? null
                      : defaultIntervalsCallback(new List<Interval<T>>());
                }
                else
                {
                    intervals = new List<Interval<T>>();
                    for (int index = 0, length = intervalsObject.Count; index < length; index += 2)
                    {
                        intervals.Add(
                          new Interval<T>(
                            intervalsObject.GetNumber(index).GetValue<T>(),
                            intervalsObject.GetNumber(index + 1).GetValue<T>()));
                    }
                }
            }
            return intervals;
        }

        public static IList<Interval<T>> GetIntervals<T>(this T[] intervalsObject, DefaultIntervalsCallback<T> defaultIntervalsCallback = null)
            where T : struct, IComparable<T>
        {
            IList<Interval<T>> intervals;
            {
                if (intervalsObject == null)
                {
                    intervals = defaultIntervalsCallback == null
                      ? null
                      : defaultIntervalsCallback(new List<Interval<T>>());
                }
                else
                {
                    intervals = new List<Interval<T>>();
                    for (int index = 0, length = intervalsObject.Length; index < length; index += 2)
                    {
                        intervals.Add(new Interval<T>(intervalsObject[index],
                                                      intervalsObject[index + 1]));
                    }
                }
            }
            return intervals;
        }
    }

    //public static class BytesExtension
    //{
    //    public static byte[] CopyOfRange(this byte[] src, int start, int end)
    //    {
    //        var len = end - start;
    //        var dest = new byte[len];
    //        Array.Copy(src, start, dest, 0, len);
    //        return dest;
    //    }
    //    public static byte[] CopyOfRange(this ReadOnlySpan<byte> src, int start, int end)
    //    {
    //        var len = end - start;
    //        var dest = new byte[len];
    //        src.Slice(start, len).CopyTo(dest);
    //        return dest;
    //    }
    //}
}

