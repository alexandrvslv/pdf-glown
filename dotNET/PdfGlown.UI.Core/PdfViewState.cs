using PdfGlown.UI.Operations;
using SkiaSharp;
using System;

namespace PdfGlown.UI
{
    public class PdfViewState : EventArgs
    {
        private PdfPageViewModel? pageView;
        private SKPoint pointerLocation;
        private SKMatrix navigationMatrix = SKMatrix.Identity;
        private SKMatrix windowMatrix = SKMatrix.Identity;

        public PdfViewState(IPdfView viewer)
        {
            Viewer = viewer;
        }

        //Common
        public readonly IPdfView Viewer;

        public EditorOperations Operations => Viewer.Operations;

        internal void SetWindowMatrix(float xScale, float yScale, float xTranslate, float yTranslate)
        {
            if (XScale != xScale
                || YScale != yScale
                || windowMatrix.TransX != xTranslate
                || windowMatrix.TransY != yTranslate)
            {
                var matrix = windowMatrix;
                matrix.ScaleX = xScale;
                matrix.ScaleY = yScale;
                matrix.TransX = xTranslate;
                matrix.TransY = yTranslate;
                WindowMatrix = matrix;
            }
        }        

        public float XScale
        {
            get => WindowMatrix.ScaleX;
        }

        public float YScale
        {
            get => WindowMatrix.ScaleY;
        }

        public SKMatrix ScaleMatrix = SKMatrix.Identity;

        public SKMatrix WindowMatrix
        {
            get => windowMatrix;
            set
            {
                if (windowMatrix != value)
                {
                    windowMatrix = value;
                    windowMatrix.TryInvert(out InvertWindowMatrix);
                }
            }
        }

        public SKMatrix InvertWindowMatrix;

        public SKMatrix NavigationMatrix
        {
            get => navigationMatrix;
            set
            {
                if (navigationMatrix != value)
                {
                    navigationMatrix = value;
                    NavigationMatrix.TryInvert(out InvertNavigationMatrix);

                    ViewMatrix = NavigationMatrix.PostConcat(windowMatrix);
                    ViewMatrix.TryInvert(out InvertViewMatrix);

                    NavigationArea = InvertNavigationMatrix.MapRect(SKRect.Create(WindowArea.Size));
                }
            }
        }
        public SKMatrix InvertNavigationMatrix = SKMatrix.Identity;
        public SKMatrix ViewMatrix = SKMatrix.Identity;
        public SKMatrix InvertViewMatrix = SKMatrix.Identity;

        public SKRect WindowArea;
        public SKRect NavigationArea;

        public PdfPageViewModel? PageView
        {
            get => pageView;
            set
            {
                pageView = value;
                if (pageView != null)
                {
                    PageViewMatrix = pageView.GetViewMatrix(this);
                    PageViewMatrix.TryInvert(out InvertPageViewMatrix);

                    PageMatrix = PageViewMatrix.PreConcat(pageView.Page.RotateMatrix);
                    PageMatrix.TryInvert(out InvertPageMatrix);

                    PagePointerLocation = InvertPageViewMatrix.MapPoint(PointerLocation);
                }
            }
        }

        public SKMatrix PageViewMatrix = SKMatrix.Identity;
        public SKMatrix InvertPageViewMatrix = SKMatrix.Identity;
        public SKMatrix PageMatrix = SKMatrix.Identity;
        public SKMatrix InvertPageMatrix = SKMatrix.Identity;

        //Touch
        public TouchAction TouchAction;
        public MouseButton TouchButton;
        public SKPoint PointerLocation
        {
            get => pointerLocation;
            set
            {
                if (pointerLocation != value)
                {
                    pointerLocation = value;
                    ViewPointerLocation = InvertViewMatrix.MapPoint(pointerLocation);
                }
            }
        }

        public SKPoint ViewPointerLocation;
        
        public SKPoint PagePointerLocation;
        
        public SKPoint? PressedLocation;

        public float Scale
        {
            get => ScaleMatrix.ScaleX;
            set
            {
                if (value != Scale)
                {
                    ScaleMatrix.ScaleX = value;
                    ScaleMatrix.ScaleY = value;
                }
            }
        }

    }

}
