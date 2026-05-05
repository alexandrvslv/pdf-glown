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

using PdfGlown.Bytes;
using PdfGlown.Documents.Contents;
using PdfGlown.Documents.Contents.ColorSpaces;
using PdfGlown.Documents.Contents.Layers;
using PdfGlown.Documents.Contents.Objects;
using PdfGlown.Documents.Contents.Tokens;
using PdfGlown.Documents.Contents.XObjects;
using PdfGlown.Documents.Interaction.Actions;
using PdfGlown.Documents.Interaction.Annotations.ControlPoints;
using PdfGlown.Objects;
using PdfGlown.Util;
using PdfGlown.Util.Math;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
//using System.Diagnostics;

namespace PdfGlown.Documents.Interaction.Annotations
{
    /// <summary>Annotation [PDF:1.6:8.4].</summary>
    [Pdf(VersionEnum.PDF10)]
    public abstract class Annotation : PdfDictionary, ILayerable, INotifyPropertyChanged
    {
        private static readonly Dictionary<PdfName, Func<Dictionary<PdfName, PdfDirectObject>, Annotation>> factory = new(24)
        {
            { PdfName.Text, static dict => new StickyNote(dict) },
            { PdfName.Link, static dict => new Link(dict) },
            { PdfName.FreeText, static dict => new FreeText(dict) },
            { PdfName.Line, static dict => new Line(dict) },
            { PdfName.Square, static dict => new Rectangle(dict) },
            { PdfName.Circle, static dict => new Ellipse(dict) },
            { PdfName.Polygon, static dict => new Polygon(dict) },
            { PdfName.PolyLine, static dict => new Polyline(dict) },
            { PdfName.Highlight, static dict => new TextMarkup(dict) },
            { PdfName.Underline, static dict => new TextMarkup(dict) },
            { PdfName.Squiggly, static dict => new TextMarkup(dict) },
            { PdfName.StrikeOut, static dict => new TextMarkup(dict) },
            { PdfName.Stamp, static dict => new Stamp(dict) },
            { PdfName.Caret, static dict => new Caret(dict) },
            { PdfName.Ink, static dict => new Scribble(dict) },
            { PdfName.Popup, static dict => new Popup(dict) },
            { PdfName.FileAttachment, static dict => new FileAttachment(dict) },
            { PdfName.Sound, static dict => new Sound(dict) },
            { PdfName.Movie, static dict => new Movie(dict) },
            { PdfName.Widget, static dict => new Widget(dict) },
            { PdfName.Screen, static dict => new Screen(dict) },
        };
        internal PdfPage page;
        private string name;
        private SKColor? skColor;
        private SKRect? box;
        protected BottomRightControlPoint cpBottomRight;
        protected BottomLeftControlPoint cpBottomLeft;
        protected TopRightControlPoint cpTopRight;
        protected TopLeftControlPoint cpTopLeft;
        protected List<ContentObject> daOperation;
        internal RefreshAppearanceState queueRefresh;
        private DeviceColor color;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>Wraps an annotation base object into an annotation object.</summary>
        /// <param name="dictionary">Annotation base object.</param>
        /// <returns>Annotation object associated to the base object.</returns>
        internal static Annotation Create(Dictionary<PdfName, PdfDirectObject> dictionary)
        {
            var annotationType = dictionary.Get(PdfName.Subtype, PdfName.None);
            if (factory.TryGetValue(annotationType, out var func))
                return func(dictionary);
            //TODO
            //     else if(annotationType.Equals(PdfName.PrinterMark)) return new PrinterMark(dictionary);
            //     else if(annotationType.Equals(PdfName.TrapNet)) return new TrapNet(dictionary);
            //     else if(annotationType.Equals(PdfName.Watermark)) return new Watermark(dictionary);
            //     else if(annotationType.Equals(PdfName.3DAnnotation)) return new 3DAnnotation(baseObjdictionaryect);
            else // Other annotation type.
                return new GenericAnnotation(dictionary);
        }

