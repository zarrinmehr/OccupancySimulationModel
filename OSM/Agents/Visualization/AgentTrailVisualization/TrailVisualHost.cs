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
using SpatialAnalysis.Geometry;
using System.Windows.Shapes;
using System.Windows.Input;
using SpatialAnalysis.CellularEnvironment;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows.Data;
using SpatialAnalysis.Visualization;
using SpatialAnalysis.Data;
using SpatialAnalysis.Miscellaneous;


namespace SpatialAnalysis.Agents.Visualization.AgentTrailVisualization
{
    /// <summary>
    /// Includes the logic of interaction and visualization of the walking trail.
    /// </summary>
    /// <seealso cref="System.Windows.Controls.Canvas" />
    /// <seealso cref="System.ComponentModel.INotifyPropertyChanged" />
    internal class TrailVisualHost: Canvas, INotifyPropertyChanged
    {        
        private const string _showTrailPolygon = "Visualize Trail Polygon";
        private const string _hideTrailPolygon = "Hide Trail Polygon";
        private const string _showStates = "Visualize Interpolated States";
        private const string _hideStates = "Hide Interpolated States";

        #region implimentation of INotifyPropertyChanged
        /// <summary>
        /// Called when the property is changed
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        private void notifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion
        #region TrailFreedom Definition
        private static DependencyProperty TrailFreedomProperty =
            DependencyProperty.Register("TrailFreedom", typeof(double), typeof(TrailVisualHost),
            new FrameworkPropertyMetadata(1.0d, TrailVisualHost.TrailFreedomPropertyChanged, TrailVisualHost.PropertyCoerce));
        /// <summary>
        /// Gets or sets the trail freedom.
        /// </summary>
        /// <value>The trail freedom.</value>
        public double TrailFreedom
        {
            get { return (double)GetValue(TrailFreedomProperty); }
            set { SetValue(TrailFreedomProperty, value); }
        }
        private static void TrailFreedomPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            TrailVisualHost createTrail = (TrailVisualHost)obj;
            if ((double)args.NewValue != (double)args.OldValue)
            {
                createTrail.TrailFreedom = (double)args.NewValue;
                createTrail.updateTrail();
            }
        }
        private static object PropertyCoerce(DependencyObject obj, object value)
        {
            return value;
        }
        #endregion
        private void updateTrail()
        {
            if (this.AgentWalkingTrail != null)
            {
                this.AgentWalkingTrail.Curvature = this._trailCreator.TrailCurvature;
                this.AgentWalkingTrail.NumberOfPointsPerUniteOfLength = this._trailCreator.PointPerUniteOfLength;
                this.AgentWalkingTrail.NumberOfStatesPerUniteOfLength= this._trailCreator.AgentsPerUniteOfLength;
                // drawing the trail
                var points = new Point[this.AgentWalkingTrail.ApproximatedPoints.Length];
                for (int i = 0; i < this.AgentWalkingTrail.ApproximatedPoints.Length; i++)
                {
                    points[i] = new Point(this.AgentWalkingTrail.ApproximatedPoints[i].U,
                        this.AgentWalkingTrail.ApproximatedPoints[i].V);
                }

                if (this._walkingTrailRepresentation == null)
                {
                    var pathGeom = new StreamGeometry();
                    using (var sgo = pathGeom.Open())
                    {
                        sgo.BeginFigure(points[0], false, false);
                        sgo.PolyLineTo(points, true, true);
                    }
                    this._walkingTrailRepresentation = new Path
                    {
                        Data = pathGeom,
                        StrokeThickness = this._thickness,
                        Stroke = Brushes.DarkRed,
                    };
                    this.Children.Add(this._walkingTrailRepresentation);
                    Canvas.SetZIndex(this._walkingTrailRepresentation, 1);
                }
                else
                {
                    using (var sgo = ((StreamGeometry)(this._walkingTrailRepresentation.Data)).Open())
                    {
                        sgo.BeginFigure(points[0], false, false);
                        sgo.PolyLineTo(points, true, true);
                    }
                }
                //drawing the states
                this.updateViualStates();
            }
        }
        private void updateViualStates()
        {
            Point[] statePoints = new Point[this.AgentWalkingTrail.InterpolatedStates.Length * 2];
            for (int i = 0; i < this.AgentWalkingTrail.InterpolatedStates.Length; i++)
            {
                statePoints[i * 2] = new Point(this.AgentWalkingTrail.InterpolatedStates[i].Location.U,
                    this.AgentWalkingTrail.InterpolatedStates[i].Location.V);
                var end = this.AgentWalkingTrail.InterpolatedStates[i].Location +
                    this._stateScleFactor * this.AgentWalkingTrail.InterpolatedStates[i].Direction;
                statePoints[i * 2 + 1] = new Point(end.U, end.V);
            }
            if (this._statesRepresentation == null && statePoints.Length != 0)
            {
                var statesGeom = new StreamGeometry();
                using (var sgo = statesGeom.Open())
                {
                    sgo.BeginFigure(statePoints[0], false, false);
                    for (int i = 0; i < statePoints.Length / 2; i++)
                    {
                        sgo.LineTo(statePoints[2 * i], false, true);
                        sgo.LineTo(statePoints[2 * i + 1], true, true);
                    }
                }
                this._statesRepresentation = new Path
                {
                    Data = statesGeom,
                    StrokeThickness = this._thickness/2,
                    Stroke = Brushes.DarkOrange,
                };
                this.Children.Add(this._statesRepresentation);
                Canvas.SetZIndex(this._statesRepresentation, 2);
            }
            else
            {
                using (var sgo = ((StreamGeometry)(this._statesRepresentation.Data)).Open())
                {
                    sgo.BeginFigure(statePoints[0], false, false);
                    for (int i = 0; i < statePoints.Length / 2; i++)
                    {
                        sgo.LineTo(statePoints[2 * i], false, true);
                        sgo.LineTo(statePoints[2 * i + 1], true, true);
                    }
                }
            }
        }
        private double _stateScleFactor = 1.0d;
        private Path _walkingTrailRepresentation;
        private Path _statesRepresentation;
        private Path _trainingRepresentation;
        private Path _bestTrainingRepresentation;
        /// <summary>
        /// Initiates the training visualization.
        /// </summary>
        public void InitiateTrainingVisualization()
        {
            if (this._trainingRepresentation == null)
            {
                StreamGeometry geom = new StreamGeometry();
                this._trainingRepresentation = new Path
                {
                    Data = geom,
                    StrokeThickness = this._thickness / 3,
                    Stroke = Brushes.Blue,
                    StrokeDashArray = new DoubleCollection { this._thickness / 3, this._thickness / 3 },
                };
            }
            else
            {
                ((StreamGeometry)this._trainingRepresentation.Data).Clear();
            }
            if (!this.Children.Contains(this._trainingRepresentation))
            {
                this.Children.Add(this._trainingRepresentation);
                Canvas.SetZIndex(this._trainingRepresentation, 6);
            }
            if (this._bestTrainingRepresentation == null)
            {
                StreamGeometry geom = new StreamGeometry();
                this._bestTrainingRepresentation = new Path
                {
                    Data = geom,
                    StrokeThickness = (this._thickness * 2.0) / 3,
                    Stroke = Brushes.DarkBlue,
                };
            }
            else
            {
                ((StreamGeometry)this._bestTrainingRepresentation.Data).Clear();
            }
            if (!this.Children.Contains(this._bestTrainingRepresentation))
            {
                this.Children.Add(this._bestTrainingRepresentation);
                Canvas.SetZIndex(this._bestTrainingRepresentation, 5);
            }
        }
        /// <summary>
        /// Terminates the training visualization.
        /// </summary>
        public void TerminateTrainingVisualization()
        {
            this.Children.Remove(this._trainingRepresentation);
            this._trainingRepresentation.Data = null;
            this._trainingRepresentation = null;
            this.Children.Remove(this._bestTrainingRepresentation);
            this._bestTrainingRepresentation.Data = null;
            this._bestTrainingRepresentation = null;
        }
        /// <summary>
        /// Visualizes the training step.
        /// </summary>
        /// <param name="agentLocations">The agent locations.</param>
        public void VisualizeTrainingStep(StateBase[] agentLocations)
        {
            if (agentLocations != null)
            {
                using (var sgo = ((StreamGeometry)(this._trainingRepresentation.Data)).Open())
                {
                    sgo.BeginFigure(this.UVtoPoint(this.AgentWalkingTrail.InterpolatedStates[0].Location), false, false);
                    for (int i = 0; i < agentLocations.Length; i++)
                    {
                        sgo.LineTo(this.UVtoPoint(this.AgentWalkingTrail.InterpolatedStates[i].Location), false, true);
                        sgo.LineTo(this.UVtoPoint(agentLocations[i].Location), true, true);
                    }
                }
            }
        }
        /// <summary>
        /// Visualizes the best training option.
        /// </summary>
        /// <param name="agentLocations">The agent locations.</param>
        public void VisualizeBestTrainingOption(StateBase[] agentLocations)
        {
            if (agentLocations != null)
            {
                using (var sgo = ((StreamGeometry)(this._bestTrainingRepresentation.Data)).Open())
                {
                    sgo.BeginFigure(this.UVtoPoint(this.AgentWalkingTrail.InterpolatedStates[0].Location), false, false);
                    for (int i = 0; i < agentLocations.Length; i++)
                    {
                        sgo.LineTo(this.UVtoPoint(this.AgentWalkingTrail.InterpolatedStates[i].Location), false, true);
                        sgo.LineTo(this.UVtoPoint(agentLocations[i].Location), true, true);
                    }
                }
            }
        }
        private SetTrail _trailCreator;
        private OSMDocument _host { get; set; }
        private List<UV> _pnts { get; set; }
        private MenuItem visualization_Menu { get; set; }
        private MenuItem _drawTrail_Menu { get; set; }
        private Line _line { get; set; }
        private Polyline _polyline { get; set; }
        #region definition of WalkingTrail
        private WalkingTrail _agentWalkingTrail;
        /// <summary>
        /// Gets or sets the agent walking trail.
        /// </summary>
        /// <value>The agent walking trail.</value>
        public WalkingTrail AgentWalkingTrail
        {
            get { return this._agentWalkingTrail; }
            set
            {
                this._agentWalkingTrail = value;
                this.notifyPropertyChanged("AgentWalkingTrail");
            }
        } 
        #endregion
        MenuItem _clearTrailMenu { get; set; }
        MenuItem _hideTrailMenu { get; set; }
        MenuItem _stateVectorSizeMenu { get; set; }
        MenuItem _setTrailThickness { get; set; }
        MenuItem _showTrailPolyLine { get; set; }
        MenuItem _showInterpolatedStates { get; set; }
        private double _thickness = 0.10d;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrailVisualHost"/> class.
        /// </summary>
        public TrailVisualHost()
        {
            this.visualization_Menu = new MenuItem { Header = "Walking Trail" };
            this._drawTrail_Menu = new MenuItem { Header = "Set Walking Trail" };
            this._drawTrail_Menu.Click += visualization_Menu_Click;
            this.visualization_Menu.Items.Add(this._drawTrail_Menu);
            this._stateVectorSizeMenu = new MenuItem { Header = "Set State Vector Size" };
            this._stateVectorSizeMenu.Click += _stateVectorSizeMenu_Click;
            this.visualization_Menu.Items.Add(this._stateVectorSizeMenu);
            this._setTrailThickness = new MenuItem { Header = "Set Thickness" };
            this.visualization_Menu.Items.Add(this._setTrailThickness);
            this._setTrailThickness.Click += new RoutedEventHandler(_setTrailThickness_Click);
            this._hideTrailMenu = new MenuItem { Header = "Hide" };
            this._hideTrailMenu.Click += new RoutedEventHandler(_hideTrailMenu_Click);
            this.visualization_Menu.Items.Add(this._hideTrailMenu);
            this._clearTrailMenu = new MenuItem  { Header = "Clear" };
            this._clearTrailMenu.Click += new RoutedEventHandler(_clearTrail_Click);
            this.visualization_Menu.Items.Add(this._clearTrailMenu);
            this._showTrailPolyLine = new MenuItem() { Header = _hideTrailPolygon };
            this.visualization_Menu.Items.Add(this._showTrailPolyLine);
            this._showTrailPolyLine.Click += _showTrailPolyLine_Click;
            this._showInterpolatedStates = new MenuItem() { Header = _hideStates };
            this.visualization_Menu.Items.Add(this._showInterpolatedStates);
            this._showInterpolatedStates.Click += _showInterpolatedStates_Click;

            this.Loaded += new RoutedEventHandler(TrailVisualHost_Loaded);
        }

