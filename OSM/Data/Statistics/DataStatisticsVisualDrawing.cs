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
using System.Windows.Media;
using System.Windows.Controls;
using SpatialAnalysis.Events;

namespace SpatialAnalysis.Data.Statistics
{
    /// <summary>
    /// Visualizes Data Statistics .
    /// </summary>
    /// <seealso cref="System.Windows.FrameworkElement" />
    public class DataStatisticsVisualDrawing : FrameworkElement
    {
        Matrix _dataTransformMatrix;
        private MatrixTransform _geometryTransform { get; set; }
        private Brush _trendlineBrush { get; set; }
        private Brush _pointsBrush { get; set; }
        private double _thickness { get; set; }
        private VisualCollection _children;
        /// <summary>
        /// Initializes a new instance of the <see cref="DataStatisticsVisualDrawing"/> class.
        /// </summary>
        public DataStatisticsVisualDrawing()
        {
            this._children = new VisualCollection(this);
            this._trendlineBrush = Brushes.DarkRed;
            this._pointsBrush = Brushes.Green;
            this._thickness = 1.0d;
            this._dataTransformMatrix = Matrix.Identity;
            this._geometryTransform = new MatrixTransform(this._dataTransformMatrix);

        }




        // Provide a required override for the VisualChildrenCount property. 
        protected override int VisualChildrenCount
        {
            get { return _children.Count; }
        }
        /// <summary>
        /// Gets the number of children of the control.
        /// </summary>
        /// <value>The child count.</value>
        public int ChildCount { get { return _children.Count; } }
        // Provide a required override for the GetVisualChild method. 
        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index >= _children.Count)
            {
                throw new ArgumentOutOfRangeException();
            }
            return _children[index];
        }
        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            this._children.Clear();
        }
        /// <summary>
        /// Adds the trend line and data.
        /// </summary>
        /// <param name="p1">The start point.</param>
        /// <param name="p2">The end point</param>
        /// <param name="samples">The samples.</param>
        public void AddTrendLineAndData(Point p1, Point p2, List<Tuple<double,double>> samples)
        {
            
            this._children.Clear();
            DataStatVisualHost parent = (DataStatVisualHost)((Grid)this.Parent).Parent;
            double x0, x1, y0, y1;
            if (double.TryParse(parent._xMin.Text, out x0) && double.TryParse(parent._xMax.Text, out x1)
                && double.TryParse(parent._yMin.Text, out y0) && double.TryParse(parent._yMax.Text, out y1))
            {
                double yScale = -this.RenderSize.Height / (y1 - y0);
                double xScale = this.RenderSize.Width / (x1 - x0);

                this._dataTransformMatrix.M11 = xScale;
                this._dataTransformMatrix.M22 = yScale;
                this._dataTransformMatrix.OffsetY = this.RenderSize.Height - y0 * yScale;
                this._dataTransformMatrix.OffsetX = -x0 * xScale;
                this._geometryTransform.Matrix = this._dataTransformMatrix;
                double thikness = this._thickness / (2 * xScale);
                var trendlineGeometry = new LineGeometry(p1, p2);
                StreamGeometry geom = new StreamGeometry();
                using (var gc = geom.Open())
                {
                    gc.BeginFigure(new Point(samples[0].Item1, samples[0].Item2), false, false);
                    foreach (var item in samples)
                    {
                        gc.LineTo(new Point(item.Item1 - thikness, item.Item2), false, false);
                        gc.LineTo(new Point(item.Item1 + thikness, item.Item2), true, true);
                    }
                }
                trendlineGeometry.Transform = this._geometryTransform;
                geom.Transform = this._geometryTransform;
                if (trendlineGeometry != null)
                {
                    DrawingVisual visual = new DrawingVisual();
                    using (var vc = visual.RenderOpen())
                    {
                        vc.DrawGeometry(null, new Pen(this._pointsBrush, this._thickness), geom);
                        vc.DrawGeometry(null, new Pen(this._trendlineBrush, 2.5*this._thickness), trendlineGeometry);
                        
                    }
                    this._children.Add(visual);
                }
            }
        }
        /// <summary>
        /// Adds the trend line.
        /// </summary>
        /// <param name="points">The points.</param>
        public void AddTrendLine(IList<Point> points)
        {
            this._children.Clear();
            DataStatVisualHost parent = (DataStatVisualHost)((Grid)this.Parent).Parent;
            double x0, x1, y0, y1;
            if (double.TryParse(parent._xMin.Text, out x0) && double.TryParse(parent._xMax.Text, out x1)
                && double.TryParse(parent._yMin.Text, out y0) && double.TryParse(parent._yMax.Text, out y1))
            {


                Matrix translate0 = new Matrix();
                translate0.Translate(-x0, -y0);
                
                double yScale = this.RenderSize.Height / (y1 - y0);
                double xScale = this.RenderSize.Width / (x1 - x0);
                Matrix scale = new Matrix();
                scale.Scale(xScale, -yScale);


                Matrix translate1 = new Matrix();
                translate1.Translate(0, this.RenderSize.Height);

                Matrix mat = new Matrix();
                mat.Append(translate0);
                mat.Append(scale);
                mat.Append(translate1);

                
                

                this._dataTransformMatrix.M11 = xScale;
                this._dataTransformMatrix.M22 = yScale;
                this._dataTransformMatrix.OffsetY = y0;
                this._dataTransformMatrix.OffsetX = x0;
                this._geometryTransform.Matrix = mat;
                double thickness = this._thickness / (2 * xScale);
                StreamGeometry geom = new StreamGeometry();
                using (var gc = geom.Open())
                {
                    gc.BeginFigure(points.First(), false, false);
                    gc.PolyLineTo(points, true, true);
                }
                geom.Transform = this._geometryTransform;
                DrawingVisual visual = new DrawingVisual();
                using (var vc = visual.RenderOpen())
                {
                    vc.DrawGeometry(null, new Pen(this._trendlineBrush, 2.5 * this._thickness), geom);
                }
                this._children.Add(visual);
            }
        }

        /// <summary>
        /// Draws the frequency.
        /// </summary>
        /// <param name="event_">The event.</param>
        /// <exception cref="System.ArgumentException">The Amplitudes cannot be null and should include more than 1 frequencies</exception>
        public void DrawFrequency(EvaluationEvent event_)
        {
            if (!event_.HasFrequencies || event_.FrequencyAmplitudes.Length < 2)
            {
                throw new ArgumentException("The Amplitudes cannot be null and should include more than 1 frequencies");
            }
            this._children.Clear();
            double x0 = 0, x1 = Math.PI, y0 = 0, y1 = double.NegativeInfinity;
            for (int i = 0; i < event_.FrequencyAmplitudes.Length; i++)
            {
                if (event_.FrequencyAmplitudes[i] > y1) y1 = event_.FrequencyAmplitudes[i];
            }
            DataStatVisualHost parent = (DataStatVisualHost)((Grid)this.Parent).Parent;
            double h = parent.Width - 10;
            double yScale = -this.RenderSize.Height / (y1 - y0);
            double xScale = this.RenderSize.Width / (x1 - x0);

            this._dataTransformMatrix.M11 = xScale;
            this._dataTransformMatrix.M22 = yScale;
            this._dataTransformMatrix.OffsetY = this.RenderSize.Height - y0 * yScale;
            this._dataTransformMatrix.OffsetX = -x0 * xScale;
            this._geometryTransform.Matrix = this._dataTransformMatrix;
            double thickness = this._thickness / (2 * xScale);            
            parent.YMAX = y1.ToString("0.0000");

            StreamGeometry geom = new StreamGeometry();
            using (var gc = geom.Open())
            {
                gc.BeginFigure(new Point(0, event_.FrequencyAmplitudes[0]), false, false);
                double dist = Math.PI / (event_.FrequencyAmplitudes.Length - 1);
                for (int i = 0; i <= event_.FrequencyAmplitudes.Length; i++)
                {
                    gc.LineTo(new Point(i * dist, event_.FrequencyAmplitudes[i]), true, true);
                }
            }
            geom.Transform = this._geometryTransform;
            if (geom.CanFreeze)
            {
                geom.Freeze();
            }
            DrawingVisual visual = new DrawingVisual();
            using (var vc = visual.RenderOpen())
            {
                vc.DrawGeometry(null, new Pen(this._trendlineBrush, 2.5 * this._thickness), geom);
            }
            
            this._children.Add(visual);
        }



        
    }
}

