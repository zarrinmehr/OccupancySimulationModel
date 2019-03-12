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
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SpatialAnalysis.Interoperability;
using SpatialAnalysis.FieldUtility;
using SpatialAnalysis.FieldUtility.Visualization;
using SpatialAnalysis.IsovistUtility;
using SpatialAnalysis.Visualization;
using SpatialAnalysis.Visualization3D;
using SpatialAnalysis.CellularEnvironment;
using SpatialAnalysis.Geometry;
using SpatialAnalysis.Data;
using SpatialAnalysis.Events;
using System.Threading.Tasks;
using SpatialAnalysis.Data.ImportData;
using System.ComponentModel;
using SpatialAnalysis.Data.Visualization;
using Jace;
using SpatialAnalysis.Agents.OptionalScenario;
using SpatialAnalysis.Agents.MandatoryScenario;
using SpatialAnalysis.Agents.MandatoryScenario.Visualization;
using SpatialAnalysis.IsovistUtility.IsovistVisualization;
using SpatialAnalysis.Miscellaneous;
using System.Reflection;

namespace SpatialAnalysis
{
    /// <summary>
    /// This class represents the main document of OSM.
    /// </summary>
    public partial class OSMDocument : Window, INotifyPropertyChanged
    {
        #region implimentation of INotifyPropertyChanged
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        private void _notifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        } 
        #endregion

        /// <summary>
        /// A utility class for the conversion of length unit type
        /// </summary>
        public UnitConversion UnitConvertor { get; set; }

        /// <summary>
        /// Gets or sets the field generator.
        /// </summary>
        /// <value>The field generator.</value>
        public SpatialDataCalculator FieldGenerator { get; set; }
        /// <summary>
        /// Gets or sets the view based Gaussian filter.
        /// </summary>
        /// <value>The view based gaussian filter.</value>
        public IsovistClippedGaussian ViewBasedGaussianFilter { get; set; }
        /// <summary>
        /// Gets the BIM to OSM interoperability framework
        /// </summary>
        /// <value>The environment.</value>
        public BIM_To_OSM_Base BIM_To_OSM { get { return this._BIM_To_OSM; } }
        private BIM_To_OSM_Base _BIM_To_OSM;//
        /// <summary>
        /// Gets the cellular floor.
        /// </summary>
        /// <value>The cellular floor.</value>
        public CellularFloor cellularFloor { get { return this._cellularFloor;} }
        private CellularFloor _cellularFloor;
        /// <summary>
        /// Gets or sets the graph start cell.
        /// </summary>
        /// <value>The graph start cell.</value>
        public Cell GraphStartCell { get; set; }

        /// <summary>
        /// Gets or sets the render transformation which centralize the view.
        /// </summary>
        /// <value>The render transformation.</value>
        public Transform RenderTransformation { get; set; }

        /// <summary>
        /// Gets or sets the inverse render transform of the document view.
        /// </summary>
        /// <value>The inverse render transform.</value>
        public Transform InverseRenderTransform { get; set; }
        /// <summary>
        /// Gets or sets the isovist information.
        /// </summary>
        /// <value>The isovist information.</value>
        public IsovistInformation IsovistInformation { get; set; }
        /// <summary>
        /// Gets the OSM to BIM interoperability framework.
        /// </summary>
        /// <value>The osm to bim.</value>
        public I_OSM_To_BIM OSM_to_BIM { get { return _OSM_to_BIM; } }
        private I_OSM_To_BIM _OSM_to_BIM;
        /// <summary>
        /// The absolute tolerance of the document.
        /// </summary>
        public const double AbsoluteTolerance = 0.0000001d;
        /// <summary>
        /// The penalty collision reaction used for push the objects away from rigid bodies to dispute the effect of numerical precision. 
        /// </summary>
        public const double PenaltyCollisionReaction = 0.0001d;

        /// <summary>
        /// Gets or sets all of the occupancy event.
        /// </summary>
        /// <value>All occupancy event.</value>
        public Dictionary<string, EvaluationEvent> AllOccupancyEvent { get; set; }
        /// <summary>
        /// Gets or sets all simulation results.
        /// </summary>
        /// <value>All simulation results.</value>
        public Dictionary<string, SimulationResult> AllSimulationResults { get; set; }
        private static object _lock = new object();
        /// <summary>
        /// Adds the result of a new simulation. This method is thread safe.
        /// </summary>
        /// <param name="result">The result.</param>
        public void AddSimulationResult(SimulationResult result)
        {
            lock (_lock)
            {
                this.AllSimulationResults.Add(result.Name, result);
            }
        }

