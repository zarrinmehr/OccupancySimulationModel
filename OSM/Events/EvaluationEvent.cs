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
using SpatialAnalysis.Geometry;
using SpatialAnalysis.Agents;
using SpatialAnalysis.Data;
using MathNet.Numerics.IntegralTransforms;
using System.Numerics;

namespace SpatialAnalysis.Events
{
    /// <summary>
    /// Enum EvaluationEventType
    /// </summary>
    public enum EvaluationEventType
    {
        /// <summary>
        /// The optional occupancy event
        /// </summary>
        Optional,
        /// <summary>
        /// The mandatory occupancy event
        /// </summary>
        Mandatory
    }

    /// <summary>
    /// Class EvaluationEvent.
    /// </summary>
    /// <seealso cref="SpatialAnalysis.Data.ISpatialData" />
    public class EvaluationEvent: ISpatialData
    {
        private string _name;
        /// <summary>
        /// Gets the name of the evaluation event.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get { return this._name; } }

        private Dictionary<CellularEnvironment.Cell, double> _data;
        /// <summary>
        /// Gets the evaluation event Probability of Agent's Presence (PAP). The values are not normalized.
        /// </summary>
        /// <value>The data.</value>
        public Dictionary<CellularEnvironment.Cell, double> Data { get { return this._data; } }

        private double _min;
        /// <summary>
        /// Gets the minimum value of the data.
        /// </summary>
        /// <value>The minimum.</value>
        public double Min { get { return this._min; } }

        private double _max;
        /// <summary>
        /// Gets the maximum value of the data.
        /// </summary>
        /// <value>The maximum.</value>
        public double Max { get { return this._max; } }

        /// <summary>
        /// Gets or sets the type of the event.
        /// </summary>
        /// <value>The type of the event.</value>
        public EvaluationEventType EventType { get; set; }

        private double _likelihood;
        /// <summary>
        /// Gets the likelihood of the event to occure.
        /// </summary>
        /// <value>The likelihood.</value>
        public double Likelihood { get { return this._likelihood; } }

        private double _timeSamplingRate;
        /// <summary>
        /// Gets the time sampling rate.
        /// </summary>
        /// <value>The time sampling rate.</value>
        public double TimeSamplingRate { get { return this._timeSamplingRate; } }

        private StateBase[] _capturedEventTrails;
        /// <summary>
        /// Gets the captured event trails.
        /// </summary>
        /// <value>The captured event trails.</value>
        public StateBase[] CapturedEventTrails { get { return this._capturedEventTrails; } }
        /// <summary>
        /// Gets a value indicating whether this instance has trail data.
        /// </summary>
        /// <value><c>true</c> if this instance has trail data; otherwise, <c>false</c>.</value>
        public bool HasTrailData { get { return this._capturedEventTrails != null; } }

        /// <summary>
        /// Gets the type of data.
        /// </summary>
        /// <value>The type.</value>
        public DataType Type { get { return DataType.OccupancyEvent; } }

        private double _timeStep;
        /// <summary>
        /// Gets the time-step used to capture this event per milliseconds.
        /// </summary>
        /// <value>The time step.</value>
        public double TimeStep { get { return this._timeStep; } }

        private double _duration;
        /// <summary>
        /// Gets the duration of this event per hours.
        /// </summary>
        /// <value>The duration.</value>
        public double Duration { get { return this._duration; } }

        private double _visibilityAngle;
        /// <summary>
        /// Gets the visibility angle used in this event.
        /// </summary>
        /// <value>The visibility angle.</value>
        public double VisibilityAngle
        {
            get { return _visibilityAngle; }
        }

        private double _velocityMagnitude;
        /// <summary>
        /// Gets the velocity magnitude.
        /// </summary>
        /// <value>The velocity magnitude Maximum.</value>
        public double MaximumVelocityMagnitude
        {
            get { return _velocityMagnitude; }
        }

        private bool _hasCapturedDataEvents;
        /// <summary>
        /// Gets a value indicating whether this instance has captured data events.
        /// </summary>
        /// <value><c>true</c> if this instance has captured data events; otherwise, <c>false</c>.</value>
        public bool HasCapturedDataEvents
        {
            get { return _hasCapturedDataEvents; }
        }

        private bool _hasCapturedVisualEvents;
        /// <summary>
        /// Gets a value indicating whether this instance has captured visual events.
        /// </summary>
        /// <value><c>true</c> if this instance has captured visual events; otherwise, <c>false</c>.</value>
        public bool HasCapturedVisualEvents
        {
            get { return _hasCapturedVisualEvents; }
        }
        //frequencyAmplitude
        private double[] _frequencyAmplitudes;
        /// <summary>
        /// Represents a low path filter which filters out 95% of the high frequencies of FFT spectrum
        /// </summary>
        public double[] FrequencyAmplitudes
        {
            get { return _frequencyAmplitudes; }
        }

