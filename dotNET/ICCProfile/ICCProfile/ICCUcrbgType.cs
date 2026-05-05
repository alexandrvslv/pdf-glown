/*
  Copyright 2006-2011 Stefano Chizzolini. http://www.pdfclown.org

  Contributors:
    * Alexandr Vassilyev (alexandr_vslv@mail.ru)

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

namespace ICCProfile
{
    public class ICCUcrbgType : ICCTag
    {
        public ICCUcrbgType(ICCTagTable table) : base(table)
        {
        }

        public const uint bfd = 0x62666420;
        public uint Reserved = 0x00000000;
        public uint CountUCRCurves;
        public ushort[] UCRCurves;
        public uint CountBGCurves;
        public ushort[] BGCurves;
        public string Charactes;

        public override void Load(ByteStream buffer)
        {
            buffer.Seek(Table.Offset);
            buffer.ReadUInt32();
            buffer.ReadUInt32();

            CountUCRCurves = buffer.ReadUInt32();
            UCRCurves = new ushort[CountUCRCurves];
            for (int i = 0; i < CountUCRCurves; i++)
            {
                UCRCurves[i] = buffer.ReadUInt16();
            }

            CountBGCurves = buffer.ReadUInt32();
            BGCurves = new ushort[CountBGCurves];
            for (int i = 0; i < CountBGCurves; i++)
            {
                BGCurves[i] = buffer.ReadUInt16();
            }

            Charactes = System.Text.Encoding.ASCII.GetString(buffer.ReadNullTermitadedSpan());
        }
    }
}