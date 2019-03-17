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
using System.Text;
//Clipper library for boolean operations of the polygons: http://sourceforge.net/projects/polyclipping/?source=navbar
using ClipperLib;
using INTPolygon = System.Collections.Generic.List<ClipperLib.IntPoint>;
using INTPolygons = System.Collections.Generic.List<System.Collections.Generic.List<ClipperLib.IntPoint>>;
using SpatialAnalysis;
using System.Windows;
using SpatialAnalysis.Geometry;
using System.Windows.Media.Media3D;
using SpatialAnalysis.Miscellaneous;

namespace SpatialAnalysis.Interoperability
{
    /// <summary>
    /// This class provides a list of public members and functions that should be implemented to read BIM data according to its format.
    /// This class uses Clipper Library for polygonal boolean operations. This library represents polygons with a list of integer points.
    /// </summary>
    public abstract class BIM_To_OSM_Base
    {
        protected Length_Unit_Types unitType;
        /// <summary>
        /// Gets the unit of length for OSM 
        /// </summary>
        public Length_Unit_Types UnitType
        {
            get { return unitType; }
        }
        /// <summary>
        /// Gets or sets the length of line segments to approximate curves with polygons
        /// </summary>
        /// <value>The length of the curve approximation.</value>
        public double CurveApproximationLength { get; set; }
        private int _polygonalBooleanPrecision;
        //PolygonalBooleanPrecision
        /// <summary>
        /// Gets or sets an integer used to determine decimals of the polygonal boolean precision.
        /// </summary>
        /// <value>The polygonal boolean precision.</value>
        /// <exception cref="ArgumentException">The number of digits cannot be larger than 8.</exception>
        protected int PolygonalBooleanPrecision 
        {
            get { return _polygonalBooleanPrecision; }
            set
            {
                if (value > 9)
                {
                    throw new ArgumentException("The number of digits cannot be larger than 8.");
                }
                this._polygonalBooleanPrecision = value;
            } 
        }
        /// <summary>
        /// Gets or sets the footprint integer polygons of visual barriers.
        /// </summary>
        /// <value>The foot print integer polygons of visual barriers.</value>
        public INTPolygons FootPrintPolygonsOfVisualBarriers { get; set; }
        /// <summary>
        /// Gets or sets the footprint integer polygons of physical barriers.
        /// </summary>
        /// <value>The footprint integer polygons of physical barriers.</value>
        public INTPolygons FootPrintPolygonsOfPhysicalBarriers { get; set; }
        /// <summary>
        /// Gets or sets the footprint integer polygons of field with voids.
        /// </summary>
        /// <value>The footprint integer polygons of field with voids.</value>
        public INTPolygons FootPrintPolygonsOfFieldWithVoids { get; set; }
        /// <summary>
        /// Gets or sets the footprint integer polygons of field without voids.
        /// </summary>
        /// <value>The footprint integer polygons of field with out voids.</value>
        public INTPolygons FootPrintPolygonsOfFieldWithOutVoids { get; set; }
        // holes, visual and physical
        /// <summary>
        /// Gets or sets the footprint integer polygons of all barriers.
        /// </summary>
        /// <value>The footprint integer polygons of all barriers.</value>
        public INTPolygons FootPrintOfAllBarriers { get; set; }
        /// <summary>
        /// The number of purged points
        /// </summary>
        protected int PurgedPoints = 0;
        /// <summary>
        /// The minimum length of lines in the polygons
        /// </summary>
        public double MinimumLengthOfLine;
        /// <summary>
        /// Gets or sets the physical barriers.
        /// </summary>
        /// <value>The physical barriers.</value>
        public BarrierPolygon[] PhysicalBarriers { get; set; }
        /// <summary>
        /// Gets or sets the visual barriers.
        /// </summary>
        /// <value>The visual barriers.</value>
        public BarrierPolygon[] VisualBarriers { get; set; }
        /// <summary>
        /// Gets or sets the field barriers.
        /// </summary>
        /// <value>The field barriers.</value>
        public BarrierPolygon[] FieldBarriers { get; set; }
        /// <summary>
        /// Gets or sets the field without holes.
        /// </summary>
        /// <value>The field without holes.</value>
        public BarrierPolygon[] FieldWithoutHoles { get; set; }
        /// <summary>
        /// The floor minimum bound
        /// </summary>
        public UV FloorMinBound;
        /// <summary>
        /// The floor maximum bound
        /// </summary>
        public UV FloorMaxBound;
        /// <summary>
        /// Gets or sets the height of the obstacle. Objects with higher heights will be considered visual barriers and lower heights will be considered physical barriers. 
        /// </summary>
        /// <value>The height of the obstacle.</value>
        public double VisibilityObstacleHeight { get; set; }//VisualObstacleHeight
        /// <summary>
        /// Gets or sets the report which are generated during the data format exchange.
        /// </summary>
        /// <value>The report.</value>
        public string Report { get; set; }
        /// <summary>
        /// Gets or sets the minimum line length squared.
        /// </summary>
        /// <value>The minimum line length squared.</value>
        public double MinimumLineLengthSquared { get; set; }
        /// <summary>
        /// Gets or sets the elevation.
        /// </summary>
        /// <value>The elevation.</value>
        public double PlanElevation { get; set; }
        /// <summary>
        /// Gets or sets the plan name of the BIM model.
        /// </summary>
        /// <value>The plan name.</value>
        public string PlanName { get; set; }
        /// <summary>
        /// Simplifies an list INTPolygons using expand and shrink technique.
        /// </summary>
        /// <param name="polygons">The INTPolygons.</param>
        /// <param name="value">The value used for expand and shrink.</param>
        /// <returns>INTPolygons.</returns>
        public INTPolygons SimplifyINTPolygons(INTPolygons polygons, double value)
        {
            double simplificationFactor = Math.Pow(10.0, this.PolygonalBooleanPrecision) * UnitConversion.Convert(value, Length_Unit_Types.FEET, UnitType);
            ClipperOffset clipperOffset = new ClipperOffset();
            clipperOffset.AddPaths(polygons, ClipperLib.JoinType.jtMiter, EndType.etClosedPolygon);
            INTPolygons shrink = new INTPolygons();
            clipperOffset.Execute(ref shrink, -simplificationFactor);
            //expanding to return the polygons to their original position
            clipperOffset.Clear();
            clipperOffset.AddPaths(shrink, ClipperLib.JoinType.jtMiter, EndType.etClosedPolygon);
            INTPolygons expand = new INTPolygons();
            clipperOffset.Execute(ref expand, simplificationFactor);
            shrink = null;
            clipperOffset = null;
            return expand;
        }
        /// <summary>
        /// Gets the BIM Model as a list of meshes to which the materials are attached. The parsed model should be sliced with A plane at obstacle height. Implementation is required for this abstract method. 
        /// </summary>
        /// <param name="min">The lover left corner of the territory.</param>
        /// <param name="Max">The upper right corner of the territory.</param>
        /// <param name="offset">The offset value of the bounding box.</param>
        /// <returns>List&lt;GeometryModel3D&gt; which will be used for data visualization.</returns>
        public abstract List<object> GetSlicedMeshGeometries(UV min, UV Max, double offset);
        /// <summary>
        /// /// Gets the BIM Model as a list of meshes to which the materials are attached. Implementation is required for this abstract method. 
        /// </summary>
        /// <param name="min">The lover left corner of the territory.</param>
        /// <param name="Max">The upper right corner of the territory.</param>
        /// <param name="offset">The offset value of the bounding box.</param>
        /// <returns>List&lt;GeometryModel3D&gt;.</returns>
        public abstract List<object> ParseBIM(UV min, UV Max, double offset);

