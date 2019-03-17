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
using SpatialAnalysis.Interoperability;
using System.Windows;

namespace SpatialAnalysis.CellularEnvironment
{

    /// <summary>
    /// Represents a Ray and has tracing functionalities
    /// </summary>
    public class Ray
    {
        private int u_Count { get; set; }
        private int v_Count { get; set; }
        /// <summary>
        /// Gets or sets the starting u value at the origin of the ray. 
        /// </summary>
        /// <value>The u start.</value>
        public double U_Start { get; set; }
        /// <summary>
        /// Gets or sets the starting V value at the origin of the ray. 
        /// </summary>
        /// <value>The v start.</value>
        public double V_Start { get; set; }
        /// <summary>
        /// Gets or sets the u increment value.
        /// </summary>
        /// <value>The u increment.</value>
        public double U_Increment { get; set; }
        /// <summary>
        /// Gets or sets the v increment value.
        /// </summary>
        /// <value>The v increment.</value>
        public double V_Increment { get; set; }
        /// <summary>
        /// Gets or sets the origin of the ray.
        /// </summary>
        /// <value>The origin.</value>
        public UV Origin { get; set; }
        /// <summary>
        /// Gets or sets the direction of the ray.
        /// </summary>
        /// <value>The direction.</value>
        public UV Direction { get; set; }
        /// <summary>
        /// Gets or sets the length of the ray.
        /// </summary>
        /// <value>The length.</value>
        public double Length { get; set; }

        public double InitialDirectionLength { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Ray"/> class.
        /// </summary>
        /// <param name="origin">The origin.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="originOfCellularFloor">The origin of cellular floor.</param>
        /// <param name="cellSize">Size of the cell.</param>
        /// <param name="tolerance">The tolerance.</param>
        public Ray(UV origin, UV direction, UV originOfCellularFloor, double cellSize, double tolerance = OSMDocument.AbsoluteTolerance)
        {
            this.Origin = origin;
            this.Direction = direction;
            double directionLengthSqrd = this.Direction.GetLengthSquared();
            if (directionLengthSqrd != 1)
            {
                if (directionLengthSqrd == 0)
                {
                    throw new ArgumentException("Ray direction vector cannot be zero");
                }
                this.InitialDirectionLength = Math.Sqrt(directionLengthSqrd);
                this.Direction /= this.InitialDirectionLength;
            }
            else
            {
                this.InitialDirectionLength = 1;
            }
            this.Length = 0;
            if (this.Direction.U==0)
            {
                this.U_Increment = double.PositiveInfinity;
                this.V_Increment = cellSize;
            }
            else if (this.Direction.V == 0)
            {
                this.V_Increment = double.PositiveInfinity;
                this.U_Increment = cellSize;
            }
            else
            {
                this.U_Increment = Math.Abs(cellSize / this.Direction.U);
                this.V_Increment = Math.Abs(cellSize / this.Direction.V);
            }
            UV p = this.Origin - originOfCellularFloor;
            double a = p.U / cellSize;
            int i = (int)a;
            if (i < 0)
            {
                i = 0;
            }
            double u = p.U - i * cellSize;
            if (this.Direction.U > 0)
            {
                u = cellSize - u;
            }
            this.U_Start = u * this.U_Increment / cellSize;
            if (this.U_Start<tolerance)
            {
                this.U_Start = 0;
            }

            double b = p.V / cellSize;
            int j = (int)b;
            if (j < 0)
            {
                j = 0;
            }
            double v = p.V - j * cellSize;
            if (this.Direction.V > 0)
            {
                v = cellSize - v;
            }
            this.V_Start = v * this.V_Increment / cellSize;
            if (this.V_Start<tolerance)
            {
                this.V_Start = 0;
            }
            this.u_Count = 0;
            this.v_Count = 0;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            int hash = this.Origin.GetHashCode();
            hash = 71 * hash + this.Direction.U.GetHashCode();
            hash = 71 * hash + this.Direction.V.GetHashCode();
            return hash;
        }
        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            Ray ray = obj as Ray;
            if (ray != null && ray.GetHashCode() == this.GetHashCode())
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return string.Format("Origin ({0}); Direction ({1})", this.Origin.ToString(), this.Direction.ToString());
        }
        /// <summary>
        /// Point at the specified length.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <returns>UV.</returns>
        public UV PointAt(double length)
        {
            return this.Origin + length * this.Direction;
        }
        /// <summary>
        /// Gets the point at the current length of the ray.
        /// </summary>
        /// <returns>UV.</returns>
        public UV GetPoint()
        {
            return this.Origin + this.Length * this.Direction;
        }
        /// <summary>
        /// Moves to the next cell in the floor.
        /// </summary>
        /// <param name="tolerance">The tolerance.</param>
        public void Next(double tolerance = OSMDocument.AbsoluteTolerance)
        {
            if (this.Direction.U == 0)
            {
                this.Length = this.v_Count * this.V_Increment + this.V_Start;
                this.v_Count++;
            }
            else if (this.Direction.V == 0)
            {
                this.Length = this.u_Count * this.U_Increment + this.U_Start;
                this.u_Count++;
            }
            else
            {
                double tu = this.u_Count * this.U_Increment + this.U_Start;
                double tv = this.v_Count * this.V_Increment + this.V_Start;
                if (tu + tolerance < tv)
                {
                    this.Length = tu;
                    this.u_Count++;
                }
                else if (tv + tolerance < tu)
                {
                    this.Length = tv;
                    this.v_Count++;
                }
                else
                {
                    this.Length = tv;
                    this.v_Count++;
                    this.u_Count++;
                }
            }
        }
        /// <summary>
        /// Resets the ray for using it again.
        /// </summary>
        public void Reset()
        {
            this.Length = 0;
            this.u_Count = 0;
            this.v_Count = 0;
        }

