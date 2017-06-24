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

namespace SpatialAnalysis.Agents.MandatoryScenario.Visualization
{
    /// <summary>
    /// Interaction logic for VisualizeScenario.xaml
    /// </summary>
    public partial class VisualizeSequence : Window
    {
        OSMDocument _host { get; set; }
        #region Hiding the close button
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        //Hiding the close button
        private void VisualizeSequence_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }
        #endregion
        public VisualizeSequence(OSMDocument host)
        {
            InitializeComponent();
            this._host = host;
            this._existingSequences.ItemsSource = this._host.AgentMandatoryScenario.Sequences;
            this._existingSequences.DisplayMemberPath = "Name";
            this.Loaded +=VisualizeSequence_Loaded;
            this._close.Click += _close_Click;
        }

        void _close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void _visualize_Click(object sender, RoutedEventArgs e)
        {
            if (this._existingSequences.SelectedIndex != -1)
            {
                Sequence sequence = (Sequence)this._existingSequences.SelectedItem;
                if (sequence == null)
                {
                    MessageBox.Show("Null Sequence!");
                    return;
                }
                if (this._straightlines.IsChecked.Value)
                {
                    this._host.activityAreaVisualHost.DrawSequenceWithStraightLines(sequence, this._colorCode.IsChecked.Value);
                }
                else
                {
                    try
                    {
                        double step = 0.7d;
                        if (!double.TryParse(this._stepSize.Text, out step))
                        {
                            MessageBox.Show("StepSize should be valid number larger than 0.7!");
                            return;
                        }
                        if (step<.07)
                        {
                            MessageBox.Show("StepSize should be valid number larger than 0.7!");
                            return;
                        }
                        this._host.activityAreaVisualHost.DrawSequenceWithForceTrajectory(sequence, this._colorCode.IsChecked.Value, step);
                    }
                    catch (Exception error)
                    {
                        MessageBox.Show(error.Report());
                    }
                }
            }
            else
            {
                MessageBox.Show("Select a sequence from the list!");
            }
        }

        private void _clear_Click(object sender, RoutedEventArgs e)
        {
            this._host.activityAreaVisualHost.Clear();
        }

        private void _save_Click(object sender, RoutedEventArgs e)
        {
            this._host.activityAreaVisualHost.Save();
        }
    }
}

