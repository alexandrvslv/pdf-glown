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

using System.Runtime.InteropServices;

namespace ICCProfile
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
    public struct ICCHeader
    {
        public uint ProfileSize;
        public uint CMMTypeSignature;
        public ICCProfileVersion ProfileVersionNumber;
        public ICCProfileDeviceSignatures ProfileDeviceClassSignature;
        public ICCColorSpaceSignatures ColorSpaceOfData;
        public ICCColorSpaceSignatures ProfileConnectionSpace;
        public ICCDateTime DateCreated;
        public uint acsp;// 0x61637370
        public ICCPrimaryPlatformSignatures PrimaryPlatformSignature;
        public ICCProfileFlags Flags;
        public uint DeviceManufacturer;
        public uint DeviceModel;
        public ICCProfileAttribute DeviceAttributes;
        public ICCRenderingIntent RenderingIntent;
        public ICCXYZNumber XYZ;
        public uint ProfileCreatorSignature;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 44)]
        public byte[] FutureUse;
    }



}