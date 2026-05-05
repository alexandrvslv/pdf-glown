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

using PdfGlown.Objects;
using SkiaSharp;

namespace PdfGlown.Documents.Interaction.Annotations
{
    public sealed partial class FreeText
    {
        /// <summary>Callout line [PDF:1.6:8.4.5].</summary>
        public class CalloutLine : PdfObjectWrapper<PdfArray>
        {
            public static CalloutLine Wrap(PdfArray array, FreeText text) => array == null ? null
                : new CalloutLine(array) { FreeText = text };

            private SKPoint? end;
            private SKPoint? knee;
            private SKPoint? start;

            public CalloutLine(PdfPage page, SKPoint start, SKPoint end)
                : this(page, start, null, end)
            { }

            public CalloutLine(PdfPage page, SKPoint start, SKPoint? knee, SKPoint end)
                : base(new PdfArrayImpl())
            {
                SKMatrix matrix = page.InvertRotateMatrix;
                PdfArray baseDataObject = DataObject;
                {
                    start = matrix.MapPoint(start);
                    baseDataObject.Add(start.X);
                    baseDataObject.Add(start.Y);
                    if (knee.HasValue)
                    {
                        knee = matrix.MapPoint(knee.Value);
                        baseDataObject.Add(knee.Value.X);
                        baseDataObject.Add(knee.Value.Y);
                    }
                    end = matrix.MapPoint(end);
                    baseDataObject.Add(end.X);
                    baseDataObject.Add(end.Y);
                }
            }

            public CalloutLine(PdfDirectObject baseObject) : base(baseObject)
            { }

            public SKPoint End
            {
                get
                {
                    return end ??= DataObject is PdfArray coordinates
                        ? coordinates.Count < 6
                            ? new SKPoint(
                            coordinates.GetFloat(2),
                            coordinates.GetFloat(3))
                            : new SKPoint(
                            coordinates.GetFloat(4),
                            coordinates.GetFloat(5))
                       : SKPoint.Empty;
                }
                set
                {
                    if (End != value)
                    {
                        SetEnd(value);
                        FreeText.QueueRefreshAppearance();
                    }
                }
            }

            internal void SetEnd(SKPoint value)
            {
                end = value;
                PdfArray coordinates = DataObject;
                if (coordinates.Count < 6)
                {
                    coordinates.Set(2, value.X);
                    coordinates.Set(3, value.Y);
                }
                else
                {
                    coordinates.Set(4, value.X);
                    coordinates.Set(5, value.Y);
                }
                FreeText.OnPropertyChanged(FreeText.Callout, FreeText.Callout, nameof(FreeText.Callout));
            }

            public SKPoint? Knee
            {
                get => knee ??= DataObject is PdfArray coordinates
                        ? coordinates.Count < 6
                            ? null
                            : new SKPoint(coordinates.GetFloat(2), coordinates.GetFloat(3))
                            : SKPoint.Empty;
                set
                {
                    if (Knee != value)
                    {
                        SetKnee(value);
                        FreeText.QueueRefreshAppearance();
                    }
                }
            }

            internal void SetKnee(SKPoint? value)
            {
                knee = value;
                PdfArray coordinates = DataObject;
                if (value is SKPoint val)
                {
                    coordinates.Set(2, val.X);
                    coordinates.Set(3, val.Y);
                }
                FreeText.OnPropertyChanged(FreeText.Callout, FreeText.Callout, nameof(FreeText.Callout));
            }

            public SKPoint Start
            {
                get
                {
                    return start ??= DataObject is PdfArray coordinates
                        ? new SKPoint(
                      coordinates.GetFloat(0),
                      coordinates.GetFloat(1))
                        : SKPoint.Empty;
                }
                set
                {
                    if (Start != value)
                    {
                        SetStart(value);
                        FreeText.QueueRefreshAppearance();
                    }
                }
            }

            internal void SetStart(SKPoint value)
            {
                start = value;
                PdfArray coordinates = DataObject;
                coordinates.Set(0, value.X);
                coordinates.Set(1, value.Y);
                FreeText.OnPropertyChanged(FreeText.Callout, FreeText.Callout, nameof(FreeText.Callout));

            }

            public FreeText FreeText { get; internal set; }
        }
    }
}