        void _showInterpolatedStates_Click(object sender, RoutedEventArgs e)
        {
            if (this._statesRepresentation != null)
            {
                if (this._statesRepresentation.Visibility == System.Windows.Visibility.Collapsed)
                {
                    this._statesRepresentation.Visibility = System.Windows.Visibility.Visible;
                    this._showInterpolatedStates.Header = TrailVisualHost._hideStates;
                }
                else if (this._statesRepresentation.Visibility == System.Windows.Visibility.Visible)
                {
                    this._statesRepresentation.Visibility = System.Windows.Visibility.Collapsed;
                    this._showInterpolatedStates.Header = TrailVisualHost._showStates;
                }
            }
        }

        void _showTrailPolyLine_Click(object sender, RoutedEventArgs e)
        {
            if (this._polyline != null)
            {
                if (this._polyline.Visibility == System.Windows.Visibility.Collapsed)
                {
                    this._polyline.Visibility = System.Windows.Visibility.Visible;
                    this._showTrailPolyLine.Header = TrailVisualHost._hideTrailPolygon;
                }
                else if (this._polyline.Visibility == System.Windows.Visibility.Visible)
                {
                    this._polyline.Visibility = System.Windows.Visibility.Collapsed;
                    this._showTrailPolyLine.Header = TrailVisualHost._showTrailPolygon;
                }
            }
        }

