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
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Windows;
using SpatialAnalysis.Geometry;
using SpatialAnalysis.Miscellaneous;
using SpatialAnalysis.Interoperability;
namespace OSM_Revit.REVIT_INTEROPERABILITY
{
    /// <summary>
    /// Class RevitVisualizer provides implementation for IVisualize interface.
    /// </summary>
    /// <seealso cref="SpatialAnalysis.ExternalConnection.I_OSM_To_BIM" />
    public class OSM_To_Revit : I_OSM_To_BIM
    {
        private UnitConversion unitConversion;
        public OSM_To_Revit(Length_Unit_Types OSM_unit)
        {
            //revit uses double values to represent feet imperial unit
            this.unitConversion = new UnitConversion(OSM_unit, Length_Unit_Types.FEET);
        }
        /// <summary>
        /// Visualizes the open boundary.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="elevation">The elevation.</param>
        [STAThread]
        public void VisualizeOpenBoundary(List<SpatialAnalysis.Geometry.UV> points, double elevation)
        {
            //create a deep copy of the list
            var copy = new List<SpatialAnalysis.Geometry.UV>();
            foreach (var item in points)
            {
                copy.Add(item.Copy());
            }
            //transform units
            unitConversion.Transform(copy);
            //draw in revit
            using (Transaction t = new Transaction(OSM_FOR_REVIT.RevitDocument, "Draw Boundary"))
            {
                t.Start();
                FailureHandlingOptions failOpt = t.GetFailureHandlingOptions();
                failOpt.SetFailuresPreprocessor(new CurveDrawingWarningSwallower());
                t.SetFailureHandlingOptions(failOpt);
                Plane p = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ(0, 0, elevation));
                SketchPlane skp = SketchPlane.Create(OSM_FOR_REVIT.RevitDocument, p);
                for (int i = 0; i < copy.Count - 1; i++)
                {
                    try
                    {
                        XYZ p1 = new XYZ(copy[i].U, copy[i].V, elevation);
                        XYZ p2 = new XYZ(copy[i + 1].U, copy[i + 1].V, elevation);
                        Line l = Line.CreateBound(p1, p2);
                        OSM_FOR_REVIT.RevitDocument.Create.NewModelCurve(l, skp);
                    }
                    catch (Exception e)
                    { MessageBox.Show(e.Report()); }
                }
                t.Commit();
            }
        }
        /// <summary>
        /// Visualizes a polygon in BIM environment.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="elevation">The elevation.</param>
        [STAThread]
        public void VisualizePolygon(SpatialAnalysis.Geometry.UV[] points, double elevation)
        {
            //create a deep copy of the list
            var copy = new SpatialAnalysis.Geometry.UV[points.Length];
            int pointCount = 0;
            foreach (var item in points)
            {
                copy[pointCount] = new SpatialAnalysis.Geometry.UV(item);
                pointCount++;
            }
            //transform units
            unitConversion.Transform(copy);
            //draw in revit
            using (Transaction t = new Transaction(OSM_FOR_REVIT.RevitDocument, "Draw Boundary"))
            {
                t.Start();
                FailureHandlingOptions failOpt = t.GetFailureHandlingOptions();
                failOpt.SetFailuresPreprocessor(new CurveDrawingWarningSwallower());
                t.SetFailureHandlingOptions(failOpt);
                Plane p = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ(0, 0, elevation));
                SketchPlane skp = SketchPlane.Create(OSM_FOR_REVIT.RevitDocument, p);
                for (int i = 0; i < copy.Length; i++)
                {
                    try
                    {
                        XYZ p1 = new XYZ(copy[i].U, copy[i].V, elevation);
                        int j = (i == copy.Length - 1) ? 0 : i + 1;
                        XYZ p2 = new XYZ(copy[j].U, copy[j].V, elevation);
                        Line l = Line.CreateBound(p1, p2);
                        OSM_FOR_REVIT.RevitDocument.Create.NewModelCurve(l, skp);
                    }
                    catch (Exception e)
                    { MessageBox.Show(e.Report()); }
                }
                t.Commit();
            }
        }
        /// <summary>
        /// Visualizes a line in BIM environment
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="elevation">The elevation.</param>
        [STAThread]
        public void VisualizeLine(UVLine line, double elevation)
        {
            SpatialAnalysis.Geometry.UV start = line.Start.Copy();
            SpatialAnalysis.Geometry.UV end = line.End.Copy();
            unitConversion.Transform(start);
            unitConversion.Transform(end);
            using (Transaction t = new Transaction(OSM_FOR_REVIT.RevitDocument, "Draw Barriers"))
            {
                t.Start();
                FailureHandlingOptions failOpt = t.GetFailureHandlingOptions();
                failOpt.SetFailuresPreprocessor(new CurveDrawingWarningSwallower());
                t.SetFailureHandlingOptions(failOpt);
                Plane p = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ(0, 0, elevation));
                SketchPlane skp = SketchPlane.Create(OSM_FOR_REVIT.RevitDocument, p);
                try
                {
                    XYZ p1 = new XYZ(start.U, start.V, elevation);
                    XYZ p2 = new XYZ(end.U, end.V, elevation);
                    Line l = Line.CreateBound(p1, p2);
                    OSM_FOR_REVIT.RevitDocument.Create.NewModelCurve(l, skp);
                }
                catch (Exception e)
                { MessageBox.Show(e.Report()); }

                t.Commit();
            }
        }
        /// <summary>
        /// Visualizes a point in BIM environment
        /// </summary>
        /// <param name="pnt">The point.</param>
        /// <param name="size">The size.</param>
        /// <param name="elevation">The elevation.</param>
        [STAThread]
        public void VisualizePoint(SpatialAnalysis.Geometry.UV pnt, double size, double elevation)
        {
            //unit conversion
            SpatialAnalysis.Geometry.UV p = pnt.Copy();
            unitConversion.Transform(p);
            //revit drawing
            XYZ p1 = new XYZ(p.U - size / 2, p.V - size / 2, elevation);
            XYZ p2 = new XYZ(p.U + size / 2, p.V + size / 2, elevation);
            XYZ q1 = new XYZ(p.U + size / 2, p.V - size / 2, elevation);
            XYZ q2 = new XYZ(p.U - size / 2, p.V + size / 2, elevation);
            using (Transaction t = new Transaction(OSM_FOR_REVIT.RevitDocument, "Show Point"))
            {
                t.Start();
                FailureHandlingOptions failOpt = t.GetFailureHandlingOptions();
                failOpt.SetFailuresPreprocessor(new CurveDrawingWarningSwallower());
                t.SetFailureHandlingOptions(failOpt);
                Plane pln = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ(0, 0, elevation));
                SketchPlane skp = SketchPlane.Create(OSM_FOR_REVIT.RevitDocument, pln);
                Line l1 = Line.CreateBound(p1, p2);
                Line l2 = Line.CreateBound(q1, q2);
                OSM_FOR_REVIT.RevitDocument.Create.NewModelCurve(l1, skp);
                OSM_FOR_REVIT.RevitDocument.Create.NewModelCurve(l2, skp);
                t.Commit();
            }
            p1 = null; p2 = null; q1 = null; q2 = null;
        }
        /// <summary>
        /// Visualizes a collection of lines in BIM environment
        /// </summary>
        /// <param name="lines">The lines.</param>
        /// <param name="elevation">The elevation.</param>
        [STAThread]
        public void VisualizeLines(ICollection<UVLine> lines, double elevation)
        {
            
            using (Transaction t = new Transaction(OSM_FOR_REVIT.RevitDocument, "Draw lines"))
            {
                t.Start();
                FailureHandlingOptions failOpt = t.GetFailureHandlingOptions();
                failOpt.SetFailuresPreprocessor(new CurveDrawingWarningSwallower());
                t.SetFailureHandlingOptions(failOpt);
                Plane p = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ(0, 0, elevation));
                SketchPlane skp = SketchPlane.Create(OSM_FOR_REVIT.RevitDocument, p);
                foreach (UVLine item in lines)
                {
                    //unit conversion
                    SpatialAnalysis.Geometry.UV start = item.Start.Copy();
                    unitConversion.Transform(start);
                    SpatialAnalysis.Geometry.UV end = item.End.Copy();
                    unitConversion.Transform(end);
                    //revit drawing part
                    try
                    {
                        XYZ p1 = new XYZ(start.U, start.V, elevation);
                        XYZ p2 = new XYZ(end.U, end.V, elevation);
                        Line l = Line.CreateBound(p1, p2);
                        OSM_FOR_REVIT.RevitDocument.Create.NewModelCurve(l, skp);
                    }
                    catch (Exception e)
                    { MessageBox.Show(e.Report()); }
                }
                t.Commit();
            }
        }
        /// <summary>
        /// Picks the point.
        /// </summary>
        /// <param name="message">The message to pass to BIM.</param>
        /// <returns>UV.</returns>
        [STAThread]
        public SpatialAnalysis.Geometry.UV PickPoint(string message)
        {
            UIDocument uidoc = new Autodesk.Revit.UI.UIDocument(OSM_FOR_REVIT.RevitDocument);
            XYZ xyz = uidoc.Selection.PickPoint(message);
            var revitUV = new SpatialAnalysis.Geometry.UV(xyz.X, xyz.Y);
            UnitConversion.Transform(revitUV, Length_Unit_Types.FEET, unitConversion.FromUnit);
            return revitUV;
        }
    }

}

