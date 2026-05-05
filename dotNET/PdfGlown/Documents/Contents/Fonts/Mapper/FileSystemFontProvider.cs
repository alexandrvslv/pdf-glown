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
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Utilities.Encoders;
using PdfGlown.Documents.Contents.Fonts.Autodetect;
using PdfGlown.Documents.Contents.Fonts.CCF;
using PdfGlown.Documents.Contents.Fonts.TTF;
using PdfGlown.Documents.Contents.Fonts.Type1;
using PdfGlown.Tokens;
using PdfGlown.Util.IO;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace PdfGlown.Documents.Contents.Fonts
{

    /// <summary>
    /// A FontProvider which searches for fonts on the local filesystem.
    /// @author John Hewson
    /// </summary>
    public sealed class FileSystemFontProvider : FontProvider
    {
        private readonly List<FSFontInfo> fontInfoList = new();
        private readonly FontCache cache;

        private class FSFontInfo : FontInfo
        {
            private readonly string postScriptName;
            private readonly FontFormat format;
            private readonly CIDSystemInfo cidSystemInfo;
            private readonly int usWeightClass;
            private readonly int sFamilyClass;
            private readonly int ulCodePageRange1;
            private readonly int ulCodePageRange2;
            private readonly int macStyle;
            private readonly PanoseClassification panose;
            internal readonly FileInfo file;
            private readonly FileSystemFontProvider parent;
            internal readonly string hash;
            private readonly long lastModified;

            public FSFontInfo(FileInfo file, FontFormat format, string postScriptName,
                               CIDSystemInfo cidSystemInfo, int usWeightClass, int sFamilyClass,
                               int ulCodePageRange1, int ulCodePageRange2, int macStyle, byte[] panose,
                               FileSystemFontProvider parent,
                               string hash, long lastModified)
            {
                this.file = file;
                this.format = format;
                this.postScriptName = postScriptName;
                this.cidSystemInfo = cidSystemInfo;
                this.usWeightClass = usWeightClass;
                this.sFamilyClass = sFamilyClass;
                this.ulCodePageRange1 = ulCodePageRange1;
                this.ulCodePageRange2 = ulCodePageRange2;
                this.macStyle = macStyle;
                this.panose = panose != null && panose.Length >= PanoseClassification.PanoseLength
                    ? new PanoseClassification(panose)
                    : null;
                this.parent = parent;
                this.hash = hash;
                this.lastModified = lastModified;
            }

            public override string PostScriptName
            {
                get => postScriptName;
            }

            public override FontFormat Format
            {
                get => format;
            }

            public override CIDSystemInfo CIDSystemInfo
            {
                get => cidSystemInfo;
            }

            public override BaseFont Font
            {
                get
                {
                    BaseFont cached = parent.cache.GetFont(this);
                    if (cached != null)
                    {
                        return cached;
                    }
                    else
                    {
                        BaseFont font;
                        switch (format)
                        {
                            case FontFormat.PFB: font = GetType1Font(postScriptName, file); break;
                            case FontFormat.TTF: font = GetTrueTypeFont(postScriptName, file); break;
                            case FontFormat.OTF: font = GetOTFFont(postScriptName, file); break;
                            default: throw new Exception("can't happen");
                        }
                        if (font != null)
                        {
                            parent.cache.AddFont(this, font);
                        }
                        return font;
                    }
                }
            }

            public override int FamilyClass
            {
                get => sFamilyClass;
            }

            public override int WeightClass
            {
                get => usWeightClass;
            }

            public override int CodePageRange1
            {
                get => ulCodePageRange1;
            }

            public override int CodePageRange2
            {
                get => ulCodePageRange2;
            }

            public override int MacStyle
            {
                get => macStyle;
            }

            public override PanoseClassification Panose
            {
                get => panose;
            }

            public override string ToString()
            {
                return $"{base.ToString()} {file} {hash} {lastModified}";
            }

            private TrueTypeFont GetTrueTypeFont(string postScriptName, FileInfo file)
            {
                try
                {

                    TrueTypeFont ttf = ReadTrueTypeFont(postScriptName, file);
#if DEBUG
                    Debug.WriteLine($"debug: Loaded {postScriptName} from {file}");
#endif
                    return ttf;
                }
                catch (IOException e)
                {
                    Debug.WriteLine($"error: Could not load font file: {file} {e}");
                }
                return null;
            }

            private TrueTypeFont ReadTrueTypeFont(string postScriptName, FileInfo file)
            {
                if (file.Name.ToLowerInvariant().EndsWith(".ttc"))
                {
                    //@SuppressWarnings("squid:S2095")
                    // ttc not closed here because it is needed later when ttf is accessed,
                    // e.g. rendering PDF with non-embedded font which is in ttc file in our font directory
                    var ttc = new TrueTypeCollection(file);
                    try
                    {
                        var ttf = ttc.GetFontByName(postScriptName);
                        if (ttf == null)
                        {
                            throw new IOException("Font " + postScriptName + " not found in " + file);
                        }
                        return ttf;
                    }
                    catch (IOException)
                    {
                        ttc.Dispose();
                        throw;
                    }
                }
                else
                {
                    TTFParser ttfParser = new TTFParser(false);
                    return ttfParser.Parse(file.FullName);
                }
            }

            private OpenTypeFont GetOTFFont(string postScriptName, FileInfo file)
            {
                try
                {
                    if (file.Name.EndsWith(".ttc", StringComparison.OrdinalIgnoreCase))
                    {
                        // ttc not closed here because it is needed later when ttf is accessed,
                        // e.g. rendering PDF with non-embedded font which is in ttc file in our font directory
                        var ttc = new TrueTypeCollection(file);
                        try
                        {
                            var ttf = ttc.GetFontByName(postScriptName);
                            if (ttf == null)
                            {
                                throw new IOException("Font " + postScriptName + " not found in " + file);
                            }
                            return (OpenTypeFont)ttf;
                        }
                        catch (IOException)
                        {
                            ttc.Dispose();
                            throw;
                        }
                    }

                    var parser = new OTFParser(false);
                    using var stream = file.OpenRead();
                    var otf = parser.Parse(stream);
#if DEBUG
                    Debug.WriteLine($"debug: Loaded {postScriptName} from {file}");
#endif
                    return (OpenTypeFont)otf;
                }
                catch (IOException e)
                {
                    Debug.WriteLine($"eror: Could not load font file: {file} {e}");
                }
                return null;
            }

            private Type1Font GetType1Font(string postScriptName, FileInfo file)
            {
                try
                {
                    using (var input = file.OpenRead())
                    {
                        Type1Font type1 = Type1Font.CreateWithPFB(new Bytes.ByteStream(input));
#if DEBUG
                        Debug.WriteLine($"debug: Loaded {postScriptName} from {file}");
#endif
                        return type1;
                    }
                }
                catch (IOException e)
                {
                    Debug.WriteLine($"eror: Could not load font file: {file} {e}");
                }
                return null;
            }
        }

        private FSFontInfo CreateFSIgnored(FileInfo file, FontFormat format, String postScriptName)
        {
            String hash;
            try
            {
                hash = ComputeHash(file.FullName);
            }
            catch (IOException)
            {
                hash = "";
            }
            return new FSFontInfo(file, format, postScriptName, null, 0, 0, 0, 0, 0, null, null, hash, file.LastWriteTimeUtc.ToBinary());
        }

        public FileSystemFontProvider(FontCache cache)
        {
            this.cache = cache;
            try
            {
#if TRACE
                Debug.WriteLine("trace: Will search the local system for fonts");
#endif
                // scan the local system for font files
                var fontFileFinder = new FontFileFinder();
                List<FileInfo> fonts = fontFileFinder.Find();

#if TRACE
                Debug.WriteLine($"trace: Found {fonts.Count} fonts on the local system");
#endif
                if (fonts.Any())
                {
                    // load cached FontInfo objects
                    List<FSFontInfo> cachedInfos = LoadDiskCache(fonts);
                    if (cachedInfos != null && cachedInfos.Count > 0)
                    {
                        fontInfoList.AddRange(cachedInfos);
                    }
                    else
                    {
                        Debug.WriteLine("warn: Building on-disk font cache, this may take a while");
                        ScanFonts(fonts);
                        SaveDiskCache();
                        Debug.WriteLine($"warn: Finished building on-disk font cache, found {fontInfoList.Count} fonts");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"error: Error accessing the file system {e}");
            }
        }

        private void ScanFonts(List<FileInfo> files)
        {
            foreach (FileInfo file in files)
            {
                try
                {
                    if (file.Name.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) ||
                            file.Name.EndsWith(".otf", StringComparison.OrdinalIgnoreCase))
                    {
                        AddTrueTypeFont(file);
                    }
                    else if (file.Name.EndsWith(".ttc", StringComparison.OrdinalIgnoreCase) ||
                            file.Name.EndsWith(".otc", StringComparison.OrdinalIgnoreCase))
                    {
                        AddTrueTypeCollection(file);
                    }
                    else if (file.Name.EndsWith(".pfb", StringComparison.OrdinalIgnoreCase))
                    {
                        AddType1Font(file);
                    }
                }
                catch (IOException e)
                {
                    Debug.WriteLine($"eror: Error parsing font {file.Name} {e}");
                }
            }
        }

        private FileInfo GetDiskCacheFile()
        {
            string path = Environment.GetEnvironmentVariable("PdfBox.FontCache");
            if (path == null || !Directory.Exists(path))// || !new DirectoryInfo(path).GetAccessControl()
            {
                path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PdfBox.FontCache");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            return new FileInfo(Path.Combine(path, "PdfBox.Cache"));
            //return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        }

        /// <summary>Saves the font metadata cache to disk.</summary>
        private void SaveDiskCache()
        {
            try
            {
                FileInfo file = GetDiskCacheFile();

                try
                {
                    using (var fileStream = file.OpenWrite())
                    using (var writer = new BinaryWriter(fileStream, System.Text.Encoding.UTF8))
                    {
                        foreach (FSFontInfo fontInfo in fontInfoList)
                        {
                            WriteFontInfo(writer, fontInfo);
                        }
                        writer.Flush();
                    }
                }
                catch (IOException e)
                {
                    Debug.WriteLine($"warn: Could not write to font cache {e}");
                    Debug.WriteLine("warn: Installed fonts information will have to be reloaded for each start");
                    Debug.WriteLine("warn: You can assign a directory to the 'pdfbox.fontcache' property");
                }
            }
            catch (SecurityException e)
            {
                Debug.WriteLine($"debug: Couldn't create writer for font cache file {e}");
            }
        }

        private void WriteFontInfo(BinaryWriter writer, FSFontInfo fontInfo)
        {
            writer.Write(fontInfo.PostScriptName.Trim());
            writer.Write((int)fontInfo.Format);
            if (fontInfo.CIDSystemInfo != null)
            {
                writer.Write(fontInfo.CIDSystemInfo.Registry + '-' +
                                fontInfo.CIDSystemInfo.Ordering + '-' +
                                fontInfo.CIDSystemInfo.Supplement);
            }
            else
            {
                writer.Write(" ");
            }
            writer.Write(fontInfo.WeightClass);
            writer.Write(fontInfo.FamilyClass);
            writer.Write(fontInfo.CodePageRange1);
            writer.Write(fontInfo.CodePageRange2);
            writer.Write(fontInfo.MacStyle);
            if (fontInfo.Panose != null)
            {
                writer.Write((byte)10);
                var bytes = fontInfo.Panose.Span;
                writer.Write(bytes);
            }
            else
            {
                writer.Write((byte)0);
            }
            writer.Write(fontInfo.file.FullName);
            writer.Write(fontInfo.hash);
            writer.Write(fontInfo.file.LastWriteTimeUtc.ToBinary());
            writer.Write((byte)'\n');

        }

        /// <summary>Loads the font metadata cache from disk.</summary>
        /// <param name="files"></param>
        /// <returns></returns>
        private List<FSFontInfo> LoadDiskCache(List<FileInfo> files)
        {
            ISet<string> pending = new HashSet<string>(files.Select(x => x.FullName), StringComparer.Ordinal);
            List<FSFontInfo> results = new List<FSFontInfo>(files.Count);

            // Get the disk cache
            FileInfo file = null;
            bool fileExists = false;
            try
            {
                file = GetDiskCacheFile();
                fileExists = file.Exists;
            }
            catch (SecurityException e)
            {
                Debug.WriteLine("debug: Error checking for file existence", e);
            }

            if (fileExists)
            {
                try
                {
                    using (var fileStream = file.OpenRead())
                    using (var reader = new BinaryReader(fileStream, System.Text.Encoding.UTF8))
                    {
                        while (reader.PeekChar() > -1)
                        {

                            string postScriptName = reader.ReadString();
                            FontFormat format = (FontFormat)reader.ReadInt32();

                            CIDSystemInfo cidSystemInfo = null;
                            string cidSystemInfoText = reader.ReadString();
                            if (!string.IsNullOrWhiteSpace(cidSystemInfoText))
                            {
                                string[] ros = cidSystemInfoText.Split('-');
                                cidSystemInfo = new CIDSystemInfo(null, ros[0], ros[1], int.TryParse(ros[2], out var intValue) ? intValue : 0);
                            }
                            int usWeightClass = reader.ReadInt32();
                            int sFamilyClass = reader.ReadInt32();
                            int ulCodePageRange1 = reader.ReadInt32();
                            int ulCodePageRange2 = reader.ReadInt32();
                            int macStyle = reader.ReadInt32();

                            byte[] panose = null;
                            if (reader.ReadByte() > 0)
                            {
                                panose = new byte[10];
                                reader.Read(panose, 0, 10);
                            }

                            var fullpath = reader.ReadString();
                            var hash = reader.ReadString();
                            var lastModified = reader.ReadInt64();
                            if (reader.ReadByte() != '\n')
                                return null;

                            if (!string.IsNullOrEmpty(fullpath))
                            {
                                FileInfo fontFile = new FileInfo(fullpath);
                                if (fontFile.Exists)
                                {
                                    if (fontFile.LastWriteTimeUtc.ToBinary() != lastModified)
                                    {
                                        string newHash = ComputeHash(fontFile.FullName);
                                        if (newHash.Equals(hash))
                                        {
                                            lastModified = fontFile.LastWriteTimeUtc.ToBinary();
                                            hash = newHash;
                                        }
                                        else
                                        {
                                            Debug.WriteLine($"debug: Font file {fontFile.FullName} is different");
                                            continue; // don't remove from "pending"
                                        }
                                    }
                                    FSFontInfo info = new FSFontInfo(fontFile, format, postScriptName,
                                            cidSystemInfo, usWeightClass, sFamilyClass, ulCodePageRange1,
                                            ulCodePageRange2, macStyle, panose, this, hash, lastModified);
                                    results.Add(info);
                                }
                                else
                                {
                                    Debug.WriteLine($"debug: Font file {fullpath} not found, skipped");
                                }
                            }
                            else
                            {
                                Debug.WriteLine($"debug: Font file {fullpath} not found, skipped");
                            }
                            pending.Remove(fullpath);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"eror: Error loading font cache, will be re-built {e}");
                }
            }

            if (pending.Count > 0)
            {
                // re-build the entire cache if we encounter un-cached fonts (could be optimised)
                Debug.WriteLine("warn: New/Changed fonts found, font cache will be re-built");
                return null;
            }

            return results;
        }

        /// <summary>
        /// Adds a TTC or OTC to the file cache. To reduce memory, the parsed font is not cached.
        /// </summary>
        private void AddTrueTypeCollection(FileInfo ttcFile)
        {
            try
            {
                using var ttc = new TrueTypeCollection(ttcFile);
                ttc.ProcessAllFonts(AddTrueTypeFontImpl, ttcFile);
            }
            catch (IOException e)
            {
                Debug.WriteLine("eror: Could not load font file: " + ttcFile, e);
            }
        }

        /// <summary>
        /// Adds an OTF or TTF font to the file cache. To reduce memory, the parsed font is not cached.
        /// </summary>
        private void AddTrueTypeFont(FileInfo ttfFile)
        {
            FontFormat fontFormat = (FontFormat)(-1);
            try
            {
                if (ttfFile.Name.EndsWith(".otf", StringComparison.OrdinalIgnoreCase))
                {
                    fontFormat = FontFormat.OTF;
                    var parser = new OTFParser(false);
                    var otf = parser.Parse(ttfFile.FullName);
                    AddTrueTypeFontImpl(otf, ttfFile);
                }
                else
                {
                    fontFormat = FontFormat.TTF;
                    var parser = new TTFParser(false);
                    var ttf = parser.Parse(ttfFile.FullName);
                    AddTrueTypeFontImpl(ttf, ttfFile);
                }
            }
            catch (IOException e)
            {
                Debug.WriteLine("eror: Could not load font file: " + ttfFile, e);
                fontInfoList.Add(CreateFSIgnored(ttfFile, fontFormat, "*skipexception*"));
            }
        }

        /// <summary>
        /// Adds an OTF or TTF font to the file cache. To reduce memory, the parsed font is not cached.
        /// </summary>
        private void AddTrueTypeFontImpl(TrueTypeFont ttf, object tag)
        {
            AddTrueTypeFontImpl(ttf, (FileInfo)tag);
        }

        private void AddTrueTypeFontImpl(TrueTypeFont ttf, FileInfo file)
        {
            try
            {
                // read PostScript name, if any
                if (ttf.Name != null && ttf.Name.Contains("|"))
                {
                    fontInfoList.Add(CreateFSIgnored(file, FontFormat.TTF, "*skippipeinname*"));
                    Debug.WriteLine($"warn: Skipping font with '|' in name {ttf.Name} in file {file}");
                }
                else if (ttf.Name != null)
                {
                    // ignore bitmap fonts
                    if (ttf.Header == null)
                    {
                        fontInfoList.Add(CreateFSIgnored(file, FontFormat.TTF, ttf.Name));
                        return;
                    }
                    int macStyle = ttf.Header.MacStyle;

                    int sFamilyClass = -1;
                    int usWeightClass = -1;
                    int ulCodePageRange1 = 0;
                    int ulCodePageRange2 = 0;
                    byte[] panose = null;
                    // Apple's AAT fonts don't have an OS/2 table
                    if (ttf.OS2Windows is OS2WindowsMetricsTable os2windowsMetricsTable)
                    {
                        sFamilyClass = os2windowsMetricsTable.FamilyClass;
                        usWeightClass = os2windowsMetricsTable.WeightClass;
                        ulCodePageRange1 = (int)os2windowsMetricsTable.CodePageRange1;
                        ulCodePageRange2 = (int)os2windowsMetricsTable.CodePageRange2;
                        panose = os2windowsMetricsTable.Panose;
                    }
                    string hash = ComputeHash(file.FullName);

                    if (ttf is OpenTypeFont openTypeFont && openTypeFont.IsPostScript)
                    {
                        CIDSystemInfo ros = null;
                        if (openTypeFont.IsSupportedOTF && openTypeFont.CFF != null)
                        {
                            CFFFont cff = openTypeFont.CFF.Font;

                            if (cff is CFFCIDFont cidFont)
                            {
                                string registry = cidFont.Registry;
                                string ordering = cidFont.Ordering;
                                int supplement = cidFont.Supplement;
                                ros = new CIDSystemInfo(null, registry, ordering, supplement);
                            }
                        }
                        fontInfoList.Add(new FSFontInfo(file, FontFormat.OTF, ttf.Name, ros,
                                usWeightClass, sFamilyClass, ulCodePageRange1, ulCodePageRange2,
                                macStyle, panose, this, hash, file.LastWriteTimeUtc.ToBinary()));
                    }
                    else
                    {
                        CIDSystemInfo ros = null;
                        if (ttf.TableMap.TryGetValue("gcid", out var gcid))
                        {
                            // Apple's AAT fonts have a "gcid" table with CID info
                            var bytes = ttf.GetTableBytes(gcid).Span;
                            string reg = Charset.ASCII.GetString(bytes.Slice(10, 64));
                            string registryName = reg.Substring(0, reg.IndexOf('\0'));
                            string ord = Charset.ASCII.GetString(bytes.Slice(76, 64));
                            string orderName = ord.Substring(0, ord.IndexOf('\0'));
                            int supplementVersion = (bytes[140] & 0xff) << 8 & (bytes[141] & 0xff);
                            ros = new CIDSystemInfo(null, registryName, orderName, supplementVersion);
                        }

                        fontInfoList.Add(new FSFontInfo(file, FontFormat.TTF, ttf.Name, ros,
                                usWeightClass, sFamilyClass, ulCodePageRange1, ulCodePageRange2,
                                macStyle, panose, this, hash, file.LastWriteTimeUtc.ToBinary()));
                    }

                }
                else
                {
                    fontInfoList.Add(CreateFSIgnored(file, FontFormat.TTF, "*skipnoname*"));
                    Debug.WriteLine($"warn: Missing 'name' entry for PostScript name in font {file}");
                }
            }
            catch (IOException e)
            {
                fontInfoList.Add(CreateFSIgnored(file, FontFormat.TTF, "*skipexception*"));
                Debug.WriteLine($"eror: Could not load font file: {file} {e}");
            }
            finally
            {
                ttf.Dispose();
            }
        }

        /// <summary>
        /// Adds a Type 1 font to the file cache. To reduce memory, the parsed font is not cached.
        /// </summary>
        private void AddType1Font(FileInfo pfbFile)
        {
            try
            {
                using var fileStream = pfbFile.OpenRead();
                using var input = new Bytes.ByteStream(fileStream);
                Type1Font type1 = Type1Font.CreateWithPFB(input);
                if (type1.Name == null)
                {
                    fontInfoList.Add(CreateFSIgnored(pfbFile, FontFormat.PFB, "*skipnoname*"));
                    Debug.WriteLine("warn: Missing 'name' entry for PostScript name in font " + pfbFile);
                    return;
                }
                if (type1.Name.Contains("|"))
                {
                    fontInfoList.Add(CreateFSIgnored(pfbFile, FontFormat.PFB, "*skippipeinname*"));
                    Debug.WriteLine($"warn: Skipping font with '|' in name {type1.Name} in file {pfbFile}");
                    return;
                }
                string hash = ComputeHash(pfbFile.FullName);
                fontInfoList.Add(new FSFontInfo(pfbFile, FontFormat.PFB, type1.Name,
                                                null, -1, -1, 0, 0, -1, null, this, hash, pfbFile.LastWriteTimeUtc.ToBinary()));

#if TRACE
                    Debug.WriteLine($"trace: PFB: '{type1.Name}' / '{type1.FamilyName}' / '{type1.Weight}'");
#endif
            }
            catch (IOException e)
            {
                Debug.WriteLine($"eror: Could not load font file: {pfbFile} {e}");
            }
        }


        public override string ToDebugString()
        {
            var sb = new StringBuilder();
            foreach (FSFontInfo info in fontInfoList)
            {
                sb.Append(info.Format);
                sb.Append(": ");
                sb.Append(info.PostScriptName);
                sb.Append(": ");
                sb.Append(info.file.Name);
                sb.Append('\n');
            }
            return sb.ToString();
        }


        public override IEnumerable<FontInfo> FontInfo
        {
            get => fontInfoList;
        }

        private static string ComputeHash(string filePath)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(4 * 1024);
            try
            {
#if __BC_HASH__
                var md = new Sha512Digest();
#else
                using var md = IncrementalHash.CreateHash(HashAlgorithmName.SHA512);
#endif
                using var file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, buffer.Length);
                var read = 0;
                while ((read = file.Read(buffer, 0, buffer.Length)) > 0)
                {
                    md.Update(buffer, 0, read);
                }
                var sha512 = md.Digest();
                return Hex.ToHexString(sha512);
            }
            catch (CryptographicException)
            {
                // never happens
                return "";
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
