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
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Input;
using SpatialAnalysis.FieldUtility;
using SpatialAnalysis.Visualization;
using SpatialAnalysis.CellularEnvironment;
using SpatialAnalysis.Geometry;

namespace SpatialAnalysis.FieldUtility.Visualization
{
    /// <summary>
    /// The control that hosts the gradients
    /// </summary>
    public class GradientActivityVisualHost : FrameworkElement
    {
        private OSMDocument _host { get; set; }
        /// <summary>
        /// Gets or sets the scaling factor of the gradient lines.
        /// </summary>
        /// <value>The scaling factor.</value>
        public static double ScalingFactor { get; set; }
        /// <summary>
        /// Gets or sets the thickness of the gradient lines.
        /// </summary>
        /// <value>The thickness.</value>
        public static double Thickness { get; set; }
        private Brush _brush { get; set; }

        private MenuItem Visualization_Menu { get; set; }
        private MenuItem brush_Menu { get; set; }
        private MenuItem thickness_Menu { get; set; }
        private MenuItem draw_Menu { get; set; }
        private MenuItem scale_Menu { get; set; }
        private MenuItem clear_Menu { get; set; }

        // Create a collection of child visual objects.
        private VisualCollection _children;

        /// <summary>
        /// Initializes a new instance of the <see cref="GradientActivityVisualHost"/> class.
        /// </summary>
        public GradientActivityVisualHost()
        {
            _children = new VisualCollection(this);
            this.Visualization_Menu = new MenuItem() { Header = "Gradient Field Visualization" };

            this.draw_Menu = new MenuItem() { Header = "Draw Active Gradient Field" };
            this.draw_Menu.Click += new RoutedEventHandler(draw_Menu_Click);
            this.Visualization_Menu.Items.Add(this.draw_Menu);

            this.clear_Menu = new MenuItem() { Header = "Clear Gradient Field" };
            this.clear_Menu.Click += new RoutedEventHandler(clear_Menu_Click);


            this._brush = Brushes.MidnightBlue;
            this.brush_Menu = new MenuItem() { Header = "Set Brush" };
            this.brush_Menu.Click += new RoutedEventHandler(brush_Menu_Click);
            this.Visualization_Menu.Items.Add(this.brush_Menu);

            GradientActivityVisualHost.Thickness = .5;
            this.thickness_Menu = new MenuItem() { Header = "Set Thickness" };
            this.thickness_Menu.Click += new RoutedEventHandler(thickness_Menu_Click);
            this.Visualization_Menu.Items.Add(this.thickness_Menu);

            GradientActivityVisualHost.ScalingFactor = 0.025;
            this.scale_Menu = new MenuItem() { Header = "Magnitude Scale for Visualization" };
            this.scale_Menu.Click += new RoutedEventHandler(scale_Menu_Click);
            this.Visualization_Menu.Items.Add(this.scale_Menu);


        }

        private void clear_Menu_Click(object sender, RoutedEventArgs e)
        {
            this._children.Clear();
            this.Visualization_Menu.Items.RemoveAt(0);
            this.Visualization_Menu.Items.Insert(0, this.draw_Menu);
        }
        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            this._host = null;
            this._children.Clear();
            this._children = null;
            this.draw_Menu.Click -= draw_Menu_Click;
            this.clear_Menu.Click -= clear_Menu_Click;
            this.brush_Menu.Click -= brush_Menu_Click;
            this.thickness_Menu.Click -= thickness_Menu_Click;
            this.scale_Menu.Click -= scale_Menu_Click;
            this._brush = null;
            this.Visualization_Menu = null;
            this.brush_Menu = null;
            this.thickness_Menu = null;
            this.draw_Menu = null;
            this.scale_Menu = null;
            this.clear_Menu = null;
        }

        private void scale_Menu_Click(object sender, RoutedEventArgs e)
        {
            GetNumber gn = new GetNumber("Enter New Thickness Value", "New scaling factor will be applied to the size of visualized gradient field", GradientActivityVisualHost.ScalingFactor);
            gn.Owner = this._host;
            gn.ShowDialog();
            GradientActivityVisualHost.ScalingFactor = gn.NumberValue;
            gn = null;
            if (this._children.Count != 0)
            {
                this.draw();
            }
        }

