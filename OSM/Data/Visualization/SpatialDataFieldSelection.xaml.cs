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
using SpatialAnalysis.FieldUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SpatialAnalysis.CellularEnvironment;
using SpatialAnalysis.Data;

namespace SpatialAnalysis.Data.Visualization
{
    /// <summary>
    /// Interaction logic for SpatialDataFieldSelection.xaml
    /// </summary>
    public partial class SpatialDataFieldSelection : Window
    {
        #region Hiding the close button
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        void dataSelection_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }
        #endregion
        private OSMDocument _host { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="SpatialDataFieldSelection"/> is result.
        /// </summary>
        /// <value><c>true</c> if the data is assigned correctly; otherwise, <c>false</c>.</value>
        public bool Result { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to visualize data cost or data
        /// </summary>
        /// <value><c>true</c> if visualize cost; otherwise, <c>false</c>.</value>
       public bool VisualizeCost { get; set; }
        TextBlock cellularDataNames { get; set; }
        TextBlock fieldNames { get; set; }
        TextBlock _eventNames { get; set; }
        /// <summary>
        /// Gets or sets all selected spatial data.
        /// </summary>
        /// <value>All selected spatial data.</value>
        public List<ISpatialData> AllSelectedSpatialData { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="SpatialDataFieldSelection"/> class.
        /// </summary>
        /// <param name="host">The main document to which this class belongs.</param>
        /// <param name="title">The title.</param>
        /// <param name="allowForMultipleSelections">if set to <c>true</c> multiple selections is allowed.</param>
        public SpatialDataFieldSelection(OSMDocument host, string title, bool allowForMultipleSelections)
        {
            InitializeComponent();
            this._host = host;
            this._title.Text = title;
            if (allowForMultipleSelections)
            {
                this.dataNames.SelectionMode = SelectionMode.Multiple;
            }
            else
            {
                this.dataNames.SelectionMode = SelectionMode.Single;
            }
            this.cellularDataNames = new TextBlock()
            {
                Text = "Data".ToUpper(),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.DarkGray)
            };
            this.dataNames.Items.Add(this.cellularDataNames);
            foreach (Function item in this._host.cellularFloor.AllSpatialDataFields.Values)
            {
                SpatialDataField data = item as SpatialDataField;
                if (data != null)
                {
                    this.dataNames.Items.Add(data);
                }
            }
            this.fieldNames = new TextBlock()
            {
                Text = "Activity".ToUpper(),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.DarkGray)
            };
            if (this._host.AllActivities.Count > 0)
            {
                this.dataNames.Items.Add(this.fieldNames);
                foreach (KeyValuePair<string, Activity> item in this._host.AllActivities)
                {
                    this.dataNames.Items.Add(item.Value);
                }
            }
            this._eventNames = new TextBlock
            {
                Text = "Occupancy Events".ToUpper(),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.DarkGray)
            };
            if (this._host.AllOccupancyEvent.Count > 0)
            {
                this.dataNames.Items.Add(this._eventNames);
                foreach (SpatialAnalysis.Events.EvaluationEvent item in this._host.AllOccupancyEvent.Values)
                {
                    this.dataNames.Items.Add(item);
                }
            }
            this.dataNames.SelectionChanged += dataNames_SelectionChanged;
            this.Result = false;
            this.AllSelectedSpatialData = new List<ISpatialData>();
            this.Loaded += dataSelection_Loaded;
            this.KeyDown += new KeyEventHandler(_dataSelection_KeyDown);
        }
        void dataNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.dataNames.SelectionChanged -= dataNames_SelectionChanged;
            try
            {
                if (this.dataNames.SelectionMode == SelectionMode.Single)
                {
                    var obj = this.dataNames.SelectedItem;
                    TextBlock selected = obj as TextBlock;
                    if (selected != null)
                    {
                        this.dataNames.SelectedIndex = -1;
                    }
                }
                else if (this.dataNames.SelectionMode == SelectionMode.Multiple)
                {
                    List<object> data = new List<object>();
                    foreach (var item in this.dataNames.SelectedItems)
                    {
                        TextBlock selected = item as TextBlock;
                        if (selected == null)
                        {
                            data.Add(item);
                        }
                    }
                    this.dataNames.SelectedItems.Clear();
                    foreach (var item in data)
                    {
                        this.dataNames.SelectedItems.Add(item);
                    }
                }
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
            }
            this.dataNames.SelectionChanged += dataNames_SelectionChanged;
        }
        void _dataSelection_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                foreach (var item in this.dataNames.SelectedItems)
                {
                    ISpatialData spatialData = item as ISpatialData;
                    if (spatialData != null)
                    {
                        this.AllSelectedSpatialData.Add(spatialData);
                    }
                }
                this.Result = true;
                this.Close();
            }
            else if (e.Key == Key.Escape)
            {
                this.Result = false;
                this.Close();
            }
        }


        private void Okay_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in this.dataNames.SelectedItems)
            {
                ISpatialData spatialData = item as ISpatialData;
                if (spatialData != null)
                {
                    this.AllSelectedSpatialData.Add(spatialData);
                }
            }
            this.Result = true;
            this.Close();
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Result = false;
            this.Close();
        }
    }
}

