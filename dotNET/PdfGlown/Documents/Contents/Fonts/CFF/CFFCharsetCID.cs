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
    /**
     * A CFF charset. A charset is an array of CIDs for all glyphs in the font.
     *
     * @author Valery Bokov
     */
    public class CFFCharsetCID : CFFCharset
    {
        private readonly static string EXCEPTION_MESSAGE = "Not a Type 1-equivalent font";

        private readonly Dictionary<int, int> sidOrCidToGid = new(250);

        // inverse
        private readonly Dictionary<int, int> gidToCid = new();

        public override bool IsCIDFont
        {
            get => true;
        }

        public override void AddSID(int gid, int sid, string name)
        {
            throw new NotSupportedException(EXCEPTION_MESSAGE);
        }

        public override void AddCID(int gid, int cid)
        {
            sidOrCidToGid[cid] = gid;
            gidToCid[gid] = cid;
        }

        public override int GetSIDForGID(int sid)
        {
            throw new NotSupportedException(EXCEPTION_MESSAGE);
        }

        public override int GetGIDForSID(int sid)
        {
            throw new NotSupportedException(EXCEPTION_MESSAGE);
        }

        public override int GetGIDForCID(int cid)
        {
            return !sidOrCidToGid.TryGetValue(cid, out int gid) ? 0 : gid;
        }

        public override int GetSID(string name)
        {
            throw new NotSupportedException(EXCEPTION_MESSAGE);
        }

        public override string GetNameForGID(int gid)
        {
            throw new NotSupportedException(EXCEPTION_MESSAGE);
        }

        public override int GetCIDForGID(int gid)
        {
            return !gidToCid.TryGetValue(gid, out int cid) ? cid : 0;
        }
    }
}
