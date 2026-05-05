/*
  Copyright 2008-2012 Stefano Chizzolini. http://www.pdfclown.org

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

using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Interaction.Actions
{
    ///<summary>Action to be performed by the viewer application [PDF:1.6:8.5].</summary>
    [Pdf(VersionEnum.PDF11)]
    public class PdfAction : PdfDictionary, ITextDisplayable
    {
        private static readonly Dictionary<PdfName, Func<Dictionary<PdfName, PdfDirectObject>, PdfAction>> nCache = new(4)
        {
             { PdfName.NextPage, static dict => new GoToNextPage(dict) },
             { PdfName.PrevPage, static dict => new GoToPreviousPage(dict) },
             { PdfName.FirstPage, static dict => new GoToFirstPage(dict) },
             { PdfName.LastPage, static dict => new GoToLastPage(dict) },
        };

        private static readonly Dictionary<PdfName, Func<Dictionary<PdfName, PdfDirectObject>, PdfAction>> sCache = new(32)
        {
            { PdfName.GoTo, static dict => new GoToLocal(dict) },
            { PdfName.GoToR, static dict => new GoToRemote(dict) },
            { PdfName.GoToE, static dict => new GoToEmbedded(dict) },
            { PdfName.Launch, static dict => new Launch(dict) },
            { PdfName.Thread, static dict => new GoToThread(dict) },
            { PdfName.URI, static dict => new GoToURI(dict) },
            { PdfName.Sound, static dict => new PlaySound(dict) },
            { PdfName.Movie, static dict => new PlayMovie(dict) },
            { PdfName.Hide, static dict => new ToggleVisibility(dict) },
            { PdfName.SubmitForm, static dict => new SubmitForm(dict) },
            { PdfName.ResetForm, static dict => new ResetForm(dict) },
            { PdfName.ImportData, static dict => new ImportData(dict) },
            { PdfName.JavaScript, static dict => new JavaScript(dict) },
            { PdfName.SetOCGState, static dict => new SetLayerState(dict) },
            { PdfName.Rendition, static dict => new Render(dict) },
            { PdfName.Trans, static dict => new DoTransition(dict) },
            { PdfName.GoTo3DView, static dict => new GoTo3dView(dict) },
            { PdfName.Named, static dict =>
            {
                var actionName = dict.Get<PdfName>(PdfName.N);
                return nCache.TryGetValue(actionName, out var func)
                    ? func(dict)
                    : new NamedAction(dict);
            }},
        };
        private ChainedActions actions;

        ///<summary>Wraps an action base object into an action object.</summary>
        ///<param name="baseObject">Action base object.</param>
        ///<returns>Action object associated to the base object.</returns>
        internal static PdfAction Create(Dictionary<PdfName, PdfDirectObject> dictionary)
        {
            var actionType = dictionary.Get<PdfName>(PdfName.S);
            return sCache.TryGetValue(actionType, out var func)
                ? func(dictionary)
                : new PdfAction(dictionary);
        }

        ///<summary>Creates a new action within the given document context.</summary>
        protected PdfAction(PdfDocument context, PdfName actionType)
            : base(context, new Dictionary<PdfName, PdfDirectObject>(4)
            {
                { PdfName.Type, PdfName.Action },
                { PdfName.S, actionType }
            })
        { }

        protected PdfAction(Dictionary<PdfName, PdfDirectObject> dictionary)
            : base(dictionary)
        { }

        ///<summary>Gets/Sets the actions to be performed after the current one.</summary>
        [Pdf(VersionEnum.PDF12)]
        public ChainedActions Actions
        {
            get => actions ??= ChainedActions.Wrap(Get(PdfName.Next), this);
            set => Set(PdfName.Next, actions = value);
        }

        public virtual string GetDisplayName() => string.Empty;
    }
}