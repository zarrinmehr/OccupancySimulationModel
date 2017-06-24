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

namespace SpatialAnalysis.CellularEnvironment
{
    /// <summary>
    /// Represents an interval between two real numbers
    /// </summary>
    public class Interval
    {
        /// <summary>
        /// The minimum value of the interval
        /// </summary>
        public double Minimum { get; set; }
        /// <summary>
        /// The maximum value of the interval
        /// </summary>
        public double Maximum { get; set; }
        /// <summary>
        /// the length of the interval
        /// </summary>
        public double Length
        {
            get { return this.Maximum - this.Minimum; }
        }
        /// <summary>
        /// creates and interval with two numbers
        /// </summary>
        /// <param name="p">A real number</param>
        /// <param name="q">Another real number</param>
        public Interval(double p, double q)
        {
            this.Maximum = Math.Max(p, q);
            this.Minimum = Math.Min(p, q);
        }
        /// <summary>
        /// Reports if a number is included in this interval
        /// </summary>
        /// <param name="t">A real number</param>
        /// <returns>True if it is inside the interval, false if it is not inside the interval</returns>
        public bool Includes(double t)
        {
            if (t <= this.Maximum && t >= this.Minimum)
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// Reports if this interval intersects with another interval
        /// </summary>
        /// <param name="interval">Another interval</param>
        /// <returns>True if intersection exists, false if intersection does not exist</returns>
        public bool Overlaps(Interval interval)
        {
            if (this.Includes(interval.Maximum))
            {
                return true;
            }
            if (this.Includes(interval.Minimum))
            {
                return true;
            }
            if (interval.Includes(this.Minimum))
            {
                return true;
            }
            if (interval.Includes(this.Maximum))
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// Creates an interval of two overlapping intervals
        /// </summary>
        /// <param name="interval">Another interval</param>
        /// <returns>The overlapping interval</returns>
        public Interval Overlap(Interval interval)
        {
            if (!this.Overlaps(interval))
            {
                return null;
            }
            return new Interval(Math.Max(this.Minimum, interval.Minimum), Math.Min(this.Maximum, interval.Maximum));
        }
        /// <summary>
        /// Finds the interval of a line at U direction
        /// </summary>
        /// <param name="line">A line to find the interval for</param>
        /// <returns>A new interval</returns>
        public static Interval LineUValues(UVLine line)
        {
            return new Interval(line.Start.U, line.End.U);
        }
        /// <summary>
        /// Finds the interval of a line at V direction
        /// </summary>
        /// <param name="line">A line to find the interval for</param>
        /// <returns>A new interval</returns>
        public static Interval LineVValues(UVLine line)
        {
            return new Interval(line.Start.V, line.End.V);
        }


    }
}

