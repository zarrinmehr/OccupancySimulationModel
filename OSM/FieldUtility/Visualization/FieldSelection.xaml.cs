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
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SpatialAnalysis.FieldUtility.Visualization
{
    /// <summary>
    /// Interaction logic for FieldSelection.xaml
    /// </summary>
    public partial class FieldSelection : Window
    {
        #region Hiding the close button
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        //Hiding the close button
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }
        #endregion
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="FieldSelection"/> should be rejected or not.
        /// </summary>
        /// <value><c>true</c> if retry; otherwise, <c>false</c>.</value>
        public bool Retry { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="FieldSelection"/> class.
        /// </summary>
        public FieldSelection()
        {
            InitializeComponent();
            this.KeyDown += new System.Windows.Input.KeyEventHandler(FieldSelection_KeyDown);
        }

        void FieldSelection_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                this.Retry = false;
                this.Close();
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                this.Retry = true;
                this.Close();
            }
        }

        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            this.Retry = false;
            this.Close();
        }

        private void retry__Click(object sender, RoutedEventArgs e)
        {
            this.Retry = true;
            this.Close();
        }


    }
}

