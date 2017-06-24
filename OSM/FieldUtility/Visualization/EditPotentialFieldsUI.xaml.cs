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
using SpatialAnalysis.Data;
using SpatialAnalysis.Data.Visualization;
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
using SpatialAnalysis.Miscellaneous;

namespace SpatialAnalysis.FieldUtility.Visualization
{
    /// <summary>
    /// Interaction logic for EditActivitiesUI.xaml
    /// </summary>
    public partial class EditActivitiesUI : Window
    {
        #region _host Definition
        public static DependencyProperty _hostProperty =
            DependencyProperty.Register("_host", typeof(OSMDocument), typeof(EditActivitiesUI),
            new FrameworkPropertyMetadata(null, _hostPropertyChanged, _propertyCoerce));
        private OSMDocument _host
        {
            get { return (OSMDocument)GetValue(_hostProperty); }
            set { SetValue(_hostProperty, value); }
        }
        private static void _hostPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            EditActivitiesUI createTrail = (EditActivitiesUI)obj;
            createTrail._host = (OSMDocument)args.NewValue;
        }
        private static object _propertyCoerce(DependencyObject obj, object value)
        {
            return value;
        }
        #endregion


        public EditActivitiesUI(OSMDocument host)
        {
            InitializeComponent();
            this._host = host;
            this._availableFields.ItemsSource = this._host.AllActivities.Values;
            this._availableFields.DisplayMemberPath = "Name";
            this._save.Click += this._save_Click;
            this._load.Click += _load_Click;
            this.OKAY.Click += OKAY_Click;
            this._setCosts.Click += _setCosts_Click;
            this._parameters.Click += _parameters_Click;
        }

