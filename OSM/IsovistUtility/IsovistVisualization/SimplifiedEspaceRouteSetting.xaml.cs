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
using SpatialAnalysis.CellularEnvironment;
using SpatialAnalysis.Data;
using SpatialAnalysis.Data.Visualization;
using SpatialAnalysis.Agents;
using SpatialAnalysis.Agents.OptionalScenario;
using SpatialAnalysis.Miscellaneous;

namespace SpatialAnalysis.IsovistUtility.IsovistVisualization
{
    /// <summary>
    /// Interaction logic for SimplifiedEspaceRouteSetting.xaml
    /// </summary>
    public partial class SimplifiedEspaceRouteSetting : Window
    {
        OSMDocument _host;
        /// <summary>
        /// Initializes a new instance of the <see cref="SimplifiedEspaceRouteSetting"/> class.
        /// </summary>
        /// <param name="host">The main document to which this window belongs.</param>
        public SimplifiedEspaceRouteSetting(OSMDocument host)
        {
            InitializeComponent();
            this._host = host;
            this._done.Click += _done_Click;
            this._setCosts.Click += _setCosts_Click;
            this.IsovistExternalDepth.Text = this._host.AgentIsovistExternalDepth.ToString();
            this.AngleIntercept.Text = this._host.MaximumNumberOfDestinations.ToString();
        }

        void _setCosts_Click(object sender, RoutedEventArgs e)
        {
            var costSetter = new SpatialDataControlPanel(this._host, IncludedDataTypes.SpatialData);
            costSetter.ShowDialog();
        }

        void _done_Click(object sender, RoutedEventArgs e)
        {
            #region validate input
            double externalDepth;
            if (!double.TryParse(this.IsovistExternalDepth.Text, out externalDepth))
            {
                MessageBox.Show("Inappropriate input for external isovist depth");
                return;
            }
            if (externalDepth <= 0)
            {
                MessageBox.Show("External isovist depth should be larger than zero");
                return;
            }
            try
            {
                this._host.Parameters[AgentParameters.OPT_IsovistExternalDepth.ToString()].Value = externalDepth;
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Report());
                return;
            }
            int num;
            if (!int.TryParse(this.AngleIntercept.Text, out num))
            {
                MessageBox.Show("Inappropriate input for 'Maximum Number of Destinations in Isovist Perimeter'");
                return;
            }
            if (num <= 19)
            {
                MessageBox.Show("'Maximum Number of Destinations in Isovist Perimeter' should be larger than 20");
                return;
            }
            try
            {
                this._host.Parameters[AgentParameters.OPT_NumberOfDestinations.ToString()].Value = num;
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Report());
                return;
            }
            #endregion
            this.Close();
        }
    }
}

