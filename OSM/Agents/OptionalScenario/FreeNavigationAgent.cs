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
using SpatialAnalysis.Agents.Visualization.AgentModel;
using SpatialAnalysis.Geometry;
using SpatialAnalysis.Data;
using MathNet.Numerics.Distributions;
using SpatialAnalysis.CellularEnvironment;

namespace SpatialAnalysis.Agents.OptionalScenario
{

    /// <summary>
    /// Class FreeNavigationAgent is a simple data model and does offer functionalities.
    /// </summary>
    public class FreeNavigationAgent: IAgent
    {
        /// <summary>
        /// Creates the specified current state.
        /// </summary>
        /// <param name="currentState">State of the current.</param>
        /// <returns>FreeNavigationAgent.</returns>
        public static FreeNavigationAgent Create(StateBase currentState)
        {
            FreeNavigationAgent agent = new FreeNavigationAgent
            {
                VisibilityCosineFactor = Math.Cos(Parameter.DefaultParameters[AgentParameters.GEN_VisibilityAngle].Value / 2),
                DecisionMakingPeriod = Parameter.DefaultParameters[AgentParameters.OPT_DecisionMakingPeriodLambdaFactor].Value,
                WalkTime = 0.0d,
                AngleDistributionLambdaFactor = Parameter.DefaultParameters[AgentParameters.OPT_AngleDistributionLambdaFactor].Value,
                DesirabilityDistributionLambdaFactor = Parameter.DefaultParameters[AgentParameters.OPT_DesirabilityDistributionLambdaFactor].Value,
                AngularVelocity = Parameter.DefaultParameters[AgentParameters.GEN_AngularVelocity].Value,
                BodySize = Parameter.DefaultParameters[AgentParameters.GEN_BodySize].Value,
                VelocityMagnitude = Parameter.DefaultParameters[AgentParameters.GEN_VelocityMagnitude].Value,
                CurrentState = currentState,
                Destination = null,
                VisibilityAngle = Parameter.DefaultParameters[AgentParameters.GEN_VisibilityAngle].Value,
                DecisionMakingPeriodLambdaFactor = Parameter.DefaultParameters[AgentParameters.OPT_DecisionMakingPeriodLambdaFactor].Value,
                DecisionMakingPeriodDistribution = new Exponential(1.0d / Parameter.DefaultParameters[AgentParameters.OPT_DecisionMakingPeriodLambdaFactor].Value),
                BodyElasticity = Parameter.DefaultParameters[AgentParameters.GEN_AgentBodyElasticity].Value,
                AccelerationMagnitude = Parameter.DefaultParameters[AgentParameters.GEN_AccelerationMagnitude].Value,
                BarrierFriction = Parameter.DefaultParameters[AgentParameters.GEN_BarrierFriction].Value,
                BarrierRepulsionRange = Parameter.DefaultParameters[AgentParameters.GEN_BarrierRepulsionRange].Value,
                RepulsionChangeRate = Parameter.DefaultParameters[AgentParameters.GEN_MaximumRepulsion].Value,
                EdgeCollisionState= null,
                TimeStep =0.0d,
            };
            return agent;
        }
        private double _decisionMakingPeriodLambdaFactor;
        /// <summary>
        /// Gets or sets the decision making period lambda factor.
        /// </summary>
        /// <value>The decision making period lambda factor.</value>
        public double DecisionMakingPeriodLambdaFactor
        {
            get { return _decisionMakingPeriodLambdaFactor; }
            set 
            {
                if (_decisionMakingPeriodLambdaFactor != value)
                {
                    _decisionMakingPeriodLambdaFactor = value;
                    this.DecisionMakingPeriodDistribution = new Exponential(1.0d / this._decisionMakingPeriodLambdaFactor);
                }
            }
        }
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
        /// Gets or sets the time-step.
        /// </summary>
        /// <value>The time step.</value>
        public double TimeStep { get; set; }
        /// <summary>
        /// The barrier repulsion range
        /// </summary>
        public double BarrierRepulsionRange;
        /// <summary>
        /// The repulsion change rate
        /// </summary>
        public double RepulsionChangeRate;
        /// <summary>
        /// The acceleration magnitude
        /// </summary>
        public double AccelerationMagnitude;
        /// <summary>
        /// The destination
        /// </summary>
        public UV Destination;
        /// <summary>
        /// The edge collision state
        /// </summary>
        public CollisionAnalyzer EdgeCollisionState;
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
        /// <summary>
        /// Gets or sets the current state of the agent.
        /// </summary>
        /// <value>The state of the current.</value>
        public StateBase CurrentState { get; set; }
        /// <summary>
        /// Gets or sets the visibility angle.
        /// </summary>
        /// <value>The visibility angle.</value>
        public double VisibilityAngle { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="FreeNavigationAgent"/> class.
        /// </summary>
        public FreeNavigationAgent() { }

        public static bool operator ==(FreeNavigationAgent a, FreeNavigationAgent b)
        {
            if ((object)a == null || (object)b == null)
            {
                return false;
            }
            bool result =
                a.AngularVelocity == b.AngularVelocity &&
                a.CurrentState.Equals(b.CurrentState) &&
                a.DecisionMakingPeriod == b.DecisionMakingPeriod &&
                a.BodySize == b.BodySize &&
                a.VelocityMagnitude == b.VelocityMagnitude &&
                a.VisibilityAngle == b.VisibilityAngle &&
                a.VisibilityCosineFactor == b.VisibilityCosineFactor &&
                a.WalkTime == b.WalkTime &&
                a.DesirabilityDistributionLambdaFactor == b.DesirabilityDistributionLambdaFactor &&
                a.BodyElasticity == b.BodyElasticity &&
                a.BodySize == b.BodySize &&
                a.AccelerationMagnitude == b.AccelerationMagnitude &&
                a.BarrierFriction == b.BarrierFriction &&
                a.BarrierRepulsionRange == b.BarrierRepulsionRange &&
                a.RepulsionChangeRate == b.RepulsionChangeRate &&
                a.AngleDistributionLambdaFactor == b.AngleDistributionLambdaFactor;
            return result;
        }
        public static bool operator !=(FreeNavigationAgent a, FreeNavigationAgent b)
        {
            return !(a == b);
        }
        /// <summary>
        /// Copies this instance deeply.
        /// </summary>
        /// <returns>FreeNavigationAgent.</returns>
        public FreeNavigationAgent Copy()
        {
            FreeNavigationAgent copied = new FreeNavigationAgent() 
            { 
                AngleDistributionLambdaFactor = this.AngleDistributionLambdaFactor,
                AngularVelocity = this.AngularVelocity,
                DesirabilityDistributionLambdaFactor = this.DesirabilityDistributionLambdaFactor,
                CurrentState= this.CurrentState,
                DecisionMakingPeriod = this.DecisionMakingPeriod,
                BodySize = this.BodySize,
                VelocityMagnitude = this.VelocityMagnitude,
                VisibilityAngle = this.VisibilityAngle,
                VisibilityCosineFactor = this.VisibilityCosineFactor,
                WalkTime = this.WalkTime,
                RepulsionChangeRate = this.RepulsionChangeRate,
                BarrierRepulsionRange = this.BarrierRepulsionRange,
                BarrierFriction = this.BarrierFriction,
                AccelerationMagnitude = this.AccelerationMagnitude,
                BodyElasticity = this.BodyElasticity,
            };

            return copied;
        }

        /// <summary>
        /// Gets or sets the barrier repulsion range.
        /// </summary>
        /// <value>The barrier repulsion range.</value>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        double IAgent.BarrierRepulsionRange
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
        /// Gets or sets the repulsion change rate.
        /// </summary>
        /// <value>The repulsion change rate.</value>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        double IAgent.RepulsionChangeRate
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
        /// Gets or sets the acceleration magnitude.
        /// </summary>
        /// <value>The acceleration magnitude.</value>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        double IAgent.AccelerationMagnitude
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
        /// Gets or sets the total walk time.
        /// </summary>
        /// <value>The total walk time.</value>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        public double TotalWalkTime
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
        /// Gets or sets the total walked length.
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
        /// Gets or sets the state of the edge collision.
        /// </summary>
        /// <value>The state of the edge collision.</value>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        CollisionAnalyzer IAgent.EdgeCollisionState
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
        /// Updates the time-step.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        public void TimeStepUpdate()
        {
            throw new NotImplementedException();
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

