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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;
using SpatialAnalysis.FieldUtility;
using SpatialAnalysis.Visualization;
using SpatialAnalysis.CellularEnvironment;
using SpatialAnalysis.Geometry;
using SpatialAnalysis.Data;
using SpatialAnalysis.Interoperability;

namespace SpatialAnalysis.Visualization3D
{
    /// <summary>
    /// Interaction logic for _3DVisualizerHost.xaml
    /// </summary>
    public partial class VisualizerHost3D : Window
    {
        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            this.Owner.WindowState = this.WindowState;
        }
        private enum GeometryType
        {
            None=0,
            Grid=1,
            Contour=2,
        }
        private GeometryType _geomType { get; set; }
        private enum ColorMode
        {
            Solid=0,
            Data=1,
        }
        private int _contourCount { get; set; }
        private double _dataOpacity { get; set; }
        private ColorMode _clrMode { get; set; }
        private Brush surfaceBrush { get; set; }
        private Brush geomBrush { get; set; }
        private double geomThickness { get; set; }

        private ScaleTransform3D _scaleTransform3D { get; set; }
        private GeometryModel3D dataMeshModel { get; set; }
        private DiffuseMaterial dataMeshMaterial { get; set; }
        private MeshGeometry3D dataMesh { get; set; }
        private Point3D leftBottom, leftTop, rightBottom, rightTop;
        private OSMDocument _host { get; set; }
        private SelectDataFor3DVisualization _selectedData { get; set; }
        private Point3D objectCenter = new Point3D();
        private double textureDPI { get; set; }
        private List<GeometryModel3D> modelGeometries { get; set; }
        private DiffuseMaterial geometryMaterial { get; set; }
        private Brush geometryBrush { get; set; }
        private Transform _materialTransformation { get; set; }
        private Rect _materialBound { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualizerHost3D"/> class.
        /// </summary>
        /// <param name="host">The main document to which this window belongs.</param>
        public VisualizerHost3D(OSMDocument host)
        {
            InitializeComponent();
            this._host = host;
            this._materialBound = new Rect(0, 0,
                                    this._host.cellularFloor.TopRight.U - this._host.cellularFloor.Origin.U,
                                    this._host.cellularFloor.TopRight.V - this._host.cellularFloor.Origin.V);
            Matrix matrix = Matrix.Identity;
            matrix.Translate(-this._host.cellularFloor.Origin.U, -this._host.cellularFloor.Origin.V);
            matrix.Scale(1, -1);
            matrix.Translate(0, this._host.cellularFloor.TopRight.V - this._host.cellularFloor.Origin.V);
            this._materialTransformation = new MatrixTransform(matrix);
            this._contourCount = 10;
            this._clrMode = ColorMode.Solid;
            this._geomType = GeometryType.None;
            this.geomBrush = Brushes.Black.Clone();
            this.geomThickness = UnitConversion.Convert(0.1, Length_Unit_Types.FEET, this._host.BIM_To_OSM.UnitType, 5);
            this.surfaceBrush = Brushes.Tomato.Clone();
            this.geometryBrush = Brushes.AliceBlue.Clone();
            this.geometryBrush.Opacity = .4;
            this.geometryMaterial = new DiffuseMaterial(this.geometryBrush);
            this.textureDPI = UnitConversion.Convert(1000, this._host.BIM_To_OSM.UnitType, Length_Unit_Types.FEET, 5);
            this._dataOpacity = 1.0d;
            #region load ground
            double defaultHeight = -.1;
            this.leftBottom = new Point3D(this._host.cellularFloor.Territory_Min.U,
                this._host.cellularFloor.Territory_Min.V, defaultHeight);
            this.leftTop = new Point3D(this._host.cellularFloor.Territory_Min.U,
                    this._host.cellularFloor.Territory_Max.V, defaultHeight);
            this.rightBottom = new Point3D(this._host.cellularFloor.Territory_Max.U,
                    this._host.cellularFloor.Territory_Min.V, defaultHeight);
            this.rightTop = new Point3D(this._host.cellularFloor.Territory_Max.U,
                this._host.cellularFloor.Territory_Max.V, defaultHeight);
            
            Point3DCollection collection2 = new Point3DCollection() 
            { 
                this.leftBottom,
                this.leftTop,
                this.rightBottom,
                this.rightTop
            };
            Int32Collection indices2 = new Int32Collection()
            {
                0,2,1, 1,2,3,
            };
            this.groundMesh.Positions = collection2;
            this.groundMesh.TriangleIndices = indices2;
            
            
            #endregion 

            #region set Camera
            UV center = this._host.cellularFloor.Origin + this._host.cellularFloor.TopRight;
            center /= 2;
            this.objectCenter = new Point3D(center.U, center.V, 0);
            double x = this._host.cellularFloor.Origin.DistanceTo(this._host.cellularFloor.TopRight);
            x *= -1;
            var look = new Vector3D(-1, 1, -1);
            this.camera.LookDirection = look;
            this.camera.Position = Point3D.Add(this.objectCenter, x * look);
            var z = new Vector3D(0, 0, 1);
            var helper = Vector3D.CrossProduct(z, look);
            var up = Vector3D.CrossProduct(look, helper);
            up.Normalize();
            this.camera.UpDirection = up;
            #endregion
            //for zooming
            this.PreviewMouseWheel += new MouseWheelEventHandler(MainWindow_PreviewMouseWheel);
            //this.previousCameraPos = this.camera.Position;
            this.MouseRightButtonDown += new MouseButtonEventHandler(navigationTrigger);

            this._translateTransform3D = new TranslateTransform3D(new Vector3D(0.0d, 0.0d, UnitConversion.Convert(0.005d, Length_Unit_Types.FEET, this._host.BIM_To_OSM.UnitType)));
        }

        protected override void OnClosed(EventArgs e) //releasing memory 
        {
            try
            {
                this._host = null;
                this.groundModel = null;
                this.groundMesh = null;
                this.groundMeshMaterial = null;

                this.Visualizer3D.UnregisterName("camera");
                this.camera = null;

                this.Visualizer3D.UnregisterName("AllModels");
                this.AllModels.Children.Clear();
                this.AllModels = null;

                this.UnregisterName("Visualizer3D");
                this.MainGrid.Children.Remove(this.Visualizer3D);
                this.Visualizer3D = null;

                this.UnregisterName("Scene");
                this.MainGrid.Children.Remove(this.Scene);
                this.Scene = null;
                
                if (this.modelGeometries != null)
                {
                    this.modelGeometries.Clear();
                }
                this.modelGeometries = null;

                this.geometryMaterial = null;
                this.geometryBrush = null;

                this.surfaceBrush = null;
                this.geomBrush = null;
                this.dataMeshModel = null;
                this.dataMeshMaterial = null;
                this.dataMesh = null;
            }
            catch (Exception error)
            {
                MessageBox.Show("memory not released: " + this.GetType().ToString() + "\n" + error.Message);
            }


            base.OnClosed(e);
        }
        #region util functions
        // add points
        private Point3D AddPoints(Point3D p1, Point3D p2)
        {
            return new Point3D(p1.X + p2.X, p1.Y + p2.Y, p1.Z + p2.Z);
        }
        //Point3D previousCameraPos;
        private double getDistance(Point3D p1, Point3D p2)
        {
            var v = Point3D.Subtract(p2, p1);
            return v.Length;
        }
        #endregion

        #region navigation
        private double distanceToTargetPoint { get; set; }
        private Point3D targetPoint { get; set; }
        private Point navigationVectorStart { get; set; }
        private Matrix3D navigationMat = new Matrix3D();
        private Line navigationVectorVisual { get; set; }
        private Point3D initialCameraPosition { get; set; }
        private Vector3D initialCameraUpDirection { get; set; }
        private Vector3D initialCameraLookDirection { get; set; }
        //navigation start trigger
        private void navigationTrigger(object sender, MouseButtonEventArgs e)
        {
            try
            {
                this.navigationVectorStart = Mouse.GetPosition(this.Scene);
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
                return;
            }

            #region update transformation matrix
            // up is the direction of camera
            Vector3D z = this.camera.LookDirection;
            if (z.LengthSquared != 1)
            {
                z.Normalize();
            }
            // y is the updirection of the camera
            Vector3D y = this.camera.UpDirection;
            if (y.LengthSquared != 1)
            {
                y.Normalize();
            }
            // x is calculated by the cross product
            Vector3D x = Vector3D.CrossProduct(y, z);
            if (x.LengthSquared != 1)
            {
                x.Normalize();
            }
            // pluging the x, y, and z vectors in a Matrix3D 
            // the Matrix class manuplation is ridiculous... auuuhh
            this.navigationMat.M11 = x.X;
            this.navigationMat.M12 = x.Y;
            this.navigationMat.M13 = x.Z;
            this.navigationMat.M21 = y.X;
            this.navigationMat.M22 = y.Y;
            this.navigationMat.M23 = y.Z;
            this.navigationMat.M31 = z.X;
            this.navigationMat.M32 = z.Y;
            this.navigationMat.M33 = z.Z;
            //this.panMat.Invert();
            this.navigationMat.Translate(Point3D.Subtract(new Point3D(0, 0, 0), this.initialCameraPosition));
            #endregion
            this.initialCameraPosition = this.camera.Position;
            this.initialCameraLookDirection = z;
            this.initialCameraUpDirection = y;
            this.navigationVectorVisual = new Line()
            {
                X1 = this.navigationVectorStart.X,
                Y1 = this.navigationVectorStart.Y,
                StrokeThickness = 1,
                Stroke = Brushes.DarkSeaGreen
            };
            if (!(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))) //triger pan
            {
                this.MouseMove += new MouseEventHandler(panUpdate);
                this.MouseRightButtonUp += new MouseButtonEventHandler(panTerminate);
                this.Cursor = Cursors.Cross;
            }
            else //triger orbit
            {
                this.MouseMove += new MouseEventHandler(orbitUpdate);
                this.MouseRightButtonUp += new MouseButtonEventHandler(orbitTerminate);
                //find distance from target along the camera look direction
                Vector3D CameraPositionToObjectCenter = Point3D.Subtract(this.objectCenter, this.initialCameraPosition);
                this.distanceToTargetPoint = Vector3D.DotProduct(CameraPositionToObjectCenter, z);
                // find target relative to mesh center
                this.targetPoint = Point3D.Add(this.initialCameraPosition, this.distanceToTargetPoint * z);
            }
        }

