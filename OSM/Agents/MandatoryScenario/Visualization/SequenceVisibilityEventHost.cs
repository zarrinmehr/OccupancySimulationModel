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
using System.Windows.Controls;
using SpatialAnalysis.Geometry;
using System.Windows.Shapes;
using SpatialAnalysis.CellularEnvironment;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using SpatialAnalysis.IsovistUtility;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Threading;
using System.Windows.Media.Imaging;
using SpatialAnalysis.Agents.Visualization.AgentModel;
using System.Windows.Data;
using SpatialAnalysis.Events;

namespace SpatialAnalysis.Agents.MandatoryScenario.Visualization
{
    /// <summary>
    /// This class includes the interaction logic to create a Visibility Target for occupancy simulations.
    /// </summary>
    /// <seealso cref="System.Windows.Controls.Canvas" />
    class SequenceVisibilityEventHost: Canvas
    {
        private OSMDocument _host { get; set; }
        private VisualEventSettings _settings { get; set; }
        public VisibilityTarget VisualEvent { get; set; }
        private double _stroke_thickness;
        /// <summary>
        /// Initializes a new instance of the <see cref="SequenceVisibilityEventHost"/> class.
        /// </summary>
        public SequenceVisibilityEventHost() {
            this._stroke_thickness = 0.1d;
        }

        /// <summary>
        /// Sets the visual events.
        /// </summary>
        /// <param name="routedEventHandler">The routed event handler.</param>
        public void SetVisualEvents(EventHandler routedEventHandler)
        {
            this._barriers = new List<SpatialAnalysis.Geometry.BarrierPolygon>();
            this._destinations = new HashSet<Cell>();
            this._settings = new VisualEventSettings(this._host.cellularFloor, false);
            this._settings.Owner = this._host;
            this._settings._addVisibilityArea.Click += _addVisibilityArea_Click;
            this._settings._addVisibilityPoints.Click += _addVisibilityPoints_Click;
            this._settings.Closing += new System.ComponentModel.CancelEventHandler(_settings_Closing);
            this._settings.Closed += routedEventHandler;
            this._settings._close.Click += new RoutedEventHandler(_close_Click);
            this._settings.ShowDialog();
        }

        void _close_Click(object sender, RoutedEventArgs e)
        {
            HashSet<Index> allIndices = new HashSet<Index>();
            foreach (SpatialAnalysis.Geometry.BarrierPolygon item in this._barriers)
            {
                allIndices.UnionWith(this._host.cellularFloor.GetIndicesInsideBarrier(item, 0.0000001));
            }
            var visibleCells = new HashSet<int>();
            foreach (Index item in allIndices)
            {
                if (this._host.cellularFloor.ContainsCell(item) &&
                    this._host.cellularFloor.Cells[item.I, item.J].VisualOverlapState == OverlapState.Outside)
                {
                    visibleCells.Add(this._host.cellularFloor.Cells[item.I, item.J].ID);
                }
            }
            allIndices.Clear();
            allIndices = null;
            foreach (Cell item in this._destinations)
            {
                if (item.VisualOverlapState == OverlapState.Outside)
                {
                    visibleCells.Add(item.ID);
                }
            }
            if (visibleCells.Count == 0)
            {
                MessageBox.Show("Cannot Proceed without Setting Visibility Targets!");
                return;
            }
            this._settings._panel1.Visibility = this._settings._panel2.Visibility = System.Windows.Visibility.Collapsed;
            this._settings._panel4.Visibility = System.Windows.Visibility.Visible;
            var cellsOnEdge = CellUtility.GetEdgeOfField(this._host.cellularFloor, visibleCells);
            List<Isovist> isovists = new List<Isovist>(cellsOnEdge.Count);
            this._settings.progressBar.Maximum = cellsOnEdge.Count;
            this._settings.progressBar.Minimum = 0;
            double depth = this._host.cellularFloor.Origin.DistanceTo(this._host.cellularFloor.TopRight) + 1;
            foreach (int item in cellsOnEdge)
            {
                isovists.Add(new Isovist(this._host.cellularFloor.FindCell(item)));
            }
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            Parallel.ForEach(isovists, (a) =>
            {
                a.Compute(depth, BarrierType.Visual, this._host.cellularFloor, 0.0000001);
                Dispatcher.BeginInvoke(DispatcherPriority.Background,
        (SendOrPostCallback)delegate
        {
            double val = this._settings.progressBar.Value + 1;
            this._settings.progressBar.SetValue(ProgressBar.ValueProperty, val);
        }
        , null);
                Dispatcher.BeginInvoke(DispatcherPriority.Background,
        (SendOrPostCallback)delegate
        {
            double x = this._settings.progressBar.Value / this._settings.progressBar.Maximum;
            int percent = (int)(x * 100);
            int minutes = (int)Math.Floor(timer.Elapsed.TotalMinutes);
            int seconds = (int)(timer.Elapsed.Seconds);
            string message = string.Format("{0} minutes and {1} seconds\n%{2} completed",
                minutes.ToString(), seconds.ToString(), percent.ToString());
            this._settings.IsovistPrgressReport.SetValue(TextBlock.TextProperty, message);
        }
        , null);
                Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => { })).Wait(TimeSpan.FromMilliseconds(1));
            });
            var t = timer.Elapsed.TotalMilliseconds;
            timer.Stop();
            HashSet<int> visibleArea = new HashSet<int>();
            foreach (Isovist item in isovists)
            {
                visibleArea.UnionWith(item.VisibleCells);
            }
            double _h = ((UIElement)this.Parent).RenderSize.Height;
            double _w = ((UIElement)this.Parent).RenderSize.Width;
            this._host.activityVisiblArea.Source = null;
            WriteableBitmap _view = BitmapFactory.New((int)_w, (int)_h);
            this._host.activityVisiblArea.Source = _view;
            this._host.activityVisiblArea.Visibility = System.Windows.Visibility.Visible;

