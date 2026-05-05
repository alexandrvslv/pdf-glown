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
    public class ICCCurveStructure
    {
        public ICCCurveMeasurementEncodings MesurementUnit;
        public uint[] Counts;
        public ICCXYZNumber[] Measurments;
        public ICCResponse16Number[] Responces;

        public void Load(ByteStream buffer, ushort numberOfChannels)
        {
            MesurementUnit = (ICCCurveMeasurementEncodings)buffer.ReadUInt32();
            Counts = new uint[numberOfChannels];
            Measurments = new ICCXYZNumber[numberOfChannels];
            Responces = new ICCResponse16Number[numberOfChannels];
            for (int i = 0; i < numberOfChannels; i++)
            {
                Counts[i] = buffer.ReadUInt32();
            }
            for (int i = 0; i < numberOfChannels; i++)
            {
                Measurments[i].Load(buffer);
            }
            for (int i = 0; i < numberOfChannels; i++)
            {
                Responces[i].Load(buffer);
            }
        }
    }
}