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
using System.Collections.ObjectModel;
using SpatialAnalysis.FieldUtility;
using SpatialAnalysis.Miscellaneous;

namespace SpatialAnalysis.Agents.MandatoryScenario.Visualization
{
    /// <summary>
    /// Interaction logic for GenerateSequenceUI.xaml
    /// </summary>
    public partial class GenerateSequenceUI : Window
    {
        #region Host Definition
        private static DependencyProperty _hostProperty =
            DependencyProperty.Register("_host", typeof(OSMDocument), typeof(GenerateSequenceUI),
            new FrameworkPropertyMetadata(null, GenerateSequenceUI.hostPropertyChanged, GenerateSequenceUI.PropertyCoerce));
        private OSMDocument _host
        {
            get { return (OSMDocument)GetValue(_hostProperty); }
            set { SetValue(_hostProperty, value); }
        }
        private static void hostPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            GenerateSequenceUI instance = (GenerateSequenceUI)obj;
            instance._host = (OSMDocument)args.NewValue;
        }
        #endregion
        #region Sequence Definition
        private static DependencyProperty _sequenceProperty =
            DependencyProperty.Register("_sequence", typeof(Sequence), typeof(GenerateSequenceUI),
            new FrameworkPropertyMetadata(null, GenerateSequenceUI.sequencePropertyChanged, GenerateSequenceUI.PropertyCoerce));
        private Sequence _sequence
        {
            get { return (Sequence)GetValue(_sequenceProperty); }
            set { SetValue(_sequenceProperty, value); }
        }
        private static void sequencePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            GenerateSequenceUI instance = (GenerateSequenceUI)obj;
            instance._sequence = (Sequence)args.NewValue;
        }
        #endregion
        private static object PropertyCoerce(DependencyObject obj, object value)
        {
            return value;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateSequenceUI"/> class.
        /// </summary>
        /// <param name="host">The main document to which this window belongs.</param>
        public GenerateSequenceUI(OSMDocument host)
        {
            InitializeComponent();
            this._host = host;
            foreach (var item in this._host.AllActivities.Values)
	        {
                this._availablePoentialFields.Items.Add(item);
	        }
            this._up.Click += new RoutedEventHandler(_up_Click);
            this._add.Click += this._add_Click;
            this._remove.Click += this._remove_Click;
            this._down.Click += new RoutedEventHandler(_down_Click);
            this._okay.Click += this._okay_Click;
            this._addVisualAwareness.Click += _visibilityEvent_Click;
            this._existingSequences.ItemsSource = this._host.AgentMandatoryScenario.Sequences;
            this._existingSequences.DisplayMemberPath = "Name";
            this._sequenceName.Text = "Sequence " + (this._host.AgentMandatoryScenario.Sequences.Count + 1).ToString();
            this._sequenceName.Focus();
            this._sequenceName.Select(0, this._sequenceName.Text.Length);
            this._includeVisualAwarenessField.Unchecked += _includeVisualAwarenessField_Unchecked;

        }
        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateSequenceUI"/> class.
        /// </summary>
        /// <param name="host">The main document to which this window belongs.</param>
        /// <param name="sequence">The sequence.</param>
        public GenerateSequenceUI(OSMDocument host, Sequence sequence)
        {
            InitializeComponent();
            this._host = host;
            this._sequence = sequence;
            foreach (var item in this._host.AllActivities.Values)
            {
                this._availablePoentialFields.Items.Add(item);
                //if (!this._sequence.ActivityNames.Contains(item.Name))
                //{
                //    this._availablePoentialFields.Items.Add(item);
                //}
            }
            for (int i = 0; i < this._sequence.ActivityCount; i++)
            {
                this._orderedActivities.Items.Add(this._host.AllActivities[this._sequence.ActivityNames[i]]);
            }
            this._sequenceName.Visibility = System.Windows.Visibility.Collapsed;
            this._selectedSequenceName.Visibility = System.Windows.Visibility.Visible;
            this._selectedSequenceName.Text = sequence.Name;
            this._activationTime.Text = sequence.ActivationLambdaFactor.ToString();
            this._up.Click += new RoutedEventHandler(_up_Click);
            this._add.Click += this._add_Click;
            this._remove.Click += this._remove_Click;
            this._down.Click += new RoutedEventHandler(_down_Click);
            this._okay.Click += this._edit_Click;
            this._addVisualAwareness.Click += _visibilityEvent_Click;
            this._existingSequences.ItemsSource = this._host.AgentMandatoryScenario.Sequences;
            this._existingSequences.DisplayMemberPath = "Name";
            this._okay.Content = "Finish";
            this._includeVisualAwarenessField.Unchecked += _includeVisualAwarenessField_Unchecked;
            this._includeVisualAwarenessField.IsChecked = this._sequence.HasVisualAwarenessField;
        }

        void _includeVisualAwarenessField_Unchecked(object sender, RoutedEventArgs e)
        {
            if (this._sequence != null && this._sequence.HasVisualAwarenessField)
            {
                this._sequence.VisualAwarenessField = null;
            }
            this._host.sequenceVisibilityEventHost.Clear();
        }
        void _down_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int i = this._orderedActivities.SelectedIndex;
                if (this._orderedActivities.Items.Count > 1 && i < this._orderedActivities.Items.Count - 1)
                {
                    object field = this._orderedActivities.SelectedItem;
                    this._orderedActivities.Items.RemoveAt(i);
                    this._orderedActivities.Items.Insert(i + 1, field);
                    this._orderedActivities.SelectedIndex = i + 1;
                }
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Report());
            }
        }

        void _up_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int i = this._orderedActivities.SelectedIndex;
                if (this._orderedActivities.Items.Count > 1 && i > 0)
                {
                    object field = this._orderedActivities.SelectedItem;
                    this._orderedActivities.Items.RemoveAt(i);
                    this._orderedActivities.Items.Insert(i - 1, field);
                    this._orderedActivities.SelectedIndex = i - 1;
                }
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Report());
            }
        }
        void _okay_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(this._sequenceName.Text) || string.IsNullOrWhiteSpace(this._sequenceName.Text))
            {
                MessageBox.Show("Enter a name for the new 'Sequence'", "Erorr", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            else
            {
                foreach (var item in this._host.AgentMandatoryScenario.Sequences)
                {
                    if (item.Name == this._sequenceName.Text)
                    {
                        MessageBox.Show("A 'Sequence' with the same already exists in the 'Scenario'", "Erorr", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                }
            }
            double lambda = 0;
            if (!double.TryParse(this._activationTime.Text,out lambda))
            {
                MessageBox.Show("Invalid input for 'Average Activation Time'!", "Erorr", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (lambda<=0)
            {
                MessageBox.Show("'Average Activation Time' should be larger than zero!", "Erorr", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var activities = new List<string>();
            foreach (var item in this._orderedActivities.Items)
            {
                activities.Add((item as Activity).Name);
            }
            if (activities.Count==0)
            {
                MessageBox.Show("At leat one activity is needed for each Sequence", "Sequence Is Empty", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            Sequence seq = new Sequence(activities, this._sequenceName.Text, lambda);
            if (this._includeVisualAwarenessField.IsChecked.Value)
            {
                if (this._host.sequenceVisibilityEventHost.VisualEvent == null)
                {
                    var results = MessageBox.Show("Visual Awareness Field is not sssigned!", "invalid input", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    seq.AssignVisualEvent(this._host.sequenceVisibilityEventHost.VisualEvent);
                }
            }

            this._host.AgentMandatoryScenario.Sequences.Add(seq);
            this.Close();
        }
        void _edit_Click(object sender, RoutedEventArgs e)
        {
            double lambda = 0;
            if (!double.TryParse(this._activationTime.Text, out lambda))
            {
                MessageBox.Show("Invalid input for 'Average Activation Time'!", "Erorr", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (lambda <= 0)
            {
                MessageBox.Show("'Average Activation Time' should be larger than zero!", "Erorr", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var activities = new List<string>();
            foreach (var item in this._orderedActivities.Items)
            {
                activities.Add((item as Activity).Name);
            }
            if (activities.Count == 0)
            {
                MessageBox.Show("At leat one activity is needed for each Sequence", "Sequence Is Empty", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            this._sequence.ActivityNames = activities;
            this._sequence.ActivationLambdaFactor = lambda;
            if (this._includeVisualAwarenessField.IsChecked.Value)
            {
                if (this._host.sequenceVisibilityEventHost.VisualEvent == null)
                {
                    var results = MessageBox.Show("Visual Awareness Field is not sssigned!", "invalid input", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    this._sequence.AssignVisualEvent(this._host.sequenceVisibilityEventHost.VisualEvent);
                }
            }
            this.Close();
        }
        void _remove_Click(object sender, RoutedEventArgs e)
        {
            if (this._orderedActivities.SelectedIndex != -1)
            {
                Activity name = this._orderedActivities.SelectedItem as Activity;
                this._orderedActivities.Items.RemoveAt(this._orderedActivities.SelectedIndex);
                //this._availablePoentialFields.Items.Add(name);
            }
        }

        void _add_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this._availablePoentialFields.SelectedIndex != -1)
                {
                    var name = this._availablePoentialFields.SelectedItem;
                    //this._availablePoentialFields.Items.RemoveAt(this._availablePoentialFields.SelectedIndex);
                    this._orderedActivities.Items.Add(name);
                }
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Report());
            }
        }

        void _visibilityEvent_Click(object sender, RoutedEventArgs e)
        {
            this._host.sequenceVisibilityEventHost.Clear();
            this.Owner.Hide();
            this.Hide();
            this._host.sequenceVisibilityEventHost.SetVisualEvents(visualEventWindowClosed);
        }
        void visualEventWindowClosed(object sender, EventArgs e)
        {
            this.ShowDialog();
            this.Owner.ShowDialog();
        }
        protected override void OnClosed(EventArgs e)
        {
            this._host.sequenceVisibilityEventHost.Clear();
            base.OnClosed(e);
        }
    }
}

