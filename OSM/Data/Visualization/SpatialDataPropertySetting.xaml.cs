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
using System.Windows.Navigation;
using System.Windows.Shapes;
using SpatialAnalysis.Data;
using SpatialAnalysis.Data.CostFormulaSet;
using SpatialAnalysis.Events;
using SpatialAnalysis.FieldUtility;
using SpatialAnalysis.Visualization;
using SpatialAnalysis.Miscellaneous;

namespace SpatialAnalysis.Data.Visualization
{
    /// <summary>
    /// Interaction logic for SpatialDataEventCaptureSetting.xaml
    /// </summary>
    public partial class SpatialDataPropertySetting : UserControl
    {
        private const string format = "0.00000";
        private const string percent = "0.00%";
        OSMDocument _host;
        SpatialDataField _spatialDataField;
        ISpatialData _dataField;
        /// <summary>
        /// Initializes a new instance of the <see cref="SpatialDataPropertySetting"/> class.
        /// </summary>
        /// <param name="host">The main document to which this control belongs.</param>
        /// <param name="dataField">The data field.</param>
        public SpatialDataPropertySetting(OSMDocument host, ISpatialData dataField)
        {
            InitializeComponent();
            this._host = host;
            this._dataField = dataField;
            this._spatialDataField = dataField as SpatialDataField;
            if (this._spatialDataField != null)
            {
                this._eventCase.Visibility = System.Windows.Visibility.Collapsed;
                this._includeCost.IsChecked = this._spatialDataField.UseCostFunctionForEventCapturing;
                this._includeCost.Checked += _includeCost_Checked;
                this._includeCost.Unchecked += _includeCost_Unchecked;
                if (this._spatialDataField.EventCapturingInterval != null)
                {
                    this._intervalMax.Text = this._spatialDataField.EventCapturingInterval.Maximum.ToString(format);
                    this._intervalMin.Text = this._spatialDataField.EventCapturingInterval.Minimum.ToString(format);
                    this._capture.IsChecked = this._spatialDataField.UseToCaptureEvent;
                    this._outsideInterval.IsChecked = this._spatialDataField.CaptureEventWhenOutsideInterval;
                }
                else
                {
                    this._capture.IsChecked = false;
                    this._capture.IsEnabled = false;
                    this._outsideInterval.IsChecked = false;
                    this._outsideInterval.IsEnabled = false;
                }

                this._intervalMin.TextChanged += _intervalMin_TextChanged;
                this._intervalMax.TextChanged += _intervalMax_TextChanged;
                this._capture.Checked += _capture_Checked;
                this._capture.Unchecked += _capture_Unchecked;
                this._outsideInterval.Checked += _outsideInterval_Checked;
                this._outsideInterval.Unchecked += _outsideInterval_Unchecked;

                this._method.Items.Add(CostCalculationMethod.RawValue);
                this._method.Items.Add(CostCalculationMethod.WrittenFormula);
                this._method.Items.Add(CostCalculationMethod.Interpolation);
                if (this._spatialDataField.HasBuiltInRepulsion)
                {
                    this._method.Items.Add(CostCalculationMethod.BuiltInRepulsion);
                }

                this._method.SelectedItem = this._spatialDataField.CostCalculationType;
                this._method.SelectionChanged += new SelectionChangedEventHandler(_method_SelectionChanged);
                this._include.IsChecked = this._spatialDataField.IncludeInActivityGeneration;
                this._vis.Click += new RoutedEventHandler(_vis_Click);
            }
            else
            {
                this._desirability.Visibility = System.Windows.Visibility.Collapsed;
                this._setDataCostProperties.Visibility = System.Windows.Visibility.Collapsed;
            }

            switch (this._dataField.Type)
            {
                case DataType.SpatialData:
                    this._signal.Visibility = System.Windows.Visibility.Collapsed;
                    this._activityTimePeriod.Visibility = System.Windows.Visibility.Collapsed;
                    this._activityEngagementEvent.Visibility = System.Windows.Visibility.Collapsed;
                    this._simulationResult.Visibility = System.Windows.Visibility.Collapsed;
                    this._dataType.Text = "Spatial Data Field";
                    break;
                case DataType.ActivityPotentialField:
                    this._signal.Visibility = System.Windows.Visibility.Collapsed;
                    this._simulationResult.Visibility = System.Windows.Visibility.Collapsed;
                    this._eventCase.Visibility = System.Windows.Visibility.Collapsed;
                    this._dataType.Text = "Activity";
                    this._maximumEngagementTime.TextChanged += new TextChangedEventHandler(activityEngagementTime_TextChanged);
                    this._maximumEngagementTime.Text = ((Activity)this._dataField).MaximumEngagementTime.ToString(format);
                    this._minimumEngagementTime.TextChanged += new TextChangedEventHandler(activityEngagementTime_TextChanged);
                    this._minimumEngagementTime.Text = ((Activity)this._dataField).MinimumEngagementTime.ToString(format);
                    this._captureActivityEvent.IsChecked = ((Activity)this._dataField).UseToCaptureEvent;
                    this._captureActivityEvent.Checked += _captureActivityEvent_Checked;
                    this._captureActivityEvent.Unchecked += _captureActivityEvent_Unchecked;
                    break;
                case DataType.OccupancyEvent:
                    this._simulationResult.Visibility = System.Windows.Visibility.Collapsed;
                    this._activityTimePeriod.Visibility = System.Windows.Visibility.Collapsed;
                    this._activityEngagementEvent.Visibility = System.Windows.Visibility.Collapsed;
                    EvaluationEvent occupancyEvent = this._dataField as EvaluationEvent;
                    switch (occupancyEvent.EventType)
                    {
                        case EvaluationEventType.Optional:
                            this._dataType.Text = "Optional Occupancy Event";
                            this._mandatoryEvntInfo.Visibility = System.Windows.Visibility.Collapsed;
                            break;
                        case EvaluationEventType.Mandatory:
                            this._dataType.Text = "Mandatory Occupancy Event";
                            this._hasActivityEvents.IsChecked = ((MandatoryEvaluationEvent)occupancyEvent).HasActivityEngagementEvent;
                            break;
                    }
                    if (occupancyEvent.HasFrequencies)
                    {
                        try
                        {
                            this.Loaded += drawFrequencies;
                        }
                        catch (Exception error)
                        {
                            MessageBox.Show(error.Report());
                        }
                    }
                    else
                    {
                        this._signal.Visibility = System.Windows.Visibility.Collapsed;
                    }
                    this._timeStep.Text = occupancyEvent.TimeStep.ToString(format);
                    this._duration.Text = occupancyEvent.Duration.ToString(format);
                    this._likelihood.Text = occupancyEvent.Likelihood.ToString(format);
                    this._timeSamplingRate.Text = occupancyEvent.TimeSamplingRate.ToString(format);
                    this._velocityCap.Text = occupancyEvent.MaximumVelocityMagnitude.ToString(format);
                    this._visibilityAngle.Text = occupancyEvent.VisibilityAngle.ToString(format);
                    this._hasCapturedDataEvents.IsChecked = occupancyEvent.HasCapturedDataEvents;
                    this._hasCapturedVisualEvents.IsChecked = occupancyEvent.HasCapturedVisualEvents;
                    break;
                case DataType.SimulationResult:
                    this._setDataCostProperties.Visibility = System.Windows.Visibility.Collapsed;
                    this._desirability.Visibility = System.Windows.Visibility.Collapsed;
                    this._signal.Visibility = System.Windows.Visibility.Collapsed;
                    this._activityTimePeriod.Visibility = System.Windows.Visibility.Collapsed;
                    this._activityEngagementEvent.Visibility = System.Windows.Visibility.Collapsed;
                    this._eventCase.Visibility = System.Windows.Visibility.Collapsed;
                    if (this._dataField.GetType() == typeof(SimulationResult))
                    {
                        this._s_duration.Text = ((SimulationResult)this._dataField).SimulationDuration.ToString(format);
                        this._s_timeStep.Text = ((SimulationResult)this._dataField).TimeStep.ToString(format);
                        this._mandatorySimulationResults.Visibility = System.Windows.Visibility.Collapsed;
                        this._dataType.Text = "Optional Occupancy Simulation Results";
                    }
                    else //if (this._dataField.GetType() == typeof(MandatorySimulationResult))
                    {
                        this._s_duration.Text = ((SimulationResult)this._dataField).SimulationDuration.ToString(format);
                        this._s_timeStep.Text = ((SimulationResult)this._dataField).TimeStep.ToString(format);
                        this._distance.Text = ((MandatorySimulationResult)this._dataField).WalkedDistancePerHour.ToString(format);
                        this._walkingTime.Text = ((MandatorySimulationResult)this._dataField).WalkingTime.ToString(percent);
                        this._timeInMainStations.Text = ((MandatorySimulationResult)this._dataField).TimeInMainStations.ToString(percent);
                        this._engagementTime.Text = ((MandatorySimulationResult)this._dataField).ActivityEngagementTime.ToString(percent);
                        this._dataType.Text = "Mandatory Occupancy Simulation Results";
                        this._sequencesWhichNeededVisualDetection.Text = ((MandatorySimulationResult)this._dataField).SequencesWhichNeededVisualDetection.ToString();
                        this._averageDelayChanceForVisualDetection.Text = ((MandatorySimulationResult)this._dataField).AverageDelayChanceForVisualDetection.ToString(percent);
                        this._minimumDelayChanceForVisualDetection.Text = ((MandatorySimulationResult)this._dataField).MinimumDelayChanceForVisualDetection.ToString(percent);
                        this._maximumDelayChanceForVisualDetection.Text = ((MandatorySimulationResult)this._dataField).MaximumDelayChanceForVisualDetection.ToString(percent);
                    }
                    break;
            }
            this._boxPlot._canvas.Loaded += new RoutedEventHandler(_canvas_Loaded);
        }

