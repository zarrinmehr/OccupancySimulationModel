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
using SpatialAnalysis.Geometry;
using SpatialAnalysis.CellularEnvironment;
using System.Windows.Threading;
using SpatialAnalysis.Miscellaneous;

namespace SpatialAnalysis.Agents.Visualization.AgentTrailVisualization
{

    /// <summary>
    /// Interaction logic for SetTrail.xaml
    /// </summary>
    public partial class SetTrail : Window
    {
        #region TrailParametersUpdated event definition
        // Create a custom routed event by first registering a RoutedEventID
        // This event uses the bubbling routing strategy
        public static readonly RoutedEvent TrailParametersUpdatedEvent = EventManager.RegisterRoutedEvent(
            "TrailParametersUpdated", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SetTrail));
        /// <summary>
        /// Called when trail parameters are updated
        /// </summary>
        public event RoutedEventHandler TrailParametersUpdated
        {
            add { AddHandler(TrailParametersUpdatedEvent, value); }
            remove { RemoveHandler(TrailParametersUpdatedEvent, value); }
        }

        // This method raises the TrailParametersUpdated event
        void raiseTrailParametersUpdatedEvent()
        {
            RoutedEventArgs newEventArgs = new RoutedEventArgs(SetTrail.TrailParametersUpdatedEvent);
            RaiseEvent(newEventArgs);
        } 
        #endregion

        #region PointPerUniteOfLength Definition
        /// <summary>
        /// The point per unite of length property
        /// </summary>
        public static DependencyProperty PointPerUniteOfLengthProperty =
            DependencyProperty.Register("PointPerUniteOfLength", typeof(int), typeof(SetTrail),
            new FrameworkPropertyMetadata(5, SetTrail.PointPerUniteOfLengthPropertyChanged, SetTrail.PropertyCoerce));
        /// <summary>
        /// Gets or sets the number of points per unite of length.
        /// </summary>
        /// <value>The length of the point per unite of.</value>
        public int PointPerUniteOfLength
        {
            get { return (int)GetValue(PointPerUniteOfLengthProperty); }
            set { SetValue(PointPerUniteOfLengthProperty, value); }
        }
        private static void PointPerUniteOfLengthPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            SetTrail createTrail = (SetTrail)obj;
            if ((int)args.NewValue != (int)args.OldValue)
            {
                createTrail.PointPerUniteOfLength = (int)args.NewValue;
                createTrail.raiseTrailParametersUpdatedEvent();
            }
        }
        private static object PropertyCoerce(DependencyObject obj, object value)
        {
            return value;
        }
        #endregion

        #region TrailCurvature Definition
        public static DependencyProperty TrailCurvatureProperty =
            DependencyProperty.Register("TrailCurvature", typeof(double), typeof(SetTrail),
            new FrameworkPropertyMetadata(1.0d, SetTrail.TrailFreedomPropertyChanged, SetTrail.PropertyCoerce));
        /// <summary>
        /// Gets or sets the trail freedom factor.
        /// </summary>
        /// <value>The trail freedom.</value>
        public double TrailCurvature
        {
            get { return (double)GetValue(TrailCurvatureProperty); }
            set { SetValue(TrailCurvatureProperty, value); }
        }
        private static void TrailFreedomPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            SetTrail createTrail = (SetTrail)obj;
            if ((double)args.NewValue != (double)args.OldValue)
            {
                createTrail.TrailCurvature = (double)args.NewValue;
                createTrail.raiseTrailParametersUpdatedEvent();
            }
        }
        #endregion

        #region AgentsPerUniteOfLength Definition
        /// <summary>
        /// The number of agents per unite of length property
        /// </summary>
        public static DependencyProperty AgentsPerUniteOfLengthProperty =
            DependencyProperty.Register("AgentsPerUniteOfLength", typeof(int), typeof(SetTrail),
            new FrameworkPropertyMetadata(1, SetTrail.AgentsPerUniteOfLengthPropertyChanged, SetTrail.PropertyCoerce));
        /// <summary>
        /// Gets or sets the number of subdivisions (i.e. agent locations) per unite of.
        /// </summary>
        /// <value>The length of the agents per unite of.</value>
        public int AgentsPerUniteOfLength
        {
            get { return (int)GetValue(AgentsPerUniteOfLengthProperty); }
            set { SetValue(AgentsPerUniteOfLengthProperty, value); }
        }
        private static void AgentsPerUniteOfLengthPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            SetTrail createTrail = (SetTrail)obj;
            if ((int)args.NewValue != (int)args.OldValue)
            {
                createTrail.AgentsPerUniteOfLength = (int)args.NewValue;
                createTrail.raiseTrailParametersUpdatedEvent();
            }
        } 
        #endregion




        private OSMDocument _host;
        /// <summary>
        /// Initializes a new instance of the <see cref="SetTrail"/> class.
        /// </summary>
        /// <param name="host">The document to which this class belongs.</param>
        public SetTrail(OSMDocument host)
        {
            InitializeComponent();
            this._host = host;
            if (this._host.trailVisualization.AgentWalkingTrail != null)
            {
                this._smoothness.Value = this._host.trailVisualization.AgentWalkingTrail.Curvature;
                this._pointPerLengthUnite.SelectedValue = this._host.trailVisualization.AgentWalkingTrail.NumberOfPointsPerUniteOfLength;
                this._subdivision.SelectedValue = this._host.trailVisualization.AgentWalkingTrail.NumberOfPointsPerUniteOfLength;
            }
            this.Loaded += new RoutedEventHandler(CreateTrail_Loaded);
            this._closeBtm.Click += _closeBtm_Click;
            this._pointPerLengthUnite.SelectionChanged += _pointPerLengthUnite_SelectionChanged;
            this._smoothness.ValueChanged += _smoothness_ValueChanged;
            this._subdivision.SelectionChanged += _subdivision_SelectionChanged;
        }

        void _subdivision_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.AgentsPerUniteOfLength = (int)this._subdivision.SelectedItem;
        }



        void _smoothness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (e.NewValue != e.OldValue)
            {
                this.TrailCurvature = e.NewValue;
            }
        }

        void _pointPerLengthUnite_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.PointPerUniteOfLength = (int)this._pointPerLengthUnite.SelectedItem;
        }


        void _closeBtm_Click(object sender, RoutedEventArgs e)
        {
            this._closeBtm.Click -= _closeBtm_Click;
            BindingOperations.ClearBinding(this._export, GroupBox.IsEnabledProperty);
            BindingOperations.ClearBinding(this._edit, GroupBox.IsEnabledProperty);
            this._host = null;
            this.Close();
        }

        #region Hiding the close button
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        #endregion
        void CreateTrail_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
            Binding bind = new Binding("WalkingTrail");
            bind.Source = this._host.trailVisualization;
            bind.Mode = BindingMode.OneWay;
            bind.Converter = new ValueToBoolConverter();
            this._export.SetBinding(GroupBox.IsEnabledProperty, bind);
        }

    }
}

