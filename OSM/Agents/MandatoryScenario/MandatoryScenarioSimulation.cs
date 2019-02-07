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
using SpatialAnalysis.CellularEnvironment;
using SpatialAnalysis.FieldUtility;
using SpatialAnalysis.Agents.MandatoryScenario.Visualization;
using SpatialAnalysis.Geometry;
using SpatialAnalysis.Data;
using SpatialAnalysis.Events;
using SpatialAnalysis.Miscellaneous;

namespace SpatialAnalysis.Agents.MandatoryScenario
{
    /// <summary>
    /// Delegate ProgressReport for UI update
    /// </summary>
    /// <param name="progress">The progress.</param>
    public delegate void ProgressReport(double progress);
    /// <summary>
    /// Simulates and evaluates Mandatory Scenarios
    /// </summary>
    /// <seealso cref="SpatialAnalysis.Agents.ISimulateAgent" />
    public class MandatoryScenarioSimulation : ISimulateAgent
    {
        private Dictionary<string, Activity> _allActivities { get; set; }
        /// <summary>
        /// Gets or sets the UI update method.
        /// </summary>
        /// <value>The report progress.</value>
        public ProgressReport ReportProgress { get; set; }
        /// <summary>
        /// The debuger window
        /// </summary>
        public static StringBuilder Debuger = new StringBuilder();

        private Random _random { get; set; }
        private OSMDocument _host;

        /// <summary>
        /// Gets or sets all activities.
        /// </summary>
        /// <value>All Activities.</value>
        public Dictionary<string, Activity> AllActivities { get; set; }
        /// <summary>
        /// Gets or sets the current activity.
        /// </summary>
        /// <value>The current activity.</value>
        public Activity CurrentActivity { get; set; }
        /// <summary>
        /// Gets or sets the index of the current activity.
        /// </summary>
        /// <value>The index of the current activity.</value>
        public int CurrentActivityIndex { get; set; }
        /// <summary>
        /// Gets or sets the current sequence.
        /// </summary>
        /// <value>The current sequence.</value>
        public Sequence CurrentSequence { get; set; }
        /// <summary>
        /// Gets or sets the occupancy scenario.
        /// </summary>
        /// <value>The occupancy scenario.</value>
        public Scenario OccupancyScenario { get; set; }

        /// <summary>
        /// Gets or sets the agent physical movement mode.
        /// </summary>
        /// <value>The agent physical movement mode.</value>
        public PhysicalMovementMode AgentPhysicalMovementMode { get; set; }
        /// <summary>
        /// Gets or sets the agent engagement status.
        /// </summary>
        /// <value>The agent engagement status.</value>
        public MandatoryScenarioStatus AgentEngagementStatus { get; set; }
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
        /// Gets or sets the acceleration magnitude.
        /// </summary>
        /// <value>The acceleration magnitude.</value>
        public double AccelerationMagnitude { get; set; }
        /// <summary>
        /// Gets or sets the current state of the agent.
        /// </summary>
        /// <value>The state of the current.</value>
        public StateBase CurrentState { get; set; }

        /// <summary>
        /// Gets or sets the occupancy duration per hour.
        /// </summary>
        /// <value>The duration.</value>
        public double Duration { get; set; }
        /// <summary>
        /// Gets or sets the time-step.
        /// </summary>
        /// <value>The time step.</value>
        public double TimeStep { get; set; }
        /// <summary>
        /// Gets or sets the fixed time-step.
        /// </summary>
        /// <value>The fixed time step.</value>
        public double FixedTimeStep { get; set; }
        /// <summary>
        /// Gets or sets the activity engagement time.
        /// </summary>
        /// <value>The activity engagement time.</value>
        public double ActivityEngagementTime { get; set; }
        /// <summary>
        /// Gets or sets the total walk time.
        /// </summary>
        /// <value>The total walk time.</value>
        public double TotalWalkTime { get; set; }

