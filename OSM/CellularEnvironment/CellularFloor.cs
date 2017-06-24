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
    /// This class represents a 2D grid that covers the floor and can include spatial data
    /// </summary>
    public class CellularFloor : CellularFloorBaseGeometry
    {
        private static object _lock = new object();
        /// <summary>
        /// The data fields that loaded to the floor. Should not be accessed directly.
        /// </summary>
        public Dictionary<string, SpatialDataField> AllSpatialDataFields { get; set; }
        internal static string DistanceFromEdgesOfField = "Distance from Edges of Walkable Field";
        internal static string DistanceFromPhysicalBarriers = "Distance from Physical Barriers";
        internal static string DistanceFromVisualBarriers = "Distance from Visual Barriers";
        /// <summary>
        /// An empty constructor of the CellularFloor for internal use only.
        /// </summary>
        internal CellularFloor():base() { }
        /// <summary>
        /// A public constructor that creates an instance of the CellularFloor 
        /// </summary>
        /// <param name="desiredCellSize">Desired cell size</param>
        /// <param name="barrierEnvironment">The parser of the BIM model</param>
        /// <param name="pointOnWalkableArea">A point on the walkable field which is expected to be on the walkable area</param>
        public CellularFloor(double desiredCellSize, BIM_To_OSM_Base barrierEnvironment, UV pointOnWalkableArea)
        :base(desiredCellSize, barrierEnvironment, pointOnWalkableArea) 
        {
            this.AllSpatialDataFields = new Dictionary<string, SpatialDataField>();

            #region loading distances
            HashSet<Cell> edge = new HashSet<Cell>();
            HashSet<Cell> temp_edge = new HashSet<Cell>();

            #region loading the Distances from field barriers
            Dictionary<Cell, double> fieldBarrierDistance = new Dictionary<Cell, double>();
            foreach (Cell cell in this.Cells)
            {
                double dist = double.PositiveInfinity;
                if (cell.FieldOverlapState != OverlapState.Inside)
                {
                    dist = 0;
                }
                if (cell.FieldOverlapState == OverlapState.Overlap)
                {
                    edge.Add(cell);
                }
                fieldBarrierDistance.Add(cell, dist);
            }
            while (edge.Count > 0)
            {
                foreach (Cell cell in edge)
                {
                    nextEdgesOfCell_field(cell, fieldBarrierDistance, ref temp_edge);
                }
                edge.Clear();
                edge.UnionWith(temp_edge);
                foreach (Cell cell in edge)
                {
                    fieldBarrierDistance[cell] = this.loadDistancesAndIndices_field(cell, fieldBarrierDistance[cell]);
                }
                temp_edge.Clear();
            }
            edge.Clear();
            temp_edge.Clear();
            #endregion

            #region loading the Distances from visual barriers
            Dictionary<Cell, double> visualBarrierDistance = new Dictionary<Cell, double>();
            foreach (Cell cell in this.Cells)
            {
                double dist = double.PositiveInfinity;
                if (cell.VisualOverlapState != OverlapState.Outside)
                {
                    dist = 0;
                }
                if (cell.VisualOverlapState == OverlapState.Overlap)
                {
                    edge.Add(cell);
                }
                visualBarrierDistance.Add(cell, dist);
            }
            while (edge.Count != 0)
            {
                //finding the next edge
                foreach (Cell cell in edge)
                {
                    nextEdgesOfCell_visual(cell, visualBarrierDistance, ref temp_edge);
                }
                edge.Clear();
                edge.UnionWith(temp_edge);
                foreach (Cell cell in edge)
                {
                    visualBarrierDistance[cell] = this.loadDistancesAndIndices_visual(cell, visualBarrierDistance[cell]);
                }
                temp_edge.Clear();
            }

            edge.Clear();
            temp_edge.Clear();
            #endregion

            #region loading the Distances from physical barriers
            Dictionary<Cell, double> physicalBarrierDistance = new Dictionary<Cell, double>();
            foreach (Cell cell in this.Cells)
            {
                double dist = double.PositiveInfinity;
                if (cell.PhysicalOverlapState != OverlapState.Outside)
                {
                    dist = 0;
                }
                if (cell.PhysicalOverlapState == OverlapState.Overlap)
                {
                    edge.Add(cell);
                }
                physicalBarrierDistance.Add(cell, dist);
            }
            while (edge.Count != 0)
            {
                //finding the next edge
                foreach (Cell cell in edge)
                {
                    nextEdgesOfCell_physical(cell, physicalBarrierDistance, ref temp_edge);
                }
                edge.Clear();
                edge.UnionWith(temp_edge);
                foreach (Cell cell in edge)
                {
                    physicalBarrierDistance[cell] = this.loadDistancesAndIndices_physical(cell, physicalBarrierDistance[cell]);
                }
                temp_edge.Clear();
            }

            edge.Clear();
            temp_edge.Clear();
            #endregion

            edge = null;
            temp_edge = null;

            foreach (Cell cell in this.Cells)
            {
                if (cell.FieldOverlapState == OverlapState.Outside)
                {
                    fieldBarrierDistance.Remove(cell);
                    visualBarrierDistance.Remove(cell);
                    physicalBarrierDistance.Remove(cell);
                }
            }
            this.AddSpatialDataField(new SpatialDataField(CellularFloor.DistanceFromEdgesOfField, fieldBarrierDistance, true,true));
            this.AddSpatialDataField(new SpatialDataField(CellularFloor.DistanceFromPhysicalBarriers, physicalBarrierDistance,true,false));
            this.AddSpatialDataField(new SpatialDataField(CellularFloor.DistanceFromVisualBarriers, visualBarrierDistance, true, false));
            #endregion
        }

        #region Methods for loading distances from barriers
        private void nextEdgesOfCell_visual(Cell cell, Dictionary<Cell, double> visualBarrierDistance, ref HashSet<Cell> nextEdges)
        {
            Index cellIndex = this.FindIndex(cell);
            int iMin = (cellIndex.I == 0) ? 0 : cellIndex.I - 1;
            int iMax = (cellIndex.I == this.GridWidth - 1) ? this.GridWidth - 1 : cellIndex.I + 1;
            int jMin = (cellIndex.J == 0) ? 0 : cellIndex.J - 1;
            int jMax = (cellIndex.J == this.GridHeight - 1) ? this.GridHeight - 1 : cellIndex.J + 1;

            for (int i = iMin; i <= iMax; i++)
            {
                for (int j = jMin; j <= jMax; j++)
                {
                    if (visualBarrierDistance[this.Cells[i, j]] > visualBarrierDistance[cell] + this.cellDiagonalDistance)
                    {
                        if (!(i == cellIndex.I && j == cellIndex.J))
                        {
                            nextEdges.Add(this.Cells[i, j]);
                            this.Cells[i, j].VisualBarrierEdgeIndices.UnionWith(cell.VisualBarrierEdgeIndices);
                        }
                    }
                }
            }
        }

        private void nextEdgesOfCell_field(Cell cell, Dictionary<Cell, double> fieldBarrierDistance, ref HashSet<Cell> nextEdges)
        {
            Index cellIndex = this.FindIndex(cell);
            int iMin = (cellIndex.I == 0) ? 0 : cellIndex.I - 1;
            int iMax = (cellIndex.I == this.GridWidth - 1) ? this.GridWidth - 1 : cellIndex.I + 1;
            int jMin = (cellIndex.J == 0) ? 0 : cellIndex.J - 1;
            int jMax = (cellIndex.J == this.GridHeight - 1) ? this.GridHeight - 1 : cellIndex.J + 1;
            for (int i = iMin; i <= iMax; i++)
            {
                for (int j = jMin; j <= jMax; j++)
                {
                    if (fieldBarrierDistance[this.Cells[i, j]] > fieldBarrierDistance[cell] + this.cellDiagonalDistance)
                    {
                        if (!(i == cellIndex.I && j == cellIndex.J))
                        {
                            nextEdges.Add(this.Cells[i, j]);
                            this.Cells[i, j].FieldBarrierEdgeIndices.UnionWith(cell.FieldBarrierEdgeIndices);
                        }
                    }
                }
            }
        }
        private void nextEdgesOfCell_physical(Cell cell, Dictionary<Cell, double> physicalBarrierDistance, ref HashSet<Cell> nextEdges)
        {
            Index cellIndex = this.FindIndex(cell);
            int iMin = (cellIndex.I == 0) ? 0 : cellIndex.I - 1;
            int iMax = (cellIndex.I == this.GridWidth - 1) ? this.GridWidth - 1 : cellIndex.I + 1;
            int jMin = (cellIndex.J == 0) ? 0 : cellIndex.J - 1;
            int jMax = (cellIndex.J == this.GridHeight - 1) ? this.GridHeight - 1 : cellIndex.J + 1;
            for (int i = iMin; i <= iMax; i++)
            {
                for (int j = jMin; j <= jMax; j++)
                {
                    if (physicalBarrierDistance[this.Cells[i, j]] > physicalBarrierDistance[cell] + this.cellDiagonalDistance)
                    {
                        if (!(i == cellIndex.I && j == cellIndex.J))
                        {
                            nextEdges.Add(this.Cells[i, j]);
                            this.Cells[i, j].PhysicalBarrierEdgeIndices.UnionWith(cell.PhysicalBarrierEdgeIndices);
                        }
                    }
                }
            }
        }
        private double loadDistancesAndIndices_visual(Cell cell, double currentDistance)
        {
            int index = -1;
            foreach (int lineIndex in cell.VisualBarrierEdgeIndices)
            {
                double x = cell.ClosestDistance(this.VisualBarrierEdges[lineIndex]);
                if (x < currentDistance)
                {
                    currentDistance = x;
                    index = lineIndex;
                }
            }
            cell.VisualBarrierEdgeIndices.Clear();
            cell.VisualBarrierEdgeIndices.Add(index);
            return currentDistance;
        }
        //LoadDistancesAndIndices_field
        private double loadDistancesAndIndices_field(Cell cell, double currentDistance)
        {
            int index = -1;
            foreach (int lineIndex in cell.FieldBarrierEdgeIndices)
            {
                double x = cell.ClosestDistance(this.FieldBarrierEdges[lineIndex]);
                if (x < currentDistance)
                {
                    currentDistance = x;
                    index = lineIndex;
                }
            }
            cell.FieldBarrierEdgeIndices.Clear();
            cell.FieldBarrierEdgeIndices.Add(index);
            return currentDistance;
        }
        private double loadDistancesAndIndices_physical(Cell cell, double currentDistance)
        {
            int index = -1;
            foreach (int lineIndex in cell.PhysicalBarrierEdgeIndices)
            {
                double x = cell.ClosestDistance(this.PhysicalBarrierEdges[lineIndex]);
                if (x < currentDistance)
                {
                    currentDistance = x;
                    index = lineIndex;
                }
            }
            cell.PhysicalBarrierEdgeIndices.Clear();
            cell.PhysicalBarrierEdgeIndices.Add(index);
            return currentDistance;
        }
        /// <summary>
        /// return an instance of the CollisionAnalyzer to determine the collision state of a given point in relation to a barrier type 
        /// </summary>
        /// <param name="point">A point in the field</param>
        /// <param name="barrierType">The type of barrier</param>
        /// <returns>An instance of CollisionAnalyzer</returns>
        public CollisionAnalyzer GetCollidingEdge(UV point, BarrierType barrierType)
        {
            return CollisionAnalyzer.GetCollidingEdge(point, this, barrierType);
        }
        /// <summary>
        /// Return a repulsion line from the closest barrier
        /// </summary>
        /// <param name="point">A point in the field</param>
        /// <param name="barrierType">The type of barrier</param>
        /// <returns>A line</returns>
        public UVLine GetRepulsionLine(UV point, BarrierType barrierType)
        {
            var collision = CollisionAnalyzer.GetCollidingEdge(point, this, barrierType);
            if (collision ==null)
            {
                return null;
            }
            if (collision.ClosestPointOnBarrier == null)
            {
                return null;
            }
            return new UVLine(point, collision.ClosestPointOnBarrier);
        }
        #endregion

        #region Adding and Removing data
        /// <summary>
        /// Remove a field of spatial data
        /// </summary>
        /// <param name="dataFieldName">Name of spatial data field</param>
        public void RemoveSpatialDataField(string dataFieldName)
        {
            if (dataFieldName == CellularFloor.DistanceFromEdgesOfField ||
                dataFieldName == CellularFloor.DistanceFromPhysicalBarriers ||
                dataFieldName == CellularFloor.DistanceFromVisualBarriers)
            {
                MessageBox.Show(string.Format("'{0}' cannot be removed!", dataFieldName),
                    "Removing not allowed", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else
            {
                if (!this.AllSpatialDataFields.ContainsKey(dataFieldName))
                {
                    MessageBox.Show("Data not found");
                    return;
                }
                lock (_lock)
                {
                    this.AllSpatialDataFields.Remove(dataFieldName);
                }
            }
        }
        /// <summary>
        /// Remove a collection of spatial data fields
        /// </summary>
        /// <param name="dataFieldNames">Names of spatial data fields</param>
        public void RemoveSpatialDataFields(ICollection<string> dataFieldNames)
        {
            foreach (var item in dataFieldNames)
            {
                if (item == CellularFloor.DistanceFromEdgesOfField ||
                    item == CellularFloor.DistanceFromPhysicalBarriers ||
                    item == CellularFloor.DistanceFromVisualBarriers)
                {
                    MessageBox.Show(string.Format("'{0}' cannot be removed!", item), 
                        "Removing not allowed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    this.RemoveSpatialDataField(item);
                }
            }
        }
        /// <summary>
        /// Add a field of spatial data. This methods is thread safe.
        /// </summary>
        /// <param name="data">The data field</param>
        public void AddSpatialDataField(SpatialDataField data)
        {
            lock (_lock)
            {
                this.AllSpatialDataFields.Add(data.Name, data);
            }
        }
        #endregion
        /// <summary>
        /// The barrier buffer edges
        /// </summary>
        public UVLine[] BarrierBufferEdges { get; set; }
        /// <summary>
        /// An array of polygons which form a buffer around the barriers using Minkowski addition
        /// </summary>
        public BarrierPolygons[] BarrierBuffers { get; set; }
        /// <summary>
        /// Create the barrier buffer polygons around the edges of the walkable field using Minkowski addition
        /// </summary>
        /// <param name="barrierEnvironment"></param>
        /// <param name="offsetValue"></param>
        public void LoadAllBarriersOffseted(BIM_To_OSM_Base barrierEnvironment, double offsetValue)
        {
            //offetting all of the boundaries
            this.BarrierBuffers = barrierEnvironment.ExpandAllBarrierPolygons(offsetValue);
            // creating lines for the boundaries
            int edgeNum = 0;
            foreach (BarrierPolygons item in this.BarrierBuffers)
            {
                edgeNum += item.Length;
            }
            this.BarrierBufferEdges = new UVLine[edgeNum];
            edgeNum = 0;
            foreach (BarrierPolygons brr in this.BarrierBuffers)
            {
                for (int i = 0; i < brr.Length; i++)
                {
                    UVLine line = new UVLine(brr.PointAt(i), brr.PointAt(brr.NextIndex(i)));
                    this.BarrierBufferEdges[edgeNum] = line;
                    edgeNum++;
                }
            }
            // cleaning up the cells from previous settings
            foreach (Cell cell in this.Cells)
            {
                if (cell.BarrierBufferEdgeIndices == null)
                {
                    cell.BarrierBufferEdgeIndices = new HashSet<int>();
                }
                else
                {
                    cell.BarrierBufferEdgeIndices.Clear();
                }
                cell.BarrierBufferOverlapState = OverlapState.Outside;
            }
            // loading the lines into the cells
            for (int i = 0; i < this.BarrierBufferEdges.Length; i++)
            {
                Index[] intersectingCellIndices = this.FindLineIndices(this.BarrierBufferEdges[i]);
                foreach (Index item in intersectingCellIndices)
                {
                    if (this.ContainsCell(item))
                    {
                        this.Cells[item.I, item.J].BarrierBufferEdgeIndices.Add(i);
                    }
                }
            }
            // setting the overlapping states of the cells that overlap
            foreach (Cell cell in this.Cells)
            {
                if (cell.BarrierBufferEdgeIndices.Count>0)
                {
                    cell.BarrierBufferOverlapState = OverlapState.Overlap;
                }
            }
            HashSet<Index> edge = new HashSet<Index>();
            foreach (Cell item in this.Cells)
            {
                if (item.FieldOverlapState == OverlapState.Overlap)
                {
                    var index = this.FindIndex(item);
                    if (index.I == 0 || index.I == this.GridWidth - 1 ||
                        index.J == 0 || index.J == this.GridHeight - 1)
                    { }
                    else
                    {
                        edge.Add(index);
                    }
                    
                }
            }
            HashSet<Index> temp_edge = new HashSet<Index>();
            HashSet<Index> area = new HashSet<Index>();
            area.UnionWith(edge);
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
                                if (this.Cells[next.I, next.J].FieldOverlapState == OverlapState.Inside &&
                                     this.Cells[next.I, next.J].BarrierBufferOverlapState != OverlapState.Overlap)
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
            foreach (Index item in area)
            {
                this.Cells[item.I, item.J].BarrierBufferOverlapState = OverlapState.Inside;
            }
            edge.Clear();
            edge = null;
            temp_edge.Clear();
            temp_edge = null;
            area.Clear();
            area = null;
            for (int i = 0; i < this.GridWidth; i++)
            {
                for (int j = 0; j < this.GridHeight; j++)
                {
                    if (this.Cells[i, j].FieldOverlapState != OverlapState.Inside)
                    {
                        if (this.Cells[i, j].BarrierBufferOverlapState != OverlapState.Overlap)
                        {
                            this.Cells[i, j].BarrierBufferOverlapState = OverlapState.Inside;
                        }
                    }
                }
            }
        }

        #region Isovist Polygon
        /// <summary>
        /// Loads a collection of lines which can serve as visual obstacles 
        /// </summary>
        /// <param name="vantagePoint">The vantage point</param>
        /// <param name="depth">Depth of view</param>
        /// <param name="barrierType">The type of barrier to select</param>
        /// <returns></returns>
        public HashSet<UVLine> PolygonalIsovistVisualObstacles(UV vantagePoint, double depth, BarrierType barrierType)
        {
            HashSet<int> blockIndices = new HashSet<int>();
            double lowerLimit, upperLimit;
            //finding the lower index of i
            lowerLimit = (vantagePoint.U - this.Origin.U - depth) / this.CellSize;
            int iMin = (int)lowerLimit;
            iMin = (iMin < 0) ? 0 : iMin;
            upperLimit = (vantagePoint.U - this.Origin.U + depth) / this.CellSize;
            int iMax = (int)Math.Ceiling(upperLimit);
            iMax = (iMax >= this.GridWidth) ? this.GridWidth - 1 : iMax;
            lowerLimit = (vantagePoint.V - this.Origin.V - depth) / this.CellSize;
            int jMin = (int)lowerLimit;
            jMin = (jMin < 0) ? 0 : jMin;
            upperLimit = (vantagePoint.V - this.Origin.V + depth) / this.CellSize;
            int jMax = (int)Math.Ceiling(upperLimit);
            jMax = (jMax >= this.GridHeight) ? this.GridHeight - 1 : jMax;
            if (iMin >= this.GridWidth || iMax < 0 || jMin >= this.GridHeight || jMax < 0)
            {
            }
            else
            {
                for (int i = iMin; i <= iMax; i++)
                {
                    for (int j = jMin; j <= jMax; j++)
                    {
                        switch (barrierType)
                        {
                            case BarrierType.Visual:
                                blockIndices.UnionWith(Cells[i, j].VisualBarrierEdgeIndices);
                                break;
                            case BarrierType.Physical:
                                blockIndices.UnionWith(Cells[i, j].PhysicalBarrierEdgeIndices);
                                break;
                            case BarrierType.Field:
                                blockIndices.UnionWith(Cells[i, j].FieldBarrierEdgeIndices);
                                break;
                            case BarrierType.BarrierBuffer:
                                blockIndices.UnionWith(Cells[i, j].BarrierBufferEdgeIndices);
                                break;
                        }
                    }
                }
            }
            HashSet<UVLine> lineBlocks = new HashSet<UVLine>();
            foreach (int item in blockIndices)
            {
                switch (barrierType)
                {
                    case BarrierType.Visual:
                        lineBlocks.Add(this.VisualBarrierEdges[item]);
                        break;
                    case BarrierType.Physical:
                        lineBlocks.Add(this.PhysicalBarrierEdges[item]);
                        break;
                    case BarrierType.Field:
                        lineBlocks.Add(this.FieldBarrierEdges[item]);
                        break;
                    case BarrierType.BarrierBuffer:
                        lineBlocks.Add(this.BarrierBufferEdges[item]);
                        break;
                }

            }
            return lineBlocks;
        }

        #endregion
        /// <summary>
        /// calculated the static cost of the selected spatial data fields.
        /// </summary>
        /// <returns></returns>
        public Dictionary<Cell, double> GetStaticCost()
        {
            List<SpatialDataField> includedData = new List<SpatialDataField>();
            foreach (Function function in this.AllSpatialDataFields.Values)
            {
                SpatialDataField dataField = function as SpatialDataField;
                if (dataField != null)
                {
                    if (dataField.IncludeInActivityGeneration)
                    {
                        includedData.Add(dataField);
                    }
                }
            }
            Dictionary<Cell, double> costs = new Dictionary<Cell, double>();
            double min = double.PositiveInfinity;
            foreach (Cell cell in this.Cells)
            {
                if (cell.FieldOverlapState == OverlapState.Inside)
                {
                    double cost = 0;
                    foreach (SpatialDataField dataField in includedData)
                    {
                        if (dataField.Data.ContainsKey(cell))
                        {
                            cost += dataField.GetCost(dataField.Data[cell]);
                        }
                        else
                        {
                            throw new ArgumentException("Data field does not include value for the given cell");
                        }
                        min = Math.Min(min, cost);
                    }
                    costs.Add(cell, cost);
                }
            }
            if (double.IsInfinity(min))
            {
                foreach (Cell item in costs.Keys.ToArray())
                {
                    costs[item] = 0.0d;
                }
            }
            else
            {
                foreach (Cell item in costs.Keys.ToArray())
                {
                    costs[item] -= min;
                }
            }

            includedData.Clear();
            includedData = null;
            return costs;
        }
    }


}

