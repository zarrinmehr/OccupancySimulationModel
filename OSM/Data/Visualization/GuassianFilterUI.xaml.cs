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
using SpatialAnalysis.FieldUtility;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using SpatialAnalysis.Events;
using SpatialAnalysis.Miscellaneous;

namespace SpatialAnalysis.Data.Visualization
{

    /// <summary>
    /// Class GaussianFilterUI.
    /// </summary>
    /// <seealso cref="System.Windows.Window" />
    public partial class GaussianFilterUI : Window
    {
        #region _host Definition
        private static DependencyProperty _hostProperty =
            DependencyProperty.Register("_host", typeof(OSMDocument), typeof(GaussianFilterUI),
            new FrameworkPropertyMetadata(null, _hostPropertyChanged, _propertyCoerce));
        private OSMDocument _host
        {
            get { return (OSMDocument)GetValue(_hostProperty); }
            set { SetValue(_hostProperty, value); }
        }
        private static void _hostPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            GaussianFilterUI createTrail = (GaussianFilterUI)obj;
            createTrail._host = (OSMDocument)args.NewValue;
        }
        private static object _propertyCoerce(DependencyObject obj, object value)
        {
            return value;
        }
        #endregion

        #region Hiding the close button
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        private void GuassianFilterUI_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="GaussianFilterUI"/> class.
        /// </summary>
        /// <param name="host">The main document to which this window belongs</param>
        public GaussianFilterUI(OSMDocument host)
        {
            InitializeComponent();
            this._host = host;
            this.Owner = this._host;

            TextBlock cellularDataNames = new TextBlock()
            {
                Text = "Data".ToUpper(),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.DarkGray)
            };
            this.dataNames.Items.Add(cellularDataNames);
            foreach (Function item in this._host.cellularFloor.AllSpatialDataFields.Values)
            {
                SpatialDataField data = item as SpatialDataField;
                if (data != null)
                {
                    this.dataNames.Items.Add(data);
                }
            }
            TextBlock fieldNames = new TextBlock()
            {
                Text = "Activity".ToUpper(),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.DarkGray)
            };
            if (this._host.AllActivities.Count > 0)
            {
                this.dataNames.Items.Add(fieldNames);
                foreach (KeyValuePair<string, Activity> item in this._host.AllActivities)
                {
                    this.dataNames.Items.Add(item.Value);
                }
            }
            TextBlock eventNames = new TextBlock
            {
                Text = "Occupancy Events".ToUpper(),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.DarkGray)
            };
            if (this._host.AllOccupancyEvent.Count > 0)
            {
                this.dataNames.Items.Add(eventNames);
                foreach (SpatialAnalysis.Events.EvaluationEvent item in this._host.AllOccupancyEvent.Values)
                {
                    this.dataNames.Items.Add(item);
                }
            }
            TextBlock simulationNames = new TextBlock
            {
                Text = "Simulation Results".ToUpper(),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.DarkGray)
            };
            if (this._host.AllSimulationResults.Count > 0)
            {
                this.dataNames.Items.Add(simulationNames);
                foreach (SimulationResult item in this._host.AllSimulationResults.Values)
                {
                    this.dataNames.Items.Add(item);
                }
            }
            if (this._host.ViewBasedGaussianFilter != null)
            {
                this._range.Text = this._host.ViewBasedGaussianFilter.Range.ToString();
            }
            this.Loaded += GuassianFilterUI_Loaded;
            this.dataNames.SelectionChanged += dataNames_SelectionChanged;
            this._apply.Click += _apply_Click;
            this._cancel.Click += _cancel_Click;
            this.dataNames.SelectionChanged += DataNames_SelectionChanged;
        }

        private void DataNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.dataNames.SelectedIndex != -1)
            {
                try
                {
                    this._name.Text = dataNames.SelectedItem.ToString();
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.Report());
                }
            }
            
        }

        void dataNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.dataNames.SelectionChanged -= dataNames_SelectionChanged;
            try
            {
                TextBlock selected = this.dataNames.SelectedItem as TextBlock;
                if (selected != null)
                {
                    this.dataNames.SelectedIndex = -1;
                }
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message);
            }
            this.dataNames.SelectionChanged += dataNames_SelectionChanged;
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            this.dataNames.SelectionChanged -= dataNames_SelectionChanged;
            this._apply.Click -= _apply_Click;
            this._cancel.Click -= _cancel_Click;
            this.dataNames.Items.Clear();
            this.Loaded -= GuassianFilterUI_Loaded;
            base.OnClosing(e);
        }

        void _cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        void _apply_Click(object sender, RoutedEventArgs e)
        {
            #region input validation
            double _n = 0;
            if (!double.TryParse(this._range.Text, out _n))
            {
                MessageBox.Show("'Neighborhood Size' should be a valid number larger than 0",
                    "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            int n = (int)_n;
            if (n < 1)
            {
                MessageBox.Show("'Neighborhood Size' should be a valid number larger than 0",
                    "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            if (this.dataNames.SelectedIndex == -1)
            {
                MessageBox.Show("Select a data field to continue",
                    "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            ISpatialData data = this.dataNames.SelectedItem as ISpatialData;
            if (data == null)
            {
                MessageBox.Show("Cannot cast selection to data!");
                return;
            }
            if (string.IsNullOrEmpty(this._name.Text) || string.IsNullOrWhiteSpace(this._name.Text))
            {
                MessageBox.Show("Enter a name for the new data field",
                    "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            switch (data.Type)
            {
                case DataType.SpatialData:
                    if (this._host.cellularFloor.AllSpatialDataFields.ContainsKey(this._name.Text))
                    {
                        MessageBox.Show("A spatial data field with the given name Already exists.\n Change the name to continue", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                    break;
                case DataType.ActivityPotentialField:
                    if (this._host.AllActivities.ContainsKey(this._name.Text))
                    {
                        MessageBox.Show("An activity with the given name Already exists.\n Change the name to continue", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                    break;
                case DataType.OccupancyEvent:
                    if (this._host.AllOccupancyEvent.ContainsKey(this._name.Text))
                    {
                        MessageBox.Show("An occupancy even with the given name Already exists.\n Change the name to continue", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                    break;
                case DataType.SimulationResult:
                    if (this._host.AllSimulationResults.ContainsKey(this._name.Text))
                    {
                        MessageBox.Show("An simulation result with the given name Already exists.\n Change the name to continue", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                    break;
            } 
            #endregion
            //update filter
            if (this._host.ViewBasedGaussianFilter != null)
            {
                if (this._host.ViewBasedGaussianFilter.Range != n)
                {
                    try
                    {
                        this._host.ViewBasedGaussianFilter = new IsovistClippedGaussian(this._host.cellularFloor, n);
                    }
                    catch (Exception error)
                    {
                        MessageBox.Show(error.Report(), "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }
                }
            }
            else
            {
                try
                {
                    this._host.ViewBasedGaussianFilter = new IsovistClippedGaussian(this._host.cellularFloor, n);
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.Report(), "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
            }

            try
            {
                //apply filter
                ISpatialData filteredData = this._host.ViewBasedGaussianFilter.ApplyParallel(data, this._name.Text);
                //ISpatialData filteredData = this._host.GaussianFilter.Apply(data, this._name.Text);
                //add new data to the document
                switch (filteredData.Type)
                {
                    case DataType.SpatialData:
                        this._host.cellularFloor.AddSpatialDataField(filteredData as SpatialDataField);
                        break;
                    case DataType.ActivityPotentialField:
                        this._host.AddActivity(filteredData as Activity);
                        break;
                    case DataType.OccupancyEvent:
                        this._host.AllOccupancyEvent.Add(filteredData.Name, filteredData as EvaluationEvent);
                        break;
                    case DataType.SimulationResult:
                        this._host.AllSimulationResults.Add(filteredData.Name, filteredData as SimulationResult);
                        break;
                }
            }
            catch (Exception error2)
            {
                MessageBox.Show(error2.Report(),"Exception!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            this.Close();
        }

    }
}

