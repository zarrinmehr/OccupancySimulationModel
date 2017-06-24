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
using SpatialAnalysis.Geometry;
using SpatialAnalysis.FieldUtility;

namespace SpatialAnalysis.Data.Visualization
{
    /// <summary>
    /// Class SpatialDataCalculator.
    /// </summary>
    public class SpatialDataCalculator
    {
        /// <summary>
        /// Gets or sets the potential field generation time.
        /// </summary>
        /// <value>The potential field generation time.</value>
        public double PotentialFieldGenerationTime { get; set; }
        /// <summary>
        /// Gets or sets the rays of neighborhood range.
        /// </summary>
        /// <value>The rays.</value>
        public Dictionary<Index, WeightedIndex[]> Rays { get; set; }
        /// <summary>
        /// Gets or sets the range of neighborhood.
        /// </summary>
        /// <value>The range.</value>
        public int Range { get; set; }
        private OSMDocument _host;
        /// <summary>
        /// Initializes a new instance of the <see cref="SpatialDataCalculator"/> class.
        /// </summary>
        /// <param name="cellularFloor">The cellular floor.</param>
        /// <param name="range">The range.</param>
        /// <param name="tolerance">The tolerance.</param>
        public SpatialDataCalculator(OSMDocument host, int range, double tolerance= OSMDocument.AbsoluteTolerance)
        {
            this.Range = range;
            this._host = host;
            this.loadRays(tolerance);
        }
        private void loadRays(double tolerance)
        {
            var indices = Index.GetPotentialFieldNeighborhood(this.Range);
            this.Rays = new Dictionary<Index, WeightedIndex[]>();
            foreach (var index in indices)
            {
                var signI = Math.Sign(index.I);
                var signJ = Math.Sign(index.J);
                Index newItem = new Index(index.I * signI, index.J * signJ);
                var weightedIndices = Ray.LoadWeightingFactors(this._host.cellularFloor.Cells[0, 0],
                    this._host.cellularFloor.Cells[newItem.I, newItem.J], this._host.cellularFloor, tolerance);
                for (int i = 0; i < weightedIndices.Length; i++)
                {
                    weightedIndices[i].I *= signI;
                    if (signI < 0)
                    {
                        weightedIndices[i].I += signI;
                    }
                    weightedIndices[i].J *= signJ;
                    if (signJ < 0)
                    {
                        weightedIndices[i].J += signJ;
                    }
                }
                this.Rays.Add(index, weightedIndices);
            }
        }
        private double getStaticCostOfEdge(Index current, Index next, Dictionary<Cell, double> cost)
        {
            Index relativeIndex = next - current;
            WeightedIndex[] indicesAndWeights;
            if (!this.Rays.TryGetValue(relativeIndex, out indicesAndWeights))
            {
                throw new ArgumentException("Relative index is not in the graph neighborhood range");
            }
            double costValue = 0;
            foreach (var item in indicesAndWeights)
            {
                Index cellIndex = item + current;
                Cell cell = this._host.cellularFloor.FindCell(cellIndex);
                if (cell == null)
                {
                    throw new ArgumentException("The found cell is not part of the floor");
                }
                if (cost.ContainsKey(cell))
                {
                    //to account for the cost
                    costValue += item.WeightingFactor * cost[cell];
                    //to account for the distance
                    costValue += item.WeightingFactor;
                }
                else
                {
                    if (cell.FieldOverlapState == OverlapState.Inside)
                    {
                        cell.Visualize(this._host.OSM_to_BIM, this._host.cellularFloor.CellSize, 0);
                        throw new ArgumentException("Cost value for a cell that belongs to the field does not exist");
                    }
                    return double.PositiveInfinity;
                }
            }
            return costValue;
        }
        private double getEdgeLength(Index current, Index next)
        {
            Index relativeIndex = next - current;
            WeightedIndex[] indicesAndWeights;
            if (!this.Rays.TryGetValue(relativeIndex, out indicesAndWeights))
            {
                throw new ArgumentException("Relative index is not in the graph neighborhood range");
            }
            double costValue = 0;
            foreach (var item in indicesAndWeights)
            {
                Index cellIndex = item + current;
                Cell cell = this._host.cellularFloor.FindCell(cellIndex);
                if (cell == null)
                {
                    throw new ArgumentException("The found cell is not part of the floor");
                }
                if (cell.FieldOverlapState != OverlapState.Inside)
                {
                    return double.PositiveInfinity;
                }
                costValue += item.WeightingFactor;
            }
            return costValue;
        }
        /// <summary>
        /// Gets the static potential field for training.
        /// </summary>
        /// <param name="destination">The destination.</param>
        /// <param name="destinationArea">The destination area.</param>
        /// <param name="defaultState">The default state.</param>
        /// <param name="name">The name.</param>
        /// <param name="trail">The trail.</param>
        /// <returns>Activity.</returns>
        /// <exception cref="System.ArgumentException">Negative cost for edge found: " + edgeCost.ToString() +
        ///                                 "\nP1: " +
        ///                             this._cellularFloor.FindCell(current).Origin.ToString() + "\nP2: " + this._cellularFloor.FindCell(neighborIndex).Origin.ToString() +
        ///                             "\nDistance: " + this._cellularFloor.FindCell(current).Origin.DistanceTo(this._cellularFloor.FindCell(neighborIndex).Origin).ToString()</exception>
        public Activity GetStaticPotentialFieldForTraining(Cell destination, BarrierPolygons destinationArea, StateBase defaultState, string name, HashSet<Index> trail)
        {
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            //Calculating static costs
            Dictionary<Cell, double> costs = this._host.cellularFloor.GetStaticCost();
            #region set the initial potential to positive infinity; define heap and labeled where the potential is zero;
            SortedSet<WeightedIndex> heap = new SortedSet<WeightedIndex>();
            WeightedIndex[,] indices = new WeightedIndex[this._host.cellularFloor.GridWidth, this._host.cellularFloor.GridHeight];
            bool[,] marked = new bool[this._host.cellularFloor.GridWidth, this._host.cellularFloor.GridHeight];
            for (int i = 0; i < this._host.cellularFloor.GridWidth; i++)
            {
                for (int j = 0; j < this._host.cellularFloor.GridHeight; j++)
                {
                    indices[i, j] = new WeightedIndex(i, j, double.PositiveInfinity);
                }
            }
            var destinations = new HashSet<Cell> { destination };
            foreach (var item in destinations)
            {
                indices[item.CellToIndex.I, item.CellToIndex.J].WeightingFactor = 0;
                marked[item.CellToIndex.I, item.CellToIndex.J] = true;
                heap.Add(indices[item.CellToIndex.I, item.CellToIndex.J]);
            }
            #endregion
            HashSet<Index> trailIndices = new HashSet<Index>(trail);
            while (!(heap.Count == 0 || trailIndices.Count == 0))
            {
                var current = heap.Min;
                heap.Remove(current);
                marked[current.I, current.J] = true;
                if (trailIndices.Contains(current))
                {
                    trailIndices.Remove(current);
                }
                foreach (var item in this.Rays.Keys)
                {
                    int I = current.I + item.I;
                    int J = current.J + item.J;
                    if (this._host.cellularFloor.ContainsCell(I, J) &&
                        this._host.cellularFloor.Cells[I, J].FieldOverlapState == OverlapState.Inside &&
                        !marked[I, J])
                    {
                        var neighborIndex = indices[I, J];
                        double edgeCost = this.getStaticCostOfEdge(neighborIndex, current, costs);
                        if (edgeCost <= 0)
                        {
                            throw new ArgumentException("Negative cost for edge found: " + edgeCost.ToString() +
                                "\nP1: " +
                            this._host.cellularFloor.FindCell(current).ToString() + "\nP2: " + this._host.cellularFloor.FindCell(neighborIndex).ToString() +
                            "\nDistance: " + this._host.cellularFloor.FindCell(current).DistanceTo(this._host.cellularFloor.FindCell(neighborIndex)).ToString());
                        }
                        double newPotential = current.WeightingFactor + edgeCost;
                        if (!heap.Contains(neighborIndex))
                        {
                            neighborIndex.WeightingFactor = newPotential;
                            heap.Add(neighborIndex);
                        }
                        else
                        {
                            if (neighborIndex.WeightingFactor > newPotential)
                            {
                                heap.Remove(neighborIndex);
                                neighborIndex.WeightingFactor = newPotential;
                                heap.Add(neighborIndex);
                            }
                        }
                    }
                }
            }
            heap = null;
            marked = null;
            costs = null;
            Dictionary<Cell, double> potentials = new Dictionary<Cell, double>();
            foreach (var item in indices)
            {
                if (!double.IsInfinity(item.WeightingFactor))
                {
                    potentials.Add(this._host.cellularFloor.Cells[item.I, item.J], item.WeightingFactor);
                }
            }
            indices = null;
            timer.Stop();
            this.PotentialFieldGenerationTime = timer.Elapsed.TotalMilliseconds;
            return new Activity(potentials, destinations, destinationArea, defaultState, name, this._host.cellularFloor);
        }
        /// <summary>
        /// Calculates the activity (i.e. potential field) with respect to distance and static cost only
        /// </summary>
        /// <param name="destinations">The destinations.</param>
        /// <param name="destinationArea">The destination area.</param>
        /// <param name="defaultState">The default state.</param>
        /// <param name="name">The name of activity.</param>
        /// <returns>Activity</returns>
        /// <exception cref="System.ArgumentException">Negative cost for edge found: " + edgeCost.ToString() +
        ///                                 "\nP1: " +
        ///                             this._cellularFloor.FindCell(current).Origin.ToString() + "\nP2: " + this._cellularFloor.FindCell(neighborIndex).Origin.ToString() +
        ///                             "\nDistance: " + this._cellularFloor.FindCell(current).Origin.DistanceTo(this._cellularFloor.FindCell(neighborIndex).Origin).ToString()</exception>
        public Activity GetStaticActivity(HashSet<Cell> destinations, BarrierPolygons destinationArea, StateBase defaultState, string name)
        {
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            //Calculating static costs
            Dictionary<Cell, double> costs = this._host.cellularFloor.GetStaticCost();
            #region set the initial potential to positive infinity; define heap and labeled where the potential is zero;
            SortedSet<WeightedIndex> heap = new SortedSet<WeightedIndex>();
            WeightedIndex[,] indices = new WeightedIndex[this._host.cellularFloor.GridWidth, this._host.cellularFloor.GridHeight];
            bool[,] marked = new bool[this._host.cellularFloor.GridWidth, this._host.cellularFloor.GridHeight];
            for (int i = 0; i < this._host.cellularFloor.GridWidth; i++)
            {
                for (int j = 0; j < this._host.cellularFloor.GridHeight; j++)
                {
                    indices[i, j] = new WeightedIndex(i, j, double.PositiveInfinity);
                }
            }          
            foreach (var item in destinations)
            {
                indices[item.CellToIndex.I, item.CellToIndex.J].WeightingFactor = 0;
                marked[item.CellToIndex.I, item.CellToIndex.J] = true;
                heap.Add(indices[item.CellToIndex.I, item.CellToIndex.J]);
            }
            #endregion

            while (heap.Count != 0)
            {
                var current = heap.Min;
                heap.Remove(current);
                marked[current.I, current.J] = true;
                foreach (var item in this.Rays.Keys)
                {
                    int I = current.I + item.I;
                    int J = current.J + item.J;
                    if (this._host.cellularFloor.ContainsCell(I, J) &&
                        this._host.cellularFloor.Cells[I, J].FieldOverlapState == OverlapState.Inside &&
                        !marked[I, J])
                    {
                        var neighborIndex = indices[I, J];
                        double edgeCost = this.getStaticCostOfEdge(neighborIndex, current, costs);
                        if (edgeCost <= 0)
                        {
                            throw new ArgumentException("Negative cost for edge found: " + edgeCost.ToString() +
                                "\nP1: " +
                            this._host.cellularFloor.FindCell(current).ToString() + "\nP2: " + this._host.cellularFloor.FindCell(neighborIndex).ToString() +
                            "\nDistance: " + this._host.cellularFloor.FindCell(current).DistanceTo(this._host.cellularFloor.FindCell(neighborIndex)).ToString());
                        }
                        double newPotential = current.WeightingFactor + edgeCost;
                        if (!heap.Contains(neighborIndex))
                        {
                            neighborIndex.WeightingFactor = newPotential;
                            heap.Add(neighborIndex);
                        }
                        else
                        {
                            if (neighborIndex.WeightingFactor > newPotential)
                            {
                                heap.Remove(neighborIndex);
                                neighborIndex.WeightingFactor = newPotential;
                                heap.Add(neighborIndex);
                            }
                        }
                    }
                }
            }
            heap = null;
            marked = null;
            costs = null;
            Dictionary<Cell, double> potentials = new Dictionary<Cell, double>();
            foreach (var item in indices)
            {
                if (!double.IsInfinity(item.WeightingFactor))
                {
                    potentials.Add(this._host.cellularFloor.Cells[item.I, item.J], item.WeightingFactor);
                }
            }
            indices = null;
            timer.Stop();
            this.PotentialFieldGenerationTime = timer.Elapsed.TotalMilliseconds;
            return new Activity(potentials, destinations, destinationArea, defaultState, name, this._host.cellularFloor);
        }

