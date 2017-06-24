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
using MathNet.Numerics.Distributions;
using SpatialAnalysis.Agents.Visualization.AgentModel;
using SpatialAnalysis.Data;
using SpatialAnalysis.Miscellaneous;

namespace SpatialAnalysis.Agents.OptionalScenario.Visualization
{

    /// <summary>
    /// Includes a logic for comparing two cells based on their direction and distance from an agent's state
    /// </summary>
    /// <seealso cref="System.Collections.Generic.IComparer{SpatialAnalysis.CellularEnvironment.Cell}" />
    class CellComparer : IComparer<Cell>
    {
        private StateBase _state;
        /// <summary>
        /// Initializes a new instance of the <see cref="CellComparer"/> class.
        /// </summary>
        /// <param name="currentState">Agent's state.</param>
        public CellComparer(StateBase currentState)
        {
            this._state = currentState;
        }

        /// <summary>
        /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
        /// </summary>
        /// <param name="x">The first Cell to compare.</param>
        /// <param name="y">The second Cell to compare.</param>
        /// <returns>A signed integer that indicates the relative values of <paramref name="x" /> and <paramref name="y" />, as shown in the following table.Value Meaning Less than zero<paramref name="x" /> is less than <paramref name="y" />.Zero<paramref name="x" /> equals <paramref name="y" />.Greater than zero<paramref name="x" /> is greater than <paramref name="y" />.</returns>
        public int Compare(Cell x, Cell y)
        {
            UV uvx = x - this._state.Location;
            double xl = uvx.GetLength();
            UV uvy = y - this._state.Location;
            double yl = uvy.GetLength();
            double xa = this._state.Direction.DotProduct(uvx)/xl;
            double ya = this._state.Direction.DotProduct(uvy)/yl;
            int result = xa.CompareTo(ya);
            if (result!=0)
            {
                return result;
            }
            result = xl.CompareTo(yl);
            if (result != 0)
            {
                return result;
            }
            return x.CompareTo(y);
        }
    }
    /// <summary>
    /// This function pointer is used to connect updates in the 3D animation with 2D scene of the floor
    /// </summary>
    public delegate void AgentUpdated ();
    /// <summary>
    /// This class includes the logic of optional occupancy simulation and animating the agent.
    /// </summary>
    /// <seealso cref="System.Windows.Shapes.Shape" />
    /// <seealso cref="SpatialAnalysis.Agents.IVisualAgent" />
    public class VisualAgentOptionalScenario : Shape, IVisualAgent
    {
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
        /// The color of agent when it's inside the barrier repulsion range. 
        /// </summary>
        public static readonly Brush InsideRange = Brushes.DarkRed;
        /// <summary>
        /// The color of agent when it's outside the barrier repulsion range. 
        /// </summary>
        public static readonly Brush OutsideRange = Brushes.DarkGreen;
        private const double _timeCycle = 200.0d;
        private Random _random { get; set; }
        /// <summary>
        /// When raises shows the line of sight in the cone of vision to the visibility targets if this line of sight exists.
        /// </summary>
        public AgentUpdated RaiseVisualEvent;
        /// <summary>
        /// When raised shows the closest barrier.
        /// </summary>
        public AgentUpdated ShowClosestBarrier;

        /// <summary>
        /// When raised shows the repulsion trajectory.
        /// </summary>
        public AgentUpdated ShowRepulsionTrajectory;
        private CellularFloor _cellularFloor { get; set; }
        private Dictionary<Cell, AgentEscapeRoutes> _escapeRouts { get; set; }
        private Matrix _matrix;
        private static Vector3D Z = new Vector3D(0, 0, 1);
        /// <summary>
        /// The debuger window which when the animation finished, shows up.
        /// </summary>
        public static StringBuilder Debuger = new StringBuilder();
        private MatrixTransform _transformGeometry { get; set; }

        protected override System.Windows.Media.Geometry DefiningGeometry
        {
            get { return this.shapeGeom; }
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="VisualAgentOptionalScenario"/> class.
        /// </summary>
        public VisualAgentOptionalScenario()
        {
            this.shapeGeom = new StreamGeometry();
            this._transformGeometry = new MatrixTransform(this._matrix);
            this.SetGeometry();
            this._random = new Random(DateTime.Now.Millisecond);
            this.Fill = VisualAgentOptionalScenario.OutsideRange;
        }

        #region TimeStoryboard Definition
        /// <summary>
        /// The time storyboard property
        /// </summary>
        public static DependencyProperty TimeStoryboardProperty =
            DependencyProperty.Register("TimeStoryboard", typeof(Storyboard), typeof(VisualAgentOptionalScenario),
            new FrameworkPropertyMetadata(null, VisualAgentOptionalScenario.TimeStoryboardPropertyPropertyChanged, VisualAgentOptionalScenario.PropertyCoerce));
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
            VisualAgentOptionalScenario agent = (VisualAgentOptionalScenario)obj;
            agent.TimeStoryboard = args.NewValue as Storyboard;
        }
        #endregion

