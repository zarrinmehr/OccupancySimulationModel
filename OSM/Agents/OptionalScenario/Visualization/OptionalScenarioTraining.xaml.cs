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
using SpatialAnalysis.Agents.Visualization.AgentTrailVisualization;
using SpatialAnalysis.Data;
using System.Windows.Threading;
using SpatialAnalysis.Optimization;
using SpatialAnalysis.Geometry;

namespace SpatialAnalysis.Agents.OptionalScenario.Visualization
{
    /// <summary>
    /// Interaction logic for OptionalScenarioTraining.xaml
    /// </summary>
    public partial class OptionalScenarioTraining : Window
    {
        private StringBuilder _sb;
        #region Host Definition
        private static DependencyProperty _hostProperty =
            DependencyProperty.Register("_host", typeof(OSMDocument), typeof(OptionalScenarioTraining),
            new FrameworkPropertyMetadata(null, OptionalScenarioTraining.hostPropertyChanged, OptionalScenarioTraining.PropertyCoerce));
        private OSMDocument _host 
        {
            get { return (OSMDocument)GetValue(_hostProperty); }
            set { SetValue(_hostProperty, value); }
        }
        private static void hostPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            OptionalScenarioTraining instance = (OptionalScenarioTraining)obj;
            instance._host = (OSMDocument)args.NewValue;
        }
        #endregion