        /// <summary>
        /// Determines whether the document contains a spatial data field with the specified name.
        /// </summary>
        /// <param name="name">The name of the spatial data.</param>
        /// <returns><c>true</c> if the specified name contains data; otherwise, <c>false</c>.</returns>
        public bool ContainsSpatialData(string name)
        {
            if (this.cellularFloor.AllSpatialDataFields.ContainsKey(name))
            {
                return true;
            }
            if (this.AllActivities.ContainsKey(name))
            {
                return true;
            }
            if (this.AllSimulationResults.ContainsKey(name))
            {
                return true;
            }
            if (this.AllOccupancyEvent.ContainsKey(name))
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// Gets the spatial data.
        /// </summary>
        /// <param name="name">The name of the spatial data.</param>
        /// <returns>ISpatialData.</returns>
        public ISpatialData GetSpatialData(string name)
        {
            if (this.cellularFloor.AllSpatialDataFields.ContainsKey(name))
            {
                return this.cellularFloor.AllSpatialDataFields[name];
            }
            if (this.AllActivities.ContainsKey(name))
            {
                return this.AllActivities[name];
            }
            if (this.AllSimulationResults.ContainsKey(name))
            {
                return this.AllSimulationResults[name];
            }
            if (this.AllOccupancyEvent.ContainsKey(name))
            {
                return this.AllOccupancyEvent[name];
            }
            return null;
        }
        /// <summary>
        /// Gets or sets the isovist depth.
        /// </summary>
        /// <value>The isovist depth.</value>
        public double IsovistDepth { get; set; }
        private double _desiredCellSize = 0.30d;
        private double _barrierBufferValue { get; set; }
        /// <summary>
        /// The barrier type used to capture isovists
        /// </summary>
        public BarrierType IsovistBarrierType = BarrierType.Visual;
        internal ColorCodePolicy ColorCode { get; set; }
        /// <summary>
        /// Gets the agent isovist external depth.
        /// </summary>
        /// <value>The agent isovist external depth.</value>
        public double AgentIsovistExternalDepth { get { return Parameter.DefaultParameters[AgentParameters.OPT_IsovistExternalDepth].Value;} }
        /// <summary>
        /// Gets the maximum number of destinations in cellular isovists' perimeters.
        /// </summary>
        /// <value>The maximum number of destinations.</value>
        public int MaximumNumberOfDestinations { get { return (int)Parameter.DefaultParameters[AgentParameters.OPT_NumberOfDestinations].Value; } }
        /// <summary>
        /// The integration method by default set to Euler forward
        /// </summary>
        public static IntegrationMode IntegrationMethod = IntegrationMode.Euler;

        #region Definition of Parameters
        private Dictionary<string, Parameter> _parameters;
        /// <summary>
        /// Gets or sets the parameters used in this document.
        /// </summary>
        /// <value>The parameters.</value>
        public Dictionary<string, Parameter> Parameters
        {
            get { return _parameters; }
            set
            {
                _parameters = value;
                this._notifyPropertyChanged("Parameters");
            }
        }
        /// <summary>
        /// Adds the parameter to this documents.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        public void AddParameter(Parameter parameter)
        {
            if (this.Parameters.ContainsKey(parameter.Name))
            {
                MessageBox.Show(string.Format("'{0}' already exists!", parameter.Name));
                return;
            }
            this.Parameters.Add(parameter.Name, parameter);
            this._notifyPropertyChanged("Parameters");
            parameter.PropertyChanged += this.parameter_Value_Changed;
        }
        private void parameter_Value_Changed(object sender, PropertyChangedEventArgs e)
        {
            Parameter param = sender as Parameter;
            foreach (var item in param.LinkedFunctions)
            {
                if (item.CostCalculationType == CostCalculationMethod.WrittenFormula)
                {
                    try
                    {
                        string textFormula = (string)item.TextFormula.Clone();
                        foreach (var paramItem in this.Parameters)
                        {
                            if (textFormula.Contains(paramItem.Key))
                            {
                                textFormula = textFormula.Replace(paramItem.Key, paramItem.Value.Value.ToString());
                            }
                        }
                        CalculationEngine engine = new CalculationEngine();
                        Func<double, double> func = (Func<double, double>)engine.Formula(textFormula)
                            .Parameter("X", Jace.DataType.FloatingPoint)
                            .Result(Jace.DataType.FloatingPoint)
                            .Build();
                        item.GetCost = new CalculateCost(func);
                    }
                    catch (Exception error)
                    {
                        MessageBox.Show("Failed to update the parameter!\n\t" + error.Report());
                        return;
                    }
                }
            }
        }
        /// <summary>
        /// Removes a parameter from this document.
        /// </summary>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <returns><c>true</c> if parameter successfully deleted, <c>false</c> otherwise.</returns>
        /// <exception cref="ArgumentException">
        /// Failed to remove parameter: " + parameterName + "!\n\t" + error.Report()
        /// </exception>
        public bool RemoveParameter(string parameterName)
        {
            if (!this.Parameters.ContainsKey(parameterName))
            {
                throw new ArgumentException(string.Format("'{0}' does not exists!", parameterName));
            }
            var param = this.Parameters[parameterName];
            if (!param.CanBeDeleted)
            {
                MessageBox.Show("This is a read-only parameter and cannot be deleted!","", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return false;
            }
            foreach (var item in param.LinkedFunctions)
            {
                try
                {
                    item.TextFormula = item.TextFormula.Replace(parameterName, param.Value.ToString());
                }
                catch (Exception error)
                {
                    throw new ArgumentException("Failed to remove parameter: " + parameterName + "!\n\t" + error.Report());
                }
            }
            param.PropertyChanged -= this.parameter_Value_Changed;
            this.Parameters.Remove(parameterName);
            param = null;
            this._notifyPropertyChanged("Parameters");
            return true;
        }
        #endregion

        #region Definition of AgentScapeRoutes
        private Dictionary<Cell, AgentEscapeRoutes> _agentScapeRoutes;
        /// <summary>
        /// Gets or sets the agent scape routes for optional scenario simulations.
        /// </summary>
        /// <value>The agent scape routes.</value>
        public Dictionary<Cell, AgentEscapeRoutes> AgentScapeRoutes
        {
            get { return _agentScapeRoutes; }
            set
            {
                _agentScapeRoutes = value;
                this._notifyPropertyChanged("AgentScapeRoutes");
            }
        } 
        #endregion

        #region Definition of AgentMandatoryScenario
        private Scenario _agentMandatoryScenario;
        /// <summary>
        /// Gets or sets the agent mandatory scenario.
        /// </summary>
        /// <value>The agent mandatory scenario.</value>
        public Scenario AgentMandatoryScenario
        {
            get { return this._agentMandatoryScenario; }
            set
            {
                this._agentMandatoryScenario = value;
                this._notifyPropertyChanged("AgentMandatoryScenario");
            }
        } 
        #endregion
        
        #region Definition of VisualEventSettings
        private VisibilityTarget _visualEventSettings;
        /// <summary>
        /// Gets or sets the visibility targets for visual event analysis.
        /// </summary>
        /// <value>The visual event settings.</value>
        public VisibilityTarget VisualEventSettings
        {
            get { return _visualEventSettings; }
            set
            {
                if (this._visualEventSettings == null || this._visualEventSettings != value)
                {
                    _visualEventSettings = value;
                    this._notifyPropertyChanged("VisualEventSettings");
                }
            }
        }
        #endregion

        #region Definition of FreeNavigationAgentCharacter
        private FreeNavigationAgent _freeNavigationAgentCharacter;
        /// <summary>
        /// Gets or sets the free navigation agent character. This instance will be automatically generated when the user terminates the visualization of the optional scenrio.
        /// </summary>
        /// <value>The free navigation agent character.</value>
        public FreeNavigationAgent FreeNavigationAgentCharacter
        {
            get { return _freeNavigationAgentCharacter; }
            set
            {
                if (this._freeNavigationAgentCharacter != value)//(this._freeNavigationAgentCharacter == null || this._freeNavigationAgentCharacter != value)
                {
                    _freeNavigationAgentCharacter = value;
                    this._notifyPropertyChanged("FreeNavigationAgentCharacter");
                }

            }
        }
        #endregion

        //Scene Scaling and utilities
        #region Zoom
        /*
            COYRIGHT: The idea and implementation of the zoom and drag support in a ScrollViewer 
            are obtained from the following article in www.codeproject.com which is protected 
            under “The Code Project Open License (CPOL) 1.02”. The implementation is integrated 
            in the OSMDocument.xaml and OSMDocument.xaml.cs files with some changes in the 
            names of the variables.
        
            Article name:   WPF simple zoom and drag support in a ScrollViewer
            Author:         Kevin Stumpf	
            URL:            https://www.codeproject.com/Articles/97871/WPF-simple-zoom-and-drag-support-in-a-ScrollViewer
            License:        https://www.codeproject.com/info/cpol10.aspx
        */
        Point? lastCenterPositionOnTarget;
        Point? lastMousePositionOnTarget;
        Point? lastDragPoint;
        ScaleTransform scaleTransform { get; set; }
        #endregion

        //constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="OSMDocument"/> class.
        /// </summary>
        /// <param name="BarrierEnvironment">The barrier environment that is inherited from a BIM authoring tool.</param>
        public OSMDocument(BIM_To_OSM_Base BarrierEnvironment, I_OSM_To_BIM _BIM_Visualizer)
        {
            InitializeComponent();
            //this.loadLayers();
            this.Title = Assembly.GetExecutingAssembly().GetName().Name + " (" + BarrierEnvironment.PlanName + ")";
            // create revit visualizer
            this._OSM_to_BIM = _BIM_Visualizer;
            // adding the environment
            this._BIM_To_OSM = BarrierEnvironment;
            this.UnitConvertor = new UnitConversion(Length_Unit_Types.FEET, this.BIM_To_OSM.UnitType);
            this._desiredCellSize = this.UnitConvertor.Convert(_desiredCellSize,4);
            this.DepthOfView.IsEnabled = false;
            this.FloorScene.Background = Brushes.Transparent;
            
            this.UIMessage.Visibility = System.Windows.Visibility.Hidden;
            this.RenderTransformation = new MatrixTransform();

            this.AllOccupancyEvent = new Dictionary<string, EvaluationEvent>();
            this.AllSimulationResults = new Dictionary<string, SimulationResult>();
            this.AllActivities = new Dictionary<string, Activity>();
            this.Parameters = new Dictionary<string, Parameter>();
            this.IsovistDepth = Math.Min(BIM_To_OSM.FloorMaxBound.U - BIM_To_OSM.FloorMinBound.U, BIM_To_OSM.FloorMaxBound.V - BIM_To_OSM.FloorMinBound.V);
            this.Loaded += mainDocument_Loaded;
            Parameter.LoadDefaultParameters(Length_Unit_Types.FEET, this._BIM_To_OSM.UnitType);
            foreach (var item in Parameter.DefaultParameters)
            {
                this.AddParameter(item.Value);
            }
            this.AgentMandatoryScenario = new Scenario();

            this.ColorCode = ColorCodePolicy.LoadOSMPolicy();

            #region Zoom
            this._scrollViewer.ScrollChanged += OnScrollViewerScrollChanged;
            this._scrollViewer.MouseRightButtonUp += new MouseButtonEventHandler(scrollViewer_MouseRightButtonUp);
            this._scrollViewer.PreviewMouseWheel += OnPreviewMouseWheel;
            this._scrollViewer.PreviewMouseRightButtonDown += new MouseButtonEventHandler(scrollViewer_PreviewMouseRightButtonDown);
            this._scrollViewer.MouseMove += OnMouseMove;
            slider.ValueChanged += OnSliderValueChanged;
            scaleTransform = new ScaleTransform();
            this._grid.LayoutTransform = scaleTransform;
            #endregion
        }

        #region Zoom
        void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (lastDragPoint.HasValue)
            {
                Point posNow = e.GetPosition(this._scrollViewer);

                double dX = posNow.X - lastDragPoint.Value.X;
                double dY = posNow.Y - lastDragPoint.Value.Y;

                lastDragPoint = posNow;

                this._scrollViewer.ScrollToHorizontalOffset(this._scrollViewer.HorizontalOffset - dX);
                this._scrollViewer.ScrollToVerticalOffset(this._scrollViewer.VerticalOffset - dY);
            }
        }

        void scrollViewer_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var mousePos = e.GetPosition(this._scrollViewer);
            if (mousePos.X <= this._scrollViewer.ViewportWidth && mousePos.Y < this._scrollViewer.ViewportHeight) //make sure we still can use the scrollbars
            {
                this._scrollViewer.Cursor = Cursors.SizeAll;
                lastDragPoint = mousePos;
                Mouse.Capture(this._scrollViewer);
            }
        }

