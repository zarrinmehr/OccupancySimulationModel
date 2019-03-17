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
using SpatialAnalysis.Data;
using System.Diagnostics;
using SpatialAnalysis.Miscellaneous;

namespace SpatialAnalysis.IsovistUtility.IsovistVisualization
{
    /// <summary>
    /// Visualize cellular isovists
    /// </summary>
    public class CellularVisualHost : FrameworkElement
    {
        private EscapeRouteType _escapeRouteType { get; set; }
        private Dictionary<Cell, double> _staticCosts { get; set; }

        private OSMDocument _host { get; set; }
        private Dictionary<Cell, List<BarrierPolygon>> _edges;
        private List<IsovistEscapeRoutes> _escapeRoutes;

        private Brush _fillBrush { get; set; }

        private Brush _centerBrush { get; set; }

        private MenuItem _visualizationMenu { get; set; }
        private MenuItem _hide_Show_Menu { get; set; }
        private MenuItem _fillBrush_Menu { get; set; }
        private MenuItem _centerBrush_Menu { get; set; }
        private MenuItem _clear_Menu { get; set; }
        private MenuItem _getIsovist_Menu { get; set; }
        private MenuItem _getEscapeRoute { get; set; }
        // Create a collection of child visual objects.
        private VisualCollection _children;
        /// <summary>
        /// Initializes a new instance of the <see cref="CellularVisualHost"/> class.
        /// </summary>
        public CellularVisualHost()
        {
            _children = new VisualCollection(this);
            this._edges = new Dictionary<Cell, List<BarrierPolygon>>();
            this._escapeRoutes = new List<IsovistEscapeRoutes>();
            this._centerBrush = Brushes.DarkRed.Clone();
            this._centerBrush.Opacity = .8;
            this._fillBrush = Brushes.YellowGreen.Clone();
            this._fillBrush.Opacity = .5;

            this._visualizationMenu = new MenuItem() { Header = "Cellular Isovist" };
            this._fillBrush_Menu = new MenuItem() { Header = "Fill Brush" };
            this._hide_Show_Menu = new MenuItem() { Header = "Hide" };
            this._centerBrush_Menu = new MenuItem() { Header = "Center Brush" };
            this._clear_Menu = new MenuItem() { Header = "Clear Isovists" };
            this._getIsovist_Menu = new MenuItem() { Header = "Get Cellular Isovist" };
            this._getEscapeRoute = new MenuItem() { Header = "Get Escape Route" };
            this._visualizationMenu.Items.Add(this._getIsovist_Menu);
            this._visualizationMenu.Items.Add(this._getEscapeRoute);
            this._visualizationMenu.Items.Add(this._hide_Show_Menu);
            this._visualizationMenu.Items.Add(this._fillBrush_Menu);
            this._visualizationMenu.Items.Add(this._centerBrush_Menu);
            this._visualizationMenu.Items.Add(this._clear_Menu);

            this._centerBrush_Menu.Click += new RoutedEventHandler(centerBrush_Menu_Click);
            this._fillBrush_Menu.Click += new RoutedEventHandler(_fillBrush_Menu_Click);
            this._clear_Menu.Click += new RoutedEventHandler(clear_Menu_Click);
            this._hide_Show_Menu.Click += new RoutedEventHandler(hide_Show_Menu_Click);
            this._getIsovist_Menu.Click += new RoutedEventHandler(getCellularIsovist_Click);
            this._getEscapeRoute.Click += new RoutedEventHandler(getEscapeRoute_Click);

        }

