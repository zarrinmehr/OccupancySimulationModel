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
using SpatialAnalysis.FieldUtility;
using SpatialAnalysis.Miscellaneous;
using SpatialAnalysis.Interoperability;

namespace SpatialAnalysis.Agents.MandatoryScenario.Visualization
{
    /// <summary>
    /// Interaction logic for EditMandatoryScenario.xaml
    /// </summary>
    public partial class EditMandatoryScenario : Window
    {
        #region Host Definition
        private static DependencyProperty _hostProperty =
            DependencyProperty.Register("_host", typeof(OSMDocument), typeof(EditMandatoryScenario),
            new FrameworkPropertyMetadata(null, EditMandatoryScenario.hostPropertyChanged, EditMandatoryScenario.PropertyCoerce));
        private OSMDocument _host
        {
            get { return (OSMDocument)GetValue(_hostProperty); }
            set { SetValue(_hostProperty, value); }
        }
        private static void hostPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            EditMandatoryScenario instance = (EditMandatoryScenario)obj;
            instance._host = (OSMDocument)args.NewValue;
        }
        #endregion
        private static object PropertyCoerce(DependencyObject obj, object value)
        {
            return value;
        }
        public EditMandatoryScenario(OSMDocument host)
        {
            InitializeComponent();
            this._host = host;
            foreach (var item in this._host.AllActivities.Values)
            {
                if (this._host.AgentMandatoryScenario.MainStations.Contains(item.Name))
                {
                    this._selectedStations.Items.Add(item);
                }
                else
                {
                    this._mainStations.Items.Add(item);
                }
            }
            this._add.Click += new RoutedEventHandler(_add_Click);
            this._remove.Click += new RoutedEventHandler(_remove_Click);
            this._okay.Click += new RoutedEventHandler(_okay_Click);
            this._addSequence.Click += new RoutedEventHandler(_addSequence_Click);
            this._existingSequences.ItemsSource = this._host.AgentMandatoryScenario.Sequences;
            this._existingSequences.DisplayMemberPath = "Name";
            this._editSequence.Click += new RoutedEventHandler(_editSequence_Click);
            this._removeSequence.Click += _removeSequence_Click;
            this.Activated += EditMandatoryScenario_Activated;
            this._saveSequence.Click += _saveSequence_Click;
            this._loadSequence.Click += _loadSequence_Click;
        }

