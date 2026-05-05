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
using System.Collections.Generic;

namespace PdfGlown.Documents.Interaction.Actions
{
    /// <summary>Annotation actions [PDF:1.6:8.5.2].</summary>
    [Pdf(VersionEnum.PDF12)]
    public class AdditionalActions : PdfDictionary
    {
        public AdditionalActions()
            : this((PdfDocument)null)
        { }

        public AdditionalActions(PdfDocument document)
            : base(document, new())
        { }

        internal AdditionalActions(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        public override PdfName ModifyTypeKey(PdfName key) => PdfName.Action;

        /// <summary>Gets/Sets the action to be performed when the page is closed.</summary>
        public PdfAction OnClose
        {
            get => Get<PdfAction>(PdfName.C);
            set => Set(PdfName.C, value);
        }

        /// <summary>Gets/Sets a JavaScript action to be performed to recalculate the value
        /// of this field when that of another field changes.</summary>
        public JavaScript OnCalculate
        {
            get => Get<JavaScript>(PdfName.C);
            set => Set(PdfName.C, value);
        }

        /// <summary>Gets/Sets the action to be performed when the mouse button is pressed
        /// inside the annotation's active area.</summary>
        public PdfAction OnMouseDown
        {
            get => Get<PdfAction>(PdfName.D);
            set => Set(PdfName.D, value);
        }

        /// <summary>Gets/Sets the action to be performed when the cursor enters the annotation's active area.</summary>
        public PdfAction OnEnter
        {
            get => Get<PdfAction>(PdfName.E);
            set => Set(PdfName.E, value);
        }

        /// <summary>Gets/Sets a JavaScript action to be performed before the field is formatted
        /// to display its current value.</summary>
        /// <remarks>This action can modify the field's value before formatting.</remarks>
        public PdfAction OnFormat
        {
            get => Get<PdfAction>(PdfName.F);
            set => Set(PdfName.F, value);
        }

        /// <summary>Gets/Sets the action to be performed when the annotation receives the input focus.</summary>
        public PdfAction OnFocus
        {
            get => Get<PdfAction>(PdfName.Fo);
            set => Set(PdfName.Fo, value);
        }

        /// <summary>Gets/Sets the action to be performed when the page is opened.</summary>
        public PdfAction OnOpen
        {
            get => Get<PdfAction>(PdfName.O);
            set => Set(PdfName.O, value);
        }

        /// <summary>Gets/Sets the action to be performed when the mouse button is released
        /// inside the annotation's active area.</summary>
        public PdfAction OnMouseUp
        {
            get => Get<PdfAction>(PdfName.U);
            set => Set(PdfName.U, value);
        }
        
        /// <summary>Gets/Sets the action to be performed when the cursor exits the annotation's active area.</summary>
        public PdfAction OnExit
        {
            get => Get<PdfAction>(PdfName.X);
            set => Set(PdfName.X, value);
        }        

        /// <summary>Gets/Sets the action to be performed when the page containing the annotation is closed.</summary>
        public PdfAction OnPageClose
        {
            get => Get<PdfAction>(PdfName.PC);
            set => Set(PdfName.PC, value);
        }

        /// <summary>Gets/Sets the action to be performed when the page containing the annotation
        /// is no longer visible in the viewer application's user interface.</summary>
        public PdfAction OnPageInvisible
        {
            get => Get<PdfAction>(PdfName.PI);
            set => Set(PdfName.PI, value);
        }

        /// <summary>Gets/Sets the action to be performed when the page containing the annotation is opened.</summary>
        public PdfAction OnPageOpen
        {
            get => Get<PdfAction>(PdfName.PO);
            set => Set(PdfName.PO, value);
        }

        /// <summary>Gets/Sets the action to be performed when the page containing the annotation
        /// becomes visible in the viewer application's user interface.</summary>
        public PdfAction OnPageVisible
        {
            get => Get<PdfAction>(PdfName.PV);
            set => Set(PdfName.PV, value);
        }

        /// <summary>Gets/Sets the action to be performed when the annotation loses the input focus.</summary>
        public PdfAction OnBlur
        {
            get => Get<PdfAction>(PdfName.Bl);
            set => Set(PdfName.Bl, value);
        }

        /// <summary>Gets/Sets the action to be performed before closing the document.</summary>
        public PdfAction OnDocumentClose
        {
            get => Get<PdfAction>(PdfName.DC);
            set => Set(PdfName.DC, value);
        }

        /// <summary>Gets/Sets the action to be performed after printing the document.</summary>
        public PdfAction AfterPrint
        {
            get => Get<PdfAction>(PdfName.DP);
            set => Set(PdfName.DP, value);
        }

        /// <summary>Gets/Sets the action to be performed after saving the document.</summary>
        public PdfAction AfterSave
        {
            get => Get<PdfAction>(PdfName.DS);
            set => Set(PdfName.DS, value);
        }

        /// <summary>Gets/Sets the action to be performed before printing the document.</summary>
        public PdfAction BeforePrint
        {
            get => Get<PdfAction>(PdfName.WP);
            set => Set(PdfName.WP, value);
        }

        /// <summary>Gets/Sets the action to be performed before saving the document.</summary>
        public PdfAction BeforeSave
        {
            get => Get<PdfAction>(PdfName.WS);
            set => Set(PdfName.WS, value);
        }

        /// <summary>Gets/Sets a JavaScript action to be performed when the user types a keystroke
        /// into a text field or combo box or modifies the selection in a scrollable list box.</summary>
        public JavaScript OnChange
        {
            get => Get<JavaScript>(PdfName.K);
            set => Set(PdfName.K, value);
        }

        /// <summary>Gets/Sets a JavaScript action to be performed when the field's value is changed.</summary>
        /// <remarks>This action can check the new value for validity.</remarks>
        public JavaScript OnValidate
        {
            get => Get<JavaScript>(PdfName.V);
            set => Set(PdfName.V, value);
        }
    }
}