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
using System.Windows.Media;
using System.Windows.Input;
using SpatialAnalysis.Geometry;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading;
using System.Windows.Media.Media3D;
using SpatialAnalysis.Data;
using SpatialAnalysis.Events;
using SpatialAnalysis.Visualization;


namespace SpatialAnalysis.Agents.MandatoryScenario.Visualization
{
    /// <summary>
    /// This class controls the animation control panel, visualizes the 2D animation, and implements the interaction logic among user, the 2d scene and the control panel. For protection its members are private.
    /// </summary>
    /// <seealso cref="System.Windows.Controls.Canvas" />
    public class MandatoryScenarioAnimationVisualHost : Canvas
    {
        private Line _lineToVisualTrigger { get; set; }
        private Line _lineToTarget { get; set; }
        private Dictionary<string, Polygon> _destinationPolygons { get; set; }
        private string currentActivityName { get; set; }
        private bool _animationInProgress;
        private VisualAgentMandatoryScenario agent { get; set; }
        private OSMDocument _host;
        //private MenuItem _calculateScapeRoutesMenu { get; set; }
        private MenuItem _getWalkingTrailDataMenu { get; set; }
        private MenuItem _captureEventMenu { get; set; }
        private MenuItem _trainingMenu { get; set; }
        /// <summary>
        /// Gets or sets the control panel menu.
        /// </summary>
        /// <value>The control panel menu.</value>
        private MenuItem _controlPanelMenu { get; set; }
        private Line _closestEdge { get; set; }
        private Line _repulsionTrajectoryLine { get; set; }

