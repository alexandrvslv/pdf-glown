using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Runtime.Versioning;
using System.Runtime.InteropServices.JavaScript;

namespace PdfClown.UI.Blazor.Internal
{
    [SupportedOSPlatform("browser")]
    internal partial class SKHtmlScrollInterop : JSModuleInterop
    {
        private const string ModuleName = "SKHtmlScroll";
        private const string JsFilename = ".././_content/PdfClown.UI.Blazor/SKHtmlScroll.js";
        private const string InitSymbol = "SKHtmlScroll.init";
        private const string GetDPRSymbol = "SKHtmlScroll.getDPR";
        private const string RequestLockSymbol = "SKHtmlScroll.requestLock";
        private const string SetCaptureSymbol = "SKHtmlScroll.setCapture";
        private const string ReleaseCaptureSymbol = "SKHtmlScroll.releaseCapture";
        private const string ChangeCursorSymbol = "SKHtmlScroll.changeCursor";
        private const string DeinitSymbol = "SKHtmlScroll.deinit";
        private const string UnwrappSymbol = "SKHtmlScroll.unwrapp";

        [JSImport(InitSymbol, ModuleName)]
        static partial void Init(string elementId,
            //[JSMarshalAs<JSType.Function<JSType.Array<JSType.Number>>>] Action<int[]> moveAction,
            [JSMarshalAs<JSType.Function<JSType.Object>>] Action<JSObject> moveAction,
            [JSMarshalAs<JSType.Function<JSType.Number, JSType.Number>>] Action<double, double> sizeAction,
            [JSMarshalAs<JSType.Function<JSType.Number, JSType.Boolean>>] Action<double, bool> scrollAction);

        [JSImport(DeinitSymbol, ModuleName)]
        static partial void DeInit(string elementId);

        [JSImport(GetDPRSymbol, ModuleName)]
        public static partial float GetDPR();

        [JSImport(RequestLockSymbol, ModuleName)]
        static partial void RequestLock(string elementId);

        [JSImport(SetCaptureSymbol, ModuleName)]
        static partial void SetCapture(string elementId, [JSMarshalAs<JSType.Number>] int pointerId);

        [JSImport(ReleaseCaptureSymbol, ModuleName)]
        static partial void ReleaseCapture(string elementId, [JSMarshalAs<JSType.Number>] int pointerId);

        [JSImport(ChangeCursorSymbol, ModuleName)]
        static partial void ChangeCursor(string elementId, string cursorName);

        [JSImport(UnwrappSymbol, ModuleName)]
        [return: JSMarshalAs<JSType.Array<JSType.Number>>]
        internal static partial int[] Unwrapp(JSObject jsObject);

        private static string GetCursorName(CursorType cursor)
        {
            return cursor switch
            {
                CursorType.SizeWestEast => "ew-resize",
                CursorType.BottomLeftCorner => "nesw-resize",
                CursorType.SizeNorthSouth => "ns-resize",
                CursorType.BottomRightCorner => "nwse-resize",
                CursorType.Hand => "pointer",
                CursorType.Wait => "wait",
                CursorType.SizeAll => "all-scroll",
                CursorType.Cross => "crosshair",
                CursorType.IBeam => "text",
                _ => "default",
            };
        }

        private readonly string htmlElementId;
        private readonly Action<JSObject> moveCallback;
        private readonly Action<double, double> sizeCallback;
        private readonly Action<double, bool> scrollCallback;

        public static async Task<SKHtmlScrollInterop> ImportAsync(IJSRuntime js, string elementId,
            Action<int[]> moveAction,
            Action<double, double> sizeAction,
            Action<double, bool> scrollAction)
        {
            var interop = new SKHtmlScrollInterop(js, elementId, moveAction, sizeAction, scrollAction);
            await interop.ImportAsync();
            return interop;
        }

        public SKHtmlScrollInterop(IJSRuntime js, string elementId,
            Action<int[]> moveAction,
            Action<double, double> sizeAction,
            Action<double, bool> scrollAction)
            : base(js, ModuleName, JsFilename)
        {
            htmlElementId = elementId;
            moveCallback = (jsObject) =>
            {
                var intArray = Unwrapp(jsObject);
                moveAction(intArray);
            };
            sizeCallback = sizeAction;
            scrollCallback = scrollAction;
        }

        protected override void OnDisposingModule() => DeInit();

        public void Init() => Init(htmlElementId, moveCallback, sizeCallback, scrollCallback);

        public void RequestLock() => RequestLock(htmlElementId);

        public void SetCapture(int pointerId) => SetCapture(htmlElementId, pointerId);

        public void ReleaseCapture(int pointerId) => ReleaseCapture(htmlElementId, pointerId);

        public void ChangeCursor(CursorType cursor) => ChangeCursor(htmlElementId, GetCursorName(cursor));

        public void DeInit() => DeInit(htmlElementId);

    }
}
