/*
  Copyright 2012 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfGlown.Documents.Files;
using PdfGlown.Documents.Interaction.Actions;
using PdfGlown.Documents.Interaction.Forms;
using PdfGlown.Documents.Multimedia;
using PdfGlown.Objects;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Interaction.Annotations
{
    /// <summary>Screen annotation[PDF:1.6:8.4.5].</summary>
    /// <remarks>It specifies a region of a page upon which media clips may be played.</remarks>
    [Pdf(VersionEnum.PDF15)]
    public sealed class Screen : Annotation
    {
        private const string PlayerPlaceholder = "%player%";
        /**
          <summary>Script for preview and rendering control.</summary>
          <remarks>NOTE: PlayerPlaceholder MUST be replaced with the actual player instance symbol.
          </remarks>
        */
        private const string RenderScript = "if(" + PlayerPlaceholder + "==undefined){"
          + "var doc = this;"
          + "var settings={autoPlay:false,visible:false,volume:100,startAt:0};"
          + "var events=new app.media.Events({"
            + "afterFocus:function(event){try{if(event.target.isPlaying){event.target.pause();}else{event.target.play();}doc.getField('" + PlayerPlaceholder + "').setFocus();}catch(e){}},"
            + "afterReady:function(event){try{event.target.seek(event.target.settings.startAt);event.target.visible=true;}catch(e){}}"
            + "});"
          + "var " + PlayerPlaceholder + "=app.media.openPlayer({settings:settings,events:events});"
          + "}";

        public Screen(PdfPage page, SKRect box, String text, String mediaPath, String mimeType)
            : this(page, box, text, new MediaRendition(new MediaClipData(
                IFileSpecification.Get(EmbeddedFile.Get(page.Document, mediaPath), System.IO.Path.GetFileName(mediaPath)),
                mimeType))
            )
        { }

        public Screen(PdfPage page, SKRect box, String text, Rendition rendition)
            : base(page, PdfName.Screen, box, text)
        {
            var render = new Render(this, Render.OperationEnum.PlayResume, rendition);
            {
                // Adding preview and play/pause control...
                /*
                  NOTE: Mouse-related actions don't work when the player is active; therefore, in order to let
                  the user control the rendering of the media clip (play/pause) just by mouse-clicking on the
                  player, we can only rely on the player's focus event. Furthermore, as the player's focus can
                  only be altered setting it on another widget, we have to define an ancillary field on the
                  same page (so convoluted!).
                */
                string playerReference = "__player" + render.Reference.Number;
                Catalog.Form.Fields.Add(new TextField(playerReference, new Widget(page, SKRect.Create(box.Left, box.Top, 0, 0)), "")); // Ancillary field.
                render.Script = RenderScript.Replace(PlayerPlaceholder, playerReference);
            }
            Actions.OnPageOpen = render;

            if (rendition is MediaRendition mediaRendition)
            {
                if (mediaRendition.Clip.Data is IFileSpecification fileSpec)
                {
                    // Adding fallback annotation...
                    /*
                      NOTE: In case of viewers which don't support video rendering, this annotation gently
                      degrades to a file attachment that can be opened on the same location of the corresponding
                      screen annotation.
                    */
                    var attachment = new FileAttachment(page, box, text, fileSpec);
                    Set(PdfName.T, fileSpec.FilePath);
                    // Force empty appearance to ensure no default icon is drawn on the canvas!
                    attachment.Appearance = new Appearance(new Dictionary<PdfName, PdfDirectObject>(3)
                    {
                        { PdfName.D, new PdfDictionary() },
                        { PdfName.R, new PdfDictionary() },
                        { PdfName.N, new PdfDictionary() }
                    });
                    page.Annotations.Add(attachment);
                }
            }
        }

        internal Screen(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        protected override FormXObject GenerateAppearance()
        {
            return Appearance.Normal[null];
        }
    }
}