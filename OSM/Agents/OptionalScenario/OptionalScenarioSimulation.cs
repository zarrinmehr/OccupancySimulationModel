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
using System.Windows;
using SpatialAnalysis.Events;
using SpatialAnalysis.Data;
using MathNet.Numerics.Distributions;
using SpatialAnalysis.Agents.OptionalScenario.Visualization;
using SpatialAnalysis.Miscellaneous;

namespace SpatialAnalysis.Agents.OptionalScenario
{
    /// <summary>
    /// Delegate Progress Report for the UI
    /// </summary>
    /// <param name="progress">The progress.</param>
    public delegate void ProgressReport(double progress);
    /// <summary>
    /// Simulates and evaluates optional occupancy scenarios
    /// </summary>
    /// <seealso cref="SpatialAnalysis.Agents.ISimulateAgent" />
    public class OptionalScenarioSimulation : ISimulateAgent 
    {
        private bool _onError = false;
        /// <summary>
        /// Gets or sets the report progress delegate for the UI.
        /// </summary>
        /// <value>The report progress.</value>
        public ProgressReport ReportProgress { get; set; }
        /// <summary>
        /// The debuger
        /// </summary>
        public static StringBuilder Debuger = new StringBuilder();
        /// <summary>
        /// Gets or sets the total walked length.
        /// </summary>
        /// <value>The total length of the walked.</value>
        public double TotalWalkedLength { get; set; }

        /// <summary>
        /// Gets or sets the decision making period lambda factor.
        /// </summary>
        /// <value>The decision making period lambda factor.</value>
        public double DecisionMakingPeriodLambdaFactor { get; set; }
        /// <summary>
        /// Gets or sets the decision making period distribution.
        /// </summary>
        /// <value>The decision making period distribution.</value>
        public Exponential DecisionMakingPeriodDistribution { get; set; }
        /// <summary>
        /// Gets or sets the total walk time.
        /// </summary>
        /// <value>The total walk time.</value>
        public double TotalWalkTime { get; set; }
        /// <summary>
        /// Gets or sets the duration per hour.
        /// </summary>
        /// <value>The duration.</value>
        public double Duration { get; set; }
        /// <summary>
        /// Gets or sets the fixed time-step.
        /// </summary>
        /// <value>The fixed time step.</value>
        public double FixedTimeStep { get; set; }
        /// <summary>
        /// Gets or sets the visibility cosine factor to the cosine of the half of the visibility angle.
        /// </summary>
        /// <value>The visibility cosine factor.</value>
        public double VisibilityCosineFactor { get; set; }
        /// <summary>
        /// Gets or sets the decision making period.
        /// </summary>
        /// <value>The decision making period.</value>
        public double DecisionMakingPeriod { get; set; }
        /// <summary>
        /// the time step and the remainder of the time-step
        /// </summary>
        public double WalkTime { get; set; }
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
        /// <summary>
        /// Gets or sets the current state of the agent.
        /// </summary>
        /// <value>The state of the current.</value>
        public StateBase CurrentState { get; set; }
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
        private OSMDocument _host { get; set; }
        private Dictionary<Cell, AgentEscapeRoutes> _escapeRouts { get; set; }
        private Random _random { get; set; }
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
        /// Only captures trail data
        /// </summary>
        /// <param name="host">The main document to which this class belongs.</param>
        /// <param name="fixedTimeStep">The fixed time-step.</param>
        /// <param name="duration">The duration per hour.</param>
        public OptionalScenarioSimulation(OSMDocument host,
            double fixedTimeStep, double duration)
        {
            this._host = host;
            this.TimeStep = 0;
            this.BodyElasticity = host.FreeNavigationAgentCharacter.BodyElasticity;
            this.BarrierFriction = host.FreeNavigationAgentCharacter.BarrierFriction;
            this.BarrierRepulsionRange = host.FreeNavigationAgentCharacter.BarrierRepulsionRange;
            this.RepulsionChangeRate = host.FreeNavigationAgentCharacter.RepulsionChangeRate;
            this.AccelerationMagnitude = host.FreeNavigationAgentCharacter.AccelerationMagnitude;
            this.Duration = duration * 60 * 60;
            this.WalkTime = host.FreeNavigationAgentCharacter.WalkTime;
            this.TotalWalkTime = 0;
            this.FixedTimeStep = fixedTimeStep / 1000.0d;
            this.VisibilityCosineFactor = host.FreeNavigationAgentCharacter.VisibilityCosineFactor;
            ;
            this.AngleDistributionLambdaFactor = host.FreeNavigationAgentCharacter.AngleDistributionLambdaFactor;
            this.DesirabilityDistributionLambdaFactor = host.FreeNavigationAgentCharacter.DesirabilityDistributionLambdaFactor;
            this.AngularVelocity = host.FreeNavigationAgentCharacter.AngularVelocity;
            this.BodySize = host.FreeNavigationAgentCharacter.BodySize;
            this.VelocityMagnitude = host.FreeNavigationAgentCharacter.VelocityMagnitude;
            this.CurrentState = host.FreeNavigationAgentCharacter.CurrentState.Copy();
            if (host.FreeNavigationAgentCharacter.Destination != null)
            {
                this.Destination = host.FreeNavigationAgentCharacter.Destination.Copy();
            }
            else
            {
                this.Destination = null;
            }
            
            this.VisibilityAngle = host.FreeNavigationAgentCharacter.VisibilityAngle;
            this._random = new Random(DateTime.Now.Millisecond);
            this.DecisionMakingPeriodLambdaFactor = host.FreeNavigationAgentCharacter.DecisionMakingPeriodLambdaFactor;
            //this.DecisionMakingPeriodDistribution = new Exponential(1.0d/this.DecisionMakingPeriodLambdaFactor, this._random);
            this.DecisionMakingPeriod = host.FreeNavigationAgentCharacter.DecisionMakingPeriod;
            this.DecisionMakingPeriodDistribution = new Exponential(1.0d / Parameter.DefaultParameters[AgentParameters.OPT_DecisionMakingPeriodLambdaFactor].Value, this._random);
            this._escapeRouts = host.AgentScapeRoutes;
        }


