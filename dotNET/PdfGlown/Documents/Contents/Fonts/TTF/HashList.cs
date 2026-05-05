/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Runtime.InteropServices;

namespace PdfGlown.Documents.Contents.Fonts.TTF
{
    public struct HashList<T> : IEquatable<HashList<T>>
    {
        public static explicit operator HashList<T>(T[] data) => new HashList<T>(data);
        public static explicit operator HashList<T>(T single) => new HashList<T>(single);

        public Memory<T> Data;
        public T Single;
        private int? hash = null; 

        public HashList(T[] data)
        {
            Single = default(T);
            Data = data;
        }

        public HashList(T single)
        {
            Single = single;
            Data = Memory<T>.Empty;
        }

        public Span<T> Span => Data.Length == 0
            ? MemoryMarshal.CreateSpan(ref Single, 1)
            : Data.Span;

        public override bool Equals(object obj)
        {
            return obj is HashList<T> other ? Equals(other) : false;
        }

        public bool Equals(HashList<T> other)
        {
            return Span.SequenceEqual(other.Span);
        }

        public override int GetHashCode()
        {
            return hash ??= GenerateHashCode();
        }

        private int GenerateHashCode()
        {
            var hash = new HashCode();
            foreach (var entry in Span)
                hash.Add<T>(entry);
            return hash.ToHashCode();
        }
    }
}