        void _setTrailThickness_Click(object sender, RoutedEventArgs e)
        {
            GetNumber gn = new GetNumber("Set Trail Thickness",
                "The visual effect does not influence the training process", this._thickness);
            gn.Owner = this._host;
            gn.ShowDialog();
            if (gn.NumberValue != this._stateScleFactor)
            {
                this._thickness = gn.NumberValue;
                if (this._walkingTrailRepresentation != null)
                {
                    this._walkingTrailRepresentation.StrokeThickness = this._thickness;
                }
                if (this._statesRepresentation != null)
                {
                    this._statesRepresentation.StrokeThickness = this._thickness / 2;
                }
                if (this._polyline != null)
                {
                    this._polyline.StrokeThickness = this._thickness;
                }
                foreach (var item in this.Children)
                {
                    Type type = item.GetType();
                    if (type == typeof(Line))
                    {
                        ((Line)item).StrokeThickness = this._thickness/5;
                    }
                }
            }
        }

        void _stateVectorSizeMenu_Click(object sender, RoutedEventArgs e)
        {
            GetNumber gn = new GetNumber("Set State Vector Size", 
                "The visual size of the vectors does not influence the training process", this._stateScleFactor);
            gn.Owner = this._host;
            gn.ShowDialog();
            if (gn.NumberValue != this._stateScleFactor)
            {
                this._stateScleFactor = gn.NumberValue;
                this.updateViualStates();
            }
        }

