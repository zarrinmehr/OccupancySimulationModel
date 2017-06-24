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
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Jace;
using SpatialAnalysis.Miscellaneous;

namespace SpatialAnalysis.Data.Visualization
{
    /// <summary>
    /// Interaction logic for AddDataField.xaml
    /// </summary>
    public partial class AddDataField : Window
    {
        #region Hiding the close button
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        void AddData_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }
        #endregion
        /// <summary>
        /// Initializes a new instance of the <see cref="AddDataField"/> class.
        /// </summary>
        public AddDataField()
        {
            InitializeComponent();
            this.Loaded += AddData_Loaded;
        }
        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/pieterderycke/Jace/wiki");
        }

        private void TextBlock_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Hand;
        }

        private void TextBlock_MouseLeave(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Arrow;
        }

        /// <summary>
        /// Gets or sets the interpolation function.
        /// </summary>
        /// <value>The interpolation function.</value>
        public Func<double, double> InterpolationFunction { get; set; }
        /// <summary>
        /// Loads the interpolation function.
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool LoadFunction()
        {
            try
            {
                CalculationEngine engine = new CalculationEngine();
                this.InterpolationFunction = (Func<double, double>)engine.Formula(this.main.Text)
                .Parameter("X", Jace.DataType.FloatingPoint)
                .Result(Jace.DataType.FloatingPoint)
                .Build();
                for (int i = 0; i < 100; i++)
                {
                    this.InterpolationFunction(((double)i) / 3);
                }
            }
            catch (Exception error)
            {
                MessageBox.Show("Wrong formula!\n" + error.Report(), "FORMULA PARSING Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }

    }
    
 
}

