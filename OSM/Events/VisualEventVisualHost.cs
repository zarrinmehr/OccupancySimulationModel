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
using SpatialAnalysis.Data.Visualization;
using SpatialAnalysis.Miscellaneous;
using SpatialAnalysis.Interoperability;

namespace SpatialAnalysis.Events
{
    /// <summary>
    /// Class VisualEventVisualHost.
    /// </summary>
    /// <seealso cref="System.Windows.Controls.Canvas" />
    internal class VisualEventVisualHost: Canvas
    {
        private static Color[] AreaColors =new Color[2]{Colors.Green, Colors.DarkBlue};
        /// <summary>
        /// Gets the color through interpolation.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <param name="max">The maximum.</param>
        /// <param name="alpha">The alpha.</param>
        /// <returns>Color.</returns>
        public static Color GetColor(int count, int max, byte alpha)
        {
            float w1 = ((float)count)/max;
            Color color = Color.Add(Color.Multiply(AreaColors[0], w1), Color.Multiply(AreaColors[1], 1.0f-w1));
            color.A = alpha;
            return color;
        }
        private OSMDocument _host { get; set; }
        private VisualEventSettings _settings { get; set; }
        private MenuItem _showVisibleArea { get; set; }
        private MenuItem _reportVisibilityDetails { get; set; }
        private MenuItem _setEvents { get; set; }
        private MenuItem _eventMenu { get; set; }
        private MenuItem _generateData { get; set; }
        private double _stroke_thickness;
        /// <summary>
        /// Initializes a new instance of the <see cref="VisualEventVisualHost"/> class.
        /// </summary>
        public VisualEventVisualHost()
        {
            this._eventMenu = new MenuItem { Header = "Visual Events" };
            this._setEvents = new MenuItem { Header = "Set Event" };
            this._showVisibleArea = new MenuItem { Header = "Show Visible Area" };
            this._reportVisibilityDetails = new MenuItem { Header = "Report Visibility Details" };


            this._eventMenu.Items.Add(this._setEvents);
            this._eventMenu.Items.Add(this._reportVisibilityDetails);
            this._eventMenu.Items.Add(this._showVisibleArea);
            this._setEvents.Click += _setEvents_Click;
            this._reportVisibilityDetails.Click += _reportVisibilityDetails_Click;
            this._showVisibleArea.Click += _showVisibleArea_Click;


            this._generateData = new MenuItem { Header = "Visibility Method" };
            this._generateData.Click += _generateData_Click;
            this._stroke_thickness = 0.1d;
        }

        private void _reportVisibilityDetails_Click(object sender, RoutedEventArgs e)
        {
            int allVisibleCells = this._host.VisualEventSettings.AllVisibleCells.Count;
            int vantageCells = this._host.VisualEventSettings.VantageCells.Count;
            int field = 0, visualBarriers = 0;
            foreach (Cell item in this._host.cellularFloor.Cells)
            {
                if (item.FieldOverlapState == OverlapState.Inside)
                {
                    field++;
                }
                if (item.VisualOverlapState == OverlapState.Outside)
                {
                    visualBarriers++;
                }
            }
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Number of Visible Cells: \t" + allVisibleCells.ToString());
            double cellArea = this._host.cellularFloor.CellSize * this._host.cellularFloor.CellSize;
            sb.AppendLine("Area of Visible Cells: \t\t" + (allVisibleCells * cellArea).ToString("0.00000"));
            sb.AppendLine("");
            sb.AppendLine("All of the vantage cells are also included in the 'Visible Cells'.");
            MessageBox.Show(sb.ToString(), "Visibility Report".ToUpper());
            sb.Clear();
            sb = null;
        }

        void _generateData_Click(object sender, RoutedEventArgs e)
        {
            this._barriers = new List<SpatialAnalysis.Geometry.BarrierPolygon>();
            this._destinations = new HashSet<Cell>();
            this._settings = new VisualEventSettings(this._host.cellularFloor, true);
            this._settings.Owner = this._host;
            this._settings._addVisibilityArea.Click += _addVisibilityArea_Click;
            this._settings._addVisibilityPoints.Click += _addVisibilityPoints_Click;
            this._settings.Closing += new System.ComponentModel.CancelEventHandler(_settings_Closing);
            this._settings._close.Click += new RoutedEventHandler(_generateDataTermination_Click);
            this._settings._title.Text = "Visibility to Spatial Data";
            this._settings.ShowDialog();
        }

