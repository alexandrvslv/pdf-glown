using PdfGlown.Documents.Contents;
using PdfGlown.Documents.Contents.Scanner;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PdfGlown.UI.Text
{
    public class TextSelection
    {
        private static void AppendPageBreak(StringBuilder textBuilder)
        {
            textBuilder.Append('\r');
            textBuilder.Append('\n');
            textBuilder.Append('\f');
        }

        private string? selectedString;
        private IContentContext? startPage;
        private PageTextSelection? startPageSelection;
        private TextChar hoverChar;
        private ITextString? hoverString;
        private TextChar startChar;
        private ITextString? startString;
        private ITextBlock? startBlock;
        private TextSelectionEventArgs? args;

        private TextSelectionEventArgs Args => args ??= new TextSelectionEventArgs(this);

        public Dictionary<IContentContext, PageTextSelection> Chars { get; private set; } = new();

        public string String
        {
            get
            {
                if (selectedString == null)
                {
                    var textBuilder = new StringBuilder();
                    IContentContext? prevPage = null;

                    foreach (var entry in Chars.OrderBy(x => x.Key))
                    {
                        if (prevPage != null)
                        {
                            AppendPageBreak(textBuilder);
                        }
                        foreach (var textChar in entry.Value.Chars)
                        {
                            textBuilder.Append(textChar.Value);
                        }
                        prevPage = entry.Key;
                    }
                    selectedString = textBuilder.ToString();
                }
                return selectedString;
            }
        }

        public TextChar StartChar { get => startChar; }

        public ITextString? StartString { get => startString; }

        public ITextBlock? StartBlock { get => startBlock; }

        public IContentContext? StartPage => startPage;

        public PageTextSelection? StartSelection => startPageSelection;

        public bool Any => Chars.Count > 0;// && Chars.Values.Any(c => c.Chars.Count > 0);

        public event TextSelectionEventHandler? Changed;


        public bool SetHoverChar(IContentContext page, ITextBlock textBlock, ITextString textString, TextChar textChar)
        {
            if (hoverString == textString
                && hoverChar.Equals(textChar))
                return false;
            hoverString = textString;
            hoverChar = textChar;
            return true;
        }

        public void SetStartChar(IContentContext page, ITextBlock textBlock, ITextString textString, TextChar textChar)
        {
            Clear();
            startPage = page;
            startPageSelection = GetOrCreatePageSelection(page);
            startBlock = textBlock;
            startString = textString;
            startChar = textChar;
        }

        public void ClearHoverChar()
        {
            hoverChar = TextChar.Empty;
            hoverString = null;
        }

        public void ClearStartChar()
        {
            startChar = TextChar.Empty;
            startString = null;
            startBlock = null;
            startPageSelection = null;
            startPage = null;
        }

        public PageTextSelection? GetPageSelection(IContentContext page) =>
            Chars.TryGetValue(page, out var contextTextSelection) ? contextTextSelection : null;

        public PageTextSelection GetOrCreatePageSelection(IContentContext page)
        {
            if (!Chars.TryGetValue(page, out var contextTextSelection))
                Chars[page] = contextTextSelection = new PageTextSelection();
            return contextTextSelection;
        }

        public void Reset()
        {
            selectedString = null;
            hoverString = null;
        }

        public void Clear()
        {
            Reset();
            ClearStartChar();
            ClearHoverChar();
            if (Chars.Count > 0)
            {
                foreach (var list in Chars.Values)
                    list.Dispose();
                Chars.Clear();
                OnChanged();
            }
        }

        public void OnChanged()
        {
            Reset();
            Changed?.Invoke(Args);
        }


    }
}