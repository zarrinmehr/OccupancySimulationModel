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
using SpatialAnalysis.CellularEnvironment;
using SpatialAnalysis.Data;
using SpatialAnalysis.IsovistUtility;
using SpatialAnalysis.Geometry;
using SpatialAnalysis.Agents;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Threading;
using SpatialAnalysis.Interoperability;

namespace SpatialAnalysis.Events
{
    /// <summary>
    /// Class VisibilityEvaluationEvent.
    /// </summary>
    public class VisibilityTarget //VisibilityTarget
    {
        
        /// <summary>
        /// Gets or sets all visible cells.
        /// </summary>
        /// <value>All visible cells.</value>
        public HashSet<int> AllVisibleCells { get; set; }
        /// <summary>
        /// Gets or sets the referenced vantage cells.
        /// </summary>
        /// <value>The referenced vantage cells.</value>
        public Dictionary<int, List<int>> ReferencedVantageCells{ get; set; }
        /// <summary>
        /// Gets or sets the vantage cells.
        /// </summary>
        /// <value>The vantage cells.</value>
        public ICollection<int> VantageCells { get; set; }
        //public Isovist[] VisibilityTargetIsovists { get; set; }
        /// <summary>
        /// Gets or sets the visual targets.
        /// </summary>
        /// <value>The visual targets.</value>
        public SpatialAnalysis.Geometry.BarrierPolygons[] VisualTargets { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="VisibilityEvaluationEvent"/> class.
        /// </summary>
        /// <param name="allVisibleCells">All visible cells.</param>
        /// <param name="visibilityTargetIsovists">The visibility target isovists.</param>
        /// <param name="visualTargets">The visual targets.</param>
        public VisibilityTarget(HashSet<int> allVisibleCells, ICollection<Isovist> visibilityTargetIsovists, ICollection<SpatialAnalysis.Geometry.BarrierPolygons> visualTargets)
        {
            this.AllVisibleCells = allVisibleCells;
            //this.VisibilityTargetIsovists = visibilityTargetIsovists;
            this.VisualTargets = visualTargets.ToArray();
            this.ReferencedVantageCells = new Dictionary<int, List<int>>();
            var vantageCells = new List<int>(visibilityTargetIsovists.Count);
            foreach (var item in this.AllVisibleCells)
            {
                this.ReferencedVantageCells.Add(item, new List<int>());
            }
            foreach (var isovist in visibilityTargetIsovists)
            {
                vantageCells.Add(isovist.VantageCell.ID);
                foreach (var cellID in isovist.VisibleCells)
                {
                    this.ReferencedVantageCells[cellID].Add(isovist.VantageCell.ID);
                }
            }
            this.VantageCells = vantageCells;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="VisibilityEvaluationEvent"/> class.
        /// </summary>
        /// <param name="visualTargets">The visual targets.</param>
        /// <param name="cellularFloor">The cellular floor.</param>
        /// <param name="tolerance">The tolerance by default set to the main document's absolute tolerance value.</param>
        /// <exception cref="System.ArgumentException">Cannot generate 'Occupancy Visual Event' with no visibility target cells!</exception>
        public VisibilityTarget(ICollection<SpatialAnalysis.Geometry.BarrierPolygons> visualTargets, CellularFloor cellularFloor, double tolerance = OSMDocument.AbsoluteTolerance)
        {
            this.VisualTargets = visualTargets.ToArray();
            HashSet<Index> allIndices = new HashSet<Index>();
            foreach (SpatialAnalysis.Geometry.BarrierPolygons item in this.VisualTargets)
            {
                allIndices.UnionWith(cellularFloor.GetIndicesInsideBarrier(item, tolerance));
            }
            var visibleCells = new HashSet<int>();
            foreach (Index item in allIndices)
            {
                if (cellularFloor.ContainsCell(item) &&
                    cellularFloor.Cells[item.I, item.J].VisualOverlapState == OverlapState.Outside)
                {
                    visibleCells.Add(cellularFloor.Cells[item.I, item.J].ID);
                }
            }
            allIndices.Clear();
            allIndices = null;
            if (visibleCells.Count == 0)
            {
                throw new ArgumentException("Cannot generate 'Occupancy Visual Event' with no visibility target cells!");
            }
            var cellsOnEdge = CellUtility.GetEdgeOfField(cellularFloor, visibleCells);
            List<Isovist> isovists = new List<Isovist>(cellsOnEdge.Count);
            double depth = cellularFloor.Origin.DistanceTo(cellularFloor.TopRight) + 1;
            foreach (int item in cellsOnEdge)
            {
                isovists.Add(new Isovist(cellularFloor.FindCell(item)));
            }
            Parallel.ForEach(isovists, (a) =>
            {
                a.Compute(depth, BarrierType.Visual, cellularFloor, 0.0000001);
            });
            HashSet<int> visibleArea = new HashSet<int>();
            foreach (Isovist item in isovists)
            {
                visibleArea.UnionWith(item.VisibleCells);
            }
            this.AllVisibleCells = visibleArea;
            this.ReferencedVantageCells = new Dictionary<int, List<int>>();
            var vantageCells = new List<int>(isovists.Count);
            foreach (var item in this.AllVisibleCells)
            {
                this.ReferencedVantageCells.Add(item, new List<int>());
            }
            foreach (var isovist in isovists)
            {
                vantageCells.Add(isovist.VantageCell.ID);
                foreach (var cellID in isovist.VisibleCells)
                {
                    this.ReferencedVantageCells[cellID].Add(isovist.VantageCell.ID);
                }
            }
            this.VantageCells = vantageCells;
        }
        /// <summary>
        /// Saves the instance of the visual event as string.
        /// </summary>
        /// <returns>System.String.</returns>
        public string SaveAsString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (SpatialAnalysis.Geometry.BarrierPolygons item in VisualTargets)
            {
                sb.AppendLine(item.ToString());
            }
            string text = sb.ToString();
            sb.Clear();
            sb = null;
            return text;
        }
        /// <summary>
        /// Creates an instance of the event from its string representation .
        /// </summary>
        /// <param name="lines">The lines.</param>
        /// <param name="start">The start.</param>
        /// <param name="cellularFloor">The cellular floor.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>VisibilityEvaluationEvent.</returns>
        public static VisibilityTarget FromString(List<String> lines, int start, Length_Unit_Types unitType, CellularFloor cellularFloor, double tolerance = 0.0000001d)
        {
            List<SpatialAnalysis.Geometry.BarrierPolygons> barriers = new List<Geometry.BarrierPolygons>();
            for (int i = start; i < lines.Count; i++)
            {
                var barrier = SpatialAnalysis.Geometry.BarrierPolygons.FromStringRepresentation(lines[i]);
                UnitConversion.Transform(barrier.BoundaryPoints, unitType, cellularFloor.UnitType);
                barriers.Add(barrier);
            }
            return new VisibilityTarget(barriers, cellularFloor, tolerance);
        }

        /// <summary>
        /// Targets the visibility test.
        /// </summary>
        /// <param name="currentState">The current state of the agent.</param>
        /// <param name="visibilityCosineFactor">The visibility cosine factor.</param>
        /// <param name="cellularFloor">The cellular floor.</param>
        /// <returns>UV.</returns>
        public UV TargetVisibilityTest(StateBase currentState, double visibilityCosineFactor, CellularFloorBaseGeometry cellularFloor)
        {
            var vantageCell = cellularFloor.FindCell(currentState.Location);
            if (!this.AllVisibleCells.Contains(vantageCell.ID))
            {
                return null;
            }
            UV target = null;
            foreach (var cellID in this.ReferencedVantageCells[vantageCell.ID])
            {
                UV targetCell = cellularFloor.FindCell(cellID);
                UV direction = targetCell - currentState.Location;
                direction.Unitize();
                if (direction.DotProduct(currentState.Direction) >= visibilityCosineFactor)
                {
                    target = targetCell;
                    break;
                }
            }
            return target;
        }
        /// <summary>
        /// Determines whether a visual the event is raised.
        /// </summary>
        /// <param name="currentState">The current state of the agent.</param>
        /// <param name="visibilityCosineFactor">The visibility cosine factor.</param>
        /// <param name="cellularFloor">The cellular floor.</param>
        /// <returns><c>true</c> if visual the event is raised, <c>false</c> otherwise.</returns>
        public bool VisualEventRaised(StateBase currentState, double visibilityCosineFactor, CellularFloorBaseGeometry cellularFloor)
        {
            var vantageCell = cellularFloor.FindCell(currentState.Location);
            if (!this.AllVisibleCells.Contains(vantageCell.ID))
            {
                return false;
            }
            bool raised = false;
            foreach (var cellID in this.ReferencedVantageCells[vantageCell.ID])
            {
                UV targetCell = cellularFloor.FindCell(cellID);
                UV direction = targetCell - currentState.Location;
                direction.Unitize();
                if (direction.DotProduct(currentState.Direction) >= visibilityCosineFactor)
                {
                    raised = true;
                    break;
                }
            }
            return raised;
        }
        public static bool operator ==(VisibilityTarget a, VisibilityTarget b)
        {
            if ((object)a == null && (object)b == null)
            {
                return true;
            }
            if ((object)a == null || (object)b == null)
            {
                return false;
            }
            if (a.AllVisibleCells.Count == b.AllVisibleCells.Count)
            {
                foreach (var item in a.AllVisibleCells)
                {
                    if (!b.AllVisibleCells.Contains(item))
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        public static bool operator !=(VisibilityTarget a, VisibilityTarget b)
        {
            return !(a == b);
        }
    }

}

