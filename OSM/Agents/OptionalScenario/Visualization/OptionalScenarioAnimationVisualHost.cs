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
using SpatialAnalysis.CellularEnvironment;
using System.Windows.Shapes;
using SpatialAnalysis.Agents.Visualization.AgentModel;
using System.Windows.Media.Imaging;
using SpatialAnalysis.Interoperability;
using System.Windows.Threading;
using System.Threading;
using System.Windows.Media.Media3D;
using SpatialAnalysis.Data;
using SpatialAnalysis.Events;
using System.Windows.Data;
using SpatialAnalysis.Miscellaneous;


namespace SpatialAnalysis.Agents.OptionalScenario.Visualization
{
    /// <summary>
    /// This class controls the animation control panel, visualizes the 2D animation, and implements the interaction logic among user, the 2d scene and the control panel. For protection its members are private.
    /// </summary>
    /// <seealso cref="System.Windows.Controls.Canvas" />
    internal class OptionalScenarioAnimationVisualHost : Canvas
    {
        private Line _lineToTarget { get; set; }
        private Path _destinationLines { get; set; }
        private StreamGeometry _destinationLinesGeometry { get; set; }
        private bool _animationInProgress;
        private VisualAgentOptionalScenario agent { get; set; }
        private OSMDocument _host;
        private MenuItem _calculateScapeRoutesMenu { get; set; }
        private MenuItem _getWalkingTrailDataMenu { get; set; }
        private MenuItem _captureEventMenu { get; set; }
        private MenuItem _trainingMenu { get; set; }
        public MenuItem _animation_Menu { get; set; }
        private Line _closestEdge { get; set; }
        private Line _repulsionTrajectoryLine { get; set; }