        void _clearTrail_Click(object sender, RoutedEventArgs e)
        {
            this.clear();
        }

        private void clear()
        {
            this.Children.Clear();
            this.AgentWalkingTrail = null;
            this._walkingTrailRepresentation = null;
            //this._trailCreator = null;
            //this._pnts = null;
            this._line = null;
            this._polyline = null;
            this._statesRepresentation = null;
            if (this.Visibility != System.Windows.Visibility.Visible)
            {
                this._hideShowSwitch();
            }
            this._showTrailPolyLine.Header = _hideTrailPolygon;
            this._showInterpolatedStates.Header = _hideStates;
        }

        void TrailVisualHost_Loaded(object sender, RoutedEventArgs e)
        {
            Binding bind = new Binding("AgentWalkingTrail");
            bind.Source = this;
            bind.Mode = BindingMode.OneWay;
            bind.Converter = new ValueToBoolConverter();
            this._hideTrailMenu.SetBinding(MenuItem.IsEnabledProperty, bind);
            this._clearTrailMenu.SetBinding(MenuItem.IsEnabledProperty, bind);
            this._stateVectorSizeMenu.SetBinding(MenuItem.IsEnabledProperty, bind);
            this._setTrailThickness.SetBinding(MenuItem.IsEnabledProperty, bind);
            this._showTrailPolyLine.SetBinding(MenuItem.IsEnabledProperty, bind);
            this._showInterpolatedStates.SetBinding(MenuItem.IsEnabledProperty, bind);
        }


