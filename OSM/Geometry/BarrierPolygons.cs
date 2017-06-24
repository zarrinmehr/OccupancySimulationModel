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
    /// Class BarrierPolygons.
    /// </summary>
    public class BarrierPolygons
    {
        /// <summary>
        /// Gets or sets a value indicating whether this instance is closed.
        /// </summary>
        /// <value><c>true</c> if this instance is closed; otherwise, <c>false</c>.</value>
        public bool IsClosed { get; set; }
        /// <summary>
        /// Gets or sets the boundary points.
        /// </summary>
        /// <value>The boundary points.</value>
        public UV[] BoundaryPoints { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether has ascending order.
        /// </summary>
        /// <value><c>true</c> if [ascending order]; otherwise, <c>false</c>.</value>
        public bool AscendingOrder { get; set; }
        /// <summary>
        /// Gets the length of the point array
        /// </summary>
        /// <value>The length.</value>
        public int Length
        {
            get { return this.BoundaryPoints.Length; }
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="BarrierPolygons"/> class.
        /// </summary>
        /// <param name="points">An rray of UV points.</param>
        public BarrierPolygons(UV[] points)
        {
            this.AscendingOrder = true;
            this.BoundaryPoints = points;
        }
        /// <summary>
        /// Returns the index before this index
        /// </summary>
        /// <param name="currentIndex">Current index of the point.</param>
        /// <returns>System.Int32.</returns>
        public int PreviousIndex(int currentIndex)
        {
            int pre;
            if (this.AscendingOrder)
            {
                pre = (currentIndex == 0) ? this.BoundaryPoints.Length - 1 : currentIndex - 1;
            }
            else
            {
                pre = (currentIndex == this.BoundaryPoints.Length - 1) ? 0 : currentIndex + 1;
            }
            return pre;
        }
        /// <summary>
        /// Returns the index after this index
        /// </summary>
        /// <param name="currentIndex">Current index of the point.</param>
        /// <returns>System.Int32.</returns>
        public int NextIndex(int currentIndex)
        {
            int next;
            if (this.AscendingOrder)
            {
                next = (currentIndex == this.BoundaryPoints.Length - 1) ? 0 : currentIndex + 1;
            }
            else
            {
                next = (currentIndex == 0) ? this.BoundaryPoints.Length - 1 : currentIndex - 1;
            }
            return next;
        }
        /// <summary>
        /// Points at the provided index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>UV.</returns>
        public UV PointAt(int index)
        {
            return this.BoundaryPoints[index];
        }
        /// <summary>
        /// Visualizes the polygon in the BIM environment.
        /// </summary>
        /// <param name="visualizer">The visualizer.</param>
        /// <param name="elevation">The elevation.</param>
        public void Visualize(I_OSM_To_BIM visualizer, double elevation)
        {
            visualizer.VisualizePolygon(this.BoundaryPoints, elevation);
        }
        /// <summary>
        /// Returns the area of the polygon
        /// </summary>
        /// <returns>System.Double.</returns>
        public double GetArea()
        {
            double area = 0;
            for (int i = 1; i < this.Length - 1; i++)
            {
                var a = this.PointAt(i) - this.PointAt(0);
                var b = this.PointAt(this.NextIndex(i)) - this.PointAt(0);
                area += a.CrossProductValue(b);
            }
            return .5 * Math.Abs(area);
        }
        /// <summary>
        /// Returns the perimeter of the polygon.
        /// </summary>
        /// <returns>System.Double.</returns>
        public double GetPerimeter()
        {
            double perimeter = 0;
            for (int i = 0; i < this.Length; i++)
            {
                perimeter += this.PointAt(i).DistanceTo(this.PointAt(this.NextIndex(i)));
            }
            return perimeter;
        }
        /// <summary>
        /// Determines whether the specified list of points determine a convex polygon
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns><c>true</c> if the specified points is convex; otherwise, <c>false</c>.</returns>
        public static bool IsConvex(UV[] points)
        {
            double[] z_values = new double[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                int next = i + 1;
                if (next == points.Length) next = 0;
                int before = i - 1;
                if (before == -1) before = points.Length - 1;
                var crossProductValue = (points[next] - points[i]).CrossProductValue(points[before] - points[i]);
                z_values[i] = crossProductValue;
            }
            int sign = 0;
            foreach (var item in z_values)
            {
                int k= Math.Sign(item);
                if (k!=0)
                {
                    sign = k;
                    break;
                }
            }
            foreach (var item in z_values)
            {
                int k = Math.Sign(item);
                if (k != 0 && k!=sign)
                {
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// Determines whether this instance is convex.
        /// </summary>
        /// <returns><c>true</c> if this instance is convex; otherwise, <c>false</c>.</returns>
        public bool IsConvex()
        {
            return BarrierPolygons.IsConvex(this.BoundaryPoints);
        }
        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < this.Length; i++)
            {
                sb.Append(this.BoundaryPoints[i].U.ToString());
                sb.Append(",");
                sb.Append(this.BoundaryPoints[i].V.ToString());
                sb.Append(",");
            }
            sb.Append(this.IsClosed.ToString());
            string s = sb.ToString();
            sb.Clear();
            sb = null;
            return s;
        }
        /// <summary>
        /// Creates an instance of BarrierPolygons from its string representation
        /// </summary>
        /// <param name="s">The string representation.</param>
        /// <returns>BarrierPolygons.</returns>
        /// <exception cref="ArgumentException">
        /// Cannot create a polygon from an empty string
        /// or
        /// Failed to parse polygon's closed property: " + strings[strings.Length - 1]
        /// or
        /// Failed to parse polygon's points: " + strings[2 * i] + ", " + strings[2 * i + 1]
        /// </exception>
        public static BarrierPolygons FromStringRepresentation(string s)
        {
            if (string.IsNullOrWhiteSpace(s) || string.IsNullOrEmpty(s))
            {
                throw new ArgumentException("Cannot create a polygon from an empty string");
            }
            var strings = s.Split(',');
            bool isclosed;
            if (!bool.TryParse(strings[strings.Length - 1], out isclosed))
            {
                throw new ArgumentException("Failed to parse polygon's closed property: " + strings[strings.Length - 1]);
            }
            UV[] pnts = new UV[(strings.Length - 1) / 2];
            for (int i = 0; i < pnts.Length; i++)
            {
                double u = 0, v = 0;
                if (double.TryParse(strings[2 * i], out u) && double.TryParse(strings[2 * i+1], out v))
                {
                    pnts[i] = new UV(double.Parse(strings[2 * i]), double.Parse(strings[2 * i + 1]));
                }
                else
                {
                    throw new ArgumentException("Failed to parse polygon's points: " + strings[2 * i] + ", " + strings[2 * i + 1]);
                }
            }
            BarrierPolygons polygon = new BarrierPolygons(pnts);
            polygon.IsClosed = isclosed;
            strings = null;
            return polygon;
        }

        public UV GetCenter()
        {
            var center = UV.ZeroBase;
            foreach (var item in this.BoundaryPoints)
            {
                center += item;
            }
            return center / this.Length;
        }

        public static void VisualizeBarriers(I_OSM_To_BIM visualizer, BarrierPolygons[] barriers, double height = 0)
        {
            foreach (BarrierPolygons item in barriers)
            {
                visualizer.VisualizePolygon(item.BoundaryPoints, height);
            }
        }
    }
}

