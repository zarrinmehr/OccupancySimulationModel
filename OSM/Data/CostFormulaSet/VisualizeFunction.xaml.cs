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
using SpatialAnalysis.Data;
using SpatialAnalysis.Miscellaneous;

namespace SpatialAnalysis.Data.CostFormulaSet
{
    /// <summary>
    /// Interaction logic for VisualizeFunction.xaml
    /// </summary>
    public partial class VisualizeFunction : Window
    {
        CalculateCost CostFunction { get; set; }
        double _min, _max;
        /// <summary>
        /// Initializes a new instance of the <see cref="VisualizeFunction"/> class.
        /// </summary>
        /// <param name="function">The function.</param>
        public VisualizeFunction(Function function)
        {
            InitializeComponent();
            this._name.Text = function.Name;
            SpatialDataField data = function as SpatialDataField;
            if (data != null)
            {
                this._min = data.Min;
                this._max = data.Max;
                this._MIN.IsEnabled = false;
                this._MAX.IsEnabled = false;
            }
            else
            {
                this._min = 0;
                this._max = 10;
            }
            this._MIN.Text = this._min.ToString();
            this._MAX.Text = this._max.ToString();
            this.CostFunction = function.GetCost;
            this._test.Click += new RoutedEventHandler(_test_Click);
            this._close.Click += new RoutedEventHandler(_close_Click);
        }

        void _close_Click(object sender, RoutedEventArgs e)
        {
            this._graphs._graphsHost.Clear();
            this.CostFunction = null;
            this.Close();
        }

        void _test_Click(object sender, RoutedEventArgs e)
        {
            int num = 0;
            double min=0,max=0;
            bool parsed = int.TryParse(this._interval.Text,out num) &&
                double.TryParse(this._MAX.Text, out max) &&
                double.TryParse(this._MIN.Text,out min);
            if (!parsed)
            {
                MessageBox.Show("Invalid input range or interval");
                return;
            }
            if (num<2)
            {
                MessageBox.Show("Interval should be larger than 1");
                return;
            }
            this._min = min;
            this._max = max;
            try
            {
                this._graphs._graphsHost.Clear();
                PointCollection points = new PointCollection();
                double yMax = double.NegativeInfinity;
                double yMin = double.PositiveInfinity;
                double d = (this._max - this._min) / num;
                double t = this._min;
                for (int i = 0; i <= num; i++)
                {
                    double yVal = this.CostFunction(t);
                    if (yVal == double.MaxValue || yVal == double.MinValue || yVal == double.NaN
                        || yVal == double.NegativeInfinity || yVal == double.PositiveInfinity)
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
                this.CostFunction = null;
                MessageBox.Show(error.Report());
                return;
            }
        }
        
    }
}