        void _showVisibleArea_Click(object sender, RoutedEventArgs e)
        {
            if (this._host.agentVisiblArea.Source==null)
            {
                MessageBox.Show("Visibility area was not defined for event capturing");
                return;
            }
            string title = ((MenuItem)sender).Header as string;
            switch (title)
            {
                case "Hide Visible Area":
                    this._host.agentVisiblArea.Visibility = System.Windows.Visibility.Collapsed;
                    this._showVisibleArea.Header = "Show Visible Area";
                    break;
                case "Show Visible Area":
                    this._host.agentVisiblArea.Visibility = System.Windows.Visibility.Visible;
                    this._showVisibleArea.Header = "Hide Visible Area";
                    break;
            }
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

        void _setEvents_Click(object sender, RoutedEventArgs e)
        {
            this._barriers = new List<SpatialAnalysis.Geometry.BarrierPolygon>();
            this._destinations = new HashSet<Cell>();
            this._settings = new VisualEventSettings(this._host.cellularFloor, false);
            this._settings.Owner = this._host;
            this._settings._addVisibilityArea.Click += _addVisibilityArea_Click;
            this._settings._addVisibilityPoints.Click += _addVisibilityPoints_Click;
            this._settings.Closing += new System.ComponentModel.CancelEventHandler(_settings_Closing);
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
            if (visibleCells.Count==0)
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
            double depth = this._host.cellularFloor.Origin.DistanceTo(this._host.cellularFloor.TopRight)+1;
            foreach (int item in cellsOnEdge)
            {
                isovists.Add(new Isovist(this._host.cellularFloor.FindCell(item)));
            }
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            Parallel.ForEach(isovists, (a)=>
                {
                    a.Compute(depth, BarrierType.Visual,this._host.cellularFloor,0.0000001);
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
            
            foreach (Cell item in this._destinations)
            {
                UV p1 = item;
                UV p2 = item + this._host.cellularFloor.CellSize * UV.UBase;
                UV p3 = item + this._host.cellularFloor.CellSize * (UV.UBase + UV.VBase);
                UV p4 = item + this._host.cellularFloor.CellSize * UV.VBase;
                var polygon = new SpatialAnalysis.Geometry.BarrierPolygon(new UV[4] { p1, p2, p3, p4 });
                this._barriers.Add(polygon);
            }
            this._host.VisualEventSettings = new VisibilityTarget(visibleArea,
                isovists, this._barriers.ToArray());
            #region visualization
            double _h = ((UIElement)this.Parent).RenderSize.Height;
            double _w = ((UIElement)this.Parent).RenderSize.Width;
            this._host.agentVisiblArea.Source = null;
            WriteableBitmap _view = BitmapFactory.New((int)_w, (int)_h);
            this._host.agentVisiblArea.Source = _view;
            this._host.agentVisiblArea.Visibility = System.Windows.Visibility.Visible;
            this._showVisibleArea.Header = "Hide Visible Area";
            switch (this._settings._colorCode.IsChecked.Value)
            {
                case true:
                    using (_view.GetBitmapContext())
                    {
                        int max = int.MinValue;
                        foreach (var item in this._host.VisualEventSettings.ReferencedVantageCells.Values)
                        {
                            if (max < item.Count) max = item.Count;
                        }
                        byte alpha = (byte)(255 * 0.4);
                        Color yellow = Color.Add(Colors.Yellow, Color.Multiply(Colors.Red, 4.0f));
                        yellow.ScA = 0.7f;
                        foreach (int cellID in visibleArea)
                        {
                            Cell item = this._host.cellularFloor.FindCell(cellID);
                            Point p1 = this._host.Transform(item);
                            Point p2 = this._host.Transform(item + new UV(this._host.cellularFloor.CellSize, this._host.cellularFloor.CellSize));
                            _view.FillRectangle((int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y, GetColor(this._host.VisualEventSettings.ReferencedVantageCells[cellID].Count, max, alpha));
                        }
                        foreach (int cellID in visibleCells)
                        {
                            Cell item = this._host.cellularFloor.FindCell(cellID);
                            Point p1 = this._host.Transform(item);
                            Point p2 = this._host.Transform(item + new UV(this._host.cellularFloor.CellSize, this._host.cellularFloor.CellSize));
                            _view.FillRectangle((int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y, yellow);
                        }
                    }
                    break;
                case false:
                    using (_view.GetBitmapContext())
                    {
                        Color green = Colors.GreenYellow;
                        green.ScA = 0.4f;
                        Color yellow = Color.Add(Colors.Yellow, Color.Multiply(Colors.Red, 4.0f));
                        yellow.ScA = 0.7f;
                        foreach (int cellID in visibleArea)
                        {
                            Cell item = this._host.cellularFloor.FindCell(cellID);
                            Point p1 = this._host.Transform(item);
                            Point p2 = this._host.Transform(item + new UV(this._host.cellularFloor.CellSize, this._host.cellularFloor.CellSize));
                            _view.FillRectangle((int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y, green);
                        }
                        foreach (int cellID in visibleCells)
                        {
                            Cell item = this._host.cellularFloor.FindCell(cellID);
                            Point p1 = this._host.Transform(item);
                            Point p2 = this._host.Transform(item + new UV(this._host.cellularFloor.CellSize, this._host.cellularFloor.CellSize));
                            _view.FillRectangle((int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y, yellow);
                        }
                    }
                    break;
            } 
            #endregion
            //cleanup
            isovists.Clear();
            isovists = null;
            this._settings.Close();
        }
        void _generateDataTermination_Click(object sender, RoutedEventArgs e)
        {
            #region Input validation
            if (string.IsNullOrEmpty(this._settings._dataName.Text))
            {
                MessageBox.Show("Enter Data Field Name");
                return;
            }
            if (this._host.cellularFloor.AllSpatialDataFields.ContainsKey(this._settings._dataName.Text))
            {
                MessageBox.Show("The Data Field Name Exists. Try a Different Name...");
                return;
            }
            double _in = 0, _out = 0, _on = 0;
            if (!double.TryParse(this._settings._valueOfVantageCells.Text, out _on))
            {
                MessageBox.Show("Value of visibility vantage areas is invalid!");
                return;
            }
            if (!double.TryParse(this._settings._valueOutside.Text, out _out))
            {
                MessageBox.Show("Value of not visible areas is invalid!");
                return;
            }
            if (!this._settings._interpolationMethod.IsChecked.Value && !double.TryParse(this._settings._constantValue.Text, out _in))
            {
                MessageBox.Show("Value of visible areas is invalid!");
                return;
            }
            if (this._settings._interpolationMethod.IsChecked.Value)
            {
                //test the interpolation function
                if (!this._settings.LoadFunction())
                {
                    return;
                }
            } 
            #endregion

            #region get the vantage Cells
            HashSet<Index> allIndices = new HashSet<Index>();
            foreach (SpatialAnalysis.Geometry.BarrierPolygon item in this._barriers)
            {
                allIndices.UnionWith(this._host.cellularFloor.GetIndicesInsideBarrier(item, OSMDocument.AbsoluteTolerance));
            }
            var vantageCells = new HashSet<int>();
            foreach (Index item in allIndices)
            {
                if (this._host.cellularFloor.ContainsCell(item) &&
                    this._host.cellularFloor.Cells[item.I, item.J].VisualOverlapState == OverlapState.Outside)
                {
                    vantageCells.Add(this._host.cellularFloor.Cells[item.I, item.J].ID);
                }
            }
            allIndices.Clear();
            allIndices = null;
            foreach (Cell item in this._destinations)
            {
                if (item.VisualOverlapState == OverlapState.Outside)
                {
                    vantageCells.Add(item.ID);
                }
            }
            if (vantageCells.Count == 0)
            {
                MessageBox.Show("Cannot Proceed without Setting Visibility Targets!");
                return;
            } 
            #endregion
            

            this._settings._panel1.Visibility = this._settings._panel2.Visibility = System.Windows.Visibility.Collapsed;
            this._settings._panel4.Visibility = System.Windows.Visibility.Visible;
            var cellsOnEdge = CellUtility.GetEdgeOfField(this._host.cellularFloor, vantageCells);
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
            if (this._settings._interpolationMethod.IsChecked.Value)
            {
                //HashSet<int> notVisibleCellIds = new HashSet<int>();
                //foreach (var item in this._host.cellularFloor.Cells)
                //{
                //    if (item.FieldOverlapState != OverlapState.Outside)
                //    {
                //        if (!vantageCells.Contains(item.ID) && !visibleArea.Contains(item.ID))
                //        {
                //            notVisibleCellIds.Add(item.ID);
                //        }
                //    }
                //}
                //var newData = this._host.FieldGenerator.GetSpatialDataField(vantageCells, _on, notVisibleCellIds, _out, this._settings._dataName.Text, this._settings.InterpolationFunction);
                //this._host.cellularFloor.AddSpatialDataField(newData);
                Dictionary<Cell, double> data = new Dictionary<Cell, double>();
                foreach (var item in this._host.cellularFloor.Cells)
                {
                    if (item.FieldOverlapState != OverlapState.Outside)
                    {
                        if (vantageCells.Contains(item.ID))
                        {
                            data.Add(item, _on);
                        }
                        else
                        {
                            if (visibleArea.Contains(item.ID))
                            {
                                UV visiblePoint = this._host.cellularFloor.FindCell(item.ID);
                                double dist = double.PositiveInfinity;
                                foreach (var id in cellsOnEdge)
                                {
                                    var cell = this._host.cellularFloor.FindCell(id);
                                    dist = Math.Min(UV.GetLengthSquared(cell, visiblePoint), dist);
                                }
                                double value = _on + this._settings.InterpolationFunction(Math.Sqrt(dist));
                                data.Add(item, Math.Max(value, _out));
                            }
                            else
                            {
                                data.Add(item, _out);
                            }
                        }
                    }
                }
                Data.SpatialDataField newData = new Data.SpatialDataField(this._settings._dataName.Text, data);
                this._host.cellularFloor.AddSpatialDataField(newData);
            }
            else
            {
                Dictionary<Cell, double> data = new Dictionary<Cell, double>();
                foreach (var item in this._host.cellularFloor.Cells)
                {
                    if (item.FieldOverlapState != OverlapState.Outside)
                    {
                        if (vantageCells.Contains(item.ID))
                        {
                            data.Add(item, _on);
                        }
                        else
                        {
                            if (visibleArea.Contains(item.ID))
                            {
                                data.Add(item, _in);
                            }
                            else
                            {
                                data.Add(item, _out);
                            }
                        }
                    }
                }
                Data.SpatialDataField newData = new Data.SpatialDataField(this._settings._dataName.Text, data);
                this._host.cellularFloor.AddSpatialDataField(newData);
            }

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

            this.Children.Clear();
        }

        #region Areas that are checked for visibility
        private List<UV> _pnts { get; set; }
        private Line _line { get; set; }
        private Polyline _polyline { get; set; }
        private List<SpatialAnalysis.Geometry.BarrierPolygon> _barriers { get; set; }
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
                Stroke = System.Windows.Media.Brushes.Green,
            };
            this.Children.Add(this._line);
            this._polyline = new Polyline()
            {
                Stroke = System.Windows.Media.Brushes.DarkGreen,
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
                polygon.Stroke = Brushes.Black;
                polygon.StrokeThickness = this._stroke_thickness;
                polygon.StrokeMiterLimit = 0;
                Brush brush = Brushes.LightBlue.Clone();
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
                polygon.Stroke = Brushes.Black;
                polygon.StrokeThickness = this._stroke_thickness;
                polygon.StrokeMiterLimit = 0;
                Brush brush = Brushes.LightBlue.Clone();
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
        /// Sets the main document to which this control belongs.
        /// </summary>
        /// <param name="host">The host.</param>
        public void SetHost(OSMDocument host)
        {
            this._host = host;
            this.RenderTransform = this._host.RenderTransformation;
            this._stroke_thickness = UnitConversion.Convert(0.1d, Length_Unit_Types.FEET, this._host.BIM_To_OSM.UnitType);
            this._host.Menues.Items.Insert(5, this._eventMenu);
            this._host._createNewSpatialDataField.Items.Add(this._generateData);

            var bindConvector = new ValueToBoolConverter();
            Binding bind = new Binding("VisualEventSettings");
            bind.Source = this._host;
            bind.Converter = bindConvector;
            bind.Mode = BindingMode.OneWay;
            this._reportVisibilityDetails.SetBinding(MenuItem.IsEnabledProperty, bind);
        }
    }
}

