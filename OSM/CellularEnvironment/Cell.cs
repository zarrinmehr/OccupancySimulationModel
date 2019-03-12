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
using SpatialAnalysis.Geometry;

namespace SpatialAnalysis.CellularEnvironment
{
    /// <summary>
    /// Represents a square space in between the grid lines on the floor 
    /// </summary>
    public class Cell: UV
    {
        /// <summary>
        /// Indices of the visual obstacle edges that pass through this cell
        /// </summary>
        public HashSet<int> VisualBarrierEdgeIndices { get; set; }
        /// <summary>
        /// Indices of the physical obstacle edges that pass through this cell
        /// </summary>
        public HashSet<int> PhysicalBarrierEdgeIndices { get; set; }
        /// <summary>
        /// Indices of the field edges that pass through this cell
        /// </summary>
        public HashSet<int> FieldBarrierEdgeIndices { get; set; }
        /// <summary>
        /// Indices of the field buffer edges that pass through this cell
        /// </summary>
        public HashSet<int> BarrierBufferEdgeIndices { get; set; }
        /// <summary>
        /// Outside indicates outside visual barriers
        /// </summary>
        public OverlapState VisualOverlapState { get; set; }
        /// <summary>
        /// Outside indicates outside physical barriers
        /// </summary>
        public OverlapState PhysicalOverlapState { get; set; }
        /// <summary>
        /// Inside indicates inside field
        /// </summary>
        public OverlapState FieldOverlapState { get; set; }
        /// <summary>
        /// Inside indicates inside field buffer
        /// </summary>
        public OverlapState BarrierBufferOverlapState { get; set; }
        private int _id;
        /// <summary>
        /// The unique ID of the cell 
        /// </summary>
        public int ID { get { return this._id; } }
        private readonly Index _cellIndex;
        /// <summary>
        /// The index of the cell
        /// Problem: cell is passed by reference and despite being readonly can have a changed value!
        /// FIX: make Index class a struct. This asks for many changes in inherited classes
        /// </summary>
        public Index CellToIndex { get { return _cellIndex; } }
        /// <summary>
        /// true if the cell contains an end point for a visual barrier edge
        /// </summary>
        public bool ContainsVisualEdgeEndPoints { get; set; }
        /// <summary>
        /// true if the cell contains an end point for a physical barrier edge
        /// </summary>
        public bool ContainsPhysicalEdgeEndPoints { get; set; }
        /// <summary>
        /// true if the cell contains an end point for a field barrier edge
        /// </summary>
        public bool ContainsFieldEdgeEndPoints { get; set; }
        /// <summary>
        /// true if the cell contains an end point for a barrier buffer edge
        /// </summary>
        public bool ContainsBufferEdgeEndPoints { get; set; }
        /// <summary>
        /// The constructor of the cell
        /// </summary>
        /// <param name="origin">Cell origin</param>
        /// <param name="id">Cell ID</param>
        /// <param name="i">Cell index width factor</param>
        /// <param name="j">Cell index height factor</param>
        public Cell(UV origin, int id, int i, int j):base(origin)
        {
            this._id = id;
            this.VisualBarrierEdgeIndices = new HashSet<int>();
            this.PhysicalBarrierEdgeIndices = new HashSet<int>();
            this.FieldBarrierEdgeIndices = new HashSet<int>();
            this._cellIndex = new Index(i, j);
            this.ContainsBufferEdgeEndPoints = this.ContainsFieldEdgeEndPoints =
                this.ContainsVisualEdgeEndPoints = this.ContainsPhysicalEdgeEndPoints = false;
        }


        public override int GetHashCode()
        {
            //return this._id;
            return base.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            Cell cell = obj as Cell;
            if (cell != null)
            {
                //return this.U == cell.U && this.V == cell.V;
                //return this._id == cell._id;
                return this.CellToIndex.I == cell.CellToIndex.I && this.CellToIndex.J == cell.CellToIndex.J;
            }
            return false;
        }
        /// <summary>
        /// Determines if the cell include a point
        /// </summary>
        /// <param name="p">Point</param>
        /// <param name="cellSize">Width of cell</param>
        /// <param name="cellHeight"></param>
        /// <returns></returns>
        public bool IncludePoint(UV p, double cellSize)
        {
            if (p.U >= this.U && p.U <= (this.U + cellSize))
            {
                if (p.V >= this.V && p.V <= (this.V + cellSize))
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// Visualize a cell in the BIM target platform
        /// </summary>
        /// <param name="visualizer">An instance of the IVisualize interface</param>
        /// <param name="size">Cell size</param>
        /// <param name="elevation">Elevation of visualization</param>
        public void Visualize(I_OSM_To_BIM visualizer, double size, double elevation)
        {
            UV[] pnts = new UV[4];
            pnts[0] = this;
            pnts[1] = this + UV.UBase * size;
            pnts[2] = this + UV.UBase * size + UV.VBase * size;
            pnts[3] = this + UV.VBase * size;
            visualizer.VisualizePolygon(pnts, elevation);
        }
        /// <summary>
        /// Cell Center Point
        /// </summary>
        /// <param name="cellSize">Cell size</param>
        /// <returns>Center of cell</returns>
        public UV GetCenter(double cellSize)
        {
            return this + new UV(cellSize / 2, cellSize / 2);
        }
        /// <summary>
        /// Converts a Cell to a collection of lines at its edges
        /// </summary>
        /// <returns>A line collection</returns>
        public HashSet<UVLine> ToUVLines(double size)
        {
            HashSet<UVLine> lines = new HashSet<UVLine>();
            UV x1 = this + new UV(0, size);
            UV x2 = this + new UV(size, size);
            UV x3 = this + new UV(size, 0);
            lines.Add(new UVLine(this, x1));
            lines.Add(new UVLine(x1, x2));
            lines.Add(new UVLine(x2, x3));
            lines.Add(new UVLine(x3, this));
            x1 = null; x2 = null; x3 = null;
            return lines;
        }

        public bool ContainsPoint(UV pnt, double cellSize)
        {
            bool widthIncluded = this.U <= pnt.U && pnt.U < (this.U + cellSize);
            bool heightIncluded = this.V <= pnt.V && pnt.V < (this.V + cellSize);
            return widthIncluded && heightIncluded;
        }
    }
}

