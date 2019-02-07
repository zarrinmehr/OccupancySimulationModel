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
using SpatialAnalysis.Visualization;
using SpatialAnalysis.CellularEnvironment;
using SpatialAnalysis.Geometry;

namespace SpatialAnalysis.IsovistUtility.IsovistVisualization
{
    /// <summary>
    /// Visualize polygonal isovists
    /// </summary>
    public class PolygonalIsovistVisualHost : FrameworkElement
    {
        private OSMDocument _host { get; set; }
        private double boarderThickness { get; set; }
        private Brush boarderBrush { get; set; }
        private Brush fillBrush { get; set; }
        private Brush centerBrush { get; set; }
        private double centerSize { get; set; }
        private MenuItem visualization_Menu { get; set; }
        private MenuItem hide_Show_Menu { get; set; }
        private MenuItem boarderThickness_Menu { get; set; }
        private MenuItem boarderBrush_Menu { get; set; }
        private MenuItem fillBrush_Menu { get; set; }
        private MenuItem centerBrush_Menu { get; set; }
        private MenuItem centerSize_Menu { get; set; }
        private MenuItem clear_Menu { get; set; }
        private MenuItem getIsovist_Menu { get; set; }
        private List<IsovistPolygon> _isovistPolygons;
        // Create a collection of child visual objects.
        private VisualCollection _children;
        /// <summary>
        /// Initializes a new instance of the <see cref="PolygonalIsovistVisualHost"/> class.
        /// </summary>
        public PolygonalIsovistVisualHost()
        {
            _isovistPolygons = new List<IsovistPolygon>();
            _children = new VisualCollection(this);
            this.fillBrush = Brushes.Khaki.Clone();
            this.fillBrush.Opacity = .6;
            this.centerSize = 3;
            this.centerBrush = Brushes.DarkRed;
            this.boarderThickness = 1;
            this.boarderBrush = Brushes.Black;

            this.visualization_Menu = new MenuItem() { Header = "Polygonal Isovist" };
            this.boarderBrush_Menu = new MenuItem() { Header = "Boarder Brush" };
            this.boarderThickness_Menu = new MenuItem() { Header = "Boarder Thickness" };
            this.fillBrush_Menu = new MenuItem() { Header = "Fill Brush" };
            this.hide_Show_Menu = new MenuItem() { Header = "Hide" };
            this.centerSize_Menu = new MenuItem() { Header = "Center Size" };
            this.centerBrush_Menu = new MenuItem() { Header = "Center Brush" };
            this.clear_Menu = new MenuItem() { Header = "Clear Isovists" };
            this.getIsovist_Menu = new MenuItem() { Header = "Get Polygonal Isovist" };
            this.visualization_Menu.Items.Add(this.getIsovist_Menu);
            this.visualization_Menu.Items.Add(this.hide_Show_Menu);
            this.visualization_Menu.Items.Add(this.boarderThickness_Menu);
            this.visualization_Menu.Items.Add(this.fillBrush_Menu);
            this.visualization_Menu.Items.Add(this.boarderBrush_Menu);
            this.visualization_Menu.Items.Add(this.centerBrush_Menu);
            this.visualization_Menu.Items.Add(this.centerSize_Menu);
            this.visualization_Menu.Items.Add(this.clear_Menu);
            this.getIsovist_Menu.Click += new RoutedEventHandler(getPolygonalIsovist_Click);
            this.boarderThickness_Menu.Click += new RoutedEventHandler(boarderThickness_Menu_Click);
            this.fillBrush_Menu.Click += new RoutedEventHandler(fillBrush_Menu_Click);
            this.boarderBrush_Menu.Click += new RoutedEventHandler(boarderBrush_Menu_Click);
            this.hide_Show_Menu.Click += new RoutedEventHandler(hide_Show_Menu_Click);
            this.centerSize_Menu.Click += new RoutedEventHandler(centerSize_Menu_Click);
            this.centerBrush_Menu.Click += new RoutedEventHandler(centerBrush_Menu_Click);
            this.clear_Menu.Click += new RoutedEventHandler(clear_Menu_Click);
        }

