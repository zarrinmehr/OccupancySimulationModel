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
using System.Windows;
using MathNet.Numerics.Interpolation;
using SpatialAnalysis.CellularEnvironment;
using SpatialAnalysis.Geometry;
using SpatialAnalysis.Data;
using SpatialAnalysis.Miscellaneous;
using SpatialAnalysis.Optimization;
using SpatialAnalysis.Events;
using System.IO;
using SpatialAnalysis.Interoperability;

namespace SpatialAnalysis.FieldUtility
{
    /// <summary>
    /// Class ActivityDestination.
    /// </summary>
    public class ActivityDestination
    {
        /// <summary>
        /// Gets or sets the origins of the activity.
        /// </summary>
        /// <value>The origins.</value>
        public HashSet<Cell> Origins { get; set; }
        /// <summary>
        /// Gets or sets the default state of the agent in the activity area.
        /// </summary>
        /// <value>The default state.</value>
        public StateBase DefaultState { get; set; }
        private BarrierPolygons _destinationArea;
        /// <summary>
        /// A polygon that shows the destination area
        /// </summary>
        public BarrierPolygons DestinationArea { get { return _destinationArea; } }

        /// <summary>
        /// Gets or sets the name of the engaged activity.
        /// </summary>
        /// <value>The name of the engaged activity.</value>
        public string EngagedActivityName { get; set; }
        /// <summary>
        /// Gets the name of the activity.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get { return this.EngagedActivityName; } }

        private double _maximumEngagementTime = 60.0d;
        /// <summary>
        /// Gets or sets the maximum engagement time.
        /// </summary>
        /// <value>The maximum engagement time.</value>
        /// <exception cref="System.ArgumentException">'Maximum Engagement Time' should be larger than 'Minimum Engagement Time'</exception>
        public double MaximumEngagementTime
        {
            get { return _maximumEngagementTime; }
            set
            {
                if (this._maximumEngagementTime != value)
                {
                    if (value > this.MinimumEngagementTime)
                    {
                        this._maximumEngagementTime = value;
                    }
                    else
                    {
                        throw new ArgumentException("'Maximum Engagement Time' should be larger than 'Minimum Engagement Time'");
                    }
                }
            }
        }

