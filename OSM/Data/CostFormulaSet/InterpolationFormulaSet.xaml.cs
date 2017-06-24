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
using System.Collections.ObjectModel;
using MathNet.Numerics.Interpolation;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using SpatialAnalysis.Visualization;
using SpatialAnalysis.Data;
using SpatialAnalysis.Miscellaneous;

namespace SpatialAnalysis.Data.CostFormulaSet
{
    /// <summary>
    /// Interaction logic for InterpolationFormulaSet.xaml
    /// </summary>
    public partial class InterpolationFormulaSet : Window
    {
        #region Hiding the close button
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        //Hiding the close button
        private void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }
        #endregion
        private double _min { get; set; }
        private double _max { get; set; }
        /// <summary>
        /// Gets or sets the interpolation.
        /// </summary>
        /// <value>The interpolation.</value>
        public IInterpolation interpolation { get; set; }
        private SortedDictionary<double, double> _data { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="InterpolationFormulaSet"/> class.
        /// </summary>
        /// <param name="data">The data.</param>
        public InterpolationFormulaSet(SpatialDataField data)
        {
            InitializeComponent();
            this._data = new SortedDictionary<double, double>();
            this._addBtn.Click += _addBtn_Click;
            this._removeBtn.Click += _removeBtn_Click;
            this._min = data.Min;
            this._max = data.Max;
            this._ok.Click += _ok_Click;
            this.Loaded += Window_Loaded;
        }

        void _ok_Click(object sender, RoutedEventArgs e)
        {
            if (this.interpolation == null)
            {
                MessageBox.Show("Interpolation method not set yet");
            }
            else
            {
                this._graphs._graphsHost.Clear();
                this._addedPoints.Items.Clear();
                this._data.Clear();
                this._data = null;
                this._addBtn.Click -= _addBtn_Click;
                this._removeBtn.Click -= _removeBtn_Click;
                this._ok.Click -= _ok_Click;
                this.Close();
            }
        }

        void _removeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (this._addedPoints.SelectedItems != null)
            {
                KeyValuePair<double, double>[] pnts = this._addedPoints.SelectedItems.Cast<KeyValuePair<double, double>>().ToArray();
                foreach (KeyValuePair<double, double> item in pnts)
                {
                    this._data.Remove(item.Key);
                    this._addedPoints.Items.Remove(item);
                }
                this.updateFunc();
            }


        }

        void _addBtn_Click(object sender, RoutedEventArgs e)
        {
            double x, y;
            if (double.TryParse(this._x.Text, out x) && double.TryParse(this._y.Text, out y))
            {
                if (this._data.ContainsKey(x))
                {
                    MessageBox.Show("The input x value cannoy be douplicated!");
                    return;
                }
                else
                {
                    this._data.Add(x, y);
                    this._x.Text = string.Empty;
                    this._y.Text = string.Empty;
                }
            }
            else
            {
                MessageBox.Show("Invalid input");
                return;
            }
            this._addedPoints.Items.Clear();
            foreach (KeyValuePair<double,double> item in this._data)
            {
                this._addedPoints.Items.Add(item);
            }
            
            this.updateFunc();
        }

        private void updateGraph(int num)
        {
            this._graphs._graphsHost.Clear();
            try
            {
                PointCollection pntCollection = new PointCollection(); 
                double yMax = double.NegativeInfinity;
                double yMin = double.PositiveInfinity;
                double d = (this._max - this._min) / num;
                double t = 0;
                for (int i = 0; i <= num; i++)
                {
                    double x_ = this._min + t;
                    double yVal = this.interpolation.Interpolate(x_);
                    Point pnt = new Point(x_, yVal);
                    t += d;
                    pntCollection.Add(pnt);
                    yMax = (yMax < yVal) ? yVal : yMax;
                    yMin = (yMin > yVal) ? yVal : yMin;
                }
                this._graphs._yMax.Text = yMax.ToString();
                this._graphs._yMin.Text = yMin.ToString();
                this._graphs._xMin.Text = this._min.ToString();
                this._graphs._xMax.Text = this._max.ToString();
                if (yMax - yMin < .01)
                {
                    this._graphs._graphsHost.Clear();
                    throw new ArgumentException(string.Format("f(x) = {0}\n\tWPF Charts does not support drawing it!", ((yMax + yMin)/2).ToString()));
                }
                this._graphs._graphsHost.AddTrendLine(pntCollection);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

        }

        private void updateFunc()
        {
            if (this._data.Count>1)
            {
                this.interpolation = null;
                this.interpolation = CubicSpline.InterpolateBoundariesSorted(this._data.Keys.ToArray(), this._data.Values.ToArray(), SplineBoundaryCondition.Natural, 0, SplineBoundaryCondition.Natural, 0);
                this.updateGraph(20);
            }
            else
            {
                this.interpolation = null;
                this._graphs._graphsHost.Clear();
            }

        }

        void InterpolationFormulaSet_Loaded(object sender, RoutedEventArgs e)
        {
            //LineSeries _functionLine= new LineSeries();
            //this.chart.Series.Add(_functionLine);
            //http://www.c-sharpcorner.com/UploadFile/mahesh/line-chart-in-wpf/
            var points =
                new PointCollection()
                {
                    new Point(0,0),
                    new Point(1,1),
                    new Point(2,4),
                    new Point(3,9),
                    new Point(4,16),
                    new Point(5,25)
                };
            this._graphs._graphsHost.AddTrendLine(points);
        }
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            double dpi = 96;
            GetNumber getNumber0 = new GetNumber("Set Image Resolution",
                "The graph will be exported in PGN format. Setting a heigh resolution value may crash this app.", dpi);
            getNumber0.Owner = this;
            getNumber0.ShowDialog();
            dpi = getNumber0.NumberValue;
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
            Rect bounds = VisualTreeHelper.GetDescendantBounds(this._graphs);
            RenderTargetBitmap main_rtb = new RenderTargetBitmap((int)(bounds.Width * dpi / 96), (int)(bounds.Height * dpi / 96), dpi, dpi, System.Windows.Media.PixelFormats.Default);
            DrawingVisual dvFloorScene = new DrawingVisual();
            using (DrawingContext dc = dvFloorScene.RenderOpen())
            {
                VisualBrush vb = new VisualBrush(this._graphs);
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
    }
    
}

