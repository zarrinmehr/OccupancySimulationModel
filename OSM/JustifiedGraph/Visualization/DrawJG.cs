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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using SpatialAnalysis.FieldUtility;
using SpatialAnalysis.JustifiedGraph;
using SpatialAnalysis.CellularEnvironment;
using SpatialAnalysis.Geometry;
using SpatialAnalysis.Miscellaneous;

namespace SpatialAnalysis.JustifiedGraph.Visualization
{
    /// <summary>
    /// Class DrawJG. Draws the Justified Graph
    /// </summary>
    /// <seealso cref="System.Windows.Controls.Canvas" />
    internal class DrawJG : Canvas
    {
        #region Graph field variables

        private JGGraph jgGraph { get; set; }
        private JGVertex rootVertex { get; set; }
        private Map<JGVertex, Line> vertex_mark = new Map<JGVertex, Line>();
        private Map<UVLine, Line> edge_line = new Map<UVLine, Line>();
        private List<HashSet<JGVertex>> JGHierarchy = null;
        private Line rootLine { get; set; }
        private bool move = true;
        
        #endregion

        #region Movement Mode
        private enum MoveMode
        {
            Horizontally = 0,
            Freely = 1,
        }
        private MoveMode _moveMode { get; set; }
        private ComboBox _movementMode { get; set; }
        #endregion

        private double lineThickness = 10;
        private Brush lineBrush = Brushes.Aqua;
        private double EdgeThickness = 5;
        private Brush edgeBrush = Brushes.Green;

