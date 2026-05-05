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

using PdfGlown.Bytes.Filters.Jpeg;
using PdfGlown.Objects;
using System;
using System.Collections.Generic;

namespace PdfGlown.Bytes.Filters
{
    public sealed class DCTFilter : Filter
    {
        internal DCTFilter()
        { }

        public override Memory<byte> Decode(IInputStream data, PdfDirectObject parameters, IDictionary<PdfName, PdfDirectObject> header)
        {
            var imageParams = header;
            var dictionary = header as PdfDictionary;
            var dictHeight = dictionary?.GetNInt(PdfName.Height) ?? dictionary?.GetNInt(PdfName.H)
                ?? ((IPdfNumber)(header[PdfName.Height] ?? header[PdfName.H])).IntValue;
            var dictWidth = dictionary?.GetNInt(PdfName.Width) ?? dictionary?.GetNInt(PdfName.W)
                ?? ((IPdfNumber)(header[PdfName.Width] ?? header[PdfName.W])).IntValue;
            var bitsPerComponent = ((IPdfNumber)imageParams[PdfName.BitsPerComponent])?.IntValue ?? 8;
            var flag = imageParams[PdfName.ImageMask] as PdfBoolean;
            var jpegOptions = new JpegOptions(decodeTransform: null, colorTransform: null);

            // Checking if values need to be transformed before conversion.
            var decodeObj = imageParams[PdfName.Decode] ?? imageParams[PdfName.D];
            var decodeArr = (PdfArray)decodeObj?.Resolve(PdfName.Decode);
            if (false && decodeArr != null)
            {
                var decode = decodeArr.ToIntArray();
                var decodeArrLength = decodeArr.Count;
                var transform = new int[decodeArr.Count];
                var transformNeeded = false;
                var maxValue = (1 << bitsPerComponent) - 1;
                for (var i = 0; i < decodeArrLength; i += 2)
                {
                    transform[i] = ((decode[i + 1] - decode[i]) * 256) | 0;
                    transform[i + 1] = (decode[i] * maxValue) | 0;
                    if (transform[i] != 256 || transform[i + 1] != 0)
                    {
                        transformNeeded = true;
                    }
                }
                if (transformNeeded)
                {
                    jpegOptions.DecodeTransform = transform;
                }
            }
            // Fetching the 'ColorTransform' entry, if it exists.
            if (parameters is PdfDictionary paramDict)
            {
                jpegOptions.ColorTransform = paramDict.GetNInt(PdfName.ColorTransform);
            }
            var jpegImage = new JpegImage(jpegOptions);

            jpegImage.Parse(data);
            var buffer = jpegImage.GetData(width: dictWidth, height: dictHeight, forceRGB: false, isSourcePDF: true);
            return buffer;
        }

        public override Memory<byte> Encode(IInputStream data, PdfDirectObject parameters, IDictionary<PdfName, PdfDirectObject> header)
        {
            throw new NotSupportedException();
        }


        bool GetMaybeValidDimensions(IInputStream data, IDictionary<PdfName, PdfDirectObject> dict)
        {
            var dictHeight = ((IPdfNumber)(dict[PdfName.Height] ?? dict[PdfName.H])).IntValue;
            var startPos = data.Position;

            var validDimensions = true;
            var foundSOF = false;
            var b = 0;
            while ((b = data.ReadByte()) != -1)
            {
                if (b != 0xff)
                {
                    // Not a valid marker.
                    continue;
                }
                switch (data.ReadByte())
                {
                    case 0xc0: // SOF0
                    case 0xc1: // SOF1
                    case 0xc2: // SOF2
                               // These three SOF{n} markers are the only ones that the built-in
                               // PDF.js JPEG decoder currently supports.
                        foundSOF = true;

                        data.Position += 2; // Skip marker length.
                        data.Position += 1; // Skip precision.
                        var scanLines = data.ReadUInt16();
                        var samplesPerLine = data.ReadUInt16();

                        // Letting the browser handle the JPEG decoding, on the main-thread,
                        // will cause a *large* increase in peak memory usage since there's
                        // a handful of short-lived copies of the image data. For very big
                        // JPEG images, always let the PDF.js image decoder handle them to
                        // reduce overall memory usage during decoding (see issue 11694).
                        if (scanLines * samplesPerLine > 1e6)
                        {
                            validDimensions = false;
                            break;
                        }

                        // The "normal" case, where the image data and dictionary agrees.
                        if (scanLines == dictHeight)
                        {
                            break;
                        }
                        // A DNL (Define Number of Lines) marker is expected,
                        // which browsers (usually) cannot decode natively.
                        if (scanLines == 0)
                        {
                            validDimensions = false;
                            break;
                        }
                        // The dimensions of the image, among other properties, should
                        // always be taken from the image data *itself* rather than the
                        // XObject dictionary. However there's cases of corrupt images that
                        // browsers cannot decode natively, for example:
                        //  - JPEG images with DNL markers, where the SOF `scanLines`
                        //    parameter has an unexpected value (see issue 8614).
                        //  - JPEG images with too large SOF `scanLines` parameter, where
                        //    the EOI marker is encountered prematurely (see issue 10880).
                        // In an attempt to handle these kinds of corrupt images, compare
                        // the dimensions in the image data with the dictionary and *always*
                        // let the PDF.js JPEG decoder (rather than the browser) handle the
                        // image if the difference is larger than one order of magnitude
                        // (since that would generally suggest that something is off).
                        if (scanLines > dictHeight * 10)
                        {
                            validDimensions = false;
                            break;
                        }
                        break;

                    case 0xc3: // SOF3
                    /* falls through */
                    case 0xc5: // SOF5
                    case 0xc6: // SOF6
                    case 0xc7: // SOF7
                    /* falls through */
                    case 0xc9: // SOF9
                    case 0xca: // SOF10
                    case 0xcb: // SOF11
                    /* falls through */
                    case 0xcd: // SOF13
                    case 0xce: // SOF14
                    case 0xcf: // SOF15
                        foundSOF = true;
                        break;

                    case 0xc4: // DHT
                    case 0xcc: // DAC
                    /* falls through */
                    case 0xda: // SOS
                    case 0xdb: // DQT
                    case 0xdc: // DNL
                    case 0xdd: // DRI
                    case 0xde: // DHP
                    case 0xdf: // EXP
                    /* falls through */
                    case 0xe0: // APP0
                    case 0xe1: // APP1
                    case 0xe2: // APP2
                    case 0xe3: // APP3
                    case 0xe4: // APP4
                    case 0xe5: // APP5
                    case 0xe6: // APP6
                    case 0xe7: // APP7
                    case 0xe8: // APP8
                    case 0xe9: // APP9
                    case 0xea: // APP10
                    case 0xeb: // APP11
                    case 0xec: // APP12
                    case 0xed: // APP13
                    case 0xee: // APP14
                    case 0xef: // APP15
                    /* falls through */
                    case 0xfe: // COM
                        var markerLength = data.ReadUInt16();
                        if (markerLength > 2)
                        {
                            data.Skip(markerLength - 2); // Jump to the next marker.
                        }
                        else
                        {
                            // The marker length is invalid, resetting the stream position.
                            data.Skip(-2);
                        }
                        break;

                    case 0xff: // Fill byte.
                               // Avoid skipping a valid marker, resetting the stream position.
                        data.Skip(-1);
                        break;

                    case 0xd9: // EOI
                        foundSOF = true;
                        break;
                }
                if (foundSOF)
                {
                    break;
                }
            }
            // Finally, don't forget to reset the stream position.
            data.Position = startPos;

            return validDimensions;
        }

    }
}