        /// <summary>
        /// Converts a list of UV points to a list of IntPoints (i.e. an INTPolygon) which can be used for polygonal operations.
        /// </summary>
        /// <param name="UVs">The list of UV points.</param>
        /// <returns>INTPolygon.</returns>
        public INTPolygon ConvertUVListToINTPolygon(List<UV> UVs)
        {
            INTPolygon contour = new INTPolygon();
            for (int i = 0; i < UVs.Count; i++)
            {
                contour.Add(ConvertUVToIntPoint(UVs[i]));
            }
            return contour;
        }
        /// <summary>
        /// Converts a UV to an IntPoint.
        /// </summary>
        /// <param name="uv">The uv.</param>
        /// <returns>IntPoint.</returns>
        /// <exception cref="ArgumentException"></exception>
        public IntPoint ConvertUVToIntPoint(UV uv)
        {
            IntPoint pnt = new IntPoint();
            try
            {
                long x = Convert.ToInt64(Math.Floor(uv.U * Math.Pow(10, this.PolygonalBooleanPrecision)));
                long y = Convert.ToInt64(Math.Floor(uv.V * Math.Pow(10, this.PolygonalBooleanPrecision)));
                pnt = new IntPoint(x, y);
            }
            catch (Exception e)
            {
                throw new ArgumentException(e.Report());
            }
            return pnt;
        }
        /// <summary>
        /// Converts the IntPoint to a UV.
        /// </summary>
        /// <param name="pnt">The IntPoint.</param>
        /// <returns>UV.</returns>
        public UV ConvertIntPointToUV(IntPoint pnt)
        {
            double x = Convert.ToDouble(pnt.X) / Math.Pow(10, this.PolygonalBooleanPrecision);
            double y = Convert.ToDouble(pnt.Y) / Math.Pow(10, this.PolygonalBooleanPrecision);
            UV xy = new UV(x, y);
            return xy;
        }
        /// <summary>
        /// Converts the INTPolygon to BarrierPolygons.
        /// </summary>
        /// <param name="ply">The ply.</param>
        /// <returns>BarrierPolygons.</returns>
        public BarrierPolygon ConvertINTPolygonToBarrierPolygon(INTPolygon ply)
        {
            List<UV> pnts = new List<UV>();
            for (int i = 0; i < ply.Count; i++)
            {
                pnts.Add(ConvertIntPointToUV(ply[i]));
            }
            pnts = SimplifyPolygon(pnts);
            return new BarrierPolygon(pnts.ToArray());
        }