        /// <summary>
        /// Calculates the activity (i.e. potential field) with respect to distance and static cost only
        /// </summary>
        /// <param name="activityDestination">The activity destination.</param>
        /// <returns>Activity.</returns>
        public Activity GetStaticActivity(ActivityDestination activityDestination)
        {
            return this.GetStaticActivity(activityDestination.Origins, activityDestination.DestinationArea, activityDestination.DefaultState, activityDestination.Name);
        }
        /// <summary>
        /// Gets the dynamic potential field for training.
        /// </summary>
        /// <param name="destination">The destination.</param>
        /// <param name="destinationArea">The destination area.</param>
        /// <param name="defaultState">The default state.</param>
        /// <param name="name">The name.</param>
        /// <param name="angleCost">The angle cost.</param>
        /// <param name="trail">The trail.</param>
        /// <returns>Activity.</returns>
        /// <exception cref="System.ArgumentException">Negative cost for edge found: " + edgeCost.ToString() +
        ///                                 "\nP1: " +
        ///                             this._cellularFloor.FindCell(current).Origin.ToString() + "\nP2: " + this._cellularFloor.FindCell(neighborIndex).Origin.ToString() +
        ///                             "\nDistance: " + this._cellularFloor.FindCell(current).Origin.DistanceTo(this._cellularFloor.FindCell(neighborIndex).Origin).ToString()</exception>
        public Activity GetDynamicPotentialFieldForTraining(Cell destination, BarrierPolygons destinationArea, StateBase defaultState, string name, double angleCost, HashSet<Index> trail)
        {
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            var destinations = new HashSet<Cell> { destination };
            var trailIndices = new HashSet<Index>(trail);
            //Calculating static costs
            Dictionary<Cell, double> costs = this._host.cellularFloor.GetStaticCost();
            #region set the initial potential to positive infinity; define heap and labeled where the potential is zero;
            SortedSet<WeightedIndex> heap = new SortedSet<WeightedIndex>();
            WeightedIndex[,] indices = new WeightedIndex[this._host.cellularFloor.GridWidth, this._host.cellularFloor.GridHeight];
            bool[,] marked = new bool[this._host.cellularFloor.GridWidth, this._host.cellularFloor.GridHeight];
            UV[,] previousOrigins = new UV[this._host.cellularFloor.GridWidth, this._host.cellularFloor.GridHeight];
            for (int i = 0; i < this._host.cellularFloor.GridWidth; i++)
            {
                for (int j = 0; j < this._host.cellularFloor.GridHeight; j++)
                {
                    indices[i, j] = new WeightedIndex(i, j, double.PositiveInfinity);
                }
            }
            foreach (var item in destinations)
            {
                indices[item.CellToIndex.I, item.CellToIndex.J].WeightingFactor = 0;
                marked[item.CellToIndex.I, item.CellToIndex.J] = true;
                heap.Add(indices[item.CellToIndex.I, item.CellToIndex.J]);
            }
            #endregion

            while (!(heap.Count == 0 || trailIndices.Count == 0))
            {
                var current = heap.Min;
                heap.Remove(current);
                marked[current.I, current.J] = true;
                if (trailIndices.Contains(current))
                {
                    trailIndices.Remove(current);
                }
                UV currentDirection = null;
                if (previousOrigins[current.I, current.J] != null)
                {
                    currentDirection = this._host.cellularFloor.Cells[current.I, current.J] - previousOrigins[current.I, current.J];
                    currentDirection.Unitize();
                }
                foreach (var item in this.Rays.Keys)
                {
                    int I = current.I + item.I;
                    int J = current.J + item.J;
                    if (this._host.cellularFloor.ContainsCell(I, J) &&
                        this._host.cellularFloor.Cells[I, J].FieldOverlapState == OverlapState.Inside &&
                        !marked[I, J])
                    {
                        var neighborIndex = indices[I, J];
                        double edgeCost = this.getStaticCostOfEdge(neighborIndex, current, costs);
                        if (edgeCost <= 0)
                        {
                            throw new ArgumentException("Negative cost for edge found: " + edgeCost.ToString() +
                                "\nP1: " +
                            this._host.cellularFloor.FindCell(current).ToString() + "\nP2: " + this._host.cellularFloor.FindCell(neighborIndex).ToString() +
                            "\nDistance: " + this._host.cellularFloor.FindCell(current).DistanceTo(this._host.cellularFloor.FindCell(neighborIndex)).ToString());
                        }
                        double newPotential = current.WeightingFactor + edgeCost;
                        if (currentDirection != null)
                        {
                            var newDirection = this._host.cellularFloor.Cells[current.I, current.J] - this._host.cellularFloor.Cells[I, J];
                            newDirection.Unitize();
                            double angle = Math.Abs(newDirection.AngleTo(currentDirection));
                            newPotential += angleCost * (Math.PI - Math.Abs(angle));
                        }
                        if (!heap.Contains(neighborIndex))
                        {
                            previousOrigins[neighborIndex.I, neighborIndex.J] = this._host.cellularFloor.Cells[current.I, current.J];
                            neighborIndex.WeightingFactor = newPotential;
                            heap.Add(neighborIndex);
                        }
                        else
                        {
                            if (neighborIndex.WeightingFactor > newPotential)
                            {
                                previousOrigins[neighborIndex.I, neighborIndex.J] = this._host.cellularFloor.Cells[current.I, current.J];
                                heap.Remove(neighborIndex);
                                neighborIndex.WeightingFactor = newPotential;
                                heap.Add(neighborIndex);
                            }
                        }
                    }
                }
            }
            heap = null;
            marked = null;
            costs = null;
            previousOrigins = null;
            Dictionary<Cell, double> potentials = new Dictionary<Cell, double>();
            foreach (var item in indices)
            {
                if (!double.IsInfinity(item.WeightingFactor))
                {
                    potentials.Add(this._host.cellularFloor.Cells[item.I, item.J], item.WeightingFactor);
                }
            }
            indices = null;
            timer.Stop();
            this.PotentialFieldGenerationTime = timer.Elapsed.TotalMilliseconds;
            return new Activity(potentials, destinations, destinationArea, defaultState, name, this._host.cellularFloor);
        }
        /// <summary>
        /// Calculates the activity (i.e. potential field) with respect to distance, static cost and angle.
        /// </summary>
        /// <param name="destinations">The destinations.</param>
        /// <param name="destinationArea">The destination area.</param>
        /// <param name="defaultState">The default state.</param>
        /// <param name="name">The name.</param>
        /// <param name="angleCost">The angle cost.</param>
        /// <returns>Potential Field</returns>
        /// <exception cref="System.ArgumentException">Negative cost for edge found: " + edgeCost.ToString() +
        ///                                 "\nP1: " +
        ///                             this._cellularFloor.FindCell(current).Origin.ToString() + "\nP2: " + this._cellularFloor.FindCell(neighborIndex).Origin.ToString() +
        ///                             "\nDistance: " + this._cellularFloor.FindCell(current).Origin.DistanceTo(this._cellularFloor.FindCell(neighborIndex).Origin).ToString()</exception>
        public Activity GetDynamicActivity(HashSet<Cell> destinations, BarrierPolygons destinationArea, StateBase defaultState, string name, double angleCost)
        {
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            //Calculating static costs
            Dictionary<Cell, double> costs = this._host.cellularFloor.GetStaticCost();
            #region set the initial potential to positive infinity; define heap and labeled where the potential is zero;
            SortedSet<WeightedIndex> heap = new SortedSet<WeightedIndex>();
            WeightedIndex[,] indices = new WeightedIndex[this._host.cellularFloor.GridWidth, this._host.cellularFloor.GridHeight];
            bool[,] marked = new bool[this._host.cellularFloor.GridWidth, this._host.cellularFloor.GridHeight];
            UV[,] previousOrigins = new UV[this._host.cellularFloor.GridWidth, this._host.cellularFloor.GridHeight];
            for (int i = 0; i < this._host.cellularFloor.GridWidth; i++)
            {
                for (int j = 0; j < this._host.cellularFloor.GridHeight; j++)
                {
                    indices[i, j] = new WeightedIndex(i, j, double.PositiveInfinity);
                }
            }
            foreach (var item in destinations)
            {
                indices[item.CellToIndex.I, item.CellToIndex.J].WeightingFactor = 0;
                marked[item.CellToIndex.I, item.CellToIndex.J] = true;
                heap.Add(indices[item.CellToIndex.I, item.CellToIndex.J]);
            }
            #endregion

            while (heap.Count != 0)
            {
                var current = heap.Min;
                heap.Remove(current);
                marked[current.I, current.J] = true;
                UV currentDirection = null;
                if (previousOrigins[current.I, current.J] != null)
                {
                    currentDirection = this._host.cellularFloor.Cells[current.I, current.J] - previousOrigins[current.I, current.J];
                    currentDirection.Unitize();
                }
                foreach (var item in this.Rays.Keys)
                {
                    int I = current.I + item.I;
                    int J = current.J + item.J;
                    if (this._host.cellularFloor.ContainsCell(I, J) &&
                        this._host.cellularFloor.Cells[I, J].FieldOverlapState == OverlapState.Inside &&
                        !marked[I, J])
                    {
                        var neighborIndex = indices[I, J];
                        double edgeCost = this.getStaticCostOfEdge(neighborIndex, current, costs);
                        if (edgeCost <= 0)
                        {
                            throw new ArgumentException("Negative cost for edge found: " + edgeCost.ToString() +
                                "\nP1: " +
                            this._host.cellularFloor.FindCell(current).ToString() + "\nP2: " + this._host.cellularFloor.FindCell(neighborIndex).ToString() +
                            "\nDistance: " + this._host.cellularFloor.FindCell(current).DistanceTo(this._host.cellularFloor.FindCell(neighborIndex)).ToString());
                        }
                        double newPotential = current.WeightingFactor + edgeCost;
                        if (currentDirection != null)
                        {
                            var newDirection = this._host.cellularFloor.Cells[current.I, current.J] - this._host.cellularFloor.Cells[I, J];
                            newDirection.Unitize();
                            double angle = Math.Abs(newDirection.AngleTo(currentDirection));
                            newPotential += angleCost * (Math.PI - Math.Abs(angle));
                        }
                        if (!heap.Contains(neighborIndex))
                        {
                            previousOrigins[neighborIndex.I, neighborIndex.J] = this._host.cellularFloor.Cells[current.I, current.J];
                            neighborIndex.WeightingFactor = newPotential;
                            heap.Add(neighborIndex);
                        }
                        else
                        {
                            if (neighborIndex.WeightingFactor > newPotential)
                            {
                                previousOrigins[neighborIndex.I, neighborIndex.J] = this._host.cellularFloor.Cells[current.I, current.J];
                                heap.Remove(neighborIndex);
                                neighborIndex.WeightingFactor = newPotential;
                                heap.Add(neighborIndex);
                            }
                        }
                    }
                }
            }
            heap = null;
            marked = null;
            costs = null;
            previousOrigins = null;
            Dictionary<Cell, double> potentials = new Dictionary<Cell, double>();
            foreach (var item in indices)
            {
                if (!double.IsInfinity(item.WeightingFactor))
                {
                    potentials.Add(this._host.cellularFloor.Cells[item.I, item.J], item.WeightingFactor);
                }
            }
            indices = null;
            timer.Stop();
            this.PotentialFieldGenerationTime = timer.Elapsed.TotalMilliseconds;
            return new Activity(potentials, destinations, destinationArea, defaultState, name, this._host.cellularFloor);
        }
        /// <summary>
        /// Calculates the activity (i.e. potential field) with respect to distance, static cost and angle.
        /// </summary>
        /// <param name="activityDestination">The activity destination.</param>
        /// <param name="angleCost">The angle cost.</param>
        /// <returns>Activity.</returns>
        public Activity GetDynamicActivity(ActivityDestination activityDestination, double angleCost)
        {
            return this.GetDynamicActivity(activityDestination.Origins, activityDestination.DestinationArea, activityDestination.DefaultState, activityDestination.Name, angleCost);
        }
        /// <summary>
        /// Calculates the activity (i.e. potential field) with respect to distance, static cost and angle.
        /// </summary>
        /// <param name="destination">The destination.</param>
        /// <param name="destinationArea">The destination area.</param>
        /// <param name="defaultState">The default state.</param>
        /// <param name="name">The name.</param>
        /// <param name="angleCost">The angle cost.</param>
        /// <param name="radius">The radius.</param>
        /// <returns>Activity.</returns>
        /// <exception cref="System.ArgumentException">Negative cost for edge found: " + edgeCost.ToString() +
        ///                                 "\nP1: " +
        ///                             this._cellularFloor.FindCell(current).Origin.ToString() + "\nP2: " + this._cellularFloor.FindCell(neighborIndex).Origin.ToString() +
        ///                             "\nDistance: " + this._cellularFloor.FindCell(current).Origin.DistanceTo(this._cellularFloor.FindCell(neighborIndex).Origin).ToString()</exception>
        public Activity GetDynamicActivity(Cell destination, BarrierPolygons destinationArea, StateBase defaultState, string name, double angleCost, double radius)
        {
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            //Calculating static costs
            Dictionary<Cell, double> costs = this._host.cellularFloor.GetStaticCost();
            #region set the initial potential to positive infinity; define heap and labeled where the potential is zero;
            SortedSet<WeightedIndex> heap = new SortedSet<WeightedIndex>();
            WeightedIndex[,] indices = new WeightedIndex[this._host.cellularFloor.GridWidth, this._host.cellularFloor.GridHeight];
            bool[,] marked = new bool[this._host.cellularFloor.GridWidth, this._host.cellularFloor.GridHeight];
            UV[,] previousOrigins = new UV[this._host.cellularFloor.GridWidth, this._host.cellularFloor.GridHeight];
            for (int i = 0; i < this._host.cellularFloor.GridWidth; i++)
            {
                for (int j = 0; j < this._host.cellularFloor.GridHeight; j++)
                {
                    indices[i, j] = new WeightedIndex(i, j, double.PositiveInfinity);
                }
            }
            indices[destination.CellToIndex.I, destination.CellToIndex.J].WeightingFactor = 0;
            marked[destination.CellToIndex.I, destination.CellToIndex.J] = true;
            heap.Add(indices[destination.CellToIndex.I, destination.CellToIndex.J]);
            #endregion
            var radiusSquared = radius * radius;
            var target = this._host.cellularFloor.Cells[destination.CellToIndex.I, destination.CellToIndex.J];
            while (heap.Count != 0)
            {
                var current = heap.Min;
                heap.Remove(current);
                marked[current.I, current.J] = true;
                UV currentDirection = null;
                if (previousOrigins[current.I, current.J] != null)
                {
                    currentDirection = this._host.cellularFloor.Cells[current.I, current.J] - previousOrigins[current.I, current.J];
                    currentDirection.Unitize();
                }
                foreach (var item in this.Rays.Keys)
                {
                    int I = current.I + item.I;
                    int J = current.J + item.J;
                    if (this._host.cellularFloor.ContainsCell(I, J) &&
                        this._host.cellularFloor.Cells[I, J].FieldOverlapState == OverlapState.Inside &&
                        !marked[I, J]&&
                        radiusSquared >= UV.GetLengthSquared(this._host.cellularFloor.Cells[I, J], target))
                    {
                        var neighborIndex = indices[I, J];
                        double edgeCost = this.getStaticCostOfEdge(neighborIndex, current, costs);
                        if (edgeCost <= 0)
                        {
                            throw new ArgumentException("Negative cost for edge found: " + edgeCost.ToString() +
                                "\nP1: " +
                            this._host.cellularFloor.FindCell(current).ToString() + "\nP2: " + this._host.cellularFloor.FindCell(neighborIndex).ToString() +
                            "\nDistance: " + this._host.cellularFloor.FindCell(current).DistanceTo(this._host.cellularFloor.FindCell(neighborIndex)).ToString());
                        }
                        double newPotential = current.WeightingFactor + edgeCost;
                        if (currentDirection != null)
                        {
                            var newDirection = this._host.cellularFloor.Cells[current.I, current.J] - this._host.cellularFloor.Cells[I, J];
                            newDirection.Unitize();
                            double angle = Math.Abs(newDirection.AngleTo(currentDirection));
                            newPotential += angleCost * (Math.PI - Math.Abs(angle));
                        }
                        if (!heap.Contains(neighborIndex))
                        {
                            previousOrigins[neighborIndex.I, neighborIndex.J] = this._host.cellularFloor.Cells[current.I, current.J];
                            neighborIndex.WeightingFactor = newPotential;
                            heap.Add(neighborIndex);
                        }
                        else
                        {
                            if (neighborIndex.WeightingFactor > newPotential)
                            {
                                previousOrigins[neighborIndex.I, neighborIndex.J] = this._host.cellularFloor.Cells[current.I, current.J];
                                heap.Remove(neighborIndex);
                                neighborIndex.WeightingFactor = newPotential;
                                heap.Add(neighborIndex);
                            }
                        }
                    }
                }
            }
            heap = null;
            marked = null;
            costs = null;
            previousOrigins = null;
            Dictionary<Cell, double> potentials = new Dictionary<Cell, double>();
            foreach (var item in indices)
            {
                if (!double.IsInfinity(item.WeightingFactor))
                {
                    potentials.Add(this._host.cellularFloor.Cells[item.I, item.J], item.WeightingFactor);
                }
            }
            indices = null;
            timer.Stop();
            this.PotentialFieldGenerationTime = timer.Elapsed.TotalMilliseconds;
            return new Activity(potentials, new HashSet<Cell> { destination }, destinationArea, defaultState, name, this._host.cellularFloor);
        }

