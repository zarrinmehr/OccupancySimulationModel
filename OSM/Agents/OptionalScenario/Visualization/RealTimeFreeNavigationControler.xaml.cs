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
using SpatialAnalysis.IsovistUtility;
using SpatialAnalysis.Geometry;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using SpatialAnalysis.Data;

namespace SpatialAnalysis.Agents.OptionalScenario.Visualization
{
    /// <summary>
    /// Interaction logic for RealTimeFreeNavigationControler.xaml
    /// </summary>
    public partial class RealTimeFreeNavigationControler : Window
    {
        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            this.Owner.WindowState = this.WindowState;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            this._velocityMin.TextChanged -= _velocityMin_TextChanged;
            this._velocity.ValueChanged -= _velocity_ValueChanged;
            this._velocityMax.TextChanged -= _velocityMax_TextChanged;
            this._angularVelocityMin.TextChanged -= _angularVelocityMin_TextChanged;
            this._angularVelocity.ValueChanged -= _angularVelocity_ValueChanged;
            this._angularVelocityMax.TextChanged -= _angularVelocityMax_TextChanged;
            this._bodySizeMin.TextChanged -= _bodySizeMin_TextChanged;
            this._bodySize.ValueChanged -= _bodySize_ValueChanged;
            this._bodySizeMax.TextChanged -= _bodySizeMax_TextChanged;
            this._viewAngleMin.TextChanged -= _viewAngleMin_TextChanged;
            this._viewAngle.ValueChanged -= _viewAngle_ValueChanged;
            this._viewAngleMax.TextChanged -= _viewAngleMax_TextChanged;
            this._minDecisionMakingPeriod.TextChanged -= _minDecisionMakingPeriod_TextChanged;
            this._decisionMakingPeriod.ValueChanged -= _decisionMakingPeriod_ValueChanged;
            this._maxDecisionMakingPeriod.TextChanged -= _maxDecisionMakingPeriod_TextChanged;
            this._angleWeightMin.TextChanged -= _angleWeightMin_TextChanged;
            this._angleWeight.ValueChanged -= _angleWeight_ValueChanged;
            this._angleWeightMax.TextChanged -= _angleWeightMax_TextChanged;
            this._desirabilityWeightMin.TextChanged -= _desirabilityWeightMin_TextChanged;
            this._desirabilityWeight.ValueChanged -= _desirabilityWeight_ValueChanged;
            this._desirabilityWeightMax.TextChanged -= _desirabilityWeightMax_TextChanged;
            this._minBarrierRepulsionRange.TextChanged -= _minBarrierRepulsionRange_TextChanged;
            this._barrierRepulsionRange.ValueChanged -= _barrierRepulsionRange_ValueChanged;
            this._maxBarrierRepulsionRange.TextChanged -= _maxBarrierRepulsionRange_TextChanged;
            this._minRepulsionChangeRate.TextChanged -= _minRepulsionChangeRate_TextChanged;
            this._repulsionChangeRate.ValueChanged -= _repulsionChangeRate_ValueChanged;
            this._maxRepulsionChangeRate.TextChanged -= _maxRepulsionChangeRate_TextChanged;
            this._accelerationMagnitudeMin.TextChanged -= _accelerationMagnitudeMin_TextChanged;
            this._accelerationMagnitude.ValueChanged -= _accelerationMagnitude_ValueChanged;
            this._accelerationMagnitudeMax.TextChanged -= _accelerationMagnitudeMax_TextChanged;
            this._barrierFrictionMin.TextChanged -= _barrierFrictionMin_TextChanged;
            this._barrierFriction.ValueChanged -= _barrierFriction_ValueChanged;
            this._barrierFrictionMax.TextChanged -= _barrierFrictionMax_TextChanged;
            this._bodyElasticityMin.TextChanged -= _bodyElasticityMin_TextChanged;
            this._bodyElasticity.ValueChanged -= _bodyElasticity_ValueChanged;
            this._bodyElasticityMax.TextChanged -= _bodyElasticityMax_TextChanged;
        }