        /// <summary>
        /// Captures the evaluation event.
        /// </summary>
        /// <param name="captureDataEvent">if set to <c>true</c> captures data event.</param>
        /// <param name="captureVisualEvents">if set to <c>true</c> captures visual events.</param>
        /// <param name="timeSamplingRate">The time sampling rate.</param>
        /// <param name="dataFieldName">Name of the data field.</param>
        /// <param name="fileAddress">The file address to which the data will be saved.</param>
        /// <param name="visualEventSetting">The visual event setting.</param>
        /// <param name="includedVisibility">if set to <c>true</c> captures events when visibility exists.</param>
        /// <param name="frequencyAnalysis">if set to <c>true</c> frequency analysis will be done.</param>
        /// <returns>EvaluationEvent.</returns>
        /// <exception cref="System.ArgumentException">Agent location cannot be found</exception>
        public EvaluationEvent CaptureEvent(bool captureDataEvent, bool captureVisualEvents,
            int timeSamplingRate, string dataFieldName, string fileAddress,
            VisibilityTarget visualEventSetting, bool includedVisibility, bool frequencyAnalysis)
        {
            int timeStepCounter = 0;
            double captured = 0;
            double uncaptured = 0;
            int percent = 0;
            List<SpatialDataField> dataFields = new List<SpatialDataField>();
            foreach (var item in this._host.cellularFloor.AllSpatialDataFields.Values)
            {
                SpatialDataField data = item as SpatialDataField;
                if (data != null)
                {
                    if (data.UseToCaptureEvent)
                        dataFields.Add(data);
                }
            }
            List<StateBase> states = null;
            if (!string.IsNullOrEmpty(fileAddress) && !string.IsNullOrWhiteSpace(fileAddress))
            {
                states = new List<StateBase>();
            }
            List<double> signal = null;
            if (frequencyAnalysis)
            {
                signal = new List<double>();
            }

            var trailData = new Dictionary<Cell, double>();
            foreach (Cell item in this._host.cellularFloor.Cells)
            {
                trailData.Add(item, 0.0d);
            }
            while (this.TotalWalkTime < this.Duration)
            {
                this.TimeStep = this.FixedTimeStep;
                this.TotalWalkTime += this.FixedTimeStep;
                while (this.TimeStep != 0.0d)
                {
                    this.TimeStepUpdate();
                }
                timeStepCounter++;
                if (this.ReportProgress != null)
                {
                    double percentd = 100 * this.TotalWalkTime / this.Duration;
                    if ((int)percentd != percent)
                    {
                        percent = (int)percentd;
                        this.ReportProgress(percent);
                    }
                }
                Cell vantageCell = this._host.cellularFloor.FindCell(this.CurrentState.Location);
                // capturing events
                var eventRaised = true;
                if (captureDataEvent && captureVisualEvents)
                {
                    if (includedVisibility)
                    {
                        eventRaised = visualEventSetting.VisualEventRaised(this.CurrentState, this.VisibilityCosineFactor, this._host.cellularFloor);
                    }
                    else
                    {
                        eventRaised = !visualEventSetting.VisualEventRaised(this.CurrentState, this.VisibilityCosineFactor, this._host.cellularFloor);
                    }
                    if (eventRaised)
                    {
                        foreach (var item in dataFields)
                        {
                            if (item.EventCaptured(vantageCell) == null)
                            {
                                eventRaised = false;
                                break;
                            }
                        }
                    }        
                }
                else if (captureDataEvent && !captureVisualEvents)
                {
                    foreach (var item in dataFields)
                    {
                        if (item.EventCaptured(vantageCell) == null)
                        {
                            eventRaised = false;
                            break;
                        }
                    }
                }
                else if (!captureDataEvent && captureVisualEvents)
                {
                    if (includedVisibility)
                    {
                        eventRaised = visualEventSetting.VisualEventRaised(this.CurrentState, this.VisibilityCosineFactor, this._host.cellularFloor);
                    }
                    else
                    {
                        eventRaised = !visualEventSetting.VisualEventRaised(this.CurrentState, this.VisibilityCosineFactor, this._host.cellularFloor);
                    }
                }
                //updating data
                bool record = (timeStepCounter % timeSamplingRate == 0);
                if (eventRaised)
                {
                    captured++;

                    if (states != null && record)
                    {
                        states.Add(this.CurrentState.Copy());
                    }
                    if (signal != null && record)
                    {
                        signal.Add(1.0d);
                    }
                    if (trailData.ContainsKey(vantageCell))
                    {
                        trailData[vantageCell] += 1;
                    }
                    else
                    {
                        throw new ArgumentException("Agent location cannot be found");
                    }
                }
                else
                {
                    uncaptured++;
                    if (states != null && record)
                    {
                        states.Add(null);
                    }
                    if (signal != null && record)
                    {
                        signal.Add(0.0d);
                    }
                }
                //checking to see if events are captured
                
            }
            if (!string.IsNullOrEmpty(fileAddress) && !string.IsNullOrWhiteSpace(fileAddress))
            {
                using (System.IO.StreamWriter sw = new System.IO.StreamWriter(fileAddress))
                {
                    foreach (var item in states)
                    {
                        if (item == null)
                        {
                            sw.WriteLine("null");
                        }
                        else
                        {
                            sw.WriteLine(item.ToString());
                        }
                    }
                }
            }

            foreach (Cell item in this._host.cellularFloor.Cells)
            {
                if (item.FieldOverlapState == OverlapState.Outside)
                {
                    trailData.Remove(item);
                }
            }
            double probability = captured / (captured + uncaptured);
            var event_= new EvaluationEvent(dataFieldName, trailData, probability,
                timeSamplingRate, this.FixedTimeStep * 1000, this.Duration / (60 * 60), 
                this.VelocityMagnitude, this.VisibilityAngle, captureVisualEvents, 
                captureDataEvent, EvaluationEventType.Optional, null);//states.ToArray());
            if (frequencyAnalysis)
            {
                event_.LoadFrequencyAmplitudes(signal.ToArray());
                signal.Clear();
                signal = null;
            }
            return event_;
        }
        /// <summary>
        /// Simulates the occupancy process.
        /// </summary>
        /// <param name="dataFieldName">Name of the data field to store the simulation results.</param>
        /// <param name="notify">if set to <c>true</c> sends a message when the background thread finishes the simulation.</param>
        /// <exception cref="System.ArgumentException">Agent location cannot be found</exception>
        public void Simulate(string dataFieldName, bool notify)
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            var trailData = new Dictionary<Cell, double>();
            foreach (Cell item in this._host.cellularFloor.Cells)
            {
                trailData.Add(item, 0.0d);
            }
            while (this.TotalWalkTime < this.Duration)
            {
                this.TimeStep = this.FixedTimeStep;
                this.TotalWalkTime += this.FixedTimeStep;
                this.WalkTime += this.FixedTimeStep;
                while (this.TimeStep != 0.0d)
                {
                    this.TimeStepUpdate();
                    if (this._onError)
                    {
                        return;
                    }
                }
                Cell vantageCell = this._host.cellularFloor.FindCell(this.CurrentState.Location);
                if (trailData.ContainsKey(vantageCell))
                {
                    trailData[vantageCell] += 1;
                }
                else
                {
                    throw new ArgumentException("Agent location cannot be found");
                }

            }
            foreach (Cell item in this._host.cellularFloor.Cells)
            {
                if (item.FieldOverlapState == OverlapState.Outside)
                {
                    trailData.Remove(item);
                }
                //if (this._trailData[item] == 0.0d)
                //{
                //    this._trailData.Remove(item);
                //}
            }
            var results = new SpatialAnalysis.Data.SimulationResult(dataFieldName, trailData, this.FixedTimeStep * 1000, this.Duration / (60 * 60));
            this._host.AddSimulationResult(results);
            
