/*
  Copyright 2006-2013 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Stefano Chizzolini (original code developer, http://www.stefanochizzolini.it):
      - porting and adaptation (extension to any bit depth other than 8) of [JT]
        predictor-decoding implementation.
    * Joshua Tauberer (code contributor, http://razor.occams.info):
      - predictor-decoding contributor on .NET implementation.
    * Jean-Claude Truy (bugfix contributor): [FIX:0.0.8:JCT].

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

using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using PdfGlown.Objects;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace PdfGlown.Bytes.Filters
{
    /// <summary>zlib/deflate [RFC:1950,1951] filter [PDF:1.6:3.3.3].</summary>
    [Pdf(VersionEnum.PDF12)]
    public class FlateFilter : Filter
    {
        private const int BufferSize = 4 * 1024;
        internal FlateFilter()
        { }

        public override Memory<byte> Decode(IInputStream data, PdfDirectObject parameters, IDictionary<PdfName, PdfDirectObject> header)
        {
            using var outputStream = new MemoryStream();
            try
            {
                using var inputFilter = new InflaterInputStream((Stream)data);
                Transform(inputFilter, outputStream);
                inputFilter.Close();
            }
            catch (ICSharpCode.SharpZipLib.SharpZipBaseException)
            {
                outputStream.Reset();
                data.Position = 0;
                using var inputFilter = new DeflateStream((Stream)data, CompressionMode.Decompress);
                Transform(inputFilter, outputStream);
                inputFilter.Close();
            }
            return DecodePredictor(outputStream, parameters, header);
        }

        public override Memory<byte> Encode(IInputStream data, PdfDirectObject parameters, IDictionary<PdfName, PdfDirectObject> header)
        {
            data.Position = 0;
            using var outputStream = new MemoryStream();
            using var outputFilter = new DeflaterOutputStream(outputStream);
            // Add zlib's 2-byte header [RFC 1950] [FIX:0.0.8:JCT]!
            //outputStream.WriteByte(0x78); // CMF = {CINFO (bits 7-4) = 7; CM (bits 3-0) = 8} = 0x78.
            //outputStream.WriteByte(0xDA); // FLG = {FLEVEL (bits 7-6) = 3; FDICT (bit 5) = 0; FCHECK (bits 4-0) = {31 - ((CMF * 256 + FLG - FCHECK) Mod 31)} = 26} = 0xDA.
            Transform((Stream)data, outputFilter);
            outputFilter.Flush();
            outputFilter.Finish();
            return outputStream.AsMemory();
        }

        protected Memory<byte> DecodePredictor(Stream input, PdfDirectObject parameters, IDictionary<PdfName, PdfDirectObject> header)
        {
            if (parameters is not PdfDictionary dictionary)
                return input.AsMemory();

            int predictor = dictionary.GetInt(PdfName.Predictor, 1);
            if (predictor == 1) // No predictor was applied during data encoding.
                return input.AsMemory();

            int sampleComponentBitsCount = dictionary.GetInt(PdfName.BitsPerComponent, 8);
            int sampleComponentsCount = dictionary.GetInt(PdfName.Colors, 1);
            int rowSamplesCount = dictionary.GetInt(PdfName.Columns, 1);

            input.Position = 0;
            var output = new MemoryStream();
            {
                switch (predictor)
                {
                    case 2: // TIFF Predictor 2 (component-based).
                        {
                            Span<int> sampleComponentPredictions = stackalloc int[sampleComponentsCount];
                            int sampleComponentDelta = 0;
                            int sampleComponentIndex = 0;
                            while ((sampleComponentDelta = input.ReadByte()) != -1)
                            {
                                int sampleComponent = sampleComponentDelta + sampleComponentPredictions[sampleComponentIndex];
                                output.WriteByte((byte)sampleComponent);

                                sampleComponentPredictions[sampleComponentIndex] = sampleComponent;

                                sampleComponentIndex = ++sampleComponentIndex % sampleComponentsCount;
                            }
                            break;
                        }
                    default: // PNG Predictors [RFC 2083] (byte-based).
                        {
                            int sampleBytesCount = (int)Math.Ceiling(sampleComponentBitsCount * sampleComponentsCount / 8d); // Number of bytes per pixel (bpp).
                            int rowSampleBytesCount = (int)Math.Ceiling(sampleComponentBitsCount * sampleComponentsCount * rowSamplesCount / 8d) + sampleBytesCount; // Number of bytes per row (comprising a leading upper-left sample (see Paeth method)).
                            Span<int> previousRowBytePredictions = stackalloc int[rowSampleBytesCount];
                            Span<int> currentRowBytePredictions = stackalloc int[rowSampleBytesCount];
                            Span<int> leftBytePredictions = stackalloc int[sampleBytesCount];
                            int predictionMethod;
                            while ((predictionMethod = input.ReadByte()) != -1)
                            {
                                currentRowBytePredictions.CopyTo(previousRowBytePredictions);
                                leftBytePredictions.Fill(0);
                                // Starts after the leading upper-left sample (see Paeth method).
                                for (int rowIndex = sampleBytesCount; rowIndex < rowSampleBytesCount; rowIndex++)
                                {
                                    int byteDelta = input.ReadByte();

                                    int sampleByteIndex = rowIndex % sampleBytesCount;

                                    int sampleByte;
                                    switch (predictionMethod)
                                    {
                                        case 0: // None (no prediction).
                                            sampleByte = byteDelta;
                                            break;
                                        case 1: // Sub (predicts the same as the sample to the left).
                                            sampleByte = byteDelta + leftBytePredictions[sampleByteIndex];
                                            break;
                                        case 2: // Up (predicts the same as the sample above).
                                            sampleByte = byteDelta + previousRowBytePredictions[rowIndex];
                                            break;
                                        case 3: // Average (predicts the average of the sample to the left and the sample above).
                                            sampleByte = byteDelta + (int)Math.Floor(((leftBytePredictions[sampleByteIndex] + previousRowBytePredictions[rowIndex])) / 2d);
                                            break;
                                        case 4: // Paeth (a nonlinear function of the sample above, the sample to the left, and the sample to the upper left).
                                            {
                                                int paethPrediction;
                                                {
                                                    int leftBytePrediction = leftBytePredictions[sampleByteIndex];
                                                    int topBytePrediction = previousRowBytePredictions[rowIndex];
                                                    int topLeftBytePrediction = previousRowBytePredictions[rowIndex - sampleBytesCount];
                                                    int initialPrediction = leftBytePrediction + topBytePrediction - topLeftBytePrediction;
                                                    int leftPrediction = Math.Abs(initialPrediction - leftBytePrediction);
                                                    int topPrediction = Math.Abs(initialPrediction - topBytePrediction);
                                                    int topLeftPrediction = Math.Abs(initialPrediction - topLeftBytePrediction);
                                                    if (leftPrediction <= topPrediction
                                                      && leftPrediction <= topLeftPrediction)
                                                    { paethPrediction = leftBytePrediction; }
                                                    else if (topPrediction <= topLeftPrediction)
                                                    { paethPrediction = topBytePrediction; }
                                                    else
                                                    { paethPrediction = topLeftBytePrediction; }
                                                }
                                                sampleByte = byteDelta + paethPrediction;
                                                break;
                                            }
                                        default:
                                            throw new NotSupportedException("Prediction method " + predictionMethod + " unknown.");
                                    }
                                    output.WriteByte((byte)sampleByte);

                                    leftBytePredictions[sampleByteIndex] = currentRowBytePredictions[rowIndex] = (byte)sampleByte;
                                }
                            }
                            break;
                        }
                }
                return output.AsMemory();
            }
        }

        protected static void Transform(Stream input, Stream output)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            int bufferRead;
            while ((bufferRead = input.Read(buffer, 0, buffer.Length)) != 0)
            {
                output.Write(buffer, 0, bufferRead);
            }
            output.Flush();
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}