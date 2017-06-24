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

namespace SpatialAnalysis.Visualization
{
    /// <summary>
    /// Interaction logic for GetNumber.xaml
    /// </summary>
    public partial class GetNumber : Window
    {
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


        /// <summary>
        /// Gets the number value which was entered.
        /// </summary>
        /// <value>The number value.</value>
        public double NumberValue
        {
            get { return double.Parse(this.Number.Text); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GetNumber"/> class.
        /// This class is used to get numerical input form user.
        /// </summary>
        /// <param name="title">The title of the window.</param>
        /// <param name="description">The description of the requested number.</param>
        /// <param name="defaultValue">The default value of the number.</param>
        public GetNumber(string title, string description, double defaultValue)
        {
            InitializeComponent();
            this.title_.Text = title;
            if (string.IsNullOrEmpty(description) || string.IsNullOrWhiteSpace(description) )
            {
                this.Description.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                this.Description.Text = description;
            }
            this.Number.Text = defaultValue.ToString();
            this.KeyDown += new KeyEventHandler(GetNumber_KeyDown);
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            this.MouseWheel += GetNumber_MouseWheel;
        }
        
        void GetNumber_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double val = 0;
            if (double.TryParse(this.Number.Text, out val))
            {
                if (val == 0)
                {
                    val = .00010 * Math.Sign(e.Delta);
                    this.Number.Text = val.ToString();
                    return;
                }
                double newval = val + .1 * Math.Abs(val) * Math.Sign(e.Delta);
                if (Math.Abs(newval) < .00010)
                {
                    this.Number.Text = "0.00000";
                    return;
                }
                string s = newval.ToString();
                if (s.Contains('.'))
                {
                    string[] components = s.Split('.');
                    if (components[1].Length>5)
                    {
                        components[1] = components[1].Substring(0, 5);
                    }
                    this.Number.Text = components[0] + "." + components[1];
                }
                else
                {
                    this.Number.Text = s;
                }
            }
        }

        void GetNumber_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.getNumber();
            }
        }

        private void getNumber()
        {
            double value;
            if (double.TryParse(this.Number.Text, out value))
            {
                if (value <= 0f)
                {
                    MessageBox.Show("Please enter a valied number!\n (Larger than zero)");
                    return;
                }
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("Please enter a valied number!\n (Larger than zero)");
            }
        }
        private void Set_Click(object sender, RoutedEventArgs e)
        {
            this.getNumber();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.MouseWheel -= GetNumber_MouseWheel;
            this.KeyDown -= GetNumber_KeyDown;
            this.Set.Click -= Set_Click;
        }


    }
}

