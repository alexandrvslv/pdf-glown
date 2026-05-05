/*
  Copyright 2007-2015 Stefano Chizzolini. http://www.pdfclown.org

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

using PdfGlown.Documents.Contents.Objects;
using SkiaSharp;
using System.Collections.Generic;

namespace PdfGlown.Documents.Contents
{
    /// <summary>Content objects scanner.</summary>
    /// <remarks>
    ///   <para>It wraps the <see cref="Contents">content objects collection</see> to scan its graphics state
    ///   through a forward cursor.</para>
    ///   <para>Scanning is performed at an arbitrary deepness, according to the content objects nesting:
    ///   each depth level corresponds to a scan level so that at any time it's possible to seamlessly
    ///   navigate across the levels (see <see cref="ParentLevel"/>, <see cref="ChildLevel"/>).</para>
    /// </remarks>
    public sealed partial class ContentScanner
    {
        /// <summary>Handles the scan start notification.</summary>
        /// <param name="scanner">Content scanner started.</param>
        public delegate void OnStartEventHandler(ContentScanner scanner);
        public delegate void OnContentObjectEventHandler(ContentObject obj);
        public delegate bool OnContentObjectScanningEventHandler(ContentObject obj, ICompositeObject container, int index);

        public event OnContentObjectScanningEventHandler OnObjectScanning;
        public event OnContentObjectEventHandler OnObjectScanned;

        private readonly IContentContext context;
        // Content objects collection.
        private readonly ContentWrapper contents;
        
        // Parent level.
        private readonly ContentScanner parentLevel;
        private ContentScanner resourceParentLevel;
        // Current graphics state.
        private GraphicsState state;

        // Rendering context.
        private SKCanvas canvas;
        // Rendering object.
        private SKPath path;

        /// <summary>Device-independent size of the graphics canvas.</summary>
        private SKRect contextBox;

        private Stack<GraphicsState> states;

        private Stack<TextGraphicsState> textStates;


        /// <summary>Instantiates a top-level content scanner.</summary>
        /// <param name="context">Content context containing the content objects collection to scan.</param>
        public ContentScanner(IContentContext context)
            : this(context, context.Contents, null, null, context.Box)
        { }

        public ContentScanner(IContentContext context, ContentWrapper contentWrapper)
             : this(context, contentWrapper, null, null, context.Box)
        { }

        /// <summary>Instantiates a child-level content scanner for <see cref="XObjects.FormXObject">external form</see>.</summary>
        /// <param name="context">External form.</param>
        /// <param name="parentLevel">Parent scan level.</param>
        public ContentScanner(IContentContext context, ContentScanner parentLevel)
            : this(context, context.Contents, parentLevel, parentLevel.Canvas, parentLevel.ContextBox)
        { }

        public ContentScanner(IContentContext context, SKCanvas canvas, SKRect box, SKColor? clearColor = null)
            : this(context, context.Contents, null, canvas, box, clearColor)
        { }

        private ContentScanner(IContentContext context, ContentWrapper contentWrapper, ContentScanner parentLevel, SKCanvas canvas, SKRect box, SKColor? clearColor = null)
        {
            this.context = context;
            this.contents = contentWrapper;
            this.parentLevel = parentLevel;
            this.canvas = canvas;
            contextBox = box;
            ClearColor = clearColor;
            ClearCanvas();
            InitState();
        }

        /// <summary>Gets the rendering context.</summary>
        /// <returns><code>null</code> in case of dry scanning.</returns>
        public SKCanvas Canvas
        {
            get => canvas;
            internal set => canvas = value;
        }

        /// <summary>Gets the rendering object.</summary>
        /// <returns><code>null</code> in case of scanning outside a shape.</returns>
        public SKPath Path
        {
            get => path;
            internal set => path = value;
        }

        /// <summary>Gets the current graphics state applied to the current content object.</summary>
        public GraphicsState State => state;

        public SKColor? ClearColor { get; set; }

        /// <summary>Size of the graphics canvas.</summary>
        /// <remarks>According to the current processing (whether it is device-independent scanning or
        /// device-based rendering), it may be expressed, respectively, in user-space units or in
        /// device-space units.</remarks>
        public SKRect CanvasBox => Canvas?.DeviceClipBounds ?? ContextBox;

        /// <summary>Gets the content context associated to the content objects collection.</summary>
        public IContentContext Context => context;

        /// <summary>Gets the content objects collection this scanner is inspecting.</summary>
        public ContentWrapper Contents => contents;

        /// <summary>Gets the size of the current imageable area in user-space units.</summary>
        public SKRect ContextBox => contextBox;

        public Stack<GraphicsState> StateStack => states ??= new Stack<GraphicsState>(4);

        public Stack<TextGraphicsState> TextStateStack => textStates ??= new Stack<TextGraphicsState>(2);

        /// <summary>Gets the parent scan level.</summary>
        public ContentScanner Parent => parentLevel;

        public ContentScanner ResourceParent
        {
            get => resourceParentLevel ?? Parent;
            internal set => resourceParentLevel = value;
        }

        public GraphicsState InitState()
        {
            if (state == null)
            {
                state = new GraphicsState(this);               
            }
            else
            {
                state.Initialize();
            }
            return state;
        }

        private void ClearCanvas()
        {
            if (Canvas == null)
                return;
            //var mapped = state.Ctm.MapRect(ContextBox);
            Canvas.ClipRect(ContextBox);
            if (ClearColor is SKColor color)
            {
                Canvas.Clear(color);
            }
        }

        public void Scan()
        {
            for (int i = 0; i< contents.Count; i++)
            {
                contents[i].Scan(State, contents, i);
            }
        }

        public bool StartScan(ContentObject contentObject, ICompositeObject compositeObject, int index)
        {
            return OnObjectScanning?.Invoke(contentObject, compositeObject, index) ?? true;
        }

        public void FinishScan(ContentObject contentObject)
        {
            OnObjectScanned?.Invoke(contentObject);
        }
    }
}