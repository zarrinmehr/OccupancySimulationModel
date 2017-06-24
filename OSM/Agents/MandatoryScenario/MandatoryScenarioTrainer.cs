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
using SpatialAnalysis.CellularEnvironment;
using SpatialAnalysis.Data;
using SpatialAnalysis.FieldUtility;
using SpatialAnalysis.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpatialAnalysis.Miscellaneous;

namespace SpatialAnalysis.Agents.MandatoryScenario
{

    /// <summary>
    /// This class includes the mechanism for updating of the agent's state in mandatory and is designed for use in training process.
    /// </summary>
    /// <seealso cref="SpatialAnalysis.Agents.IAgent" />
    public class MandatoryScenarioTrainer: IAgent
    {

        /// <summary>
        /// Updates the agent.
        /// </summary>
        /// <param name="newState">The new state.</param>
        public void UpdateAgent(StateBase newState)
        {
            this.CurrentState = newState;
            this.TotalWalkTime = 0;
            this.EdgeCollisionState = null;
            while (this.TotalWalkTime < this.Duration)
            {
                this.TimeStep = this.FixedTimeStep;
                this.TotalWalkTime += this.FixedTimeStep;
                while (this.TimeStep != 0.0d)
                {
                    this.TimeStepUpdate();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MandatoryScenarioTrainer"/> class.
        /// </summary>
        /// <param name="host">The host.</param>
        /// <param name="fixedTimeStep">The fixed time step.</param>
        /// <param name="duration">The duration.</param>
        /// <param name="activity">The activity.</param>
        public MandatoryScenarioTrainer (OSMDocument host, double fixedTimeStep, double duration, Activity activity)
	    {

            this.Duration = duration;
            this.FixedTimeStep = fixedTimeStep / 1000.0d;
            this._random = new Random(DateTime.Now.Millisecond);
            this._cellularFloor = host.cellularFloor;
            this.BarrierRepulsionRange = host.Parameters[AgentParameters.GEN_BarrierRepulsionRange.ToString()].Value;
            this.RepulsionChangeRate = host.Parameters[AgentParameters.GEN_MaximumRepulsion.ToString()].Value;
            this.BarrierFriction = host.Parameters[AgentParameters.GEN_BarrierFriction.ToString()].Value;
            this.BodyElasticity = host.Parameters[AgentParameters.GEN_AgentBodyElasticity.ToString()].Value;
            this.AccelerationMagnitude = host.Parameters[AgentParameters.GEN_AccelerationMagnitude.ToString()].Value;
            this.AngularVelocity = host.Parameters[AgentParameters.GEN_AngularVelocity.ToString()].Value;

            this.BodySize = host.Parameters[AgentParameters.GEN_BodySize.ToString()].Value;
            this.VelocityMagnitude = host.Parameters[AgentParameters.GEN_VelocityMagnitude.ToString()].Value;
            this.CurrentActivity = activity;
	    }

        private Random _random { get; set; }
        private CellularFloor _cellularFloor { get; set; }
        /// <summary>
        /// Gets or sets the current activity.
        /// </summary>
        /// <value>The current activity.</value>
        public Activity CurrentActivity { get; set; }
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
        /// Gets or sets the total walk time.
        /// </summary>
        /// <value>The total walk time.</value>
        public double TotalWalkTime { get; set; }
        /// <summary>
        /// Gets or sets the state of the edge collision.
        /// </summary>
        /// <value>The state of the edge collision.</value>
        public CollisionAnalyzer EdgeCollisionState { get; set; }






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
                //checking the direction of the velocity agenst x direction 
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
                this.EdgeCollisionState = CollisionAnalyzer.GetCollidingEdge(newState.Location, this._cellularFloor, BarrierType.Field);
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
            acceleration += this.AccelerationMagnitude * direction + repulsionForce;
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
            //this._matrix.OffsetX = newState.Location.U;
            //this._matrix.OffsetY = newState.Location.V;

            // update direction
            double deltaAngle = this.AngularVelocity * this.TimeStep;
            Axis axis = new Axis(this.CurrentState.Direction, direction, deltaAngle);
            newState.Direction = axis.V_Axis;
            //update state
            newState.Direction.Unitize();
            //checking for collisions
            var newEdgeCollisionState = CollisionAnalyzer.GetCollidingEdge(newState.Location, this._cellularFloor, BarrierType.Field);
            if (newEdgeCollisionState == null)
            {
                this.EdgeCollisionState = CollisionAnalyzer.GetCollidingEdge(newState.Location, this._cellularFloor, BarrierType.Field);
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
        }




        /// <summary>
        /// Gets or sets the visibility angle.
        /// </summary>
        /// <value>The visibility angle.</value>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        public double VisibilityAngle
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
        /// Gets or sets the visibility cosine factor.
        /// </summary>
        /// <value>The visibility cosine factor.</value>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        public double VisibilityCosineFactor
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
    }
}

