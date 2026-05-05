/*
  Copyright 2007-2011 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfGlown.Documents.Contents.XObjects;
using PdfGlown.Objects;
using PdfGlown.Tools;
using SkiaSharp;
using System;

namespace PdfGlown.Documents.Contents.Objects
{
    /// <summary>'Paint the specified XObject' operation [PDF:1.6:4.7].</summary>
    [Pdf(VersionEnum.PDF10)]
    public sealed class PaintXObject : Operation, IResourceReference<XObject>, IBoxed
    {
        public static readonly string OperatorKeyword = "Do";

        public PaintXObject(PdfName name) : base(OperatorKeyword, name)
        { }

        public PaintXObject(PdfArray operands) : base(OperatorKeyword, operands)
        { }

        public PdfName Name
        {
            get => (PdfName)operands.Get(0);
            set => operands.SetSimple(0, value);
        }

        /// <summary>Gets the scanner for the contents of the painted external object.</summary>
        /// <param name="context">Scanning context.</param>
        public ContentScanner GetScanner(ContentScanner context)
        {
            XObject xObject = GetResource(context);
            return xObject is FormXObject form
              ? new ContentScanner(form, context)
              : null;
        }

        /// <summary>Gets the <see cref="XObject">external object</see> resource to be painted.
        /// </summary>
        /// <param name="context">Content context.</param>
        public XObject GetResource(ContentScanner scanner)
        {
            var pscanner = scanner;
            XObject xobj;

            while ((xobj = pscanner.Context.Resources.XObjects[Name]) == null
                && (pscanner = pscanner.ResourceParent) != null)
            { }
            return xobj;
        }

        public override void Scan(GraphicsState state)
        {
            var scanner = state.Scanner;
            var xObject = GetResource(scanner);
            var ctm = state.Ctm;
            var size = xObject.Size;

            var canvas = scanner.Canvas;
            if (canvas == null)
                return;
            try
            {
                canvas.Save();
                if (xObject is ImageXObject imageObject)
                {
                    var image = imageObject.Load(state);
                    if (image != null)
                    {
                        var imageMatrix = imageObject.Matrix;
                        imageMatrix.ScaleY *= -1;
#if NET9_0_OR_GREATER
                        canvas.Concat(in imageMatrix);
#else
                        canvas.Concat(ref imageMatrix);
#endif

                        if (imageObject.ImageMask)
                        {
                            using var paint = state.CreateFillPaint();
                            canvas.DrawImage(image, 0, -size.Height, paint);
                        }
                        else
                        {
                            using var paint = state.CreateFillPaint();
                            canvas.DrawImage(image, 0, -size.Height, paint);
                        }
                    }
                }
                else if (xObject is FormXObject formObject)
                {
                    var picture = formObject.Render(scanner);

                    ctm = ctm.PreConcat(formObject.Matrix);
                    canvas.SetMatrix(ctm);
                    //canvas.ClipRect(formObject.Box);
                    using (var paint = state.CreateFillPaint())
                    {
                        canvas.DrawPicture(picture, paint);
                    }

                    foreach (var textBlock in formObject.TextBlocks)
                    {
                        scanner.Context.TextBlocks.Add(TextBlock.Transform(textBlock, ctm));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Some ({ex}) trouble with {xObject}");
            }
            finally
            {
                canvas.Restore();
            }
        }

        public SKRect GetBox(GraphicsState state)
        {
            var xObject = GetResource(state.Scanner);
            var ctm = state.Ctm.PreConcat(xObject.Matrix);
            var size = xObject.Size;
            var mappedSize = ctm.MapVector(size.Width, size.Height);
            return SKRect.Create(ctm.TransX, ctm.TransY,
                          mappedSize.X, mappedSize.Y);
        }
    }
}
