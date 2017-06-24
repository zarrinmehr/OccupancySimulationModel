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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SpatialAnalysis.Data.Statistics
{
    /// <summary>
    /// Interaction logic for DataStatVisualHost.xaml
    /// </summary>
    public partial class DataStatVisualHost : UserControl
    {
        #region XAxisName definition
        /// <summary>
        /// The x axis name property
        /// </summary>
        public static DependencyProperty XAxisNameProperty =
            DependencyProperty.Register("X", typeof(string), typeof(DataStatVisualHost),
            new FrameworkPropertyMetadata(null, DataStatVisualHost.XAxisNamePropertyChanged,
            DataStatVisualHost.PropertyCoerce));
        /// <summary>
        /// Gets or sets the name of the x axis.
        /// </summary>
        /// <value>The name of the x axis.</value>
        public string XAxisName
        {
            get { return (string)GetValue(XAxisNameProperty); }
            set { SetValue(XAxisNameProperty, value); }
        }
        private static void XAxisNamePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            DataStatVisualHost graph = (DataStatVisualHost)obj;
            graph.XAxisName = (string)args.NewValue;
            graph.Data1Name.Text = graph.XAxisName;
        }
        #endregion

        #region YAxisName definition
        /// <summary>
        /// The y axis name property
        /// </summary>
        public static DependencyProperty YAxisNameProperty =
            DependencyProperty.Register("Y", typeof(string), typeof(DataStatVisualHost),
            new FrameworkPropertyMetadata(null, DataStatVisualHost.YAxisNamePropertyChanged,
            DataStatVisualHost.PropertyCoerce));
        /// <summary>
        /// Gets or sets the name of the y axis.
        /// </summary>
        /// <value>The name of the y axis.</value>
        public string YAxisName
        {
            get { return (string)GetValue(YAxisNameProperty); }
            set { SetValue(YAxisNameProperty, value); }
        }
        private static void YAxisNamePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            DataStatVisualHost graph = (DataStatVisualHost)obj;
            graph.YAxisName = (string)args.NewValue;
            graph.Data2Name.Text = graph.YAxisName;
        }
        #endregion
        #region XMIN definition
        /// <summary>
        /// The xmin property
        /// </summary>
        public static DependencyProperty XMINProperty =
            DependencyProperty.Register("XMIN", typeof(string), typeof(DataStatVisualHost),
            new FrameworkPropertyMetadata(null, DataStatVisualHost.XMINPropertyChanged,
            DataStatVisualHost.PropertyCoerce));
        /// <summary>
        /// Gets or sets the xmin.
        /// </summary>
        /// <value>Minimum of X values.</value>
        public string XMIN
        {
            get { return (string)GetValue(XMINProperty); }
            set { SetValue(XMINProperty, value); }
        }
        private static void XMINPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            DataStatVisualHost graph = (DataStatVisualHost)obj;
            graph.XMIN = (string)args.NewValue;
            graph._xMin.Text = graph.XMIN;
        }
        #endregion
        #region XMAX definition
        /// <summary>
        /// The xmax property
        /// </summary>
        public static DependencyProperty XMAXProperty =
            DependencyProperty.Register("XMAX", typeof(string), typeof(DataStatVisualHost),
            new FrameworkPropertyMetadata(null, DataStatVisualHost.XMAXPropertyChanged,
            DataStatVisualHost.PropertyCoerce));
        /// <summary>
        /// Gets or sets the xmax.
        /// </summary>
        /// <value>The maximum of X values.</value>
        public string XMAX
        {
            get { return (string)GetValue(XMAXProperty); }
            set { SetValue(XMAXProperty, value); }
        }
        private static void XMAXPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            DataStatVisualHost graph = (DataStatVisualHost)obj;
            graph.XMAX = (string)args.NewValue;
            graph._xMax.Text = graph.XMAX;
        }
        #endregion
        #region YMIN definition
        /// <summary>
        /// The ymin property
        /// </summary>
        public static DependencyProperty YMINProperty =
            DependencyProperty.Register("YMIN", typeof(string), typeof(DataStatVisualHost),
            new FrameworkPropertyMetadata(null, DataStatVisualHost.YMINPropertyChanged,
            DataStatVisualHost.PropertyCoerce));
        /// <summary>
        /// Gets or sets the ymin.
        /// </summary>
        /// <value>The minimum of Y values.</value>
        public string YMIN
        {
            get { return (string)GetValue(YMINProperty); }
            set { SetValue(YMINProperty, value); }
        }
        private static void YMINPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            DataStatVisualHost graph = (DataStatVisualHost)obj;
            graph.YMIN = (string)args.NewValue;
            graph._yMin.Text = graph.YMIN;
        }
        #endregion
        #region YMAX definition
        /// <summary>
        /// The ymax property
        /// </summary>
        public static DependencyProperty YMAXProperty =
            DependencyProperty.Register("YMAX", typeof(string), typeof(DataStatVisualHost),
            new FrameworkPropertyMetadata(null, DataStatVisualHost.YMAXPropertyChanged,
            DataStatVisualHost.PropertyCoerce));
        /// <summary>
        /// Gets or sets the ymax.
        /// </summary>
        /// <value>The maximum of Y values.</value>
        public string YMAX
        {
            get { return (string)GetValue(YMAXProperty); }
            set { SetValue(YMAXProperty, value); }
        }
        private static void YMAXPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            DataStatVisualHost graph = (DataStatVisualHost)obj;
            graph.YMAX = (string)args.NewValue;
            graph._yMax.Text = graph.YMAX;
        }
        #endregion


        private static object PropertyCoerce(DependencyObject obj, object value)
        {
            return value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataStatVisualHost"/> class.
        /// </summary>
        public DataStatVisualHost()
        {
            InitializeComponent();
        }
    }
}

