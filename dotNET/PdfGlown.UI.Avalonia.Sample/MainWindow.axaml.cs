using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Generic;
using System.IO;

namespace PdfGlown.UI.Aval.Sample;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void OnOpenFileClicked(object sender, RoutedEventArgs e)
    {
        var service = this.StorageProvider;
        try
        {
            var fileInfos = await service.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                AllowMultiple = false,
                Title = "Open Pdf",
                FileTypeFilter = [new FilePickerFileType("Pdf") { Patterns = ["*.pdf"] }]
            });
            if (fileInfos.Count == 1)
            {
                var fileInfo = fileInfos[0];
                Title = Path.GetFileNameWithoutExtension(fileInfo.Name);
                var stream = await fileInfo.OpenReadAsync();
                viewer.Load(stream);
            }
        }
        catch (Exception ex)
        {
            //await DisplayAlert("Error: " + ex.GetType().Name, ex.Message, "Close");
        }
    }
}