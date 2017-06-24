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

namespace SpatialAnalysis.IsovistUtility.IsovistVisualization
{
    /// <summary>
    /// Interaction logic for BoundarySetting.xaml
    /// </summary>
    public partial class BoundarySetting : Window
    {
        #region Hiding the close button
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        //Hiding the close button
        private void BoundarySetting_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);

        }
        #endregion

        /// <summary>
        /// Gets or sets the proxemics radiuses.
        /// </summary>
        /// <value>The radiuses.</value>
        public double[] Radiuses { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="BoundarySetting"/> class.
        /// </summary>
        /// <param name="radiuses">The radiuses.</param>
        public BoundarySetting(double[] radiuses)
        {
            InitializeComponent();
            string s = radiuses[0].ToString();
            if (s.Length>6)
            {
                s = s.Substring(0, 6);
            }
            this.R1Text.Text = s;

            s = radiuses[1].ToString();
            if (s.Length > 6)
            {
                s = s.Substring(0, 6);
            }
            this.R2Text.Text = s;

            s = radiuses[2].ToString();
            if (s.Length > 6)
            {
                s = s.Substring(0, 6);
            }
            this.R3Text.Text = s;

            s = radiuses[3].ToString();
            if (s.Length > 6)
            {
                s = s.Substring(0, 6);
            }
            this.R4Text.Text = s;

            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            this.Loaded +=new RoutedEventHandler(BoundarySetting_Loaded);
            this.Done.Click += new RoutedEventHandler(Done_Click);
        }


        private void Done_Click(object sender, RoutedEventArgs e)
        {
            double r1, r2, r3, r4;
            if (!double.TryParse(this.R1Text.Text,out r1))
            {
                MessageBox.Show("Cannot parse the value for R1\n" + "Try again...");
                return;
            }
            if (!double.TryParse(this.R2Text.Text, out r2))
            {
                MessageBox.Show("Cannot parse the value for R2\n" + "Try again...");
                return;
            }
            if (!double.TryParse(this.R3Text.Text, out r3))
            {
                MessageBox.Show("Cannot parse the value for R3\n" + "Try again...");
                return;
            }
            if (!double.TryParse(this.R4Text.Text, out r4))
            {
                MessageBox.Show("Cannot parse the value for R4\n" + "Try again...");
                return;
            }
            if (r1>0 && r2>r1 && r3>r2 && r4>r3)
            {
                this.Radiuses = new double[4];
                this.Radiuses[0] = r1;
                this.Radiuses[1] = r2;
                this.Radiuses[2] = r3;
                this.Radiuses[3] = r4;
                this.Close();
            }
            else
            {
                MessageBox.Show("Radiuses should be greater than zero and sorted ascendingly\n" + "Try again...");
                return;
            }
            
        }
    }
}

