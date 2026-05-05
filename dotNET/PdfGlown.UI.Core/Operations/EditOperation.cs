namespace PdfGlown.UI.Operations
{
    public abstract class EditOperation
    {
        protected EditOperation(PdfDocumentViewModel document, EditorOperations operations) 
        {
            Operations = operations;
            Document = document;
        }

        public EditorOperations Operations { get; set; }

        public PdfDocumentViewModel Document { get; set; }

        public OperationType Type { get; set; }

        public int PageIndex { get; set; }

        public object? Property { get; set; }

        public object? OldValue { get; set; }

        public object? NewValue { get; set; }

        public virtual object? EndOperation()
        {
            Operations.OnEndOperation(this, null);
            return null;
        }

        public abstract void Redo();

        public abstract void Undo();

        public virtual EditOperation Clone(PdfDocumentViewModel document)
        {
            var cloned = (EditOperation)this.MemberwiseClone();
            cloned.Document = document;
            return cloned;
        }
    }
}