        private RealTimeMandatoryNavigationControler _settings { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MandatoryScenarioAnimationVisualHost"/> class.
        /// </summary>
        public MandatoryScenarioAnimationVisualHost()
        {
            this._controlPanelMenu = new MenuItem
            {
                Header = "Mandatory Navigation Control Panel",
            };
            this._controlPanelMenu.Click += animation_Menu_Click;
            this._getWalkingTrailDataMenu = new MenuItem()
            {
                Header = "Simulate",
            };
            this._getWalkingTrailDataMenu.Click += new RoutedEventHandler(_getWalkingTrailDataMenu_Click);
            this._captureEventMenu = new MenuItem { Header = "Capture Events" };
            this._captureEventMenu.Click += new RoutedEventHandler(_captureEventMenu_Click);
            this._trainingMenu = new MenuItem { Header = "Train Agent" };
            this._trainingMenu.Click += new RoutedEventHandler(_trainingMenu_Click);
        }


        void _captureEventMenu_Click(object sender, RoutedEventArgs e)
        {
            var eventCapturingInterface = new EventCapturingInterface(this._host, EvaluationEventType.Mandatory);
            eventCapturingInterface.Owner = this._host;
            eventCapturingInterface._subTitle.Text = "Mandatory Scenario";
            eventCapturingInterface.ShowDialog();
            eventCapturingInterface = null;
        }
        WalkingTrailData _walkingTrailDataComputer { get; set; }
        void _getWalkingTrailDataMenu_Click(object sender, RoutedEventArgs e)
        {
            if (!this._host.AgentMandatoryScenario.IsReadyForPerformance())
            {
                MessageBox.Show(this._host.AgentMandatoryScenario.Message,"Scenario Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            this._walkingTrailDataComputer = new WalkingTrailData();
            foreach (SimulationResult item in this._host.AllSimulationResults.Values)
            {
                    this._walkingTrailDataComputer.dataNames.Items.Add(item.Name);
            }
            foreach (ISpatialData item in this._host.AllOccupancyEvent.Values)
            {
                this._walkingTrailDataComputer.dataNames.Items.Add(item.Name);
            }
            foreach (ISpatialData item in this._host.AllActivities.Values)
            {
                this._walkingTrailDataComputer.dataNames.Items.Add(item.Name);
            }
            foreach (ISpatialData item in this._host.cellularFloor.AllSpatialDataFields.Values)
            {
                this._walkingTrailDataComputer.dataNames.Items.Add(item.Name);
            }
            this._walkingTrailDataComputer._getWalkingTrailBtn.Click += _getWalkingTrailBtn_Click;
            this._walkingTrailDataComputer.Owner = this._host;
            this._walkingTrailDataComputer.ShowDialog();
        }

        void _getWalkingTrailBtn_Click(object sender, RoutedEventArgs e)
        {
            this._walkingTrailDataComputer._getWalkingTrailBtn.Click -= _getWalkingTrailBtn_Click;
            string name = string.Empty;
            if (string.IsNullOrEmpty(this._walkingTrailDataComputer._spatialDataName.Text))
            {
                MessageBox.Show("Enter a name for the 'Spatial Data Field'!");
                return;
            }
            else
            {
                if (this._host.ContainsSpatialData(name))
                {
                    MessageBox.Show("'A Data Field' with the same name already exists! \nTry a new name.");
                    return;
                }
                name = this._walkingTrailDataComputer._spatialDataName.Text;
            }
            double h, duration;
            if (!double.TryParse(this._walkingTrailDataComputer._timeStep.Text, out h) ||
                !double.TryParse(this._walkingTrailDataComputer._timeDuration.Text, out duration))
            {
                MessageBox.Show("Invalid input for 'time step' and/or 'duration'");
                return;
            }
            if (h <= 0 || duration <= 0)
            {
                MessageBox.Show("'Time step' and/or 'duration' must be larger than zero!");
                return;
            }
            this._walkingTrailDataComputer.Close();
            this._host.AgentMandatoryScenario.LoadQueues(this._host.AllActivities, duration, 0.0d);
            bool notification = this._walkingTrailDataComputer._notify.IsChecked.Value;
            var simulator = new MandatoryScenarioSimulation(this._host, h, duration);
            Thread simulationThread = new Thread(
                () => simulator.Simulate(name, notification)
                );
            simulationThread.Start();
        }


        void animation_Menu_Click(object sender, RoutedEventArgs e)
        {
            if (!this._host.AgentMandatoryScenario.IsReadyForPerformance())
            {
                MessageBox.Show(this._host.AgentMandatoryScenario.Message, "Scenario Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            this._settings = new RealTimeMandatoryNavigationControler();
            //set events
            this._settings._addAgent.Click += _addAgent_Click;
            this._settings.Closing += _settings_Closing;
            //show the dialog
            this._settings.Owner = this._host;
            this._settings.ShowDialog();
        }

        void _settings_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.stopAnimation();
            if (this.agent != null)
            {
                this.cleanUpAgent();
            }
            //set events
            this._settings._addAgent.Click -= _addAgent_Click;
            this.Children.Clear();
        }

        void _addAgent_Click(object sender, RoutedEventArgs e)
        {
            this._settings._addAgent.Click -= _addAgent_Click;
            this._settings._addAgent.IsEnabled = false;
            this._settings._clearAgent.Click += _clearAgent_Click;
            this._settings._clearAgent.IsEnabled = true;
            #region Agent Creation
            this.agent = new VisualAgentMandatoryScenario
            {
                StrokeThickness = this._settings._strokeThickness.Value,
                Stroke = VisualAgentMandatoryScenario.OutsideRange,
                Fill = VisualAgentMandatoryScenario.OutsideRange,
                VelocityMagnitude = this._settings._velocity.Value,
                ShowVisibilityCone = this._settings._showVisibiityAngle.IsChecked.Value,
                ShowSafetyBuffer = this._settings._showSafetyBuffer.IsChecked.Value,
                BodySize = this._settings._bodySize.Value,
                VisibilityAngle = this._settings._viewAngle.Value * Math.PI / 180.0d,
                AngularVelocity = this._settings._angularVelocity.Value,
                BarrierRepulsionRange = this._settings._barrierRepulsionRange.Value,
                RepulsionChangeRate = this._settings._repulsionChangeRate.Value,
                AccelerationMagnitude = this._settings._accelerationMagnitude.Value,
                CurrentActivity=this._host.AllActivities[this._host.AgentMandatoryScenario.MainStations.First()],
                CurrentState = this._host.AllActivities[this._host.AgentMandatoryScenario.MainStations.First()].DefaultState,
                OccupancyScenario = this._host.AgentMandatoryScenario,
                AllActivities = this._host.AllActivities,
            };

            this.agent.SetInitialState(this.agent.CurrentState);
            Canvas.SetZIndex(this.agent, 100);

            #endregion

            this.Children.Add(this.agent);

            #region Agent Events' Registration
            this._settings._strokeThickness.ValueChanged += _strokeThickness_ValueChanged;

            this._settings._viewAngle.ValueChanged += _viewAngle_ValueChanged;

            //this._host.FloorScene.MouseMove += scene_AgentPositionSet;
            //this._host.FloorScene.MouseLeftButtonDown += scene_AgentPositionSetTerminated;

            this._settings._visualTrigger.Checked += _visualTrigger_Checked;
            this._settings._visualTrigger.Unchecked += _visualTrigger_Unchecked;

            this._settings._velocity.ValueChanged += _velocity_ValueChanged;
            this._settings._accelerationMagnitude.ValueChanged += _accelerationMagnitude_ValueChanged;

            this._settings._bodySize.ValueChanged += _scale_ValueChanged;

            this._settings._angularVelocity.ValueChanged += _angularVelocity_ValueChanged;

            this._settings._showVisibiityAngle.Checked += _showVisibilityAngle_Checked;
            this._settings._showVisibiityAngle.Unchecked += _showVisibilityAngle_Unchecked;

            this._settings._showSafetyBuffer.Checked += _showSafetyBuffer_Checked;
            this._settings._showSafetyBuffer.Unchecked += _showSafetyBuffer_Unchecked;

            this._settings._showDestination.Checked += _showVisibleDestinations_Checked;
            this._settings._showDestination.Unchecked += _showVisibleDestinations_Unchecked;

            this._settings._captureVisualEvents.Checked += _captureVisualEvents_Checked;
            this._settings._captureVisualEvents.Unchecked += _captureVisualEvents_Unchecked;
            this._settings._showClosestBarrier.Checked += _showClosestBarrier_Checked;
            this._settings._showClosestBarrier.Unchecked += _showClosestBarrier_Unchecked;

            this._settings._barrierRepulsionRange.ValueChanged += _barrierRepulsionRange_ValueChanged;
            this._settings._repulsionChangeRate.ValueChanged += _repulsionChangeRate_ValueChanged;

            this._settings._showRepulsionTrajectory.Checked += this._showRepulsionTrajectory_Checked;
            this._settings._showRepulsionTrajectory.Unchecked += this._showRepulsionTrajectory_Unchecked;

            this._settings._barrierFriction.ValueChanged += _barrierFriction_ValueChanged;
            this._settings._bodyElasticity.ValueChanged += _bodyElasticity_ValueChanged;

            this._settings._init.Click += _initStart_Click;
            #endregion

            //this._settings.Hide();
        }

        void _bodyElasticity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.agent.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (SendOrPostCallback)delegate
                {
                    this.agent.SetValue(VisualAgentMandatoryScenario.BodyElasticityProperty, e.NewValue);
                }
                , null);
        }

        void _barrierFriction_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.agent.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (SendOrPostCallback)delegate
                {
                    this.agent.SetValue(VisualAgentMandatoryScenario.BarrierFrictionProperty, e.NewValue);
                }
                , null);
        }



