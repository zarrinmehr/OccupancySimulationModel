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
using SpatialAnalysis.Events;

namespace SpatialAnalysis.Data.Visualization
{
    /// <summary>
    /// Interaction logic for SpatialDataControlPanel.xaml
    /// </summary>
    public partial class SpatialDataControlPanel : Window
    {
        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            this.Owner.WindowState = this.WindowState;
        }
        SpatialDataPropertySetting _dataPropertySetter;
        OSMDocument _host;
        /// <summary>
        /// Initializes a new instance of the <see cref="SpatialDataControlPanel"/> class.
        /// </summary>
        /// <param name="host">The main document to which this window belongs.</param>
        /// <param name="types">The types of data to include.</param>
        public SpatialDataControlPanel(OSMDocument host, IncludedDataTypes types)
	    {
            InitializeComponent();
            this._host = host;
            switch (types)
            {
                case IncludedDataTypes.SpatialData:
                    this.onlySpatialData(this._host.cellularFloor.AllSpatialDataFields);
                    break;
                case IncludedDataTypes.SpatialDataAndActivities:
                    this.SpatialDataAndActivities();
                    break;
                case IncludedDataTypes.All:
                    this.anyData();
                    break;
            }
            this._grid.SizeChanged += _grid_SizeChanged;
	    }

        void _grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this._dataPropertySetter != null)
            {
                this._dataPropertySetter.Width = this._grid.ColumnDefinitions[1].ActualWidth - 5;
                this._dataPropertySetter.Height = this._grid.RowDefinitions[0].ActualHeight - 5;
            }
        }
        private void onlySpatialData(Dictionary<string, SpatialDataField> data)
        {
            var cellularDataNames = new TextBlock()
            {
                Text = "Spatial Data".ToUpper(),
                FontSize = 13,
                FontWeight = FontWeights.DemiBold,
            };
            this._dataNames.Items.Add(cellularDataNames);
            foreach (var item in data.Values)
            {
                SpatialDataField spatialData = item as SpatialDataField;
                if (spatialData != null)
                {
                    this._dataNames.Items.Add(spatialData);
                }
            }
            this._dataNames.SelectionChanged += new SelectionChangedEventHandler(_dataNames_SelectionChanged);
        }
        private void anyData()
        {
            var cellularDataNames = new TextBlock()
            {
                Text = "Spatial Data".ToUpper(),
                FontSize = 13,
                FontWeight = FontWeights.DemiBold,
            };
            this._dataNames.Items.Add(cellularDataNames);
            foreach (var item in this._host.cellularFloor.AllSpatialDataFields.Values)
            {
                SpatialDataField spatialData = item as SpatialDataField;
                if (spatialData != null)
                {
                    this._dataNames.Items.Add(spatialData);
                }
            }
            if (this._host.AllActivities.Count > 0)
            {
                var fieldNames = new TextBlock()
                {
                    Text = "Activity".ToUpper(),
                    FontSize = 13,
                    FontWeight = FontWeights.DemiBold,
                };
                this._dataNames.Items.Add(fieldNames);
                foreach (var item in this._host.AllActivities.Values)
                {
                    this._dataNames.Items.Add(item);
                }
            }
            if (this._host.AllOccupancyEvent.Count > 0)
            {
                var eventNames = new TextBlock
                {
                    Text = "Occupancy Events".ToUpper(),
                    FontSize = 13,
                    FontWeight = FontWeights.DemiBold,
                };
                this._dataNames.Items.Add(eventNames);
                foreach (var item in this._host.AllOccupancyEvent.Values)
                {
                    this._dataNames.Items.Add(item);
                }
            }
            if (this._host.AllSimulationResults.Count>0)
            {
                 var simulationResults = new TextBlock
                {
                    Text = "Simulation Results".ToUpper(),
                    FontSize = 13,
                    FontWeight = FontWeights.DemiBold,
                };
                 this._dataNames.Items.Add(simulationResults);
                 foreach (var item in this._host.AllSimulationResults.Values)
                 {
                     this._dataNames.Items.Add(item);
                 }
            }
            this._dataNames.SelectionChanged += new SelectionChangedEventHandler(_dataNames_SelectionChanged);
        }
        private void SpatialDataAndActivities()
        {
            var cellularDataNames = new TextBlock()
            {
                Text = "Spatial Data".ToUpper(),
                FontSize = 13,
                FontWeight = FontWeights.DemiBold,
            };
            this._dataNames.Items.Add(cellularDataNames);
            foreach (var item in this._host.cellularFloor.AllSpatialDataFields.Values)
            {
                SpatialDataField spatialData = item as SpatialDataField;
                if (spatialData != null)
                {
                    this._dataNames.Items.Add(spatialData);
                }
            }
            if (this._host.AllActivities.Count > 0)
            {
                var fieldNames = new TextBlock()
                {
                    Text = "Activity".ToUpper(),
                    FontSize = 13,
                    FontWeight = FontWeights.DemiBold,
                };
                this._dataNames.Items.Add(fieldNames);
                foreach (var item in this._host.AllActivities.Values)
                {
                    this._dataNames.Items.Add(item);
                }
            }
            this._dataNames.SelectionChanged += new SelectionChangedEventHandler(_dataNames_SelectionChanged);
        }
        void _dataNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ISpatialData spatialDataField = ((ListBox)sender).SelectedItem as ISpatialData;
            if (spatialDataField == null)
            {
                ((ListBox)sender).SelectedIndex = -1;
                return;
            }
            if (this._dataPropertySetter != null)
            {
                if (this._grid.Children.Contains(this._dataPropertySetter))
                {
                    this._grid.Children.Remove(this._dataPropertySetter);
                    this._dataPropertySetter = null;
                }
            }
            this._dataPropertySetter = new SpatialDataPropertySetting(this._host,spatialDataField);
            this._grid.Children.Add(this._dataPropertySetter);
            Grid.SetColumn(this._dataPropertySetter, 1);
            Grid.SetRow(this._dataPropertySetter, 0);
            this._dataPropertySetter.Width = this._grid.ColumnDefinitions[1].ActualWidth - 5;
            this._dataPropertySetter.Height = this._grid.RowDefinitions[0].ActualHeight - 5;
        }

        private void _close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        
    }
    /// <summary>
    /// Enum Included Data Types in the data control panel
    /// </summary>
    public enum IncludedDataTypes
    {
        /// <summary>
        /// The spatial data
        /// </summary>
        SpatialData = 0,
        /// <summary>
        /// The spatial data and activities
        /// </summary>
        SpatialDataAndActivities = 1,
        /// <summary>
        /// All
        /// </summary>
        All = 2,
    }
}

