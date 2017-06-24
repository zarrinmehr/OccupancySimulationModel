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
        /// <summary>
        /// Visualizes the open boundary.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="elevation">The elevation.</param>
        [STAThread]
        public void VisualizeOpenBoundary(List<SpatialAnalysis.Geometry.UV> points, double elevation)
        {
            using (Transaction t = new Transaction(OSM_FOR_REVIT.RevitDocument, "Draw Boundary"))
            {
                t.Start();
                FailureHandlingOptions failOpt = t.GetFailureHandlingOptions();
                failOpt.SetFailuresPreprocessor(new CurveDrawingWarningSwallower());
                t.SetFailureHandlingOptions(failOpt);
                Plane p = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ(0, 0, elevation));
                SketchPlane skp = SketchPlane.Create(OSM_FOR_REVIT.RevitDocument, p);
                for (int i = 0; i < points.Count - 1; i++)
                {
                    try
                    {
                        XYZ p1 = new XYZ(points[i].U, points[i].V, elevation);
                        XYZ p2 = new XYZ(points[i + 1].U, points[i + 1].V, elevation);
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
            using (Transaction t = new Transaction(OSM_FOR_REVIT.RevitDocument, "Draw Boundary"))
            {
                t.Start();
                FailureHandlingOptions failOpt = t.GetFailureHandlingOptions();
                failOpt.SetFailuresPreprocessor(new CurveDrawingWarningSwallower());
                t.SetFailureHandlingOptions(failOpt);
                Plane p = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ(0, 0, elevation));
                SketchPlane skp = SketchPlane.Create(OSM_FOR_REVIT.RevitDocument, p);
                for (int i = 0; i < points.Length; i++)
                {
                    try
                    {
                        XYZ p1 = new XYZ(points[i].U, points[i].V, elevation);
                        int j = (i == points.Length - 1) ? 0 : i + 1;
                        XYZ p2 = new XYZ(points[j].U, points[j].V, elevation);
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
                    XYZ p1 = new XYZ(line.Start.U, line.Start.V, elevation);
                    XYZ p2 = new XYZ(line.End.U, line.End.V, elevation);
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
            XYZ p1 = new XYZ(pnt.U - size / 2, pnt.V - size / 2, elevation);
            XYZ p2 = new XYZ(pnt.U + size / 2, pnt.V + size / 2, elevation);
            XYZ q1 = new XYZ(pnt.U + size / 2, pnt.V - size / 2, elevation);
            XYZ q2 = new XYZ(pnt.U - size / 2, pnt.V + size / 2, elevation);
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
                    try
                    {
                        XYZ p1 = new XYZ(item.Start.U, item.Start.V, elevation);
                        XYZ p2 = new XYZ(item.End.U, item.End.V, elevation);
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
            return new SpatialAnalysis.Geometry.UV(xyz.X, xyz.Y);
        }
    }

}

