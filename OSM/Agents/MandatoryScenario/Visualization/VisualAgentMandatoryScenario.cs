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
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using SpatialAnalysis.Geometry;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using SpatialAnalysis.CellularEnvironment;
using SpatialAnalysis.IsovistUtility;
using SpatialAnalysis.Agents.Visualization.AgentModel;
using SpatialAnalysis.Data;
using SpatialAnalysis.FieldUtility;
using SpatialAnalysis.Miscellaneous;

namespace SpatialAnalysis.Agents.MandatoryScenario.Visualization
{
    /// <summary>
    /// The event argument that includes the information about the tasks that are visually detected.
    /// </summary>
    /// <seealso cref="System.Windows.RoutedEventArgs" />
    public class VisualTriggerArgs : RoutedEventArgs
    {
        /// <summary>
        /// Gets or sets the triggered sequence.
        /// </summary>
        /// <value>The triggered sequence.</value>
        public Sequence TriggeredSequence { get; set; }
        /// <summary>
        /// Gets or sets the target.
        /// </summary>
        /// <value>The target.</value>
        public UV Target { get; set; }
        /// <summary>
        /// Gets or sets the delay time after the sequence activation when it was noticed.
        /// </summary>
        /// <value>The delay.</value>
        public double Delay { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="VisualTriggerArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event.</param>
        /// <param name="sequence">The sequence.</param>
        /// <param name="target">The target.</param>
        /// <param name="delay">The delay.</param>
        public VisualTriggerArgs(RoutedEvent routedEvent, Sequence sequence, UV target, double delay)
            : base(routedEvent)
        {
            this.TriggeredSequence = sequence;
            this.Target = target;
            this.Delay = delay;
        }
    }
    /// <summary>
    /// Enum MandatoryScenarioStatus
    /// </summary>
    public enum MandatoryScenarioStatus
    {
        /// <summary>
        /// the agent is not performing a task and will move to the best station
        /// </summary>
        Free = 0,
        /// <summary>
        /// the agent is located in the destination area and is engaged with a given task 
        /// </summary>
        Engaged = 1,
        /// <summary>
        /// the agent is walking to address a task
        /// </summary>
        WalkingInSequence = 2,
    }
    /// <summary>
    /// Enum PhysicalMovementMode
    /// </summary>
    public enum PhysicalMovementMode
    {
        /// <summary>
        /// when an agent tries to stop at a destination acceleration acts as a resistance force against velocity
        /// </summary>
        StopAndOrient = 0,
        /// <summary>
        /// When an agent tries to reach a destination acceleration drags the agent
        /// </summary>
        Move = 1,
    }
    /// <summary>
    /// This function pointer is used to connect updates in the 3D animation with 2D scene of the floor
    /// </summary>
    public delegate void AgentUpdated();
    /// <summary>
    /// This class includes the logic of mandatory occupancy simulation and animating the agent.
    /// </summary>
    /// <seealso cref="System.Windows.Shapes.Shape" />
    /// <seealso cref="SpatialAnalysis.Agents.IVisualAgent" />
    public class VisualAgentMandatoryScenario : Shape, IVisualAgent
    {
        private static readonly object _lockerObject = new object();
        /// <summary>
        /// The fixed time-step
        /// </summary>
        public const double FixedTimeStep = 0.02d;
        /// <summary>
        /// Gets or sets the UI message.
        /// </summary>
        /// <value>The UI message.</value>
        public static TextBlock UIMessage { get; set; }
        /// <summary>
        /// The color code applied when the barrier repulsion is applied
        /// </summary>
        public static readonly Brush InsideRange = Brushes.DarkRed;
        /// <summary>
        /// The color code applied when the barrier repulsion is not applied
        /// </summary>
        public static readonly Brush OutsideRange = Brushes.DarkGreen;
        private const double _timeCycle = 200.0d;
        private Random _random { get; set; }
        /// <summary>
        /// When raises shows the line of sight in the cone of vision to the visibility targets if this line of sight exists.
        /// </summary>
        public AgentUpdated RaiseVisualEvent;
        /// <summary>
        /// Visualizes the closest barrier
        /// </summary>
        public AgentUpdated ShowBarrier;
        /// <summary>
        /// The show repulsion trajectory
        /// </summary>
        public AgentUpdated ShowRepulsionTrajectory;
        private CellularFloor _cellularFloor { get; set; }
        private Matrix _matrix;
        private static Vector3D Z = new Vector3D(0, 0, 1);
        /// <summary>
        /// The debuger window.
        /// </summary>
        public static StringBuilder Debuger = new StringBuilder();
        private MatrixTransform _transformGeometry { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="VisualAgentMandatoryScenario"/> class.
        /// </summary>
        public VisualAgentMandatoryScenario()
        {
            this.shapeGeom = new StreamGeometry();
            this._transformGeometry = new MatrixTransform(this._matrix);
            this.SetGeometry();
            this._random = new Random(DateTime.Now.Millisecond);
            this.Fill = VisualAgentMandatoryScenario.OutsideRange;
        }
        protected override System.Windows.Media.Geometry DefiningGeometry
        {
            get { return this.shapeGeom; }
        }

        #region VisualTriggerDetected event definition
        // Create a custom routed event by first registering a RoutedEventID
        // This event uses the bubbling routing strategy
        /// <summary>
        /// The visual trigger detected event
        /// </summary>
        public static readonly RoutedEvent VisualTriggerDetectedEvent = EventManager.RegisterRoutedEvent(
            "VisualTriggerDetected", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(VisualAgentMandatoryScenario));
        /// <summary>
        /// Runs when a visual trigger is detected
        /// </summary>
        public event RoutedEventHandler VisualTriggerDetected
        {
            add { AddHandler(VisualTriggerDetectedEvent, value); }
            remove { RemoveHandler(VisualTriggerDetectedEvent, value); }
        }

        // This method raises the TrailParametersUpdated event
        void raiseTrailParametersUpdatedEvent(Sequence sequence, UV target, double delay)
        {
            //RoutedEventArgs newEventArgs = new RoutedEventArgs(VisualAgentMandatoryScenario.VisualTriggerDetectedEvent);
            VisualTriggerArgs newEventArgs = new VisualTriggerArgs(VisualAgentMandatoryScenario.VisualTriggerDetectedEvent, sequence, target, delay);
            RaiseEvent(newEventArgs);
        }
        #endregion

        #region DetectedVisualTriggers Definition
        private static DependencyProperty DetectedVisualTriggersProperty =
            DependencyProperty.Register("DetectedVisualTriggers", typeof(Dictionary<double, Sequence>), typeof(VisualAgentMandatoryScenario),
            new FrameworkPropertyMetadata(new Dictionary<double, Sequence>(), VisualAgentMandatoryScenario.DetectedVisualTriggersPropertyChanged, VisualAgentMandatoryScenario.PropertyCoerce));

        /// <summary>
        /// Gets or sets the detected visual triggers.
        /// </summary>
        /// <value>The detected visual triggers.</value>
        public Dictionary<double,Sequence> DetectedVisualTriggers
        {
            get { return (Dictionary<double, Sequence>)GetValue(DetectedVisualTriggersProperty); }
            set { SetValue(DetectedVisualTriggersProperty, value); }
        }
        private static void DetectedVisualTriggersPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentMandatoryScenario instance = (VisualAgentMandatoryScenario)obj;
            instance.DetectedVisualTriggers = (Dictionary<double, Sequence>)args.NewValue;
        }
        #endregion

        #region AllActivities Definition
        private static DependencyProperty AllActivitiesProperty =
            DependencyProperty.Register("AllActivities", typeof(Dictionary<string, Activity>), typeof(VisualAgentMandatoryScenario),
            new FrameworkPropertyMetadata(null, VisualAgentMandatoryScenario.AllActivitiesPropertyChanged, VisualAgentMandatoryScenario.PropertyCoerce));

        /// <summary>
        /// Gets or sets all activities.
        /// </summary>
        /// <value>All activities.</value>
        public Dictionary<string, Activity> AllActivities
        {
            get { return (Dictionary<string, Activity>)GetValue(AllActivitiesProperty); }
            set { SetValue(AllActivitiesProperty, value); }
        }
        private static void AllActivitiesPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentMandatoryScenario instance = (VisualAgentMandatoryScenario)obj;
            instance.AllActivities = (Dictionary<string, Activity>)args.NewValue;
        }
        #endregion

        #region CurrentActivity Definition
        private static DependencyProperty CurrentActivityProperty =
            DependencyProperty.Register("CurrentActivity", typeof(Activity), typeof(VisualAgentMandatoryScenario),
            new FrameworkPropertyMetadata(null, VisualAgentMandatoryScenario.CurrentActivityPropertyChanged, VisualAgentMandatoryScenario.PropertyCoerce));