        void _showRepulsionTrajectory_Unchecked(object sender, RoutedEventArgs e)
        {
            this.agent.ShowRepulsionTrajectory -= this.showRepulsionTrajectory;
            if (this._repulsionTrajectoryLine != null)
            {
                int index = this.Children.IndexOf(this._repulsionTrajectoryLine);
                if (index != -1)
                {
                    this.Children.RemoveAt(index);
                }
                this._repulsionTrajectoryLine = null;
            }
        }
        private void showRepulsionTrajectory()
        {
            if (this._repulsionTrajectoryLine != null && this.agent.EdgeCollisionState != null)
            {
                this._repulsionTrajectoryLine.Visibility = System.Windows.Visibility.Visible;
                this._repulsionTrajectoryLine.X1 = this.agent.CurrentState.Location.U;
                this._repulsionTrajectoryLine.Y1 = this.agent.CurrentState.Location.V;
                this._repulsionTrajectoryLine.X2 = this.agent.EdgeCollisionState.ClosestPointOnBarrier.U;
                this._repulsionTrajectoryLine.Y2 = this.agent.EdgeCollisionState.ClosestPointOnBarrier.V;
            }
            else
            {
                this._repulsionTrajectoryLine.Visibility = System.Windows.Visibility.Hidden;
            }
        }
        void _showRepulsionTrajectory_Checked(object sender, RoutedEventArgs e)
        {
            if (this._repulsionTrajectoryLine == null)
            {
                this._repulsionTrajectoryLine = new Line()
                {
                    Stroke = Brushes.DarkTurquoise,
                    StrokeThickness = this._settings._strokeThickness.Value * 1.5d,
                };
                this.Children.Add(this._repulsionTrajectoryLine);
            }
            this.agent.ShowRepulsionTrajectory += this.showRepulsionTrajectory;
        }

