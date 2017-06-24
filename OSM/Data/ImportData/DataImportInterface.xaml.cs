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
using System.Runtime.InteropServices;
using System.Windows.Interop;
using SpatialAnalysis.CellularEnvironment;

namespace SpatialAnalysis.Data.ImportData
{
    /// <summary>
    /// Interaction logic for DataImportInterface.xaml
    /// </summary>
    public partial class DataImportInterface : Window
    {

        /// <summary>
        /// Gets or sets the scale.
        /// </summary>
        /// <value>The scale.</value>
        public double Scale { get; set; }
        /// <summary>
        /// Gets or sets the index of the x.
        /// </summary>
        /// <value>The index of the x.</value>
        public int X_Index { get; set; }
        /// <summary>
        /// Gets or sets the index of the y.
        /// </summary>
        /// <value>The index of the y.</value>
        public int Y_Index { get; set; }
        /// <summary>
        /// Gets or sets the selected indexes.
        /// </summary>
        /// <value>The selected indexes.</value>
        public HashSet<int> SelectedIndices { get; set; }
        private Dictionary<string, int> FieldsToID { get; set; }
        private Dictionary<int, string> IDToFields { get; set; }
        /// <summary>
        /// The names of the data fields 
        /// </summary>
        public string[] Names;
        private string[] importedData { get; set; }

        private CellularFloor cellularFloor { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataImportInterface"/> class.
        /// </summary>
        /// <param name="inputData">The input data.</param>
        /// <param name="cellularFloor_">The cellular floor.</param>
        public DataImportInterface(string[] inputData, CellularFloor cellularFloor_)
        {
            InitializeComponent();
            this.cellularFloor = cellularFloor_;
            this.importedData = inputData;
            this.Remove.Content = "<";
            this.RemoveAll.Content = "<< All";
            this.FieldsToID = new Dictionary<string, int>();
            this.IDToFields = new Dictionary<int, string>();
            this.Names = inputData[0].Split(',');
            for (int i = 0; i < this.Names.Length; i++)
            {
                this.FieldsToID.Add(this.Names[i], i);
                this.IDToFields.Add(i, this.Names[i]);
                this.fields_X.Items.Add(this.Names[i]);
                this.fields_Y.Items.Add(this.Names[i]);
            }
            this.SelectedIndices = new HashSet<int>();
            this.X_Index = -1;
            this.Y_Index = -1;
        }

        #region Hiding the close button
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        //Hiding the close button
        private void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);

        }
        #endregion
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.X_Index = -1;
            this.Y_Index = -1;
            this.Close();
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            if (this.Selected.Items.Count == 0)
            {
                this.X_Index = -1;
                this.Y_Index = -1;
                this.Close();
                return;
            }
            else
            {
                double k;
                if (double.TryParse(this.ScaleBox.Text, out k))
                {
                    this.Scale = k;
                }
                else
                {
                    MessageBox.Show("Enter appropriate input for scale");
                    return;
                }
            }
            foreach (object item in this.Selected.Items)
            {
                this.SelectedIndices.Add(this.FieldsToID[(string)item]);
            }

