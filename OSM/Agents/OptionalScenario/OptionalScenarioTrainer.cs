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
using SpatialAnalysis.CellularEnvironment;
using SpatialAnalysis.IsovistUtility;
using SpatialAnalysis.Geometry;
using SpatialAnalysis.Data;
using MathNet.Numerics.Distributions;
using SpatialAnalysis.Miscellaneous;

namespace SpatialAnalysis.Agents.OptionalScenario
{

    /// <summary>
    /// This class includes the mechanism for updating of the agent's state in optional scenarios and is designed for use in training process.
    /// </summary>
    /// <seealso cref="SpatialAnalysis.Agents.IAgent" />
    internal class OptionalScenarioTrainer: IAgent
    {
        /// <summary>
        /// Gets or sets the body elasticity.
        /// </summary>
        /// <value>The body elasticity.</value>
        public double BodyElasticity { get; set; }
        /// <summary>
        /// Gets or sets the barrier friction.
        /// </summary>
        /// <value>The barrier friction.</value>
        public double BarrierFriction { get; set; }
        /// <summary>
        /// Gets or sets the barrier repulsion range.
        /// </summary>
        /// <value>The barrier repulsion range.</value>
        public double BarrierRepulsionRange { get; set; }
        /// <summary>
        /// Gets or sets the repulsion change rate.
        /// </summary>
        /// <value>The repulsion change rate.</value>
        public double RepulsionChangeRate { get; set; }
        /// <summary>
        /// Gets or sets the acceleration magnitude.
        /// </summary>
        /// <value>The acceleration magnitude.</value>
        public double AccelerationMagnitude { get; set; }
        /// <summary>
        /// Gets or sets the time-step.
        /// </summary>
        /// <value>The time step.</value>
        public double TimeStep { get; set; }
        /// <summary>
        /// Gets or sets the state of the edge collision.
        /// </summary>
        /// <value>The state of the edge collision.</value>
        public CollisionAnalyzer EdgeCollisionState { get; set; }
        /// <summary>
        /// Gets or sets the decision making period distribution.
        /// </summary>
        /// <value>The decision making period distribution.</value>
        public Exponential DecisionMakingPeriodDistribution { get; set; }
        /// <summary>
        /// Gets or sets the angle distribution lambda factor.
        /// </summary>
        /// <value>The angle distribution lambda factor.</value>
        public double AngleDistributionLambdaFactor { get; set; }
        /// <summary>
        /// Gets or sets the desirability distribution lambda factor.
        /// </summary>
        /// <value>The desirability distribution lambda factor.</value>
        public double DesirabilityDistributionLambdaFactor { get; set; }
        /// <summary>
        /// The trail points
        /// </summary>
        public static List<UV> TrailPoints = new List<UV>();
        /// <summary>
        /// Gets or sets the report progress.
        /// </summary>
        /// <value>The report progress.</value>
        public ProgressReport ReportProgress { get; set; }
        /// <summary>
        /// The debuger
        /// </summary>
        public static StringBuilder Debuger = new StringBuilder();
        /// <summary>
        /// Gets or sets the total walk time.
        /// </summary>
        /// <value>The total walk time.</value>
        public double TotalWalkTime { get; set; }
        /// <summary>
        /// Gets or sets the duration.
        /// </summary>
        /// <value>The duration.</value>
        public double Duration { get; set; }
        /// <summary>
        /// Gets or sets the fixed time step.
        /// </summary>
        /// <value>The fixed time step.</value>
        public double FixedTimeStep { get; set; }
        /// <summary>
        /// Gets or sets the visibility cosine factor.
        /// </summary>
        /// <value>The visibility cosine factor.</value>
        public double VisibilityCosineFactor { get; set; }
        /// <summary>
        /// Gets or sets the decision making period.
        /// </summary>
        /// <value>The decision making period.</value>
        public double DecisionMakingPeriod { get; set; }
        /// <summary>
        /// Gets or sets the walk time.
        /// </summary>
        /// <value>The walk time.</value>
        public double WalkTime { get; set; }
        /// <summary>
        /// Gets or sets the angular velocity.
        /// </summary>
        /// <value>The angular velocity.</value>
        public double AngularVelocity { get; set; }
        /// <summary>
        /// Gets or sets the size of the body.
        /// </summary>
        /// <value>The size of the body.</value>
        public double BodySize { get; set; }
        /// <summary>
        /// Gets or sets the velocity magnitude.
        /// </summary>
        /// <value>The velocity magnitude.</value>
        public double VelocityMagnitude { get; set; }
        public StateBase _currentState;
        /// <summary>
        /// Gets or sets the current state of the agent.
        /// </summary>
        /// <value>The state of the current.</value>
        public StateBase CurrentState 
        {
            get { return this._currentState; }
            set
            {
                this._currentState= value;
            }
        }
        /// <summary>
        /// Gets or sets the destination.
        /// </summary>
        /// <value>The destination.</value>
        public UV Destination { get; set; }
        /// <summary>
        /// Gets or sets the visibility angle.
        /// </summary>
        /// <value>The visibility angle.</value>
        public double VisibilityAngle { get; set; }
        private CellularFloor _cellularFloor { get; set; }
        private Dictionary<Cell, AgentEscapeRoutes> _escapeRouts { get; set; }
        private Random _random { get; set; }
        private Dictionary<Cell, double> _staticCost { get; set; }
        private double _isovistExternalRadius { get; set; }
        private int _destinationCount { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionalScenarioTrainer"/> class.
        /// </summary>
        /// <param name="host">The main document to which the agent belongs.</param>
        /// <param name="fixedTimeStep">The fixed time-step per milliseconds.</param>
        /// <param name="duration">The duration per hours.</param>
        public OptionalScenarioTrainer(OSMDocument host, double fixedTimeStep, double duration)
        {
            this._isovistExternalRadius = host.Parameters[AgentParameters.OPT_IsovistExternalDepth.ToString()].Value;
            this._destinationCount = (int)(host.Parameters[AgentParameters.OPT_NumberOfDestinations.ToString()].Value);
            this.Duration = duration;
            this.FixedTimeStep = fixedTimeStep / 1000.0d;
            this._random = new Random(DateTime.Now.Millisecond);
            this._cellularFloor = host.cellularFloor;
            this._staticCost = this._cellularFloor.GetStaticCost();
            this._escapeRouts = new Dictionary<Cell, AgentEscapeRoutes>();
            this.BarrierRepulsionRange = host.Parameters[AgentParameters.GEN_BarrierRepulsionRange.ToString()].Value;
            this.RepulsionChangeRate = host.Parameters[AgentParameters.GEN_MaximumRepulsion.ToString()].Value;
            this.BarrierFriction = host.Parameters[AgentParameters.GEN_BarrierFriction.ToString()].Value;
            this.BodyElasticity = host.Parameters[AgentParameters.GEN_AgentBodyElasticity.ToString()].Value;
            this.AccelerationMagnitude = host.Parameters[AgentParameters.GEN_AccelerationMagnitude.ToString()].Value;
            this.AngularVelocity = host.Parameters[AgentParameters.GEN_AngularVelocity.ToString()].Value;
            this.DesirabilityDistributionLambdaFactor = host.Parameters[AgentParameters.OPT_DecisionMakingPeriodLambdaFactor.ToString()].Value;
            this.BodySize = host.Parameters[AgentParameters.GEN_BodySize.ToString()].Value;
            this.VelocityMagnitude = host.Parameters[AgentParameters.GEN_VelocityMagnitude.ToString()].Value;
            this.VisibilityAngle = host.Parameters[AgentParameters.GEN_VisibilityAngle.ToString()].Value;
            this.VisibilityCosineFactor = Math.Cos(this.VisibilityAngle / 2);
            this.AngleDistributionLambdaFactor = host.Parameters[AgentParameters.OPT_AngleDistributionLambdaFactor.ToString()].Value;
            this.DecisionMakingPeriodDistribution = new Exponential(1.0d / this.DecisionMakingPeriod);
        }

        /// <summary>
        /// Updates the agent.
        /// </summary>
        /// <param name="newState">The new state.</param>
        public void UpdateAgent(StateBase newState)
        {
            this.CurrentState = newState;
            this.TotalWalkTime = 0;
            this.WalkTime = 0.0d;
            this.EdgeCollisionState = null;
            this.Destination = null;
            while (this.TotalWalkTime < this.Duration)
            {
                this.TimeStep = this.FixedTimeStep;
                this.TotalWalkTime += this.FixedTimeStep;
                this.WalkTime += this.FixedTimeStep;
                while (this.TimeStep != 0.0d)
                {
                    this.TimeStepUpdate();
                }
            }
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
                    try
                    {
                        escapeRoute = CellularIsovistCalculator.GetAgentEscapeRoutes(
                            vantageCell,
                            this._isovistExternalRadius,
                            this._destinationCount,
                            this._cellularFloor,
                            this._staticCost,
                            0.0000001d);
                    }
                    catch (Exception)
                    {
                        //throw new ArgumentException("Escape Routes cannot be calculated for the agent training!");
                        //MessageBox.Show(e.Report());
                        //try
                        //{
                        //    escapeRoute = CellularIsovistCalculator.GetAgentEscapeRoutes(
                        //        vantageCell,
                        //        this._isovistRadius,
                        //        this._destinationCount,
                        //        this._cellularFloor,
                        //        this._staticCost,
                        //        0.0000001d);
                        //}
                        //catch (Exception) { }
                    }
                    if (escapeRoute == null)
                    {
                        return;
                    }
                    this._escapeRouts.Add(vantageCell, escapeRoute);
                }
                else
                {
                    escapeRoute = this._escapeRouts[vantageCell];
                }
                this.DecisionMakingPeriod = this.DecisionMakingPeriodDistribution.Sample();
                this.WalkTime = 0.0d;
                //updating desired state
                //filtering the destinations to those in the cone of vision
                var visibleDestinationList = new List<AgentCellDestination>();
                foreach (var item in escapeRoute.Destinations)
                {
                    UV direction = item.Destination - this.CurrentState.Location;
                    direction.Unitize();
                    if (direction.DotProduct(this.CurrentState.Direction) >= this.VisibilityCosineFactor)
                    {
                        visibleDestinationList.Add(item);
                    }
                }
                AgentCellDestination[] destinations;
                if (visibleDestinationList.Count > 0)
                {
                    destinations = visibleDestinationList.ToArray();
                    visibleDestinationList.Clear();
                    visibleDestinationList = null;
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
            catch (Exception)
            {
                //MessageBox.Show(e.Report());
            }
        }

        /// <summary>
        /// Updates the time-step.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">
        /// Collision Analyzer failed!
        /// or
        /// Collision Analyzer failed!
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// Collision not found
        /// or
        /// </exception>
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
                    //only if the barrier is visible to the agent the repulstion force will be applied
                    if (this.EdgeCollisionState.NormalizedRepulsion.DotProduct(newState.Direction) >= 0.0d)//this.VisibilityCosineFactor)
                    {
                    }
                    else
                    {
                        //calculate repulsion force
                        repulsionForce = this.EdgeCollisionState.NormalizedRepulsion * this.LoadRepulsionMagnetude();
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
                //check velocity magnetude against the cap
                if (newState.Velocity.GetLengthSquared() > this.VelocityMagnitude * this.VelocityMagnitude)
                {
                    newState.Velocity.Unitize();
                    newState.Velocity *= this.VelocityMagnitude;
                }
                //update location
                var location = newState.Location + this.TimeStep * newState.Velocity;
                newState.Location = location;
                //update the direction of the agent in the transformation matrix
                double deltaAngle = this.AngularVelocity * this.TimeStep;
                Axis axis = new Axis(this.CurrentState.Direction, direction, deltaAngle);
                newState.Direction = axis.V_Axis;
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
                        var collision = CollisionAnalyzer.GetCollision(this.EdgeCollisionState, newEdgeCollisionState, this.BodySize / 2, OSMDocument.AbsoluteTolerance);
                        if (collision == null)
                        {
                            throw new ArgumentException("Collision not found");
                        }
                        else
                        {
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
                            //update direction
                            deltaAngle = this.AngularVelocity * newTimeStep;
                            axis = new Axis(this.CurrentState.Direction, direction, deltaAngle);
                            newState.Direction = axis.V_Axis;
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
                throw new ArgumentException(error.Report());
            }
        }

        /// <summary>
        /// Loads the repulsion magnitude.
        /// </summary>
        /// <returns>System.Double.</returns>
        public double LoadRepulsionMagnetude()
        {
            return Function.BezierRepulsionMethod(this.EdgeCollisionState.DistanceToBarrier);
        }




        /// <summary>
        /// Gets or sets the walked total length.
        /// </summary>
        /// <value>The total length of the walked.</value>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        public double TotalWalkedLength
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Loads the repulsion vector's magnitude.
        /// </summary>
        /// <returns>System.Double.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public double LoadRepulsionMagnitude()
        {
            throw new NotImplementedException();
        }
    }
}

