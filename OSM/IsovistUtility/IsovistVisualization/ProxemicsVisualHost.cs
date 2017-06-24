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
using System.ComponentModel;
using SpatialAnalysis.Miscellaneous;

namespace SpatialAnalysis.IsovistUtility.IsovistVisualization
{
    /// <summary>
    /// Class ProxemicsVisualHost. Visualizes the proxemics
    /// </summary>
    /// <seealso cref="System.Windows.FrameworkElement" />
    public class ProxemicsVisualHost : FrameworkElement 
    {
        private double _opacity { get; set; }
        private OSMDocument _host { get; set; }
        private Brush[] brushes { get; set; }
        private double[] radiuses { get; set; }
        private Brush centerBrush { get; set; }
        private double centerSize { get; set; }
        private MenuItem visualization_Menu { get; set; }
        private MenuItem hide_Show_Menu { get; set; }
        private MenuItem setProximity_Menu { get; set; }
        private MenuItem setOpacity_Menu { get; set; }
        private MenuItem centerSize_Menu { get; set; }
        private MenuItem clear_Menu { get; set; }
        private MenuItem getProxemics_Menu { get; set; }
        
        // Create a collection of child visual objects.
        private VisualCollection _children;
        /// <summary>
        /// Initializes a new instance of the <see cref="ProxemicsVisualHost"/> class.
        /// </summary>
        public ProxemicsVisualHost()
        {
            _children = new VisualCollection(this);
            this.radiuses = new double[] { 1.5, 4, 12, 25 };
            this.brushes = new Brush[] { Brushes.IndianRed.Clone(), Brushes.Orange.Clone(), 
                             Brushes.DarkSeaGreen.Clone(), Brushes.LightBlue.Clone(), 
                             Brushes.AliceBlue.Clone() };
            this._opacity = .7;
            for (int i = 0; i < this.brushes.Length; i++)
            {
                this.brushes[i].Opacity = this._opacity;
            }
            this.centerSize = 3;
            this.centerBrush = Brushes.DarkRed;

            this.visualization_Menu = new MenuItem() { Header = "Proxemics" };
            this.hide_Show_Menu = new MenuItem() { Header = "Hide" };
            this.setOpacity_Menu = new MenuItem() { Header = "Set Opacity" };
            this.setProximity_Menu = new MenuItem() { Header = "Set Proximity Boundaries" };
            this.centerSize_Menu = new MenuItem() { Header = "Center Size" };
            this.clear_Menu = new MenuItem() { Header = "Clear Proxemics" };
            this.getProxemics_Menu = new MenuItem() { Header = "Get Proxemics" };

            this.visualization_Menu.Items.Add(this.getProxemics_Menu);
            this.visualization_Menu.Items.Add(this.setProximity_Menu);
            this.visualization_Menu.Items.Add(this.setOpacity_Menu);

            this.visualization_Menu.Items.Add(this.hide_Show_Menu);
            this.visualization_Menu.Items.Add(this.centerSize_Menu);
            this.visualization_Menu.Items.Add(this.clear_Menu);

            this.getProxemics_Menu.Click += new RoutedEventHandler(getProxemics_Click);
            this.hide_Show_Menu.Click += new RoutedEventHandler(hide_Show_Menu_Click);
            this.centerSize_Menu.Click += new RoutedEventHandler(centerSize_Menu_Click);
            this.clear_Menu.Click += new RoutedEventHandler(clear_Menu_Click);
            this.setProximity_Menu.Click += new RoutedEventHandler(setProximity_Menu_Click);
            this.setOpacity_Menu.Click += new RoutedEventHandler(setOpacity_Menu_Click);
        }

        private void setOpacity_Menu_Click(object sender, RoutedEventArgs e)
        {
            this.setOpacity();
        }
        private void setOpacity()
        {
            GetNumberSlider gn = new GetNumberSlider(0.0d, this._opacity, 1.0d, "Set Proxemics Opacity",
                "Set a number for the transparency of the proxamic colors");
            gn.Owner = this._host;
            gn.ShowDialog();
            this._opacity = gn.GetNumber;
            for (int i = 0; i < this.brushes.Length; i++)
            {
                this.brushes[i] = this.brushes[i].Clone();
                this.brushes[i].Opacity = this._opacity;
            }
            gn = null;
        }

        #region polygonal isovist

        private void getProxemics_Click(object sender, RoutedEventArgs e)
        {
            this._host.Menues.IsEnabled = false;
            this._host.UIMessage.Text = "Click on your desired vantage point on screen";
            this._host.UIMessage.Visibility = System.Windows.Visibility.Visible;
            this._host.Cursor = Cursors.Pen;
            this._host.FloorScene.MouseLeftButtonDown += mouseLeftButtonDown_GetProxemics;
            this._host.MouseBtn.MouseDown += releaseProxemicsMode;
        }
        private void releaseProxemicsMode(object sender, MouseButtonEventArgs e)
        {
            this._host.Menues.IsEnabled = true;
            this._host.Cursor = Cursors.Arrow;
            this._host.UIMessage.Visibility = System.Windows.Visibility.Hidden;
            this._host.FloorScene.MouseLeftButtonDown -= mouseLeftButtonDown_GetProxemics;
            this._host.CommandReset.MouseDown -= releaseProxemicsMode;
        }
        private void mouseLeftButtonDown_GetProxemics(object sender, MouseButtonEventArgs e)
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
            try
            {
                BarrierPolygons[] barriers = new BarrierPolygons[this.radiuses.Length];
                for (int i = 0; i < this.radiuses.Length; i++)
                {
                    HashSet<UVLine> blocks = this._host.cellularFloor.PolygonalIsovistVisualObstacles(p, this.radiuses[i], this._host.IsovistBarrierType);
                    barriers[i] = this._host.BIM_To_OSM.IsovistPolygon(p, this.radiuses[i], blocks);
                }
                Proxemics proxemics = new Proxemics(barriers, p);
                this.draw(proxemics);
            }
            catch (Exception error0)
            {
                MessageBox.Show(error0.Report());
            }
        }
        #endregion

