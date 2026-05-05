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

namespace PdfGlown.Documents.Contents.Fonts.CCF
{
    /// <summary>Class representing an embedded CFF charset.</summary>
    class EmbeddedCharset : CFFCharset
    {
        private readonly CFFCharset charset;

        public EmbeddedCharset(bool isCIDFont)
        {
            charset = isCIDFont ? new CFFCharsetCID() : new CFFCharsetType1();
        }

        public override int GetCIDForGID(int gid)
        {
            return charset.GetCIDForGID(gid);
        }

        public override bool IsCIDFont
        {
            get => charset.IsCIDFont;
        }

        public override void AddSID(int gid, int sid, string name)
        {
            charset.AddSID(gid, sid, name);
        }

        public override void AddCID(int gid, int cid)
        {
            charset.AddCID(gid, cid);
        }

        public override int GetSIDForGID(int sid)
        {
            return charset.GetSIDForGID(sid);
        }

        public override int GetGIDForSID(int sid)
        {
            return charset.GetGIDForSID(sid);
        }

        public override int GetGIDForCID(int cid)
        {
            return charset.GetGIDForCID(cid);
        }

        public override int GetSID(string name)
        {
            return charset.GetSID(name);
        }

        public override string GetNameForGID(int gid)
        {
            return charset.GetNameForGID(gid);
        }
    }
}
