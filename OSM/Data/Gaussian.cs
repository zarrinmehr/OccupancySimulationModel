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
using SpatialAnalysis.CellularEnvironment;
using SpatialAnalysis.Events;
using System.Threading.Tasks;
using SpatialAnalysis.FieldUtility;
using SpatialAnalysis.Interoperability;

namespace SpatialAnalysis.Data
{
    //this class only makes sense for performance
    /// <summary>
    /// Includes mechanisms for applying a Gaussian filter which is clipped by the field of visibility.
    /// </summary>
    public class IsovistClippedGaussian
    {
        //private static int failedRays{get;set;}
        private static readonly double _PIComponent = 1.0d / Math.Sqrt(2 * Math.PI);
        /// <summary>
        /// Gets or sets the weighting factors of the filter.
        /// </summary>
        /// <value>The filter.</value>
        public double[,] Filter { get; set; }
        /// <summary>
        /// Gets or sets the ray indices.
        /// </summary>
        /// <value>The ray indices.</value>
        public Dictionary<Index, Index[]> RayIndices { get; set; }
        /// <summary>
        /// Gets or sets the range of interpolation after which the weighing factor value will be zero.
        /// </summary>
        /// <value>The range.</value>
        public int Range { get; set; }
        private readonly double _sigma;
        /// <summary>
        /// Gets the sigma factor of the normal distribution.
        /// </summary>
        /// <value>The sigma.</value>
        public double Sigma { get { return this._sigma; } }
        double Tolerance { get; set; }
        private readonly double _rangeLengthSquared;
        private CellularFloor _cellularFloor { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="IsovistClippedGaussian"/> class.
        /// </summary>
        /// <param name="cellularFloor">The cellular floor.</param>
        /// <param name="range">The range of interpolation.</param>
        /// <param name="tolerance">The tolerance.</param>
        public IsovistClippedGaussian(CellularFloor cellularFloor, int range, double tolerance = OSMDocument.AbsoluteTolerance)
        {
            this.Range = range;
            this._cellularFloor = cellularFloor;
            this.Tolerance = tolerance;
            double x = (this.Range + 1) * this._cellularFloor.CellSize;
            this._rangeLengthSquared = x * x;
            double lowerBound = UnitConversion.Convert(0.001d, Length_Unit_Types.FEET, this._cellularFloor.UnitType);
            double upperBound = UnitConversion.Convert(5.0d, Length_Unit_Types.FEET, this._cellularFloor.UnitType);
            this._sigma = MathNet.Numerics.RootFinding.RobustNewtonRaphson.FindRoot(this.gaussianPDF, differentialOfGaussianPDF, lowerBound, upperBound);
            //MessageBox.Show("Sigma: " + this.Sigma.ToString() + "\nValue: " + this.gaussianPDF(this.Sigma).ToString());
            this.Filter = IsovistClippedGaussian.GaussianFilter(this.Range + 1, this._cellularFloor.CellSize, this.Sigma);
            this.RayIndices = IsovistClippedGaussian.LoadRayIndices(this.Range, this._cellularFloor, this.Tolerance);
        }
        private double gaussianPDF(double sigma)
        {
            double coefficient = _PIComponent / sigma;
            double value = coefficient * Math.Exp(-this._rangeLengthSquared / (2 * sigma * sigma));
            return value - 0.00001d;
        }
        //derivative of gaussian PDF for sigma
        /// <summary>
        /// Differentials the of gaussian PDF.
        /// </summary>
        /// <param name="sigma">The sigma.</param>
        /// <returns>System.Double.</returns>
        private double differentialOfGaussianPDF(double sigma)
        {
            double A = (_PIComponent / (sigma * sigma)) * Math.Exp(-this._rangeLengthSquared / (2 * sigma * sigma));
            double B = this._rangeLengthSquared/(sigma * sigma) -1.0d;
            double value = A*B;
            return value;
        }
        /// <summary>
        /// Applies the filter on the specified spatial data.
        /// </summary>
        /// <param name="spatialData">The spatial data.</param>
        /// <param name="name">The name of the filtered data .</param>
        /// <returns>ISpatialData.</returns>
        public ISpatialData Apply(ISpatialData spatialData, string name)
        {
            //Gaussian.failedRays = 0;
            ISpatialData data = IsovistClippedGaussian.Apply(spatialData, name, this.Filter, this.RayIndices, this._cellularFloor);
            //MessageBox.Show("Failed Rays: " + Gaussian.failedRays.ToString());
            return data;
        }
        /// <summary>
        /// Applies the filter on the specified spatial data in parallel.
        /// </summary>
        /// <param name="spatialData">The spatial data.</param>
        /// <param name="name">The name of the filtered data.</param>
        /// <returns>ISpatialData.</returns>
        public ISpatialData ApplyParallel(ISpatialData spatialData, string name)
        {
            return IsovistClippedGaussian.ApplyParallel(spatialData, name, this.Filter, this.RayIndices, this._cellularFloor);
        }
        /// <summary>
        /// Loads the ray indices.
        /// </summary>
        /// <param name="range">The range.</param>
        /// <param name="cellularFloor">The cellular floor.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>Dictionary&lt;Index, Index[]&gt;.</returns>
        /// <exception cref="System.ArgumentException">The range of neighborhood should be smaller than the minimum of grid width and height: " + min.ToString()</exception>
        public static Dictionary<Index, Index[]> LoadRayIndices(int range, CellularFloor cellularFloor, double tolerance = .0000001)
        {
            int min = Math.Min(cellularFloor.GridHeight,cellularFloor.GridWidth);
            if (range>= min)
            {
                throw new ArgumentException("The range of neighborhood should be smaller than the minimum of grid width and height: " + min.ToString());
            }
            int rangeSquared = range * range;
            Dictionary<Index, Index[]> rayIndices = new Dictionary<Index, Index[]>();
            for (int i = 0; i <= range; i++)
            {
                for (int j = 0; j <= range; j++)
                {
                    if (!(i==0 && j==0))
                    {
                        //make sure we do not create rays where the gaussian distribution produces very low weighting factors
                        if ((i * i + j * j) <= rangeSquared)
                        {
                            Index first = new Index(i, j);
                            Index[] extractedndices = cellularFloor.FindLineIndices(cellularFloor.Cells[0, 0], cellularFloor.Cells[i, j], tolerance);
                            Index[] firstIndices = new Index[extractedndices.Length + 1];
                            extractedndices.CopyTo(firstIndices, 0);
                            firstIndices[extractedndices.Length] = first;
                            rayIndices.Add(first, firstIndices);
                            //i<0 and j>0
                            var second = new Index(-i, j);
                            if (!rayIndices.ContainsKey(second))
                            {
                                Index[] secondIndices = new Index[firstIndices.Length];
                                for (int k = 0; k < firstIndices.Length; k++)
                                {
                                    secondIndices[k] = new Index(-firstIndices[k].I, firstIndices[k].J);
                                }
                                rayIndices.Add(second, secondIndices);
                            }
                            //i<0 and j<0
                            var third = new Index(-i, -j);
                            if (!rayIndices.ContainsKey(third))
                            {
                                Index[] thirdIndices = new Index[firstIndices.Length];
                                for (int k = 0; k < firstIndices.Length; k++)
                                {
                                    thirdIndices[k] = new Index(-firstIndices[k].I, -firstIndices[k].J);
                                }
                                rayIndices.Add(third, thirdIndices);
                            }
                            //i<0 and j<0
                            var fourth = new Index(i, -j);
                            if (!rayIndices.ContainsKey(fourth))
                            {
                                Index[] fourthIndices = new Index[firstIndices.Length];
                                for (int k = 0; k < firstIndices.Length; k++)
                                {
                                    fourthIndices[k] = new Index(firstIndices[k].I, -firstIndices[k].J);
                                }
                                rayIndices.Add(fourth, fourthIndices);
                            }
                        }
                    }
                }
            }
            return rayIndices;
        }
        /// <summary>
        /// Applies the specified spatial data.
        /// </summary>
        /// <param name="spatialData">The spatial data.</param>
        /// <param name="cell">The cell.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="neighborhood_range">The neighborhood range.</param>
        /// <param name="rayIndices">The ray indices.</param>
        /// <param name="cellularFloor">The cellular floor.</param>
        /// <returns>System.Double.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        private static double Apply(ISpatialData spatialData, Cell cell, double[,] filter, int neighborhood_range,
            Dictionary<Index, Index[]> rayIndices, CellularFloor cellularFloor)
        {
            //the filtered value will be sum/w
            double sum = 0, w = 0;
            foreach (Index index in rayIndices.Keys)
            {
                //translating the index of the end of the ray
                var translatedIndex = index + cell.CellToIndex;
                //check to see if the translated index is valid in the cellular floor
                if (cellularFloor.ContainsCell(translatedIndex))
                {
                    //get the current cell at the end of the ray
                    var currentCell = cellularFloor.Cells[translatedIndex.I, translatedIndex.J];
                    //ignore the entir ray if the cell does not belong to the original spatialData
                    //this cell serves as a visual barrier
                    if (spatialData.Data.ContainsKey(currentCell))
                    {
                        //check for visibility along the ray
                        bool isVisible = true;
                        foreach (Index rayIndex in rayIndices[index])
                        {
                            Index current = cell.CellToIndex + rayIndex;
                            if (!cellularFloor.ContainsCell(current))
                            {
                                throw new ArgumentOutOfRangeException(current.ToString());
                            }
                            //if the current cell is not inside the field or it does not belong to the original spatial data
                            // it is considered as a visual block
                            if (cellularFloor.Cells[current.I, current.J].FieldOverlapState != OverlapState.Inside ||
                                !spatialData.Data.ContainsKey(cellularFloor.Cells[current.I, current.J]))
                            {
                                //Gaussian.failedRays++;
                                isVisible = false;
                                break;
                            }
                        }
                        if (isVisible)
                        {
                            sum += filter[Math.Abs(index.I), Math.Abs(index.J)] * spatialData.Data[currentCell];
                            w += filter[Math.Abs(index.I), Math.Abs(index.J)];
                        }
                    }
                }
            }
            if (sum != 0 && w != 0)
            {
                //apply filter on the cell itself
                sum += spatialData.Data[cell] * filter[0, 0];
                w += filter[0, 0];
                return sum / w;
            }
            else
            {
                return spatialData.Data[cell];
            }
        }
        /// <summary>
        /// Getpotentialses the specified spatial data.
        /// </summary>
        /// <param name="spatialData">The spatial data.</param>
        /// <returns>Dictionary&lt;Cell, System.Double&gt;.</returns>
        public Dictionary<Cell, double> GetFilteredValues(ISpatialData spatialData)
        {
            return GetFilteredValues(spatialData, this.Filter, this.RayIndices, this._cellularFloor);
        }
        /// <summary>
        /// Gets the filtered values.
        /// </summary>
        /// <param name="spatialData">The spatial data.</param>
        /// <param name="filter">The weighting factors of the filter.</param>
        /// <param name="rayIndices">The ray indices.</param>
        /// <param name="cellularFloor">The cellular floor.</param>
        /// <returns>Dictionary&lt;Cell, System.Double&gt;.</returns>
        public static Dictionary<Cell, double> GetFilteredValues(ISpatialData spatialData, double[,] filter,
            Dictionary<Index, Index[]> rayIndices, CellularFloor cellularFloor)
        {
            Cell[] cells = new Cell[spatialData.Data.Count];
            spatialData.Data.Keys.CopyTo(cells, 0);
            double[] values = new double[spatialData.Data.Count];
            int neighborhood_range = filter.GetLength(0);
            Parallel.For(0, cells.Length, (I) =>
            {
                values[I] = IsovistClippedGaussian.Apply(spatialData, cells[I], filter, neighborhood_range, rayIndices, cellularFloor);
            });
            Dictionary<Cell, double> filtered = new Dictionary<Cell, double>();
            for (int i = 0; i < cells.Length; i++)
            {
                filtered.Add(cells[i], values[i]);
            }
            return filtered;
        }
        /// <summary>
        /// Applies the filter in parallel.
        /// </summary>
        /// <param name="spatialData">The spatial data.</param>
        /// <param name="name">The name of filtered data.</param>
        /// <param name="filter">The weighting factors of the filter.</param>
        /// <param name="rayIndices">The ray indices.</param>
        /// <param name="cellularFloor">The cellular floor.</param>
        /// <returns>ISpatialData.</returns>
        public static ISpatialData ApplyParallel(ISpatialData spatialData, string name, double[,] filter,
            Dictionary<Index, Index[]> rayIndices, CellularFloor cellularFloor)
        {
            Cell[] cells = new Cell[spatialData.Data.Count];
            spatialData.Data.Keys.CopyTo(cells, 0);
            double[] values = new double[spatialData.Data.Count];
            int neighborhood_range = filter.GetLength(0);
            Parallel.For(0, cells.Length, (I) => 
                {
                    values[I] = IsovistClippedGaussian.Apply(spatialData, cells[I], filter, neighborhood_range, rayIndices, cellularFloor);
                });
            Dictionary<Cell, double> filtered = new Dictionary<Cell, double>();
            for (int i = 0; i < cells.Length; i++)
            {
                filtered.Add(cells[i], values[i]);
            }
            ISpatialData newData = null;
            switch (spatialData.Type)
            {
                case DataType.SpatialData:
                    if (spatialData.GetType() == typeof(SpatialDataField))
                    {
                        newData = new SpatialDataField(name, filtered);
                    }
                    break;
                case DataType.ActivityPotentialField:
                    newData = new FieldUtility.Activity(filtered, ((Activity)spatialData).Origins, ((Activity)spatialData).DestinationArea, ((Activity)spatialData).DefaultState, name, cellularFloor);
                    ((Activity)newData).TrySetEngagementTime(((Activity)spatialData).MinimumEngagementTime, ((Activity)spatialData).MaximumEngagementTime);
                    break;
                case DataType.OccupancyEvent:
                    if (spatialData.GetType() == typeof(MandatoryEvaluationEvent))
                    {
                        MandatoryEvaluationEvent event_ = (MandatoryEvaluationEvent)spatialData;
                        newData = new SpatialAnalysis.Events.MandatoryEvaluationEvent(name, filtered, event_.Likelihood, event_.TimeSamplingRate, event_.TimeStep, event_.Duration,
                            event_.MaximumVelocityMagnitude, event_.VisibilityAngle, event_.HasCapturedVisualEvents, event_.HasCapturedDataEvents, event_.EventType, event_.CapturedEventTrails)
                        {
                            HasActivityEngagementEvent = event_.HasActivityEngagementEvent
                        };
                    }
                    else
                    {
                        EvaluationEvent event_ = (EvaluationEvent)spatialData;
                        newData = new SpatialAnalysis.Events.EvaluationEvent(name, filtered, event_.Likelihood, event_.TimeSamplingRate, event_.TimeStep, event_.Duration,
                            event_.MaximumVelocityMagnitude, event_.VisibilityAngle, event_.HasCapturedVisualEvents, event_.HasCapturedDataEvents, event_.EventType, event_.CapturedEventTrails);
                    }
                    break;
                case DataType.SimulationResult:
                    if (spatialData.GetType() == typeof(SimulationResult))
                    {
                        SimulationResult simulationResult = (SimulationResult)spatialData;
                        newData = new SimulationResult(name, filtered, simulationResult.TimeStep, simulationResult.SimulationDuration);
                    }
                    else if (spatialData.GetType() == typeof(MandatorySimulationResult))
                    {
                        MandatorySimulationResult mandatorySimulationResult = (MandatorySimulationResult)spatialData;
                        newData = new MandatorySimulationResult(name, filtered, mandatorySimulationResult.TimeStep, mandatorySimulationResult.SimulationDuration, mandatorySimulationResult.WalkedDistancePerHour,
                            mandatorySimulationResult.TimeInMainStations, mandatorySimulationResult.WalkingTime, mandatorySimulationResult.ActivityEngagementTime, mandatorySimulationResult.SequencesWhichNeededVisualDetection,
                            mandatorySimulationResult.AverageDelayChanceForVisualDetection, mandatorySimulationResult.MinimumDelayChanceForVisualDetection, mandatorySimulationResult.MaximumDelayChanceForVisualDetection);
                    }
                    break;
            }
            return newData;
        }

