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
using SpatialAnalysis.Interoperability;
using SpatialAnalysis.IsovistUtility;
using System.Windows;
using System.IO;
using SpatialAnalysis.Geometry;
using SpatialAnalysis.Data;

namespace SpatialAnalysis.CellularEnvironment
{
    /// <summary>
    /// This base class represents a 2D grid that covers the floor
    /// </summary>
    public class CellularFloorBaseGeometry
    {
        /// <summary>
        /// The elevation of the floor used for simulation
        /// </summary>
        public double Elevation { get; set; }
        /// <summary>
        /// The bottom left corner of the simulation area
        /// </summary>
        public UV Origin { get; set; }
        /// <summary>
        /// The upper right corner of the simulation area
        /// </summary>
        public UV TopRight { get; set; }
        /// <summary>
        ///  The right border line of the simulation area
        /// </summary>
        protected UVLine right { get; set; }
        /// <summary>
        /// The left border line of the simulation area
        /// </summary>
        protected UVLine left { get; set; }
        /// <summary>
        /// The top border line of the simulation area
        /// </summary>
        protected UVLine top { get; set; }
        /// <summary>
        /// The bottom border line of the simulation area
        /// </summary>
        protected UVLine bottom { get; set; }
        /// <summary>
        /// A two dimensional array that included all of the cells in the floor
        /// </summary>
        public Cell[,] Cells { get; set; }
        /// <summary>
        /// The edges of all of the visual barriers
        /// </summary>
        public UVLine[] VisualBarrierEdges { get; set; }
        /// <summary>
        /// The edges of all of the physical barriers
        /// </summary>
        public UVLine[] PhysicalBarrierEdges { get; set; }
        /// <summary>
        /// The edges of all of the visual barriers
        /// </summary>
        public UVLine[] FieldBarrierEdges { get; set; }
        /// <summary>
        /// The size (i.e. width and height) of each cell
        /// </summary>
        public double CellSize { get; set; }
        /// <summary>
        /// Number of cells in the hight direction of the cellular floor
        /// </summary>
        public int GridHeight { get; set; }
        /// <summary>
        /// Number of cells in the width direction of the cellular floor
        /// </summary>
        public int GridWidth { get; set; }
        /// <summary>
        /// This dictionary can be used to retrieve  the cell index in the floor
        /// </summary>
        public Dictionary<Cell, Index> CellToIndexGuide { get; set; }
        /// <summary>
        /// Number of the cells that are inside the walkable field
        /// </summary>
        public int NumberOfCellsInField { get; set; }
        /// <summary>
        /// The lower left corner of the walkable field territory inside the cellular floor
        /// </summary>
        public UV Territory_Min { get; set; }
        /// <summary>
        /// The upper right corner of the walkable field territory inside the cellular floor
        /// </summary>
        public UV Territory_Max { get; set; }
        /// <summary>
        /// Field polygons
        /// </summary>
        public BarrierPolygons[] FieldBarriers { get; set; }
        /// <summary>
        /// Visual obstacle polygons
        /// </summary>
        public BarrierPolygons[] VisualBarriers { get; set; }
        /// <summary>
        /// Physical obstacle polygons
        /// </summary>
        public BarrierPolygons[] PhysicalBarriers { get; set; }
        /// <summary>
        /// The length of the cell diagonal
        /// </summary>
        public double cellDiagonalDistance { get; set; }
        /// <summary>
        /// Using this dictionary you can find the barrier index of an visual barrier's edge as well as the start point of that edge in that barrier from its global edge index.
        /// </summary>
        public Dictionary<int, EdgeGlobalAddress> VisualBarrierEdgeAddress { get; set; }
        /// <summary>
        /// Using this dictionary you can find the barrier index of an physical barrier's edge as well as the start point of that edge in that barrier from its global edge index.
        /// </summary>
        public Dictionary<int, EdgeGlobalAddress> PhysicalBarrierEdgeAddress { get; set; }

