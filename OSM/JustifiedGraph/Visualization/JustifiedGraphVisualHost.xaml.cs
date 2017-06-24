/*
MIT License

Copyright (c) 2017 Saied Zarrinmehr

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SpatialAnalysis.Visualization;
using SpatialAnalysis.Miscellaneous;

namespace SpatialAnalysis.JustifiedGraph.Visualization
{
    /// <summary>
    /// Interaction logic for JustifiedGraphVisualHost.xaml
    /// </summary>
    public partial class JustifiedGraphVisualHost : Window
    {
        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            this.Owner.WindowState = this.WindowState;
        }
        private double dpi = 96;
        //Scene Scaling and utilities
        #region Zoom
        /*
            COYRIGHT: The idea and implementation of the zoom and drag support in a ScrollViewer 
            are obtained from the following article in www.codeproject.com which is protected 
            under “The Code Project Open License (CPOL) 1.02”. The implementation is integrated 
            in the JustifiedGraphVisualHost.xaml and JustifiedGraphVisualHost.xaml.cs files with 
            some changes in the names of the variables.

            Article name:   WPF simple zoom and drag support in a ScrollViewer
            Author:         Kevin Stumpf	
            URL:            https://www.codeproject.com/Articles/97871/WPF-simple-zoom-and-drag-support-in-a-ScrollViewer
            License:        https://www.codeproject.com/info/cpol10.aspx
        */
        Point? lastCenterPositionOnTarget;
        Point? lastMousePositionOnTarget;
        Point? lastDragPoint;
        ScaleTransform scaleTransform { get; set; }
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="JustifiedGraphVisualHost"/> class.
        /// </summary>
        public JustifiedGraphVisualHost()
        {
            InitializeComponent();
            this.JGVisualizer.SetNodeMovementMode(this.MovementMode);
            #region Zoom
            scrollViewer.ScrollChanged += OnScrollViewerScrollChanged;
            scrollViewer.MouseRightButtonUp += new MouseButtonEventHandler(scrollViewer_MouseRightButtonUp);
            scrollViewer.PreviewMouseWheel += OnPreviewMouseWheel;
            scrollViewer.PreviewMouseRightButtonDown += new MouseButtonEventHandler(scrollViewer_PreviewMouseRightButtonDown);
            scrollViewer.MouseMove += OnMouseMove;
            slider.ValueChanged += OnSliderValueChanged;
            scaleTransform = new ScaleTransform();
            this.grid.LayoutTransform = scaleTransform;
            #endregion
        }
        #region Zoom
        void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (lastDragPoint.HasValue)
            {
                Point posNow = e.GetPosition(scrollViewer);

                double dX = posNow.X - lastDragPoint.Value.X;
                double dY = posNow.Y - lastDragPoint.Value.Y;

                lastDragPoint = posNow;

                scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - dX);
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - dY);
            }
        }

        void scrollViewer_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var mousePos = e.GetPosition(scrollViewer);
            if (mousePos.X <= scrollViewer.ViewportWidth && mousePos.Y < scrollViewer.ViewportHeight) //make sure we still can use the scrollbars
            {
                scrollViewer.Cursor = Cursors.SizeAll;
                lastDragPoint = mousePos;
                Mouse.Capture(scrollViewer);
            }
        }

        void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            lastMousePositionOnTarget = Mouse.GetPosition(grid);

            if (e.Delta > 0)
            {
                slider.Value += 1;
            }
            if (e.Delta < 0)
            {
                slider.Value -= 1;
            }

            e.Handled = true;
        }

        void scrollViewer_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            scrollViewer.Cursor = Cursors.Arrow;
            scrollViewer.ReleaseMouseCapture();
            lastDragPoint = null;
        }

        void OnSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            scaleTransform.ScaleX = e.NewValue;
            scaleTransform.ScaleY = e.NewValue;

            var centerOfViewport = new Point(scrollViewer.ViewportWidth / 2, scrollViewer.ViewportHeight / 2);
            lastCenterPositionOnTarget = scrollViewer.TranslatePoint(centerOfViewport, grid);
        }

        void OnScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange != 0 || e.ExtentWidthChange != 0)
            {
                Point? targetBefore = null;
                Point? targetNow = null;

                if (!lastMousePositionOnTarget.HasValue)
                {
                    if (lastCenterPositionOnTarget.HasValue)
                    {
                        var centerOfViewport = new Point(scrollViewer.ViewportWidth / 2, scrollViewer.ViewportHeight / 2);
                        Point centerOfTargetNow = scrollViewer.TranslatePoint(centerOfViewport, grid);

                        targetBefore = lastCenterPositionOnTarget;
                        targetNow = centerOfTargetNow;
                    }
                }
                else
                {
                    targetBefore = lastMousePositionOnTarget;
                    targetNow = Mouse.GetPosition(grid);

                    lastMousePositionOnTarget = null;
                }

                if (targetBefore.HasValue)
                {
                    double dXInTargetPixels = targetNow.Value.X - targetBefore.Value.X;
                    double dYInTargetPixels = targetNow.Value.Y - targetBefore.Value.Y;

                    double multiplicatorX = e.ExtentWidth / grid.Width;
                    double multiplicatorY = e.ExtentHeight / grid.Height;

                    double newOffsetX = scrollViewer.HorizontalOffset - dXInTargetPixels * multiplicatorX;
                    double newOffsetY = scrollViewer.VerticalOffset - dYInTargetPixels * multiplicatorY;

                    if (double.IsNaN(newOffsetX) || double.IsNaN(newOffsetY))
                    {
                        return;
                    }

                    scrollViewer.ScrollToHorizontalOffset(newOffsetX);
                    scrollViewer.ScrollToVerticalOffset(newOffsetY);
                }
            }
        }
        #endregion

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            GetNumber getNumber0 = new GetNumber("Set Image Resolution",
                "The Scene will be exported in PGN format. Setting a high resolution value may crash this app.", this.dpi);
            getNumber0.Owner = this;
            getNumber0.ShowDialog();
            this.dpi = getNumber0.NumberValue;
            getNumber0 = null;
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Title = "Save the Scene to PNG Image format";
            dlg.DefaultExt = ".png";
            dlg.Filter = "PNG documents (.png)|*.png";
            Nullable<bool> result = dlg.ShowDialog(this);
            string fileAddress = "";
            if (result == true)
            {
                fileAddress = dlg.FileName;
            }
            else
            {
                return;
            }
            Rect bounds = VisualTreeHelper.GetDescendantBounds(this.ViewHolder);
            RenderTargetBitmap main_rtb = new RenderTargetBitmap((int)(bounds.Width * dpi / 96), (int)(bounds.Height * dpi / 96), dpi, dpi, System.Windows.Media.PixelFormats.Default);
            DrawingVisual dvFloorScene = new DrawingVisual();
            using (DrawingContext dc = dvFloorScene.RenderOpen())
            {
                VisualBrush vb = new VisualBrush(this.ViewHolder);
                dc.DrawRectangle(vb, null, bounds);
            }
            main_rtb.Render(dvFloorScene);
            BitmapEncoder pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(main_rtb));
            try
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                {
                    pngEncoder.Save(ms);
                    ms.Close();
                    System.IO.File.WriteAllBytes(fileAddress, ms.ToArray());
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Report(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }


        }
        private void SaveVisible_Click(object sender, RoutedEventArgs e)
        {
            GetNumber getNumber0 = new GetNumber("Set Image Resolution",
                "The Scene will be exported in PGN format. Setting a high resolution value may crash this app.", this.dpi);
            getNumber0.Owner = this;
            getNumber0.ShowDialog();
            this.dpi = getNumber0.NumberValue;
            getNumber0 = null;
            this.scrollViewer.Background = null;
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Title = "Save the Scene to PNG Image format";
            dlg.DefaultExt = ".png";
            dlg.Filter = "PNG documents (.png)|*.png";
            Nullable<bool> result = dlg.ShowDialog(this);
            string fileAddress = "";
            if (result == true)
            {
                fileAddress = dlg.FileName;
            }
            else
            {
                return;
            }

            Rect bounds = VisualTreeHelper.GetDescendantBounds(this.scrollViewer);
            RenderTargetBitmap main_rtb = new RenderTargetBitmap((int)(bounds.Width * dpi / 96), (int)(bounds.Height * dpi / 96), dpi, dpi, System.Windows.Media.PixelFormats.Default);
            DrawingVisual dvFloorScene = new DrawingVisual();
            using (DrawingContext dc = dvFloorScene.RenderOpen())
            {
                VisualBrush vb = new VisualBrush(this.scrollViewer);
                dc.DrawRectangle(vb, null, bounds);
            }
            main_rtb.Render(dvFloorScene);
            BitmapEncoder pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(main_rtb));
            try
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                {
                    pngEncoder.Save(ms);
                    ms.Close();
                    System.IO.File.WriteAllBytes(fileAddress, ms.ToArray());
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Report(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                this.UnregisterName(this.JGVisualizer.Name);
                this.ViewHolder.Children.Remove(this.JGVisualizer);
                this.JGVisualizer.Clear();
                this.JGVisualizer = null;
            }
            catch (Exception error)
            {
                MessageBox.Show("memory not released: " + this.GetType().ToString() + "\n" + error.Report());
            }
            base.OnClosed(e);
        }
    }
}

