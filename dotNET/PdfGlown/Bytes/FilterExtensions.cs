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

using PdfGlown.Bytes.Filters;
using PdfGlown.Objects;
using System.Collections.Generic;

namespace PdfGlown.Bytes
{
    public static class FilterExtensions
    {
        /// <summary>Applies the specified filter to decode the buffer.</summary>
        /// <param name="filter">Filter to use for decoding the buffer.</param>
        /// <param name="parameters">Decoding parameters.</param>
        public static ByteStream Decode(this IInputStream data, Filter filter, PdfDirectObject parameters, IDictionary<PdfName, PdfDirectObject> header)
        {
            data.Position = 0;
            return new ByteStream(filter.Decode(data, parameters, header));
        }

        /// <summary>Applies the specified filter to encode the buffer.</summary>
        /// <param name="filter">Filter to use for encoding the buffer.</param>
        /// <param name="parameters">Encoding parameters.</param>
        /// <returns>Encoded buffer.</returns>
        public static ByteStream Encode(this IInputStream data, Filter filter, PdfDirectObject parameters, IDictionary<PdfName, PdfDirectObject> header)
        {
            data.Position = 0;
            return new ByteStream(filter.Encode(data, parameters, header));
        }

        public static IInputStream Decode(this IInputStream buffer, PdfDirectObject filter, PdfDirectObject parameters, IDictionary<PdfName, PdfDirectObject> header)
        {
            if (filter == null)
            {
                return buffer;
            }
            if (filter is PdfName name) // Single filter.
            {
                buffer = buffer.Decode(Filter.Get(name), (PdfDictionary)parameters, header);
            }
            else // Multiple filters.
            {
                using var filterIterator = ((PdfArray)filter).GetItems().GetEnumerator();
                IEnumerator<PdfDirectObject> parametersIterator = (parameters != null ? ((PdfArray)parameters).GetItems().GetEnumerator() : null);
                while (filterIterator.MoveNext())
                {
                    PdfDictionary filterParameters;
                    if (parametersIterator == null)
                    { filterParameters = null; }
                    else
                    {
                        parametersIterator.MoveNext();
                        filterParameters = (PdfDictionary)parametersIterator.Current?.Resolve(PdfName.DecodeParms);
                    }
                    buffer = buffer.Decode(Filter.Get((PdfName)filterIterator.Current?.Resolve(PdfName.Filter)), filterParameters, header);
                }
            }
            return buffer;
        }
    }
}