        public readonly int NumberOfVisualBarriers;
        public readonly int NumberOfPhysicalBarriers;
        public readonly int NumberOfFieldBarriers;
        /// <summary>
        /// Using this dictionary you can find the barrier index of an field edge as well as the start point of that edge in that barrier from its global edge index.
        /// </summary>
        public Dictionary<int, EdgeGlobalAddress> FieldBarrierEdgeAddress { get; set; }
        /// <summary>
        /// An empty constructor to create an instance of CellularFloorBaseGeometry. This is limited to internal use only.
        /// </summary>
        internal CellularFloorBaseGeometry() { }
        /// <summary>
        /// a public constructor to create an instance of CellularFloorBaseGeometry
        /// </summary>
        /// <param name="desiredCellSize">Desired size for cells</param>
        /// <param name="barrierEnvironment">The environment parsed from BIM</param>
        /// <param name="pointOnWalkableArea">A point of the cellular floor where the walking territory should include</param>
        public CellularFloorBaseGeometry(double desiredCellSize, BIM_To_OSM_Base barrierEnvironment, UV pointOnWalkableArea)
        {
            this.CellSize = desiredCellSize;
            this.VisualBarriers = barrierEnvironment.VisualBarriers;
            this.PhysicalBarriers = barrierEnvironment.PhysicalBarriers;
            this.FieldBarriers = barrierEnvironment.FieldBarriers;
            this.loadCellularSize(barrierEnvironment.FloorMinBound, barrierEnvironment.FloorMaxBound);
            this.Elevation = barrierEnvironment.PlanElevation;           

            #region Creating visual, physical, and field barrier lines

            #region creating the edges of the visual barriers and loading their addresses
            this.VisualBarrierEdgeAddress = new Dictionary<int, EdgeGlobalAddress>();
            int numberOfVisualBarrier = 0;
            foreach (BarrierPolygons item in this.VisualBarriers)
            {
                numberOfVisualBarrier += item.BoundaryPoints.Length;
            }
            this.VisualBarrierEdges = new UVLine[numberOfVisualBarrier];
            numberOfVisualBarrier = 0;
            for (int barrierIndex = 0; barrierIndex < this.VisualBarriers.Length; barrierIndex++)
            {
                for (int i = 0; i < this.VisualBarriers[barrierIndex].Length; i++)
                {
                    UVLine barrierEdge = new UVLine(this.VisualBarriers[barrierIndex].PointAt(i), 
                        this.VisualBarriers[barrierIndex].PointAt(this.VisualBarriers[barrierIndex].NextIndex(i)));
                    this.VisualBarrierEdges[numberOfVisualBarrier] = barrierEdge;
                    this.VisualBarrierEdgeAddress.Add(numberOfVisualBarrier, new EdgeGlobalAddress(barrierIndex, i));
                    barrierEdge = null;
                    numberOfVisualBarrier++;
                }
            }
            this.NumberOfVisualBarriers = numberOfVisualBarrier;
            #endregion

            #region creating the edges of the physical barriers and loading their addresses
            this.PhysicalBarrierEdgeAddress = new Dictionary<int, EdgeGlobalAddress>();
            int numberOfPhysicalBarrier = 0;
            foreach (BarrierPolygons item in this.PhysicalBarriers)
            {
                numberOfPhysicalBarrier += item.BoundaryPoints.Length;
            }
            this.PhysicalBarrierEdges = new UVLine[numberOfPhysicalBarrier];
            numberOfPhysicalBarrier = 0;
            for (int barrierIndex = 0; barrierIndex < this.PhysicalBarriers.Length; barrierIndex++)
            {
                for (int i = 0; i < this.PhysicalBarriers[barrierIndex].Length; i++)
                {
                    UVLine barrierEdge = new UVLine(this.PhysicalBarriers[barrierIndex].PointAt(i), 
                        this.PhysicalBarriers[barrierIndex].PointAt(this.PhysicalBarriers[barrierIndex].NextIndex(i)));
                    this.PhysicalBarrierEdges[numberOfPhysicalBarrier] = barrierEdge;
                    this.PhysicalBarrierEdgeAddress.Add(numberOfPhysicalBarrier, new EdgeGlobalAddress(barrierIndex, i));
                    barrierEdge = null;
                    numberOfPhysicalBarrier++;
                }
            }
            this.NumberOfPhysicalBarriers = numberOfPhysicalBarrier;
            #endregion
            
            #region creating the edges of the fields and loading their addresses
            this.FieldBarrierEdgeAddress = new Dictionary<int, EdgeGlobalAddress>();
            int numberOfFieldBarriers = 0;
            foreach (BarrierPolygons item in this.FieldBarriers)
            {
                numberOfFieldBarriers += item.BoundaryPoints.Length;
            }
            this.FieldBarrierEdges = new UVLine[numberOfFieldBarriers];
            numberOfFieldBarriers = 0;
            for (int barrierIndex = 0; barrierIndex < this.FieldBarriers.Length; barrierIndex++)
            {
                for (int i = 0; i < this.FieldBarriers[barrierIndex].Length; i++)
                {
                    UVLine barrierEdge = new UVLine(this.FieldBarriers[barrierIndex].PointAt(i), 
                        this.FieldBarriers[barrierIndex].PointAt(this.FieldBarriers[barrierIndex].NextIndex(i)));
                    this.FieldBarrierEdges[numberOfFieldBarriers] = barrierEdge;
                    this.FieldBarrierEdgeAddress.Add(numberOfFieldBarriers, new EdgeGlobalAddress(barrierIndex, i));
                    barrierEdge = null;
                    numberOfFieldBarriers++;
                }
            }
            this.NumberOfFieldBarriers = numberOfFieldBarriers;
            #endregion

            #endregion

            #region creating the cells
            this.Cells = new Cell[this.GridWidth, this.GridHeight];
            //this._cells = new Cell[this.GridWidth* this.GridHeight];
            this.CellToIndexGuide = new Dictionary<Cell, Index>();
            int id = 0;
            for (int i = 0; i < this.GridWidth; i++)
            {
                for (int j = 0; j < this.GridHeight; j++)
                {
                    this.Cells[i, j] = new Cell(this.Origin + new UV(i * this.CellSize, j * this.CellSize), id, i, j);
                    this.CellToIndexGuide.Add(this.Cells[i, j], new Index(i, j));
                    //this._cells[id] = this.Cells[i, j];
                    id++;
                }
            }
            this.cellDiagonalDistance = Math.Sqrt(this.CellSize * this.CellSize + this.CellSize * this.CellSize);
            #endregion

            #region loading the lines into the cells

            #region visual barriers
            for (int i = 0; i < this.VisualBarrierEdges.Length; i++)
            {
                Index[] intersectingCellIndices = this.FindLineIndices(this.VisualBarrierEdges[i]);
                foreach (Index index in intersectingCellIndices)
                {
                    this.FindCell(index).VisualBarrierEdgeIndices.Add(i);
                }
                if (intersectingCellIndices.Length != 0)
                {
                    this.FindCell(intersectingCellIndices[0]).ContainsVisualEdgeEndPoints = true;
                }
                if (intersectingCellIndices.Length != 1)
                {
                    this.FindCell(intersectingCellIndices[intersectingCellIndices.Length-1]).ContainsVisualEdgeEndPoints = true;
                }
            }
            #endregion

            #region physical barriers
            for (int i = 0; i < this.PhysicalBarrierEdges.Length; i++)
            {
                Index[] intersectingCellIndices = this.FindLineIndices(this.PhysicalBarrierEdges[i]);
                foreach (Index index in intersectingCellIndices)
                {
                    this.FindCell(index).PhysicalBarrierEdgeIndices.Add(i);
                }
                if (intersectingCellIndices.Length != 0)
                {
                    this.FindCell(intersectingCellIndices[0]).ContainsPhysicalEdgeEndPoints = true;
                }
                if (intersectingCellIndices.Length != 1)
                {
                    this.FindCell(intersectingCellIndices[intersectingCellIndices.Length - 1]).ContainsPhysicalEdgeEndPoints = true;
                }
            }
            #endregion

            #region loading field Edges
            for (int i = 0; i < this.FieldBarrierEdges.Length; i++)
            {
                Index[] intersectingCellIndices = this.FindLineIndices(this.FieldBarrierEdges[i]);
                foreach (Index index in intersectingCellIndices)
                {
                    this.FindCell(index).FieldBarrierEdgeIndices.Add(i);
                }
                if (intersectingCellIndices.Length != 0)
                {
                    this.FindCell(intersectingCellIndices[0]).ContainsFieldEdgeEndPoints = true;
                }
                if (intersectingCellIndices.Length != 1)
                {
                    this.FindCell(intersectingCellIndices[intersectingCellIndices.Length - 1]).ContainsFieldEdgeEndPoints = true;
                }
            }

            #endregion
            #endregion

            #region loading the overlapping states in cells
            this.LoadFieldOverlappingStates(pointOnWalkableArea);
            this.LoadVisualOverlappingStates(pointOnWalkableArea);
            this.LoadPhysicalOverlappingStates(pointOnWalkableArea);
            #endregion
            
            #region setting min and max of the selected territory
            this.Territory_Max = new UV(double.NegativeInfinity, double.NegativeInfinity);
            this.Territory_Min = new UV(double.PositiveInfinity, double.PositiveInfinity);
            UV d = new UV(this.CellSize, this.CellSize);
            foreach (Cell cell in this.Cells)
            {
                if (cell.FieldOverlapState != OverlapState.Outside)
                {
                    if (this.Territory_Min.U > cell.U) this.Territory_Min.U = cell.U;
                    if (this.Territory_Min.V > cell.V) this.Territory_Min.V = cell.V;
                    var p = cell + d;
                    if (this.Territory_Max.U < p.U) this.Territory_Max.U = p.U;
                    if (this.Territory_Max.V < p.V) this.Territory_Max.V = p.V;
                }
            }
            d = null;
            #endregion
        }
        /// <summary>
        /// Loads the cell size
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        private void loadCellularSize(UV min, UV max)
        {
            double xMin = min.U;
            double xMax = max.U;
            double yMin = min.V;
            double yMax = max.V;
            foreach (BarrierPolygons barrier in this.PhysicalBarriers)
            {
                foreach (UV point in barrier.BoundaryPoints)
                {
                    xMin = (point.U < xMin) ? point.U : xMin;
                    xMax = (point.U > xMax) ? point.U : xMax;
                    yMin = (point.V < yMin) ? point.V : yMin;
                    yMax = (point.V > yMax) ? point.V : yMax;
                }
            }
            xMin -= this.CellSize / 2;
            yMin -= this.CellSize / 2;
            xMax += this.CellSize / 2;
            yMax += this.CellSize / 2;
            double w = xMax - xMin;
            double h = yMax - yMin;
            this.GridWidth = (int)(w / this.CellSize) + 1;
            this.GridHeight = (int)(h / this.CellSize) + 1;
            xMax = this.CellSize * this.GridWidth +xMin;
            yMax = this.CellSize * this.GridHeight +yMin;
            this.Origin = new UV(xMin, yMin);
            this.TopRight = new UV(xMax, yMax);
            this.top = new UVLine(new UV(xMin, yMax), new UV(xMax, yMax));
            this.bottom = new UVLine(new UV(xMin, yMin), new UV(xMax, yMin));
            this.left = new UVLine(new UV(xMin, yMin), new UV(xMin, yMax));
            this.right = new UVLine(new UV(xMax, yMin), new UV(xMax, yMax));
        }

