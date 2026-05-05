using System.Threading;

namespace PdfGlown.UI
{
    public static class Envir
    {
        public static void Init()
        {
            if (MainContext == null)
            {
                MainContext = SynchronizationContext.Current;
                MainThreadId = Thread.CurrentThread.ManagedThreadId;
            }
        }
        public static bool IsMainContext => Thread.CurrentThread.ManagedThreadId == MainThreadId;

        public static SynchronizationContext? MainContext { get; private set; }
        public static int MainThreadId { get; private set; }
    }

}