        #region Show and Hide the control
        private void _hideShowSwitch()
        {
            if (this.Visibility == System.Windows.Visibility.Visible)
            {
                this.Visibility = System.Windows.Visibility.Hidden;
                this._hideTrailMenu.Header = "Show";
            }
            else
            {
                this.Visibility = System.Windows.Visibility.Visible;
                this._hideTrailMenu.Header = "Hide";
            }
        }
        void _hideTrailMenu_Click(object sender, RoutedEventArgs e)
        {
            this._hideShowSwitch();
        }
        void _hide()
        {
            this.Visibility = System.Windows.Visibility.Hidden;
            this._hideTrailMenu.Header = "Show";
        }
        void _show()
        {
            this.Visibility = System.Windows.Visibility.Visible;
            this._hideTrailMenu.Header = "Hide";
        } 
        #endregion


        void visualization_Menu_Click(object sender, RoutedEventArgs e)
        {
            this._trailCreator = new SetTrail(this._host);
            this._trailCreator.Owner = this._host;
            this._trailCreator._drawBtm.Click += _drawBtm_Click;
            this._trailCreator.TrailParametersUpdated += _trailCreator_Updated;
            this._trailCreator._importBtm.Click += _importBtm_Click;
            this._trailCreator._exportBtm.Click += _exportBtm_Click;
            this._trailCreator.Closing += _trailCreator_Closing;
            
            this._trailCreator.ShowDialog();
        }

        void _trailCreator_Closing(object sender, CancelEventArgs e)
        {
            this._trailCreator._drawBtm.Click -= _drawBtm_Click;
            this._trailCreator.TrailParametersUpdated -= _trailCreator_Updated;
            this._trailCreator._importBtm.Click -= _importBtm_Click;
            this._trailCreator._exportBtm.Click -= _exportBtm_Click;
            this._trailCreator.Closing -= _trailCreator_Closing;
        }

        void _exportBtm_Click(object sender, RoutedEventArgs e)
        {
            if (this.AgentWalkingTrail == null)
            {
                MessageBox.Show("Walking trail data does not exist");
                return;
            }
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.DefaultExt = ".txt";
            dlg.Filter = "Text documents (.txt)|*.txt";
            Nullable<bool> result = dlg.ShowDialog(this._host);
            string fileAddress = "";
            if (result == true)
            {
                fileAddress = dlg.FileName;
            }
            else
            {
                return;
            }
            using (var sw = new System.IO.StreamWriter(fileAddress))
            {
                sw.WriteLine(this.AgentWalkingTrail.GetStringRepresentation());
            }
        }

        void _importBtm_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();

            //dlg.Title = "Import data and load it into the grid";
            dlg.DefaultExt = ".txt";
            dlg.Filter = "Text file (.txt)|*.txt";
            Nullable<bool> result = dlg.ShowDialog(this._host);
            string fileAddress = "";
            if (result == true)
            {
                fileAddress = dlg.FileName;
            }
            else
            {
                return;
            }

