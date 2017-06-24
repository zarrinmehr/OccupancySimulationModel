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
using SpatialAnalysis.Interoperability;

namespace SpatialAnalysis.Geometry
{
    /// <summary>
    /// Two dimensional line
    /// </summary>
    public class UVLine
    {
        /// <summary>
        /// Gets or sets the start point of the line.
        /// </summary>
        /// <value>The start.</value>
        public UV Start { get; set; }
        /// <summary>
        /// Gets or sets the end point of the line.
        /// </summary>
        /// <value>The end.</value>
        public UV End { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="UVLine"/> class.
        /// </summary>
        /// <param name="start">The start point.</param>
        /// <param name="end">The end point.</param>
        public UVLine(UV start, UV end)
        {
            this.Start = start;
            this.End = end;
        }
        #region utility functions
        /// <summary>
        /// Gets the length of the line.
        /// </summary>
        /// <returns>System.Double.</returns>
        public double GetLength()
        {
            return this.Start.DistanceTo(this.End);
        }
        /// <summary>
        /// Gets the length squared of this line.
        /// </summary>
        /// <returns>System.Double.</returns>
        public double GetLengthSquared()
        {
            return UV.GetLengthSquared(this.Start, this.End);
        }
        /// <summary>
        /// Finds a point on this line with a given parameter.
        /// </summary>
        /// <param name="u">The u.</param>
        /// <returns>UV.</returns>
        public UV FindPoint(double u)
        {
            UV p = this.Start + (u / (this.End.DistanceTo(this.Start))) * (this.End - this.Start);
            return p;
        }
        /// <summary>
        /// Inverts this instance.
        /// </summary>
        public void Invert()
        {
            UV p = new UV(this.Start.U, this.Start.V);
            this.Start = this.End;
            this.End = p;
        }
        /// <summary>
        /// return the direction of the line which is not normalized
        /// </summary>
        /// <returns></returns>
        public UV GetDirection()
        {
            return this.End - this.Start;
        }


        /// <summary>
        /// Returns a parameter at the intersection point with another line if found.
        /// </summary>
        /// <param name="l">The l.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Nullable&lt;System.Double&gt;.</returns>
        public double? Intersection(UVLine l, double tolerance = OSMDocument.AbsoluteTolerance)
        {
            UV lineVector = this.End - this.Start;
            double area1 = lineVector.CrossProductValue(l.Start - this.Start);
            double area2 = lineVector.CrossProductValue(l.End - this.Start);
            if (area1 * area2 > tolerance)
            {
                lineVector = null;
                return null;
            }
            lineVector = l.End - l.Start;
            area1 = lineVector.CrossProductValue(this.Start - l.Start);
            area2 = lineVector.CrossProductValue(this.End - l.Start);
            if (area1 * area2 > tolerance)
            {
                lineVector = null;
                return null;
            }
            //double lengthL = l.GetLength();
            double a1 = (l.Start - this.Start).CrossProductValue(l.End - this.Start);
            if (a1 == 0)
            {
                return Math.Min(l.Start.DistanceTo(this.Start), l.End.DistanceTo(this.Start));
            }
            lineVector = null;
            double u = this.GetLength() * Math.Abs(a1) / (Math.Abs(area1) + Math.Abs(area2));
            return u;
        }

        /// <summary>
        /// Returns a parameter at the intersection point with another line if found. This function takes the length of this line if it is available for performance optimization.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="length">The length of this line.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Nullable&lt;System.Double&gt;.</returns>
        public double? Intersection(UVLine line, double length, double tolerance = .000001f)
        {
            UV lineVector = this.End - this.Start;
            double area1 = lineVector.CrossProductValue(line.Start - this.Start);
            double area2 = lineVector.CrossProductValue(line.End - this.Start);
            if (area1 * area2 > tolerance)
            {
                lineVector = null;
                return null;
            }
            lineVector = line.End - line.Start;
            area1 = lineVector.CrossProductValue(this.Start - line.Start);
            area2 = lineVector.CrossProductValue(this.End - line.Start);
            if (area1 * area2 > tolerance)
            {
                lineVector = null;
                return null;
            }
            double a1 = (line.Start - this.Start).CrossProductValue(line.End - this.Start);
            if (a1 == 0)
            {
                return Math.Min(line.Start.DistanceTo(this.Start), line.End.DistanceTo(this.Start));
            }
            lineVector = null;
            double u = length * Math.Abs(a1) / (Math.Abs(area1) + Math.Abs(area2));
            return u;
        }

        /// <summary>
        /// Determines if this line intersects with another line.
        /// </summary>
        /// <param name="l">The l.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool Intersects(UVLine l, double tolerance = OSMDocument.AbsoluteTolerance)
        {
            UV lineVector = this.End - this.Start;
            double area1 = lineVector.CrossProductValue(l.Start - this.Start);
            double area2 = lineVector.CrossProductValue(l.End - this.Start);
            if (area1 * area2 > tolerance)
            {
                lineVector = null;
                return false;
            }
            lineVector = l.End - l.Start;
            area1 = lineVector.CrossProductValue(this.Start - l.Start);
            area2 = lineVector.CrossProductValue(this.End - l.Start);
            if (area1 * area2 > tolerance)
            {
                lineVector = null;
                return false;
            }
            return true;
        }
        /// <summary>
        /// Visualizes this line in the BIM environment
        /// </summary>
        /// <param name="visualizer">The visualizer.</param>
        /// <param name="elevation">The elevation.</param>
        public void Visualize(I_OSM_To_BIM visualizer, double elevation)
        {
            visualizer.VisualizeLine(this, elevation);
        }

        #endregion

        public override int GetHashCode()
        {
            UV less = new UV(), more = new UV();
            if (Start.CompareTo(End) > 0)
            {
                less = End;
                more = Start;
            }
            else
            {
                less = Start;
                more = End;
            }
            int hash = less.GetHashCode();
            hash = 71 * hash + more.U.GetHashCode();
            hash = 71 * hash + more.V.GetHashCode();
            return hash;
        }
        public override bool Equals(object obj)
        {
            UVLine l = obj as UVLine;
            if (l==null)
            {
                return false;
            }
            else
            {
                if ((l.Start == this.Start && l.End == this.End) ||
                    (l.End == this.Start && l.Start == this.End))
                {
                    return true;
                }
            }
            return false;
        }
        public override string ToString()
        {
            return string.Format("Start ({0}); End ({1})", this.Start.ToString(), this.End.ToString());
        }

    }
}

