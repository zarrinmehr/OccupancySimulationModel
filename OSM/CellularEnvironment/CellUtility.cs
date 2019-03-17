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
using SpatialAnalysis.CellularEnvironment.GetCellValue;
using System.Windows;
using SpatialAnalysis.Geometry;

namespace SpatialAnalysis.CellularEnvironment
{
    /// <summary>
    /// This class includes utility functions for cells
    /// </summary>
    internal static class CellUtility
    {
        /// <summary>
        /// Determines if a cell is on the edges of a cell collections
        /// </summary>
        /// <param name="cellID">The ID of the cell</param>
        /// <param name="cellularFloor">The CellularFloorBaseGeometry to which this cell belongs</param>
        /// <param name="collection">Cell ID collection</param>
        /// <returns>true for being on the edge false for not being on the edge</returns>
        private static bool onEdge(int cellID, CellularFloorBaseGeometry cellularFloor, HashSet<int> collection)
        {
            var index = cellularFloor.FindIndex(cellID);
            if (index.I == 0 || index.I == cellularFloor.GridWidth - 1 || index.J == 0 || index.J == cellularFloor.GridHeight - 1)
            {
                return true;
            }
            int iMin = (index.I == 0) ? 0 : index.I - 1;
            int iMax = (index.I == cellularFloor.GridWidth - 1) ? cellularFloor.GridWidth - 1 : index.I + 1;
            int jMin = (index.J == 0) ? 0 : index.J - 1;
            int jMax = (index.J == cellularFloor.GridHeight - 1) ? cellularFloor.GridHeight - 1 : index.J + 1;
            for (int i = iMin; i <= iMax; i++)
            {
                for (int j = jMin; j <= jMax; j++)
                {
                    if (cellularFloor.ContainsCell(i,j))
                    {
                        if (!collection.Contains(cellularFloor.Cells[i, j].ID))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// Determines if a cell is on the edges of a cell collections
        /// </summary>
        /// <param name="index">The index of the cell</param>
        /// <param name="cellularFloor">The CellularFloorBaseGeometry to which this cell belongs</param>
        /// <param name="collection">Cell ID collection</param>
        /// <returns>true for being on the edge false for not being on the edge</returns>
        private static bool onEdge(Index index, CellularFloorBaseGeometry cellularFloor, HashSet<Cell> collection)
        {
            foreach (Index item in Index.Neighbors)
            {
                Index neighbor = item + index;
                if (cellularFloor.ContainsCell(neighbor))
                {
                    if (!collection.Contains(cellularFloor.Cells[neighbor.I,neighbor.J]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// Determines if a cell is on the edges of a cell collections
        /// </summary>
        /// <param name="index">The index of the cell</param>
        /// <param name="cellularFloor">The CellularFloorBaseGeometry to which this cell belongs</param>
        /// <param name="collection">Cell index collection</param>
        /// <returns>true for being on the edge false for not being on the edge</returns>
        private static bool onEdge(Index index, CellularFloorBaseGeometry cellularFloor, HashSet<Index> collection)
        {
            foreach (Index item in Index.Neighbors)
            {
                Index neighbor = item + index;
                if (cellularFloor.ContainsCell(neighbor))
                {
                    if (!collection.Contains(neighbor))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// Extracts the edges of a collection of cells 
        /// </summary>
        /// <param name="cellularFloor">The CellularFloorBaseGeometry to which the cells belong</param>
        /// <param name="collection">A collection of cells IDs to find its edges</param>
        /// <returns>A collection of cell IDs on the edge</returns>
        public static HashSet<int> GetEdgeOfField(CellularFloorBaseGeometry cellularFloor, HashSet<int> collection)
        {
            HashSet<int> edge = new HashSet<int>();
            foreach (var item in collection)
            {
                if (CellUtility.onEdge(item, cellularFloor, collection))
                {
                    edge.Add(item);
                }
            }
            return edge;
        }
        /// <summary>
        /// Extracts the edges of a collection of cells
        /// </summary>
        /// <param name="cellularFloor">The CellularFloorBaseGeometry to which the cells belong</param>
        /// <param name="collection">A collection of cells to find its edges</param>
        /// <returns>A collection of cell indices on the edge</returns>
        public static HashSet<Index> GetIndexEdgeOfField(CellularFloorBaseGeometry cellularFloor, HashSet<Cell> collection)
        {
            HashSet<Index> edge = new HashSet<Index>();
            foreach (var item in collection)
            {
                Index index = cellularFloor.FindIndex(item);
                if (CellUtility.onEdge(index, cellularFloor, collection))
                {
                    edge.Add(index);
                }
            }
            return edge;
        }
        /// <summary>
        /// Get a list of ordered cells on the edge of a cell collection
        /// </summary>
        /// <param name="cellularFloor">The CellularFloorBaseGeometry to which the cells belong</param>
        /// <param name="collection">A collection of cells to find its edges</param>
        /// <returns>An ordered list of cells</returns>
        public static List<Cell> GetOrderedEdge(CellularFloorBaseGeometry cellularFloor, HashSet<Cell> collection)
        {
            HashSet<Index> edge = CellUtility.GetIndexEdgeOfField(cellularFloor, collection);
            IndexGraph indexGraph = new IndexGraph(edge);
            int zero = 0, one = 0, two = 0, three = 0, four = 0;
            foreach (var item in indexGraph.IndexNodeMap.Values)
            {
                switch (item.Connections.Count)
                {
                    case 0:
                        zero++;
                        break;
                    case 1:
                        one++;
                        break;
                    case 2: 
                        two++;
                        break;
                    case 3: 
                        three++;
                        break;
                    case 4: 
                        four++;
                        break;
                    default:
                        break;
                }
            }
            MessageBox.Show(string.Format("Zero: {0}\nOne: {1}\nTwo: {2}\nThree: {3}\nFour: {4}",
                zero.ToString(), one.ToString(), two.ToString(), three.ToString(), four.ToString()));
            return null;
        }

        /// <summary>
        /// Gets the boundary polygons of a collection of cells.
        /// </summary>
        /// <param name="cellIDs">The visible cells.</param>
        /// <param name="cellularFloor">The cellular floor.</param>
        /// <returns>List&lt;BarrierPolygons&gt;.</returns>
        public static List<BarrierPolygon> GetBoundary(ICollection<int> cellIDs, CellularFloorBaseGeometry cellularFloor)
        {
            Dictionary<UVLine, int> guid = new Dictionary<UVLine, int>();
            foreach (var item in cellIDs)
            {
                var lines = cellularFloor.CellToLines(cellularFloor.FindCell(item));
                foreach (var line in lines)
                {
                    if (guid.ContainsKey(line))
                    {
                        guid[line]++;
                    }
                    else
                    {
                        guid.Add(line, 1);
                    }
                }
            }
            List<UVLine> boundaryLines = new List<UVLine>();
            foreach (KeyValuePair<UVLine,int> item in guid)
            {
                if (item.Value==1)
                {
                    boundaryLines.Add(item.Key);
                }
            }
            guid.Clear();
            guid = null;
            var plines = PLine.ExtractPLines(boundaryLines);
            List<BarrierPolygon> boundary = new List<BarrierPolygon>();
            foreach (PLine item in plines)
            {
                var oneBoundary = item.Simplify(cellularFloor.CellSize / 10);
                if (oneBoundary != null)
                {
                    boundary.Add(new BarrierPolygon(oneBoundary.ToArray()));
                }
            }
            boundaryLines.Clear();
            boundaryLines = null;
            return boundary;
        }

        /// <summary>
        /// Gets the field boundary polygons.
        /// </summary>
        /// <param name="cellularFloor">The cellular floor.</param>
        /// <returns>List&lt;BarrierPolygons&gt;.</returns>
        public static List<BarrierPolygon> GetFieldBoundary(CellularFloor cellularFloor)
        {
            Dictionary<UVLine, int> guid = new Dictionary<UVLine, int>();
            foreach (var item in cellularFloor.Cells)
            {
                if (item.FieldOverlapState == OverlapState.Inside)
                {
                    var lines = cellularFloor.CellToLines(item);
                    foreach (var line in lines)
                    {
                        if (guid.ContainsKey(line))
                        {
                            guid[line]++;
                        }
                        else
                        {
                            guid.Add(line, 1);
                        }
                    }
                }
            }
            List<UVLine> boundaryLines = new List<UVLine>();
            foreach (KeyValuePair<UVLine, int> item in guid)
            {
                if (item.Value == 1)
                {
                    boundaryLines.Add(item.Key);
                }
            }
            guid.Clear();
            guid = null;
            var pLines = PLine.ExtractPLines(boundaryLines);
            List<BarrierPolygon> boundary = new List<BarrierPolygon>();
            foreach (PLine item in pLines)
            {
                var oneBoundary = item.Simplify(cellularFloor.CellSize / 10);
                if (oneBoundary != null)
                {
                    boundary.Add(new BarrierPolygon(oneBoundary.ToArray()));
                }
            }
            boundaryLines.Clear();
            boundaryLines = null;
            return boundary;
        }
        /// <summary>
        /// Expands a collection of indices in the walkable field.
        /// </summary>
        /// <param name="cellularFloor">The cellular floor.</param>
        /// <param name="indices">The indices.</param>
        /// <returns>HashSet&lt;Index&gt;.</returns>
        public static HashSet<Index> ExpandInWalkableField(CellularFloor cellularFloor, ICollection<Index> indices)
        {
            HashSet<Index> collection = new HashSet<Index>();
            foreach (var item in indices)
            {
                collection.Add(item);
                foreach (var relativeIndex in Index.Neighbors)
                {
                    Index index = item + relativeIndex;
                    if (cellularFloor.ContainsCell(index) && !collection.Contains(index))
                    {
                        if (cellularFloor.Cells[index.I,index.J].FieldOverlapState == OverlapState.Inside)
                        {
                            collection.Add(index);
                        }
                    }
                }
            }
            return collection;
        }
    }

}