            try
            {
                this.clear();
                string content = string.Empty;
                using (System.IO.StreamReader sr = new System.IO.StreamReader(fileAddress))
                    content = sr.ReadToEnd();
                this.AgentWalkingTrail = WalkingTrail.FromStringRepresentation(content);
                PointCollection pnts = new PointCollection();
                for (int i = 0; i < this.AgentWalkingTrail.ObservedStates.Length; i++)
                {
                    Point p = new Point(this.AgentWalkingTrail.ObservedStates[i].Location.U, this.AgentWalkingTrail.ObservedStates[i].Location.V);
                    pnts.Add(p);
                }
                this.Children.Clear();
                this._polyline = new Polyline()
                {
                    Stroke = Brushes.DarkGreen,
                    StrokeThickness = this._thickness,
                };

                this._polyline.Points = pnts;
                this.Children.Add(this._polyline);
                Canvas.SetZIndex(this._polyline, 0);
                this.updateTrail();
            }
            catch (Exception splineError)
            {
                MessageBox.Show(splineError.Report());
            }
        }

        void _trailCreator_Updated(object sender, RoutedEventArgs e)
        {
            this.updateTrail();
        }

        void _drawBtm_Click(object sender, RoutedEventArgs e)
        {
            this._trailCreator.Hide();
            this.Children.Clear();
            this._host.FloorScene.MouseLeftButtonDown += getStartpoint;
            this._host.Menues.IsEnabled = false;
            this._host.UIMessage.Text = "Click to draw a trail";
            this._host.UIMessage.Visibility = System.Windows.Visibility.Visible;
            this._host.Cursor = Cursors.Pen;
        }

        void getStartpoint(object sender, MouseButtonEventArgs e)
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
                return;
            }
            this._pnts = new List<UV>() { p };
            this._line = new Line()
            {
                X1 = p.U,
                Y1 = p.V,
                StrokeThickness = this._thickness,
                Stroke = Brushes.Green,
            };
            this.Children.Add(this._line);
            this._polyline = new Polyline()
            {
                Stroke = Brushes.DarkGreen,
                StrokeThickness = this._line.StrokeThickness
            };
            this._polyline.Points = new PointCollection() { point };
            this.Children.Add(this._polyline);
            Canvas.SetZIndex(this._polyline, 0);
            this.registerTrailGenerationEvents();
        }
        private void registerTrailGenerationEvents()
        {
            this._host.MouseBtn.MouseDown += mainWindowMouseBtn_MouseDown;
            this._host.KeyDown += regionTermination_KeyDown;
            this._host.FloorScene.MouseMove += FloorScene_MouseMove;
            this._host.FloorScene.MouseLeftButtonDown += getNextTrailPoint;
        }
        private void mainWindowMouseBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.terminateTrailGeneration_Enter();
        }
        private void regionTermination_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.terminateTrailGeneration_Enter();
            }
            if (e.Key == Key.Escape)
            {
                this.terminateTrailGeneration_Cancel();
            }
        }
        private void FloorScene_MouseMove(object sender, MouseEventArgs e)
        {
            var point = this._host.InverseRenderTransform.Transform(Mouse.GetPosition(this._host.FloorScene));
            this._line.X2 = point.X;
            this._line.Y2 = point.Y;
        }
        private void getNextTrailPoint(object sender, MouseButtonEventArgs e)
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
        private void terminateTrailGeneration_Enter()
        {
            this.unregisterTrailGenerationEvents();
            //update canvas
            double velocity = Parameter.DefaultParameters[AgentParameters.GEN_VelocityMagnitude].Value;
            this.AgentWalkingTrail = new Geometry.WalkingTrail(this._pnts.ToArray(), velocity);
            if (this._walkingTrailRepresentation != null)
            {
                this.Children.Remove(this._walkingTrailRepresentation);
                this._walkingTrailRepresentation = null;
            }
            if (this._statesRepresentation != null)
            {
                this.Children.Remove(this._statesRepresentation);
                this._statesRepresentation = null;
            }
            this.updateTrail();
            this._pnts.Clear();
            this._pnts = null;
            this.Children.Remove(this._line);
            this._line = null;
            this._host.Menues.IsEnabled = true;
            this._host.UIMessage.Visibility = System.Windows.Visibility.Collapsed;
            this._host.Cursor = Cursors.Arrow;
            this._trailCreator.ShowDialog();
        }
        private void terminateTrailGeneration_Cancel()
        {
            this.unregisterTrailGenerationEvents();
            this.Children.Remove(this._polyline);
            this.Children.Remove(this._line);
            this._polyline.Points.Clear();
            this._polyline = null;
            this._line = null;
            this._host.Menues.IsEnabled = true;
            this._host.UIMessage.Visibility = System.Windows.Visibility.Collapsed;
            this._host.Cursor = Cursors.Arrow;
            this._trailCreator.ShowDialog();
        }
        private void unregisterTrailGenerationEvents()
        {
            this._host.MouseBtn.MouseDown -= mainWindowMouseBtn_MouseDown;
            this._host.KeyDown -= regionTermination_KeyDown;
            this._host.FloorScene.MouseMove -= FloorScene_MouseMove;
            this._host.FloorScene.MouseLeftButtonDown -= getNextTrailPoint;
        }

        private Point UVtoPoint(UV uv)
        {
            return new Point(uv.U, uv.V);
        }

        /// <summary>
        /// Sets the host.
        /// </summary>
        /// <param name="host">The main document to which this class instance belongs.</param>
        public void SetHost(OSMDocument host)
        {
            this._host = host;
            this.RenderTransform = this._host.RenderTransformation;
            this._host.Menues.Items.Add(this.visualization_Menu);
            this._thickness = this._host.UnitConvertor.Convert(this._thickness);

        }
    }
}