        #region pan
        // terminate paning
        private void panTerminate(object sender, MouseButtonEventArgs e)
        {
            this.Cursor = Cursors.Arrow;
            this.MouseMove -= panUpdate;
            this.MouseRightButtonUp -= panTerminate;
            this.Scene.Children.Clear();
            this.navigationVectorVisual = null;
        }
        // camera update when panning
        private void panUpdate(object sender, MouseEventArgs e)
        {
            var p = Mouse.GetPosition(this.Scene);
            this.navigationVectorVisual.X2 = p.X;
            this.navigationVectorVisual.Y2 = p.Y;
            if (!this.Scene.Children.Contains(this.navigationVectorVisual))
            {
                this.Scene.Children.Add(this.navigationVectorVisual);
            }
            // displacement on the scene
            Vector3D translationOn2DScene = new Vector3D(p.X - this.navigationVectorStart.X, p.Y - this.navigationVectorStart.Y, 0);
            Vector3D direction = this.navigationMat.Transform(translationOn2DScene);
            direction = direction - Vector3D.DotProduct(this.camera.LookDirection, direction) * this.camera.LookDirection;
            direction = UnitConversion.Convert(0.1,Length_Unit_Types.FEET,_host.BIM_To_OSM.UnitType) * direction;
            this.camera.Position = Point3D.Add(this.initialCameraPosition, direction);
        }
        #endregion

