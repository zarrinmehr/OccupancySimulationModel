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
using SpatialAnalysis.Geometry;

namespace SpatialAnalysis.CellularEnvironment
{
    /// <summary>
    /// This class analyzes the state of an agent in relation to the barriers. 
    /// </summary>
    public class CollisionAnalyzer
    {
        /// <summary>
        /// Reports if collision has occured
        /// </summary>
        public bool CollisionOccured { get; set; }
        /// <summary>
        /// Reports if the closest point to this location is the end point of an edge or not
        /// </summary>
        public bool IsClosestPointAnEndPoint { get; set; }
        /// <summary>
        /// The barrier
        /// </summary>
        public UVLine Barrrier { get; set; }
        /// <summary>
        /// The closest point on the barrier
        /// </summary>
        public UV ClosestPointOnBarrier { get; set; }
        /// <summary>
        /// the location of agent or target
        /// </summary>
        public UV Location { get; set; }
        /// <summary>
        /// Distance to the barrier
        /// </summary>
        public double DistanceToBarrier { get; set; }
        /// <summary>
        /// The normal vector at the closest point 
        /// </summary>
        public UV NormalizedRepulsion { get; set; }
        /// <summary>
        /// Creats an instance of the CollisionAnalyzer. For internal use only.
        /// </summary>
        /// <param name="point">The agent or target location</param>
        /// <param name="cell">The cell to which the point belongs</param>
        /// <param name="cellularFloor">The CellularFloorBaseGeometry which includes the agent or target</param>
        /// <param name="barrierType">Barrier type</param>
        internal CollisionAnalyzer(UV point, Cell cell, CellularFloorBaseGeometry cellularFloor, BarrierType barrierType)
        {
            this.CollisionOccured = false;
            this.Location = point;
            Index index = cellularFloor.FindIndex(cell);
            int I = index.I + 2;
            //int I = index.I + 1;
            I = Math.Min(I, cellularFloor.GridWidth - 1);
            int J = index.J + 2;
            //int J = index.J + 1;
            J = Math.Min(J, cellularFloor.GridHeight - 1);
            index.I = Math.Max(0, index.I - 1);
            index.J = Math.Max(0, index.J - 1);
            double DistanceSquared = double.PositiveInfinity;
            for (int i = index.I; i <= I; i++)
            {
                for (int j = index.J; j <= J; j++)
                {
                    switch (barrierType)
                    {
                        case BarrierType.Visual:
                            if (cellularFloor.Cells[i, j].VisualOverlapState == OverlapState.Outside)
                            {
                                foreach (int item in cellularFloor.Cells[i, j].VisualBarrierEdgeIndices)
                                {
                                    double d = point.GetDistanceSquared(cellularFloor.VisualBarrierEdges[item]);
                                    if (d < DistanceSquared)
                                    {
                                        DistanceSquared = d;
                                        Barrrier = cellularFloor.VisualBarrierEdges[item];
                                    }
                                }
                            }
                            break;
                        case BarrierType.Physical:
                            if (cellularFloor.Cells[i, j].PhysicalOverlapState != OverlapState.Outside)
                            {
                                foreach (int item in cellularFloor.Cells[i, j].PhysicalBarrierEdgeIndices)
                                {
                                    double d = point.GetDistanceSquared(cellularFloor.PhysicalBarrierEdges[item]);
                                    if (d < DistanceSquared)
                                    {
                                        DistanceSquared = d;
                                        Barrrier = cellularFloor.PhysicalBarrierEdges[item];
                                    }
                                }
                            }
                            break;
                        case BarrierType.Field:
                            if (cellularFloor.Cells[i, j].BarrierBufferOverlapState != OverlapState.Inside)
                            {
                                foreach (int item in cellularFloor.Cells[i, j].FieldBarrierEdgeIndices)
                                {
                                    double d = point.GetDistanceSquared(cellularFloor.FieldBarrierEdges[item]);
                                    if (d < DistanceSquared)
                                    {
                                        DistanceSquared = d;
                                        Barrrier = cellularFloor.FieldBarrierEdges[item];
                                    }
                                }
                            }
                            break;
                    }
                }
            }
            if (!double.IsPositiveInfinity(DistanceSquared)
                && this.Barrrier != null)
            {
                bool isEndpoint = false;
                this.ClosestPointOnBarrier = point.GetClosestPoint(this.Barrrier, ref isEndpoint); 
                this.IsClosestPointAnEndPoint = isEndpoint;
                this.NormalizedRepulsion = this.Location - this.ClosestPointOnBarrier;
                this.DistanceToBarrier = this.NormalizedRepulsion.GetLength();
                this.NormalizedRepulsion /= this.DistanceToBarrier;
            }
        }
        /// <summary>
        /// Creats an instance of the CollisionAnalyzer.
        /// </summary>
        /// <param name="point">The agent or target location</param>
        /// <param name="cellularFloor">The CellularFloorBaseGeometry which includes the agent or target</param>
        /// <param name="barrierType">Barrier type</param>
        /// <returns>An instance of the CollisionAnalyzer which can be null</returns>
        public static CollisionAnalyzer GetCollidingEdge(UV point, CellularFloorBaseGeometry cellularFloor, BarrierType barrierType)
        {
            Cell cell = cellularFloor.FindCell(point);
            if (cell == null)
            {
                return null;
            }
            switch (barrierType)
            {
                case BarrierType.Visual:
                    if (cell.VisualOverlapState == OverlapState.Inside)
                    {
                        return null;
                    }
                    break;
                case BarrierType.Physical:
                    if (cell.PhysicalOverlapState == OverlapState.Inside)
                    {
                        return null;
                    }
                    break;
                case BarrierType.Field:
                    if (cell.FieldOverlapState == OverlapState.Outside)
                    {
                        //cell.Visualize(MainDocument.TargetVisualizer, Cell.Size, Cell.Size, 0);
                        return null;
                    }
                    break;
                case BarrierType.BarrierBuffer:
                    return null;
            }
            var colision = new CollisionAnalyzer(point, cell, cellularFloor, barrierType);
            if (colision.Barrrier == null)
            {
                return null;
            }
            return colision;
        }
        //Offsets an edge towards a line
        private static UVLine OffsetLine(UVLine edge, UV location, double offset)
        {
            UV vector = location - edge.Start;
            UV edgeDirection = edge.End - edge.Start;
            edgeDirection.Unitize();
            UV vt = edgeDirection * edgeDirection.DotProduct(vector);
            UV vp = vector - vt;
            vp.Unitize();
            UV start = edge.Start + vp * offset;
            UV end = edge.End + vp * offset;
            return new UVLine(start, end);
        }
        /// <summary>
        /// Solves the ax^2 + bX + c = 0
        /// </summary>
        /// <param name="a">Coefficient of x^2</param>
        /// <param name="b">Coefficient of x</param>
        /// <param name="c">Constant parameter</param>
        /// <returns></returns>
        public static double[] QuadraticSolver(double a, double b, double c)
        {
            double delta = b * b - 4 * a * c;
            if (delta < 0)
            {
                return null;
            }
            double deltaRoot = Math.Sqrt(delta);
            return new double[2] { (-b - deltaRoot) / (2 * a), (-b + deltaRoot) / (2 * a) };
        }
        /// <summary>
        /// Detects the collision that occurs between two states
        /// </summary>
        /// <param name="previous">Previous state</param>
        /// <param name="current">Current state</param>
        /// <param name="buffer">Offset buffer which is the radius of the body</param>
        /// <param name="tolerance">Tolerance set by default to the main document's absolute tolerance factor</param>
        /// <returns>An instance of the collision which can be null</returns>
        public static ICollision GetCollision(CollisionAnalyzer previous, CollisionAnalyzer current, double buffer, double tolerance = OSMDocument.AbsoluteTolerance)
        {
            Trajectory trajectory = new Trajectory(current.Location, previous.Location);
            List<ICollision> collisions = new List<ICollision>();

            UVLine edgeBuffer1 = CollisionAnalyzer.OffsetLine(previous.Barrrier, previous.Location, buffer);
            ICollision collisionWithEdgeBuffer1 = new GetCollisionWithEdge(trajectory, edgeBuffer1);
            collisions.Add(collisionWithEdgeBuffer1);

            ICollision collisionWithPreviousBarrierEndPoint = new GetCollisionWithEndPoint(trajectory, previous.Barrrier.End, buffer);
            collisions.Add(collisionWithPreviousBarrierEndPoint);
            ICollision collisionWithPreviousBarrierStartPoint = new GetCollisionWithEndPoint(trajectory, previous.Barrrier.Start, buffer);
            collisions.Add(collisionWithPreviousBarrierStartPoint);

            if (!previous.Barrrier.Equals(current.Barrrier))
            {
                UVLine edgeBuffer2 = CollisionAnalyzer.OffsetLine(current.Barrrier, previous.Location, buffer);
                ICollision collisionWithEdgeBuffer2 = new GetCollisionWithEdge(trajectory, edgeBuffer2);
                collisions.Add(collisionWithEdgeBuffer2);
                if (current.Barrrier.End != previous.Barrrier.End && current.Barrrier.End != previous.Barrrier.Start)
                {
                    ICollision collisionWithCurrentBarrierEndPoint = new GetCollisionWithEndPoint(trajectory, current.Barrrier.End, buffer);
                    collisions.Add(collisionWithCurrentBarrierEndPoint);
                }
                if (current.Barrrier.Start != previous.Barrrier.End && current.Barrrier.Start != previous.Barrrier.Start)
                {
                    ICollision collisionWithCurrentBarrierStartPoint = new GetCollisionWithEndPoint(trajectory, current.Barrrier.Start, buffer);
                    collisions.Add(collisionWithCurrentBarrierStartPoint);
                }
            }
            if (collisions.Count != 0)
            {
                collisions.Sort((a, b) => a.LengthToCollision.CompareTo(b.LengthToCollision));
                ICollision firstCollision = collisions[0];
                if (!double.IsInfinity(firstCollision.LengthToCollision))
                {
                    //if (double.IsNaN(firstCollision.TimeStepRemainderProportion))
                    //{
                    //    System.Windows.MessageBox.Show(double.NaN.ToString() + " caught!");
                    //    GetCollision(previous, current, buffer, tolerance);
                    //}
                    return firstCollision;
                }
            }
            return null;
        }
    }
    /// <summary>
    /// The trajectory of movement from one point to another
    /// </summary>
    public class Trajectory
    {
        /// <summary>
        /// The trajectory line
        /// </summary>
        public UVLine TrajectoryLine { get; set; }
        /// <summary>
        /// The displacement length
        /// </summary>
        public double TrajectoryLength { get; set; }
        /// <summary>
        /// The displacement direction
        /// </summary>
        public UV TrajectoryNormalizedVector { get; set; }
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="start">Start point</param>
        /// <param name="end">End point</param>
        public Trajectory(UV start, UV end)
        {
            this.TrajectoryLine = new UVLine(start, end);
            this.TrajectoryLength = UV.GetDistanceBetween(start, end);
            this.TrajectoryNormalizedVector = (end - start) / this.TrajectoryLength;
        }
    }
    /// <summary>
    /// Collision data model
    /// </summary>
    public interface ICollision
    {
        /// <summary>
        /// The timestep proportion after collision
        /// </summary>
        double TimeStepRemainderProportion { get; set; }
        /// <summary>
        /// Collision Point
        /// </summary>
        UV CollisionPoint { get; set; }
        /// <summary>
        /// Distance to the collision point
        /// </summary>
        double LengthToCollision { get; set; }
        /// <summary>
        /// Movement trajectory
        /// </summary>
        Trajectory DisplacementTrajectory { get; set; }
        /// <summary>
        /// Calculates the normal vector at the collision point
        /// </summary>
        /// <returns></returns>
        UV GetCollisionNormal();
    }
    /// <summary>
    /// Collision will end points of edges
    /// </summary>
    internal class GetCollisionWithEndPoint : ICollision
    {
        /// <summary>
        /// Movement trajectory
        /// </summary>
        public Trajectory DisplacementTrajectory { get; set; }
        /// <summary>
        /// The timestep proportion after collision
        /// </summary>
        public double TimeStepRemainderProportion { get; set; }
        /// <summary>
        /// Collision Point
        /// </summary>
        public UV CollisionPoint { get; set; }
        /// <summary>
        /// Distance to the collision point
        /// </summary>
        public double LengthToCollision { get; set; }
        /// <summary>
        /// Edge end point
        /// </summary>
        public UV EndPoint { get; set; }
        /// <summary>
        /// Collision buffer
        /// </summary>
        public double Buffer { get; set; }
        /// <summary>
        /// Creates an instance of the collision
        /// </summary>
        /// <param name="displacementTrajectory">Displacement Trajectory</param>
        /// <param name="endPoint">point at the end of an edge</param>
        /// <param name="buffer">Buffer</param>
        public GetCollisionWithEndPoint(Trajectory displacementTrajectory, UV endPoint, double buffer)
        {
            this.DisplacementTrajectory = displacementTrajectory;
            this.Buffer = buffer;
            //this.TrajectoryLine = trajectoryLine;
            this.EndPoint = endPoint;
            //create a vector that connects the start point of the line to the center of the circle
            UV sO = this.EndPoint - this.DisplacementTrajectory.TrajectoryLine.Start;
            double b = -2 * sO.DotProduct(this.DisplacementTrajectory.TrajectoryNormalizedVector);
            double c = sO.GetLengthSquared() - this.Buffer * this.Buffer;
            var results = CollisionAnalyzer.QuadraticSolver(1, b, c);
            if (results == null)
            {
                this.LengthToCollision = double.PositiveInfinity;
            }
            else if (results[0] > this.DisplacementTrajectory.TrajectoryLength)
            {
                this.LengthToCollision = double.PositiveInfinity;
            }
            else
            {
                this.CollisionPoint = this.DisplacementTrajectory.TrajectoryLine.Start + this.DisplacementTrajectory.TrajectoryNormalizedVector * results[1];
                this.TimeStepRemainderProportion = results[1] / this.DisplacementTrajectory.TrajectoryLength;
                this.LengthToCollision = this.DisplacementTrajectory.TrajectoryLength - results[1];
            }
        }
        /// <summary>
        /// Calculates the normal at the collision point
        /// </summary>
        /// <returns>Normal vector</returns>
        public UV GetCollisionNormal()
        {
            UV normal = this.CollisionPoint - this.EndPoint;
            normal.Unitize();
            return normal;
        }
        public override string ToString()
        {
            return string.Format("Length To Collision: {0}; Time-Step Proportion: {1}", this.LengthToCollision.ToString("0.0000000"), this.TimeStepRemainderProportion.ToString("0.0000000"));
        }

    }
    /// <summary>
    /// Collision will an edge
    /// </summary>
    internal class GetCollisionWithEdge : ICollision
    {
        /// <summary>
        /// Movement trajectory
        /// </summary>
        public Trajectory DisplacementTrajectory { get; set; }
        /// <summary>
        /// The timestep proportion after collision
        /// </summary>
        public double TimeStepRemainderProportion { get; set; }
        /// <summary>
        /// Collision Point
        /// </summary>
        public UV CollisionPoint { get; set; }
        /// <summary>
        /// Distance to the collision point
        /// </summary>
        public double LengthToCollision { get; set; }
        /// <summary>
        /// The direction for offset buffer
        /// </summary>
        private UV BufferDirection { get; set; }
        /// <summary>
        /// Creates an instance of the collision
        /// </summary>
        /// <param name="displacementTrajectory">Displacement Trajectory</param>
        /// <param name="bufferEdge">Barrier edge offseted according to buffer</param>
        /// <param name="tolerance">Tolerance by default set to the tolerance absolute value of the main document</param>
        public GetCollisionWithEdge(Trajectory displacementTrajectory, UVLine bufferEdge, double tolerance = OSMDocument.AbsoluteTolerance)
        {
            this.DisplacementTrajectory = displacementTrajectory;
            this.BufferDirection = bufferEdge.End - bufferEdge.Start;
            double? t = this.DisplacementTrajectory.TrajectoryLine.Intersection(bufferEdge, this.DisplacementTrajectory.TrajectoryLength, tolerance);
            if (t == null)
            {
                this.LengthToCollision = double.PositiveInfinity;
                return;
            }

            this.CollisionPoint = this.DisplacementTrajectory.TrajectoryLine.Start + t.Value * this.DisplacementTrajectory.TrajectoryNormalizedVector;
            this.LengthToCollision = DisplacementTrajectory.TrajectoryLength - t.Value;
            this.TimeStepRemainderProportion = t.Value / this.DisplacementTrajectory.TrajectoryLength;
        }
        /// <summary>
        /// Calculates the normal at the collision point
        /// </summary>
        /// <returns>Normal Vector</returns>
        public UV GetCollisionNormal()
        {
            this.BufferDirection.Unitize();
            var Tt = this.BufferDirection * (this.BufferDirection.DotProduct(this.DisplacementTrajectory.TrajectoryNormalizedVector));
            UV normal = Tt - this.DisplacementTrajectory.TrajectoryNormalizedVector;
            normal.Unitize();
            return -1 * normal;
        }

        public override string ToString()
        {
            return string.Format("Length To Collision: {0}; Time-Step Proportion: {1}", this.LengthToCollision.ToString("0.0000000"), this.TimeStepRemainderProportion.ToString("0.0000000"));
        }

    }


}