        void drawFrequencies(object sender, RoutedEventArgs e)
        {
            EvaluationEvent occupancyEvent = this._dataField as EvaluationEvent;
            this._signalPlot.DrawFrequency(occupancyEvent);
            this.Loaded -= drawFrequencies;
        }

        void _captureActivityEvent_Checked(object sender, RoutedEventArgs e)
        {
            ((Activity)this._dataField).UseToCaptureEvent = true;
        }

        void _captureActivityEvent_Unchecked(object sender, RoutedEventArgs e)
        {
            ((Activity)this._dataField).UseToCaptureEvent = false;
        }

        private void activityEngagementTime_TextChanged(object sender, TextChangedEventArgs e)
        {
            double max = 0, min = 0;
            bool parsed = true;
            if (double.TryParse(this._maximumEngagementTime.Text, out max))
            {
                this._maximumEngagementTime.Foreground = Brushes.Black;
            }
            else
            {
                this._maximumEngagementTime.Foreground = Brushes.Red;
                parsed = false;
            }
            if (double.TryParse(this._minimumEngagementTime.Text, out min))
            {
                this._minimumEngagementTime.Foreground = Brushes.Black;
            }
            else
            {
                this._minimumEngagementTime.Foreground = Brushes.Red;
                parsed = false;
            }
            if (!parsed)
            {
                return;
            }
            if (min<=0)
            {
                this._minimumEngagementTime.Foreground = Brushes.Red;
                return;
            }
            if (max<min)
            {
                this._maximumEngagementTime.Foreground = Brushes.Red;
                return;
            }
            Activity activity = (Activity)this._dataField;
            if (activity.TrySetEngagementTime(min,max))
            {
                this._minimumEngagementTime.Foreground = Brushes.Black;
                this._maximumEngagementTime.Foreground = Brushes.Black;
            }
            else
            {
                this._minimumEngagementTime.Foreground = Brushes.Red;
                this._maximumEngagementTime.Foreground = Brushes.Red;
            }
        }

