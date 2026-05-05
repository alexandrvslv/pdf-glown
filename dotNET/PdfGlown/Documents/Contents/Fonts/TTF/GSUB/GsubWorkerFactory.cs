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

using PdfGlown.Documents.Contents.Fonts.TTF.Model;

namespace PdfGlown.Documents.Contents.Fonts.TTF.GSUB
{
    /// <summary>
    /// Gets a {@link Language} specific instance of a {@link GsubWorker}
    /// @author Palash Ray
    /// </summary>
    public class GsubWorkerFactory
    {
        public IGsubWorker GetGsubWorker(ICmapLookup cmapLookup, IGsubData gsubData)
        {
            switch (gsubData.Language)
            {
                case Language.BENGALI:
                    return new GsubWorkerForBengali(cmapLookup, gsubData);
                //case Language.DEVANAGARI:
                //    return new GsubWorkerForDevanagari(cmapLookup, gsubData);
                //case Language.GUJARATI:
                //    return new GsubWorkerForGujarati(cmapLookup, gsubData);
                case Language.LATIN:
                    return new GsubWorkerForLatin(cmapLookup, gsubData);
                default:
                    return new DefaultGsubWorker();

            }
        }
    }
}