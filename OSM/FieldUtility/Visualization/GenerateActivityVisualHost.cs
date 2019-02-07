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
using System.Windows.Controls;
using SpatialAnalysis.Geometry;
using System.Windows.Shapes;
using SpatialAnalysis.CellularEnvironment;
using System.Windows.Input;
using System.Windows;
using SpatialAnalysis.Data;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using SpatialAnalysis.Data.Visualization;
using SpatialAnalysis.FieldUtility;
using SpatialAnalysis.Visualization;
using System.Windows.Media.Imaging;
using SpatialAnalysis.Miscellaneous;
using SpatialAnalysis.Interoperability;

namespace SpatialAnalysis.FieldUtility.Visualization
{
    /// <summary>
    /// Class GenerateActivityVisualHost.
    /// </summary>
    /// <seealso cref="System.Windows.Controls.Canvas" />
    internal class GenerateActivityVisualHost: Canvas
    {
        #region _host Definition
        private static DependencyProperty _hostProperty =
            DependencyProperty.Register("_host", typeof(OSMDocument), typeof(GenerateActivityVisualHost),
            new FrameworkPropertyMetadata(null, _hostPropertyChanged, _propertyCoerce));
        private OSMDocument _host
        {
            get { return (OSMDocument)GetValue(_hostProperty); }
            set { SetValue(_hostProperty, value); }
        }
        private static void _hostPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            GenerateActivityVisualHost createTrail = (GenerateActivityVisualHost)obj;
            createTrail._host = (OSMDocument)args.NewValue;
        }
        private static object _propertyCoerce(DependencyObject obj, object value)
        {
            return value;
        }
        #endregion

        private Line _directionline { get; set; }
        private List<UV> _pnts { get; set; }
        private MenuItem visualization_Menu { get; set; }
        private MenuItem _editeActivity_Menu { get; set; }
        private Line _line { get; set; }
        private Polyline _polyline { get; set; }
        BarrierPolygons _barrier { get; set; }
        private GenerateFields _fieldGenerator { get; set; }
        private HashSet<Cell> _destinations { get; set; }
        //private double _angularDeviationWeight = 1.0d;
        private StateBase _stateBase { get; set; }

        private double _stroke_thickness;
        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateActivityVisualHost"/> class.
        /// </summary>
        public GenerateActivityVisualHost()
        {
            this.visualization_Menu = new MenuItem() { Header = "Create a New Activity" };
            this.visualization_Menu.Click += visualization_Menu_Click;
            this._editeActivity_Menu = new MenuItem() { Header = "Edit, Save, Load Activities" };
            this._editeActivity_Menu.Click += _editeActivity_Menu_Click;
            this._stroke_thickness = 0.1d;
        }

        void _editeActivity_Menu_Click(object sender, RoutedEventArgs e)
        {
            EditActivitiesUI editor = new EditActivitiesUI(this._host);
            editor.Owner = this._host;
            editor.ShowInTaskbar = false;
            editor.ShowDialog();
            editor = null;
        }

