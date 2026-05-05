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
    public class ICCLut16Type : ICCTag
    {
        public ICCLut16Type(ICCTagTable table) : base(table)
        {            
        }
        public const uint mft2 = 0x6D667432;
        public uint Reserved = 0x00000000;
        public byte NumberOfInputChannels;
        public byte NumberOfOutputChannels;
        public byte NumberOfCLUTGridPoints;
        public byte ReservedForPadding = 0x00;
        public float Encoded00;
        public float Encoded01;
        public float Encoded02;
        public float Encoded10;
        public float Encoded11;
        public float Encoded12;
        public float Encoded20;
        public float Encoded21;
        public float Encoded22;
        public ushort NumberOfInputTables;
        public ushort NumberOfOutputTables;
        public ushort[] InputTables;
        public ushort[] CLUTValues;
        public ushort[] OutputTables;
        public override void Load(ByteStream buffer)
        {
            buffer.Seek(Table.Offset);
            buffer.ReadUInt32();
            buffer.ReadUInt32();
            NumberOfInputChannels = (byte)buffer.ReadByte();
            NumberOfOutputChannels = (byte)buffer.ReadByte();
            NumberOfCLUTGridPoints = (byte)buffer.ReadByte();
            buffer.ReadByte();
            Encoded00 = buffer.ReadFixed32();
            Encoded01 = buffer.ReadFixed32();
            Encoded02 = buffer.ReadFixed32();
            Encoded10 = buffer.ReadFixed32();
            Encoded11 = buffer.ReadFixed32();
            Encoded12 = buffer.ReadFixed32();
            Encoded20 = buffer.ReadFixed32();
            Encoded21 = buffer.ReadFixed32();
            Encoded22 = buffer.ReadFixed32();
            NumberOfInputTables = buffer.ReadUInt16();
            NumberOfOutputTables = buffer.ReadUInt16();
            InputTables = new ushort[NumberOfInputTables];
            for (int i = 0; i < NumberOfInputTables; i++)
            {
                InputTables[i] = buffer.ReadUInt16();
            }
            CLUTValues = new ushort[NumberOfCLUTGridPoints];
            for (int i = 0; i < NumberOfCLUTGridPoints; i++)
            {
                CLUTValues[i] = buffer.ReadUInt16();
            }
            OutputTables = new ushort[NumberOfOutputTables];
            for (int i = 0; i < NumberOfOutputTables; i++)
            {
                OutputTables[i] = buffer.ReadUInt16();
            }
        }
    }
}