        #region finding cells and indices interchangeably
        /// <summary>
        /// Find a cell based on its ID
        /// </summary>
        /// <param name="id">Cell ID</param>
        /// <returns>Found Cell</returns>
        public Cell FindCell(int id)
        {
            int i = (int)(id / this.GridHeight);
            int j = id - i * this.GridHeight;
            return this.Cells[i, j];
        }
        /// <summary>
        /// Finds a cell index based on a cell ID
        /// </summary>
        /// <param name="id">Cell ID</param>
        /// <returns>Cell Index</returns>
        public Index FindIndex(int id)
        {
            int i = (int)(id / this.GridHeight);
            int j = id - i * this.GridHeight;
            return new Index(i, j);
        }
        /// <summary>
        /// Finds a cell that contains a given point
        /// </summary>
        /// <param name="p">A point</param>
        /// <returns>The containing cell if found</returns>
        public Cell FindCell(UV p)
        {
            if (p.U<this.Origin.U || p.V<this.Origin.V || p.U>this.TopRight.U || p.V>this.TopRight.V)
            {
                return null;
            }
            double i = (p.U - this.Origin.U) / this.CellSize;
            int I = (int)i;
            double j = (p.V - this.Origin.V) / this.CellSize;
            int J = (int)j;
            if (I>=0 && i<this.GridWidth && J>=0 && j<this.GridHeight)
            {
                return this.Cells[I, J];
            }
            return null;
        }
        /// <summary>
        /// Find a cell based a given index
        /// </summary>
        /// <param name="index">A given index</param>
        /// <returns>The corresponding cell if found</returns>
        public Cell FindCell(Index index)
        {
            if (index.I >= this.GridWidth || index.I < 0)
            {
                return null;
            }
            if (index.J >= this.GridHeight || index.J < 0)
            {
                return null;
            }
            return this.Cells[index.I, index.J];
        }
        /// <summary>
        /// Finds the index of the cell that contains a given point
        /// </summary>
        /// <param name="p">A given point</param>
        /// <returns>The index of the containing cell if found</returns>
        public Index FindIndex(UV p)
        {
            double i = (p.U - this.Origin.U) / this.CellSize;
            int I = (int)i;
            double j = (p.V - this.Origin.V) / this.CellSize;
            int J = (int)j;
            if (J == this.GridHeight)
            {
                J -= 1;
            }
            if (I == this.GridWidth)
            {
                I -= 1;
            }
            return new Index(I, J);
        }
        /// <summary>
        /// Returns the index of a given cell
        /// </summary>
        /// <param name="cell">A given cell</param>
        /// <returns>The index of the cell</returns>
        public Index FindIndex(Cell cell)
        {
            /*
            Index index;
            this.CellToIndexGuide.TryGetValue(cell, out index);
            */
            //double i = (double)(.5 + (cell.Origin.U - this.Origin.U) / this.WidthOfCells);
            //int I = (int)i;
            //double j = (double)(.5 + (cell.Origin.V - this.Origin.V) / this.HeightOfCells);
            //int J = (int)j;
            //Index index = new Index(I, J);
            return cell.CellToIndex.Copy();
        }
        /// <summary>
        /// Finds a cell that is translated from a given index
        /// </summary>
        /// <param name="baseIndex">The original cell location</param>
        /// <param name="translation">Translation Index</param>
        /// <returns>A translated cell if found</returns>
        public Cell RelativeIndex(Index baseIndex, Index translation)
        {
            int i = baseIndex.I + translation.I;
            int j = baseIndex.J + translation.J;
            if (i > -1 && i < this.GridWidth && j > -1 && j < this.GridHeight)
            {
                return this.Cells[i, j];
            }
            return null;
        }
        /// <summary>
        /// Finds a cell translated from a given cell
        /// </summary>
        /// <param name="cell">Original cell</param>
        /// <param name="translation">Translation Index</param>
        /// <returns>A translated cell if it exists</returns>
        public Cell RelativeIndex(Cell cell, Index translation)
        {
            Index baseIndex = this.FindIndex(cell);
            int i = baseIndex.I + translation.I;
            int j = baseIndex.J + translation.J;
            if (i > -1 && i < this.GridWidth && j > -1 && j < this.GridHeight)
            {
                return this.Cells[i, j];
            }
            return null;
        }
        /// <summary>
        /// Reports if a cell with the given index exists
        /// </summary>
        /// <param name="index">Index to find the cell for</param>
        /// <returns>True when exists, false when does not exist</returns>
        public bool ContainsCell(Index index)
        {
            if (index.I>=0 && index.I<this.GridWidth && index.J>=0 && index.J<this.GridHeight)
            {
                return true;
            }
            return false;
        }
        /// <summary>
        ///  Reports if a cell with given index exists 
        /// </summary>
        /// <param name="i">I index of the cell</param>
        /// <param name="j">J index of a cell</param>
        /// <returns>True when exists, false when does not exist</returns>
        public bool ContainsCell(int i, int j)
        {
            if (i >= 0 && i < this.GridWidth && j >= 0 && j < this.GridHeight)
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// Reports if a cell includes a point
        /// </summary>
        /// <param name="cell">The cell that is subject to inclusion test</param>
        /// <param name="point">The point</param>
        /// <returns>True when contains, false when does not contain</returns>
        public bool CellIncludesPoint(Cell cell, UV point)
        {
            double u = point.U - cell.U;
            double v = point.V - cell.V;
            return u >= 0 && u < this.CellSize && v >= 0 && v < this.CellSize;
        }
        /// <summary>
        /// Reports if a cell with the given index includes a point
        /// </summary>
        /// <param name="index">The index of t he cell that is subject to inclusion test</param>
        /// <param name="point">The point</param>
        /// <returns>True when contains, false when does not contain</returns>
        public bool CellIncludesPoint(Index index, UV point)
        {
            double u = point.U - this.Cells[index.I,index.J].U;
            double v = point.V - this.Cells[index.I, index.J].V;
            return u >= 0 && u < this.CellSize && v >= 0 && v < this.CellSize;
        }
        #endregion

        #region Ray tracing
        /// <summary>
        /// Find the intersection result of a ray
        /// </summary>
        /// <param name="origin">Origin of the ray</param>
        /// <param name="direction">Direction of  the ray</param>
        /// <param name="barrierType">Type of intersecting barriers</param>
        /// <param name="tolerance">Tolerance which by default is set to the absolute tolerance of the main document</param>
        /// <returns>An instance of RayIntersectionResult</returns>
        public RayIntersectionResult RayIntersection(UV origin, UV direction, BarrierType barrierType, double tolerance = OSMDocument.AbsoluteTolerance)
        {
            Index originIndex = this.FindIndex(origin);
            if (!this.ContainsCell(originIndex))
            {
                return null;
            }
            UV hitPoint = null;
            UV _hitPoint = null;
            UVLine hitEdge = null;
            int edgeIndex=-1;
            double min = double.PositiveInfinity;
            Ray ray = new Ray(origin, direction, this.Origin, this.CellSize, tolerance);
            Index nextIndex = originIndex.Copy();
            while (min == double.PositiveInfinity)
            {
                nextIndex = ray.NextIndex(this.FindIndex, tolerance);
                if (this.ContainsCell(nextIndex))
                {
                    switch (barrierType)
                    {
                        case BarrierType.Visual:
                            if (this.Cells[nextIndex.I, nextIndex.J].VisualOverlapState == OverlapState.Overlap)
                            {
                                foreach (int item in this.Cells[nextIndex.I, nextIndex.J].VisualBarrierEdgeIndices)
                                {
                                    double? distance = ray.DistanceTo(this.VisualBarrierEdges[item], ref hitPoint, tolerance);
                                    if (distance != null)
                                    {
                                        if (min>distance.Value && distance > tolerance )
                                        {
                                            _hitPoint = hitPoint;
                                            hitEdge = this.VisualBarrierEdges[item];
                                            edgeIndex = item;
                                            min = distance.Value;
                                        }
                                    }
                                }
                            }
                            break;
                        case BarrierType.Field:
                            if (this.Cells[nextIndex.I, nextIndex.J].FieldOverlapState == OverlapState.Overlap)
                            {
                                foreach (int item in this.Cells[nextIndex.I, nextIndex.J].FieldBarrierEdgeIndices)
                                {
                                    double? distance = ray.DistanceTo(this.FieldBarrierEdges[item], ref hitPoint, tolerance);
                                    if (distance != null)
                                    {
                                        if (min > distance.Value && distance > tolerance)
                                        {
                                            _hitPoint = hitPoint;
                                            hitEdge = this.FieldBarrierEdges[item];
                                            edgeIndex = item;
                                            min = distance.Value;
                                        }
                                    }
                                }
                            }
                            break;
                        case BarrierType.Physical:
                            if (this.Cells[nextIndex.I, nextIndex.J].PhysicalOverlapState == OverlapState.Overlap)
                            {
                                foreach (int item in this.Cells[nextIndex.I, nextIndex.J].PhysicalBarrierEdgeIndices)
                                {
                                    double? distance = ray.DistanceTo(this.PhysicalBarrierEdges[item], ref hitPoint, tolerance);

                                    if (distance != null)
                                    {
                                        if (min > distance.Value && distance > tolerance)
                                        {
                                            _hitPoint = hitPoint;
                                            hitEdge = this.PhysicalBarrierEdges[item];
                                            edgeIndex = item;
                                            min = distance.Value;
                                        }
                                    } 
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    break;
                }
            }
            RayIntersectionResult result=null;
            if (min != double.PositiveInfinity)
            {
                switch (barrierType)
	            {
		            case BarrierType.Visual:
                        result = new RayIntersectionResult(_hitPoint,edgeIndex, this.VisualBarrierEdgeAddress[edgeIndex], min, barrierType);
                     break;
                    case BarrierType.Physical:
                        result = new RayIntersectionResult(_hitPoint,edgeIndex, this.PhysicalBarrierEdgeAddress[edgeIndex], min, barrierType);
                     break;
                    case BarrierType.Field:
                     result = new RayIntersectionResult(_hitPoint, edgeIndex, this.FieldBarrierEdgeAddress[edgeIndex], min, barrierType);
                     break;
                    default:
                     break;
	            }
            }
            return result;
        }
        /// <summary>
        /// Find the intersection results of a ray with a number of bounces
        /// </summary>
        /// <param name="origin">Origin of the ray</param>
        /// <param name="direction">Direction of the ray</param>
        /// <param name="bounces">Number of bounces</param>
        /// <param name="barrierType">Barrier type</param>
        /// <param name="tolerance">Tolerance which by default is set to the absolute tolerance of the main document</param>
        /// <returns>a list of RayIntersectionResults</returns>
        public List<RayIntersectionResult> RayTrace(UV origin, UV direction, int bounces, BarrierType barrierType, double tolerance = OSMDocument.AbsoluteTolerance)
        {
            List<RayIntersectionResult> results = new List<RayIntersectionResult>();
            int bounceNum = 0;
            var _origin= origin;
            var _direction = direction;
            while (bounceNum < bounces)
            {
                var intersectionResult = this.RayIntersection(_origin, _direction, barrierType, tolerance);
                if (intersectionResult != null)
                {
                    bounceNum++;
                    results.Add(intersectionResult);
                    UVLine edge = null;
                    switch (barrierType)
                    {
                        case BarrierType.Visual:
                            edge = this.VisualBarrierEdges[intersectionResult.EdgeIndexInCellularFloor];
                            break;
                        case BarrierType.Physical:
                            edge = this.PhysicalBarrierEdges[intersectionResult.EdgeIndexInCellularFloor]; 
                            break;
                        case BarrierType.Field:
                            edge = this.FieldBarrierEdges[intersectionResult.EdgeIndexInCellularFloor];
                            break;
                        default:
                            break;
                    }
                    _direction = _direction.GetReflection(edge);
                    _origin = intersectionResult.IntersectingPoint;
                }
                else
                {
                    break;
                }
            }
            return results;
        }
        /// <summary>
        /// Checks to see if two points are visible from each other
        /// </summary>
        public bool Visible(UV origin, UV target, double tolerance = 0.000001f)
        {
            return Ray.Visible(origin, target, BarrierType.Physical, this, tolerance);
        }
        /// <summary>
        /// Checks the visibility between the origins of two cells
        /// </summary>
        public bool CellVisibility(Cell cell1, Cell cell2, double tolerance = 0.000001)
        {
            return Ray.Visible(cell1, cell2, BarrierType.Physical, this, tolerance);
        }

        #endregion

        #region Finding indices of cells that a given curve hits
        /// <summary>
        /// Findes the indices of the cells that a line intersects with
        /// </summary>
        /// <param name="start">Start point of the line</param>
        /// <param name="end">End point of the line</param>
        /// <param name="tolerance">Tolerance which by default is set to the absolute tolerance of the main document</param>
        /// <returns>An array of indices</returns>
        public Index[] FindLineIndices(UV start, UV end, double tolerance = OSMDocument.AbsoluteTolerance)
        {
            List<Index> _indices = new List<Index>();
            UV direction = end - start;
            double length = direction.GetLength();
            direction /= length;
            Ray ray = new Ray(start, direction, this.Origin,
                this.CellSize, tolerance);
            while (ray.Length + tolerance < length)
            {
                var _index = ray.NextIndex(this.FindIndex, tolerance);
                _indices.Add(_index);
            }
            ray = null;
            Index[] _IndexArray = _indices.ToArray();
            _indices.Clear();
            _indices = null;
            return _IndexArray;
        }
        /// <summary>
        /// Findes the indices of the cells that a line intersects with
        /// </summary>
        /// <param name="edge">The line instance</param>
        /// <param name="tolerance">Tolerance which by default is set to the absolute tolerance of the main document</param>
        /// <returns>An array of indices</returns>
        public Index[] FindLineIndices(UVLine edge, double tolerance = OSMDocument.AbsoluteTolerance)
        {
            List<Index> _indices = new List<Index>();
            UV direction = edge.End - edge.Start;
            double length = direction.GetLength();
            direction /= length;
            Ray ray = new Ray(edge.Start, direction, this.Origin,
                this.CellSize, tolerance);
            while (ray.Length + tolerance < length)
            {
                var _index = ray.NextIndex(this.FindIndex, tolerance);
                _indices.Add(_index);
            }
            ray = null;
            Index[] _IndexArray = _indices.ToArray();
            _indices.Clear();
            _indices = null;
            return _IndexArray;
        }
        /// <summary>
        /// Finds the overlapping part of a line clipped by the cellular floor
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        private UVLine LineClip(UVLine ray, double tolerance = OSMDocument.AbsoluteTolerance)
        {
            List<double> values = new List<double>();
            double? u = ray.Intersection(top);
            if (u != null)
            {
                values.Add(u.Value);
            }
            u = ray.Intersection(bottom);
            if (u != null)
            {
                values.Add(u.Value);
            }
            u = ray.Intersection(left);
            if (u != null)
            {
                values.Add(u.Value);
            }
            u = ray.Intersection(right);
            if (u != null)
            {
                values.Add(u.Value);
            }
            if (values.Count == 1)
            {
                if (new Interval(this.Origin.U, this.TopRight.U).Includes(ray.Start.U) && 
                    new Interval(this.Origin.V, this.TopRight.V).Includes(ray.Start.V))
                {
                    return new UVLine(ray.Start, ray.FindPoint(values[0] - tolerance));
                }
                return new UVLine(ray.FindPoint(values[0] + tolerance), ray.End);
            }
            if (values.Count == 2)
            {
                return new UVLine(ray.FindPoint(Math.Min(values[0], values[1]) + tolerance), ray.FindPoint(Math.Max(values[0], values[1]) - tolerance));
            }
            if (values.Count == 0 && ray.Start.U > this.Origin.U && ray.Start.U < this.TopRight.U)
            {
                return ray;
            }
            return null;
        }
        /// <summary>
        /// Finds indices of the cells which intersect with a line that extends outside the floor
        /// </summary>
        /// <param name="ray">A line that represents the ray</param>
        /// <param name="tolerance">Numeric tolerance which by default is set to the absolute tolerance of the main document</param>
        /// <returns>An array of indices</returns>
        public Index[] FindRayIndices(UVLine ray, double tolerance = OSMDocument.AbsoluteTolerance)
        {
            UVLine rayOverlapWithTerritory = this.LineClip(ray, tolerance);
            if (rayOverlapWithTerritory != null)
            {
                return FindLineIndices(rayOverlapWithTerritory);
            }
            return new Index[0];
        }
        #endregion

        #region Determining the overlapping state of cells
        private void LoadFieldOverlappingStates(UV p)
        {
            for (int i = 0; i < this.GridWidth; i++)
            {
                for (int j = 0; j < this.GridHeight; j++)
                {
                    if (this.Cells[i, j].FieldBarrierEdgeIndices.Count != 0)
                    {
                        this.Cells[i, j].FieldOverlapState = OverlapState.Overlap;
                    }
                    else
                    {
                        this.Cells[i, j].FieldOverlapState = OverlapState.Outside;
                    }
                }
            }
            HashSet<Index> overlaps = new HashSet<Index>();
            Index index = this.FindIndex(p);
            HashSet<Index> edge = new HashSet<Index>();
            HashSet<Index> temp_edge = new HashSet<Index>();
            HashSet<Index> area = new HashSet<Index>();
            area.Add(index);
            edge.Add(index);
            while (edge.Count != 0)
            {
                foreach (var item in edge)
                {
                    foreach (var neighbor in Index.Neighbors)
                    {
                        var next = item + neighbor;
                        if (this.ContainsCell(next))
                        {
                            if (!area.Contains(next))
                            {
                                if (this.Cells[next.I, next.J].FieldOverlapState != OverlapState.Overlap)
                                {
                                    temp_edge.Add(next);
                                }
                                else
                                {
                                    overlaps.Add(next);
                                }
                            }
                        }
                    }
                }
                edge.Clear();
                edge.UnionWith(temp_edge);
                area.UnionWith(temp_edge);
                temp_edge.Clear();
            }
            foreach (Cell item in this.Cells)
            {
                item.FieldOverlapState = OverlapState.Outside;
            }

            foreach (var item in area)
            {
                this.Cells[item.I, item.J].FieldOverlapState = OverlapState.Inside;
            }
            this.NumberOfCellsInField = area.Count;
            foreach (var item in overlaps)
            {
                this.Cells[item.I, item.J].FieldOverlapState = OverlapState.Overlap;
            }
            edge.Clear();
            area.Clear();
            temp_edge.Clear();
            overlaps.Clear();
            area = null;
            edge = null;
            temp_edge = null;
            overlaps = null;
        }
        private void LoadVisualOverlappingStates(UV p)
        {
            for (int i = 0; i < this.GridWidth; i++)
            {
                for (int j = 0; j < this.GridHeight; j++)
                {
                    if (this.Cells[i, j].VisualBarrierEdgeIndices.Count != 0)
                    {
                        this.Cells[i, j].VisualOverlapState = OverlapState.Overlap;
                    }
                    else
                    {
                        this.Cells[i, j].VisualOverlapState = OverlapState.Inside;
                    }
                }
            }
            Index index = this.FindIndex(p);
            HashSet<Index> edge = new HashSet<Index>();
            HashSet<Index> temp_edge = new HashSet<Index>();
            HashSet<Index> area = new HashSet<Index>();
            area.Add(index);
            edge.Add(index);
            while (edge.Count != 0)
            {
                foreach (var item in edge)
                {
                    foreach (var neighbor in Index.Neighbors)
                    {
                        var next = item + neighbor;
                        if (this.ContainsCell(next))
                        {
                            if (!area.Contains(next))
                            {
                                if (this.Cells[next.I,next.J].VisualOverlapState != OverlapState.Overlap)
                                {
                                    temp_edge.Add(next);
                                }
                            }
                        }
                    }
                    
                }
                edge.Clear();
                edge.UnionWith(temp_edge);
                area.UnionWith(temp_edge);
                temp_edge.Clear();
            }
            foreach (var item in area)
            {
                this.Cells[item.I, item.J].VisualOverlapState = OverlapState.Outside;
            }
            area = null;
            edge = null;
            temp_edge = null;

        }
        private void LoadPhysicalOverlappingStates(UV p)
        {
            for (int i = 0; i < this.GridWidth; i++)
            {
                for (int j = 0; j < this.GridHeight; j++)
                {
                    if (this.Cells[i, j].PhysicalBarrierEdgeIndices.Count != 0)
                    {
                        this.Cells[i, j].PhysicalOverlapState = OverlapState.Overlap;
                    }
                    else
                    {
                        this.Cells[i, j].PhysicalOverlapState = OverlapState.Inside;
                    }
                }
            }
            Index index = this.FindIndex(p);
            HashSet<Index> edge = new HashSet<Index>();
            HashSet<Index> temp_edge = new HashSet<Index>();
            HashSet<Index> area = new HashSet<Index>();
            area.Add(index);
            edge.Add(index);
            while (edge.Count != 0)
            {
                foreach (var item in edge)
                {
                    foreach (var neighbor in Index.Neighbors)
                    {
                        var next = item + neighbor;
                        if (this.ContainsCell(next))
                        {
                            if (!area.Contains(next))
                            {
                                if (this.Cells[next.I,next.J].PhysicalOverlapState != OverlapState.Overlap)
                                {
                                    temp_edge.Add(next);
                                }
                            }
                        }
                    }
                }
                edge.Clear();
                edge.UnionWith(temp_edge);
                area.UnionWith(temp_edge);
                temp_edge.Clear();
            }
            foreach (var item in area)
            {
                this.Cells[item.I, item.J].PhysicalOverlapState = OverlapState.Outside;
            }
            area = null;
            edge = null;
            temp_edge = null;

        }
        #endregion
        /// <summary>
        /// Converts a given cell to a hash set of lines
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        public HashSet<UVLine> CellToLines(Cell cell)
        {
            Index index = this.FindIndex(cell);
            double u, v;
            u = (index.I == this.GridWidth - 1) ? this.TopRight.U : this.Cells[index.I + 1, index.J].U;
            v = (index.J == this.GridHeight - 1) ? this.TopRight.V : this.Cells[index.I, index.J + 1].V;
            HashSet<UVLine> lines = new HashSet<UVLine>();
            UV pt = new UV(cell.U, v);
            UV pb = new UV(u, cell.V);
            UV p = new UV(u, v);
            lines.Add(new UVLine(cell, pt));
            lines.Add(new UVLine(cell, pb));
            lines.Add(new UVLine(pb, p));
            lines.Add(new UVLine(pt, p));
            return lines;
        }

        public virtual void GetCellBarrierEndPoint(Index cellIndex, List<UV> updatedEndPoints, BarrierType barrierType)
        {
            updatedEndPoints.Clear();
            if (!ContainsCell(cellIndex)) return;
            Cell cell = this.Cells[cellIndex.I, cellIndex.J];
            switch (barrierType)
            {
                case BarrierType.Visual:
                    foreach (var edgeIndex in cell.VisualBarrierEdgeIndices)
                    {
                        UVLine edge = this.VisualBarrierEdges[edgeIndex];
                        if(cell.ContainsPoint(edge.Start,this.CellSize))
                        {
                            updatedEndPoints.Add(edge.Start);
                        }
                        if (cell.ContainsPoint(edge.End, this.CellSize))
                        {
                            updatedEndPoints.Add(edge.End);
                        }
                    }
                    break;
                case BarrierType.Physical:
                    foreach (var edgeIndex in cell.PhysicalBarrierEdgeIndices)
                    {
                        UVLine edge = this.PhysicalBarrierEdges[edgeIndex];
                        if (cell.ContainsPoint(edge.Start, this.CellSize))
                        {
                            updatedEndPoints.Add(edge.Start);
                        }
                        if (cell.ContainsPoint(edge.End, this.CellSize))
                        {
                            updatedEndPoints.Add(edge.End);
                        }
                    }
                    break;
                case BarrierType.Field:
                    foreach (var edgeIndex in cell.FieldBarrierEdgeIndices)
                    {
                        UVLine edge = this.FieldBarrierEdges[edgeIndex];
                        if (cell.ContainsPoint(edge.Start, this.CellSize))
                        {
                            updatedEndPoints.Add(edge.Start);
                        }
                        if (cell.ContainsPoint(edge.End, this.CellSize))
                        {
                            updatedEndPoints.Add(edge.End);
                        }
                    }
                    break;
                case BarrierType.BarrierBuffer:
                    //Not loaded in the base class. Will be overloaded in the derived class.
                    break;
                default:
                    break;
            }
        }

        #region Find cells inside a barrier
        /// <summary>
        /// For any polygon finds the cells that locate inside it or intersect with it
        /// </summary>
        /// <param name="barrier">A polygon</param>
        /// <param name="tolerance">Tolerance factor by default set to the absolute tolerance of the main document</param>
        /// <returns></returns>
        public HashSet<Index> GetIndicesInsideBarrier(BarrierPolygons barrier, double tolerance = OSMDocument.AbsoluteTolerance )
        {
            Dictionary<Index, List<UVLine>> indexToLines =
                new Dictionary<Index, List<UVLine>>(new IndexComparer());
            for (int i = 0; i < barrier.Length; i++)
            {
                UVLine edge = new UVLine(barrier.PointAt(i), barrier.PointAt(barrier.NextIndex(i)));
                var indices = this.FindLineIndices(edge, tolerance);
                foreach (Index item in indices)
                {
                    if (indexToLines.ContainsKey(item))
                    {
                        indexToLines[item].Add(edge);
                    }
                    else
                    {
                        List<UVLine> lines = new List<UVLine>() { edge };
                        indexToLines.Add(item, lines);
                    }
                }
            }
            int iMin = this.GridWidth, iMax = -1, jMin = this.GridHeight, jMax = -1;
            foreach (var item in indexToLines.Keys)
            {
                if (iMin > item.I) iMin = item.I;
                if (iMax < item.I) iMax = item.I;
                if (jMin > item.J) jMin = item.J;
                if (jMax < item.J) jMax = item.J;
            }
            HashSet<Index> inside = new HashSet<Index>();
            for (int i = iMin; i <= iMax; i++)
            {
                List<Index> column = new List<Index>();
                List<int> numbers = new List<int>();
                int n = 0;
                for (int j = jMin; j < jMax; j++)
                {
                    UVLine line = new UVLine(this.Cells[i, j], this.Cells[i, j + 1]);
                    Index index = new Index(i, j);
                    if (indexToLines.ContainsKey(index))
                    {
                        foreach (var item in indexToLines[index])
                        {
                            if (line.Intersects(item, tolerance))
                            {
                                n++;
                            }
                        }
                    }
                    column.Add(index);
                    numbers.Add(n);
                }
                for (int k = 0; k < column.Count; k++)
                {
                    if (numbers[k] % 2 == 1 )
                    {
                        inside.Add(column[k]);
                    }
                }
            }
            inside.UnionWith(indexToLines.Keys);
            return inside;
        }
        /// <summary>
        /// For any polygon finds the cells that only intersect with it
        /// </summary>
        /// <param name="barrier">A polygon</param>
        /// <param name="tolerance">Tolerance factor by default set to the absolute tolerance of the main document</param>
        /// <returns></returns>
        public HashSet<Index> GetIndicesOnBarrier(BarrierPolygons barrier, double tolerance = OSMDocument.AbsoluteTolerance)
        {
            HashSet<Index> indices = new HashSet<Index>();
            for (int i = 0; i < barrier.Length; i++)
            {
                var edgeIndices = this.FindLineIndices(barrier.PointAt(i), barrier.PointAt(barrier.NextIndex(i)), tolerance);
                indices.UnionWith(edgeIndices);
            }
            return indices;
        }

        #endregion

    }
}