            using (_view.GetBitmapContext())
            {
                Color pink = Colors.Pink;
                pink.ScA = 0.4f;
                Color lightPink = Color.Add(Colors.Red, Color.Multiply(Colors.Red, 4.0f));
                lightPink.ScA = 0.5f;
                foreach (int cellID in visibleArea)
                {
                    Cell item = this._host.cellularFloor.FindCell(cellID);
                    Point p1 = this._host.Transform(item);
                    Point p2 = this._host.Transform(item + new UV(this._host.cellularFloor.CellSize, this._host.cellularFloor.CellSize));
                    _view.FillRectangle((int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y, pink);
                }
                foreach (int cellID in visibleCells)
                {
                    Cell item = this._host.cellularFloor.FindCell(cellID);
                    Point p1 = this._host.Transform(item);
                    Point p2 = this._host.Transform(item + new UV(this._host.cellularFloor.CellSize, this._host.cellularFloor.CellSize));
                    _view.FillRectangle((int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y, lightPink);
                }
            }
            foreach (Cell item in this._destinations)
            {
                UV p1 = item;
                UV p2 = item + this._host.cellularFloor.CellSize * UV.UBase;
                UV p3 = item + this._host.cellularFloor.CellSize * (UV.UBase + UV.VBase);
                UV p4 = item + this._host.cellularFloor.CellSize * UV.VBase;
                var polygon = new SpatialAnalysis.Geometry.BarrierPolygon(new UV[4] { p1, p2, p3, p4 });
                this._barriers.Add(polygon);
            }
            this.VisualEvent = new VisibilityTarget(visibleArea,
                isovists.ToArray(), this._barriers.ToArray());
            isovists.Clear();
            isovists = null;
            this._settings.Close();
        }

        void _settings_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this._barriers.Clear();
            this._barriers = null;
            this._destinations.Clear();
            this._destinations = null;
            //set events
            this._settings._addVisibilityArea.Click -= _addVisibilityArea_Click;
            this._settings._addVisibilityPoints.Click -= _addVisibilityPoints_Click;
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            this.Children.Clear();
            this.VisualEvent = null;
            this._host.activityVisiblArea.Source = null;
        }


