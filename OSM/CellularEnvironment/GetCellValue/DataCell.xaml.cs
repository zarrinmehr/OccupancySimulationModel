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
using SpatialAnalysis.FieldUtility;
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
using SpatialAnalysis.CellularEnvironment;
using SpatialAnalysis.Miscellaneous;

namespace SpatialAnalysis.CellularEnvironment.GetCellValue
{
    /// <summary>
    /// Interaction logic for DataCell.xaml
    /// </summary>
    public partial class DataCell : Window
    {
        #region Hiding the close button
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        void DataCell_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
            try
            {
                foreach (var item in this.dataNames.Items)
                {
                    DataValue dataValue = item as DataValue;
                    if (dataValue != null)
                    {
                        dataValue.Width = this.dataNames.RenderSize.Width;
                    }
                }
            }
            catch (Exception er)
            {
                MessageBox.Show(er.Report());
            }
        }
        #endregion

        TextBlock cellularDataNames { get; set; }
        TextBlock fieldNames { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="DataCell"/> class.
        /// </summary>
        /// <param name="cell">The cell.</param>
        /// <param name="cellularFloor">The cellular floor.</param>
        /// <param name="allFields">All fields.</param>
        public DataCell(Cell cell, CellularFloor cellularFloor, Dictionary<string,Activity> allFields)
        {
            InitializeComponent();
            Brush light = new SolidColorBrush(Colors.LightGray) { Opacity = .6 };
            this.CellInfo.Text = string.Format("X = {0}; Y = {1}", cell.U.ToString(), cell.V.ToString());
            this.Loaded += DataCell_Loaded;
            this.SizeChanged += DataCell_SizeChanged;
            this.cellularDataNames = new TextBlock()
            {
                Text = "Data".ToUpper(),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.Gray)
            };
            this.dataNames.Items.Add(this.cellularDataNames);
            int x = 0;
            foreach (SpatialAnalysis.Data.Function item in cellularFloor.AllSpatialDataFields.Values)
            {
                var data = item as SpatialAnalysis.Data.SpatialDataField;
                if (data != null)
                {
                    DataValue dataValue = new DataValue(data.Name, data.Data[cell]);
                    x++;
                    if (x % 2 == 1)
                    {
                        dataValue.Background = light;
                    }
                    this.dataNames.Items.Add(dataValue);
                }
            }
            if (allFields.Count != 0)
            {
                this.fieldNames = new TextBlock()
                {
                    Text = "Avtivity".ToUpper(),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Colors.Gray)
                };
                this.dataNames.Items.Add(fieldNames);
                x = 0;
                foreach (KeyValuePair<string, Activity> item in allFields)
                {
                    DataValue dataValue = new DataValue(item.Key, item.Value.Potentials[cell]);
                    x++;
                    if (x % 2 == 1)
                    {
                        dataValue.Background = light;
                    }
                    this.dataNames.Items.Add(dataValue);
                }
            }
        }

        void DataCell_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                foreach (var item in this.dataNames.Items)
                {
                    DataValue dataValue = item as DataValue;
                    if (dataValue != null)
                    {
                        dataValue.Width = this.dataNames.RenderSize.Width;
                    }
                }
            }
            catch (Exception er)
            {
                MessageBox.Show(er.Report());
            }
        }

        private void Okay_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

