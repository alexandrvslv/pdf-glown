using Xamarin.Forms;

namespace PdfGlown.UI
{
    public static class VisualElementExtension
    {
        public static bool IsParentsVisible(this VisualElement visual)
        {
            var parent = visual.Parent as VisualElement;
            var flag = visual.IsVisible;
            while (flag && parent != null)
            {
                flag = parent.IsVisible;
                parent = parent.Parent as VisualElement;
            }
            return flag;
        }
    }
}