        #region Orbit

        //unregister all orbit events
        private void orbitTerminate(object sender, MouseButtonEventArgs e)
        {
            this.MouseMove -= orbitUpdate;
            this.MouseLeftButtonUp -= orbitTerminate;
            this.Scene.Children.Clear();
            this.navigationVectorVisual = null;
        }
        //orbit camera
        private void orbitUpdate(object sender, MouseEventArgs e)
        {
            var p = Mouse.GetPosition(this.Scene);
            this.navigationVectorVisual.X2 = p.X;
            this.navigationVectorVisual.Y2 = p.Y;
            if (!this.Scene.Children.Contains(this.navigationVectorVisual))
            {
                this.Scene.Children.Add(this.navigationVectorVisual);
            }
            // displacement on the scene
            Vector3D translationOn2DScene = new Vector3D(p.X - this.navigationVectorStart.X, p.Y - this.navigationVectorStart.Y, 0);
            Vector3D direction = this.navigationMat.Transform(translationOn2DScene);
            direction = direction - Vector3D.DotProduct(this.initialCameraLookDirection, direction) * this.initialCameraLookDirection;
            direction *= UnitConversion.Convert(1.0d, Length_Unit_Types.FEET, this._host.BIM_To_OSM.UnitType);
            if (direction.Length != 0)
            {
                Point3D pn1 = Vector3D.Add(direction, this.initialCameraPosition);
                Vector3D fromTargetToCamera = Point3D.Subtract(pn1, this.targetPoint);
                Vector3D translation = fromTargetToCamera * (this.distanceToTargetPoint / fromTargetToCamera.Length);
                //getting new camera position
                Point3D newCamPos = Point3D.Add(targetPoint, translation);
                //getting new camera look direction
                var newCamDir = -1 * fromTargetToCamera;
                newCamDir.Normalize();
                //getting new camera up direction
                Vector3D helper = Vector3D.CrossProduct(newCamDir, this.initialCameraUpDirection);
                Vector3D newUp = Vector3D.CrossProduct(helper, newCamDir);
                if (newUp.LengthSquared != 0)
                {
                    newUp.Normalize();
                }
                this.camera.UpDirection = newUp;
                this.camera.Position = newCamPos;
                this.camera.LookDirection = newCamDir;
            }
        }
        #endregion

