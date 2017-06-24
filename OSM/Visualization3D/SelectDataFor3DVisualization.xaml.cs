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
using System.Runtime.InteropServices;
using System.Windows.Interop;
using SpatialAnalysis.FieldUtility;
using SpatialAnalysis.CellularEnvironment;
using SpatialAnalysis.Data;

namespace SpatialAnalysis.Visualization3D
{
    /// <summary>
    /// Interaction logic for SelectDataFor3DVisualization.xaml
    /// </summary>
    public partial class SelectDataFor3DVisualization : Window
    {
        #region Hiding the close button
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        void SelectDataFor3DVisualization_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }
        #endregion
        /// <summary>
        /// Gets or sets a value indicating whether [visualize cost].
        /// </summary>
        /// <value><c>true</c> if [visualize cost]; otherwise, <c>false</c>.</value>
        public bool VisualizeCost { get; set; }
        /// <summary>
        /// Gets or sets the selected spatial data.
        /// </summary>
        /// <value>The selected spatial data.</value>
        public ISpatialData SelectedSpatialData { get; set; }
        private OSMDocument _host { get; set; }
        public SelectDataFor3DVisualization(OSMDocument host)
        {
            InitializeComponent();
            this._host = host;
            this.Loaded += new RoutedEventHandler(SelectDataFor3DVisualization_Loaded);
            this.dataNames.SelectionMode = SelectionMode.Single;
            this._selectedItem.Text = string.Empty;
            var cellularDataNames = new TextBlock()
            {
                Text = "Data".ToUpper(),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.DarkGray)
            };
            this.dataNames.Items.Add(cellularDataNames);
            foreach (SpatialDataField item in this._host.cellularFloor.AllSpatialDataFields.Values)
            {
                this.dataNames.Items.Add(item.Name);
            }
            if (this._host.AllActivities.Count > 0)
            {
                var fieldNames = new TextBlock()
                {
                    Text = "Activities".ToUpper(),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Colors.DarkGray)
                };
                this.dataNames.Items.Add(fieldNames);
                foreach (var item in this._host.AllActivities.Values)
                {
                    this.dataNames.Items.Add(item.Name);
                }
            }

            if (this._host.AllOccupancyEvent.Count > 0)
            {
                var eventNames = new TextBlock
                {
                    Text = "Occupancy Events".ToUpper(),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Colors.DarkGray)
                };
                this.dataNames.Items.Add(eventNames);
                foreach (var item in this._host.AllOccupancyEvent.Values)
                {
                    this.dataNames.Items.Add(item.Name);
                }
            }
            if (this._host.AllSimulationResults.Count > 0)
            {
                var results = new TextBlock
                {
                    Text = "Simulation Results".ToUpper(),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Colors.DarkGray)
                };
                this.dataNames.Items.Add(results);
                foreach (var item in this._host.AllSimulationResults.Values)
                {
                    this.dataNames.Items.Add(item.Name);
                }
            }
            this.dataNames.SelectionChanged += new SelectionChangedEventHandler(dataNames_SelectionChanged);
            this.KeyDown += new KeyEventHandler(SelectDataFor3DVisualization_KeyDown);
        }

        void SelectDataFor3DVisualization_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.Okay();
            }
            else if(e.Key == Key.Escape)
            {
                this.SelectedSpatialData = null;
                this.Close();
            }
        }
        void dataNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.dataNames.SelectedIndex == -1)
            {
                return;
            }
            try
            {
                var obj = this.dataNames.SelectedItem;
                TextBlock selected = obj as TextBlock;
                if (selected != null)
                {
                    this.dataNames.SelectedIndex = -1;
                    this._selectedItem.Text = string.Empty;
                    return;
                }
                else
                {
                    string name = (string)this.dataNames.SelectedItem;
                    if (this._host.ContainsSpatialData(name))
                    {
                        this.SelectedSpatialData = this._host.GetSpatialData(name);
                        this._selectedItem.Text = this.SelectedSpatialData.Name;
                    }
                }
                this._useCost.IsChecked = false;
                if (this.SelectedSpatialData != null && this.SelectedSpatialData.Type != DataType.SpatialData)
                {
                    this._useCost.IsEnabled = false;
                    this._useCost.IsChecked = false;
                }
                else
                {
                    this._useCost.IsChecked = false;
                    this._useCost.IsEnabled = true;
                }
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
            }
        }

        private void Okay_Click(object sender, RoutedEventArgs e)
        {
            this.Okay();
        }
        private void Okay()
        {
            if (string.IsNullOrEmpty(this._selectedItem.Text))
            {
                MessageBox.Show("Select a data field");
                return;
            }
            if (this._useCost.IsChecked == true && this._useCost.IsEnabled == true)
            {
                this.VisualizeCost = true;
            }
            else
            {
                VisualizeCost = false;
            }
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.SelectedSpatialData = null;
            this.Close();
        }
    }
}

