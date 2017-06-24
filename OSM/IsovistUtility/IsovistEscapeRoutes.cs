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
using CellCollection = System.Collections.Generic.HashSet<SpatialAnalysis.CellularEnvironment.Cell>;
using SpatialAnalysis.Geometry;
using System.Windows;
using SpatialAnalysis.Visualization;

namespace SpatialAnalysis.IsovistUtility
{
    /// <summary>
    /// Represents the cells that are located at the boundaries of an isovist
    /// </summary>
    public class IsovistEscapeRoutes
    {
        /// <summary>
        /// Gets or sets the escape routes (i.e. the cells at the boundary of the isovist.
        /// </summary>
        /// <value>The escape routes.</value>
        public Cell[] EscapeRoutes { get; set; }
        /// <summary>
        /// Gets or sets the vantage cell of the cellular isovist.
        /// </summary>
        /// <value>The vantage cell.</value>
        public Cell VantageCell { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="IsovistEscapeRoutes"/> class.
        /// </summary>
        /// <param name="escapeRoutes">The escape routes.</param>
        /// <param name="vantageCell">The vantage cell.</param>
        public IsovistEscapeRoutes(Cell[] escapeRoutes, Cell vantageCell)
        {
            this.VantageCell = vantageCell;
            this.EscapeRoutes = escapeRoutes;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="IsovistEscapeRoutes"/> class.
        /// </summary>
        /// <param name="escapeRoutes">The escape routes.</param>
        /// <param name="vantageCell">The vantage cell.</param>
        /// <param name="angleIntercept">The angle intercept.</param>
        /// <param name="staticCost">The static cost.</param>
        public IsovistEscapeRoutes(Cell[] escapeRoutes, Cell vantageCell, int angleIntercept, Dictionary<Cell, double> staticCost)
        {
            this.VantageCell = vantageCell;
            CellDestination[] cells = IsovistEscapeRoutes.SplitExtractedEscapeRoute(vantageCell, angleIntercept, escapeRoutes, staticCost);
            Cell[] destinations = new Cell[cells.Length];
            for (int i = 0; i < cells.Length; i++)
            {
                destinations[i] = cells[i].CellValue;
            }
            this.EscapeRoutes = destinations;
        }
        public override bool Equals(object obj)
        {
            IsovistEscapeRoutes other = obj as IsovistEscapeRoutes;
            if (other == null)
            {
                return false;
            }
            return other.VantageCell.Equals(this.VantageCell);
        }
        public override int GetHashCode()
        {
            return this.VantageCell.GetHashCode();
        }
        public override string ToString()
        {
            return string.Format("{0}; Possible escape routes: {1}",
                this.VantageCell.ToString(), this.EscapeRoutes.Length.ToString());
        }
        // SimplifiedEscapeRoute step 2
        /// <summary>
        /// Splits the extracted escape route to a collection of cells which are visible destinations.
        /// </summary>
        /// <param name="vantageCell">The vantage cell.</param>
        /// <param name="angleIntercept">The angle intercept.</param>
        /// <param name="collection">The collection of visible cells at the boundary of the isovist.</param>
        /// <param name="staticCosts">The static costs.</param>
        /// <returns>CellDestination[].</returns>
        public static CellDestination[] SplitExtractedEscapeRoute(Cell vantageCell, int angleIntercept, ICollection<Cell> collection, Dictionary<Cell,double> staticCosts)
        {
            CellDestination[] destinations = new CellDestination[collection.Count];
            var timer = new System.Diagnostics.Stopwatch();
            int i = 0;
            timer.Start();
            foreach (var item in collection)
            {
                destinations[i] = new CellDestination(vantageCell, item, staticCosts[item]);
                i++;
            }
            Array.Sort(destinations, new CellDestinationComparerAngleThenCost(angleIntercept));
            //HashSet<CellDestination> SimplifiedSet = new HashSet<CellDestination>(destinations, new CellDestinationComparerAngleOnly(angleIntercept));
            //SimplifiedSet.UnionWith(destinations);
            List<CellDestination> simplified = new List<CellDestination> { destinations[0] };
            CellDestination last = destinations[0];
            double angle = (2 * Math.PI) / angleIntercept;
            for (i = 1; i < destinations.Length; i++)
            {
                int a1 = (int)(last.Angle / angle);
                int a2 = (int)(destinations[i].Angle / angle);
                if (a1 != a2)
                {
                    simplified.Add(destinations[i]);
                    last = destinations[i];
                }
            }
            List<CellDestination> purged = new List<CellDestination>();
            bool[] keep = new bool[simplified.Count];
            for (i = 0; i < keep.Length; i++)
            {
                keep[i] = true;
            }
            for (i = 0; i < simplified.Count; i++)
            {
                if (keep[i])
                {
                    int next_Index = (i < simplified.Count - 1) ? i + 1 : 0;
                    double angleWith_next;

                    if (i < simplified.Count - 1)
                    {
                        angleWith_next = simplified[i + 1].Angle - simplified[i].Angle;
                    }
                    else
                    {
                        angleWith_next = 2 * Math.PI - simplified[i].Angle + simplified[0].Angle;
                    }
                    if (angleWith_next < angle)
                    {
                        if (simplified[i].DesirabilityCost > simplified[next_Index].DesirabilityCost)
                        {
                            keep[i] = false;
                        }
                        else
                        {
                            keep[next_Index] = false;
                        }
                    }
                    if (keep[i])
                    {
                        int before_Index = (i > 0) ? i - 1 : simplified.Count - 1;
                        double angleWith_Before;
                        if (i > 0)
                        {
                            angleWith_Before = simplified[i].Angle - simplified[i - 1].Angle;
                        }
                        else
                        {
                            angleWith_Before = 2 * Math.PI - simplified[simplified.Count - 1].Angle + simplified[0].Angle;
                        }
                        if (angleWith_Before < angle)
                        {
                            if (simplified[i].DesirabilityCost > simplified[before_Index].DesirabilityCost)
                            {
                                keep[i] = false;
                            }
                            else
                            {
                                keep[before_Index] = false;
                            }
                        }
                    }
                }
            }
            for (i = 0; i < keep.Length; i++)
            {
                if (keep[i])
                {
                    purged.Add(simplified[i]);
                }
            }
            timer.Stop();
            int prgd = destinations.Length - simplified.Count;
            string s = timer.Elapsed.TotalMilliseconds.ToString();
            MessageBox.Show("Time: " + s
                + "\n\t" + simplified.Count.ToString() + " included"
                + "\n\t" + prgd.ToString() + " removed"
                + "\n\t" + (simplified.Count-purged.Count).ToString() + "close points removed"
                + "\n\tMinimum Angle: " + (destinations[0].Angle * 180 / Math.PI).ToString()
                + "\n\tMaximum Angle: " + (destinations[destinations.Length - 1].Angle * 180 / Math.PI).ToString());
            return purged.ToArray();
            //return SimplifiedSet.ToArray();
        }
    }

    /// <summary>
    /// This class is used to defines the sorting logic of the cell destinations based on angle and then quality.
    /// </summary>
    /// <seealso cref="System.Collections.Generic.IComparer{SpatialAnalysis.IsovistUtility.CellDestination}" />
    class CellDestinationComparerAngleThenCost : IComparer<CellDestination>
    {
        private readonly double _angle;
        public CellDestinationComparerAngleThenCost(int angleIntercept)
        {
            this._angle = (2 * Math.PI) / angleIntercept;
        }
        public int Compare(CellDestination d1, CellDestination d2)
        {
            int a1 = (int)(d1.Angle / this._angle);
            int a2 = (int)(d2.Angle / this._angle);
            int result = a1.CompareTo(a2);
            if (result != 0)
            {
                return result;
            }
            result = d1.DesirabilityCost.CompareTo(d2.DesirabilityCost);
            return result;
        }
    }

    /// <summary>
    /// includes a destination cell from a vantage cell, 
    /// the normalized direction to it from the vantage cell, 
    /// the angle of the direction vector, 
    /// and the desirability cost of destination
    /// </summary>
    public class CellDestination
    {
        /// <summary>
        /// Gets or sets the cost.
        /// </summary>
        /// <value>The cost.</value>
        public double DesirabilityCost { get; set; }
        /// <summary>
        /// Gets or sets the angle.
        /// </summary>
        /// <value>The angle.</value>
        public double Angle { get; set; }
        /// <summary>
        /// Gets or sets the normalized direction.
        /// </summary>
        /// <value>The normalized direction.</value>
        public UV NormalizedDirection { get; set; }
        /// <summary>
        /// Gets or sets the cell value.
        /// </summary>
        /// <value>The cell value.</value>
        public Cell CellValue { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="CellDestination"/> class.
        /// </summary>
        /// <param name="origin">The origin.</param>
        /// <param name="destination">The destination.</param>
        /// <param name="cost">The cost.</param>
        public CellDestination(Cell origin, Cell destination, double cost)
        {
            var direction = destination - origin;
            direction.Unitize();
            double dot = UV.VBase.DotProduct(direction);
            double angle;
            if (dot >= 1)
            {
                angle = 0;
            }
            else if (dot <= -1)
            {
                angle = Math.PI;
            }
            else
            {
                angle = Math.Acos(dot);
                if (angle < 0)
                {
                    angle = Math.PI - angle;
                }
            }
            double angleFactor;
            if (direction.U < 0)
            {
                angleFactor = angle;
            }
            else
            {
                angleFactor = angle + Math.PI;
            }
            this.CellValue = destination;
            this.Angle = angleFactor;
            this.NormalizedDirection = direction;
            this.DesirabilityCost = cost;
        }
        public override string ToString()
        {
            return string.Format("Angle: {0}; Cost: {1}; Direction: {2}",
                this.Angle.ToString(), this.DesirabilityCost.ToString(), this.NormalizedDirection.ToString());
        }
    }
}