        private void thickness_Menu_Click(object sender, RoutedEventArgs e)
        {
            GetNumber gn = new GetNumber("Enter New Thickness Value", "New thickness value will be applied to the gradients", GradientActivityVisualHost.Thickness);
            gn.Owner = this._host; 
            gn.ShowDialog();
            GradientActivityVisualHost.Thickness = gn.NumberValue;
            gn = null;
            if (this._children.Count != 0)
            {
                this.draw();
            }
        }

        private void draw_Menu_Click(object sender, RoutedEventArgs e)
        {
            this.draw();
            this.Visualization_Menu.Items.RemoveAt(0);
            this.Visualization_Menu.Items.Insert(0, this.clear_Menu);
        }

        private void brush_Menu_Click(object sender, RoutedEventArgs e)
        {
            BrushPicker colorPicker = new BrushPicker(this._brush);
            colorPicker.Owner = this._host;
            colorPicker.ShowDialog();
            this._brush = colorPicker._Brush;
            colorPicker = null;
            if (this._children.Count != 0)
            {
                this.draw();
            }
        }

        // Provide a required override for the VisualChildrenCount property. 
        protected override int VisualChildrenCount
        {
            get { return _children.Count; }
        }

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
        /// Sets the host.
        /// </summary>
        /// <param name="host">The main document to which this control belongs.</param>
        public void SetHost(OSMDocument host)
        {
            this._host = host;
            this.RenderTransform = this._host.RenderTransformation;
            this._host._activities.Items.Add(this.Visualization_Menu);
            //Thickness = this._host.UnitConvertor.Convert(Thickness, 4);
            //this._host._activities.Items.Insert(this._host._activities.Items.Count - 1, this.Visualization_Menu);
        }
        private void draw()
        {
            this._children.Clear();
            if (this._host.ActiveFieldName.Items == null || this._host.ActiveFieldName.Items.Count == 0)
            {
                MessageBox.Show("Field was not assigned!");
                return;
            }
            Activity activeField = null;
            foreach (MenuItem item in this._host.ActiveFieldName.Items)
            {
                if (item.IsChecked)
                {
                    if (this._host.AllActivities.TryGetValue((string)item.Header, out activeField))
                    {
                        break;
                    }
                }
            }
            if (activeField == null)
            {
                MessageBox.Show("Active was not found!");
                return;
            }
            Dictionary<Cell, UV> gradients = new Dictionary<Cell, UV>();
            foreach (Cell item in activeField.Potentials.Keys)
            {
                UV gradient = activeField.Differentiate(item);
                if (gradient != null)
                {
                    gradients.Add(item, gradient);
                }
            }

            double scale = this.getScaleFactor();
            DrawingVisual drawingVisual = new DrawingVisual();
            using (var dvc = drawingVisual.RenderOpen())
            {
                StreamGeometry sg = new StreamGeometry();
                using (var sgc = sg.Open())
                {
                    Point start = this.toPoint(gradients.First().Key);
                    sgc.BeginFigure(start, false, false);
                    foreach (KeyValuePair<Cell, UV> item in gradients)
                    {
                        sgc.LineTo(this.toPoint(item.Key), false, false);
                        sgc.LineTo(this.toPoint(item.Key + item.Value * GradientActivityVisualHost.ScalingFactor), true, false);
                    }
                }
                sg.Freeze();
                Pen pen = new Pen(this._brush, GradientActivityVisualHost.Thickness / scale);
                dvc.DrawGeometry(null, pen, sg);
            }
            drawingVisual.Drawing.Freeze();
            this._children.Add(drawingVisual);
        }
        private Point toPoint(UV p)
        {
            return new Point(p.U, p.V);
        }
        private double getScaleFactor()
        {
            double scale = this.RenderTransform.Value.M11 * this.RenderTransform.Value.M11 +
               this.RenderTransform.Value.M12 * this.RenderTransform.Value.M12;
            return Math.Sqrt(scale);
        }

    }
}

