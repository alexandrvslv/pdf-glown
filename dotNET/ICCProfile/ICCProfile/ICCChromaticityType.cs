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
    public class ICCChromaticityType : ICCTag
    {
        public ICCChromaticityType(ICCTagTable table) : base(table)
        {
        }
        public const uint chrm = 0x6368726D;
        public uint Reserved = 0x00000000;
        public ushort NumberOfDeviceChannels;
        public ICCPhosphorOrColorantTypes PhosphorOrColorantType;
        public XYCoord Channel1;
        public XYCoord Channel2;
        public XYCoord Channel3;
        public XYCoord[] OtherChannels;

        public override void Load(ByteStream buffer)
        {
            buffer.Seek(Table.Offset);
            buffer.ReadUInt32();
            buffer.ReadUInt32();
            NumberOfDeviceChannels = buffer.ReadUInt16();
            PhosphorOrColorantType = (ICCPhosphorOrColorantTypes)buffer.ReadUInt16();
            Channel1.X = buffer.ReadFixed32();
            Channel1.Y = buffer.ReadFixed32();
            Channel2.X = buffer.ReadFixed32();
            Channel2.Y = buffer.ReadFixed32();
            Channel3.X = buffer.ReadFixed32();
            Channel3.Y = buffer.ReadFixed32();

        }
    }
}