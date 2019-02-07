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
using SpatialAnalysis.Data;
using SpatialAnalysis.Agents;
using System.Windows.Threading;
using System.Threading;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using SpatialAnalysis.Data.Visualization;
using SpatialAnalysis.Agents.OptionalScenario;

namespace SpatialAnalysis.Events
{
    /// <summary>
    /// Interaction logic for OccupancyEventCapturingInterface.xaml
    /// </summary>
    public partial class EventCapturingInterface : Window
    {
        private string _trailDataFileAddress;
        #region Hiding the close button
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        //Hiding the close button
        private void OccupancyEventCapturingInterface_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }
        #endregion
        private OSMDocument _host { get; set; }
        private EvaluationEventType _eventType { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="EventCapturingInterface"/> class.
        /// </summary>
        /// <param name="host">The main document to which this window belongs.</param>
        /// <param name="eventType">Type of the event.</param>
        public EventCapturingInterface(OSMDocument host, EvaluationEventType eventType)
        {
            InitializeComponent();
            this._host = host;
            this._eventType = eventType;
            this.Loaded += OccupancyEventCapturingInterface_Loaded;
            this._dataCtlr.Click += _dataClr_Click;
            this._visibilityEvent.Click += _visibilityEvent_Click;
            this._runBtm.Click += this._runBtm_Click;
            this._closeBtm.Click += _closeBtm_Click;
            this._recordTrailBtn.Click += _recordTrailBtn_Click;
            this._timeSamplingRate.TextChanged += _timeSamplingRate_TextChanged;
            this._timeStep.TextChanged += _timeSamplingRate_TextChanged;
            this.loadRate();
            switch (eventType)
            {
                case EvaluationEventType.Optional:
                    this._activityEngamementEvnts.Visibility = System.Windows.Visibility.Collapsed;
                    break;
                case EvaluationEventType.Mandatory:
                    this._dataCtlr2.Click += _dataCtlr2_Click;
                    break;
                default:
                    break;
            }
        }

        void _dataCtlr2_Click(object sender, RoutedEventArgs e)
        {
            SpatialDataControlPanel cntrlPanel = new SpatialDataControlPanel(this._host, IncludedDataTypes.SpatialDataAndActivities);
            cntrlPanel.Owner = this._host;
            cntrlPanel.ShowDialog();
        }

        void _closeBtm_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        void loadRate()
        {
            double timeStep;
            int timeSamplingRate;
            if (!double.TryParse(_timeStep.Text, out timeStep))
            {
                MessageBox.Show("Invalid value for 'Time Step'");
                return;
            }
            if (!int.TryParse(_timeSamplingRate.Text, out timeSamplingRate))
            {
                MessageBox.Show("Invalid value for 'Time Sampling Rate'");
                return;
            }
            double pointPerSeconds = 1000d / (timeSamplingRate * timeStep);
            this._pointPerSeconds.Text = pointPerSeconds.ToString();
        }
        void _timeSamplingRate_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.loadRate();
        }

