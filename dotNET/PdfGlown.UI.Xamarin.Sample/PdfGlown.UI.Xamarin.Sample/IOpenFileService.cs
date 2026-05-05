using System.IO;
using System.Threading.Tasks;

namespace PdfGlown.UI.Sample
{
    public interface IOpenFileService
    {
        Task<(Stream Stream, string FileName)> OpenFileDialog();
    }
}
