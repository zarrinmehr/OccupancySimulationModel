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
using System.Collections.Generic;
using System;
using SpatialAnalysis.CellularEnvironment;
using SpatialAnalysis.Geometry;
using SpatialAnalysis.Miscellaneous;

namespace SpatialAnalysis.FieldUtility.Visualization
{
    /// <summary>
    /// Visualize gradient paths
    /// </summary>
    public class PathVisualHost : FrameworkElement
    {
        private OSMDocument _host { get; set; }
        private double pathThickness { get; set; }
        private Brush pathBrush { get; set; }
        private List<System.Windows.Media.Geometry> geoms { get; set; }
        private MenuItem visualizationMenu { get; set; }
        private MenuItem draw_menu { get; set; }
        private MenuItem set_iterations_menu { get; set; }
        private MenuItem set_gradient_stepSize_menu { get; set; }
        private MenuItem hide_Show_Menu { get; set; }
        private MenuItem boarderThickness_Menu { get; set; }
        private MenuItem boarderBrush_Menu { get; set; }
        private MenuItem clear_Menu { get; set; }
        private double maximum_gradient_stepSize;
        private int maximum_gradient_descent_iterations;
        // Create a collection of child visual objects.
        private VisualCollection _children { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PathVisualHost"/> class.
        /// </summary>
        public PathVisualHost()
        {
            this.maximum_gradient_stepSize = 0.1d;
            this.maximum_gradient_descent_iterations = 10000;
            this.pathThickness = 1;
            this.pathBrush = Brushes.Black;
            this.geoms = new List<System.Windows.Media.Geometry>();
            _children = new VisualCollection(this);

            this.visualizationMenu = new MenuItem() { Header = "Gradient Path" };
            
            this.draw_menu = new MenuItem() { Header = "Draw Path" };
            this.set_iterations_menu = new MenuItem { Header = "Maximum Descent Iterations" };
            this.set_gradient_stepSize_menu = new MenuItem { Header = "Maximum Gradient Step-Size" };
            this.boarderBrush_Menu = new MenuItem() { Header = "Path Brush" };
            this.boarderThickness_Menu = new MenuItem() { Header = "Path Thickness" };
            this.hide_Show_Menu = new MenuItem() { Header = "Hide" };
            this.clear_Menu = new MenuItem() { Header = "Clear" };
            this.visualizationMenu.Items.Add(this.draw_menu);
            this.visualizationMenu.Items.Add(this.set_gradient_stepSize_menu);
            this.visualizationMenu.Items.Add(this.set_iterations_menu);
            this.visualizationMenu.Items.Add(this.boarderThickness_Menu);
            this.visualizationMenu.Items.Add(this.boarderBrush_Menu);
            this.visualizationMenu.Items.Add(this.hide_Show_Menu);
            this.visualizationMenu.Items.Add(this.clear_Menu);
            this.hide_Show_Menu.Click += hide_Show_Menu_Click;
            this.boarderThickness_Menu.Click += boarderThickness_Menu_Click;
            this.boarderBrush_Menu.Click += boarderBrush_Menu_Click;
            this.clear_Menu.Click += clear_Menu_Click;
            this.draw_menu.Click += new RoutedEventHandler(findGradientPath_Click);
            this.set_iterations_menu.Click += Set_iterations_menu_Click;
            this.set_gradient_stepSize_menu.Click += Set_stepFactor_menu_Click;
        }

        private void Set_stepFactor_menu_Click(object sender, RoutedEventArgs e)
        {

            GetNumber getNumber = new GetNumber("Gradient Step Size Factor",
                "Gradients will be multiplied with this factor to descent.", this.maximum_gradient_stepSize);
            getNumber.Owner = this._host;
            getNumber.ShowInTaskbar = false;
            getNumber.ShowDialog();
            double number = getNumber.NumberValue;
            if (number < 0.01) { MessageBox.Show("The new factor should be larger than 0.01"); }
            else if(number>this._host.cellularFloor.CellSize) { MessageBox.Show("The new factor should be smaller than cell size: " + this._host.cellularFloor.CellSize.ToString("0.0000")); }
            else
            {
                if (number != this.maximum_gradient_stepSize)
                {
                    this.maximum_gradient_stepSize = number;
                }
            }
        }

        private void Set_iterations_menu_Click(object sender, RoutedEventArgs e)
        {
            GetNumber getNumber = new GetNumber("Set Maximum Descent Iterations",
                "Descending process will terminate after this number of iterations.", this.maximum_gradient_descent_iterations);
            getNumber.Owner = this._host;
            getNumber.ShowInTaskbar = false;
            getNumber.ShowDialog();
            int number = (int)getNumber.NumberValue;
            if (number < 100) { MessageBox.Show("Iterations should be larger than 100"); }
            else
            {
                if(number != this.maximum_gradient_descent_iterations)
                {
                    this.maximum_gradient_descent_iterations = number;
                }
            }
        }

        void clear_Menu_Click(object sender, RoutedEventArgs e)
        {
            this._children.Clear();
            this.geoms.Clear();
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            this._host = null;
            this.geoms.Clear();
            this.geoms = null;
            this.hide_Show_Menu.Click -= hide_Show_Menu_Click;
            this.boarderThickness_Menu.Click -= boarderThickness_Menu_Click;
            this.boarderBrush_Menu.Click -= boarderBrush_Menu_Click;
            this.clear_Menu.Click -= clear_Menu_Click;
            this.draw_menu.Click -= findGradientPath_Click;
            this.set_iterations_menu.Click -= Set_iterations_menu_Click;
            this.set_gradient_stepSize_menu.Click -= Set_stepFactor_menu_Click;
            this.pathBrush = null;
            this.visualizationMenu = null;
            this.draw_menu = null;
            this.hide_Show_Menu = null;
            this.boarderThickness_Menu = null;
            this.boarderBrush_Menu = null;
            this.clear_Menu = null;
        }

        void boarderBrush_Menu_Click(object sender, RoutedEventArgs e)
        {
            BrushPicker colorPicker = new BrushPicker(this.pathBrush);
            colorPicker.Owner = this._host;
            colorPicker.ShowDialog();
            this.pathBrush = colorPicker._Brush;
            //this.reDraw();                        /*Do not change the color of the previous pathes*/
            colorPicker = null;
        }

        void boarderThickness_Menu_Click(object sender, RoutedEventArgs e)
        {
            GetNumber gn = new GetNumber("Enter New Thickness Value", "New thickness value will be applied to the paths", this.pathThickness);
            gn.Owner = this._host;
            gn.ShowDialog();
            this.pathThickness = gn.NumberValue;
            this.reDraw();                          /*Do not change the thickness of the previous pathes*/
            gn = null;
        }

        void hide_Show_Menu_Click(object sender, RoutedEventArgs e)
        {
            if (this.Visibility == System.Windows.Visibility.Visible)
            {
                this.Visibility = System.Windows.Visibility.Collapsed;
                this.hide_Show_Menu.Header = "Show";
                this.boarderBrush_Menu.IsEnabled = false;
                this.boarderThickness_Menu.IsEnabled = false;
                this.clear_Menu.IsEnabled = false;
                this.draw_menu.IsEnabled = false;
            }
            else
            {
                this.Visibility = System.Windows.Visibility.Visible;
                this.hide_Show_Menu.Header = "Hide";
                this.boarderBrush_Menu.IsEnabled = true;
                this.boarderThickness_Menu.IsEnabled = true;
                this.clear_Menu.IsEnabled = true;
                this.draw_menu.IsEnabled = true;
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
        private void draw(List<UV> path)
        {
            double scale = this.getScaleFactor();
            Pen pen = new Pen(this.pathBrush, this.pathThickness / scale);

            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                StreamGeometry sg = new StreamGeometry();
                using (StreamGeometryContext sgc = sg.Open())
                {
                    sgc.BeginFigure(this.toPoint(path[0]), false, false);
                    for (int i = 1; i < path.Count; i++)
                    {
                        sgc.LineTo(this.toPoint(path[i]), true, true);
                    }
                }
                sg.Freeze();
                this.geoms.Add(sg);
                drawingContext.DrawGeometry(null, pen, sg);
            }
            drawingVisual.Drawing.Freeze();
            this._children.Add(drawingVisual);
        }

        private void reDraw()
        {
            if (this.geoms == null || this.geoms.Count == 0) return;
            double scale = this.getScaleFactor();
            Pen pen = new Pen(this.pathBrush, this.pathThickness / scale);
            _children.Clear();
            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                foreach (System.Windows.Media.Geometry geom in this.geoms)
                {
                    drawingContext.DrawGeometry(null, pen, geom);
                }
            }
            drawingVisual.Drawing.Freeze();
            this._children.Add(drawingVisual);
        }
        /// <summary>
        /// Sets the host.
        /// </summary>
        /// <param name="host">The main document to which this control belongs.</param>
        public void SetHost(OSMDocument host)
        {
            this._host = host;
            this.RenderTransform = this._host.RenderTransformation;
            this._host._activities.Items.Add(this.visualizationMenu);
            this.maximum_gradient_stepSize = this._host.UnitConvertor.Convert(0.1);
        }
        #region find gradient path
        private void findGradientPath_Click(object sender, RoutedEventArgs e)
        {
            this._host.Menues.IsEnabled = false;
            this._host.FloorScene.MouseLeftButtonDown += floorScene_GradientPathActive;
            this._host.MouseBtn.MouseDown += gradientPathRelease;
            this._host.UIMessage.Text = "Pick a point on the Field";
            this._host.UIMessage.Visibility = System.Windows.Visibility.Visible;
            this._host.Cursor = Cursors.Pen;
        }

        private void gradientPathRelease(object sender, MouseButtonEventArgs e)
        {
            this._host.FloorScene.MouseLeftButtonDown -= floorScene_GradientPathActive;
            this._host.CommandReset.MouseDown -= gradientPathRelease;
            this._host.Menues.IsEnabled = true;
            this._host.UIMessage.Visibility = System.Windows.Visibility.Hidden;
            this._host.Cursor = Cursors.Arrow;
        }

        private void floorScene_GradientPathActive(object sender, MouseButtonEventArgs e)
        {
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
                MessageBox.Show("Active field not found!");
                return;
            }
            Point p1 = Mouse.GetPosition(this._host.FloorScene);
            Point p2 = this._host.InverseRenderTransform.Transform(p1);
            UV p = new UV(p2.X, p2.Y);
            Cell cellOrigin = this._host.cellularFloor.FindCell(p);
            if (cellOrigin.VisualOverlapState != OverlapState.Outside)
            {
                MessageBox.Show("Pick a point on the walkable field and try again!\n");
                return;
            }
            try
            {
                var path = activeField.GetGradientPath(p,this.maximum_gradient_stepSize,this.maximum_gradient_descent_iterations);
                this.draw(path);
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Report());
            }
        }


        #endregion
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

