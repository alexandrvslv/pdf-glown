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
    public enum ICCTagTypes : uint
    {
        AToB0Tag = 0x41324230,// Multidimensional transformation structure
        AToB1Tag = 0x41324231,//Multidimensional transformation structure
        AToB2Tag = 0x41324232,//Multidimensional transformation structure
        blueColorantTag = 0x6258595A,//Relative XYZ values of blue phosphor or colorant
        blueTRCTag = 0x62545243,//Blue channel tone reproduction curve
        BToA0Tag = 0x42324130,//Multidimensional transformation structure
        BToA1Tag = 0x42324131,//Multidimensional transformation structure
        BToA2Tag = 0x42324132,//Multidimensional transformation structure
        calibrationDateTimeTag = 0x63616C74,//Profile calibration date and time
        charTargetTag = 0x74617267,//Characterization target such as IT8/7.2
        chromaticAdaptationTag = 0x63686164,//Multidimensional transform for non-D50 adaptation
        chromaticityTag = 0x6368726D,//Set of phosphor/colorant chromaticity
        copyrightTag = 0x63707274,//7-bit ASCII profile copyright information
        crdInfoTag = 0x63726469,//Names of companion CRDs to the profile
        deviceMfgDescTag = 0x646D6E64,//Displayable description of device manufacturer
        deviceModelDescTag = 0x646D6464,//Displayable description of device model
        deviceSettingsTagSpecifies = 0x64657673,// the device settings for which the profile is valid
        gamutTag = 0x67616D74,//Out of gamut: 8-bit or 16-bit data 
        grayTRCTag = 0x6B545243,//Gray tone reproduction curve
        greenColorantTag = 0x6758595A,//Relative XYZ values of green phosphor or colorant
        greenTRCTag = 0x67545243,//Green channel tone reproduction curve
        luminanceTag = 0x6C756D69,//Absolute luminance for emissive device
        measurementTag = 0x6D656173,//Alternative measurement specification information
        mediaBlackPointTag = 0x626B7074,//Media XYZ black point 
        mediaWhitePointTag = 0x77747074,//Media XYZ white point
        namedColorTag = 0x6E636F6C,//
        namedColor2Tag = 0x6E636C32,//
        outputResponseTag = 0x72657370,//Description of the desired device response
        preview0Tag = 0x70726530,//Preview transformation: 8-bit or 16-bit data
        preview1Tag = 0x70726531,//Preview transformation: 8-bit or 16-bit data
        preview2Tag = 0x70726532,//Preview transformation: 8-bit or 16-bit data
        profileDescriptionTag = 0x64657363,//Profile description for display
        profileSequenceDescTag = 0x70736571,//Profile sequence description from source to destination
        ps2CRD0Tag = 0x70736430,//PostScript Level 2 color rendering dictionary: perceptual
        ps2CRD1Tag = 0x70736431,//PostScript Level 2 color rendering dictionary: colorimetric
        ps2CRD2Tag = 0x70736432,//PostScript Level 2 color rendering dictionary: saturation
        ps2CRD3Tag = 0x70736433,//PostScript Level 2 color rendering dictionary: ICC-absolute
        ps2CSATag = 0x70733273,//PostScript Level 2 color space array
        ps2RenderingIntentTag = 0x70733269,//PostScript Level 2 Rendering Intent
        redColorantTag = 0x7258595A,//Relative XYZ values of red phosphor or colorant
        redTRCTag = 0x72545243,//Red channel tone reproduction curve
        screeningDescTag = 0x73637264,//Screening attributes description 
        screeningTag = 0x7363726E,//Screening attributes such as frequency, angle and spot shape 
        technologyTag = 0x74656368,//Device technology information such as LCD, CRT, Dye Sub-limation, etc.
        ucrbgTag = 0x62666420,//Under color removal and black generation description
        viewingCondDescTag = 0x76756564,//Viewing condition description
        viewingConditionsTag = 0x76696577,//Viewing condition parameters
    }



}