        /// <summary>
        /// Loads the frequency amplitudes.
        /// </summary>
        /// <param name="values">The values.</param>
        public void LoadFrequencyAmplitudes(double[] values)
        {
            int n = values.Length;
            Complex[] complexes = new Complex[n];
            //the complex numbers are the signals now
            for (int i = 0; i < n; i++)
            {
                complexes[i] = new Complex(values[i], 0.0d);
            }
            //the complex numbers will change to frequency
            Fourier.Forward(complexes, FourierOptions.Default);
            this._frequencyAmplitudes = new double[n];
            for (int i = 0; i < n; i++)
            {
                this._frequencyAmplitudes[i] = complexes[i].Magnitude;
            }
        }
        /// <summary>
        /// Gets a value indicating whether this instance has frequencies.
        /// </summary>
        /// <value><c>true</c> if this instance has frequencies; otherwise, <c>false</c>.</value>
        public bool HasFrequencies { get { return this._frequencyAmplitudes != null; } }

        /// <summary>
        /// Represents the transcript of an event query. This class is a data model and offers.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="data">The data.</param>
        /// <param name="likelihood">The likelihood.</param>
        /// <param name="timeSamplingRate">The time sampling rate.</param>
        /// <param name="timeStep">The time step.</param>
        /// <param name="duration">The duration.</param>
        /// <param name="velocityMagnitude">The velocity magnitude.</param>
        /// <param name="visibilityAngle">The visibility angle.</param>
        /// <param name="hasCapturedVisualEvents">if set to <c>true</c> has captured visual events.</param>
        /// <param name="hasCapturedDataEvents">if set to <c>true</c> has captured data events.</param>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="trailData">The trail data.</param>
        public EvaluationEvent(string name, Dictionary<CellularEnvironment.Cell,double> data, 
            double likelihood, double timeSamplingRate, double timeStep, double duration, 
            double velocityMagnitude, double visibilityAngle,
            bool hasCapturedVisualEvents, bool hasCapturedDataEvents, EvaluationEventType eventType,
            StateBase[] trailData)
        {
            this._visibilityAngle = visibilityAngle;
            this._velocityMagnitude = velocityMagnitude;
            this._hasCapturedDataEvents = hasCapturedDataEvents;
            this._hasCapturedVisualEvents = hasCapturedVisualEvents;
            this._capturedEventTrails = trailData;
            this._data = data;
            this._min = double.PositiveInfinity;
            this._max = double.NegativeInfinity;
            foreach (var item in this.Data.Values)
            {
                this._min = Math.Min(this._min, item);
                this._max = Math.Max(this._max, item);
            }
            //trim the unwanted chars from the input name
            char[] charsToTrim = { ' ', '\'' };
            this._name = name.Trim(charsToTrim);
            this._duration = duration;
            this._likelihood = likelihood;
            this._timeSamplingRate = timeSamplingRate;
            this._timeStep = timeStep;
            this.EventType = eventType;
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return this.Name;
        }
    }

    /// <summary>
    /// Class MandatoryEvaluationEvent.
    /// </summary>
    /// <seealso cref="SpatialAnalysis.Events.EvaluationEvent" />
    public class MandatoryEvaluationEvent : EvaluationEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MandatoryEvaluationEvent"/> class.
        /// </summary>
        /// <param name="name">The name of the event.</param>
        /// <param name="data">The data.</param>
        /// <param name="likelihood">The likelihood of the event .</param>
        /// <param name="timeSamplingRate">The time sampling rate.</param>
        /// <param name="timeStep">The time-step per milliseconds.</param>
        /// <param name="duration">The duration per hour.</param>
        /// <param name="velocityMagnitude">The velocity magnitude.</param>
        /// <param name="visibilityAngle">The visibility angle.</param>
        /// <param name="hasCapturedVisualEvents">if set to <c>true</c> the event has captured visual events.</param>
        /// <param name="hasCapturedDataEvents">if set to <c>true</c> the event has captured data events.</param>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="trailData">The trail data.</param>
        public MandatoryEvaluationEvent(string name, Dictionary<CellularEnvironment.Cell, double> data,
            double likelihood, double timeSamplingRate, double timeStep, double duration,
            double velocityMagnitude, double visibilityAngle,
            bool hasCapturedVisualEvents, bool hasCapturedDataEvents, EvaluationEventType eventType,
            StateBase[] trailData): 
            base(name, data, likelihood, timeSamplingRate,timeStep, duration, velocityMagnitude,visibilityAngle,hasCapturedVisualEvents,hasCapturedDataEvents,eventType,trailData)
        {

        }
        /// <summary>
        /// Gets or sets a value indicating whether this instance has activity engagement event.
        /// </summary>
        /// <value><c>true</c> if this instance has activity engagement event; otherwise, <c>false</c>.</value>
        public bool HasActivityEngagementEvent {get;set;}


    }
}

