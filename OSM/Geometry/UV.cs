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
    /// A two dimensional point or vector
    /// </summary>
    public class UV : IComparable<UV>
    {
        /// <summary>
        /// Gets or sets the u value.
        /// </summary>
        /// <value>The u.</value>
        public double U { get; set; }
        /// <summary>
        /// Gets or sets the v value.
        /// </summary>
        /// <value>The v.</value>
        public double V { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="UV"/> class.
        /// </summary>
        public UV()
        {
            this.U = 0;
            this.V = 0;
        }
        public UV(UV uv) 
        { 
            this.U = uv.U;
            this.V = uv.V;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="UV"/> class.
        /// </summary>
        /// <param name="u">The u value.</param>
        /// <param name="v">The v value.</param>
        public UV(double u, double v)
        {
            this.U = u;
            this.V = v;
        }

        #region Utility functions
        /// <summary>
        /// returns yje cross product value.
        /// </summary>
        /// <param name="vector2D">The vector2 d.</param>
        /// <returns>System.Double.</returns>
        public double CrossProductValue(UV vector2D)
        {
            return this.U * vector2D.V - this.V * vector2D.U;
        }

        /// <summary>
        /// Rotates the this vector and returns the new UV.
        /// </summary>
        /// <param name="angle">The angle.</param>
        /// <returns>UV.</returns>
        public UV RotateNew(double angle)
        {
            double x = Math.Cos(angle) * this.U - Math.Sin(angle) * this.V;
            double y = Math.Sin(angle) * this.U + Math.Cos(angle) * this.V;
            return new UV(x, y);
        }
        /// <summary>
        /// Rotates this UV at the specified angle.
        /// </summary>
        /// <param name="angle">The angle.</param>
        public void Rotate(double angle)
        {
            double x = Math.Cos(angle) * this.U - Math.Sin(angle) * this.V;
            double y = Math.Sin(angle) * this.U + Math.Cos(angle) * this.V;
            this.U = x;
            this.V = y;
        }

        /// <summary>
        /// Gets the length of the UV as a vector.
        /// </summary>
        /// <returns>System.Double.</returns>
        public double GetLength()
        {
            return Math.Sqrt(this.U * this.U + this.V * this.V);
        }
        /// <summary>
        /// Gets the length squared of this UV as a vector.
        /// </summary>
        /// <returns>System.Double.</returns>
        public double GetLengthSquared()
        {
            return this.U * this.U + this.V * this.V;
        }

        /// <summary>
        /// Gets the length squared between two UVs as points.
        /// </summary>
        /// <param name="a">Point a.</param>
        /// <param name="b">Point b.</param>
        /// <returns>System.Double.</returns>
        public static double GetLengthSquared(UV a, UV b)
        {
            return (a.U - b.U) * (a.U - b.U) + (a.V - b.V) * (a.V - b.V);
        }
        /// <summary>
        /// Gets the distance between two UVs as points.
        /// </summary>
        /// <param name="a">Point a.</param>
        /// <param name="b">Point b.</param>
        /// <returns>System.Double.</returns>
        public static double GetDistanceBetween(UV a, UV b)
        {
            return Math.Sqrt((a.U - b.U) * (a.U - b.U) + (a.V - b.V) * (a.V - b.V));
        }
        /// <summary>
        /// Unitizes this instance.
        /// </summary>
        public void Unitize()
        {
            double length = this.GetLength();
            if (length != 0)
            {
                this.U /= length;
                this.V /= length;
            }
        }

        /// <summary>
        /// Almost the equals to.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <param name="DistTolerance">The dist tolerance.</param>
        /// <returns><c>true</c> if almost equal, <c>false</c> otherwise.</returns>
        public bool AlmostEqualsTo(object p, double DistTolerance = OSMDocument.AbsoluteTolerance)
        {
            UV xy = p as UV;
            if (xy != null)
            {
                if (this.DistanceTo(xy) < DistTolerance)
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Returns the angle with the U axis on clockwise direction.
        /// </summary>
        /// <returns>System.Double.</returns>
        public double Angle()
        {
            return this.AngleTo(UV.UBase);
        }

        /// <summary>
        /// Returns the angle with another vector on clockwise direction.
        /// </summary>
        /// <param name="xy">The xy.</param>
        /// <returns>System.Double.</returns>
        public double AngleTo(UV xy)
        {
            if (this.Equals(UV.ZeroBase) || xy.Equals(UV.ZeroBase))
            {
                return 0.0d;
            }
            double dot = this.DotProduct(xy) / (this.GetLength() * xy.GetLength());
            if (dot >= 1)
            {
                return 0;
            }
            if (dot <= -1)
            {
                return Math.PI;
            }
            double angle = Math.Acos(dot);
            if (angle < 0)
            {
                angle = Math.PI - angle;
            }
            double cross = this.CrossProductValue(xy);
            if (cross > 0)
            {
                angle *= -1;
            }
            return angle;
        }
        /// <summary>
        /// Retrns the distance between this UV as a point from another UV point.
        /// </summary>
        /// <param name="p">P as a point.</param>
        /// <returns>System.Double.</returns>
        public double DistanceTo(UV p)
        {
            return Math.Sqrt((this.U - p.U) * (this.U - p.U) + (this.V - p.V) * (this.V - p.V));
        }

        /// <summary>
        /// Returns the dot product of this UV as a vector with another UV
        /// </summary>
        /// <param name="v">V as a vector.</param>
        /// <returns>System.Double.</returns>
        public double DotProduct(UV v)
        {
            return this.U * v.U + this.V * v.V;
        }
        #endregion

        #region Defining operators
        /// <summary>
        /// Implements the == operator.
        /// </summary>
        /// <param name="a">UV a.</param>
        /// <param name="b">UV b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(UV a, UV b)
        {
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }
            if ((object)a == null || (object)b == null)
            {
                return false;
            }
            return a.U == b.U && a.V == b.V;
        }
        /// <summary>
        /// Implements the != operator.
        /// </summary>
        /// <param name="a">UV a.</param>
        /// <param name="b">UV b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(UV a, UV b)
        {
            return !(a == b);
        }
        /// <summary>
        /// Implements the + operator.
        /// </summary>
        /// <param name="a">UV a.</param>
        /// <param name="b">UV b.</param>
        /// <returns>The result of the operator.</returns>
        public static UV operator +(UV a, UV b)
        {
            return new UV(a.U + b.U, a.V + b.V);
        }
        /// <summary>
        /// Implements the - operator.
        /// </summary>
        /// <param name="a">UV a.</param>
        /// <param name="b">UV b.</param>
        /// <returns>The result of the operator.</returns>
        public static UV operator -(UV a, UV b)
        {
            return new UV(a.U - b.U, a.V - b.V);
        }
        /// <summary>
        /// Implements the / operator.
        /// </summary>
        /// <param name="a">UV a.</param>
        /// <param name="k">The scalar k.</param>
        /// <returns>The result of the operator.</returns>
        public static UV operator /(UV a, double k)
        {
            return new UV(a.U / k, a.V / k);
        }
        /// <summary>
        /// Implements the * operator.
        /// </summary>
        /// <param name="a">UV a.</param>
        /// <param name="k">The scalar k.</param>
        /// <returns>The result of the operator.</returns>
        public static UV operator *(UV a, double k)
        {
            return new UV(a.U * k, a.V * k);
        }

        /// <summary>
        /// Implements the * operator.
        /// </summary>
        /// <param name="k">The scalar k.</param>
        /// <param name="a">UV a.</param>
        /// <returns>The result of the operator.</returns>
        public static UV operator *(double k, UV a)
        {
            return new UV(a.U * k, a.V * k);
        }
        /// <summary>
        /// The u base
        /// </summary>
        public static readonly UV UBase = new UV(1, 0);
        /// <summary>
        /// The v base
        /// </summary>
        public static readonly UV VBase = new UV(0, 1);
        /// <summary>
        /// The zero base
        /// </summary>
        public static readonly UV ZeroBase = new UV(0, 0);
        #endregion

        public override bool Equals(object p)
        {
            UV xy = p as UV;
            if (xy != null)
            {
                if (this.U == xy.U && this.V == xy.V)
                {
                    return true;
                }
            }
            return false;
        }
        public override int GetHashCode()
        {
            int hash = 7;
            hash = 71 * hash + this.U.GetHashCode();
            hash = 71 * hash + this.V.GetHashCode();
            return hash;
        }
        public override string ToString()
        {
            return string.Format("[{0}, {1}]", this.U.ToString(), this.V.ToString());
        }
        /// <summary>
        /// Return ths distance of the UV as a point from a line.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns>System.Double.</returns>
        public double DistanceTo(UVLine line)
        {
            double area = (line.Start - line.End).CrossProductValue(line.Start - this);
            area = Math.Abs(area);
            return area / line.GetLength();
        }
        /// <summary>
        /// Returns the projection of this UV as a point from a specified line.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns>UV.</returns>
        public UV Projection(UVLine line)
        {
            double u = (line.End - line.Start).DotProduct(this - line.Start);
            u /= line.GetLength();
            return line.FindPoint(u);
        }
        /// <summary>
        /// Returns the closest distance of this UV as a point from a line .
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns>System.Double.</returns>
        public double ClosestDistance(UVLine line)
        {
            UV p = new UV();
            double length = line.GetLength();
            double u = (line.End - line.Start).DotProduct(this - line.Start);
            u /= length;
            if (u < 0)
            {
                p = line.Start;
            }
            else if (u > length)
            {
                p = line.End;
            }
            else
            {
                p = this.GetClosestPoint(line);
            }
            return p.DistanceTo(this);
        }
        /// <summary>
        /// Gets the distance squared of this UV as a point from another UV as a point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>System.Double.</returns>
        public double GetDistanceSquared(UV point)
        {
            return UV.GetLengthSquared(this, point);
        }

        /// <summary>
        /// Gets the distance squared of this UV as a point from a line.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns>System.Double.</returns>
        public double GetDistanceSquared(UVLine line)
        {
            UV vs = this - line.Start;
            double vs2 = vs.GetLengthSquared();
            UV ve = this - line.End;
            double ve2 = ve.GetLengthSquared();
            double area = vs.CrossProductValue(ve);
            area *= area;
            double d = UV.GetLengthSquared(line.Start, line.End);
            double distSquared = area / d;
            if (distSquared < vs2 - d || distSquared < ve2 - d)
            {
                distSquared = Math.Min(vs2, ve2);
            }
            return distSquared;
        }
        /// <summary>
        /// Gets the closest point of this UV as a point from a line.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns>UV.</returns>
        public UV GetClosestPoint(UVLine line)
        {
            double length = line.GetLength();
            double u = (line.End - line.Start).DotProduct(this - line.Start);
            u /= length;
            if (u < 0)
            {
                return line.Start;
            }
            if (u > length)
            {
                return line.End;
            }
            return line.FindPoint(u);
        }
        /// <summary>
        /// Gets the closest point of this UV as a point from a line.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="isEndPoint">if set to <c>true</c> the closest point is the end point of the line.</param>
        /// <returns>UV.</returns>
        public UV GetClosestPoint(UVLine line, ref bool isEndPoint)
        {
            double length = line.GetLength();
            double u = (line.End - line.Start).DotProduct(this - line.Start);
            u /= length;
            if (u < 0)
            {
                isEndPoint = true;
                return line.Start;
            }
            if (u > length)
            {
                isEndPoint = true;
                return line.End;
            }
            isEndPoint = false;
            return line.FindPoint(u);
        }
        /// <summary>
        /// Shows the point in the BIM environment.
        /// </summary>
        /// <param name="visualizer">The visualizer.</param>
        /// <param name="size">The size of the cross.</param>
        /// <param name="elevation">The elevation.</param>
        public void ShowPoint(I_OSM_To_BIM visualizer, double size, double elevation = 0.0d)
        {
            visualizer.VisualizePoint(this, size, elevation);
        }
        /// <summary>
        /// Compares the current object with another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>A value that indicates the relative order of the objects being compared. The return value has the following meanings: Value Meaning Less than zero This object is less than the <paramref name="other" /> parameter.Zero This object is equal to <paramref name="other" />. Greater than zero This object is greater than <paramref name="other" />.</returns>
        public int CompareTo(UV other)
        {
            int uCompared = this.U.CompareTo(other.U);
            if (uCompared != 0)
            {
                return uCompared;
            }
            return this.V.CompareTo(other.V);
        }
        /// <summary>
        /// Gets the reflection of this UV as a vector from another UV as a vector.
        /// </summary>
        /// <param name="vector">The vector.</param>
        /// <returns>UV.</returns>
        public UV GetReflection(UV vector)
        {
            if (vector.GetLengthSquared() != 1)
            {
                vector.Unitize();
            }
            double dotPro = this.DotProduct(vector);
            UV horizontal_Component = vector * dotPro;
            UV normal_Component = this - horizontal_Component;
            var result = horizontal_Component - normal_Component;
            return result;
        }
        /// <summary>
        /// Gets the reflection of this UV as a vector from a line.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns>UV.</returns>
        public UV GetReflection(UVLine line)
        {
            var vector = line.GetDirection();
            vector.Unitize();
            double dotPro = this.DotProduct(vector);
            UV horizontal_Component = vector * dotPro;
            UV normal_Component = this - horizontal_Component;
            var result = horizontal_Component - normal_Component;
            return result;
        }
        /// <summary>
        /// Copies this instance deeply.
        /// </summary>
        /// <returns>UV.</returns>
        public UV Copy()
        {
            return new UV(this.U, this.V);
        }
    }

}