        #region Isovist scape routes
        void getEscapeRoute_Click(object sender, RoutedEventArgs e)
        {
            if (this.Visibility != System.Windows.Visibility.Visible)
            {
                this.hide_Show();
            }
            //set the escape Route Type
            var result = MessageBox.Show("Do you want to simplify the escape routes?", 
                "Set Escape Route Type", MessageBoxButton.YesNo, MessageBoxImage.Question);
            switch (result)
            {
                case MessageBoxResult.No:
                    this._escapeRouteType = EscapeRouteType.All;
                    break;
                case MessageBoxResult.Yes:
                    this._escapeRouteType = EscapeRouteType.WeightedAndSimplified;
                    var setting = new SimplifiedEspaceRouteSetting(this._host);
                    setting.Owner = this._host;
                    setting.ShowDialog();
                    setting = null;
                    #region Calculating static costs
                    List<SpatialDataField> includedData = new List<SpatialDataField>();
                    foreach (Function function in this._host.cellularFloor.AllSpatialDataFields.Values)
                    {
                        SpatialDataField dataField = function as SpatialDataField;
                        if (dataField != null)
                        {
                            if (dataField.IncludeInActivityGeneration)
                            {
                                includedData.Add(dataField);
                            }
                        }
                    }
                    this._staticCosts = new Dictionary<Cell, double>();
                    foreach (Cell item in this._host.cellularFloor.Cells)
                    {
                        if (item.FieldOverlapState == OverlapState.Inside)
                        {
                            double cost = 0;
                            foreach (SpatialDataField dataField in includedData)
                            {
                                cost += dataField.GetCost(dataField.Data[item]);
                            }
                            this._staticCosts.Add(item, cost);
                        }
                    }
                    #endregion
                    break;
                default:
                    break;
            }
            this._host.Menues.IsEnabled = false;
            this._host.UIMessage.Text = "Click on your desired vantage point on screen";
            this._host.UIMessage.Visibility = Visibility.Visible;
            this._host.Cursor = Cursors.Pen;
            this._host.FloorScene.MouseLeftButtonDown += getEscapeRoute_MouseLeftButtonDown;
            this._host.MouseBtn.MouseDown += releaseGetEscapeRouteIsovist;
        }
        private void releaseGetEscapeRouteIsovist(object sender, MouseButtonEventArgs e)
        {
            this._host.Menues.IsEnabled = true;
            this._host.Cursor = Cursors.Arrow;
            this._host.UIMessage.Visibility = System.Windows.Visibility.Hidden;
            this._host.FloorScene.MouseLeftButtonDown -= getEscapeRoute_MouseLeftButtonDown;
            this._host.CommandReset.MouseDown -= releaseGetEscapeRouteIsovist;
        }
        private void getEscapeRoute_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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
                IsovistEscapeRoutes escapeRoutes = null;
                switch (this._escapeRouteType)
                {
                    case EscapeRouteType.All:
                        escapeRoutes = CellularIsovistCalculator.GetAllEscapeRoute(p, this._host.IsovistDepth, 
                            this._host.IsovistBarrierType,this._host.cellularFloor);
                        this._children.Clear();
                        this._edges.Clear();
                        this._escapeRoutes.Clear();
                        this.draw(escapeRoutes);
                        break;
                    case EscapeRouteType.WeightedAndSimplified:
                        escapeRoutes = CellularIsovistCalculator.GetWeightedSimplifiedEscapeRoute(p,
                            this._host.AgentIsovistExternalDepth,
                            this._host.IsovistBarrierType, this._host.cellularFloor, this._staticCosts,
                            this._host.MaximumNumberOfDestinations);
                        this._children.Clear();
                        this._edges.Clear();
                        this._escapeRoutes.Clear();
                        this.draw(escapeRoutes);
                        break;
                    default:
                        break;
                }

            }
            catch (Exception error1)
            {
                this._host.IsovistInformation = null;
                MessageBox.Show(error1.Report());
            }
        } 
        #endregion

        #region Cellular isovist

        private void drawInRevit()
        {
            this._host.Hide();
            UV p = this._host.OSM_to_BIM.PickPoint("Pick a vantage point to draw cellular Isovist");
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
            Isovist isovist = null;
            var timer = new System.Diagnostics.Stopwatch();
            try
            {
                timer.Start();
                isovist = CellularIsovistCalculator.GetIsovist(p, this._host.IsovistDepth, this._host.IsovistBarrierType, this._host.cellularFloor);
                timer.Stop();
                this._host.IsovistInformation = new IsovistInformation(IsovistInformation.IsovistType.Cellular,
                    timer.Elapsed.TotalMilliseconds, isovist.GetArea(this._host.cellularFloor.CellSize), double.NaN);
            }
            catch (Exception error0)
            {
                this._host.IsovistInformation = null;
                MessageBox.Show(error0.Report());
            }
            timer = null;
            if (isovist != null)
            {
                try
                {
                    var boundaries = isovist.GetBoundary(this._host.cellularFloor);
                    double elevation = this._host.BIM_To_OSM.PlanElevation;
                    foreach (var item in boundaries)
                    {
                        this._host.OSM_to_BIM.VisualizePolygon(item.BoundaryPoints, elevation);
                    }
                }
                catch (Exception error1)
                {
                    MessageBox.Show(error1.Report());
                }
            }
            this._host.ShowDialog();
        }
        private void getCellularIsovist_Click(object sender, RoutedEventArgs e)
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
                this._host.UIMessage.Visibility = Visibility.Visible;
                this._host.Cursor = Cursors.Pen;
                this._host.FloorScene.MouseLeftButtonDown += getCellularIsovist_MouseLeftButtonDown;
                this._host.MouseBtn.MouseDown += releaseGetCellularIsovist;
            }
        }
        private void releaseGetCellularIsovist(object sender, MouseButtonEventArgs e)
        {
            this._host.Menues.IsEnabled = true;
            this._host.Cursor = Cursors.Arrow;
            this._host.UIMessage.Visibility = System.Windows.Visibility.Hidden;
            this._host.FloorScene.MouseLeftButtonDown -= getCellularIsovist_MouseLeftButtonDown;
            this._host.CommandReset.MouseDown -= releaseGetCellularIsovist;
        }
        private void getCellularIsovist_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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
            var timer = new System.Diagnostics.Stopwatch();
            try
            {
                timer.Start();
                Isovist isovist = CellularIsovistCalculator.GetIsovist(p, this._host.IsovistDepth, this._host.IsovistBarrierType, this._host.cellularFloor);
                //Isovist isovist = CellularIsovistCalculator.GetIsovist(p,this._host.isoDepth, BarrierType.BarrierBuffer, this._host.cellularFloor);
                timer.Stop();
                this._host.IsovistInformation = new IsovistInformation(IsovistInformation.IsovistType.Cellular,
                    timer.Elapsed.TotalMilliseconds, isovist.GetArea(this._host.cellularFloor.CellSize), double.NaN);
                this.draw(isovist);
            }
            catch (Exception error1)
            {
                this._host.IsovistInformation = null;
                MessageBox.Show(error1.Report());
            }
            timer = null;
        }

        #endregion
        void hide_Show()
        {
            if (this.Visibility == System.Windows.Visibility.Visible)
            {
                this.Visibility = System.Windows.Visibility.Collapsed;
                this._hide_Show_Menu.Header = "Show";
                this._fillBrush_Menu.IsEnabled = false;
                this._clear_Menu.IsEnabled = false;
                this._centerBrush_Menu.IsEnabled = false;
            }
            else
            {
                this.Visibility = System.Windows.Visibility.Visible;
                this._hide_Show_Menu.Header = "Hide";
                this._fillBrush_Menu.IsEnabled = true;
                this._clear_Menu.IsEnabled = true;
                this._centerBrush_Menu.IsEnabled = true;
            }
        }

        void hide_Show_Menu_Click(object sender, RoutedEventArgs e)
        {
            this.hide_Show();
        }

        void clear_Menu_Click(object sender, RoutedEventArgs e)
        {
            this._children.Clear();
            this._edges.Clear();
            this._escapeRoutes.Clear();
        }

        void _fillBrush_Menu_Click(object sender, RoutedEventArgs e)
        {
            BrushPicker colorPicker = new BrushPicker(this._fillBrush);
            colorPicker.Owner = this._host;
            colorPicker.ShowDialog();
            this._fillBrush = colorPicker._Brush;
            colorPicker = null;
        }

        void centerBrush_Menu_Click(object sender, RoutedEventArgs e)
        {
            BrushPicker colorPicker = new BrushPicker(this._centerBrush);
            colorPicker.Owner = this._host;
            colorPicker.ShowDialog();
            this._centerBrush = colorPicker._Brush;
            colorPicker = null;
        }

        /// <summary>
        /// Sets the host.
        /// </summary>
        /// <param name="host">The main document to which this control belongs.</param>
        public void SetHost(OSMDocument host)
        {
            this._host = host;
            this.RenderTransform = this._host.RenderTransformation;
            this._host.IsovistMenu.Items.Insert(1, this._visualizationMenu);
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
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            this._children.Clear();
            this._edges.Clear();
            this._escapeRoutes.Clear();
            this._centerBrush_Menu.Click -= centerBrush_Menu_Click;
            this._fillBrush_Menu.Click -= _fillBrush_Menu_Click;
            this._clear_Menu.Click -= clear_Menu_Click;
            this._hide_Show_Menu.Click -= hide_Show_Menu_Click;
            this._getIsovist_Menu.Click -= getCellularIsovist_Click;
            this._getEscapeRoute.Click -= getEscapeRoute_Click;
            this._escapeRoutes = null;
            this._edges = null;
            this._fillBrush = null;
            this._centerBrush = null;
            this._visualizationMenu.Items.Clear();
            this._visualizationMenu = null;
            this._hide_Show_Menu = null;
            this._fillBrush_Menu = null;
            this._centerBrush_Menu = null;
            this._clear_Menu = null;
            this._getIsovist_Menu = null;
        }
        private void draw(Isovist isovist)
        {
            this._children.Clear();
            this._edges.Clear();
            this._escapeRoutes.Clear();
            var boundaries = isovist.GetBoundary(this._host.cellularFloor);
            this._edges.Add(isovist.VantageCell, boundaries);
            double scale = this.getScaleFactor();
            Point p1 = new Point(), p2 = new Point();
            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                StreamGeometry sg = new StreamGeometry();
                using (StreamGeometryContext sgc = sg.Open())
                {
                    foreach (BarrierPolygon item in boundaries)
                    {
                        sgc.BeginFigure(new Point(item.BoundaryPoints[0].U, item.BoundaryPoints[0].V), true, true);
                        foreach (var uv in item.BoundaryPoints)
                        {
                            sgc.LineTo(new Point(uv.U, uv.V), true, true);
                        }
                    }
                }
                drawingContext.DrawGeometry(this._fillBrush, null, sg);
                Pen pen = new Pen(this._centerBrush, this._host.cellularFloor.CellSize);
                p1.X = isovist.VantageCell.U;
                p1.Y = isovist.VantageCell.V + this._host.cellularFloor.CellSize / 2;
                p2.X = isovist.VantageCell.U + this._host.cellularFloor.CellSize;
                p2.Y = isovist.VantageCell.V + this._host.cellularFloor.CellSize / 2;
                drawingContext.DrawLine(pen, p1, p2);
            }
            drawingVisual.Drawing.Freeze();
            this._children.Add(drawingVisual);
        }
        private void draw(IsovistEscapeRoutes escapeRoutes)
        {
            this._escapeRoutes.Add(escapeRoutes);
            double scale = this.getScaleFactor();
            Point p1 = new Point(), p2 = new Point();
            DrawingVisual drawingVisual = new DrawingVisual();
            using (DrawingContext drawingContext = drawingVisual.RenderOpen())
            {
                Pen pen = new Pen(this._fillBrush, this._host.cellularFloor.CellSize);
                foreach (Cell item in escapeRoutes.EscapeRoutes)
                {
                    p1.X = item.U;
                    p1.Y = item.V + this._host.cellularFloor.CellSize / 2;
                    p2.X = item.U + this._host.cellularFloor.CellSize;
                    p2.Y = item.V + this._host.cellularFloor.CellSize / 2;
                    drawingContext.DrawLine(pen, p1, p2);
                }
                pen = new Pen(this._centerBrush, this._host.cellularFloor.CellSize);
                p1.X = escapeRoutes.VantageCell.U;
                p1.Y = escapeRoutes.VantageCell.V + this._host.cellularFloor.CellSize / 2;
                p2.X = escapeRoutes.VantageCell.U + this._host.cellularFloor.CellSize;
                p2.Y = escapeRoutes.VantageCell.V + this._host.cellularFloor.CellSize / 2;
                drawingContext.DrawLine(pen, p1, p2);
            }
            drawingVisual.Drawing.Freeze();
            this._children.Add(drawingVisual);
        }
        private Point toPoint(UV uv)
        {
            return new Point(uv.U, uv.V);
        }
        private double getScaleFactor()
        {
            double scale = this.RenderTransform.Value.M11 * this.RenderTransform.Value.M11 +
               this.RenderTransform.Value.M12 * this.RenderTransform.Value.M12;
            return Math.Sqrt(scale);
        }

    }
}