        private double _minimumEngagementTime = 10.0d;
        /// <summary>
        /// Gets or sets the minimum engagement time.
        /// </summary>
        /// <value>The minimum engagement time.</value>
        /// <exception cref="System.ArgumentException">'Minimum Engagement Time' should be smaller than 'Maximum Engagement Time' and larger than zero</exception>
        public double MinimumEngagementTime
        {
            get { return _minimumEngagementTime; }
            set
            {
                if (this._minimumEngagementTime != value)
                {
                    if (this._minimumEngagementTime > 0.0d && this._minimumEngagementTime < this.MaximumEngagementTime)
                    {
                        this._minimumEngagementTime = value;
                    }
                    else
                    {
                        throw new ArgumentException("'Minimum Engagement Time' should be smaller than 'Maximum Engagement Time' and larger than zero");
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityDestination"/> class.
        /// </summary>
        /// <param name="name">The name of activity.</param>
        /// <param name="origins">The origins of the activity.</param>
        /// <param name="defaultState">The default state of the agent in the activity area.</param>
        /// <param name="destinationArea">The destination area.</param>
        public ActivityDestination(string name, HashSet<Cell> origins, StateBase defaultState, BarrierPolygons destinationArea)
        {
            this.DefaultState = defaultState;
            this._destinationArea = destinationArea;
            //trim the unwanted chars from the input name
            char[] charsToTrim = {' ', '\''};
            this.EngagedActivityName = name.Trim(charsToTrim); 
            this.Origins = origins;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityDestination"/> class.
        /// </summary>
        /// <param name="activityDestination">The activity destination to be copied deeply.</param>
        public ActivityDestination(ActivityDestination activityDestination)
        {
            this.EngagedActivityName=activityDestination.Name;
            this._destinationArea=activityDestination.DestinationArea;
            this.DefaultState = activityDestination.DefaultState;
            this._minimumEngagementTime= activityDestination._minimumEngagementTime;
            this._maximumEngagementTime=activityDestination._maximumEngagementTime;
            this.Origins = activityDestination.Origins;
        }
        /// <summary>
        /// Tries the set engagement time.
        /// </summary>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        /// <returns><c>true</c> if engagement time set, <c>false</c> otherwise.</returns>
        public bool TrySetEngagementTime(double min, double max)
        {
            if (min > 0 && max > min)
            {
                this._minimumEngagementTime = min;
                this._maximumEngagementTime = max;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the string representation of this instance.
        /// </summary>
        /// <returns>System.String.</returns>
        public string GetStringRepresentation()
        {
            StringBuilder sb = new StringBuilder();
            //sb.AppendLine("# Name:");
            sb.AppendLine(this.Name);
            //sb.AppendLine("# Default State:");
            sb.AppendLine(this.DefaultState.ToString());
            //sb.AppendLine("# Area");
            sb.AppendLine(this.DestinationArea.ToString());
            //sb.AppendLine("# Engagement Time:");
            sb.AppendLine(this.MinimumEngagementTime.ToString() + "," + this.MaximumEngagementTime.ToString());
            string s = sb.ToString();
            sb.Clear();
            sb = null;
            return s;
        }
        /// <summary>
        /// creates an Activity Destination from its string representation
        /// </summary>
        /// <param name="lines">The lines.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="cellularFloor">The cellular floor.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>ActivityDestination.</returns>
        /// <exception cref="System.ArgumentException">
        /// Activity does not include a name!
        /// or
        /// Failed to parse activity's engagement duration: " + lines[startIndex + 3]
        /// or
        /// Activity does not include cell origins!
        /// or
        /// Failed to set activity engagement duration!
        /// </exception>
        public static ActivityDestination FromString(List<string> lines, int startIndex, Length_Unit_Types unitType, CellularFloor cellularFloor, double tolerance = 0.0000001d)
        {
            string name = lines[startIndex];
            if (string.IsNullOrEmpty(lines[startIndex]) || string.IsNullOrWhiteSpace(lines[startIndex]))
            {
                throw new ArgumentException("Activity does not include a name!");
            }
            StateBase state = StateBase.FromStringRepresentation(lines[startIndex + 1]);
            BarrierPolygons barrier = BarrierPolygons.FromStringRepresentation(lines[startIndex + 2]);
            //unit converion
            UnitConversion.Transform(state.Location, unitType, cellularFloor.UnitType);
            UnitConversion.Transform(state.Velocity, unitType, cellularFloor.UnitType);
            UnitConversion.Transform(barrier.BoundaryPoints, unitType, cellularFloor.UnitType);

            var strings = lines[startIndex + 3].Split(',');
            double min = 0, max = 0; 
            if (!double.TryParse(strings[0], out min) || !double.TryParse(strings[1], out max))
            {
                throw new ArgumentException("Failed to parse activity's engagement duration: " + lines[startIndex + 3]);
            }
            HashSet<Cell> origins = new HashSet<Cell>();
            var indices = cellularFloor.GetIndicesInsideBarrier(barrier, tolerance);
            if (indices.Count > 0)
            {
                foreach (var index in indices)
                {
                    Cell cell = cellularFloor.FindCell(index);
                    if (cell != null && cell.FieldOverlapState == OverlapState.Inside)
                    {
                        origins.Add(cell);
                    }
                }
            }
            if (origins.Count == 0)
            {
                throw new ArgumentException("Activity does not include cell origins!");
            }
            ActivityDestination dest = new ActivityDestination(name, origins, state, barrier);
            if (!dest.TrySetEngagementTime(min, max))
            {
                throw new ArgumentException("Failed to set activity engagement duration!");
            }
            return dest;
        }

    }


    /// <summary>
    /// Class Activity.
    /// </summary>
    /// <seealso cref="SpatialAnalysis.FieldUtility.ActivityDestination" />
    /// <seealso cref="SpatialAnalysis.Data.ISpatialData" />
    public class Activity : ActivityDestination, ISpatialData
    {
        /// <summary>
        /// The gradient interpolation neighborhood size
        /// </summary>
        public static int GradientInterpolationNeighborhoodSize = 1;
        /// <summary>
        /// The interpolation method
        /// </summary>
        public static InterpolationMethod InterpolationMethod = InterpolationMethod.CubicSpline;
        /// <summary>
        /// Indicates whether to use engagement with this activity in capturing event].
        /// </summary>
        /// <value><c>true</c> if it should be used to capture events; otherwise, <c>false</c>.</value>
        public bool UseToCaptureEvent { get; set; }
        /// <summary>
        /// Gets or sets the potentials of the walkable floor for this activity.
        /// </summary>
        /// <value>The potentials.</value>
        public Dictionary<Cell, double> Potentials { get; set; }
        /// <summary>
        /// Returns the potentials of the walkable floor for this activity.
        /// </summary>
        /// <value>The data.</value>
        public Dictionary<Cell, double> Data { get { return this.Potentials; } }
        private CellularFloor _cellularFloor { get; set; }
        /// <summary>
        /// Gets the type of data.
        /// </summary>
        /// <value>The type.</value>
        public DataType Type { get { return DataType.ActivityPotentialField; } } 
        double _min;
        /// <summary>
        /// Gets the minimum value of the data.
        /// </summary>
        /// <value>The minimum.</value>
        public double Min { get { return _min; } }
        double _max;
        /// <summary>
        /// Gets the maximum value of the data.
        /// </summary>
        /// <value>The maximum.</value>
        public double Max { get { return _max; } }



        /// <summary>
        /// Initializes a new instance of the <see cref="Activity"/> class.
        /// </summary>
        /// <param name="potentials">The potentials.</param>
        /// <param name="origins">The origins.</param>
        /// <param name="destinationArea">The destination area.</param>
        /// <param name="defaultState">The default state.</param>
        /// <param name="engagedActivityName">Name of the engaged activity.</param>
        /// <param name="cellularFloor">The cellular floor.</param>
        public Activity(Dictionary<Cell, double> potentials, HashSet<Cell> origins, BarrierPolygons destinationArea, StateBase defaultState, string engagedActivityName, CellularFloor cellularFloor)
            : base(engagedActivityName, origins, defaultState, destinationArea)
        {
            this.Potentials = potentials;
            this.EngagedActivityName = engagedActivityName;
            this._cellularFloor = cellularFloor;
            this._min = double.PositiveInfinity;
            this._max = double.NegativeInfinity;
            foreach (var item in this.Potentials.Values)
            {
                this._max = (this._max < item) ? item : this._max;
                this._min = (this._min > item) ? item : this._min;
            }
            this.Origins = new HashSet<Cell>(origins);
            this.DefaultState = defaultState.Copy();
            //this._destinationArea = destinationArea;
            this.UseToCaptureEvent = true;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Activity"/> class.
        /// </summary>
        /// <param name="activityDestination">The activity destination.</param>
        /// <param name="cellularFloor">The cellular floor.</param>
        public Activity(ActivityDestination activityDestination, CellularFloor cellularFloor)
            : base(activityDestination)
        {
            this._cellularFloor = cellularFloor;
            var indices = this._cellularFloor.GetIndicesInsideBarrier(activityDestination.DestinationArea, .000001);
            

        }

        #region Gradient Interpolation

        private IInterpolation toInterpolation(IndexRange range)
        {
            double[] x = new double[range.Length];
            double[] y = new double[x.Length];
            Index next = range.Min.Copy();
            switch (range.Direction)
            {
                case Direction.Horizontal:
                    for (int i = 0; i < x.Length; i++)
                    {
                        Cell cell = this._cellularFloor.FindCell(next);
                        x[i] = cell.U;
                        y[i] = this.Potentials[cell];
                        next.I += 1;
                    }
                    break;
                case Direction.Vertical:
                    for (int i = 0; i < x.Length; i++)
                    {
                        Cell cell = this._cellularFloor.FindCell(next);
                        x[i] = cell.V;
                        y[i] = this.Potentials[cell];
                        next.J += 1;
                    }
                    break;
                default:
                    break;
            }
            IInterpolation interpolator = null;
            switch (Activity.InterpolationMethod)
            {
                case SpatialAnalysis.FieldUtility.InterpolationMethod.CubicSpline:
                    if (x.Length > 1)
                    {
                        interpolator = CubicSpline.InterpolateBoundariesSorted(x, y,
                            SplineBoundaryCondition.Natural, 0,
                            SplineBoundaryCondition.Natural, 0);
                    }
                    else
                    {
                        interpolator = LinearSpline.InterpolateSorted(x, y);
                    }
                    break;
                //case FieldUtility.InterpolationMethod.Barycentric:
                //    interpolator = Barycentric.InterpolatePolynomialEquidistantSorted(x, y);
                //    break;
                case SpatialAnalysis.FieldUtility.InterpolationMethod.Linear:
                    interpolator = LinearSpline.InterpolateSorted(x, y);
                    break;
                default:
                    break;
            }
            x = null;
            y = null;
            next = null;
            return interpolator;
        }

        private IndexRange verticalRange (Index index)
        {
            int minJ = index.J, maxJ = index.J;
            Index next = index.Copy();
            for (int j = 0; j <= Activity.GradientInterpolationNeighborhoodSize; j++)
            {
                next.J += 1;
                Cell cell = this._cellularFloor.FindCell(next);
                if (cell != null)
                {
                    if (this.Potentials.ContainsKey(cell))
                    {
                        maxJ = next.J;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
            next = index.Copy();
            for (int j = 0; j < Activity.GradientInterpolationNeighborhoodSize; j++)
            {
                next.J -= 1;
                Cell cell = this._cellularFloor.FindCell(next);
                if (cell != null)
                {
                    if (this.Potentials.ContainsKey(cell))
                    {
                        minJ = next.J;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
            IndexRange range = new IndexRange(new Index(index.I, minJ), new Index(index.I, maxJ), Direction.Vertical);
            next = null;
            return range;
        }

        private IndexRange horizontalRange(Index index)
        {
            int minI = index.I, maxI = index.I;
            Index next = index.Copy();
            for (int i = 0; i <= Activity.GradientInterpolationNeighborhoodSize; i++)
            {
                next.I += 1;
                Cell cell = this._cellularFloor.FindCell(next);
                if (cell != null)
                {
                    if (this.Potentials.ContainsKey(cell))
                    {
                        maxI = next.I;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
            next = index.Copy();
            for (int i = 0; i < Activity.GradientInterpolationNeighborhoodSize; i++)
            {
                next.I -= 1;
                Cell cell = this._cellularFloor.FindCell(next);
                if (cell != null)
                {
                    if (this.Potentials.ContainsKey(cell))
                    {
                        minI = next.I;
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
            IndexRange range = new IndexRange(new Index(minI, index.J), new Index(maxI, index.J), Direction.Horizontal);
            next = null;
            return range;
        }

        private IInterpolation interpolation_U(UV point, Index index)
        {
            IndexRange horizontalExpansion = this.horizontalRange(index);
            //getting all vertical ranges of the horizontal
            IndexRange[] verticals = new IndexRange[horizontalExpansion.Length];
            Index next = horizontalExpansion.Min.Copy();
            for (int i = 0; i < horizontalExpansion.Length; i++)
            {
                verticals[i] = this.verticalRange(next);
                next.I++;
            }
            //getting interpolators for u values 
            IInterpolation[] verticalsU = new IInterpolation[horizontalExpansion.Length];
            for (int i = 0; i < verticals.Length; i++)
            {
                verticalsU[i] = this.toInterpolation(verticals[i]);
            }
            //generating u interpolation
            double[] x = new double[horizontalExpansion.Length];
            double[] y = new double[horizontalExpansion.Length];
            next = horizontalExpansion.Min.Copy();
            for (int i = 0; i < horizontalExpansion.Length; i++)
            {
                x[i] = this._cellularFloor.FindCell(next).U;
                y[i] = verticalsU[i].Interpolate(point.V);
                next.I++;
            }
            IInterpolation interpolatorU = null;
            switch (Activity.InterpolationMethod)
            {
                case SpatialAnalysis.FieldUtility.InterpolationMethod.CubicSpline:
                    if (x.Length > 1)
                    {
                        interpolatorU = CubicSpline.InterpolateBoundariesSorted(x, y,
                            SplineBoundaryCondition.Natural, 0,
                            SplineBoundaryCondition.Natural, 0);
                    }
                    else
                    {
                        interpolatorU = LinearSpline.InterpolateSorted(x, y);
                    }
                    break;
                //case FieldUtility.InterpolationMethod.Barycentric:
                //    interpolatorU = Barycentric.InterpolatePolynomialEquidistantSorted(x, y);
                //    break;
                case SpatialAnalysis.FieldUtility.InterpolationMethod.Linear:
                    interpolatorU = LinearSpline.InterpolateSorted(x, y);
                    break;
                default:
                    break;
            }
            return interpolatorU;
        }

        private IInterpolation interpolation_V(UV point, Index index)
        {
            IndexRange verticalExpansion = this.verticalRange(index);
            IndexRange[] horizontals = new IndexRange[verticalExpansion.Length];
            Index next = verticalExpansion.Min.Copy();
            for (int i = 0; i < verticalExpansion.Length; i++)
            {
                horizontals[i] = this.horizontalRange(next);
                next.J++;
            }
            IInterpolation[] horizontalV = new IInterpolation[verticalExpansion.Length];
            for (int i = 0; i < horizontals.Length; i++)
            {
                horizontalV[i] = this.toInterpolation(horizontals[i]);
            }
            double[] x = new double[verticalExpansion.Length];
            double[] y = new double[verticalExpansion.Length];
            next = verticalExpansion.Min.Copy();
            for (int i = 0; i < verticalExpansion.Length; i++)
            {
                x[i] = this._cellularFloor.FindCell(next).V;
                y[i] = horizontalV[i].Interpolate(point.U);
                next.J++;
            }
            //getting U
            IInterpolation interpolatorV = null;
            switch (Activity.InterpolationMethod)
            {
                case SpatialAnalysis.FieldUtility.InterpolationMethod.CubicSpline:
                    if (x.Length > 1)
                    {
                        interpolatorV = CubicSpline.InterpolateBoundariesSorted(x, y,
                            SplineBoundaryCondition.Natural, 0,
                            SplineBoundaryCondition.Natural, 0);
                    }
                    else
                    {
                        interpolatorV = LinearSpline.InterpolateSorted(x, y);
                    }
                    break;
                //case FieldUtility.InterpolationMethod.Barycentric:
                //    interpolatorV = Barycentric.InterpolatePolynomialEquidistantSorted(x, y);
                //    break;
                case SpatialAnalysis.FieldUtility.InterpolationMethod.Linear:
                    interpolatorV = LinearSpline.InterpolateSorted(x, y);
                    break;
                default:
                    break;
            }
            return interpolatorV;
        }

        /// <summary>
        /// Differentiates the specified point to get the steepest gradient.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>UV.</returns>
        public UV Differentiate(UV point)
        {
            Index index = this._cellularFloor.FindIndex(point);
            if (this.Origins.Contains(this._cellularFloor.Cells[index.I,index.J]))
            {
                UV vector = this.DefaultState.Location - point;
                vector.Unitize();
                return vector;
            }
            try
            {
                IInterpolation u_Interpolation = this.interpolation_U(point, index);
                IInterpolation v_Interpolation = this.interpolation_V(point, index);
                return new UV(-1 * u_Interpolation.Differentiate(point.U), 
                    -1 * v_Interpolation.Differentiate(point.V));
            }
            catch (Exception)
            {
            }
            return null;
        }

        /// <summary>
        /// Interpolates the potentials at the specified point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>System.Nullable&lt;System.Double&gt;.</returns>
        public double? Interpolate(UV point)
        {
            Index index = this._cellularFloor.FindIndex(point);
            try
            {
                IInterpolation u_Interpolation = this.interpolation_U(point, index);
                return u_Interpolation.Interpolate(point.U);
            }
            catch (Exception)
            {
            }
            try
            {
                IInterpolation v_Interpolation = this.interpolation_V(point, index);
                return v_Interpolation.Interpolate(point.V);
            }
            catch (Exception)
            {
            }
            return null;
        }

        public double? GetPotential(UV point)
        {
            /*
                INDEXING MODEL
                up      up_right
                origin  right
            */
            try
            {
                Index index = this._cellularFloor.FindIndex(point);
                int I = index.I, J = index.J;
                if (!Data.ContainsKey(this._cellularFloor.Cells[I, J])) return null;
                double u = point.U - _cellularFloor.Cells[I, J].U;
                double v = point.V - _cellularFloor.Cells[I, J].V;
                double origin = Data[_cellularFloor.FindCell(index)];

                J++;
                bool contains_up = Data.ContainsKey(this._cellularFloor.Cells[I, J]);
                double up = origin;
                if (contains_up) up = Data[this._cellularFloor.Cells[I, J]];

                I++; J--;
                bool contains_right = Data.ContainsKey(this._cellularFloor.Cells[I, J]);
                double right = origin;
                if (contains_right) right = Data[this._cellularFloor.Cells[I, J]];

                J++;
                bool contains_up_right = Data.ContainsKey(this._cellularFloor.Cells[I, J]);
                double up_right = origin;
                if (contains_up_right) up_right = Data[this._cellularFloor.Cells[I, J]];

                /*
                    All Possible cases will be investigated
                */
                //none of the corners exist
                if (!contains_up && !contains_right && !contains_up_right)
                {
                    return origin;
                }
                //only one of the corners exist
                {
                    //ONLY contains_up_right
                    if (!contains_up && !contains_right && contains_up_right)
                    {
                        double distance_to_origin = UV.GetDistanceBetween(point, this._cellularFloor.Cells[index.I, index.J]);
                        double distance_to_up_tight = UV.GetDistanceBetween(point, this._cellularFloor.Cells[index.I + 1, index.J + 1]);
                        double total_distance = distance_to_origin + distance_to_up_tight;
                        double potential = (distance_to_up_tight * origin + distance_to_origin * up_right) / (total_distance);
                        return potential;
                    }
                    //ONLY contains_right
                    if (!contains_up && contains_right && !contains_up_right)
                    {
                        double t = u / this._cellularFloor.CellSize;
                        double potential = (1 - t) * origin + t * right;
                        return potential;
                    }
                    //ONLY contains_up
                    if (contains_up && !contains_right && !contains_up_right)
                    {
                        double t = v / this._cellularFloor.CellSize;
                        double potential = (1 - t) * origin + t * up;
                        return potential;
                    }
                }

                /*
                    only one of the corners does not exist.
                    The missing corner's potential will be calculated assuming that 
                    center = up + right = origing + up_right
                    the result will be a planar quad
                */
                {
                    //EXCEPT contains_up_right
                    if (contains_up && contains_right && !contains_up_right)
                    {
                        up_right = up + right - origin;
                    }
                    //EXCEPT contains_right
                    if (contains_up && !contains_right && contains_up_right)
                    {
                        right = origin + up_right - up;
                    }
                    //EXCEPT contains_up
                    if (!contains_up && contains_right && contains_up_right)
                    {
                        up = origin + up_right - right;
                    }
                }
                //ALL corners have potentials or predicted potentials
                double a = u / this._cellularFloor.CellSize;
                double b = v / this._cellularFloor.CellSize;
                double value =
                    origin * (1 - a) * (1 - b) +
                    up * a * (1 - b) +
                    up * (1 - a) * b +
                    up_right * a * b;
                return value;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Report());
            }
            return null;
        }

        #endregion

        /// <summary>
        /// Gets a path using the gradient-descend method
        /// </summary>
        /// <param name="p">The point to get the gradient from.</param>
        /// <param name="distanceFactor">The distance tolerance.</param>
        /// <returns>List&lt;UV&gt;.</returns>
        public List<UV> GetGradientPath(UV p, double distanceFactor = 0.1d, int maxIterations = 5000)
        {
            //distanceFactor = UnitConversion.Convert(distanceFactor, Length_Unit_Types.FEET, this._cellularFloor.UnitType);
            //set the termination conditions
            double distanceTolerance = this._cellularFloor.CellSize * this._cellularFloor.CellSize;
            //find a minimum potential for the termination of the descenting process
            double min = 0;
            if(this.Origins.Count == 1)                 // a neighborhood of size 2 will be selected if only one cell is set as the origin of the potential field
            {
                foreach (Index item in Index.Neighbors)
                {
                    Index nextIndex = this.Origins.First().CellToIndex + item;
                    foreach (Index index in Index.Neighbors)
                    {
                        Index neighbor = nextIndex + index;
                        
                        if (this.Potentials.ContainsKey(this._cellularFloor.Cells[neighbor.I, neighbor.J]))
                        {
                            min = Math.Max(min, this.Potentials[this._cellularFloor.Cells[neighbor.I, neighbor.J]]);
                        }
                    }
                }
            }
            else                                    // a neighborhood of size 1 will be chosen to 
            {
                foreach (Cell cell in this.Origins)
                {
                    foreach (Index index in Index.Neighbors)
                    {
                        Index neighbor = cell.CellToIndex + index;
                        if (this.Potentials.ContainsKey(this._cellularFloor.Cells[neighbor.I, neighbor.J]))
                        {
                            min = Math.Max(min, this.Potentials[this._cellularFloor.Cells[neighbor.I, neighbor.J]]);
                        }
                    }
                }
            }
            List<UV> path = new List<UV>();
            path.Add(p);
            UV previous = new UV();
            UV current = p.Copy();
            int n = 0;
            UV gradient = this.Differentiate(current);
            gradient.Unitize();
            gradient *= distanceFactor;
            UV next = current + gradient;

            double? current_potential = this.GetPotential(p);
            double? previous_potential = current_potential;
            bool terminate = false;
            if (current_potential == null)
            {
                terminate = true;
            }
            if (current_potential.Value < min)
            {
                terminate = true;
            }
            while (!terminate)
            {
                path.Add(next);
                previous = current;
                current = next;
                gradient = this.Differentiate(current);
                gradient.Unitize();
                gradient *= distanceFactor;
                next = current + gradient;
                /*check termination conditions*/
                // 1- out of floor
                Index index = this._cellularFloor.FindIndex(next);
                if(!this.Potentials.ContainsKey(this._cellularFloor.Cells[index.I, index.J]))
                {
                    MessageBox.Show("Path Was pushed outside floor!");
                    break;
                }
                current_potential = this.GetPotential(next);
                if (current_potential == null)
                {
                    break;
                }
                // 2- increasing potentials
                /* This condition is numerically unstable and depends on the stepfactor!
                if (previous_potential.Value < current_potential.Value)
                {
                    terminate = true;
                }
                previous_potential = current_potential;
                */
                // 3- passing minimum threshold potential
                /*
                if (current_potential.Value < min)
                {
                    terminate = true;
                }
                */
                // 4- passing distance threshold to the default state
                if (UV.GetLengthSquared(current, this.DefaultState.Location) < distanceTolerance)
                {
                    terminate = true;
                }
                // 5- iteration off limit
                n++;
                if (n > maxIterations)
                {
                    MessageBox.Show("Maximum number of iterations in descending was riched\n" + maxIterations.ToString());
                    break;
                }
            }
            //double d = (destination - current).LengthSquared();
            //MessageBox.Show("Iteration: " + n.ToString() + "\ndistance: " + d.ToString() + "\nDot: " + dot.ToString());
            path.Add(this.DefaultState.Location);

            return path;
        }

        public override string ToString()
        {
            return this.Name;
        }

        
    }


    /// <summary>
    /// Represents the direction of interpolation on the surface of 2D data
    /// </summary>
    enum U_Or_V
    {
        U = 0,
        V = 1
    }
    /// <summary>
    /// The range of neighborhood for interpolation either at horizontal or vertical directions.
    /// </summary>
    class IndexRange
    {
        private int length;
        /// <summary>
        /// Gets the length of index range.
        /// </summary>
        /// <value>The length.</value>
        public int Length { get { return this.length; } }
        public Direction Direction { get; set; }
        /// <summary>
        /// Gets or sets the minimum index.
        /// </summary>
        /// <value>The minimum.</value>
        public Index Min { get; set; }
        /// <summary>
        /// Gets or sets the maximum index.
        /// </summary>
        /// <value>The maximum.</value>
        public Index Max { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="IndexRange"/> class.
        /// </summary>
        /// <param name="min">The minimum index.</param>
        /// <param name="max">The maximum index.</param>
        /// <param name="direction">The direction.</param>
        public IndexRange(Index min, Index max, Direction direction)
        {
            this.Min = min;
            this.Max = max;
            this.Direction = direction;
            switch (this.Direction)
            {
                case Direction.Horizontal:
                    this.length = this.Max.I - this.Min.I + 1;
                    break;
                case Direction.Vertical:
                    this.length = this.Max.J - this.Min.J + 1;
                    break;
                default:
                    break;
            }
        }
    }
}

