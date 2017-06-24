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
using Autodesk.Revit.DB;
using System.Windows.Media.Media3D;
using System.Windows;
using SpatialAnalysis.Miscellaneous;

namespace OSM_Revit.REVIT_INTEROPERABILITY
{
    /// <summary>
    /// Class ExtractGeometryModel3D.
    /// </summary>
    internal class ExtractGeometryModel3D
    {
        private static StringBuilder reporter = new StringBuilder();
        private static int failedAttempt = 0, meshNum = 0, solidNum = 0, faceNum = 0, outOfTerritory = 0, failedMatParse = 0;
        /// <summary>
        /// The default material
        /// </summary>
        public DiffuseMaterial DefaultMaterial = new DiffuseMaterial(System.Windows.Media.Brushes.AliceBlue);
        /// <summary>
        /// Gets or sets the WPF geometries.
        /// </summary>
        /// <value>The WPF geometries.</value>
        public List<GeometryModel3D> WPFGeometries { get; set; }
        private Options _options { get; set; }
        private Rect3D _territory { get; set; }
        private SolidOrShellTessellationControls control { get; set; }
        private Transform inverse { get; set; }
        private Document doc { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="ExtractGeometryModel3D"/> class.
        /// </summary>
        /// <param name="document">The Revit document.</param>
        /// <param name="options">The Revit options.</param>
        /// <param name="elements">The Revit elements.</param>
        /// <param name="inverseTransform">The Revit inverse transform.</param>
        /// <param name="territory">The territory bounding box.</param>
        public ExtractGeometryModel3D(Document document, Options options, IEnumerable<Element> elements, Transform inverseTransform, Rect3D territory)
        {
            this.control = new SolidOrShellTessellationControls();
            this.inverse = inverseTransform;
            this.WPFGeometries = new List<GeometryModel3D>();
            this._options = options;
            this.doc = document;
            this._territory = territory;
            foreach (Element element in elements)
            {
                GeometryElement geomElem = null;
                geomElem = element.get_Geometry(this._options);
                if (typeof(FamilyInstance) == element.GetType())
                {
                    FamilyInstance familyInstance = element as FamilyInstance;
                    geomElem = familyInstance.get_Geometry(this._options).GetTransformed(Transform.Identity);
                }
                else
                {
                    geomElem = element.get_Geometry(this._options);
                }
                if (geomElem != null)
                {
                    this.GetGeometry(geomElem);
                }
            }
        }
        private MeshGeometry3D _toMeshGeometry3D(Mesh mesh)
        {
            try
            {
                MeshGeometry3D WPFMesh = new MeshGeometry3D();
                WPFMesh.Positions = new Point3DCollection();
                for (int i = 0; i < mesh.Vertices.Count; i++)
                {
                    XYZ pnt = mesh.Vertices[i];
                    WPFMesh.Positions.Add(new Point3D(pnt.X, pnt.Y, pnt.Z));
                }
                WPFMesh.TriangleIndices = new System.Windows.Media.Int32Collection();
                for (int i = 0; i < mesh.NumTriangles; i++)
                {
                    var triangle = mesh.get_Triangle(i);
                    WPFMesh.TriangleIndices.Add((int)triangle.get_Index(0));
                    WPFMesh.TriangleIndices.Add((int)triangle.get_Index(1));
                    WPFMesh.TriangleIndices.Add((int)triangle.get_Index(2));
                }
                return WPFMesh;
            }
            catch (Exception error)
            {
                failedAttempt++;
                reporter.AppendLine(string.Format("{0}: Failed to parse mesh:\n {1}", failedAttempt.ToString(), error.Report()));
                return null;
            }
        }
        private void _solidToMeshGeometry3D(Solid solid)
        {
            try
            {
                IList<Solid> solids = SolidUtils.SplitVolumes(solid);
                foreach (Solid _solid in solids)
                {
                    if (SolidUtils.IsValidForTessellation(_solid))
                    {
                        var material = this.getMaterial(_solid);
                        TriangulatedSolidOrShell shell = SolidUtils.TessellateSolidOrShell(_solid, control);
                        for (int i = 0; i < shell.ShellComponentCount; i++)
                        {
                            try
                            {
                                TriangulatedShellComponent component = shell.GetShellComponent(i);
                                MeshGeometry3D WPFMesh = new MeshGeometry3D();
                                Point3D[] points = new Point3D[component.VertexCount];
                                WPFMesh.TriangleIndices = new System.Windows.Media.Int32Collection();
                                for (int j = 0; j < component.TriangleCount; j++)
                                {
                                    TriangleInShellComponent triangle = component.GetTriangle(j);
                                    WPFMesh.TriangleIndices.Add(triangle.VertexIndex0);
                                    XYZ p = inverse.OfPoint(component.GetVertex(triangle.VertexIndex0));
                                    points[triangle.VertexIndex0] = new Point3D(p.X, p.Y, p.Z);

                                    WPFMesh.TriangleIndices.Add(triangle.VertexIndex1);
                                    p = inverse.OfPoint(component.GetVertex(triangle.VertexIndex1));
                                    points[triangle.VertexIndex1] = new Point3D(p.X, p.Y, p.Z);

                                    WPFMesh.TriangleIndices.Add(triangle.VertexIndex2);
                                    p = inverse.OfPoint(component.GetVertex(triangle.VertexIndex2));
                                    points[triangle.VertexIndex2] = new Point3D(p.X, p.Y, p.Z);
                                }
                                WPFMesh.Positions = new Point3DCollection(points);
                                if (this._territory.IntersectsWith(WPFMesh.Bounds))
                                {
                                    GeometryModel3D geometryModel3D = new GeometryModel3D(WPFMesh, material);
                                    this.WPFGeometries.Add(geometryModel3D);
                                    solidNum++;
                                }
                                else
                                {
                                    outOfTerritory++;
                                }
                            }
                            catch (Exception e)
                            {
                                failedAttempt++;
                                reporter.AppendLine(string.Format("{0}: Failed to parse Solid:\n {1}", failedAttempt.ToString(), e.Report()));
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("Solid not valid for tessellation!");
                    }
                }
            }
            catch (Exception e)
            {
                failedAttempt++;
                reporter.AppendLine(string.Format("{0}: Failed to parse S0lid:\n {1}", failedAttempt.ToString(), e.Report()));
            }
        }
        private DiffuseMaterial getMaterial(Solid solid)
        {
            if (solid.Faces == null || solid.Faces.Size == 0)
            {
                failedMatParse++;
                return this.DefaultMaterial.CloneCurrentValue();
            }
            return this.getMaterial(solid.Faces.get_Item(0));
        }
        private DiffuseMaterial getMaterial(Face face)
        {
            ElementId matId = face.MaterialElementId;
            Autodesk.Revit.DB.Material mat = this.doc.GetElement(matId) as Autodesk.Revit.DB.Material;
            return this.getMaterial(mat);
        }
        private DiffuseMaterial getMaterial(Autodesk.Revit.DB.Material mat)
        {
            if (mat == null)
            {
                failedMatParse++;
                return this.DefaultMaterial.CloneCurrentValue();
            }
            System.Windows.Media.Brush brush = null;
            try
            {
                
                System.Windows.Media.Color clr = System.Windows.Media.Color.FromRgb(
                    mat.Color.Red,
                    mat.Color.Green,
                    mat.Color.Blue);
                float r= 1 - ((float)mat.Transparency) / 100;
                clr.A = (byte)(r * 255);
                brush = new System.Windows.Media.SolidColorBrush(clr);
            }
            catch (Exception)
            {
                failedMatParse++;
            }
            if (brush != null)
            {
                return new DiffuseMaterial(brush);
            }
            failedMatParse++;
            return this.DefaultMaterial.CloneCurrentValue();
        }
        private void GetGeometry(GeometryElement geomElement)
        {
            if (geomElement != null)
            {
                foreach (GeometryObject geomObj in geomElement)
                {
                    if (geomObj as Solid != null)
                    {
                        this._solidToMeshGeometry3D(geomObj as Solid);
                    }
                    else if (geomObj as Mesh != null)
                    {
                        Mesh transformedMesh = (geomObj as Mesh).get_Transformed(this.inverse);
                        var WPFMesh = this._toMeshGeometry3D(transformedMesh);
                        if (WPFMesh != null)
                        {
                            if (this._territory.IntersectsWith(WPFMesh.Bounds))
                            {
                                GeometryModel3D geometryModel3D = new GeometryModel3D(WPFMesh, this.getMaterial(geomElement.MaterialElement));
                                this.WPFGeometries.Add(geometryModel3D);
                                meshNum++;
                            }
                            else
                            {
                                outOfTerritory++;
                            }
                        }
                    }
                    else if (geomObj as Face != null)
                    {
                        Mesh transformedMesh = null;
                        try
                        {
                            Mesh mesh = (geomObj as Face).Triangulate(1);
                            transformedMesh = mesh.get_Transformed(this.inverse);
                        }
                        catch (Exception error)
                        {
                            failedAttempt++;
                            reporter.AppendLine(string.Format("{0}: Failed to parse Face:\n {1}", failedAttempt.ToString(), error.Report()));
                        }
                        if (transformedMesh != null)
                        {
                            var WPFMesh = this._toMeshGeometry3D(transformedMesh);
                            if (WPFMesh != null)
                            {
                                if (this._territory.IntersectsWith(WPFMesh.Bounds))
                                {
                                    GeometryModel3D geometryModel3D = new GeometryModel3D(WPFMesh, this.getMaterial(geomObj as Face));
                                    this.WPFGeometries.Add(geometryModel3D);
                                    faceNum++;
                                }
                                else
                                {
                                    outOfTerritory++;
                                }
                            }
                        }
                    }
                    else if (geomObj as GeometryElement != null)
                    {
                        this.GetGeometry(geomObj as GeometryElement);
                    }
                    else if (geomObj as GeometryInstance != null)
                    {
                        this.GetGeometry(geomObj as GeometryInstance);
                    }
                    else
                    {
                        //this.Others.Add(geomObj);
                    }
                }
            }
        }
        private void GetGeometry(GeometryInstance geomInstance)
        {
            GeometryElement geomElement = geomInstance.GetInstanceGeometry();
            this.GetGeometry(geomElement);
        }

        /// <summary>
        /// Shows the parsing report.
        /// </summary>
        public void ShowParsingReport()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format("{0} Elements parsed to WPF", (faceNum + meshNum + solidNum).ToString()));
            sb.AppendLine(string.Format("\t{0} Solids", solidNum));
            sb.AppendLine(string.Format("\t{0} Faces", faceNum));
            sb.AppendLine(string.Format("\t{0} Meshes", meshNum));
            sb.AppendLine(string.Format("{0} Elements were located out of the field teritory ", outOfTerritory));
            sb.AppendLine(string.Format("{0} Failed Attempts of parsing material", failedMatParse));
            sb.AppendLine(string.Format("{0} Failed Attempts of parsing geometry", failedAttempt));
            sb.AppendLine(string.Format("\tFailure Report:"));
            sb.Append(reporter.ToString());
            string report = sb.ToString();
            sb.Clear();
            sb = null;
            MessageBox.Show(report, "Geometry Parsing Report");
        }
        /// <summary>
        /// Cleans this instance.
        /// </summary>
        public void Clean()
        {
            this.control.Dispose();
            this._options = null;
            this.doc = null;
        }
        /// <summary>
        /// Resets the report.
        /// </summary>
        public static void ResetReport()
        {
            ExtractGeometryModel3D.reporter.Clear();
            failedAttempt = 0; meshNum = 0; solidNum = 0; faceNum = 0; outOfTerritory = 0; failedMatParse = 0;
        }
    }
}