        private void clear_Menu_Click(object sender, RoutedEventArgs e)
        {
            this._children.Clear();
        }
        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            this._host = null;
            this.visualization_Menu.Items.Clear();
            this._children.Clear();
            this._children = null;
            this.getProxemics_Menu.Click -= getProxemics_Click;
            this.hide_Show_Menu.Click -= hide_Show_Menu_Click;
            this.centerSize_Menu.Click -= centerSize_Menu_Click;
            this.clear_Menu.Click -= clear_Menu_Click;
            this.setProximity_Menu.Click -= setProximity_Menu_Click;
            this.setOpacity_Menu.Click -= setOpacity_Menu_Click;
            this.brushes = null;
            this.radiuses = null;
            this.centerBrush = null;
            this.visualization_Menu = null;
            this.hide_Show_Menu = null;
            this.setProximity_Menu = null;
            this.setOpacity_Menu = null;
            this.centerSize_Menu = null;
            this.clear_Menu = null;
            this.getProxemics_Menu = null;
        }
        private void centerSize_Menu_Click(object sender, RoutedEventArgs e)
        {
            GetNumber gn = new GetNumber("Enter New Center Size", "New square size will be applied to the edges of Isovists", this.centerSize);
            gn.Owner = this._host;
            gn.ShowDialog();
            this.centerSize = gn.NumberValue;
            gn = null;
        }
        private void hide_Show()
        {
            if (this.Visibility == System.Windows.Visibility.Visible)
            {
                this.Visibility = System.Windows.Visibility.Collapsed;
                this.hide_Show_Menu.Header = "Show";

                this.clear_Menu.IsEnabled = false;
                this.centerSize_Menu.IsEnabled = false;
                
            }
            else
            {
                this.Visibility = System.Windows.Visibility.Visible;
                this.hide_Show_Menu.Header = "Hide";

                this.clear_Menu.IsEnabled = true;
                this.centerSize_Menu.IsEnabled = true;
                
            }
        }
        private void hide_Show_Menu_Click(object sender, RoutedEventArgs e)
        {
            this.hide_Show();
        }
        private void setProximity_Menu_Click(object sender, RoutedEventArgs e)
        {
            BoundarySetting bs = new BoundarySetting(this.radiuses);
            bs.Owner = this._host;
            bs.ShowDialog();
            this.radiuses = bs.Radiuses;
            bs = null;
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
        private void draw(Proxemics proxemics)
        {
            double scale = this.getScaleFactor();
            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                StreamGeometry sg0 = new StreamGeometry();
                using (StreamGeometryContext sgc = sg0.Open())
                {
                    sgc.BeginFigure(this.toPoint(proxemics.ProxemicsPolygons[0].BoundaryPoints[0]), true, true);
                    for (int i = 1; i < proxemics.ProxemicsPolygons[0].BoundaryPoints.Length; i++)
                    {
                        sgc.LineTo(this.toPoint(proxemics.ProxemicsPolygons[0].BoundaryPoints[i]), true, true);
                    }
                }
                sg0.Freeze();
                drawingContext.DrawGeometry(this.brushes[0], null, sg0);
                for (int n = 1; n < this.radiuses.Length; n++)
                {
                    StreamGeometry sg = new StreamGeometry();
                    using (StreamGeometryContext sgc = sg.Open())
                    {
                        sgc.BeginFigure(this.toPoint(proxemics.ProxemicsPolygons[n - 1].BoundaryPoints[0]), true, true);
                        for (int i = 1; i < proxemics.ProxemicsPolygons[n - 1].BoundaryPoints.Length; i++)
                        {
                            sgc.LineTo(this.toPoint(proxemics.ProxemicsPolygons[n - 1].BoundaryPoints[i]), true, true);
                        }
                        sgc.BeginFigure(this.toPoint(proxemics.ProxemicsPolygons[n].BoundaryPoints[0]), true, true);
                        for (int i = 1; i < proxemics.ProxemicsPolygons[n].BoundaryPoints.Length; i++)
                        {
                            sgc.LineTo(this.toPoint(proxemics.ProxemicsPolygons[n].BoundaryPoints[i]), true, true);
                        }
                    }
                    sg.FillRule = FillRule.EvenOdd;
                    sg.Freeze();
                    drawingContext.DrawGeometry(this.brushes[n], null, sg);
                }

                Point center = this.toPoint(proxemics.Center);
                var p1 = new Point(center.X - this.centerSize / (2 * scale), center.Y);
                var p2 = new Point(center.X + this.centerSize / (2 * scale), center.Y);
                drawingContext.DrawLine(new Pen(this.centerBrush, this.centerSize / scale), p1, p2);
            }
            drawingVisual.Drawing.Freeze();
            this._children.Add(drawingVisual);
        }
        
        private Point toPoint(UV uv)
        {
            return new Point(uv.U, uv.V);
        }
        public void SetHost(OSMDocument host)
        {
            this._host = host;
            this.RenderTransform = this._host.RenderTransformation;
            this._host.IsovistMenu.Items.Insert(3, this.visualization_Menu);
        }
    }
}

