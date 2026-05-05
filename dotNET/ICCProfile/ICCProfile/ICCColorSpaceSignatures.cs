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

namespace ICCProfile
{
    public enum ICCColorSpaceSignatures : uint
    {
        XYZ = 0x58595A20,
        Lab = 0x4C616220,
        Luv = 0x4C757620,
        YCbCr = 0x59436272,
        Yxy = 0x59787920,
        RGB = 0x52474220,
        GRAY = 0x47524159,
        HSV = 0x48535620,
        HLS = 0x484C5320,
        CMYK = 0x434D594B,
        CMY = 0x434D5920,
        Colors2 = 0x32434C52,
        Colors3 = 0x33434C52,
        Colors4 = 0x34434C52,
        Colors5 = 0x35434C52,
        Colors6 = 0x36434C52,
        Colors7 = 0x37434C52,
        Colors8 = 0x38434C52,
        Colors9 = 0x39434C52,
        Colors10 = 0x41434C52,
        Colors11 = 0x42434C52,
        Colors12 = 0x43434C52,
        Colors13 = 0x44434C52,
        Colors14 = 0x45434C52,
        Colors15 = 0x46434C52
    }



}