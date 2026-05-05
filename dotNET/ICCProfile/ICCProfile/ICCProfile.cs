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
using System;
using System.Collections.Generic;

namespace ICCProfile
{
    public class ICCProfile
    {
        public static readonly Dictionary<uint, Func<ICCTagTable, ICCTag>> Types = new(32)
        {
            { ICCChromaticityType.chrm, (t) => new ICCChromaticityType(t) },
            { ICCCrdInfoType.crdi, (t) => new ICCCrdInfoType(t) },
            { ICCCurveType.curv, (t) => new ICCCurveType(t) },
            { ICCDataType.data, (t) => new ICCDataType(t) },
            { ICCDateTimeType.dtim, (t) => new ICCDateTimeType(t) },
            { ICCDeviceSettingsType.devs, (t) => new ICCDeviceSettingsType(t) },
            { ICCLut16Type.mft2, (t) => new ICCLut16Type(t) },
            { ICCLut8Type.mft1, (t) => new ICCLut8Type(t) },
            { ICCMeasurementType.meas, (t) => new ICCMeasurementType(t) },
            { ICCNamedColorType.ncol, (t) => new ICCNamedColorType(t) },
            { ICCNamedColor2Type.ncl2, (t) => new ICCNamedColor2Type(t)  },
            { ICCProfileSequenceDescType.pseq, (t) => new ICCProfileSequenceDescType(t)  },
            { ICCResponseCurveSet16Type.rcs2, (t) => new ICCResponseCurveSet16Type(t)  },
            { ICCS15Fixed16ArrayType.sf32, (t) => new ICCS15Fixed16ArrayType(t)  },
            { ICCU16Fixed16ArrayType.uf32, (t) => new ICCU16Fixed16ArrayType(t)  },
            { ICCUInt16ArrayType.ui16, (t) => new ICCUInt16ArrayType(t)  },
            { ICCUInt32ArrayType.ui32, (t) => new ICCUInt32ArrayType(t)  },
            { ICCUInt64ArrayType.ui64, (t) => new ICCUInt64ArrayType(t)  },
            { ICCUInt8ArrayType.ui08, (t) => new ICCUInt8ArrayType(t)  },
            { ICCScreeningType.scrn, (t) => new ICCScreeningType(t)  },
            { ICCSignatureType.sig, (t) => new ICCSignatureType(t)  },
            { ICCTextDescriptionType.desc, (t) => new ICCTextDescriptionType(t)  },
            { ICCTextType.text, (t) => new ICCTextType(t)  },
            { ICCUcrbgType.bfd, (t) => new ICCUcrbgType(t)  },
            { ICCViewingConditionsTypee.view, (t) => new ICCViewingConditionsTypee(t)  },
            { ICCXYZType.XYZ, (t) => new ICCXYZType(t)  },

        };

        public static ICCProfile Load(Memory<byte> data)
        {
            var profile = new ICCProfile();
            var header = new ICCHeader();
            var buffer = new ByteStream(data);
            header.ProfileSize = buffer.ReadUInt32();
            header.CMMTypeSignature = buffer.ReadUInt32();
            header.ProfileVersionNumber.Major = (byte)buffer.ReadByte();
            header.ProfileVersionNumber.Minor = (byte)buffer.ReadByte();
            header.ProfileVersionNumber.Reserv1 = (byte)buffer.ReadByte();
            header.ProfileVersionNumber.Reserv2 = (byte)buffer.ReadByte();
            header.ProfileDeviceClassSignature = (ICCProfileDeviceSignatures)buffer.ReadUInt32();
            header.ColorSpaceOfData = (ICCColorSpaceSignatures)buffer.ReadUInt32();
            header.ProfileConnectionSpace = (ICCColorSpaceSignatures)buffer.ReadUInt32();
            header.DateCreated.Load(buffer);
            header.acsp = buffer.ReadUInt32();
            header.PrimaryPlatformSignature = (ICCPrimaryPlatformSignatures)buffer.ReadUInt32();
            header.Flags = (ICCProfileFlags)buffer.ReadUInt32();
            header.DeviceManufacturer = buffer.ReadUInt32();
            header.DeviceModel = buffer.ReadUInt32();
            header.DeviceAttributes.Load(buffer);
            header.RenderingIntent.Intents = buffer.ReadUInt16();
            header.RenderingIntent.Reserved = buffer.ReadUInt16();
            header.XYZ.Load(buffer);
            header.ProfileCreatorSignature = buffer.ReadUInt32();
            header.FutureUse = new byte[44];
            buffer.Read(header.FutureUse);
            profile.Header = header;
            var tagCount = buffer.ReadUInt32();
            for (int i = 0; i < tagCount; i++)
            {
                var tag = new ICCTagTable
                {
                    Signature = (ICCTagTypes)buffer.ReadUInt32(),
                    Offset = buffer.ReadUInt32(),
                    ElementSize = buffer.ReadUInt32()
                };
                profile.Tags[tag.Signature] = tag;
            }
            foreach (var tagTable in profile.Tags.Values)
            {
                buffer.Seek(tagTable.Offset);
                var key = buffer.ReadUInt32();
                if (Types.TryGetValue(key, out var type))
                {
                    tagTable.Tag = type(tagTable);
                    tagTable.Tag.Profile = profile;
                    tagTable.Tag.Load(buffer);
                }
            }
            return profile;
        }

        public float MapGrayDisplay(float g)
        {
            var trc = Tags[ICCTagTypes.grayTRCTag].Tag as ICCCurveType;
            if (trc.Values.Length > 1)
            {
                return LinearCurve(g, trc);
            }
            return g;
        }

        public float LinearCurve(float x, ICCCurveType curveType)
        {
            var gIndex = (int)Linear(x, 0, 1, 0, curveType.Values.Length);
            var value = curveType.Values[gIndex];
            return (float)Linear(value, curveType.Values[0], curveType.Values[curveType.Values.Length - 1], 0, 1);
        }

        static public double Linear(double x, double x0, double x1, double y0, double y1)
        {
            if ((x1 - x0) == 0)
            {
                return (y0 + y1) / 2;
            }
            return y0 + (x - x0) * ((y1 - y0) / (x1 - x0));
        }


        public ICCHeader Header;

        public Dictionary<ICCTagTypes, ICCTagTable> Tags { get; set; } = new Dictionary<ICCTagTypes, ICCTagTable>();
    }
}