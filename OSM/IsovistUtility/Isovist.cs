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
using System.Text;
using System.Linq;
using System.Collections;
using System.IO;
using System.Windows;

using System.Windows.Media;
using System.Windows.Shapes;
using SpatialAnalysis.FieldUtility;
using SpatialAnalysis.CellularEnvironment;
using SpatialAnalysis.Geometry;

namespace SpatialAnalysis.IsovistUtility
{
    /// <summary>
    /// Class Isovist represents the field of visibility with a collection of cells which are visible from a vantage cell.
    /// </summary>
    public class Isovist
    {
        /// <summary>
        /// Gets or sets the vantage cell of the isovist.
        /// </summary>
        /// <value>The vantage cell.</value>
        public Cell VantageCell { get; set; }
        /// <summary>
        /// Gets or sets the IDs of the visible cells.
        /// </summary>
        /// <value>The visible cells.</value>
        public HashSet<int> VisibleCells { get; set; }
        /// <summary>
        /// Returns the area of the filed of visibility.
        /// </summary>
        /// <param name="cellSize">Size of the cell.</param>
        /// <returns>System.Double.</returns>
        /// <value>The area.</value>
        public double GetArea(double cellSize) { return this.VisibleCells.Count * cellSize; }
        /// <summary>
        /// Gets or sets the edges of the field of visibility.
        /// </summary>
        /// <value>The edge lines.</value>
        public HashSet<UVLine> EdgeLines { get; set; }
        public bool IsEdgeLoaded = false;
        /// <summary>
        /// Initializes a new instance of the <see cref="Isovist"/> class.
        /// </summary>
        /// <param name="vantageCell">The vantage cell.</param>
        /// <param name="visibleCells">The visible cells.</param>
        public Isovist(Cell vantageCell, HashSet<int> visibleCells)
        {
            this.VantageCell = vantageCell;
            this.VisibleCells = visibleCells;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Isovist"/> class.
        /// </summary>
        /// <param name="vantageCell">The vantage cell.</param>
        public Isovist(Cell vantageCell)
        {
            this.VantageCell = vantageCell;
        }
        /// <summary>
        /// Computes the isovist at the specified depth of view.
        /// </summary>
        /// <param name="depth">The depth of view.</param>
        /// <param name="typeOfBarrier">The type of barrier.</param>
        /// <param name="cellularFloor">The cellularFloor.</param>
        /// <param name="Tolerance">The tolerance.</param>
        public void Compute(double depth, BarrierType typeOfBarrier,
            CellularFloor cellularFloor, double Tolerance = OSMDocument.AbsoluteTolerance)
        {
            var isovist = CellularIsovistCalculator.GetIsovist(this.VantageCell + new UV(cellularFloor.CellSize / 2, cellularFloor.CellSize / 2),
                depth, typeOfBarrier, cellularFloor, Tolerance);
            this.VisibleCells = isovist.VisibleCells;
        }
        public override int GetHashCode()
        {
            return this.VantageCell.GetHashCode();
        }
        /// <summary>
        /// Gets the cell IDs at the edges of the visibility area.
        /// </summary>
        /// <param name="cellularFloor">The cellularFloor.</param>
        /// <returns>HashSet&lt;System.Int32&gt;.</returns>
        public HashSet<int> GetIsovistEdge(CellularFloor cellularFloor)
        {
            return CellUtility.GetEdgeOfField(cellularFloor, this.VisibleCells);
        }
        /// <summary>
        /// Gets the boundary polygon of the visibility area.
        /// </summary>
        /// <param name="cellularFloor">The cellularFloor.</param>
        /// <returns>List&lt;BarrierPolygons&gt;.</returns>
        public List<BarrierPolygons> GetBoundary(CellularFloor cellularFloor)
        {
            Dictionary<UVLine, int> guid = new Dictionary<UVLine, int>();
            foreach (var item in this.VisibleCells)
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
            List<BarrierPolygons> boundary = new List<BarrierPolygons>();
            foreach (PLine item in plines)
            {
                var oneBoundary = item.Simplify(cellularFloor.CellSize / 10);
                if (oneBoundary != null)
                {
                    boundary.Add(new BarrierPolygons(oneBoundary.ToArray()));
                }
            }
            boundaryLines.Clear();
            boundaryLines = null;
            return boundary;
        }
    }

    /// <summary>
    /// Class IsovistInformation.
    /// </summary>
    public class IsovistInformation
    {
        /// <summary>
        /// Enum IsovistType
        /// </summary>
        public enum IsovistType
        {
            /// <summary>
            /// The isovist is a collection of cells
            /// </summary>
            Cellular,
            /// <summary>
            /// The isovist is polygonal
            /// </summary>
            Polygonal
        }
        /// <summary>
        /// Gets or sets the type of the isovist.
        /// </summary>
        /// <value>The type of the iso.</value>
        public IsovistType IsoType { get; set; }
        /// <summary>
        /// Gets or sets the time for generation of the isovist.
        /// </summary>
        /// <value>The time for generation.</value>
        public double TimeForGeneration { get; set; }
        /// <summary>
        /// Gets or sets the visibility area.
        /// </summary>
        /// <value>The area.</value>
        public double Area { get; set; }
        /// <summary>
        /// Gets or sets the perimeter of the isovist. This number is not set for cellular type isovists.
        /// </summary>
        /// <value>The perimeter.</value>
        public double Perimeter { get; set; }
        public IsovistInformation(IsovistType isovistType, double time, double area, double preimeter)
        {
            this.TimeForGeneration = time; this.IsoType = isovistType; this.Area = area; this.Perimeter = preimeter;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format("Isovist type: {0}", this.IsoType.ToString()));
            sb.AppendLine(string.Format("Generation Time: {0}", this.TimeForGeneration.ToString()));
            sb.AppendLine(string.Format("Area: {0}", this.Area.ToString()));
            sb.AppendLine(string.Format("Perimeter: {0}", this.Perimeter.ToString()));
            return sb.ToString();
        }
    }


    /// <summary>
    /// Class IsovistPolygon represents polygonal isovists
    /// </summary>
    /// <seealso cref="SpatialAnalysis.Geometry.BarrierPolygons" />
    public class IsovistPolygon : BarrierPolygons
    {

        /// <summary>
        /// Gets or sets the vantage point of the Isovist.
        /// </summary>
        /// <value>The vantage point.</value>
        public SpatialAnalysis.Geometry.UV VantagePoint { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="IsovistPolygon"/> class.
        /// </summary>
        /// <param name="points">The points that represent the isovist polygon.</param>
        /// <param name="vantagePoint">The vantage point of the polygonal isovist.</param>
        public IsovistPolygon(UV[] points, UV vantagePoint)
            : base(points)
        {
            this.VantagePoint = vantagePoint;
        }
    }


    /// <summary>
    /// This class includes a list of polygonal isovists for Proxemics analysis.
    /// </summary>
    internal class Proxemics
    {
        public UV Center { get; set; }
        public BarrierPolygons[] ProxemicsPolygons { get; set; }
        public Proxemics(BarrierPolygons[] polygons, UV center)
        {
            this.Center = center;
            this.ProxemicsPolygons = polygons;
        }
    }

}