        #region shapeGeom Definition
        /// <summary>
        /// The shape geometry property
        /// </summary>
        private static DependencyProperty shapeGeomProperty =
            DependencyProperty.Register("shapeGeom", typeof(StreamGeometry), typeof(VisualAgentOptionalScenario),
            new FrameworkPropertyMetadata(null, VisualAgentOptionalScenario.shapeGeomPropertyChanged, VisualAgentOptionalScenario.PropertyCoerce));
        private StreamGeometry shapeGeom
        {
            get { return (StreamGeometry)GetValue(shapeGeomProperty); }
            set { SetValue(shapeGeomProperty, value); }
        }
        private static void shapeGeomPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentOptionalScenario agent = (VisualAgentOptionalScenario)obj;
            agent.shapeGeom = (StreamGeometry)args.NewValue;
        }
        #endregion

        #region BodyElasticity  Definition
        /// <summary>
        /// The body elasticity property
        /// </summary>
        public static DependencyProperty BodyElasticityProperty =
            DependencyProperty.Register("BodyElasticity", typeof(double), typeof(VisualAgentOptionalScenario),
            new FrameworkPropertyMetadata(0.1d, VisualAgentOptionalScenario.BodyElasticityPropertyChanged, VisualAgentOptionalScenario.PropertyCoerce));
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
            VisualAgentOptionalScenario agent = (VisualAgentOptionalScenario)obj;
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
            DependencyProperty.Register("BarrierFriction", typeof(double), typeof(VisualAgentOptionalScenario),
            new FrameworkPropertyMetadata(0.1d, VisualAgentOptionalScenario.BarrierFrictionPropertyChanged, VisualAgentOptionalScenario.PropertyCoerce));
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
            VisualAgentOptionalScenario agent = (VisualAgentOptionalScenario)obj;
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
            DependencyProperty.Register("TimeStep", typeof(double), typeof(VisualAgentOptionalScenario),
            new FrameworkPropertyMetadata(0.0d, VisualAgentOptionalScenario.TimeStepPropertyChanged, VisualAgentOptionalScenario.PropertyCoerce));
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
            VisualAgentOptionalScenario agent = (VisualAgentOptionalScenario)obj;
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
            DependencyProperty.Register("BarrierRepulsionRange", typeof(double), typeof(VisualAgentOptionalScenario),
            new FrameworkPropertyMetadata(5.0d, VisualAgentOptionalScenario.BarrierRepulsionRangePropertyChanged, VisualAgentOptionalScenario.PropertyCoerce));
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
            VisualAgentOptionalScenario agent = (VisualAgentOptionalScenario)obj;
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
            DependencyProperty.Register("RepulsionChangeRate", typeof(double), typeof(VisualAgentOptionalScenario),
            new FrameworkPropertyMetadata(5.0d, VisualAgentOptionalScenario.RepulsionChangeRatePropertyChanged, VisualAgentOptionalScenario.PropertyCoerce));
        /// <summary>
        /// larger numbers make the barrier to have a more sudden impact and smaller number make the impact smoother and slower
        /// </summary>
        /// <value>The repulsion change rate.</value>
        public double RepulsionChangeRate
        {
            get { return (double)GetValue(RepulsionChangeRateProperty); }
            set { SetValue(RepulsionChangeRateProperty, value); }
        }
        private static void RepulsionChangeRatePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentOptionalScenario agent = (VisualAgentOptionalScenario)obj;
            if ((double)args.OldValue == (double)args.NewValue)
            {
                agent.RepulsionChangeRate = (double)args.NewValue;
            }
        }
        #endregion

        #region DecisionMakingPeriod Definition
        /// <summary>
        /// The decision making period property
        /// </summary>
        public static DependencyProperty DecisionMakingPeriodProperty =
            DependencyProperty.Register("DecisionMakingPeriod", typeof(double), typeof(VisualAgentOptionalScenario),
            new FrameworkPropertyMetadata(5.0d, VisualAgentOptionalScenario.DecisionMakingPeriodPropertyChanged, VisualAgentOptionalScenario.PropertyCoerce));
        /// <summary>
        /// Gets or sets the decision making period.
        /// </summary>
        /// <value>The decision making period.</value>
        public double DecisionMakingPeriod
        {
            get { return (double)GetValue(DecisionMakingPeriodProperty); }
            set { SetValue(DecisionMakingPeriodProperty, value); }
        }
        private static void DecisionMakingPeriodPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentOptionalScenario agent = (VisualAgentOptionalScenario)obj;
            if ((double)args.OldValue == (double)args.NewValue)
            {
                agent.DecisionMakingPeriod = (double)args.NewValue;
            }
        }
        #endregion

        #region DecisionMakingPeriodLambdaFactor Definition
        /// <summary>
        /// The decision making period lambda factor property
        /// </summary>
        public static DependencyProperty DecisionMakingPeriodLambdaFactorProperty =
            DependencyProperty.Register("DecisionMakingPeriodLambdaFactor", typeof(double), typeof(VisualAgentOptionalScenario),
            new FrameworkPropertyMetadata(0.40d, VisualAgentOptionalScenario.DecisionMakingPeriodLambdaFactorPropertyChanged, VisualAgentOptionalScenario.PropertyCoerce));
        /// <summary>
        /// Gets or sets the decision making period lambda factor.
        /// </summary>
        /// <value>The decision making period lambda factor.</value>
        public double DecisionMakingPeriodLambdaFactor
        {
            get { return (double)GetValue(DecisionMakingPeriodLambdaFactorProperty); }
            set { SetValue(DecisionMakingPeriodLambdaFactorProperty, value); }
        }
        private static void DecisionMakingPeriodLambdaFactorPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentOptionalScenario agent = (VisualAgentOptionalScenario)obj;
            if ( (double)args.OldValue  != (double)args.NewValue)
            {
                agent.DecisionMakingPeriodLambdaFactor = (double)args.NewValue;
                agent.DecisionMakingPeriodDistribution = new Exponential(1.0d/agent.DecisionMakingPeriodLambdaFactor, agent._random);
            }
        }  
        #endregion

        #region DecisionMakingPeriodDistribution Definition
        /// <summary>
        /// The decision making period distribution property
        /// </summary>
        public static DependencyProperty DecisionMakingPeriodDistributionProperty =
            DependencyProperty.Register("DecisionMakingPeriodDistribution", typeof(Exponential), typeof(VisualAgentOptionalScenario),
            new FrameworkPropertyMetadata(null, VisualAgentOptionalScenario.DecisionMakingPeriodDistributionPropertyChanged, VisualAgentOptionalScenario.PropertyCoerce));
        /// <summary>
        /// Gets or sets the decision making period distribution.
        /// </summary>
        /// <value>The decision making period distribution.</value>
        public Exponential DecisionMakingPeriodDistribution
        {
            get { return (Exponential)GetValue(DecisionMakingPeriodDistributionProperty); }
            set { SetValue(DecisionMakingPeriodDistributionProperty, value); }
        }
        private static void DecisionMakingPeriodDistributionPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentOptionalScenario agent = (VisualAgentOptionalScenario)obj;
            agent.DecisionMakingPeriodDistribution = (Exponential)args.NewValue;
        }
        #endregion

        #region VisibilityCosineFactor definition
        /// <summary>
        /// The visibility cosine factor property
        /// </summary>
        public static DependencyProperty VisibilityCosineFactorProperty =
            DependencyProperty.Register("VisibilityCosineFactor", typeof(double), typeof(VisualAgentOptionalScenario),
            new FrameworkPropertyMetadata(0.0d, VisualAgentOptionalScenario.VisibilityCosineFactorPropertyChanged, VisualAgentOptionalScenario.PropertyCoerce));
        private static void VisibilityCosineFactorPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentOptionalScenario agent = (VisualAgentOptionalScenario)obj;
            agent.VisibilityCosineFactor = (double)args.NewValue;
        }
        /// <summary>
        /// Gets or sets the visibility cosine factor.
        /// </summary>
        /// <value>The visibility cosine factor.</value>
        public double VisibilityCosineFactor
        {
            get { return (double)GetValue(VisibilityCosineFactorProperty); }
            set { SetValue(VisibilityCosineFactorProperty, value); }
        } 
        #endregion

        #region WalkTime Definition
        /// <summary>
        /// The walk-time property
        /// </summary>
        public static DependencyProperty WalkTimeProperty =
            DependencyProperty.Register("WalkTime", typeof(double), typeof(VisualAgentOptionalScenario), 
            new FrameworkPropertyMetadata(0.0d, VisualAgentOptionalScenario.WalkTimePropertyChanged, VisualAgentOptionalScenario.PropertyCoerce));
        /// <summary>
        /// This is to check against decision making period. If WalkTime is larger than decision making period destinations are updated
        /// </summary>
        public double WalkTime
        {
            get { return (double)GetValue(WalkTimeProperty); }
            set { SetValue(WalkTimeProperty, value); }
        }
        private static void WalkTimePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentOptionalScenario agent = (VisualAgentOptionalScenario)obj;
            agent.WalkTime = (double)args.NewValue;
        } 
        #endregion

        #region AngleDistributionLambdaFactor definition
        /// <summary>
        /// The angle distribution lambda factor property
        /// </summary>
        public static DependencyProperty AngleDistributionLambdaFactorProperty =
            DependencyProperty.Register("AngleDistributionLambdaFactor", typeof(double), typeof(VisualAgentOptionalScenario),
            new FrameworkPropertyMetadata(1.0d,
                VisualAgentOptionalScenario.AngleDistributionLambdaFactorPropertyChanged, VisualAgentOptionalScenario.PropertyCoerce));
        /// <summary>
        /// Gets or sets the angle distribution lambda factor.
        /// </summary>
        /// <value>The angle distribution lambda factor.</value>
        public double AngleDistributionLambdaFactor
        {
            get
            {
                return (double)GetValue(AngleDistributionLambdaFactorProperty);
            }
            set
            {
                SetValue(AngleDistributionLambdaFactorProperty, value);
            }
        }
        private static void AngleDistributionLambdaFactorPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentOptionalScenario agent = (VisualAgentOptionalScenario)obj;
            if ((double)args.NewValue != (double)args.OldValue)
            {
                agent.AngleDistributionLambdaFactor = (double)args.NewValue;
            }
        }
        #endregion

        #region DesirabilityDistributionLambdaFactor definition
        /// <summary>
        /// The desirability distribution lambda factor property
        /// </summary>
        public static DependencyProperty DesirabilityDistributionLambdaFactorProperty =
            DependencyProperty.Register("DesirabilityDistributionLambdaFactor", typeof(double), typeof(VisualAgentOptionalScenario),
            new FrameworkPropertyMetadata(1.0d,
                VisualAgentOptionalScenario.DesirabilityDistributionLambdaFactorPropertyChanged, VisualAgentOptionalScenario.PropertyCoerce));
        /// <summary>
        /// Gets or sets the desirability distribution lambda factor.
        /// </summary>
        /// <value>The desirability distribution lambda factor.</value>
        public double DesirabilityDistributionLambdaFactor
        {
            get
            {
                return (double)GetValue(DesirabilityDistributionLambdaFactorProperty);
            }
            set
            {
                SetValue(DesirabilityDistributionLambdaFactorProperty, value);
            }
        }
        private static void DesirabilityDistributionLambdaFactorPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentOptionalScenario agent = (VisualAgentOptionalScenario)obj;
            if ((double)args.NewValue != (double)args.OldValue)
            {
                agent.DesirabilityDistributionLambdaFactor = (double)args.NewValue;
            }
        }
        #endregion

        #region AngularVelocity definition
        /// <summary>
        /// The angular velocity property
        /// </summary>
        public static DependencyProperty AngularVelocityProperty = DependencyProperty.Register("AngularVelocity",
            typeof(double), typeof(VisualAgentOptionalScenario),
            new FrameworkPropertyMetadata(3.14d, VisualAgentOptionalScenario.AngularVelocityPropertyChanged,
                VisualAgentOptionalScenario.AngularVelocityPropertySet));
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
            VisualAgentOptionalScenario agent = (VisualAgentOptionalScenario)obj;
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
            DependencyProperty.Register("BodySize", typeof(double), typeof(VisualAgentOptionalScenario),
            new FrameworkPropertyMetadata(1.0d, FrameworkPropertyMetadataOptions.AffectsRender,
                VisualAgentOptionalScenario.BodySizePropertyChanged, VisualAgentOptionalScenario.ScalePropertySet,true));
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
            VisualAgentOptionalScenario agent = (VisualAgentOptionalScenario)obj;
            double _new = (double)args.NewValue;
            double _old = (double)args.OldValue;
            if (_old!=_new)
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
            typeof(double), typeof(VisualAgentOptionalScenario),
            new FrameworkPropertyMetadata(2.50d, VisualAgentOptionalScenario.VelocityMagnitudePropertyChanged,
            VisualAgentOptionalScenario.VelocityMagnitudePropertySet));
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
            VisualAgentOptionalScenario agent = (VisualAgentOptionalScenario)obj;
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
            typeof(double), typeof(VisualAgentOptionalScenario),
            new FrameworkPropertyMetadata(10.0d, VisualAgentOptionalScenario.AccelerationMagnitudePropertyChanged,
            VisualAgentOptionalScenario.PropertyCoerce));
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
            VisualAgentOptionalScenario agent = (VisualAgentOptionalScenario)obj;
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
            DependencyProperty.Register("CurrentState", typeof(StateBase), typeof(VisualAgentOptionalScenario),
            new FrameworkPropertyMetadata(new StateBase(UV.ZeroBase, UV.VBase), VisualAgentOptionalScenario.CurrentStatePropertyChanged,
            VisualAgentOptionalScenario.PropertyCoerce));
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
            VisualAgentOptionalScenario agent = (VisualAgentOptionalScenario)obj;
            if (!((StateBase)args.NewValue).Equals(((StateBase)args.OldValue)))
            {
                StateBase state = (StateBase)args.NewValue;
                if (state == null || state.Velocity == null || state.Direction == null || state.Location == null)
                {
                    MessageBox.Show("Null State caught");
                }
                agent.CurrentState = state;
            }
        }
        #endregion

        #region Destination definition
        /// <summary>
        /// The destination property
        /// </summary>
        public static DependencyProperty DestinationProperty =
            DependencyProperty.Register("Destination", typeof(UV), typeof(VisualAgentOptionalScenario),
            new FrameworkPropertyMetadata(null, VisualAgentOptionalScenario.DestinationPropertyChanged,
            VisualAgentOptionalScenario.PropertyCoerce));
        /// <summary>
        /// Gets or sets the destination.
        /// </summary>
        /// <value>The destination.</value>
        public UV Destination
        {
            get { return (UV)GetValue(DestinationProperty); }
            set { SetValue(DestinationProperty, value); }
        }
        private static void DestinationPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentOptionalScenario agent = (VisualAgentOptionalScenario)obj;
            agent.Destination = (UV)args.NewValue;
        }
        #endregion

        #region TotalWalkTime definition
        /// <summary>
        /// The total walk time property
        /// </summary>
        public static DependencyProperty TotalWalkTimeProperty =
            DependencyProperty.Register("TotalWalkTime", typeof(double), typeof(VisualAgentOptionalScenario),
            new FrameworkPropertyMetadata(0.00d,
                VisualAgentOptionalScenario.TotalWalkTimePropertyChanged, VisualAgentOptionalScenario.PropertyCoerce));
        /// <summary>
        /// The total time of walking. In this class it is used to determine the geometry of the agent
        /// </summary>
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
            VisualAgentOptionalScenario agent = (VisualAgentOptionalScenario)obj;
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
            DependencyProperty.Register("TotalWalkedLength", typeof(double), typeof(VisualAgentOptionalScenario),
            new FrameworkPropertyMetadata(0.00d,
                VisualAgentOptionalScenario.TotalWalkedLengthPropertyChanged, VisualAgentOptionalScenario.PropertyCoerce));
        /// <summary>
        /// The total length of walking. 
        /// </summary>
        public double TotalWalkedLength
        {
            get { return (double)GetValue(TotalWalkedLengthProperty); }
            set { SetValue(TotalWalkedLengthProperty, value); }
        }
        private static void TotalWalkedLengthPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentOptionalScenario agent = (VisualAgentOptionalScenario)obj;
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
            typeof(bool), typeof(VisualAgentOptionalScenario), new FrameworkPropertyMetadata(true,
            FrameworkPropertyMetadataOptions.AffectsRender,
            VisualAgentOptionalScenario.ShowVisibilityConePropertyChanged, VisualAgentOptionalScenario.PropertyCoerce, true));
        /// <summary>
        /// Gets or sets a value indicating whether to show visibility cone or not.
        /// </summary>
        /// <value><c>true</c> if [show visibility cone]; otherwise, <c>false</c>.</value>
        public bool ShowVisibilityCone
        {
            get { return (bool)GetValue(ShowVisibilityConeProperty); }
            set { SetValue(ShowVisibilityConeProperty, value); }
        }
        private static void ShowVisibilityConePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentOptionalScenario agent = (VisualAgentOptionalScenario)obj;
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
            typeof(bool), typeof(VisualAgentOptionalScenario), new FrameworkPropertyMetadata(true,
            FrameworkPropertyMetadataOptions.AffectsRender,
            VisualAgentOptionalScenario.ShowSafetyBufferPropertyChanged, VisualAgentOptionalScenario.PropertyCoerce, true));
        /// <summary>
        /// Gets or sets a value indicating whether to show the safety buffer or not.
        /// </summary>
        /// <value><c>true</c> if [show safety buffer]; otherwise, <c>false</c>.</value>
        public bool ShowSafetyBuffer
        {
            get { return (bool)GetValue(ShowSafetyBufferProperty); }
            set { SetValue(ShowSafetyBufferProperty, value); }
        }
        private static void ShowSafetyBufferPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            VisualAgentOptionalScenario agent = (VisualAgentOptionalScenario)obj;
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
            typeof(double), typeof(VisualAgentOptionalScenario), new FrameworkPropertyMetadata(2*Math.PI/3,
            FrameworkPropertyMetadataOptions.AffectsRender,
            VisualAgentOptionalScenario.VisibilityAnglePropertyChanged, VisualAgentOptionalScenario.PropertyCoerce, true));
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
            VisualAgentOptionalScenario agent = (VisualAgentOptionalScenario)obj;
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
            typeof(double), typeof(VisualAgentOptionalScenario), new FrameworkPropertyMetadata(0.00d,
                FrameworkPropertyMetadataOptions.AffectsRender,
                VisualAgentOptionalScenario.AnimationTimerPropertyChanged, VisualAgentOptionalScenario.AnimationTimerPropertySet, false));
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
            VisualAgentOptionalScenario agent = (VisualAgentOptionalScenario)obj;
            double now = (double)args.NewValue;
            agent.AnimationTimer = now;
            double before = (double)args.OldValue;
            double h = now - before;
            if (h > VisualAgentOptionalScenario.FixedTimeStep || h < 0)
            {
                h = VisualAgentOptionalScenario.FixedTimeStep;
            }
            var previousLocation = agent.CurrentState.Location.Copy();
            agent.TimeStep = h;
            while (agent.TimeStep > 0.0)
            {
                agent.TimeStepUpdate();
            }
            agent.WalkTime += h;
            //physical step update
            agent.TotalWalkTime += h;
            agent.TotalWalkedLength += agent.CurrentState.Location.DistanceTo(previousLocation);
            #region render stuff
            if (agent.RaiseVisualEvent != null)
            {
                agent.RaiseVisualEvent();
            }
            if (agent.ShowClosestBarrier != null)
            {
                agent.ShowClosestBarrier();
            }
            if (agent.ShowRepulsionTrajectory != null)
            {
                agent.ShowRepulsionTrajectory();
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
            DependencyProperty.Register("EdgeCollisionState", typeof(CollisionAnalyzer), typeof(VisualAgentOptionalScenario),
            new FrameworkPropertyMetadata(null, VisualAgentOptionalScenario.EdgeCollisionStatePropertyChanged,
            VisualAgentOptionalScenario.PropertyCoerce));
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
            VisualAgentOptionalScenario agent = (VisualAgentOptionalScenario)obj;
            agent.EdgeCollisionState = (CollisionAnalyzer)args.NewValue;
        }
        #endregion

        private static object PropertyCoerce(DependencyObject obj, object value)
        {
            return value;
        }

        private void updateDestination()
        {
            try
            {
                //update the destination
                AgentEscapeRoutes escapeRoute = null;
                var vantageCell = this._cellularFloor.FindCell(this.CurrentState.Location);
                if (!this._escapeRouts.ContainsKey(vantageCell))
                {
                    Index vantageIndex = this._cellularFloor.FindIndex(vantageCell);
                    //var cells = new SortedDictionary<double, Cell>();
                    var cells = new SortedSet<Cell>(new CellComparer(this.CurrentState));
                    for (int i = -2; i <= 2; i++)
                    {
                        for (int j = -2; j <= 2; j++)
                        {
                            if (i != 0 && j != 0)
                            {
                                Index neighbor = vantageIndex + new Index(i, j);
                                if (this._cellularFloor.ContainsCell(neighbor))
                                {
                                    if (this._escapeRouts.ContainsKey(this._cellularFloor.Cells[neighbor.I, neighbor.J]))
                                    {
                                        cells.Add(this._cellularFloor.Cells[neighbor.I, neighbor.J]);
                                    }
                                }
                            }
                        }
                    }
                    if (cells.Count > 0)
                    {
                        escapeRoute = this._escapeRouts[cells.Min];
                    }
                    else
                    {
                        // AgentEscapeRoutes cannot be found!
                        return;
                        //throw new ArgumentException("The agent and all of its surrounding cells are inside barrier buffer");
                    }
                }
                else
                {
                    escapeRoute = this._escapeRouts[vantageCell];
                }
                // escape route exists and is not null
                this.WalkTime = 0.0d;
                this.DecisionMakingPeriod = this.DecisionMakingPeriodDistribution.Sample();
                //updating desired state
                //filtering the destinations to those in the cone of vision
                var visableDestinationList = new List<AgentCellDestination>();
                foreach (var item in escapeRoute.Destinations)
                {
                    UV direction = item.Destination - this.CurrentState.Location;
                    direction.Unitize();
                    if (direction.DotProduct(this.CurrentState.Direction) >= this.VisibilityCosineFactor)
                    {
                        visableDestinationList.Add(item);
                    }
                }
                AgentCellDestination[] destinations;
                if (visableDestinationList.Count>0)
                {
                    destinations = visableDestinationList.ToArray();
                    visableDestinationList.Clear();
                    visableDestinationList = null;
                }
                else
                {
                    destinations = escapeRoute.Destinations;
                }

                double[] normalizedAngleCost = new double[destinations.Length]; //between zero and 1
                for (int i = 0; i < destinations.Length; i++)
                {
                    UV direction = destinations[i].Destination - this.CurrentState.Location;
                    direction.Unitize();
                    normalizedAngleCost[i] = (direction.DotProduct(this.CurrentState.Direction) + 1) / 2;
                }
                double[] weighted = new double[destinations.Length];
                double sum = 0;
                for (int i = 0; i < destinations.Length; i++)
                {
                    weighted[i] = this.AngleDistributionLambdaFactor * Math.Exp(-this.AngleDistributionLambdaFactor * normalizedAngleCost[i]) +
                        this.DesirabilityDistributionLambdaFactor * Math.Exp(-this.DesirabilityDistributionLambdaFactor * destinations[i].DesirabilityCost);
                    sum += weighted[i];
                }

                //selecting the destination
                double selected = this._random.NextDouble() * sum;
                sum = 0;
                int selectedIndex = 0;
                for (int i = 0; i < destinations.Length; i++)
                {
                    sum += weighted[i];
                    if (sum > selected)
                    {
                        selectedIndex = i;
                        break;
                    }
                }
                this.Destination = destinations[selectedIndex].Destination;
            }
            catch (Exception e)
            {
                //MessageBox.Show(e.Message);
                
            }
        }
        /// <summary>
        /// Loads the repulsion vector's magnitude.
        /// </summary>
        /// <returns>System.Double.</returns>
        public double LoadRepulsionMagnitude()
        {
            return Function.BezierRepulsionMethod(this.EdgeCollisionState.DistanceToBarrier);
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
                #region update destination
                bool decisionPeriodUpdate = this.WalkTime > this.DecisionMakingPeriod;
                bool desiredStateIsNull = this.Destination == null;
                bool distanceToDestination = true;
                if (this.Destination != null)
                {
                    distanceToDestination = UV.GetLengthSquared(this.CurrentState.Location, this.Destination) < .01d;
                }
                if (decisionPeriodUpdate || desiredStateIsNull || distanceToDestination || this.EdgeCollisionState.DistanceToBarrier < this.BodySize)
                {
                    this.updateDestination();
                }
                #endregion
                // make a copy of current state
                var newState = this.CurrentState.Copy();
                var previousState = this.CurrentState.Copy();
                // direction update
                var direction = this.Destination - newState.Location;
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
                    //only if the barrier is visible to the agent the repulsion force will be applied
                    if (this.EdgeCollisionState.NormalizedRepulsion.DotProduct(newState.Direction) >= 0.0d)//this.VisibilityCosineFactor)
                    {
                        //render agent color
                        this.Fill = VisualAgentOptionalScenario.OutsideRange;
                        this.Stroke = VisualAgentOptionalScenario.OutsideRange;
                    }
                    else
                    {
                        double repulsionMagnitude = this.LoadRepulsionMagnitude();
                        if (repulsionMagnitude != 0.0d)
                        {
                            //render agent color
                            this.Fill = VisualAgentOptionalScenario.InsideRange;
                            this.Stroke = VisualAgentOptionalScenario.InsideRange;
                            //calculate repulsion force
                            repulsionForce = this.EdgeCollisionState.NormalizedRepulsion * repulsionMagnitude;
                        }
                        else
                        {
                            //render agent color
                            this.Fill = VisualAgentOptionalScenario.OutsideRange;
                            this.Stroke = VisualAgentOptionalScenario.OutsideRange;
                        }
                    }
                }
                else
                {
                    throw new ArgumentNullException("Collision Analyzer failed!");
                } 
                #endregion
                // find Acceleration force
                UV acceleration = this.AccelerationMagnitude * direction + repulsionForce;
                //update velocity
                newState.Velocity += acceleration * this.TimeStep;
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

                this.CurrentState = newState;
                //checking for collisions
                var newEdgeCollisionState = CollisionAnalyzer.GetCollidingEdge(newState.Location, this._cellularFloor, BarrierType.Field);
                if (newEdgeCollisionState == null)
                {
                    throw new ArgumentNullException("Collision Analyzer failed!");
                }
                else
                {
                    if (newEdgeCollisionState.DistanceToBarrier <= this.BodySize / 2)
                    {
                        //newEdgeCollisionState.CollisionOccurred = true;
                        var collision = CollisionAnalyzer.GetCollision(this.EdgeCollisionState, newEdgeCollisionState, this.BodySize / 2, OSMDocument.AbsoluteTolerance);
                        if (collision == null)
                        {
                            throw new ArgumentException("Collision not found");
                        }
                        else 
                        {
                            UIMessage.Text = collision.TimeStepRemainderProportion.ToString();
                            //find partial timestep and update the timestep
                            double newTimeStep = 0.0;
                            if (collision.TimeStepRemainderProportion <= 0)
                            {
                                newTimeStep = this.TimeStep;
                                this.TimeStep = 0.0;
                            }
                            else
                            {
                                if (collision.TimeStepRemainderProportion >1.0)
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
                            UIMessage.Foreground = Brushes.Green;
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
            }
            catch (Exception error)
            {
                StopAnimation();
                MessageBox.Show(error.Report());
            }
        }

        /// <summary>
        /// Walks the initialize.
        /// </summary>
        /// <param name="cellularFloor">The cellular floor.</param>
        /// <param name="escapeRouts">The escape routs.</param>
        public void WalkInit(CellularFloor cellularFloor, Dictionary<Cell, AgentEscapeRoutes> escapeRouts)
        {
            this.WalkTime = 0;
            this._cellularFloor = cellularFloor;
            this._escapeRouts = escapeRouts;
            this.TimeStoryboard = new Storyboard();
            DoubleAnimation timeAnimator = new DoubleAnimation();
            timeAnimator.From = this.AnimationTimer;
            timeAnimator.To = this.AnimationTimer + VisualAgentOptionalScenario._timeCycle;
            timeAnimator.BeginTime = new TimeSpan(0);
            timeAnimator.Duration = TimeSpan.FromSeconds(VisualAgentOptionalScenario._timeCycle);
            Storyboard.SetTarget(timeAnimator, this);
            Storyboard.SetTargetProperty(timeAnimator, new PropertyPath(VisualAgentOptionalScenario.AnimationTimerProperty));
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
        /// Sets the initial position.
        /// </summary>
        /// <param name="currentStatePosition">The current state position.</param>
        public void SetInitialPosition(UV currentStatePosition)
        {
            this.CurrentState.Location = currentStatePosition;
            this._matrix.OffsetX = currentStatePosition.U;
            this._matrix.OffsetY = currentStatePosition.V;
            this.SetGeometry();
        }

        /// <summary>
        /// Sets the initial direction.
        /// </summary>
        /// <param name="currentStateDirection">The current state direction.</param>
        public void SetInitialDirection(UV currentStateDirection)
        {
            currentStateDirection.Unitize();
            this.CurrentState.Direction = currentStateDirection;
            Vector3D y3d = new Vector3D(this.CurrentState.Direction.U, this.CurrentState.Direction.V, 0);
            Vector3D x3d = Vector3D.CrossProduct(y3d, Z);
            _matrix.M11 = -y3d.Y;   _matrix.M12 = -x3d.Y;
            _matrix.M21 = y3d.X;    _matrix.M22 = x3d.X;
            this.SetGeometry();
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
                    sgc.BeginFigure(new Point(0, 0.5d*this.BodySize), false, true);
                    sgc.ArcTo(new Point(0.0d, -0.5d * this.BodySize), new Size(0.5d * this.BodySize, 0.5d * this.BodySize), 180.0d, false, SweepDirection.Clockwise, true, true);
                    sgc.ArcTo(new Point(0.0d, 0.5d * this.BodySize), new Size(0.5d * this.BodySize, 0.5d * this.BodySize), 180.0d, false, SweepDirection.Clockwise, true, true);
                }
            }

            this._transformGeometry.Matrix = this._matrix;
            this.shapeGeom.Transform = this._transformGeometry;
        }
    }


}