        #region polygonal isovist
        private void drawInRevit()
        {
            var timer = new System.Diagnostics.Stopwatch();
            this._host.Hide();
            UV p = this._host.OSM_to_BIM.PickPoint("Pick a vantage point to draw polygonal Isovist");
            Cell cell = this._host.cellularFloor.FindCell(p);
            if (cell == null)
            {
                MessageBox.Show("Pick a point on the walkable field and try again!\n");
                this._host.ShowDialog();
                return;
            }
            switch (this._host.IsovistBarrierType)
            {
                case BarrierType.Visual:
                    if (cell.VisualOverlapState != OverlapState.Outside)
                    {
                        MessageBox.Show("Pick a point outside visual barriers.\nTry again!");
                        this._host.ShowDialog();
                        return;
                    }
                    break;
                case BarrierType.Physical:
                    if (cell.PhysicalOverlapState != OverlapState.Outside)
                    {
                        MessageBox.Show("Pick a point outside physical barriers.\nTry again!");
                        this._host.ShowDialog();
                        return;
                    }
                    break;
                case BarrierType.Field:
                    if (cell.FieldOverlapState != OverlapState.Inside)
                    {
                        MessageBox.Show("Pick a point inside the walkable field.\nTry again!");
                        this._host.ShowDialog();
                        return;
                    }
                    break;
                case BarrierType.BarrierBuffer:
                    if (cell.BarrierBufferOverlapState != OverlapState.Outside)
                    {
                        MessageBox.Show("Pick a point outside barrier buffers.\nTry again!");
                        this._host.ShowDialog();
                        return;
                    }
                    break;
                default:
                    break;
            }
            try
            {
                timer.Start();
                HashSet<UVLine> blocks = this._host.cellularFloor.PolygonalIsovistVisualObstacles(p, this._host.IsovistDepth, this._host.IsovistBarrierType);
                BarrierPolygons isovistPolygon = this._host.BIM_To_OSM.IsovistPolygon(p, this._host.IsovistDepth, blocks);
                timer.Stop();
                isovistPolygon.Visualize(this._host.OSM_to_BIM, this._host.BIM_To_OSM.PlanElevation);
                this._host.IsovistInformation = new IsovistInformation(IsovistInformation.IsovistType.Polygonal,
                    timer.Elapsed.TotalMilliseconds, isovistPolygon.GetArea(), isovistPolygon.GetPerimeter());
            }
            catch (Exception error0)
            {
                this._host.IsovistInformation = null;
                MessageBox.Show(error0.Message);
            }
            timer = null;
            this._host.ShowDialog();
        }
        private void getPolygonalIsovist_Click(object sender, RoutedEventArgs e)
        {
            this._host.IsovistInformation = null;
            if (this._host.RevitEnv.IsChecked)
            {
                this.drawInRevit();
            }
            else
            {
                if (this.Visibility != System.Windows.Visibility.Visible)
                {
                    this.hide_Show();
                }
                this._host.Menues.IsEnabled = false;
                this._host.UIMessage.Text = "Click on your desired vantage point on screen";
                this._host.UIMessage.Visibility = System.Windows.Visibility.Visible;
                this._host.Cursor = Cursors.Pen;
                this._host.FloorScene.MouseLeftButtonDown += mouseLeftButtonDown_GetPolygonalIsovist;
                this._host.MouseBtn.MouseDown += releasePolygonalIsovistMode;
            }
        }
        private void releasePolygonalIsovistMode(object sender, MouseButtonEventArgs e)
        {
            this._host.Menues.IsEnabled = true;
            this._host.Cursor = Cursors.Arrow;
            this._host.UIMessage.Visibility = System.Windows.Visibility.Hidden;
            this._host.FloorScene.MouseLeftButtonDown -= mouseLeftButtonDown_GetPolygonalIsovist;
            this._host.CommandReset.MouseDown -= releasePolygonalIsovistMode;
        }
        private void mouseLeftButtonDown_GetPolygonalIsovist(object sender, MouseButtonEventArgs e)
        {
            var point = this._host.InverseRenderTransform.Transform(Mouse.GetPosition(this._host.FloorScene));
            UV p = new UV(point.X, point.Y);
            Cell cell = this._host.cellularFloor.FindCell(p);
            if (cell == null)
            {
                MessageBox.Show("Pick a point on the walkable field and try again!\n");
                return;
            }
            switch (this._host.IsovistBarrierType)
            {
                case BarrierType.Visual:
                    if (cell.VisualOverlapState != OverlapState.Outside)
                    {
                        MessageBox.Show("Pick a point outside visual barriers.\nTry again!");
                        return;
                    }
                    break;
                case BarrierType.Physical:
                    if (cell.PhysicalOverlapState != OverlapState.Outside)
                    {
                        MessageBox.Show("Pick a point outside physical barriers.\nTry again!");
                        return;
                    }
                    break;
                case BarrierType.Field:
                    if (cell.FieldOverlapState != OverlapState.Inside)
                    {
                        MessageBox.Show("Pick a point inside the walkable field.\nTry again!");
                        return;
                    }
                    break;
                case BarrierType.BarrierBuffer:
                    if (cell.BarrierBufferOverlapState != OverlapState.Outside)
                    {
                        MessageBox.Show("Pick a point outside barrier buffers.\nTry again!");
                        return;
                    }
                    break;
                default:
                    break;
            }
            this._children.Clear();

            var timer = new System.Diagnostics.Stopwatch();
            try
            {
                timer.Start();
                HashSet<UVLine> blocks = this._host.cellularFloor.PolygonalIsovistVisualObstacles(p, this._host.IsovistDepth, this._host.IsovistBarrierType);
                BarrierPolygons isovistPolygon = this._host.BIM_To_OSM.IsovistPolygon(p, this._host.IsovistDepth, blocks);
                IsovistPolygon newIsovistPolygon = new IsovistPolygon(isovistPolygon.BoundaryPoints, p);
                timer.Stop();
                this._host.IsovistInformation = new IsovistInformation(IsovistInformation.IsovistType.Polygonal,
                    timer.Elapsed.TotalMilliseconds, isovistPolygon.GetArea(), isovistPolygon.GetPerimeter());
                this.draw(newIsovistPolygon);
            }
            catch (Exception error0)
            {
                this._host.IsovistInformation = null;
                MessageBox.Show(error0.Message);
            }
            timer = null;

        }
        #endregion

