/*
  Copyright 2006-2015 Stefano Chizzolini. http://www.pdfclown.org

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
using PdfGlown.Util;
using PdfGlown.Util.Math;

using System;
using System.Collections.Generic;

namespace PdfGlown.Documents.Interaction.Viewer
{
    /// <summary>Viewer preferences [PDF:1.7:8.1].</summary>
    [Pdf(VersionEnum.PDF12)]
    public sealed class ViewerPreferences : PdfDictionary
    {
        /// <summary>Predominant reading order for text [PDF:1.7:8.1].</summary>
        [Pdf(VersionEnum.PDF13)]
        public enum DirectionEnum
        {
            /// <summary>Left to right.</summary>
            LeftToRight,
            /// <summary>Right to left (including vertical writing systems, such as Chinese, Japanese, and
            /// Korean).</summary>
            RightToLeft
        };

        /// <summary>Page layout to be used when the document is opened [PDF:1.7:3.6.1].</summary>
        [Pdf(VersionEnum.PDF10)]
        public enum PageLayoutEnum
        {
            /// <summary>Displays one page at a time.</summary>
            SinglePage,
            /// <summary>Displays the pages in one column.</summary>
            OneColumn,
            /// <summary>Displays the pages in two columns, with odd-numbered pages on the left.</summary>
            TwoColumnLeft,
            /// <summary>Displays the pages in two columns, with odd-numbered pages on the right.</summary>
            TwoColumnRight,
            /// <summary>Displays the pages two at a time, with odd-numbered pages on the left.</summary>
            [Pdf(VersionEnum.PDF15)]
            TwoPageLeft,
            /// <summary>Displays the pages two at a time, with odd-numbered pages on the right.</summary>
            [Pdf(VersionEnum.PDF15)]
            TwoPageRight
        };

        /// <summary>Page mode specifying how the document should be displayed when opened [PDF:1.7:3.6.1].
        /// </summary>
        [Pdf(VersionEnum.PDF10)]
        public enum PageModeEnum
        {
            /// <summary>Neither document outline nor thumbnail images visible.</summary>
            Simple,
            /// <summary>Document outline visible.</summary>
            Bookmarks,
            /// <summary>Thumbnail images visible.</summary>
            Thumbnails,
            /// <summary>Full-screen mode, with no menu bar, window controls, or any other window visible.
            /// </summary>
            FullScreen,
            /// <summary>Optional content group panel visible.</summary>
            [Pdf(VersionEnum.PDF15)]
            Layers,
            /// <summary>Attachments panel visible.</summary>
            [Pdf(VersionEnum.PDF16)]
            Attachments
        };

        /// <summary>Paper handling option to use when printing the file from the print dialog
        /// [PDF:1.7:8.1].</summary>
        [Pdf(VersionEnum.PDF17)]
        public enum PaperModeEnum
        {
            /// <summary>Print single-sided.</summary>
            Simplex,
            /// <summary>Duplex and flip on the short edge of the sheet.</summary>
            DuplexShortEdge,
            /// <summary>Duplex and flip on the long edge of the sheet.</summary>
            DuplexLongEdge
        };

        private static readonly DirectionEnum DefaultDirection = DirectionEnum.LeftToRight;
        private static readonly bool DefaultFlag = false;
        private static readonly PageLayoutEnum DefaultPageLayout = PageLayoutEnum.SinglePage;
        private static readonly PageModeEnum DefaultPageMode = PageModeEnum.Simple;
        private static readonly int DefaultPrintCount = 1;
        private static readonly PdfName DefaultPrintScaledObject = PdfName.AppDefault;

        private static readonly BiDictionary<PageModeEnum, string> pmeCodes = new()
        {
            [PageModeEnum.Simple] = PdfName.UseNone.StringValue,
            [PageModeEnum.Bookmarks] = PdfName.UseOutlines.StringValue,
            [PageModeEnum.Thumbnails] = PdfName.UseThumbs.StringValue,
            [PageModeEnum.FullScreen] = PdfName.FullScreen.StringValue,
            [PageModeEnum.Layers] = PdfName.UseOC.StringValue,
            [PageModeEnum.Attachments] = PdfName.UseAttachments.StringValue
        };

        private static readonly BiDictionary<PageLayoutEnum, string> pleCodes = new()
        {
            [PageLayoutEnum.SinglePage] = PdfName.SinglePage.StringValue,
            [PageLayoutEnum.OneColumn] = PdfName.OneColumn.StringValue,
            [PageLayoutEnum.TwoColumnLeft] = PdfName.TwoColumnLeft.StringValue,
            [PageLayoutEnum.TwoColumnRight] = PdfName.TwoColumnRight.StringValue,
            [PageLayoutEnum.TwoPageLeft] = PdfName.TwoPageLeft.StringValue,
            [PageLayoutEnum.TwoPageRight] = PdfName.TwoPageRight.StringValue
        };

        private static readonly BiDictionary<PaperModeEnum, string> paperModeCodes = new()
        {
            [PaperModeEnum.Simplex] = PdfName.Simplex.StringValue,
            [PaperModeEnum.DuplexShortEdge] = PdfName.DuplexFlipShortEdge.StringValue,
            [PaperModeEnum.DuplexLongEdge] = PdfName.DuplexFlipLongEdge.StringValue
        };

        private static readonly BiDictionary<DirectionEnum, string> directionCodes = new()
        {
            [DirectionEnum.LeftToRight] = PdfName.L2R.StringValue,
            [DirectionEnum.RightToLeft] = PdfName.R2L.StringValue
        };

        public static DirectionEnum? GetDirection(string code) => GetDirection(code, null);

        public static DirectionEnum? GetDirection(string code, DirectionEnum? defaultValue)
        {
            if (code == null)
                return defaultValue;

            DirectionEnum? value = directionCodes.GetKey(code);
            if (!value.HasValue)
                throw new ArgumentException(code.ToString());

            return value.Value;
        }

        public static PdfName GetName(DirectionEnum value) => PdfName.Get(directionCodes[value], true);

        public static PaperModeEnum? GetPaperMode(string code) => GetPaperMode(code, null);

        public static PaperModeEnum? GetPaperMode(string code, PaperModeEnum? defaultValue)
        {
            if (code == null)
                return defaultValue;

            PaperModeEnum? value = paperModeCodes.GetKey(code);
            if (!value.HasValue)
                throw new ArgumentException(code.ToString());

            return value.Value;
        }

        public static PdfName GetName(PaperModeEnum value) => PdfName.Get(paperModeCodes[value], true);

        public static PageLayoutEnum? GetPageLayout(string code, PageLayoutEnum? defaultValue)
        {
            if (code == null)
                return defaultValue;

            PageLayoutEnum? value = pleCodes.GetKey(code);
            if (!value.HasValue)
                throw new ArgumentException(code.ToString());

            return value.Value;
        }

        public static PdfName GetName(PageLayoutEnum value) => PdfName.Get(pleCodes[value], true);

        public static PageModeEnum? Get(string code) => GetPageMode(code, null);

        public static PageModeEnum? GetPageMode(string code, PageModeEnum? defaultValue)
        {
            if (code == null)
                return defaultValue;

            PageModeEnum? value = pmeCodes.GetKey(code);
            if (!value.HasValue)
                throw new ArgumentException(code.ToString());

            return value.Value;
        }

        public static PdfName GetName(PageModeEnum value) => PdfName.Get(pmeCodes[value], true);

        public ViewerPreferences()
            : this((PdfDocument)null)
        { }

        public ViewerPreferences(PdfDocument context)
            : base(context, new ())
        { }

        internal ViewerPreferences(Dictionary<PdfName, PdfDirectObject> baseObject)
            : base(baseObject)
        { }

        /// <summary>Gets/Sets the predominant reading order for text.</summary>
        [Pdf(VersionEnum.PDF13)]
        public DirectionEnum Direction
        {
            get => GetDirection(GetString(PdfName.Direction), DefaultDirection).Value;
            set => this[PdfName.Direction] = (value != DefaultDirection ? GetName(value) : null);
        }

        /// <summary>Gets/Sets whether the window's title bar should display the <see
        /// cref="Information.Title">document title</see> (or the name of the PDF file instead).</summary>
        [Pdf(VersionEnum.PDF14)]
        public bool DocTitleDisplayed
        {
            get => GetBool(PdfName.DisplayDocTitle, DefaultFlag);
            set => Set(PdfName.DisplayDocTitle, value != DefaultFlag ? value : null);
        }

        /// <summary>Gets/Sets whether the viewer application's menu bar is visible when the document is
        /// active.</summary>
        public bool MenubarVisible
        {
            get => !GetBool(PdfName.HideMenubar, DefaultFlag);
            set => Set(PdfName.HideMenubar, value != !DefaultFlag ? !value : null);
        }

        /// <summary>Gets/Sets the normal page mode, that is how the document should be displayed on
        /// exiting full-screen mode.</summary>
        public PageModeEnum NormalPageMode
        {
            get => GetPageMode(GetString(PdfName.NonFullScreenPageMode), DefaultPageMode).Value;
            set => this[PdfName.NonFullScreenPageMode] = (value != DefaultPageMode ? GetName(value) : null);
        }

        /// <summary>Gets/Sets the page layout to be used when the document is opened [PDF:1.7:3.6.1].
        /// </summary>
        [Pdf(VersionEnum.PDF10)]
        public PageLayoutEnum PageLayout
        {
            get => GetPageLayout(Catalog.GetString(PdfName.PageLayout), DefaultPageLayout).Value;
            set => Catalog[PdfName.PageLayout] = (value != DefaultPageLayout ? GetName(value) : null);
        }

        /// <summary>Gets/Sets the page mode, that is how the document should be displayed when is opened
        /// [PDF:1.7:3.6.1].</summary>
        [Pdf(VersionEnum.PDF10)]
        public PageModeEnum PageMode
        {
            get => GetPageMode(Catalog.GetString(PdfName.PageMode), DefaultPageMode).Value;
            set => Catalog[PdfName.PageMode] = (value != DefaultPageMode ? GetName(value) : null);
        }

        /// <summary>Gets/Sets the paper handling option to use when printing the file from the print
        /// dialog.</summary>
        [Pdf(VersionEnum.PDF17)]
        public PaperModeEnum? PaperMode
        {
            get => GetPaperMode(GetString(PdfName.Duplex));
            set => this[PdfName.Duplex] = (value.HasValue ? GetName(value.Value) : null);
        }

        /// <summary>Gets/Sets whether the page size is used to select the input paper tray, as defined
        /// through the print dialog presented by the viewer application.</summary>
        [Pdf(VersionEnum.PDF17)]
        public bool PaperTraySelected
        {
            get => GetBool(PdfName.PickTrayByPDFSize, DefaultFlag);
            set => Set(PdfName.PickTrayByPDFSize, value != DefaultFlag ? value : null);
        }

        /// <summary>Gets/Sets the number of copies to be printed when the print dialog is opened for this
        /// file.</summary>
        [Pdf(VersionEnum.PDF17)]
        public int PrintCount
        {
            get => GetInt(PdfName.NumCopies, DefaultPrintCount);
            set
            {
                /*
                  NOTE: Supported values range from 1 to 5; values outside this range are ignored.
                */
                if (value < 1)
                { value = 1; }
                else if (value > 5)
                { value = 5; }
                Set(PdfName.NumCopies, value != DefaultPrintCount ? value : null);
            }
        }

        /// <summary>Gets/Sets the page numbers used to initialize the print dialog box when the file is
        /// printed.</summary>
        /// <remarks>Page numbers are 1-based.</remarks>
        [Pdf(VersionEnum.PDF17)]
        public IList<Interval<int>> PrintPageRanges
        {
            get
            {
                var printPageRangesObject = Get<PdfArray>(PdfName.PrintPageRange);
                if (printPageRangesObject == null
                  || printPageRangesObject.Count == 0
                  || printPageRangesObject.Count % 2 != 0)
                    return null;

                var printPageRanges = new List<Interval<int>>();
                for (int index = 0, length = printPageRangesObject.Count; index < length;)
                {
                    printPageRanges.Add(
                      new Interval<int>(
                        printPageRangesObject.GetInt(index++),
                        printPageRangesObject.GetInt(index++))
                      );
                }
                return printPageRanges;
            }
            set
            {
                PdfArray printPageRangesObject = null;
                if (value != null && value.Count > 0)
                {
                    printPageRangesObject = new PdfArrayImpl();
                    int pageCount = Catalog.Pages.Count;
                    foreach (Interval<int> printPageRange in value)
                    {
                        int low = printPageRange.Low,
                          high = printPageRange.High;
                        if (low < 1)
                            throw new ArgumentException(String.Format("Page number {0} is out of range (page numbers are 1-based).", low));
                        else if (high > pageCount)
                            throw new ArgumentException(String.Format("Page number {0} is out of range (document pages are {1}).", high, pageCount));
                        else if (low > high)
                            throw new ArgumentException(String.Format("Last page ({0}) can't be less than first one ({1}).", high, low));

                        printPageRangesObject.Add(low);
                        printPageRangesObject.Add(high);
                    }
                }
                Set(PdfName.PrintPageRange, printPageRangesObject);
            }
        }

        /// <summary>Gets/Sets whether the viewer application should use the current print scaling when a
        /// print dialog is displayed for this document.</summary>
        [Pdf(VersionEnum.PDF16)]
        public bool PrintScaled
        {
            get
            {
                var printScaledObject = Get(PdfName.PrintScaling);
                return printScaledObject == null || printScaledObject.Equals(DefaultPrintScaledObject);
            }
            set => this[PdfName.PrintScaling] = (!value ? PdfName.None : null);
        }

        /// <summary>Gets/Sets whether the viewer application's tool bars are visible when the document is
        /// active.</summary>
        public bool ToolbarVisible
        {
            get => !GetBool(PdfName.HideToolbar, DefaultFlag);
            set => Set(PdfName.HideToolbar, value != !DefaultFlag ? !value : null);
        }

        /// <summary>Gets/Sets whether to position the document's window in the center of the screen.
        /// </summary>
        public bool WindowCentered
        {
            get => GetBool(PdfName.CenterWindow, DefaultFlag);
            set => Set(PdfName.CenterWindow, value != DefaultFlag ? value : null);
        }

        /// <summary>Gets/Sets whether to resize the document's window to fit the size of the first
        /// displayed page.</summary>
        public bool WindowFitted
        {
            get => GetBool(PdfName.FitWindow, DefaultFlag);
            set => Set(PdfName.FitWindow, value != DefaultFlag ? value : null);
        }

        /// <summary>Gets/Sets whether user interface elements in the document's window (such as scroll
        /// bars and navigation controls) are visible when the document is active.</summary>
        public bool WindowUIVisible
        {
            get => !GetBool(PdfName.HideWindowUI, DefaultFlag);
            set => Set(PdfName.HideWindowUI, value != !DefaultFlag ? !value : null);
        }
    }    
}