            timer.Stop();
            double time = timer.Elapsed.TotalSeconds;
            timer = null;
            if (notify) MessageBox.Show(dataFieldName + " was added!\n" + "Run time: " + time.ToString() + " (Seconds)\n" + "'Seconds' per 'Occupancy Hour': " + (time / (this.Duration / (60 * 60))).ToString(), "Simulation Completed");
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
                    this.EdgeCollisionState = CollisionAnalyzer.GetCollidingEdge(newState.Location, this._host.cellularFloor, BarrierType.Field);
                }
                if (this.EdgeCollisionState != null)
                {
                    //only if the barrier is visible to the agent the repulsion force will be applied
                    if (this.EdgeCollisionState.NormalizedRepulsion.DotProduct(newState.Direction) >= 0.0d)//this.VisibilityCosineFactor)
                    {
                    }
                    else
                    {
                        double repulsionMagnitude = this.LoadRepulsionMagnitude();
                        if (repulsionMagnitude != 0.0d)
                        {
                            //calculate repulsion force
                            repulsionForce = this.EdgeCollisionState.NormalizedRepulsion * repulsionMagnitude;
                        }
                        else
                        {
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
                //check velocity magnetude against the cap
                if (newState.Velocity.GetLengthSquared() > this.VelocityMagnitude * this.VelocityMagnitude)
                {
                    newState.Velocity.Unitize();
                    newState.Velocity *= this.VelocityMagnitude;
                }
                //update location
                var location = newState.Location + this.TimeStep * newState.Velocity;
                newState.Location = location;
                //update direction
                double deltaAngle = this.AngularVelocity * this.TimeStep;
                Axis axis = new Axis(this.CurrentState.Direction, direction, deltaAngle);
                newState.Direction = axis.V_Axis;
                this.CurrentState = newState;
                //checking for collisions
                var newEdgeCollisionState = CollisionAnalyzer.GetCollidingEdge(newState.Location, this._host.cellularFloor, BarrierType.Field);
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
                            //update direction
                            deltaAngle = this.AngularVelocity * newTimeStep;
                            axis = new Axis(this.CurrentState.Direction, direction, deltaAngle);
                            newState.Direction = axis.V_Axis;

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
                //StopAnimation();
                MessageBox.Show(error.Report());
            }
        }

        private void updateDestination()
        {
            try
            {
                //update the destination
                AgentEscapeRoutes escapeRoute = null;
                var vantageCell = this._host.cellularFloor.FindCell(this.CurrentState.Location);
                if (!this._escapeRouts.ContainsKey(vantageCell))
                {
                    Index vantageIndex = this._host.cellularFloor.FindIndex(vantageCell);
                    //var cells = new SortedDictionary<double, Cell>();
                    var cells = new SortedSet<Cell>(new CellComparer(this.CurrentState));
                    for (int i = -2; i <= 2; i++)
                    {
                        for (int j = -2; j <= 2; j++)
                        {
                            if (i != 0 && j != 0)
                            {
                                Index neighbor = vantageIndex + new Index(i, j);
                                if (this._host.cellularFloor.ContainsCell(neighbor))
                                {
                                    if (this._escapeRouts.ContainsKey(this._host.cellularFloor.Cells[neighbor.I, neighbor.J]))
                                    {
                                        cells.Add(this._host.cellularFloor.Cells[neighbor.I, neighbor.J]);
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

    }
}