        /// <summary>
        /// Gets or sets the current activity.
        /// </summary>
        /// <value>The current activity.</value>
        public Activity CurrentActivity
        {
            get { return (Activity)GetValue(CurrentActivityProperty); }
            set { SetValue(CurrentActivityProperty, value); }
        }
        private static void CurrentActivityPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentMandatoryScenario instance = (VisualAgentMandatoryScenario)obj;
            instance.CurrentActivity = (Activity)args.NewValue;
        }
        #endregion

        #region CurrentActivityFieldIndex Definition
        private static DependencyProperty CurrentActivityFieldIndexProperty =
            DependencyProperty.Register("CurrentActivityFieldIndex", typeof(int), typeof(VisualAgentMandatoryScenario),
            new FrameworkPropertyMetadata(0, VisualAgentMandatoryScenario.CurrentActivityFieldIndexPropertyChanged, VisualAgentMandatoryScenario.PropertyCoerce));
        /// <summary>
        /// Gets or sets the index of the current activity field.
        /// </summary>
        /// <value>The index of the current activity field.</value>
        public int CurrentActivityFieldIndex
        {
            get { return (int)GetValue(CurrentActivityFieldIndexProperty); }
            set { SetValue(CurrentActivityFieldIndexProperty, value); }
        }
        private static void CurrentActivityFieldIndexPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentMandatoryScenario instance = (VisualAgentMandatoryScenario)obj;
            instance.CurrentActivityFieldIndex = (int)args.NewValue;
        }
        #endregion

        #region CurrentSequence Definition
        private static DependencyProperty CurrentSequenceProperty =
            DependencyProperty.Register("CurrentSequence", typeof(Sequence), typeof(VisualAgentMandatoryScenario),
            new FrameworkPropertyMetadata(null, VisualAgentMandatoryScenario.CurrentSequencePropertyChanged, VisualAgentMandatoryScenario.PropertyCoerce));
        /// <summary>
        /// Gets or sets the current sequence.
        /// </summary>
        /// <value>The current sequence.</value>
        public Sequence CurrentSequence
        {
            get { return (Sequence)GetValue(CurrentSequenceProperty); }
            set { SetValue(CurrentSequenceProperty, value); }
        }
        private static void CurrentSequencePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentMandatoryScenario instance = (VisualAgentMandatoryScenario)obj;
            instance.CurrentSequence = (Sequence)args.NewValue;
        }
        #endregion

        #region OccupancyScenario Definition
        private static DependencyProperty OccupancyScenarioProperty =
            DependencyProperty.Register("OccupancyScenario", typeof(Scenario), typeof(VisualAgentMandatoryScenario),
            new FrameworkPropertyMetadata(null, VisualAgentMandatoryScenario.OccupancyScenarioPropertyChanged, VisualAgentMandatoryScenario.PropertyCoerce));
        /// <summary>
        /// Gets or sets the occupancy scenario.
        /// </summary>
        /// <value>The occupancy scenario.</value>
        public Scenario OccupancyScenario
        {
            get { return (Scenario)GetValue(OccupancyScenarioProperty); }
            set { SetValue(OccupancyScenarioProperty, value); }
        }
        private static void OccupancyScenarioPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentMandatoryScenario instance = (VisualAgentMandatoryScenario)obj;
            instance.OccupancyScenario = (Scenario)args.NewValue;
        }
        #endregion

        #region ActivityEngagementTime Definition
        public static DependencyProperty ActivityEngagementTimeProperty =
            DependencyProperty.Register("ActivityEngagementTime", typeof(double), typeof(VisualAgentMandatoryScenario),
            new FrameworkPropertyMetadata(0.0d, VisualAgentMandatoryScenario.ActivityEngagementTimePropertyChanged, VisualAgentMandatoryScenario.PropertyCoerce));
        /// <summary>
        /// Gets or sets the activity engagement time.
        /// </summary>
        /// <value>The activity engagement time.</value>
        public double ActivityEngagementTime
        {
            get { return (double)GetValue(ActivityEngagementTimeProperty); }
            set { SetValue(ActivityEngagementTimeProperty, value); }
        }
        private static void ActivityEngagementTimePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentMandatoryScenario agent = (VisualAgentMandatoryScenario)obj;
            if ((double)args.OldValue == (double)args.NewValue)
            {
                agent.ActivityEngagementTime = (double)args.NewValue;
            }
        }
        #endregion

        #region AgentAccelerationMode Definition
        public static DependencyProperty AgentPhysicalMovementModeProperty =
            DependencyProperty.Register("AgentPhysicalMovementMode", typeof(PhysicalMovementMode), typeof(VisualAgentMandatoryScenario),
            new FrameworkPropertyMetadata(PhysicalMovementMode.StopAndOrient, VisualAgentMandatoryScenario.AgentPhysicalMovementModePropertyChanged, VisualAgentMandatoryScenario.PropertyCoerce));
        /// <summary>
        /// Gets or sets the agent physical movement mode.
        /// </summary>
        /// <value>The agent physical movement mode.</value>
        public PhysicalMovementMode AgentPhysicalMovementMode
        {
            get { return (PhysicalMovementMode)GetValue(AgentPhysicalMovementModeProperty); }
            set { SetValue(AgentPhysicalMovementModeProperty, value); }
        }
        private static void AgentPhysicalMovementModePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentMandatoryScenario agent = (VisualAgentMandatoryScenario)obj;
            if ((MandatoryScenarioStatus)args.OldValue == (MandatoryScenarioStatus)args.NewValue)
            {
                agent.AgentPhysicalMovementMode = (PhysicalMovementMode)args.NewValue;
            }
        }
        #endregion

        #region EngagementStatus Definition
        public static DependencyProperty AgentEngagementStatusProperty =
            DependencyProperty.Register("AgentEngagementStatus", typeof(MandatoryScenarioStatus), typeof(VisualAgentMandatoryScenario),
            new FrameworkPropertyMetadata(MandatoryScenarioStatus.Free, VisualAgentMandatoryScenario.AgentEngagementStatusPropertyChanged, VisualAgentMandatoryScenario.PropertyCoerce));
        /// <summary>
        /// Gets or sets the agent engagement status.
        /// </summary>
        /// <value>The agent engagement status.</value>
        public MandatoryScenarioStatus AgentEngagementStatus
        {
            get { return (MandatoryScenarioStatus)GetValue(AgentEngagementStatusProperty); }
            set { SetValue(AgentEngagementStatusProperty, value); }
        }
        private static void AgentEngagementStatusPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentMandatoryScenario agent = (VisualAgentMandatoryScenario)obj;
            if ((MandatoryScenarioStatus)args.OldValue == (MandatoryScenarioStatus)args.NewValue)
            {
                agent.AgentEngagementStatus = (MandatoryScenarioStatus)args.NewValue;
            }
        }
        #endregion

        #region TimeStoryboard Definition
        private static DependencyProperty TimeStoryboardProperty =
            DependencyProperty.Register("TimeStoryboard", typeof(Storyboard), typeof(VisualAgentMandatoryScenario),
            new FrameworkPropertyMetadata(null, VisualAgentMandatoryScenario.TimeStoryboardPropertyPropertyChanged, VisualAgentMandatoryScenario.PropertyCoerce));
        /// <summary>
        /// Gets or sets the time storyboard.
        /// </summary>
        /// <value>The time storyboard.</value>
        public Storyboard TimeStoryboard
        {
            get { return (Storyboard)GetValue(TimeStoryboardProperty); }
            set { SetValue(TimeStoryboardProperty, value); }
        }
        private static void TimeStoryboardPropertyPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentMandatoryScenario agent = (VisualAgentMandatoryScenario)obj;
            agent.TimeStoryboard = args.NewValue as Storyboard;
        }
        #endregion

        #region shapeGeom Definition
        /// <summary>
        /// The shape geom property
        /// </summary>
        private static DependencyProperty shapeGeomProperty =
            DependencyProperty.Register("shapeGeom", typeof(StreamGeometry), typeof(VisualAgentMandatoryScenario),
            new FrameworkPropertyMetadata(null, VisualAgentMandatoryScenario.shapeGeomPropertyChanged, VisualAgentMandatoryScenario.PropertyCoerce));
        private StreamGeometry shapeGeom
        {
            get { return (StreamGeometry)GetValue(shapeGeomProperty); }
            set { SetValue(shapeGeomProperty, value); }
        }
        private static void shapeGeomPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentMandatoryScenario agent = (VisualAgentMandatoryScenario)obj;
            agent.shapeGeom = (StreamGeometry)args.NewValue;
        }
        #endregion

        #region BodyElasticity  Definition
        /// <summary>
        /// The body elasticity property
        /// </summary>
        public static DependencyProperty BodyElasticityProperty =
            DependencyProperty.Register("BodyElasticity", typeof(double), typeof(VisualAgentMandatoryScenario),
            new FrameworkPropertyMetadata(0.1d, VisualAgentMandatoryScenario.BodyElasticityPropertyChanged, VisualAgentMandatoryScenario.PropertyCoerce));
        /// <summary>
        /// Gets or sets the body elasticity.
        /// </summary>
        /// <value>The body elasticity.</value>
        public double BodyElasticity
        {
            get { return (double)GetValue(BodyElasticityProperty); }
            set { SetValue(BodyElasticityProperty, value); }
        }
        private static void BodyElasticityPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentMandatoryScenario agent = (VisualAgentMandatoryScenario)obj;
            if ((double)args.OldValue == (double)args.NewValue)
            {
                agent.BodyElasticity = (double)args.NewValue;
            }
            if (agent.BodyElasticity > 1.0d)
            {
                agent.BodyElasticity = 1.0d;
            }
            if (agent.BodyElasticity < 0.0d)
            {
                agent.BodyElasticity = 0.0d;
            }
        }
        #endregion

        #region BarrierFriction  Definition
        /// <summary>
        /// The barrier friction property
        /// </summary>
        public static DependencyProperty BarrierFrictionProperty =
            DependencyProperty.Register("BarrierFriction", typeof(double), typeof(VisualAgentMandatoryScenario),
            new FrameworkPropertyMetadata(0.1d, VisualAgentMandatoryScenario.BarrierFrictionPropertyChanged, VisualAgentMandatoryScenario.PropertyCoerce));
        /// <summary>
        /// Gets or sets the barrier friction.
        /// </summary>
        /// <value>The barrier friction.</value>
        public double BarrierFriction
        {
            get { return (double)GetValue(BarrierFrictionProperty); }
            set { SetValue(BarrierFrictionProperty, value); }
        }
        private static void BarrierFrictionPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentMandatoryScenario agent = (VisualAgentMandatoryScenario)obj;
            if ((double)args.OldValue == (double)args.NewValue)
            {
                agent.BarrierFriction = (double)args.NewValue;
            }
            if (agent.BarrierFriction > 1.0d)
            {
                agent.BarrierFriction = 1.0d;
            }
            if (agent.BarrierFriction < 0.0d)
            {
                agent.BarrierFriction = 0.0d;
            }
        }
        #endregion

        #region TimeStep Definition
        /// <summary>
        /// The time step property
        /// </summary>
        public static DependencyProperty TimeStepProperty =
            DependencyProperty.Register("TimeStep", typeof(double), typeof(VisualAgentMandatoryScenario),
            new FrameworkPropertyMetadata(0.0d, VisualAgentMandatoryScenario.TimeStepPropertyChanged, VisualAgentMandatoryScenario.PropertyCoerce));
        /// <summary>
        /// Gets or sets the time-step.
        /// </summary>
        /// <value>The time step.</value>
        public double TimeStep
        {
            get { return (double)GetValue(TimeStepProperty); }
            set { SetValue(TimeStepProperty, value); }
        }
        private static void TimeStepPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentMandatoryScenario agent = (VisualAgentMandatoryScenario)obj;
            if ((double)args.OldValue == (double)args.NewValue)
            {
                agent.TimeStep = (double)args.NewValue;
            }
        }
        #endregion

        #region BarrierRepulsionRange Definition
        /// <summary>
        /// The barrier repulsion range property
        /// </summary>
        public static DependencyProperty BarrierRepulsionRangeProperty =
            DependencyProperty.Register("BarrierRepulsionRange", typeof(double), typeof(VisualAgentMandatoryScenario),
            new FrameworkPropertyMetadata(5.0d, VisualAgentMandatoryScenario.BarrierRepulsionRangePropertyChanged, VisualAgentMandatoryScenario.PropertyCoerce));
        /// <summary>
        /// Gets or sets the barrier repulsion range.
        /// </summary>
        /// <value>The barrier repulsion range.</value>
        public double BarrierRepulsionRange
        {
            get { return (double)GetValue(BarrierRepulsionRangeProperty); }
            set { SetValue(BarrierRepulsionRangeProperty, value); }
        }
        private static void BarrierRepulsionRangePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentMandatoryScenario agent = (VisualAgentMandatoryScenario)obj;
            if ((double)args.OldValue == (double)args.NewValue)
            {
                agent.BarrierRepulsionRange = (double)args.NewValue;
            }
        }
        #endregion

        #region BarrierRepulsionChangeRate Definition
        /// <summary>
        /// The repulsion change rate property
        /// </summary>
        public static DependencyProperty RepulsionChangeRateProperty =
            DependencyProperty.Register("RepulsionChangeRate", typeof(double), typeof(VisualAgentMandatoryScenario),
            new FrameworkPropertyMetadata(5.0d, VisualAgentMandatoryScenario.RepulsionChangeRatePropertyChanged, VisualAgentMandatoryScenario.PropertyCoerce));

        /// <summary>
        /// Gets or sets the repulsion change rate. Larger numbers make the barrier to have a more sudden impact and smaller number make the impact smoother and slower
        /// </summary>
        /// <value>The repulsion change rate.</value>
        public double RepulsionChangeRate
        {
            get { return (double)GetValue(RepulsionChangeRateProperty); }
            set { SetValue(RepulsionChangeRateProperty, value); }
        }
        private static void RepulsionChangeRatePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentMandatoryScenario agent = (VisualAgentMandatoryScenario)obj;
            if ((double)args.OldValue == (double)args.NewValue)
            {
                agent.RepulsionChangeRate = (double)args.NewValue;
            }
        }
        #endregion

        #region VisibilityCosineFactor definition
        /// <summary>
        /// The visibility cosine factor property
        /// </summary>
        public static DependencyProperty VisibilityCosineFactorProperty =
            DependencyProperty.Register("VisibilityCosineFactor", typeof(double), typeof(VisualAgentMandatoryScenario),
            new FrameworkPropertyMetadata(0.0d, VisualAgentMandatoryScenario.VisibilityCosineFactorPropertyChanged, VisualAgentMandatoryScenario.PropertyCoerce));
        private static void VisibilityCosineFactorPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentMandatoryScenario agent = (VisualAgentMandatoryScenario)obj;
            agent.VisibilityCosineFactor = (double)args.NewValue;
        }
        /// <summary>
        /// Gets or sets the visibility cosine factor which is the cosine of half of the visibility angle.
        /// </summary>
        /// <value>The visibility cosine factor.</value>
        public double VisibilityCosineFactor
        {
            get { return (double)GetValue(VisibilityCosineFactorProperty); }
            set { SetValue(VisibilityCosineFactorProperty, value); }
        }
        #endregion

        #region AngularVelocity definition
        /// <summary>
        /// The angular velocity property
        /// </summary>
        public static DependencyProperty AngularVelocityProperty = DependencyProperty.Register("AngularVelocity",
            typeof(double), typeof(VisualAgentMandatoryScenario),
            new FrameworkPropertyMetadata(3.14d, VisualAgentMandatoryScenario.AngularVelocityPropertyChanged,
                VisualAgentMandatoryScenario.AngularVelocityPropertySet));
        /// <summary>
        /// Gets or sets the angular velocity.
        /// </summary>
        /// <value>The angular velocity.</value>
        public double AngularVelocity
        {
            get { return (double)GetValue(AngularVelocityProperty); }
            set { SetValue(AngularVelocityProperty, value); }
        }
        private static void AngularVelocityPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentMandatoryScenario agent = (VisualAgentMandatoryScenario)obj;
            double _new = (double)args.NewValue;
            double _old = (double)args.OldValue;
            if (_old != _new)
            {
                agent.AngularVelocity = _new;
            }
        }
        private static object AngularVelocityPropertySet(DependencyObject obj, object value)
        {
            return value;
        }
        #endregion

        #region BodySize definition
        /// <summary>
        /// The body size property
        /// </summary>
        public static DependencyProperty BodySizeProperty =
            DependencyProperty.Register("BodySize", typeof(double), typeof(VisualAgentMandatoryScenario),
            new FrameworkPropertyMetadata(1.0d, FrameworkPropertyMetadataOptions.AffectsRender,
                VisualAgentMandatoryScenario.BodySizePropertyChanged, VisualAgentMandatoryScenario.ScalePropertySet, true));
        /// <summary>
        /// Gets or sets the size of the body.
        /// </summary>
        /// <value>The size of the body.</value>
        public double BodySize
        {
            get { return (double)GetValue(BodySizeProperty); }
            set { SetValue(BodySizeProperty, value); }
        }
        private static void BodySizePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentMandatoryScenario agent = (VisualAgentMandatoryScenario)obj;
            double _new = (double)args.NewValue;
            double _old = (double)args.OldValue;
            if (_old != _new)
            {
                agent.BodySize = _new;
                agent.SetGeometry();
            }
        }
        private static object ScalePropertySet(DependencyObject obj, object value)
        {
            return value;
        }
        #endregion

        #region VelocityMagnitude definition
        /// <summary>
        /// The velocity magnitude property
        /// </summary>
        public static DependencyProperty VelocityMagnitudeProperty = DependencyProperty.Register("VelocityMagnitude",
            typeof(double), typeof(VisualAgentMandatoryScenario),
            new FrameworkPropertyMetadata(2.50d, VisualAgentMandatoryScenario.VelocityMagnitudePropertyChanged,
            VisualAgentMandatoryScenario.VelocityMagnitudePropertySet));
        /// <summary>
        /// Gets or sets the velocity magnitude.
        /// </summary>
        /// <value>The velocity magnitude.</value>
        public double VelocityMagnitude
        {
            get { return (double)GetValue(VelocityMagnitudeProperty); }
            set { SetValue(VelocityMagnitudeProperty, value); }
        }
        private static void VelocityMagnitudePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentMandatoryScenario agent = (VisualAgentMandatoryScenario)obj;
            double _new = (double)args.NewValue;
            double _old = (double)args.OldValue;
            if (_old != _new)
            {
                agent.VelocityMagnitude = _new;
            }
        }
        private static object VelocityMagnitudePropertySet(DependencyObject obj, object value)
        {
            return value;
        }
        #endregion

        #region AccelerationMagnitude definition
        /// <summary>
        /// The acceleration magnitude property
        /// </summary>
        public static DependencyProperty AccelerationMagnitudeProperty = DependencyProperty.Register("AccelerationMagnitude",
            typeof(double), typeof(VisualAgentMandatoryScenario),
            new FrameworkPropertyMetadata(10.0d, VisualAgentMandatoryScenario.AccelerationMagnitudePropertyChanged,
            VisualAgentMandatoryScenario.PropertyCoerce));
        /// <summary>
        /// Gets or sets the acceleration magnitude.
        /// </summary>
        /// <value>The acceleration magnitude.</value>
        public double AccelerationMagnitude
        {
            get { return (double)GetValue(AccelerationMagnitudeProperty); }
            set { SetValue(AccelerationMagnitudeProperty, value); }
        }
        private static void AccelerationMagnitudePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentMandatoryScenario agent = (VisualAgentMandatoryScenario)obj;
            double _new = (double)args.NewValue;
            if (agent.AccelerationMagnitude != _new)
            {
                agent.AccelerationMagnitude = _new;
            }
        }
        #endregion

        #region CurrentState definition
        /// <summary>
        /// The current state property
        /// </summary>
        public static DependencyProperty CurrentStateProperty =
            DependencyProperty.Register("CurrentState", typeof(StateBase), typeof(VisualAgentMandatoryScenario),
            new FrameworkPropertyMetadata(new StateBase(UV.ZeroBase, UV.VBase), VisualAgentMandatoryScenario.CurrentStatePropertyChanged,
            VisualAgentMandatoryScenario.PropertyCoerce));
        /// <summary>
        /// Gets or sets the current state of the agent.
        /// </summary>
        /// <value>The state of the current.</value>
        public StateBase CurrentState
        {
            get { return (StateBase)GetValue(CurrentStateProperty); }
            set { SetValue(CurrentStateProperty, value); }
        }
        private static void CurrentStatePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentMandatoryScenario agent = (VisualAgentMandatoryScenario)obj;
            if (!((StateBase)args.NewValue).Equals(((StateBase)args.OldValue)))
            {
                agent.CurrentState = (StateBase)args.NewValue;
            }
        }
        #endregion

        #region TotalWalkTime definition
        /// <summary>
        /// The total walk time property
        /// </summary>
        public static DependencyProperty TotalWalkTimeProperty =
            DependencyProperty.Register("TotalWalkTime", typeof(double), typeof(VisualAgentMandatoryScenario),
            new FrameworkPropertyMetadata(0.00d,
                VisualAgentMandatoryScenario.TotalWalkTimePropertyChanged, VisualAgentMandatoryScenario.PropertyCoerce));
        /// <summary>
        /// Gets or sets the total walk time.
        /// </summary>
        /// <value>The total walk time.</value>
        public double TotalWalkTime
        {
            get
            {
                return (double)GetValue(TotalWalkTimeProperty);
            }
            set
            {
                SetValue(TotalWalkTimeProperty, value);
            }
        }
        private static void TotalWalkTimePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentMandatoryScenario agent = (VisualAgentMandatoryScenario)obj;
            if ((double)args.NewValue != (double)args.OldValue)
            {
                agent.TotalWalkTime = (double)args.NewValue;
            }
        }
        #endregion

        #region TotalWalkedLength definition
        /// <summary>
        /// The total walked length property
        /// </summary>
        public static DependencyProperty TotalWalkedLengthProperty =
            DependencyProperty.Register("TotalWalkedLength", typeof(double), typeof(VisualAgentMandatoryScenario),
            new FrameworkPropertyMetadata(0.00d,
                VisualAgentMandatoryScenario.TotalWalkedLengthPropertyChanged, VisualAgentMandatoryScenario.PropertyCoerce));

        /// <summary>
        /// Gets or sets the walked total length.
        /// </summary>
        /// <value>The total length of the walked.</value>
        public double TotalWalkedLength
        {
            get { return (double)GetValue(TotalWalkedLengthProperty); }
            set { SetValue(TotalWalkedLengthProperty, value); }
        }
        private static void TotalWalkedLengthPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentMandatoryScenario agent = (VisualAgentMandatoryScenario)obj;
            if ((double)args.NewValue != (double)args.OldValue)
            {
                agent.TotalWalkedLength = (double)args.NewValue;
            }
        }
        #endregion

        #region ShowVisibilityCone definition
        /// <summary>
        /// The show visibility cone property
        /// </summary>
        public static DependencyProperty ShowVisibilityConeProperty = DependencyProperty.Register("ShowVisibilityCone",
            typeof(bool), typeof(VisualAgentMandatoryScenario), new FrameworkPropertyMetadata(true,
            FrameworkPropertyMetadataOptions.AffectsRender,
            VisualAgentMandatoryScenario.ShowVisibilityConePropertyChanged, VisualAgentMandatoryScenario.PropertyCoerce, true));
        /// <summary>
        /// Gets or sets a value indicating whether [show visibility cone].
        /// </summary>
        /// <value><c>true</c> if [show visibility cone]; otherwise, <c>false</c>.</value>
        public bool ShowVisibilityCone
        {
            get { return (bool)GetValue(ShowVisibilityConeProperty); }
            set { SetValue(ShowVisibilityConeProperty, value); }
        }
        private static void ShowVisibilityConePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentMandatoryScenario agent = (VisualAgentMandatoryScenario)obj;
            if ((bool)args.NewValue != (bool)args.OldValue)
            {
                agent.ShowVisibilityCone = (bool)args.NewValue;
                agent.SetGeometry();
            }
        }
        #endregion

        #region ShowSafetyBuffer definition
        /// <summary>
        /// The show safety buffer property
        /// </summary>
        public static DependencyProperty ShowSafetyBufferProperty = DependencyProperty.Register("ShowSafetyBuffer",
            typeof(bool), typeof(VisualAgentMandatoryScenario), new FrameworkPropertyMetadata(true,
            FrameworkPropertyMetadataOptions.AffectsRender,
            VisualAgentMandatoryScenario.ShowSafetyBufferPropertyChanged, VisualAgentMandatoryScenario.PropertyCoerce, true));
        /// <summary>
        /// Gets or sets a value indicating whether [show safety buffer].
        /// </summary>
        /// <value><c>true</c> if [show safety buffer]; otherwise, <c>false</c>.</value>
        public bool ShowSafetyBuffer
        {
            get { return (bool)GetValue(ShowSafetyBufferProperty); }
            set { SetValue(ShowSafetyBufferProperty, value); }
        }
        private static void ShowSafetyBufferPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentMandatoryScenario agent = (VisualAgentMandatoryScenario)obj;
            if ((bool)args.NewValue != (bool)args.OldValue)
            {
                agent.ShowSafetyBuffer = (bool)args.NewValue;
                agent.SetGeometry();
            }
        }
        #endregion

        #region VisibilityAngle definition
        /// <summary>
        /// The visibility angle property
        /// </summary>
        public static DependencyProperty VisibilityAngleProperty = DependencyProperty.Register("VisibilityAngle",
            typeof(double), typeof(VisualAgentMandatoryScenario), new FrameworkPropertyMetadata(2 * Math.PI / 3,
            FrameworkPropertyMetadataOptions.AffectsRender,
            VisualAgentMandatoryScenario.VisibilityAnglePropertyChanged, VisualAgentMandatoryScenario.PropertyCoerce, true));
        /// <summary>
        /// Gets or sets the visibility angle.
        /// </summary>
        /// <value>The visibility angle.</value>
        public double VisibilityAngle
        {
            get { return (double)GetValue(VisibilityAngleProperty); }
            set { SetValue(VisibilityAngleProperty, value); }
        }
        private static void VisibilityAnglePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentMandatoryScenario agent = (VisualAgentMandatoryScenario)obj;
            if ((double)args.NewValue != (double)args.OldValue)
            {
                agent.VisibilityAngle = (double)args.NewValue;
                agent.VisibilityCosineFactor = Math.Cos(agent.VisibilityAngle / 2);
                agent.SetGeometry();
            }
        }
        #endregion

        #region AnimationTimer definition
        /// <summary>
        /// The animation timer property
        /// </summary>
        public static DependencyProperty AnimationTimerProperty = DependencyProperty.Register("AnimationTimer",
            typeof(double), typeof(VisualAgentMandatoryScenario), new FrameworkPropertyMetadata(0.00d,
                FrameworkPropertyMetadataOptions.AffectsRender,
                VisualAgentMandatoryScenario.AnimationTimerPropertyChanged, VisualAgentMandatoryScenario.AnimationTimerPropertySet, false));
        /// <summary>
        /// The animation timer should not be accessed and is updated by timer storyboard
        /// </summary>
        /// <value>The animation timer.</value>
        public double AnimationTimer
        {
            get { return (double)GetValue(AnimationTimerProperty); }
            set { SetValue(AnimationTimerProperty, value); }
        }
        private static void AnimationTimerPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentMandatoryScenario agent = (VisualAgentMandatoryScenario)obj;
            //if (agent.OccupancyScenario.HoldingTasks.Count == 0)
            //{
            //    agent.OccupancyScenario.LoadTasks(agent.AllActivities, 1.0d, agent._random, agent.TotalWalkTime);
            //}
            double now = (double)args.NewValue;
            agent.AnimationTimer = now;
            double before = (double)args.OldValue;
            double h = now - before;
            //h might be negative or when debuging it might be really large
            //hear i am controling the value of it in such cases
            if (h > VisualAgentMandatoryScenario.FixedTimeStep || h < 0)
            {
                h = VisualAgentMandatoryScenario.FixedTimeStep;
            }
            var previousState = agent.CurrentState.Copy();
            
            if (OSMDocument.IntegrationMethod == IntegrationMode.Euler)
            {
                agent.TimeStep = h;
                while (agent.TimeStep != 0)
                {
                    agent.TimeStepUpdate();
                }
            }
            else //if (MainDocument.IntegrationMethod == IntegrationMode.RK4)
            {
                agent.TimeStep = h;
                while (agent.TimeStep != 0)
                {
                    agent.TimeStepUpdate();
                }
                var K1 = agent.CurrentState.Copy();
                var mid1 = StateAverage.Average(previousState, 1, K1, 1);
                agent.SetState(mid1);
                agent.TimeStep = h/2;
                while (agent.TimeStep != 0)
                {
                    agent.TimeStepUpdate();
                }
                var K2 = agent.CurrentState.Copy();
                var mid2 = StateAverage.Average(K2, 1, mid1, 1);
                agent.SetState(mid2);
                agent.TimeStep = h / 2;
                while (agent.TimeStep != 0)
                {
                    agent.TimeStepUpdate();
                }
                var K3 = agent.CurrentState.Copy();
                var mid3 = StateAverage.Average(mid2, 1, K3, 1);
                agent.SetState(mid3);
                agent.TimeStep = h;
                while (agent.TimeStep != 0)
                {
                    agent.TimeStepUpdate();
                }
                var K4 = agent.CurrentState.Copy();
                var average_claculator = new StateAverage();
                average_claculator.AddState(K1, 1);
                average_claculator.AddState(K2, 2);
                average_claculator.AddState(K3, 2);
                average_claculator.AddState(K4, 1);
                var finalState = average_claculator.GetAverage();
                agent.SetState(finalState);
            }

            //update total walked time
            agent.TotalWalkTime += h;
            //physical step update
            double deltaX = agent.CurrentState.Location.DistanceTo(previousState.Location);
            if (deltaX != 0)
            {
                agent.TotalWalkedLength += deltaX;
            }
            else//agent needs to take the default orientation and the resting position. This section is for render only and does not influence the result of the simulation
            {
                double angleBetween = agent.CurrentState.Direction.AngleTo(previousState.Direction);
                if (angleBetween!=0)
                {
                    agent.TotalWalkedLength += Math.Abs(angleBetween) * agent.BodySize / 2;
                }
                else
                {
                    double walkedLength = agent.TotalWalkedLength / (2 * agent.BodySize);
                    int R = (int)Math.Floor(walkedLength);
                    double q = walkedLength - R;
                    int n = (int)Math.Floor(q * 4);
                    double t = q - n * 0.250;
                    double tolerance = 0.02d;
                    bool c1 = n == 0 && t > 0.25d - tolerance;
                    bool c2 = n == 1 && t < tolerance;
                    bool c3 = n == 2 && t > 0.25d - tolerance;
                    bool c4 = n == 3 && t < tolerance;
                    if (!(c1 || c2 || c3 || c4))
                    {
                        agent.TotalWalkedLength += agent.AngularVelocity * h;
                    }
                }
            }
            

            #region render stuff
            if (agent.RaiseVisualEvent != null)
            {
                agent.RaiseVisualEvent();
            }
            if (agent.ShowBarrier != null)
            {
                agent.ShowBarrier();
            }
            if (agent.ShowRepulsionTrajectory != null)
            {
                agent.ShowRepulsionTrajectory();
            }
            if (agent.OccupancyScenario.ExpectedTasks.Count !=0)
            {
                string currentSequence = (agent.CurrentSequence == null) ? "Not Set" : agent.CurrentSequence.Name;
                string currentActivity = (agent.CurrentActivity == null) ? "Not Set" : agent.CurrentActivity.Name;

                string report = string.Format("Engagement Status: '{0}',\tPhysical Movement Status: '{1}',\tCurrent Sequence: '{2}',\t Current Activity: '{3}',\tTime: {4}",
                    agent.AgentEngagementStatus.ToString(),
                    agent.AgentPhysicalMovementMode.ToString(),
                    currentSequence,
                    currentActivity,
                    ((int)agent.TotalWalkTime).ToString());
                UIMessage.Text = report;
            }

            agent.SetGeometry();
            #endregion
        }
        private static object AnimationTimerPropertySet(DependencyObject obj, object value)
        {
            return value;
        }

        #endregion

        #region EdgeCollisionState definition
        /// <summary>
        /// The edge collision state property
        /// </summary>
        public static DependencyProperty EdgeCollisionStateProperty =
            DependencyProperty.Register("EdgeCollisionState", typeof(CollisionAnalyzer), typeof(VisualAgentMandatoryScenario),
            new FrameworkPropertyMetadata(null, VisualAgentMandatoryScenario.EdgeCollisionStatePropertyChanged,
            VisualAgentMandatoryScenario.PropertyCoerce));
        /// <summary>
        /// Gets or sets the state of the edge collision.
        /// </summary>
        /// <value>The state of the edge collision.</value>
        public CollisionAnalyzer EdgeCollisionState
        {
            get { return (CollisionAnalyzer)GetValue(EdgeCollisionStateProperty); }
            set { SetValue(EdgeCollisionStateProperty, value); }
        }
        private static void EdgeCollisionStatePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentMandatoryScenario agent = (VisualAgentMandatoryScenario)obj;
            agent.EdgeCollisionState = (CollisionAnalyzer)args.NewValue;
        }
        #endregion

        private static object PropertyCoerce(DependencyObject obj, object value)
        {
            return value;
        }

        /// <summary>
        /// Determines whether the agent should stops and orient towards the focal point of the activity.
        /// </summary>
        /// <returns><c>true</c> if the agent is in the destination, <c>false</c> otherwise.</returns>
        public bool StopAndOrientCheck()
        {
            UV direction = this.CurrentActivity.DefaultState.Location - this.CurrentState.Location;
            if (direction.GetLengthSquared()==0.0d)
            {
                return true;
            }
            if (UV.GetLengthSquared(this.CurrentState.Location, this.CurrentActivity.DefaultState.Location) < this.BodySize||
                UV.GetLengthSquared(this.CurrentState.Location, this.CurrentActivity.DefaultState.Location) < this._cellularFloor.CellSize)
            {
                return true;
            }
            double directionLength = direction.GetLength();
            direction /= directionLength;
            var velocity_x = direction * (direction.DotProduct(this.CurrentState.Velocity));
            var velocity_y = this.CurrentState.Velocity - velocity_x;
            //both velocity_x and velocity_y should become zeros
            var velocity_x_length = velocity_x.GetLength();
            var tx = velocity_x_length / this.AccelerationMagnitude;
            velocity_x /= velocity_x_length;

            var velocity_y_length = velocity_y.GetLength();
            //when velocity_y is zero
            var ty = velocity_y_length / this.AccelerationMagnitude;
            velocity_y /= velocity_y_length;
            //where velocity_y is zero
            var location1 = this.CurrentState.Location + (0.5 * this.AccelerationMagnitude * ty * ty) * velocity_y + ty * velocity_x * velocity_x_length;

            //where velocity_x is zero
            var location2 = location1 + (0.5 * this.AccelerationMagnitude * tx * tx) * velocity_x;

            return directionLength * directionLength < UV.GetLengthSquared(this.CurrentState.Location, location2);
        }
        /// <summary>
        /// Sets the velocity vector when the agent is in the destination area and needs to stop and then orient.
        /// </summary>
        /// <returns>UV.</returns>
        public UV StopAndOrientVelocity()
        {
            //decomposing the velocity to its componets towards center (x) and perpendecular to it (y)
            UV direction = this.CurrentActivity.DefaultState.Location - this.CurrentState.Location;
            if (direction.GetLengthSquared()!=0.0d)
            {
                direction.Unitize();
            }
            else
            {
                if (this.CurrentState.Velocity.GetLengthSquared() > this.AccelerationMagnitude * this.TimeStep * this.AccelerationMagnitude * this.TimeStep)
                {
                    double velocityMagnetude = this.CurrentState.Velocity.GetLength();
                    var nextVelocity = this.CurrentState.Velocity - this.AccelerationMagnitude * this.TimeStep * (this.CurrentState.Velocity / velocityMagnetude);
                    return nextVelocity;
                }
                else
                {
                    return UV.ZeroBase.Copy();
                }
            }
            var velocity_xDir = direction * (direction.DotProduct(this.CurrentState.Velocity));
            var velocity_yDir = this.CurrentState.Velocity - velocity_xDir;
            //calculating the length of the velocity components and unitizing them
            var velocity_x_length = velocity_xDir.GetLength();
            if (velocity_x_length!=0.0d)
            {
                velocity_xDir /= velocity_x_length;
            }
            var velocity_y_length = velocity_yDir.GetLength();
            if (velocity_y_length != 0.0d)
            {
                velocity_yDir /= velocity_y_length;
            }
            //creating a copy of the velocity of the current state
            var velocity = this.CurrentState.Velocity.Copy();
            //checking if exerting the acceleration can stop the perpendecular component of the velocity 
            if (velocity_y_length >= this.AccelerationMagnitude*this.TimeStep)
            {
                //all of the the acceleration force in the timestep is assigned to stop the perpendecular component of the velocity 
                velocity -= this.TimeStep * this.AccelerationMagnitude * velocity_yDir;
                return velocity;
            }
            else
            {
                //finding part of the timestep when the excursion of the acceleration force stops the the perpendecular component of the velocity 
                //velocity_y_length = this.AccelerationMagnitude * timeStep_y;
                double timeStep_y = velocity_y_length / this.AccelerationMagnitude;
                //update the velocity and location after first portion of the timestep
                var velocity1 = velocity - timeStep_y * this.AccelerationMagnitude * velocity_yDir;
                UV location_y = this.CurrentState.Location + velocity1 * timeStep_y;
                //calculating the remainder of the timestep
                double timeStep_x = this.TimeStep - timeStep_y;
                //checking the direction of the velocity agenst x direction 
                var sign = Math.Sign(direction.DotProduct(velocity_xDir));
                if (sign ==1)//moving towards destination
                {
                    //the time needed to make the velocity zero with the same acceleration
                    double t = velocity_x_length / this.AccelerationMagnitude;
                    //traveled distance within this time
                    double traveledDistance = 0.5d * this.AccelerationMagnitude * t * t;
                    double distance = location_y.DistanceTo(this.CurrentActivity.DefaultState.Location);
                    //if traveled distance is larger than the current distance use acceleration to adjust velocity
                    if (traveledDistance >= distance)
                    {
                        velocity = velocity1 - timeStep_x * this.AccelerationMagnitude * velocity_xDir;
                    }
                    else//do not excert velocity to change the celocity
                    {
                        velocity = velocity1 + timeStep_x * this.AccelerationMagnitude * velocity_xDir;
                    }
                }
                else if (sign == -1)//exert the acceleration force to change the direction
                {
                    velocity = velocity1 - timeStep_x * this.AccelerationMagnitude * velocity_xDir;
                }
                else if (sign == 0) //which means velocity_x_length =0
                {
                    velocity = velocity1 + timeStep_x * this.AccelerationMagnitude * direction;
                }
            }
            return velocity;
        }


        private void updateAgentStatusInScenario()
        {
            switch (this.AgentEngagementStatus)
            {
                case MandatoryScenarioStatus.Free:
                    //something has came up and the agent should stop
                    if (this.TotalWalkTime > this.OccupancyScenario.ExpectedTasks.First().Key)
                    {
                        //find the next sequence
                        this.AgentEngagementStatus = MandatoryScenarioStatus.WalkingInSequence;
                        this.AgentPhysicalMovementMode = PhysicalMovementMode.Move;
                        this.loadNextSequence();
                    }
                    else
                    {
                        //check to see if the agent is close to the defualt location of station to set the Physical Movement Mode of the agent
                        this.AgentEngagementStatus = MandatoryScenarioStatus.Free;
                        if (this.StopAndOrientCheck())
                        {
                            //try to stop and orient 
                            this.AgentPhysicalMovementMode = PhysicalMovementMode.StopAndOrient;
                        }
                        else
                        {
                            //walk towards default state of the station
                            this.AgentPhysicalMovementMode = PhysicalMovementMode.Move;
                        }
                    }
                    break;
                case MandatoryScenarioStatus.Engaged:
                    // check to see if engagement is finished or not
                    if (this.ActivityEngagementTime > this.TotalWalkTime)//engagement is not finished
                    {
                        //engagement continues
                        this.AgentPhysicalMovementMode = PhysicalMovementMode.StopAndOrient;
                        this.AgentEngagementStatus = MandatoryScenarioStatus.Engaged;
                    }
                    else //engagement time is over and the agent needs to find the next action
                    {
                        this.AgentPhysicalMovementMode = PhysicalMovementMode.Move;
                        //checking the sequence is finished or not
                        if (this.CurrentActivityFieldIndex == this.CurrentSequence.ActivityCount - 1)//the sequence is finished
                        {
                            //reactivate the sequence and put it on the list
                            this.OccupancyScenario.ReActivate(this.CurrentSequence, this.TotalWalkTime);
                            //checking if a new sequence should be raised or the agent should go back to the main station
                            if (this.TotalWalkTime < this.OccupancyScenario.ExpectedTasks.First().Key)//should go back to the station
                            {
                                this.AgentEngagementStatus = MandatoryScenarioStatus.Free;
                                //find the next station
                                this.loadStation();
                            }
                            else//load a new sequence
                            {
                                this.AgentEngagementStatus = MandatoryScenarioStatus.WalkingInSequence;
                                this.loadNextSequence();
                            }
                        }
                        else//the sequence is not finished and the agent needs to choose the next activity in the current sequence
                        {
                            this.AgentEngagementStatus = MandatoryScenarioStatus.WalkingInSequence;
                            this.CurrentActivityFieldIndex++;
                            this.CurrentActivity = this.AllActivities[this.CurrentSequence.ActivityNames[this.CurrentActivityFieldIndex]];
                            this.loadActivityEngagementTime();
                        }
                    }
                    break;
                case MandatoryScenarioStatus.WalkingInSequence:
                    if (this.StopAndOrientCheck())
                    {
                        //try to stop and orient 
                        this.AgentPhysicalMovementMode = PhysicalMovementMode.StopAndOrient;
                        this.AgentEngagementStatus = MandatoryScenarioStatus.Engaged;
                        this.loadActivityEngagementTime();
                    }
                    else
                    {
                        this.AgentPhysicalMovementMode = PhysicalMovementMode.Move;
                        this.AgentEngagementStatus = MandatoryScenarioStatus.WalkingInSequence;
                    }
                    break;
            }
        }
        /// <summary>
        /// Loads the repulsion vector's magnitude.
        /// </summary>
        /// <returns>System.Double.</returns>
        public double LoadRepulsionMagnitude()
        {
            double value = 0;
            switch (this.AgentPhysicalMovementMode)
            {
                case PhysicalMovementMode.StopAndOrient:
                    value = 0;
                    break;
                case PhysicalMovementMode.Move:
                    value = Function.BezierRepulsionMethod(this.EdgeCollisionState.DistanceToBarrier);
                    break;
            }
            return value;
        }
        /// <summary>
        /// Loads the next sequence.
        /// </summary>
        public void loadNextSequence()
        {
            try
            {
                this.CurrentSequence = this.OccupancyScenario.ExpectedTasks.First().Value;
                double time = this.OccupancyScenario.ExpectedTasks.First().Key;
                this.OccupancyScenario.ExpectedTasks.Remove(time);
                this.CurrentActivityFieldIndex = 0;
                this.CurrentActivity = this.AllActivities[this.CurrentSequence.ActivityNames[this.CurrentActivityFieldIndex]];
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Report());
            }
        }
        /// <summary>
        /// Loads the station which is most conveniently accessible.
        /// </summary>
        public void loadStation()
        {
            Cell cell = this._cellularFloor.FindCell(this.CurrentState.Location);
            double min = double.PositiveInfinity;
            foreach (var item in this.OccupancyScenario.MainStations)
            {
                double value = this.AllActivities[item].Data[cell];
                if (value < min)
                {
                    min = value;
                    this.CurrentActivity = this.AllActivities[item];
                }
            }
        }

        /// <summary>
        /// Loads the activity engagement time.
        /// </summary>
        public void loadActivityEngagementTime()
        {
            this.ActivityEngagementTime = this._random.NextDouble() * (this.CurrentActivity.MaximumEngagementTime - this.CurrentActivity.MinimumEngagementTime) +
                this.CurrentActivity.MinimumEngagementTime + this.TotalWalkTime;
        }

        /// <summary>
        /// Updates the time-step.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">
        /// Collision Analyzer failed!
        /// or
        /// Collision Analyzer failed!
        /// </exception>
        /// <exception cref="System.ArgumentException">Collision not found</exception>
        public void TimeStepUpdate()
        {
            try
            {
                this.updateAgentStatusInScenario();
                // make a copy of current state
                var newState = this.CurrentState.Copy();
                var previousState = this.CurrentState.Copy();
                // direction update
                UV direction = this.CurrentState.Direction;
                try
                {
                    direction = this.CurrentActivity.Differentiate(this.CurrentState.Location);
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.Report());
                }
                direction.Unitize();
                #region update barrier relation factors
                UV repulsionForce = new UV(0.0, 0.0);
                // this condition is for the first time only when walking begins
                if (this.EdgeCollisionState == null)
                {
                    this.EdgeCollisionState = CollisionAnalyzer.GetCollidingEdge(newState.Location, this._cellularFloor, BarrierType.Field);
                }
                if (this.EdgeCollisionState != null)
                {
                    //only if the barrier is visible to the agent the repulstion force will be applied
                    if (this.EdgeCollisionState.NormalizedRepulsion.DotProduct(newState.Direction) >= 0.0d)//this.VisibilityCosineFactor)
                    {
                        //render agent color
                        this.Fill = VisualAgentMandatoryScenario.OutsideRange;
                        this.Stroke = VisualAgentMandatoryScenario.OutsideRange;
                    }
                    else
                    {
                        double repulsionMagnetude = this.LoadRepulsionMagnitude();
                        if (repulsionMagnetude != 0.0d)
                        {
                            //render agent color
                            this.Fill = VisualAgentMandatoryScenario.InsideRange;
                            this.Stroke = VisualAgentMandatoryScenario.InsideRange;
                            //calculate repulsion force
                            repulsionForce = this.EdgeCollisionState.NormalizedRepulsion * repulsionMagnetude;
                        }
                        else
                        {
                            //render agent color
                            this.Fill = VisualAgentMandatoryScenario.OutsideRange;
                            this.Stroke = VisualAgentMandatoryScenario.OutsideRange;
                        }
                    }
                }
                else
                {
                    throw new ArgumentNullException("Collision Analyzer failed!");
                }
                #endregion
                // find Acceleration force and update velocity
                UV acceleration = new UV(0, 0);
                switch (this.AgentPhysicalMovementMode)
                {
                    //try to stop agent
                    case PhysicalMovementMode.StopAndOrient:
                        if (newState.Velocity.GetLengthSquared()>0)
                        {
                            if (newState.Velocity.GetLengthSquared() < this.AccelerationMagnitude * this.TimeStep * this.AccelerationMagnitude * this.TimeStep &&
                                UV.GetLengthSquared(this.CurrentState.Location, this.CurrentActivity.DefaultState.Location) < this._cellularFloor.CellSize)
                            {
                                newState.Velocity *= 0;
                            }
                            else
                            {
                                newState.Velocity = this.StopAndOrientVelocity();
                            }
                        }
                        else
                        {
                            direction = this.CurrentActivity.DefaultState.Direction.Copy();
                        }
                        break;
                    //agent continues to move
                    case PhysicalMovementMode.Move:
                        acceleration += this.AccelerationMagnitude * direction + repulsionForce;
                        newState.Velocity += acceleration * this.TimeStep;
                        break;
                }             
                //check velocity magnitude against the cap
                if (newState.Velocity.GetLengthSquared() > this.VelocityMagnitude * this.VelocityMagnitude)
                {
                    newState.Velocity.Unitize();
                    newState.Velocity *= this.VelocityMagnitude;
                }
                //update location
                var location = newState.Location + this.TimeStep * newState.Velocity;
                newState.Location = location;
                //update the location of the agent in the transformation matrix
                this._matrix.OffsetX = newState.Location.U;
                this._matrix.OffsetY = newState.Location.V;
                // update direction
                double deltaAngle = this.AngularVelocity * this.TimeStep;
                Axis axis = new Axis(this.CurrentState.Direction, direction, deltaAngle);
                newState.Direction = axis.V_Axis;
                //update transformation Matrix
                this._matrix.M11 = axis.U_Axis.U; this._matrix.M12 = axis.U_Axis.V;
                this._matrix.M21 = axis.V_Axis.U; this._matrix.M22 = axis.V_Axis.V;
                //update state
                this.CurrentState = newState;
                //checking for collisions
                var newEdgeCollisionState = CollisionAnalyzer.GetCollidingEdge(newState.Location, this._cellularFloor, BarrierType.Field);
                if (newEdgeCollisionState == null)
                {
                    this.EdgeCollisionState = CollisionAnalyzer.GetCollidingEdge(newState.Location, this._cellularFloor, BarrierType.Field);
                    throw new ArgumentNullException("Collision Analyzer failed!");
                }
                else
                {
                    if (newEdgeCollisionState.DistanceToBarrier <= this.BodySize / 2)
                    {
                        //newEdgeCollisionState.CollisionOccured = true;
                        var collision = CollisionAnalyzer.GetCollision(this.EdgeCollisionState, newEdgeCollisionState, this.BodySize / 2, OSMDocument.AbsoluteTolerance);
                        if (collision == null)
                        {
                            throw new ArgumentException("Collision not found");
                        }
                        else
                        {
                            //find partial timestep and update the timestep
                            double newTimeStep = 0.0;
                            if (collision.TimeStepRemainderProportion <= 0)
                            {
                                newTimeStep = this.TimeStep;
                                this.TimeStep = 0.0;
                            }
                            else
                            {
                                if (collision.TimeStepRemainderProportion > 1.0)
                                {
                                    newTimeStep = 0;
                                    this.TimeStep = TimeStep;
                                }
                                else
                                {
                                    newTimeStep = this.TimeStep * (1.0 - collision.TimeStepRemainderProportion);
                                    this.TimeStep = this.TimeStep - newTimeStep;
                                }
                            }
                            // update velocity
                            var velocity = this.CurrentState.Velocity + newTimeStep * acceleration;
                            if (velocity.GetLengthSquared() > this.VelocityMagnitude * this.VelocityMagnitude)
                            {
                                velocity.Unitize();
                                velocity *= this.VelocityMagnitude;
                            }
                            //decompose velocity
                            UV normal = collision.GetCollisionNormal();
                            UV velocityVerticalComponent = normal * (normal.DotProduct(velocity));
                            double velocityVerticalComponentLength = velocityVerticalComponent.GetLength();
                            UV velocityHorizontalComponent = velocity - velocityVerticalComponent;
                            double velocityHorizontalComponentLength = velocityHorizontalComponent.GetLength();
                            //decompose acceleration
                            UV accelerationVerticalComponent = normal * (normal.DotProduct(acceleration));
                            // elasticity
                            UV ve = this.BodyElasticity * velocityVerticalComponent;
                            //friction
                            double f1 = this.BarrierFriction * velocityVerticalComponentLength;
                            double f2 = velocityHorizontalComponentLength;
                            UV vf = velocityHorizontalComponent - Math.Min(f1, f2) * velocityHorizontalComponent / velocityHorizontalComponentLength;
                            //update velocity
                            var adjustedVelocity = vf - ve - newTimeStep * accelerationVerticalComponent;
                            if (adjustedVelocity.GetLengthSquared() > this.VelocityMagnitude * this.VelocityMagnitude)
                            {
                                adjustedVelocity.Unitize();
                                adjustedVelocity *= this.VelocityMagnitude;
                            }
                            newState.Velocity = adjustedVelocity;
                            // update location
                            if (newTimeStep == 0)
                            {
                                newState.Location = CurrentState.Location;
                            }
                            else
                            {
                                newState.Location = collision.CollisionPoint;
                            }
                            newState.Location += OSMDocument.PenaltyCollisionReaction * normal;
                            this._matrix.OffsetX = newState.Location.U;
                            this._matrix.OffsetY = newState.Location.V;
                            //update direction
                            deltaAngle = this.AngularVelocity * newTimeStep;
                            axis = new Axis(this.CurrentState.Direction, direction, deltaAngle);
                            newState.Direction = axis.V_Axis;
                            //update transformation Matrix
                            this._matrix.M11 = axis.U_Axis.U; this._matrix.M12 = axis.U_Axis.V;
                            this._matrix.M21 = axis.V_Axis.U; this._matrix.M22 = axis.V_Axis.V;
                            //update state
                            newEdgeCollisionState.DistanceToBarrier = this.BodySize / 2;
                            newEdgeCollisionState.Location = newState.Location;
                            this.EdgeCollisionState = newEdgeCollisionState;
                            this.CurrentState = newState;
                        }
                    }
                    else
                    {
                        //update state for the full timestep length
                        this.EdgeCollisionState = newEdgeCollisionState;
                        this.CurrentState = newState;
                        this.TimeStep = 0.0d;
                    }
                }
                //check the sequences that need visual attention
                this.UpdateVisualTriggers();

            }
            catch (Exception error)
            {
                StopAnimation();
                MessageBox.Show(error.Report());
            }
        }

        /// <summary>
        /// Updates the visual triggers.
        /// </summary>
        public void UpdateVisualTriggers()
        {
            this.DetectedVisualTriggers.Clear();
            foreach (var a in this.OccupancyScenario.UnexpectedTasks)
            {
                UV target = a.Value.VisualAwarenessField.TargetVisibilityTest(this.CurrentState, this.VisibilityCosineFactor, this._cellularFloor);
                if (target != null)
                {
                    if (a.Key<this.TotalWalkTime)
                    {
                        this.DetectedVisualTriggers.Add(a.Key, a.Value);
                        double delay = this.TotalWalkTime - a.Key;
                        a.Value.TimeToGetVisuallyDetected += delay;
                        raiseTrailParametersUpdatedEvent(a.Value, target, delay);
                    }
                }
            }

            //Parallel.ForEach(this.OccupancyScenario.VisualTriggers, (a) => 
            //{
            //    Dispatcher.BeginInvoke(DispatcherPriority.Background,
            //        (SendOrPostCallback)delegate
            //        {
            //            if (a.Value.VisualAwarenessField.VisualEventRaised(this.CurrentState, this.VisibilityCosineFactor, this._cellularFloor))
            //            {
            //                lock (VisualAgentMandatoryScenario._luckerObject)
            //                {
            //                    this.DetectedVisualTriggers.Add(a.Key, a.Value);
            //                }
            //            }
            //        }
            //        , null);

            //});
            foreach (var item in this.DetectedVisualTriggers)
            {
                try
                {
                    //remove the sequence from visual triggers collection
                    this.OccupancyScenario.UnexpectedTasks.Remove(item.Key);
                    //add the sequence to the beginning of the routine tasks
                    //set the time of activation to the current time 
                    double timePriority = this.TotalWalkTime;
                    //if there are exepected sequences, there is a chance that some of t hem are past due, therefore the 
                    //visually trigered sequence should be given priority to the fist sequence on the queue
                    if (this.OccupancyScenario.ExpectedTasks.Count != 0)
                    {
                        timePriority = this.OccupancyScenario.ExpectedTasks.First().Key - 1.0d;
                    }
                    else
                    {
                        //MessageBox.Show("Error handled successfully!");
                    }
                    if (timePriority>this.TotalWalkTime)
                    {
                        timePriority = this.TotalWalkTime - 1;
                    }
                    this.OccupancyScenario.ExpectedTasks.Add(timePriority, item.Value);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Report());
                }
            }
             
        }

        /// <summary>
        /// Sets the initial state.
        /// </summary>
        /// <param name="initialState">The initial state.</param>
        public void SetInitialState(StateBase initialState)
        {
            this.SetState(initialState);
            this.SetGeometry();
        }
        /// <summary>
        /// Sets the state.
        /// </summary>
        /// <param name="state">The state.</param>
        public void SetState(StateBase state)
        {
            this.CurrentState = state.Copy();
            this.CurrentState.Direction.Unitize();
            this._matrix.OffsetX = this.CurrentState.Location.U;
            this._matrix.OffsetY = this.CurrentState.Location.V;
            Vector3D y3d = new Vector3D(this.CurrentState.Direction.U, this.CurrentState.Direction.V, 0);
            Vector3D x3d = Vector3D.CrossProduct(y3d, Z);
            _matrix.M11 = -y3d.Y; _matrix.M12 = -x3d.Y;
            _matrix.M21 = y3d.X; _matrix.M22 = x3d.X;
        }

        /// <summary>
        /// Initializes animation.
        /// </summary>
        /// <param name="cellularfloor">The cellularfloor.</param>
        /// <param name="escapeRouts">The escape routs.</param>
        public void WalkInit(CellularFloor cellularfloor, Dictionary<Cell, AgentEscapeRoutes> escapeRouts)
        {
            //this.WalkTime = 0;
            this._cellularFloor = cellularfloor;
            //this._escapeRouts = escapeRouts;
            this.TimeStoryboard = new Storyboard();
            DoubleAnimation timeAnimator = new DoubleAnimation();
            timeAnimator.From = this.AnimationTimer;
            timeAnimator.To = this.AnimationTimer + VisualAgentMandatoryScenario._timeCycle;
            timeAnimator.BeginTime = new TimeSpan(0);
            timeAnimator.Duration = TimeSpan.FromSeconds(VisualAgentMandatoryScenario._timeCycle);
            Storyboard.SetTarget(timeAnimator, this);
            Storyboard.SetTargetProperty(timeAnimator, new PropertyPath(VisualAgentMandatoryScenario.AnimationTimerProperty));
            TimeStoryboard.Children.Add(timeAnimator);
            TimeStoryboard.RepeatBehavior = RepeatBehavior.Forever;
            TimeStoryboard.Begin(this, true);
        }

        /// <summary>
        /// Stops the animation.
        /// </summary>
        public void StopAnimation()
        {
            if (this.TimeStoryboard != null)
            {
                this.TimeStoryboard.Stop(this);
                this.TimeStoryboard = null;
            }
        }

        /// <summary>
        /// Sets the geometry of the animated agent.
        /// </summary>
        public void SetGeometry()
        {
            var shapeData = MaleAgentShapeData.GetCurveInterpolatedDate(this.TotalWalkedLength,
                this.BodySize, this.ShowVisibilityCone, this.VisibilityAngle);
            this.shapeGeom.Clear();
            using (var sgc = this.shapeGeom.Open())
            {
                //head filled and closed
                sgc.BeginFigure(shapeData[0][0], true, true);
                sgc.PolyLineTo(shapeData[0], true, true);
                //body and guide lines
                for (int i = 1; i < shapeData.Length; i++)
                {
                    sgc.BeginFigure(shapeData[i][0], false, false);
                    sgc.PolyLineTo(shapeData[i], true, true);
                }
                //add the safety buffer
                if (this.ShowSafetyBuffer)
                {
                    sgc.BeginFigure(new Point(0, 0.5d * this.BodySize), false, true);
                    sgc.ArcTo(new Point(0.0d, -0.5d * this.BodySize), new Size(0.5d * this.BodySize, 0.5d * this.BodySize), 180.0d, false, SweepDirection.Clockwise, true, true);
                    sgc.ArcTo(new Point(0.0d, 0.5d * this.BodySize), new Size(0.5d * this.BodySize, 0.5d * this.BodySize), 180.0d, false, SweepDirection.Clockwise, true, true);
                }
            }

            this._transformGeometry.Matrix = this._matrix;
            this.shapeGeom.Transform = this._transformGeometry;
        }
    }
}

