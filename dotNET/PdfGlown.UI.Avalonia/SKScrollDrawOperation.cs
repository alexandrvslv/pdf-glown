using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;

namespace PdfGlown.UI.Aval;

internal class SKScrollDrawOperation : ICustomDrawOperation
{
    private readonly SKScrollView scrollView;
    private Rect bounds;

    public SKScrollDrawOperation(SKScrollView scrollView)
    {
        this.scrollView = scrollView;
    }

    public Rect Bounds
    {
        get => bounds;
        set => bounds = value;
    }

    public void Dispose()
    {
    }

    public bool Equals(ICustomDrawOperation? other) => ReferenceEquals(this, other);

    public bool HitTest(Point p) => bounds.Contains(p);

    public void Render(ImmediateDrawingContext context)
    {
        var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
        if (leaseFeature != null)
        {
            using var lease = leaseFeature.Lease();
            //lease.SkCanvas.ClipRect(Bounds.ToSKRect());
            var args = new SKPaintSurfaceEventArgs(lease.SkSurface, lease.SkCanvas);
            scrollView.OnPaintSurface(args);
            //lease.GrContext?.PurgeResources();
        }
    }
}