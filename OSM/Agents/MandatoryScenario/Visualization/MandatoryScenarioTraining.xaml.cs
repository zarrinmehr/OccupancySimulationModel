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
using SpatialAnalysis.CellularEnvironment;
using SpatialAnalysis.Data;
using SpatialAnalysis.FieldUtility;
using SpatialAnalysis.FieldUtility.Visualization;
using SpatialAnalysis.Geometry;
using SpatialAnalysis.Optimization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SpatialAnalysis.Agents.MandatoryScenario.Visualization
{
    /// <summary>
    /// Interaction logic for MandatoryScenarioTraining.xaml
    /// </summary>
    public partial class MandatoryScenarioTraining : Window
    {
        private StringBuilder _sb;
        #region Host Definition
        private static DependencyProperty _hostProperty =
            DependencyProperty.Register("_host", typeof(OSMDocument), typeof(MandatoryScenarioTraining),
            new FrameworkPropertyMetadata(null, MandatoryScenarioTraining.hostPropertyChanged, MandatoryScenarioTraining.PropertyCoerce));
        private OSMDocument _host
        {
            get { return (OSMDocument)GetValue(_hostProperty); }
            set { SetValue(_hostProperty, value); }
        }
        private static void hostPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            MandatoryScenarioTraining instance = (MandatoryScenarioTraining)obj;
            instance._host = (OSMDocument)args.NewValue;
        }
        #endregion

        #region AllParameters Definition
        public static DependencyProperty AllParametersProperty =
            DependencyProperty.Register("AllParameters", typeof(HashSet<Parameter>), typeof(MandatoryScenarioTraining),
            new FrameworkPropertyMetadata(null, MandatoryScenarioTraining.AllParametersPropertyChanged, MandatoryScenarioTraining.PropertyCoerce));
        private HashSet<Parameter> AllParameters
        {
            get { return (HashSet<Parameter>)GetValue(AllParametersProperty); }
            set { SetValue(AllParametersProperty, value); }
        }
        private static void AllParametersPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            MandatoryScenarioTraining instance = (MandatoryScenarioTraining)obj;
            instance.AllParameters = (HashSet<Parameter>)args.NewValue;
        }
        #endregion

        #region AllDestinations Definition
        public static DependencyProperty AllDestinationsProperty =
            DependencyProperty.Register("AllDestinations", typeof(StateBase[]), typeof(MandatoryScenarioTraining),
            new FrameworkPropertyMetadata(null, MandatoryScenarioTraining.AllDestinationsPropertyChanged, MandatoryScenarioTraining.PropertyCoerce));
        StateBase[] AllDestinations
        {
            get { return (StateBase[])GetValue(AllDestinationsProperty); }
            set { SetValue(AllDestinationsProperty, value); }
        }
        private static void AllDestinationsPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            MandatoryScenarioTraining instance = (MandatoryScenarioTraining)obj;
            instance.AllDestinations = (StateBase[])args.NewValue;
        }
        #endregion
        #region _timeStep Definition
        private static DependencyProperty _timeStepValueProperty =
            DependencyProperty.Register("_timeStepValue", typeof(double), typeof(MandatoryScenarioTraining),
            new FrameworkPropertyMetadata(17.0d, MandatoryScenarioTraining._timeStepValuePropertyChanged, MandatoryScenarioTraining.PropertyCoerce));
        private double _timeStepValue
        {
            get { return (double)GetValue(_timeStepValueProperty); }
            set { SetValue(_timeStepValueProperty, value); }
        }
        private static void _timeStepValuePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            MandatoryScenarioTraining instance = (MandatoryScenarioTraining)obj;
            instance._timeStepValue = (double)args.NewValue;
        }
        #endregion
        #region _durationValue Definition
        private static DependencyProperty _durationValueProperty =
            DependencyProperty.Register("_durationValue", typeof(double), typeof(MandatoryScenarioTraining),
            new FrameworkPropertyMetadata(0.0d, MandatoryScenarioTraining._durationValuePropertyChanged, MandatoryScenarioTraining.PropertyCoerce));
        private double _durationValue
        {
            get { return (double)GetValue(_durationValueProperty); }
            set { SetValue(_durationValueProperty, value); }
        }
        private static void _durationValuePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            MandatoryScenarioTraining instance = (MandatoryScenarioTraining)obj;
            instance._durationValue = (double)args.NewValue;
        }
        #endregion
        #region _iterationCount Definition
        private static DependencyProperty _iterationCountProperty =
            DependencyProperty.Register("_iterationCount", typeof(int), typeof(MandatoryScenarioTraining),
            new FrameworkPropertyMetadata(0, MandatoryScenarioTraining._iterationCountPropertyChanged, MandatoryScenarioTraining.PropertyCoerce));
        private int _iterationCount
        {
            get { return (int)GetValue(_iterationCountProperty); }
            set { SetValue(_iterationCountProperty, value); }
        }
        private static void _iterationCountPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            MandatoryScenarioTraining instance = (MandatoryScenarioTraining)obj;
            instance._iterationCount = (int)args.NewValue;
        }
        #endregion
        #region _iterationCount Definition
        private static DependencyProperty _counterProperty =
            DependencyProperty.Register("_counter", typeof(int), typeof(MandatoryScenarioTraining),
            new FrameworkPropertyMetadata(0, MandatoryScenarioTraining._counterPropertyChanged, MandatoryScenarioTraining.PropertyCoerce));
        private int _counter
        {
            get { return (int)GetValue(_counterProperty); }
            set { SetValue(_counterProperty, value); }
        }
        private static void _counterPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            MandatoryScenarioTraining instance = (MandatoryScenarioTraining)obj;
            instance._counter = (int)args.NewValue;
        }
        #endregion

        Activity _activity;
        int _gaussianRange;
        HashSet<Index> _trailIndices;
        BarrierPolygons _destination;
        StateBase _defaultState;
        Cell _cellDestination;
        private static object PropertyCoerce(DependencyObject obj, object value)
        {
            return value;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="MandatoryScenarioTraining"/> class.
        /// </summary>
        /// <param name="host">The main document to which this window belongs.</param>
        public MandatoryScenarioTraining(OSMDocument host)
        {
            InitializeComponent();
            this._host = host;
            this._dataCtrlPanel.Click += this._dataCtrlPanel_Click;
            this._paramCtrlPanel.Click += new RoutedEventHandler(_paramCtrlPanel_Click);
            this.AllDestinations = new StateBase[this._host.trailVisualization.AgentWalkingTrail.InterpolatedStates.Length - 1];
            this._sb = new StringBuilder();
            this._run_close.Click += _run_Click;
            if (this._host.FieldGenerator == null)
            {
                this._host.FieldGenerator = new Data.Visualization.SpatialDataCalculator(this._host, 5);
            }
            this._neighborhoodRangePotential.Text = this._host.FieldGenerator.Range.ToString();
            if (this._host.ViewBasedGaussianFilter == null)
            {
                this._host.ViewBasedGaussianFilter = new IsovistClippedGaussian(this._host.cellularFloor, 7);
            }
            this._gaussianRangeDefault.Text = this._host.ViewBasedGaussianFilter.Range.ToString();
        }



        private void _dataCtrlPanel_Click(object sender, RoutedEventArgs e)
        {
            var dataCtrlPanel = new Data.Visualization.SpatialDataControlPanel(this._host, Data.Visualization.IncludedDataTypes.SpatialData);
            dataCtrlPanel.Owner = this._host;
            dataCtrlPanel.ShowDialog();
        }
        private void _paramCtrlPanel_Click(object sender, RoutedEventArgs e)
        {
            var parameterSetting = new Data.Visualization.ParameterSetting(this._host, false);
            parameterSetting.Owner = this._host;
            parameterSetting.ShowDialog();
        }
        void _run_Click(object sender, RoutedEventArgs e)
        {

            #region Validate activity related input
            if (this._applyFilter.IsChecked.Value)
            {
                int gr = 0;
                if (!int.TryParse(this._gaussianRangeDefault.Text, out gr))
                {
                    MessageBox.Show("Invalid input for Gaussian filter range");
                    return;
                }
                else
                {
                    if (gr < 2)
                    {
                        MessageBox.Show("Gaussian filter range should be larger than 1!");
                        return;
                    }
                    else
                    {
                        this._gaussianRange = gr;
                    }
                }
                if (this._host.ViewBasedGaussianFilter.Range != gr)
                {
                    this._host.ViewBasedGaussianFilter = new IsovistClippedGaussian(this._host.cellularFloor, gr);
                }
            }
            int _r = 0;
            if (!int.TryParse(this._neighborhoodRangePotential.Text,out _r))
            {
                MessageBox.Show("Invalid input for potential field calculation range!");
                        return;
            }
            else
            {
                if (_r<1)
                {
                    MessageBox.Show("Potential field calculation range should be larger than 0!");
                        return;
                }
                else
                {
                    if (this._host.FieldGenerator.Range != _r)
                    {
                        this._host.FieldGenerator = new Data.Visualization.SpatialDataCalculator(this._host, _r, OSMDocument.AbsoluteTolerance);
                    }
                }
            }
            
            #endregion

            #region Validate simulates annealing parameters
            int iterationCount = 0;
            if (!int.TryParse(this._numberOfIterations.Text, out iterationCount))
            {
                this.invalidInput("Number of Iterations");
                return;
            }
            if (iterationCount <= 0)
            {
                this.valueSmallerThanZero("Number of Iterations");
                return;
            }
            this._iterationCount = iterationCount;
            double minimumEnergy = 0;
            if (!double.TryParse(this._minimumTemperature.Text, out minimumEnergy))
            {
                this.invalidInput("Minimum Temperature");
                return;
            }
            if (minimumEnergy < 0)
            {
                MessageBox.Show("'Minimum Temperature' should not be smaller than zero!",
                    "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            double maximumEnergy = 0;
            if (!double.TryParse(this._maximumTemperature.Text, out maximumEnergy))
            {
                this.invalidInput("Maximum Temperature");
                return;
            }
            if (maximumEnergy <= 0)
            {
                this.valueSmallerThanZero("Maximum Temperature");
                return;
            }
            #endregion

            #region Validate duration and timeStep parameters
            double timeStep = 0;
            if (!double.TryParse(this._timeStep.Text, out timeStep))
            {
                MessageBox.Show("Invalid input for 'Time Step'!",
                    "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else
            {
                if (timeStep <= 0)
                {
                    MessageBox.Show("'Time Step' must be larger than zero!",
                        "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            this._timeStepValue = timeStep;

            this._durationValue = this._host.trailVisualization.AgentWalkingTrail.TimeIntervalBetweenInterpolatedStates;//convert seconds to hours
            #endregion

            #region Check to see if spatial data has been included
            int spatialDataCount = 0;
            foreach (Function function in this._host.cellularFloor.AllSpatialDataFields.Values)
            {
                SpatialDataField dataField = function as SpatialDataField;
                if (dataField != null)
                {
                    if (dataField.IncludeInActivityGeneration)
                    {
                        spatialDataCount++;
                    }
                }
            }
            if (spatialDataCount == 0)
            {
                var res = MessageBox.Show("There is no a spatial data field included to calculate cost/desirability!");
                return;
            }
            #endregion

            #region extracting the ralated parameters and checking to see if number of parameter is not zero
            this.AllParameters = new HashSet<Parameter>();

            if (this._velocityMagnetude.IsChecked.Value)
            {
                this.AllParameters.Add(this._host.Parameters[AgentParameters.GEN_VelocityMagnitude.ToString()]);
            }
            if (this._angularVelocity.IsChecked.Value)
            {
                this.AllParameters.Add(this._host.Parameters[AgentParameters.GEN_AngularVelocity.ToString()]);
            }
            if (this._bodySize.IsChecked.Value)
            {
                this.AllParameters.Add(this._host.Parameters[AgentParameters.GEN_BodySize.ToString()]);
            }
            if (this._accelerationMagnitude.IsChecked.Value)
            {
                this.AllParameters.Add(this._host.Parameters[AgentParameters.GEN_AccelerationMagnitude.ToString()]);
            }
            if (this._barrierRepulsionRange.IsChecked.Value)
            {
                this.AllParameters.Add(this._host.Parameters[AgentParameters.GEN_BarrierRepulsionRange.ToString()]);
            }
            if (this._repulsionChangeRate.IsChecked.Value)
            {
                this.AllParameters.Add(this._host.Parameters[AgentParameters.GEN_MaximumRepulsion.ToString()]);
            }
            if (this._barrierFriction.IsChecked.Value)
            {
                this.AllParameters.Add(this._host.Parameters[AgentParameters.GEN_BarrierFriction.ToString()]);
            }
            if (this._bodyElasticity.IsChecked.Value)
            {
                this.AllParameters.Add(this._host.Parameters[AgentParameters.GEN_AgentBodyElasticity.ToString()]);
            }
            if (this._angularDeviationCost.IsChecked.Value)
            {
                this.AllParameters.Add(this._host.Parameters[AgentParameters.MAN_AngularDeviationCost.ToString()]);
            }
            if (this._distanceCost.IsChecked.Value)
            {
                this.AllParameters.Add(this._host.Parameters[AgentParameters.MAN_DistanceCost.ToString()]);
            }
            foreach (var item in this._host.Parameters)
            {
                foreach (var function in item.Value.LinkedFunctions)
                {
                    if (function.IncludeInActivityGeneration)
                    {
                        this.AllParameters.Add(item.Value);
                        break;
                    }
                }
            }
            if (this.AllParameters.Count == 0)
            {
                MessageBox.Show("No parameter is included to account for the variability in the optimization process",
                    "Parameter Not Assigned", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            #endregion

            #region Trail Related Stuff
            this._trailIndices = new HashSet<Index>();
            foreach (var item in this._host.trailVisualization.AgentWalkingTrail.ApproximatedPoints)
            {
                Index index = this._host.cellularFloor.FindIndex(item);
                if (this._host.cellularFloor.Cells[index.I, index.J].FieldOverlapState != OverlapState.Inside)
                {
                    MessageBox.Show("The training process cannot proceed with this trail. Parts of the trail are not included in the walkable field!", "Invalid Trail", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                else
                {
                    this._trailIndices.Add(index);
                }
            }
            this._trailIndices = CellUtility.ExpandInWalkableField(this._host.cellularFloor, this._trailIndices);
            this._defaultState = this._host.trailVisualization.AgentWalkingTrail.InterpolatedStates[this._host.trailVisualization.AgentWalkingTrail.InterpolatedStates.Length - 1];
            this._cellDestination = this._host.cellularFloor.FindCell(this._defaultState.Location);
            UV p1 = this._cellDestination;
            var p2 = p1 + UV.UBase * this._host.cellularFloor.CellSize;
            var p3 = p1 + UV.UBase * this._host.cellularFloor.CellSize + UV.VBase * this._host.cellularFloor.CellSize;
            var p4 = p1 + UV.VBase * this._host.cellularFloor.CellSize;
            this._destination = new BarrierPolygons(new UV[] { p1, p2, p3, p4 }); 
            #endregion
            
            //execute visibility changes in the interface
            Dispatcher.Invoke(new Action(() =>
            {
                this._run_close.SetValue(Button.VisibilityProperty, System.Windows.Visibility.Collapsed);
                this._run_close.SetValue(Button.ContentProperty, "Close");
                this._mainInterface.SetValue(Grid.VisibilityProperty, System.Windows.Visibility.Collapsed);
                this._progressPanel.SetValue(StackPanel.VisibilityProperty, System.Windows.Visibility.Visible);

            }), DispatcherPriority.ContextIdle);
            //unregister run button event
            this._run_close.Click -= this._run_Click;
            //create annealing
            SimulatedAnnealingSolver solver = new SimulatedAnnealingSolver(this.AllParameters);
            //register UI update event for best fitness changes
            solver.BestFitnessUpdated += solver_BestFitnessUpdated;
            //register UI update event for fitness changes
            solver.FitnessUpdated += solver_FitnessUpdated;
            //Setting the initial iteration to zero            
            this._counter = 0;
            //initializing the trial visualization
            this._host.trailVisualization.InitiateTrainingVisualization();

            //running the annealing process
            solver.Solve(minimumEnergy, maximumEnergy, iterationCount, this.measureFitnessWithInterfaceUpdate);

            //unregister UI update event for fitness changes
            solver.BestFitnessUpdated -= solver_BestFitnessUpdated;
            //unregister UI update event for fitness changes
            solver.FitnessUpdated -= solver_FitnessUpdated;
            //showing the close btn. The close event is not registered yet
            Dispatcher.Invoke(new Action(() =>
            {
                this._run_close.SetValue(Button.VisibilityProperty, System.Windows.Visibility.Visible);
            }), DispatcherPriority.ContextIdle);
            //Show the last parameter setting on UI
            this._sb.Clear();
            foreach (var item in this.AllParameters)
            {
                this._sb.AppendLine(item.ToString());
            }
            this._updateMessage.SetValue(TextBlock.TextProperty, this._sb.ToString());
            //set Window closing events
            this._run_close.Click += (s, e1) =>
            {
                var result = MessageBox.Show("Do you want to clear the training data from screen?",
                    "", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                if (result == MessageBoxResult.Yes)
                {
                    this._host.trailVisualization.TerminateTrainingVisualization();
                }
                this.Close();
            };
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            this._activity = null;

            this._trailIndices.Clear();
            this._trailIndices = null;
            this._destination = null;
            this._defaultState = null;
            this._cellDestination = null;
        }

        protected override void OnClosed(EventArgs e)
        {
            var result = MessageBox.Show("Do you want to update the activity potential fields?", "Update Activities", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                EditActivitiesUI editor = new EditActivitiesUI(this._host);
                editor.Owner = this._host;
                editor.ShowDialog();
            }
        }

        void solver_BestFitnessUpdated(object sender, UIEventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                this._host.trailVisualization.VisualizeBestTrainingOption(this.AllDestinations);
                this._bestFitnessValue.SetValue(TextBlock.TextProperty, e.Value.ToString());
            }), DispatcherPriority.ContextIdle);
        }

        void solver_FitnessUpdated(object sender, UIEventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                this._host.trailVisualization.VisualizeTrainingStep(this.AllDestinations);
                this._currentFitnessValue.SetValue(TextBlock.TextProperty, e.Value.ToString());
            }), DispatcherPriority.ContextIdle);
        }


        //measure fitness based on sum of differences in angles
        private double measureFitnessWithInterfaceUpdate()
        {
            Activity activity = null;
            if (this._angularDeviationCost.IsChecked.Value)
            {
                activity = this._host.FieldGenerator.GetDynamicPotentialFieldForTraining(this._cellDestination, this._destination, this._defaultState, "Temporary",
                Parameter.DefaultParameters[AgentParameters.MAN_AngularDeviationCost].Value, this._trailIndices);
            }
            else
            {
                activity = this._host.FieldGenerator.GetStaticPotentialFieldForTraining(this._cellDestination, this._destination, this._defaultState, "Temporary", this._trailIndices);
            }
            if (this._applyFilter.IsChecked.Value)
            {
                activity = (Activity)this._host.ViewBasedGaussianFilter.ApplyParallel(activity, activity.Name);
            }
            if (activity == null)
            {
                throw new ArgumentNullException("Failed to create the activity in the training process");
            }
            MandatoryScenarioTrainer training = new MandatoryScenarioTrainer(this._host, this._timeStepValue, this._durationValue, activity);

            double value = 0;
            //OptionalWalkingTraining.TrailPoints.Clear();
            //var directions = new StateBase[this._host.trailVisualization.AgentWalkingTrail.NormalizedStates.Length];
            for (int i = 0; i < this._host.trailVisualization.AgentWalkingTrail.InterpolatedStates.Length - 1; i++)
            {
                StateBase newState = this._host.trailVisualization.AgentWalkingTrail.InterpolatedStates[i].Copy();
                
                training.UpdateAgent(newState);
                this.AllDestinations[i] = training.CurrentState;
                //double fitness = StateBase.DistanceSquared(training.CurrentState, this._host.trailVisualization.AgentWalkingTrail.NormalizedStates[i + 1]);
                double fitness = UV.GetLengthSquared(training.CurrentState.Location, this._host.trailVisualization.AgentWalkingTrail.InterpolatedStates[i + 1].Location);
                value += fitness;
            }

            this._counter++;
            //interface update call in parallel
            Dispatcher.Invoke(new Action(() =>
            {
                this._host.trailVisualization.VisualizeTrainingStep(this.AllDestinations);
                this._sb.Clear();
                foreach (var item in this.AllParameters)
                {
                    this._sb.AppendLine(item.ToString());
                }
                this._updateMessage.SetValue(TextBlock.TextProperty, this._sb.ToString());

                int percent = (int)((this._counter * 100.0d) / this._iterationCount);
                if ((int)this._progressBar.Value != percent)
                {
                    this._progressReport.SetValue(TextBlock.TextProperty, string.Format("%{0}", percent.ToString()));
                    this._progressBar.SetValue(ProgressBar.ValueProperty, (double)percent);
                }
            }), DispatcherPriority.ContextIdle);
            return value;
        }

        private void invalidInput(string name)
        {
            MessageBox.Show(string.Format("Invalid input for '{0}'!", name),
                "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        private void valueSmallerThanZero(string name)
        {
            MessageBox.Show(string.Format("'{0}' should not be smaller than or equal to zero!", name),
                "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

