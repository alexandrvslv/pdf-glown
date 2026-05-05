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

using PdfGlown.Documents.Interaction.Annotations;
using PdfGlown.Objects;

using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Interaction.Actions
{
    /// <summary>'Play a movie' action [PDF:1.6:8.5.3].</summary>
    [Pdf(VersionEnum.PDF12)]
    public sealed class PlayMovie : PdfAction
    {
        /// <summary>Creates a new action within the given document context.</summary>
        public PlayMovie(PdfDocument context, Movie movie)
            : base(context, PdfName.Movie)
        {
            Movie = movie;
        }

        internal PlayMovie(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets/Sets the movie to be played.</summary>
        public Movie Movie
        {
            get
            {
                var annotationObject = Get(PdfName.Annotation);
                if (annotationObject == null)
                {
                    annotationObject = Get(PdfName.T);
                    throw new NotImplementedException("No by-title movie annotation support currently: we have to implement a hook to the page of the referenced movie to get it from its annotations collection.");
                }
                return (Movie)annotationObject.Resolve(PdfName.Movie);
            }
            set
            {
                if (value == null)
                    throw new ArgumentException("Movie MUST be defined.");

                Set(PdfName.Annotation, value);
            }
        }

        public override string GetDisplayName() => "Play Movie";
    }
}