        private void visualization_Menu_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this._host.activityAreaVisualHost.DrawAllActivities();
            this._fieldGenerator = new GenerateFields();
            this._fieldGenerator._fieldName.Text = "Activity " + (this._host.AllActivities.Count + 1).ToString();
            this._fieldGenerator._fieldName.Focus();
            this._fieldGenerator._fieldName.Select(0, this._fieldGenerator._fieldName.Text.Length);
            if (this._host.FieldGenerator != null)
            {
                this._fieldGenerator._range.Text = this._host.FieldGenerator.Range.ToString();
            }
            this._fieldGenerator.Loaded += _fieldGenerator_Loaded;
            this._fieldGenerator._addPoint.Click += _addPoint_Click;
            this._fieldGenerator._addRegion.Click += _addRegion_Click;
            this._fieldGenerator._addDirection.Click += _addDirection_Click;
            this._fieldGenerator._parameters.Click += _parameters_Click;
            this._destinations = new HashSet<Cell>();
            this._fieldGenerator.Owner = this._host;
            this._fieldGenerator.Closed += _fieldGenerator_Closed;
            this._fieldGenerator.ContextMenu = new ContextMenu();
            var menue = new MenuItem() { Header = "Save Background" };
            menue.Click += menue_Click;
            this._fieldGenerator.ContextMenu.Items.Add(menue);
            this._fieldGenerator.ShowDialog();
        }

        void menue_Click(object sender, RoutedEventArgs e)
        {
            this._host.activityAreaVisualHost.Save();
        }

        void _fieldGenerator_Closed(object sender, EventArgs e)
        {
            this._fieldGenerator.Closed -= _fieldGenerator_Closed;
            ((MenuItem)this._fieldGenerator.ContextMenu.Items[0]).Click -= menue_Click;
            this._fieldGenerator.ContextMenu.Items.Clear();
            this._fieldGenerator.ContextMenu = null;
            this._host.activityAreaVisualHost.Clear();
        }



        void _parameters_Click(object sender, RoutedEventArgs e)
        {
            ParameterSetting paramSetting = new ParameterSetting(this._host, false);
            paramSetting.Owner = this._host;
            paramSetting.ShowDialog();
            paramSetting = null;
        }

        void _addDirection_Click(object sender, RoutedEventArgs e)
        {
            this._fieldGenerator.Hide();
            if (this._directionline != null)
            {
                if (this.Children.Contains(this._directionline))
                {
                    this.Children.Remove(this._directionline);
                }
                this._directionline = null;
            }
            this._directionline = new Line
            {
                X1 = this._stateBase.Location.U,
                Y1 = this._stateBase.Location.V,
                StrokeThickness = this._stroke_thickness,
                Stroke = System.Windows.Media.Brushes.Green,
            };
            this.Children.Add(this._directionline);
            this._host.FloorScene.MouseLeftButtonDown += directionSetTerminate;
            this._host.FloorScene.MouseMove += directionSet;
        }

        void directionSet(object sender, MouseEventArgs e)
        {
            var point = this._host.InverseRenderTransform.Transform(Mouse.GetPosition(this._host.FloorScene));
            UV p = new UV(point.X, point.Y);
            this._directionline.X2 = point.X;
            this._directionline.Y2 = point.Y;
        }

        void directionSetTerminate(object sender, MouseButtonEventArgs e)
        {
            this._host.FloorScene.MouseLeftButtonDown -= directionSetTerminate;
            this._host.FloorScene.MouseMove -= directionSet;
            this._stateBase.Direction = new UV(this._directionline.X2 - this._directionline.X1, this._directionline.Y2 - this._directionline.Y1);
            this._stateBase.Direction.Unitize();
            this._fieldGenerator.ShowDialog();
        }

        private void _addPoint_Click(object sender, RoutedEventArgs e)
        {
            this._fieldGenerator.Hide();
            this._destinations.Clear();
            this.Children.Clear();
            this._barrier = null;
            this._host.Menues.IsEnabled = false;
            this._host.UIMessage.Text = "Click to pick a cell";
            this._host.UIMessage.Visibility = System.Windows.Visibility.Visible;
            this._host.Cursor = Cursors.Pen;
            this._host.FloorScene.MouseLeftButtonDown += pickCell;
        }

        private void pickCell(object sender, MouseButtonEventArgs e)
        {
            this._host.FloorScene.MouseLeftButtonDown -= pickCell;
            var point = this._host.InverseRenderTransform.Transform(Mouse.GetPosition(this._host.FloorScene));
            UV p = new UV(point.X, point.Y);
            Cell cell = this._host.cellularFloor.FindCell(p);
            if (cell == null || cell.FieldOverlapState != OverlapState.Inside)
            {
                MessageBox.Show("Pick a point on the walkable field and try again!\n");
                this._fieldGenerator._addDirection.IsEnabled = false;
            }
            else
            {
                this._destinations.Add(cell);
                var lines = cell.ToUVLines(this._host.cellularFloor.CellSize);
                Polygon polygon = new Polygon();
                polygon.Stroke = Brushes.Black;
                polygon.StrokeThickness = this._stroke_thickness;
                polygon.StrokeMiterLimit = 0;
                Brush brush = Brushes.LightBlue.Clone();
                brush.Opacity = .3;
                polygon.Fill = brush;
                polygon.Points.Add(new Point(cell.U, cell.V));
                polygon.Points.Add(new Point(cell.U + this._host.cellularFloor.CellSize, cell.V));
                polygon.Points.Add(new Point(cell.U + this._host.cellularFloor.CellSize, cell.V + this._host.cellularFloor.CellSize));
                polygon.Points.Add(new Point(cell.U, cell.V + this._host.cellularFloor.CellSize));
                this._barrier = new BarrierPolygons(
                    new UV[4] 
                    {
                        new UV(cell.U, cell.V),
                        new UV(cell.U + this._host.cellularFloor.CellSize, cell.V),
                        new UV(cell.U + this._host.cellularFloor.CellSize, cell.V + this._host.cellularFloor.CellSize),
                        new UV(cell.U, cell.V + this._host.cellularFloor.CellSize)
                    }
                );
                this.Children.Add(polygon);
                this._stateBase = new StateBase(cell + new UV(this._host.cellularFloor.CellSize / 2, this._host.cellularFloor.CellSize / 2), new UV(0, 0));
                this._fieldGenerator._addDirection.IsEnabled = true;
            }
            this._host.Menues.IsEnabled = true;
            this._host.UIMessage.Visibility = System.Windows.Visibility.Collapsed;
            this._host.Cursor = Cursors.Arrow;
            this._fieldGenerator.ShowDialog();
        }

        private void _addRegion_Click(object sender, RoutedEventArgs e)
        {
            this._fieldGenerator.Hide();
            this._barrier = null;
            this._stateBase = null;
            this._fieldGenerator._addDirection.IsEnabled = false;
            this.Children.Clear(); 
            this._host.Menues.IsEnabled = false;
            this._host.UIMessage.Text = "Click to draw a region";
            this._host.UIMessage.Visibility = System.Windows.Visibility.Visible;
            this._host.Cursor = Cursors.Pen;
            this._host.FloorScene.MouseLeftButtonDown += getStartpoint;
        }

        private void getStartpoint(object sender, MouseButtonEventArgs e)
        {
            this._host.FloorScene.MouseLeftButtonDown -= getStartpoint;
            var point = this._host.InverseRenderTransform.Transform(Mouse.GetPosition(this._host.FloorScene));
            UV p = new UV(point.X, point.Y);
            Cell cell = this._host.cellularFloor.FindCell(p);
            if (cell == null)
            {
                MessageBox.Show("Pick a point on the walkable field and try again!\n");
                this._host.Menues.IsEnabled = true;
                this._host.UIMessage.Visibility = System.Windows.Visibility.Collapsed;
                this._host.Cursor = Cursors.Arrow;
                this._fieldGenerator.ShowDialog();
                return;
            }
            this._pnts = new List<UV>() { p };
            this._line = new Line()
            {
                X1 = p.U,
                Y1 = p.V,
                StrokeThickness = this._stroke_thickness,
                Stroke = System.Windows.Media.Brushes.Green,
            };
            this.Children.Add(this._line);
            this._polyline = new Polyline()
            {
                Stroke = System.Windows.Media.Brushes.DarkGreen,
                StrokeThickness = this._line.StrokeThickness
            };
            this._polyline.Points = new PointCollection() { point };
            this.Children.Add(this._polyline);
            this.registerRegionGenerationEvents();
        }

        private void terminateRegionGeneration_Enter()
        {
            this.unregisterRegionGenerationEvents();
            if (this._pnts.Count > 1)
            {
                Polygon polygon = new Polygon();
                polygon.Points = this._polyline.Points.CloneCurrentValue();
                polygon.Stroke = Brushes.Black;
                polygon.StrokeThickness = this._stroke_thickness;
                polygon.StrokeMiterLimit = 0;
                Brush brush = Brushes.LightBlue.Clone();
                brush.Opacity = .3;
                polygon.Fill = brush;
                this.Children.Add(polygon);
                this._barrier = new BarrierPolygons(this._pnts.ToArray());
                if (!this._barrier.IsConvex())
                {
                    MessageBox.Show("The destination region should be a convex polygon. \nTry again...",
                        "Concavity Problem", MessageBoxButton.OK, MessageBoxImage.Information);
                    this._barrier = null;
                }
                else
                {
                    this._stateBase = new StateBase(this._barrier.GetCenter(), new UV(0, 0));
                    this._fieldGenerator._addDirection.IsEnabled = true;
                }
            }
            this.Children.Remove(this._polyline);
            this.Children.Remove(this._line);
            this._polyline.Points.Clear();
            this._polyline = null;
            this._line = null;
            this._host.Menues.IsEnabled = true;
            this._host.UIMessage.Visibility = System.Windows.Visibility.Collapsed;
            this._host.Cursor = Cursors.Arrow;
            this._fieldGenerator.ShowDialog();
        }

        private void terminateRegionGeneration_Cancel()
        {
            this.unregisterRegionGenerationEvents();
            this.Children.Remove(this._polyline);
            this.Children.Remove(this._line);
            this._polyline.Points.Clear();
            this._polyline = null;
            this._line = null;
            this._host.Menues.IsEnabled = true;
            this._host.UIMessage.Visibility = System.Windows.Visibility.Collapsed;
            this._host.Cursor = Cursors.Arrow;
            this._fieldGenerator.ShowDialog();
        }

        private void registerRegionGenerationEvents()
        {
            this._host.MouseBtn.MouseDown += MouseBtn_MouseDown;
            this._host.KeyDown += regionTermination_KeyDown;
            this._host.FloorScene.MouseMove += FloorScene_MouseMove;
            this._host.FloorScene.MouseLeftButtonDown += getNextRegionPoint;
        }

        private void unregisterRegionGenerationEvents()
        {
            this._host.MouseBtn.MouseDown -= MouseBtn_MouseDown;
            this._host.KeyDown -= regionTermination_KeyDown;
            this._host.FloorScene.MouseMove -= FloorScene_MouseMove;
            this._host.FloorScene.MouseLeftButtonDown -= getNextRegionPoint;
        }

        private void MouseBtn_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.terminateRegionGeneration_Enter();
        }

        private void regionTermination_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.terminateRegionGeneration_Enter();
            }
            if (e.Key == Key.Escape)
            {
                this.terminateRegionGeneration_Cancel();
            }
        }

        private void FloorScene_MouseMove(object sender, MouseEventArgs e)
        {
            var point = this._host.InverseRenderTransform.Transform(Mouse.GetPosition(this._host.FloorScene));
            this._line.X2 = point.X;
            this._line.Y2 = point.Y;
        }

        private void getNextRegionPoint(object sender, MouseButtonEventArgs e)
        {
            var point = this._host.InverseRenderTransform.Transform(Mouse.GetPosition(this._host.FloorScene));
            UV p = new UV(point.X, point.Y);
            Cell cell = this._host.cellularFloor.FindCell(p);
            if (cell == null)
            {
                MessageBox.Show("Pick a point on the walkable field and try again!\n");
                return;
            }
            this._host.UIMessage.Text = "Click to continue; Press enter to close the region; Press escape to abort the region";
            this._line.X1 = point.X;
            this._line.Y1 = point.Y;
            this._pnts.Add(p);
            this._polyline.Points.Add(point);
        }

        #region Hiding the close button
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        #endregion 
        private void _fieldGenerator_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            //Hiding the close button
            var hwnd = new WindowInteropHelper(this._fieldGenerator).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
            #region Stuff reralted to the interface
            foreach (string item in this._host.AllActivities.Keys)
            {
                this._fieldGenerator._availableFields.Items.Add(item);
            }
            this._fieldGenerator.KeyDown += new KeyEventHandler(GenerateFields_KeyDown);
            this._fieldGenerator.OKAY.Click += new RoutedEventHandler(OKAY_Click);
            this._fieldGenerator.CANCEL.Click += new RoutedEventHandler(CANCEL_Click);
            this._fieldGenerator._setCosts.Click += new RoutedEventHandler(_setCosts_Click);
            #endregion
        }

        void _setCosts_Click(object sender, RoutedEventArgs e)
        {
            var controlPanel = new SpatialDataControlPanel(this._host, IncludedDataTypes.SpatialData);
            controlPanel.Owner = this._host;
            controlPanel.ShowDialog();
        }


        private void OKAY_Click(object sender, RoutedEventArgs e)
        {
            this.OKAY();
        }

        private void GenerateFields_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.OKAY();
            }
            else if (e.Key == Key.Escape)
            {
                this._fieldGenerator.Close();
            }
        }

        private void CANCEL_Click(object sender, RoutedEventArgs e)
        {
            this.terminateDataCreation();
        }

        private void OKAY()
        {
            if (string.IsNullOrEmpty(this._fieldGenerator._fieldName.Text) || string.IsNullOrWhiteSpace(this._fieldGenerator._fieldName.Text))
            {
                MessageBox.Show("Enter a name for the new activity", "Activity Name",
                     MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (this._host.AllActivities.ContainsKey(this._fieldGenerator._fieldName.Text))
            {
                MessageBox.Show("An activity with the same name exists!", "Activity Name",
                     MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            double min = 0;
            if (!double.TryParse(this._fieldGenerator._min.Text, out min))
            {
                MessageBox.Show("Minimum activity engagement time is invalid!", "Activity Engagement Time",
                     MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (min<=0)
            {
                MessageBox.Show("Minimum activity engagement time should be larger than zero!", "Activity Engagement Time",
                     MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            double max = 0;
            if (!double.TryParse(this._fieldGenerator._max.Text, out max))
            {
                MessageBox.Show("Maximum activity engagement time is invalid!", "Activity Engagement Time",
                     MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (max <= min)
            {
                MessageBox.Show("Maximum activity engagement time should be larger than maximum activity engagement time!", "Activity Engagement Time",
                     MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (this._barrier == null && this._destinations.Count == 0)
            {
                MessageBox.Show("Set destination to continue!",
                    "Missing Destinations", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (this._destinations.Count == 0)
            {
                var indices = this._host.cellularFloor.GetIndicesInsideBarrier(this._barrier, 0.0000001);
                if (indices.Count > 0)
                {
                    foreach (var index in indices)
                    {
                        Cell cell = this._host.cellularFloor.FindCell(index);
                        if (cell != null && cell.FieldOverlapState == OverlapState.Inside)
                        {
                            this._destinations.Add(cell);
                        }
                    }
                }
            }

            if (this._destinations.Count==0)
            {
                MessageBox.Show("No destination is set for creating this new Activity on the walkable field", 
                    "Missing Input", MessageBoxButton.OK,    MessageBoxImage.Information);
                return;
            }
            if (this._stateBase.Direction== UV.ZeroBase)
            {
                    MessageBox.Show("The direction for the agent at the destination is not set!", 
                    "Missing Input", MessageBoxButton.OK,    MessageBoxImage.Information);
                return;
            }
            double r = 0;
            if (!double.TryParse(this._fieldGenerator._range.Text,out r))
            {
                MessageBox.Show("'Neighborhood Size' should be a number larger than 1", 
                    "Missing Input", MessageBoxButton.OK,    MessageBoxImage.Information);
                return;
            }
            int range = (int)r;
            if (range<1)
            {
                MessageBox.Show("'Neighborhood Size' should be a number larger than 1", 
                    "Missing Input", MessageBoxButton.OK,    MessageBoxImage.Information);
                return;
            }
            if (this._host.FieldGenerator == null || this._host.FieldGenerator.Range != range)
            {
                this._host.FieldGenerator = new SpatialDataCalculator(this._host, range, OSMDocument.AbsoluteTolerance);
            }
            try
            {
                Activity newField = null;
                
                if (this._fieldGenerator._includeAngularCost.IsChecked.Value)
                {
                    //double angularVelocityWeight = 0;
                    if (Parameter.DefaultParameters[AgentParameters.MAN_AngularDeviationCost].Value<=0)
                    {
                        MessageBox.Show("Cost of angular change should be exclusively larger than zero", "Activity Generation",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    newField = this._host.FieldGenerator.GetDynamicActivity(this._destinations, this._barrier, this._stateBase, this._fieldGenerator._fieldName.Text, 
                        Parameter.DefaultParameters[AgentParameters.MAN_AngularDeviationCost].Value);
                }
                else
                {
                    newField = this._host.FieldGenerator.GetStaticActivity(this._destinations, this._barrier, this._stateBase, this._fieldGenerator._fieldName.Text);
                }
                if (!newField.TrySetEngagementTime(min, max))
                {
                    MessageBox.Show("Cannot Set activity engagement time", "Activity Engagement Time", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                this._host.AddActivity(newField);
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Report());
            }
            
            this.terminateDataCreation();
        }

        private void terminateDataCreation()
        {
            // unregister all of the events and make all resources null
            this._fieldGenerator.Close();
            this._fieldGenerator._setCosts.Click -= this._setCosts_Click;
            this._fieldGenerator.KeyDown -= GenerateFields_KeyDown;
            this._fieldGenerator.OKAY.Click -= OKAY_Click;
            this._fieldGenerator.CANCEL.Click -= CANCEL_Click;
            this._fieldGenerator.Loaded -= _fieldGenerator_Loaded;
            this._fieldGenerator._availableFields.Items.Clear();
            this._fieldGenerator._addRegion.Click -= _addRegion_Click;
            this._fieldGenerator._addDirection.Click -= _addDirection_Click;
            this._fieldGenerator._addPoint.Click -= _addPoint_Click;
            this._fieldGenerator = null;
            this.Children.Clear();
            this._barrier = null;
            this._destinations.Clear();
            this._destinations = null;
            this._line = null;
            this._polyline = null;
            this._directionline = null;
        }
        /// <summary>
        /// Sets the host.
        /// </summary>
        /// <param name="host">The main document to which this control belongs.</param>
        public void SetHost(OSMDocument host)
        {
            this._host = host;
            this._stroke_thickness = UnitConversion.Convert(0.1, Length_Unit_Types.FEET, this._host.BIM_To_OSM.UnitType);
            this.RenderTransform = this._host.RenderTransformation;
            this._host._activities.Items.Insert(0,this._editeActivity_Menu);
            this._host._activities.Items.Insert(0, this.visualization_Menu);
        }
        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            this._host = null;
            this.visualization_Menu.Click -= this.visualization_Menu_Click;
            this.visualization_Menu = null;
        }

    }
}

