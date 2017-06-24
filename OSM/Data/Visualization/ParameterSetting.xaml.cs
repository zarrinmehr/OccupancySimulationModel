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
using SpatialAnalysis.Miscellaneous;

namespace SpatialAnalysis.Data.Visualization
{
    /// <summary>
    /// Interaction logic for ParameterSetting.xaml
    /// </summary>
    public partial class ParameterSetting : Window
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
        #region ParameterName Definition
        /// <summary>
        /// The parameter name property
        /// </summary>
        public static DependencyProperty ParameterNameProperty =
            DependencyProperty.Register("ParameterName", typeof(string), typeof(ParameterSetting),
            new FrameworkPropertyMetadata(string.Empty, ParameterSetting.ParameterNamePropertyChanged, ParameterSetting.PropertyCoerce));
        /// <summary>
        /// Gets or sets the name of the parameter.
        /// </summary>
        /// <value>The name of the parameter.</value>
        public string ParameterName
        {
            get { return (string)GetValue(ParameterNameProperty); }
            set { SetValue(ParameterNameProperty, value); }
        }
        private static void ParameterNamePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            ParameterSetting parameterSetting = (ParameterSetting)obj;
            if ((string)args.NewValue != (string)args.OldValue)
            {
                parameterSetting.ParameterName = (string)args.NewValue;
            }
        }
        private static object PropertyCoerce(DependencyObject obj, object value)
        {
            return value;
        }
        #endregion
        OSMDocument _host;
        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterSetting"/> class.
        /// </summary>
        /// <param name="host">The document to which this window belongs</param>
        /// <param name="insertmode">If set to <c>true</c> inserts the selected parameter.</param>
        public ParameterSetting(OSMDocument host, bool insertmode)
        {
            InitializeComponent();
            this.Loaded += Window_Loaded;
            this._host = host;
            foreach (var item in this._host.Parameters.Keys)
            {
                this._parameterList.Items.Add(item);
            }
            this._close.Click += _close_Click;
            this._createAndAdd.Click += _createAndAdd_Click;
            this._parameterList.SelectionChanged += _parameterList_SelectionChanged;
            this._update.Click += _update_Click;
            this._paramMax.TextChanged += _updateEnabled;
            this._paramMin.TextChanged += _updateEnabled;
            this._paramValue.TextChanged += _updateEnabled;
            this._insert.Click += _insert_Click;
            this._delete.Click += new RoutedEventHandler(_delete_Click);
            if (insertmode)
            {
                this._close.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                this._insert.Visibility = System.Windows.Visibility.Collapsed;
            }
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterSetting"/> class.
        /// </summary>
        /// <param name="host">The host.</param>
        /// <param name="insertmode">If set to <c>true</c> inserts the selected parameter.</param>
        /// <param name="newParameterName">New name of the parameter.</param>
        public ParameterSetting(OSMDocument host, bool insertmode, string newParameterName)
        {
            InitializeComponent();
            this.Loaded += Window_Loaded;
            this._host = host;
            foreach (var item in this._host.Parameters.Keys)
            {
                this._parameterList.Items.Add(item);
            }
            this._close.Click += _close_Click;
            this._createAndAdd.Click += _createAndAdd_Click;
            this._parameterList.SelectionChanged += _parameterList_SelectionChanged;
            this._update.Click += _update_Click;
            this._paramMax.TextChanged += _updateEnabled;
            this._paramMin.TextChanged += _updateEnabled;
            this._paramValue.TextChanged += _updateEnabled;
            this._insert.Click += _insert_Click;
            this._delete.Click += new RoutedEventHandler(_delete_Click);
            this._name.Text = newParameterName;
            this._name.SelectAll();
            if (insertmode)
            {
                this._close.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                this._insert.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        void _delete_Click(object sender, RoutedEventArgs e)
        {
            if (this._parameterList.SelectedIndex != -1)
            {
                string name = (string)this._parameterList.SelectedValue;
                if (!this._host.Parameters.ContainsKey(name))
                {
                    MessageBox.Show("Parameter not found");
                    return;
                }
                if (this._host.RemoveParameter(name))
                {
                    this._paramName.Text = string.Empty;
                    this._paramMax.Text = string.Empty;
                    this._paramMin.Text = string.Empty;
                    this._paramValue.Text = string.Empty;
                    this._update.IsEnabled = false;
                    this._parameterList.SelectedIndex = -1;
                    this._parameterList.Items.Remove(name);
                }
            }
            else
            {
                MessageBox.Show("No parameter selected", "", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }

        void _insert_Click(object sender, RoutedEventArgs e)
        {
            if (this._parameterList.SelectedIndex == -1)
            {
                var result = MessageBox.Show("No Parameter Selected!\nDo you want to continue with no selection?",
                    "No Selection Made", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);
                if (result == MessageBoxResult.Yes)
                {
                    this.Close();
                }
                else
                {
                    return;
                }
            }
            else
            {
                string name = (string)this._parameterList.SelectedValue;
                if (!this._host.Parameters.ContainsKey(name))
                {
                    MessageBox.Show("Parameter not found");
                    return;
                }
                this.ParameterName = name;
                this.Close();
            }
        }

        void _updateEnabled(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(this._paramMax.Text) && !string.IsNullOrWhiteSpace(this._paramMax.Text) &&
                !string.IsNullOrEmpty(this._paramMin.Text) && !string.IsNullOrWhiteSpace(this._paramMin.Text) &&
                !string.IsNullOrEmpty(this._paramValue.Text) && !string.IsNullOrWhiteSpace(this._paramValue.Text))
            {
                if (this._parameterList.SelectedIndex != -1)
                {
                    string name = (string)this._parameterList.SelectedValue;
                    if (this._host.Parameters.ContainsKey(name))
                    {
                        var param = this._host.Parameters[name];
                        if (this._paramMax.Text != param.Maximum.ToString() ||
                            this._paramMin.Text != param.Minimum.ToString() ||
                            this._paramValue.Text != param.Value.ToString())
                        {
                            this._update.IsEnabled = true;
                        }
                    }
                }
            }
        }
        

        void _update_Click(object sender, RoutedEventArgs e)
        {
            double min, max, value;
            if (!double.TryParse(this._paramMin.Text, out min) ||
                !double.TryParse(this._paramMax.Text, out max) ||
                !double.TryParse(this._paramValue.Text, out value))
            {
                MessageBox.Show("Invalid input values for updating the selected parameter");
                return;
            }
            string name = (string)this._parameterList.SelectedValue;
            if (!this._host.Parameters.ContainsKey(name))
            {
                MessageBox.Show("Parameter not found");
                return;
            }
            var param = this._host.Parameters[name];
            try
            {
                param.Maximum = max;
                param.Minimum = min;
                param.Value = value;
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Report());
                return;
            }
            this._update.IsEnabled = false;
        }

        void _parameterList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this._parameterList.SelectedIndex == -1)
            {
                return;
            }
            this._update.IsEnabled = false;
            string name = (string)this._parameterList.SelectedValue;
            if (!this._host.Parameters.ContainsKey(name))
            {
                MessageBox.Show("Parameter not found");
                return;
            }
            var param = this._host.Parameters[name];
            this._paramName.Text = param.Name;
            this._paramMax.Text = param.Maximum.ToString();
            this._paramMin.Text = param.Minimum.ToString();
            this._paramValue.Text = param.Value.ToString();
        }

        void _createAndAdd_Click(object sender, RoutedEventArgs e)
        {
            double min, max, value;
            if (!double.TryParse(this._min.Text, out min) ||
                !double.TryParse(this._max.Text, out max) ||
                !double.TryParse(this._value.Text, out value))
            {
                MessageBox.Show("Invalid input values for the new parameter");
                return;
            }
            string name = this._name.Text;
            if (this._host.Parameters.ContainsKey(name))  
            {
                MessageBox.Show("A parameter with the same name exists");
                return;
            }

            Parameter param = null;
            try
            {
                param = new Parameter(name, value, min, max);
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Report());
            }
            if (param != null)
            {
                this._host.AddParameter(param);
                this._value.Text = string.Empty;
                this._min.Text = string.Empty;
                this._max.Text = string.Empty;
                this._value.Text = string.Empty;
                this._name.Text = string.Empty;
                this._parameterList.Items.Add(param.Name);
            }
        }

        void _close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

