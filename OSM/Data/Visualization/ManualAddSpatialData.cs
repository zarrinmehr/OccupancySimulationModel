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
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Input;
using SpatialAnalysis.CellularEnvironment;
using SpatialAnalysis.Geometry;
using SpatialAnalysis.FieldUtility.Visualization;
using SpatialAnalysis.FieldUtility;
using SpatialAnalysis.Interoperability;

namespace SpatialAnalysis.Data.Visualization
{
    /// <summary>
    /// Class ManualAddDataField.
    /// </summary>
    /// <seealso cref="System.Windows.Controls.Canvas" />
    internal class ManualAddDataField : Canvas
    {
        private OSMDocument _host { get; set; }
        private List<UV> _pnts { get; set; }
        private AddDataField _addData { get; set; }
        private MenuItem visualization_Menu { get; set; }
        private double _regionVal { get; set; }
        private Line _line { get; set; }
        private Polyline _polyline { get; set; }
        private List<BarrierPolygons> _barriers { get; set; }
        private List<double> _regionValues { get; set; }
        private double _strok_thickness;
        /// <summary>
        /// Initializes a new instance of the <see cref="ManualAddDataField"/> class.
        /// </summary>
        public ManualAddDataField()
        {
            this.visualization_Menu = new MenuItem() { Header = "Polygon Method" };
            this.visualization_Menu.Click += visualization_Menu_Click;
            this._strok_thickness = 0.1d;
        }

        private void visualization_Menu_Click(object sender, RoutedEventArgs e)
        {
            this._barriers = new List<BarrierPolygons>();
            this._regionValues = new List<double>();
            this._addData = new AddDataField();
            foreach (Function item in this._host.cellularFloor.AllSpatialDataFields.Values)
            {
                if(item as SpatialDataField != null)
                    this._addData.dataNames.Items.Add(item);
            }
            if (this._host.FieldGenerator != null)
            {
                this._addData._range.Text = this._host.FieldGenerator.Range.ToString();
            }
            this._addData.createBtm.Click += done_Click;
            this._addData.addRegion.Click += addRegion_Click;
            this._addData.cancelBtm.Click += cancelBtm_Click;
            this._addData._defaultValueMethod.Checked += _defaultValueMethod_Checked;
            this._addData._interpolationMethod.Checked += _interpolationMethod_Checked;
            this._addData.Owner = this._host;
            this._addData.ShowDialog();
        }

        void _interpolationMethod_Checked(object sender, RoutedEventArgs e)
        {
            if (this._barriers.Count > 0)
            {
                var result = MessageBox.Show("Do you want to keep the existing selections?", "Fill Rule Changes", MessageBoxButton.YesNo);
                this._regionValues.Clear();
                if (result == MessageBoxResult.No)
                {
                    this._barriers.Clear();
                    this.Children.Clear();
                }
            }
        }

        void _defaultValueMethod_Checked(object sender, RoutedEventArgs e)
        {
            if (this._barriers.Count > 0)
            {
                this._barriers.Clear();
                this._regionValues.Clear();
                MessageBox.Show("The existing selextions are dismissed", "Fill Rule Changes");
                this.Children.Clear();
            }
        }

        private void addRegion_Click(object sender, RoutedEventArgs e)
        {
            if (this._addData._defaultValueMethod.IsChecked.Value)
            {
                double value = 0;
                if (!double.TryParse(this._addData.regionValue.Text, out value))
                {
                    MessageBox.Show("Enter a valied value for the region!");
                    return;
                }
                else
                {
                    this._regionVal = value;
                }
                this._addData.regionValue.Text = string.Empty;
            }
            this._addData.Hide();
            this._host.Menues.IsEnabled = false;
            this._host.UIMessage.Text = "Click to draw a region";
            this._host.UIMessage.Visibility = System.Windows.Visibility.Visible;
            this._host.Cursor = Cursors.Pen;
            this._host.FloorScene.MouseLeftButtonDown += getStartpoint;
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
                this._addData.ShowDialog();
                return;
            }
            this._pnts = new List<UV>() { p };
            this._line = new Line()
            {
                X1 = p.U,
                Y1 = p.V,
                StrokeThickness = this._strok_thickness,
                Stroke = Brushes.Green,
            };
            this.Children.Add(this._line);
            this._polyline = new Polyline() 
            { 
                Stroke = Brushes.DarkGreen, 
                StrokeThickness = this._line.StrokeThickness 
            };
            this._polyline.Points = new PointCollection() { point };
            this.Children.Add(this._polyline);
            this.registerRegionGenerationEvents();
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
        private void terminateRegionGeneration_Enter()
        {
            this.unregisterRegionGenerationEvents();
            if (this._pnts.Count>1)
            {
                Polygon polygon = new Polygon();
                polygon.Points = this._polyline.Points.CloneCurrentValue();
                polygon.Stroke = Brushes.Black;
                polygon.StrokeThickness = this._strok_thickness;
                polygon.StrokeMiterLimit = 0;
                Brush brush = Brushes.LightBlue.Clone();
                brush.Opacity=.3;
                polygon.Fill = brush;
                this.Children.Add(polygon);
                this._barriers.Add(new BarrierPolygons(this._pnts.ToArray()));
                this._regionValues.Add(this._regionVal);
            }
            this.Children.Remove(this._polyline);
            this.Children.Remove(this._line);
            this._polyline.Points.Clear();
            this._polyline = null;
            this._line = null;
            this._host.Menues.IsEnabled = true;
            this._host.UIMessage.Visibility = System.Windows.Visibility.Collapsed;
            this._host.Cursor = Cursors.Arrow;
            this._addData.ShowDialog();
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
            this._addData.ShowDialog();
        }