        #region AllParameters Definition
        public static DependencyProperty AllParametersProperty =
            DependencyProperty.Register("AllParameters", typeof(HashSet<Parameter>), typeof(OptionalScenarioTraining),
            new FrameworkPropertyMetadata(null, OptionalScenarioTraining.AllParametersPropertyChanged, OptionalScenarioTraining.PropertyCoerce));
        private HashSet<Parameter> AllParameters
        {
            get { return (HashSet<Parameter>)GetValue(AllParametersProperty); }
            set { SetValue(AllParametersProperty, value); }
        }
        private static void AllParametersPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            OptionalScenarioTraining instance = (OptionalScenarioTraining)obj;
            instance.AllParameters = (HashSet<Parameter>)args.NewValue;
        }
        #endregion

        #region AllDestinations Definition
        public static DependencyProperty AllDestinationsProperty =
            DependencyProperty.Register("AllDestinations", typeof(StateBase[]), typeof(OptionalScenarioTraining),
            new FrameworkPropertyMetadata(null, OptionalScenarioTraining.AllDestinationsPropertyChanged, OptionalScenarioTraining.PropertyCoerce));
        StateBase[] AllDestinations
        {
            get { return (StateBase[])GetValue(AllDestinationsProperty); }
            set { SetValue(AllDestinationsProperty, value); }
        }
        private static void AllDestinationsPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            OptionalScenarioTraining instance = (OptionalScenarioTraining)obj;
            instance.AllDestinations = (StateBase[])args.NewValue;
        }
        #endregion
        #region _timeStep Definition
        private static DependencyProperty _timeStepValueProperty =
            DependencyProperty.Register("_timeStepValue", typeof(double), typeof(OptionalScenarioTraining),
            new FrameworkPropertyMetadata(17.0d, OptionalScenarioTraining._timeStepValuePropertyChanged, OptionalScenarioTraining.PropertyCoerce));
        private double _timeStepValue
        {
            get { return (double)GetValue(_timeStepValueProperty); }
            set { SetValue(_timeStepValueProperty, value); }
        }
        private static void _timeStepValuePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            OptionalScenarioTraining instance = (OptionalScenarioTraining)obj;
            instance._timeStepValue = (double)args.NewValue;
        }
        #endregion
        #region _durationValue Definition
        private static DependencyProperty _durationValueProperty =
            DependencyProperty.Register("_durationValue", typeof(double), typeof(OptionalScenarioTraining),
            new FrameworkPropertyMetadata(0.0d, OptionalScenarioTraining._durationValuePropertyChanged, OptionalScenarioTraining.PropertyCoerce));
        private double _durationValue
        {
            get { return (double)GetValue(_durationValueProperty); }
            set { SetValue(_durationValueProperty, value); }
        }
        private static void _durationValuePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            OptionalScenarioTraining instance = (OptionalScenarioTraining)obj;
            instance._durationValue = (double)args.NewValue;
        }
        #endregion
        #region _iterationCount Definition
        private static DependencyProperty _iterationCountProperty =
            DependencyProperty.Register("_iterationCount", typeof(int), typeof(OptionalScenarioTraining),
            new FrameworkPropertyMetadata(0, OptionalScenarioTraining._iterationCountPropertyChanged, OptionalScenarioTraining.PropertyCoerce));
        private int _iterationCount
        {
            get { return (int)GetValue(_iterationCountProperty); }
            set { SetValue(_iterationCountProperty, value); }
        }
        private static void _iterationCountPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            OptionalScenarioTraining instance = (OptionalScenarioTraining)obj;
            instance._iterationCount = (int)args.NewValue;
        }
        #endregion
        #region _iterationCount Definition
        private static DependencyProperty _counterProperty =
            DependencyProperty.Register("_counter", typeof(int), typeof(OptionalScenarioTraining),
            new FrameworkPropertyMetadata(0, OptionalScenarioTraining._counterPropertyChanged, OptionalScenarioTraining.PropertyCoerce));
        private int _counter
        {
            get { return (int)GetValue(_counterProperty); }
            set { SetValue(_counterProperty, value); }
        }
        private static void _counterPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            OptionalScenarioTraining instance = (OptionalScenarioTraining)obj;
            instance._counter = (int)args.NewValue;
        }
        #endregion
        
        private static object PropertyCoerce(DependencyObject obj, object value)
        {
            return value;
        }

        //double _timeStep, _duration, _isovistRadius;
        //int _destinationCount;

        public OptionalScenarioTraining(OSMDocument host)
        {
            InitializeComponent();
            this._host = host;
            this._dataCtrlPanel.Click += this._dataCtrlPanel_Click;
            this._paramCtrlPanel.Click += new RoutedEventHandler(_paramCtrlPanel_Click);
            this.AllDestinations = new StateBase[this._host.trailVisualization.AgentWalkingTrail.InterpolatedStates.Length-1];
            this._sb = new StringBuilder();
            this._run_close.Click += this._run_Click;
        }

        private void _paramCtrlPanel_Click(object sender, RoutedEventArgs e)
        {
            var parameterSetting = new Data.Visualization.ParameterSetting(this._host, false);
            parameterSetting.Owner = this._host;
            parameterSetting.ShowDialog();
        }

        private void _dataCtrlPanel_Click(object sender, RoutedEventArgs e)
        {
            var dataCtrlPanel = new Data.Visualization.SpatialDataControlPanel(this._host, Data.Visualization.IncludedDataTypes.SpatialData);
            dataCtrlPanel.Owner = this._host;
            dataCtrlPanel.ShowDialog();
        }

        private void _run_Click(object sender, RoutedEventArgs e)
        {

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
                if (timeStep<=0)
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
            if (this._isoExternalDepth.IsChecked.Value)
            {
                this.AllParameters.Add(this._host.Parameters[AgentParameters.OPT_IsovistExternalDepth.ToString()]);
            }
            if (this._numberOfDestinations.IsChecked.Value)
            {
                this.AllParameters.Add(this._host.Parameters[AgentParameters.OPT_NumberOfDestinations.ToString()]);
            }
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
            if (this._visibilityAngle.IsChecked.Value)
            {
                this.AllParameters.Add(this._host.Parameters[AgentParameters.GEN_VisibilityAngle.ToString()]);
            }
            if (this._decisionMakingPeriodLamdaFactor.IsChecked.Value)
            {
                this.AllParameters.Add(this._host.Parameters[AgentParameters.OPT_DecisionMakingPeriodLambdaFactor.ToString()]);
            }
            if (this._angleDistributionLambdaFactor.IsChecked.Value)
            {
                this.AllParameters.Add(this._host.Parameters[AgentParameters.OPT_AngleDistributionLambdaFactor.ToString()]);
            }
            if (this._desirabilityDistributionLambdaFactor.IsChecked.Value)
            {
                this.AllParameters.Add(this._host.Parameters[AgentParameters.OPT_DesirabilityDistributionLambdaFactor.ToString()]);
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
            if (this.AllParameters.Count==0)
            {
                MessageBox.Show("No parameter is included to account for the variability in the optimization process", 
                    "Parameter Not Assigned", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
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
            //updating the FreeNavigationAgentCharacters
            this._host.FreeNavigationAgentCharacter = FreeNavigationAgent.Create(this._host.trailVisualization.AgentWalkingTrail.InterpolatedStates[0]);
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
            OptionalScenarioTrainer training = new OptionalScenarioTrainer(this._host, this._timeStepValue, this._durationValue);
            double value = 0; 
            //OptionalWalkingTraining.TrailPoints.Clear();
            //var directions = new StateBase[this._host.trailVisualization.AgentWalkingTrail.NormalizedStates.Length];
            for (int i = 0; i < this._host.trailVisualization.AgentWalkingTrail.InterpolatedStates.Length - 1; i++)
            {
                StateBase newState = this._host.trailVisualization.AgentWalkingTrail.InterpolatedStates[i].Copy();
                newState.Velocity *= training.AccelerationMagnitude;
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

