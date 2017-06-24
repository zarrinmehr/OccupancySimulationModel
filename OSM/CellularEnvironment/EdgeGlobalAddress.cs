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

namespace SpatialAnalysis.CellularEnvironment
{
    /// <summary>
    /// A data model that maps the barrier edges to the barrier polygons and the indices of their edges
    /// </summary>
    public class EdgeGlobalAddress
    {
        /// <summary>
        /// The index of the barrier in the cellular floor to which this edge belongs
        /// </summary>
        public int BarrierIndex { get; set; }
        /// <summary>
        /// The index of the start point of the edge in the barrier polygon
        /// </summary>
        public int PointIndex { get; set; }
        /// <summary>
        /// Creates an instance of EdgeGlobalAddress 
        /// </summary>
        /// <param name="barrierIdex">Barrier index</param>
        /// <param name="pointIndex">Point index</param>
        public EdgeGlobalAddress(int barrierIdex, int pointIndex)
        {
            this.BarrierIndex = barrierIdex;
            this.PointIndex = pointIndex;
        }
        public override int GetHashCode()
        {
            int hash = 7;
            hash = 71 * hash + this.BarrierIndex.GetHashCode();
            hash = 71 * hash + this.PointIndex.GetHashCode();
            return hash;
        }
        public override bool Equals(object obj)
        {
            EdgeGlobalAddress pa = obj as EdgeGlobalAddress;
            if (pa != null)
            {
                return pa.PointIndex == this.PointIndex && pa.BarrierIndex == this.BarrierIndex;
            }
            return false;
        }

    }
}


