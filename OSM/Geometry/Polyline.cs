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
using System.Windows;
using SpatialAnalysis.Miscellaneous;

namespace SpatialAnalysis.Geometry
{

    /// <summary>
    /// Class PLine.
    /// </summary>
    public class PLine
    {
        private UVComparer _comparer { get; set; }
        /// <summary>
        /// Gets the point count.
        /// </summary>
        /// <value>The point count.</value>
        public int PointCount { get { return this._pntList.Count; } }
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="PLine"/> is closed.
        /// </summary>
        /// <value><c>true</c> if closed; otherwise, <c>false</c>.</value>
        public bool Closed { get; set; }
        private HashSet<UV> _pntSet { get; set; }
        private List<UV> _pntList;
        /// <summary>
        /// Gets the points.
        /// </summary>
        /// <value>The points.</value>
        public List<UV> Points
        {
            get { return this._pntList; }
        }
        private UV _start;
        /// <summary>
        /// Gets the start point of the polygon.
        /// </summary>
        /// <value>The start.</value>
        public UV Start
        {
            get { return this._start; }
        }
        private UV _end;
        /// <summary>
        /// Gets the end point of the polygon.
        /// </summary>
        /// <value>The end.</value>
        public UV End
        {
            get { return this._end; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PLine"/> class.
        /// </summary>
        /// <param name="line">A line to start with.</param>
        /// <param name="numberOfFractionalDigits">The number of fractional digits used as the tolerance of equality.</param>
        /// <exception cref="ArgumentException">Line length is zero</exception>
        public PLine(UVLine line, int numberOfFractionalDigits = 5, double tolerance = OSMDocument.AbsoluteTolerance)
        {
            if (line.End == line.Start)
            {
                throw new ArgumentException("Line length is zero");
            }
            this._end = line.End;
            this._start = line.Start;
            this._pntList = new List<UV>() 
            {
                this._start,
                this._end,
            };
            this._comparer = new UVComparer(numberOfFractionalDigits, tolerance);
            this._pntSet = new HashSet<UV>(this._comparer)
            {
                this._end,
                this._start
            };
            this.Closed = false;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="PLine"/> class.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="closed">if set to <c>true</c> [closed].</param>
        /// <param name="numberOfFractionalDigits">The number of fractional digits used as the tolerance of equality.</param>
        /// <exception cref="ArgumentException">Points are duplicated</exception>
        public PLine(List<UV> points, bool closed, int numberOfFractionalDigits = 5, double tolerance = OSMDocument.AbsoluteTolerance)
        {
            this._end = points[points.Count - 1];
            this._start = points[0];
            this._pntList = points;
            this._comparer = new UVComparer(numberOfFractionalDigits, tolerance);
            this._pntSet = new HashSet<UV>(this._comparer);
            foreach (var item in this._pntList)
            {
                if (this._pntSet.Contains(item))
                {
                    throw new ArgumentException("Points are duplicated");
                }
                else
                {
                    this._pntSet.Add(item);
                }
            }
            this.Closed = closed;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="PLine"/> class. This method does not include any rounding to test equality!
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="closed">if set to <c>true</c> the polyline is closed.</param>
        /// <exception cref="ArgumentException">Points are duplicated</exception>
        //public PLine(List<UV> points, bool closed)
        //{
        //    this._end = points[points.Count - 1];
        //    this._start = points[0];
        //    this._pntList = points;
        //    this._pntSet = new HashSet<UV>();//(new UVComparer(fractionalDigits));
        //    foreach (var item in this._pntList)
        //    {
        //        if (this._pntSet.Contains(item))
        //        {
        //            throw new ArgumentException("Points are duplicated");
        //        }
        //        else
        //        {
        //            this._pntSet.Add(item);
        //        }
        //    }
        //    this.Closed = closed;
        //}
        /// <summary>
        /// Determines whether this instance is convex.
        /// </summary>
        /// <returns><c>true</c> if this instance is convex; otherwise, <c>false</c>.</returns>
        public bool IsConvex()
        {
            return BarrierPolygon.IsConvex(this._pntList.ToArray());
        }
        /// <summary>
        /// Splits the polygon to a list of points at given distances.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <returns>List&lt;UV&gt;.</returns>
        public List<UV> SplitToPoints(double length)
        {
            List<UV> pnts = new List<UV>();
            pnts.Add(this.Start);
            double totalLength = 0;
            double u = 0;
            for (int i = 0; i < this.PointCount; i++)
            {
                int j = (i == this.PointCount - 1) ? 0 : i + 1;
                if (!this.Closed && j == 0)
                {
                    break;
                }
                UV vector = this.Points[j] - this.Points[i];
                double dist = vector.GetLength();
                vector /= dist;
                while (u < totalLength + dist)
                {
                    pnts.Add(this.Points[i] + (u - totalLength) * vector);
                    u += length;
                }
                totalLength += dist;
            }
            if (this.End != pnts[pnts.Count - 1] && !this.Closed)
            {
                pnts.Add(this.End);
            }

            UV current = pnts[0];
            List<UV> pnts2 = new List<UV>() { current };
            for (int i = 0; i < pnts.Count; i++)
            {
                if (pnts[i] != current)
                {
                    current = pnts[i];
                    pnts2.Add(current);
                }
            }
            pnts.Clear();
            pnts = null;
            return pnts2;
        }
        /// <summary>
        /// Splits this polyline to a list of polylines with equal lengths
        /// </summary>
        /// <param name="length">The length.</param>
        /// <param name="tolerance">The tolerance by default set to main documents absolute tolerance.</param>
        /// <returns>List&lt;PLine&gt;.</returns>
        public List<PLine> SplitToPLines(double length, double tolerance = OSMDocument.AbsoluteTolerance)
        {
            List<int> indexes = new List<int>() { 0 };
            List<UV> pnts = new List<UV>();
            double totalLength = 0;
            double u = length;
            for (int i = 0; i < this.PointCount; i++)
            {
                pnts.Add(this.Points[i]);
                //indexes.Add(pnts.Count - 1);
                int j = (i == this.PointCount - 1) ? 0 : i + 1;
                if (!this.Closed && j == 0)
                {
                    break;
                }
                UV vector = this.Points[j] - this.Points[i];
                double dist = vector.GetLength();
                vector /= dist;
                while (u < totalLength + dist)
                {
                    var p = this.Points[i] + (u - totalLength) * vector;
                    if (!this._pntSet.Contains(p))
                    {
                        pnts.Add(p);
                    }
                    indexes.Add(pnts.Count - 1);
                    u += length;
                }
                totalLength += dist;
            }
            if (this.Closed)
            {
                if (this.Start != pnts[pnts.Count - 1])
                {
                    pnts.Add(this.Start);
                }
                indexes.Add(pnts.Count - 1);
            }
            else
            {
                if (this.End != pnts[pnts.Count - 1])
                {
                    pnts.Add(this.End);
                }
                indexes.Add(pnts.Count - 1);
            }
            List<PLine> plines = new List<PLine>();
            if (indexes.Count == 2)
            {
                plines.Add(this);
            }
            for (int i = 0; i < indexes.Count - 1; i++)
            {
                var points = pnts.GetRange(indexes[i], indexes[i + 1] - indexes[i] + 1);
                if (points[0].DistanceTo(points[1]) < tolerance)
                {
                    points.RemoveAt(1);
                }
                if (points.Count > 1)
                {
                    if (points[points.Count - 1].DistanceTo(points[points.Count - 2]) < tolerance)
                    {
                        points.RemoveAt(points.Count - 2);
                    }
                    if (points.Count > 1)
                    {
                        PLine pline = new PLine(points, false);
                        plines.Add(pline);
                    }
                }
            }
            return plines;
        }
        /// <summary>
        /// Gets the length of this polygon.
        /// </summary>
        /// <returns>System.Double.</returns>
        public double GetLength()
        {
            double length = 0;
            for (int i = 0; i < this.Points.Count - 1; i++)
            {
                length += UV.GetDistanceBetween(this.Points[i], this.Points[i + 1]);
            }
            if (this.Closed)
            {
                length += UV.GetDistanceBetween(this.Start, this.End);
            }
            return length;
        }

        private void addStart(UV pnt)
        {
            this._start = pnt;
            this._pntList.Insert(0, pnt);
            this._pntSet.Add(pnt);
        }
        private void addEnd(UV pnt)
        {
            this._end = pnt;
            this._pntList.Add(pnt);
            this._pntSet.Add(pnt);
        }
        private bool addSegment(UVLine line)
        {
            try
            {
                if (this.Closed)
                {
                    return false;
                }
                else
                {
                    if (this._pntSet.Contains(line.Start) && this._pntSet.Contains(line.End))
                    {
                        if (this._comparer.Equals(this._start, line.Start) && this._comparer.Equals(this._end, line.End))
                        {
                            this.Closed = true;
                            return true;
                        }
                        else if (this._comparer.Equals(this._start, line.End) && this._comparer.Equals(this._end, line.Start))
                        {
                            this.Closed = true;
                            return true;
                        }
                        else
                        {
                            /*
                            StringBuilder sb = new StringBuilder();
                            sb.AppendLine("POINTS:");
                            foreach (var item in this._pntList)
                            {
                                sb.AppendLine(item.ToString());
                            }
                            sb.AppendLine("\nAdded Line:");
                            sb.AppendLine(line.Start.ToString());
                            sb.AppendLine(line.End.ToString());
                            MessageBox.Show(sb.ToString());
                            sb.Clear();
                            sb = null;
                            throw new ArgumentException("Self intersection");
                            */
                        }
                    }

                    if (this._comparer.Equals(this._start, line.Start))

                    {
                        this.addStart(line.End);
                        return true;
                    }
                    if (this._comparer.Equals(this._start, line.End))
                    {
                        this.addStart(line.Start);
                        return true;
                    }
                    if (this._comparer.Equals(this._end, line.End))
                    {
                        this.addEnd(line.Start);
                        return true;
                    }
                    if (this._comparer.Equals(this._end, line.Start))
                    {
                        this.addEnd(line.End);
                        return true;
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Report());
            }
            return false;
        }
        /// <summary>
        /// Visualizes this polygon at the BIM environment
        /// </summary>
        /// <param name="visualizer">The visualizer.</param>
        /// <param name="elevation">The elevation.</param>
        public void Visualize(I_OSM_To_BIM visualizer, double elevation = 0)
        {
            List<UVLine> lines = new List<UVLine>();
            for (int i = 0; i < this.PointCount - 1; i++)
            {
                lines.Add(new UVLine(this._pntList[i], this._pntList[i + 1]));
            }
            if (this.Closed)
            {
                lines.Add(new UVLine(this._end, this._start));
            }
            visualizer.VisualizeLines(lines, elevation);
        }
        /// <summary>
        /// Converts this polyline to a list of lines.
        /// </summary>
        /// <returns>List&lt;UVLine&gt;.</returns>
        public List<UVLine> ToLines()
        {
            List<UVLine> lines = new List<UVLine>();
            for (int i = 0; i < this.PointCount - 1; i++)
            {
                lines.Add(new UVLine(this._pntList[i], this._pntList[i + 1]));
            }
            if (this.Closed)
            {
                lines.Add(new UVLine(this._pntList[this.PointCount - 1], this._pntList[0]));
            }
            return lines;
        }
        /// <summary>
        /// Copies this instance deeply.
        /// </summary>
        /// <returns>PLine.</returns>
        public PLine Copy()
        {
            PLine pline = new PLine(new UVLine(this._pntList[0].Copy(), this._pntList[1].Copy()));
            for (int i = 2; i < this.PointCount; i++)
            {
                pline.addEnd(this._pntList[i].Copy());
            }
            pline.Closed = this.Closed;
            return pline;
        }
        /// <summary>
        /// Simplifies this polygon.
        /// </summary>
        /// <param name="distance">The distance tolerance.</param>
        /// <param name="angle">The angle tolerance.</param>
        /// <returns>List&lt;UV&gt;.</returns>
        /// <exception cref="ArgumentException">Heap cannot remove the found end point</exception>
        public List<UV> Simplify(double distance, double angle = .0001)
        {
            double distanceTolerance = distance * distance;
            double sine = Math.Sin(angle * Math.PI / 180);
            double angleTolerance = sine * sine;
            SortedSet<PLNode> heap = new SortedSet<PLNode>(new PLNodeComparer(distanceTolerance));
            PLNode[] allNodes = new PLNode[this.PointCount];
            for (int i = 0; i < allNodes.Length; i++)
            {
                allNodes[i] = new PLNode(this._pntList[i]);
            }
            if (this.Closed)
            {
                for (int i = 0; i < allNodes.Length; i++)
                {
                    int i0 = (i == 0) ? allNodes.Length - 1 : i - 1;
                    int i1 = (i == allNodes.Length - 1) ? 0 : i + 1;
                    allNodes[i].AddConnection(allNodes[i0]);
                    allNodes[i].AddConnection(allNodes[i1]);
                    allNodes[i].LoadFactors();
                    if (heap.Contains(allNodes[i]))
                    {
                        allNodes[i].Remained = false;
                        // this sucks. Although the polyline does not have equal points, there are 
                        // points that are so close that their distance squared is zero! Damn it.
                        // this meains the heap should be reconstructed because removing one node 
                        // breaks the connection! I have to make sure that each node is connected 
                        // to the neighbors that exist in the heap
                    }
                    else
                    {
                        heap.Add(allNodes[i]);
                    }
                }
                if (heap.Count < this.PointCount) // reconstructing the heap
                {
                    PLNode[] newNodes = new PLNode[heap.Count];
                    int k = 0;
                    for (int i = 0; i < this.PointCount; i++)
                    {
                        if (allNodes[i].Remained)
                        {
                            newNodes[k] = new PLNode(allNodes[i].Point);
                            k++;
                        }
                    }
                    heap.Clear();
                    allNodes = newNodes;
                    for (int i = 0; i < allNodes.Length; i++)
                    {
                        int i0 = (i == 0) ? allNodes.Length - 1 : i - 1;
                        int i1 = (i == allNodes.Length - 1) ? 0 : i + 1;
                        allNodes[i].AddConnection(allNodes[i0]);
                        allNodes[i].AddConnection(allNodes[i1]);
                        allNodes[i].LoadFactors();
                        heap.Add(allNodes[i]);
                    }
                }
            }
            else
            {
                for (int i = 0; i < allNodes.Length; i++)
                {
                    if (i > 0)
                    {
                        allNodes[i].AddConnection(allNodes[i - 1]);
                    }
                    if (i < allNodes.Length - 1)
                    {
                        allNodes[i].AddConnection(allNodes[i + 1]);
                    }
                    allNodes[i].LoadFactors();
                    if (heap.Contains(allNodes[i]))
                    {
                        allNodes[i].Remained = false;
                        // this sucks. Although the polyline does not have equal points, there are 
                        // points that are so close that their distance squared is zero! Damn it.
                        // this meains the heap should be reconstructed because removing one node 
                        // breaks the connection! I have to make sure that each node is connected 
                        // to the neighbors that exist in the heap
                    }
                    else
                    {
                        heap.Add(allNodes[i]);
                    }
                }
                if (heap.Count < this.PointCount) // reconstructing the heap
                {
                    PLNode[] newNodes = new PLNode[heap.Count];
                    int k = 0;
                    for (int i = 0; i < this.PointCount; i++)
                    {
                        if (allNodes[i].Remained)
                        {
                            newNodes[k] = new PLNode(allNodes[i].Point);
                            k++;
                        }
                    }
                    heap.Clear();
                    allNodes = newNodes;
                    for (int i = 0; i < allNodes.Length; i++)
                    {
                        if (i > 0)
                        {
                            allNodes[i].AddConnection(allNodes[i - 1]);
                        }
                        if (i < allNodes.Length - 1)
                        {
                            allNodes[i].AddConnection(allNodes[i + 1]);
                        }
                        allNodes[i].LoadFactors();
                        if (heap.Contains(allNodes[i]))
                        {
                            allNodes[i].Remained = false;
                        }
                        else
                        {
                            heap.Add(allNodes[i]);
                        }
                    }
                }

            }
            // purging
            var min = heap.Min;
            while (!min.IsSignificant(angleTolerance, distanceTolerance))
            {
                heap.Remove(min);
                if (min.Connections.Count == 1)
                {
                    throw new ArgumentException("Heap cannot remove the found end point");
                }
                PLNode node1 = min.Connections.First();
                min.Connections.Remove(node1);
                PLNode node2 = min.Connections.First();
                min.Connections.Remove(node2);
                heap.Remove(node1);
                heap.Remove(node2);
                node1.Connections.Remove(min);
                node2.Connections.Remove(min);
                node1.AddConnection(node2);
                node2.AddConnection(node1);
                node1.LoadFactors();
                heap.Add(node1);
                node2.LoadFactors();
                heap.Add(node2);
                min.Remained = false;
                min = heap.Min;
            }
            List<UV> purged = new List<UV>();
            for (int i = 0; i < allNodes.Length; i++)
            {
                if (allNodes[i].Remained)
                {
                    purged.Add(allNodes[i].Point);
                }
            }
            return purged;
        }
        /// <summary>
        /// Extracts a list of polylines from an arbitrary collection of lines. NOTE: There might be different possibilities for this operation.
        /// </summary>
        /// <param name="lines">The lines.</param>
        /// <param name="fractionalDigits">The fractional digits used for hashing.</param>
        /// <param name="tolerance">The tolerance of point equality.</param>
        /// <returns>List&lt;PLine&gt;.</returns>
        public static List<PLine> ExtractPLines(ICollection<UVLine> lines, int fractionalDigits = 5, double tolerance = OSMDocument.AbsoluteTolerance)
        {
            Dictionary<UV, HashSet<UVLine>> pntToLine = new Dictionary<UV, HashSet<UVLine>>(new UVComparer(fractionalDigits, tolerance));
            foreach (var item in lines)
            {
                if (item.GetLengthSquared() > 0)
                {
                    if (pntToLine.ContainsKey(item.Start))
                    {
                        pntToLine[item.Start].Add(item);
                    }
                    else
                    {
                        var set = new HashSet<UVLine>() { item };
                        pntToLine.Add(item.Start, set);
                    }
                    if (pntToLine.ContainsKey(item.End))
                    {
                        pntToLine[item.End].Add(item);
                    }
                    else
                    {
                        var list = new HashSet<UVLine>() { item };
                        pntToLine.Add(item.End, list);
                    }
                }
                else
                {
                    MessageBox.Show("Line with zero length cannot be added to a polyLine!");
                }
            }
            List<PLine> pLines = new List<PLine>();
            while (pntToLine.Count != 0)
            {
                UVLine line = null;
                try
                {
                    line = pntToLine.First().Value.First();
                }
                catch (Exception e)
                {
                    MessageBox.Show("Failed at 3\n" + e.Report());
                }
                PLine pLine = new PLine(line, fractionalDigits);
                //lines.Remove(line);
                pntToLine[line.Start].Remove(line);
                if (pntToLine[line.Start].Count == 0)
                {
                    pntToLine.Remove(line.Start);
                }
                pntToLine[line.End].Remove(line);
                if (pntToLine[line.End].Count == 0)
                {
                    pntToLine.Remove(line.End);
                }
                bool iterate = true;
                while (iterate)
                {
                    iterate = false;
                    if (pntToLine.ContainsKey(pLine._start))
                    {
                        UVLine line_ = null;
                        try
                        {
                            line_ = pntToLine[pLine._start].First();
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show("Failed at 1\n" + e.Report());
                        }
                        pLine.addSegment(line_);
                        pntToLine[line_.Start].Remove(line_);
                        if (pntToLine[line_.Start].Count == 0)
                        {
                            pntToLine.Remove(line_.Start);
                        }
                        pntToLine[line_.End].Remove(line_);
                        if (pntToLine[line_.End].Count == 0)
                        {
                            pntToLine.Remove(line_.End);
                        }
                        //lines.Remove(line);
                        if (pLine.Closed)
                        {
                            break;
                        }
                        iterate = true;
                    }
                    if (pntToLine.Count == 0) break;
                    if (pntToLine.ContainsKey(pLine._end))
                    {
                        UVLine line_ = null;
                        try
                        {
                            line_ = pntToLine[pLine._end].First();
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show("Failed at 2\n" + e.Report());
                            try
                            {
                                line_ = pntToLine[pLine._end].First();
                            }
                            catch (Exception)
                            {
                            }
                        }
                        pLine.addSegment(line_);
                        pntToLine[line_.Start].Remove(line_);
                        if (pntToLine[line_.Start].Count == 0)
                        {
                            pntToLine.Remove(line_.Start);
                        }
                        pntToLine[line_.End].Remove(line_);
                        if (pntToLine[line_.End].Count == 0)
                        {
                            pntToLine.Remove(line_.End);
                        }
                        //lines.Remove(line);
                        if (pLine.Closed)
                        {
                            break;
                        }
                        iterate = true;
                    }
                }
                pLines.Add(pLine);
            }
            return pLines;
        }
        /// <summary>
        /// Finds the closest point on the polygon from a given point
        /// </summary>
        /// <param name="points">The points that represent a polygon.</param>
        /// <param name="p">The point from which the closest point should be found.</param>
        /// <param name="closed">if set to <c>true</c> the polygon is closed.</param>
        /// <returns>UV.</returns>
        public static UV ClosestPoint(List<UV> points, UV p, bool closed)
        {
            UV[] vecs = new UV[points.Count];
            double[] vecLengths = new double[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                vecs[i] = points[i] - p;
                vecLengths[i] = vecs[i].GetLengthSquared();
            }
            double minDistSquared = double.PositiveInfinity;
            int index = 0;
            for (int i = 0; i < points.Count - 1; i++)
            {
                double area = vecs[i].CrossProductValue(vecs[i + 1]);
                area *= area;
                double d1 = UV.GetLengthSquared(points[i], points[i + 1]);
                double distSquared = area / d1;
                if (distSquared < vecLengths[i] - d1 || distSquared < vecLengths[i + 1] - d1)
                {
                    distSquared = Math.Min(vecLengths[i + 1], vecLengths[i]);
                }
                if (minDistSquared > distSquared)
                {
                    minDistSquared = distSquared;
                    index = i;
                }
            }
            if (closed)
            {
                double area = vecs[points.Count - 1].CrossProductValue(vecs[0]);
                area *= area;
                double d1 = UV.GetLengthSquared(points[points.Count - 1], points[0]);
                double distSquared = area / d1;
                if (distSquared < vecLengths[0] - d1 || distSquared < vecLengths[points.Count - 1] - d1)
                {
                    distSquared = Math.Min(vecLengths[0], vecLengths[points.Count - 1]);
                }
                if (minDistSquared > distSquared)
                {
                    minDistSquared = distSquared;
                    index = points.Count - 1;
                }
            }
            int j = index + 1;
            if (j == points.Count)
                j = 0;
            UVLine line = new UVLine(points[index], points[j]);
            return p.GetClosestPoint(line);
        }
        /// <summary>
        /// Finds the closest point on the polygon from a given point
        /// </summary>
        /// <param name="points">The points that represent a polygon.</param>
        /// <param name="p">The point from which the closest point should be found.</param>
        /// <param name="closed">if set to <c>true</c> the polygon is closed.</param>
        /// <returns>UV.</returns>
        public static UV ClosestPoint(UV[] points, UV p, bool closed)
        {
            UV[] vecs = new UV[points.Length];
            double[] vecLengths = new double[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                vecs[i] = points[i] - p;
                vecLengths[i] = vecs[i].GetLengthSquared();
            }
            double minDistSquared = double.PositiveInfinity;
            int index = 0;
            for (int i = 0; i < points.Length - 1; i++)
            {
                double area = vecs[i].CrossProductValue(vecs[i + 1]);
                area *= area;
                double d1 = UV.GetLengthSquared(points[i], points[i + 1]);
                double distSquared = area / d1;
                if (distSquared < vecLengths[i] - d1 || distSquared < vecLengths[i + 1] - d1)
                {
                    distSquared = Math.Min(vecLengths[i + 1], vecLengths[i]);
                }
                if (minDistSquared > distSquared)
                {
                    minDistSquared = distSquared;
                    index = i;
                }
            }
            if (closed)
            {
                double area = vecs[points.Length - 1].CrossProductValue(vecs[0]);
                area *= area;
                double d1 = UV.GetLengthSquared(points[points.Length - 1], points[0]);
                double distSquared = area / d1;
                if (distSquared < vecLengths[0] - d1 || distSquared < vecLengths[points.Length - 1] - d1)
                {
                    distSquared = Math.Min(vecLengths[0], vecLengths[points.Length - 1]);
                }
                if (minDistSquared > distSquared)
                {
                    minDistSquared = distSquared;
                    index = points.Length - 1;
                }
            }
            int j = index + 1;
            if (j == points.Length)
                j = 0;
            UVLine line = new UVLine(points[index], points[j]);
            return p.GetClosestPoint(line);
        }
        /// <summary>
        /// Returns the distance squared to a list of points that represent a polygon
        /// </summary>
        /// <param name="pLinePoints">A list of points that represent a polygon.</param>
        /// <param name="p">The point to measure the distance squared from.</param>
        /// <param name="closed">if set to <c>true</c> the polygon is closed.</param>
        /// <returns>System.Double.</returns>
        public static double DistanceSquaredTo(List<UV> pLinePoints, UV p, bool closed)
        {
            UV[] vecs = new UV[pLinePoints.Count];
            double[] vecLengths = new double[pLinePoints.Count];
            for (int i = 0; i < pLinePoints.Count; i++)
            {
                vecs[i] = pLinePoints[i] - p;
                vecLengths[i] = vecs[i].GetLengthSquared();
            }
            double minDistSquared = double.PositiveInfinity;
            int index = 0;
            for (int i = 0; i < pLinePoints.Count - 1; i++)
            {
                double area = vecs[i].CrossProductValue(vecs[i + 1]);
                area *= area;
                double d1 = UV.GetLengthSquared(pLinePoints[i], pLinePoints[i + 1]);
                double distSquared = area / d1;
                if (distSquared < vecLengths[i] - d1 || distSquared < vecLengths[i + 1] - d1)
                {
                    distSquared = Math.Min(vecLengths[i + 1], vecLengths[i]);
                }
                if (minDistSquared > distSquared)
                {
                    minDistSquared = distSquared;
                    index = i;
                }
            }
            if (closed)
            {
                double area = vecs[pLinePoints.Count - 1].CrossProductValue(vecs[0]);
                area *= area;
                double d1 = UV.GetLengthSquared(pLinePoints[pLinePoints.Count - 1], pLinePoints[0]);
                double distSquared = area / d1;
                if (distSquared < vecLengths[0] - d1 || distSquared < vecLengths[pLinePoints.Count - 1] - d1)
                {
                    distSquared = Math.Min(vecLengths[0], vecLengths[pLinePoints.Count - 1]);
                }
                if (minDistSquared > distSquared)
                {
                    minDistSquared = distSquared;
                    index = pLinePoints.Count - 1;
                }
            }
            return minDistSquared;
        }
        /// <summary>
        /// Returns the distance squared to a list of points that represent a polygon
        /// </summary>
        /// <param name="pLinePoints">A list of points that represent a polygon.</param>
        /// <param name="p">The point to measure the distance squared from.</param>
        /// <param name="closed">if set to <c>true</c> the polygon is closed.</param>
        /// <returns>System.Double.</returns>
        public static double DistanceSquaredTo(UV[] pLinePoints, UV p, bool closed)
        {
            UV[] vecs = new UV[pLinePoints.Length];
            double[] vecLengths = new double[pLinePoints.Length];
            for (int i = 0; i < pLinePoints.Length; i++)
            {
                vecs[i] = pLinePoints[i] - p;
                vecLengths[i] = vecs[i].GetLengthSquared();
            }
            double minDistSquared = double.PositiveInfinity;
            int index = 0;
            for (int i = 0; i < pLinePoints.Length - 1; i++)
            {
                double area = vecs[i].CrossProductValue(vecs[i + 1]);
                area *= area;
                double d1 = UV.GetLengthSquared(pLinePoints[i], pLinePoints[i + 1]);
                double distSquared = area / d1;
                if (distSquared < vecLengths[i] - d1 || distSquared < vecLengths[i + 1] - d1)
                {
                    distSquared = Math.Min(vecLengths[i + 1], vecLengths[i]);
                }
                if (minDistSquared > distSquared)
                {
                    minDistSquared = distSquared;
                    index = i;
                }
            }
            if (closed)
            {
                double area = vecs[pLinePoints.Length - 1].CrossProductValue(vecs[0]);
                area *= area;
                double d1 = UV.GetLengthSquared(pLinePoints[pLinePoints.Length - 1], pLinePoints[0]);
                double distSquared = area / d1;
                if (distSquared < vecLengths[0] - d1 || distSquared < vecLengths[pLinePoints.Length - 1] - d1)
                {
                    distSquared = Math.Min(vecLengths[0], vecLengths[pLinePoints.Length - 1]);
                }
                if (minDistSquared > distSquared)
                {
                    minDistSquared = distSquared;
                    index = pLinePoints.Length - 1;
                }
            }
            return minDistSquared;
        }
        /// <summary>
        /// Returns the distance squared to a this polygon
        /// </summary>
        /// <param name="p">The point to measure the distance squared from.</param>
        /// <returns>System.Double.</returns>
        public double DistanceSquaredTo(UV p)
        {
            return PLine.DistanceSquaredTo(this.Points, p, this.Closed);
        }
        /// <summary>
        /// Finds the closest point on the polygon from a given point
        /// </summary>
        /// <param name="p">The point from which the closest point should be found.</param>
        /// <returns>UV.</returns>
        public UV ClosestPoint(UV p)
        {
            return PLine.ClosestPoint(this.Points, p, this.Closed);
        }
    }
    /// <summary>
    /// Compares UV values and tests their equality with adjustable level of precision. 
    /// </summary>
    /// <seealso cref="System.Collections.Generic.IEqualityComparer{SpatialAnalysis.Geometry.UV}" />
    internal class UVComparer : IEqualityComparer<UV>
    {
        private int _fractionalDigits;
        /// <summary>
        /// Gets the fractional digits used for hashing.
        /// </summary>
        /// <value>The fractional digits.</value>
        public int FractionalDigits
        {
            get { return _fractionalDigits; }
        }
        private double _tolerance;

        /// <summary>
        /// Gets the tolerance used for testing equality.
        /// </summary>
        /// <value>The tolerance.</value>
        public double Tolerance
        {
            get { return _tolerance; }
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="UVComparer"/> class.
        /// </summary>
        /// <param name="fractionalDigits">The fractional digits for hashing.</param>
        /// <param name="tolerance">The tolerance to equality.</param>
        public UVComparer(int fractionalDigits, double tolerance)
        {
            this._tolerance = tolerance;
            this._fractionalDigits = fractionalDigits;
        }
        public bool Equals(UV x, UV y)
        {
            double deltaX = x.U-y.U;
            double deltaY = x.V - y.V;
            if (Math.Abs(deltaX)<this._tolerance && Math.Abs(deltaY)<this._tolerance)
            {
                return true;
            }
            return false;
        }
        public int GetHashCode(UV obj)
        {
            int hash = 7;
            hash = 71 * hash + Math.Round(obj.U,this._fractionalDigits).GetHashCode();
            hash = 71 * hash + Math.Round(obj.V, this._fractionalDigits).GetHashCode();
            return hash;
        }
    }

    internal class PLNode
    {
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="PLNode"/> is remained.
        /// </summary>
        /// <value><c>true</c> if remained; otherwise, <c>false</c>.</value>
        public bool Remained { get; set; }
        /// <summary>
        /// Gets or sets the connections.
        /// </summary>
        /// <value>The connections.</value>
        public HashSet<PLNode> Connections { get; set; }
        /// <summary>
        /// Gets or sets the point.
        /// </summary>
        /// <value>The point.</value>
        public UV Point { get; set; }
        /// <summary>
        /// Cosine squared of the angle between two edges
        /// </summary>
        public double AngleFacotr { get; set; }
        /// <summary>
        /// Minimum distance between the previous and next points
        /// </summary>
        public double DistanceFactor { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="PLNode"/> class.
        /// </summary>
        /// <param name="point">The point.</param>
        public PLNode(UV point)
        {
            this.Point = point;
            this.Connections = new HashSet<PLNode>();
            this.Remained = true;
        }
        /// <summary>
        /// Adds the connection.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <exception cref="ArgumentException">Each node cannot be connected to more than 2 points</exception>
        public void AddConnection(PLNode node)
        {
            this.Connections.Add(node);
            if (this.Connections.Count > 2)
            {
                throw new ArgumentException("Each node cannot be connected to more than 2 points");
            }
        }
        /// <summary>
        /// Loads the factors.
        /// </summary>
        public void LoadFactors()
        {
            UV v1 = new UV();
            UV v2 = new UV();
            int i = 0;
            foreach (var item in this.Connections)
            {
                if (i == 2)
                    break;
                else if (i == 0)
                {
                    v1 = this.Point - item.Point;
                }
                if (i == 1)
                {
                    v2 = this.Point - item.Point;
                }
                i++;
            }
            if (i < 2)
            {
                this.AngleFacotr = double.PositiveInfinity;
                this.DistanceFactor = double.PositiveInfinity;
            }
            else
            {
                double d1 = v1.GetLengthSquared();
                double d2 = v2.GetLengthSquared();
                this.DistanceFactor = Math.Min(d1, d2);
                double crossProductValue = v1.CrossProductValue(v2);
                this.AngleFacotr = crossProductValue * crossProductValue / (d1 * d2);
            }
        }
        public override bool Equals(object obj)
        {
            PLNode node = (PLNode)obj;
            if (node == null)
            {
                return false;
            }
            return this.Point.Equals(node.Point);
        }
        public override int GetHashCode()
        {
            return this.Point.GetHashCode();
        }
        public override string ToString()
        {
            return this.Point.ToString();
        }

        /// <summary>
        /// Determines whether the specified angle tolerance is significant.
        /// </summary>
        /// <param name="angleTolerance">Angle tolerance is cos(angle)^2 *sign(cos(angle)</param>
        /// <param name="distanceTolerance">Distance tolerance is distance^2</param>
        /// <returns><c>true</c> if the specified angle tolerance is significant; otherwise, <c>false</c>.</returns>
        public bool IsSignificant(double angleTolerance, double distanceTolerance)
        {
            return this.DistanceFactor > distanceTolerance && this.AngleFacotr > angleTolerance;
        }
    }
    internal class PLNodeComparer : IComparer<PLNode>
    {
        private double _epsilon { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="PLNodeComparer"/> class.
        /// </summary>
        /// <param name="distanceTolerance">The distance tolerance.</param>
        public PLNodeComparer(double distanceTolerance)
        {
            this._epsilon = distanceTolerance;
        }
        /// <summary>
        /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        /// <returns>A signed integer that indicates the relative values of <paramref name="x" /> and <paramref name="y" />, as shown in the following table.Value Meaning Less than zero<paramref name="x" /> is less than <paramref name="y" />.Zero<paramref name="x" /> equals <paramref name="y" />.Greater than zero<paramref name="x" /> is greater than <paramref name="y" />.</returns>
        public int Compare(PLNode x, PLNode y)
        {
            //end points
            int result = -1 * x.Connections.Count.CompareTo(y.Connections.Count);
            if (result != 0)
            {
                return result;
            }
            //distance
            if (x.DistanceFactor < this._epsilon || y.DistanceFactor < this._epsilon)
            {
                if (x.DistanceFactor < this._epsilon && y.DistanceFactor < this._epsilon)
                {
                    result = x.DistanceFactor.CompareTo(y.DistanceFactor);
                }
                else if (x.DistanceFactor < this._epsilon && y.DistanceFactor >= this._epsilon)
                {
                    result = 1;
                }
                else if (x.DistanceFactor >= this._epsilon && y.DistanceFactor < this._epsilon)
                {
                    result = -1;
                }
                return result;
            }
            result = x.AngleFacotr.CompareTo(y.AngleFacotr);
            if (result == 0)
            {
                result = x.Point.GetHashCode().CompareTo(y.GetHashCode());
            }
            return result;
        }
    }
}

