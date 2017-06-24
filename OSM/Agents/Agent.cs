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
using SpatialAnalysis.Geometry;
using SpatialAnalysis.IsovistUtility;
using System.Collections.Generic;
using System.Windows.Media.Animation;

namespace SpatialAnalysis.Agents
{
    /// <summary>
    /// Interface IAgent from which all agents are inherited
    /// </summary>
    internal interface IAgent
    {
        /// <summary>
        /// Gets or sets the body elasticity.
        /// </summary>
        /// <value>The body elasticity.</value>
        double BodyElasticity { get; set; }
        /// <summary>
        /// Gets or sets the barrier friction.
        /// </summary>
        /// <value>The barrier friction.</value>
        double BarrierFriction { get; set; }
        /// <summary>
        /// Gets or sets the time-step.
        /// </summary>
        /// <value>The time step.</value>
        double TimeStep { get; set; }
        /// <summary>
        /// Gets or sets the barrier repulsion range.
        /// </summary>
        /// <value>The barrier repulsion range.</value>
        double BarrierRepulsionRange { get; set; }
        /// <summary>
        /// Gets or sets the repulsion change rate.
        /// </summary>
        /// <value>The repulsion change rate.</value>
        double RepulsionChangeRate { get; set; }
        /// <summary>
        /// Gets or sets the visibility angle.
        /// </summary>
        /// <value>The visibility angle.</value>
        double VisibilityAngle { get; set; }
        /// <summary>
        /// Gets or sets the visibility cosine factor.
        /// </summary>
        /// <value>The visibility cosine factor.</value>
        double VisibilityCosineFactor { get; set; }
        /// <summary>
        /// Gets or sets the angular velocity.
        /// </summary>
        /// <value>The angular velocity.</value>
        double AngularVelocity { get; set; }
        /// <summary>
        /// Gets or sets the velocity magnitude.
        /// </summary>
        /// <value>The velocity magnitude.</value>
        double VelocityMagnitude { get; set; }
        /// <summary>
        /// Gets or sets the size of the body.
        /// </summary>
        /// <value>The size of the body.</value>
        double BodySize { get; set; }
        /// <summary>
        /// Gets or sets the acceleration magnitude.
        /// </summary>
        /// <value>The acceleration magnitude.</value>
        double AccelerationMagnitude { get; set; }
        /// <summary>
        /// Gets or sets the current state of the agent.
        /// </summary>
        /// <value>The state of the current.</value>
        StateBase CurrentState { get; set; }
        /// <summary>
        /// Gets or sets the total walk time.
        /// </summary>
        /// <value>The total walk time.</value>
        double TotalWalkTime { get; set; }
        /// <summary>
        /// Gets or sets the walked total length.
        /// </summary>
        /// <value>The total length of the walked.</value>
        double TotalWalkedLength { get; set; }
        /// <summary>
        /// Gets or sets the state of the edge collision.
        /// </summary>
        /// <value>The state of the edge collision.</value>
        CollisionAnalyzer EdgeCollisionState { get; set; }
        /// <summary>
        /// Updates the time-step.
        /// </summary>
        void TimeStepUpdate();
        /// <summary>
        /// Loads the repulsion vector's magnitude.
        /// </summary>
        /// <returns>System.Double.</returns>
        double LoadRepulsionMagnitude();

    }
    /// <summary>
    /// Interface ISimulateAgent from which agent simulation and evaluation models are inherited
    /// </summary>
    /// <seealso cref="SpatialAnalysis.Agents.IAgent" />
    internal interface ISimulateAgent : IAgent
    {
        /// <summary>
        /// Simulates the occupancy process.
        /// </summary>
        /// <param name="dataFieldName">Name of the data field to store the simulation results.</param>
        /// <param name="notify">if set to <c>true</c> sends a message when the background thread finishes the simulation.</param>
        void Simulate(string dataFieldName, bool notify);
    }
    /// <summary>
    /// Interface IVisualAgent from which both mandatory and optional visualization agents are inherited
    /// </summary>
    /// <seealso cref="SpatialAnalysis.Agents.IAgent" />
    internal interface IVisualAgent : IAgent
    {
        /// <summary>
        /// Sets the geometry of the animated agent.
        /// </summary>
        void SetGeometry();
        /// <summary>
        /// Walks the initialize.
        /// </summary>
        /// <param name="cellularfloor">The cellularfloor.</param>
        /// <param name="escapeRouts">The escape routs.</param>
        void WalkInit(CellularFloor cellularfloor, Dictionary<Cell, AgentEscapeRoutes> escapeRouts);
        /// <summary>
        /// Stops the animation.
        /// </summary>
        void StopAnimation();
        /// <summary>
        /// Gets or sets the time storyboard.
        /// </summary>
        /// <value>The time storyboard.</value>
        Storyboard TimeStoryboard { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [show visibility cone].
        /// </summary>
        /// <value><c>true</c> if [show visibility cone]; otherwise, <c>false</c>.</value>
        bool ShowVisibilityCone { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [show safety buffer].
        /// </summary>
        /// <value><c>true</c> if [show safety buffer]; otherwise, <c>false</c>.</value>
        bool ShowSafetyBuffer { get; set; }
        /// <summary>
        /// Gets or sets the animation timer.
        /// </summary>
        /// <value>The animation timer.</value>
        double AnimationTimer { get; set; }
    }
}

