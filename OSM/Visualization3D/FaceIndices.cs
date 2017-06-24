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
using System.Windows.Media.Media3D;

namespace SpatialAnalysis.Visualization3D
{
    /// <summary>
    /// Class FaceIndices.
    /// Represents a face of a 3D mesh which is a triangle
    /// </summary>
    public class FaceIndices
    {
        /// <summary>
        /// Gets or sets the indices.
        /// </summary>
        /// <value>The indices.</value>
        public int[] Indices { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="FaceIndices"/> class.
        /// </summary>
        /// <param name="v1">The v1.</param>
        /// <param name="v2">The v2.</param>
        /// <param name="v3">The v3.</param>
        public FaceIndices(int v1, int v2, int v3)
        {
            this.Indices = new int[] { v1, v2, v3 };
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="FaceIndices"/> class.
        /// </summary>
        public FaceIndices()
        {
            this.Indices = new int[3];
        }
        /// <summary>
        /// Flips this instance and its normal
        /// </summary>
        public void Flip()
        {
            Array.Reverse(this.Indices);
        }
        /// <summary>
        /// Copies this instance deeply.
        /// </summary>
        /// <returns>FaceIndices.</returns>
        public FaceIndices Copy()
        {
            return new FaceIndices(this.Indices[0], this.Indices[1], this.Indices[2]);
        }
    }
    /// <summary>
    /// Class MeshIntersectionEdge.
    /// </summary>
    internal class MeshIntersectionEdge
    {
        /// <summary>
        /// Gets or sets the start point of the intersecting line segment.
        /// </summary>
        /// <value>The start.</value>
        public Point3D Start { get; set; }
        /// <summary>
        /// Gets or sets the end point of the intersecting line segment.
        /// </summary>
        /// <value>The end.</value>
        public Point3D End { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="MeshIntersectionEdge"/> class.
        /// </summary>
        /// <param name="start">The start point.</param>
        /// <param name="end">The end point.</param>
        public MeshIntersectionEdge(Point3D start, Point3D end)
        {
            this.Start = start;
            this.End = end;
        }
    }
    /// <summary>
    /// Class Face. Represents a face of a mesh.
    /// </summary>
    internal class Face
    {
        /// <summary>
        /// Gets or sets the vertices.
        /// </summary>
        /// <value>The vertices.</value>
        public Point3D[] Vertices { get; set; }
        /// <summary>
        /// Gets or sets the vertex with maximum elevation.
        /// </summary>
        /// <value>The maximum.</value>
        public double Max { get; set; }
        /// <summary>
        /// Gets or sets the vertex with minimum elevation.
        /// </summary>
        /// <value>The minimum.</value>
        public double Min { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Face"/> class.
        /// </summary>
        /// <param name="p1">The p1.</param>
        /// <param name="p2">The p2.</param>
        /// <param name="p3">The p3.</param>
        public Face(Point3D p1, Point3D p2, Point3D p3)
        {
            this.Vertices = new Point3D[] { p1, p2, p3 };
            this.Max = Math.Max(p1.Z, Math.Max(p2.Z, p3.Z));
            this.Min = Math.Min(p1.Z, Math.Min(p2.Z, p3.Z));
        }
        /// <summary>
        /// Determines if a plane at the specified elevation intersects with this face.
        /// </summary>
        /// <param name="elevation">The elevation.</param>
        /// <returns><c>true</c> if intersects, <c>false</c> otherwise.</returns>
        public bool Intersects(double elevation)
        {
            return elevation >= this.Min && elevation <= this.Max;
        }
        private static bool IntersectionsWithLine(Point3D p1, Point3D p2, double elevation)
        {
            return (p1.Z - elevation) * (p2.Z - elevation) <= 0;
        }
        private static Point3D GetIntersectionPoint(Point3D p1, Point3D p2, double elevation)
        { 
            double zDifference = p1.Z - p2.Z;
            var direction = Point3D.Subtract(p2, p1);
            double length = direction.Length;
            //u/length= (p1.Z-elevation)/zDifference
            double u = length * (p1.Z-elevation) / zDifference;
            var normalizedDirection = Vector3D.Divide(direction, length);
            Point3D intersection = p1 + u * normalizedDirection;
            return intersection;
        }
        /// <summary>
        /// Gets the intersection.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <returns>MeshIntersectionEdge.</returns>
        public MeshIntersectionEdge GetIntersection(double offset)
        {
            List<Point3D> plus = new List<Point3D>();
            List<Point3D> zero = new List<Point3D>();
            List<Point3D> minus = new List<Point3D>();
            for (int i = 0; i < 3; i++)
            {
                double z = this.Vertices[i].Z;
                if (z > offset)
                {
                    plus.Add(this.Vertices[i]);
                }
                else if (z == offset)
                {
                    zero.Add(this.Vertices[i]);
                }
                else //if (z < offset)
                {
                    minus.Add(this.Vertices[i]);
                }
            }
            if (plus.Count == 3 || minus.Count == 3 || zero.Count == 3)
            {
                return null;
            }
            if (zero.Count == 1)
            {
                if ((plus.Count == 0 || minus.Count == 0))
                {
                    return null;
                }
                else // there is a point on the top and a point on the bottom
                {
                    return new MeshIntersectionEdge(zero[0], GetIntersectionPoint(plus[0], minus[0], offset));
                }
            }
            if (zero.Count == 2)
            {
                return new MeshIntersectionEdge(zero[0], zero[1]);
            }
            // if (zero.Count == 0)
            var pnts = new List<Point3D>(2);
            foreach (Point3D item1 in plus)
            {
                foreach (Point3D item2 in minus)
                {
                    pnts.Add(GetIntersectionPoint(item2, item1, offset));
                }
            }
            return new MeshIntersectionEdge(pnts[0], pnts[1]);
        }

    }
}