        /// <summary>
        /// Simplifies the polygon.
        /// </summary>
        /// <param name="uvs">A list of uv points.</param>
        /// <param name="proportionOfMinimumLengthOfLine">The proportion of minimum length of line parameter by default set to 3.</param>
        /// <param name="angle">The angle by default set to zero.</param>
        /// <returns>List&lt;UV&gt;.</returns>
        public List<UV> SimplifyPolygon(List<UV> uvs, UInt16 proportionOfMinimumLengthOfLine = 3, double angle = 0.0d)
        {
            SpatialAnalysis.Geometry.PLine pln = new PLine(uvs, true);
            return pln.Simplify(this.MinimumLengthOfLine / proportionOfMinimumLengthOfLine);
        }
        /// <summary>
        /// Returns an array of barriers that is used for autonomous walking scenarios
        /// </summary>
        /// <param name="offsetValue">Human body size which is the distance that you want the agents from barriers</param>
        /// <returns>Expanded version of all barrier polygons including field naked edges and holes, visual barriers and physical barriers </returns>
        public BarrierPolygon[] ExpandAllBarrierPolygons(double offsetValue)
        {
            ClipperOffset clipperOffset = new ClipperOffset();
            //clipperOffset.AddPaths(this.FootPrintOfAllBarriers, JoinType.jtSquare, EndType.etClosedPolygon);
            clipperOffset.AddPaths(this.FootPrintPolygonsOfFieldWithVoids, JoinType.jtSquare, EndType.etClosedPolygon);
            INTPolygons plygns = new INTPolygons();
            double uniqueOffsetValue = -Math.Pow(10.0, this.PolygonalBooleanPrecision) * offsetValue;
            clipperOffset.Execute(ref plygns, uniqueOffsetValue);
            List<BarrierPolygon> brrs = new List<BarrierPolygon>();
            for (int i = 0; i < plygns.Count; i++)
            {
                BarrierPolygon brr = this.ConvertINTPolygonToBarrierPolygon(plygns[i]);
                if (brr.Length > 0)
                {
                    brrs.Add(brr);
                }
            }
            clipperOffset.Clear();
            plygns.Clear();
            return brrs.ToArray();
        }
        /// <summary>
        /// Sets the territory.
        /// </summary>
        protected void setTerritory()
        {
            double xMin = double.PositiveInfinity, xMax = double.NegativeInfinity, yMin = double.PositiveInfinity, yMax = double.NegativeInfinity;
            foreach (BarrierPolygon item in this.VisualBarriers)
            {
                foreach (UV p in item.BoundaryPoints)
                {
                    xMin = (p.U < xMin) ? p.U : xMin;
                    xMax = (p.U > xMax) ? p.U : xMax;
                    yMin = (p.V < yMin) ? p.V : yMin;
                    yMax = (p.V > yMax) ? p.V : yMax;
                }
            }
            foreach (BarrierPolygon item in this.FieldBarriers)
            {
                foreach (UV p in item.BoundaryPoints)
                {
                    xMin = (p.U < xMin) ? p.U : xMin;
                    xMax = (p.U > xMax) ? p.U : xMax;
                    yMin = (p.V < yMin) ? p.V : yMin;
                    yMax = (p.V > yMax) ? p.V : yMax;
                }
            }
            this.FloorMinBound = new UV(xMin, yMin);
            this.FloorMaxBound = new UV(xMax, yMax);
        }
        #region Polygonal Isovist Claculation
        private INTPolygon createCircle(UV center, double r)
        {
            int n = (int)(Math.PI * 2 * r / this.CurveApproximationLength) + 1;
            INTPolygon circle = new INTPolygon();
            for (int i = 0; i < n; i++)
            {
                UV direction = new UV(r * Math.Cos(i * Math.PI * 2 / n), r * Math.Sin(i * Math.PI * 2 / n));
                circle.Add(ConvertUVToIntPoint(direction + center));
            }
            return circle;
        }
        private INTPolygon excludedArea(UV center, double r, UVLine edge)
        {
            INTPolygon area = new INTPolygon();

            double ds = edge.Start.DistanceTo(center);
            double de = edge.End.DistanceTo(center);
            r = (r > ds) ? r : ds;
            r = (r > de) ? r : de;
            double R = 1.43 * r; // somthing larger than Math.Sqrt(2)*r
            UV os = edge.Start - center;
            os.Unitize();
            UV oe = edge.End - center;
            oe.Unitize();
            List<UV> pnts = new List<UV>();
            double x = oe.DotProduct(os);
            if (x == 0)
            {
                return area;
            }
            else if (x >= 0)
            {
                pnts.Add(edge.Start);
                pnts.Add(center + os * R);
                pnts.Add(center + oe * R);
                pnts.Add(edge.End);
            }
            else // if (x<0)
            {
                UV om = (os + oe) / 2;
                om.Unitize();
                pnts.Add(edge.Start);
                pnts.Add(center + os * R);
                pnts.Add(center + om * R);
                pnts.Add(center + oe * R);
                pnts.Add(edge.End);
            }
            return this.ConvertUVListToINTPolygon(pnts);
        }
        /// <summary>
        /// Gets the Isovist polygon.
        /// </summary>
        /// <param name="vantagePoint">The vantage point.</param>
        /// <param name="viewDepth">The view depth.</param>
        /// <param name="edges">The edges.</param>
        /// <returns>BarrierPolygons.</returns>
        public BarrierPolygon IsovistPolygon(UV vantagePoint, double viewDepth, HashSet<UVLine> edges)
        {
            /*first and expand and shrink operation is performed to merge the shadowing edges*/
            double expandShrinkFactor = Math.Pow(10.0, this.PolygonalBooleanPrecision) * UnitConversion.Convert(0.075, Length_Unit_Types.FEET, UnitType);
            //offsetting the excluded area of each edge
            INTPolygons offsetedPolygons = new INTPolygons();
            ClipperOffset clipperOffset = new ClipperOffset();
            foreach (UVLine edgeItem in edges)
            {
                clipperOffset.AddPath(this.excludedArea(vantagePoint, viewDepth + 1, edgeItem), ClipperLib.JoinType.jtMiter, EndType.etClosedPolygon);
                INTPolygons plygns = new INTPolygons();
                clipperOffset.Execute(ref plygns, expandShrinkFactor);
                offsetedPolygons.AddRange(plygns);
                clipperOffset.Clear();
            }
            //Unioning the expanded exclusions
            INTPolygons offsetUnioned = new INTPolygons();
            Clipper c = new Clipper();
            c.AddPaths(offsetedPolygons, PolyType.ptSubject, true);
            c.Execute(ClipType.ctUnion, offsetUnioned, PolyFillType.pftNonZero, PolyFillType.pftNonZero);
            //shrink the polygons to retain their original size
            INTPolygons results = new INTPolygons();
            clipperOffset.Clear();
            clipperOffset.AddPaths(offsetUnioned, JoinType.jtMiter, EndType.etClosedPolygon);
            clipperOffset.Execute(ref results, -expandShrinkFactor);
            clipperOffset.Clear();
            offsetUnioned.Clear();
            /*
            * What ever is a hole in the resulting mereged polygon is the visibility polygon
            * Now we classify the polygons based on being a hole or not
            */
            //filtering out the holes that do not include the center
            INTPolygons holesNOT = new INTPolygons();
            INTPolygons holesIncludingCenter = new INTPolygons();
            IntPoint iCenter = ConvertUVToIntPoint(vantagePoint);
            foreach (INTPolygon item in results)
            {
                if (!Clipper.Orientation(item))
                {
                    if (Clipper.PointInPolygon(iCenter, item) == 1)
                    {
                        holesIncludingCenter.Add(item);
                    }
                }
                else
                {
                    holesNOT.Add(item);
                }
            }
            if (holesIncludingCenter.Count == 0)
            {
                //there is no hole. The shadow polygones should clip the potential field of visibility (i.e. circle) by an subtraction operation
                INTPolygon circle = createCircle(vantagePoint, viewDepth);
                //subtraction
                c.Clear();
                c.AddPath(circle, PolyType.ptSubject, true);
                c.AddPaths(holesNOT, PolyType.ptClip, true);
                INTPolygons isovistPolygon = new INTPolygons();
                c.Execute(ClipType.ctDifference, isovistPolygon);
                //searching for a polygon that includes the center
                foreach (INTPolygon item in isovistPolygon)
                {
                    if (Clipper.PointInPolygon(iCenter, item) == 1)
                    {
                        BarrierPolygon isovist = this.ConvertINTPolygonToBarrierPolygon(item);
                        results = null;
                        c = null;
                        clipperOffset = null;
                        offsetedPolygons = null;
                        circle = null;
                        holesNOT = null;
                        holesIncludingCenter = null;
                        isovistPolygon = null;
                        return isovist;
                    }
                }
                MessageBox.Show(string.Format("Isovist not found!\nNo hole detected\n{0} polygons can be isovist", isovistPolygon.Count.ToString()));
            }
            else if (holesIncludingCenter.Count == 1)
            {
                INTPolygons isovistPolygon = holesIncludingCenter;
                foreach (INTPolygon item in isovistPolygon)
                {
                    if (Clipper.PointInPolygon(iCenter, item) == 1)
                    {
                        item.Reverse();
                        BarrierPolygon isovist = this.ConvertINTPolygonToBarrierPolygon(item);
                        results = null;
                        c = null;
                        clipperOffset = null;
                        offsetedPolygons = null;
                        holesNOT = null;
                        holesIncludingCenter = null;
                        isovistPolygon = null;
                        return isovist;
                    }
                }
                MessageBox.Show(string.Format("Isovist not found!\nOne hole detected\n{0} polygons can be isovist", isovistPolygon.Count.ToString()));
            }
            else if (holesIncludingCenter.Count > 1)
            {
                MessageBox.Show("Isovist not found!\nMore than one hole found that can include the vantage point");
            }
            return null;
        }
        #endregion
    }
}

