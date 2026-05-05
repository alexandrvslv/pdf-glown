/*
  Copyright 2015 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfGlown.Bytes;
using PdfGlown.Util;

using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents.Fonts
{
    /// <summary>CMap builder [PDF:1.6:5.6.4,5.9.2;CMAP].</summary>
    internal sealed class CMapBuilder
    {
        public enum EntryTypeEnum
        {
            BaseFont,
            CID
        }

        public static string GetTag(EntryTypeEnum entryType)
        {
            switch (entryType)
            {
                case EntryTypeEnum.BaseFont:
                    return "bf";
                case EntryTypeEnum.CID:
                    return "cid";
                default:
                    throw new NotImplementedException();
            }
        }

        public delegate int GetOutCodeDelegate(KeyValuePair<ByteKey, int> entry);

        private delegate void BuildEntryDelegate<T>(T entry, IByteStream buffer, GetOutCodeDelegate outCodeFunction, string outCodeFormat);

        private static readonly int SubSectionMaxCount = 100;

        /// <summary>Builds a CMap according to the specified arguments.</summary>
        /// <param name="entryType"></param>
        /// <param name="cmapName">CMap name (<code>null</code> in case no custom name is needed).</param>
        /// <param name="codes"></param>
        /// <param name="outCodeFunction"></param>
        /// <returns>Buffer containing the serialized CMap.</returns>
        public static IByteStream Build(EntryTypeEnum entryType, string cmapName, SortedDictionary<ByteKey, int> codes, GetOutCodeDelegate outCodeFunction)
        {
            IByteStream buffer = new ByteStream();

            // Header.
            string outCodeFormat;
            switch (entryType)
            {
                case EntryTypeEnum.BaseFont:
                    {
                        if (cmapName == null)
                        { cmapName = "Adobe-Identity-UCS"; }
                        buffer.Write(
                          "/CIDInit /ProcSet findresource begin\n"
                            + "12 dict begin\n"
                            + "begincmap\n"
                            + "/CIDSystemInfo\n"
                            + "<< /Registry (Adobe)\n"
                            + "/Ordering (UCS)\n"
                            + "/Supplement 0\n"
                            + ">> def\n"
                            + "/CMapName /");
                        buffer.Write(cmapName);
                        buffer.Write(" def\n"
                            + "/CMapVersion 10.001 def\n"
                            + "/CMapType 2 def\n"
                            + "1 begincodespacerange\n"
                            + "<0000> <FFFF>\n"
                            + "endcodespacerange\n"
                          );
                        outCodeFormat = "<{0:X4}>";
                        break;
                    }
                case EntryTypeEnum.CID:
                    {
                        if (cmapName == null)
                        { cmapName = "Custom"; }
                        buffer.Write(
                          "%!PS-Adobe-3.0 Resource-CMap\n"
                            + "%%DocumentNeededResources: ProcSet (CIDInit)\n"
                            + "%%IncludeResource: ProcSet (CIDInit)\n"
                            + "%%BeginResource: CMap (");
                        buffer.Write(cmapName);
                        buffer.Write(")\n"
                            + "%%Title: (");
                        buffer.Write(cmapName);
                        buffer.Write(" Adobe Identity 0)\n"
                        + "%%Version: 1\n"
                        + "%%EndComments\n"
                        + "/CIDInit /ProcSet findresource begin\n"
                        + "12 dict begin\n"
                        + "begincmap\n"
                        + "/CIDSystemInfo 3 dict dup begin\n"
                        + "/Registry (Adobe) def\n"
                        + "/Ordering (Identity) def\n"
                        + "/Supplement 0 def\n"
                        + "end def\n"
                        + "/CMapVersion 1 def\n"
                        + "/CMapType 1 def\n"
                        + "/CMapName /");
                        buffer.Write(cmapName);
                        buffer.Write(" def\n"
                        + "/WMode 0 def\n"
                        + "1 begincodespacerange\n"
                        + "<0000> <FFFF>\n"
                        + "endcodespacerange\n"
                      );
                        outCodeFormat = "{0}";
                        break;
                    }
                default:
                    throw new NotImplementedException();
            }

            // Entries.
            {
                IList<KeyValuePair<ByteKey, int>> cidChars = new List<KeyValuePair<ByteKey, int>>();
                IList<KeyValuePair<ByteKey, int>[]> cidRanges = new List<KeyValuePair<ByteKey, int>[]>();
                {
                    KeyValuePair<ByteKey, int>? lastCodeEntry = null;
                    KeyValuePair<ByteKey, int>[] lastCodeRange = null;
                    foreach (KeyValuePair<ByteKey, int> codeEntry in codes)
                    {
                        if (lastCodeEntry.HasValue)
                        {
                            var codeArray = codeEntry.Key.ToArray();
                            var lastCodeArray = lastCodeEntry.Value.Key.ToArray();
                            int codeLength = codeEntry.Key.Length;
                            if (codeLength == lastCodeEntry.Value.Key.Length
                              && codeArray[codeLength - 1] - lastCodeArray[codeLength - 1] == 1
                              && outCodeFunction(codeEntry) - outCodeFunction(lastCodeEntry.Value) == 1) // Contiguous codes.
                            {
                                if (lastCodeRange == null)
                                { lastCodeRange = new KeyValuePair<ByteKey, int>[] { lastCodeEntry.Value, default(KeyValuePair<ByteKey, int>) }; }
                            }
                            else // Separated codes.
                            {
                                AddEntry(cidRanges, cidChars, lastCodeEntry.Value, lastCodeRange);
                                lastCodeRange = null;
                            }
                        }
                        lastCodeEntry = codeEntry;
                    }
                    AddEntry(cidRanges, cidChars, lastCodeEntry.Value, lastCodeRange);
                }
                // Ranges section.
                BuildEntriesSection(buffer, entryType, cidRanges, BuildRangeEntry, "range", outCodeFunction, outCodeFormat);
                // Chars section.
                BuildEntriesSection(buffer, entryType, cidChars, BuildCharEntry, "char", outCodeFunction, outCodeFormat);
            }

            // Trailer.
            switch (entryType)
            {
                case EntryTypeEnum.BaseFont:
                    buffer.Write(
                      "endcmap\n"
                        + "CMapName currentdict /CMap defineresource pop\n"
                        + "end\n"
                        + "end\n"
                      );
                    break;
                case EntryTypeEnum.CID:
                    buffer.Write(
                      "endcmap\n"
                        + "CMapName currentdict /CMap defineresource pop\n"
                        + "end\n"
                        + "end\n"
                        + "%%EndResource\n"
                        + "%%EOF"
                      );
                    break;
                default:
                    throw new NotImplementedException();
            }

            return buffer;
        }

        private static void AddEntry(
          IList<KeyValuePair<ByteKey, int>[]> cidRanges,
          IList<KeyValuePair<ByteKey, int>> cidChars,
          KeyValuePair<ByteKey, int> lastEntry,
          KeyValuePair<ByteKey, int>[] lastRange)
        {
            if (lastRange != null) // Range.
            {
                lastRange[1] = lastEntry;
                cidRanges.Add(lastRange);
            }
            else // Single character.
            { cidChars.Add(lastEntry); }
        }

        private static void BuildCharEntry(KeyValuePair<ByteKey, int> cidChar, IByteStream buffer, GetOutCodeDelegate outCodeFunction, string outCodeFormat)
        {
            buffer.Write("<");
            buffer.Write(ConvertUtils.ByteArrayToHex(cidChar.Key.ToArray()));
            buffer.Write("> ");
            buffer.Write(String.Format(outCodeFormat, outCodeFunction(cidChar)));
            buffer.Write("\n");
        }

        private static void BuildEntriesSection<T>(
          IByteStream buffer,
          EntryTypeEnum entryType,
          IList<T> items,
          BuildEntryDelegate<T> buildEntryFunction,
          string operatorSuffix,
          GetOutCodeDelegate outCodeFunction,
          string outCodeFormat)
        {
            if (items.Count == 0)
                return;

            for (int index = 0, count = items.Count; index < count; index++)
            {
                if (index % SubSectionMaxCount == 0)
                {
                    if (index > 0)
                    {
                        buffer.Write("end");
                        buffer.Write(GetTag(entryType));
                        buffer.Write(operatorSuffix);
                        buffer.Write("\n");
                    }
                    buffer.Write(Math.Min(count - index, SubSectionMaxCount).ToString());
                    buffer.Write(" ");
                    buffer.Write("begin");
                    buffer.Write(GetTag(entryType));
                    buffer.Write(operatorSuffix);
                    buffer.Write("\n");
                }
                buildEntryFunction(items[index], buffer, outCodeFunction, outCodeFormat);
            }
            buffer.Write("end");
            buffer.Write(GetTag(entryType));
            buffer.Write(operatorSuffix);
            buffer.Write("\n");
        }

        private static void BuildRangeEntry(KeyValuePair<ByteKey, int>[] cidRange, IByteStream buffer, GetOutCodeDelegate outCodeFunction, string outCodeFormat)
        {
            buffer.Write("<");
            buffer.Write(ConvertUtils.ByteArrayToHex(cidRange[0].Key.ToArray()));
            buffer.Write("> <");
            buffer.Write(ConvertUtils.ByteArrayToHex(cidRange[1].Key.ToArray()));
            buffer.Write("> ");
            buffer.Write(String.Format(outCodeFormat, outCodeFunction(cidRange[0])));
            buffer.Write("\n");
        }
    }    
}