        void _load_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Title = "Load Activities";
            dlg.DefaultExt = ".act";
            dlg.Filter = "act documents (.act)|*.act";
            Nullable<bool> result = dlg.ShowDialog(this._host);
            string fileAddress = "";
            if (result == true)
            {
                fileAddress = dlg.FileName;
            }
            else
            {
                return;
            }          
            List<string> lines = new List<string>();
            using (System.IO.StreamReader sr = new System.IO.StreamReader(fileAddress))
            {
                string line = string.Empty;
                while ((line = sr.ReadLine()) != null)
                {
                    if (!(string.IsNullOrEmpty(line) || string.IsNullOrWhiteSpace(line)))
                    {
                        lines.Add(line);
                    }
                }
            }
            if (lines.Count == 0)
            {
                MessageBox.Show("The file includes no input",
                    "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            int n = 7;
            if (this._includeGuassian.IsChecked.Value)
            {
                double _n = 0;
                if (!double.TryParse(this._range_guassian.Text, out _n))
                {
                    MessageBox.Show("'Neighborhood Size' should be a valid number larger than 0",
                        "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                n = (int)_n;
                if (n < 1)
                {
                    MessageBox.Show("'Neighborhood Size' should be a valid number larger than 0",
                        "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
            }
            double r = 0;
            if (!double.TryParse(this._range.Text, out r))
            {
                MessageBox.Show("'Neighborhood Size' should be a number larger than 1",
                    "Missing Input", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            int range = (int)r;
            if (range < 1)
            {
                MessageBox.Show("'Neighborhood Size' should be a number larger than 1",
                    "Missing Input", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (this._host.FieldGenerator == null || this._host.FieldGenerator.Range != range)
            {
                this._host.FieldGenerator = new SpatialDataCalculator(this._host, range, OSMDocument.AbsoluteTolerance);
            }
            List<ActivityDestination> destinations = new List<ActivityDestination>();
            for (int i = 0; i < lines.Count/4; i++)
            {
                try
                {
                    ActivityDestination destination = ActivityDestination.FromString(lines, i * 4, this._host.cellularFloor);
                    if (this._host.AllActivities.ContainsKey(destination.Name))
                    {
                        MessageBox.Show("An activity with the same name exists!", "", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else
                    {
                        destinations.Add(destination);
                    }
                    
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.Report());
                }

            }
            if (destinations.Count == 0)
            {
                return;
            }
            Dispatcher.Invoke(new Action(() =>
            {
                this._report.Visibility = System.Windows.Visibility.Visible;
                this.grid.IsEnabled = false;
                this._progressBar.Maximum = destinations.Count;
                this._activityName.Text = string.Empty;
            }), System.Windows.Threading.DispatcherPriority.ContextIdle);
            int count = 0;
            foreach (var item in destinations)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    this._activityName.Text = item.Name;
                }), System.Windows.Threading.DispatcherPriority.ContextIdle);
                count++;
                try
                {
                    Activity newField = null;
                    if (this._includeAngularCost.IsChecked.Value)
                    {
                        //double angularVelocityWeight = 0;
                        if (Parameter.DefaultParameters[AgentParameters.MAN_AngularDeviationCost].Value <= 0)
                        {
                            MessageBox.Show("Cost of angular change should be exclusively larger than zero", "Activity Generation",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                        newField = this._host.FieldGenerator.GetDynamicActivity(item, Parameter.DefaultParameters[AgentParameters.MAN_AngularDeviationCost].Value);
                        if (!newField.TrySetEngagementTime(item.MinimumEngagementTime, item.MinimumEngagementTime))
                        {
                            throw new ArgumentException("Failed to set activity engagement duration!");
                        }
                    }
                    else
                    {
                        newField = this._host.FieldGenerator.GetStaticActivity(item);
                        if (!newField.TrySetEngagementTime(item.MinimumEngagementTime, item.MaximumEngagementTime))
                        {
                            throw new ArgumentException("Failed to set activity engagement duration!");
                        }
                    }
                    if (this._includeGuassian.IsChecked.Value)
                    {
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
                        var data = this._host.ViewBasedGaussianFilter.GetFilteredValues(newField);
                        newField.Potentials = data;
                    }
                    this._host.AddActivity(newField);
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.Report());
                }
                this.updateProgressBar(count);
            }
            this.Close();
        }
        void _parameters_Click(object sender, RoutedEventArgs e)
        {
            ParameterSetting paramSetting = new ParameterSetting(this._host, false);
            paramSetting.Owner = this._host;
            paramSetting.ShowDialog();
            paramSetting = null;
        }
        void _setCosts_Click(object sender, RoutedEventArgs e)
        {
            var controlPanel = new SpatialDataControlPanel(this._host, IncludedDataTypes.SpatialData);
            controlPanel.Owner = this._host;
            controlPanel.ShowDialog();
        }
        void OKAY_Click(object sender, RoutedEventArgs e)
        {
            if (this._availableFields.SelectedItems.Count == 0)
            {
                MessageBox.Show("Select activities to continue",
                    "Missing Input", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            int n = 7;
            if (this._includeGuassian.IsChecked.Value)
            {
                double _n = 0;
                if (!double.TryParse(this._range_guassian.Text, out _n))
                {
                    MessageBox.Show("'Neighborhood Size' should be a valid number larger than 0",
                        "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
                n = (int)_n;
                if (n < 1)
                {
                    MessageBox.Show("'Neighborhood Size' should be a valid number larger than 0",
                        "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }
            }
            double r = 0;
            if (!double.TryParse(this._range.Text, out r))
            {
                MessageBox.Show("'Neighborhood Size' should be a number larger than 1",
                    "Missing Input", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            int range = (int)r;
            if (range < 1)
            {
                MessageBox.Show("'Neighborhood Size' should be a number larger than 1",
                    "Missing Input", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (this._host.FieldGenerator == null || this._host.FieldGenerator.Range != range)
            {
                this._host.FieldGenerator = new SpatialDataCalculator(this._host, range, OSMDocument.AbsoluteTolerance);
            }
            Dispatcher.Invoke(new Action(() =>
            {
                this._report.Visibility = System.Windows.Visibility.Visible;
                this.grid.IsEnabled = false;
                this._progressBar.Maximum = this._availableFields.SelectedItems.Count;
                this._activityName.Text = string.Empty;
            }), System.Windows.Threading.DispatcherPriority.ContextIdle);
            int count = 0;
            foreach (var item in this._availableFields.SelectedItems)
            {
                count++;
                try
                {
                    Activity oldField = (Activity)item;
                    Activity newField = null;
                    Dispatcher.Invoke(new Action(() =>
                    {
                        this._activityName.Text = oldField.Name;
                    }), System.Windows.Threading.DispatcherPriority.ContextIdle);
                    if (this._includeAngularCost.IsChecked.Value)
                    {
                        //double angularVelocityWeight = 0;
                        if (Parameter.DefaultParameters[AgentParameters.MAN_AngularDeviationCost].Value <= 0)
                        {
                            MessageBox.Show("Cost of angular change should be exclusively larger than zero", "Activity Generation",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                        newField = this._host.FieldGenerator.GetDynamicActivity(oldField.Origins, oldField.DestinationArea, oldField.DefaultState, oldField.Name,
                            Parameter.DefaultParameters[AgentParameters.MAN_AngularDeviationCost].Value);
                    }
                    else
                    {
                        newField = this._host.FieldGenerator.GetStaticActivity(oldField.Origins, oldField.DestinationArea, oldField.DefaultState, oldField.Name);
                    }
                    if (!newField.TrySetEngagementTime(oldField.MinimumEngagementTime, oldField.MaximumEngagementTime))
                    {
                        MessageBox.Show("Cannot Set activity engagement time", "Activity Engagement Time", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    if (this._includeGuassian.IsChecked.Value)
                    {
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
                        var data = this._host.ViewBasedGaussianFilter.GetFilteredValues(oldField);
                        this._host.AllActivities[oldField.Name].Potentials = data;
                    }
                    else
                    {
                        this._host.AllActivities[oldField.Name].Potentials = newField.Potentials;
                    }
                    newField = null;
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.Report());
                }
                this.updateProgressBar(count);
            }
            this.Close();
        }
        void updateProgressBar(double percent)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                this._progressBar.SetValue(ProgressBar.ValueProperty, percent);
            }), System.Windows.Threading.DispatcherPriority.ContextIdle);
        }

        void _save_Click(object sender, RoutedEventArgs e)
        {
            if (this._availableFields.SelectedItems.Count == 0)
            {
                MessageBox.Show("Select activities to continue",
                    "Missing Input", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Title = "Save Activities";
            dlg.DefaultExt = ".act";
            dlg.Filter = "ACT documents (.act)|*.act";
            Nullable<bool> result = dlg.ShowDialog(this._host);
            string fileAddress = "";
            if (result == true)
            {
                fileAddress = dlg.FileName;
            }
            else
            {
                return;
            }
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(fileAddress))
            {
                foreach (var item in this._availableFields.SelectedItems)
                {
                    Activity activity = (Activity)item;
                    sw.WriteLine(activity.GetStringRepresentation());
                }
                sw.Close();
            }
        }
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            this._availableFields.ItemsSource = null;
        }
    }
}

