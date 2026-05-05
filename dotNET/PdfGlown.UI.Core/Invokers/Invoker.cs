using PdfGlown.Documents;
using PdfGlown.Documents.Contents.ColorSpaces;
using PdfGlown.Documents.Contents.Layers;
using PdfGlown.Documents.Files;
using PdfGlown.Documents.Interaction;
using PdfGlown.Documents.Interaction.Actions;
using PdfGlown.Documents.Interaction.Annotations;
using PdfGlown.Objects;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace PdfGlown.Util.Invokers
{
    public abstract class Invoker : IInvoker
    {
        private static readonly Dictionary<Type, Dictionary<string, IInvoker>> cache = new Dictionary<Type, Dictionary<string, IInvoker>>();
        static Invoker()
        {
            cache.Add(typeof(Annotation), new Dictionary<string, IInvoker>()
            {
                {  nameof(Annotation.Action), new ActionInvoker<Annotation, PdfAction>(nameof(Annotation.Action), a => a.Action, (a,v) => a.Action = v ) },
                {  nameof(Annotation.Alpha), new ActionInvoker<Annotation, float>(nameof(Annotation.Alpha), a => a.Alpha, (a,v) => a.Alpha = v ) },
                {  nameof(Annotation.Appearance), new ActionInvoker<Annotation, Appearance>(nameof(Annotation.Appearance), a => a.Appearance, (a,v) => a.Appearance = v ) },
                {  nameof(Annotation.Author), new ActionInvoker<Annotation, string>(nameof(Annotation.Appearance), a => a.Author, (a,v) => a.Author = v ) },
                {  nameof(Annotation.Border), new ActionInvoker<Annotation, Border>(nameof(Annotation.Border), a => a.Border, (a,v) => a.Border = v ) },
                {  nameof(Annotation.Box), new ActionInvoker<Annotation, SKRect>(nameof(Annotation.Box), a => a.Box, (a,v) => a.Box = v ) },
                {  nameof(Annotation.Color), new ActionInvoker<Annotation, DeviceColor>(nameof(Annotation.Color), a => a.Color, (a,v) => a.Color = v ) },
                {  nameof(Annotation.Contents), new ActionInvoker<Annotation, string>(nameof(Annotation.Contents), a => a.Contents, (a,v) => a.Contents = v ) },
                {  nameof(Annotation.CreationDate), new ActionInvoker<Annotation, DateTime?>(nameof(Annotation.CreationDate), a => a.CreationDate, (a,v) => a.CreationDate = v ) },
                {  nameof(Annotation.DefaultAppearence), new ActionInvoker<Annotation, string>(nameof(Annotation.DefaultAppearence), a => a.DefaultAppearence, (a,v) => a.DefaultAppearence = v ) },
                {  nameof(Annotation.Flags), new ActionInvoker<Annotation, AnnotationFlagsEnum>(nameof(Annotation.Flags), a => a.Flags, (a,v) => a.Flags = v ) },
                {  nameof(Annotation.Layer), new ActionInvoker<Annotation, LayerEntity>(nameof(Annotation.Layer), a => a.Layer, (a,v) => a.Layer = v ) },
                {  nameof(Annotation.ModificationDate), new ActionInvoker<Annotation, DateTime?>(nameof(Annotation.ModificationDate), a => a.ModificationDate, (a,v) => a.ModificationDate = v ) },
                {  nameof(Annotation.Name), new ActionInvoker<Annotation, string>(nameof(Annotation.Name), a => a.Name, (a,v) => a.Name = v ) },
                {  nameof(Annotation.Page), new ActionInvoker<Annotation, PdfPage>(nameof(Annotation.Page), a => a.Page, (a,v) => a.Page = v ) },
                {  nameof(Annotation.Rect), new ActionInvoker<Annotation, PdfRectangle>(nameof(Annotation.Rect), a => a.Rect, (a,v) => a.Rect = v ) },
                {  nameof(Annotation.Printable), new ActionInvoker<Annotation, bool>(nameof(Annotation.Printable), a => a.Printable, (a,v) => a.Printable = v ) },
                {  nameof(Annotation.SKColor), new ActionInvoker<Annotation, SKColor>(nameof(Annotation.SKColor), a => a.SKColor, (a,v) => a.SKColor = v ) },
                {  nameof(Annotation.Subject), new ActionInvoker<Annotation, string>(nameof(Annotation.Subject), a => a.Subject, (a,v) => a.Subject = v ) },
                {  nameof(Annotation.Visible), new ActionInvoker<Annotation, bool>(nameof(Annotation.Subject), a => a.Visible, (a,v) => a.Visible = v ) },
            });
            cache.Add(typeof(Markup), new Dictionary<string, IInvoker>()
            {
                {  nameof(Markup.BorderEffect), new ActionInvoker<Markup, BorderEffect>(nameof(Markup.BorderEffect), a => a.BorderEffect, (a,v) => a.BorderEffect = v ) },
                {  nameof(Markup.DefaultStyle), new ActionInvoker<Markup, string>(nameof(Markup.DefaultStyle), a => a.DefaultStyle, (a,v) => a.DefaultStyle = v ) },
                {  nameof(Markup.Intent), new ActionInvoker<Markup, MarkupIntent?>(nameof(Markup.Intent), a => a.Intent, (a,v) => a.Intent = v ) },
                {  nameof(Markup.InteriorColor), new ActionInvoker<Markup, DeviceColor>(nameof(Markup.InteriorColor), a => a.InteriorColor, (a,v) => a.InteriorColor = v ) },
                {  nameof(Markup.InteriorSKColor), new ActionInvoker<Markup, SKColor?>(nameof(Markup.InteriorSKColor), a => a.InteriorSKColor, (a,v) => a.InteriorSKColor = v ) },
                {  nameof(Markup.Popup), new ActionInvoker<Markup, Popup>(nameof(Markup.Popup), a => a.Popup, (a,v) => a.Popup = v ) },
                {  nameof(Markup.ReplyTo), new ActionInvoker<Markup, Annotation>(nameof(Markup.ReplyTo), a => a.ReplyTo, (a,v) => a.ReplyTo = v ) },
                {  nameof(Markup.ReplyType), new ActionInvoker<Markup, ReplyTypeEnum?>(nameof(Markup.ReplyType), a => a.ReplyType, (a,v) => a.ReplyType = v ) },
                {  nameof(Markup.RichContents), new ActionInvoker<Markup, string>(nameof(Markup.RichContents), a => a.RichContents, (a,v) => a.RichContents = v ) },
            });
            cache.Add(typeof(Caret), new Dictionary<string, IInvoker>()
            {
                {  nameof(Caret.SymbolType), new ActionInvoker<Caret, Caret.SymbolTypeEnum>(nameof(Caret.SymbolType), a => a.SymbolType, (a,v) => a.SymbolType = v ) },
            });
            cache.Add(typeof(FileAttachment), new Dictionary<string, IInvoker>()
            {
                {  nameof(FileAttachment.AttachmentName), new ActionInvoker<FileAttachment, FileAttachmentImageType>(nameof(FileAttachment.AttachmentName), a => a.AttachmentName, (a,v) => a.AttachmentName = v ) },
                {  nameof(FileAttachment.DataFile), new ActionInvoker<FileAttachment, IFileSpecification>(nameof(FileAttachment.DataFile), a => a.DataFile, (a,v) => a.DataFile = v ) },
            });
            cache.Add(typeof(FreeText), new Dictionary<string, IInvoker>()
            {
                {  nameof(FreeText.TextBox), new ActionInvoker<FreeText, SKRect>(nameof(FreeText.TextBox), a => a.TextBox, (a,v) => a.TextBox = v ) },
                {  nameof(FreeText.Justification), new ActionInvoker<FreeText, JustificationEnum>(nameof(FreeText.Justification), a => a.Justification, (a,v) => a.Justification = v ) },
                {  nameof(FreeText.Callout), new ActionInvoker<FreeText, PdfArray>(nameof(FreeText.Callout), a => a.Callout, (a,v) => a.Callout = v ) },
                {  nameof(FreeText.Line), new ActionInvoker<FreeText, FreeText.CalloutLine>(nameof(FreeText.Line), a => a.Line, (a,v) => a.Line = v ) },
                {  nameof(FreeText.LineEndStyle), new ActionInvoker<FreeText, LineEndStyleEnum>(nameof(FreeText.LineEndStyle), a => a.LineEndStyle, (a,v) => a.LineEndStyle = v ) },
                {  nameof(FreeText.Padding), new ActionInvoker<FreeText, Objects.PdfPadding>(nameof(FreeText.Padding), a => a.Padding, (a,v) => a.Padding = v ) },
            });
            cache.Add(typeof(Line), new Dictionary<string, IInvoker>()
            {
                {  nameof(Line.CaptionVisible), new ActionInvoker<Line, bool>(nameof(Line.CaptionVisible), a => a.CaptionVisible, (a,v) => a.CaptionVisible = v ) },
                {  nameof(Line.CaptionPosition), new ActionInvoker<Line, LineCaptionPosition?>(nameof(Line.CaptionPosition), a => a.CaptionPosition, (a,v) => a.CaptionPosition = v ) },
                {  nameof(Line.CaptionOffset), new ActionInvoker<Line, SKPoint?>(nameof(Line.CaptionOffset), a => a.CaptionOffset, (a,v) => a.CaptionOffset = v ) },
                {  nameof(Line.StartStyle), new ActionInvoker<Line, LineEndStyleEnum>(nameof(Line.StartStyle), a => a.StartStyle, (a,v) => a.StartStyle = v ) },
                {  nameof(Line.EndStyle), new ActionInvoker<Line, LineEndStyleEnum>(nameof(Line.EndStyle), a => a.EndStyle, (a,v) => a.EndStyle = v ) },
                {  nameof(Line.LeaderLineExtension), new ActionInvoker<Line, double>(nameof(Line.LeaderLineExtension), a => a.LeaderLineExtension, (a,v) => a.LeaderLineExtension = v ) },
                {  nameof(Line.LeaderLineOffset), new ActionInvoker<Line, double>(nameof(Line.LeaderLineOffset), a => a.LeaderLineOffset, (a,v) => a.LeaderLineOffset = v ) },
                {  nameof(Line.LeaderLineLength), new ActionInvoker<Line, double>(nameof(Line.LeaderLineLength), a => a.LeaderLineLength, (a,v) => a.LeaderLineLength = v ) },
                {  nameof(Line.StartPoint), new ActionInvoker<Line, SKPoint>(nameof(Line.StartPoint), a => a.StartPoint, (a,v) => a.StartPoint = v ) },
                {  nameof(Line.EndPoint), new ActionInvoker<Line, SKPoint>(nameof(Line.EndPoint), a => a.EndPoint, (a,v) => a.EndPoint = v ) },
            });
            cache.Add(typeof(VertexShape), new Dictionary<string, IInvoker>()
            {
                {  nameof(VertexShape.Points), new ActionInvoker<VertexShape, SKPoint[]>(nameof(VertexShape.Points), a => a.Points, (a,v) => a.Points = v ) },
                {  nameof(VertexShape.Vertices), new ActionInvoker<VertexShape, PdfArray>(nameof(VertexShape.Vertices), a => a.Vertices, (a,v) => a.Vertices = v ) },
            });
            cache.Add(typeof(Popup), new Dictionary<string, IInvoker>()
            {
                {  nameof(Popup.IsOpen), new ActionInvoker<Popup, bool>(nameof(Popup.IsOpen), a => a.IsOpen, (a,v) => a.IsOpen = v ) },
                {  nameof(Popup.Parent), new ActionInvoker<Popup, Markup>(nameof(Popup.Parent), a => a.Parent, (a,v) => a.Parent = v ) },
            });
            cache.Add(typeof(Scribble), new Dictionary<string, IInvoker>()
            {
                {  nameof(Scribble.InkList), new ActionInvoker<Scribble, PdfArray>(nameof(Scribble.InkList), a => a.InkList, (a,v) => a.InkList = v ) },
            });
            cache.Add(typeof(Stamp), new Dictionary<string, IInvoker>()
            {
                {  nameof(Stamp.TypeName), new ActionInvoker<Stamp, string>(nameof(Stamp.TypeName), a => a.TypeName, (a,v) => a.TypeName = v ) },
                {  nameof(Stamp.Rotation), new ActionInvoker<Stamp, int>(nameof(Stamp.Rotation), a => a.Rotation, (a,v) => a.Rotation = v ) },
            });
            cache.Add(typeof(StickyNote), new Dictionary<string, IInvoker>()
            {
                {  nameof(StickyNote.ImageName), new ActionInvoker<StickyNote, NoteImageEnum>(nameof(StickyNote.ImageName), a => a.ImageName, (a,v) => a.ImageName = v ) },
                {  nameof(StickyNote.IsOpen), new ActionInvoker<StickyNote, bool>(nameof(StickyNote.IsOpen), a => a.IsOpen, (a,v) => a.IsOpen = v ) },
                {  nameof(StickyNote.State), new ActionInvoker<StickyNote, MarkupState?>(nameof(StickyNote.State), a => a.State, (a,v) => a.State = v ) },
                {  nameof(StickyNote.StateModel), new ActionInvoker<StickyNote, MarkupStateModel?>(nameof(StickyNote.StateModel), a => a.StateModel, (a,v) => a.StateModel = v ) },
            });
            cache.Add(typeof(TextMarkup), new Dictionary<string, IInvoker>()
            {
                {  nameof(TextMarkup.QuadPoints), new ActionInvoker<TextMarkup, PdfArray>(nameof(TextMarkup.QuadPoints), a => a.QuadPoints, (a,v) => a.QuadPoints = v ) },

            });
        }

        public static IInvoker? GetPropertyInvoker(Type type, string propertyName)
        {
            TryGetPropertyInvoker(type, propertyName, out var invoker);
            return invoker;
        }

        public static bool TryGetPropertyInvoker(Type declaringType, string propertyName, out IInvoker? invoker)
        {
            foreach (var entry in cache)
            {
                if (entry.Key.IsAssignableFrom(declaringType)
                    && entry.Value.TryGetValue(propertyName, out invoker))
                {
                    return true;
                }
            }
            invoker = null;
            return false;
        }

        public abstract string Name { get; }

        public abstract Type DataType { get; }

        public abstract Type TargetType { get; }

        public abstract bool CanWrite { get; }


        public abstract object? GetValue(object target);

        public abstract void SetValue(object target, object? value);
    }

    public abstract class Invoker<T, V> : Invoker, IInvoker<T, V>
    {
        public Invoker()
        { }

        public override Type DataType => typeof(V);
        public override Type TargetType => typeof(T);

        public abstract V GetValue(T target);

        public override object? GetValue(object target) => GetValue((T)target);

        public abstract void SetValue(T target, V value);

        public void SetValue(object target, V value) => SetValue((T)target, value);

        public override void SetValue(object target, object? value) => SetValue((T)target, (V)value!);

        public override string ToString()
        {
            return $"{TargetType.Name}.{Name} {DataType.Name}";
        }
    }
}
