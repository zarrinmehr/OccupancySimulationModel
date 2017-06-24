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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace SpatialAnalysis.Visualization
{
    /// <summary>
    /// Interaction logic for BrushPicker.xaml
    /// </summary>
    public partial class BrushPicker : Window
    {
        #region Hiding the close button
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        //Hiding the close button
        private void BrushPicker_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }
        #endregion
        public Brush _Brush { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="BrushPicker"/> class.
        /// The class allows for the selection of a solid color brush and setting its opacity.
        /// </summary>
        /// <param name="inputBrush">The input brush.</param>
        public BrushPicker(Brush inputBrush)
        {
            InitializeComponent();
            PropertyInfo[] properties = typeof(Brushes).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                
                if (property.PropertyType == typeof(SolidColorBrush))
                {
                    var propVal = (SolidColorBrush)(property.GetValue(null, null));
                    TextBlock item = new TextBlock() {
                        FontStyle = FontStyles.Italic,
                        Text = property.Name,
                        Background = propVal
                    };
                    this._brushes.Items.Add(item);
                    if (inputBrush != null)
                    {
                        if (propVal.Color == ((SolidColorBrush)inputBrush).Color)
	                    {
		                    this._brushes.SelectedItem = item;
	                    }
                    }
                }
            }
            this.Loaded += new RoutedEventHandler(BrushPicker_Loaded);
            this._brushes.DropDownClosed += new EventHandler(_brushes_DropDownClosed);
            this._opacity.ValueChanged += new RoutedPropertyChangedEventHandler<double>(_opacity_ValueChanged);
            if (inputBrush != null)
            {
                SolidColorBrush brsh = (SolidColorBrush)inputBrush;
                if (brsh != null)
                {
                    this._Brush = brsh.Clone();
                    this.Rect_.Fill = this._Brush;
                    this._opacity.Value = (1 - this._Brush.Opacity) * this._opacity.Maximum;
                    this.Description.Text = string.Format("Opacity: {0}", (1 - this._opacity.Value / this._opacity.Maximum).ToString());
                }
            }
            this.KeyDown += new KeyEventHandler(BrushPicker_KeyDown);
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
        }

        void BrushPicker_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                this.Close();
            }
        }

        void _opacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            try
            {
                this.Description.Text = string.Format("Opacity: {0}", (1 - this._opacity.Value / this._opacity.Maximum).ToString());
                if (this._Brush != null)
                {
                    this._Brush.Opacity = 1 - this._opacity.Value / this._opacity.Maximum;
                    this.Rect_.Fill = this._Brush;
                }
            }
            catch (Exception error)
            {
                MessageBox.Show("from _opacity_ValueChanged\n" + error.Message);
            }
        }

        void _brushes_DropDownClosed(object sender, EventArgs e)
        {
            try
            {
                this._Brush = ((TextBlock)this._brushes.SelectedItem).Background.Clone();
                if (this._Brush != null)
                {
                    this._Brush.Opacity = 1 - this._opacity.Value / this._opacity.Maximum;
                    this.Rect_.Fill = this._Brush;
                }
            }
            catch (Exception error)
            {
                MessageBox.Show("from _brushes_DropDownClosed\n" + error.Message);
            }
        }

        private void Set_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

