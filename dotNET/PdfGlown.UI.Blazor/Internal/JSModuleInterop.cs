using Microsoft.JSInterop;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace PdfGlown.UI.Blazor.Internal
{
    [SupportedOSPlatform("browser")]
    internal partial class JSModuleInterop : IDisposable
	{
        private readonly Task<JSObject> moduleTask;
		private JSObject? module;

		public JSModuleInterop(IJSRuntime js, string moduleName, string moduleUrl)
		{
            moduleTask = JSHost.ImportAsync(moduleName, moduleUrl);
        }

		public async Task ImportAsync()
		{
			module = await moduleTask;
		}

		public void Dispose()
		{
			OnDisposingModule();
			Module.Dispose();
		}

		protected JSObject Module =>
			module ?? throw new InvalidOperationException("Make sure to run ImportAsync() first.");
		
		protected virtual void OnDisposingModule() { }
	}
}