            this.Close();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            int i = this.Imported.SelectedIndex;
            if (i != -1)
            {
                string s = (string)this.Imported.SelectedItem;
                this.Selected.Items.Add(s);
                this.Imported.Items.RemoveAt(i);
            }
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            int i = this.Selected.SelectedIndex;
            if (i != -1)
            {
                string s = (string)this.Selected.SelectedItem;
                this.Imported.Items.Add(s);
                this.Selected.Items.RemoveAt(i);
            }
        }

        private void fields_X_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string name = (string)this.fields_X.SelectedItem;
            if (this.fields_X.SelectedIndex == -1)
            {
                this.X_Index = -1;
                this.AnalyzeX.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                this.X_Index = this.FieldsToID[name];
                this.AnalyzeX.Visibility = System.Windows.Visibility.Visible;
            }
            if (this.Y_Index == -1 || this.X_Index == -1)
            {
                this.Imported.Items.Clear();
                this.Selected.Items.Clear();
                this.Selected.IsEnabled = false;
                this.Imported.IsEnabled = false;
                return;
            }
            if (this.Y_Index == this.X_Index)
            {
                MessageBox.Show("The fields for X and Y cannot be the same!\nTo Continue make Separate selections...");
                this.Imported.Items.Clear();
                this.Selected.Items.Clear();
                this.Selected.IsEnabled = false;
                this.Imported.IsEnabled = false;
                return;
            }
            if (this.Y_Index != -1 && this.X_Index != -1)
            {
                this.Selected.Items.Clear();
                this.Imported.Items.Clear();
                for (int i = 0; i < this.Names.Length; i++)
                {
                    if (i != this.X_Index && i != this.Y_Index)
                    {
                        this.Imported.Items.Add(this.Names[i]);
                    }
                }
                this.Imported.IsEnabled = true;
                this.Selected.IsEnabled = true;
            }
            
            

        }

        private void AnalyzeX_Click(object sender, RoutedEventArgs e)
        {
            var sortedInput = ParseCSV.GetRange(this.importedData, this.X_Index);
            double average_space = (sortedInput[sortedInput.Length - 1] - sortedInput[0]) / (sortedInput.Length - 1);
            double spacingVariance = 0;
            for (int i = 0; i < sortedInput.Length - 1; i++)
            {
                double space = sortedInput[i + 1] - sortedInput[i];
                space -= average_space;
                spacingVariance += space * space;
            }
            spacingVariance /= (sortedInput.Length - 1);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format("Field Name:\t\t{0}", this.IDToFields[this.X_Index]));
            sb.AppendLine(string.Format("Field Index:\t\t{0}", this.X_Index.ToString()));
            sb.AppendLine(string.Format("Number of X Spaces:\t{0}", sortedInput.Length.ToString()));
            sb.AppendLine(string.Format("Average of X Space:\t{0}", average_space.ToString()));
            sb.AppendLine(string.Format("Variance of X Space:\t{0}", Math.Sqrt(spacingVariance).ToString()));
            sb.AppendLine("Input Range: ");
            sb.AppendLine(string.Format("\tMin:\t{0}", sortedInput[0].ToString()));
            sb.AppendLine(string.Format("\tMax:\t{0}", sortedInput[sortedInput.Length - 1].ToString()));
            sb.AppendLine("Output Range: ");
            sb.AppendLine(string.Format("\tMin:\t{0}", this.cellularFloor.Origin.U.ToString()));
            sb.AppendLine(string.Format("\tMax:\t{0}", this.cellularFloor.TopRight.U.ToString()));
            MessageBox.Show(sb.ToString(), "Fields Data Description");
            sb.Clear();
            sb = null;
            sortedInput = null;
        }

        private void fields_Y_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string name = (string)this.fields_Y.SelectedItem;
            if (this.fields_Y.SelectedIndex == -1)
            {
                this.Y_Index = -1;
                this.AnalyzeY.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                this.Y_Index = this.FieldsToID[name];
                this.AnalyzeY.Visibility = System.Windows.Visibility.Visible;
            }
            if (this.Y_Index == -1 || this.X_Index == -1)
            {
                this.Imported.Items.Clear();
                this.Selected.Items.Clear();
                this.Selected.IsEnabled = false;
                this.Imported.IsEnabled = false;
                return;
            }
            if (this.Y_Index == this.X_Index)
            {
                MessageBox.Show("The fields for X and Y cannot be the same!\nTo Continue make Separate selections...");
                this.Imported.Items.Clear();
                this.Selected.Items.Clear();
                this.Selected.IsEnabled = false;
                this.Imported.IsEnabled = false;
                return;
            }
            if (this.Y_Index != -1 && this.X_Index != -1)
            {
                this.Selected.Items.Clear();
                this.Imported.Items.Clear();
                for (int i = 0; i < this.Names.Length; i++)
                {
                    if (i != this.X_Index && i != this.Y_Index)
                    {
                        this.Imported.Items.Add(this.Names[i]);
                    }
                }
                this.Selected.IsEnabled = true;
                this.Imported.IsEnabled = true;
            }

        }


        private void AnalyzeY_Click(object sender, RoutedEventArgs e)
        {
            var sortedInput = ParseCSV.GetRange(this.importedData, this.Y_Index);
            double average_space = (sortedInput[sortedInput.Length - 1] - sortedInput[0]) / (sortedInput.Length - 1);
            double spacingVariance = 0;
            for (int i = 0; i < sortedInput.Length - 1; i++)
            {
                double space = sortedInput[i + 1] - sortedInput[i];
                space -= average_space;
                spacingVariance += space * space;
            }
            spacingVariance /= (sortedInput.Length - 1);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format("Field Name:\t\t{0}", this.IDToFields[this.Y_Index]));
            sb.AppendLine(string.Format("Field Index:\t\t{0}", this.Y_Index.ToString()));
            sb.AppendLine(string.Format("Number of Y Spaces:\t{0}", sortedInput.Length.ToString()));
            sb.AppendLine(string.Format("Average of Y Space:\t{0}", average_space.ToString()));
            sb.AppendLine(string.Format("Variance of Y Space:\t{0}", Math.Sqrt(spacingVariance).ToString()));
            sb.AppendLine("Range: ");
            sb.AppendLine(string.Format("\tMin:\t{0}", sortedInput[0].ToString()));
            sb.AppendLine(string.Format("\tMax:\t{0}", sortedInput[sortedInput.Length - 1].ToString()));
            sb.AppendLine("Output Range: ");
            sb.AppendLine(string.Format("\tMin:\t{0}", this.cellularFloor.Origin.V.ToString()));
            sb.AppendLine(string.Format("\tMax:\t{0}", this.cellularFloor.TopRight.V.ToString()));
            MessageBox.Show(sb.ToString(), "Fields Data Description");
            sb.Clear();
            sb = null;
            sortedInput = null;
        }

        private void RemoveAll_Click(object sender, RoutedEventArgs e)
        {
            if (this.Selected.IsEnabled)
            {
                foreach (var item in this.Selected.Items)
                {
                    string s = (string)item;
                    this.Imported.Items.Add(s);
                }
                this.Selected.Items.Clear();
            }
        }

        private void AddAll_Click(object sender, RoutedEventArgs e)
        {
            if (this.Selected.IsEnabled)
            {
                foreach (var item in this.Imported.Items)
                {
                    string s = (string)item;
                    this.Selected.Items.Add(s);
                }
                this.Imported.Items.Clear();
            }
        }

        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("https://en.wikipedia.org/wiki/Bilinear_interpolation");
        }

        private void TextBlock_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Hand;
        }

        private void TextBlock_MouseLeave(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Arrow;
        }



    }
}

