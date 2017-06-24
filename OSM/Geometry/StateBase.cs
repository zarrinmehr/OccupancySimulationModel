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
using SpatialAnalysis.Geometry;

namespace SpatialAnalysis.Geometry
{
    /// <summary>
    /// Represents the location and direction of agents
    /// </summary>
    public class StateBase
    {
        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        /// <value>The location.</value>
        public UV Location { get; set; }
        /// <summary>
        /// Gets or sets the direction.NOTE: Direction should be a normalized vector
        /// </summary>
        /// <value>The direction.</value>
        public UV Direction { get; set; }

        /// <summary>
        /// Gets or sets the velocity.
        /// </summary>
        /// <value>The velocity.</value>
        public UV Velocity { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StateBase"/> class.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="velocityMagnitude">The velocity magnitude.</param>
        public StateBase(UV location, UV direction, double velocityMagnitude)
        {
            this.Direction = direction;
            this.Velocity = direction.Copy();
            this.Velocity.Unitize();
            this.Velocity *= velocityMagnitude;
            this.Location = location;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="StateBase"/> class.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="direction">The direction.</param>
        public StateBase(UV location, UV direction)
        {
            this.Direction = direction;
            this.Velocity = direction.Copy();
            this.Location = location;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="StateBase"/> class.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="velocity">The velocity.</param>
        public StateBase(UV location, UV direction, UV velocity)
        {
            this.Direction = direction;
            this.Location = location;
            this.Velocity = velocity;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="StateBase"/> class.
        /// </summary>
        /// <param name="location_u">The location u.</param>
        /// <param name="location_v">The location v.</param>
        /// <param name="position_u">The position u.</param>
        /// <param name="position_v">The position v.</param>
        public StateBase(double location_u, double location_v, double position_u, double position_v)
        {
            this.Direction = new UV(position_u, position_v);
            this.Location = new UV(location_u, location_v);
        }
        public override bool Equals(object obj)
        {
            StateBase other = (StateBase)obj;
            if (other==null)
            {
                return false;
            }
            if (this.Velocity == null && other.Velocity == null)
            {
                return other.Direction == this.Direction
                    && other.Location == this.Location;
            }
            if (this.Velocity != null && other.Velocity == null)
            {
                return false;
            }
            if (this.Velocity == null && other.Velocity != null)
            {
                return false;
            }
            return other.Direction == this.Direction
                && other.Location == this.Location
                && other.Velocity == this.Velocity;
        }
        public override int GetHashCode()
        {
            int hash = 7;
            hash = 71 * hash + this.Location.U.GetHashCode();
            hash = 71 * hash + this.Location.V.GetHashCode();
            hash = 71 * hash + this.Direction.U.GetHashCode();
            hash = 71 * hash + this.Direction.V.GetHashCode();
            return hash;
        }
        public override string ToString()
        {
            if (this.Velocity != null)
            {
                return string.Format("Location:[{0},{1}]; Velocity:[{2},{3}]; Direction: [{4},{5}]", this.Location.U.ToString(),
                    this.Location.V.ToString(), this.Velocity.U.ToString(),this.Velocity.V.ToString(), 
                    this.Direction.U.ToString(), this.Direction.V.ToString());
            }
            return string.Format("Location:[{0},{1}]; Velocity:[null]; Direction: [{2},{3}]", this.Location.U.ToString(),
                this.Location.V.ToString(), this.Direction.U.ToString(), this.Direction.V.ToString());
        }
        /// <summary>
        /// Creates an instance of StateBase from its string representation.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <returns>StateBase.</returns>
        /// <exception cref="ArgumentException">
        /// Failed to parse state's location property
        /// or
        /// Failed to parse state's velocity property
        /// or
        /// Failed to parse state's direction property
        /// or
        /// Failed to parse state's direction property
        /// </exception>
        public static StateBase FromStringRepresentation(string s)
        {
            StateBase state = null;
            s= s.Replace("Location:[", string.Empty);
            s= s.Replace("; Velocity:[", ",");
            s= s.Replace("]; Direction: [", ",");
            s= s.Replace("]", string.Empty);
            var strings = s.Split(',');
            double u = 0, v = 0;
            UV location = null;
            if (double.TryParse(strings[0], out u) && double.TryParse(strings[1],out v))
            {
                location = new UV(u,v);
            }
            else
            {
                throw new ArgumentException("Failed to parse state's location property");
            }
            UV velocity = null;
            UV direction = null;
            if (!s.Contains("null"))
            {
                if (double.TryParse(strings[2], out u) && double.TryParse(strings[3], out v))
                {
                    velocity = new UV(u, v);
                }
                else
                {
                    throw new ArgumentException("Failed to parse state's velocity property");
                }
                if (double.TryParse(strings[4], out u) && double.TryParse(strings[5], out v))
                {
                    direction = new UV(u, v);
                }
                else
                {
                    throw new ArgumentException("Failed to parse state's direction property");
                }
                state = new StateBase(location, direction, velocity);
            }
            else
            {
                if (double.TryParse(strings[3], out u) && double.TryParse(strings[4], out v))
                {
                    direction = new UV(u, v);
                }
                else
                {
                    throw new ArgumentException("Failed to parse state's direction property");
                }
                state = new StateBase(location, direction);
                state.Velocity = null;
            }
            strings = null;
            return state;
        }
        /// <summary>
        /// Copies this instance deeply.
        /// </summary>
        /// <returns>StateBase.</returns>
        public StateBase Copy()
        {
            return new StateBase(this.Location.Copy(), this.Direction.Copy(), this.Velocity.Copy());
        }
        public static StateBase operator +(StateBase a, StateBase b)
        {
            if (a.Direction.GetLengthSquared() != 1)
            {
                a.Direction.Unitize();
            }
            if (b.Direction.GetLengthSquared()!=1)
            {
                b.Direction.Unitize();
            }
            var newState = new StateBase(a.Location + b.Location, a.Direction + b.Direction, a.Velocity + b.Velocity);
            newState.Direction.Unitize();
            return newState;
        }
        public static StateBase operator *(double k, StateBase b)
        {
            return new StateBase(k * b.Location, Math.Sign(k) * b.Direction, k * b.Velocity);
        }
        public static StateBase operator *(StateBase b, double k)
        {
            return new StateBase(k * b.Location, Math.Sign(k) * b.Direction, k * b.Velocity);
        }
        /// <summary>
        /// Gets the squared distance between two states.
        /// </summary>
        /// <param name="state1">The state1.</param>
        /// <param name="state2">The state2.</param>
        /// <returns>System.Double.</returns>
        public static double DistanceSquared(StateBase state1, StateBase state2)
        {
            return UV.GetLengthSquared(state1.Direction, state2.Direction) + UV.GetLengthSquared(state1.Location, state2.Location) + UV.GetLengthSquared(state1.Velocity, state2.Velocity);
        }
    }
    //provides a list of associative operations for StateBase
    internal class StateAverage
    {
        public UV _location, _velocity, _direction;
        public double _w;
        /// <summary>
        /// A helper class to calculate the average of a collection of states primarily used for integration purposes
        /// </summary>
        public StateAverage()
        {
            this._location = new UV();
            this._direction = new UV();
            this._velocity = new UV();
            this._w = 0;
        }
        /// <summary>
        /// Add a new State to affect the average
        /// </summary>
        /// <param name="s">The new state to be added</param>
        /// <param name="w">The weihting factor of the new state</param>
        public void AddState(StateBase s, double w)
        {
            this._location += s.Location * w;
            this._direction += s.Direction * w;
            this._velocity += s.Velocity * w;
            this._w += w;
        }
        /// <summary>
        /// Get the average of the included states
        /// </summary>
        /// <returns>The average states</returns>
        public StateBase GetAverage()
        {
            if (this._w == 0)
            {
                return null;
            }
            UV location = this._location / this._w;
            UV direction = this._direction.Copy();
            direction.Unitize();
            UV velocity = this._velocity / this._w;
            return new StateBase(location, direction, velocity);
        }
        /// <summary>
        /// Resets the values to use the operator again for calculating a new average
        /// </summary>
        public void Rest()
        {
            this._location.U = 0;
            this._location.V = 0;
            this._direction.U = 0;
            this._direction.V = 0;
            this._velocity.U = 0;
            this._velocity.V = 0;
            this._w = 0;
        }
        public static StateBase Average(StateBase s1, double w1, StateBase s2, double w2)
        {
            double w = w1 + w2;
            if (w == 0) return null;
            var location = (s1.Location * w1 + s2.Location * w2) / (w1 + w2);
            var velocity = (s1.Velocity * w1 + s2.Velocity * w2) / (w1 + w2);
            var direction = s1.Direction * w1 + s2.Direction * w2;
            direction.Unitize();
            return new StateBase(location, direction, velocity);
        }
    }
}