        void _showClosestBarrier_Unchecked(object sender, RoutedEventArgs e)
        {
            this.agent.ShowBarrier -= this.showClosestBarrier;
            if (this._closestEdge != null)
            {
                int index = this.Children.IndexOf(this._closestEdge);
                if (index != -1)
                {
                    this.Children.RemoveAt(index);
                }
                this._closestEdge = null;
            }
        }

        void _showClosestBarrier_Checked(object sender, RoutedEventArgs e)
        {
            if (this._closestEdge == null)
            {
                this._closestEdge = new Line()
                {
                    Stroke = Brushes.DarkSalmon,
                    StrokeThickness = this._settings._strokeThickness.Value * 3,
                };
                this.Children.Add(this._closestEdge);
            }
            this.agent.ShowBarrier += this.showClosestBarrier;
        }
        private void showClosestBarrier()
        {
            if (this.agent.EdgeCollisionState.Barrrier != null)
            {
                this._closestEdge.Visibility = System.Windows.Visibility.Visible;
                this._closestEdge.X1 = this.agent.EdgeCollisionState.Barrrier.Start.U;
                this._closestEdge.X2 = this.agent.EdgeCollisionState.Barrrier.End.U;
                this._closestEdge.Y1 = this.agent.EdgeCollisionState.Barrrier.Start.V;
                this._closestEdge.Y2 = this.agent.EdgeCollisionState.Barrrier.End.V;
            }
            else
            {
                this._closestEdge.Visibility = System.Windows.Visibility.Hidden;
            }
        }
        void _accelerationMagnitude_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.agent.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (SendOrPostCallback)delegate
                {
                    this.agent.SetValue(VisualAgentMandatoryScenario.AccelerationMagnitudeProperty, e.NewValue);
                }
                , null);
        }

        void _repulsionChangeRate_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.agent.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (SendOrPostCallback)delegate
                {
                    this.agent.SetValue(VisualAgentMandatoryScenario.RepulsionChangeRateProperty, e.NewValue);
                }
                , null);
        }

        void _barrierRepulsionRange_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.agent.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (SendOrPostCallback)delegate
                {
                    this.agent.SetValue(VisualAgentMandatoryScenario.BarrierRepulsionRangeProperty, e.NewValue);
                }
                , null);
        }

        void _captureVisualEvents_Unchecked(object sender, RoutedEventArgs e)
        {
            this.agent.RaiseVisualEvent -= this.raiseVisualEvent;
            if (this._lineToTarget != null)
            {
                try
                {
                    this.Children.Remove(this._lineToTarget);
                    this._lineToTarget = null;
                }
                catch (Exception)
                {
                }
            }
        }

        void _captureVisualEvents_Checked(object sender, RoutedEventArgs e)
        {
            this.agent.RaiseVisualEvent += this.raiseVisualEvent;
        }
        private void raiseVisualEvent()
        {
            if (!VisibilityTarget.ReferenceEquals(this._host.VisualEventSettings, null))
            {
                UV visualTarget = this._host.VisualEventSettings.TargetVisibilityTest(this.agent.CurrentState,
                    this.agent.VisibilityCosineFactor, this._host.cellularFloor);

                if (visualTarget != null)
                {
                    if (this._lineToTarget == null)
                    {
                        this._lineToTarget = new Line();
                        this.Children.Add(this._lineToTarget);
                    }
                    this._lineToTarget.Visibility = System.Windows.Visibility.Visible;
                    this._lineToTarget.X1 = this.agent.CurrentState.Location.U;
                    this._lineToTarget.Y1 = this.agent.CurrentState.Location.V;
                    this._lineToTarget.X2 = visualTarget.U;
                    this._lineToTarget.Y2 = visualTarget.V;
                    this._lineToTarget.Stroke = Brushes.DarkRed;
                    this._lineToTarget.StrokeThickness = this.agent.StrokeThickness;
                }
                else
                {
                    if (this._lineToTarget != null)
                    {
                        this._lineToTarget.Visibility = System.Windows.Visibility.Collapsed;
                    }
                }
            }
        }

        void _showVisibleDestinations_Unchecked(object sender, RoutedEventArgs e)
        {

            this.agent.RaiseVisualEvent -= showDestinationArea;
        }

        void _showVisibleDestinations_Checked(object sender, RoutedEventArgs e)
        {
            this.agent.RaiseVisualEvent += showDestinationArea;
        }

        /// <summary>
        /// Shows the destination area.
        /// </summary>
        private void showDestinationArea()
        {
            if (this._destinationPolygons == null)
            {
                this._destinationPolygons = new Dictionary<string, Polygon>();
                foreach (var item in this._host.AgentMandatoryScenario.Sequences)
                {
                    foreach (var name in item.ActivityNames)
                    {
                        if (!this._destinationPolygons.ContainsKey(name))
                        {
                            Polygon polygon = new Polygon();
                            foreach (var pnt in this._host.AllActivities[name].DestinationArea.BoundaryPoints)
                            {
                                polygon.Points.Add(new Point(pnt.U, pnt.V));
                            }
                            polygon.StrokeThickness = 0.0;
                            polygon.Fill = Brushes.Wheat;
                            polygon.Opacity = 0.2d;
                            this.Children.Add(polygon);
                            this._destinationPolygons.Add(name, polygon);
                            Canvas.SetZIndex(polygon, 70);
                        }
                    }
                }
                foreach (var name in this._host.AgentMandatoryScenario.MainStations)
                {
                    if (!this._destinationPolygons.ContainsKey(name))
                    {
                        Polygon polygon = new Polygon();
                        foreach (var pnt in this._host.AllActivities[name].DestinationArea.BoundaryPoints)
                        {
                            polygon.Points.Add(new Point(pnt.U, pnt.V));
                        }
                        polygon.StrokeThickness = 0.0;
                        polygon.Fill = Brushes.Tomato;
                        polygon.Opacity = 0.2d;
                        this.Children.Add(polygon);
                        this._destinationPolygons.Add(name, polygon);
                        Canvas.SetZIndex(polygon, 70);
                    }
                }
            }

            if (this.agent.CurrentActivity != null && this._host.AllActivities.ContainsKey(this.agent.CurrentActivity.Name))
            {
                if (this.currentActivityName == null || this.currentActivityName != this.agent.CurrentActivity.Name)
                {
                    this.currentActivityName = this.agent.CurrentActivity.Name;
                    foreach (var item in this._destinationPolygons.Values)
                    {
                        item.Opacity = 0.2d;
                    }
                    this._destinationPolygons[this.currentActivityName].Opacity = 1.0d;
                }
            }
        }

        void _strokeThickness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.agent.Dispatcher.BeginInvoke(DispatcherPriority.Background,
            (SendOrPostCallback)delegate
            {
                this.agent.SetValue(VisualAgentMandatoryScenario.StrokeThicknessProperty, e.NewValue);
            }
            , null);
        }

        void _viewAngle_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.agent.Dispatcher.BeginInvoke(DispatcherPriority.Background,
            (SendOrPostCallback)delegate
            {
                this.agent.SetValue(VisualAgentMandatoryScenario.VisibilityAngleProperty, e.NewValue * Math.PI / 180);
            }
            , null);
        }

        void _angularVelocity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.agent.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (SendOrPostCallback)delegate
                {
                    this.agent.SetValue(VisualAgentMandatoryScenario.AngularVelocityProperty, e.NewValue);
                }
                , null);
        }

        void _scale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.agent.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (SendOrPostCallback)delegate
                {
                    this.agent.SetValue(VisualAgentMandatoryScenario.BodySizeProperty, e.NewValue);
                }
                , null);
        }

        void _velocity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.agent.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (SendOrPostCallback)delegate
                {
                    this.agent.SetValue(VisualAgentMandatoryScenario.VelocityMagnitudeProperty, e.NewValue);
                }
                , null);
        }

        private void _showVisibilityAngle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (this.agent != null)
            {
                this.agent.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    (SendOrPostCallback)delegate
                    {
                        this.agent.SetValue(VisualAgentMandatoryScenario.ShowVisibilityConeProperty, false);
                    }
                    , null);
            }
        }

        private void _showVisibilityAngle_Checked(object sender, RoutedEventArgs e)
        {
            if (this.agent != null)
            {
                this.agent.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    (SendOrPostCallback)delegate
                    {
                        this.agent.SetValue(VisualAgentMandatoryScenario.ShowVisibilityConeProperty, true);
                    }
                    , null);
            }
        }

        void _showSafetyBuffer_Unchecked(object sender, RoutedEventArgs e)
        {
            if (this.agent != null)
            {
                this.agent.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    (SendOrPostCallback)delegate
                    {
                        this.agent.SetValue(VisualAgentMandatoryScenario.ShowSafetyBufferProperty, false);
                    }
                    , null);
            }
        }

        void _showSafetyBuffer_Checked(object sender, RoutedEventArgs e)
        {
            if (this.agent != null)
            {
                this.agent.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    (SendOrPostCallback)delegate
                    {
                        this.agent.SetValue(VisualAgentMandatoryScenario.ShowSafetyBufferProperty, true);
                    }
                    , null);
            }
        }

        //void scene_AgentDirectionSetTerminated(object sender, MouseButtonEventArgs e)
        //{
        //    this._host.FloorScene.MouseMove -= scene_AgentDirectionSet;
        //    this._host.FloorScene.MouseLeftButtonDown -= scene_AgentDirectionSetTerminated;
        //    

        //    
        //    this._host.UIMessage.Visibility = System.Windows.Visibility.Hidden;
        //    this._settings.ShowDialog();
        //}

        void _initStart_Click(object sender, RoutedEventArgs e)
        {

            #region Debug only
            VisualAgentMandatoryScenario.UIMessage = this._host.UIMessage;
            this._host.UIMessage.Text = string.Empty;
            this._host.UIMessage.Visibility = System.Windows.Visibility.Visible;
            #endregion

            this._settings._walkThrough.IsEnabled = true;
            this._settings._walkThrough.Checked += _walkThrough_Checked;
            this._settings._walkThrough.Unchecked += _walkThrough_Unchecked;
            this.agent.WalkInit(this._host.cellularFloor, this._host.AgentScapeRoutes);
            this._settings.init_Title.Text = "Stop Animation";
            this._settings._init.Click -= _initStart_Click;
            this._settings._init.Click += _initStop_Click;
            this._animationInProgress = true;
            this._settings._hide.Visibility = System.Windows.Visibility.Visible;
            this._settings._hide.Click += _hide_Click;

        }

        void _walkThrough_Checked(object sender, RoutedEventArgs e)
        {
            this._settings.view3d.Visibility = System.Windows.Visibility.Visible;
            if (this._settings._allModels.Children.Count < 2)
            {
                var models = this._host.BIM_To_OSM.ParseBIM(this._host.cellularFloor.Territory_Min, this._host.cellularFloor.Territory_Max, 1);//offset);
                List<GeometryModel3D> meshObjects = new List<GeometryModel3D>(models.Count);
                foreach (GeometryModel3D item in models)
                {
                    meshObjects.Add(item);
                }
                this._settings._allModels.Children = new System.Windows.Media.Media3D.Model3DCollection(meshObjects);
                var light = new DirectionalLight()
                {
                    Color = Colors.White,
                    Direction = new Vector3D(-1, -1, -3),
                };
                this._settings._allModels.Children.Add(light);
            }
            this.agent.RaiseVisualEvent += this.updateCamera;
            this._settings._camera.UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 0, 1);
        }
        void _walkThrough_Unchecked(object sender, RoutedEventArgs e)
        {
            this._settings.view3d.Visibility = System.Windows.Visibility.Collapsed;
            this.agent.RaiseVisualEvent -= this.updateCamera;
        }
        void _hide_Click(object sender, RoutedEventArgs e)
        {
            this._host.Menues.IsEnabled = false;
            this._host.UIMessage.Visibility = System.Windows.Visibility.Visible;
            this._host.UIMessage.Text = "Press the ARROW to show agent control panel";
            this._host.MouseBtn.MouseDown += _unhide_Click;
            this._settings.Hide();
        }

        void _unhide_Click(object sender, MouseButtonEventArgs e)
        {
            this._host.Menues.IsEnabled = true;
            this._host.UIMessage.Visibility = System.Windows.Visibility.Collapsed;
            this._host.MouseBtn.MouseDown -= _unhide_Click;
            this._settings.ShowDialog();
        }
        void _initStop_Click(object sender, RoutedEventArgs e)
        {
            this.stopAnimation();
            var reporter = new DebugReporter();
            reporter.AddReport(VisualAgentMandatoryScenario.Debuger.ToString());
            VisualAgentMandatoryScenario.Debuger.Clear();
            reporter.Owner = this._host;
            reporter.ShowDialog();
        }

        public void stopAnimation()
        {
            if (this._animationInProgress)
            {
                this._getWalkingTrailDataMenu.IsEnabled = true;
                this._settings._walkThrough.IsEnabled = false;
                this._settings._walkThrough.IsChecked = false;
                this._settings._walkThrough.Checked -= _walkThrough_Checked;
                this._settings._walkThrough.Unchecked -= _walkThrough_Unchecked;
                this._settings._hide.Visibility = System.Windows.Visibility.Collapsed;
                this._settings._hide.Click -= _hide_Click;
                this._settings.init_Title.Text = "Start Animation";
                this.agent.StopAnimation();
                this._settings._init.Click += _initStart_Click;
                this._settings._init.Click -= _initStop_Click;
                this._animationInProgress = false;
            }
        }


        private void updateCamera()
        {
            this._settings._camera.Position = new System.Windows.Media.Media3D.Point3D(this.agent.CurrentState.Location.U,
                this.agent.CurrentState.Location.V, this._host.BIM_To_OSM.PlanElevation + this._host.BIM_To_OSM.VisibilityObstacleHeight);
            this._settings._camera.LookDirection = new System.Windows.Media.Media3D.Vector3D(this.agent.CurrentState.Direction.U,
                this.agent.CurrentState.Direction.V, 0);
        }

        // clear agent and the events related to it
        private void _clearAgent_Click(object sender, RoutedEventArgs e)
        {
            this.stopAnimation();
            this.cleanUpAgent();
        }

        private void cleanUpAgent()
        {
            if (this._animationInProgress)
            {
                MessageBox.Show("Stop the animation before removing the agent");
                return;
            }
            this._settings._visualTrigger.Checked -= _visualTrigger_Checked;
            this._settings._visualTrigger.Unchecked -= _visualTrigger_Unchecked;
            this._settings._showRepulsionTrajectory.Checked -= _showRepulsionTrajectory_Checked;
            this._settings._showRepulsionTrajectory.Unchecked -= _showRepulsionTrajectory_Unchecked;
            this._settings._showClosestBarrier.Checked -= _showClosestBarrier_Checked;
            this._settings._showClosestBarrier.Unchecked -= _showClosestBarrier_Unchecked;
            this._settings._accelerationMagnitude.ValueChanged -= _accelerationMagnitude_ValueChanged;
            this._settings._barrierRepulsionRange.ValueChanged -= _barrierRepulsionRange_ValueChanged;
            this._settings._repulsionChangeRate.ValueChanged -= _repulsionChangeRate_ValueChanged;
            this._settings._captureVisualEvents.Checked -= _captureVisualEvents_Checked;
            this._settings._captureVisualEvents.Unchecked -= _captureVisualEvents_Unchecked;
            this._settings._showDestination.Checked -= _showVisibleDestinations_Checked;
            this._settings._showDestination.Unchecked -= _showVisibleDestinations_Unchecked;
            this._settings._angularVelocity.ValueChanged -= _angularVelocity_ValueChanged;
            this._settings._velocity.ValueChanged -= _velocity_ValueChanged;
            this._settings._bodySize.ValueChanged -= _scale_ValueChanged;
            this._settings._viewAngle.ValueChanged -= _viewAngle_ValueChanged;
            this._settings._showVisibiityAngle.Checked -= _showVisibilityAngle_Checked;
            this._settings._showVisibiityAngle.Unchecked -= _showVisibilityAngle_Unchecked;
            this._settings._init.Click -= _initStart_Click;
            this._settings._strokeThickness.ValueChanged -= _strokeThickness_ValueChanged;
            this._settings._showSafetyBuffer.Checked -= _showSafetyBuffer_Checked;
            this._settings._showSafetyBuffer.Unchecked -= _showSafetyBuffer_Unchecked;
            this._settings._barrierFriction.ValueChanged -= _barrierFriction_ValueChanged;
            this._settings._bodyElasticity.ValueChanged -= _bodyElasticity_ValueChanged;
            this.Children.Remove(this.agent);
            this.agent = null;
            VisualAgentMandatoryScenario.UIMessage = null;
            this._host.UIMessage.Text = string.Empty;
            this._host.UIMessage.Visibility = System.Windows.Visibility.Hidden;
            this._host.UIMessage.Foreground = Brushes.DarkRed;
            if (this._destinationPolygons != null)
            {
                foreach (var item in this._destinationPolygons.Values)
                {
                    int index = this.Children.IndexOf(item);
                    if (index != -1)
                    {
                        this.Children.RemoveAt(index);
                    }
                }
                this._destinationPolygons.Clear();
                this._destinationPolygons = null;
            }

            if (this._lineToTarget != null)
            {
                try
                {
                    this.Children.Remove(this._lineToTarget);
                }
                catch (Exception)
                {
                }
                this._lineToTarget = null;
            }
            this._settings._addAgent.IsEnabled = true;
            this._settings._addAgent.Click += this._addAgent_Click;
            this._settings._clearAgent.IsEnabled = false;
        }

        void _visualTrigger_Unchecked(object sender, RoutedEventArgs e)
        {
            this.agent.VisualTriggerDetected -= agent_VisualTriggerDetected;
        }

        void _visualTrigger_Checked(object sender, RoutedEventArgs e)
        {
            this.agent.VisualTriggerDetected += agent_VisualTriggerDetected;
        }

        void agent_VisualTriggerDetected(object sender, RoutedEventArgs e)
        {
            VisualTriggerArgs args = (VisualTriggerArgs)e;

            if (this._lineToVisualTrigger == null)
            {
                this._lineToVisualTrigger = new Line();
                this._lineToVisualTrigger.Stroke = Brushes.DarkBlue;
                this._lineToVisualTrigger.StrokeThickness = 2 * this.agent.StrokeThickness;
                this.Children.Add(this._lineToVisualTrigger);               
            }
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                this._lineToVisualTrigger.Visibility = System.Windows.Visibility.Visible;
                this._lineToVisualTrigger.X1 = this.agent.CurrentState.Location.U;
                this._lineToVisualTrigger.Y1 = this.agent.CurrentState.Location.V;
                this._lineToVisualTrigger.X2 = args.Target.U;
                this._lineToVisualTrigger.Y2 = args.Target.V;
            }));

            var reporter = new SpatialAnalysis.Visualization.DebugReporter();
            reporter.Title = "A sequence was visually triggered".ToUpper();
            reporter.ShowInTaskbar = false;
            reporter.Owner = this._host;
            reporter.Closing += reporter_Closing;
            reporter.Height = 120;
            reporter.Width = 550;
            reporter.AddReport(string.Format("'{0}' is visually detected {1} seconds after its activation!", args.TriggeredSequence.Name, args.Delay.ToString("0.00000")));
            reporter.ShowDialog();
        }

        void reporter_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ((DebugReporter) sender).Closing += reporter_Closing;
            this._lineToVisualTrigger.Visibility = System.Windows.Visibility.Hidden;
        }


        void _trainingMenu_Click(object sender, RoutedEventArgs e)
        {
            if (this._host.trailVisualization.AgentWalkingTrail == null)
            {
                MessageBox.Show("Setting a walking trail for training is required", "Walking Trail Not Defined");
                return;
            }

            MandatoryScenarioTraining training = new MandatoryScenarioTraining(this._host);
            training.Owner = this._host;
            training.ShowDialog();
            
        }

        /// <summary>
        /// Sets the host.
        /// </summary>
        /// <param name="host">The main document to which this scene belongs.</param>
        public void SetHost(OSMDocument host)
        {
            this._host = host;
            this.RenderTransform = this._host.RenderTransformation;
            this._host.MandatoryScenario.Items.Add(this._controlPanelMenu);
            this._host.MandatoryScenario.Items.Add(this._getWalkingTrailDataMenu);
            this._host.MandatoryScenario.Items.Add(this._captureEventMenu);
            this._host.MandatoryScenario.Items.Add(this._trainingMenu);
        }


    }
}