        #region Areas that are checked for visibility
        private List<UV> _pnts { get; set; }
        private Line _line { get; set; }
        private Polyline _polyline { get; set; }
        /// <summary>
        /// created polygons
        /// </summary>
        private List<SpatialAnalysis.Geometry.BarrierPolygon> _barriers { get; set; }
        /// <summary>
        /// Picked cells
        /// </summary>
        private HashSet<Cell> _destinations { get; set; }

        #region Add area
        private void _addVisibilityArea_Click(object sender, RoutedEventArgs e)
        {
            this._settings.Hide();
            this._host.Menues.IsEnabled = false;
            this._host.UIMessage.Text = "Click to draw a region";
            this._host.UIMessage.Visibility = System.Windows.Visibility.Visible;
            this._host.Cursor = Cursors.Pen;
            this._host.FloorScene.MouseLeftButtonDown += getStartpoint;
        }

        private void getStartpoint(object sender, MouseButtonEventArgs e)
        {
            this._host.FloorScene.MouseLeftButtonDown -= getStartpoint;
            var point = this._host.InverseRenderTransform.Transform(Mouse.GetPosition(this._host.FloorScene));
            UV p = new UV(point.X, point.Y);
            Cell cell = this._host.cellularFloor.FindCell(p);
            if (cell == null)
            {
                MessageBox.Show("Pick a point on the walkable field and try again!\n");
                this._host.Menues.IsEnabled = true;
                this._host.UIMessage.Visibility = System.Windows.Visibility.Collapsed;
                this._host.Cursor = Cursors.Arrow;
                this._settings.ShowDialog();
                return;
            }
            this._pnts = new List<UV>() { p };
            this._line = new Line()
            {
                X1 = p.U,
                Y1 = p.V,
                StrokeThickness = this._stroke_thickness,
                Stroke = System.Windows.Media.Brushes.DarkRed,
            };
            this.Children.Add(this._line);
            this._polyline = new Polyline()
            {
                Stroke = System.Windows.Media.Brushes.DarkRed,
                StrokeThickness = this._line.StrokeThickness
            };
            this._polyline.Points = new PointCollection() { point };
            this.Children.Add(this._polyline);
            this.registerRegionGenerationEvents();
        }

        private void terminateRegionGeneration_Enter()
        {
            this.unregisterRegionGenerationEvents();
            if (this._pnts.Count > 1)
            {
                Polygon polygon = new Polygon();
                polygon.Points = this._polyline.Points.CloneCurrentValue();
                polygon.Stroke = Brushes.DarkRed;
                polygon.StrokeThickness = this._stroke_thickness;
                polygon.StrokeMiterLimit = 0;
                Brush brush = Brushes.DarkRed.Clone();
                brush.Opacity = .3;
                polygon.Fill = brush;
                this.Children.Add(polygon);
                this._barriers.Add(new SpatialAnalysis.Geometry.BarrierPolygon(this._pnts.ToArray()));
            }
            this.Children.Remove(this._polyline);
            this.Children.Remove(this._line);
            this._polyline.Points.Clear();
            this._polyline = null;
            this._line = null;
            this._host.Menues.IsEnabled = true;
            this._host.UIMessage.Visibility = System.Windows.Visibility.Collapsed;
            this._host.Cursor = Cursors.Arrow;
            this._settings.ShowDialog();
        }

        private void terminateRegionGeneration_Cancel()
        {
            this.unregisterRegionGenerationEvents();
            this.Children.Remove(this._polyline);
            this.Children.Remove(this._line);
            this._polyline.Points.Clear();
            this._polyline = null;
            this._line = null;
            this._host.Menues.IsEnabled = true;
            this._host.UIMessage.Visibility = System.Windows.Visibility.Collapsed;
            this._host.Cursor = Cursors.Arrow;
            this._settings.ShowDialog();
        }

        private void registerRegionGenerationEvents()
        {
            this._host.MouseBtn.MouseDown += MouseBtn_MouseDown;
            this._host.KeyDown += regionTermination_KeyDown;
            this._host.FloorScene.MouseMove += FloorScene_MouseMove;
            this._host.FloorScene.MouseLeftButtonDown += getNextRegionPoint;
        }