        void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            lastMousePositionOnTarget = Mouse.GetPosition(this._grid);

            if (e.Delta > 0)
            {
                slider.Value += 1;
            }
            if (e.Delta < 0)
            {
                slider.Value -= 1;
            }

            e.Handled = true;
        }

        void scrollViewer_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            this._scrollViewer.Cursor = Cursors.Arrow;
            this._scrollViewer.ReleaseMouseCapture();
            lastDragPoint = null;
            
        }

        void OnSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            scaleTransform.ScaleX = e.NewValue;
            scaleTransform.ScaleY = e.NewValue;

            var centerOfViewport = new Point(this._scrollViewer.ViewportWidth / 2, this._scrollViewer.ViewportHeight / 2);
            lastCenterPositionOnTarget = this._scrollViewer.TranslatePoint(centerOfViewport, this._grid);
        }

        void OnScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange != 0 || e.ExtentWidthChange != 0)
            {
                Point? targetBefore = null;
                Point? targetNow = null;

                if (!lastMousePositionOnTarget.HasValue)
                {
                    if (lastCenterPositionOnTarget.HasValue)
                    {
                        var centerOfViewport = new Point(this._scrollViewer.ViewportWidth / 2, this._scrollViewer.ViewportHeight / 2);
                        Point centerOfTargetNow = this._scrollViewer.TranslatePoint(centerOfViewport, this._grid);

                        targetBefore = lastCenterPositionOnTarget;
                        targetNow = centerOfTargetNow;
                    }
                }
                else
                {
                    targetBefore = lastMousePositionOnTarget;
                    targetNow = Mouse.GetPosition(this._grid);

                    lastMousePositionOnTarget = null;
                }

                if (targetBefore.HasValue)
                {
                    double dXInTargetPixels = targetNow.Value.X - targetBefore.Value.X;
                    double dYInTargetPixels = targetNow.Value.Y - targetBefore.Value.Y;

                    double multiplicatorX = e.ExtentWidth / this._grid.Width;
                    double multiplicatorY = e.ExtentHeight / this._grid.Height;

                    double newOffsetX = this._scrollViewer.HorizontalOffset - dXInTargetPixels * multiplicatorX;
                    double newOffsetY = this._scrollViewer.VerticalOffset - dYInTargetPixels * multiplicatorY;

                    if (double.IsNaN(newOffsetX) || double.IsNaN(newOffsetY))
                    {
                        return;
                    }

                    this._scrollViewer.ScrollToHorizontalOffset(newOffsetX);
                    this._scrollViewer.ScrollToVerticalOffset(newOffsetY);
                }
            }
        }
        #endregion
        void mainDocument_Loaded(object sender, RoutedEventArgs e)
        {
            this.LoadRenderTransform();

            this.FieldViewer.SetHost(this, "Field", SceneType.Field);
            this.VisualBarrierViewer.SetHost(this, "Visual Barriers", SceneType.VisualBarriers);
            this.PhysicalBarrierViewer.SetHost(this, "Physical Barriers", SceneType.PhysicalBarriers);
            this.CellularViewer.SetHost(this);
            this.PolygonalIsovistViewer.SetHost(this);
            this.ProxemicsViewer.SetHost(this);
            this.PathVisualer.SetHost(this);
            this.gradiantFieldViewer.SetHost(this);
            this.gradientViewer.SetHost(this);
            this.DataViewer.SetHost(this);
            this.DataAdder.SetHost(this);
            this.FieldAdder.SetHost(this);
            this._fieldEdgeViewer.SetHost(this);
            this.sequenceVisibilityEventHost.SetHost(this);
            this.activityAreaVisualHost.SetHost(this);
        }

        //this is ridiculous, but i have to do it to claim the memory back
        protected override void OnClosed(EventArgs e)
        {
            this.DataContext = null;
            this._cellularFloor = null;
            this.AllActivities.Clear();
            this.AllActivities = null;
            this.Parameters.Clear();
            this.Parameters = null;
            try
            {
                this.UnregisterName(this.FieldViewer.Name);
                this.ViewHolder.Children.Remove(this.FieldViewer);
                this.FieldViewer.Clear();
                this.FieldViewer = null;

                this.UnregisterName(this.DataViewer.Name);
                this.ViewHolder.Children.Remove(this.DataViewer);
                this.DataViewer.Clear();
                this.DataViewer = null;

                this.UnregisterName(this._fieldEdgeViewer.Name);
                this.ViewHolder.Children.Remove(this._fieldEdgeViewer);
                this._fieldEdgeViewer.Clear();
                this._fieldEdgeViewer = null;

                this.UnregisterName(this.CellularViewer.Name);
                this.ViewHolder.Children.Remove(this.CellularViewer);
                this.CellularViewer.Clear();
                this.CellularViewer = null;

                this.UnregisterName(this.PolygonalIsovistViewer.Name);
                this.ViewHolder.Children.Remove(this.PolygonalIsovistViewer);
                this.PolygonalIsovistViewer.Clear();
                this.PolygonalIsovistViewer = null;

                this.UnregisterName(this.ProxemicsViewer.Name);
                this.ViewHolder.Children.Remove(this.ProxemicsViewer);
                this.ProxemicsViewer.Clear();
                this.ProxemicsViewer = null;

                this.UnregisterName(this.gradiantFieldViewer.Name);
                this.ViewHolder.Children.Remove(this.gradiantFieldViewer);
                this.gradiantFieldViewer.Clear();
                this.gradiantFieldViewer = null;

                this.UnregisterName(this.gradientViewer.Name);
                this.ViewHolder.Children.Remove(this.gradientViewer);
                this.gradientViewer.Clear();
                this.gradientViewer = null;

                this.UnregisterName(this.PathVisualer.Name);
                this.ViewHolder.Children.Remove(this.PathVisualer);
                this.PathVisualer.Clear();
                this.PathVisualer = null;

                this.UnregisterName(this.PhysicalBarrierViewer.Name);
                this.ViewHolder.Children.Remove(this.PhysicalBarrierViewer);
                this.PhysicalBarrierViewer.Clear();
                this.PhysicalBarrierViewer = null;

                this.UnregisterName(this.VisualBarrierViewer.Name);
                this.ViewHolder.Children.Remove(this.VisualBarrierViewer);
                this.VisualBarrierViewer.Clear();
                this.VisualBarrierViewer = null;

                this.UnregisterName(this.GridViewer.Name);
                this.ViewHolder.Children.Remove(this.GridViewer);
                this.GridViewer.Clear();
                this.GridViewer = null;

                this.UnregisterName(this.FloorScene.Name);
                this.ViewHolder.Children.Remove(this.FloorScene);
                this.FloorScene = null;

                this.UnregisterName(this.JGViewer.Name);
                this.ViewHolder.Children.Remove(this.JGViewer);
                this.JGViewer.Clear();
                this.JGViewer = null;

                this.UnregisterName(this.ViewHolder.Name);
                this.ViewHolder.Children.Clear();
                this.ViewHolder = null;

                this.UnregisterName(this._viewbox.Name);
                this._viewbox.Child = null;
                this._viewbox = null;

                this.UnregisterName(this._grid.Name);
                this._grid.Children.Clear();
                this._grid = null;
                this.UnregisterName(this._scrollViewer.Name);
                this._scrollViewer.Content = null;
                this._scrollViewer = null;

                this.UnregisterName(this.DataAdder.Name);
                this.DataAdder.Clear();
                this.DataAdder = null;
                
                this.UnregisterName(this.FieldAdder.Name);
                this.FieldAdder.Clear();
                this.FieldAdder = null;

                this._OSM_to_BIM = null;
            }
            catch (Exception error)
            {
                MessageBox.Show("memory not released: " + this.GetType().ToString() + "\n" + error.Report());
            }
            base.OnClosed(e);
        }

        #region Transformation
        private void LoadRenderTransform()
        {
            //centralizing original drawing
            UV centerOfDrawing = .5 * (this.BIM_To_OSM.FloorMaxBound + this.BIM_To_OSM.FloorMinBound);
            Matrix mat = Matrix.Identity;
            mat.Translate(-centerOfDrawing.U, -centerOfDrawing.V);
            //load scaling factor
            UV diagonal = this.BIM_To_OSM.FloorMaxBound - this.BIM_To_OSM.FloorMinBound;
            double Su = (this.ViewHolder.RenderSize.Width - 10) / diagonal.U;
            double Sv = (this.ViewHolder.RenderSize.Height - 10) / diagonal.V;
            double scalingFactor = Math.Min(Su, Sv);
            //scaling the drawing
            mat.Scale(scalingFactor, -scalingFactor);
            //moving center to the center of the scene
            mat.Translate(this.ViewHolder.RenderSize.Width / 2, this.ViewHolder.RenderSize.Height / 2);
            this.RenderTransformation = new MatrixTransform(mat);
            this.InverseRenderTransform = this.RenderTransformation.Clone();
            Matrix invert = mat;
            invert.Invert();
            this.InverseRenderTransform = new MatrixTransform(invert);
        }
        /// <summary>
        /// Transforms the specified UV to a Point on the scene.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <returns>Point.</returns>
        public Point Transform(UV p)
        {
            Point pnt = new Point(p.U, p.V);
            return this.RenderTransformation.Transform(pnt);
        }
        /// <summary>
        /// Inverse transforms a point from the scene to a UV.
        /// </summary>
        /// <param name="p">The p.</param>
        /// <returns>UV.</returns>
        public UV TransformInverse(Point p)
        {
            var pnt = this.InverseRenderTransform.Transform(p);
            return new UV(pnt.X, pnt.Y);
        }
        #endregion
        
        #region Grid setting and visualization
        
        private void SetGridSize_Click(object sender, RoutedEventArgs e)
        {
            SetGridSize();
        }
        private void SetGridSize()
        {
            this.Menues.IsEnabled = false;
            GetNumber getnumber = new GetNumber("Enter your desired cell size", "The desired cell size will determine the resolution of the isovists.",
                this._desiredCellSize);
            getnumber.Owner = this;
            getnumber.ShowDialog(); 
            this._desiredCellSize = getnumber.NumberValue;
            getnumber = null;
            this.UIMessage.Text = "Pick a point on the walkable field!";
            this.UIMessage.Visibility = System.Windows.Visibility.Visible;
            this.Cursor = Cursors.Pen;
            FloorScene.MouseLeftButtonDown += FloorScene_WalkableArea;
        }
        private void FloorScene_WalkableArea(object sender, MouseButtonEventArgs e)
        {
            this.UIMessage.Visibility = System.Windows.Visibility.Hidden;
            var pointOnScreen = Mouse.GetPosition(this.FloorScene);
            var point = this.InverseRenderTransform.Transform(pointOnScreen);
            FloorScene.MouseLeftButtonDown -= FloorScene_WalkableArea;
            this.Cursor = Cursors.Arrow;
            UV p = new UV(point.X, point.Y);
            bool isPointOnTheArea = p.U > this.BIM_To_OSM.FloorMinBound.U &&
                p.U < this.BIM_To_OSM.FloorMaxBound.U &&
                p.V > this.BIM_To_OSM.FloorMinBound.V &&
                p.V < this.BIM_To_OSM.FloorMaxBound.V;

            if (!isPointOnTheArea)
            {
                MessageBox.Show("The selected point is not in the building area");
                this.UIMessage.Visibility = System.Windows.Visibility.Collapsed;
                this.Menues.IsEnabled = true;
                return;
            }

            this._cellularFloor = new CellularFloor(this._desiredCellSize, this.BIM_To_OSM, p);
            Cell cell = this.cellularFloor.FindCell(p);
            if (cell.FieldOverlapState== OverlapState.Overlap)
            {
                MessageBox.Show("Pick a point inside the walkable field or decrease the desried cell size and try again!");
                this.UIMessage.Visibility = System.Windows.Visibility.Collapsed;
                this.Menues.IsEnabled = true;
                this._cellularFloor = null;
                return;
            }
            this._fieldEdgeViewer.Highlight();
            FieldSelection fieldDialog = new FieldSelection();
            fieldDialog.Owner = this;
            fieldDialog.ShowDialog();
            if (fieldDialog.Retry)
            {
                fieldDialog = null;
                this._cellularFloor = null;
                this._fieldEdgeViewer.Clear();
                SetGridSize();
            }
            else
            {
                this.Menues.IsEnabled = true;
                fieldDialog = null;
                this._fieldEdgeViewer.Clear();
                this.gridSetting.Click -= SetGridSize_Click;
                this.gridSetting.Header = "Grid Information";
                this.gridSetting.Click += gridSetting_Click;
                this.IsovistMenu.IsEnabled = true;
                this.DepthOfView.IsEnabled = true;
                this.OptionalScenarios.IsEnabled = true;
                this.MandatoryScenario.IsEnabled = true;
                this.JGViewer.SetHost(this);
                this.GridViewer.SetHost(this, "Grid", SpatialAnalysis.Visualization.SceneType.Grid);
                this.freeNavigation.SetHost(this);
                this.mandatoryNavigation.SetHost(this);
                this.eventSetting.SetHost(this);
                this.trailVisualization.SetHost(this);
                this.GetData.IsEnabled = true;
                this.DataManagement.IsEnabled = true;
                this.GraphStartCell = this.cellularFloor.FindCell(p);
                this._barrierBufferValue = Parameter.DefaultParameters[AgentParameters.GEN_BodySize].Value / 2;
                this.DataManagement.IsEnabled = true;
                this._activities.IsEnabled = true;
                MenuItem about = new MenuItem
                {
                    Header = "About OSM",
                };
                this.Menues.Items.Add(about);
                about.Click += this.about_Clicked;
            }
        }

        void gridSetting_Click(object sender, RoutedEventArgs e)
        {
            this._fieldEdgeViewer.Highlight();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format("Grid width: {0}", this.cellularFloor.GridWidth.ToString()));
            sb.AppendLine(string.Format("Grid Height: {0}", this.cellularFloor.GridHeight.ToString()));
            sb.AppendLine(string.Format("Number of cells: {0}", (this.cellularFloor.GridHeight*this.cellularFloor.GridWidth).ToString()));
            sb.AppendLine(string.Format("Number of cells on walkable field: {0}", this.cellularFloor.NumberOfCellsInField.ToString()));
            sb.AppendLine(string.Format("Cell size: {0} {1}", this.cellularFloor.CellSize.ToString("0.0000"), this.BIM_To_OSM.UnitType.ToString().ToLower()));
            sb.AppendLine(string.Format("Cell area: {0} {1} squared", (this.cellularFloor.CellSize * this.cellularFloor.CellSize).ToString("0.0000"),this.BIM_To_OSM.UnitType.ToString().ToLower()));
            MessageBox.Show(sb.ToString(), "Grid Information");
            sb.Clear();
            sb = null;
            this._fieldEdgeViewer.Clear();
        }
        #endregion

        #region Expport and print
        
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            double dpi = 96;
            GetNumber getNumber0 = new GetNumber("Set Image Resolution",
                "The Scene will be exported in PGN format. Setting a heigh resolution value may crash this app.", dpi);
            getNumber0.Owner = this;
            getNumber0.ShowDialog();
            dpi = getNumber0.NumberValue;
            getNumber0 = null;
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Title = "Save the Scene to PNG Image format";
            dlg.DefaultExt = ".png";
            dlg.Filter = "PNG documents (.png)|*.png";
            Nullable<bool> result = dlg.ShowDialog(this);
            string fileAddress = "";
            if (result == true)
            {
                fileAddress = dlg.FileName;
            }
            else
            {
                return;
            }
            Rect bounds = VisualTreeHelper.GetDescendantBounds(this.ViewHolder);
            RenderTargetBitmap main_rtb = new RenderTargetBitmap((int)(bounds.Width * dpi / 96), (int)(bounds.Height * dpi / 96), dpi, dpi, System.Windows.Media.PixelFormats.Default);
            DrawingVisual dvFloorScene = new DrawingVisual();
            using (DrawingContext dc = dvFloorScene.RenderOpen())
            {
                VisualBrush vb = new VisualBrush(this.ViewHolder);
                dc.DrawRectangle(vb, null, bounds);
            }
            main_rtb.Render(dvFloorScene);
            BitmapEncoder pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(main_rtb));
            try
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                {
                    pngEncoder.Save(ms);
                    ms.Close();
                    System.IO.File.WriteAllBytes(fileAddress, ms.ToArray());
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Report(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveVisible_Click(object sender, RoutedEventArgs e)
        {
            double dpi = 96;
            GetNumber getNumber0 = new GetNumber("Set Image Resolution",
                "The Scene will be exported in PGN format. Setting a heigh resolution value may crash this app.", dpi);
            getNumber0.Owner = this;
            getNumber0.ShowDialog();
            dpi = getNumber0.NumberValue;
            getNumber0 = null;
            var background = this._scrollViewer.Background.Clone();
            this._scrollViewer.Background = null;
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Title = "Save the Scene to PNG Image format";
            dlg.DefaultExt = ".png";
            dlg.Filter = "PNG documents (.png)|*.png";
            Nullable<bool> result = dlg.ShowDialog(this);
            string fileAddress = "";
            if (result == true)
            {
                fileAddress = dlg.FileName;
            }
            else
            {
                this._scrollViewer.Background = background;
                return;
            }

            Rect bounds = VisualTreeHelper.GetDescendantBounds(this._scrollViewer);
            RenderTargetBitmap main_rtb = new RenderTargetBitmap((int)(bounds.Width * dpi / 96), (int)(bounds.Height * dpi / 96), dpi, dpi, System.Windows.Media.PixelFormats.Default);
            DrawingVisual dvFloorScene = new DrawingVisual();
            using (DrawingContext dc = dvFloorScene.RenderOpen())
            {
                VisualBrush vb = new VisualBrush(this._scrollViewer);
                dc.DrawRectangle(vb, null, bounds);
            }
            main_rtb.Render(dvFloorScene);
            BitmapEncoder pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(main_rtb));
            
            try
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                {
                    pngEncoder.Save(ms);
                    ms.Close();
                    System.IO.File.WriteAllBytes(fileAddress, ms.ToArray());
                }
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Report(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            this._scrollViewer.Background = background;
        }
        #endregion
        
        #region Set Environment
        private void SceneEnv_Click(object sender, RoutedEventArgs e)
        {
            this.SceneEnv.FontWeight = FontWeights.Bold;
            this.SceneEnv.IsChecked = true;

            this.RevitEnv.FontWeight = FontWeights.Normal;
            this.RevitEnv.IsChecked = false;
        }

        private void RevitEnv_Click(object sender, RoutedEventArgs e)
        {
            this.RevitEnv.FontWeight = FontWeights.Bold;
            this.RevitEnv.IsChecked = true;

            this.SceneEnv.FontWeight = FontWeights.Normal;
            this.SceneEnv.IsChecked = false;
        }
        #endregion
        
        #region Isovist
        private void depthOfView_Click(object sender, RoutedEventArgs e)
        {
            GetNumber getnumber = new GetNumber("Set the Depth of Visibility", "Areas that are beyond this distance are not considered visible.", this.IsovistDepth);
            getnumber.Owner = this;
            getnumber.ShowDialog();
            this.IsovistDepth = getnumber.NumberValue;
            getnumber = null;
        }  
        private void isovistInfo_Click(object sender, RoutedEventArgs e)
        {
            if (this.IsovistInformation == null)
            {
                MessageBox.Show("Cannot retrive the isovist information!", "Isovist Information");
            }
            else
            {
                MessageBox.Show(this.IsovistInformation.ToString(), "Isovist Information");
            }
            
        }
        private void barrierType_Click(object sender, RoutedEventArgs e)
        {
            foreach (MenuItem item in this.barrierTypeMenu.Items)
            {
                item.IsChecked = false;
            }
            MenuItem selected = (MenuItem)sender;
            selected.IsChecked = true;
            switch ((string)selected.Header)
            {
                case "Visual Barriers":
                    this.IsovistBarrierType = BarrierType.Visual;
                    break;
                case "Physical Barriers":
                    this.IsovistBarrierType = BarrierType.Physical;
                    break;
                case "Field Edges":
                    this.IsovistBarrierType = BarrierType.Field;
                    break;
                case "Barrier Buffer":
                    this.IsovistBarrierType = BarrierType.BarrierBuffer;
                    break;
                default:
                    break;
            }
            
        }
        #endregion

        #region Activities
        /// <summary>
        /// Gets or sets all of the activities used in mandatory scenario.
        /// </summary>
        /// <value>All activities.</value>
        public Dictionary<string, Activity> AllActivities { get; set; }
        /// <summary>
        /// Adds a new activity.
        /// </summary>
        /// <param name="activity">The new activity.</param>
        public void AddActivity(Activity activity)
        {
            this.AllActivities.Add(activity.Name, activity);
            this.ActiveFieldName.IsEnabled = true;
            MenuItem newMenuItem = new MenuItem();
            this.ActiveFieldName.Items.Add(newMenuItem);
            foreach (MenuItem item in this.ActiveFieldName.Items)
            {
                item.IsChecked = false;
            }
            newMenuItem.IsChecked = true;
            newMenuItem.Header = activity.Name;
            newMenuItem.Click += newMenuItem_Click;
            if (!this.PathVisualer.IsEnabled)
            {
                this.PathVisualer.IsEnabled = true;
            }
        }

        /// <summary>
        /// Removes an activity.
        /// </summary>
        /// <param name="activityName">Name of the activity.</param>
        public void RemoveActivity(string activityName)
        {
            #region updating the mandatory scenario

            bool affectsScenario = this.AgentMandatoryScenario.MainStations.Contains(activityName);
            if (!affectsScenario)
            {
                foreach (Sequence item in this.AgentMandatoryScenario.Sequences)
                {
                    if (item.ActivityNames.Contains(activityName))
                    {
                        affectsScenario = true;
                        break;
                    }
                }
            }
            if (affectsScenario)
            {
                var result = MessageBox.Show("The selected activity is set as part of the mandatory occupancy scenario.\nDo you want to continue?", "Warning",
                    MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                if (result == MessageBoxResult.OK)
                {
                    if (this.AgentMandatoryScenario.MainStations.Contains(activityName))
                    {
                        this.AgentMandatoryScenario.MainStations.Remove(activityName);
                    }
                    Sequence[] sequences = new Sequence[this.AgentMandatoryScenario.Sequences.Count];
                    this.AgentMandatoryScenario.Sequences.CopyTo(sequences);
                    for (int i = 0; i < sequences.Length; i++)
                    {
                        while (sequences[i].ActivityNames.Contains(activityName))
                        {
                            sequences[i].ActivityNames.Remove(activityName);
                        }
                    }
                    this.AgentMandatoryScenario.Sequences.Clear();
                    for (int i = 0; i < sequences.Length; i++)
                    {
                        if (sequences[i].ActivityCount != 0)
                        {
                            this.AgentMandatoryScenario.Sequences.Add(sequences[i]);
                        }
                    }
                }
                else
                {
                    return;
                }
            } 
            #endregion

            this.AllActivities.Remove(activityName);
            if (this.AllActivities.Count == 0)
            {
                this.ActiveFieldName.IsEnabled = false;
                this.PathVisualer.IsEnabled = false;
            }
            MenuItem menuItem = null;
            foreach (MenuItem item in this.ActiveFieldName.Items)
            {
                string name = (string)item.Header;
                if (name != null && name == activityName)
                {
                    menuItem = item;
                }
            }
            if (menuItem != null)
            {
                this.ActiveFieldName.Items.Remove(menuItem);
                menuItem.Click -= newMenuItem_Click;
                menuItem = null;
            }
        }
        private void newMenuItem_Click(object sender, RoutedEventArgs e)
        {
            foreach (MenuItem item in this.ActiveFieldName.Items)
            {
                item.IsChecked = false;
            }
            ((MenuItem)sender).IsChecked = true;
        }
        private void interpolation_Click(object sender, RoutedEventArgs e)
        {
            foreach (MenuItem item in InterpolationMethod.Items)
            {
                item.IsChecked = false;
            }
            string name = (string)(((MenuItem)sender).Name);
            foreach (InterpolationMethod val in Enum.GetValues(typeof(InterpolationMethod)))
            {
                if (val.ToString() == name)
                {
                    Activity.InterpolationMethod = val;
                }
            }
            ((MenuItem)sender).IsChecked = true;
        }
        private void interpolationRange_Click(object sender, RoutedEventArgs e)
        {
            GetNumber getNumber = new GetNumber("Interpolation Range",
                "The size of surrounding cell neighbors at which the gradients will be interpolated", 
                (double)Activity.GradientInterpolationNeighborhoodSize);
            getNumber.Owner = this;
            getNumber.ShowDialog();
            int range = (int)getNumber.NumberValue;
            if (range <0)
            {
                MessageBox.Show("Gradient interpolation neighborhood size cannot be set to zero!");
                getNumber = null;
                return;
            }
            Activity.GradientInterpolationNeighborhoodSize = range;
            getNumber = null;
        }
        #endregion

        #region Data Management

        private void import_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ParseCSV parser = new ParseCSV(this);
                if (parser.Succeed)
                {
                    List<int> validIndices = new List<int>();
                    foreach (int item in parser.importer.SelectedIndices)
                    {
                        if (!this.cellularFloor.AllSpatialDataFields.ContainsKey(parser.importer.Names[item]))
                        {
                            validIndices.Add(item);
                        }
                        else
                        {
                            MessageBox.Show("The following data already exists and cannot be added!\n\t" + parser.importer.Names[item]);
                        }
                    }
                    if (validIndices.Count == 0)
                    {
                        return;
                    }
                    var allValues = new Dictionary<int, Dictionary<Cell, double>>();
                    foreach (int item in validIndices)
                    {
                        allValues.Add(item, new Dictionary<Cell, double>());
                    }
                    Index cellIndex = new Index(0, 0);
                    for (int i = 0; i < this.cellularFloor.GridWidth; i++)
                    {
                        for (int j = 0; j < this.cellularFloor.GridHeight; j++)
                        {
                            cellIndex.I = i;
                            cellIndex.J = j;
                            //get the weigting factors
                            parser.LoadWeightingFactors(cellIndex);
                            //loop on the imported data
                            foreach (int item in validIndices)
                            {
                                //load data in the cells
                                double areas = 0, sum = 0;
                                foreach (KeyValuePair<Index, double> dataCell in parser.IndexToArea)
                                {
                                    if (dataCell.Value != 0)
                                    {
                                        areas += dataCell.Value;
                                        sum += parser.DataPoints[dataCell.Key.I, dataCell.Key.J].GetData(item) * dataCell.Value;
                                    }
                                }
                                double val = 0;
                                if (areas == 0)
                                {
                                    val = 0;
                                }
                                else
                                {
                                    val = sum / areas;
                                }
                                if (this.cellularFloor.Cells[i, j].FieldOverlapState != OverlapState.Outside)
                                {
                                    allValues[item].Add(this.cellularFloor.Cells[i, j], val);
                                }
                            }
                        }
                    }
                    foreach (KeyValuePair<int, Dictionary<Cell, double>> item in allValues)
                    {
                        if (true)
                        {
                            
                        }
                        SpatialDataField newData = new SpatialDataField(parser.importer.Names[item.Key], item.Value);
                        this.cellularFloor.AddSpatialDataField(newData);
                        //this.DataFields.Add(newData.Name, newData);
                    }
                }
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Report());
            }
        }

        private void export_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Title = "Export the data to a CSV file";
            dlg.DefaultExt = ".csv";
            dlg.Filter = "CSV documents (.CSV)|*.csv";
            Nullable<bool> result = dlg.ShowDialog(this);
            string fileAddress = "";
            if (result == true)
            {
                fileAddress = dlg.FileName;
            }
            else
            {
                return;
            }
            List<ISpatialData> allSpatialData = new List<ISpatialData>();
            foreach (var item in this.cellularFloor.AllSpatialDataFields.Values)
            {
                ISpatialData spatialData = item as ISpatialData;
                if (spatialData != null)
                {
                    allSpatialData.Add(spatialData);
                }
            }
            foreach (Activity item in this.AllActivities.Values)
            {
                ISpatialData activity = (ISpatialData)item;
                if (activity != null)
                {
                    allSpatialData.Add(activity);
                }
            }
            foreach (EvaluationEvent item in this.AllOccupancyEvent.Values)
            {
                ISpatialData event_ = (ISpatialData)item;
                if (event_ != null)
                {
                    allSpatialData.Add(event_);
                }
            }
            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(fileAddress))
            {
                var format = System.Globalization.CultureInfo.InvariantCulture;
                sw.Write("U,V");
                foreach (var item in allSpatialData)
                {
                    sw.Write(string.Format(",{0}", item.Name));
                }
                foreach (var item in this.cellularFloor.Cells)
                {
                    if (item.FieldOverlapState == OverlapState.Inside)
                    {
                        sw.Write(string.Format("\n{0},{1}", item.U.ToString(format), item.V.ToString(format)));
                        foreach (var data in allSpatialData)
                        {
                            if (data.Data.ContainsKey(item))
                            {
                                sw.Write(string.Format(",{0}", data.Data[item].ToString(format)));
                            }
                            else
                            {
                                sw.Write(string.Format(",{0}", double.NaN.ToString(format)));
                            }
                        }
                    }
                }
            }
            allSpatialData.Clear();
            allSpatialData = null;
        }

        private void removeData_Click(object sender, RoutedEventArgs e)
        {
            SpatialDataFieldSelection select = new SpatialDataFieldSelection(this,"Select Data to Remove", true);
            select.Owner = this;
            select.ShowDialog();
            if (select.Result)
            {
                foreach (ISpatialData item in select.AllSelectedSpatialData)
                {
                    switch (item.Type)
                    {
                        case SpatialAnalysis.Data.DataType.SpatialData:
                            this.cellularFloor.RemoveSpatialDataField(item.Name);
                            break;
                        case SpatialAnalysis.Data.DataType.ActivityPotentialField:
                            this.RemoveActivity(item.Name);
                            break;
                        case SpatialAnalysis.Data.DataType.OccupancyEvent:
                            break;
                        case SpatialAnalysis.Data.DataType.SimulationResult:
                            break;
                        default:
                            break;
                    }
                }
                
            }
            select = null;
        }

        private void dataStats_Click(object sender, RoutedEventArgs e)
        {
            var analyzer = new DataDescriptionAndComparison(this.cellularFloor);
            foreach (var item in this.AllActivities.Values) analyzer.LoadData(item);
            foreach (var item in this.AllOccupancyEvent.Values) analyzer.LoadData(item);
            foreach (var item in this.AllSimulationResults.Values) analyzer.LoadData(item);
            foreach (var item in this._cellularFloor.AllSpatialDataFields.Values) analyzer.LoadData(item);
            
            analyzer.Owner = this;
            analyzer.ShowDialog();
            analyzer = null;
        }

        private void getCoord_Click(object sender, RoutedEventArgs e)
        {
            this.Menues.IsEnabled = false;
            this.Cursor = Cursors.Pen;
            this.FloorScene.MouseLeftButtonDown += getCoord_Click_MouseLeftButtonDown;
            this.UIMessage.Text = "Click to get the coordinates";
            this.UIMessage.Visibility = System.Windows.Visibility.Visible;
        }

        private void getCoord_Click_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.FloorScene.MouseLeftButtonDown -= getCoord_Click_MouseLeftButtonDown;
            var pointScreen = Mouse.GetPosition(this.FloorScene);
            UV p = TransformInverse(pointScreen);
            this.Menues.IsEnabled = true;
            this.Cursor = Cursors.Arrow;
            this.UIMessage.Visibility = System.Windows.Visibility.Hidden;
            bool isPointOnTheArea = p.U > this.BIM_To_OSM.FloorMinBound.U &&
                p.U < this.BIM_To_OSM.FloorMaxBound.U &&
                p.V > this.BIM_To_OSM.FloorMinBound.V &&
                p.V < this.BIM_To_OSM.FloorMaxBound.V;

            if (!isPointOnTheArea)
            {
                MessageBox.Show("The selected point is not in the building area");
                return;
            }
            if (cellularFloor.FindCell(p).PhysicalOverlapState == OverlapState.Outside)
            {
                try
                {
                    var report = new SpatialAnalysis.CellularEnvironment.GetCellValue.DataCell(this.cellularFloor.FindCell(p), this.cellularFloor, this.AllActivities);
                    report.Owner = this;
                    report.ShowDialog();
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.Report());
                }
            }
            else
            {
                MessageBox.Show("Selected Point is out of the walkable area");
            }
            p = null;
        }

        private void data3DVisualizer_Click(object sender, RoutedEventArgs e)
        {
            VisualizerHost3D visualizer3D = new VisualizerHost3D(this);
            visualizer3D.Owner = this;
            visualizer3D.ShowDialog();
            visualizer3D = null;
        }

        private void dataControlPanel_Click(object sender, RoutedEventArgs e)
        {
            SpatialDataControlPanel controlPanel = new SpatialDataControlPanel(this, IncludedDataTypes.All);
            controlPanel.Owner = this;
            controlPanel.ShowDialog();
        }

        private void smoothenData_Click(object sender, RoutedEventArgs e)
        {
            GaussianFilterUI filter = new GaussianFilterUI(this);
            filter.ShowDialog();
            filter = null;
        }

        #endregion

        #region Debug Zone
        private void debug_Click(object sender, RoutedEventArgs e)
        {
            if (this.cellularFloor == null)
            {
                MessageBox.Show("The cellular floor should be loaded first");
                return;
            }
            string name = "StaticCost" + cellularFloor.AllSpatialDataFields.Count.ToString();
            SpatialDataField data = new SpatialDataField(name, cellularFloor.GetStaticCost());
            this.cellularFloor.AddSpatialDataField(data);
        }


        private void debug_Click_old(object sender, RoutedEventArgs e)
        {

            HashSet<int> cellAddresses = new HashSet<int>();
            foreach (Cell item in this.cellularFloor.Cells)
            {
                cellAddresses.Add(item.ID);
                Cell cell = cellularFloor.FindCell(item.ID);
                if (!cell.Equals(item))
                {
                    throw new ArgumentException("Failed");
                }
            }
            MessageBox.Show("Everything matched");
            foreach (Cell item in this.cellularFloor.Cells)
            {
                if (cellAddresses.Contains(item.ID))
                {
                    var result = MessageBox.Show("Addresses contains " + item.ID.ToString() + "\nDo you want to continue?",
                        "CHeck", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.No)
                    {
                        return;
                    }
                }
            }
            
        }

        private void debug_Click_old_old(object sender, RoutedEventArgs e)
        {
            var dlg_result = MessageBox.Show("Are you sure you want to calculate all of the isovists?", "Performance Comparison", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (dlg_result== MessageBoxResult.No)
            {
                return;
            }
            var shift = new UV(this.cellularFloor.CellSize / 2, this.cellularFloor.CellSize / 2);
            var timer24 = new System.Diagnostics.Stopwatch();
            timer24.Start();
            List<Isovist> isovists = new List<Isovist>();
            foreach (var item in this.cellularFloor.Cells)
            {
                if (item.VisualOverlapState == OverlapState.Outside)
                {
                    isovists.Add(new Isovist(item));
                    //SpatialAnalysis.IsovistUtility.CellularIsovistCalculator.GetIsovist(item.Origin + shift, this.isoDepth, BarrierType.Visual,
                    //    this.cellularFloor, .0001);
                }
            }
            Parallel.ForEach(isovists, (a) => a.Compute(this.IsovistDepth, BarrierType.Visual, this.cellularFloor, .0001));
            timer24.Stop();
            double t24 = timer24.Elapsed.TotalMilliseconds;
            MessageBox.Show("Time: " + ((int)t24).ToString());
        }

        #endregion

        #region Optional Walking Scenario
        private void barrierBuffer_Click(object sender, RoutedEventArgs e)
        {
            this.FreeNavigationAgentCharacter = null;
            this.AgentScapeRoutes = null;
            GetNumber offsetValue = new GetNumber("Set Boundary Buffer",
                "Minimum Distance from the barriers", this._barrierBufferValue);
            offsetValue.Owner = this;
            offsetValue.ShowDialog();
            this._barrierBufferValue = offsetValue.NumberValue;
            offsetValue = null;
            this._cellularFloor.LoadAllBarriersOffseted(this.BIM_To_OSM, this._barrierBufferValue);
            if (this.BarrierBufferVisualizer.Geometry == null)
            {
                this.BarrierBufferVisualizer.SetHost(this, "Barrier Buffer");
                //adding a new menu for isovist barrier types
                MenuItem barrierBufferTypeMenu = new MenuItem()
                {
                    Header = "Barrier Buffer",
                };
                barrierBufferTypeMenu.Click += barrierType_Click;
                this.barrierTypeMenu.Items.Add(barrierBufferTypeMenu);
            }
            else
            {
                this.BarrierBufferVisualizer.ReDraw();
            }
            this._calculatePossibleDestinations.IsEnabled = true;
            //this.freeNavigation.EnableAnimationMenu(false);
            
        }
        private void calculatePossibleDestinations_Click(object sender, RoutedEventArgs e)
        {
            var getAgentEspace_Route = new AgentRouteParameterSetting(this);
            getAgentEspace_Route.Owner = this;
            getAgentEspace_Route.ShowDialog();
            //this.freeNavigation.EnableAnimationMenu(true);
        }
        #endregion

        private void parameterSetting_Click(object sender, RoutedEventArgs e)
        {
            ParameterSetting parameterSetting = new ParameterSetting(this, false);
            parameterSetting.Owner = this;
            parameterSetting.ShowDialog();
        }

        #region Mandatory Scenario
        private void editScenario_Click(object sender, RoutedEventArgs e)
        {
            EditMandatoryScenario editMandatoryScenario = new EditMandatoryScenario(this);
            editMandatoryScenario.Owner = this;
            editMandatoryScenario.Closed += editMandatoryScenario_Closed;
            editMandatoryScenario.ShowDialog();          
        }

        private void editMandatoryScenario_Closed(object sender, EventArgs e)
        {
            ((EditMandatoryScenario)sender).Closed -= editMandatoryScenario_Closed;
            if (!this.AgentMandatoryScenario.IsReadyForPerformance())
            {
                MessageBox.Show(this.AgentMandatoryScenario.Message);
                return;
            }
            DebugReporter reporter = new DebugReporter();
            this.AgentMandatoryScenario.LoadQueues(this.AllActivities, 0.0d);
            this.AgentMandatoryScenario.AdjustFirstExpectedTaksActivation(4);
            foreach (var item in this.AgentMandatoryScenario.ExpectedTasks)
            {
                reporter.AddReport(item.ToString());
            }
            reporter.ShowDialog();
        }
        
        #endregion

        private void eulerForward_Click(object sender, RoutedEventArgs e)
        {
            OSMDocument.IntegrationMethod = IntegrationMode.Euler;
            this._eulerForward.IsChecked = true;
            this._RK4.IsChecked = false;
        }

        private void RK4_Click(object sender, RoutedEventArgs e)
        {
            OSMDocument.IntegrationMethod = IntegrationMode.RK4;
            this._eulerForward.IsChecked = false;
            this._RK4.IsChecked = true;
        }

        private void visualizeSequence_Click(object sender, RoutedEventArgs e)
        {
            VisualizeSequence visualizeSequence = new VisualizeSequence(this);
            visualizeSequence.Owner = this;
            visualizeSequence.ShowDialog();
            visualizeSequence = null;
        }

        private void about_Clicked(object sender, RoutedEventArgs e)
        {
            About about = new About();
            about.Owner = this;
            Uri address = new Uri("https://rawgit.com/zarrinmehr/OccupancySimulationModel/master/about.html");
            about.html_Page.Source = address;
            about.ShowDialog();
        }

        private void _reverseColorCode_Click(object sender, RoutedEventArgs e)
        {
            this.ColorCode.Reverse();
        }
    }



}