        public RealTimeFreeNavigationControler()
        {
            InitializeComponent();

            this._velocityMin.Text = Parameter.DefaultParameters[AgentParameters.GEN_VelocityMagnitude].Minimum.ToString();
            this._velocityMin.TextChanged += _velocityMin_TextChanged;
            this._velocity.Value = Parameter.DefaultParameters[AgentParameters.GEN_VelocityMagnitude].Value;
            this._velocity.ValueChanged += _velocity_ValueChanged;
            this._velocityMax.Text = Parameter.DefaultParameters[AgentParameters.GEN_VelocityMagnitude].Maximum.ToString();
            this._velocityMax.TextChanged += _velocityMax_TextChanged;

            this._angularVelocityMin.Text = Parameter.DefaultParameters[AgentParameters.GEN_AngularVelocity].Minimum.ToString();
            this._angularVelocityMin.TextChanged += _angularVelocityMin_TextChanged;
            this._angularVelocity.Value = Parameter.DefaultParameters[AgentParameters.GEN_AngularVelocity].Value;
            this._angularVelocity.ValueChanged += _angularVelocity_ValueChanged;
            this._angularVelocityMax.Text = Parameter.DefaultParameters[AgentParameters.GEN_AngularVelocity].Maximum.ToString();
            this._angularVelocityMax.TextChanged += _angularVelocityMax_TextChanged;

            this._bodySizeMin.Text = Parameter.DefaultParameters[AgentParameters.GEN_BodySize].Minimum.ToString();
            this._bodySizeMin.TextChanged += _bodySizeMin_TextChanged;
            this._bodySize.Value = Parameter.DefaultParameters[AgentParameters.GEN_BodySize].Value;
            this._bodySize.ValueChanged += _bodySize_ValueChanged;
            this._bodySizeMax.Text = Parameter.DefaultParameters[AgentParameters.GEN_BodySize].Maximum.ToString();
            this._bodySizeMax.TextChanged += _bodySizeMax_TextChanged;

            this._viewAngleMin.Text = Parameter.DefaultParameters[AgentParameters.GEN_VisibilityAngle].Minimum.ToString();
            this._viewAngleMin.TextChanged += _viewAngleMin_TextChanged;
            this._viewAngle.Value = Parameter.DefaultParameters[AgentParameters.GEN_VisibilityAngle].Value;
            this._viewAngle.ValueChanged += _viewAngle_ValueChanged;
            this._viewAngleMax.Text = Parameter.DefaultParameters[AgentParameters.GEN_VisibilityAngle].Maximum.ToString();
            this._viewAngleMax.TextChanged += _viewAngleMax_TextChanged;

            this._minDecisionMakingPeriod.Text = Parameter.DefaultParameters[AgentParameters.OPT_DecisionMakingPeriodLambdaFactor].Minimum.ToString();
            this._minDecisionMakingPeriod.TextChanged += _minDecisionMakingPeriod_TextChanged;
            this._decisionMakingPeriod.Value = Parameter.DefaultParameters[AgentParameters.OPT_DecisionMakingPeriodLambdaFactor].Value;
            this._decisionMakingPeriod.ValueChanged += _decisionMakingPeriod_ValueChanged;
            this._maxDecisionMakingPeriod.Text = Parameter.DefaultParameters[AgentParameters.OPT_DecisionMakingPeriodLambdaFactor].Maximum.ToString();
            this._maxDecisionMakingPeriod.TextChanged += _maxDecisionMakingPeriod_TextChanged;

            this._angleWeightMin.Text = Parameter.DefaultParameters[AgentParameters.OPT_AngleDistributionLambdaFactor].Minimum.ToString();
            this._angleWeightMin.TextChanged += _angleWeightMin_TextChanged;
            this._angleWeight.Value = Parameter.DefaultParameters[AgentParameters.OPT_AngleDistributionLambdaFactor].Value;
            this._angleWeight.ValueChanged += _angleWeight_ValueChanged;
            this._angleWeightMax.Text = Parameter.DefaultParameters[AgentParameters.OPT_AngleDistributionLambdaFactor].Maximum.ToString();
            this._angleWeightMax.TextChanged += _angleWeightMax_TextChanged;

            this._desirabilityWeightMin.Text = Parameter.DefaultParameters[AgentParameters.OPT_DesirabilityDistributionLambdaFactor].Minimum.ToString();
            this._desirabilityWeightMin.TextChanged += _desirabilityWeightMin_TextChanged;
            this._desirabilityWeight.Value = Parameter.DefaultParameters[AgentParameters.OPT_DesirabilityDistributionLambdaFactor].Value;
            this._desirabilityWeight.ValueChanged += _desirabilityWeight_ValueChanged;
            this._desirabilityWeightMax.Text = Parameter.DefaultParameters[AgentParameters.OPT_DesirabilityDistributionLambdaFactor].Maximum.ToString();
            this._desirabilityWeightMax.TextChanged += _desirabilityWeightMax_TextChanged;

            this._minBarrierRepulsionRange.Text = Parameter.DefaultParameters[AgentParameters.GEN_BarrierRepulsionRange].Minimum.ToString();
            this._minBarrierRepulsionRange.TextChanged += _minBarrierRepulsionRange_TextChanged;
            this._barrierRepulsionRange.Value = Parameter.DefaultParameters[AgentParameters.GEN_BarrierRepulsionRange].Value;
            this._barrierRepulsionRange.ValueChanged += _barrierRepulsionRange_ValueChanged;
            this._maxBarrierRepulsionRange.Text = Parameter.DefaultParameters[AgentParameters.GEN_BarrierRepulsionRange].Maximum.ToString();
            this._maxBarrierRepulsionRange.TextChanged += _maxBarrierRepulsionRange_TextChanged;

            this._minRepulsionChangeRate.Text = Parameter.DefaultParameters[AgentParameters.GEN_MaximumRepulsion].Minimum.ToString();
            this._minRepulsionChangeRate.TextChanged += _minRepulsionChangeRate_TextChanged;
            this._repulsionChangeRate.Value = Parameter.DefaultParameters[AgentParameters.GEN_MaximumRepulsion].Value;
            this._repulsionChangeRate.ValueChanged += _repulsionChangeRate_ValueChanged;
            this._maxRepulsionChangeRate.Text = Parameter.DefaultParameters[AgentParameters.GEN_MaximumRepulsion].Maximum.ToString();
            this._maxRepulsionChangeRate.TextChanged += _maxRepulsionChangeRate_TextChanged;

            this._accelerationMagnitudeMin.Text = Parameter.DefaultParameters[AgentParameters.GEN_AccelerationMagnitude].Minimum.ToString();
            this._accelerationMagnitudeMin.TextChanged += _accelerationMagnitudeMin_TextChanged;
            this._accelerationMagnitude.Value = Parameter.DefaultParameters[AgentParameters.GEN_AccelerationMagnitude].Value;
            this._accelerationMagnitude.ValueChanged += _accelerationMagnitude_ValueChanged;
            this._accelerationMagnitudeMax.Text = Parameter.DefaultParameters[AgentParameters.GEN_AccelerationMagnitude].Maximum.ToString();
            this._accelerationMagnitudeMax.TextChanged += _accelerationMagnitudeMax_TextChanged;

            this._barrierFrictionMin.Text = Parameter.DefaultParameters[AgentParameters.GEN_BarrierFriction].Minimum.ToString();
            this._barrierFrictionMin.TextChanged += _barrierFrictionMin_TextChanged;
            this._barrierFriction.Value = Parameter.DefaultParameters[AgentParameters.GEN_BarrierFriction].Value;
            this._barrierFriction.ValueChanged += _barrierFriction_ValueChanged;
            this._barrierFrictionMax.Text = Parameter.DefaultParameters[AgentParameters.GEN_BarrierFriction].Maximum.ToString();
            this._barrierFrictionMax.TextChanged += _barrierFrictionMax_TextChanged;

            this._bodyElasticityMin.Text = Parameter.DefaultParameters[AgentParameters.GEN_AgentBodyElasticity].Minimum.ToString();
            this._bodyElasticityMin.TextChanged += _bodyElasticityMin_TextChanged;
            this._bodyElasticity.Value = Parameter.DefaultParameters[AgentParameters.GEN_AgentBodyElasticity].Value;
            this._bodyElasticity.ValueChanged += _bodyElasticity_ValueChanged;
            this._bodyElasticityMax.Text = Parameter.DefaultParameters[AgentParameters.GEN_AgentBodyElasticity].Maximum.ToString();
            this._bodyElasticityMax.TextChanged += _bodyElasticityMax_TextChanged;
        }

