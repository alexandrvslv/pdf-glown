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

using PdfGlown.Bytes.Filters.Jpx;
using PdfGlown.Objects;
using System;
using System.Collections.Generic;

namespace PdfGlown.Bytes.Filters
{
    public sealed class JPXFilter : Filter
    {
        internal JPXFilter()
        { }

        public override Memory<byte> Decode(IInputStream data, PdfDirectObject parameters, IDictionary<PdfName, PdfDirectObject> header)
        {
            var imageParams = header;
            //var width = imageParams.Resolve(PdfName.Width) as PdfInteger;
            //var height = imageParams.Resolve(PdfName.Height) as PdfInteger;
            var bpp = imageParams[PdfName.BitsPerComponent] as PdfInteger;
            var flag = imageParams[PdfName.ImageMask] as PdfBoolean;
            var jpxImage = new JpxImage();
            jpxImage.Parse(data);

            var width = jpxImage.width;
            var height = jpxImage.height;
            var componentsCount = jpxImage.componentsCount;
            var tileCount = jpxImage.tiles.Count;
            var buffer = (byte[])null;
            if (tileCount == 1)
            {
                buffer = jpxImage.tiles[0].items;
            }
            else
            {
                buffer = new byte[width * height * componentsCount];

                for (var k = 0; k < tileCount; k++)
                {
                    var tileComponents = jpxImage.tiles[k];
                    var tileWidth = tileComponents.Width;
                    var tileHeight = tileComponents.Height;
                    var tileLeft = tileComponents.Left;
                    var tileTop = tileComponents.Top;

                    var src = tileComponents.items.AsSpan();
                    var srcPosition = 0;
                    var dataPosition = (width * tileTop + tileLeft) * componentsCount;
                    var imgRowSize = width * componentsCount;
                    var tileRowSize = tileWidth * componentsCount;

                    for (var j = 0; j < tileHeight; j++)
                    {
                        var rowBytes = src.Slice(srcPosition, tileRowSize);
                        rowBytes.CopyTo(buffer.AsSpan(dataPosition));
                        srcPosition += tileRowSize;
                        dataPosition += imgRowSize;
                    }
                }
            }
            return buffer;
        }

        public override Memory<byte> Encode(IInputStream data, PdfDirectObject parameters, IDictionary<PdfName, PdfDirectObject> header)
        {
            throw new NotSupportedException();
        }
    }
}