        /// <summary>
        /// Calculates the activity (i.e. potential field) with respect to distance, static cost and angle.
        /// </summary>
        /// <param name="activityDestination">The activity destination.</param>
        /// <param name="angleCost">The angle cost.</param>
        /// <param name="radius">The radius.</param>
        /// <returns>Activity.</returns>
        public Activity GetDynamicActivity(ActivityDestination activityDestination, double angleCost, double radius)
        {
            return this.GetDynamicActivity(activityDestination.Origins.First(), activityDestination.DestinationArea, activityDestination.DefaultState, activityDestination.Name, angleCost, radius);
        }

        /// <summary>
        /// Gets the spatial data field.
        /// </summary>
        /// <param name="destinations">The destinations.</param>
        /// <param name="initialValue">The initial value.</param>
        /// <param name="name">The name of data.</param>
        /// <param name="interpolation">The interpolation function.</param>
        /// <returns>SpatialAnalysis.Data.SpatialDataField.</returns>
        public SpatialAnalysis.Data.SpatialDataField GetSpatialDataField(HashSet<Index> destinations, double initialValue, string name, Func<double, double> interpolation)
        {
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            #region set the initial potential to positive infinity; define heap and labeled where the potential is zero;
            SortedSet<WeightedIndex> heap = new SortedSet<WeightedIndex>();
            WeightedIndex[,] indices = new WeightedIndex[this._host.cellularFloor.GridWidth, this._host.cellularFloor.GridHeight];
            bool[,] marked = new bool[this._host.cellularFloor.GridWidth, this._host.cellularFloor.GridHeight];
            for (int i = 0; i < this._host.cellularFloor.GridWidth; i++)
            {
                for (int j = 0; j < this._host.cellularFloor.GridHeight; j++)
                {
                    indices[i, j] = new WeightedIndex(i, j, double.PositiveInfinity);
                }
            }
            foreach (var item in destinations)
            {
                indices[item.I, item.J].WeightingFactor = initialValue;
                marked[item.I, item.J] = true;
                heap.Add(indices[item.I, item.J]);
            }
            #endregion

            while (heap.Count != 0)
            {
                var current = heap.Min;
                heap.Remove(current);
                marked[current.I, current.J] = true;
                foreach (var item in this.Rays.Keys)
                {
                    int I = current.I + item.I;
                    int J = current.J + item.J;
                    if (this._host.cellularFloor.ContainsCell(I, J) &&
                        this._host.cellularFloor.Cells[I, J].FieldOverlapState == OverlapState.Inside &&
                        !marked[I, J])
                    {
                        var neighborIndex = indices[I, J];
                        double edgeCost = this.getEdgeLength(neighborIndex, current);
                        double newPotential = current.WeightingFactor + edgeCost;
                        if (!heap.Contains(neighborIndex))
                        {
                            neighborIndex.WeightingFactor = newPotential;
                            heap.Add(neighborIndex);
                        }
                        else
                        {
                            if (neighborIndex.WeightingFactor > newPotential)
                            {
                                heap.Remove(neighborIndex);
                                neighborIndex.WeightingFactor = newPotential;
                                heap.Add(neighborIndex);
                            }
                        }
                    }
                }
            }
            heap = null;
            marked = null;
            Dictionary<Cell, double> potentials = new Dictionary<Cell, double>();
            foreach (var item in indices)
            {
                if (!double.IsInfinity(item.WeightingFactor))
                {
                    double value_ = interpolation(item.WeightingFactor);
                    if (!double.IsInfinity(value_))
                    {
                        potentials.Add(this._host.cellularFloor.Cells[item.I, item.J], value_);
                    }
                }
            }
            indices = null;
            timer.Stop();
            this.PotentialFieldGenerationTime = timer.Elapsed.TotalMilliseconds;
            return new Data.SpatialDataField(name, potentials);
        }
        /// <summary>
        /// This method return a data field in which the values assigned to visible cells from a list of vantage cells are calculated through an interpolation function.
        /// </summary>
        /// <param name="vantageCells">The destinations.</param>
        /// <param name="initialValue">The initial value.</param>
        /// <param name="notVisibleCellIds">The cell IDs that are not visible.</param>
        /// <param name="notVisibleValue">The value of not visible cells.</param>
        /// <param name="name">The name of the data.</param>
        /// <param name="interpolation">The interpolation function.</param>
        /// <returns>SpatialAnalysis.Data.SpatialDataField.</returns>
        public SpatialAnalysis.Data.SpatialDataField GetSpatialDataField(HashSet<int> vantageCells, double initialValue, HashSet<int> notVisibleCellIds, 
            double notVisibleValue, string name, Func<double, double> interpolation)
        {
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            #region set the initial potential to positive infinity; define heap and labeled where the potential is zero;
            SortedSet<WeightedIndex> heap = new SortedSet<WeightedIndex>();
            WeightedIndex[,] indices = new WeightedIndex[this._host.cellularFloor.GridWidth, this._host.cellularFloor.GridHeight];
            bool[,] marked = new bool[this._host.cellularFloor.GridWidth, this._host.cellularFloor.GridHeight];
            for (int i = 0; i < this._host.cellularFloor.GridWidth; i++)
            {
                for (int j = 0; j < this._host.cellularFloor.GridHeight; j++)
                {
                    indices[i, j] = new WeightedIndex(i, j, double.PositiveInfinity);
                }
            }
            foreach (var id in vantageCells)
            {
                Cell cell = this._host.cellularFloor.FindCell(id);
                indices[cell.CellToIndex.I, cell.CellToIndex.J].WeightingFactor = initialValue;
                marked[cell.CellToIndex.I, cell.CellToIndex.J] = true;
                heap.Add(indices[cell.CellToIndex.I, cell.CellToIndex.J]);
            }
            foreach (int id in notVisibleCellIds)
            {
                var cell = this._host.cellularFloor.FindCell(id);
                marked[cell.CellToIndex.I, cell.CellToIndex.J] = true;
            }
            #endregion

            while (heap.Count != 0)
            {
                var current = heap.Min;
                heap.Remove(current);
                marked[current.I, current.J] = true;
                foreach (var item in this.Rays.Keys)
                {
                    int I = current.I + item.I;
                    int J = current.J + item.J;
                    if (this._host.cellularFloor.ContainsCell(I, J) &&
                        this._host.cellularFloor.Cells[I, J].FieldOverlapState == OverlapState.Inside &&
                        !marked[I, J])
                    {
                        var neighborIndex = indices[I, J];
                        double edgeCost = this.getEdgeLength(neighborIndex, current);
                        double newPotential = current.WeightingFactor + edgeCost;
                        if (!heap.Contains(neighborIndex))
                        {
                            neighborIndex.WeightingFactor = newPotential;
                            heap.Add(neighborIndex);
                        }
                        else
                        {
                            if (neighborIndex.WeightingFactor > newPotential)
                            {
                                heap.Remove(neighborIndex);
                                neighborIndex.WeightingFactor = newPotential;
                                heap.Add(neighborIndex);
                            }
                        }
                    }
                }
            }
            heap = null;
            marked = null;
            foreach (var item in indices)
            {
                if (notVisibleCellIds.Contains(this._host.cellularFloor.Cells[item.I, item.J].ID))
                {
                    item.WeightingFactor = notVisibleValue;
                }
            }
            Dictionary<Cell, double> potentials = new Dictionary<Cell, double>();
            foreach (var item in indices)
            {
                if (!double.IsInfinity(item.WeightingFactor))
                {
                    double value_ = interpolation(item.WeightingFactor);
                    if (!double.IsInfinity(value_))
                    {
                        potentials.Add(this._host.cellularFloor.Cells[item.I, item.J], value_);
                    }
                }
            }
            indices = null;
            timer.Stop();
            this.PotentialFieldGenerationTime = timer.Elapsed.TotalMilliseconds;
            return new Data.SpatialDataField(name, potentials);
        }
    }
}

