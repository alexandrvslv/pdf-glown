/*
  Copyright 2008-2015 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfGlown.Objects;
using PdfGlown.Util;
using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Interaction.Forms.Signature
{
    public class SignatureDictionary : PdfDictionary
    {
        private int[] byteRange;
        private PropBuild propBuild;

        public SignatureDictionary()
            : base(new Dictionary<PdfName, PdfDirectObject>(){
                { PdfName.Type, PdfName.Sig },
            })
        { }

        public SignatureDictionary(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        {
            if (Type == null)
                this[PdfName.Type] = PdfName.Sig;
        }

        public string Type
        {
            get => GetString(PdfName.Type);
            set => SetName(PdfName.Type, value);
        }

        public string Filter
        {
            get => GetString(PdfName.Filter);
            set => SetName(PdfName.Filter, value);
        }

        public string SubFilter
        {
            get => GetString(PdfName.SubFilter);
            set => SetName(PdfName.SubFilter, value);
        }

        public int[] ByteRange
        {
            get => byteRange ??= GetOrCreate<PdfArrayImpl>(PdfName.ByteRange)?.ToIntArray();
            set
            {
                byteRange = value;
                SetDirect(PdfName.ByteRange, value != null ? new PdfArrayImpl(value) : null);
            }
        }

        public Memory<byte> Contents
        {
            get => Get<PdfString>(PdfName.Contents)?.AsMemory() ?? Memory<byte>.Empty;
            set => this[PdfName.Contents] = value.IsEmpty ? null : new PdfByteString(value);
        }

        public PdfDirectObject Cert
        {
            get => Get(PdfName.Cert);
            set => Set(PdfName.Cert, value);
        }

        public DateTime? DateM
        {
            get => GetNDate(PdfName.M);
            set => Set(PdfName.M, value);
        }

        public string Name
        {
            get => GetString(PdfName.Name);
            set => SetText(PdfName.Name, value);
        }

        public string Location
        {
            get => GetString(PdfName.Location);
            set => SetText(PdfName.Location, value);
        }

        public string Reason
        {
            get => GetString(PdfName.Reason);
            set => SetText(PdfName.Reason, value);
        }

        public PdfArray Ref
        {
            get => GetOrCreate<PdfArrayImpl>(PdfName.Reference);
            set => SetDirect(PdfName.Reference, value);
        }

        public PdfArray Changes
        {
            get => GetOrCreate<PdfArrayImpl>(PdfName.Changes);
            set => SetDirect(PdfName.Changes, value);
        }

        public PropBuild PropBuild
        {
            get => propBuild ??= new PropBuild(GetOrCreateInderect<PdfDictionary>(PdfName.Prop_Build));
            set => Set(PdfName.Prop_Build, propBuild = value);
        }

        /// <summary>
        /// Will return the embedded signature between the byterange gap.
        /// </summary>
        /// <param name="pdfFile">The signed pdf file as InputStream. It will be closed in this method.</param>
        /// <returns>a byte array containing the signature</returns>
        public Memory<byte> GetContents(IInputStream pdfFile)
        {
            var byteRange = ByteRange;
            int begin = byteRange[0] + byteRange[1] + 1;
            int len = byteRange[2] - begin;

            using var input = new ByteStream(pdfFile, begin, len);
            return GetConvertedContents(input);
        }

        /// <summary>
        /// Will return the embedded signature between the byterange gap.
        /// </summary>
        /// <param name="pdfFile">The signed pdf file as byte array</param>
        /// <returns>a byte array containing the signature</returns>
        public Memory<byte> GetContents(Memory<byte> pdfFile)
        {
            var byteRange = ByteRange;
            int begin = byteRange[0] + byteRange[1] + 1;
            int len = byteRange[2] - begin - 1;

            using var input = new ByteStream(pdfFile.Slice(begin, len));
            return GetConvertedContents(input);
        }

        private Memory<byte> GetConvertedContents(IInputStream input)
        {
            using var output = new ByteStream((int)input.Length / 2);
            Span<byte> buffer = stackalloc byte[2];
            while (input.IsAvailable)
            {
                var b = input.PeekByte();
                // Filter < and (
                if (b == 0x3C || b == 0x28)
                {
                    input.Skip(1);
                }
                // Filter > and ) at the end
                if (b == -1 || b == 0x3E || b == 0x29)
                {
                    break;
                }
                if (input.Read(buffer) == 2)
                    output.WriteByte(ConvertUtils.ReadHexByte(buffer));
            }
            return output.AsMemory();
        }
    }
}