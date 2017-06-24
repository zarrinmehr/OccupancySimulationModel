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
using SpatialAnalysis.CellularEnvironment;
using System.Windows;
using System.Threading.Tasks;
using SpatialAnalysis.Data;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Threading;

namespace SpatialAnalysis.IsovistUtility
{
    /// <summary>
    /// Enum EscapeRouteType
    /// </summary>
    public enum EscapeRouteType
    {
        /// <summary>
        /// All of the escape routes will be returned
        /// </summary>
        All=0,
        /// <summary>
        /// The escape routes will be simplified with respect to angle and their weighting factors
        /// </summary>
        WeightedAndSimplified=1,
    }
    /// <summary>
    /// This class includes methods for calculating cellular isovists, cells on the borders of the isovists, calculating scape routs and their simplifications. 
    /// </summary>
    public class CellularIsovistCalculator
    {
        private Index[] _edge {get;set;}
        private HashSet<Index> _temp_edge {get;set;}
        private bool[,] _rayExcludedIndices {get;set;}
        private bool[,] _area {get;set;}
        private HashSet<UV> _edgeEndPoints {get;set;}
        private HashSet<int> _edgeIndices {get;set;}
        private CellularFloor _cellularFloor {get; set;}
        private UV _vantagePoint { get; set; }
        private double _tolerance { get; set; }
        private BarrierType _barrierType { get; set; }
        private Index _vantageIndex { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="CellularIsovistCalculator"/> class.
        /// This constructor is designed to be called in parallel and access to it is only allowed internally. 
        /// </summary>
        /// <param name="isovistVantagePoint">The isovist vantage point.</param>
        /// <param name="typeOfBarrier">The type of barrier.</param>
        /// <param name="cellularFloor">The cellular floor.</param>
        /// <param name="Tolerance">The tolerance.</param>
        internal CellularIsovistCalculator(UV isovistVantagePoint, BarrierType typeOfBarrier, 
            CellularFloor cellularFloor, double Tolerance = OSMDocument.AbsoluteTolerance)
        {
            this._temp_edge = new HashSet<Index>();
            this._rayExcludedIndices = new bool[cellularFloor.GridWidth, cellularFloor.GridHeight];
            this._area = new bool[cellularFloor.GridWidth, cellularFloor.GridHeight];
            this. _edgeEndPoints = new HashSet<UV>();
            this. _edgeIndices = new HashSet<int>();
            this._cellularFloor = cellularFloor;
            this._vantagePoint = isovistVantagePoint;
            this._tolerance = Tolerance;
            this._barrierType = typeOfBarrier;
            this._vantageIndex = cellularFloor.FindIndex(this._vantagePoint);
        }
        private CellularIsovistCalculator(Cell cell, BarrierType typeOfBarrier,
            CellularFloor cellularFloor, double Tolerance = 0.0000001)
        {
            this._temp_edge = new HashSet<Index>();
            this._rayExcludedIndices = new bool[cellularFloor.GridWidth, cellularFloor.GridHeight];
            this._area = new bool[cellularFloor.GridWidth, cellularFloor.GridHeight];
            this._edgeEndPoints = new HashSet<UV>();
            this._edgeIndices = new HashSet<int>();
            this._cellularFloor = cellularFloor;
            this._vantagePoint = cell + new UV(cellularFloor.CellSize / 2, cellularFloor.CellSize / 2);
            this._tolerance = Tolerance;
            this._barrierType = typeOfBarrier;
            this._vantageIndex = cellularFloor.FindIndex(this._vantagePoint);
        }
        
        private void addPointToExclude(UV endPoint)
        {
            if (this._edgeEndPoints.Contains(endPoint))
            {
                return;
            }
            this._edgeEndPoints.Add(endPoint);
            UV direction = endPoint - this._vantagePoint;
            Ray ray = new Ray(endPoint, direction, this._cellularFloor.Origin, this._cellularFloor.CellSize, this._tolerance);
            Index baseIndex = this._cellularFloor.FindIndex(endPoint);
            Index nextIndex = baseIndex.Copy();
            bool extendRay = true;
            //while (nextIndex != null)
            while (extendRay)
            {
                //nextIndex = ray.NextIndex(this._cellularFloor.FindIndex, this._tolerance);
                if (this._cellularFloor.ContainsCell(nextIndex))
                {
                    switch (this._barrierType)
                    {
                        case BarrierType.Visual:
                            switch (this._cellularFloor.Cells[nextIndex.I, nextIndex.J].VisualOverlapState)
                            {
                                case OverlapState.Overlap:
                                    foreach (var item in this._cellularFloor.Cells[nextIndex.I, nextIndex.J].VisualBarrierEdgeIndices)
                                    {
                                        double? distance = ray.DistanceToForIsovist(this._cellularFloor.VisualBarrierEdges[item], this._tolerance);
                                        if (distance != null)
                                        {
                                            if (distance > this._tolerance)
                                            {
                                                extendRay = false;
                                                //nextIndex = null;
                                            }
                                        }
                                    }
                                    //if this cell includes other endpoints ray shooting might be needed.
                                    break;
                                case OverlapState.Inside:
                                    nextIndex = null;
                                    break;
                                case OverlapState.Outside:
                                    this._rayExcludedIndices[nextIndex.I,nextIndex.J]=true;;
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case BarrierType.Physical:
                            switch (this._cellularFloor.Cells[nextIndex.I, nextIndex.J].PhysicalOverlapState)
                            {
                                case OverlapState.Overlap:
                                    foreach (var item in this._cellularFloor.Cells[nextIndex.I, nextIndex.J].PhysicalBarrierEdgeIndices)
                                    {
                                        double? distance = ray.DistanceToForIsovist(this._cellularFloor.PhysicalBarrierEdges[item], this._tolerance);
                                        if (distance != null)
                                        {
                                            if (distance > this._tolerance)
                                            {
                                                extendRay = false;
                                                //nextIndex = null;
                                            }
                                        }
                                    }
                                    break;
                                case OverlapState.Inside:
                                    nextIndex = null;
                                    break;
                                case OverlapState.Outside:
                                    this._rayExcludedIndices[nextIndex.I,nextIndex.J]=true;
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case BarrierType.Field:
                            switch (this._cellularFloor.Cells[nextIndex.I, nextIndex.J].FieldOverlapState)
                            {
                                case OverlapState.Overlap:
                                    foreach (var item in this._cellularFloor.Cells[nextIndex.I, nextIndex.J].FieldBarrierEdgeIndices)
                                    {
                                        double? distance = ray.DistanceToForIsovist(this._cellularFloor.FieldBarrierEdges[item], this._tolerance);
                                        if (distance != null)
                                        {
                                            if (distance > this._tolerance)
                                            {
                                                extendRay = false;
                                                //nextIndex = null;
                                            }
                                        }
                                    }
                                    break;
                                case OverlapState.Inside:
                                    this._rayExcludedIndices[nextIndex.I,nextIndex.J]=true;
                                    break;
                                case OverlapState.Outside:
                                    nextIndex = null;
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case BarrierType.BarrierBuffer:
                            switch (this._cellularFloor.Cells[nextIndex.I, nextIndex.J].BarrierBufferOverlapState)
                            {
                                case OverlapState.Overlap:
                                    foreach (var item in this._cellularFloor.Cells[nextIndex.I, nextIndex.J].BarrierBufferEdgeIndices)
                                    {
                                        double? distance = ray.DistanceToForIsovist(this._cellularFloor.BarrierBufferEdges[item], this._tolerance);
                                        if (distance != null)
                                        {
                                            if (distance > this._tolerance)
                                            {
                                                extendRay = false;
                                                //nextIndex = null;
                                            }
                                        }
                                    }
                                    break;
                                case OverlapState.Inside:
                                    nextIndex = null;
                                    break;
                                case OverlapState.Outside:
                                    this._rayExcludedIndices[nextIndex.I,nextIndex.J]=true;
                                    break;
                                default:
                                    break;
                            }
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    extendRay = false;
                    nextIndex = null;
                }
                if (extendRay)
                {
                    nextIndex = ray.NextIndex(this._cellularFloor.FindIndex, this._tolerance);
                }
            }
            if (nextIndex != null && nextIndex.Equals(baseIndex))
            {
                nextIndex = null;
            }
            if (nextIndex != null)
            {
                //HashSet<UV> rayOrigins = new HashSet<UV>();
                //int n = 0;
                Index startPointIndex = null;
                Index endPointIndex = null;
                switch (this._barrierType)
                {
                    case BarrierType.Visual:
                        foreach (int id in this._cellularFloor.Cells[nextIndex.I,nextIndex.J].VisualBarrierEdgeIndices)
                        {
                            UVLine edge = this._cellularFloor.VisualBarrierEdges[id];
                            startPointIndex = this._cellularFloor.FindIndex(edge.Start);
                            if (startPointIndex.Equals(nextIndex))
                            {
                                //n++;
                                //rayOrigins.Add(edge.Start);
                                this.addPointToExclude(edge.Start);
                            }
                            endPointIndex = this._cellularFloor.FindIndex(edge.End);
                            if (endPointIndex.Equals(nextIndex))
                            {
                                //n++;
                                //rayOrigins.Add(edge.End);
                                this.addPointToExclude(edge.End);
                            }
                        }
                        break;
                    case BarrierType.Physical:
                        foreach (int id in this._cellularFloor.Cells[nextIndex.I, nextIndex.J].PhysicalBarrierEdgeIndices)
                        {
                            UVLine edge = this._cellularFloor.PhysicalBarrierEdges[id];
                            startPointIndex = this._cellularFloor.FindIndex(edge.Start);
                            if (startPointIndex.Equals(nextIndex))
                            {
                                //n++;
                                //rayOrigins.Add(edge.Start);
                                this.addPointToExclude(edge.Start);
                            }
                            endPointIndex = this._cellularFloor.FindIndex(edge.End);
                            if (endPointIndex.Equals(nextIndex))
                            {
                                //n++;
                                //rayOrigins.Add(edge.End);
                                this.addPointToExclude(edge.End);
                            }
                        }
                        break;
                    case BarrierType.Field:
                        foreach (int id in this._cellularFloor.Cells[nextIndex.I, nextIndex.J].FieldBarrierEdgeIndices)
                        {
                            UVLine edge = this._cellularFloor.FieldBarrierEdges[id];
                            startPointIndex = this._cellularFloor.FindIndex(edge.Start);
                            if (startPointIndex.Equals(nextIndex))
                            {
                                //n++;
                                //rayOrigins.Add(edge.Start);
                                this.addPointToExclude(edge.Start);
                            }
                            endPointIndex = this._cellularFloor.FindIndex(edge.End);
                            if (endPointIndex.Equals(nextIndex))
                            {
                                //n++;
                                //rayOrigins.Add(edge.End);
                                this.addPointToExclude(edge.End);
                            }
                        }
                        break;
                    case BarrierType.BarrierBuffer:
                        foreach (int id in this._cellularFloor.Cells[nextIndex.I, nextIndex.J].BarrierBufferEdgeIndices)
                        {
                            UVLine edge = this._cellularFloor.BarrierBufferEdges[id];
                            startPointIndex = this._cellularFloor.FindIndex(edge.Start);
                            if (startPointIndex.Equals(nextIndex))
                            {
                                //n++;
                                //rayOrigins.Add(edge.Start);
                                this.addPointToExclude(edge.Start);
                            }
                            endPointIndex = this._cellularFloor.FindIndex(edge.End);
                            if (endPointIndex.Equals(nextIndex))
                            {
                                //n++;
                                //rayOrigins.Add(edge.End);
                                this.addPointToExclude(edge.End);
                            }
                        }
                        break;
                }
                //foreach (UV item in rayOrigins)
                //{
                //    this.addPointToExclude(item);
                //}
            }
        }
        private void addLineToExclude(int edgeindex)
        {
            switch (this._barrierType)
            {
                case BarrierType.Visual:
                    this.addPointToExclude(this._cellularFloor.VisualBarrierEdges[edgeindex].Start);
                    this.addPointToExclude(this._cellularFloor.VisualBarrierEdges[edgeindex].End);
                    break;
                case BarrierType.Physical:
                    this.addPointToExclude(this._cellularFloor.PhysicalBarrierEdges[edgeindex].Start);
                    this.addPointToExclude(this._cellularFloor.PhysicalBarrierEdges[edgeindex].End);
                    break;
                case BarrierType.Field:
                    this.addPointToExclude(this._cellularFloor.FieldBarrierEdges[edgeindex].Start);
                    this.addPointToExclude(this._cellularFloor.FieldBarrierEdges[edgeindex].End);
                    break;
                case BarrierType.BarrierBuffer:
                    this.addPointToExclude(this._cellularFloor.BarrierBufferEdges[edgeindex].Start);
                    this.addPointToExclude(this._cellularFloor.BarrierBufferEdges[edgeindex].End);
                    break;
                default:
                    break;
            }

        }
        /// <summary>
        /// This function tests to see if a new index (possible) juxtapposed to an existing visible index 
        /// should be added to the visible indices or not.
        /// </summary>
        private void addNextIndex(Index possible, double depthSquared)
        {
            if (!this._area[possible.I,possible.J] && !this._rayExcludedIndices[possible.I,possible.J])
            {
                double distance = UV.GetLengthSquared(this._cellularFloor.Cells[possible.I, possible.J], this._vantagePoint);
                if (distance < depthSquared)
                {
                    switch (this._barrierType)
                    {
                        case BarrierType.Visual:
                            if (this._cellularFloor.Cells[possible.I, possible.J].VisualOverlapState == OverlapState.Overlap)
                            {
                                foreach (int edgeindex in this._cellularFloor.Cells[possible.I, possible.J].VisualBarrierEdgeIndices)
                                {
                                    if (!this._edgeIndices.Contains(edgeindex))
                                    {
                                        this._edgeIndices.Add(edgeindex);
                                        this.addLineToExclude(edgeindex);
                                    }
                                }
                            }
                            if (this._cellularFloor.Cells[possible.I, possible.J].VisualOverlapState == OverlapState.Outside)
                            {
                                this._temp_edge.Add(possible);
                            }
                            break;
                        case BarrierType.Physical:
                            if (this._cellularFloor.Cells[possible.I, possible.J].PhysicalOverlapState == OverlapState.Overlap)
                            {
                                foreach (int edgeindex in this._cellularFloor.Cells[possible.I, possible.J].PhysicalBarrierEdgeIndices)
                                {
                                    if (!this._edgeIndices.Contains(edgeindex))
                                    {
                                        this._edgeIndices.Add(edgeindex);
                                        this.addLineToExclude(edgeindex);
                                    }
                                }
                            }
                            if (this._cellularFloor.Cells[possible.I, possible.J].PhysicalOverlapState == OverlapState.Outside)
                            {
                                this._temp_edge.Add(possible);
                            }
                            break;
                        case BarrierType.Field:
                            if (this._cellularFloor.Cells[possible.I, possible.J].FieldOverlapState == OverlapState.Overlap)
                            {
                                foreach (int edgeindex in this._cellularFloor.Cells[possible.I, possible.J].FieldBarrierEdgeIndices)
                                {
                                    if (!this._edgeIndices.Contains(edgeindex))
                                    {
                                        this._edgeIndices.Add(edgeindex);
                                        this.addLineToExclude(edgeindex);
                                    }
                                }
                            }
                            if (this._cellularFloor.Cells[possible.I, possible.J].FieldOverlapState == OverlapState.Inside)
                            {
                                this._temp_edge.Add(possible);
                            }
                            break;
                        case BarrierType.BarrierBuffer:
                            if (this._cellularFloor.Cells[possible.I, possible.J].BarrierBufferOverlapState == OverlapState.Overlap)
                            {
                                foreach (int edgeindex in this._cellularFloor.Cells[possible.I, possible.J].BarrierBufferEdgeIndices)
                                {
                                    if (!this._edgeIndices.Contains(edgeindex))
                                    {
                                        this._edgeIndices.Add(edgeindex);
                                        this.addLineToExclude(edgeindex);
                                    }
                                }
                            }
                            if (this._cellularFloor.Cells[possible.I, possible.J].BarrierBufferOverlapState == OverlapState.Outside)
                            {
                                this._temp_edge.Add(possible);
                            }
                            break;
                        default:
                            break;
                    }

                }
            }
        }
        /// <summary>
        /// Returns a collection of cells that are completely visible from a vantage point withing a given distance
        /// </summary>
        /// <param name="vantagePoint">Center of Visibility</param>
        /// <param name="depth">Visibility Range</param>
        /// <param name="tolerance">Numerical Tolerance</param>
        /// <returns>Isovist as Collection of Cells</returns>
        private Isovist getIsovist(double depth)
        {
            Cell cell = this._cellularFloor.FindCell(this._vantageIndex);
            if (cell == null)
            {
                return null;
            }
            switch (this._barrierType)
            {
                case BarrierType.Visual:
                    if (cell.VisualOverlapState != OverlapState.Outside)
                    {
                        return null;
                    }
                    break;
                case BarrierType.Physical:
                    if (cell.PhysicalOverlapState != OverlapState.Outside)
                    {
                        return null;
                    }
                    break;
                case BarrierType.Field:
                    if (cell.FieldOverlapState != OverlapState.Inside)
                    {
                        return null;
                    }
                    break;
                case BarrierType.BarrierBuffer:
                    if (cell.BarrierBufferOverlapState != OverlapState.Outside)
                    {
                        return null;
                    }
                    break;
                default:
                    break;
            }
            double depthSquared = depth * depth;
            //creating the first neighbors
            foreach (var item in Index.Neighbors)
            {
                Index neighbor = item + this._vantageIndex;
                if (this._cellularFloor.ContainsCell(neighbor))
                {
                    switch (this._barrierType)
                    {
                        case BarrierType.Visual:
                            if (this._cellularFloor.Cells[neighbor.I, neighbor.J].VisualOverlapState == OverlapState.Outside)
                            {
                                this._temp_edge.Add(neighbor);
                            }
                            else
                            {
                                foreach (int edgeindex in this._cellularFloor.Cells[neighbor.I, neighbor.J].VisualBarrierEdgeIndices)
                                {
                                    if (!this._edgeIndices.Contains(edgeindex))
                                    {
                                        this._edgeIndices.Add(edgeindex);
                                        this.addLineToExclude(edgeindex);
                                    }
                                }
                            }
                            break;
                        case BarrierType.Physical:
                            if (this._cellularFloor.Cells[neighbor.I, neighbor.J].PhysicalOverlapState == OverlapState.Outside)
                            {
                                this._temp_edge.Add(neighbor);
                            }
                            else
                            {
                                foreach (int edgeindex in this._cellularFloor.Cells[neighbor.I, neighbor.J].PhysicalBarrierEdgeIndices)
                                {
                                    if (!this._edgeIndices.Contains(edgeindex))
                                    {
                                        this._edgeIndices.Add(edgeindex);
                                        this.addLineToExclude(edgeindex);
                                    }
                                }
                            }
                            break;
                        case BarrierType.Field:
                            if (this._cellularFloor.Cells[neighbor.I, neighbor.J].FieldOverlapState == OverlapState.Inside)
                            {
                                this._temp_edge.Add(neighbor);
                            }
                            else
                            {
                                foreach (int edgeindex in this._cellularFloor.Cells[neighbor.I, neighbor.J].FieldBarrierEdgeIndices)
                                {
                                    if (!this._edgeIndices.Contains(edgeindex))
                                    {
                                        this._edgeIndices.Add(edgeindex);
                                        this.addLineToExclude(edgeindex);
                                    }
                                }
                            }
                            break;
                        case BarrierType.BarrierBuffer:
                            if (this._cellularFloor.Cells[neighbor.I, neighbor.J].BarrierBufferOverlapState == OverlapState.Outside)
                            {
                                this._temp_edge.Add(neighbor);
                            }
                            else
                            {
                                foreach (int edgeindex in this._cellularFloor.Cells[neighbor.I, neighbor.J].BarrierBufferEdgeIndices)
                                {
                                    if (!this._edgeIndices.Contains(edgeindex))
                                    {
                                        this._edgeIndices.Add(edgeindex);
                                        this.addLineToExclude(edgeindex);
                                    }
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            this._area[this._vantageIndex.I,this._vantageIndex.J]=true;
            foreach (var item in this._temp_edge)
            {
                this._area[item.I, item.J] = true;
            }
            this._edge=this._temp_edge.ToArray();
            this._temp_edge.Clear();
            while (this._edge.Length != 0)
            {
                foreach (Index item in _edge)
                {
                    var _direction = item - this._vantageIndex;
                    if (_direction.I == 0)
                    {
                        // first
                        Index possible = new Index(
                            item.I - 1,
                            item.J + Math.Sign(_direction.J));
                        if (this._cellularFloor.ContainsCell(possible))
                        {
                            this.addNextIndex(possible, depthSquared);
                        }
                        // second
                        possible = new Index(
                            item.I,
                            item.J + Math.Sign(_direction.J));
                        if (this._cellularFloor.ContainsCell(possible))
                        {
                            this.addNextIndex(possible, depthSquared);
                        }
                        // third
                        possible = new Index(
                            item.I + 1,
                            item.J + Math.Sign(_direction.J));
                        if (this._cellularFloor.ContainsCell(possible))
                        {
                            this.addNextIndex(possible, depthSquared);
                        }
                    }
                    else if (_direction.J == 0)
                    {
                        // first
                        Index possible = new Index(
                            item.I + Math.Sign(_direction.I),
                            item.J - 1);
                        if (this._cellularFloor.ContainsCell(possible))
                        {
                            this.addNextIndex(possible, depthSquared);
                        }
                        // second
                        possible = new Index(
                            item.I + Math.Sign(_direction.I),
                            item.J);
                        if (this._cellularFloor.ContainsCell(possible))
                        {
                            this.addNextIndex(possible, depthSquared);
                        }
                        // third
                        possible = new Index(
                            item.I + Math.Sign(_direction.I),
                            item.J + 1);
                        if (this._cellularFloor.ContainsCell(possible))
                        {
                            this.addNextIndex(possible, depthSquared);
                        }
                    }
                    else
                    {
                        // first
                        Index possible = new Index(
                            item.I + Math.Sign(_direction.I),
                            item.J + Math.Sign(_direction.J));
                        if (this._cellularFloor.ContainsCell(possible))
                        {
                            this.addNextIndex(possible, depthSquared);
                        }
                        // second
                        possible = new Index(
                            item.I + Math.Sign(_direction.I),
                            item.J);
                        if (this._cellularFloor.ContainsCell(possible))
                        {
                            this.addNextIndex(possible, depthSquared);
                        }
                        // third
                        possible = new Index(
                            item.I,
                            item.J + Math.Sign(_direction.J));
                        if (this._cellularFloor.ContainsCell(possible))
                        {
                            this.addNextIndex(possible, depthSquared);
                        }
                    }
                }
                //this.edge.Clear();
                //this.edge.UnionWith(this.temp_edge);
                this._edge = this._temp_edge.ToArray();
                foreach (var item in this._temp_edge)
                {
                    this._area[item.I, item.J] = true;
                }
                this._temp_edge.Clear();
            }
            HashSet<int> cells = new HashSet<int>();
            for (int i = 0; i < this._cellularFloor.GridWidth; i++)
            {
                for (int j = 0; j < this._cellularFloor.GridHeight; j++)
                {
                    if (this._area[i,j])
                    {
                        cells.Add(this._cellularFloor.Cells[i, j].ID);
                    }
                }
            }
            var isovist = new Isovist(cell, cells);
            return isovist;
        }
        private HashSet<Cell> getEscapeRoute(double depth)
        {
            Cell cell = this._cellularFloor.FindCell(this._vantageIndex);
            if (cell == null)
            {
                return null;
            }
            switch (this._barrierType)
            {
                case BarrierType.Visual:
                    if (cell.VisualOverlapState != OverlapState.Outside)
                    {
                        return null;
                    }
                    break;
                case BarrierType.Physical:
                    if (cell.PhysicalOverlapState != OverlapState.Outside)
                    {
                        return null;
                    }
                    break;
                case BarrierType.Field:
                    if (cell.FieldOverlapState != OverlapState.Inside)
                    {
                        return null;
                    }
                    break;
                case BarrierType.BarrierBuffer:
                    if (cell.BarrierBufferOverlapState != OverlapState.Outside)
                    {
                        return null;
                    }
                    break;
                default:
                    break;
            }
            double depthSquared = depth * depth;
            //creating the first neighbors
            foreach (var item in Index.Neighbors)
            {
                Index neighbor = item + this._vantageIndex;
                if (this._cellularFloor.ContainsCell(neighbor))
                {
                    switch (this._barrierType)
                    {
                        case BarrierType.Visual:
                            if (this._cellularFloor.Cells[neighbor.I, neighbor.J].VisualOverlapState == OverlapState.Outside)
                            {
                                this._temp_edge.Add(neighbor);
                            }
                            else
                            {
                                foreach (int edgeindex in this._cellularFloor.Cells[neighbor.I, neighbor.J].VisualBarrierEdgeIndices)
                                {
                                    if (!this._edgeIndices.Contains(edgeindex))
                                    {
                                        this._edgeIndices.Add(edgeindex);
                                        this.addLineToExclude(edgeindex);
                                    }
                                }
                            }
                            break;
                        case BarrierType.Physical:
                            if (this._cellularFloor.Cells[neighbor.I, neighbor.J].PhysicalOverlapState == OverlapState.Outside)
                            {
                                this._temp_edge.Add(neighbor);
                            }
                            else
                            {
                                foreach (int edgeindex in this._cellularFloor.Cells[neighbor.I, neighbor.J].PhysicalBarrierEdgeIndices)
                                {
                                    if (!this._edgeIndices.Contains(edgeindex))
                                    {
                                        this._edgeIndices.Add(edgeindex);
                                        this.addLineToExclude(edgeindex);
                                    }
                                }
                            }
                            break;
                        case BarrierType.Field:
                            if (this._cellularFloor.Cells[neighbor.I, neighbor.J].FieldOverlapState == OverlapState.Inside)
                            {
                                this._temp_edge.Add(neighbor);
                            }
                            else
                            {
                                foreach (int edgeindex in this._cellularFloor.Cells[neighbor.I, neighbor.J].FieldBarrierEdgeIndices)
                                {
                                    if (!this._edgeIndices.Contains(edgeindex))
                                    {
                                        this._edgeIndices.Add(edgeindex);
                                        this.addLineToExclude(edgeindex);
                                    }
                                }
                            }
                            break;
                        case BarrierType.BarrierBuffer:
                            if (this._cellularFloor.Cells[neighbor.I, neighbor.J].BarrierBufferOverlapState == OverlapState.Outside)
                            {
                                this._temp_edge.Add(neighbor);
                            }
                            else
                            {
                                foreach (int edgeindex in this._cellularFloor.Cells[neighbor.I, neighbor.J].BarrierBufferEdgeIndices)
                                {
                                    if (!this._edgeIndices.Contains(edgeindex))
                                    {
                                        this._edgeIndices.Add(edgeindex);
                                        this.addLineToExclude(edgeindex);
                                    }
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            this._area[this._vantageIndex.I, this._vantageIndex.J] = true;
            foreach (var item in this._temp_edge)
            {
                this._area[item.I, item.J] = true;
            }
            this._edge = this._temp_edge.ToArray();
            this._temp_edge.Clear();
            while (_edge.Length != 0)
            {
                foreach (Index item in _edge)
                {
                    var _direction = item - this._vantageIndex;
                    if (_direction.I == 0)
                    {
                        // first
                        Index possible = new Index(
                            item.I - 1,
                            item.J + Math.Sign(_direction.J));
                        if (this._cellularFloor.ContainsCell(possible))
                        {
                            this.addNextIndex(possible, depthSquared);
                        }
                        // second
                        possible = new Index(
                            item.I,
                            item.J + Math.Sign(_direction.J));
                        if (this._cellularFloor.ContainsCell(possible))
                        {
                            this.addNextIndex(possible, depthSquared);
                        }
                        // third
                        possible = new Index(
                            item.I + 1,
                            item.J + Math.Sign(_direction.J));
                        if (this._cellularFloor.ContainsCell(possible))
                        {
                            this.addNextIndex(possible, depthSquared);
                        }
                    }
                    else if (_direction.J == 0)
                    {
                        // first
                        Index possible = new Index(
                            item.I + Math.Sign(_direction.I),
                            item.J - 1);
                        if (this._cellularFloor.ContainsCell(possible))
                        {
                            this.addNextIndex(possible, depthSquared);
                        }
                        // second
                        possible = new Index(
                            item.I + Math.Sign(_direction.I),
                            item.J);
                        if (this._cellularFloor.ContainsCell(possible))
                        {
                            this.addNextIndex(possible, depthSquared);
                        }
                        // third
                        possible = new Index(
                            item.I + Math.Sign(_direction.I),
                            item.J + 1);
                        if (this._cellularFloor.ContainsCell(possible))
                        {
                            this.addNextIndex(possible, depthSquared);
                        }
                    }
                    else
                    {
                        // first
                        Index possible = new Index(
                            item.I + Math.Sign(_direction.I),
                            item.J + Math.Sign(_direction.J));
                        if (this._cellularFloor.ContainsCell(possible))
                        {
                            this.addNextIndex(possible, depthSquared);
                        }
                        // second
                        possible = new Index(
                            item.I + Math.Sign(_direction.I),
                            item.J);
                        if (this._cellularFloor.ContainsCell(possible))
                        {
                            this.addNextIndex(possible, depthSquared);
                        }
                        // third
                        possible = new Index(
                            item.I,
                            item.J + Math.Sign(_direction.J));
                        if (this._cellularFloor.ContainsCell(possible))
                        {
                            this.addNextIndex(possible, depthSquared);
                        }
                    }
                }
                //this.edge.Clear();
                //this.edge.UnionWith(this.temp_edge);
                this._edge = this._temp_edge.ToArray();
                foreach (var item in this._edge)
                {
                    this._area[item.I, item.J] = true;
                }
                this._temp_edge.Clear();
            }
            HashSet<Cell> Overlap = new HashSet<Cell>();
            for (int i = 0; i < this._cellularFloor.GridWidth; i++)
            {
                for (int j = 0; j < this._cellularFloor.GridHeight; j++)
                {
                    if (this._rayExcludedIndices[i,j])
                    {
                        foreach (Index neighbor in Index.Neighbors)
                        {
                            int i_ = neighbor.I + i;
                            int j_ = neighbor.J + j;
                            if (this._cellularFloor.ContainsCell(i_,j_))
                            {
                                if (this._area[i_, j_])
                                {
                                    Overlap.Add(this._cellularFloor.Cells[i_, j_]);
                                }
                            }
                        }
                    }
                }
            }
            return Overlap;
        }
        private void calculateIsovist(double depth)
        {
            Cell cell = this._cellularFloor.FindCell(this._vantageIndex);
            if (cell == null)
            {
                return;
            }
            switch (this._barrierType)
            {
                case BarrierType.Visual:
                    if (cell.VisualOverlapState != OverlapState.Outside)
                    {
                        return;
                    }
                    break;
                case BarrierType.Physical:
                    if (cell.PhysicalOverlapState != OverlapState.Outside)
                    {
                        return;
                    }
                    break;
                case BarrierType.Field:
                    if (cell.FieldOverlapState != OverlapState.Inside)
                    {
                        return;
                    }
                    break;
                case BarrierType.BarrierBuffer:
                    if (cell.BarrierBufferOverlapState != OverlapState.Outside)
                    {
                        return;
                    }
                    break;
                default:
                    break;
            }
            double depthSquared = depth * depth;
            //creating the first neighbors
            foreach (var item in Index.Neighbors)
            {
                Index neighbor = item + this._vantageIndex;
                if (this._cellularFloor.ContainsCell(neighbor))
                {
                    switch (this._barrierType)
                    {
                        case BarrierType.Visual:
                            if (this._cellularFloor.Cells[neighbor.I, neighbor.J].VisualOverlapState == OverlapState.Outside)
                            {
                                this._temp_edge.Add(neighbor);
                            }
                            else
                            {
                                foreach (int edgeindex in this._cellularFloor.Cells[neighbor.I, neighbor.J].VisualBarrierEdgeIndices)
                                {
                                    if (!this._edgeIndices.Contains(edgeindex))
                                    {
                                        this._edgeIndices.Add(edgeindex);
                                        this.addLineToExclude(edgeindex);
                                    }
                                }
                            }
                            break;
                        case BarrierType.Physical:
                            if (this._cellularFloor.Cells[neighbor.I, neighbor.J].PhysicalOverlapState == OverlapState.Outside)
                            {
                                this._temp_edge.Add(neighbor);
                            }
                            else
                            {
                                foreach (int edgeindex in this._cellularFloor.Cells[neighbor.I, neighbor.J].PhysicalBarrierEdgeIndices)
                                {
                                    if (!this._edgeIndices.Contains(edgeindex))
                                    {
                                        this._edgeIndices.Add(edgeindex);
                                        this.addLineToExclude(edgeindex);
                                    }
                                }
                            }
                            break;
                        case BarrierType.Field:
                            if (this._cellularFloor.Cells[neighbor.I, neighbor.J].FieldOverlapState == OverlapState.Inside)
                            {
                                this._temp_edge.Add(neighbor);
                            }
                            else
                            {
                                foreach (int edgeindex in this._cellularFloor.Cells[neighbor.I, neighbor.J].FieldBarrierEdgeIndices)
                                {
                                    if (!this._edgeIndices.Contains(edgeindex))
                                    {
                                        this._edgeIndices.Add(edgeindex);
                                        this.addLineToExclude(edgeindex);
                                    }
                                }
                            }
                            break;
                        case BarrierType.BarrierBuffer:
                            if (this._cellularFloor.Cells[neighbor.I, neighbor.J].BarrierBufferOverlapState == OverlapState.Outside)
                            {
                                this._temp_edge.Add(neighbor);
                            }
                            else
                            {
                                foreach (int edgeindex in this._cellularFloor.Cells[neighbor.I, neighbor.J].BarrierBufferEdgeIndices)
                                {
                                    if (!this._edgeIndices.Contains(edgeindex))
                                    {
                                        this._edgeIndices.Add(edgeindex);
                                        this.addLineToExclude(edgeindex);
                                    }
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            this._area[this._vantageIndex.I, this._vantageIndex.J] = true;
            foreach (var item in this._temp_edge)
            {
                this._area[item.I, item.J] = true;
            }
            this._edge = this._temp_edge.ToArray();
            this._temp_edge.Clear();
            while (this._edge.Length != 0)
            {
                foreach (Index item in _edge)
                {
                    var _direction = item - this._vantageIndex;
                    if (_direction.I == 0)
                    {
                        // first
                        Index possible = new Index(
                            item.I - 1,
                            item.J + Math.Sign(_direction.J));
                        if (this._cellularFloor.ContainsCell(possible))
                        {
                            this.addNextIndex(possible, depthSquared);
                        }
                        // second
                        possible = new Index(
                            item.I,
                            item.J + Math.Sign(_direction.J));
                        if (this._cellularFloor.ContainsCell(possible))
                        {
                            this.addNextIndex(possible, depthSquared);
                        }
                        // third
                        possible = new Index(
                            item.I + 1,
                            item.J + Math.Sign(_direction.J));
                        if (this._cellularFloor.ContainsCell(possible))
                        {
                            this.addNextIndex(possible, depthSquared);
                        }
                    }
                    else if (_direction.J == 0)
                    {
                        // first
                        Index possible = new Index(
                            item.I + Math.Sign(_direction.I),
                            item.J - 1);
                        if (this._cellularFloor.ContainsCell(possible))
                        {
                            this.addNextIndex(possible, depthSquared);
                        }
                        // second
                        possible = new Index(
                            item.I + Math.Sign(_direction.I),
                            item.J);
                        if (this._cellularFloor.ContainsCell(possible))
                        {
                            this.addNextIndex(possible, depthSquared);
                        }
                        // third
                        possible = new Index(
                            item.I + Math.Sign(_direction.I),
                            item.J + 1);
                        if (this._cellularFloor.ContainsCell(possible))
                        {
                            this.addNextIndex(possible, depthSquared);
                        }
                    }
                    else
                    {
                        // first
                        Index possible = new Index(
                            item.I + Math.Sign(_direction.I),
                            item.J + Math.Sign(_direction.J));
                        if (this._cellularFloor.ContainsCell(possible))
                        {
                            this.addNextIndex(possible, depthSquared);
                        }
                        // second
                        possible = new Index(
                            item.I + Math.Sign(_direction.I),
                            item.J);
                        if (this._cellularFloor.ContainsCell(possible))
                        {
                            this.addNextIndex(possible, depthSquared);
                        }
                        // third
                        possible = new Index(
                            item.I,
                            item.J + Math.Sign(_direction.J));
                        if (this._cellularFloor.ContainsCell(possible))
                        {
                            this.addNextIndex(possible, depthSquared);
                        }
                    }
                }
                //this.edge.Clear();
                //this.edge.UnionWith(this.temp_edge);
                this._edge = this._temp_edge.ToArray();
                foreach (var item in this._temp_edge)
                {
                    this._area[item.I, item.J] = true;
                }
                this._temp_edge.Clear();
            }
        }
        /// <summary>
        /// Gets the isovist.
        /// </summary>
        /// <param name="isovistVantagePoint">The isovist vantage point.</param>
        /// <param name="depth">The depth of the isovist view.</param>
        /// <param name="typeOfBarrier">The type of barriers.</param>
        /// <param name="cellularFloor">The cellular floor.</param>
        /// <param name="Tolerance">The tolerance.</param>
        /// <returns>Isovist.</returns>
        public static Isovist GetIsovist(UV isovistVantagePoint, double depth, BarrierType typeOfBarrier, 
            CellularFloor cellularFloor, double Tolerance = OSMDocument.AbsoluteTolerance)
        {
            CellularIsovistCalculator isovistCalculator = new CellularIsovistCalculator(isovistVantagePoint, typeOfBarrier, cellularFloor, Tolerance);
            Isovist isovist = isovistCalculator.getIsovist(depth);
            isovistCalculator._cellularFloor = null;
            isovistCalculator._vantagePoint = null;
            isovistCalculator._vantageIndex = null;
            isovistCalculator._temp_edge.Clear();
            isovistCalculator._temp_edge = null;
            isovistCalculator._rayExcludedIndices = null;
            isovistCalculator._edgeEndPoints.Clear();
            isovistCalculator._edgeEndPoints = null;
            //isovistCalculator.edge.Clear();
            isovistCalculator._edge = null;
            isovistCalculator._area = null;
            isovistCalculator._edgeIndices.Clear();
            isovistCalculator._edgeIndices = null;
            isovistCalculator = null;
            return isovist;
        }

        private static object _lock = new object();

        /// <summary>
        /// Gets all escape routes from a vantage point.
        /// </summary>
        /// <param name="isovistVantagePoint">The isovist vantage point.</param>
        /// <param name="depth">The depth of isovist view.</param>
        /// <param name="typeOfBarrier">The type of barriers.</param>
        /// <param name="cellularFloor">The cellular floor.</param>
        /// <param name="Tolerance">The tolerance.</param>
        /// <returns>IsovistEscapeRoutes.</returns>
        public static IsovistEscapeRoutes GetAllEscapeRoute(UV isovistVantagePoint, double depth, 
            BarrierType typeOfBarrier, CellularFloor cellularFloor, double Tolerance = OSMDocument.AbsoluteTolerance)
        {
            CellularIsovistCalculator isovistCalculator = new CellularIsovistCalculator(isovistVantagePoint, typeOfBarrier, cellularFloor, Tolerance);
            var cells = isovistCalculator.getEscapeRoute(depth);
            List<Cell> OtherCells = CellularIsovistCalculator.ExtractIsovistEscapeRoute(cellularFloor, isovistCalculator._area, typeOfBarrier);
            cells.UnionWith(OtherCells);
            IsovistEscapeRoutes isovistEscapeRoutes = new IsovistEscapeRoutes(cells.ToArray(),
                cellularFloor.Cells[isovistCalculator._vantageIndex.I, isovistCalculator._vantageIndex.J]);
            cells.Clear();
            cells = null;
            isovistCalculator._cellularFloor = null;
            isovistCalculator._vantagePoint = null;
            isovistCalculator._vantageIndex = null;
            isovistCalculator._temp_edge.Clear();
            isovistCalculator._temp_edge = null;
            isovistCalculator._rayExcludedIndices = null;
            isovistCalculator._edgeEndPoints.Clear();
            isovistCalculator._edgeEndPoints = null;
            //isovistCalculator.edge.Clear();
            isovistCalculator._edge = null;
            isovistCalculator._area = null;
            isovistCalculator._edgeIndices.Clear();
            isovistCalculator._edgeIndices = null;
            isovistCalculator = null;
            return isovistEscapeRoutes;
        }
        private static bool isIsovistEscapeRoute(int i, int j, CellularFloor cellularFloor, 
            bool[,] collection, BarrierType barriertype)
        {
            bool isEscapeRoute = false;
            bool allneighborsInside = true;
            foreach (Index item in Index.Neighbors)
            {
                int neighbor_I = item.I + i;
                int neighbor_J = item.J + j;
                if (cellularFloor.ContainsCell(neighbor_I, neighbor_J))
                {
                    if (!collection[neighbor_I, neighbor_J])
                    {
                        isEscapeRoute = true;
                    }
                    switch (barriertype)
                    {
                        case BarrierType.Visual:
                            if (cellularFloor.Cells[neighbor_I, neighbor_J].VisualOverlapState != OverlapState.Outside)
                            {
                                allneighborsInside = false;
                            }
                            break;
                        case BarrierType.Physical:
                            if (cellularFloor.Cells[neighbor_I, neighbor_J].PhysicalOverlapState != OverlapState.Outside)
                            {
                                allneighborsInside = false;
                            }
                            break;
                        case BarrierType.Field:
                            if (cellularFloor.Cells[neighbor_I, neighbor_J].FieldOverlapState != OverlapState.Inside)
                            {
                                allneighborsInside = false;
                            }
                            break;
                        case BarrierType.BarrierBuffer:
                            if (cellularFloor.Cells[neighbor_I, neighbor_J].BarrierBufferOverlapState != OverlapState.Outside)
                            {
                                allneighborsInside = false;
                            }
                            break;
                        default:
                            break;
                    }

                }
            }
            return isEscapeRoute && allneighborsInside;
        }

        /// <summary>
        /// Extracts the isovist escape routes.
        /// </summary>
        /// <param name="cellularFloor">The cellular floor.</param>
        /// <param name="collection">A two dimensional array of Booleans representing the cellular floor in which the escape routs are marked true; otherwise false.</param>
        /// <param name="barriertype">The barriertype.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>List&lt;Cell&gt;.</returns>
        public static List<Cell> ExtractIsovistEscapeRoute(CellularFloor cellularFloor, 
            bool[,] collection, BarrierType barriertype, double tolerance = OSMDocument.AbsoluteTolerance)
        {
            var edge = new List<Cell>();
            for (int i = 0; i < cellularFloor.GridWidth; i++)
            {
                for (int j = 0; j < cellularFloor.GridHeight; j++)
                {
                    if (collection[i,j])
                    {
                        if (CellularIsovistCalculator.isIsovistEscapeRoute(i,j, cellularFloor, collection, barriertype))
                        {
                            edge.Add(cellularFloor.Cells[i, j]);
                        }
                    }
                }
            }
            return edge;
        }
        // SimplifiedEscapeRoute step 1
        #region GetWeightedSimplifiedEscapeRoute
        /// <summary>
        /// Gets the escape route which is simplified according to angle and the weighting factors that are assigned to them.
        /// </summary>
        /// <param name="isovistVantagePoint">The isovist vantage point.</param>
        /// <param name="depth">The depth of the isovist view.</param>
        /// <param name="typeOfBarrier">The type of barrier.</param>
        /// <param name="cellularFloor">The cellular floor.</param>
        /// <param name="staticCost">The static cost used to assign weighting factors.</param>
        /// <param name="angleIntercept">The angle intercept.</param>
        /// <param name="Tolerance">The tolerance.</param>
        /// <returns>IsovistEscapeRoutes.</returns>
        public static IsovistEscapeRoutes GetWeightedSimplifiedEscapeRoute(UV isovistVantagePoint, double depth,
            BarrierType typeOfBarrier, CellularFloor cellularFloor, Dictionary<Cell, double> staticCost,
            int angleIntercept, double Tolerance = OSMDocument.AbsoluteTolerance)
        {
            CellularIsovistCalculator isovistCalculator = new CellularIsovistCalculator(isovistVantagePoint, typeOfBarrier, cellularFloor, Tolerance);
            var cells = isovistCalculator.getEscapeRoute(depth);
            List<Cell> OtherCells = CellularIsovistCalculator.ExtractIsovistEscapeRoute(cellularFloor, isovistCalculator._area, typeOfBarrier);
            cells.UnionWith(OtherCells);
            
            IsovistEscapeRoutes weightedAndSimplifiedEscapeRoutes = new IsovistEscapeRoutes(cells.ToArray(),
                cellularFloor.FindCell(isovistVantagePoint), angleIntercept, staticCost);
            cells.Clear();
            cells = null;
            isovistCalculator._cellularFloor = null;
            isovistCalculator._vantagePoint = null;
            isovistCalculator._vantageIndex = null;
            isovistCalculator._temp_edge.Clear();
            isovistCalculator._temp_edge = null;
            isovistCalculator._rayExcludedIndices = null;
            isovistCalculator._edgeEndPoints.Clear();
            isovistCalculator._edgeEndPoints = null;
            //isovistCalculator.edge.Clear();
            isovistCalculator._edge = null;
            isovistCalculator._area = null;
            isovistCalculator._edgeIndices.Clear();
            isovistCalculator._edgeIndices = null;
            isovistCalculator = null;
            return weightedAndSimplifiedEscapeRoutes;
        } 
        #endregion
        #region AgentEscapeRoutes
        // step 3
        /// <summary>
        /// Gets the agent escape routes.
        /// </summary>
        /// <param name="vantageCell">The vantage cell.</param>
        /// <param name="isovistDepth">The isovist depth.</param>
        /// <param name="desiredNumber">The desired number of escape routes.</param>
        /// <param name="cellularFloor">The cellular floor.</param>
        /// <param name="staticCost">The static cost used as weighting factors.</param>
        /// <param name="Tolerance">The tolerance.</param>
        /// <returns>AgentEscapeRoutes.</returns>
        public static AgentEscapeRoutes GetAgentEscapeRoutes(Cell vantageCell, 
            double isovistDepth, int desiredNumber,
            CellularFloor cellularFloor, Dictionary<Cell, double> staticCost, double Tolerance = OSMDocument.AbsoluteTolerance)
        {
            CellularIsovistCalculator isovistCalculator = new CellularIsovistCalculator(vantageCell, BarrierType.BarrierBuffer, cellularFloor, Tolerance);
            var cells = isovistCalculator.getEscapeRoute(isovistDepth);
            
            List<Cell> OtherCells = CellularIsovistCalculator.ExtractIsovistEscapeRoute(cellularFloor, isovistCalculator._area, BarrierType.BarrierBuffer);
            cells.UnionWith(OtherCells);
            if (cells.Count == 0)
            {
                cells = null;
                isovistCalculator._cellularFloor = null;
                isovistCalculator._vantagePoint = null;
                isovistCalculator._vantageIndex = null;
                isovistCalculator._temp_edge.Clear();
                isovistCalculator._temp_edge = null;
                isovistCalculator._rayExcludedIndices = null;
                isovistCalculator._edgeEndPoints.Clear();
                isovistCalculator._edgeEndPoints = null;
                //isovistCalculator.edge.Clear();
                isovistCalculator._edge = null;
                isovistCalculator._area = null;
                isovistCalculator._edgeIndices.Clear();
                isovistCalculator._edgeIndices = null;
                isovistCalculator = null;
                return null;
            }
            AgentCellDestination[] destinations = AgentCellDestination.ExtractedEscapeRoute(vantageCell,
                desiredNumber, cells, staticCost);
            AgentEscapeRoutes agentScapeRoutes = new AgentEscapeRoutes(vantageCell, destinations);
            cells.Clear();
            cells = null;
            isovistCalculator._cellularFloor = null;
            isovistCalculator._vantagePoint = null;
            isovistCalculator._vantageIndex = null;
            isovistCalculator._temp_edge.Clear();
            isovistCalculator._temp_edge = null;
            isovistCalculator._rayExcludedIndices = null;
            isovistCalculator._edgeEndPoints.Clear();
            isovistCalculator._edgeEndPoints = null;
            //isovistCalculator.edge.Clear();
            isovistCalculator._edge = null;
            isovistCalculator._area = null;
            isovistCalculator._edgeIndices.Clear();
            isovistCalculator._edgeIndices = null;
            isovistCalculator = null;
            return agentScapeRoutes;
        } 
        #endregion

    }
}