        void _loadSequence_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Title = "Load Sequences";
            dlg.DefaultExt = ".seq";
            dlg.Filter = "SEQ documents (.seq)|*.seq";
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
            bool unitAssigned = false;
            Length_Unit_Types unitType = Length_Unit_Types.FEET;
            string unitString = "UNIT:";
            List<string> lines = new List<string>();
            using (System.IO.StreamReader sr = new System.IO.StreamReader(fileAddress))
            {
                string line = string.Empty;
                while ((line = sr.ReadLine()) != null)
                {
                    if (!(string.IsNullOrEmpty(line) || string.IsNullOrWhiteSpace(line)))
                    {
                        line = line.Trim(' ');
                        if (line[0] != '#')
                        {
                            if (line.Length > unitString.Length && line.Substring(0,unitString.Length).ToUpper() == unitString.ToUpper())
                            {
                                string unitName = line.Substring(unitString.Length, line.Length - unitString.Length).ToUpper().Trim(' ');
                                switch (unitName)
                                {
                                    case "METERS":
                                        unitAssigned = true;
                                        unitType = Length_Unit_Types.METERS;
                                        break;
                                    case "DECIMETERS":
                                        unitAssigned = true;
                                        unitType = Length_Unit_Types.DECIMETERS;
                                        break;
                                    case "CENTIMETERS":
                                        unitAssigned = true;
                                        unitType = Length_Unit_Types.CENTIMETERS;
                                        break;
                                    case "MILLIMETERS":
                                        unitAssigned = true;
                                        unitType = Length_Unit_Types.MILLIMETERS;
                                        break;
                                    case "FEET":
                                        unitAssigned = true;
                                        unitType = Length_Unit_Types.FEET;
                                        break;
                                    case "INCHES":
                                        unitAssigned = true;
                                        unitType = Length_Unit_Types.METERS;
                                        break;
                                }
                            }
                            else
                            {
                                lines.Add(line);
                            }
                        }
                    }
                }
            }
            if (lines.Count == 0)
            {
                MessageBox.Show("The file includes no input",
                    "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            if (!unitAssigned)
            {
                MessageBox.Show("The did not include information for 'Unit of Length'",
                    "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            List<List<string>> inputs = new List<List<string>>();
            List<string> input = new List<string>();
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i] == "SEQUENCE")
                {
                    if (input.Count != 0)
                    {
                        List<string> list = new List<string>(input);
                        inputs.Add(list);
                        input.Clear();
                    }
                }
                else
                {
                    input.Add(lines[i]);
                }
            }
            if (input.Count != 0)
            {
                inputs.Add(input);
            }
            foreach (List<string> item in inputs)
            {
                try
                {
                    var sequence = Sequence.FromStringRepresentation(item, unitType, this._host.cellularFloor, 0.0000001d);
                    bool add = true;
                    foreach (var activityName in sequence.ActivityNames)
                    {
                        if (!this._host.AllActivities.ContainsKey(activityName))
                        {
                            MessageBox.Show("The sequence " + sequence.Name + " includes an activity which is not included in the document: " + activityName,
                                "Missing Input", MessageBoxButton.OK, MessageBoxImage.Error);
                            add = false;
                            break;
                        }
                    }
                    if (add)
                    {
                        if (this._host.AgentMandatoryScenario.Sequences.Contains(sequence))
                        {
                            MessageBox.Show("The sequence " + sequence.Name + " is already included in the document",
                                "Missing Input", MessageBoxButton.OK, MessageBoxImage.Error);
                            add = false;
                        }
                    }
                    if (add)
                    {
                        this._host.AgentMandatoryScenario.Sequences.Add(sequence);
                    }
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.Report());
                }
            }
            this._existingSequences.Items.Refresh();
        }

        void _saveSequence_Click(object sender, RoutedEventArgs e)
        {
            if (this._host.AgentMandatoryScenario.Sequences.Count == 0)
            {
                return;
            }
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Title = "Save Sequences";
            dlg.DefaultExt = ".seq";
            dlg.Filter = "SEQ documents (.seq)|*.seq";
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
                sw.WriteLine("# Comment lines in this file should start with '#' character");
                sw.WriteLine(string.Empty);
                string unitString = "UNIT: ";
                sw.WriteLine("# Project Unit type");
                sw.WriteLine(unitString + this._host.BIM_To_OSM.UnitType.ToString());
                sw.WriteLine(string.Empty);
                foreach (var item in this._host.AgentMandatoryScenario.Sequences)
                {
                    sw.WriteLine(item.GetStringRepresentation());
                    sw.WriteLine(string.Empty);
                }
                sw.Close();
            }
        }

        void _removeSequence_Click(object sender, RoutedEventArgs e)
        {
            if (this._existingSequences.SelectedIndex != -1)
            {
                Sequence seq = this._existingSequences.SelectedItem as Sequence;
                if (this._host.AgentMandatoryScenario.RemoveSequence(seq))
                {
                    seq = null;
                    this._existingSequences.Items.Refresh();
                }
            }
            else
            {
                MessageBox.Show("Select a sequence to continue...", "", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        void EditMandatoryScenario_Activated(object sender, EventArgs e)
        {
            try
            {
                this._existingSequences.Items.Refresh();
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Report());
            }
        }

        void _editSequence_Click(object sender, RoutedEventArgs e)
        {
            if (this._existingSequences.SelectedIndex!=-1)
            {
                Sequence seq = this._existingSequences.SelectedItem as Sequence;
                GenerateSequenceUI editor = new GenerateSequenceUI(this._host, seq);
                editor.Owner = this;
                editor.ShowDialog();
            }
            else
            {
                MessageBox.Show("Select a sequence to continue...", "", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        void _addSequence_Click(object sender, RoutedEventArgs e)
        {
            GenerateSequenceUI generateSequence = new GenerateSequenceUI(this._host);
            generateSequence.Owner = this;
            generateSequence.ShowDialog();

            
        }

        void _okay_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        void _remove_Click(object sender, RoutedEventArgs e)
        {
            if (this._selectedStations.SelectedIndex !=-1)
            {
                Activity item = this._selectedStations.SelectedItem as Activity;
                this._selectedStations.Items.Remove(item);
                this._mainStations.Items.Add(item);
                this._host.AgentMandatoryScenario.MainStations.Remove(item.Name);
                this._selectedStations.SelectedIndex = -1;
            }
        }

        void _add_Click(object sender, RoutedEventArgs e)
        {
            if (this._mainStations.SelectedIndex !=-1)
            {
                Activity item = this._mainStations.SelectedItem as Activity;
                this._mainStations.Items.Remove(item);
                this._mainStations.SelectedIndex = -1;
                this._selectedStations.Items.Add(item);
                this._host.AgentMandatoryScenario.MainStations.Add(item.Name);
            }
        }

        private void _visualizeSequence_Click(object sender, RoutedEventArgs e)
        {
            VisualizeSequence visualizeSequence = new VisualizeSequence(this._host);
            visualizeSequence.Owner = this;
            visualizeSequence.ShowDialog();
            visualizeSequence = null;
        }
    }
}

