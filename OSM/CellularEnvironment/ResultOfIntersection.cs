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
using SpatialAnalysis.Interoperability;

namespace SpatialAnalysis.CellularEnvironment
{
    /// <summary>
    /// Includes the results of ray intersection.
    /// </summary>
    public class RayIntersectionResult
    {
        /// <summary>
        /// Gets or sets the distance to the barrier.
        /// </summary>
        /// <value>The distance.</value>
        public double Distance { get; set; }
        /// <summary>
        /// Gets or sets the type of barrier.
        /// </summary>
        /// <value>The type.</value>
        public BarrierType Type { get; set; }
        /// <summary>
        /// Gets or sets the intersecting point.
        /// </summary>
        /// <value>The intersecting point.</value>
        public UV IntersectingPoint { get; set; }
        /// <summary>
        /// Gets or sets the index of the barrier from which the barrier can be retrieved.
        /// </summary>
        /// <value>The index of the barrier.</value>
        public int BarrierIndex { get; set; }
        /// <summary>
        /// Gets or sets the edge index in barrier.
        /// </summary>
        /// <value>The edge index in barrier.</value>
        public int EdgeIndexInBarrier { get; set; }
        /// <summary>
        /// Gets or sets the edge index in cellular floor.
        /// </summary>
        /// <value>The edge index in cellular floor.</value>
        public int EdgeIndexInCellularFloor { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="RayIntersectionResult"/> class.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="edgeIndexInCellularFloor">The edge index in cellular floor.</param>
        /// <param name="edgeGlobalAddress">The edge global address.</param>
        /// <param name="distance">The distance.</param>
        /// <param name="type">The Barrier type.</param>
        public RayIntersectionResult(UV point, int edgeIndexInCellularFloor, EdgeGlobalAddress edgeGlobalAddress, double distance, BarrierType type)
        {
            this.Type = type;
            this.Distance = distance;
            this.IntersectingPoint = point;
            this.BarrierIndex = edgeGlobalAddress.BarrierIndex;
            this.EdgeIndexInBarrier = edgeGlobalAddress.PointIndex;
            this.EdgeIndexInCellularFloor = edgeIndexInCellularFloor;
        }
        /// <summary>
        /// Visualizes the intersection in BIM environment.
        /// </summary>
        /// <param name="visualizer">The visualizer.</param>
        /// <param name="rayOrigin">The ray origin.</param>
        /// <param name="cellularFloor">The cellular floor.</param>
        /// <param name="elevation">The elevation.</param>
        /// <param name="pointSize">Size of the point.</param>
        public void Visualize(I_OSM_To_BIM visualizer, UV rayOrigin, CellularFloorBaseGeometry cellularFloor, double elevation, double pointSize = .3)
        {
            switch (this.Type)
            {
                case BarrierType.Visual:
                    //visualizer.VisualizeBoundary(cellularFloor.VisualBarriers[this.BarrierIndex].BoundaryPoints, elevation);
                    
                    visualizer.VisualizeLine(cellularFloor.VisualBarrierEdges[this.EdgeIndexInCellularFloor], elevation);
                    break;
                case BarrierType.Physical:
                    //visualizer.VisualizeBoundary(cellularFloor.PhysicalBarriers[this.BarrierIndex].BoundaryPoints, elevation);
                    
                    visualizer.VisualizeLine(cellularFloor.PhysicalBarrierEdges[this.EdgeIndexInCellularFloor], elevation);
                    break;
                case BarrierType.Field:
                    //visualizer.VisualizeBoundary(cellularFloor.PhysicalBarriers[this.BarrierIndex].BoundaryPoints, elevation);

                    visualizer.VisualizeLine(cellularFloor.FieldBarrierEdges[this.EdgeIndexInCellularFloor], elevation);
                    break;
                default:
                    break;
            }
            //visualizer.VisualizePoint(IntersectingPoint, pointSize, elevation);
            visualizer.VisualizeLine(new UVLine(rayOrigin, this.IntersectingPoint), elevation);
            
        }
    }
}