        /// <summary>
        /// Gets or sets the total walked length.
        /// </summary>
        /// <value>The total length of the walked.</value>
        public double TotalWalkedLength { get; set; }
        /// <summary>
        /// Gets or sets the visibility angle cosine.
        /// </summary>
        /// <value>The visibility cosine factor.</value>
        public double VisibilityCosineFactor { get; set; }
        private double _visibilityAngle;
        /// <summary>
        /// Gets or sets the visibility angle.
        /// </summary>
        /// <value>The visibility angle.</value>
        public double VisibilityAngle
        {
            get { return _visibilityAngle; }
            set 
            { 
                this._visibilityAngle = value;
                this.VisibilityCosineFactor = Math.Cos(this.VisibilityAngle / 2);
            }
        }
        /// <summary>
        /// Gets or sets the state of the edge collision.
        /// </summary>
        /// <value>The state of the edge collision.</value>
        public CollisionAnalyzer EdgeCollisionState { get; set; }

        /// <summary>
        /// Gets or sets the detected visual triggers.
        /// </summary>
        /// <value>The detected visual triggers.</value>
        public Dictionary<double, Sequence> DetectedVisualTriggers { get; set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="MandatoryScenarioSimulation"/> class.
        /// </summary>
        /// <param name="host">The host.</param>
        /// <param name="fixedTimeStep">The fixed time-step.</param>
        /// <param name="duration">The duration.</param>
        public MandatoryScenarioSimulation(OSMDocument host,
            double fixedTimeStep, double duration)
        {
            this._host = host;
            this._allActivities = host.AllActivities;
            this._random = new Random(DateTime.Now.Millisecond);
            //this._matrix = Matrix.Identity;
            this.TimeStep = 0.0d;
            this.Duration = duration * 60 * 60;
            this.TotalWalkTime = 0.0d;
            this.FixedTimeStep = fixedTimeStep / 1000.0d;
            this.TotalWalkedLength = 0.0d;
            this.AgentEngagementStatus = MandatoryScenarioStatus.Free;
            this.AgentPhysicalMovementMode = PhysicalMovementMode.StopAndOrient;
            this.AllActivities = host.AllActivities;
            this.CurrentActivity = null;
            this.CurrentSequence = null;
            this.CurrentState = null;
            this.CurrentActivityIndex = 0;
            this.DetectedVisualTriggers = new Dictionary<double, Sequence>();

            //parameters
            this.AccelerationMagnitude = Parameter.DefaultParameters[AgentParameters.GEN_AccelerationMagnitude].Value;
            this.AngularVelocity = Parameter.DefaultParameters[AgentParameters.GEN_AngularVelocity].Value;
            
            this.BodyElasticity = Parameter.DefaultParameters[AgentParameters.GEN_AgentBodyElasticity].Value;
            this.BodySize = Parameter.DefaultParameters[AgentParameters.GEN_BodySize].Value;
            this.BarrierFriction = Parameter.DefaultParameters[AgentParameters.GEN_BarrierFriction].Value;
            this.BarrierRepulsionRange = Parameter.DefaultParameters[AgentParameters.GEN_BarrierRepulsionRange].Value;

            this.OccupancyScenario = host.AgentMandatoryScenario;
            this.OccupancyScenario.PartialSequenceToBeCompleted.Reset();
            this.RepulsionChangeRate = Parameter.DefaultParameters[AgentParameters.GEN_MaximumRepulsion].Value;
            this.VelocityMagnitude = Parameter.DefaultParameters[AgentParameters.GEN_VelocityMagnitude].Value;
            this.VisibilityAngle = Parameter.DefaultParameters[AgentParameters.GEN_VisibilityAngle].Value;
            this.EdgeCollisionState = null;
            this.ActivityEngagementTime = 0.0d;
        }

        /// <summary>
        /// Captures the evaluation event.
        /// </summary>
        /// <param name="captureDataEvent">if set to <c>true</c> captures data event.</param>
        /// <param name="captureVisualEvents">if set to <c>true</c> captures visual events.</param>
        /// <param name="captureActivityEngagement">if set to <c>true</c> captures activity engagement events.</param>
        /// <param name="timeSamplingRate">The time sampling rate.</param>
        /// <param name="dataFieldName">Name of the data field.</param>
        /// <param name="fileAddress">The file address to store the data captured.</param>
        /// <param name="visualEventSetting">The visual event setting.</param>
        /// <param name="visibilityExists">if set to <c>true</c> captures visual events when visibility exists.</param>
        /// <param name="frequencyAnalysis">if set to <c>true</c> analyzes the frequency of events.</param>
        /// <returns>Evaluation Event.</returns>
        /// <exception cref="System.ArgumentException">Agent location cannot be found</exception>
        public EvaluationEvent CaptureEvent(bool captureDataEvent, bool captureVisualEvents,bool captureActivityEngagement,
            int timeSamplingRate, string dataFieldName, string fileAddress,
            VisibilityTarget visualEventSetting, bool visibilityExists, bool frequencyAnalysis)
        {
            this.CurrentActivity = this.AllActivities[this.OccupancyScenario.MainStations.First()];
            this.CurrentState = this.CurrentActivity.DefaultState;
            int timeStepCounter = 0;
            double captured = 0;
            double uncaptured = 0;
            int percent = 0;
            this.TotalWalkedLength = 0;
            //creating a list of spatial data which are included in event capturing
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
            //creating a set of activities which are included in event capturing
            HashSet<Activity> activities = new HashSet<Activity>();
            if (captureActivityEngagement)
            {
                foreach (var item in this._allActivities.Values)
                {
                    if (item.UseToCaptureEvent)
                    {
                        activities.Add(item);
                    }
                }
            }
            //creating a list for saving states
            List<StateBase> states = null;
            if (!string.IsNullOrEmpty(fileAddress) && !string.IsNullOrWhiteSpace(fileAddress))
            {
                states = new List<StateBase>();
            }
            //for frequency analysis
            List<double> signal = null;
            if (frequencyAnalysis)
            {
                signal = new List<double>();
            }
            //initiating the trail data
            var trailData = new Dictionary<Cell, double>();
            foreach (Cell item in this._host.cellularFloor.Cells)
            {
                trailData.Add(item, 0.0d);
            }
            //running simulation
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

                switch (captureDataEvent)
                {
                    case true:
                        eventRaised = this.dataEventCaptured(dataFields, vantageCell);
                        if (eventRaised)
                        {
                            switch (captureVisualEvents)
                            {
                                case true:
                                    eventRaised = this.visualEventCaptured(visibilityExists, visualEventSetting);
                                    if (eventRaised)
                                    {
                                        if (captureActivityEngagement)
                                        {
                                            eventRaised = this.activityEventCaptured(activities);
                                        }
                                    }
                                    break;
                                case false:
                                    if (captureActivityEngagement)
                                    {
                                        eventRaised = this.activityEventCaptured(activities);
                                    }
                                    break;
                            }
                        }
                        break;
                    case false:
                        switch (captureVisualEvents)
                        {
                            case true:
                                eventRaised = this.visualEventCaptured(visibilityExists, visualEventSetting);
                                if (eventRaised)
                                {
                                    if (captureActivityEngagement)
                                    {
                                        eventRaised = this.activityEventCaptured(activities);
                                    }
                                }
                                break;
                            case false:
                                if (captureActivityEngagement)
                                {
                                    eventRaised = this.activityEventCaptured(activities);
                                }
                                break;
                        }
                        break;
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
            var occupancyEvent = new MandatoryEvaluationEvent(dataFieldName, trailData, probability,
                timeSamplingRate, this.FixedTimeStep * 1000, this.Duration / (60 * 60),
                this.VelocityMagnitude, this.VisibilityAngle, captureVisualEvents,
                captureDataEvent, EvaluationEventType.Mandatory, null)
                {
                    HasActivityEngagementEvent = captureActivityEngagement,
                };
            if (frequencyAnalysis)
            {
                occupancyEvent.LoadFrequencyAmplitudes(signal.ToArray());
                signal.Clear();
                signal = null;
            }
            return occupancyEvent;
        }
        private bool dataEventCaptured(List<SpatialDataField> dataFields, Cell vantageCell)
        {
            foreach (var item in dataFields)
            {
                if (item.EventCaptured(vantageCell) == null)
                {
                    return false;
                }
            }
            return true;
        }
        private bool visualEventCaptured(bool includedVisibility, VisibilityTarget visualEventSetting)
        {
            if (includedVisibility)
            {
                return visualEventSetting.VisualEventRaised(this.CurrentState, this.VisibilityCosineFactor, this._host.cellularFloor);
            }
            return !visualEventSetting.VisualEventRaised(this.CurrentState, this.VisibilityCosineFactor, this._host.cellularFloor);
        }
        private bool activityEventCaptured(HashSet<Activity> activities)
        {
            if (this.AgentEngagementStatus == MandatoryScenarioStatus.Engaged && this.AgentPhysicalMovementMode == PhysicalMovementMode.StopAndOrient && activities.Contains(this.CurrentActivity))
            {
                return true;
            }
            return false;
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
            this.CurrentActivity = this.AllActivities[this.OccupancyScenario.MainStations.First()];
            this.CurrentState = this.CurrentActivity.DefaultState;
            double timeInMainStations = 0d;
            double walkingTime = 0d;
            double activityEngagementTime = 0d;
            double total = 0d;
            while (this.TotalWalkTime < this.Duration)
            {
                this.TimeStep = this.FixedTimeStep;
                this.TotalWalkTime += this.FixedTimeStep;
                var previousState = this.CurrentState.Copy();
                while (this.TimeStep != 0.0d)
                {
                    this.TimeStepUpdate();
                }
                this.TotalWalkedLength += this.CurrentState.Location.DistanceTo(previousState.Location);
                Cell vantageCell = this._host.cellularFloor.FindCell(this.CurrentState.Location);
                if (trailData.ContainsKey(vantageCell))
                {
                    trailData[vantageCell] += 1;
                }
                else
                {
                    throw new ArgumentException("Agent location cannot be found");
                }
                total++;
                switch (this.AgentEngagementStatus)
	            {
		            case MandatoryScenarioStatus.Free:
                        timeInMainStations++;
                     break;
                    case MandatoryScenarioStatus.Engaged:
                        activityEngagementTime++;
                     break;
                    case MandatoryScenarioStatus.WalkingInSequence:
                        walkingTime++;
                     break;
	            }
            }
            foreach (Cell item in this._host.cellularFloor.Cells)
            {
                if (item.FieldOverlapState == OverlapState.Outside)
                {
                    trailData.Remove(item);
                }
            }

            timeInMainStations/=total;
            activityEngagementTime/=total;
            walkingTime/=total;
            int visualTriggerCount = 0;

            double averageChanceForVisualDetection = 0;
            foreach (var item in this.OccupancyScenario.Sequences)
            {
                if (item.HasVisualAwarenessField)
                {
                    visualTriggerCount++;
                    averageChanceForVisualDetection += item.TimeToGetVisuallyDetected;
                }
            }
            if (visualTriggerCount !=0)
	        {
                averageChanceForVisualDetection /= visualTriggerCount;
	        }
            averageChanceForVisualDetection /= this.Duration;

            double minimumChanceForVisualDetection = 0, maximumChanceForVisualDetection = 0;
            if (visualTriggerCount != 0)
            {
                List<double> values = new List<double>();
                foreach (var item in this.OccupancyScenario.Sequences)
                {
                    if (item.HasVisualAwarenessField)
                    {
                        values.Add(item.TimeToGetVisuallyDetected / this.Duration);
                    }
                }
                values.Sort();
                minimumChanceForVisualDetection = values[0];
                maximumChanceForVisualDetection = values[values.Count - 1];
            }

            var results = new SpatialAnalysis.Data.MandatorySimulationResult(dataFieldName, trailData, this.FixedTimeStep * 1000, this.Duration / (60 * 60),
                this.TotalWalkedLength / (this.Duration / (60 * 60)), timeInMainStations, walkingTime, activityEngagementTime, visualTriggerCount, 
                averageChanceForVisualDetection, minimumChanceForVisualDetection, maximumChanceForVisualDetection);

            this._host.AddSimulationResult(results);
            timer.Stop();
            double time = timer.Elapsed.TotalSeconds;
            timer = null;
            if (notify) System.Windows.MessageBox.Show(dataFieldName + " was added!\n" + "Run time: " + time.ToString() + " (Seconds)\n" + "'Seconds' per 'Occupancy Hour': " + (time / (this.Duration / (60 * 60))).ToString(), "Simulation Completed");
        }

        /// <summary>
        /// Determines whether the agent should stops and orient towards the focal point of the activity.
        /// </summary>
        /// <returns><c>true</c> if the agent is in the destination, <c>false</c> otherwise.</returns>
        public bool StopAndOrientCheck()
        {
            UV direction = this.CurrentActivity.DefaultState.Location - this.CurrentState.Location;
            if (direction.GetLengthSquared() == 0.0d)
            {
                return true;
            }
            if (UV.GetLengthSquared(this.CurrentState.Location, this.CurrentActivity.DefaultState.Location) < this.BodySize ||
                UV.GetLengthSquared(this.CurrentState.Location, this.CurrentActivity.DefaultState.Location) < this._host.cellularFloor.CellSize)
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
            //decomposing the velocity to its components towards center (x) and perpendicular to it (y)
            UV direction = this.CurrentActivity.DefaultState.Location - this.CurrentState.Location;
            if (direction.GetLengthSquared() != 0.0d)
            {
                direction.Unitize();
            }
            else
            {
                if (this.CurrentState.Velocity.GetLengthSquared() > this.AccelerationMagnitude * this.TimeStep * this.AccelerationMagnitude * this.TimeStep)
                {
                    double velocityMagnitude = this.CurrentState.Velocity.GetLength();
                    var nextVelocity = this.CurrentState.Velocity - this.AccelerationMagnitude * this.TimeStep * (this.CurrentState.Velocity / velocityMagnitude);
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
            if (velocity_x_length != 0.0d)
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
            //checking if exerting the acceleration can stop the perpendicular component of the velocity 
            if (velocity_y_length >= this.AccelerationMagnitude * this.TimeStep)
            {
                //all of the the acceleration force in the timestep is assigned to stop the perpendicular component of the velocity 
                velocity -= this.TimeStep * this.AccelerationMagnitude * velocity_yDir;
                return velocity;
            }
            else
            {
                //finding part of the timestep when the excursion of the acceleration force stops the the perpendicular component of the velocity 
                //velocity_y_length = this.AccelerationMagnitude * timeStep_y;
                double timeStep_y = velocity_y_length / this.AccelerationMagnitude;
                //update the velocity and location after first portion of the timestep
                var velocity1 = velocity - timeStep_y * this.AccelerationMagnitude * velocity_yDir;
                UV location_y = this.CurrentState.Location + velocity1 * timeStep_y;
                //calculating the remainder of the timestep
                double timeStep_x = this.TimeStep - timeStep_y;
                //checking the direction of the velocity agent x direction 
                var sign = Math.Sign(direction.DotProduct(velocity_xDir));
                if (sign == 1)//moving towards destination
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
                    else//do not except velocity to change the velocity
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
                        //check to see if the agent is close to the default location of station to set the Physical Movement Mode of the agent
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
                        //checking the sequence is finished or not
                        if (this.CurrentActivityIndex == this.CurrentSequence.ActivityCount - 1)//the sequence is finished
                        {
                            //reactivate the sequence and put it on the list
                            if (this.CurrentSequence.PriorityType == SEQUENCE_PRIORITY_LEVEL.PARTIAL)
                            {
                                PartialSequence incompleteSequence = (PartialSequence)this.CurrentSequence;
                                this.OccupancyScenario.ReActivate(incompleteSequence.OriginalSequence, this.TotalWalkTime);
                                incompleteSequence.Reset();
                            }
                            else
                            {
                                this.OccupancyScenario.ReActivate(this.CurrentSequence, this.TotalWalkTime);
                            }
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
                            this.CurrentActivityIndex++;
                            this.CurrentActivity = this.AllActivities[this.CurrentSequence.ActivityNames[this.CurrentActivityIndex]];
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
            Sequence firstSequence = this.OccupancyScenario.ExpectedTasks.First().Value;
            //Complete the partial sequence first if the next sequence is the queue does not have urgent priority
            if (firstSequence.PriorityType != SEQUENCE_PRIORITY_LEVEL.URGENT && !this.OccupancyScenario.PartialSequenceToBeCompleted.IsEmpty)
            {
                this.CurrentSequence = this.OccupancyScenario.PartialSequenceToBeCompleted;
            }
            else
            {
                this.CurrentSequence = firstSequence;
                double time = this.OccupancyScenario.ExpectedTasks.First().Key;
                this.OccupancyScenario.ExpectedTasks.Remove(time);
            }
            this.CurrentActivityIndex = 0;
            this.CurrentActivity = this.AllActivities[this.CurrentSequence.ActivityNames[this.CurrentActivityIndex]];
        }

        /// <summary>
        /// Loads the station which is most conveniently accessible.
        /// </summary>
        public void loadStation()
        {
            Cell cell = this._host.cellularFloor.FindCell(this.CurrentState.Location);
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
        /// <exception cref="System.ArgumentException">
        /// Collision not found
        /// or
        /// The proportion of timestep before collision is out of range: " + collision.TimeStepRemainderProportion.ToString()
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// Collision Analyzer failed!
        /// or
        /// Collision Analyzer failed!
        /// </exception>
        public void TimeStepUpdate()
        {
            this.updateAgentStatusInScenario();
            // make a copy of current state
            var newState = this.CurrentState.Copy();
            var previousState = this.CurrentState.Copy();
            // direction update
            UV direction = this.CurrentState.Direction;
            try
            {
                var filed_direction = this.CurrentActivity.Differentiate(this.CurrentState.Location);
                if (filed_direction != null)
                {
                    direction = filed_direction;
                }
            }
            catch (Exception error)
            {
                throw new ArgumentException(error.Report());
            }
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
                    ////render agent color
                    //this.Fill = VisualAgentMandatoryScenario.OutsideRange;
                    //this.Stroke = VisualAgentMandatoryScenario.OutsideRange;
                }
                else
                {
                    double repulsionMagnitude = this.LoadRepulsionMagnitude();
                    if (repulsionMagnitude != 0.0d)
                    {
                        ////render agent color
                        //this.Fill = VisualAgentMandatoryScenario.InsideRange;
                        //this.Stroke = VisualAgentMandatoryScenario.InsideRange;
                        //calculate repulsion force
                        repulsionForce = this.EdgeCollisionState.NormalizedRepulsion * repulsionMagnitude;
                    }
                    else
                    {
                        ////render agent color
                        //this.Fill = VisualAgentMandatoryScenario.OutsideRange;
                        //this.Stroke = VisualAgentMandatoryScenario.OutsideRange;
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
                //try to stop agent and assume the engagment position
                case PhysicalMovementMode.StopAndOrient:
                    if (newState.Velocity.GetLengthSquared() > 0)
                    {
                        UV deltaLOcation = this.CurrentState.Location - this.CurrentActivity.DefaultState.Location;
                        double deltaX = deltaLOcation.GetLength();
                        //figuring out the velocity
                        if (newState.Velocity.GetLengthSquared() < this.AccelerationMagnitude * this.TimeStep * this.AccelerationMagnitude * this.TimeStep && deltaX<this.BodySize/2)
                        {
                            newState.Velocity *= 0;
                            direction = this.CurrentActivity.DefaultState.Direction.Copy();
                        }
                        else
                        {
                            newState.Velocity = this.StopAndOrientVelocity();
                        }
                        //figuring out the direction
                        /*
                            if the agent is guaranteed to be able to stop at the destination then it will start 
                            taking the direction appropriate for engagement with the activity
                          DERIVING EQUATION 
                            calculate estatimated time to stop
                            x = 1/2 at^2
                            t = sqrt(2x/a)
                            the maximum velocity that can be gained or reduced is 
                            v = at 
                            v = a*sqrt(2x/a)=sqrt(2xa)
                            v^2= 2ax
                            if the length-squared of v is smaller than 2ax it is guaranteed that the agent can savely stop
                            |v|^2<2*|x|*a
                        */

                        bool stopGuaranteed = newState.Velocity.GetLengthSquared() < (2 * deltaX * this.AccelerationMagnitude);
                        if (stopGuaranteed)
                        {
                            direction = this.CurrentActivity.DefaultState.Direction.Copy();
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
            //this._matrix.OffsetX = newState.Location.U;
            //this._matrix.OffsetY = newState.Location.V;

            // update direction
            double deltaAngle = this.AngularVelocity * this.TimeStep;
            Axis axis = new Axis(this.CurrentState.Direction, direction, deltaAngle);
            newState.Direction = axis.V_Axis;
            //update state
            newState.Direction.Unitize();
            //checking for collisions
            var newEdgeCollisionState = CollisionAnalyzer.GetCollidingEdge(newState.Location, this._host.cellularFloor, BarrierType.Field);
            if (newEdgeCollisionState == null)
            {
                this.EdgeCollisionState = CollisionAnalyzer.GetCollidingEdge(newState.Location, this._host.cellularFloor, BarrierType.Field);
                throw new ArgumentNullException("Collision Analyzer failed!");
            }
            else
            {
                if (this.EdgeCollisionState.DistanceToBarrier > this.BodySize / 2 && newEdgeCollisionState.DistanceToBarrier < this.BodySize / 2)
                {
                    var collision = CollisionAnalyzer.GetCollision(this.EdgeCollisionState, newEdgeCollisionState, this.BodySize / 2, OSMDocument.AbsoluteTolerance);
                    if (collision == null)
                    {
                        throw new ArgumentException("Collision not found");
                    }
                    else
                    {
                        if (collision.TimeStepRemainderProportion > 1.0 || collision.TimeStepRemainderProportion < 0.0d)
                        {
                            throw new ArgumentException("The proportion of timestep before collision is out of range: " + collision.TimeStepRemainderProportion.ToString());
                        }
                        else
                        {
                            //update for partial timestep
                            //update timestep
                            double newTimeStep = this.TimeStep * collision.TimeStepRemainderProportion;
                            this.TimeStep -= newTimeStep;
                            // update location
                            newState.Location = collision.CollisionPoint;
                            //this._matrix.OffsetX = newState.Location.U;
                            //this._matrix.OffsetY = newState.Location.V;
                            // update velocity
                            var velocity = this.CurrentState.Velocity + newTimeStep * acceleration;
                            if (velocity.GetLengthSquared() > this.VelocityMagnitude * this.VelocityMagnitude)
                            {
                                velocity.Unitize();
                                velocity *= this.VelocityMagnitude;
                            }
                            //decompose velocity
                            UV velocityVerticalComponent = newEdgeCollisionState.NormalizedRepulsion * (newEdgeCollisionState.NormalizedRepulsion.DotProduct(velocity));
                            double velocityVerticalComponentLength = velocityVerticalComponent.GetLength();
                            UV velocityHorizontalComponent = velocity - velocityVerticalComponent;
                            double velocityHorizontalComponentLength = velocityHorizontalComponent.GetLength();
                            //decompose acceleration
                            UV accelerationVerticalComponent = newEdgeCollisionState.NormalizedRepulsion * (newEdgeCollisionState.NormalizedRepulsion.DotProduct(acceleration));
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
            this.EvaluateVisualTriggers();
        }

        /// <summary>
        /// visually scans the environment to find activated tasks that are not expected to occurs.
        /// </summary>
        public void EvaluateVisualTriggers()
        {
            this.DetectedVisualTriggers.Clear();
            foreach (var unexpectedTask in this.OccupancyScenario.UnexpectedTasks)
            {
                if (unexpectedTask.Value.VisualAwarenessField.VisualEventRaised(this.CurrentState, this.VisibilityCosineFactor, this._host.cellularFloor))
                {
                    if (unexpectedTask.Key < this.TotalWalkTime)
                    {
                        this.DetectedVisualTriggers.Add(unexpectedTask.Key, unexpectedTask.Value);
                        double delay = this.TotalWalkTime - unexpectedTask.Key;
                        unexpectedTask.Value.TimeToGetVisuallyDetected += delay;
                    }
                }
            }
            foreach (var item in this.DetectedVisualTriggers)
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
                if (timePriority > this.TotalWalkTime)
                {
                    timePriority = this.TotalWalkTime - 1;
                }
                this.OccupancyScenario.ExpectedTasks.Add(timePriority, item.Value);
            }
            //update current sequence and current activity
            if (DetectedVisualTriggers.Count != 0 && this.CurrentSequence != null && CurrentSequence.PriorityType != SEQUENCE_PRIORITY_LEVEL.URGENT)
            {
                if (this.CurrentSequence.PriorityType == SEQUENCE_PRIORITY_LEVEL.PARTIAL)
                {
                    this.OccupancyScenario.PartialSequenceToBeCompleted.Trim(this.CurrentActivityIndex);
                }
                else
                {
                    this.OccupancyScenario.PartialSequenceToBeCompleted.Assign(this.CurrentSequence, this.CurrentActivityIndex);
                }
                this.loadNextSequence();
                this.AgentEngagementStatus = MandatoryScenarioStatus.WalkingInSequence;
                this.AgentPhysicalMovementMode = PhysicalMovementMode.Move;
            }

        }

    }
}

