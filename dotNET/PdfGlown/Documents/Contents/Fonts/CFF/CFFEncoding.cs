/*
 * https://github.com/apache/pdfbox
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
namespace PdfGlown.Documents.Contents.Fonts.CCF
{
    /// <summary>
    /// A CFF Type 1-equivalent Encoding.An encoding is an array of codes associated with some or all
    /// glyphs in a font
    /// @author John Hewson
    /// </summary>
    public abstract class CFFEncoding : Encoding
    {
        /// <summary>Package-private constructor for subclasses.</summary>
        public CFFEncoding()
        { }


        /// <summary>Adds a new code/SID combination to the encoding.</summary>
        /// <param name="code">the given code</param>
        /// <param name="sid">the given SID</param>
        /// <param name="name"></param>
        public void Add(int code, int sid, string name)
        {
            Put(code, name);
        }

        /// <summary>For use by subclasses only.</summary>
        protected void Add(int code, int sid)
        {
            Put(code, CFFStandardString.GetName(sid));
        }
    }
}