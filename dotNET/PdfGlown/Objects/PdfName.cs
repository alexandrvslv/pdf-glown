/*
  Copyright 2006-2015 Stefano Chizzolini. http://www.pdfclown.org

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
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using PdfGlown.Tokens;

namespace PdfGlown.Objects
{
    /// <summary>PDF name object [PDF:1.6:3.2.4].</summary>
    public sealed partial class PdfName : PdfSimpleObject<string>, IPdfString, IEquatable<PdfName>
    {
        //NOTE: As name objects are simple symbols uniquely defined by sequences of characters,
        //the bytes making up the name are never treated as text, always keeping them escaped.

        //NOTE: Name lexical conventions prescribe that the following reserved characters
        //are to be escaped when placed inside names' character sequences:
        //  - delimiters;
        //  - whitespaces;
        //  - '#' (number sign character).
        [GeneratedRegex("#([\\da-fA-F]{2})", RegexOptions.CultureInvariant)]
        private static partial Regex GetEscapedPattern();
        [GeneratedRegex("[\\s\\(\\)<>\\[\\]{}/%#]", RegexOptions.CultureInvariant)]
        private static partial Regex GetUnescapedPattern();

        private static readonly byte[] NamePrefixChunk = Tokens.BaseEncoding.Pdf.Encode(Keyword.NamePrefix);
        private static readonly ConcurrentDictionary<string, PdfName> names = new(Environment.ProcessorCount, 800, StringComparer.Ordinal);

#pragma warning disable 0108
        public static readonly PdfName A = Add("A");
        public static readonly PdfName a = Add("a");
        public static readonly PdfName A85 = Add("A85");
        public static readonly PdfName AA = Add("AA");
        public static readonly PdfName AC = Add("AC");
        public static readonly PdfName Accepted = Add("Accepted");
        public static readonly PdfName Action = Add("Action");
        public static readonly PdfName AcroForm = Add("AcroForm");
        public static readonly PdfName AdobePPKLite = Add("Adobe.PPKLite");
        public static readonly PdfName AESV2 = Add("AESV2");
        public static readonly PdfName AESV3 = Add("AESV3");
        public static readonly PdfName AHx = Add("AHx");
        public static readonly PdfName AIS = Add("AIS");
        public static readonly PdfName All = Add("All");
        public static readonly PdfName AllOff = Add("AllOff");
        public static readonly PdfName AllOn = Add("AllOn");
        public static readonly PdfName AllPages = Add("AllPages");
        public static readonly PdfName Alternate = Add("Alternate");
        public static readonly PdfName AN = Add("AN");
        public static readonly PdfName And = Add("And");
        public static readonly PdfName Annot = Add("Annot");
        public static readonly PdfName Annotation = Add("Annotation");
        public static readonly PdfName Annots = Add("Annots");
        public static readonly PdfName AnyOff = Add("AnyOff");
        public static readonly PdfName AnyOn = Add("AnyOn");
        public static readonly PdfName AntiAlias = Add("AntiAlias");
        public static readonly PdfName AP = Add("AP");
        public static readonly PdfName App = Add("App");
        public static readonly PdfName AppData = Add("AppData");
        public static readonly PdfName AppDefault = Add("AppDefault");
        public static readonly PdfName Approved = Add("Approved");
        public static readonly PdfName ArtBox = Add("ArtBox");
        public static readonly PdfName AS = Add("AS");
        public static readonly PdfName Ascent = Add("Ascent");
        public static readonly PdfName ASCII85Decode = Add("ASCII85Decode");
        public static readonly PdfName ASCIIHexDecode = Add("ASCIIHexDecode");
        public static readonly PdfName AsIs = Add("AsIs");
        public static readonly PdfName Author = Add("Author");
        public static readonly PdfName AvgWidth = Add("AvgWidth");
        public static readonly PdfName B = Add("B");
        public static readonly PdfName Background = Add("Background");
        public static readonly PdfName BaseEncoding = Add("BaseEncoding");
        public static readonly PdfName BaseFont = Add("BaseFont");
        public static readonly PdfName BaseState = Add("BaseState");
        public static readonly PdfName BBox = Add("BBox");
        public static readonly PdfName BC = Add("BC");
        public static readonly PdfName BE = Add("BE");
        public static readonly PdfName Bead = Add("Bead");
        public static readonly PdfName BG = Add("BG");
        public static readonly PdfName BG2 = Add("BG2");
        public static readonly PdfName BitsPerComponent = Add("BitsPerComponent");
        public static readonly PdfName BitsPerCoordinate = Add("BitsPerCoordinate");
        public static readonly PdfName BitsPerFlag = Add("BitsPerFlag");
        public static readonly PdfName BitsPerSample = Add("BitsPerSample");
        public static readonly PdfName Bl = Add("Bl");
        public static readonly PdfName BlackPoint = Add("BlackPoint");
        public static readonly PdfName BlackIs1 = Add("BlackIs1");
        public static readonly PdfName BleedBox = Add("BleedBox");
        public static readonly PdfName Blinds = Add("Blinds");
        public static readonly PdfName BM = Add("BM");
        public static readonly PdfName Border = Add("Border");
        public static readonly PdfName Bounds = Add("Bounds");
        public static readonly PdfName Box = Add("Box");
        public static readonly PdfName BPC = Add("BPC");
        public static readonly PdfName BS = Add("BS");
        public static readonly PdfName Btn = Add("Btn");
        public static readonly PdfName BU = Add("BU");
        public static readonly PdfName Butt = Add("Butt");
        public static readonly PdfName ByteRange = Add("ByteRange");
        public static readonly PdfName C = Add("C");
        public static readonly PdfName C0 = Add("C0");
        public static readonly PdfName C1 = Add("C1");
        public static readonly PdfName CA = Add("CA");
        public static readonly PdfName ca = Add("ca");
        public static readonly PdfName CalGray = Add("CalGray");
        public static readonly PdfName CalRGB = Add("CalRGB");
        public static readonly PdfName Cancelled = Add("Cancelled");
        public static readonly PdfName Cap = Add("Cap");
        public static readonly PdfName CapHeight = Add("CapHeight");
        public static readonly PdfName Caret = Add("Caret");
        public static readonly PdfName Catalog = Add("Catalog");
        public static readonly PdfName Category = Add("Category");
        public static readonly PdfName CCF = Add("CCF");
        public static readonly PdfName CCITTFaxDecode = Add("CCITTFaxDecode");
        public static readonly PdfName CenterWindow = Add("CenterWindow");
        public static readonly PdfName Cert = Add("Cert");
        public static readonly PdfName CF = Add("CF");
        public static readonly PdfName CFM = Add("CFM");
        public static readonly PdfName CMYK = Add("CMYK");
        public static readonly PdfName Ch = Add("Ch");
        public static readonly PdfName CharSet = Add("CharSet");
        public static readonly PdfName CIDFontType0 = Add("CIDFontType0");
        public static readonly PdfName CIDFontType2 = Add("CIDFontType2");
        public static readonly PdfName CIDSet = Add("CIDSet");
        public static readonly PdfName CIDSystemInfo = Add("CIDSystemInfo");
        public static readonly PdfName CIDToGIDMap = Add("CIDToGIDMap");
        public static readonly PdfName Circle = Add("Circle");
        public static readonly PdfName CL = Add("CL");
        public static readonly PdfName ClosedArrow = Add("ClosedArrow");
        public static readonly PdfName CMap = Add("CMap");
        public static readonly PdfName CMapName = Add("CMapName");
        public static readonly PdfName CMapType = Add("CMapType");
        public static readonly PdfName CO = Add("CO");
        public static readonly PdfName Color = Add("Color");
        public static readonly PdfName ColorBurn = Add("ColorBurn");
        public static readonly PdfName ColorDodge = Add("ColorDodge");
        public static readonly PdfName Colors = Add("Colors");
        public static readonly PdfName ColorSpace = Add("ColorSpace");
        public static readonly PdfName ColorSpaces = Add("ColorSpaces");
        public static readonly PdfName ColorTransform = Add("ColorTransform");
        public static readonly PdfName Columns = Add("Columns");
        public static readonly PdfName Compatible = Add("Compatible");
        public static readonly PdfName Completed = Add("Completed");
        public static readonly PdfName Comment = Add("Comment");
        public static readonly PdfName Confidential = Add("Confidential");
        public static readonly PdfName Config = Add("Config");
        public static readonly PdfName Configs = Add("Configs");
        public static readonly PdfName Contents = Add("Contents");
        public static readonly PdfName Coords = Add("Coords");
        public static readonly PdfName Count = Add("Count");
        public static readonly PdfName Cover = Add("Cover");
        public static readonly PdfName CP = Add("CP");
        public static readonly PdfName Changes = Add("Changes");
        public static readonly PdfName CharProc = Add("CharProc");
        public static readonly PdfName CharProcs = Add("CharProcs");
        public static readonly PdfName CreationDate = Add("CreationDate");
        public static readonly PdfName Creator = Add("Creator");
        public static readonly PdfName CreatorInfo = Add("CreatorInfo");
        public static readonly PdfName CropBox = Add("CropBox");
        public static readonly PdfName Crypt = Add("Crypt");
        public static readonly PdfName CryptFilter = Add("CryptFilter");
        public static readonly PdfName CS = Add("CS");
        public static readonly PdfName CT = Add("CT");
        public static readonly PdfName D = Add("D");
        public static readonly PdfName DA = Add("DA");
        public static readonly PdfName Date = Add("Date");
        public static readonly PdfName Darken = Add("Darken");
        public static readonly PdfName DC = Add("DC");
        public static readonly PdfName DCT = Add("DCT");
        public static readonly PdfName DCTDecode = Add("DCTDecode");
        public static readonly PdfName Decode = Add("Decode");
        public static readonly PdfName DecodeParms = Add("DecodeParms");
        public static readonly PdfName Default = Add("Default");
        public static readonly PdfName DefaultCryptFilter = Add("DefaultCryptFilter");
        public static readonly PdfName Departmental = Add("Departmental");
        public static readonly PdfName Desc = Add("Desc");
        public static readonly PdfName DescendantFonts = Add("DescendantFonts");
        public static readonly PdfName Descent = Add("Descent");
        public static readonly PdfName Design = Add("Design");
        public static readonly PdfName Dest = Add("Dest");
        public static readonly PdfName Dests = Add("Dests");
        public static readonly PdfName DeviceCMYK = Add("DeviceCMYK");
        public static readonly PdfName DeviceGray = Add("DeviceGray");
        public static readonly PdfName DeviceRGB = Add("DeviceRGB");
        public static readonly PdfName DeviceN = Add("DeviceN");
        public static readonly PdfName Di = Add("Di");
        public static readonly PdfName Diamond = Add("Diamond");
        public static readonly PdfName Difference = Add("Difference");
        public static readonly PdfName Differences = Add("Differences");
        public static readonly PdfName DigestMethod = Add("DigestMethod");
        public static readonly PdfName Direction = Add("Direction");
        public static readonly PdfName DisplayDocTitle = Add("DisplayDocTitle");
        public static readonly PdfName Dissolve = Add("Dissolve");
        public static readonly PdfName Dm = Add("Dm");
        public static readonly PdfName DocMDP = Add("DocMDP");
        public static readonly PdfName DocTimeStamp = Add("DocTimeStamp");
        public static readonly PdfName Domain = Add("Domain");
        public static readonly PdfName DOS = Add("DOS");
        public static readonly PdfName DP = Add("DP");
        public static readonly PdfName DR = Add("DR");
        public static readonly PdfName Draft = Add("Draft");
        public static readonly PdfName DS = Add("DS");
        public static readonly PdfName DSS = Add("DSS");
        public static readonly PdfName Duplex = Add("Duplex");
        public static readonly PdfName DuplexFlipLongEdge = Add("DuplexFlipLongEdge");
        public static readonly PdfName DuplexFlipShortEdge = Add("DuplexFlipShortEdge");
        public static readonly PdfName Dur = Add("Dur");
        public static readonly PdfName DV = Add("DV");
        public static readonly PdfName DW = Add("DW");
        public static readonly PdfName DW2 = Add("DW2");
        public static readonly PdfName E = Add("E");
        public static readonly PdfName EarlyChange = Add("EarlyChange");
        public static readonly PdfName EF = Add("EF");
        public static readonly PdfName EmbeddedFile = Add("EmbeddedFile");
        public static readonly PdfName EmbeddedFiles = Add("EmbeddedFiles");
        public static readonly PdfName Encode = Add("Encode");
        public static readonly PdfName EncodedByteAlign = Add("EncodedByteAlign");
        public static readonly PdfName Encoding = Add("Encoding");
        public static readonly PdfName EndOfBlock = Add("EndOfBlock");
        public static readonly PdfName EndOfLine = Add("EndOfLine");
        public static readonly PdfName Encrypt = Add("Encrypt");
        public static readonly PdfName EncryptMetadata = Add("EncryptMetadata");
        public static readonly PdfName Event = Add("Event");
        public static readonly PdfName Exclusion = Add("Exclusion");
        public static readonly PdfName Experimental = Add("Experimental");
        public static readonly PdfName Expired = Add("Expired");
        public static readonly PdfName Export = Add("Export");
        public static readonly PdfName ExportState = Add("ExportState");
        public static readonly PdfName Extend = Add("Extend");
        public static readonly PdfName Extends = Add("Extends");
        public static readonly PdfName ExtGState = Add("ExtGState");
        public static readonly PdfName ExtGStates = Add("ExtGStates");
        public static readonly PdfName F = Add("F");
        public static readonly PdfName Fade = Add("Fade");
        public static readonly PdfName FB = Add("FB");
        public static readonly PdfName FD = Add("FD");
        public static readonly PdfName FDecodeParms = Add("FDecodeParms");
        public static readonly PdfName Ff = Add("Ff");
        public static readonly PdfName FFilter = Add("FFilter");
        public static readonly PdfName FG = Add("FG");
        public static readonly PdfName Fields = Add("Fields");
        public static readonly PdfName FileAttachment = Add("FileAttachment");
        public static readonly PdfName Filespec = Add("Filespec");
        public static readonly PdfName Filter = Add("Filter");
        public static readonly PdfName Final = Add("Final");
        public static readonly PdfName First = Add("First");
        public static readonly PdfName FirstChar = Add("FirstChar");
        public static readonly PdfName FirstPage = Add("FirstPage");
        public static readonly PdfName Fit = Add("Fit");
        public static readonly PdfName FitB = Add("FitB");
        public static readonly PdfName FitBH = Add("FitBH");
        public static readonly PdfName FitBV = Add("FitBV");
        public static readonly PdfName FitH = Add("FitH");
        public static readonly PdfName FitR = Add("FitR");
        public static readonly PdfName FitV = Add("FitV");
        public static readonly PdfName FitWindow = Add("FitWindow");
        public static readonly PdfName Fl = Add("Fl");
        public static readonly PdfName Flags = Add("Flags");
        public static readonly PdfName FlateDecode = Add("FlateDecode");
        public static readonly PdfName Fly = Add("Fly");
        public static readonly PdfName Fo = Add("Fo");
        public static readonly PdfName Font = Add("Font");
        public static readonly PdfName Fonts = Add("Fonts");
        public static readonly PdfName FontBBox = Add("FontBBox");
        public static readonly PdfName FontDescriptor = Add("FontDescriptor");
        public static readonly PdfName FontFile = Add("FontFile");
        public static readonly PdfName FontFile2 = Add("FontFile2");
        public static readonly PdfName FontFile3 = Add("FontFile3");
        public static readonly PdfName FontMatrix = Add("FontMatrix");
        public static readonly PdfName FontName = Add("FontName");
        public static readonly PdfName FontFamily = Add("FontFamily");
        public static readonly PdfName FontStretch = Add("FontStretch");
        public static readonly PdfName ForComment = Add("ForComment");
        public static readonly PdfName Form = Add("Form");
        public static readonly PdfName FormType = Add("FormType");
        public static readonly PdfName ForPublicRelease = Add("ForPublicRelease");
        public static readonly PdfName FreeText = Add("FreeText");
        public static readonly PdfName FS = Add("FS");
        public static readonly PdfName FT = Add("FT");
        public static readonly PdfName FreeTextCallout = Add("FreeTextCallout");
        public static readonly PdfName FreeTextTypeWriter = Add("FreeTextTypeWriter");
        public static readonly PdfName FullScreen = Add("FullScreen");
        public static readonly PdfName Function = Add("Function");
        public static readonly PdfName Functions = Add("Functions");
        public static readonly PdfName FunctionType = Add("FunctionType");
        public static readonly PdfName FWParams = Add("FWParams");
        public static readonly PdfName G = Add("G");
        public static readonly PdfName Gamma = Add("Gamma");
        public static readonly PdfName Glitter = Add("Glitter");
        public static readonly PdfName GoTo = Add("GoTo");
        public static readonly PdfName GoTo3DView = Add("GoTo3DView");
        public static readonly PdfName GoToAction = Add("GoToAction");
        public static readonly PdfName GoToE = Add("GoToE");
        public static readonly PdfName GoToR = Add("GoToR");
        public static readonly PdfName Graph = Add("Graph");
        public static readonly PdfName Group = Add("Group");
        public static readonly PdfName H = Add("H");
        public static readonly PdfName HardLight = Add("HardLight");
        public static readonly PdfName Height = Add("Height");
        public static readonly PdfName Help = Add("Help");
        public static readonly PdfName HF = Add("HF");
        public static readonly PdfName HI = Add("HI");
        public static readonly PdfName Hide = Add("Hide");
        public static readonly PdfName HideMenubar = Add("HideMenubar");
        public static readonly PdfName HideToolbar = Add("HideToolbar");
        public static readonly PdfName HideWindowUI = Add("HideWindowUI");
        public static readonly PdfName Highlight = Add("Highlight");
        public static readonly PdfName highlight = Add("highlight");
        public static readonly PdfName Hue = Add("Hue");
        public static readonly PdfName I = Add("I");
        public static readonly PdfName IC = Add("IC");
        public static readonly PdfName ICCBased = Add("ICCBased");
        public static readonly PdfName ID = Add("ID");
        public static readonly PdfName Identity = Add("Identity");
        public static readonly PdfName IdentityH = Add("Identity-H");
        public static readonly PdfName IdentityV = Add("Identity-V");
        public static readonly PdfName IF = Add("IF");
        public static readonly PdfName IM = Add("IM");
        public static readonly PdfName Image = Add("Image");
        public static readonly PdfName ImageMask = Add("ImageMask");
        public static readonly PdfName ImportData = Add("ImportData");
        public static readonly PdfName Ind = Add("Ind");
        public static readonly PdfName Index = Add("Index");
        public static readonly PdfName Indexed = Add("Indexed");
        public static readonly PdfName Info = Add("Info");
        public static readonly PdfName Inline = Add("Inline");
        public static readonly PdfName Ink = Add("Ink");
        public static readonly PdfName InkList = Add("InkList");
        public static readonly PdfName Insert = Add("Insert");
        public static readonly PdfName Interpolate = Add("Interpolate");
        public static readonly PdfName Intent = Add("Intent");
        public static readonly PdfName IRT = Add("IRT");
        public static readonly PdfName IT = Add("IT");
        public static readonly PdfName ItalicAngle = Add("ItalicAngle");
        public static readonly PdfName IX = Add("IX");
        public static readonly PdfName JavaScript = Add("JavaScript");
        public static readonly PdfName JBIG2Decode = Add("JBIG2Decode");
        public static readonly PdfName JBIG2Globals = Add("JBIG2Globals");
        public static readonly PdfName JPXDecode = Add("JPXDecode");
        public static readonly PdfName JS = Add("JS");
        public static readonly PdfName K = Add("K");
        public static readonly PdfName Key = Add("Key");
        public static readonly PdfName Keywords = Add("Keywords");
        public static readonly PdfName Kids = Add("Kids");
        public static readonly PdfName L = Add("L");
        public static readonly PdfName L2R = Add("L2R");
        public static readonly PdfName Lab = Add("Lab");
        public static readonly PdfName Lang = Add("Lang");
        public static readonly PdfName Language = Add("Language");
        public static readonly PdfName Last = Add("Last");
        public static readonly PdfName LastChar = Add("LastChar");
        public static readonly PdfName LastModified = Add("LastModified");
        public static readonly PdfName LastPage = Add("LastPage");
        public static readonly PdfName Launch = Add("Launch");
        public static readonly PdfName LC = Add("LC");
        public static readonly PdfName LE = Add("LE");
        public static readonly PdfName Leading = Add("Leading");
        public static readonly PdfName Length = Add("Length");
        public static readonly PdfName Length1 = Add("Length1");
        public static readonly PdfName Length2 = Add("Length2");
        public static readonly PdfName Length3 = Add("Length3");
        public static readonly PdfName LI = Add("LI");
        public static readonly PdfName Lighten = Add("Lighten");
        public static readonly PdfName Limits = Add("Limits");
        public static readonly PdfName Line = Add("Line");
        public static readonly PdfName LineArrow = Add("LineArrow");
        public static readonly PdfName Linearized = Add("Linearized");
        public static readonly PdfName LineDimension = Add("LineDimension");
        public static readonly PdfName Link = Add("Link");
        public static readonly PdfName ListMode = Add("ListMode");
        public static readonly PdfName LJ = Add("LJ");
        public static readonly PdfName LL = Add("LL");
        public static readonly PdfName LLE = Add("LLE");
        public static readonly PdfName LLO = Add("LLO");
        public static readonly PdfName Location = Add("Location");
        public static readonly PdfName Locked = Add("Locked");
        public static readonly PdfName Luminosity = Add("Luminosity");
        public static readonly PdfName LW = Add("LW");
        public static readonly PdfName LZW = Add("LZW");
        public static readonly PdfName LZWDecode = Add("LZWDecode");
        public static readonly PdfName M = Add("M");
        public static readonly PdfName Mac = Add("Mac");
        public static readonly PdfName MacRomanEncoding = Add("MacRomanEncoding");
        public static readonly PdfName MacExpertEncoding = Add("MacExpertEncoding");
        public static readonly PdfName Matrix = Add("Matrix");
        public static readonly PdfName Matte = Add("Matte");
        public static readonly PdfName max = Add("max");
        public static readonly PdfName MaxLen = Add("MaxLen");
        public static readonly PdfName MaxWidth = Add("MaxWidth");
        public static readonly PdfName MCD = Add("MCD");
        public static readonly PdfName MCID = Add("MCID");
        public static readonly PdfName MCS = Add("MCS");
        public static readonly PdfName MediaBox = Add("MediaBox");
        public static readonly PdfName MediaClip = Add("MediaClip");
        public static readonly PdfName MediaDuration = Add("MediaDuration");
        public static readonly PdfName MediaOffset = Add("MediaOffset");
        public static readonly PdfName MediaPlayerInfo = Add("MediaPlayerInfo");
        public static readonly PdfName MediaPlayParams = Add("MediaPlayParams");
        public static readonly PdfName MediaScreenParams = Add("MediaScreenParams");
        public static readonly PdfName Marked = Add("Marked");
        public static readonly PdfName Mask = Add("Mask");
        public static readonly PdfName Metadata = Add("Metadata");
        public static readonly PdfName MH = Add("MH");
        public static readonly PdfName Mic = Add("Mic");
        public static readonly PdfName min = Add("min");
        public static readonly PdfName MissingWidth = Add("MissingWidth");
        public static readonly PdfName MK = Add("MK");
        public static readonly PdfName ML = Add("ML");
        public static readonly PdfName MMType1 = Add("MMType1");
        public static readonly PdfName ModDate = Add("ModDate");
        public static readonly PdfName Movie = Add("Movie");
        public static readonly PdfName MR = Add("MR");
        public static readonly PdfName MU = Add("MU");
        public static readonly PdfName Multiply = Add("Multiply");
        public static readonly PdfName N = Add("N");
        public static readonly PdfName Name = Add("Name");
        public static readonly PdfName Named = Add("Named");
        public static readonly PdfName Names = Add("Names");
        public static readonly PdfName NewParagraph = Add("NewParagraph");
        public static readonly PdfName NewWindow = Add("NewWindow");
        public static readonly PdfName Next = Add("Next");
        public static readonly PdfName NextPage = Add("NextPage");
        public static readonly PdfName NM = Add("NM");
        public static readonly PdfName None = Add("None");
        public static readonly PdfName NonEFontNoWarn = Add("NonEFontNoWarn");
        public static readonly PdfName NonFullScreenPageMode = Add("NonFullScreenPageMode");
        public static readonly PdfName Normal = Add("Normal");
        public static readonly PdfName Not = Add("Not");
        public static readonly PdfName NotApproved = Add("NotApproved");
        public static readonly PdfName Note = Add("Note");
        public static readonly PdfName NotForPublicRelease = Add("NotForPublicRelease");
        public static readonly PdfName NU = Add("NU");
        public static readonly PdfName NumCopies = Add("NumCopies");
        public static readonly PdfName Nums = Add("Nums");
        public static readonly PdfName O = Add("O");
        public static readonly PdfName ObjStm = Add("ObjStm");
        public static readonly PdfName OC = Add("OC");
        public static readonly PdfName OE = Add("OE");
        public static readonly PdfName OCG = Add("OCG");
        public static readonly PdfName OCGs = Add("OCGs");
        public static readonly PdfName OCMD = Add("OCMD");
        public static readonly PdfName OCProperties = Add("OCProperties");
        public static readonly PdfName OFF = Add("OFF");
        public static readonly PdfName Off = Add("Off");
        public static readonly PdfName ON = Add("ON");
        public static readonly PdfName OneColumn = Add("OneColumn");
        public static readonly PdfName OP = Add("OP");
        public static readonly PdfName op = Add("op");
        public static readonly PdfName Open = Add("Open");
        public static readonly PdfName OpenAction = Add("OpenAction");
        public static readonly PdfName OpenArrow = Add("OpenArrow");
        public static readonly PdfName OpenType = Add("OpenType");
        public static readonly PdfName OPM = Add("OPM");
        public static readonly PdfName Opt = Add("Opt");
        public static readonly PdfName Or = Add("Or");
        public static readonly PdfName Order = Add("Order");
        public static readonly PdfName Ordering = Add("Ordering");
        public static readonly PdfName Org = Add("Org");
        public static readonly PdfName OS = Add("OS");
        public static readonly PdfName Outline = Add("Outline");
        public static readonly PdfName Outlines = Add("Outlines");
        public static readonly PdfName Overlay = Add("Overlay");
        public static readonly PdfName P = Add("P");
        public static readonly PdfName Panose = Add("Panose");
        public static readonly PdfName Page = Add("Page");
        public static readonly PdfName PageElement = Add("PageElement");
        public static readonly PdfName PageLabel = Add("PageLabel");
        public static readonly PdfName PageLabels = Add("PageLabels");
        public static readonly PdfName PageLayout = Add("PageLayout");
        public static readonly PdfName PageMode = Add("PageMode");
        public static readonly PdfName Pages = Add("Pages");
        public static readonly PdfName PaintType = Add("PaintType");
        public static readonly PdfName Paperclip = Add("Paperclip");
        public static readonly PdfName Paragraph = Add("Paragraph");
        public static readonly PdfName Params = Add("Params");
        public static readonly PdfName Parent = Add("Parent");
        public static readonly PdfName Pattern = Add("Pattern");
        public static readonly PdfName Patterns = Add("Patterns");
        public static readonly PdfName PatternType = Add("PatternType");
        public static readonly PdfName PC = Add("PC");
        public static readonly PdfName PDFDocEncoding = Add("PdfDocEncoding");
        public static readonly PdfName Perms = Add("Perms");
        public static readonly PdfName PI = Add("PI");
        public static readonly PdfName PickTrayByPDFSize = Add("PickTrayByPDFSize");
        public static readonly PdfName PID = Add("PID");
        public static readonly PdfName PieceInfo = Add("PieceInfo");
        public static readonly PdfName PL = Add("PL");
        public static readonly PdfName PO = Add("PO");
        public static readonly PdfName Polygon = Add("Polygon");
        public static readonly PdfName PolygonCloud = Add("PolygonCloud");
        public static readonly PdfName PolygonDimension = Add("PolygonDimension");
        public static readonly PdfName PolyLine = Add("PolyLine");
        public static readonly PdfName PolyLineDimension = Add("PolyLineDimension");
        public static readonly PdfName Popup = Add("Popup");
        public static readonly PdfName Predictor = Add("Predictor");
        public static readonly PdfName Preferred = Add("Preferred");
        public static readonly PdfName PreRelease = Add("PreRelease");
        public static readonly PdfName Prev = Add("Prev");
        public static readonly PdfName PrevPage = Add("PrevPage");
        public static readonly PdfName Print = Add("Print");
        public static readonly PdfName PrintPageRange = Add("PrintPageRange");
        public static readonly PdfName PrintScaling = Add("PrintScaling");
        public static readonly PdfName PrintState = Add("PrintState");
        public static readonly PdfName Private = Add("Private");
        public static readonly PdfName Producer = Add("Producer");
        public static readonly PdfName Prop_Build = Add("Prop_Build");
        public static readonly PdfName Properties = Add("Properties");
        public static readonly PdfName PubSec = Add("PubSec");
        public static readonly PdfName Push = Add("Push");
        public static readonly PdfName PushPin = Add("PushPin");
        public static readonly PdfName PV = Add("PV");
        public static readonly PdfName Q = Add("Q");
        public static readonly PdfName QuadPoints = Add("QuadPoints");
        public static readonly PdfName R = Add("R");
        public static readonly PdfName r = Add("r");
        public static readonly PdfName R2L = Add("R2L");
        public static readonly PdfName Range = Add("Range");
        public static readonly PdfName Rejected = Add("Rejected");
        public static readonly PdfName RBGroups = Add("RBGroups");
        public static readonly PdfName RC = Add("RC");
        public static readonly PdfName RClosedArrow = Add("RClosedArrow");
        public static readonly PdfName RD = Add("RD");
        public static readonly PdfName Reason = Add("Reason");
        public static readonly PdfName Recipients = Add("Recipients");
        public static readonly PdfName Rect = Add("Rect");
        public static readonly PdfName Reference = Add("Reference");
        public static readonly PdfName Review = Add("Review");
        public static readonly PdfName Registry = Add("Registry");
        public static readonly PdfName Rendition = Add("Rendition");
        public static readonly PdfName Renditions = Add("Renditions");
        public static readonly PdfName ResetForm = Add("ResetForm");
        public static readonly PdfName Resources = Add("Resources");
        public static readonly PdfName REx = Add("REx");
        public static readonly PdfName RF = Add("RF");
        public static readonly PdfName RGB = Add("RGB");
        public static readonly PdfName RI = Add("RI");
        public static readonly PdfName RL = Add("RL");
        public static readonly PdfName Root = Add("Root");
        public static readonly PdfName ROpenArrow = Add("ROpenArrow");
        public static readonly PdfName Rotate = Add("Rotate");
        public static readonly PdfName Rows = Add("Rows");
        public static readonly PdfName RT = Add("RT");
        public static readonly PdfName RunLengthDecode = Add("RunLengthDecode");
        public static readonly PdfName S = Add("S");
        public static readonly PdfName SA = Add("SA");
        public static readonly PdfName Saturation = Add("Saturation");
        public static readonly PdfName SBApproved = Add("SBApproved");
        public static readonly PdfName SBCompleted = Add("SBCompleted");
        public static readonly PdfName SBConfidential = Add("SBConfidential");
        public static readonly PdfName SBDraft = Add("SBDraft");
        public static readonly PdfName SBFinal = Add("SBFinal");
        public static readonly PdfName SBForComment = Add("SBForComment");
        public static readonly PdfName SBForPublicRelease = Add("SBForPublicRelease");
        public static readonly PdfName SBInformationOnly = Add("SBInformationOnly");
        public static readonly PdfName SBNotApproved = Add("SBNotApproved");
        public static readonly PdfName SBNotForPublicRelease = Add("SBNotForPublicRelease");
        public static readonly PdfName SBPreliminaryResults = Add("SBPreliminaryResults");
        public static readonly PdfName SBRejected = Add("SBRejected");
        public static readonly PdfName SBVoid = Add("SBVoid");
        public static readonly PdfName Screen = Add("Screen");
        public static readonly PdfName Separation = Add("Separation");
        public static readonly PdfName SetOCGState = Add("SetOCGState");
        public static readonly PdfName SHAccepted = Add("SHAccepted");
        public static readonly PdfName Shading = Add("Shading");
        public static readonly PdfName Shadings = Add("Shadings");
        public static readonly PdfName ShadingType = Add("ShadingType");
        public static readonly PdfName SHInitialHere = Add("SHInitialHere");
        public static readonly PdfName SHSignHere = Add("SHSignHere");
        public static readonly PdfName SHWitness = Add("SHWitness");
        public static readonly PdfName Sig = Add("Sig");
        public static readonly PdfName SigRef = Add("SigRef");
        public static readonly PdfName Simplex = Add("Simplex");
        public static readonly PdfName SinglePage = Add("SinglePage");
        public static readonly PdfName Size = Add("Size");
        public static readonly PdfName Slash = Add("Slash");
        public static readonly PdfName SMask = Add("SMask");
        public static readonly PdfName SoftLight = Add("SoftLight");
        public static readonly PdfName SoftwareIdentifier = Add("SoftwareIdentifier");
        public static readonly PdfName Sold = Add("Sold");
        public static readonly PdfName Sound = Add("Sound");
        public static readonly PdfName SP = Add("SP");
        public static readonly PdfName Speaker = Add("Speaker");
        public static readonly PdfName Split = Add("Split");
        public static readonly PdfName Square = Add("Square");
        public static readonly PdfName Squiggly = Add("Squiggly");
        public static readonly PdfName SR = Add("SR");
        public static readonly PdfName SS = Add("SS");
        public static readonly PdfName St = Add("St");
        public static readonly PdfName Stamp = Add("Stamp");
        public static readonly PdfName StandardEncoding = Add("StandardEncoding");
        public static readonly PdfName State = Add("State");
        public static readonly PdfName StateModel = Add("StateModel");
        public static readonly PdfName StdCF = Add("StdCF");
        public static readonly PdfName StemV = Add("StemV");
        public static readonly PdfName StemH = Add("StemH");
        public static readonly PdfName StrikeOut = Add("StrikeOut");
        public static readonly PdfName StructParent = Add("StructParent");
        public static readonly PdfName StrF = Add("StrF");
        public static readonly PdfName StmF = Add("StmF");
        public static readonly PdfName Style = Add("Style");
        public static readonly PdfName SubFilter = Add("SubFilter");
        public static readonly PdfName Subj = Add("Subj");
        public static readonly PdfName Subject = Add("Subject");
        public static readonly PdfName SubmitForm = Add("SubmitForm");
        public static readonly PdfName Subtype = Add("Subtype");
        public static readonly PdfName Supplement = Add("Supplement");
        public static readonly PdfName SW = Add("SW");
        public static readonly PdfName Sy = Add("Sy");
        public static readonly PdfName Symbol = Add("Symbol");
        public static readonly PdfName T = Add("T");
        public static readonly PdfName Tabs = Add("Tabs");
        public static readonly PdfName Tag = Add("Tag");
        public static readonly PdfName Text = Add("Text");
        public static readonly PdfName TF = Add("TF");
        public static readonly PdfName Thread = Add("Thread");
        public static readonly PdfName Threads = Add("Threads");
        public static readonly PdfName TilingType = Add("TilingType");
        public static readonly PdfName Timespan = Add("Timespan");
        public static readonly PdfName Title = Add("Title");
        public static readonly PdfName TK = Add("TK");
        public static readonly PdfName Toggle = Add("Toggle");
        public static readonly PdfName Top = Add("Top");
        public static readonly PdfName TopSecret = Add("TopSecret");
        public static readonly PdfName ToUnicode = Add("ToUnicode");
        public static readonly PdfName TP = Add("TP");
        public static readonly PdfName TR = Add("TR");
        public static readonly PdfName Trans = Add("Trans");
        public static readonly PdfName Trailer = Add("Trailer");
        public static readonly PdfName TransformMethod = Add("TransformMethod");
        public static readonly PdfName TransformParams = Add("TransformParams");
        public static readonly PdfName Transparency = Add("Transparency");
        public static readonly PdfName TrimBox = Add("TrimBox");
        public static readonly PdfName TrueType = Add("TrueType");
        public static readonly PdfName TrustedMode = Add("TrustedMode");
        public static readonly PdfName Ttl = Add("Ttl");
        public static readonly PdfName TwoColumnLeft = Add("TwoColumnLeft");
        public static readonly PdfName TwoColumnRight = Add("TwoColumnRight");
        public static readonly PdfName TwoPageLeft = Add("TwoPageLeft");
        public static readonly PdfName TwoPageRight = Add("TwoPageRight");
        public static readonly PdfName Tx = Add("Tx");
        public static readonly PdfName Type = Add("Type");
        public static readonly PdfName Type0 = Add("Type0");
        public static readonly PdfName Type1 = Add("Type1");
        public static readonly PdfName Type1C = Add("Type1C");
        public static readonly PdfName Type3 = Add("Type3");
        public static readonly PdfName U = Add("U");
        public static readonly PdfName UC = Add("UC");
        public static readonly PdfName UE = Add("UE");
        public static readonly PdfName Unchanged = Add("Unchanged");
        public static readonly PdfName Uncover = Add("Uncover");
        public static readonly PdfName Underline = Add("Underline");
        public static readonly PdfName Unix = Add("Unix");
        public static readonly PdfName Unmarked = Add("Unix");
        public static readonly PdfName URI = Add("URI");
        public static readonly PdfName URL = Add("URL");
        public static readonly PdfName Usage = Add("Usage");
        public static readonly PdfName UseAttachments = Add("UseAttachments");
        public static readonly PdfName UseCMap = Add("UseCMap");
        public static readonly PdfName UseNone = Add("UseNone");
        public static readonly PdfName UseOC = Add("UseOC");
        public static readonly PdfName UseOutlines = Add("UseOutlines");
        public static readonly PdfName User = Add("User");
        public static readonly PdfName UseThumbs = Add("UseThumbs");
        public static readonly PdfName V = Add("V");
        public static readonly PdfName VE = Add("VE");
        public static readonly PdfName Version = Add("Version");
        public static readonly PdfName Vertices = Add("Vertices");
        public static readonly PdfName VerticesPerRow = Add("VerticesPerRow");
        public static readonly PdfName View = Add("View");
        public static readonly PdfName ViewerPreferences = Add("ViewerPreferences");
        public static readonly PdfName ViewState = Add("ViewState");
        public static readonly PdfName VisiblePages = Add("VisiblePages");
        public static readonly PdfName W = Add("W");
        public static readonly PdfName W2 = Add("W2");
        public static readonly PdfName FontWeight = Add("FontWeight");
        public static readonly PdfName WhitePoint = Add("WhitePoint");
        public static readonly PdfName Widget = Add("Widget");
        public static readonly PdfName Width = Add("Width");
        public static readonly PdfName Widths = Add("Widths");
        public static readonly PdfName Win = Add("Win");
        public static readonly PdfName WinAnsiEncoding = Add("WinAnsiEncoding");
        public static readonly PdfName Wipe = Add("Wipe");
        public static readonly PdfName WMode = Add("WMode");
        public static readonly PdfName WP = Add("WP");
        public static readonly PdfName WS = Add("WS");
        public static readonly PdfName X = Add("X");
        public static readonly PdfName XML = Add("XML");
        public static readonly PdfName XObject = Add("XObject");
        public static readonly PdfName XObjects = Add("XObjects");
        public static readonly PdfName XRef = Add("XRef");
        public static readonly PdfName XRefStm = Add("XRefStm");
        public static readonly PdfName XHeight = Add("XHeight");
        public static readonly PdfName XStep = Add("XStep");
        public static readonly PdfName XYZ = Add("XYZ");
        public static readonly PdfName Yes = Add("Yes");
        public static readonly PdfName YStep = Add("YStep");
        public static readonly PdfName Z = Add("Z");
        public static readonly PdfName ZapfDingbats = Add("ZapfDingbats");
        public static readonly PdfName Zoom = Add("Zoom");
#pragma warning restore 0108

        private string stringValue;

        private static PdfName Add(string value) => names[value] = new PdfName(value);

        /// <summary>Gets the object equivalent to the given value.</summary>
        public static PdfName Get(object value, bool escaped = false) => Get(value?.ToString(), escaped);

        /// <summary>Gets the object equivalent to the given value.</summary>
        public static PdfName Get(string value, bool escaped = false)
        {
            return value == null ? null : names.GetOrAdd(value, (v) => new PdfName(v, escaped));
        }

        private PdfName(string value)
        {
            stringValue = value;
            RawValue = value;
        }

        private PdfName(string value, bool escaped)
        {
            // NOTE: To avoid ambiguities due to the presence of '#' characters,
            // it's necessary to explicitly state when a name value has already been escaped.
            // This is tipically the case of names parsed from a previously-serialized PDF file.
            if (escaped)
            {
                RawValue = value;
            }
            else
            {
                if (!GetUnescapedPattern().IsMatch(value))
                {
                    stringValue = value;
                    RawValue = value;
                }
                else
                {
                    Value = value;
                }
            }
        }

        public string StringValue => stringValue ??= ToString();

        public override object Value
        {
            get => base.Value;
            protected set
            {
                //NOTE: Before being accepted, any character sequence identifying a name MUST be normalized
                //escaping reserved characters.
                stringValue = (string)value;
                var buffer = new StringBuilder();
                {
                    string stringValue = (string)value;
                    int index = 0;
                    Match unescapedMatch = GetUnescapedPattern().Match(stringValue);
                    while (unescapedMatch.Success)
                    {
                        int start = unescapedMatch.Index;
                        if (start > index)
                        {
                            buffer.Append(stringValue.AsSpan(index, start - index));
                        }

                        buffer.Append('#' + String.Format("{0:x}", (int)unescapedMatch.Groups[0].Value[0]));

                        index = start + unescapedMatch.Length;
                        unescapedMatch = unescapedMatch.NextMatch();
                    }
                    if (index < stringValue.Length)
                    {
                        buffer.Append(index == 0 ? stringValue : stringValue.Substring(index));
                    }
                }
                RawValue = buffer.ToString();
            }
        }

        public override PdfObject Accept(IVisitor visitor, PdfName parentKey, object data) => visitor.Visit(this, parentKey, data);

        public override int CompareTo(PdfDirectObject obj)
        {
            if (obj is not PdfName objName)
                throw new ArgumentException("Object MUST be a PdfName");

            return string.Compare(RawValue, objName.RawValue, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            if (obj is PdfName objName)
            {
                return Equals(objName);
            }
            else if (obj is IPdfString pdfString)
            {
                return Equals(pdfString);
            }
            else if (obj is string objString)
            {
                return Equals(objString);
            }

            return base.Equals(obj);
        }

        public bool Equals(IPdfString pdfString)
        {
            return string.Equals(StringValue, pdfString.StringValue, StringComparison.Ordinal);
        }

        public bool Equals(string objString)
        {
            return string.Equals(StringValue, objString, StringComparison.Ordinal);
        }

        public bool Equals(PdfName other)
        {
            return ReferenceEquals(this, other);// string.Equals(RawValue, other?.RawValue, StringComparison.Ordinal);
        }

        public override int GetHashCode() => base.GetHashCode();

        public override string ToString()
        {
            //NOTE: The textual representation of a name concerns unescaping reserved characters.
            string value = RawValue;

            Match escapedMatch = GetEscapedPattern().Match(value);
            if (escapedMatch.Success)
            {
                var buffer = new StringBuilder();
                int index = 0;
                while (escapedMatch.Success)
                {
                    int start = escapedMatch.Index;
                    if (start > index)
                    {
                        buffer.Append(value.AsSpan(index, start - index));
                    }

                    buffer.Append((char)Int32.Parse(escapedMatch.Groups[1].Value, NumberStyles.HexNumber));

                    index = start + escapedMatch.Length;
                    escapedMatch = escapedMatch.NextMatch();
                }
                if (index < value.Length)
                {
                    buffer.Append(value.AsSpan(index));
                }

                return buffer.ToString();
            }
            return value;
        }

        public override void WriteTo(IOutputStream stream, PdfDocument context)
        {
            stream.Write(NamePrefixChunk);
            stream.Write(RawValue);
        }

        
    }
}