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
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Input;
using SpatialAnalysis.FieldUtility;
using SpatialAnalysis.Visualization;
using System;
using SpatialAnalysis.Geometry;
using SpatialAnalysis.Miscellaneous;

namespace SpatialAnalysis.FieldUtility.Visualization
{
    /// <summary>
    /// Visualize gradients dynamically on mouse hover
    /// </summary>
    public class GradientVisualHost : Canvas
    {
        private OSMDocument _host { get; set; }
        private Line _gradient { get; set; }
        private MenuItem visualization_Menu { get; set; }
        private Activity _activeField { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="GradientVisualHost"/> class.
        /// </summary>
        public GradientVisualHost()
        {
            this.visualization_Menu = new MenuItem() { Header = "Dynamic Gradient Visualization" };
            this.visualization_Menu.Click += new RoutedEventHandler(visualization_Menu_Click);
            this._gradient = new Line();
        }

        private void visualization_Menu_Click(object sender, RoutedEventArgs e)
        {
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
                MessageBox.Show("Active Field was not found!");
                return;
            }
            this._activeField = activeField;
            this._host.FloorScene.MouseMove += new System.Windows.Input.MouseEventHandler(gradientVisualHost_MouseMove);
            this._host.CommandReset.Click += new RoutedEventHandler(_terminationBtn_Click);
            double scale = this.getScaleFactor();
            this._gradient.StrokeThickness = 2 * GradientActivityVisualHost.Thickness / scale;
            this._gradient.Stroke = Brushes.DarkRed;
            this._host.Cursor = Cursors.Pen;
            this._host.UIMessage.Text = "Hover the mouse to see the gradient force";
            this._host.UIMessage.Visibility = System.Windows.Visibility.Visible;
            this._host.Menues.IsEnabled = false;
            this.Children.Add(this._gradient);
        }

        private void _terminationBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Children.Clear();
            this._host.FloorScene.MouseMove -= gradientVisualHost_MouseMove;
            this._host.Cursor = Cursors.Arrow;
            this._host.UIMessage.Visibility = System.Windows.Visibility.Hidden;
            this._host.Menues.IsEnabled = true;
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            this._host = null;
            this.visualization_Menu.Click -= visualization_Menu_Click;
            this._gradient = null;
            this.visualization_Menu = null;
            this._activeField = null;
        }

        private void gradientVisualHost_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                var p_ = Mouse.GetPosition(this._host.FloorScene);
                Point p = this._host.InverseRenderTransform.Transform(p_);
                UV gradient = this._activeField.Differentiate(new UV(p.X, p.Y));
                if (gradient != null)
                {
                    gradient *= GradientActivityVisualHost.ScalingFactor;
                    Point end = new Point(p.X + gradient.U, p.Y + gradient.V);
                    this._gradient.X1 = p.X;
                    this._gradient.Y1 = p.Y;
                    this._gradient.X2 = end.X;
                    this._gradient.Y2 = end.Y;
                }
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Report());
            }
        }

        private double getScaleFactor()
        {
            double scale = this.RenderTransform.Value.M11 * this.RenderTransform.Value.M11 +
               this.RenderTransform.Value.M12 * this.RenderTransform.Value.M12;
            return Math.Sqrt(scale);
        }
        /// <summary>
        /// Sets the host.
        /// </summary>
        /// <param name="host">The main document to which this control belongs.</param>
        public void SetHost(OSMDocument host)
        {
            this._host = host;
            this.RenderTransform = this._host.RenderTransformation;
            this._host._activities.Items.Add(this.visualization_Menu);
            //this._host._activities.Items.Insert(this._host._activities.Items.Count - 1, this.visualization_Menu);
        }

    }
}