        void _recordTrailBtn_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.DefaultExt = ".txt";
            dlg.Filter = "txt documents (.txt)|*.txt";
            Nullable<bool> result = dlg.ShowDialog(this);
            this._trailDataFileAddress = "";
            if (result == true)
            {
                this._trailDataFileAddress = dlg.FileName;
            }
            else
            {
                return;
            }
        }

        void _runBtm_Click(object sender, RoutedEventArgs e)
        {
            //input validation
            #region Events validation
            #region data event
            if (this._includedataEvents.IsChecked.Value)
            {
                bool dataEventAssigned = false;
                foreach (var item in this._host.cellularFloor.AllSpatialDataFields.Values)
                {
                    SpatialDataField spatialDataField = item as SpatialDataField;
                    if (spatialDataField != null)
                    {
                        if (spatialDataField.UseToCaptureEvent)
                        {
                            dataEventAssigned = true;
                            break;
                        }
                    }
                }
                if (!dataEventAssigned)
                {
                    var result = MessageBox.Show("Do you want to ignore spatial data events?",
                        "Spatial data events are not defined", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        this._includedataEvents.IsChecked = false;
                    }
                    else
                    {
                        return;
                    }
                }
            }
            #endregion
            #region activity engagement events
            if (this._includeActivityEngagementEvents.IsChecked.Value)
            {
                bool activityEventAssigned = false;
                foreach (var item in this._host.AllActivities.Values)
                {
                    if (item.UseToCaptureEvent)
                    {
                        activityEventAssigned = true;
                        break;
                    }
                }
                if (!activityEventAssigned)
                {
                    var result = MessageBox.Show("Do you want to ignore activity engagement events?",
                        "Activity engagement events are not defined", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        this._includeActivityEngagementEvents.IsChecked = false;
                    }
                    else
                    {
                        return;
                    }
                }
            } 
            #endregion
            #region visual events
            if (this._includeVisualEvents.IsChecked.Value)
            {
                if (this._host.VisualEventSettings == null)
                {
                    var result = MessageBox.Show("Do you want to ignore visual events?",
                        "Visual events are not defined", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        this._includeVisualEvents.IsChecked = false;
                    }
                    else
                    {
                        return;
                    }
                }
            }
            #endregion

            if (!this._includeVisualEvents.IsChecked.Value && !this._includedataEvents.IsChecked.Value && !this._includeActivityEngagementEvents.IsChecked.Value)
            {
                MessageBox.Show("Cannot proceed without defining events in relation to spatial data, visibility or activity engagement!");
                return;
            } 
            #endregion
            #region numeric data validation
            double timeStep, duration;
            int timeSamplingRate;
            if (!double.TryParse(_timeStep.Text, out timeStep))
            {
                MessageBox.Show("Invalid value for 'Time Step'");
                return;
            }
            else if (timeStep <= 0)
            {
                MessageBox.Show("'Time Step' must be larger than zero");
                return;
            }
            if (!double.TryParse(_timeDuration.Text, out duration))
            {
                MessageBox.Show("Invalid value for 'Total Simulation Duration'");
                return;
            }
            else if (duration <= 0)
            {
                MessageBox.Show("'Total Simulation Duration' must be larger than zero");
                return;
            }
            if (!int.TryParse(_timeSamplingRate.Text, out timeSamplingRate))
            {
                MessageBox.Show("Invalid value for 'Time Sampling Rate'");
                return;
            }
            else if (timeSamplingRate <= 0)
            {
                MessageBox.Show("'Time Sampling Rate' must be larger than zero");
                return;
            }
            #endregion
            #region Event Name Validation

            if (string.IsNullOrEmpty(_dataFieldName.Text) || string.IsNullOrWhiteSpace(_dataFieldName.Text))
            {
                MessageBox.Show("A name is needed to save the captured event Data!",
                    "Invalid Name");
                return;
            }
            if (this._host.AllOccupancyEvent.ContainsKey(_dataFieldName.Text))
            {
                    MessageBox.Show(string.Format("'{0}' is already assigned to an exiting event Data!", _dataFieldName.Text),
                    "Invalid Name");
                return;
            }
            #endregion
            #region trail Data File Address
            if (string.IsNullOrEmpty(this._trailDataFileAddress) || string.IsNullOrWhiteSpace(this._trailDataFileAddress))
            {
                var result = MessageBox.Show("Do you want to ignore saving the trail data of events?",
                    "Invalid input", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }
            #endregion

            this._runBtm.Click -= _runBtm_Click;
            Dispatcher.Invoke(new Action(() =>
            {
                this._closeBtm.SetValue(Button.IsEnabledProperty, false);
                this._runBtm.SetValue(Button.IsEnabledProperty, false);
                this._interface.SetValue(StackPanel.IsEnabledProperty, false);
                this._progress.SetValue(Grid.VisibilityProperty, System.Windows.Visibility.Visible);
            }), DispatcherPriority.ContextIdle);
            EvaluationEvent capturedOccupancyEvent = null;
            switch (this._eventType)
            {
                case EvaluationEventType.Optional:
                    var simulator_OP = new OptionalScenarioSimulation(this._host, timeStep, duration);
                    //register report progress event
                    simulator_OP.ReportProgress += this.updateProgressBar;

                    capturedOccupancyEvent = simulator_OP.CaptureEvent(this._includedataEvents.IsChecked.Value,
                        this._includeVisualEvents.IsChecked.Value, (int)double.Parse(this._pointPerSeconds.Text),
                        _dataFieldName.Text, this._trailDataFileAddress, this._host.VisualEventSettings, this._visibilityExists.IsChecked.Value, this._analyzeFrequency.IsChecked.Value);
                    this._host.AllOccupancyEvent.Add(capturedOccupancyEvent.Name, capturedOccupancyEvent);
                    simulator_OP.ReportProgress -= this.updateProgressBar;
                    break;
                case EvaluationEventType.Mandatory:
                    if (!this._host.AgentMandatoryScenario.IsReadyForPerformance())
                    {
                        MessageBox.Show(this._host.AgentMandatoryScenario.Message, "Incomplete Scenario", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    this._host.AgentMandatoryScenario.LoadQueues(this._host.AllActivities, 0.0);
                    var simulator_MAN = new SpatialAnalysis.Agents.MandatoryScenario.MandatoryScenarioSimulation(this._host, timeStep, duration);
                    //register report progress event
                    simulator_MAN.ReportProgress += this.updateProgressBar;

                    capturedOccupancyEvent = simulator_MAN.CaptureEvent(this._includedataEvents.IsChecked.Value, this._includeVisualEvents.IsChecked.Value, 
                        this._includeActivityEngagementEvents.IsChecked.Value, (int)double.Parse(this._pointPerSeconds.Text),
                        _dataFieldName.Text, this._trailDataFileAddress, this._host.VisualEventSettings,
                        this._visibilityExists.IsChecked.Value, this._analyzeFrequency.IsChecked.Value);
                    this._host.AllOccupancyEvent.Add(capturedOccupancyEvent.Name, capturedOccupancyEvent);
                    simulator_MAN.ReportProgress -= this.updateProgressBar;
                    break;
            }
            this._closeBtm.IsEnabled = true;
        }

        void updateProgressBar(double percent)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                this._progressReport.SetValue(TextBlock.TextProperty, string.Format("%{0}", percent.ToString()));
                this._progressBar.SetValue(ProgressBar.ValueProperty, percent);
            }), DispatcherPriority.ContextIdle);
        }


        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            this._dataCtlr.Click -= _dataClr_Click;
            this._visibilityEvent.Click -= _visibilityEvent_Click;
            this._closeBtm.Click -= _closeBtm_Click;
            this._recordTrailBtn.Click -= _recordTrailBtn_Click;
            this._timeSamplingRate.TextChanged -= _timeSamplingRate_TextChanged;
            this._timeStep.TextChanged -= _timeSamplingRate_TextChanged;
            if (_eventType == EvaluationEventType.Mandatory)
            {
                this._dataCtlr2.Click -= _dataCtlr2_Click;
            }
            base.OnClosing(e);
        }

        void _visibilityEvent_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            this._host.eventSetting.SetVisualEvents(visualEventWindowClosed);
        }
        void visualEventWindowClosed(object sender, EventArgs e)
        {
            this.ShowDialog();
        }
        void _dataClr_Click(object sender, RoutedEventArgs e)
        {
            SpatialDataControlPanel cntrlPanel = new SpatialDataControlPanel(this._host, IncludedDataTypes.SpatialDataAndActivities);
            cntrlPanel.Owner = this._host;
            cntrlPanel.ShowDialog();
        }
    }
}