        private void FloorScene_MouseMove(object sender, MouseEventArgs e)
        {
            var point = this._host.InverseRenderTransform.Transform(Mouse.GetPosition(this._host.FloorScene));
            this._line.X2 = point.X;
            this._line.Y2 = point.Y;
        }

        private void done_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(this._addData._dataName.Text))
            {
                MessageBox.Show("Enter Data Field Name");
                return;
            }
            if (this._host.cellularFloor.AllSpatialDataFields.ContainsKey(this._addData._dataName.Text))
            {
                MessageBox.Show("The Data Field Name Exists. Try a Different Name...");
                return;
            }
            if (this._addData._defaultValueMethod.IsChecked.Value)
            {
                double globalValue = 0;
                if (!double.TryParse(this._addData.dataValue.Text, out globalValue))
                {
                    MessageBox.Show("Cannot Read the Default Value");
                    return;
                }
                Dictionary<Cell, double> values = new Dictionary<Cell, double>();
                foreach (Cell item in this._host.cellularFloor.Cells)
                {
                    if (item.FieldOverlapState != OverlapState.Outside)
                    {
                        values.Add(item, globalValue);
                    }
                }
                if (this._barriers != null)
                {
                    for (int i = 0; i < this._barriers.Count; i++)
                    {
                        var indices = this._host.cellularFloor.GetIndicesInsideBarrier(this._barriers[i], .0000001);
                        foreach (Index index in indices)
                        {
                            var cell = this._host.cellularFloor.FindCell(index);
                            if (cell != null)
                            {
                                if (cell.FieldOverlapState != OverlapState.Outside)
                                {
                                    if (values.ContainsKey(cell))
                                    {
                                        values[cell] = this._regionValues[i];
                                    }
                                    else
                                    {
                                        values.Add(cell, this._regionValues[i]);
                                    }
                                }
                            }
                        }
                    }
                }
                SpatialDataField newData = new SpatialDataField(this._addData._dataName.Text, values);
                this._host.cellularFloor.AddSpatialDataField(newData);
            }
            else //this._addData._interpolationMethod.IsChecked.Value == true;
            {
                if (this._barriers.Count == 0)
                {
                    MessageBox.Show("At least one region needs to be defined");
                    return;
                }
                double value = 0;
                if (!double.TryParse(this._addData.regionValue.Text, out value))
                {
                    MessageBox.Show("Enter a valid value for the region!");
                    return;
                }
                double r = 0;
                if (!double.TryParse(this._addData._range.Text, out r))
                {
                    MessageBox.Show("'Neighborhood Size' should be a number larger than 1",
                        "Missing Input", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                int range = (int)r;
                if (range < 1)
                {
                    MessageBox.Show("'Neighborhood Size' should be a number larger than 1",
                        "Missing Input", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                if (this._host.FieldGenerator == null || this._host.FieldGenerator.Range != range)
                {
                    this._host.FieldGenerator = new SpatialDataCalculator(this._host, range, OSMDocument.AbsoluteTolerance);
                }
                //test the interpolation function
                if (!this._addData.LoadFunction())
                {
                    return;
                }
                //creating heap and labeled collections
                var destinations = new HashSet<Index>();
                for (int i = 0; i < this._barriers.Count; i++)
                {
                    var indices = this._host.cellularFloor.GetIndicesInsideBarrier(this._barriers[i], OSMDocument.AbsoluteTolerance);
                    destinations.UnionWith(indices);
                }
                if (destinations.Count == 0)
                {
                    MessageBox.Show("Select destination cells for interpolation!");
                    return;
                }
                SpatialDataField newData = this._host.FieldGenerator.GetSpatialDataField(destinations, value, this._addData._dataName.Text, this._addData.InterpolationFunction);
                this._host.cellularFloor.AddSpatialDataField(newData);
            }

            this.terminateDataCreation();
        }
        private void cancelBtm_Click(object sender, RoutedEventArgs e)
        {
            this.terminateDataCreation();
        }
        private void terminateDataCreation()
        {
            this._addData.Close();
            this._addData.createBtm.Click -= done_Click;
            this._addData._defaultValueMethod.Checked -= _defaultValueMethod_Checked;
            this._addData._interpolationMethod.Checked -= _interpolationMethod_Checked;
            this._addData.cancelBtm.Click -= cancelBtm_Click;
            this._addData = null;
            this._barriers.Clear();
            this._barriers = null;
            this._regionValues.Clear();
            this._regionValues = null;
            this.Children.Clear();
        }
        /// <summary>
        /// Sets the host.
        /// </summary>
        /// <param name="host">The main document to which this instance belongs.</param>
        public void SetHost(OSMDocument host)
        {
            this._host = host;
            this.RenderTransform = this._host.RenderTransformation;
            this._host._createNewSpatialDataField.Items.Add(this.visualization_Menu);
            this._strok_thickness = UnitConversion.Convert(0.1d, Length_Unit_Types.FEET, this._host.BIM_To_OSM.UnitType);
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