        private void clear_Menu_Click(object sender, RoutedEventArgs e)
        {
            this._children.Clear();
            this._isovistPolygons.Clear();
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            this._host = null;
            this.visualization_Menu.Items.Clear();
            this.visualization_Menu = null;
            this.getIsovist_Menu.Click -= getPolygonalIsovist_Click;
            this.boarderThickness_Menu.Click -= boarderThickness_Menu_Click;
            this.fillBrush_Menu.Click -= fillBrush_Menu_Click;
            this.boarderBrush_Menu.Click -= boarderBrush_Menu_Click;
            this.hide_Show_Menu.Click -= hide_Show_Menu_Click;
            this.centerSize_Menu.Click -= centerSize_Menu_Click;
            this.centerBrush_Menu.Click -= centerBrush_Menu_Click;
            this.clear_Menu.Click -= clear_Menu_Click;
            this._isovistPolygons.Clear();
            this._children.Clear();
            this.boarderBrush = null;
            this.fillBrush = null;
            this.centerBrush = null;
            this.visualization_Menu = null;
            this.hide_Show_Menu = null;
            this.boarderThickness_Menu = null;
            this.boarderBrush_Menu = null;
            this.fillBrush_Menu = null;
            this.centerBrush_Menu = null;
            this.centerSize_Menu = null;
            this.clear_Menu = null;
            this.getIsovist_Menu = null;
            this._isovistPolygons = null;
            this._children = null;
        }

        private void centerBrush_Menu_Click(object sender, RoutedEventArgs e)
        {
            BrushPicker colorPicker = new BrushPicker(this.centerBrush);
            colorPicker.Owner = this._host;
            colorPicker.ShowDialog();
            this.centerBrush = colorPicker._Brush;
            this.redraw();
            colorPicker = null;
        }

        private void centerSize_Menu_Click(object sender, RoutedEventArgs e)
        {
            GetNumber gn = new GetNumber("Enter New Center Size", "New square size will be applied to the edges of Isovists", this.centerSize);
            gn.Owner = this._host;
            gn.ShowDialog();
            this.centerSize = gn.NumberValue;
            this.redraw();
            gn = null;
        }
        private void hide_Show()
        {
            if (this.Visibility == System.Windows.Visibility.Visible)
            {
                this.Visibility = System.Windows.Visibility.Collapsed;
                this.hide_Show_Menu.Header = "Show";
                this.boarderBrush_Menu.IsEnabled = false;
                this.fillBrush_Menu.IsEnabled = false;
                this.boarderThickness_Menu.IsEnabled = false;
                this.clear_Menu.IsEnabled = false;
                this.centerSize_Menu.IsEnabled = false;
                this.centerBrush_Menu.IsEnabled = false;
            }
            else
            {
                this.Visibility = System.Windows.Visibility.Visible;
                this.hide_Show_Menu.Header = "Hide";
                this.boarderBrush_Menu.IsEnabled = true;
                this.fillBrush_Menu.IsEnabled = true;
                this.boarderThickness_Menu.IsEnabled = true;
                this.clear_Menu.IsEnabled = true;
                this.centerSize_Menu.IsEnabled = true;
                this.centerBrush_Menu.IsEnabled = true;
            }
        }
        private void hide_Show_Menu_Click(object sender, RoutedEventArgs e)
        {
            this.hide_Show();
        }