        /// <summary>
        /// Applies filter on the specified spatial data.
        /// </summary>
        /// <param name="spatialData">The spatial data.</param>
        /// <param name="name">The name of the filtered data.</param>
        /// <param name="filter">The weighting factors of the filter.</param>
        /// <param name="rayIndices">The ray indices.</param>
        /// <param name="cellularFloor">The cellular floor.</param>
        /// <returns>ISpatialData.</returns>
        public static ISpatialData Apply(ISpatialData spatialData, string name, double[,] filter, 
            Dictionary<Index, Index[]> rayIndices, CellularFloor cellularFloor)
        {
            int neighborhood_range = filter.GetLength(0);
            Dictionary<Cell, double> filtered = new Dictionary<Cell, double>();
            foreach (Cell cell in spatialData.Data.Keys)
            {
                double value = IsovistClippedGaussian.Apply(spatialData, cell, filter, neighborhood_range, rayIndices, cellularFloor);
                filtered.Add(cell, value);
            }
            ISpatialData newData = null;
            switch (spatialData.Type)
            {
                case DataType.SpatialData:
                    if (spatialData.GetType() == typeof(SpatialDataField))
                    {
                        newData = new SpatialDataField(name, filtered);
                    }
                    else if (spatialData.GetType() == typeof(SimulationResult))
                    {
                        SimulationResult simulationResult = (SimulationResult)spatialData;
                        newData = new SimulationResult(name, filtered, simulationResult.TimeStep, simulationResult.SimulationDuration);
                    }
                    else if (spatialData.GetType() == typeof(MandatorySimulationResult))
                    {
                        MandatorySimulationResult mandatorySimulationResult = (MandatorySimulationResult)spatialData;
                        newData = new MandatorySimulationResult(name, filtered, mandatorySimulationResult.TimeStep, mandatorySimulationResult.SimulationDuration, mandatorySimulationResult.WalkedDistancePerHour,
                            mandatorySimulationResult.TimeInMainStations, mandatorySimulationResult.WalkingTime, mandatorySimulationResult.ActivityEngagementTime,
                            mandatorySimulationResult.SequencesWhichNeededVisualDetection, mandatorySimulationResult.AverageDelayChanceForVisualDetection,
                            mandatorySimulationResult.MinimumDelayChanceForVisualDetection,
                            mandatorySimulationResult.MaximumDelayChanceForVisualDetection);
                    }
                    break;
                case DataType.ActivityPotentialField:
                    newData = new FieldUtility.Activity(filtered, ((Activity)spatialData).Origins, ((Activity)spatialData).DestinationArea, ((Activity)spatialData).DefaultState, name, cellularFloor);
                    ((Activity)newData).TrySetEngagementTime(((Activity)spatialData).MinimumEngagementTime, ((Activity)spatialData).MaximumEngagementTime);
                    break;
                case DataType.OccupancyEvent:
                    if (spatialData.GetType() == typeof(MandatoryEvaluationEvent))
                    {
                        MandatoryEvaluationEvent event_ = (MandatoryEvaluationEvent)spatialData;
                        newData = new SpatialAnalysis.Events.MandatoryEvaluationEvent(name, filtered, event_.Likelihood, event_.TimeSamplingRate, event_.TimeStep, event_.Duration,
                            event_.MaximumVelocityMagnitude, event_.VisibilityAngle, event_.HasCapturedVisualEvents, event_.HasCapturedDataEvents, event_.EventType, event_.CapturedEventTrails) 
                            { 
                                HasActivityEngagementEvent = event_.HasActivityEngagementEvent 
                            };
                    }
                    else 
                    {
                        EvaluationEvent event_ = (EvaluationEvent)spatialData;
                        newData = new SpatialAnalysis.Events.EvaluationEvent(name, filtered, event_.Likelihood, event_.TimeSamplingRate, event_.TimeStep, event_.Duration,
                            event_.MaximumVelocityMagnitude, event_.VisibilityAngle, event_.HasCapturedVisualEvents, event_.HasCapturedDataEvents, event_.EventType, event_.CapturedEventTrails);
                    }
                    break;
            }
            return newData;
        }

        /// <summary>
        /// Loads the weighting factors of the Gaussian filter.
        /// </summary>
        /// <param name="N">The range of filter.</param>
        /// <param name="cellSize">Size of the cell.</param>
        /// <param name="standardDeviation">The standard deviation.</param>
        /// <returns>System.Double[].</returns>
        public static double[,] GaussianFilter(int N, double cellSize, double standardDeviation)
        {
            double coefficient = _PIComponent / standardDeviation;
            double sigmaSquaredComponent = 1.0d / (2 * standardDeviation * standardDeviation);
            double xSquared = cellSize * cellSize;
            double[,] coefficients = new double[N, N];
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    coefficients[i, j] = coefficient * Math.Exp(-(i * i + j * j) * xSquared * sigmaSquaredComponent);
                }
            }
            return coefficients;
        }

    }
}

