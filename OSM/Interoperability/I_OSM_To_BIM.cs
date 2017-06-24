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

namespace SpatialAnalysis.Interoperability
{
    /// <summary>
    /// This interface allows for visualization and selection in the BIM environment
    /// </summary>
    public interface I_OSM_To_BIM
    {
        /// <summary>
        /// Visualizes a polygon in BIM environment.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="elevation">The elevation.</param>
        void VisualizePolygon(UV[] points, double elevation);
        /// <summary>
        /// Visualizes a line in BIM environment
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="elevation">The elevation.</param>
        void VisualizeLine(UVLine line, double elevation);
        /// <summary>
        /// Visualizes a point in BIM environment
        /// </summary>
        /// <param name="pnt">The point.</param>
        /// <param name="size">The size.</param>
        /// <param name="elevation">The elevation.</param>
        void VisualizePoint(UV pnt, double size, double elevation);
        /// <summary>
        /// Visualizes a collection of lines in BIM environment
        /// </summary>
        /// <param name="lines">The lines.</param>
        /// <param name="elevation">The elevation.</param>
        void VisualizeLines(ICollection<UVLine> lines, double elevation);
        /// <summary>
        /// Picks the point.
        /// </summary>
        /// <param name="message">The message to pass to BIM.</param>
        /// <returns>UV.</returns>
        UV PickPoint(string message);
    }
}

