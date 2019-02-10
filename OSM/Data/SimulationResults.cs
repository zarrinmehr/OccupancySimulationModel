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
using SpatialAnalysis.CellularEnvironment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpatialAnalysis.Data
{
    /// <summary>
    /// This class includes ths transcript of the simulation results
    /// </summary>
    /// <seealso cref="SpatialAnalysis.Data.SpatialDataField" />
    public class SimulationResult : ISpatialData
    {
        /// <summary>
        /// Gets the type of data.
        /// </summary>
        /// <value>The type.</value>
        public DataType Type { get { return this._type; } }
        private DataType _type;
        /// <summary>
        /// Gets the name of the data.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get { return _name; } }
        private string _name;
        private double _timeStep;
        /// <summary>
        /// Gets the time step.
        /// </summary>
        /// <value>The time step.</value>
        public double TimeStep
        {
            get { return _timeStep; }
        }

        private double _simulationDuration;

        /// <summary>
        /// Gets the duration of the simulation.
        /// </summary>
        /// <value>The duration of the simulation.</value>
        public double SimulationDuration
        {
            get { return _simulationDuration; }
        }

        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <value>The data.</value>
        public Dictionary<Cell, double> Data { get { return _data; } }
        Dictionary<Cell, double> _data;
        /// <summary>
        /// Gets the minimum value of the data.
        /// </summary>
        /// <value>The minimum.</value>
        public double Min { get { return this._min; } }
        private double _min;
        /// <summary>
        /// Gets the maximum value of the data.
        /// </summary>
        /// <value>The maximum.</value>
        public double Max { get { return this._max; } }
        private double _max;
        /// <summary>
        /// Initializes a new instance of the <see cref="SimulationResult"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="values">The values.</param>
        /// <param name="timeStep">The time step.</param>
        /// <param name="simulationDuration">Duration of the simulation.</param>
        public SimulationResult(string name, Dictionary<Cell, double> values, double timeStep, double simulationDuration)
        {
            this._simulationDuration = simulationDuration;
            this._timeStep = timeStep;
            //trim the unwanted chars from the input name
            char[] charsToTrim = { ' ', '\'' };
            this._name = name.Trim(charsToTrim);
            this._type = DataType.SimulationResult;
            this._data = values;
            this._min = double.PositiveInfinity;
            this._max = double.NegativeInfinity;
            foreach (var item in this.Data.Values)
            {
                this._min = Math.Min(this._min, item);
                this._max = Math.Max(this._max, item);
            }
        }

        public override string ToString()
        {
            return this.Name;
        }
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }
    }

    /// <summary>
    /// Includes the transcript of mandatory simulations.
    /// </summary>
    /// <seealso cref="SpatialAnalysis.Data.SimulationResult" />
    public class MandatorySimulationResult : SimulationResult
    {
        private double _walkedDistancePerHour;
        /// <summary>
        /// Gets the walked distance per hour.
        /// </summary>
        /// <value>The walked distance per hour.</value>
        public double WalkedDistancePerHour
        {
            get { return _walkedDistancePerHour; }
        }

        private double _timeInMainStations;
        /// <summary>
        /// Gets the time in main stations.
        /// </summary>
        /// <value>The time in main stations.</value>
        public double TimeInMainStations
        {
            get { return _timeInMainStations; }
        }

        private double _walkingTime;
        /// <summary>
        /// Gets the walking time.
        /// </summary>
        /// <value>The walking time.</value>
        public double WalkingTime
        {
            get { return _walkingTime; }
        }

        private double _activityEngagementTime;
        /// <summary>
        /// Gets the activity engagement time.
        /// </summary>
        /// <value>The activity engagement time.</value>
        public double ActivityEngagementTime
        {
            get { return _activityEngagementTime; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MandatorySimulationResult"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="values">The values.</param>
        /// <param name="timeStep">The time-step.</param>
        /// <param name="simulationDuration">Duration of the simulation.</param>
        /// <param name="walkedDistancePerHour">The walked distance per hour.</param>
        /// <param name="timeInMainStations">The time in main stations.</param>
        /// <param name="walkingTime">The walking time.</param>
        /// <param name="activityEngagementTime">The activity engagement time.</param>
        /// <param name="numberOfVisuallyTriggeredSequences">The number of visually triggered sequences.</param>
        /// <param name="averageChanceForVisualDetection">The average chance for visual detection.</param>
        /// <param name="minimumChanceForVisualDetection">The minimum chance for visual detection.</param>
        /// <param name="maximumChanceForVisualDetection">The maximum chance for visual detection.</param>
        public MandatorySimulationResult(string name, Dictionary<Cell, double> values, double timeStep, double simulationDuration,
                    double walkedDistancePerHour, double timeInMainStations, double walkingTime, double activityEngagementTime,
            int numberOfVisuallyTriggeredSequences,
            double averageChanceForVisualDetection,
            double minimumChanceForVisualDetection,
            double maximumChanceForVisualDetection)
            : base(name, values, timeStep, simulationDuration)
        {
            this._walkedDistancePerHour = walkedDistancePerHour;
            this._walkingTime = walkingTime;
            this._activityEngagementTime = activityEngagementTime;
            this._timeInMainStations = timeInMainStations;
            this._sequencesWhichNeededVisualDetection = numberOfVisuallyTriggeredSequences;
            this._averageDelayChanceForVisualDetection = averageChanceForVisualDetection;
            this._minimumDelayChanceForVisualDetection = minimumChanceForVisualDetection;
            this._maximumDelayChanceForVisualDetection = maximumChanceForVisualDetection;

        }

        private int _sequencesWhichNeededVisualDetection;
        /// <summary>
        /// Gets the number of sequences which needed visual detection.
        /// </summary>
        /// <value>The sequences which needed visual detection.</value>
        public int SequencesWhichNeededVisualDetection
        {
            get { return _sequencesWhichNeededVisualDetection; }
        }
        //
        private double _averageDelayChanceForVisualDetection;
        /// <summary>
        /// Gets the average delay chance for visual detection.
        /// </summary>
        /// <value>The average delay chance for visual detection.</value>
        public double AverageDelayChanceForVisualDetection
        {
            get { return _averageDelayChanceForVisualDetection; }
        }

        private double _minimumDelayChanceForVisualDetection;
        /// <summary>
        /// Gets the minimum delay chance for visual detection.
        /// </summary>
        /// <value>The minimum delay chance for visual detection.</value>
        public double MinimumDelayChanceForVisualDetection
        {
            get { return _minimumDelayChanceForVisualDetection; }
        }

        private double _maximumDelayChanceForVisualDetection;
        /// <summary>
        /// Gets the maximum delay chance for visual detection.
        /// </summary>
        /// <value>The maximum delay chance for visual detection.</value>
        public double MaximumDelayChanceForVisualDetection
        {
            get { return _maximumDelayChanceForVisualDetection; }
        }



    }
}