        /// <summary>
        /// Determines if the ray intersects with a specified line.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns><c>true</c> if intersects, <c>false</c> otherwise.</returns>
        public bool Intersects(UVLine line, double tolerance = 0)
        {
            double vs = this.Direction.CrossProductValue(this.Origin - line.Start);
            double ve = this.Direction.CrossProductValue(this.Origin - line.End);
            if (vs * ve > tolerance)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// returns null if ray does not hit the line. When hit, returns the distance from the hit point
        /// </summary>
        /// <param name="line">Line barrier</param>
        /// <returns>distance of ray origins from the hit point along the ray direction</returns>
        public double? DistanceToForIsovist(UVLine line, double tolerance)
        {
            //checking if the lines intersect at all
            double vs = this.Direction.CrossProductValue(this.Origin - line.Start);
            double ve = this.Direction.CrossProductValue(this.Origin - line.End);
            if (vs * ve > -tolerance)
            {
                return null;
            }
            //finding the intersection
            if (vs==0)
            {
                return this.Origin.DistanceTo(line.Start);
            }
            else if(ve==0)
            {
                return this.Origin.DistanceTo(line.End);
            }
            else
            {
                double _length = line.GetLength();
                vs = Math.Abs(vs);
                ve = Math.Abs(ve);
                double lengthFromStart = _length * vs / (vs + ve);
                UV hitPoint = line.FindPoint(lengthFromStart);
                return this.Origin.DistanceTo(hitPoint);
            }
        }

        /// <summary>
        /// returns null if ray does not hit the line. When hit, returns the distance from the hit point
        /// </summary>
        /// <param name="hitPoint">Hit point on the barrier</param>
        /// <param name="line">Line barrier</param>
        /// <returns>distance of ray origins from the hit point along the ray direction</returns>
        public double? DistanceTo(UVLine line, ref UV hitPoint, double tolerance = 0.0001)
        {
            hitPoint = null;
            double? result = null;
            double vs = this.Direction.CrossProductValue(this.Origin - line.Start);
            double ve = this.Direction.CrossProductValue(this.Origin - line.End);
            if (vs * ve > tolerance)
            {
                return null;
            }
            if (vs == 0)
            {
                result = this.Origin.DistanceTo(line.Start);
                hitPoint = line.Start;
            }
            else if (ve == 0)
            {
                result = this.Origin.DistanceTo(line.End);
                hitPoint = line.End;
            }
            else
            {
                double _length = line.GetLength();
                vs = Math.Abs(vs);
                ve = Math.Abs(ve);
                double lengthFromStart = _length * vs / (vs + ve);
                hitPoint = line.FindPoint(lengthFromStart);
                result = this.Origin.DistanceTo(hitPoint);
            }
            if (result != null)
            {
                var hitDirection = hitPoint - this.Origin;
                if (hitDirection.DotProduct(this.Direction)<0)
                {
                    result = null;
                    hitPoint = null;
                }
            }
            return result;
        }

        /// <summary>
        /// Returns the next index that it passes through.
        /// </summary>
        /// <param name="findIndex">Index of the find.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>Index.</returns>
        public Index NextIndex(Func<UV, Index> findIndex, double tolerance = 0)
        {
            UV p1 = this.GetPoint();
            this.Next(tolerance);
            UV p2 = this.GetPoint();
            return findIndex((p1 + p2) / 2);
        }

        private static bool VisibleDebug(UV origin, UV target, BarrierType barrierType, ref UVLine hitEdge,
            CellularFloorBaseGeometry cellularFloor, I_OSM_To_BIM visualizer, double tolerance = OSMDocument.AbsoluteTolerance)
        {
            hitEdge = null;
            UV direction = target - origin;
            double length = direction.GetLength();
            direction /= length;
            Ray ray = new Ray(origin, direction, cellularFloor.Origin,
                cellularFloor.CellSize, tolerance);
            while (ray.Length + tolerance < length)
            {
                Index index = ray.NextIndex(cellularFloor.FindIndex, tolerance);
                if (cellularFloor.ContainsCell(index))
                {
                    cellularFloor.Cells[index.I, index.J].Visualize(visualizer, cellularFloor.CellSize, 0);
                    switch (barrierType)
                    {
                        case BarrierType.Visual:
                            if (cellularFloor.Cells[index.I, index.J].VisualOverlapState == OverlapState.Overlap)
                            {
                                foreach (int item in cellularFloor.Cells[index.I, index.J].VisualBarrierEdgeIndices)
                                {
                                    double? distance = ray.DistanceToForIsovist(cellularFloor.VisualBarrierEdges[item], tolerance);
                                    if (distance != null && distance.Value + tolerance < length)
                                    {
                                        hitEdge = cellularFloor.VisualBarrierEdges[item];
                                        return false;
                                    }
                                }
                            }
                            break;
                        case BarrierType.Field:
                            if (cellularFloor.Cells[index.I, index.J].FieldOverlapState == OverlapState.Overlap)
                            {
                                foreach (int item in cellularFloor.Cells[index.I, index.J].FieldBarrierEdgeIndices)
                                {
                                    double? distance = ray.DistanceToForIsovist(cellularFloor.VisualBarrierEdges[item], tolerance);
                                    if (distance != null && distance.Value + tolerance < length)
                                    {
                                        hitEdge = cellularFloor.FieldBarrierEdges[item];
                                        return false;
                                    }
                                }
                            }
                            break;
                        case BarrierType.Physical:
                            if (cellularFloor.Cells[index.I, index.J].PhysicalOverlapState == OverlapState.Overlap)
                            {
                                foreach (int item in cellularFloor.Cells[index.I, index.J].PhysicalBarrierEdgeIndices)
                                {
                                    double? distance = ray.DistanceToForIsovist(cellularFloor.PhysicalBarrierEdges[item], tolerance);
                                    if (distance != null && distance.Value + tolerance < length)
                                    {
                                        hitEdge = cellularFloor.PhysicalBarrierEdges[item];
                                        return false;
                                    }
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Checks the existence of a line of sight between two points.
        /// </summary>
        /// <param name="origin">The origin.</param>
        /// <param name="target">The target.</param>
        /// <param name="barrierType">Type of the barrier.</param>
        /// <param name="hitEdge">The hit edge.</param>
        /// <param name="cellularFloor">The cellular floor.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns><c>true</c> if visible, <c>false</c> otherwise.</returns>
        public static bool Visible(UV origin, UV target, BarrierType barrierType, ref UVLine hitEdge,
            CellularFloorBaseGeometry cellularFloor, double tolerance = OSMDocument.AbsoluteTolerance)
        {
            hitEdge = null;
            UV direction = target - origin;
            double length = direction.GetLength();
            direction /= length;
            Ray ray = new Ray(origin, direction, cellularFloor.Origin,
                cellularFloor.CellSize, tolerance);
            while (ray.Length + tolerance < length)
            {
                Index index = ray.NextIndex(cellularFloor.FindIndex, tolerance);
                if (cellularFloor.ContainsCell(index))
                {
                    switch (barrierType)
                    {
                        case BarrierType.Visual:
                            if (cellularFloor.Cells[index.I, index.J].VisualOverlapState == OverlapState.Overlap)
                            {
                                foreach (int item in cellularFloor.Cells[index.I, index.J].VisualBarrierEdgeIndices)
                                {
                                    double? distance = ray.DistanceToForIsovist(cellularFloor.VisualBarrierEdges[item], tolerance);
                                    if (distance != null && distance.Value + tolerance < length)
                                    {
                                        hitEdge = cellularFloor.VisualBarrierEdges[item];
                                        return false;
                                    }
                                }
                            }
                            break;
                        case BarrierType.Physical:
                            if (cellularFloor.Cells[index.I, index.J].PhysicalOverlapState == OverlapState.Overlap)
                            {
                                foreach (int item in cellularFloor.Cells[index.I, index.J].PhysicalBarrierEdgeIndices)
                                {
                                    double? distance = ray.DistanceToForIsovist(cellularFloor.PhysicalBarrierEdges[item], tolerance);
                                    if (distance != null && distance.Value + tolerance < length)
                                    {
                                        hitEdge = cellularFloor.PhysicalBarrierEdges[item];
                                        return false;
                                    }
                                }
                            }
                            break;
                        case BarrierType.Field:
                            if (cellularFloor.Cells[index.I, index.J].FieldOverlapState == OverlapState.Overlap)
                            {
                                foreach (int item in cellularFloor.Cells[index.I, index.J].FieldBarrierEdgeIndices)
                                {
                                    double? distance = ray.DistanceToForIsovist(cellularFloor.FieldBarrierEdges[item], tolerance);
                                    if (distance != null && distance.Value + tolerance < length)
                                    {
                                        hitEdge = cellularFloor.FieldBarrierEdges[item];
                                        return false;
                                    }
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Checks the existence of a line of sight between two points.
        /// </summary>
        /// <param name="origin">The origin.</param>
        /// <param name="target">The target.</param>
        /// <param name="barrierType">Type of the barrier.</param>
        /// <param name="cellularFloor">The cellular floor.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns><c>true</c> if visible, <c>false</c> otherwise.</returns>
        public static bool Visible(UV origin, UV target, BarrierType barrierType, CellularFloorBaseGeometry cellularFloor, double tolerance = OSMDocument.AbsoluteTolerance)
        {
            UV direction = target - origin;
            double length = direction.GetLength();
            direction /= length;
            Ray ray = new Ray(origin, direction, cellularFloor.Origin,
                cellularFloor.CellSize, tolerance);
            while (ray.Length + tolerance < length)
            {
                Index index = ray.NextIndex(cellularFloor.FindIndex, tolerance);
                if (cellularFloor.ContainsCell(index))
                {
                    switch (barrierType)
                    {
                        case BarrierType.Visual:
                            if (cellularFloor.Cells[index.I, index.J].VisualOverlapState == OverlapState.Overlap)
                            {
                                foreach (int item in cellularFloor.Cells[index.I, index.J].VisualBarrierEdgeIndices)
                                {
                                    double? distance = ray.DistanceToForIsovist(cellularFloor.VisualBarrierEdges[item], tolerance);
                                    if (distance != null && distance.Value + tolerance < length)
                                    {
                                        UVLine hitEdge = cellularFloor.VisualBarrierEdges[item];
                                        return false;
                                    }
                                }
                            }
                            break;
                        case BarrierType.Physical:
                            if (cellularFloor.Cells[index.I, index.J].PhysicalOverlapState == OverlapState.Overlap)
                            {
                                foreach (int item in cellularFloor.Cells[index.I, index.J].PhysicalBarrierEdgeIndices)
                                {
                                    double? distance = ray.DistanceToForIsovist(cellularFloor.PhysicalBarrierEdges[item], tolerance);
                                    if (distance != null && distance.Value + tolerance < length)
                                    {
                                        UVLine hitEdge = cellularFloor.PhysicalBarrierEdges[item];
                                        return false;
                                    }
                                }
                            }
                            break;
                        case BarrierType.Field:
                            if (cellularFloor.Cells[index.I, index.J].FieldOverlapState == OverlapState.Overlap)
                            {
                                foreach (int item in cellularFloor.Cells[index.I, index.J].FieldBarrierEdgeIndices)
                                {
                                    double? distance = ray.DistanceToForIsovist(cellularFloor.FieldBarrierEdges[item], tolerance);
                                    if (distance != null && distance.Value + tolerance < length)
                                    {
                                        UVLine hitEdge = cellularFloor.FieldBarrierEdges[item];
                                        return false;
                                    }
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Returns a ordered array of indices that the ray intersects with. These indices include the overlapping length of the ray as their weighting factor.
        /// </summary>
        /// <param name="origin">The origin.</param>
        /// <param name="target">The target.</param>
        /// <param name="cellularFloor">The cellular floor.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>WeightedIndex[].</returns>
        public static WeightedIndex[] LoadWeightingFactors(UV origin, UV target, CellularFloorBaseGeometry cellularFloor, double tolerance = OSMDocument.AbsoluteTolerance)
        {
            UV direction = target - origin;
            double length = direction.GetLength();
            direction /= length;
            Ray ray = new Ray(origin, direction, cellularFloor.Origin,
                cellularFloor.CellSize, tolerance);
            List<WeightedIndex> weightedIndices = new List<WeightedIndex>();
            while (ray.Length + tolerance < length)
            {
                double d1 = ray.Length;
                UV p1 = ray.PointAt(d1);
                ray.Next(tolerance);
                double d2 = ray.Length;
                UV p2 = ray.PointAt(d2);
                UV p = (p1 + p2) / 2;
                double w = d2 - d1;
                if (w > tolerance)
                {
                    double i = (p.U - cellularFloor.Origin.U) / cellularFloor.CellSize;
                    int I = (int)i;
                    double j = (p.V - cellularFloor.Origin.V) / cellularFloor.CellSize;
                    int J = (int)j;
                    WeightedIndex weightedIndex = new WeightedIndex(I, J, w);
                    weightedIndices.Add(weightedIndex);
                }
            }
            return weightedIndices.ToArray();
        }
    }
}

