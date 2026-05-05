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
    public class DefaultGsubData : IGsubData
    {
        public static readonly IGsubData NO_DATA_FOUND = new DefaultGsubData();
        public bool IsFeatureSupported(string featureName)
        {
            throw new NotSupportedException();
        }

        public Language Language
        {
            get => throw new NotSupportedException();
        }

        public string ActiveScriptName
        {
            get => throw new NotSupportedException();
        }

        public ICollection<string> SupportedFeatures
        {
            get => throw new NotSupportedException();
        }

        public IScriptFeature GetFeature(string featureName)
        {
            throw new NotSupportedException();
        }
    }
}
