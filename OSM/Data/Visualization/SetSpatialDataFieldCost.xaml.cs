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
using SpatialAnalysis.Data.CostFormulaSet;

namespace SpatialAnalysis.Data.Visualization
{
    /// <summary>
    /// Interaction logic for SetSpatialDataFieldCost.xaml
    /// </summary>
    public partial class SetSpatialDataFieldCost : UserControl
    {
        private Function _function { get; set; }
        private SpatialDataField _spatialDataField { get; set; }
        private OSMDocument _host;
        /// <summary>
        /// Initializes a new instance of the <see cref="SetSpatialDataFieldCost"/> class.
        /// </summary>
        /// <param name="host">The main document to which this control belongs.</param>
        /// <param name="function">The function or spatial data field.</param>
        public SetSpatialDataFieldCost(OSMDocument host,Function function)
        {
            InitializeComponent();
            this._host = host;
            this._function = function;
            this._spatialDataFieldName.Text = this._function.Name;
            this._spatialDataField = function as SpatialDataField;
            if (this._spatialDataField != null)
            {
                this._method.Items.Add(CostCalculationMethod.RawValue);
                this._method.Items.Add(CostCalculationMethod.WrittenFormula);
                this._method.Items.Add(CostCalculationMethod.Interpolation);
            }
            else
            {
                this._method.Items.Add(CostCalculationMethod.RawValue);
                this._method.Items.Add(CostCalculationMethod.WrittenFormula);
                this._method.Items.Add(CostCalculationMethod.Interpolation);
            }

            this._method.SelectedItem = function.CostCalculationType;
            this._method.SelectionChanged += new SelectionChangedEventHandler(_method_SelectionChanged);
            this._include.IsChecked = function.IncludeInActivityGeneration;
            this._vis.Click += new RoutedEventHandler(_vis_Click);
        }

        void _vis_Click(object sender, RoutedEventArgs e)
        {
            VisualizeFunction visalizer = new VisualizeFunction(this._function);
            visalizer.ShowDialog();
            visalizer = null;
        }

        void _method_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var type = (CostCalculationMethod)this._method.SelectedValue;
            switch (type)
            {
                case CostCalculationMethod.Interpolation:
                    InterpolationFormulaSet setInterpolation = new InterpolationFormulaSet(this._spatialDataField);
                    setInterpolation.ShowDialog();
                    this._spatialDataField.SetInterpolation(setInterpolation.interpolation);
                    setInterpolation.interpolation = null;
                    setInterpolation = null;
                    break;
                case CostCalculationMethod.WrittenFormula:
                    TextFormulaSet setTextFormula = new TextFormulaSet(this._host, this._spatialDataField);
                    setTextFormula.ShowDialog();
                    this._spatialDataField.SetStringFormula(setTextFormula.CostFunction);
                    setTextFormula.CostFunction = null;
                    setTextFormula = null;
                    break;
                case CostCalculationMethod.RawValue:
                    this._function.SetRawValue();
                    break;
                default:
                    break;
            }
        }

        private void _include_Checked(object sender, RoutedEventArgs e)
        {
            this._function.IncludeInActivityGeneration = true;
        }

        private void _include_Unchecked(object sender, RoutedEventArgs e)
        {
            this._function.IncludeInActivityGeneration = false;
        }
    }
}