        //zoom
        private void MainWindow_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            double factor = UnitConversion.Convert(0.1, Length_Unit_Types.FEET, _host.BIM_To_OSM.UnitType);
            var direction = this.camera.LookDirection;
            direction.Normalize();
            this.camera.Position = Vector3D.Add(direction * factor * e.Delta, this.camera.Position);
        }
        // fix perspective
        private void CameraHorizon_Click(object sender, RoutedEventArgs e)
        {
            var z = new Vector3D(0, 0, 1);
            var helper = Vector3D.CrossProduct(z, this.camera.LookDirection);
            var up = Vector3D.CrossProduct(this.camera.LookDirection, helper);
            if (up.X != 0 && up.Y != 0 && up.Z != 0)
            {
                up.Normalize();
                this.camera.UpDirection = up;
            }
        }
        #endregion

        #region Export
        private void Exporter_Click(object sender, RoutedEventArgs e)
        {
            if (this.dataMesh == null)
	        {
		         MessageBox.Show("Valid Mesh not found!");
                return;
	        }
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Title = "Save the Scene to Wavefront Obj format";
            dlg.DefaultExt = ".obj";
            dlg.Filter = "PNG documents (.obj)|*.obj";
            dlg.FileName = string.Format("Type '{0}' -- Name '{1}' (Vertical scaling factor {2})",
                this._selectedData.SelectedSpatialData.Type.ToString(), this._selectedData.SelectedSpatialData.Name, this.scaledZ.Text);
            Nullable<bool> result = dlg.ShowDialog(this);
            string fileAddress = "";
            if (result == true)
            {
                fileAddress = dlg.FileName;
            }
            else
            {
                return;
            }
            SpatialDataToMesh.SaveToWavefrontObj(this.dataMeshModel, fileAddress);
        }

        private void PNGExport_Click(object sender, RoutedEventArgs e)
        {
            double dpi = 96;
            GetNumber getResolution = new GetNumber("Set Resolution", "The resolution will be applied on the exported PNG image", dpi);
            getResolution.Owner = this;
            getResolution.ShowDialog();
            dpi = getResolution.NumberValue;

            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Title = "Save the Scene to PNG Image format";
            dlg.DefaultExt = ".png";
            dlg.Filter = "PNG documents (.png)|*.png";
            if (this._selectedData != null && this._selectedData.SelectedSpatialData != null)
            {
                dlg.FileName = string.Format("Type '{0}' -- Name '{1}' (Vertical scaling factor {2})",
                    this._selectedData.SelectedSpatialData.Type.ToString(), this._selectedData.SelectedSpatialData.Name, this.scaledZ.Text);
            }

            Nullable<bool> result = dlg.ShowDialog(this);
            string fileAddress = "";
            if (result == true)
            {
                fileAddress = dlg.FileName;
            }
            else
            {
                return;
            }

            Rect bounds = new Rect(this.Visualizer3D.RenderSize);
            RenderTargetBitmap main_rtb = new RenderTargetBitmap((int)(bounds.Width * dpi / 96), (int)(bounds.Height * dpi / 96), dpi, dpi, System.Windows.Media.PixelFormats.Default);
            DrawingVisual dvFloorScene = new DrawingVisual();
            using (DrawingContext dc = dvFloorScene.RenderOpen())
            {
                VisualBrush vb = new VisualBrush(this.Visualizer3D);
                dc.DrawRectangle(vb, null, bounds);
            }
            main_rtb.Render(dvFloorScene);
            BitmapEncoder pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(main_rtb));
            try
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                {
                    pngEncoder.Save(ms);
                    ms.Close();
                    System.IO.File.WriteAllBytes(fileAddress, ms.ToArray());
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Mesh load and transform
        private void chooseData_Click(object sender, RoutedEventArgs e)
        {
            this._selectedData = new SelectDataFor3DVisualization(this._host);
            this._selectedData.Owner = this;
            this._selectedData.ShowDialog();
            if (this._selectedData.SelectedSpatialData == null)
            {
                return;
            }
            if (this.dataMeshModel != null)
            {
                this.AllModels.Children.Remove(this.dataMeshModel);
                this.dataMeshModel = null;
                this.ZScaler.ValueChanged -= ZScaler_ValueChanged;
            }
            
            //load the mesh
            try
            {
                SpatialDataToMesh meshBuilder = new SpatialDataToMesh(this._host, this._selectedData);
                this.dataMesh = meshBuilder.Mesh;
                this.dataMeshModel = new GeometryModel3D(this.dataMesh, this.dataMeshMaterial);
                dataMeshModel.BackMaterial = new DiffuseMaterial(Brushes.DarkGray);
                this.UpdateMaterial();
                this.AllModels.Children.Add(this.dataMeshModel);
                this._transform3DGroup.Children.Clear();
                this._scaleTransform3D = new ScaleTransform3D(1, 1, meshBuilder.SuggestedZScale, this.objectCenter.X, this.objectCenter.Y, 0);
                this._transform3DGroup.Children.Add(this._scaleTransform3D);
                this._transform3DGroup.Children.Add(this._translateTransform3D);

                this.dataMeshModel.Transform = this._transform3DGroup;
                this.ZScaler.Minimum = 0;
                this.ZScaler.Maximum = 5 * meshBuilder.SuggestedZScale;
                this.ZScaler.Value = meshBuilder.SuggestedZScale;
                this.ZScaler.ValueChanged += new RoutedPropertyChangedEventHandler<double>(ZScaler_ValueChanged);
                
                scaledZ.Text = this.ZScaler.Value.ToString();
                this.dataType.Text = this._selectedData.SelectedSpatialData.Type.ToString();
                this.dataName.Text = this._selectedData.SelectedSpatialData.Name;
                this.translationZ.Text = meshBuilder.MinValue.ToString();
                this.minVal.Text = meshBuilder.MinValue.ToString();
                this.maxVal.Text = meshBuilder.MaxValue.ToString();
                
                this.objectCenter.Z = (meshBuilder.MaxValue - meshBuilder.MinValue) * meshBuilder.SuggestedZScale / 2;

            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
            }
        }
        private TranslateTransform3D _translateTransform3D;
        private Transform3DGroup _transform3DGroup = new Transform3DGroup();
        private void ZScaler_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                this._transform3DGroup.Children.Clear();
                this.scaledZ.Text = e.NewValue.ToString();
                this._scaleTransform3D.ScaleZ = e.NewValue;
                this._transform3DGroup.Children.Add(this._scaleTransform3D);
                this._transform3DGroup.Children.Add(this._translateTransform3D);
                //this.dataMeshModel.Transform = this.transform;
                this.dataMeshModel.Transform = this._transform3DGroup;
                
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
            }

        }
        #endregion

        #region Mesh surface apearance setting
        private void UpdateMaterial()
        {
            if (this.dataMesh == null)
            {
                MessageBox.Show("Data is not loaded!");
                return;
            }
            //i guess i need to clear the material to save memory
            this.dataMeshModel.Material = null;
            this.dataMeshMaterial = null;
            switch (this._geomType)
            {
                case GeometryType.None:
                    switch (this._clrMode)
                    {
                        case ColorMode.Solid:
                            this.dataMeshMaterial = new DiffuseMaterial(this.surfaceBrush);
                            break;
                        case ColorMode.Data:
                            var colorCodeBmp = this.getImage();
                            ImageBrush imBrush = new ImageBrush(colorCodeBmp);
                            imBrush.ViewportUnits = BrushMappingMode.Absolute;
                            //creating the material
                            this.dataMeshMaterial = new DiffuseMaterial(imBrush);
                            break;
                    }
                    break;
                case GeometryType.Grid:
                    //transforming the grid geometry
                    System.Windows.Media.Geometry gridGeometry = this._host.GridViewer.Geometry.Clone();
                    gridGeometry.Transform = _materialTransformation;
                    switch (this._clrMode)
                    {
                        case ColorMode.Solid:
                            // rendering the grid on a DrawingVisual
                            DrawingVisual dv_GridSolid = new DrawingVisual();
                            using (var dvc = dv_GridSolid.RenderOpen())
                            {
                                dvc.DrawRectangle(this.surfaceBrush, null, this._materialBound);
                                dvc.DrawGeometry(null, new Pen(this.geomBrush, this.geomThickness), gridGeometry);
                            }
                            //rendering the DrawingVisual to an image
                            RenderTargetBitmap bmp_GridSolid = new RenderTargetBitmap(
                                (int)(this._materialBound.Width * this.textureDPI / 96),
                                (int)(this._materialBound.Height * this.textureDPI / 96),
                                this.textureDPI,
                                this.textureDPI,
                                PixelFormats.Pbgra32);
                            bmp_GridSolid.Render(dv_GridSolid);
                            // adding the rendered image to create an imagebrush
                            ImageBrush imBrush_GridSolid = new ImageBrush(bmp_GridSolid);
                            imBrush_GridSolid.ViewportUnits = BrushMappingMode.Absolute;
                            //creating the material
                            this.dataMeshMaterial = new DiffuseMaterial(imBrush_GridSolid);
                            dv_GridSolid = null;
                            break;
                        case ColorMode.Data:
                            var colorCodedBmp = this.getImage();
                            // rendering the grid on a DrawingVisual
                            DrawingVisual dv_GridData = new DrawingVisual();
                            using (var dvc = dv_GridData.RenderOpen())
                            {
                                dvc.DrawImage(colorCodedBmp, this._materialBound);
                                dvc.DrawGeometry(null, new Pen(this.geomBrush, this.geomThickness), gridGeometry);
                            }
                            //rendering the DrawingVisual to an image
                            RenderTargetBitmap bmp_GridData = new RenderTargetBitmap(
                                (int)(this._materialBound.Width * this.textureDPI / 96),
                                (int)(this._materialBound.Height * this.textureDPI / 96),
                                this.textureDPI,
                                this.textureDPI,
                                PixelFormats.Pbgra32);
                            bmp_GridData.Render(dv_GridData);
                            // adding the rendered image to create an imagebrush
                            ImageBrush imBrush_GridData = new ImageBrush(bmp_GridData);
                            imBrush_GridData.ViewportUnits = BrushMappingMode.Absolute;
                            //creating the material
                            this.dataMeshMaterial = new DiffuseMaterial(imBrush_GridData);
                            dv_GridData = null;
                            break;
                    }
                    gridGeometry = null;
                    break;
                case GeometryType.Contour:
                    var contuorGeoms = this.loadContourGeometry();
                    contuorGeoms.Transform = this._materialTransformation;
                    switch (this._clrMode)
                    {
                        case ColorMode.Solid:
                            // rendering the contuors on a DrawingVisual
                            DrawingVisual dv_ContuorSolid = new DrawingVisual();
                            using (var dvc = dv_ContuorSolid.RenderOpen())
                            {
                                dvc.DrawRectangle(this.surfaceBrush, null, this._materialBound);
                                dvc.DrawGeometry(null, new Pen(this.geomBrush, this.geomThickness), contuorGeoms);
                            }
                            RenderTargetBitmap bmp__ContuorSolid = new RenderTargetBitmap(
                             (int)(this._materialBound.Width * this.textureDPI / 96),
                             (int)(this._materialBound.Height * this.textureDPI / 96),
                             this.textureDPI,
                             this.textureDPI,
                             PixelFormats.Pbgra32);
                            bmp__ContuorSolid.Render(dv_ContuorSolid);
                            // adding the rendered image to create an imagebrush
                            ImageBrush imBrush__ContuorSolid = new ImageBrush(bmp__ContuorSolid);
                            imBrush__ContuorSolid.ViewportUnits = BrushMappingMode.Absolute;
                            //creating the material
                            this.dataMeshMaterial = new DiffuseMaterial(imBrush__ContuorSolid);
                            dv_ContuorSolid = null;
                            break;
                        case ColorMode.Data:
                             var colorCodedBmp = this.getImage();
                            // rendering the contuors on a DrawingVisual
                            DrawingVisual dv_ContuorData = new DrawingVisual();
                            using (var dvc = dv_ContuorData.RenderOpen())
                            {
                                dvc.DrawImage(colorCodedBmp, this._materialBound);
                                dvc.DrawGeometry(null, new Pen(this.geomBrush, this.geomThickness), contuorGeoms);
                            }
                            //rendering the DrawingVisual to an image
                            Rect bounds_ContuorData = dv_ContuorData.ContentBounds;
                            RenderTargetBitmap bmp_ContuorData = new RenderTargetBitmap(
                                (int)(bounds_ContuorData.Width * this.textureDPI / 96),
                                (int)(bounds_ContuorData.Height * this.textureDPI / 96),
                                this.textureDPI,
                                this.textureDPI,
                                PixelFormats.Pbgra32);
                            bmp_ContuorData.Render(dv_ContuorData);
                            // adding the rendered image to create an imagebrush
                            ImageBrush imBrush_ContuorData = new ImageBrush(bmp_ContuorData);
                            imBrush_ContuorData.ViewportUnits = BrushMappingMode.Absolute;
                            //creating the material
                            this.dataMeshMaterial = new DiffuseMaterial(imBrush_ContuorData);
                            dv_ContuorData = null;
                            break;
                    }
                    contuorGeoms = null;
                    break;
            }
            //update back material
            if (((DiffuseMaterial)this.dataMeshModel.BackMaterial).Brush.Opacity != this.surfaceBrush.Opacity)
            {
                Brush backMaterialBrush = ((DiffuseMaterial)this.dataMeshModel.BackMaterial).Brush.Clone();
                backMaterialBrush.Opacity = this.surfaceBrush.Opacity;
                this.dataMeshModel.BackMaterial = new DiffuseMaterial(backMaterialBrush);
            }
            //update front material
            this.dataMeshModel.Material = this.dataMeshMaterial;
        }

        private void setSurfaceBrush_Click(object sender, RoutedEventArgs e)
        {
            foreach (object item in this.applyColor.Items)
            {
                ((MenuItem)item).IsChecked = false;
            }
            ((MenuItem)sender).IsChecked = true;
            this._clrMode = ColorMode.Solid;
            BrushPicker colorPicker = new BrushPicker(this.surfaceBrush);
            colorPicker.Owner = this;
            colorPicker.ShowDialog();
            this.surfaceBrush = colorPicker._Brush;
            this.UpdateMaterial();
            colorPicker = null;
        }

        private void showGrid_Click(object sender, RoutedEventArgs e)
        {
            if (this.dataMesh == null)
            {
                MessageBox.Show("Data is not loaded!");
                return;
            }
            foreach (object item in this.geometryType_Menu.Items)
            {
                ((MenuItem)item).IsChecked = false;
            }
            ((MenuItem)sender).IsChecked = true;
            this._geomType = GeometryType.Grid;
            this.geomThickness_menu.IsEnabled = true;
            this.geomBbrush_menu.IsEnabled = true;
            this.geomTextureRes_menu.IsEnabled = true;
            this.UpdateMaterial();
        }

        private void geomThickness_Click(object sender, RoutedEventArgs e)
        {
            GetNumber gn = new GetNumber("Enter New Thickness Value", "New thickness value will be applied to the grid", this.geomThickness);
            gn.Owner = this;
            gn.ShowDialog();
            this.geomThickness = gn.NumberValue;
            UpdateMaterial();
            gn = null;
        }

        private void goembrush_Click(object sender, RoutedEventArgs e)
        {
            BrushPicker colorPicker = new BrushPicker(this.geomBrush);
            colorPicker.Owner = this;
            colorPicker.ShowDialog();
            this.geomBrush = colorPicker._Brush;
            this.UpdateMaterial();
            colorPicker = null;
        }

        private void geomTextureRes_Click(object sender, RoutedEventArgs e)
        {
            GetNumber gn = new GetNumber("Set Texture Resolution",
                "The new values will be applied to the texture resolution", this.textureDPI);
            gn.Owner = this;
            gn.ShowDialog();
            if (this.textureDPI != gn.NumberValue)
            {
                this.textureDPI = gn.NumberValue;
                this.UpdateMaterial();
            }
            gn = null;
        }

        private void setDataColor_Click(object sender, RoutedEventArgs e)
        {
            foreach (object item in this.applyColor.Items)
            {
                ((MenuItem)item).IsChecked = false;
            }
            ((MenuItem)sender).IsChecked = true;
            this._clrMode = ColorMode.Data;
            var getNumber = new GetNumberSlider(0.0d, this._dataOpacity, 1.0d, "Set Data Opacity", null);
            getNumber.Owner = this;
            getNumber.ShowDialog();
            this._dataOpacity = getNumber.GetNumber;
            getNumber = null;
            this.UpdateMaterial();
        }

        private void showContours_menu_Click(object sender, RoutedEventArgs e)
        {
            var getNumber = new GetNumber("Set Contours",
                "Enter the number of contour levels.\n\tN >= 1", this._contourCount);
            getNumber.Owner = this;
            getNumber.ShowDialog();
            this._contourCount = (int)getNumber.NumberValue;
            getNumber = null;
            while (this._contourCount < 2)
            {
                var getNumber_ = new GetNumber("Set Contours",
                    "Enter the number of contour levels.\n\tN >= 1", this._contourCount);
                getNumber_.Owner = this;
                getNumber_.ShowDialog();
                this._contourCount = (int)getNumber_.NumberValue;
                getNumber_ = null;
            }
            if (this.dataMesh == null)
            {
                MessageBox.Show("Data is not loaded!");
                return;
            }
            foreach (object item in this.geometryType_Menu.Items)
            {
                ((MenuItem)item).IsChecked = false;
            }
            ((MenuItem)sender).IsChecked = true;
            this._geomType = GeometryType.Contour;
            this.geomThickness_menu.IsEnabled = true;
            this.geomBbrush_menu.IsEnabled = true;
            this.geomTextureRes_menu.IsEnabled = true;
            this.UpdateMaterial();
        }
        private StreamGeometry loadContourGeometry()
        {
            var meshToContours = new MeshGeometry3DToContours(this.dataMesh);
            double[] elevations = new double[this._contourCount];
            double base_ = (meshToContours.Max - meshToContours.Min) / (this._contourCount + 1);
            for (int i = 0; i < this._contourCount; i++)
            {
                elevations[i] = (i + 1) * base_;
            }
            var contours = meshToContours.GetIntersection(elevations);
            StreamGeometry contourGeoms = new StreamGeometry();
            using (var sgc = contourGeoms.Open())
            {
                foreach (BarrierPolygon item in contours)
                {
                    Point[] points = new Point[item.Length];
                    for (int i = 0; i < item.Length; i++)
                    {
                        points[i] = new Point(item.BoundaryPoints[i].U, item.BoundaryPoints[i].V);
                    }
                    sgc.BeginFigure(points[0], false, item.IsClosed);
                    sgc.PolyLineTo(points, true, true);
                }
            }
            meshToContours = null;
            return contourGeoms;
        }

        private void noGeometryMenu_Click(object sender, RoutedEventArgs e)
        {
            foreach (object item in this.geometryType_Menu.Items)
            {
                ((MenuItem)item).IsChecked = false;
            }
            ((MenuItem)sender).IsChecked = true;
            this._geomType = GeometryType.None;
            this.geomThickness_menu.IsEnabled = false;
            this.geomBbrush_menu.IsEnabled = false;
            this.geomTextureRes_menu.IsEnabled = false;
            this.UpdateMaterial();
        }

        private Dictionary<Cell, double> loadData()
        {
            Dictionary<Cell, double> cellToValue = new Dictionary<Cell, double>();
            if (this._selectedData.SelectedSpatialData.Type == DataType.SpatialData)
            {
                SpatialDataField dataField = (SpatialDataField )this._selectedData.SelectedSpatialData;
                if (!this._selectedData.VisualizeCost)
                {
                    cellToValue = (dataField).Data;
                }
                else
                {
                    cellToValue = new Dictionary<Cell, double>();
                    foreach (Cell item in dataField.Data.Keys)
                    {
                        double val = dataField.GetCost(dataField.Data[item]);
                        cellToValue.Add(item, val);
                    }
                }
            }
            else
            {
                cellToValue = this._selectedData.SelectedSpatialData.Data;
            }
            return cellToValue;
        }
        private WriteableBitmap getImage()
        {
            var cellToValue = this.loadData();
            double min = double.PositiveInfinity, max = double.NegativeInfinity;
            foreach (var item in cellToValue.Values)
            {
                min = Math.Min(min, item);
                max = Math.Max(max, item);
            }
            UV diagonal = this._host.cellularFloor.TopRight-this._host.cellularFloor.Origin;
            /*Seting the size of the image*/
            double size_ = 1600* this.textureDPI / UnitConversion.Convert(1000, this._host.BIM_To_OSM.UnitType, Length_Unit_Types.FEET, 5);
            //assuming diagonal.U <= diagonal.V
            double height_ = size_;
            double width_ = size_ * diagonal.U / diagonal.V;
            if (diagonal.U > diagonal.V)
            {
                width_ = size_;
                height_ = size_ * diagonal.V / diagonal.U;
            }
            //image scale to real world scale
            double scale = width_ / diagonal.U;
            //set the image size
            int h = (int)height_;
            int w = (int)width_;
            WriteableBitmap bmp = BitmapFactory.New(w , h);
            
            using (bmp.GetBitmapContext())
            {
                bmp.Clear();
                foreach (KeyValuePair<Cell, double> item in cellToValue)
                {
                    Point p1_ = this._materialTransformation.Transform(new Point(item.Key.U,item.Key.V));
                    Point p2_ = this._materialTransformation.Transform(new Point(item.Key.U + this._host.cellularFloor.CellSize, item.Key.V + this._host.cellularFloor.CellSize));
                    var color = this._host.ColorCode.GetColor((item.Value - min) / (max - min));
                    color.A = (byte)(255 * this._dataOpacity);
                    bmp.FillRectangle((int)(p1_.X*scale), (int)(p1_.Y*scale), (int)(p2_.X*scale), (int)(p2_.Y*scale), color);
                }
            }
            return bmp;
            /*
            //apply Gaussian Smoothing
            int[,] kernel = new int[5, 5]
            {
                {1,4,7,4,1 },
                {4,16,26,16,4 },
                {7,26,41,26,7 },
                {4,16,26,16,4 },
                {1,4,7,4,1 }
            };
            return bmp.Convolute(kernel);
            */
        }


        #endregion

        #region Import Geometry 3D
        private void importModel_Click(object sender, RoutedEventArgs e)
        {
            string name = (string)this.importModel.Header;
            if (name == "Import Model Geometry")
            {
                try
                {
                    this.AllModels.Children.Remove(this.groundModel);
                    GetNumber getNumber = new GetNumber("Field Offset Value",
                        "Enter a number to expand the territory of the walkable field to include more model geometries", UnitConversion.Convert(1, Length_Unit_Types.FEET,this._host.BIM_To_OSM.UnitType,6));
                    getNumber.Owner = this;
                    getNumber.ShowDialog();
                    double offset = getNumber.NumberValue;
                    getNumber = null;
                    this.modelGeometries = new List<GeometryModel3D>();
                    foreach (GeometryModel3D item in this._host.BIM_To_OSM.GetSlicedMeshGeometries(this._host.cellularFloor.Territory_Min, this._host.cellularFloor.Territory_Max, offset))
	                {
		                this.modelGeometries.Add(item);
	                }
                        
                    foreach (var item in this.modelGeometries)
                    {
                        this.AllModels.Children.Add(item);
                    }
                    this.importModel.Header = "Clear Model Geometry";
                    this.setModelGeometryBrush.IsEnabled = true;
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.Message);
                }
            }
            else
            {
                try
                {
                    this.AllModels.Children.Add(this.groundModel);
                    if (this.modelGeometries != null)
                    {
                        foreach (GeometryModel3D item in this.modelGeometries)
                        {
                            this.AllModels.Children.Remove(item);
                        }
                    }
                    this.modelGeometries.Clear();
                    this.modelGeometries = null;
                    this.importModel.Header = "Import Model Geometry";
                    this.setModelGeometryBrush.IsEnabled = false;
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.Message);
                }
            }
        }

        private void setModelGeometryBrush_Click(object sender, RoutedEventArgs e)
        {
            BrushPicker colorPicker = new BrushPicker(this.geometryBrush);
            colorPicker.Owner = this;
            colorPicker.ShowDialog();
            this.geometryBrush = colorPicker._Brush;
            colorPicker = null;
            this.geometryMaterial = null;
            this.geometryMaterial = new DiffuseMaterial(this.geometryBrush);
            if (this.modelGeometries != null)
            {
                foreach (GeometryModel3D item in this.modelGeometries)
                {
                    item.Material = this.geometryMaterial;
                }
            }
        } 
        #endregion










    }

}