        private RealTimeFreeNavigationControler _settings { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionalScenarioAnimationVisualHost"/> class.
        /// </summary>
        public OptionalScenarioAnimationVisualHost()
        {
            this._animation_Menu = new MenuItem
            {
                Header="Free Navigation Control Panel",
                IsEnabled=false,
            };
            this._animation_Menu.Click += animation_Menu_Click;
            this._getWalkingTrailDataMenu = new MenuItem()
            {
                Header="Simulate",
                IsEnabled = false,
            };
            this._getWalkingTrailDataMenu.Click += new RoutedEventHandler(_getWalkingTrailDataMenu_Click);
            this._captureEventMenu = new MenuItem { Header = "Capture Events" };
            this._captureEventMenu.Click += new RoutedEventHandler(_captureEventMenu_Click);
            this._trainingMenu = new MenuItem { Header = "Train Agent" };
            this._trainingMenu.Click += new RoutedEventHandler(_trainingMenu_Click);
        }


        void _captureEventMenu_Click(object sender, RoutedEventArgs e)
        {
            EventCapturingInterface eventCapturingInterface = new EventCapturingInterface(this._host, EvaluationEventType.Optional);
            eventCapturingInterface.Owner = this._host;
            eventCapturingInterface.ShowDialog();
            eventCapturingInterface = null;
        }
        WalkingTrailData _walkingTrailDataComputer { get; set; }
        void _getWalkingTrailDataMenu_Click(object sender, RoutedEventArgs e)
        {
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
            bool notification = this._walkingTrailDataComputer._notify.IsChecked.Value;
            var simulator = new OptionalScenarioSimulation(this._host, h, duration);
            Thread simulationThread = new Thread(
                () => simulator.Simulate(name, notification)
                );
            simulationThread.Start();
        }


        void animation_Menu_Click(object sender, RoutedEventArgs e)
        {
            this._settings = new RealTimeFreeNavigationControler();
            //set events
            this._settings._addAgent.Click += _addAgent_Click;
            this._settings.Closing += _settings_Closing;
            //show the dialog
            this._settings.Owner = this._host;
            this._settings.ShowDialog();
        }

        void _settings_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
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
            //events for locating the agent
            this._settings._addAgent.Click -= _addAgent_Click;
            this._settings._addAgent.IsEnabled = false;
            this._settings._clearAgent.Click += _clearAgent_Click;

            #region Agent Creation
            this.agent = new VisualAgentOptionalScenario
            {
                StrokeThickness = this._settings._strokeThickness.Value,
                Stroke = VisualAgentOptionalScenario.OutsideRange,
                Fill = VisualAgentOptionalScenario.OutsideRange,
                VelocityMagnitude = this._settings._velocity.Value,
                ShowVisibilityCone = this._settings._showVisibiityAngle.IsChecked.Value,
                ShowSafetyBuffer = this._settings._showSafetyBuffer.IsChecked.Value,
                BodySize = this._settings._bodySize.Value,
                VisibilityAngle = this._settings._viewAngle.Value * Math.PI / 180.0d,
                AngularVelocity = this._settings._angularVelocity.Value,
                DecisionMakingPeriodLambdaFactor = this._settings._decisionMakingPeriod.Value,
                Destination = null,
                DesirabilityDistributionLambdaFactor = this._settings._desirabilityWeight.Value,
                AngleDistributionLambdaFactor=this._settings._angleWeight.Value,
                DecisionMakingPeriodDistribution = new MathNet.Numerics.Distributions.Exponential(1.0d/this._settings._decisionMakingPeriod.Value),
                BarrierRepulsionRange = this._settings._barrierRepulsionRange.Value,
                RepulsionChangeRate = this._settings._repulsionChangeRate.Value,
                AccelerationMagnitude = this._settings._accelerationMagnitude.Value,
            };
            //I am calling this from outside to initiate the DecisionMakingPeriodDistribution property
            Canvas.SetZIndex(this.agent, 100);
            #endregion

            this.Children.Add(this.agent);

            #region Agent Events' Registration
            this._settings._strokeThickness.ValueChanged += _strokeThickness_ValueChanged;

            this._settings._viewAngle.ValueChanged += _viewAngle_ValueChanged;

            this._host.FloorScene.MouseMove += scene_AgentPositionSet;
            this._host.FloorScene.MouseLeftButtonDown += scene_AgentPositionSetTerminated;

            this._settings._velocity.ValueChanged += _velocity_ValueChanged;
            this._settings._accelerationMagnitude.ValueChanged += _accelerationMagnitude_ValueChanged;

            this._settings._bodySize.ValueChanged += _scale_ValueChanged;

            this._settings._angularVelocity.ValueChanged += _angularVelocity_ValueChanged;

            this._settings._showVisibiityAngle.Checked += _showVisibilityAngle_Checked;
            this._settings._showVisibiityAngle.Unchecked += _showVisibilityAngle_Unchecked;

            this._settings._showSafetyBuffer.Checked += _showSafetyBuffer_Checked;
            this._settings._showSafetyBuffer.Unchecked += _showSafetyBuffer_Unchecked;

            this._settings._showVisibeDestinations.Checked += _showVisibleDestinations_Checked;
            this._settings._showVisibeDestinations.Unchecked += _showVisibleDestinations_Unchecked;

            this._settings._decisionMakingPeriod.ValueChanged += _decisionMakingPeriod_ValueChanged;
            this._settings._captureVisualEvents.Checked += _captureVisualEvents_Checked;
            this._settings._captureVisualEvents.Unchecked += _captureVisualEvents_Unchecked;
            this._settings._showClosestBarrier.Checked += _showClosestBarrier_Checked;
            this._settings._showClosestBarrier.Unchecked += _showClosestBarrier_Unchecked;

            this._settings._angleWeight.ValueChanged += _angleWeight_ValueChanged;
            this._settings._desirabilityWeight.ValueChanged += _desirabilityWeight_ValueChanged;

            this._settings._barrierRepulsionRange.ValueChanged += _barrierRepulsionRange_ValueChanged;
            this._settings._repulsionChangeRate.ValueChanged += _repulsionChangeRate_ValueChanged;

            this._settings._showRepulsionTrajectory.Checked += this._showRepulsionTrajectory_Checked;
            this._settings._showRepulsionTrajectory.Unchecked += this._showRepulsionTrajectory_Unchecked;

            this._settings._barrierFriction.ValueChanged += _barrierFriction_ValueChanged;
            this._settings._bodyElasticity.ValueChanged += _bodyElasticity_ValueChanged;

            #endregion

            this._settings.Hide();
        }

