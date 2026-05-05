using System;

namespace PdfGlown.UI.Operations
{
    public delegate void OperationEventHandler(OperationEventArgs e);
    public class OperationEventArgs : EventArgs
    {
        public OperationEventArgs(EditOperation operation, object? result = null)
        {
            Operation = operation;
            Result = result;
        }

        public EditOperation Operation { get; }
        public object? Result { get; set; }
    }
}