        private void boarderBrush_Menu_Click(object sender, RoutedEventArgs e)
        {
            BrushPicker colorPicker = new BrushPicker(this.boarderBrush);
            colorPicker.Owner = this._host;
            colorPicker.ShowDialog();
            this.boarderBrush = colorPicker._Brush;
            this.redraw();
            colorPicker = null;
        }

        private void fillBrush_Menu_Click(object sender, RoutedEventArgs e)
        {
            BrushPicker colorPicker = new BrushPicker(this.fillBrush);
            colorPicker.Owner = this._host;
            colorPicker.ShowDialog();
            this.fillBrush = colorPicker._Brush;
            this.redraw();
            colorPicker = null;
        }

        private void boarderThickness_Menu_Click(object sender, RoutedEventArgs e)
        {
            GetNumber gn = new GetNumber("Enter New Thickness Value", "New thickness value will be applied to the edges of Isovist", this.boarderThickness);
            gn.Owner = this._host;
            gn.ShowDialog();
            this.boarderThickness = gn.NumberValue;
            this.redraw();
            gn = null;
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

        private double getScaleFactor()
        {
            double scale = this.RenderTransform.Value.M11 * this.RenderTransform.Value.M11 +
               this.RenderTransform.Value.M12 * this.RenderTransform.Value.M12;
            return Math.Sqrt(scale);
        }

        private void draw(IsovistPolygon isovistPolygon)
        {
            double scale = this.getScaleFactor();
            this._isovistPolygons.Add(isovistPolygon);
            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                StreamGeometry sg = new StreamGeometry();
                using (StreamGeometryContext sgc = sg.Open())
                {
                    sgc.BeginFigure(this.toPoint(isovistPolygon.BoundaryPoints[0]), true, true);
                    for (int i = 1; i < isovistPolygon.BoundaryPoints.Length; i++)
                    {
                        sgc.LineTo(this.toPoint(isovistPolygon.BoundaryPoints[i]), true, true);
                    }
                }
                sg.Freeze();
                drawingContext.DrawGeometry(this.fillBrush, new Pen(this.boarderBrush, this.boarderThickness / scale), sg);
                Point center = this.toPoint(isovistPolygon.VantagePoint);
                var p1 = new Point(center.X - this.centerSize / (2 * scale), center.Y);
                var p2 = new Point(center.X + this.centerSize / (2 * scale), center.Y);
                drawingContext.DrawLine(new Pen(this.centerBrush, this.centerSize / scale), p1, p2);
            }
            drawingVisual.Drawing.Freeze();
            this._children.Add(drawingVisual);
        }
        private void redraw()
        {
            this._children.Clear();
            double scale = this.getScaleFactor();
            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                StreamGeometry sg1 = new StreamGeometry();
                using (StreamGeometryContext sgc1 = sg1.Open())
                {
                    foreach (IsovistPolygon isovistPolygon in this._isovistPolygons)
                    {
                        sgc1.BeginFigure(this.toPoint(isovistPolygon.BoundaryPoints[0]), true, true);
                        for (int i = 1; i < isovistPolygon.BoundaryPoints.Length; i++)
                        {
                            sgc1.LineTo(this.toPoint(isovistPolygon.BoundaryPoints[i]), true, true);
                        }
                    }
                }
                sg1.FillRule = FillRule.Nonzero;
                sg1.Freeze();
                drawingContext.DrawGeometry(this.fillBrush, new Pen(this.boarderBrush, this.boarderThickness / scale), sg1);
                Pen p_Center = new Pen(this.centerBrush, this.centerSize / scale);
                foreach (IsovistPolygon isovistPolygon in this._isovistPolygons)
                {
                    Point center = this.toPoint(isovistPolygon.VantagePoint);
                    var p1 = new Point(center.X - this.centerSize / (2 * scale), center.Y);
                    var p2 = new Point(center.X + this.centerSize / (2 * scale), center.Y);
                    drawingContext.DrawLine(p_Center, p1, p2);
                }
            }
            drawingVisual.Drawing.Freeze();
            this._children.Add(drawingVisual);
        }
        private Point toPoint(UV uv)
        {
            return new Point(uv.U, uv.V);
        }

        /// <summary>
        /// Sets the host.
        /// </summary>
        /// <param name="host">The main document to which this control belongs.</param>
        public void SetHost(OSMDocument host)
        {
            this._host = host;
            this.RenderTransform = this._host.RenderTransformation;
            this._host.IsovistMenu.Items.Insert(2, this.visualization_Menu);
        }

    }
}

