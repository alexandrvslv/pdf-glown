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
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents.Fonts.TTF.Model
{

    /// <summary>
    /// Enumerates the languages supported for GSUB operation.In order to support a new language, you
    /// need to add it here and then implement the { @link GsubWorker } for the given language and return
    /// the same from the
    /// {@link GsubWorkerFactory#getGsubWorker(org.apache.fontbox.ttf.CmapLookup, GsubData)}
    /// @author Palash Ray
    /// </summary>
    public enum Language
    {
        BENGALI,
        CYRILLIC,
        DEVANAGARI,
        GUJARATI,
        LATIN,
        UNSPECIFIED
    }

    public static class LanguageExtensions
    {
        private static readonly Dictionary<Language, string[]> langNames = new()
        {
            { Language.BENGALI, new string[] { "bng2", "beng" } },
            { Language.CYRILLIC, new string[] { "cyrl" } },
            { Language.DEVANAGARI, new string[] { "dev2", "deva" } },
            { Language.GUJARATI, new string[] { "gjr2", "gujr" } },
            { Language.LATIN, new string[] { "latn" } },
            { Language.UNSPECIFIED, Array.Empty<string>() }
        };

        public static string[] GetScriptNames(this Language language)
        {
            return langNames[language];
        }
    }
}