        void _canvas_Loaded(object sender, RoutedEventArgs e)
        {
            this._data1Max.Text = this._dataField.Max.ToString(format);
            this._data1Min.Text = this._dataField.Min.ToString(format);
            var result = new MathNet.Numerics.Statistics.DescriptiveStatistics(this._dataField.Data.Values);
            
            this._data1Mean.Text = result.Mean.ToString(format);
            this._data1Variance.Text = result.Variance.ToString(format);
            this._dataSize.Text = result.Count.ToString();
            double sum = result.Mean * this._dataField.Data.Count;
            this._sum.Text = sum.ToString(format);
            double integral = sum * this._host.cellularFloor.CellSize * this._host.cellularFloor.CellSize;
            this._integration.Text = integral.ToString(format);
            this._dataStandardDeviation.Text = result.StandardDeviation.ToString(format);
            this._boxPlot.DrawVertically(this._dataField, 3);
        }
        #region from elsewhere
        void _vis_Click(object sender, RoutedEventArgs e)
        {
            VisualizeFunction visualizer = new VisualizeFunction(this._spatialDataField);
            visualizer.Owner = (Window)((DockPanel)((Grid)this.Parent).Parent).Parent;
            visualizer.ShowDialog();
            visualizer = null;
        }

        void _method_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var type = (CostCalculationMethod)this._method.SelectedValue;
            switch (type)
            {
                case CostCalculationMethod.Interpolation:
                    InterpolationFormulaSet setInterpolation = new InterpolationFormulaSet(this._spatialDataField);
                    setInterpolation.Owner = (Window)((DockPanel)((Grid)this.Parent).Parent).Parent;
                    setInterpolation.ShowDialog();
                    this._spatialDataField.SetInterpolation(setInterpolation.interpolation);
                    setInterpolation.interpolation = null;
                    setInterpolation = null;
                    break;
                case CostCalculationMethod.WrittenFormula:
                    TextFormulaSet setTextFormula = new TextFormulaSet(this._host, this._spatialDataField);
                    setTextFormula.Owner = (Window)((DockPanel)((Grid)this.Parent).Parent).Parent;
                    setTextFormula.ShowDialog();
                    this._spatialDataField.SetStringFormula(setTextFormula.CostFunction);
                    this._spatialDataField.TextFormula = setTextFormula.main.Text;
                    foreach (var item in this._host.Parameters)
                    {
                        if (item.Value.LinkedFunctions.Contains(this._spatialDataField))
                        {
                            item.Value.LinkedFunctions.Remove(this._spatialDataField);
                        }
                    }
                    foreach (var item in setTextFormula.LinkedParameters)
                    {
                        item.LinkedFunctions.Add(this._spatialDataField);
                    }
                    setTextFormula.CostFunction = null;
                    setTextFormula = null;
                    break;
                case CostCalculationMethod.RawValue:
                    this._spatialDataField.SetRawValue();
                    break;
                case CostCalculationMethod.BuiltInRepulsion:
                    this._spatialDataField.SetBuiltInRepulsion();
                    break;
            }
        }

