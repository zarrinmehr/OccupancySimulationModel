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
using Jace;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using SpatialAnalysis.Visualization;
using SpatialAnalysis.Data;
using SpatialAnalysis.Data.Visualization;
using SpatialAnalysis.Miscellaneous;


namespace SpatialAnalysis.Data.CostFormulaSet
{
    /// <summary>
    /// Interaction logic for TextFormulaSet.xaml
    /// </summary>
    public partial class TextFormulaSet : Window
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
        /// <summary>
        /// Gets or sets the cost function.
        /// </summary>
        /// <value>The cost function.</value>
        public CalculateCost CostFunction { get; set; }
        private double _min, _max;
        OSMDocument _host;
        SpatialDataField _spatialDataField;
        /// <summary>
        /// Gets or sets the linked parameters to the cost function.
        /// </summary>
        /// <value>The linked parameters.</value>
        public HashSet<Parameter> LinkedParameters { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="TextFormulaSet"/> class.
        /// </summary>
        /// <param name="host">The main document to which this class belongs.</param>
        /// <param name="spatialDataField">The spatial data field.</param>
        public TextFormulaSet(OSMDocument host, SpatialDataField spatialDataField)
        {
            InitializeComponent();
            this._host = host;
            this._min = spatialDataField.Min;
            this._max = spatialDataField.Max;
            this._test.Click += _test_Click;
            this.main.TextChanged += main_TextChanged;
            this.Loaded += Window_Loaded;
            this._insetParameter.Click += new RoutedEventHandler(_insetParameter_Click);
            this.main.Text = spatialDataField.TextFormula;
            this._spatialDataField = spatialDataField;
            this.LinkedParameters = new HashSet<Parameter>();
        }

        void _insetParameter_Click(object sender, RoutedEventArgs e)
        {
            ParameterSetting parameterSetting = new ParameterSetting(this._host, true);
            parameterSetting.Owner = this._host;
            parameterSetting.ShowDialog();
            if (string.IsNullOrEmpty(parameterSetting.ParameterName))
            {
                return;
            }
            if (this.main.SelectedText == null)
            {
                this.main.Text = this.main.Text.Insert(this.main.CaretIndex, parameterSetting.ParameterName);
            }
            else
            {
                this.main.SelectedText = parameterSetting.ParameterName;
                this.main.CaretIndex += this.main.SelectedText.Length;
                this.main.SelectionLength = 0;
            }
        }

        void _test_Click(object sender, RoutedEventArgs e)
        {
            this._graphs._graphsHost.Clear();
            this.LinkedParameters.Clear();
            string textFormula = (string)this.main.Text.Clone();
            foreach (var item in this._host.Parameters)
            {
                if (textFormula.Contains(item.Key))
                {
                    textFormula = textFormula.Replace(item.Key, item.Value.Value.ToString());
                    this.LinkedParameters.Add(item.Value);
                }
            }
            try
            {
                CalculationEngine engine = new CalculationEngine();
                Func<double, double> func = (Func<double, double>)engine.Formula(textFormula)
                    .Parameter("X", Jace.DataType.FloatingPoint)
                    .Result(Jace.DataType.FloatingPoint)
                    .Build();
                this.CostFunction = new CalculateCost(func);
            }
            catch (Exception error)
            {
                this.CostFunction = null;

                MessageBox.Show("Failed to parse the formula!\n\t" + error.Report());
                return;
            }
            PointCollection points = new PointCollection();
            int num = 50;
            try
            {
                double yMax = double.NegativeInfinity;
                double yMin = double.PositiveInfinity;
                double d = (this._max - this._min) / num;
                double t = this._min;
                for (int i = 0; i <= num; i++)
                {
                    double yVal = this.CostFunction(t);
                    if (yVal == double.MaxValue || yVal == double.MinValue || double.IsNaN(yVal)
                        || double.IsInfinity(yVal) || double.IsNegativeInfinity(yVal) || double.IsPositiveInfinity(yVal))
                    {
                        throw new ArgumentException(yVal.ToString() + " is not a valid output for the cost function");
                    }
                    Point pnt = new Point(t, yVal);
                    t += d;
                    points.Add(pnt);
                    yMax = (yMax < yVal) ? yVal : yMax;
                    yMin = (yMin > yVal) ? yVal : yMin;
                }
                this._graphs._yMax.Text = yMax.ToString();
                this._graphs._yMin.Text = yMin.ToString();
                this._graphs._xMin.Text = this._min.ToString();
                this._graphs._xMax.Text = this._max.ToString();
                if (yMax - yMin < .01)
                {
                    throw new ArgumentException(string.Format("f(x) = {0}\n\tWPF Charts does not support drawing it!", ((yMax + yMin) / 2).ToString()));
                }
                this._graphs._graphsHost.AddTrendLine(points);
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Report());
                return;
            }
            
            this._ok.IsEnabled = true;
        }



        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/pieterderycke/Jace/wiki");
        }

        private void TextBlock_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Hand;
        }

        private void TextBlock_MouseLeave(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Arrow;
        }

        private void _ok_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void main_TextChanged(object sender, TextChangedEventArgs e)
        {
            this._ok.IsEnabled = false;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            double dpi = 96;
            GetNumber getNumber0 = new GetNumber("Set Image Resolution",
                "The graph will be exported in PGN format. Setting a heigh resolution value may crash this app.", dpi);
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

