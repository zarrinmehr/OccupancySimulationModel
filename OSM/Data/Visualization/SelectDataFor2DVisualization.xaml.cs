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
using System.Windows.Input;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using SpatialAnalysis.FieldUtility;
using SpatialAnalysis.CellularEnvironment;
using SpatialAnalysis.Miscellaneous;

namespace SpatialAnalysis.Data.Visualization
{
    /// <summary>
    /// Interaction logic for SelectDataForVisualization.xaml
    /// </summary>
    public partial class SelectDataFor2DVisualization : Window
    {
        #region Hiding the close button
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        void SelectDataForVisualization_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }
        #endregion

        /// <summary>
        /// Gets or sets a value indicating whether to visualize cost
        /// </summary>
        /// <value><c>true</c> if cost visualized; otherwise, <c>false</c>.</value>
       public bool VisualizeCost { get; set; }
        /// <summary>
        /// Gets or sets the number.
        /// </summary>
        /// <value>The number.</value>
        public int Number { get; set; }
        /// <summary>
        /// Gets or sets the name of the data.
        /// </summary>
        /// <value>The name of the selected data.</value>
        public ISpatialData SelectedSpatialData { get; set; }
        private OSMDocument _host;
        /// <summary>
        /// Initializes a new instance of the <see cref="SelectDataFor2DVisualization"/> class.
        /// </summary>
        /// <param name="cellularFloor">The cellular floor.</param>
        /// <param name="allActivities">All activities.</param>
        /// <param name="allEvents">All events.</param>
        public SelectDataFor2DVisualization(OSMDocument host)
        {
            InitializeComponent();
            this._host = host;
            this._colorStep.Text = this._host.ColorCode.Steps.ToString();
            this.VisualizeCost = false;
            this.Loaded += new RoutedEventHandler(SelectDataForVisualization_Loaded);
            this.dataNames.SelectionMode = SelectionMode.Single;
            this._selectedItem.Text = string.Empty;
            var cellularDataNames = new TextBlock()
            { 
                Text = "Spatial Data".ToUpper(),
                FontSize = 14,
                FontWeight= FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.DarkGray)
            };
            this.dataNames.Items.Add(cellularDataNames);
            foreach (Function item in this._host.cellularFloor.AllSpatialDataFields.Values)
            {
                SpatialDataField data = item as SpatialDataField;
                if (data != null)
                {
                    this.dataNames.Items.Add(data.Name);
                }
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
            if (this._host.AllSimulationResults.Count>0)
            {
                var simulationResults = new TextBlock
                {
                    Text = "Simulation Results".ToUpper(),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Colors.DarkGray)
                };
                this.dataNames.Items.Add(simulationResults);
                foreach (var item in this._host.AllSimulationResults.Values)
                {
                    this.dataNames.Items.Add(item.Name);
                }
            }
            this.dataNames.SelectionChanged += dataNames_SelectionChanged;
            this.KeyDown += SelectDataFor2DVisualization_KeyDown;
        }

        void SelectDataFor2DVisualization_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.Okay();
            }
            else if (e.Key == Key.Escape)
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
                MessageBox.Show(error.Report());
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
            double x = 0;
            if (!double.TryParse(this._colorStep.Text, out x))
            {
                MessageBox.Show("Invalid input for 'Color Steps'");
                return;
            }
            if (x<4)
            {
                MessageBox.Show("Enter a number larger than 3 for 'Color Steps'");
                return;
            }
            this.Number = (int)x;
            if (this._host.ColorCode.Steps != this.Number)
            {
                this._host.ColorCode.SetSteps(this.Number);
            }
            
            if (this._useCost.IsChecked.Value)
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

