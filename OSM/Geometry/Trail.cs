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
using MathNet.Numerics;
using MathNet.Numerics.Interpolation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpatialAnalysis.Geometry
{
    /// <summary>
    /// Enum TrailInput
    /// </summary>
    public enum TrailInput
    {
        /// <summary>
        /// The location, velocity, and direction of states found in the trail data will be used to create the trail.
        /// </summary>
        Location_Velocity_Direction = 0,
        /// <summary>
        /// The location and direction of states found in the trail data will be used to create the trail and velocities will be derived from the locations.
        /// </summary>
        Location_Direction = 1,
        /// <summary>
        /// Only the location of the states in the data will be used to create the trail and velocity and direction will be derived from the locations.
        /// </summary>
        Location = 2,
    }
    /// <summary>
    /// This class includes observed trail data and methods to capture interpolated states of the agent. It is used in the training process.
    /// </summary>
    public class WalkingTrail
    {
        private TrailInput _trailInput;
        /// <summary>
        /// Gets the trail input mode which determines the calculation of interpolated velocity and direction 
        /// </summary>
        /// <value>The trail input mode.</value>
        public TrailInput TrailInputMode {get { return _trailInput; }}

        private StateBase[] _observedStates;

        /// <summary>
        /// Gets the observed states which were used to create the trail.
        /// </summary>
        /// <value>The observed states.</value>
        public StateBase[] ObservedStates { get { return _observedStates; } }
        private double _timeInterval;

        /// <summary>
        /// Gets the time interval between interpolated states.
        /// </summary>
        /// <value>The time interval between interpolated states.</value>
        public double TimeIntervalBetweenInterpolatedStates { get { return _timeInterval; } }

        private int _numberOfStatesPerUniteOfLength = 2;
        /// <summary>
        /// Gets or sets the length of the number of states per unite of length.
        /// </summary>
        /// <value>The length of the number of states per unite of length.</value>
        public int NumberOfStatesPerUniteOfLength
        {
            get { return this._numberOfStatesPerUniteOfLength; }
            set
            {
                if (this._numberOfStatesPerUniteOfLength != value)
                {
                    if (this._numberOfStatesPerUniteOfLength <= 0)
                    {
                        this._numberOfStatesPerUniteOfLength = 1;
                    }
                    else
                    {
                        this._numberOfStatesPerUniteOfLength = value;
                    }
                    this.loadNormalizedStates();
                }
            }
        }

        private int _numberOfPointsPerUniteOfLength = 5;
        /// <summary>
        /// Gets or sets the length of the number of points per unite of length.
        /// </summary>
        /// <value>The length of the number of points per unite of length.</value>
        public int NumberOfPointsPerUniteOfLength
        {
            get { return _numberOfPointsPerUniteOfLength; }
            set
            {
                if (this._numberOfPointsPerUniteOfLength != value)
                {
                    if (value < 1)
                    {
                        this._numberOfPointsPerUniteOfLength = 2;
                    }
                    else
                    {
                        _numberOfPointsPerUniteOfLength = value;
                    }
                    this.loadApproximatedPolyline();
                }
            }
        }

        private double _curvature = 1.0d;
        /// <summary>
        /// Gets or sets a number between 0 and 1 which determines the curvature of the trail, 0 being a linear interpolation and 1 a cubic interpolation.
        /// </summary>
        /// <value>The curvature.</value>
        public double Curvature
        {
            get { return _curvature; }
            set
            {
                if (value != this._curvature)
                {
                    if (value < 0)
                    {
                        this._curvature = 0.0d;
                    }
                    else
                    {
                        if (value > 1)
                        {
                            this._curvature = 1.0d;
                        }
                        else
                        {
                            this._curvature = value;
                        }
                    }
                    this.loadNormalizedStates();
                    this.loadApproximatedPolyline();
                }
            }
        }

        private UV[] _approximatedPoints;
        /// <summary>
        /// Gets the points that approximate the trail curvature with a polyline. The resulting polyline is used for visualization only and has no impact on integration and differentiation of the trail.
        /// </summary>
        /// <value>The approximated points.</value>
        public UV[] ApproximatedPoints { get { return _approximatedPoints; } }

        private StateBase[] _interpolatedStates;


        /// <summary>
        /// Gets the interpolated states.
        /// </summary>
        /// <value>The interpolated states.</value>
        public StateBase[] InterpolatedStates
        {
            get { return _interpolatedStates; }
        }

        private IInterpolation _interpolate_location_U;
        private IInterpolation _interpolate_location_V;
        private IInterpolation _ULinearInterpolation;
        private IInterpolation _VLinearInterpolation;

        private IInterpolation _interpolate_direction_U;
        private IInterpolation _interpolate_direction_V;

        private IInterpolation _interpolate_velocity_U;
        private IInterpolation _interpolate_velocity_V;

        private double[] _observationTime;
        /// <summary>
        /// Gets the observation time of the observed states.
        /// </summary>
        /// <value>The observation time.</value>
        public double[] ObservationTime
        {
            get { return _observationTime; }
        }
        private IInterpolation _timeToLength;
        private double _startTime;
        /// <summary>
        /// Gets the start time of the trail.
        /// </summary>
        /// <value>The start time.</value>
        public double StartTime{get { return _startTime; }}
        private double _endTime;
        /// <summary>
        /// Gets the end time of the trail.
        /// </summary>
        /// <value>The end time.</value>
        public double EndTime { get { return _endTime; } }
        private double _duration;
        /// <summary>
        /// Gets the duration of the trail.
        /// </summary>
        /// <value>The duration.</value>
        public double Duration{get { return _duration; }}
        /// <summary>
        /// Gets the length of the polyline representation of the trail.
        /// </summary>
        /// <value>The length of the polyline representation.</value>
        public double PlineRepresentationLength { get { return this._length[this._length.Length - 1]; } }
        private double[] _length;

        private IInterpolation _lengthToTime;

        private double _trailLength;


        /// <summary>
        /// Initializes a new instance of the <see cref="WalkingTrail"/> class.
        /// </summary>
        /// <param name="observation_time">The observation time.</param>
        /// <param name="observed_states">The observed states.</param>
        /// <param name="inputMode">The input mode.</param>
        /// <exception cref="ArgumentException">
        /// The size of observation time and observed states are not equal
        /// or
        /// The observation times are not in ascending order!
        /// or
        /// The observed states include null elements that cannot be used for generating a trail model
        /// or
        /// The observed states include null elements that cannot be used for generating a trail model
        /// or
        /// The observed states include null locations that cannot be used for generating a trail model
        /// </exception>
        public WalkingTrail(double[] observation_time, StateBase[] observed_states, TrailInput inputMode)
        {
            if (observation_time.Length != observed_states.Length)
            {
                 throw new ArgumentException("The size of observation time and observed states are not equal");
            }
            for (int i = 0; i < observation_time.Length-1; i++)
            {
                if (observation_time[i]>=observation_time[i+1])
                {
                    throw new ArgumentException("The observation times are not in ascending order!");
                }
            }
            for (int i = 0; i < observed_states.Length; i++)
            {
                switch (inputMode)
                {
                    case TrailInput.Location_Velocity_Direction:
                        if (observed_states[i].Velocity == null || observed_states[i].Direction == null || observed_states[i].Location == null)
                        {
                            throw new ArgumentException("The observed states include null elements that cannot be used for generating a trail model");
                        }
                        break;
                    case TrailInput.Location_Direction:
                        if (observed_states[i].Direction == null || observed_states[i].Location == null)
                        {
                            throw new ArgumentException("The observed states include null elements that cannot be used for generating a trail model");
                        }
                        break;
                    case TrailInput.Location:
                        if (observed_states[i].Location == null)
                        {
                            throw new ArgumentException("The observed states include null locations that cannot be used for generating a trail model");
                        }
                        break;
                }
            }

            this._trailInput = inputMode;
            this._observationTime = observation_time;
            this._startTime = _observationTime[0];
            this._endTime = _observationTime[_observationTime.Length - 1];
            this._duration = _observationTime[_observationTime.Length - 1] - _observationTime[0];

            this._length = new double[observation_time.Length];
            this._length[0] =0;
            for (int i = 0; i < observed_states.Length-1; i++)
            {
                double d = observed_states[i].Location.DistanceTo(observed_states[i + 1].Location);
                this._length[i + 1] = this._length[i] + d;
            }
            this._trailLength = this._length[this._length.Length - 1];
            this._timeToLength = Interpolate.CubicSplineRobust(this._observationTime, this._length);
            this._lengthToTime = Interpolate.CubicSplineRobust(this._length, this._observationTime);
            
            //create interpolations
            double[] location_U_values = new double[this._observationTime.Length];
            double[] location_V_values = new double[this._observationTime.Length];
            for (int i = 0; i < this._observationTime.Length; i++)
            {
                location_U_values[i] = observed_states[i].Location.U;
                location_V_values[i] = observed_states[i].Location.V;
            }
            this._interpolate_location_U = Interpolate.CubicSplineRobust(this._observationTime, location_U_values);
            this._ULinearInterpolation = Interpolate.Linear(this._observationTime, location_U_values);

            this._interpolate_location_V = Interpolate.CubicSplineRobust(this._observationTime, location_V_values);
            this._VLinearInterpolation = Interpolate.Linear(this._observationTime, location_V_values);

            double[] velocity_U_values;
            double[] velocity_V_values;

            double[] direction_U_values;
            double[] direction_V_values;

            switch (this._trailInput)
            {
                case TrailInput.Location_Velocity_Direction:
                    velocity_U_values = new double[this._observationTime.Length];
                    velocity_V_values = new double[this._observationTime.Length];
                    direction_U_values = new double[this._observationTime.Length];
                    direction_V_values = new double[this._observationTime.Length];
                    for (int i = 0; i < this._observationTime.Length; i++)
                    {
                        velocity_U_values[i] = observed_states[i].Velocity.U;
                        velocity_V_values[i] = observed_states[i].Velocity.V;
                        observed_states[i].Direction.Unitize();
                        direction_U_values[i] = observed_states[i].Direction.U;
                        direction_V_values[i] = observed_states[i].Direction.V;
                    }
                    this._interpolate_velocity_U = Interpolate.CubicSplineRobust(this._observationTime, velocity_U_values);
                    this._interpolate_velocity_V = Interpolate.CubicSplineRobust(this._observationTime, velocity_V_values);
                    this._interpolate_direction_U = Interpolate.CubicSplineRobust(this._observationTime, direction_U_values);
                    this._interpolate_direction_V = Interpolate.CubicSplineRobust(this._observationTime, direction_V_values);
                    break;
                case TrailInput.Location_Direction:
                    direction_U_values = new double[this._observationTime.Length];
                    direction_V_values = new double[this._observationTime.Length];
                    for (int i = 0; i < this._observationTime.Length; i++)
                    {
                        observed_states[i].Direction.Unitize();
                        direction_U_values[i] = observed_states[i].Direction.U;
                        direction_V_values[i] = observed_states[i].Direction.V;
                    }
                    this._interpolate_direction_U = Interpolate.CubicSplineRobust(this._observationTime, direction_U_values);
                    this._interpolate_direction_V = Interpolate.CubicSplineRobust(this._observationTime, direction_V_values);
                    break;
                case TrailInput.Location:
                    //do nothing
                    break;
            }
            this._observedStates = new StateBase[this._observationTime.Length];
            //this._controlPoints = new UV[this._time.Length];
            for (int i = 0; i < this._observedStates.Length; i++)
            {
                UV location = observed_states[i].Location;
                //this._controlPoints[i] = location;
                UV velocity = null;
                if (observed_states[i].Velocity == null)
                {
                    velocity = this.getVelocity(this._observationTime[i]);
                }
                else
                {
                    velocity = observed_states[i].Velocity;
                }
                UV direction = null;
                if (observed_states[i].Direction == null)
                {
                    direction = this.getDirection(this._observationTime[i]);
                }
                else
                {
                    direction = observed_states[i].Direction;
                }
                this._observedStates[i] = new StateBase(location, direction, velocity);
            }
            this.loadNormalizedStates();
            loadApproximatedPolyline();
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="WalkingTrail"/> class. For Internal Use ONLY!
        /// </summary>
        /// <param name="observedPoints">The observed locations.</param>
        /// <param name="velocityMagnitude">The velocity magnitude.</param>
        internal WalkingTrail(UV[] observedPoints, double velocityMagnitude)
        {
            this._length = new double[observedPoints.Length];
            this._trailLength = 0;
            this._length[0] = 0;
            for (int i = 0; i < observedPoints.Length-1; i++)
            {
                double d = observedPoints[i].DistanceTo(observedPoints[i + 1]);
                this._length[i + 1] = this._length[i] + d;
            }
            this._trailLength = this._length[this._length.Length - 1];
            double[] observation_time = new double[this._length.Length];
            observation_time[0] = 0;
            for (int i = 0; i < observedPoints.Length -1; i++)
            {
                double deltaT = observedPoints[i].DistanceTo(observedPoints[i + 1]) / velocityMagnitude;
                observation_time[i + 1] = observation_time[i] + deltaT;
            }

            this._trailInput = TrailInput.Location;
            this._observationTime = observation_time;
            this._startTime = _observationTime[0];
            this._endTime = _observationTime[_observationTime.Length - 1];
            this._duration = _observationTime[_observationTime.Length - 1] - _observationTime[0];
            this._timeToLength = Interpolate.CubicSplineRobust(this._observationTime, this._length);
            this._lengthToTime = Interpolate.CubicSplineRobust(this._length, this._observationTime);

            //create interpolations
            double[] location_U_values = new double[this._observationTime.Length];
            double[] location_V_values = new double[this._observationTime.Length];
            for (int i = 0; i < this._observationTime.Length; i++)
            {
                location_U_values[i] = observedPoints[i].U;
                location_V_values[i] = observedPoints[i].V;
            }
            this._interpolate_location_U = Interpolate.CubicSplineRobust(this._observationTime, location_U_values);
            this._ULinearInterpolation = Interpolate.Linear(this._observationTime, location_U_values);

            this._interpolate_location_V = Interpolate.CubicSplineRobust(this._observationTime, location_V_values);
            this._VLinearInterpolation = Interpolate.Linear(this._observationTime, location_V_values);

            
            this._observedStates = new StateBase[this._observationTime.Length];
            //this._controlPoints = new UV[this._time.Length];
            for (int i = 0; i < this._observedStates.Length; i++)
            {
                UV location = observedPoints[i];
                UV velocity = this.getVelocity(this._observationTime[i]);
                UV direction = this.getDirection(this._observationTime[i]);
                //this._controlPoints[i] = location;
                this._observedStates[i] = new StateBase(location, direction, velocity);
            }
            this.loadNormalizedStates();
            loadApproximatedPolyline();
        }

        /// <summary>
        /// Gets the string representation of the trail.
        /// </summary>
        /// <returns>System.String.</returns>
        public string GetStringRepresentation()
        {
            string s = string.Empty;
            using (System.IO.StringWriter sw = new System.IO.StringWriter())
            {
                sw.WriteLine("# inputMode: " + this.TrailInputMode.ToString());
                sw.WriteLine(((int)this.TrailInputMode).ToString());
                for (int i = 0; i < this.ObservedStates.Length; i++)
                {
                    sw.WriteLine("# time");
                    sw.WriteLine(this._observationTime[i].ToString());
                    sw.WriteLine("# State");
                    sw.WriteLine(this.ObservedStates[i].ToString());
                }
                s = sw.ToString();
            }
            return s;
        }

        /// <summary>
        /// Creates a trail from its string representation.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>WalkingTrail.</returns>
        public static WalkingTrail FromStringRepresentation(string text)
        {
            List<string> lines = new List<string>();
            using (System.IO.StringReader sr = new System.IO.StringReader(text))
            {
                string line = string.Empty;
                while ((line = sr.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line) && !string.IsNullOrEmpty(line))
                    {
                        if (line[0] != '#')
                        {
                            lines.Add(line);
                        }
                    }
                }
            }
            List<double> time = new List<double>();
            List<StateBase> states = new List<StateBase>();
            int mode = int.Parse(lines[0]);
            int i = 1;
            while (i<lines.Count)
            {
                double t = double.Parse(lines[i]);
                time.Add(t);
                StateBase state = StateBase.FromStringRepresentation(lines[i + 1]);
                states.Add(state);
                i += 2;
            }
            WalkingTrail trail = new WalkingTrail(time.ToArray(), states.ToArray(), (TrailInput)mode);
            return trail;
        }

        private void loadApproximatedPolyline()
        {
            int segmentCount = (int)(this.PlineRepresentationLength * _numberOfPointsPerUniteOfLength) + 1;
            double segmentLength = this.PlineRepresentationLength / segmentCount;
            this._approximatedPoints = new UV[segmentCount + 1];
            for (int i = 0; i < segmentCount + 1; i++)
            {
                double time = _lengthToTime.Interpolate(i * segmentLength);
                this._approximatedPoints[i] = getLocation(time);
            }
        }

        private void loadNormalizedStates()
        {
            int count = (int)(this.Duration * this._numberOfStatesPerUniteOfLength);
            this._interpolatedStates = new StateBase[count + 1];
            for (int i = 0; i < count + 1; i++)
            {
                this._interpolatedStates[i] = this.GetState((1.0d * i) / (count), true);
            }
            this._timeInterval = this._duration / count;
        }

        /// <summary>
        /// Gets the normalized time between 0 and 1 on the trail.
        /// </summary>
        /// <param name="observationTime">The observation time.</param>
        /// <returns>System.Double.</returns>
        public double GetNormalizedTime(double observationTime)
        {
            double t = this.StartTime - observationTime;
            t /= this.Duration;
            return t;
        }

        /// <summary>
        /// Gets the observation time of a normalized time.
        /// </summary>
        /// <param name="normalizedTime">The normalized time.</param>
        /// <returns>System.Double.</returns>
        public double GetObservationTime(double normalizedTime)
        {
            double t = normalizedTime * this.Duration;
            t += this.StartTime;
            return t;
        }

        private UV getLocation(double time)
        {
            UV spline = new UV(this._interpolate_location_U.Interpolate(time), this._interpolate_location_V.Interpolate(time));
            UV linear = new UV(this._ULinearInterpolation.Interpolate(time), this._VLinearInterpolation.Interpolate(time));
            return this._curvature * spline + (1.0d - this._curvature) * linear;
        }

        /// <summary>
        /// Gets the location of the agent at the given time.
        /// </summary>
        /// <param name="time">The time.</param>
        /// <param name="isTimeNormalized">if set to <c>true</c> the given time is normalized, otherwise it is the observation time.</param>
        /// <returns>UV.</returns>
        public UV GetLocation(double time, bool isTimeNormalized)
        {
            if (!isTimeNormalized)
            {
                return this.getLocation(time);
            }
            else
            {
                double t = GetObservationTime(time);
                return this.getLocation(t);
            }
        }

        private UV getVelocity(double time)
        {
            if (this._interpolate_velocity_U != null && this._interpolate_velocity_V != null)
            {
                return new UV(this._interpolate_velocity_U.Interpolate(time), this._interpolate_velocity_V.Interpolate(time));
            }
            UV spline = new UV(this._interpolate_location_U.Differentiate(time), this._interpolate_location_V.Differentiate(time));
            UV linear = new UV(this._ULinearInterpolation.Differentiate(time), this._VLinearInterpolation.Differentiate(time));
            return this._curvature * spline + (1.0d - this._curvature) * linear;
        }

        /// <summary>
        /// Gets the velocity of the agent at the given time.
        /// </summary>
        /// <param name="time">The time.</param>
        /// <param name="isTimeNormalized">if set to <c>true</c> the given time is normalized, otherwise it is the observation time.</param>
        /// <returns>UV.</returns>
        public UV GetVelocity(double time, bool isTimeNormalized)
        {
            if (!isTimeNormalized)
            {
                return getVelocity(time);
            }
            else
            {
                double t = GetObservationTime(time);
                return getVelocity(t);
            }
        }

        private UV getDirection(double time)
        {
            if (this._interpolate_direction_U != null && this._interpolate_direction_V != null)
            {
                UV direction = new UV(this._interpolate_direction_U.Interpolate(time), this._interpolate_direction_V.Interpolate(time));
                direction.Unitize();
                return direction;
            }
            else
            {
                UV spline = new UV(this._interpolate_location_U.Differentiate(time), this._interpolate_location_V.Differentiate(time));
                UV linear = new UV(this._ULinearInterpolation.Differentiate(time), this._VLinearInterpolation.Differentiate(time));
                UV direction = this._curvature * spline + (1.0d - this._curvature) * linear;
                direction.Unitize();
                return direction;
            }
        }
        /// <summary>
        /// Gets the direction of the agent at the given time.
        /// </summary>
        /// <param name="time">The time.</param>
        /// <param name="isTimeNormalized">if set to <c>true</c> the given time is normalized, otherwise it is the observation time.</param>
        /// <returns>UV.</returns>
        public UV GetDirection(double time, bool isTimeNormalized)
        {
            if (!isTimeNormalized)
            {
                return getDirection(time);
            }
            else
            {
                double t = GetObservationTime(time);
                return getDirection(t);
            }
        }
        private StateBase getState(double time)
        {
            UV location = this.getLocation(time);
            UV velocity = this.getVelocity(time);
            UV direction = this.getDirection(time);
            return new StateBase(location, direction, velocity);
        }
        /// <summary>
        /// Gets the state of the agent at the given time.
        /// </summary>
        /// <param name="time">The time.</param>
        /// <param name="isTimeNormalized">if set to <c>true</c> the given time is normalized, otherwise it is the observation time.</param>
        /// <returns>StateBase.</returns>
        public StateBase GetState(double time, bool isTimeNormalized)
        {
            if (!isTimeNormalized)
            {
                return getState(time);
            }
            else
            {
                double t = GetObservationTime(time);
                return getState(t);
            }
        }

    }
}

