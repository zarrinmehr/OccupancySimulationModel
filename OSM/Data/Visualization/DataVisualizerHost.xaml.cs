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
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using SpatialAnalysis.CellularEnvironment;
using SpatialAnalysis.Geometry;
using SpatialAnalysis.Miscellaneous;

namespace SpatialAnalysis.Data.Visualization
{
    /// <summary>
    /// Interaction logic for DataVisualizerHost.xaml
    /// </summary>
    public partial class DataVisualizerHost : UserControl
    {
        private OSMDocument _host { get; set; }
        private MenuItem visualization_Menu { get; set; }
        private WriteableBitmap _view { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataVisualizerHost"/> class.
        /// </summary>
        public DataVisualizerHost()
        {
            InitializeComponent();
            this.visualization_Menu = new MenuItem() { Header = "2D Visualization" };
            this.visualization_Menu.Click += new RoutedEventHandler(visualization_Menu_Click);
        }

        private void visualization_Menu_Click(object sender, RoutedEventArgs e)
        {
            if (this._view == null)
            {
                try
                {
                    this.draw();
                    this.visualization_Menu.Header = "Clear Data";
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.Report());
                }
            }
            else
            {
                this.clear();
                this.visualization_Menu.Header = "2D Visualization";
            }
        }


        private void draw()
        {
            SelectDataFor2DVisualization select = new SelectDataFor2DVisualization(this._host);
            select.Owner = this._host;
            select.ShowDialog();
            if (select.SelectedSpatialData == null)
            {
                //MessageBox.Show("Data noe selected");
                return;
            }
            if (select.SelectedSpatialData.Type == DataType.SpatialData)
            {
                this.drawSpatialDataField((SpatialDataField)select.SelectedSpatialData, select.Number, this._host.Transform, select.VisualizeCost);
            }
            else
            {
                this.drawAnyData(select.SelectedSpatialData, select.Number, this._host.Transform);
            }
            select = null;
        }

        private void clear()
        {
            this.Scene.Source = null;
            this._view = null;
        }
        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            this.visualization_Menu.Click -= visualization_Menu_Click;
            this.visualization_Menu = null;
            this.UnregisterName(this.Scene.Name);
            this.Scene.Source = null;
            this.Scene = null;
            this._view = null;
        }
        /// <summary>
        /// Draw field
        /// </summary>
        /// <param name="anyData">Any data.</param>
        /// <param name="colorSteps">The color steps.</param>
        /// <param name="transformation">The transformation.</param>
        private void drawAnyData(ISpatialData anyData, int colorSteps, Func<UV, Point> transformation)
        {
            if (this._host.ColorCode.Steps != colorSteps)
            {
                this._host.ColorCode.SetSteps(colorSteps);
            }
            //set the dimention of the WriteableBitmap and assign it to the scene
            double _h = ((UIElement)this.Parent).RenderSize.Height;
            double _w = ((UIElement)this.Parent).RenderSize.Width;
            this.Scene.Height = _h;
            this.Scene.Width = _w;
            this.Scene.Source = null;
            this._view = null;
            this._view = BitmapFactory.New((int)_w, (int)_h);
            this.Scene.Source = _view;
            //start to draw
            using (this._view.GetBitmapContext())
            {
                foreach (KeyValuePair<Cell, double> item in anyData.Data)
                {
                    Point p1 = transformation(item.Key);
                    Point p2 = transformation(item.Key + new UV(this._host.cellularFloor.CellSize, this._host.cellularFloor.CellSize));
                    var color = this._host.ColorCode.GetColor((item.Value - anyData.Min) / (anyData.Max - anyData.Min));
                    this._view.FillRectangle((int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y, color);
                }
            }
        }
        /// <summary>
        /// Draw a data field
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="colorSteps">The color steps.</param>
        /// <param name="transformation">The transformation.</param>
        /// <param name="visualizeCost">if set to <c>true</c> [visualize cost].</param>
        private void drawSpatialDataField(SpatialDataField data, int colorSteps, Func<UV, Point> transformation, bool visualizeCost)
        {
            if (this._host.ColorCode.Steps != colorSteps)
            {
                this._host.ColorCode.SetSteps(colorSteps);
            }
            //set the dimention of the WriteableBitmap and assign it to the scene
            double _h = ((UIElement)this.Parent).RenderSize.Height;
            double _w = ((UIElement)this.Parent).RenderSize.Width;
            this.Scene.Height = _h;
            this.Scene.Width = _w;
            this.Scene.Source = null;
            this._view = null;
            this._view = BitmapFactory.New((int)_w, (int)_h);
            this.Scene.Source = _view;
            //start to draw
            if (visualizeCost)
            {
                Dictionary<Cell, double> costs = new Dictionary<Cell, double>();
                double min = double.PositiveInfinity;
                double max = double.NegativeInfinity;
                foreach (KeyValuePair<Cell,double> item in data.Data)
                {
                    double val = data.GetCost(data.Data[item.Key]);
                    costs.Add(item.Key, val);
                    min = (min > val) ? val : min;
                    max = (max < val) ? val : max;
                }
                using (this._view.GetBitmapContext())
                {
                    foreach (Cell cell in costs.Keys)
                    {
                        Point p1 = transformation(cell);
                        Point p2 = transformation(cell + new UV(this._host.cellularFloor.CellSize, this._host.cellularFloor.CellSize));
                        var color = this._host.ColorCode.GetColor((costs[cell] - min) / (max-min));
                        this._view.FillRectangle((int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y, color);
                    }
                }
                costs.Clear();
                costs = null;
            }
            else
            {
                using (this._view.GetBitmapContext())
                {
                    foreach (Cell cell in data.Data.Keys)
                    {
                        Point p1 = transformation(cell);
                        Point p2 = transformation(cell + new UV(this._host.cellularFloor.CellSize, this._host.cellularFloor.CellSize));
                        var color = this._host.ColorCode.GetColor((data.Data[cell] - data.Min) / (data.Max - data.Min));
                        this._view.FillRectangle((int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y, color);
                    }
                }
            }

        }

        /// <summary>
        /// Sets the host.
        /// </summary>
        /// <param name="host">The main document to which this instance belongs.</param>
        public void SetHost(OSMDocument host)
        {
            this._host = host;
            this._host.DataManagement.Items.Add(this.visualization_Menu);
        }
    }

}

