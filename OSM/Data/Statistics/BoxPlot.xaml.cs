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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SpatialAnalysis.Data.Statistics
{
    /// <summary>
    /// Interaction logic for BoxPlot.xaml
    /// </summary>
    public partial class BoxPlot : UserControl
    {
        /// <summary>
        /// Gets or sets the quartiles.
        /// </summary>
        /// <value>The quartiles.</value>
        public double[] Quartiles { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="BoxPlot"/> class.
        /// </summary>
        public BoxPlot()
        {
            InitializeComponent();

        }
        double getMedian(List<double> values)
        {
            int size = values.Count;
            int index = (int)((double)size / 2);
            if (size % 2 == 1)
            {
                return values[index];
            }
            else
            {
                return (values[index - 1] + values[index]) / 2;
            }
        }

        /// <summary>
        /// Draws the a horizontal box plot
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="thickness">The thickness.</param>
        public void DrawHorizontally(ISpatialData data, double thickness)
        {
            this._canvas.Children.Clear();
            List<double> values = data.Data.Values.ToList();
            values.Sort();
            this.Quartiles = new double[5];
            this.Quartiles[0] = data.Min;
            this.Quartiles[4] = data.Max;
            int size = values.Count;
            List<double> first = new List<double>();
            List<double> second = new List<double>();
            int index50 = (int)((double)size / 2);
            if (size % 2 == 1)
            {
                this.Quartiles[2] = values[index50];
                first.AddRange(values.GetRange(0, index50));
                second.AddRange(values.GetRange(index50 + 1, index50));
            }
            else
            {
                this.Quartiles[2] = (values[index50 - 1] + values[index50]) / 2;
                first.AddRange(values.GetRange(0, index50 - 1));
                second.AddRange(values.GetRange(index50, index50 - 1));
            }
            this.Quartiles[1] = this.getMedian(first);
            this.Quartiles[3] = this.getMedian(second);
            double widthRatio = this._canvas.RenderSize.Width/(data.Max - data.Min);
            double w = this._canvas.RenderSize.Width;
            double h = this._canvas.RenderSize.Height;
            Line l0 = new Line
            {
                X1 = 0,
                X2 = 0,
                Y1 = 0,
                Y2 = h,
                StrokeThickness=thickness,
                Stroke= Brushes.Black,
            };
            var fill = Brushes.Gray.CloneCurrentValue();
            fill.Opacity=0.7d;
            Rectangle rect = new Rectangle
            {
                Width = (this.Quartiles[3] - this.Quartiles[1]) * widthRatio,
                Height = h,
                StrokeThickness =  thickness / 3,
                Stroke = Brushes.Black,
                Fill = fill,
            };
            Canvas.SetLeft(rect, (this.Quartiles[1] - this.Quartiles[0]) * widthRatio);
            Line l4 = new Line
            {
                X1 = (this.Quartiles[4] - this.Quartiles[0]) * widthRatio,
                X2 = (this.Quartiles[4] - this.Quartiles[0]) * widthRatio,
                Y1 = 0,
                Y2 = h,
                StrokeThickness = thickness,
                Stroke = Brushes.Black,
            };
            Line l2 = new Line
            {
                X1 = (this.Quartiles[2] - this.Quartiles[0]) * widthRatio,
                X2 = (this.Quartiles[2] - this.Quartiles[0]) * widthRatio,
                Y1 = 0,
                Y2 = h,
                StrokeThickness = thickness,
                Stroke = Brushes.Black,
            };
            Line l01 = new Line
            {
                X1 = 0,
                X2 = (this.Quartiles[1] - this.Quartiles[0]) * widthRatio,
                Y1 = h/2,
                Y2 = h/2,
                StrokeThickness = thickness/3,
                StrokeDashArray = new DoubleCollection() { 2 * thickness,  thickness },
                Stroke = Brushes.Black,
            };
            Line l34 = new Line
            {
                X1 = (this.Quartiles[3] - this.Quartiles[0]) * widthRatio,
                X2 = w,
                Y1 = h / 2,
                Y2 = h / 2,
                StrokeThickness = thickness / 3,
                StrokeDashArray = new DoubleCollection(){2 * thickness, thickness},
                Stroke = Brushes.Black,
            };
            this._canvas.Children.Add(l0);
            this._canvas.Children.Add(l4);
            this._canvas.Children.Add(l2);
            this._canvas.Children.Add(l01);
            this._canvas.Children.Add(l34);
            this._canvas.Children.Add(rect);
            Canvas.SetZIndex(l0, 1);
            Canvas.SetZIndex(l4, 1);
            Canvas.SetZIndex(l2, 1);
            Canvas.SetZIndex(l01, 1);
            Canvas.SetZIndex(l34, 1);
            Canvas.SetZIndex(rect, 0);
        }
        /// <summary>
        /// Draws the a vertical box plot
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="thickness">The thickness.</param>
        public void DrawVertically(ISpatialData data, double thickness)
        {
            this._canvas.Children.Clear();
            List<double> values = data.Data.Values.ToList();
            values.Sort();
            this.Quartiles = new double[5];
            this.Quartiles[0] = data.Min;
            this.Quartiles[4] = data.Max;
            int size = values.Count;
            List<double> first = new List<double>();
            List<double> second = new List<double>();
            int index50 = (int)((double)size / 2);
            if (size % 2 == 1)
            {
                this.Quartiles[2] = values[index50];
                first.AddRange(values.GetRange(0, index50));
                second.AddRange(values.GetRange(index50 + 1, index50));
            }
            else
            {
                this.Quartiles[2] = (values[index50 - 1] + values[index50]) / 2;
                first.AddRange(values.GetRange(0, index50 - 1));
                second.AddRange(values.GetRange(index50, index50 - 1));
            }
            this.Quartiles[1] = this.getMedian(first);
            this.Quartiles[3] = this.getMedian(second);
            double heightRatio = this._canvas.RenderSize.Height / (data.Max - data.Min);
            double w = this._canvas.RenderSize.Width;
            double h = this._canvas.RenderSize.Height;
            Line l0 = new Line
            {
                X1 = 0,
                X2 = w,
                Y1 = h,
                Y2 = h,
                StrokeThickness = thickness,
                Stroke = Brushes.Black,
            };
            var fill = Brushes.Gray.CloneCurrentValue();
            fill.Opacity = 0.7d;
            Rectangle rect = new Rectangle
            {
                Height= (this.Quartiles[3] - this.Quartiles[1]) * heightRatio,
                Width = w,
                StrokeThickness = thickness / 3,
                Stroke = Brushes.Black,
                Fill = fill,
            };
            Canvas.SetBottom(rect, (this.Quartiles[1] - this.Quartiles[0]) * heightRatio);
            Line l4 = new Line
            {
                Y1 = h - (this.Quartiles[4] - this.Quartiles[0]) * heightRatio,
                Y2 = h - (this.Quartiles[4] - this.Quartiles[0]) * heightRatio,
                X1 = 0,
                X2 = w,
                StrokeThickness = thickness,
                Stroke = Brushes.Black,
            };
            Line l2 = new Line
            {
                Y1 = h - (this.Quartiles[2] - this.Quartiles[0]) * heightRatio,
                Y2 = h - (this.Quartiles[2] - this.Quartiles[0]) * heightRatio,
                X1 = 0,
                X2 = w,
                StrokeThickness = thickness,
                Stroke = Brushes.Black,
            };
            Line l01 = new Line
            {
                Y1 = h,
                Y2 = h - (this.Quartiles[1] - this.Quartiles[0]) * heightRatio,
                X1 = w / 2,
                X2 = w / 2,
                StrokeThickness = thickness / 3,
                StrokeDashArray = new DoubleCollection() { 2 * thickness, thickness },
                Stroke = Brushes.Black,
            };
            Line l34 = new Line
            {
                Y1 = h - (this.Quartiles[3] - this.Quartiles[0]) * heightRatio,
                Y2 = 0,
                X1 = w / 2,
                X2 = w / 2,
                StrokeThickness = thickness / 3,
                StrokeDashArray = new DoubleCollection() { 2 * thickness, thickness },
                Stroke = Brushes.Black,
            };
            this._canvas.Children.Add(l0);
            this._canvas.Children.Add(l4);
            this._canvas.Children.Add(l2);
            this._canvas.Children.Add(l01);
            this._canvas.Children.Add(l34);
            this._canvas.Children.Add(rect);
            Canvas.SetZIndex(l0, 1);
            Canvas.SetZIndex(l4, 1);
            Canvas.SetZIndex(l2, 1);
            Canvas.SetZIndex(l01, 1);
            Canvas.SetZIndex(l34, 1);
            Canvas.SetZIndex(rect, 0);
        }
    }
}

