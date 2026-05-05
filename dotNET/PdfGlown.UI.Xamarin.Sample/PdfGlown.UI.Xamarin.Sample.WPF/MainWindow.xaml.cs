using PdfGlown.UI.WPF;
using Xamarin.Forms;
using Xamarin.Forms.Platform.WPF;

namespace PdfGlown.UI.Sample.WPF
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : FormsApplicationPage
    {
        public MainWindow()
        {
            InitializeComponent();
            Forms.Init(new[] {
                typeof(MainWindow).Assembly,
                typeof(SKScrollViewRenderer).Assembly
            });
            LoadApplication(new Sample.App());
            DependencyService.Register<IOpenFileService, OpenFileService>();
        }
    }
}
