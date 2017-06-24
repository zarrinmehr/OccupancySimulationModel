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
using System.Windows.Media.Media3D;


namespace SpatialAnalysis.Visualization3D
{
    /// <summary>
    /// Class MeshGeometry3DToContours.
    /// This class finds the contours that are created when a list of planes intersect with a mesh.
    /// This class is designed for enhancement of performance and efficiency. 
    /// </summary>
    public class MeshGeometry3DToContours
    {
        private Face[] _faces { get; set; }
        private double _max;
        /// <summary>
        /// Gets the maximum height of a mesh.
        /// </summary>
        /// <value>The maximum.</value>
        public double Max { get { return _max; } }
        private double _min;
        /// <summary>
        /// Gets the minimum height of a mesh.
        /// </summary>
        /// <value>The minimum.</value>
        public double Min { get { return _min; } }
        /// <summary>
        /// Initializes a new instance of the <see cref="MeshGeometry3DToContours"/> class.
        /// </summary>
        /// <param name="mesh">The mesh.</param>
        public MeshGeometry3DToContours (MeshGeometry3D mesh)
        {
            this._min = double.PositiveInfinity;
            this._max = double.NegativeInfinity;
            this._faces = new Face[mesh.TriangleIndices.Count / 3];
            int i = 0, j = 0;
            while (i<mesh.TriangleIndices.Count)
            {
                this._faces[j] = new Face(mesh.Positions[mesh.TriangleIndices[i]],
                    mesh.Positions[mesh.TriangleIndices[i+1]],
                    mesh.Positions[mesh.TriangleIndices[i+2]]);
                this._min = Math.Min(this._faces[j].Min, this._min);
                this._max = Math.Max(this._faces[j].Max, this._max);
                i += 3;
                j++;
            }
        }
        /// <summary>
        /// Determines if a plane parallel to ground at the specified elevation intersects with the mesh
        /// </summary>
        /// <param name="elevation">The elevation.</param>
        /// <returns><c>true</c> if intersects, <c>false</c> otherwise.</returns>
        public bool Intersects(double elevation)
        {
            return (this._max - elevation) * (this._min - elevation) <= 0;
        }
        /// <summary>
        /// Gets the intersection of a plane at the specified height as a list of contours (i.e a list of 2D polygons).
        /// </summary>
        /// <param name="elevation">The elevation.</param>
        /// <returns>List&lt;BarrierPolygons&gt;.</returns>
        public List<BarrierPolygons> GetIntersection(double elevation)
        {
            var edges = new List<UVLine>();
            foreach (var item in this._faces)
            {
                if (item.Intersects(elevation))
                {
                    var edge = item.GetIntersection(elevation);
                    UV p1 = new UV(edge.Start.X, edge.Start.Y);
                    UV p2 = new UV(edge.End.X, edge.End.Y);
                    edges.Add(new UVLine(p1, p2));
                }
            }
            var plines = PLine.ExtractPLines(edges);
            List<BarrierPolygons> boundary = new List<BarrierPolygons>();
            foreach (PLine item in plines)
            {
                var oneBoundary = item.Simplify(0.001d,0.0001d);
                if (oneBoundary != null)
                {
                    var polygon = new BarrierPolygons(oneBoundary.ToArray()) { IsClosed = item.Closed };
                    boundary.Add(polygon);
                }
            }
            return boundary;
        }

        /// <summary>
        /// Gets the intersection contours as a list of 2D polygons.
        /// </summary>
        /// <param name="elevations">The elevations.</param>
        /// <returns>List&lt;BarrierPolygons&gt;.</returns>
        public List<BarrierPolygons> GetIntersection(IEnumerable<double> elevations)
        {
            List<BarrierPolygons> boundaries = new List<BarrierPolygons>();
            foreach (var item in elevations)
            {
                var boundary = this.GetIntersection(item);
                boundaries.AddRange(boundary);
            }
            return boundaries;
        }
    }
}

