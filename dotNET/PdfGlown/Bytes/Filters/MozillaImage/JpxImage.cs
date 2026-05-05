/* Copyright 2012 Mozilla Foundation
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*     http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace PdfGlown.Bytes.Filters.Jpx
{
    public class JpxError : Exception
    {
        public JpxError(string msg)
        : base($"JPX error: {msg}")
        { }
    }

    public class JpxImage
    {
        private const int DefaultHeadSize = 8;

        // Table E.1
        Dictionary<string, int> SubbandsGainLog2 = new(StringComparer.Ordinal) { { "LL", 0 }, { "LH", 1 }, { "HL", 1 }, { "HH", 2 } };
        bool failOnCorruptedImage;
        internal List<TileResultB> tiles;
        internal int width;
        internal int height;
        internal int componentsCount;
        internal int bitsPerComponent;
        // eslint-disable-next-line no-shadow
        public JpxImage()
        {
            failOnCorruptedImage = false;
        }

        void JpxImageClosure()
        { }

        public void Parse(IInputStream input)
        {
            var data = (ReadOnlySpan<byte>)input.AsSpan();
            var head = data.ReadUInt16(0);
            // No box header, immediate start of codestream (SOC)
            if (head == 0xff4f)
            {
                ParseCodestream(input, data.Length);
                return;
            }

            var position = 0;
            var length = data.Length;
            while (position < length)
            {
                var headerSize = DefaultHeadSize;
                long lbox = data.ReadUInt32(position);
                var tbox = data.ReadUInt32(position + 4);
                position += headerSize;
                if (lbox == 1)
                {
                    // XLBox: read UInt64 according to spec.
                    // JavaScript's int precision of 53 bit should be sufficient here.
                    lbox = data.ReadUInt32(position) * 4294967296 + data.ReadUInt32(position + 4);//(long)data.ReadUInt64();
                    position += DefaultHeadSize;
                    headerSize += DefaultHeadSize;
                }
                if (lbox == 0)
                {
                    lbox = length - position + headerSize;
                }
                if (lbox < headerSize)
                {
                    throw new JpxError("Invalid box field size");
                }
                var dataLength = lbox - headerSize;
                var jumpDataLength = true;
                switch (tbox)
                {
                    case 0x6a703268: // 'jp2h'                        
                        jumpDataLength = false; // parsing child boxes
                        break;
                    case 0x636f6c72: // 'colr'
                                     // Colorspaces are not used, the CS from the PDF is used.
                        var method = data[position];
                        if (method == 1)
                        {
                            // enumerated colorspace
                            var colorspace = data.ReadUInt32(position + 3);
                            switch (colorspace)
                            {
                                case 16: // this indicates a sRGB colorspace
                                case 17: // this indicates a grayscale colorspace
                                case 18: // this indicates a YUV colorspace
                                    break;
                                default:
                                    Debug.WriteLine("warn: Unknown colorspace " + colorspace);
                                    break;
                            }
                        }
                        else if (method == 2)
                        {
                            Debug.WriteLine("info: ICC profile not supported");
                        }
                        break;
                    case 0x6a703263: // 'jp2c'
                        input.Seek(position);
                        ParseCodestream(input, dataLength);
                        break;
                    case 0x6a502020: // 'jP\024\024'
                        if (data.ReadUInt32(position) != 0x0d0a870a)
                        {
                            Debug.WriteLine("warn: Invalid JP2 signature");
                        }
                        break;
                    // The following header types are valid but currently not used:
                    case 0x6a501a1a: // 'jP\032\032'
                    case 0x66747970: // 'ftyp'
                    case 0x72726571: // 'rreq'
                    case 0x72657320: // 'res '
                    case 0x69686472: // 'ihdr'
                        break;
                    default:
                        var headerType = new string(new char[] {
                            (char)((tbox >> 24) & 0xff),
                            (char)((tbox >> 16) & 0xff),
                            (char)((tbox >> 8) & 0xff),
                            (char)(tbox & 0xff)});
                        Debug.WriteLine($"warn: Unsupported header type {tbox} ({headerType})");
                        break;
                }
                if (jumpDataLength)
                {
                    position += (int)dataLength;
                }
            }
        }

        void ParseImageProperties(BinaryReader stream)
        {
            var newByte = stream.ReadByte();
            while (newByte >= 0)
            {
                var oldByte = newByte;
                newByte = stream.ReadByte();
                var code = (oldByte << 8) | newByte;
                // Image and tile size (SIZ)
                if (code == 0xff51)
                {
                    stream.BaseStream.Position += 4;
                    var Xsiz = ((uint)stream.ReadInt32()) >> 0; // Byte 4
                    var Ysiz = ((uint)stream.ReadInt32()) >> 0; // Byte 8
                    var XOsiz = ((uint)stream.ReadInt32()) >> 0; // Byte 12
                    var YOsiz = ((uint)stream.ReadInt32()) >> 0; // Byte 16
                    stream.BaseStream.Position += 16;
                    var Csiz = stream.ReadUInt16(); // Byte 36
                    width = (int)(Xsiz - XOsiz);
                    height = (int)(Ysiz - YOsiz);
                    componentsCount = Csiz;
                    // Results are always returned as "Uint8ClampedArray"s.
                    bitsPerComponent = 8;
                    return;
                }
            }
            throw new JpxError("No size marker found in JPX stream");
        }

        void ParseCodestream(IInputStream data, long streamLength)
        {
            var context = new Context();
            var doNotRecover = false;
            try
            {

                var end = data.Position + streamLength;
                while (data.Position + 1 < end)
                {
                    var code = data.ReadUInt16();
                    var position = (int)data.Position;

                    var length = 0;
                    var sqcd = 0;
                    var spqcds = (List<EpsilonMU>)null;
                    var spqcdSize = 0;
                    var scalarExpounded = false;
                    var tile = (Tile)null;
                    switch (code)
                    {
                        case 0xff4f: // Start of codestream (SOC)
                            context.MainHeader = true;
                            break;
                        case 0xffd9: // End of codestream (EOC)
                            break;
                        case 0xff51: // Image and tile size (SIZ)
                            length = data.ReadUInt16();
                            data.Skip(2);
                            var siz = new SIZ(
                                Xsiz: (int)data.ReadUInt32(),
                                Ysiz: (int)data.ReadUInt32(),
                                XOsiz: (int)data.ReadUInt32(),
                                YOsiz: (int)data.ReadUInt32(),
                                XTsiz: (int)data.ReadUInt32(),
                                YTsiz: (int)data.ReadUInt32(),
                                XTOsiz: (int)data.ReadUInt32(),
                                YTOsiz: (int)data.ReadUInt32());
                            var componentsCount = data.ReadUInt16();
                            siz.Csiz = componentsCount;
                            var components = new List<Component>(componentsCount);

                            for (var i = 0; i < componentsCount; i++)
                            {
                                var jbyte = data.ReadUByte();
                                Component component = new Component(
                                    precision: (jbyte & 0x7f) + 1,
                                    isSigned: 0 != (jbyte & 0x80),
                                    XRsiz: data.ReadUByte(),
                                    YRsiz: data.ReadUByte());

                                CalculateComponentDimensions(component, siz);
                                components.Add(component);
                            }
                            context.SIZ = siz;
                            context.Components = components;
                            CalculateTileGrids(context, components);
                            context.QCC = new Dictionary<int, Quantization>();
                            context.COC = new Dictionary<int, Cod>();
                            break;
                        case 0xff5c: // Quantization default (QCD)
                            length = data.ReadUInt16();
                            var qcd = new Quantization();
                            sqcd = data.ReadUByte();
                            switch (sqcd & 0x1f)
                            {
                                case 0:
                                    spqcdSize = 8;
                                    scalarExpounded = true;
                                    break;
                                case 1:
                                    spqcdSize = 16;
                                    scalarExpounded = false;
                                    break;
                                case 2:
                                    spqcdSize = 16;
                                    scalarExpounded = true;
                                    break;
                                default:
                                    throw new Exception("Invalid SQcd value " + sqcd);
                            }
                            qcd.NoQuantization = spqcdSize == DefaultHeadSize;
                            qcd.ScalarExpounded = scalarExpounded;
                            qcd.GuardBits = sqcd >> 5;
                            spqcds = new List<EpsilonMU>();
                            while (data.Position < length + position)
                            {
                                var jbyte = data.ReadUByte();
                                var spqcd = new EpsilonMU();
                                if (spqcdSize == DefaultHeadSize)
                                {
                                    spqcd.epsilon = jbyte >> 3;
                                    spqcd.mu = 0;
                                }
                                else
                                {
                                    spqcd.epsilon = jbyte >> 3;
                                    spqcd.mu = ((jbyte & 0x7) << 8) | data.ReadUByte();
                                }
                                spqcds.Add(spqcd);
                            }
                            qcd.SPqcds = spqcds;
                            if (context.MainHeader)
                            {
                                context.QCD = qcd;
                            }
                            else
                            {
                                context.CurrentTile.QCD = qcd;
                                context.CurrentTile.QCC = new Dictionary<int, Quantization>();
                            }
                            break;
                        case 0xff5d: // Quantization component (QCC)
                            length = data.ReadUInt16();
                            var qcc = new Quantization();
                            ushort cqcc;
                            if (context.SIZ.Csiz < 257)
                            {
                                cqcc = data.ReadUByte();
                            }
                            else
                            {
                                cqcc = data.ReadUInt16();
                            }
                            sqcd = data.ReadUByte();
                            switch (sqcd & 0x1f)
                            {
                                case 0:
                                    spqcdSize = 8;
                                    scalarExpounded = true;
                                    break;
                                case 1:
                                    spqcdSize = 16;
                                    scalarExpounded = false;
                                    break;
                                case 2:
                                    spqcdSize = 16;
                                    scalarExpounded = true;
                                    break;
                                default:
                                    throw new Exception("Invalid SQcd value " + sqcd);
                            }
                            qcc.NoQuantization = spqcdSize == DefaultHeadSize;
                            qcc.ScalarExpounded = scalarExpounded;
                            qcc.GuardBits = sqcd >> 5;
                            spqcds = new List<EpsilonMU>();
                            while (data.Position < length + position)
                            {
                                var jbyte = data.ReadUByte();
                                var spqcd = new EpsilonMU { };
                                if (spqcdSize == DefaultHeadSize)
                                {
                                    spqcd.epsilon = jbyte >> 3;
                                    spqcd.mu = 0;
                                }
                                else
                                {
                                    spqcd.epsilon = jbyte >> 3;
                                    spqcd.mu = ((jbyte & 0x7) << 8) | data.ReadUByte();
                                }
                                spqcds.Add(spqcd);
                            }
                            qcc.SPqcds = spqcds;
                            if (context.MainHeader)
                            {
                                context.QCC[cqcc] = qcc;
                            }
                            else
                            {
                                context.CurrentTile.QCC[cqcc] = qcc;
                            }
                            break;
                        case 0xff52: // Coding style default (COD)
                            length = data.ReadUInt16();
                            var cod = new Cod();
                            var scod = data.ReadUByte();

                            cod.EntropyCoderWithCustomPrecincts = 0 != (scod & 1);
                            cod.SopMarkerUsed = 0 != (scod & 2);
                            cod.EphMarkerUsed = 0 != (scod & 4);

                            cod.ProgressionOrder = data.ReadUByte();
                            cod.LayersCount = data.ReadUInt16();
                            cod.MultipleComponentTransform = data.ReadUByte() != 0;

                            cod.DecompositionLevelsCount = data.ReadUByte();
                            cod.Xcb = (data.ReadUByte() & 0xf) + 2;
                            cod.Ycb = (data.ReadUByte() & 0xf) + 2;
                            var blockStyle = data.ReadUByte();
                            cod.SelectiveArithmeticCodingBypass = 0 != (blockStyle & 1);
                            cod.ResetContextProbabilities = 0 != (blockStyle & 2);
                            cod.TerminationOnEachCodingPass = 0 != (blockStyle & 4);
                            cod.VerticallyStripe = 0 != (blockStyle & 8);
                            cod.PredictableTermination = 0 != (blockStyle & 16);
                            cod.SegmentationSymbolUsed = 0 != (blockStyle & 32);
                            cod.ReversibleTransformation = data.ReadUByte() != 0;
                            if (cod.EntropyCoderWithCustomPrecincts)
                            {
                                var precinctsSizes = new List<PrecinctSize>();
                                while (data.Position < length + position)
                                {
                                    var precinctsSize = data.ReadUByte();
                                    precinctsSizes.Add(new PrecinctSize(pPx: precinctsSize & 0xf, pPy: precinctsSize >> 4));
                                }
                                cod.PrecinctsSizes = precinctsSizes;
                            }
                            var unsupported = new List<string>();
                            if (cod.SelectiveArithmeticCodingBypass)
                            {
                                unsupported.Add("selectiveArithmeticCodingBypass");
                            }
                            if (cod.TerminationOnEachCodingPass)
                            {
                                unsupported.Add("terminationOnEachCodingPass");
                            }
                            if (cod.VerticallyStripe)
                            {
                                unsupported.Add("verticallyStripe");
                            }
                            if (cod.PredictableTermination)
                            {
                                unsupported.Add("predictableTermination");
                            }
                            if (unsupported.Count > 0)
                            {
                                doNotRecover = true;
                                Debug.WriteLine($"`warn: JPX: Unsupported COD options ({string.Join(", ", unsupported)})");
                            }
                            if (context.MainHeader)
                            {
                                context.COD = cod;
                            }
                            else
                            {
                                context.CurrentTile.COD = cod;
                                context.CurrentTile.COC = new Dictionary<int, Cod>();
                            }
                            break;
                        case 0xff90: // Start of tile-part (SOT)
                            length = data.ReadUInt16();
                            tile = new Tile(index: data.ReadUInt16(),
                                length: (int)data.ReadUInt32(),
                                partIndex: data.ReadUByte(),
                                partsCount: data.ReadUByte());
                            tile.DataEnd = tile.Length + position - 2;


                            context.MainHeader = false;
                            if (tile.PartIndex == 0)
                            {
                                // reset component specific settings
                                tile.COD = context.COD;
                                tile.COC = context.COC; // clone of the global COC
                                tile.QCD = context.QCD;
                                tile.QCC = context.QCC; // clone of the global COC
                            }
                            context.CurrentTile = tile;
                            break;
                        case 0xff93: // Start of data (SOD)
                            tile = context.CurrentTile;
                            if (tile.PartIndex == 0)
                            {
                                InitializeTile(context, tile.Index);
                                BuildPackets(context);
                            }

                            // moving to the end of the data
                            length = tile.DataEnd - position;
                            ParseTilePackets(context, data, length);
                            break;
                        case 0xff53: // Coding style component (COC)
                            length = data.ReadUInt16();
                            Debug.WriteLine("warn: JPX: Codestream code 0xFF53 (COC) is not implemented.");
                            break;
                        case 0xff55: // Tile-part lengths, main header (TLM)
                        case 0xff57: // Packet length, main header (PLM)
                        case 0xff58: // Packet length, tile-part header (PLT)
                        case 0xff64: // Comment (COM)
                            length = data.ReadUInt16();
                            // skipping content
                            break;
                        default:
                            throw new Exception($"Unknown codestream code: {code.ToString()}");
                    }
                    data.Position = position + length;
                }
            }
            catch (Exception e)
            {
                if (doNotRecover || failOnCorruptedImage)
                {
                    throw new JpxError(e.Message);
                }
                else
                {
                    Debug.WriteLine($"warn: JPX: Trying to recover from: {e.Message}");
                }
            }
            tiles = TransformComponents(context);
            width = context.SIZ.Xsiz - context.SIZ.XOsiz;
            height = context.SIZ.Ysiz - context.SIZ.YOsiz;
            componentsCount = context.SIZ.Csiz;
        }

        private void CalculateComponentDimensions(Component component, SIZ siz)
        {
            // Section B.2 Component mapping
            component.X0 = (int)Math.Ceiling((double)siz.XOsiz / component.XRsiz);
            component.X1 = (int)Math.Ceiling((double)siz.Xsiz / component.XRsiz);
            component.Y0 = (int)Math.Ceiling((double)siz.YOsiz / component.YRsiz);
            component.Y1 = (int)Math.Ceiling((double)siz.Ysiz / component.YRsiz);
            component.Width = component.X1 - component.X0;
            component.Height = component.Y1 - component.Y0;
        }

        private void CalculateTileGrids(Context context, List<Component> components)
        {
            var siz = context.SIZ;
            // Section B.3 Division into tile and tile-components
            var tile = (Tile)null;
            var tiles = new List<Tile>();
            var numXtiles = (int)Math.Ceiling((double)(siz.Xsiz - siz.XTOsiz) / siz.XTsiz);
            var numYtiles = (int)Math.Ceiling((double)(siz.Ysiz - siz.YTOsiz) / siz.YTsiz);
            for (var q = 0; q < numYtiles; q++)
            {
                for (var p = 0; p < numXtiles; p++)
                {
                    tile = new Tile();
                    tile.Tx0 = Math.Max(siz.XTOsiz + p * siz.XTsiz, siz.XOsiz);
                    tile.Ty0 = Math.Max(siz.YTOsiz + q * siz.YTsiz, siz.YOsiz);
                    tile.Tx1 = Math.Min(siz.XTOsiz + (p + 1) * siz.XTsiz, siz.Xsiz);
                    tile.Ty1 = Math.Min(siz.YTOsiz + (q + 1) * siz.YTsiz, siz.Ysiz);
                    tile.Width = tile.Tx1 - tile.Tx0;
                    tile.Height = tile.Ty1 - tile.Ty0;
                    tile.Components = new Dictionary<int, Component>();
                    tiles.Add(tile);
                }
            }
            context.Tiles = tiles;

            var componentsCount = siz.Csiz;
            for (int i = 0, ii = componentsCount; i < ii; i++)
            {
                var component = components[i];
                var jj = tiles.Count;
                for (var j = 0; j < jj; j++)
                {
                    var tileComponent = new Component();
                    tile = tiles[j];
                    tileComponent.Tcx0 = (int)Math.Ceiling((double)tile.Tx0 / component.XRsiz);
                    tileComponent.Tcy0 = (int)Math.Ceiling((double)tile.Ty0 / component.YRsiz);
                    tileComponent.Tcx1 = (int)Math.Ceiling((double)tile.Tx1 / component.XRsiz);
                    tileComponent.Tcy1 = (int)Math.Ceiling((double)tile.Ty1 / component.YRsiz);
                    tileComponent.Width = tileComponent.Tcx1 - tileComponent.Tcx0;
                    tileComponent.Height = tileComponent.Tcy1 - tileComponent.Tcy0;
                    tile.Components[i] = tileComponent;
                }
            }
        }

        Dimension GetBlocksDimensions(Context context, Component component, int r)
        {
            var codOrCoc = component.CodingStyleParameters;
            var result = new Dimension();
            if (!codOrCoc.EntropyCoderWithCustomPrecincts)
            {
                result.PPx = 15;
                result.PPy = 15;
            }
            else
            {
                result.PPx = codOrCoc.PrecinctsSizes[r].PPx;
                result.PPy = codOrCoc.PrecinctsSizes[r].PPy;
            }
            // calculate codeblock size as described in section B.7
            result.xcb_ = r > 0 ? Math.Min(codOrCoc.Xcb, result.PPx - 1) : Math.Min(codOrCoc.Xcb, result.PPx);
            result.ycb_ = r > 0 ? Math.Min(codOrCoc.Ycb, result.PPy - 1) : Math.Min(codOrCoc.Ycb, result.PPy);
            return result;
        }

        void BuildPrecincts(Context context, Resolution resolution, Dimension dimensions)
        {
            // Section B.6 Division resolution to precincts
            var precinctWidth = 1 << dimensions.PPx;
            var precinctHeight = 1 << dimensions.PPy;
            // Jasper introduces codeblock groups for mapping each subband codeblocks
            // to precincts. Precinct partition divides a resolution according to width
            // and height parameters. The subband that belongs to the resolution level
            // has a different size than the level, unless it is the zero resolution.

            // From Jasper documentation: jpeg2000.pdf, section K: Tier-2 coding:
            // The precinct partitioning for a particular subband is derived from a
            // partitioning of its parent LL band (i.e., the LL band at the next higher
            // resolution level)... The LL band associated with each resolution level is
            // divided into precincts... Each of the resulting precinct regions is then
            // mapped into its child subbands (if any) at the next lower resolution
            // level. This is accomplished by using the coordinate transformation
            // (u, v) = (ceil(x/2), ceil(y/2)) where (x, y) and (u, v) are the
            // coordinates of a point in the LL band and child subband, respectively.
            var isZeroRes = resolution.ResLevel == 0;
            var precinctWidthInSubband = 1 << (dimensions.PPx + (isZeroRes ? 0 : -1));
            var precinctHeightInSubband = 1 << (dimensions.PPy + (isZeroRes ? 0 : -1));
            var numprecinctswide = resolution.Trx1 > resolution.Trx0
            ? (int)Math.Ceiling((double)resolution.Trx1 / precinctWidth) -
            (int)Math.Floor((double)resolution.Trx0 / precinctWidth)
            : 0;
            var numprecinctshigh = resolution.Try1 > resolution.Try0
            ? (int)Math.Ceiling((double)resolution.Try1 / precinctHeight) -
            (int)Math.Floor((double)resolution.Try0 / precinctHeight)
            : 0;
            var numprecincts = numprecinctswide * numprecinctshigh;

            resolution.PrecinctParameters = new PrecinctParameters(
            precinctWidth,
            precinctHeight,
            numprecinctswide,
            numprecinctshigh,
            numprecincts,
            precinctWidthInSubband,
            precinctHeightInSubband
            );
        }

        private void BuildCodeblocks(Context context, SubBand subband, Dimension dimensions)
        {
            // Section B.7 Division sub-band into code-blocks
            var xcb_ = dimensions.xcb_;
            var ycb_ = dimensions.ycb_;
            var codeblockWidth = 1 << xcb_;
            var codeblockHeight = 1 << ycb_;
            var cbx0 = subband.Tbx0 >> xcb_;
            var cby0 = subband.Tby0 >> ycb_;
            var cbx1 = (subband.Tbx1 + codeblockWidth - 1) >> xcb_;
            var cby1 = (subband.Tby1 + codeblockHeight - 1) >> ycb_;
            var precinctParameters = subband.Resolution.PrecinctParameters;
            var codeblocks = new List<CodeBlock>();
            var precincts = new Dictionary<double, Precinct>();
            var i = 0;
            var j = 0;
            var codeblock = (CodeBlock)null;
            var precinctNumber = 0;
            for (j = cby0; j < cby1; j++)
            {
                for (i = cbx0; i < cbx1; i++)
                {
                    codeblock = new CodeBlock(
                    cbx: i,
                    cby: j,
                    tbx0: codeblockWidth * i,
                    tby0: codeblockHeight * j,
                    tbx1: codeblockWidth * (i + 1),
                    tby1: codeblockHeight * (j + 1)
                    );

                    codeblock.Tbx0_ = Math.Max(subband.Tbx0, codeblock.Tbx0);
                    codeblock.Tby0_ = Math.Max(subband.Tby0, codeblock.Tby0);
                    codeblock.Tbx1_ = Math.Min(subband.Tbx1, codeblock.Tbx1);
                    codeblock.Tby1_ = Math.Min(subband.Tby1, codeblock.Tby1);

                    // Calculate precinct number for this codeblock, codeblock position
                    // should be relative to its subband, use actual dimension and position
                    // See comment about codeblock group width and height
                    var pi = (int)Math.Floor((double)(codeblock.Tbx0_ - subband.Tbx0) / precinctParameters.PrecinctWidthInSubband);
                    var pj = (int)Math.Floor((double)(codeblock.Tby0_ - subband.Tby0) / precinctParameters.PrecinctHeightInSubband);
                    precinctNumber = pi + pj * precinctParameters.Numprecinctswide;

                    codeblock.PrecinctNumber = precinctNumber;
                    codeblock.SubbandType = subband.Type;
                    codeblock.Lblock = 3;

                    if (codeblock.Tbx1_ <= codeblock.Tbx0_ ||
                    codeblock.Tby1_ <= codeblock.Tby0_)
                    {
                        continue;
                    }
                    codeblocks.Add(codeblock);
                    // building precinct for the sub-band
                    if (precincts.TryGetValue(precinctNumber, out var precinct))
                    {
                        if (i < precinct.CbxMin)
                        {
                            precinct.CbxMin = i;
                        }
                        else if (i > precinct.CbxMax)
                        {
                            precinct.CbxMax = i;
                        }
                        if (j < precinct.CbyMin)
                        {
                            precinct.CbxMin = j;
                        }
                        else if (j > precinct.CbyMax)
                        {
                            precinct.CbyMax = j;
                        }
                    }
                    else
                    {
                        precincts[precinctNumber] = precinct = new Precinct(
                        cbxMin: i,
                        cbyMin: j,
                        cbxMax: i,
                        cbyMax: j);
                    }
                    codeblock.Precinct = precinct;
                }
            }
            subband.CodeblockParameters = new CodeblockParameters(
            codeblockWidth: xcb_,
            codeblockHeight: ycb_,
            numcodeblockwide: cbx1 - cbx0 + 1,
            numcodeblockhigh: cby1 - cby0 + 1);
            subband.Codeblocks = codeblocks;
            subband.Precincts = precincts;
        }

        internal static Packet CreatePacket(Resolution resolution, int precinctNumber, int layerNumber)
        {
            var precinctCodeblocks = new List<CodeBlock>();
            // Section B.10.8 Order of info in packet
            var subbands = resolution.subbands;
            // sub-bands already ordered in 'LL', 'HL', 'LH', and 'HH' sequence
            for (int i = 0, ii = subbands.Count; i < ii; i++)
            {
                var subband = subbands[i];
                var codeblocks = subband.Codeblocks;
                for (int j = 0, jj = codeblocks.Count; j < jj; j++)
                {
                    var codeblock = codeblocks[j];
                    if (codeblock.PrecinctNumber != precinctNumber)
                    {
                        continue;
                    }
                    precinctCodeblocks.Add(codeblock);
                }
            }
            return new Packet(layerNumber, codeblocks: precinctCodeblocks);
        }


        internal static int? GetPrecinctIndexIfExist(int pxIndex, int pyIndex, Size sizeInImageScale, PrecinctSizes precinctIterationSizes, Resolution resolution)
        {
            var posX = pxIndex * precinctIterationSizes.MinWidth;
            var posY = pyIndex * precinctIterationSizes.MinHeight;
            if (posX % sizeInImageScale.Width != 0 ||
            posY % sizeInImageScale.Height != 0)
            {
                return null;
            }
            var startPrecinctRowIndex =
            (posY / sizeInImageScale.Width) *
            resolution.PrecinctParameters.Numprecinctswide;
            return (posX / sizeInImageScale.Height + startPrecinctRowIndex);
        }

        internal static PrecinctSizes GetPrecinctSizesInImageScale(Tile tile)
        {
            var componentsCount = tile.Components.Count;
            var minWidth = int.MaxValue;
            var minHeight = int.MaxValue;
            var maxNumWide = 0;
            var maxNumHigh = 0;
            var sizePerComponent = new PrecinctSizes[componentsCount];
            for (var c = 0; c < componentsCount; c++)
            {
                var component = tile.Components[c];
                var decompositionLevelsCount = component.CodingStyleParameters.DecompositionLevelsCount;
                var sizePerResolution = new Size[decompositionLevelsCount + 1];
                var minWidthCurrentComponent = int.MaxValue;
                var minHeightCurrentComponent = int.MaxValue;
                var maxNumWideCurrentComponent = 0;
                var maxNumHighCurrentComponent = 0;
                var scale = 1;
                for (var r = decompositionLevelsCount; r >= 0; --r)
                {
                    var resolution = component.Resolutions[r];
                    var widthCurrentResolution =
                    scale * resolution.PrecinctParameters.PrecinctWidth;
                    var heightCurrentResolution =
                    scale * resolution.PrecinctParameters.PrecinctHeight;
                    minWidthCurrentComponent = Math.Min(minWidthCurrentComponent, widthCurrentResolution);
                    minHeightCurrentComponent = Math.Min(minHeightCurrentComponent, heightCurrentResolution);
                    maxNumWideCurrentComponent = Math.Max(maxNumWideCurrentComponent, resolution.PrecinctParameters.Numprecinctswide);
                    maxNumHighCurrentComponent = Math.Max(maxNumHighCurrentComponent, resolution.PrecinctParameters.Numprecinctshigh);
                    sizePerResolution[r] = new Size(width: widthCurrentResolution, height: heightCurrentResolution);
                    scale <<= 1;
                }
                minWidth = Math.Min(minWidth, minWidthCurrentComponent);
                minHeight = Math.Min(minHeight, minHeightCurrentComponent);
                maxNumWide = Math.Max(maxNumWide, maxNumWideCurrentComponent);
                maxNumHigh = Math.Max(maxNumHigh, maxNumHighCurrentComponent);
                sizePerComponent[c] = new PrecinctSizes(
                minWidth: minWidthCurrentComponent,
                minHeight: minHeightCurrentComponent,
                maxNumWide: maxNumWideCurrentComponent,
                maxNumHigh: maxNumHighCurrentComponent,
                resolutions: sizePerResolution
                );
            }
            return new PrecinctSizes(
            minWidth,
            minHeight,
            maxNumWide,
            maxNumHigh,
            components: sizePerComponent
            );
        }

        private void BuildPackets(Context context)
        {
            var siz = context.SIZ;
            var tileIndex = context.CurrentTile.Index;
            var tile = context.Tiles[tileIndex];
            var componentsCount = siz.Csiz;
            // Creating resolutions and sub-bands for each component
            for (var c = 0; c < componentsCount; c++)
            {
                var component = tile.Components[c];
                var decompositionLevelsCount =
                component.CodingStyleParameters.DecompositionLevelsCount;
                // Section B.5 Resolution levels and sub-bands
                var resolutions = new List<Resolution>();
                var subbands = new List<SubBand>();
                for (var r = 0; r <= decompositionLevelsCount; r++)
                {
                    var blocksDimensions = GetBlocksDimensions(context, component, r);
                    var scale = 1 << (decompositionLevelsCount - r);
                    var resolution = new Resolution(
                    trx0: (int)Math.Ceiling((double)component.Tcx0 / scale),
                    try0: (int)Math.Ceiling((double)component.Tcy0 / scale),
                    trx1: (int)Math.Ceiling((double)component.Tcx1 / scale),
                    try1: (int)Math.Ceiling((double)component.Tcy1 / scale),
                    resLevel: r);
                    BuildPrecincts(context, resolution, blocksDimensions);
                    resolutions.Add(resolution);

                    var subband = (SubBand)null;
                    if (r == 0)
                    {
                        // one sub-band (LL) with last decomposition
                        subband = new SubBand(
                            type: "LL",
                            tbx0: (int)Math.Ceiling((double)component.Tcx0 / scale),
                            tby0: (int)Math.Ceiling((double)component.Tcy0 / scale),
                            tbx1: (int)Math.Ceiling((double)component.Tcx1 / scale),
                            tby1: (int)Math.Ceiling((double)component.Tcy1 / scale),
                            resolution: resolution);
                        BuildCodeblocks(context, subband, blocksDimensions);
                        subbands.Add(subband);
                        resolution.subbands = new List<SubBand> { subband };
                    }
                    else
                    {
                        var bscale = 1 << (decompositionLevelsCount - r + 1);
                        var resolutionSubbands = new List<SubBand>();
                        // three sub-bands (HL, LH and HH) with rest of decompositions
                        subband = new SubBand(
                            type: "HL",
                            tbx0: (int)Math.Ceiling((double)component.Tcx0 / bscale - 0.5),
                            tby0: (int)Math.Ceiling((double)component.Tcy0 / bscale),
                            tbx1: (int)Math.Ceiling((double)component.Tcx1 / bscale - 0.5),
                            tby1: (int)Math.Ceiling((double)component.Tcy1 / bscale),
                            resolution: resolution);
                        BuildCodeblocks(context, subband, blocksDimensions);
                        subbands.Add(subband);
                        resolutionSubbands.Add(subband);

                        subband = new SubBand(
                            type: "LH",
                            tbx0: (int)Math.Ceiling((double)component.Tcx0 / bscale),
                            tby0: (int)Math.Ceiling((double)component.Tcy0 / bscale - 0.5),
                            tbx1: (int)Math.Ceiling((double)component.Tcx1 / bscale),
                            tby1: (int)Math.Ceiling((double)component.Tcy1 / bscale - 0.5),
                            resolution: resolution);
                        BuildCodeblocks(context, subband, blocksDimensions);
                        subbands.Add(subband);
                        resolutionSubbands.Add(subband);

                        subband = new SubBand(
                            type: "HH",
                            tbx0: (int)Math.Ceiling((double)component.Tcx0 / bscale - 0.5),
                            tby0: (int)Math.Ceiling((double)component.Tcy0 / bscale - 0.5),
                            tbx1: (int)Math.Ceiling((double)component.Tcx1 / bscale - 0.5),
                            tby1: (int)Math.Ceiling((double)component.Tcy1 / bscale - 0.5),
                            resolution: resolution);
                        BuildCodeblocks(context, subband, blocksDimensions);
                        subbands.Add(subband);
                        resolutionSubbands.Add(subband);

                        resolution.subbands = resolutionSubbands;
                    }
                }
                component.Resolutions = resolutions;
                component.Subbands = subbands;
            }
            // Generate the packets sequence
            var progressionOrder = tile.CodingStyleDefaultParameters.ProgressionOrder;
            switch (progressionOrder)
            {
                case 0:
                    tile.PacketsIterator = new LayerResolutionComponentPositionIterator(context);
                    break;
                case 1:
                    tile.PacketsIterator = new ResolutionLayerComponentPositionIterator(context);
                    break;
                case 2:
                    tile.PacketsIterator = new ResolutionPositionComponentLayerIterator(context);
                    break;
                case 3:
                    tile.PacketsIterator = new PositionComponentResolutionLayerIterator(context);
                    break;
                case 4:
                    tile.PacketsIterator = new ComponentPositionResolutionLayerIterator(context);
                    break;
                default:
                    throw new JpxError($"Unsupported progression order {progressionOrder}");
            }
        }

        private void ParseTilePackets(Context context, IInputStream data, int length)
        {
            var end = data.Position + length;
            var buffer = 0;
            var bufferSize = 0;
            var skipNextBit = false;
            int readBits(int count)
            {
                while (bufferSize < count)
                {
                    var b = data.ReadUByte();
                    if (skipNextBit)
                    {
                        buffer = (buffer << 7) | b;
                        bufferSize += 7;
                        skipNextBit = false;
                    }
                    else
                    {
                        buffer = (buffer << 8) | b;
                        bufferSize += 8;
                    }
                    if (b == 0xff)
                    {
                        skipNextBit = true;
                    }
                }
                bufferSize -= count;
                return (int)(((uint)buffer) >> bufferSize) & ((1 << count) - 1);
            }
            bool skipMarkerIfEqual(byte value)
            {
                if (data.PeekUByte(-1) == 0xff
                    && data.PeekUByte(0) == value)
                {
                    data.Skip(1);
                    return true;
                }
                else if (data.PeekUByte(0) == 0xff
                    && data.PeekUByte(1) == value)
                {
                    data.Skip(2);
                    return true;
                }
                return false;
            }
            void alignToByte()
            {
                bufferSize = 0;
                if (skipNextBit)
                {
                    data.Skip(1);
                    skipNextBit = false;
                }
            }
            int ReadCodingpasses()
            {
                if (readBits(1) == 0)
                {
                    return 1;
                }
                if (readBits(1) == 0)
                {
                    return 2;
                }
                var value = readBits(2);
                if (value < 3)
                {
                    return value + 3;
                }
                value = readBits(5);
                if (value < 31)
                {
                    return value + 6;
                }
                value = readBits(7);
                return value + 37;
            }
            var tileIndex = context.CurrentTile.Index;
            var tile = context.Tiles[tileIndex];
            var sopMarkerUsed = context.COD.SopMarkerUsed;
            var ephMarkerUsed = context.COD.EphMarkerUsed;
            var packetsIterator = tile.PacketsIterator;
            while (data.Position < end)
            {
                alignToByte();
                if (sopMarkerUsed && skipMarkerIfEqual(0x91))
                {
                    // Skip also marker segment length and packet sequence ID
                    data.Skip(4);
                }
                var packet = packetsIterator.NextPacket();
                if (0 == readBits(1))
                {
                    continue;
                }
                var layerNumber = packet.LayerNumber;
                var queue = new Queue<PacketItem>();
                var codeblock = (CodeBlock)null;
                for (int i = 0, ii = packet.Codeblocks.Count; i < ii; i++)
                {
                    codeblock = packet.Codeblocks[i];
                    var precinct = codeblock.Precinct;
                    var codeblockColumn = codeblock.Cbx - precinct.CbxMin;
                    var codeblockRow = codeblock.Cby - precinct.CbyMin;
                    var codeblockIncluded = false;
                    var firstTimeInclusion = false;
                    var valueReady = false;

                    if (codeblock.Included != null)
                    {
                        codeblockIncluded = 0 != readBits(1);
                    }
                    else
                    {
                        // reading inclusion tree
                        var inclusionTree = (InclusionTree)null;
                        var zeroBitPlanesTree = (TagTree)null;

                        precinct = codeblock.Precinct;
                        if (precinct.InclusionTree != null)
                        {
                            inclusionTree = precinct.InclusionTree;
                        }
                        else
                        {
                            // building inclusion and zero bit-planes trees
                            var width = precinct.CbxMax - precinct.CbxMin + 1;
                            var height = precinct.CbyMax - precinct.CbyMin + 1;
                            inclusionTree = new InclusionTree(width, height, FiltersExtension.ToByte(layerNumber));
                            zeroBitPlanesTree = new TagTree(width, height);
                            precinct.InclusionTree = inclusionTree;
                            precinct.ZeroBitPlanesTree = zeroBitPlanesTree;
                            for (var l = 0; l < layerNumber; l++)
                            {
                                if (readBits(1) != 0)
                                {
                                    throw new JpxError("Invalid tag tree");
                                }
                            }
                        }

                        if (inclusionTree.Reset(codeblockColumn, codeblockRow, layerNumber))
                        {
                            while (true)
                            {
                                if (readBits(1) != 0)
                                {
                                    valueReady = !inclusionTree.NextLevel();
                                    if (valueReady)
                                    {
                                        codeblock.Included = true;
                                        codeblockIncluded = firstTimeInclusion = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    inclusionTree.IncrementValue(layerNumber);
                                    break;
                                }
                            }
                        }
                    }
                    if (!codeblockIncluded)
                    {
                        continue;
                    }
                    if (firstTimeInclusion)
                    {
                        var zeroBitPlanesTree = precinct.ZeroBitPlanesTree;
                        zeroBitPlanesTree.Reset(codeblockColumn, codeblockRow);
                        while (true)
                        {
                            if (readBits(1) != 0)
                            {
                                valueReady = !zeroBitPlanesTree.NextLevel();
                                if (valueReady)
                                {
                                    break;
                                }
                            }
                            else
                            {
                                zeroBitPlanesTree.IncrementValue();
                            }
                        }
                        codeblock.ZeroBitPlanes = zeroBitPlanesTree.Value;
                    }
                    var codingpasses = ReadCodingpasses();
                    while (readBits(1) != 0)
                    {
                        codeblock.Lblock++;
                    }
                    var codingpassesLog2 = FiltersExtension.Log2(codingpasses);
                    // rounding down log2
                    var bits = (codingpasses < 1 << codingpassesLog2 ? codingpassesLog2 - 1 : codingpassesLog2) + codeblock.Lblock;
                    var codedDataLength = readBits(bits);
                    queue.Enqueue(new PacketItem(codeblock, codingpasses, dataLength: codedDataLength));
                }
                alignToByte();
                if (ephMarkerUsed)
                {
                    skipMarkerIfEqual(0x92);
                }
                while (queue.Count > 0)
                {
                    var packetItem = queue.Dequeue();
                    codeblock = packetItem.Codeblock;
                    if (codeblock.Data == null)
                    {
                        codeblock.Data = new List<CodeBlockData>();
                    }
                    codeblock.Data.Add(new CodeBlockData(
                        data: data.ReadMemory(packetItem.DataLength),
                        codingpasses: packetItem.Codingpasses));
                }
            }
        }

        private void CopyCoefficients(double[] coefficients, int levelWidth, int levelHeight,
        SubBand subband, double delta, int mb, bool reversible, bool segmentationSymbolUsed,
        bool resetContextProbabilities)
        {
            var x0 = subband.Tbx0;
            var y0 = subband.Tby0;
            var width = subband.Tbx1 - subband.Tbx0;
            var codeblocks = subband.Codeblocks;
            var right = subband.Type[0] == 'H' ? 1 : 0;
            var bottom = subband.Type[1] == 'H' ? levelWidth : 0;

            for (int i = 0, ii = codeblocks.Count; i < ii; ++i)
            {
                var codeblock = codeblocks[i];
                var blockWidth = codeblock.Tbx1_ - codeblock.Tbx0_;
                var blockHeight = codeblock.Tby1_ - codeblock.Tby0_;
                if (blockWidth == 0 || blockHeight == 0)
                {
                    continue;
                }
                if (codeblock.Data == null)
                {
                    continue;
                }

                // collect data
                var data = codeblock.Data;
                var totalLength = 0;
                var codingpasses = 0;
                var j = 0; var jj = 0;
                CodeBlockData dataItem;
                for (j = 0, jj = data.Count; j < jj; j++)
                {
                    dataItem = data[j];
                    totalLength += dataItem.Data.Length;
                    codingpasses += dataItem.Codingpasses;
                }

                if (totalLength == 0)
                {
                    continue;
                }

                var currentCodingpassType = 2; // first bit plane starts from cleanup

                Memory<byte> encodedData = new byte[totalLength];
                var position = 0;
                for (j = 0, jj = data.Count; j < jj; j++)
                {
                    dataItem = data[j];
                    dataItem.Data.CopyTo(encodedData.Slice(position));
                    position += dataItem.Data.Length;
                }
                // decoding the item
                var bitModel = new BitModel(blockWidth, blockHeight, codeblock.SubbandType, codeblock.ZeroBitPlanes, mb);
                var decoder = new ArithmeticDecoder(encodedData);
                bitModel.SetDecoder(decoder);

                for (j = 0; j < codingpasses; j++)
                {
                    switch (currentCodingpassType)
                    {
                        case 0:
                            bitModel.RunSignificancePropagationPass();
                            break;
                        case 1:
                            bitModel.RunMagnitudeRefinementPass();
                            break;
                        case 2:
                            bitModel.RunCleanupPass();
                            if (segmentationSymbolUsed)
                            {
                                bitModel.CheckSegmentation();
                            }
                            break;
                    }
                    if (resetContextProbabilities)
                    {
                        bitModel.Reset();
                    }
                    currentCodingpassType = (currentCodingpassType + 1) % 3;
                }

                var offset = codeblock.Tbx0_ - x0 + (codeblock.Tby0_ - y0) * width;
                var sign = bitModel.CoefficentsSign;
                var magnitude = bitModel.CoefficentsMagnitude;
                var bitsDecoded = bitModel.BitsDecoded;
                var magnitudeCorrection = reversible ? 0 : 0.5D;
                var k = 0;
                var n = 0D;
                var nb = 0;
                position = 0;
                // Do the interleaving of Section F.3.3 here, so we do not need
                // to copy later. LL level is not interleaved, just copied.
                var interleave = subband.Type != "LL";
                for (j = 0; j < blockHeight; j++)
                {
                    var row = offset / width | 0; // row in the non-interleaved subband
                    var levelOffset = 2 * row * (levelWidth - width) + right + bottom;
                    for (k = 0; k < blockWidth; k++)
                    {
                        n = magnitude[position];
                        if (n != 0)
                        {
                            n = (n + magnitudeCorrection) * delta;
                            if (sign[position] != 0)
                            {
                                n = -n;
                            }
                            nb = bitsDecoded[position];
                            var pos = interleave ? levelOffset + (offset << 1) : offset;
                            coefficients[pos] = reversible && nb >= mb ? n : n * (1 << (mb - nb));
                        }
                        offset++;
                        position++;
                    }
                    offset += width - blockWidth;
                }
            }
        }
        private TileResultF TransformTile(Context context, Tile tile, int c)
        {
            var component = tile.Components[c];
            var codingStyleParameters = component.CodingStyleParameters;
            var quantizationParameters = component.QuantizationParameters;
            var decompositionLevelsCount =
            codingStyleParameters.DecompositionLevelsCount;
            var spqcds = quantizationParameters.SPqcds;
            var scalarExpounded = quantizationParameters.ScalarExpounded;
            var guardBits = quantizationParameters.GuardBits;
            var segmentationSymbolUsed = codingStyleParameters.SegmentationSymbolUsed;
            var resetContextProbabilities = codingStyleParameters.ResetContextProbabilities;
            var precision = context.Components[c].Precision;

            var reversible = codingStyleParameters.ReversibleTransformation;
            Transform transform = reversible
            ? new ReversibleTransform()
            : new IrreversibleTransform();

            var subbandCoefficients = new List<Coefficient>();
            var b = 0;
            for (var i = 0; i <= decompositionLevelsCount; i++)
            {
                var resolution = component.Resolutions[i];

                var width = resolution.Trx1 - resolution.Trx0;
                var height = resolution.Try1 - resolution.Try0;
                // Allocate space for the whole sublevel.
                var coefficients = new double[width * height];
                for (int j = 0, jj = resolution.subbands.Count; j < jj; j++)
                {
                    var mu = 0;
                    var epsilon = 0;
                    if (!scalarExpounded)
                    {
                        // formula E-5
                        mu = spqcds[0].mu;
                        epsilon = spqcds[0].epsilon + (i > 0 ? 1 - i : 0);
                    }
                    else
                    {
                        mu = spqcds[b].mu;
                        epsilon = spqcds[b].epsilon;
                        b++;
                    }

                    var subband = resolution.subbands[j];
                    var gainLog2 = SubbandsGainLog2[subband.Type];

                    // calculate quantization coefficient (Section E.1.1.1)
                    var delta = reversible ? 1 : (double)Math.Pow(2, precision + gainLog2 - epsilon) * (1 + mu / 2048D);
                    var mb = guardBits + epsilon - 1;

                    // In the first resolution level, copyCoefficients will fill the
                    // whole array with coefficients. In the succeeding passes,
                    // copyCoefficients will consecutively fill in the values that belong
                    // to the interleaved positions of the HL, LH, and HH coefficients.
                    // The LL coefficients will then be interleaved in Transform.iterate().
                    CopyCoefficients(coefficients, width, height, subband, delta, mb, reversible, segmentationSymbolUsed, resetContextProbabilities);
                }
                subbandCoefficients.Add(new Coefficient(width, height, items: coefficients));
            }

            var result = transform.Calculate(subbandCoefficients, component.Tcx0, component.Tcy0);
            return new TileResultF(left: component.Tcx0, top: component.Tcy0, width: result.Width, height: result.Height, items: result.Items);
        }

        List<TileResultB> TransformComponents(Context context)
        {
            var siz = context.SIZ;
            var components = context.Components;
            var componentsCount = siz.Csiz;
            var resultImages = new List<TileResultB>();

            for (int i = 0, ii = context.Tiles.Count; i < ii; i++)
            {
                var tile = context.Tiles[i];
                var transformedTiles = new List<TileResultF>();
                for (var c = 0; c < componentsCount; c++)
                {
                    transformedTiles.Add(TransformTile(context, tile, c));
                }
                var tile0 = transformedTiles[0];
                var output = new byte[tile0.Items.Length * componentsCount];
                var result = new TileResultB(
                left: tile0.Left,
                top: tile0.Top,
                width: tile0.Width,
                height: tile0.Height,
                items: output);

                // Section G.2.2 Inverse multi component transform
                var shift = 0;
                var offset = 0D;
                var pos = 0;
                var j = 0;
                var jj = 0;
                var y0 = 0D;
                var y1 = 0D;
                var y2 = 0D;
                if (tile.CodingStyleDefaultParameters.MultipleComponentTransform)
                {
                    var fourComponents = componentsCount == 4;
                    var y0items = transformedTiles[0].Items;
                    var y1items = transformedTiles[1].Items;
                    var y2items = transformedTiles[2].Items;
                    var y3items = fourComponents ? transformedTiles[3].Items : null;

                    // HACK: The multiple component transform formulas below assume that
                    // all components have the same precision. With this in mind, we
                    // compute shift and offset only once.
                    shift = components[0].Precision - DefaultHeadSize;
                    offset = (128 << shift) + 0.5D;

                    var component0 = tile.Components[0];
                    var alpha01 = componentsCount - 3;
                    jj = y0items.Length;
                    if (!component0.CodingStyleParameters.ReversibleTransformation)
                    {
                        // inverse irreversible multiple component transform
                        for (j = 0; j < jj; j++, pos += alpha01)
                        {
                            y0 = y0items[j] + offset;
                            y1 = y1items[j];
                            y2 = y2items[j];
                            output[pos++] = FiltersExtension.ToByte((int)(y0 + 1.402D * y2) >> shift);
                            output[pos++] = FiltersExtension.ToByte((int)(y0 - 0.34413D * y1 - 0.71414D * y2) >> shift);
                            output[pos++] = FiltersExtension.ToByte((int)(y0 + 1.772D * y1) >> shift);
                        }
                    }
                    else
                    {
                        // inverse reversible multiple component transform
                        for (j = 0; j < jj; j++, pos += alpha01)
                        {
                            y0 = y0items[j] + offset;
                            y1 = y1items[j];
                            y2 = y2items[j];
                            var g = y0 - ((int)(y2 + y1) >> 2);

                            output[pos++] = FiltersExtension.ToByte((int)(g + y2) >> shift);
                            output[pos++] = FiltersExtension.ToByte((int)g >> shift);
                            output[pos++] = FiltersExtension.ToByte((int)(g + y1) >> shift);
                        }
                    }
                    if (fourComponents)
                    {
                        for (j = 0, pos = 3; j < jj; j++, pos += 4)
                        {
                            output[pos] = FiltersExtension.ToByte((int)(y3items[j] + offset) >> shift);
                        }
                    }
                }
                else
                {
                    // no multi-component transform
                    for (var c = 0; c < componentsCount; c++)
                    {
                        var items = transformedTiles[c].Items;
                        shift = components[c].Precision - DefaultHeadSize;
                        offset = (128 << shift) + 0.5D;
                        for (pos = c, j = 0, jj = items.Length; j < jj; j++)
                        {
                            output[pos] = FiltersExtension.ToByte((int)(items[j] + offset) >> shift);
                            pos += componentsCount;
                        }
                    }
                }
                resultImages.Add(result);
            }
            return resultImages;
        }

        void InitializeTile(Context context, int tileIndex)
        {
            var siz = context.SIZ;
            var componentsCount = siz.Csiz;
            var tile = context.Tiles[tileIndex];
            for (var c = 0; c < componentsCount; c++)
            {
                var component = tile.Components[c];
                var qcdOrQcc =
                context.CurrentTile.QCC.TryGetValue(c, out var quantization)
                ? quantization
                : context.CurrentTile.QCD;
                component.QuantizationParameters = qcdOrQcc;
                var codOrCoc =
                context.CurrentTile.COC.TryGetValue(c, out var cod)
                ? cod
                : context.CurrentTile.COD;
                component.CodingStyleParameters = codOrCoc;
            }
            tile.CodingStyleDefaultParameters = context.CurrentTile.COD;
        }

    }

    internal abstract class Iterator
    {
        public abstract Packet NextPacket();
    }

    internal class LayerResolutionComponentPositionIterator : Iterator
    {
        private int l;
        private int r;
        private int i;
        private int k;
        private Context context;
        private SIZ siz;
        private int tileIndex;
        private Tile tile;
        private int layersCount;
        private int componentsCount;
        private int maxDecompositionLevelsCount;

        public LayerResolutionComponentPositionIterator(Context context)
        {
            this.context = context;
            siz = context.SIZ;
            tileIndex = context.CurrentTile.Index;
            tile = context.Tiles[tileIndex];
            layersCount = tile.CodingStyleDefaultParameters.LayersCount;
            componentsCount = siz.Csiz;
            maxDecompositionLevelsCount = 0;
            for (var q = 0; q < componentsCount; q++)
            {
                maxDecompositionLevelsCount = Math.Max(
                maxDecompositionLevelsCount,
                tile.Components[q].CodingStyleParameters.DecompositionLevelsCount
                );
            }

            l = 0;
            r = 0;
            i = 0;
            k = 0;
        }

        public override Packet NextPacket()
        {
            // Section B.12.1.1 Layer-resolution-component-position
            for (; l < layersCount; l++)
            {
                for (; r <= maxDecompositionLevelsCount; r++)
                {
                    for (; i < componentsCount; i++)
                    {
                        var component = tile.Components[i];
                        if (r > component.CodingStyleParameters.DecompositionLevelsCount)
                        {
                            continue;
                        }

                        var resolution = component.Resolutions[r];
                        var numprecincts = resolution.PrecinctParameters.Numprecincts;
                        for (; k < numprecincts;)
                        {
                            var packet = JpxImage.CreatePacket(resolution, k, l);
                            k++;
                            return packet;
                        }
                        k = 0;
                    }
                    i = 0;
                }
                r = 0;
            }
            throw new JpxError("Out of packets");
        }
    }

    internal class ResolutionLayerComponentPositionIterator : Iterator
    {
        private Context context;
        private SIZ siz;
        private int tileIndex;
        private Tile tile;
        private int layersCount;
        private int componentsCount;
        private int maxDecompositionLevelsCount;
        private int r;
        private int l;
        private int i;
        private int k;

        public ResolutionLayerComponentPositionIterator(Context context)
        {
            this.context = context;
            siz = context.SIZ;
            tileIndex = context.CurrentTile.Index;
            tile = context.Tiles[tileIndex];
            layersCount = tile.CodingStyleDefaultParameters.LayersCount;
            componentsCount = siz.Csiz;
            maxDecompositionLevelsCount = 0;
            for (var q = 0; q < componentsCount; q++)
            {
                maxDecompositionLevelsCount = Math.Max(
                maxDecompositionLevelsCount,
                tile.Components[q].CodingStyleParameters.DecompositionLevelsCount
                );
            }

            r = 0;
            l = 0;
            i = 0;
            k = 0;
        }
        public override Packet NextPacket()
        {
            // Section B.12.1.2 Resolution-layer-component-position
            for (; r <= maxDecompositionLevelsCount; r++)
            {
                for (; l < layersCount; l++)
                {
                    for (; i < componentsCount; i++)
                    {
                        var component = tile.Components[i];
                        if (r > component.CodingStyleParameters.DecompositionLevelsCount)
                        {
                            continue;
                        }

                        var resolution = component.Resolutions[r];
                        var numprecincts = resolution.PrecinctParameters.Numprecincts;
                        for (; k < numprecincts;)
                        {
                            var packet = JpxImage.CreatePacket(resolution, k, l);
                            k++;
                            return packet;
                        }
                        k = 0;
                    }
                    i = 0;
                }
                l = 0;
            }
            throw new JpxError("Out of packets");
        }
    }

    internal class ResolutionPositionComponentLayerIterator : Iterator
    {
        private readonly Context context;
        private SIZ siz;
        private ushort tileIndex;
        private Tile tile;
        private int layersCount;
        private int componentsCount;
        private int l;
        private int r;
        private int c;
        private int p;
        private int maxDecompositionLevelsCount;
        private int[] maxNumPrecinctsInLevel;

        public ResolutionPositionComponentLayerIterator(Context context)
        {
            this.context = context;
            siz = context.SIZ;
            tileIndex = context.CurrentTile.Index;
            tile = context.Tiles[tileIndex];
            layersCount = tile.CodingStyleDefaultParameters.LayersCount;
            componentsCount = siz.Csiz;
            l = 0; r = 0; c = 0; p = 0;
            maxDecompositionLevelsCount = 0;
            for (c = 0; c < componentsCount; c++)
            {
                var component = tile.Components[c];
                maxDecompositionLevelsCount = Math.Max(
                maxDecompositionLevelsCount,
                component.CodingStyleParameters.DecompositionLevelsCount
                );
            }
            maxNumPrecinctsInLevel = new int[maxDecompositionLevelsCount + 1];
            for (r = 0; r <= maxDecompositionLevelsCount; ++r)
            {
                var maxNumPrecincts = 0D;
                for (c = 0; c < componentsCount; ++c)
                {
                    var resolutions = tile.Components[c].Resolutions;
                    if (r < resolutions.Count)
                    {
                        maxNumPrecincts = Math.Max(
                        maxNumPrecincts,
                        resolutions[r].PrecinctParameters.Numprecincts
                        );
                    }
                }
                maxNumPrecinctsInLevel[r] = (int)maxNumPrecincts;
            }
            l = 0;
            r = 0;
            c = 0;
            p = 0;
        }

        public override Packet NextPacket()
        {
            // Section B.12.1.3 Resolution-position-component-layer
            for (; r <= maxDecompositionLevelsCount; r++)
            {
                for (; p < maxNumPrecinctsInLevel[r]; p++)
                {
                    for (; c < componentsCount; c++)
                    {
                        var component = tile.Components[c];
                        if (r > component.CodingStyleParameters.DecompositionLevelsCount)
                        {
                            continue;
                        }
                        var resolution = component.Resolutions[r];
                        var numprecincts = resolution.PrecinctParameters.Numprecincts;
                        if (p >= numprecincts)
                        {
                            continue;
                        }
                        for (; l < layersCount;)
                        {
                            var packet = JpxImage.CreatePacket(resolution, p, l);
                            l++;
                            return packet;
                        }
                        l = 0;
                    }
                    c = 0;
                }
                p = 0;
            }
            throw new JpxError("Out of packets");
        }
    }

    internal class PositionComponentResolutionLayerIterator : Iterator
    {
        private readonly Context context;
        private readonly SIZ siz;
        private readonly ushort tileIndex;
        private readonly Tile tile;
        private int layersCount;
        private int componentsCount;
        private PrecinctSizes precinctsSizes;
        private PrecinctSizes precinctsIterationSizes;
        private int l;
        private int r;
        private int c;
        private int px;
        private int py;

        public PositionComponentResolutionLayerIterator(Context context)
        {
            this.context = context;
            siz = context.SIZ;
            tileIndex = context.CurrentTile.Index;
            tile = context.Tiles[tileIndex];
            layersCount = tile.CodingStyleDefaultParameters.LayersCount;
            componentsCount = siz.Csiz;
            precinctsSizes = JpxImage.GetPrecinctSizesInImageScale(tile);
            precinctsIterationSizes = precinctsSizes;
            l = 0;
            r = 0;
            c = 0;
            px = 0;
            py = 0;
        }

        public override Packet NextPacket()
        {
            // Section B.12.1.4 Position-component-resolution-layer
            for (; py < precinctsIterationSizes.MaxNumHigh; py++)
            {
                for (; px < precinctsIterationSizes.MaxNumWide; px++)
                {
                    for (; c < componentsCount; c++)
                    {
                        var component = tile.Components[c];
                        var decompositionLevelsCount = component.CodingStyleParameters.DecompositionLevelsCount;
                        for (; r <= decompositionLevelsCount; r++)
                        {
                            var resolution = component.Resolutions[r];
                            var sizeInImageScale =
                            precinctsSizes.Components[c].Resolutions[r];
                            var k = JpxImage.GetPrecinctIndexIfExist(px, py, sizeInImageScale, precinctsIterationSizes, resolution);
                            if (k == null)
                            {
                                continue;
                            }
                            for (; l < layersCount;)
                            {
                                var packet = JpxImage.CreatePacket(resolution, (int)k, l);
                                l++;
                                return packet;
                            }
                            l = 0;
                        }
                        r = 0;
                    }
                    c = 0;
                }
                px = 0;
            }
            throw new JpxError("Out of packets");
        }
    }

    internal class ComponentPositionResolutionLayerIterator : Iterator
    {
        private readonly Context context;
        private readonly SIZ siz;
        private readonly int tileIndex;
        private readonly Tile tile;
        private readonly int layersCount;
        private readonly int componentsCount;
        private readonly PrecinctSizes precinctsSizes;
        private int l;
        private int r;
        private int c;
        private int px;
        private int py;

        public ComponentPositionResolutionLayerIterator(Context context)
        {
            this.context = context;
            siz = context.SIZ;
            tileIndex = context.CurrentTile.Index;
            tile = context.Tiles[tileIndex];
            layersCount = tile.CodingStyleDefaultParameters.LayersCount;
            componentsCount = siz.Csiz;
            precinctsSizes = JpxImage.GetPrecinctSizesInImageScale(tile);
            l = 0;
            r = 0;
            c = 0;
            px = 0;
            py = 0;
        }

        public override Packet NextPacket()
        {
            // Section B.12.1.5 Component-position-resolution-layer
            for (; c < componentsCount; ++c)
            {
                var component = tile.Components[c];
                var precinctsIterationSizes = precinctsSizes.Components[c];
                var decompositionLevelsCount = component.CodingStyleParameters.DecompositionLevelsCount;
                for (; py < precinctsIterationSizes.MaxNumHigh; py++)
                {
                    for (; px < precinctsIterationSizes.MaxNumWide; px++)
                    {
                        for (; r <= decompositionLevelsCount; r++)
                        {
                            var resolution = component.Resolutions[r];
                            var sizeInImageScale = precinctsIterationSizes.Resolutions[r];
                            var k = JpxImage.GetPrecinctIndexIfExist(px, py, sizeInImageScale, precinctsIterationSizes, resolution);
                            if (k == null)
                            {
                                continue;
                            }
                            for (; l < layersCount;)
                            {
                                var packet = JpxImage.CreatePacket(resolution, (int)k, l);
                                l++;
                                return packet;
                            }
                            l = 0;
                        }
                        r = 0;
                    }
                    px = 0;
                }
                py = 0;
            }
            throw new JpxError("Out of packets");
        }
    }

    internal class Level
    {
        internal int Width;
        internal int Height;
        internal byte?[] Items;
        internal int Index;

        public Level(int width, int height, byte?[] items)
        {
            Width = width;
            Height = height;
            Items = items;
        }
    }

    internal class InclusionTree
    {
        private List<Level> levels;
        private int currentLevel;

        // eslint-disable-next-line no-shadow
        public InclusionTree(int width, int height, byte defaultValue)
        {
            var levelsLength = FiltersExtension.Log2(Math.Max(width, height)) + 1;
            levels = new List<Level>();
            for (var i = 0; i < levelsLength; i++)
            {
                var items = new byte?[width * height];
                for (int j = 0, jj = items.Length; j < jj; j++)
                {
                    items[j] = defaultValue;
                }

                var level = new Level(width, height, items);
                levels.Add(level);

                width = (int)Math.Ceiling(width / 2d);
                height = (int)Math.Ceiling(height / 2d);
            }
        }

        public bool Reset(int i, int j, int stopValue)
        {
            var currentLevel = 0;
            while (currentLevel < levels.Count)
            {
                var level = levels[currentLevel];
                var index = i + j * level.Width;
                level.Index = index;
                var value = level.Items[index];

                if (value == 0xff)
                {
                    break;
                }

                if (value > stopValue)
                {
                    this.currentLevel = currentLevel;
                    // already know about this one, propagating the value to top levels
                    PropagateValues();
                    return false;
                }

                i >>= 1;
                j >>= 1;
                currentLevel++;
            }
            this.currentLevel = currentLevel - 1;
            return true;
        }
        public void IncrementValue(int stopValue)
        {
            var level = levels[currentLevel];
            level.Items[level.Index] = FiltersExtension.ToByte(stopValue + 1);
            PropagateValues();
        }

        public void PropagateValues()
        {
            var levelIndex = currentLevel;
            var level = levels[levelIndex];
            var currentValue = level.Items[level.Index];
            while (--levelIndex >= 0)
            {
                level = levels[levelIndex];
                level.Items[level.Index] = currentValue;
            }
        }
        public bool NextLevel()
        {
            var currentLevel = this.currentLevel;
            var level = levels[currentLevel];
            var value = level.Items[level.Index];
            level.Items[level.Index] = 0xff;
            currentLevel--;
            if (currentLevel < 0)
            {
                return false;
            }

            this.currentLevel = currentLevel;
            level = levels[currentLevel];
            level.Items[level.Index] = value;
            return true;
        }
    }

    internal class TagTree
    {
        internal List<Level> Levels;
        internal int CurrentLevel;
        internal byte Value;

        // eslint-disable-next-line no-shadow
        public TagTree(int width, int height)
        {
            var levelsLength = FiltersExtension.Log2(Math.Max(width, height)) + 1;
            Levels = new List<Level>();
            for (var i = 0; i < levelsLength; i++)
            {
                var level = new Level(width, height, items: new byte?[0]);
                Levels.Add(level);
                width = (int)Math.Ceiling(width / 2D);
                height = (int)Math.Ceiling(height / 2D);
            }
        }

        public void Reset(int i, int j)
        {
            var currentLevel = 0;
            byte value = 0;
            var level = (Level)null;
            while (currentLevel < Levels.Count)
            {
                level = Levels[currentLevel];
                var index = i + j * level.Width;
                if (level.Items.Length > index
                && level.Items[index] != null)
                {
                    value = (byte)level.Items[index];
                    break;
                }
                level.Index = index;
                i >>= 1;
                j >>= 1;
                currentLevel++;
            }
            currentLevel--;
            level = Levels[currentLevel];
            CheckBuffer(level);
            level.Items[level.Index] = value;
            CurrentLevel = currentLevel;
            // delete this.value;
        }

        private static void CheckBuffer(Level level)
        {
            if (level.Items.Length < level.Index + 1)
            {
                var temp = level.Items;
                level.Items = new byte?[level.Index + 1];
                temp.AsSpan().CopyTo(level.Items);
            }
        }

        public void IncrementValue()
        {
            var level = Levels[CurrentLevel];
            level.Items[level.Index]++;
        }
        public bool NextLevel()
        {
            var currentLevel = CurrentLevel;
            var level = Levels[currentLevel];
            var value = (byte)level.Items[level.Index];
            currentLevel--;
            if (currentLevel < 0)
            {
                Value = value;
                return false;
            }

            CurrentLevel = currentLevel;
            level = Levels[currentLevel];
            CheckBuffer(level);
            level.Items[level.Index] = value;
            return true;
        }
    }

    // Section D. Coefficient bit modeling
    internal class BitModel
    {
        public static readonly int UNIFORM_CONTEXT = 17;
        public static readonly int RUNLENGTH_CONTEXT = 18;
        // Table D-1
        // The index is binary presentation: 0dddvvhh, ddd - sum of Di (0..4),
        // vv - sum of Vi (0..2), and hh - sum of Hi (0..2)
        // prettier-ignore
        public static readonly byte[] LLAndLHContextsLabel = new byte[]{
            0, 5, 8, 0, 3, 7, 8, 0, 4, 7, 8, 0, 0, 0, 0, 0, 1, 6, 8, 0, 3, 7, 8, 0, 4,
            7, 8, 0, 0, 0, 0, 0, 2, 6, 8, 0, 3, 7, 8, 0, 4, 7, 8, 0, 0, 0, 0, 0, 2, 6,
            8, 0, 3, 7, 8, 0, 4, 7, 8, 0, 0, 0, 0, 0, 2, 6, 8, 0, 3, 7, 8, 0, 4, 7, 8
            };
        // prettier-ignore
        public static readonly byte[] HLContextLabel = new byte[]{
            0, 3, 4, 0, 5, 7, 7, 0, 8, 8, 8, 0, 0, 0, 0, 0, 1, 3, 4, 0, 6, 7, 7, 0, 8,
            8, 8, 0, 0, 0, 0, 0, 2, 3, 4, 0, 6, 7, 7, 0, 8, 8, 8, 0, 0, 0, 0, 0, 2, 3,
            4, 0, 6, 7, 7, 0, 8, 8, 8, 0, 0, 0, 0, 0, 2, 3, 4, 0, 6, 7, 7, 0, 8, 8, 8
            };
        // prettier-ignore
        public static readonly byte[] HHContextLabel = new byte[]{
            0, 1, 2, 0, 1, 2, 2, 0, 2, 2, 2, 0, 0, 0, 0, 0, 3, 4, 5, 0, 4, 5, 5, 0, 5,
            5, 5, 0, 0, 0, 0, 0, 6, 7, 7, 0, 7, 7, 7, 0, 7, 7, 7, 0, 0, 0, 0, 0, 8, 8,
            8, 0, 8, 8, 8, 0, 8, 8, 8, 0, 0, 0, 0, 0, 8, 8, 8, 0, 8, 8, 8, 0, 8, 8, 8
            };

        internal int Width;
        internal int Height;
        internal byte[] ContextLabelTable;
        internal byte[] NeighborsSignificance;
        internal byte[] CoefficentsSign;
        internal uint[] CoefficentsMagnitude;
        internal byte[] ProcessingFlags;
        internal byte[] BitsDecoded;
        internal ArithmeticDecoder Decoder;
        internal sbyte[] Contexts;

        // eslint-disable-next-line no-shadow
        public BitModel(int width, int height, string subband, byte zeroBitPlanes, int mb)
        {
            Width = width;
            Height = height;

            byte[] contextLabelTable;
            if (subband == "HH")
            {
                contextLabelTable = HHContextLabel;
            }
            else if (subband == "HL")
            {
                contextLabelTable = HLContextLabel;
            }
            else
            {
                contextLabelTable = LLAndLHContextsLabel;
            }
            ContextLabelTable = contextLabelTable;

            var coefficientCount = width * height;

            // coefficients outside the encoding region treated as insignificant
            // add border state cells for significanceState
            NeighborsSignificance = new byte[coefficientCount];
            CoefficentsSign = new byte[coefficientCount];
            //Array coefficentsMagnitude;
            //if (mb > 14)
            //{
            //    coefficentsMagnitude = new uint[coefficientCount];
            //}
            //else if (mb > 6)
            //{
            //    coefficentsMagnitude = new ushort[coefficientCount];
            //}
            //else
            //{
            //    coefficentsMagnitude = new byte[coefficientCount];
            //}
            CoefficentsMagnitude = new uint[coefficientCount];
            ProcessingFlags = new byte[coefficientCount];

            var bitsDecoded = new byte[coefficientCount];
            if (zeroBitPlanes != 0)
            {
                for (var i = 0; i < coefficientCount; i++)
                {
                    bitsDecoded[i] = zeroBitPlanes;
                }
            }
            BitsDecoded = bitsDecoded;

            Reset();
        }

        public void SetDecoder(ArithmeticDecoder decoder)
        {
            Decoder = decoder;
        }

        public void Reset()
        {
            // We have 17 contexts that are accessed via context labels,
            // plus the uniform and runlength context.
            Contexts = new sbyte[19];

            // Contexts are packed into 1 byte:
            // highest 7 bits carry the index, lowest bit carries mps
            Contexts[0] = (4 << 1) | 0;
            Contexts[UNIFORM_CONTEXT] = (46 << 1) | 0;
            Contexts[RUNLENGTH_CONTEXT] = (3 << 1) | 0;
        }

        public void SetNeighborsSignificance(int row, int column, int index)
        {
            var neighborsSignificance = NeighborsSignificance;
            var width = Width;
            var height = Height;
            var left = column > 0;
            var right = column + 1 < width;
            var i = 0;

            if (row > 0)
            {
                i = index - width;
                if (left)
                {
                    neighborsSignificance[i - 1] += 0x10;
                }
                if (right)
                {
                    neighborsSignificance[i + 1] += 0x10;
                }
                neighborsSignificance[i] += 0x04;
            }

            if (row + 1 < height)
            {
                i = index + width;
                if (left)
                {
                    neighborsSignificance[i - 1] += 0x10;
                }
                if (right)
                {
                    neighborsSignificance[i + 1] += 0x10;
                }
                neighborsSignificance[i] += 0x04;
            }

            if (left)
            {
                neighborsSignificance[index - 1] += 0x01;
            }
            if (right)
            {
                neighborsSignificance[index + 1] += 0x01;
            }
            neighborsSignificance[index] |= 0x80;
        }

        public void RunSignificancePropagationPass()
        {
            var decoder = Decoder;
            var width = Width;
            var height = Height;
            var coefficentsMagnitude = CoefficentsMagnitude;
            var coefficentsSign = CoefficentsSign;
            var neighborsSignificance = NeighborsSignificance;
            var processingFlags = ProcessingFlags;
            var contexts = Contexts;
            var labels = ContextLabelTable;
            var bitsDecoded = BitsDecoded;
            var processedInverseMask = (sbyte)(~1);
            var processedMask = 1;
            var firstMagnitudeBitMask = 2;

            for (var i0 = 0; i0 < height; i0 += 4)
            {
                for (var j = 0; j < width; j++)
                {
                    var index = i0 * width + j;
                    for (var i1 = 0; i1 < 4; i1++, index += width)
                    {
                        var i = i0 + i1;
                        if (i >= height)
                        {
                            break;
                        }
                        // clear processed flag first
                        processingFlags[index] = (byte)(processingFlags[index] & processedInverseMask);

                        if (coefficentsMagnitude[index] != 0 ||
                        neighborsSignificance[index] == 0)
                        {
                            continue;
                        }

                        var contextLabel = labels[neighborsSignificance[index]];
                        var decision = decoder.ReadBit(contexts, contextLabel);
                        if (decision != 0)
                        {
                            var sign = DecodeSignBit(i, j, index);
                            coefficentsSign[index] = FiltersExtension.ToByte(sign);
                            coefficentsMagnitude[index] = 1;
                            SetNeighborsSignificance(i, j, index);
                            processingFlags[index] = FiltersExtension.ToByte(processingFlags[index] | firstMagnitudeBitMask);
                        }
                        bitsDecoded[index]++;
                        processingFlags[index] = FiltersExtension.ToByte(processingFlags[index] | processedMask);
                    }
                }
            }
        }

        private int DecodeSignBit(int row, int column, int index)
        {
            var width = Width;
            var height = Height;
            var coefficentsMagnitude = CoefficentsMagnitude;
            var coefficentsSign = CoefficentsSign;
            var contribution = 0;
            var sign0 = 0;
            var sign1 = 0;
            var significance1 = false;
            var contextLabel = 0;
            var decoded = 0;

            // calculate horizontal contribution
            significance1 = column > 0 && coefficentsMagnitude[index - 1] != 0;
            if (column + 1 < width && coefficentsMagnitude[index + 1] != 0)
            {
                sign1 = coefficentsSign[index + 1];
                if (significance1)
                {
                    sign0 = coefficentsSign[index - 1];
                    contribution = 1 - sign1 - sign0;
                }
                else
                {
                    contribution = 1 - sign1 - sign1;
                }
            }
            else if (significance1)
            {
                sign0 = coefficentsSign[index - 1];
                contribution = 1 - sign0 - sign0;
            }
            else
            {
                contribution = 0;
            }
            var horizontalContribution = 3 * contribution;

            // calculate vertical contribution and combine with the horizontal
            significance1 = row > 0 && coefficentsMagnitude[index - width] != 0;
            if (row + 1 < height && coefficentsMagnitude[index + width] != 0)
            {
                sign1 = coefficentsSign[index + width];
                if (significance1)
                {
                    sign0 = coefficentsSign[index - width];
                    contribution = 1 - sign1 - sign0 + horizontalContribution;
                }
                else
                {
                    contribution = 1 - sign1 - sign1 + horizontalContribution;
                }
            }
            else if (significance1)
            {
                sign0 = coefficentsSign[index - width];
                contribution = 1 - sign0 - sign0 + horizontalContribution;
            }
            else
            {
                contribution = horizontalContribution;
            }

            if (contribution >= 0)
            {
                contextLabel = 9 + contribution;
                decoded = Decoder.ReadBit(Contexts, contextLabel);
            }
            else
            {
                contextLabel = 9 - contribution;
                decoded = Decoder.ReadBit(Contexts, contextLabel) ^ 1;
            }
            return decoded;
        }

        public void RunMagnitudeRefinementPass()
        {
            var decoder = Decoder;
            var width = Width;
            var height = Height;
            var coefficentsMagnitude = CoefficentsMagnitude;
            var neighborsSignificance = NeighborsSignificance;
            var contexts = Contexts;
            var bitsDecoded = BitsDecoded;
            var processingFlags = ProcessingFlags;
            var processedMask = 1;
            var firstMagnitudeBitMask = 2;
            var length = width * height;
            var width4 = width * 4;

            for (int index0 = 0, indexNext = 0; index0 < length; index0 = indexNext)
            {
                indexNext = Math.Min(length, index0 + width4);
                for (var j = 0; j < width; j++)
                {
                    for (var index = index0 + j; index < indexNext; index += width)
                    {
                        // significant but not those that have just become
                        if (coefficentsMagnitude[index] == 0 || (processingFlags[index] & processedMask) != 0)
                        {
                            continue;
                        }

                        var contextLabel = 16;
                        if ((processingFlags[index] & firstMagnitudeBitMask) != 0)
                        {
                            processingFlags[index] = FiltersExtension.ToByte(processingFlags[index] ^ firstMagnitudeBitMask);
                            // first refinement
                            var significance = neighborsSignificance[index] & 127;
                            contextLabel = significance == 0 ? 15 : 14;
                        }

                        var bit = decoder.ReadBit(contexts, contextLabel);
                        coefficentsMagnitude[index] = (uint)((coefficentsMagnitude[index] << 1) | (uint)bit);
                        bitsDecoded[index]++;
                        processingFlags[index] = FiltersExtension.ToByte(processingFlags[index] | processedMask);
                    }
                }
            }
        }
        public void RunCleanupPass()
        {
            var decoder = Decoder;
            var width = Width;
            var height = Height;
            var neighborsSignificance = NeighborsSignificance;
            var coefficentsMagnitude = CoefficentsMagnitude;
            var coefficentsSign = CoefficentsSign;
            var contexts = Contexts;
            var labels = ContextLabelTable;
            var bitsDecoded = BitsDecoded;
            var processingFlags = ProcessingFlags;
            var processedMask = 1;
            var firstMagnitudeBitMask = 2;
            var oneRowDown = width;
            var twoRowsDown = width * 2;
            var threeRowsDown = width * 3;
            var iNext = 0;
            for (var i0 = 0; i0 < height; i0 = iNext)
            {
                iNext = Math.Min(i0 + 4, height);
                var indexBase = i0 * width;
                var checkAllEmpty = i0 + 3 < height;
                for (var j = 0; j < width; j++)
                {
                    var index0 = indexBase + j;
                    // using the property: labels[neighborsSignificance[index]] == 0
                    // when neighborsSignificance[index] == 0
                    var allEmpty =
                        checkAllEmpty &&
                        processingFlags[index0] == 0 &&
                        processingFlags[index0 + oneRowDown] == 0 &&
                        processingFlags[index0 + twoRowsDown] == 0 &&
                        processingFlags[index0 + threeRowsDown] == 0 &&
                        neighborsSignificance[index0] == 0 &&
                        neighborsSignificance[index0 + oneRowDown] == 0 &&
                        neighborsSignificance[index0 + twoRowsDown] == 0 &&
                        neighborsSignificance[index0 + threeRowsDown] == 0;
                    var i1 = 0;
                    var index = index0;
                    var i = i0;
                    var sign = 0;
                    if (allEmpty)
                    {
                        var hasSignificantCoefficent = decoder.ReadBit(contexts, RUNLENGTH_CONTEXT);
                        if (0 == hasSignificantCoefficent)
                        {
                            bitsDecoded[index0]++;
                            bitsDecoded[index0 + oneRowDown]++;
                            bitsDecoded[index0 + twoRowsDown]++;
                            bitsDecoded[index0 + threeRowsDown]++;
                            continue; // next column
                        }
                        i1 =
                        (decoder.ReadBit(contexts, UNIFORM_CONTEXT) << 1) |
                        decoder.ReadBit(contexts, UNIFORM_CONTEXT);
                        if (i1 != 0)
                        {
                            i = i0 + i1;
                            index += i1 * width;
                        }

                        sign = DecodeSignBit(i, j, index);
                        coefficentsSign[index] = FiltersExtension.ToByte(sign);
                        coefficentsMagnitude[index] = 1;
                        SetNeighborsSignificance(i, j, index);
                        processingFlags[index] = FiltersExtension.ToByte(processingFlags[index] | firstMagnitudeBitMask);

                        index = index0;
                        for (var i2 = i0; i2 <= i; i2++, index += width)
                        {
                            bitsDecoded[index]++;
                        }

                        i1++;
                    }
                    for (i = i0 + i1; i < iNext; i++, index += width)
                    {
                        if (coefficentsMagnitude[index] != 0 ||
                        (processingFlags[index] & processedMask) != 0)
                        {
                            continue;
                        }

                        var contextLabel = labels[neighborsSignificance[index]];
                        var decision = decoder.ReadBit(contexts, contextLabel);
                        if (decision == 1)
                        {
                            sign = DecodeSignBit(i, j, index);
                            coefficentsSign[index] = FiltersExtension.ToByte(sign);
                            coefficentsMagnitude[index] = 1;
                            SetNeighborsSignificance(i, j, index);
                            processingFlags[index] = FiltersExtension.ToByte(processingFlags[index] | firstMagnitudeBitMask);
                        }
                        bitsDecoded[index]++;
                    }
                }
            }
        }

        public void CheckSegmentation()
        {
            var decoder = Decoder;
            var contexts = Contexts;
            var symbol =
            (decoder.ReadBit(contexts, UNIFORM_CONTEXT) << 3) |
            (decoder.ReadBit(contexts, UNIFORM_CONTEXT) << 2) |
            (decoder.ReadBit(contexts, UNIFORM_CONTEXT) << 1) |
            decoder.ReadBit(contexts, UNIFORM_CONTEXT);
            if (symbol != 0xa)
            {
                throw new JpxError("Invalid segmentation symbol");
            }
        }
    }

    // Section F, Discrete wavelet transformation
    internal class Transform
    {
        // eslint-disable-next-line no-shadow
        public Transform() { }

        public Coefficient Calculate(List<Coefficient> subbands, int u0, int v0)
        {
            var ll = subbands[0];
            for (int i = 1, ii = subbands.Count; i < ii; i++)
            {
                ll = Iterate(ll, subbands[i], u0, v0);
            }
            return ll;
        }

        public void Extend(double[] buffer, int offset, int size)
        {
            // Section F.3.7 extending... using max extension of 4
            var i1 = offset - 1;
            var j1 = offset + 1;
            var i2 = offset + size - 2;
            var j2 = offset + size;
            buffer[i1--] = buffer[j1++];
            buffer[j2++] = buffer[i2--];
            buffer[i1--] = buffer[j1++];
            buffer[j2++] = buffer[i2--];
            buffer[i1--] = buffer[j1++];
            buffer[j2++] = buffer[i2--];
            buffer[i1] = buffer[j1];
            buffer[j2] = buffer[i2];
        }

        public virtual Coefficient Iterate(Coefficient ll, Coefficient hl_lh_hh, int u0, int v0)
        {
            var llWidth = ll.Width;
            var llHeight = ll.Height;
            var llItems = ll.Items;
            var width = hl_lh_hh.Width;
            var height = hl_lh_hh.Height;
            var items = hl_lh_hh.Items;
            var i = 0;
            var j = 0;
            var k = 0;
            var l = 0;
            var u = 0;
            var v = 0;

            // Interleave LL according to Section F.3.3
            for (k = 0, i = 0; i < llHeight; i++)
            {
                l = i * 2 * width;
                for (j = 0; j < llWidth; j++, k++, l += 2)
                {
                    items[l] = llItems[k];
                }
            }
            // The LL band is not needed anymore.
            llItems = ll.Items = null;

            var bufferPadding = 4;
            var rowBuffer = new double[width + 2 * bufferPadding];

            // Section F.3.4 HOR_SR
            if (width == 1)
            {
                // if width = 1, when u0 even keep items as is, when odd divide by 2
                if ((u0 & 1) != 0)
                {
                    for (v = 0, k = 0; v < height; v++, k += width)
                    {
                        items[k] = items[k] * 0.5D;
                    }
                }
            }
            else
            {
                for (v = 0, k = 0; v < height; v++, k += width)
                {
                    var sub1 = items.AsSpan(k, width);
                    sub1.CopyTo(rowBuffer.AsSpan(bufferPadding));

                    Extend(rowBuffer, bufferPadding, width);
                    Filter(rowBuffer, bufferPadding, width);

                    var sub2 = rowBuffer.AsSpan(bufferPadding, width);
                    sub2.CopyTo(items.AsSpan(k));
                }
            }

            // Accesses to the items array can take long, because it may not fit into
            // CPU cache and has to be fetched from main memory. Since subsequent
            // accesses to the items array are not local when reading columns, we
            // have a cache miss every time. To reduce cache misses, get up to
            // 'numBuffers' items at a time and store them into the individual
            // buffers. The colBuffers should be small enough to fit into CPU cache.
            var numBuffers = 16;
            var colBuffers = new List<double[]>();
            for (i = 0; i < numBuffers; i++)
            {
                colBuffers.Add(new double[height + 2 * bufferPadding]);
            }
            var b = 0;
            var currentBuffer = 0;
            var llc = bufferPadding + height;

            // Section F.3.5 VER_SR
            if (height == 1)
            {
                // if height = 1, when v0 even keep items as is, when odd divide by 2
                if ((v0 & 1) != 0)
                {
                    for (u = 0; u < width; u++)
                    {
                        items[u] *= 0.5D;
                    }
                }
            }
            else
            {
                for (u = 0; u < width; u++)
                {
                    // if we ran out of buffers, copy several image columns at once
                    if (currentBuffer == 0)
                    {
                        numBuffers = Math.Min(width - u, numBuffers);
                        for (k = u, l = bufferPadding; l < llc; k += width, l++)
                        {
                            for (b = 0; b < numBuffers; b++)
                            {
                                colBuffers[b][l] = items[k + b];
                            }
                        }
                        currentBuffer = numBuffers;
                    }

                    currentBuffer--;
                    var buffer = colBuffers[currentBuffer];
                    Extend(buffer, bufferPadding, height);
                    Filter(buffer, bufferPadding, height);

                    // If this is last buffer in this group of buffers, flush all buffers.
                    if (currentBuffer == 0)
                    {
                        k = u - numBuffers + 1;
                        for (l = bufferPadding; l < llc; k += width, l++)
                        {
                            for (b = 0; b < numBuffers; b++)
                            {
                                items[k + b] = colBuffers[b][l];
                            }
                        }
                    }
                }
            }

            return new Coefficient(width, height, items);
        }

        public virtual void Filter(double[] x, int offset, int length)
        { }
    }

    // Section 3.8.2 Irreversible 9-7 filter
    internal class IrreversibleTransform : Transform
    {
        // eslint-disable-next-line no-shadow
        public IrreversibleTransform()
        {
            //call(this);
        }

        public override void Filter(double[] x, int offset, int length)
        {
            var len = length >> 1;
            offset |= 0;
            var j = 0;
            var n = 0;
            var current = 0D;
            var next = 0D;

            const double alpha = -1.586134342059924D;
            const double beta = -0.052980118572961D;
            const double gamma = 0.882911075530934D;
            const double delta = 0.443506852043971D;
            const double K = 1.230174104914001D;
            const double K_ = 1 / K;

            // step 1 is combined with step 3

            // step 2
            j = offset - 3;
            for (n = len + 4; n-- != 0; j += 2)
            {
                x[j] *= K_;
            }

            // step 1 & 3
            j = offset - 2;
            current = delta * x[j - 1];
            for (n = len + 3; n-- != 0; j += 2)
            {
                next = delta * x[j + 1];
                x[j] = K * x[j] - current - next;
                if (n-- != 0)
                {
                    j += 2;
                    current = delta * x[j + 1];
                    x[j] = K * x[j] - current - next;
                }
                else
                {
                    break;
                }
            }

            // step 4
            j = offset - 1;
            current = gamma * x[j - 1];
            for (n = len + 2; n-- != 0; j += 2)
            {
                next = gamma * x[j + 1];
                x[j] -= current + next;
                if (n-- != 0)
                {
                    j += 2;
                    current = gamma * x[j + 1];
                    x[j] -= current + next;
                }
                else
                {
                    break;
                }
            }

            // step 5
            j = offset;
            current = beta * x[j - 1];
            for (n = len + 1; n-- != 0; j += 2)
            {
                next = beta * x[j + 1];
                x[j] -= current + next;
                if (n-- != 0)
                {
                    j += 2;
                    current = beta * x[j + 1];
                    x[j] -= current + next;
                }
                else
                {
                    break;
                }
            }

            // step 6
            if (len != 0)
            {
                j = offset + 1;
                current = alpha * x[j - 1];
                for (n = len; n-- != 0; j += 2)
                {
                    next = alpha * x[j + 1];
                    x[j] -= current + next;
                    if (n-- != 0)
                    {
                        j += 2;
                        current = alpha * x[j + 1];
                        x[j] -= current + next;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }
    }

    // Section 3.8.1 Reversible 5-3 filter
    internal class ReversibleTransform : Transform
    {
        // eslint-disable-next-line no-shadow
        public ReversibleTransform()
        {
            //call(this);
        }

        public override void Filter(double[] x, int offset, int length)
        {
            var len = length >> 1;
            offset |= 0;
            var j = 0;
            var n = 0;

            for (j = offset, n = len + 1; n-- != 0; j += 2)
            {
                x[j] -= (int)(x[j - 1] + x[j + 1] + 2) >> 2;
            }

            for (j = offset + 1, n = len; n-- != 0; j += 2)
            {
                x[j] += (int)(x[j - 1] + x[j + 1]) >> 1;
            }
        }

    }

    //Classes
    internal struct CodeBlockData
    {
        internal readonly Memory<byte> Data;
        internal readonly int Codingpasses;

        public CodeBlockData(Memory<byte> data, int codingpasses)
        {
            Data = data;
            Codingpasses = codingpasses;
        }
    }

    internal class PacketItem
    {
        internal readonly CodeBlock Codeblock;
        internal readonly int Codingpasses;
        internal readonly int DataLength;

        public PacketItem(CodeBlock codeblock, int codingpasses, int dataLength)
        {
            Codeblock = codeblock;
            Codingpasses = codingpasses;
            DataLength = dataLength;
        }
    }

    internal class PrecinctSizes
    {
        internal readonly int MinWidth;
        internal readonly int MinHeight;
        internal readonly int MaxNumWide;
        internal readonly int MaxNumHigh;
        internal Size[] Resolutions;
        internal PrecinctSizes[] Components;

        public PrecinctSizes(int minWidth, int minHeight, int maxNumWide, int maxNumHigh, PrecinctSizes[] components = null, Size[] resolutions = null)
        {
            Components = components;
            MinWidth = minWidth;
            MinHeight = minHeight;
            MaxNumWide = maxNumWide;
            MaxNumHigh = maxNumHigh;
            Resolutions = resolutions;
        }
    }

    internal class Coefficient
    {
        internal readonly int Width;
        internal readonly int Height;
        internal double[] Items;

        public Coefficient(int width, int height, double[] items)
        {
            Width = width;
            Height = height;
            Items = items;
        }
    }

    internal class Packet
    {
        internal readonly int LayerNumber;
        internal readonly List<CodeBlock> Codeblocks;

        public Packet(int layerNumber, List<CodeBlock> codeblocks)
        {
            LayerNumber = layerNumber;
            Codeblocks = codeblocks;
        }
    }

    internal class CodeblockParameters
    {
        internal readonly int CodeblockWidth;
        internal readonly int CodeblockHeight;
        internal readonly int Numcodeblockwide;
        internal readonly int Numcodeblockhigh;

        public CodeblockParameters(int codeblockWidth, int codeblockHeight, int numcodeblockwide, int numcodeblockhigh)
        {
            CodeblockWidth = codeblockWidth;
            CodeblockHeight = codeblockHeight;
            Numcodeblockwide = numcodeblockwide;
            Numcodeblockhigh = numcodeblockhigh;
        }
    }

    internal class Precinct
    {
        internal int CbxMin;
        internal int CbyMin;
        internal int CbxMax;
        internal int CbyMax;
        internal InclusionTree InclusionTree;
        internal TagTree ZeroBitPlanesTree;

        public Precinct(int cbxMin, int cbyMin, int cbxMax, int cbyMax)
        {
            CbxMin = cbxMin;
            CbyMin = cbyMin;
            CbxMax = cbxMax;
            CbyMax = cbyMax;
        }
    }

    internal class CodeBlock
    {
        internal readonly int Cbx;
        internal readonly int Cby;
        internal readonly int Tbx0;
        internal readonly int Tby0;
        internal readonly int Tbx1;
        internal readonly int Tby1;
        internal int Tbx0_;
        internal int Tby0_;
        internal int Tbx1_;
        internal int Tby1_;
        internal int PrecinctNumber;
        internal string SubbandType;
        internal int Lblock;
        internal Precinct Precinct;
        internal bool? Included;
        internal List<CodeBlockData> Data;
        internal byte ZeroBitPlanes;

        public CodeBlock(int cbx, int cby, int tbx0, int tby0, int tbx1, int tby1)
        {
            Cbx = cbx;
            Cby = cby;
            Tbx0 = tbx0;
            Tby0 = tby0;
            Tbx1 = tbx1;
            Tby1 = tby1;
        }
    }

    internal class PrecinctParameters
    {
        internal readonly int PrecinctWidth;
        internal readonly int PrecinctHeight;
        internal readonly int Numprecinctswide;
        internal readonly int Numprecinctshigh;
        internal readonly int Numprecincts;
        internal readonly int PrecinctWidthInSubband;
        internal readonly int PrecinctHeightInSubband;

        public PrecinctParameters(int precinctWidth, int precinctHeight, int numprecinctswide, int numprecinctshigh, int numprecincts, int precinctWidthInSubband, int precinctHeightInSubband)
        {
            PrecinctWidth = precinctWidth;
            PrecinctHeight = precinctHeight;
            Numprecinctswide = numprecinctswide;
            Numprecinctshigh = numprecinctshigh;
            Numprecincts = numprecincts;
            PrecinctWidthInSubband = precinctWidthInSubband;
            PrecinctHeightInSubband = precinctHeightInSubband;
        }
    }

    internal class SubBand
    {
        internal readonly string Type;
        internal readonly int Tbx0;
        internal readonly int Tby0;
        internal readonly int Tbx1;
        internal readonly int Tby1;
        internal readonly Resolution Resolution;
        internal CodeblockParameters CodeblockParameters;
        internal List<CodeBlock> Codeblocks;
        internal Dictionary<double, Precinct> Precincts;

        public SubBand(string type, int tbx0, int tby0, int tbx1, int tby1, Resolution resolution)
        {
            Type = type;
            Tbx0 = tbx0;
            Tby0 = tby0;
            Tbx1 = tbx1;
            Tby1 = tby1;
            Resolution = resolution;
        }
    }

    internal class Resolution
    {
        internal readonly int Trx0;
        internal readonly int Try0;
        internal readonly int Trx1;
        internal readonly int Try1;
        internal readonly int ResLevel;
        internal List<SubBand> subbands;
        internal PrecinctParameters PrecinctParameters;

        public Resolution(int trx0, int try0, int trx1, int try1, int resLevel)
        {
            Trx0 = trx0;
            Try0 = try0;
            Trx1 = trx1;
            Try1 = try1;
            ResLevel = resLevel;
        }
    }

    internal class Dimension
    {
        internal int PPx;
        internal int PPy;
        internal int xcb_;
        internal int ycb_;

        public Dimension()
        {
        }
    }

    internal class Component
    {
        internal int Precision;
        internal bool IsSigned;
        internal int XRsiz;
        internal int YRsiz;
        internal int X0;
        internal int X1;
        internal int Y0;
        internal int Y1;
        internal int Width;
        internal int Height;
        internal int Tcx0;
        internal int Tcy0;
        internal int Tcx1;
        internal int Tcy1;
        internal List<Resolution> Resolutions;
        internal List<SubBand> Subbands;
        internal Quantization QuantizationParameters;
        internal Cod CodingStyleParameters;

        public Component()
        {
        }

        public Component(int precision, bool isSigned, int XRsiz, int YRsiz)
        {
            Precision = precision;
            IsSigned = isSigned;
            this.XRsiz = XRsiz;
            this.YRsiz = YRsiz;
        }
    }

    internal class TileResultF
    {
        internal readonly int Left;
        internal readonly int Top;
        internal readonly int Width;
        internal readonly int Height;
        internal readonly double[] Items;

        public TileResultF(int left, int top, int width, int height, double[] items)
        {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
            Items = items;
        }
    }

    internal class TileResultB
    {
        internal readonly int Left;
        internal readonly int Top;
        internal readonly int Width;
        internal readonly int Height;
        internal readonly byte[] items;

        public TileResultB(int left, int top, int width, int height, byte[] items)
        {
            Left = left;
            Top = top;
            Width = width;
            Height = height;
            this.items = items;
        }
    }

    internal class Tile
    {
        internal int Tx0;
        internal int Ty0;
        internal int Tx1;
        internal int Ty1;
        internal int Width;
        internal int Height;
        internal Dictionary<int, Component> Components;
        internal ushort Index;
        internal int Length;
        internal int DataEnd;
        internal int PartIndex;
        internal int PartsCount;
        internal Quantization QCD;
        internal Cod COD;
        internal Dictionary<int, Quantization> QCC;
        internal Dictionary<int, Cod> COC;
        internal Iterator PacketsIterator;
        internal Cod CodingStyleDefaultParameters;

        public Tile()
        {
        }

        public Tile(ushort index, int length, int partIndex, int partsCount)
        {
            Index = index;
            Length = length;
            PartIndex = partIndex;
            PartsCount = partsCount;
        }


    }

    internal class Context
    {
        internal bool MainHeader;
        internal SIZ SIZ;
        internal List<Component> Components;
        internal List<Tile> Tiles;
        internal Tile CurrentTile;
        internal Quantization QCD;
        internal Cod COD;
        internal Dictionary<int, Quantization> QCC;
        internal Dictionary<int, Cod> COC;

        public Context()
        {
        }
    }

    internal class Size
    {
        internal readonly int Width;
        internal readonly int Height;

        public Size(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }

    internal class PrecinctSize
    {
        internal readonly int PPx;
        internal readonly int PPy;

        public PrecinctSize(int pPx, int pPy)
        {
            PPx = pPx;
            PPy = pPy;
        }

    }

    internal class Quantization
    {
        internal bool NoQuantization;
        internal int GuardBits;
        internal bool ScalarExpounded;
        internal List<EpsilonMU> SPqcds;

        public Quantization()
        {
        }
    }

    internal class Cod
    {
        //TODO Enum
        //{
        internal bool EntropyCoderWithCustomPrecincts;
        internal bool SopMarkerUsed;
        internal bool EphMarkerUsed;
        //}

        internal byte ProgressionOrder;
        internal int LayersCount;
        internal bool MultipleComponentTransform;
        internal int DecompositionLevelsCount;
        internal int Xcb;
        internal int Ycb;
        //TODO Enum
        //{
        internal bool SelectiveArithmeticCodingBypass;
        internal bool ResetContextProbabilities;
        internal bool TerminationOnEachCodingPass;
        internal bool VerticallyStripe;
        internal bool PredictableTermination;
        internal bool SegmentationSymbolUsed;
        //}
        internal bool ReversibleTransformation;
        internal List<PrecinctSize> PrecinctsSizes;

        public Cod()
        {
        }
    }

    internal class EpsilonMU
    {
        internal int epsilon;
        internal int mu;

        public EpsilonMU()
        {
        }
    }

    internal class SIZ
    {
        internal int Csiz;
        internal readonly int Xsiz;
        internal readonly int Ysiz;
        internal readonly int XOsiz;
        internal readonly int YOsiz;
        internal readonly int XTsiz;
        internal readonly int YTsiz;
        internal readonly int XTOsiz;
        internal readonly int YTOsiz;

        public SIZ(int Xsiz, int Ysiz, int XOsiz, int YOsiz, int XTsiz, int YTsiz, int XTOsiz, int YTOsiz)
        {
            this.Xsiz = Xsiz;
            this.Ysiz = Ysiz;
            this.XOsiz = XOsiz;
            this.YOsiz = YOsiz;
            this.XTsiz = XTsiz;
            this.YTsiz = YTsiz;
            this.XTOsiz = XTOsiz;
            this.YTOsiz = YTOsiz;
        }
    }
}