        private void _include_Checked(object sender, RoutedEventArgs e)
        {
            this._spatialDataField.IncludeInActivityGeneration = true;
        }

        private void _include_Unchecked(object sender, RoutedEventArgs e)
        {
            this._spatialDataField.IncludeInActivityGeneration = false;
        }
        #endregion
        void _outsideInterval_Unchecked(object sender, RoutedEventArgs e)
        {
            this._spatialDataField.CaptureEventWhenOutsideInterval = false;
        }

        void _outsideInterval_Checked(object sender, RoutedEventArgs e)
        {
            this._spatialDataField.CaptureEventWhenOutsideInterval = true;
        }

        void _includeCost_Unchecked(object sender, RoutedEventArgs e)
        {
            this._spatialDataField.UseCostFunctionForEventCapturing = false;
        }

        void _includeCost_Checked(object sender, RoutedEventArgs e)
        {
            this._spatialDataField.UseCostFunctionForEventCapturing = true;
        }

        void _capture_Unchecked(object sender, RoutedEventArgs e)
        {
            this._spatialDataField.UseToCaptureEvent = false;
        }

        void _capture_Checked(object sender, RoutedEventArgs e)
        {
            this._spatialDataField.UseToCaptureEvent = true;
        }

        void _intervalMax_TextChanged(object sender, TextChangedEventArgs e)
        {
            double min, max;
            if (double.TryParse(this._intervalMin.Text,out min) &&
                double.TryParse(this._intervalMax.Text, out max))
            {
                this._spatialDataField.EventCapturingInterval = new CellularEnvironment.Interval(min, max);
                this._capture.IsEnabled = true;
                this._outsideInterval.IsEnabled = true;
            }
            else
            {
                this._spatialDataField.EventCapturingInterval = null;
                this._capture.IsChecked = false;
                this._capture.IsEnabled = false;
                this._spatialDataField.UseToCaptureEvent = false;
                this._outsideInterval.IsChecked = false;
                this._outsideInterval.IsEnabled = false;
            }
        }

        void _intervalMin_TextChanged(object sender, TextChangedEventArgs e)
        {
            double min, max;
            if (double.TryParse(this._intervalMin.Text, out min) &&
                double.TryParse(this._intervalMax.Text, out max))
            {
                this._spatialDataField.EventCapturingInterval = new CellularEnvironment.Interval(min, max);
                this._capture.IsEnabled = true;
                this._outsideInterval.IsEnabled = true;
            }
            else
            {
                this._spatialDataField.EventCapturingInterval = null;
                this._capture.IsChecked = false;
                this._capture.IsEnabled = false;
                this._spatialDataField.UseToCaptureEvent = false;
                this._outsideInterval.IsChecked = false;
                this._outsideInterval.IsEnabled = false;
            }
        }

        private void _contextMenu_Click(object sender, RoutedEventArgs e)
        {
            Window owner = (Window)((DockPanel)((Grid)this.Parent).Parent).Parent;
            double dpi = 96;
            GetNumber getNumber0 = new GetNumber("Set Image Resolution",
                "The graph will be exported in PGN format. Setting a heigh resolution value may crash this app.", dpi);

            getNumber0.Owner = owner;
            getNumber0.ShowDialog();
            dpi = getNumber0.NumberValue;
            getNumber0 = null;
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Title = "Save the Scene to PNG Image format";
            dlg.DefaultExt = ".png";
            dlg.Filter = "PNG documents (.png)|*.png";
            Nullable<bool> result = dlg.ShowDialog(owner);
            string fileAddress = "";
            if (result == true)
            {
                fileAddress = dlg.FileName;
            }
            else
            {
                return;
            }
            Rect bounds = VisualTreeHelper.GetDescendantBounds(this);
            RenderTargetBitmap main_rtb = new RenderTargetBitmap((int)(bounds.Width * dpi / 96), (int)(bounds.Height * dpi / 96), dpi, dpi, System.Windows.Media.PixelFormats.Default);
            DrawingVisual dvFloorScene = new DrawingVisual();
            using (DrawingContext dc = dvFloorScene.RenderOpen())
            {
                VisualBrush vb = new VisualBrush(this);
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
                MessageBox.Show(err.Report(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

