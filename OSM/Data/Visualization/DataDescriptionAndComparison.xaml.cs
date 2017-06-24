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
using SpatialAnalysis.CellularEnvironment;
using SpatialAnalysis.FieldUtility;
using SpatialAnalysis.Visualization;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using SpatialAnalysis.Events;
using SpatialAnalysis.Miscellaneous;


namespace SpatialAnalysis.Data.Visualization
{
    /// <summary>
    /// Interaction logic for DataDescriptionAndComparison.xaml
    /// </summary>
    public partial class DataDescriptionAndComparison : System.Windows.Window
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
        private CellularFloor _cellularFloor;
        private ISpatialData data1, data2;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataDescriptionAndComparison"/> class.
        /// </summary>
        /// <param name="cellularFloor">The cellular floor.</param>
        /// <param name="activity">The activity.</param>
        /// <param name="occupancyEvents">The occupancy events.</param>
        public DataDescriptionAndComparison(CellularFloor cellularFloor)
        {
            InitializeComponent();
            this._cellularFloor = cellularFloor;
            this._dataList1.SelectionChanged += _dataList1_SelectionChanged;
            this._dataList2.SelectionChanged += _dataList2_SelectionChanged;
            this.Loaded += Window_Loaded;
        }


        public void LoadData(ISpatialData data)
        {
            this._dataList1.Items.Add(data);
            this._dataList2.Items.Add(data);
        }
        void _dataList2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.data2 = (ISpatialData)this._dataList2.SelectedValue;
            this._data2Max.Text = this.data2.Max.ToString();
            this._data2Min.Text = this.data2.Min.ToString();
            var result = new MathNet.Numerics.Statistics.DescriptiveStatistics(this.data2.Data.Values);
            this._data2Mean.Text = result.Mean.ToString();
            this._data2Variance.Text = result.Variance.ToString();
            this._boxPlot2.DrawHorizontally(this.data2, 3);
            this.loadStats();
        }

        void _dataList1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.data1 = (ISpatialData)this._dataList1.SelectedValue;
            this._data1Max.Text = this.data1.Max.ToString();
            this._data1Min.Text = this.data1.Min.ToString();
            var result = new MathNet.Numerics.Statistics.DescriptiveStatistics(this.data1.Data.Values);
            this._data1Mean.Text = result.Mean.ToString();
            this._data1Variance.Text = result.Variance.ToString();
            this._boxPlot1.DrawHorizontally(this.data1, 3);
            this.loadStats();
        }
        void loadStats()
        {
            if (this.data1 == null || this.data2 == null)
            {
                return;
            }
            this._graphs.Data1Name.Text = this.data1.Name;
            this._graphs.Data2Name.Text = this.data2.Name;
            List<double> value1 = new List<double>();
            List<double> value2 = new List<double>();
            foreach (Cell item in this._cellularFloor.Cells)
            {
                if (data1.Data.ContainsKey(item) && data2.Data.ContainsKey(item))
                {
                    value1.Add(data1.Data[item]);
                    value2.Add(data2.Data[item]);
                }
            }
            var samples = new List<Tuple<double, double>>();
            for (int i = 0; i < value1.Count; i++)
            {
                samples.Add(new Tuple<double, double>(value1[i], value2[i]));
            }
            var regression = MathNet.Numerics.LinearRegression.SimpleRegression.Fit(samples);
            this._regressionReport.Text = string.Format("Y = {0} X + {1}", regression.Item2.ToString(), regression.Item1.ToString());

            var correlation = MathNet.Numerics.Statistics.Correlation.Pearson(value1, value2);
            this._correlationReport.Text = string.Format("Pearson Correlation Coefficient: {0}", correlation.ToString());

            this._rSquared.Text = (correlation * correlation).ToString();

            this._graphs._xMin.Text = this.data1.Min.ToString();
            this._graphs._xMax.Text = this.data1.Max.ToString();
            this._graphs._yMax.Text = this.data2.Max.ToString();
            this._graphs._yMin.Text = this.data2.Min.ToString();

            double y0 = regression.Item2 * this.data1.Min + regression.Item1;
            double y1 = regression.Item2 * this.data1.Max + regression.Item1;
            Point p0 = new Point(this.data1.Min, y0);
            Point p1 = new Point(this.data1.Max, y1);
            this._graphs._graphsHost.AddTrendLineAndData(p0, p1, samples);

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

        private void _btm_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