        /// <summary>
        /// Initializes a new instance of the <see cref="DrawJG"/> class.
        /// </summary>
        public DrawJG()
        {
            this.Background = Brushes.Transparent;
            this.Loaded += new RoutedEventHandler(DrawJG_Loaded);
            this._moveMode = MoveMode.Horizontally;
        }
        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            this.Children.Clear();
            this.jgGraph = null;
            this.rootVertex = null;
            this.vertex_mark.Clear();
            this.vertex_mark = null;
            this.edge_line.Clear();
            this.edge_line = null;
            if (this.JGHierarchy != null)
            {
                this.JGHierarchy.Clear();
                this.JGHierarchy = null;
            }
            this.rootLine = null;
        }
        private void DrawJG_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.jgGraph == null)
            {
                return;
            }
            double levelHeight = 100;
            double levelwidth = 100;
            double[] yValues = new double[JGHierarchy.Count];
            yValues[0] = (this.RenderSize.Height - (JGHierarchy.Count - 1) * levelHeight) / 2;
            if (yValues[0] < 0)
            {
                levelHeight = (this.RenderSize.Height - 20) / (JGHierarchy.Count - 1);
                yValues[0] = 10;
            }
            for (int i = 1; i < JGHierarchy.Count; i++)
            {
                yValues[i] = yValues[i - 1] + levelHeight;
            }
            for (int i = 0; i < yValues.Length; i++)
            {
                double w = levelwidth;
                double x0 = (this.RenderSize.Width - (JGHierarchy[i].Count - 1) * w) / 2;
                if (x0 < 0)
                {
                    w = (this.RenderSize.Width - 20) / (JGHierarchy[i].Count - 1);
                    x0 = 10;
                }
                int j = 0;
                foreach (JGVertex vertex in JGHierarchy[i])
                {
                    Point p = new Point(x0 + j * w, yValues[yValues.Length - 1 - i]);
                    vertex.Point = new UV(p.X, p.Y);
                    j++;
                }
            }
            //draw Vertices
            foreach (JGVertex item in this.jgGraph.Vertices)
            {
                Line l = new Line()
                {
                    X1 = item.Point.U - this.lineThickness / 2,
                    X2 = item.Point.U + this.lineThickness / 2,
                    Y1 = item.Point.V,
                    Y2 = item.Point.V,
                    StrokeThickness = this.lineThickness,
                    Stroke = this.lineBrush
                };
                this.vertex_mark.Add(item, l);
                Canvas.SetZIndex(l, 2);
                this.Children.Add(l);
                l.MouseDown += new MouseButtonEventHandler(l_MouseDown);
            }
            Line main = this.vertex_mark.Find(this.JGHierarchy[0].First());
            main.Stroke = Brushes.DarkRed;
            //draw lines
            var lines = this.jgGraph.ToLines();
            foreach (UVLine item in lines)
            {
                Line edge = new Line()
                {
                    X1 = item.Start.U,
                    Y1 = item.Start.V,
                    X2 = item.End.U,
                    Y2 = item.End.V,
                    StrokeThickness = this.EdgeThickness,
                    Stroke = this.edgeBrush
                };
                this.edge_line.Add(edge, item);
                Canvas.SetZIndex(edge, 1);
                this.Children.Add(edge);
            }
        }

        private void l_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (this.move)
                {
                    this.rootLine = (Line)sender;
                    this.rootVertex = this.vertex_mark.Find(this.rootLine);
                    this.MouseMove += new MouseEventHandler(DrawJG_MouseMove);
                    this.move = false;
                    return;
                }
                else
                {
                    this.rootLine = null;
                    this.rootVertex = null;
                    this.MouseMove -= DrawJG_MouseMove;
                    this.move = true;
                    return;
                }
            }
            catch (Exception error)
            {
                MessageBox.Show("from mose down \n"+error.Report());
            }

        }

        private void DrawJG_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                double y = this.rootVertex.Point.V;
                Point point = Mouse.GetPosition(this);
                UV p = new UV(point.X, point.Y);
                if (this._moveMode == MoveMode.Horizontally)
                {
                    point.Y = y;
                    p.V = y;
                }
                
                foreach (JGVertex item in this.rootVertex.Connections)
                {
                    //create the UVLine
                    UVLine uvLine = new UVLine(this.rootVertex.Point.Copy(), item.Point);
                    //find the Line from the map
                    Line line = this.edge_line.Find(uvLine);
                    //clear the pair of UVLine and Line from the map
                    this.edge_line.Remove(uvLine);
                    //update the UVLine with deep copies of the endpoints
                    uvLine.Start = p.Copy();
                    uvLine.End = item.Point.Copy();
                    //update the line
                    line.X1 = uvLine.Start.U;
                    line.Y1 = uvLine.Start.V;
                    line.X2 = uvLine.End.U;
                    line.Y2 = uvLine.End.V;
                    // Add the pair of UVLine and Line to the map
                    this.edge_line.Add(line, uvLine);
                }
                //updating the vertex and its mark
                this.vertex_mark.Remove(this.rootLine);
                this.rootVertex.Point = p;
                this.rootLine.X1 = point.X - this.lineThickness / 2;
                this.rootLine.X2 = point.X + this.lineThickness / 2;
                this.rootLine.Y1 = point.Y;
                this.rootLine.Y2 = point.Y;
                this.vertex_mark.Add(this.rootVertex, this.rootLine);

            }
            catch (Exception )
            {
                //MessageBox.Show("from mouse move event: \n" + error.Message);
            }


        }

        public void SetGraph(JGGraph _JGGraph, List<HashSet<JGVertex>> _hierarchy)
        {
            this.jgGraph = _JGGraph;
            this.JGHierarchy = _hierarchy;
        }
        /// <summary>
        /// Sets the node movement mode to free movement or orthogonal 
        /// </summary>
        /// <param name="movementMode">The movement mode.</param>
        public void SetNodeMovementMode(ComboBox movementMode)
        {
            this._movementMode = movementMode;
            this._movementMode.SelectionChanged += new SelectionChangedEventHandler(_movementMode_SelectionChanged);
        }

        private void _movementMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string textValue = ((TextBlock)this._movementMode.SelectedItem).Text;
            if (textValue == "Move Nodes Horizontally")
            {
                this._moveMode = MoveMode.Horizontally;
            }
            else
            {
                this._moveMode = MoveMode.Freely;
            }
        }
      
    }
}

