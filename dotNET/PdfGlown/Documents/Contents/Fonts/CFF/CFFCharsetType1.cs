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

namespace PdfGlown.Documents.Contents.Fonts.CCF
{
    /// <summary>
    /// A CFF charset.A charset is an array of CIDs for all glyphs in the font.
    /// @author Valery Bokov
    /// </summary>
    public class CFFCharsetType1 : CFFCharset
    {
        private readonly static string EXCEPTION_MESSAGE = "Not a CIDFont";

        private readonly Dictionary<int, int> sidOrCidToGid = new(250);
        private readonly Dictionary<int, int> gidToSid = new(250);
        private readonly Dictionary<string, int> nameToSid = new(250);

        // inverse
        private readonly Dictionary<int, string> gidToName = new(250);

        public override bool IsCIDFont
        {
            get => false;
        }

        public override void AddSID(int gid, int sid, string name)
        {
            sidOrCidToGid[sid] = gid;
            gidToSid[gid] = sid;
            nameToSid[name] = sid;
            gidToName[gid] = name;
        }

        public override void AddCID(int gid, int cid)
        {
            throw new NotSupportedException(EXCEPTION_MESSAGE);
        }

        public override int GetSIDForGID(int gid)
        {
            return gidToSid.TryGetValue(gid, out int cid) ? cid : 0;
        }

        public override int GetGIDForSID(int sid)
        {
            return sidOrCidToGid.TryGetValue(sid, out int gid) ? gid : 0;
        }

        public override int GetGIDForCID(int cid)
        {
            throw new NotSupportedException(EXCEPTION_MESSAGE);
        }

        public override int GetSID(string name)
        {
            return nameToSid.TryGetValue(name, out int sid) ? sid : 0;
        }

        public override string GetNameForGID(int gid)
        {
            return gidToName.TryGetValue(gid, out var name) ? name : null;
        }

        public override int GetCIDForGID(int gid)
        {
            throw new NotSupportedException(EXCEPTION_MESSAGE);
        }
    }
}
