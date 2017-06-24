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
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SpatialAnalysis.Agents.MandatoryScenario.Visualization
{
    /// <summary>
    /// Interaction logic for SelectAnActivity.xaml
    /// </summary>
    public partial class SelectAnActivity : Window
    {
        #region Host Definition
        private static DependencyProperty _hostProperty =
            DependencyProperty.Register("_host", typeof(OSMDocument), typeof(SelectAnActivity),
            new FrameworkPropertyMetadata(null, SelectAnActivity.hostPropertyChanged, SelectAnActivity.PropertyCoerce));
        private OSMDocument _host
        {
            get { return (OSMDocument)GetValue(_hostProperty); }
            set { SetValue(_hostProperty, value); }
        }
        private static void hostPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            SelectAnActivity instance = (SelectAnActivity)obj;
            instance._host = (OSMDocument)args.NewValue;
        }
        #endregion
        public string ActivityName { get; set; }
        private static object PropertyCoerce(DependencyObject obj, object value)
        {
            return value;
        }
        public SelectAnActivity(OSMDocument host)
        {
            InitializeComponent();
            this._host = host;
            this._activities.ItemsSource = this._host.AllActivities.Values;
            this._activities.DisplayMemberPath = "Name";
            this._activities.SelectionChanged += _activities_SelectionChanged;
            this._okay.Click += _okay_Click;
        }

        void _okay_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        void _activities_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this._activities.SelectedIndex != -1)
            {
                this.ActivityName = ((Activity)this._activities.SelectedItem).Name;
            }
            else
            {
                this.ActivityName = null;
            }
        }
        
        
    }
}