        void _bodyElasticity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.agent.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (SendOrPostCallback)delegate
                {
                    this.agent.SetValue(VisualAgentOptionalScenario.BodyElasticityProperty, e.NewValue);
                }
                , null);
        }

        void _barrierFriction_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.agent.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (SendOrPostCallback)delegate
                {
                    this.agent.SetValue(VisualAgentOptionalScenario.BarrierFrictionProperty, e.NewValue);
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
            this.agent.ShowClosestBarrier -= this.showClosestBarrier;
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
            this.agent.ShowClosestBarrier += this.showClosestBarrier;
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
                    this.agent.SetValue(VisualAgentOptionalScenario.AccelerationMagnitudeProperty, e.NewValue);
                }
                , null);
        }

        void _repulsionChangeRate_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.agent.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (SendOrPostCallback)delegate
                {
                    this.agent.SetValue(VisualAgentOptionalScenario.RepulsionChangeRateProperty, e.NewValue);
                }
                , null);
        }

        void _barrierRepulsionRange_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.agent.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (SendOrPostCallback)delegate
                {
                    this.agent.SetValue(VisualAgentOptionalScenario.BarrierRepulsionRangeProperty, e.NewValue);
                }
                , null);
        }

        void _desirabilityWeight_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.agent.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (SendOrPostCallback)delegate
                {
                    this.agent.SetValue(VisualAgentOptionalScenario.DesirabilityDistributionLambdaFactorProperty, e.NewValue);
                }
                , null);
        }

        void _angleWeight_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.agent.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (SendOrPostCallback)delegate
                {
                    this.agent.SetValue(VisualAgentOptionalScenario.AngleDistributionLambdaFactorProperty, e.NewValue);
                }
                , null);
        }

        void _captureVisualEvents_Unchecked(object sender, RoutedEventArgs e)
        {
            this.agent.RaiseVisualEvent -= this.raiseVisualEvent;
            if (this._lineToTarget!= null)
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

                if (visualTarget!= null)
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
                    if (this._lineToTarget!=null)
                    {
                        this._lineToTarget.Visibility = System.Windows.Visibility.Collapsed;
                    }
                }
	        }
        }

        void _decisionMakingPeriod_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.agent.DecisionMakingPeriodLambdaFactor = e.NewValue;
        }

        void _showVisibleDestinations_Unchecked(object sender, RoutedEventArgs e)
        {
            this.agent.RaiseVisualEvent -= this.visualizeAgentPossibleDestinations;
            if (this._destinationLines != null)
            {
                int index = this.Children.IndexOf(this._destinationLines);
                if (index != -1)
                {
                    this.Children.Remove(this._destinationLines);
                    this._destinationLinesGeometry.Clear();
                }
                this._destinationLines = null;
            }
        }

        void _showVisibleDestinations_Checked(object sender, RoutedEventArgs e)
        {
            this.agent.RaiseVisualEvent += this.visualizeAgentPossibleDestinations;
        }

        void _strokeThickness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.agent.Dispatcher.BeginInvoke(DispatcherPriority.Background,
            (SendOrPostCallback)delegate
            {
                this.agent.SetValue(VisualAgentOptionalScenario.StrokeThicknessProperty, e.NewValue);
            }
            , null);
        }

        void _viewAngle_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.agent.Dispatcher.BeginInvoke(DispatcherPriority.Background,
            (SendOrPostCallback)delegate
            {
                this.agent.SetValue(VisualAgentOptionalScenario.VisibilityAngleProperty, e.NewValue * Math.PI / 180);
            }
            , null);
        }

        void _angularVelocity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.agent.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (SendOrPostCallback)delegate
                {
                    this.agent.SetValue(VisualAgentOptionalScenario.AngularVelocityProperty, e.NewValue);
                }
                , null);
        }

        void _scale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.agent.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (SendOrPostCallback)delegate
                {
                    this.agent.SetValue(VisualAgentOptionalScenario.BodySizeProperty, e.NewValue);
                }
                , null);
        }

        void _velocity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.agent.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                (SendOrPostCallback)delegate
                {
                    this.agent.SetValue(VisualAgentOptionalScenario.VelocityMagnitudeProperty, e.NewValue);
                }
                , null);
        }

        void scene_AgentPositionSet(object sender, MouseEventArgs e)
        {
            Point p = this._host.InverseRenderTransform.Transform(Mouse.GetPosition(this._host.FloorScene));
            UV uv = new UV(p.X, p.Y);
            this.agent.SetInitialPosition(uv);
            this._host.UIMessage.Visibility = System.Windows.Visibility.Visible;
            this._host.UIMessage.Text = "Click on the scene to locate the agent";
        }

        void scene_AgentPositionSetTerminated(object sender, MouseButtonEventArgs e)
        {
            Point p = this._host.InverseRenderTransform.Transform(Mouse.GetPosition(this._host.FloorScene));
            UV uv = new UV(p.X, p.Y);
            Cell agentCell = this._host.cellularFloor.FindCell(uv);
            if (!this._host.AgentScapeRoutes.ContainsKey(agentCell))
            {
                MessageBox.Show("Agent cannot be located inside boundary buffer");
                return;
            }
            this._host.FloorScene.MouseMove -= scene_AgentPositionSet;
            this._host.FloorScene.MouseLeftButtonDown -= scene_AgentPositionSetTerminated;

            this._host.FloorScene.MouseMove += scene_AgentDirectionSet;
            this._host.FloorScene.MouseLeftButtonDown += scene_AgentDirectionSetTerminated;
        }

        void scene_AgentDirectionSet(object sender, MouseEventArgs e)
        {
            Point p = this._host.InverseRenderTransform.Transform(Mouse.GetPosition(this._host.FloorScene));
            UV direction = new UV(p.X, p.Y) - this.agent.CurrentState.Location;
            this.agent.SetInitialDirection(direction);
            this._host.UIMessage.Text = "Click on the scene to set the direction of the agent";
        }

        private void _showVisibilityAngle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (this.agent != null)
            {
                this.agent.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    (SendOrPostCallback)delegate
                    {
                        this.agent.SetValue(VisualAgentOptionalScenario.ShowVisibilityConeProperty, false);
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
                        this.agent.SetValue(VisualAgentOptionalScenario.ShowVisibilityConeProperty, true);
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
                        this.agent.SetValue(VisualAgentOptionalScenario.ShowSafetyBufferProperty, false);
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
                        this.agent.SetValue(VisualAgentOptionalScenario.ShowSafetyBufferProperty, true);
                    }
                    , null);
            }
        }

        void scene_AgentDirectionSetTerminated(object sender, MouseButtonEventArgs e)
        {
            this._host.FloorScene.MouseMove -= scene_AgentDirectionSet;
            this._host.FloorScene.MouseLeftButtonDown -= scene_AgentDirectionSetTerminated;
            this._settings._init.Click += _initStart_Click;

            this._settings._clearAgent.IsEnabled = true;
            this._host.UIMessage.Visibility = System.Windows.Visibility.Hidden;
            this._settings.ShowDialog();
        }

        void _initStart_Click(object sender, RoutedEventArgs e)
        {

            #region Debug only
            VisualAgentOptionalScenario.UIMessage = this._host.UIMessage;
            this._host.UIMessage.Text = string.Empty;
            this._host.UIMessage.Visibility = System.Windows.Visibility.Visible; 
            #endregion

            this._settings._walkThrough.IsEnabled= true;
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
            if (this._settings._allModels.Children.Count<2)
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
            this._host.FreeNavigationAgentCharacter = FreeNavigationAgent.Create(this.agent.CurrentState);// new FreeNavigationAgent(this.agent);
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
            //double total = 0;
            //foreach (var item in this.agent.TimeSteps)
            //{
            //    total += item;
            //}
            //MessageBox.Show("Avarage timestep: " + (total / agent.TimeSteps.Count).ToString());
            var reporter = new SpatialAnalysis.Visualization.DebugReporter();
            reporter.AddReport(VisualAgentOptionalScenario.Debuger.ToString());
            VisualAgentOptionalScenario.Debuger.Clear();
            reporter.Owner = this._host;
            reporter.ShowDialog();

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
            this.cleanUpAgent();
        }

        private void cleanUpAgent()
        {
            if (this._animationInProgress)
            {
                MessageBox.Show("Stop the animation before removing the agent");
                return;
            }
            this._settings._showRepulsionTrajectory.Checked -= _showRepulsionTrajectory_Checked;
            this._settings._showRepulsionTrajectory.Unchecked -= _showRepulsionTrajectory_Unchecked;
            this._settings._showClosestBarrier.Checked -= _showClosestBarrier_Checked;
            this._settings._showClosestBarrier.Unchecked -= _showClosestBarrier_Unchecked;
            this._settings._accelerationMagnitude.ValueChanged -= _accelerationMagnitude_ValueChanged;
            this._settings._barrierRepulsionRange.ValueChanged -= _barrierRepulsionRange_ValueChanged;
            this._settings._repulsionChangeRate.ValueChanged -= _repulsionChangeRate_ValueChanged;
            this._settings._angleWeight.ValueChanged -= _angleWeight_ValueChanged;
            this._settings._desirabilityWeight.ValueChanged -= _desirabilityWeight_ValueChanged;
            this._settings._decisionMakingPeriod.ValueChanged -= _decisionMakingPeriod_ValueChanged;
            this._settings._captureVisualEvents.Checked -= _captureVisualEvents_Checked;
            this._settings._captureVisualEvents.Unchecked -= _captureVisualEvents_Unchecked;
            this._settings._showVisibeDestinations.Checked -= _showVisibleDestinations_Checked;
            this._settings._showVisibeDestinations.Unchecked -= _showVisibleDestinations_Unchecked;
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
            VisualAgentOptionalScenario.UIMessage = null;
            this._host.UIMessage.Text = string.Empty;
            this._host.UIMessage.Visibility = System.Windows.Visibility.Hidden;
            this._host.UIMessage.Foreground = Brushes.DarkRed;
            if (this._destinationLines != null)
            {
                if (this._destinationLines != null)
                {
                    int index = this.Children.IndexOf(this._destinationLines);
                    if (index != -1)
                    {
                        this.Children.Remove(this._destinationLines);
                        this._destinationLinesGeometry.Clear();
                    }
                }
            }
            this._destinationLinesGeometry = null;
            this._destinationLines = null;
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
        //visualize Agent's Possible Destinations
        private void visualizeAgentPossibleDestinations()
        {
            Cell vantageCell = this._host.cellularFloor.FindCell(this.agent.CurrentState.Location);
            if (vantageCell == null || !this._host.AgentScapeRoutes.ContainsKey(vantageCell))
            {
                return;
            }
            if (this._destinationLines == null)
            {
                this._destinationLinesGeometry=new StreamGeometry();
                this._destinationLines = new Path()
                {
                    Stroke = Brushes.Green,
                    StrokeThickness = this.agent.StrokeThickness,
                    StrokeDashArray = new DoubleCollection() { this.agent.StrokeThickness, this.agent.StrokeThickness },
                };
                this.Children.Add(this._destinationLines);
            }
            this._destinationLinesGeometry.Clear();
            using (var sgo = this._destinationLinesGeometry.Open())
            {
                Point p0 = new Point(this.agent.CurrentState.Location.U, this.agent.CurrentState.Location.V);
                sgo.BeginFigure(p0, false, false);
                foreach (var item in this._host.AgentScapeRoutes[vantageCell].Destinations)
                {
                    sgo.LineTo(p0, false, false);
                    sgo.LineTo(new Point(item.Destination.U, item.Destination.V), true, true);
                }
            }
            this._destinationLines.Data = this._destinationLinesGeometry;
        }

        void _trainingMenu_Click(object sender, RoutedEventArgs e)
        {
            if (this._host.trailVisualization.AgentWalkingTrail == null)
            {
                MessageBox.Show("Setting a walking trail for training is required", "Walking Trail Not Defined");
                return;
            }
            OptionalScenarioTraining training = new OptionalScenarioTraining(this._host);
            training.Owner = this._host;
            training.ShowDialog();
        }

        /// <summary>
        /// Sets the host.
        /// </summary>
        /// <param name="host">The main document to which this control belongs.</param>
        public void SetHost(OSMDocument host)
        {
            this._host = host;
            this.RenderTransform = this._host.RenderTransformation;
            this._host.OptionalScenarios.Items.Insert(2, this._captureEventMenu);
            this._host.OptionalScenarios.Items.Insert(2, this._getWalkingTrailDataMenu);
            this._host.OptionalScenarios.Items.Insert(2, this._animation_Menu);
            this._host.OptionalScenarios.Items.Add(this._trainingMenu);
            var bindConvector = new ValueToBoolConverter();
            Binding bind = new Binding("FreeNavigationAgentCharacter");
            bind.Source = this._host;
            bind.Converter = bindConvector;
            bind.Mode = BindingMode.OneWay;
            this._captureEventMenu.SetBinding(MenuItem.IsEnabledProperty, bind);
            this._getWalkingTrailDataMenu.SetBinding(MenuItem.IsEnabledProperty, bind);
            Binding animation = new Binding("AgentScapeRoutes");
            animation.Source = this._host;
            animation.Converter = bindConvector;
            animation.Mode = BindingMode.OneWay;
            this._animation_Menu.SetBinding(MenuItem.IsEnabledProperty, animation);
            this._trainingMenu.SetBinding(MenuItem.IsEnabledProperty, animation);
        }
        

    }
}

