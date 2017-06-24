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
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using SpatialAnalysis.CellularEnvironment;
using SpatialAnalysis.Data;
using SpatialAnalysis.Geometry;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Threading;
using SpatialAnalysis.Data.Visualization;
using SpatialAnalysis.Agents;
using SpatialAnalysis.Agents.OptionalScenario;
using SpatialAnalysis.Miscellaneous;

namespace SpatialAnalysis.IsovistUtility.IsovistVisualization
{
    /// <summary>
    /// Interaction logic for AgentEscape_RouteParameterSetting.xaml
    /// </summary>
    public partial class AgentRouteParameterSetting : Window
    {
        private OSMDocument _host { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="AgentRouteParameterSetting"/> class.
        /// </summary>
        /// <param name="host">The host.</param>
        public AgentRouteParameterSetting(OSMDocument host)
        {
            InitializeComponent();
            this._host = host;
            this.IsovistExternalDepth.Text = this._host.AgentIsovistExternalDepth.ToString();
            this.AngleIntercept.Text = this._host.MaximumNumberOfDestinations.ToString();
            this._done.Click += _done_Click1;
            this._setCosts.Click += _setCosts_Click;
        }

        void _setCosts_Click(object sender, RoutedEventArgs e)
        {
            var costSetter = new SpatialDataControlPanel(this._host, IncludedDataTypes.SpatialData);
            costSetter.Owner = this._host;
            costSetter.ShowDialog();
        }

        void _done_Click1(object sender, RoutedEventArgs e)
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

            #region Check if spatial data has been assigned to the static cost
            int includedData = 0;
            foreach (Function function in this._host.cellularFloor.AllSpatialDataFields.Values)
            {
                SpatialDataField dataField = function as SpatialDataField;
                if (dataField != null)
                {
                    if (dataField.IncludeInActivityGeneration)
                    {
                        includedData++;
                    }
                }
            }
            if (includedData == 0)
            {
                var res = MessageBox.Show("There is no a spatial data with a defined cost method assigned to the calculation of the 'Escape Routes'.\nDo you want to continue?",
                    "Spatial Data Cost", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res == MessageBoxResult.No)
                {
                    return;
                }
            }
            var staticCost = this._host.cellularFloor.GetStaticCost();
            #endregion
            
            #region Calculate
            var allAgentScapeRoutes = new List<AgentEscapeRoutes>();
            foreach (Cell item in this._host.cellularFloor.Cells)
            {
                if (item.BarrierBufferOverlapState == OverlapState.Outside)
                {
                    allAgentScapeRoutes.Add(new AgentEscapeRoutes(item));
                }
            }
            if (allAgentScapeRoutes.Count==0)
            {
                MessageBox.Show("There is no cell out of barrier buffers");
                this._done.Content = "Close";
                this._done.Click -= _done_Click1;
                this._done.Click += _done_Click2;
                return;
            }
            this.progressState.Visibility = System.Windows.Visibility.Visible;
            this.progressBar.Maximum = allAgentScapeRoutes.Count;
            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            /*
            foreach (var item in allAgentScapeRoutes)
            {
                item.ComputeDestinations(this._host.AgentIsovistExternalDepth, this._host.AgentIsovistInternalDepth,
                    this._host.MaximumNumberOfDestinations, this._host.cellularFloor, staticCost, filter, 0.0000001d);
            }
             */

            Parallel.ForEach(allAgentScapeRoutes, (a) =>
            {
                //step 1
                a.ComputeDestinations(this._host.AgentIsovistExternalDepth,
                    this._host.MaximumNumberOfDestinations, this._host.cellularFloor, staticCost, 0.0000001d);

                Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    (SendOrPostCallback)delegate
                    {
                        double val = this.progressBar.Value + 1;
                        this.progressBar.SetValue(ProgressBar.ValueProperty, val);
                    }
                    , null);
                Dispatcher.BeginInvoke(DispatcherPriority.Background,
                    (SendOrPostCallback)delegate
                    {
                        double x = this.progressBar.Value / this.progressBar.Maximum;
                        int percent = (int)(x * 100);
                        int minutes = (int)Math.Floor(timer.Elapsed.TotalMinutes);
                        int seconds = (int)(timer.Elapsed.Seconds);
                        string message = string.Format("{0} minutes and {1} seconds\n%{2} completed",
                            minutes.ToString(), seconds.ToString(), percent.ToString());
                        this.report.SetValue(TextBlock.TextProperty, message);
                    }
                    , null);
                Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => { })).Wait(TimeSpan.FromMilliseconds(1));
            });
            
            var t = timer.Elapsed.TotalMilliseconds;
            timer.Stop();
            #endregion

            int nulls = 0;
            Dictionary<Cell, AgentEscapeRoutes> data = new Dictionary<Cell, AgentEscapeRoutes>();
            foreach (var item in allAgentScapeRoutes)
            {
                if (item.Destinations== null)
                {
                    nulls++;
                }
                else
                {
                    data.Add(item.VantageCell, item);
                }
            }
            allAgentScapeRoutes.Clear();
            allAgentScapeRoutes = null;
            staticCost.Clear();
            staticCost = null;
            //includedData.Clear();
            //includedData = null;
            this.progressState.Visibility = System.Windows.Visibility.Collapsed;

            StringBuilder sb = new StringBuilder();
            int min = (int)Math.Floor(timer.Elapsed.TotalMinutes);
            int sec = (int)(timer.Elapsed.Seconds);
            int average = (int)(timer.Elapsed.TotalMilliseconds * 1000 / progressBar.Maximum);
            sb.AppendLine("Escape Route Analysis Results:".ToUpper());
            sb.AppendLine(string.Format("Analyzed Isovists: \t\t{0}", ((int)progressBar.Maximum).ToString()));
            sb.AppendLine(string.Format("Blind Access Isovists: \t{0}", nulls.ToString()));
            sb.AppendLine(string.Format("Total Time: \t\t{0} Min and {1} Sec", min.ToString(), sec.ToString()));
            sb.AppendLine(string.Format("Average Time: \t\t{0} MS (per isovist)", (((double)average)/1000).ToString()));

            string text = sb.ToString();
            this.finalReport.Visibility = System.Windows.Visibility.Visible;
            this.finalReport.Text = text;

            this._done.Content = "Close";
            this._done.Click -= _done_Click1;
            this._done.Click += _done_Click2;
            this._host.AgentScapeRoutes = data;

            if (nulls!=0)
            {
                this._visualize.Visibility = System.Windows.Visibility.Visible;
                this._visualize.Click += _visualize_Click;
            }

        }

        void _visualize_Click(object sender, RoutedEventArgs e)
        {
            HashSet<UVLine> lines = new HashSet<UVLine>();
            foreach (var item in this._host.cellularFloor.Cells)
            {
                if (item.BarrierBufferOverlapState == OverlapState.Outside &&
                    !this._host.AgentScapeRoutes.ContainsKey(item))
                {
                    lines.UnionWith(item.ToUVLines(this._host.cellularFloor.CellSize));
                }
            }
            //visualizing cells with no escape routes
            this._host.OSM_to_BIM.VisualizeLines(lines, 0);
        }

        void _done_Click2(object sender, RoutedEventArgs e)
        {
            this._host = null;
            this.Close();
        }


    }
}

