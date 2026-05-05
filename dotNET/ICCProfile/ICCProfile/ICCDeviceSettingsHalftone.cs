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
    public enum ICCDeviceSettingsHalftone : uint
    {
        DMDITHER_NONE = 1,
        DMDITHER_COARSE = 2,
        DMDITHER_FINE = 3,
        DMDITHER_LINEART = 4,
        DMDITHER_ERRORDIFFUSION = 5,
        DMDITHER_RESERVED6 = 6,
        DMDITHER_RESERVED7 = 7,
        DMDITHER_RESERVED8 = 8,
        DMDITHER_RESERVED9 = 9,
        DMDITHER_GRAYSCALE = 10,
        DMDITHER_USER = 256
    }
}