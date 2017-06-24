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
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using SpatialAnalysis.FieldUtility;
using SpatialAnalysis.CellularEnvironment;
using SpatialAnalysis.Geometry;
using SpatialAnalysis.Data;
using SpatialAnalysis.Events;


namespace SpatialAnalysis.Visualization3D
{
    /// <summary>
    /// Class SpatialDataToMesh.
    /// This class converts an instance of <c>ISpatialData</c> to a mesh
    /// </summary>
    internal class SpatialDataToMesh
    {
        /// <summary>
        /// Gets or sets the suggested z scale.
        /// </summary>
        /// <value>The suggested z scale.</value>
        public double SuggestedZScale { get; set; }
        /// <summary>
        /// Gets or sets the maximum value of the instance of <c>ISpatialData</c>.
        /// </summary>
        /// <value>The maximum value.</value>
        public double MaxValue { get; set; }
        /// <summary>
        /// Gets or sets the minimum value of the instance of <c>ISpatialData</c>.
        /// </summary>
        /// <value>The minimum value.</value>
        public double MinValue { get; set; }
        /// <summary>
        /// Gets or sets the mesh which is created from the data.
        /// This property can be accessed immediately after the instantiation of the class.
        /// </summary>
        /// <value>The mesh.</value>
        public MeshGeometry3D Mesh { get; set; }
        private OSMDocument _host { get; set; }
        private SelectDataFor3DVisualization _selection { get; set; }
        private Dictionary<Cell, double> cellToValue { get; set; }
        private Map<Point3D, Cell> point3DAndCell { get; set; }
        private Map<Point3D, int> Point3DAndIndex { get; set; }
        private Point3DCollection meshPoint3DCollection { get; set; }
        private Int32Collection meshTriangleIndices { get; set; }
        private Point3D[] meshVertices { get; set; }
        private PointCollection meshTextureCoordinates { get; set; }
        private Transform textureTransform { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpatialDataToMesh"/> class.
        /// </summary>
        /// <param name="host">The main document to which this window belongs.</param>
        /// <param name="selection">The selection window.</param>
        public SpatialDataToMesh(OSMDocument host, SelectDataFor3DVisualization selection)
        {
            this._host = host;
            this._selection = selection;
            #region load transform
            Matrix make = Matrix.Identity;
            make.Translate(-1 * this._host.cellularFloor.Origin.U, -1 * this._host.cellularFloor.Origin.V);
            UV diagnal = this._host.cellularFloor.TopRight - this._host.cellularFloor.Origin;
            make.Scale(1.0d / diagnal.U, -1.0d / diagnal.V);
            make.Translate(0, 1);

            this.textureTransform = new MatrixTransform(make);
            #endregion

            this.loadData();
            this.Mesh = this.setMesh();
            this.cellToValue = null;
            this.point3DAndCell = null;
            this.meshVertices = null;
            this.Point3DAndIndex = null;
        }
        // load the data before creating the mesh
        private void loadData()
        {
            #region load cells and their values
            if (this._selection.SelectedSpatialData.Type == DataType.SpatialData)
            {
                SpatialDataField dataField = this._selection.SelectedSpatialData as SpatialDataField;
                if (!this._selection.VisualizeCost)
                {
                    this.cellToValue = (dataField).Data;
                }
                else
                {
                    this.cellToValue = new Dictionary<Cell, double>();
                    foreach (Cell item in dataField.Data.Keys)
                    {
                        double val = dataField.GetCost(dataField.Data[item]);
                        this.cellToValue.Add(item, val);
                    }
                }
            }
            else
            {
                this.cellToValue = this._selection.SelectedSpatialData.Data;
            }

            #endregion
            #region load max value and min value and z scale
            this.MaxValue = double.NegativeInfinity;
            this.MinValue = double.PositiveInfinity;
            foreach (KeyValuePair<Cell, double> item in this.cellToValue)
            {
                if (this.MinValue > item.Value)
                {
                    this.MinValue = item.Value;
                }
                if (this.MaxValue < item.Value)
                {
                    this.MaxValue = item.Value;
                }
            }
            
            double sizeOfFloor = Math.Min(
                this._host.cellularFloor.TopRight.U - this._host.cellularFloor.Origin.U,
                this._host.cellularFloor.TopRight.V - this._host.cellularFloor.Origin.V);
            double maxHeight = this.MaxValue - this.MinValue;
            
            if (maxHeight > sizeOfFloor)
            {
                this.SuggestedZScale = sizeOfFloor / maxHeight;
            }
            else
            {
                this.SuggestedZScale = 1;
            }
            #endregion
            #region load point3d
            this.point3DAndCell = new Map<Point3D, Cell>();
            foreach (KeyValuePair<Cell, double> item in this.cellToValue)
            {
                Point3D pnt = new Point3D(item.Key.U, item.Key.V, item.Value - this.MinValue);// + SpatialDataToMesh.Epsilon);
                this.point3DAndCell.Add(item.Key, pnt);
            }
            #endregion
        }
        // generate the mesh
        private MeshGeometry3D setMesh()
        {
            //loading point collection
            this.meshVertices = this.point3DAndCell.GetVariable1();
            this.Point3DAndIndex = new Map<Point3D, int>();
            //creating a Map to trace the point indices
            for (int i = 0; i < this.meshVertices.Length; i++)
            {
                this.Point3DAndIndex.Add(i, this.meshVertices[i]);
            }
            this.meshPoint3DCollection = new Point3DCollection(this.meshVertices);
            this.meshTriangleIndices = new Int32Collection();
            this.meshTextureCoordinates = new PointCollection();
            foreach (KeyValuePair<Cell,double> item in this.cellToValue)
            {
                try
                {
                    Cell cell = item.Key;
                    FaceIndices[] faces = this.CellToFaces(cell);
                    if (faces != null)
                    {
                        foreach (FaceIndices face in faces)
                        {
                            foreach (int iter in face.Indices)
                            {
                                this.meshTriangleIndices.Add(iter);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }
            foreach (Point3D item in this.meshVertices)
            {
                //this.meshTextueCoordinates.Add(new Point(item.X,item.Y));
                this.meshTextureCoordinates.Add(this.getMeshTextureCoordinates(item));
            }
            MeshGeometry3D mesh = new MeshGeometry3D();
            mesh.Positions = this.meshPoint3DCollection;
            mesh.TriangleIndices = this.meshTriangleIndices;
            mesh.TextureCoordinates = this.meshTextureCoordinates;
            mesh.Freeze();
            //MessageBox.Show(string.Format("X min: {0}\nX max: {1}\nY min: {2}\nY max: {3}", minX.ToString(), maxX.ToString(),
            //    minY.ToString(), maxY.ToString()));
            return mesh;
        }

        //triangulate a cell
        private FaceIndices[] CellToFaces(Cell cell)
        {
            try
            {
                Index index = this._host.cellularFloor.FindIndex(cell);
                int k = 0;
                Cell nextI = this._host.cellularFloor.RelativeIndex(index, new Index(1, 0));
                if (nextI != null)
                {
                    if (this.cellToValue.ContainsKey(nextI))
                    {
                        k++;
                    }
                    else
                    {
                        nextI = null;
                    }
                }
                Cell nextJ = this._host.cellularFloor.RelativeIndex(index, new Index(0, 1));
                if (nextJ != null)
                {
                    if (this.cellToValue.ContainsKey(nextJ))
                    {
                        k++;
                    }
                    else
                    {
                        nextJ = null;
                    }
                }
                Cell nextIJ = this._host.cellularFloor.RelativeIndex(index, new Index(1, 1));
                if (nextIJ != null)
                {
                    if (this.cellToValue.ContainsKey(nextIJ))
                    {
                        k++;
                    }
                    else
                    {
                        nextIJ = null;
                    }
                }
                if (k<2)//there are only two points
                {
                    return null;
                }
                if (k == 2)//there are three points
                {
                    Point3D[] pnts = new Point3D[3];
                    pnts[0] = this.point3DAndCell.Find(cell);
                    int i = 1;
                    if (nextI != null)
                    {
                        pnts[i] = this.point3DAndCell.Find(nextI);
                        i++;
                    }
                    if (nextJ != null)
                    {
                        pnts[i] = this.point3DAndCell.Find(nextJ);
                        i++;
                    }
                    if (nextIJ != null)
                    {
                        pnts[i] = this.point3DAndCell.Find(nextIJ);
                        i++;
                    }
                    int v0 = this.Point3DAndIndex.Find(pnts[0]);
                    int v1 = this.Point3DAndIndex.Find(pnts[1]);
                    int v2 = this.Point3DAndIndex.Find(pnts[2]);
                    FaceIndices[] face = new FaceIndices[1];
                    face[0] = new FaceIndices(v0, v1, v2);
                    Vector3D vec10 = Point3D.Subtract(pnts[0], pnts[1]);
                    Vector3D vec12 = Point3D.Subtract(pnts[2], pnts[1]);
                    var normal = Vector3D.CrossProduct(vec12, vec10);
                    if (normal.Z<0)
                    {
                        face[0].Flip();
                    }
                    return face;
                }
                //there are four points
                FaceIndices[] faces = new FaceIndices[2];
                Point3D v00 = this.point3DAndCell.Find(cell);
                int I00 = this.Point3DAndIndex.Find(v00);
                Point3D v10 = this.point3DAndCell.Find(nextI);
                int I10 = this.Point3DAndIndex.Find(v10);
                Point3D v11 = this.point3DAndCell.Find(nextIJ);
                int I11 = this.Point3DAndIndex.Find(v11);
                Point3D v01 = this.point3DAndCell.Find(nextJ);
                int I01 = this.Point3DAndIndex.Find(v01);
                faces[0] = new FaceIndices(I00, I10, I11);
                faces[1] = new FaceIndices(I00, I11, I01);
                return faces;
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
            }
            return null;
        }

        private Vector3D getNormal(FaceIndices face)
        {
            Vector3D v10 = Point3D.Subtract(this.meshVertices[face.Indices[0]], this.meshVertices[face.Indices[1]]);
            Vector3D v12 = Point3D.Subtract(this.meshVertices[face.Indices[2]], this.meshVertices[face.Indices[1]]);
            var normal = Vector3D.CrossProduct(v12, v10);
            normal.Normalize();
            return normal;
        }
        private Point getMeshTextureCoordinates(Point3D pnt)
        {
            Point p = this.textureTransform.Transform(new Point(pnt.X, pnt.Y));
            return p;
        }

        #region Saving (for debugging)

        /// <summary>
        /// Saves the mesh to wavefront(*.obj) file format. 
        /// This method was originally taken from the following link and manipulated for use in thia project
        /// https://github.com/AnthonyNystrom/NuGenBioChemDX/blob/master/NuGenBioChem/Visualization/Mathematics/MeshGeometry3D.cs
        /// AnthonyNystrom/NuGenBioChemDX
        /// NuGenBioChemDX/NuGenBioChem/Visualization/Mathematics/MeshGeometry3D.cs
        /// </summary>
        /// <param name="mesh">Mesh geometry</param>
        /// <param name="path">Filename</param>
        public static void SaveToWavefrontObj(GeometryModel3D dataMeshModel, string path)
        {
            MeshGeometry3D mesh = (MeshGeometry3D)dataMeshModel.Geometry;
            using (var writer = new System.IO.StreamWriter(path, false, System.Text.Encoding.ASCII))
            {
                var format = System.Globalization.CultureInfo.InvariantCulture;
                Point3D[] pointsTransformed = new Point3D[mesh.Positions.Count];
                int pointCounter = 0;
                foreach (Point3D point in mesh.Positions)
                {
                    Point3D position = dataMeshModel.Transform.Transform(point);
                    pointsTransformed[pointCounter] = position;
                    pointCounter++;
                    writer.WriteLine("v {0} {1} {2}",
                        position.X.ToString(format),
                        position.Y.ToString(format),
                        position.Z.ToString(format));
                }
                int indiceCounter = 0;
                while (indiceCounter < mesh.TriangleIndices.Count)
                {
                    Vector3D v10 = Point3D.Subtract(pointsTransformed[mesh.TriangleIndices[indiceCounter]],
                        pointsTransformed[mesh.TriangleIndices[indiceCounter + 1]]);
                    Vector3D v12 = Point3D.Subtract(pointsTransformed[mesh.TriangleIndices[indiceCounter + 2]],
                        pointsTransformed[mesh.TriangleIndices[indiceCounter + 1]]);
                    var normal = Vector3D.CrossProduct(v12, v10);
                    normal.Normalize();
                    writer.WriteLine("vn {0} {1} {2}",
                        normal.X.ToString(format),
                        normal.Y.ToString(format),
                        normal.Z.ToString(format));
                    indiceCounter += 3;
                }
                pointsTransformed = null;
                foreach (Point textureCoordinate in mesh.TextureCoordinates)
                {
                    writer.WriteLine("vt {0} {1}",
                        textureCoordinate.X.ToString(format),
                        textureCoordinate.Y.ToString(format));
                }
                string formatString;
                if (mesh.TextureCoordinates.Count == mesh.Positions.Count)
                {
                    if (mesh.Normals.Count == mesh.Positions.Count) formatString = "f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}";
                    else formatString = "f {0}/{0} {1}/{1} {2}/{2}";
                }
                else
                {
                    if (mesh.Normals.Count == mesh.Positions.Count) formatString = "f {0}//{0} {1}//{1} {2}//{2}";
                    else formatString = "f {0} {1} {2}";
                }
                for (int i = 2; i < mesh.TriangleIndices.Count; i += 3)
                {
                    writer.WriteLine(formatString,
                        (mesh.TriangleIndices[i - 2] + 1).ToString(format),
                        (mesh.TriangleIndices[i - 1] + 1).ToString(format),
                        (mesh.TriangleIndices[i] + 1).ToString(format));
                }
            }
        }

        #endregion
    }




}

