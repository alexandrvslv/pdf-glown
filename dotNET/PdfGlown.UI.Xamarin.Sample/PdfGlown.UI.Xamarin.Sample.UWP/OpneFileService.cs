using PdfGlown.Viewer.Test;
using PdfGlown.Viewer.Test.UWP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;

[assembly: Xamarin.Forms.Dependency(typeof(OpenFileService))]
namespace PdfGlown.Viewer.Test.UWP
{
    public class OpenFileService : IOpenFileService
    {
        public async Task<(Stream Stream, string FileName)> OpenFileDialog()
        {
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
            picker.FileTypeFilter.Add(".pdf");
            picker.FileTypeFilter.Add("*");

            var file = await picker.PickSingleFileAsync();
            if (file == null)
            {
                return (null, null);
            }
            return (await file.OpenStreamForReadAsync(), file.Path);
        }
    }
}