        protected Annotation(PdfDocument document, PdfName subtype)
            : base(document, new Dictionary<PdfName, PdfDirectObject>(10)
                  {
                      { PdfName.Type, PdfName.Annot },
                      { PdfName.Subtype, subtype }, // NOTE: Hide border by default.
                  })
        { }

        protected Annotation(PdfPage page, PdfName subtype, SKRect box, string text)
            : this(page.Document, subtype)
        {
            GenerateName();
            page?.Annotations.LinkPage(this);
            Box = Page.InvertRotateMatrix.MapRect(box);
            Contents = text;
            Printable = true;
            IsNew = true;
        }

        internal Annotation(Dictionary<PdfName, PdfDirectObject> dictionary)
            : base(dictionary)
        { }

        public virtual string Author
        {
            get => string.Empty;
            set { }
        }

        public virtual DateTime? CreationDate
        {
            get => null;
            set { }
        }

        /// <summary>Gets/Sets action to be performed when the annotation is activated.</summary>
        [Pdf(VersionEnum.PDF11)]
        public virtual PdfAction Action
        {
            get => Get<PdfAction>(PdfName.A);
            set
            {
                var oldValue = Action;
                if (oldValue != value)
                {
                    Set(PdfName.A, value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /// <summary>Gets/Sets the annotation's behavior in response to various trigger events.</summary>
        [Pdf(VersionEnum.PDF12)]
        public virtual AdditionalActions Actions
        {
            get => GetOrCreate<AdditionalActions>(PdfName.AA);
            set
            {
                var oldValue = Actions;
                if (oldValue != value)
                {
                    Set(PdfName.AA, value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /// <summary>Gets/Sets the constant opacity value to be used in painting the annotation.</summary>
        /// <remarks>This value applies to all visible elements of the annotation (including its background
        /// and border) but not to the popup window that appears when the annotation is opened.</remarks>
        [Pdf(VersionEnum.PDF14)]
        public virtual float Alpha
        {
            get => Catalog.PageAlpha ?? GetFloat(PdfName.CA, 1F);
            set
            {
                var oldValue = Alpha;
                if (oldValue != value)
                {
                    Set(PdfName.CA, value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /// <summary>Gets/Sets the appearance specifying how the annotation is presented visually on the page.</summary>
        [Pdf(VersionEnum.PDF12)]
        public virtual Appearance Appearance
        {
            get => GetOrCreate<Appearance>(PdfName.AP);
            set
            {
                var oldValue = Appearance;
                if (oldValue != value)
                {
                    Set(PdfName.AP, value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /// <summary>Gets/Sets the border style.</summary>
        [Pdf(VersionEnum.PDF11)]
        public virtual Border Border
        {
            get => Get<Border>(PdfName.BS);
            set
            {
                var oldValue = Border;
                if (!(oldValue?.Equals(value) ?? value == null))
                {
                    SetDirect(PdfName.BS, value);
                    if (value != null)
                    { Remove(PdfName.Border); }
                    OnPropertyChanged(oldValue, value);
                    QueueRefreshAppearance();
                }
            }
        }

        /// <summary>Gets/Sets the location of the annotation on the page.</summary>
        public virtual SKRect Box
        {
            get => box ??= Rect?.ToSKRect() ?? SKRect.Empty;
            set
            {
                var oldValue = Box;
                var newValue = value.Round();
                if (oldValue != newValue)
                {
                    Rect = new PdfRectangle(newValue);
                    box = newValue;
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        public virtual PdfRectangle Rect
        {
            get => GetOrCreate<PdfRectangle>(PdfName.Rect);
            set
            {
                var oldValue = Rect;
                if (!(oldValue?.Equals(value) ?? value == null))
                {
                    box = null;
                    SetDirect(PdfName.Rect, value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /// <summary>Gets/Sets the annotation color.</summary>
        [Pdf(VersionEnum.PDF11)]
        public virtual DeviceColor Color
        {
            get => color ??= DeviceColor.Get(Get<PdfArray>(PdfName.C));
            set
            {
                var oldValue = Color;
                if (oldValue != value)
                {
                    this[PdfName.C] = (color = value)?.RefOrSelf;
                    QueueRefreshAppearance();
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        public virtual SKColor SKColor
        {
            get => skColor ??= (Color == null
                ? RGBColorSpace.CalcSKColor(RGBColor.Black, Alpha)
                : DeviceColorSpace.CalcSKColor(Color, Alpha));
            set
            {
                var oldValue = SKColor;
                if (!oldValue.Equals(value))
                {
                    skColor = value;
                    Color = RGBColor.Get(value);
                    Alpha = value.Alpha / 255F;
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /// <summary>Gets/Sets the annotation flags.</summary>
        [Pdf(VersionEnum.PDF11)]
        public virtual AnnotationFlagsEnum Flags
        {
            get => (AnnotationFlagsEnum)GetInt(PdfName.F);
            set
            {
                var oldValue = Flags;
                if (oldValue != value)
                {
                    Set(PdfName.F, (int)value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /// <summary>Gets/Sets the date and time when the annotation was most recently modified.</summary>
        [Pdf(VersionEnum.PDF11)]
        public virtual DateTime? ModificationDate
        {
            //NOTE: Despite PDF date being the preferred format, loose formats are tolerated by the spec.
            get => GetNDate(PdfName.M);
            set
            {
                var oldValue = ModificationDate;
                if (oldValue != PdfDate.Trimm(value))
                {
                    Set(PdfName.M, value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /// <summary>Gets/Sets the annotation name.</summary>
        /// <remarks>The annotation name uniquely identifies the annotation among all the annotations on its page.</remarks>
        [Pdf(VersionEnum.PDF14)]
        public virtual string Name
        {
            get => name ??= GetString(PdfName.NM);
            set
            {
                var oldValue = Name;
                if (!string.Equals(oldValue, value, StringComparison.Ordinal))
                {
                    name = value;
                    SetText(PdfName.NM, value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /// <summary>Gets/Sets the associated page.</summary>
        [Pdf(VersionEnum.PDF13)]
        public virtual PdfPage Page
        {
            get => page ??= Get<PdfPage>(PdfName.P);
            set
            {
                var oldPage = Page;
                if (oldPage != value)
                {
                    box = null;
                    oldPage?.Annotations.Remove(this);
                    page = value;
                    OnPropertyChanged(oldPage, value);
                }
                AddToPage(value);
            }
        }

        private void AddToPage(PdfPage page)
        {
            if (page == null)
            {
                return;
            }
            if (!page.Annotations.Contains(this))
            {
                page.Annotations.Add(this);
            }
            else if (Get(PdfName.P) != page.Reference)
            {
                page.Annotations.LinkPage(this);
            }
            //Debug.WriteLine($"Move to page {page}");
        }

        /// <summary>Gets/Sets whether to print the annotation when the page is printed.</summary>
        [Pdf(VersionEnum.PDF11)]
        public virtual bool Printable
        {
            get => (Flags & AnnotationFlagsEnum.Print) == AnnotationFlagsEnum.Print;
            set
            {
                var oldValue = Printable;
                if (oldValue != value)
                {
                    Flags = EnumUtils.Mask(Flags, AnnotationFlagsEnum.Print, value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /// <summary>Gets/Sets the annotation text.</summary>
        /// <remarks>Depending on the annotation type, the text may be either directly displayed
        /// or (in case of non-textual annotations) used as alternate description.</remarks>
        public virtual string Contents
        {
            get => GetString(PdfName.Contents);
            set
            {
                var oldValue = Contents;
                if (!string.Equals(oldValue, value, StringComparison.Ordinal))
                {
                    SetText(PdfName.Contents, value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /// <summary>Gets/Sets the annotation subject.</summary>
        [Pdf(VersionEnum.PDF15)]
        public virtual string Subject
        {
            get => GetString(PdfName.Subj);
            set
            {
                var oldValue = Subject;
                if (!string.Equals(oldValue, value, StringComparison.Ordinal))
                {
                    SetText(PdfName.Subj, value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        /// <summary>Gets/Sets whether the annotation is visible.</summary>
        [Pdf(VersionEnum.PDF11)]
        public virtual bool Visible
        {
            get => (Flags & AnnotationFlagsEnum.Hidden) != AnnotationFlagsEnum.Hidden;
            set
            {
                var oldValue = Visible;
                if (oldValue != value)
                {
                    Flags = EnumUtils.Mask(Flags, AnnotationFlagsEnum.Hidden, !value);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        [Pdf(VersionEnum.PDF15)]
        public virtual LayerEntity Layer
        {
            get => Get<LayerEntity>(PdfName.OC);
            set
            {
                var oldValue = Layer;
                if (oldValue != value)
                {
                    Set(PdfName.OC, value?.Membership);
                    OnPropertyChanged(oldValue, value);
                }
            }
        }

        protected internal SKMatrix PageMatrix
        {
            get => Page?.RotateMatrix ?? GraphicsState.GetRotationLeftBottomMatrix(SKRect.Create(Catalog.GetSize()), 0);
        }

        protected internal SKMatrix InvertPageMatrix
        {
            get => Page?.InvertRotateMatrix ?? (GraphicsState.GetRotationLeftBottomMatrix(SKRect.Create(Catalog.GetSize()), 0).TryInvert(out var inverted) ? inverted : SKMatrix.Identity);
        }

        public SKPoint TopLeftPoint
        {
            get => new SKPoint(Box.Left, Box.Top);
            set
            {
                var rect = new SKRect(value.X, value.Y, Box.Right, Box.Bottom);
                MoveTo(rect);
            }
        }

        public SKPoint TopRightPoint
        {
            get => new SKPoint(Box.Right, Box.Top);
            set
            {
                var rect = new SKRect(Box.Left, value.Y, value.X, Box.Bottom);
                MoveTo(rect);
            }
        }

        public SKPoint BottomLeftPoint
        {
            get => new SKPoint(Box.Left, Box.Bottom);
            set
            {
                var rect = new SKRect(value.X, Box.Top, Box.Right, value.Y);
                MoveTo(rect);
            }
        }

        public SKPoint BottomRightPoint
        {
            get => new SKPoint(Box.Right, Box.Bottom);
            set
            {
                var rect = new SKRect(Box.Left, Box.Top, value.X, value.Y);
                MoveTo(rect);
            }
        }

        public virtual bool ShowToolTip => true;

        public virtual bool AllowDrag => true;

        public virtual bool AllowSize => true;

        public bool IsNew { get; set; }

        public List<Annotation> Replies { get; set; } = new();

        public string DefaultAppearence
        {
            get => GetString(PdfName.DA);
            set => Set(PdfName.DA, value);
        }

        protected SetFont DASetFont => DAOperations?.OfType<SetFont>().FirstOrDefault();

        protected SetColor DASetColor => DAOperations?.OfType<SetColor>().FirstOrDefault();

        protected virtual List<ContentObject> DAOperations
        {
            get
            {
                if (daOperation != null)
                    return daOperation;
                if (Get<PdfString>(PdfName.DA) is not PdfString daString)
                    return null;
                daOperation = new List<ContentObject>();
                var parser = new ContentParser(daString.RawValue);
                daOperation.AddRange(parser.ParseContentObjects());
                return daOperation;
            }
            set
            {
                daOperation = value;
                if (daOperation != null)
                {
                    var buffer = new ByteStream(64);
                    foreach (var item in value)
                        item.WriteTo(buffer, Document);
                    this[PdfName.DA] = new PdfString(buffer.AsMemory());
                }
            }
        }

        public bool IsDrawed { get; private set; }

        public object Tag { get; set; }

        public virtual void MoveTo(SKRect newBox)
        {
            Box = newBox;
        }

        /// <summary>Deletes this annotation removing also its reference on the page.</summary>
        public override bool Delete()
        {
            Remove();

            // Deep removal (indirect object).
            return base.Delete();
        }

        public virtual void Remove()
        {
            // Shallow removal (references):
            // * reference on page
            Page?.Annotations.Remove(this);
        }

        protected RotationEnum GetPageRotation()
        {
            return Page?.Rotation ?? RotationEnum.Downward;
        }

        public SKRect Draw(SKCanvas canvas)
        {
            if ((queueRefresh & RefreshAppearanceState.Queued) == RefreshAppearanceState.Queued
                && (queueRefresh & RefreshAppearanceState.Suspend) != RefreshAppearanceState.Suspend
                || Box == SKRect.Empty)
            {
                queueRefresh |= RefreshAppearanceState.Process;
                RefreshAppearance();
            }
            var appearance = Appearance.Normal[null];
            if (appearance != null && appearance.GetInputStream()?.Length > 0)
            {
                return DrawAppearance(canvas, appearance);
            }
            else
            {
                return RefreshAppearance(canvas);
            }
        }

        public void RefreshAppearance()
        {
            try
            {
                RefreshBox();
                GenerateAppearance();
            }
            finally
            {
                queueRefresh = RefreshAppearanceState.None;
            }
        }

        protected abstract FormXObject GenerateAppearance();

        public virtual SKRect RefreshAppearance(SKCanvas canvas)
        {
            var appearance = GenerateAppearance();
            return appearance != null ? DrawAppearance(canvas, appearance) : Box;
        }

        protected virtual SKRect DrawAppearance(SKCanvas canvas, FormXObject appearance)
        {
            var picture = appearance.Render(null);
            SKMatrix matrix = GenerateDrawMatrix(appearance, out var bounds);

            if (Alpha < 1)
            {
                using var paint = new SKPaint();
                paint.Color = paint.Color.WithAlpha((byte)(Alpha * 255));
#if NET9_0_OR_GREATER
                canvas.DrawPicture(picture, in matrix, paint);
#else
                canvas.DrawPicture(picture, ref matrix, paint);
#endif
            }
            else
            {
#if NET9_0_OR_GREATER
                canvas.DrawPicture(picture, in matrix);
#else
                canvas.DrawPicture(picture, ref matrix);
#endif
            }
            IsDrawed = true;
            return bounds;
        }

        public FormXObject ResetAppearance(out SKMatrix zeroMatrix) => ResetAppearance(Box, out zeroMatrix);

        public virtual FormXObject ResetAppearance(SKRect box, out SKMatrix zeroMatrix)
        {
            var boxSize = SKRect.Create(box.Width, box.Height);
            zeroMatrix = PageMatrix;
            var pageBox = zeroMatrix.MapRect(box);
            zeroMatrix = zeroMatrix.PostConcat(SKMatrix.CreateTranslation(-pageBox.Left, -pageBox.Top));
            AppearanceStates normalAppearances = Appearance.Normal;
            FormXObject normalAppearance = normalAppearances[null];
            if (normalAppearance != null)
            {
                normalAppearance.Box = boxSize;
                normalAppearance.Matrix = SKMatrix.Identity;
                normalAppearance.GetOutputStream().SetLength(0);
                normalAppearance.ReloadContents();
            }
            else
            {
                normalAppearances[null] =
                      normalAppearance = new FormXObject(Document, boxSize);
            }
            IsDrawed = false;
            return normalAppearance;
        }

        protected virtual SKMatrix GenerateDrawMatrix(FormXObject appearance, out SKRect bound)
        {
            bound = GetDrawBox();
            var appearanceBounds = appearance.Box;

            var matrix = appearance.Matrix;
            var quad = new Quad(appearanceBounds);
            quad.Transform(ref matrix);

            var a = SKMatrix.CreateScale(bound.Width / quad.HorizontalLength, bound.Height / quad.VerticalLenght);
            var quadA = Quad.Transform(quad, ref a);
            a = a.PostConcat(SKMatrix.CreateTranslation(bound.Left - quadA.MinX, bound.Top - quadA.MinY));

            return matrix = matrix.PostConcat(a);
        }

        protected virtual SKRect GetDrawBox()
        {
            return Box;
        }

        public virtual SKRect GetViewBounds() => PageMatrix.MapRect(Box);

        public virtual SKRect GetViewBounds(SKMatrix viewMatrix)
        {
            if ((Flags & AnnotationFlagsEnum.NoZoom) == AnnotationFlagsEnum.NoZoom)
            {

            }
            return viewMatrix.PreConcat(PageMatrix).MapRect(Box);
        }

        public virtual void SetBounds(SKRect value) => MoveTo(InvertPageMatrix.MapRect(value));

        protected virtual void OnPropertyChanged<T>(T oldValue, T newValue, [CallerMemberName] string propertyName = "")
        {
            if ((queueRefresh & RefreshAppearanceState.Suspend) == RefreshAppearanceState.Suspend)
                return;
            PropertyChanged?.Invoke(this, new DetailedPropertyChangedEventArgs<T>(oldValue, newValue, propertyName));
        }

        public override PdfObject Clone(Cloner cloner)
        {
            var cloned = (Annotation)base.Clone(cloner);
            cloned.page = null;
            cloned.cpBottomRight = null;
            cloned.cpBottomLeft = null;
            cloned.cpTopLeft = null;
            cloned.cpTopRight = null;
            return cloned;
        }

        public string GenerateName()
        {
            return Name = Guid.NewGuid().ToString();
        }

        public string GenerateExistingName(string key = null)
        {
            Updateable = false;
            Name = $"{GetType().Name}{Subject}{Page?.Index}{Reference?.Number}{Reference?.Generation}{Author}{key}";
            Updateable = true;
            return Name;
        }


        public virtual void RefreshBox()
        { }

        public virtual IEnumerable<ControlPoint> GetControlPoints()
        {
            yield break;
        }

        public IEnumerable<ControlPoint> GetDefaultControlPoint()
        {
            yield return cpTopLeft ??= new TopLeftControlPoint { Annotation = this };
            yield return cpTopRight ??= new TopRightControlPoint { Annotation = this };
            yield return cpBottomLeft ??= new BottomLeftControlPoint { Annotation = this };
            yield return cpBottomRight ??= new BottomRightControlPoint { Annotation = this };
        }

        public bool IsQueueRefreshAppearance => (queueRefresh | RefreshAppearanceState.Queued) == RefreshAppearanceState.Queued;

        public void QueueRefreshAppearance()
        {
            queueRefresh |= RefreshAppearanceState.Queued;
        }

        public void DequeueRefreshAppearance()
        {
            queueRefresh &= ~RefreshAppearanceState.Queued;
        }

        public void SuspendRefreshAppearance()
        {
            queueRefresh |= RefreshAppearanceState.Suspend;
        }

        public void ResumeRefreshAppearance()
        {
            queueRefresh &= ~RefreshAppearanceState.Suspend;
        }

        public void UserQueueRefreshAppearance()
        {
            queueRefresh |= RefreshAppearanceState.User;
        }
    }

    [Flags]
    internal enum RefreshAppearanceState
    {
        None = 0,
        Queued = 1,
        Process = 2,
        Move = 4,
        GenerateBox = 8,
        Suspend = 16,
        User = 32,
    }
}