        private void unregisterRegionGenerationEvents()
        {
            this._host.MouseBtn.MouseDown -= MouseBtn_MouseDown;
            this._host.KeyDown -= regionTermination_KeyDown;
            this._host.FloorScene.MouseMove -= FloorScene_MouseMove;
            this._host.FloorScene.MouseLeftButtonDown -= getNextRegionPoint;
        }

        private void MouseBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.terminateRegionGeneration_Enter();
        }

        private void regionTermination_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.terminateRegionGeneration_Enter();
            }
            if (e.Key == Key.Escape)
            {
                this.terminateRegionGeneration_Cancel();
            }
        }

        private void FloorScene_MouseMove(object sender, MouseEventArgs e)
        {
            var point = this._host.InverseRenderTransform.Transform(Mouse.GetPosition(this._host.FloorScene));
            this._line.X2 = point.X;
            this._line.Y2 = point.Y;
        }

        private void getNextRegionPoint(object sender, MouseButtonEventArgs e)
        {
            var point = this._host.InverseRenderTransform.Transform(Mouse.GetPosition(this._host.FloorScene));
            UV p = new UV(point.X, point.Y);
            Cell cell = this._host.cellularFloor.FindCell(p);
            if (cell == null)
            {
                MessageBox.Show("Pick a point on the walkable field and try again!\n");
                return;
            }
            this._host.UIMessage.Text = "Click to continue; Press enter to close the region; Press escape to abort the region";
            this._line.X1 = point.X;
            this._line.Y1 = point.Y;
            this._pnts.Add(p);
            this._polyline.Points.Add(point);
        }
        #endregion

        #region Add cell
        void _addVisibilityPoints_Click(object sender, RoutedEventArgs e)
        {
            this._settings.Hide();
            this._host.Menues.IsEnabled = false;
            this._host.UIMessage.Text = "Click to pick a cell";
            this._host.UIMessage.Visibility = System.Windows.Visibility.Visible;
            this._host.Cursor = Cursors.Pen;
            this._host.FloorScene.MouseLeftButtonDown += pickCell;
        }
        private void pickCell(object sender, MouseButtonEventArgs e)
        {
            this._host.FloorScene.MouseLeftButtonDown -= pickCell;
            var point = this._host.InverseRenderTransform.Transform(Mouse.GetPosition(this._host.FloorScene));
            UV p = new UV(point.X, point.Y);
            Cell cell = this._host.cellularFloor.FindCell(p);
            if (cell == null)
            {
                MessageBox.Show("Pick a point on the floor territory and try again!\n");
            }
            else
            {
                this._destinations.Add(cell);
                var lines = cell.ToUVLines(this._host.cellularFloor.CellSize);
                Polygon polygon = new Polygon();
                polygon.Stroke = Brushes.DarkRed;
                polygon.StrokeThickness = this._stroke_thickness;
                polygon.StrokeMiterLimit = 0;
                Brush brush = Brushes.DarkRed.Clone();
                brush.Opacity = .3;
                polygon.Fill = brush;
                polygon.Points.Add(new Point(cell.U, cell.V));
                polygon.Points.Add(new Point(cell.U + this._host.cellularFloor.CellSize, cell.V));
                polygon.Points.Add(new Point(cell.U + this._host.cellularFloor.CellSize, cell.V + this._host.cellularFloor.CellSize));
                polygon.Points.Add(new Point(cell.U, cell.V + this._host.cellularFloor.CellSize));
                this.Children.Add(polygon);
            }
            this._host.Menues.IsEnabled = true;
            this._host.UIMessage.Visibility = System.Windows.Visibility.Collapsed;
            this._host.Cursor = Cursors.Arrow;
            this._settings.ShowDialog();
        }
        #endregion
        #endregion

        /// <summary>
        /// Sets the host.
        /// </summary>
        /// <param name="host">The main document to which this control belongs.</param>
        public void SetHost(OSMDocument host)
        {
            this._host = host;
            this.RenderTransform = this._host.RenderTransformation;
            this._stroke_thickness = this._host.UnitConvertor.Convert(this._stroke_thickness);
        }

    }
}

