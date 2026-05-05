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

using PdfGlown.Documents.Contents.ColorSpaces;
using PdfGlown.Documents.Contents.Composition;
using PdfGlown.Documents.Contents.XObjects;
using PdfGlown.Documents.Interaction.Annotations.ControlPoints;
using PdfGlown.Objects;
using PdfGlown.Util.Math;
using SkiaSharp;
using System.Collections.Generic;
using System.Linq;

namespace PdfGlown.Documents.Interaction.Annotations
{
    /// <summary>Freehand "scribble" composed of one or more disjoint paths [PDF:1.6:8.4.5].</summary>
    [Pdf(VersionEnum.PDF13)]
    public sealed class Scribble : Markup
    {
        private IList<SKPath> paths;
        private IList<SKPath> pagePaths;
        private SKRect newbox;

        public Scribble(PdfPage page, IList<SKPath> paths, string text, DeviceColor color)
            : base(page, PdfName.Ink, new SKRect(), text)
        {
            Paths = paths;
            Color = color;
        }

        public Scribble(Dictionary<PdfName, PdfDirectObject> baseObject) 
            : base(baseObject)
        {
            paths = new List<SKPath>();
        }

        public PdfArray InkList
        {
            get => Get<PdfArray>(PdfName.InkList);
            set
            {
                var oldValue = InkList;
                if (!PdfArray.SequenceEquals(oldValue, value))
                {
                    this[PdfName.InkList] = value;
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        public IList<SKPath> PagePaths
        {
            get => pagePaths ??= GetPagePaths();
            set
            {
                ClearPath(pagePaths);
                pagePaths = value;
                var pathsObject = new PdfArrayImpl();
                newbox = SKRect.Empty;
                foreach (var path in value)
                {
                    pathsObject.Add(path.Points.ToPdfArray(ref newbox));
                }
                QueueRefreshAppearance();
                InkList = pathsObject;
                ClearPath(paths);
            }
        }

        ///<summary>Gets/Sets the coordinates of each path.</summary>
        public IList<SKPath> Paths
        {
            get => paths ??= TransformPaths(PagePaths, PageMatrix);
            set
            {
                var newPaths = new List<SKPath>();
                TransformPaths(value, newPaths, InvertPageMatrix);
                PagePaths = newPaths;
                paths = value;
            }
        }

        protected override FormXObject GenerateAppearance()
        {
            var appearence = ResetAppearance(out var zeroMatrix);
            var bound = zeroMatrix.MapRect(Box);
            var matrix = zeroMatrix;//.PostConcat(SKMatrix.CreateTranslation(0, bound.Height));
            var paint = new PrimitiveComposer(appearence);
            paint.SetStrokeColor(Color);
            paint.SetLineWidth(1);
            paint.SetLineJoin(Documents.Contents.LineJoinEnum.Round);
            Border?.Apply(paint);
            foreach (var pathData in PagePaths)
            {
                using var tempPath = new SKPath();
                pathData.Transform(matrix, tempPath);
                paint.DrawPath(tempPath);
                paint.Stroke();
            }
            paint.Flush();
            return appearence;
        }

        public override void MoveTo(SKRect newBox)
        {
            var oldBox = Box;
            InvertBorderAndEffect(ref oldBox);
            InvertBorderAndEffect(ref newBox);
            if (oldBox.Width != newBox.Width
                || oldBox.Height != newBox.Height)
            {
                QueueRefreshAppearance();
            }
            //base.MoveTo(newBox);
            var dif = SKMatrix.CreateIdentity()
                .PreConcat(SKMatrix.CreateTranslation(newBox.MidX, newBox.MidY))
                .PreConcat(SKMatrix.CreateScale(newBox.Width / oldBox.Width, newBox.Height / oldBox.Height))
                .PreConcat(SKMatrix.CreateTranslation(-oldBox.MidX, -oldBox.MidY));
            var oldPaths = PagePaths;
            var newPaths = new List<SKPath>();
            TransformPaths(oldPaths, newPaths, dif);
            PagePaths = newPaths;
            ClearPath(oldPaths);
        }

        public override void RefreshBox()
        {
            var box = newbox;
            if (newbox == SKRect.Empty)
            {
                foreach (var path in PagePaths)
                {
                    if (box == default)
                    {
                        box = path.Bounds;
                    }
                    else
                    {
                        box.Add(path.Bounds);
                    }
                }
            }
            ApplyBorderAndEffect(ref box);
            Box = box;
            newbox = default;
        }

        public override IEnumerable<ControlPoint> GetControlPoints()
        {
            foreach (var cpBase in GetDefaultControlPoint())
            {
                yield return cpBase;
            }
        }

        private IList<SKPath> GetPagePaths()
        {
            var list = new List<SKPath>();
            var pathsObject = InkList;
            for (int i = 0, c = pathsObject.Count; i < c; i++)
            {
                var pathObject = pathsObject.Get<PdfArray>(i);
                list.Add(pathObject.ToSKPath());
            }
            return list;
        }

        private void ClearPath(IList<SKPath> paths)
        {
            if (paths == null)
                return;
            var temp = paths.ToList();
            paths.Clear();
            foreach (var path in temp)
            {
                path.Dispose();
            }
        }

        private IList<SKPath> TransformPaths(IList<SKPath> fromPaths, SKMatrix sKMatrix)
        {
            var toPaths = new List<SKPath>();
            TransformPaths(fromPaths, toPaths, sKMatrix);
            return toPaths;
        }

        private void TransformPaths(IList<SKPath> fromPaths, IList<SKPath> toPaths, SKMatrix sKMatrix)
        {
            ClearPath(toPaths);
            foreach (var path in fromPaths)
            {
                var clone = new SKPath();
                path.Transform(sKMatrix, clone);


                toPaths.Add(clone);
            }
        }
    }
}