        void _bodyElasticityMax_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            double x;
            if (double.TryParse(textBox.Text, out x))
            {
                try
                {
                    Parameter.DefaultParameters[AgentParameters.GEN_AgentBodyElasticity].Maximum = x;
                    textBox.Foreground = Brushes.Black;
                }
                catch (Exception)
                {
                    textBox.Foreground = Brushes.Red;
                }
            }
            else
            {
                textBox.Foreground = Brushes.Red;
            }
        }

        void _bodyElasticity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Parameter.DefaultParameters[AgentParameters.GEN_AgentBodyElasticity].Value = e.NewValue;
        }

        void _bodyElasticityMin_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            double x;
            if (double.TryParse(textBox.Text, out x))
            {
                try
                {
                    Parameter.DefaultParameters[AgentParameters.GEN_AgentBodyElasticity].Minimum = x;
                    textBox.Foreground = Brushes.Black;
                }
                catch (Exception)
                {
                    textBox.Foreground = Brushes.Red;
                }
            }
            else
            {
                textBox.Foreground = Brushes.Red;
            }
        }

        void _barrierFrictionMax_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            double x;
            if (double.TryParse(textBox.Text, out x))
            {
                try
                {
                    Parameter.DefaultParameters[AgentParameters.GEN_BarrierFriction].Maximum = x;
                    textBox.Foreground = Brushes.Black;
                }
                catch (Exception)
                {
                    textBox.Foreground = Brushes.Red;
                }
            }
            else
            {
                textBox.Foreground = Brushes.Red;
            }
        }

        void _barrierFriction_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Parameter.DefaultParameters[AgentParameters.GEN_BarrierFriction].Value = e.NewValue;
        }

        void _barrierFrictionMin_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            double x;
            if (double.TryParse(textBox.Text, out x))
            {
                try
                {
                    Parameter.DefaultParameters[AgentParameters.GEN_BarrierFriction].Minimum = x;
                    textBox.Foreground = Brushes.Black;
                }
                catch (Exception)
                {
                    textBox.Foreground = Brushes.Red;
                }
            }
            else
            {
                textBox.Foreground = Brushes.Red;
            }
        }

        void _accelerationMagnitudeMax_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            double x;
            if (double.TryParse(textBox.Text, out x))
            {
                try
                {
                    Parameter.DefaultParameters[AgentParameters.GEN_AccelerationMagnitude].Maximum = x;
                    textBox.Foreground = Brushes.Black;
                }
                catch (Exception)
                {
                    textBox.Foreground = Brushes.Red;
                }
            }
            else
            {
                textBox.Foreground = Brushes.Red;
            }
        }

        void _accelerationMagnitude_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Parameter.DefaultParameters[AgentParameters.GEN_AccelerationMagnitude].Value = e.NewValue;
        }

        void _accelerationMagnitudeMin_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            double x;
            if (double.TryParse(textBox.Text, out x))
            {
                try
                {
                    Parameter.DefaultParameters[AgentParameters.GEN_AccelerationMagnitude].Minimum = x;
                    textBox.Foreground = Brushes.Black;
                }
                catch (Exception)
                {
                    textBox.Foreground = Brushes.Red;
                }
            }
            else
            {
                textBox.Foreground = Brushes.Red;
            }
        }

        void _maxRepulsionChangeRate_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            double x;
            if (double.TryParse(textBox.Text, out x))
            {
                try
                {
                    Parameter.DefaultParameters[AgentParameters.GEN_MaximumRepulsion].Maximum = x;
                    textBox.Foreground = Brushes.Black;
                }
                catch (Exception)
                {
                    textBox.Foreground = Brushes.Red;
                }
            }
            else
            {
                textBox.Foreground = Brushes.Red;
            }
        }

        void _repulsionChangeRate_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Parameter.DefaultParameters[AgentParameters.GEN_MaximumRepulsion].Value = e.NewValue;
        }

        void _minRepulsionChangeRate_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            double x;
            if (double.TryParse(textBox.Text, out x))
            {
                try
                {
                    Parameter.DefaultParameters[AgentParameters.GEN_MaximumRepulsion].Minimum = x;
                    textBox.Foreground = Brushes.Black;
                }
                catch (Exception)
                {
                    textBox.Foreground = Brushes.Red;
                }
            }
            else
            {
                textBox.Foreground = Brushes.Red;
            }
        }

        void _maxBarrierRepulsionRange_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            double x;
            if (double.TryParse(textBox.Text, out x))
            {
                try
                {
                    Parameter.DefaultParameters[AgentParameters.GEN_BarrierRepulsionRange].Maximum = x;
                    textBox.Foreground = Brushes.Black;
                }
                catch (Exception)
                {
                    textBox.Foreground = Brushes.Red;
                }
            }
            else
            {
                textBox.Foreground = Brushes.Red;
            }
        }

        void _barrierRepulsionRange_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Parameter.DefaultParameters[AgentParameters.GEN_BarrierRepulsionRange].Value = e.NewValue;
        }

        void _minBarrierRepulsionRange_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            double x;
            if (double.TryParse(textBox.Text, out x))
            {
                try
                {
                    Parameter.DefaultParameters[AgentParameters.GEN_BarrierRepulsionRange].Minimum = x;
                    textBox.Foreground = Brushes.Black;
                }
                catch (Exception)
                {
                    textBox.Foreground = Brushes.Red;
                }
            }
            else
            {
                textBox.Foreground = Brushes.Red;
            }
        }

        void _desirabilityWeightMax_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            double x;
            if (double.TryParse(textBox.Text, out x))
            {
                try
                {
                    Parameter.DefaultParameters[AgentParameters.OPT_DesirabilityDistributionLambdaFactor].Maximum = x;
                    textBox.Foreground = Brushes.Black;
                }
                catch (Exception)
                {
                    textBox.Foreground = Brushes.Red;
                }
            }
            else
            {
                textBox.Foreground = Brushes.Red;
            }
        }

        void _desirabilityWeight_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Parameter.DefaultParameters[AgentParameters.OPT_DesirabilityDistributionLambdaFactor].Value = e.NewValue;
        }

        void _desirabilityWeightMin_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            double x;
            if (double.TryParse(textBox.Text, out x))
            {
                try
                {
                    Parameter.DefaultParameters[AgentParameters.OPT_DesirabilityDistributionLambdaFactor].Minimum = x;
                    textBox.Foreground = Brushes.Black;
                }
                catch (Exception)
                {
                    textBox.Foreground = Brushes.Red;
                }
            }
            else
            {
                textBox.Foreground = Brushes.Red;
            }
        }

        void _angleWeightMax_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            double x;
            if (double.TryParse(textBox.Text, out x))
            {
                try
                {
                    Parameter.DefaultParameters[AgentParameters.OPT_AngleDistributionLambdaFactor].Maximum = x;
                    textBox.Foreground = Brushes.Black;
                }
                catch (Exception)
                {
                    textBox.Foreground = Brushes.Red;
                }
            }
            else
            {
                textBox.Foreground = Brushes.Red;
            }
        }

        void _angleWeight_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Parameter.DefaultParameters[AgentParameters.OPT_AngleDistributionLambdaFactor].Value = e.NewValue;
        }

        void _angleWeightMin_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            double x;
            if (double.TryParse(textBox.Text, out x))
            {
                try
                {
                    Parameter.DefaultParameters[AgentParameters.OPT_AngleDistributionLambdaFactor].Minimum = x;
                    textBox.Foreground = Brushes.Black;
                }
                catch (Exception)
                {
                    textBox.Foreground = Brushes.Red;
                }
            }
            else
            {
                textBox.Foreground = Brushes.Red;
            }
        }

        void _maxDecisionMakingPeriod_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            double x;
            if (double.TryParse(textBox.Text, out x))
            {
                try
                {
                    Parameter.DefaultParameters[AgentParameters.OPT_DecisionMakingPeriodLambdaFactor].Maximum = x;
                    textBox.Foreground = Brushes.Black;
                }
                catch (Exception)
                {
                    textBox.Foreground = Brushes.Red;
                }
            }
            else
            {
                textBox.Foreground = Brushes.Red;
            }
        }

        void _decisionMakingPeriod_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Parameter.DefaultParameters[AgentParameters.OPT_DecisionMakingPeriodLambdaFactor].Value = e.NewValue;
        }

        void _minDecisionMakingPeriod_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            double x;
            if (double.TryParse(textBox.Text, out x))
            {
                try
                {
                    Parameter.DefaultParameters[AgentParameters.OPT_DecisionMakingPeriodLambdaFactor].Minimum = x;
                    textBox.Foreground = Brushes.Black;
                }
                catch (Exception)
                {
                    textBox.Foreground = Brushes.Red;
                }
            }
            else
            {
                textBox.Foreground = Brushes.Red;
            }
        }

        void _viewAngleMax_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            double x;
            if (double.TryParse(textBox.Text, out x))
            {
                try
                {
                    Parameter.DefaultParameters[AgentParameters.GEN_VisibilityAngle].Maximum = x;
                    textBox.Foreground = Brushes.Black;
                }
                catch (Exception)
                {
                    textBox.Foreground = Brushes.Red;
                }
            }
            else
            {
                textBox.Foreground = Brushes.Red;
            }
        }

        void _viewAngle_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Parameter.DefaultParameters[AgentParameters.GEN_VisibilityAngle].Value = e.NewValue;
        }

        void _viewAngleMin_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            double x;
            if (double.TryParse(textBox.Text, out x))
            {
                try
                {
                    Parameter.DefaultParameters[AgentParameters.GEN_VisibilityAngle].Minimum = x;
                    textBox.Foreground = Brushes.Black;
                }
                catch (Exception)
                {
                    textBox.Foreground = Brushes.Red;
                }
            }
            else
            {
                textBox.Foreground = Brushes.Red;
            }
        }

        void _bodySizeMax_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            double x;
            if (double.TryParse(textBox.Text, out x))
            {
                try
                {
                    Parameter.DefaultParameters[AgentParameters.GEN_BodySize].Maximum = x;
                    textBox.Foreground = Brushes.Black;
                }
                catch (Exception)
                {
                    textBox.Foreground = Brushes.Red;
                }
            }
            else
            {
                textBox.Foreground = Brushes.Red;
            }
        }

        void _bodySize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Parameter.DefaultParameters[AgentParameters.GEN_BodySize].Value = e.NewValue;
        }

        void _bodySizeMin_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            double x;
            if (double.TryParse(textBox.Text, out x))
            {
                try
                {
                    Parameter.DefaultParameters[AgentParameters.GEN_BodySize].Minimum = x;
                    textBox.Foreground = Brushes.Black;
                }
                catch (Exception)
                {
                    textBox.Foreground = Brushes.Red;
                }
            }
            else
            {
                textBox.Foreground = Brushes.Red;
            }
        }

        void _angularVelocityMax_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            double x;
            if (double.TryParse(textBox.Text, out x))
            {
                try
                {
                    Parameter.DefaultParameters[AgentParameters.GEN_AngularVelocity].Maximum = x;
                    textBox.Foreground = Brushes.Black;
                }
                catch (Exception)
                {
                    textBox.Foreground = Brushes.Red;
                }
            }
            else
            {
                textBox.Foreground = Brushes.Red;
            }
        }

        void _angularVelocity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                Parameter.DefaultParameters[AgentParameters.GEN_AngularVelocity].Value = e.NewValue;
            }
            catch (Exception ) { }
        }

        void _angularVelocityMin_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            double x;
            if (double.TryParse(textBox.Text, out x))
            {
                try
                {
                    Parameter.DefaultParameters[AgentParameters.GEN_AngularVelocity].Minimum = x;
                    textBox.Foreground = Brushes.Black;
                }
                catch (Exception)
                {
                    textBox.Foreground = Brushes.Red;
                }
            }
            else
            {
                textBox.Foreground = Brushes.Red;
            }
        }

        void _velocityMax_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            double x;
            if (double.TryParse(textBox.Text, out x))
            {
                try
                {
                    Parameter.DefaultParameters[AgentParameters.GEN_VelocityMagnitude].Maximum = x;
                    textBox.Foreground = Brushes.Black;
                }
                catch (Exception)
                {
                    textBox.Foreground = Brushes.Red;
                }
            }
            else
            {
                textBox.Foreground = Brushes.Red;
            }
        }

        void _velocity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Parameter.DefaultParameters[AgentParameters.GEN_VelocityMagnitude].Value = e.NewValue;
        }

        void _velocityMin_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            double x;
            if (double.TryParse(textBox.Text, out x))
            {
                try
                {
                    Parameter.DefaultParameters[AgentParameters.GEN_VelocityMagnitude].Minimum = x;
                    textBox.Foreground = Brushes.Black;
                }
                catch (Exception)
                {
                    textBox.Foreground = Brushes.Red;
                }
            }
            else
            {
                textBox.Foreground = Brushes.Red;
            }
        }



    }

}

