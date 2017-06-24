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
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Input;
using SpatialAnalysis.Visualization;
using System.Collections.Generic;
using System;
using SpatialAnalysis.JustifiedGraph;
using TriangleNet;
using SpatialAnalysis.IsovistUtility;
using SpatialAnalysis.CellularEnvironment;
using SpatialAnalysis.Geometry;
using SpatialAnalysis.Miscellaneous;

namespace SpatialAnalysis.JustifiedGraph.Visualization
{
    /// <summary>
    /// Class ConvexSpace. Designed for the visualization of the graph.
    /// </summary>
    /// <seealso cref="System.Windows.Controls.Canvas" />
    public class ConvexSpace : Canvas
    {
        private OSMDocument _host { get; set; }
        //menues
        private MenuItem JustifiedGraphMenu { get; set; }
        private MenuItem CreateCovexGraph { get; set; }
        private MenuItem EditGraph { get; set; }
        private MenuItem AddVertex { get; set; }
        private MenuItem RemoveVertex { get; set; }
        private MenuItem MoveVertex { get; set; }
        private MenuItem AddEdge { get; set; }
        private MenuItem RemoveEdge { get; set; }
        private MenuItem DrawJG { get; set; }
        private MenuItem Hide_show_Menu { get; set; }

        #region Graph field variables
        private HashSet<Node> JGNodes = new HashSet<Node>();
        private JGGraph jgGraph { get; set; }
        List<JGEdge> edges = new List<JGEdge>();
        bool nodeRemovingMode = false;
        Line edgeLine = null;
        public Node node1 = null;
        public Node node2 = null;
        bool edgeRemoveMode = false;
        HashSet<JGEdge> relatedEdges = null;
        JGVertex root = null;
        private List<HashSet<JGVertex>> JGHierarchy = null;
        #endregion


        /// <summary>
        /// Initializes a new instance of the <see cref="ConvexSpace"/> class.
        /// </summary>
        public ConvexSpace()
        {
            //Main Menu
            this.JustifiedGraphMenu = new MenuItem() { Header = "Justified Graph" };
            //Create Menu
            this.CreateCovexGraph = new MenuItem() { Header = "Create Convex Graph" };
            this.CreateCovexGraph.Click += CreateConvexGraph_Click;
            this.JustifiedGraphMenu.Items.Add(this.CreateCovexGraph);
            //Hide and show menu
            this.Hide_show_Menu = new MenuItem()
            {
                Header = "Hide Justified Graph",
                IsEnabled = false
            };
            this.Hide_show_Menu.Click += new RoutedEventHandler(Hide_show_Menu_Click);
            this.JustifiedGraphMenu.Items.Add(this.Hide_show_Menu);
            //Edit Menu
            this.EditGraph = new MenuItem()
            {
                Header = "Edit Graph",
                IsEnabled = false
            };
            this.JustifiedGraphMenu.Items.Add(this.EditGraph);
            //Add vertices
            this.AddVertex = new MenuItem() { Header = "Add Convex Space" };
            this.AddVertex.Click += AddVertex_Click;
            this.EditGraph.Items.Add(this.AddVertex);
            //Remove vertices
            this.RemoveVertex = new MenuItem() { Header = "Remove Convex Space" };
            this.RemoveVertex.Click += RemoveVertex_Click;
            this.EditGraph.Items.Add(this.RemoveVertex);
            //Move vertex
            this.MoveVertex = new MenuItem() { Header = "Move Convex Space" };
            this.MoveVertex.Click += MoveVertex_Click;
            this.EditGraph.Items.Add(this.MoveVertex);
            //Add edge
            this.AddEdge = new MenuItem() { Header = "Add Connection" };
            this.AddEdge.Click += AddEdge_Click;
            this.EditGraph.Items.Add(this.AddEdge);
            //Remove Edge
            this.RemoveEdge = new MenuItem() { Header = "Remove Connection" };
            this.RemoveEdge.Click += RemoveEdge_Click;
            this.EditGraph.Items.Add(this.RemoveEdge);
            //Draw
            this.DrawJG = new MenuItem() 
            { 
                Header = "Draw Justified Graph",
                IsEnabled=false
            };
            this.DrawJG.Click += DrawJG_Click;
            this.JustifiedGraphMenu.Items.Add(this.DrawJG);


        }

        void Hide_show_Menu_Click(object sender, RoutedEventArgs e)
        {
            if ((string)this.Hide_show_Menu.Header == "Hide Justified Graph")
            {
                this.EditGraph.IsEnabled = false;
                this.DrawJG.IsEnabled = false;
                this.Hide_show_Menu.Header = "Show Justified Graph";
                this.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                this.EditGraph.IsEnabled = true;
                this.DrawJG.IsEnabled = true;
                this.Hide_show_Menu.Header = "Hide Justified Graph";
                this.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void CreateConvexGraph_Click(object sender, RoutedEventArgs e)
        {
            if ((string)this.CreateCovexGraph.Header == "Create Convex Graph")
            {
                this.Visibility = System.Windows.Visibility.Visible;
                #region clearing the nodes and edges in case of reset
                foreach (Node node in this.JGNodes)
                {
                    node.Clear();
                }
                Node.LineToUVGuide.Clear();
                this.JGNodes.Clear();
                JGEdge.LineToEdgeGuide.Clear();
                foreach (JGEdge edge in this.edges)
                {
                    edge.Clear();
                }
                this.edges.Clear();
                #endregion
                this._host.Menues.IsEnabled = false;
                this._host.CommandReset.Click += endBtn_Click;
                this._host.UIMessage.Text = "Select points at the center of convex spaces";
                this._host.UIMessage.Visibility = System.Windows.Visibility.Visible;
                //this.JGraphMode = true;
                this._host.Cursor = Cursors.Pen;
                this._host.FloorScene.MouseLeftButtonDown += FloorScene_MouseLeftButtonDown;
            }
            else
            {
                this.Hide_show_Menu.IsEnabled = false;
                this.EditGraph.IsEnabled = false;
                this.DrawJG.IsEnabled = false;
                this.CreateCovexGraph.Header = "Create Convex Graph";
                this.Children.Clear();
                //resetting the graph
                this.JGNodes.Clear();
                this.jgGraph = null;
                this.edges.Clear();
                this.nodeRemovingMode = false;
                edgeLine = null;
                node1 = null;
                node2 = null;
                edgeRemoveMode = false;
                relatedEdges = null;
                root = null;
                JGHierarchy = null;
            }
        }

        private void FloorScene_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            UV p = this._host.TransformInverse(Mouse.GetPosition(this._host.FloorScene));
            try
            {
                Cell cell = this._host.cellularFloor.FindCell(p);
                if (cell.VisualOverlapState != OverlapState.Outside)
                {
                    MessageBox.Show("The selected point is outside the walkable filed");
                    return;
                }
                Node node = new Node(p);
                node.Draw();
                this.JGNodes.Add(node);
            }
            catch (Exception)
            {
                MessageBox.Show("The selected point is outside the walkable filed");
                return;
            }
        }

        private void endBtn_Click(object sender, RoutedEventArgs e)
        {
            this._host.CommandReset.Click -= endBtn_Click;
            this._host.Menues.IsEnabled = true;
            this._host.UIMessage.Visibility = Visibility.Hidden;
            //this.JGraphMode = false;
            this._host.Cursor = Cursors.Arrow;
            this._host.FloorScene.MouseLeftButtonDown -= FloorScene_MouseLeftButtonDown;
            try
            {
                #region Create Graph
                HashSet<UV> pnts = new HashSet<UV>();
                TriangleNet.Behavior behavior = new Behavior();
                TriangleNet.Mesh t_mesh = new TriangleNet.Mesh();
                TriangleNet.Geometry.InputGeometry geom = new TriangleNet.Geometry.InputGeometry();
                foreach (Node node in JGNodes)
                {
                    TriangleNet.Data.Vertex vertex = new TriangleNet.Data.Vertex(node.Coordinates.U, node.Coordinates.V, 0);
                    geom.AddPoint(vertex);
                    pnts.Add(node.Coordinates);
                }
                t_mesh.Triangulate(geom);

                var graph = new JGGraph(pnts);
                foreach (var item in t_mesh.Triangles)
                {
                    UV a = null;
                    var vrtx = t_mesh.GetVertex(item.P0);
                    if (vrtx != null)
                    {
                        a = new UV(vrtx.X, vrtx.Y);
                    }
                    UV b = null;
                    vrtx = t_mesh.GetVertex(item.P1);
                    if (vrtx != null)
                    {
                        b = new UV(vrtx.X, vrtx.Y);
                    }
                    UV c = null;
                    vrtx = t_mesh.GetVertex(item.P2);
                    if (vrtx != null)
                    {
                        c = new UV(vrtx.X, vrtx.Y);
                    }
                    if (a != null && b != null)
                    {
                        graph.AddConnection(a, b);
                    }
                    if (a != null && c != null)
                    {
                        graph.AddConnection(a, c);
                    }
                    if (c != null && b != null)
                    {
                        graph.AddConnection(c, b);
                    }
                }
                #endregion
                #region Remove Edges with isovists at the ends that do not overlap
                this.edges = graph.ToEdges();
                Dictionary<int, Isovist> IsovistGuid = new Dictionary<int, Isovist>();
                foreach (JGVertex item in graph.Vertices)
                {
                    double x = double.NegativeInfinity;
                    foreach (JGVertex vertex in item.Connections)
                    {
                        var y = item.Point.DistanceTo(vertex.Point);
                        if (y > x)
                        {
                            x = y;
                        }
                    }
                    var isovist = CellularIsovistCalculator.GetIsovist(item.Point, x, BarrierType.Visual, this._host.cellularFloor);
                    IsovistGuid.Add(item.Point.GetHashCode(), isovist);
                }
                HashSet<JGEdge> visibleVertexes = new HashSet<JGEdge>();
                foreach (JGEdge item in this.edges)
                {
                    Isovist v1 = null;
                    IsovistGuid.TryGetValue(item.P1.GetHashCode(), out v1);
                    Isovist v2 = null;
                    IsovistGuid.TryGetValue(item.P2.GetHashCode(), out v2);
                    if (v1 != null && v2 != null)
                    {
                        if (v2.VisibleCells.Overlaps(v1.VisibleCells))
                        {
                            visibleVertexes.Add(item);
                        }
                    }
                }
                #endregion
                #region setting the edges
                JGEdge.LineToEdgeGuide.Clear();
                foreach (JGEdge edge in this.edges)
                {
                    edge.Clear();
                }
                this.edges = visibleVertexes.ToList<JGEdge>();
                foreach (JGEdge item in this.edges)
                {
                    item.Draw();
                }
                #endregion
                //cleaning up the used data
                t_mesh = null;
                graph = null;
                geom.Clear();
                geom = null;
                visibleVertexes = null;
                IsovistGuid = null;
                //enabling edit mode
                this.EditGraph.IsEnabled = true;
                this.DrawJG.IsEnabled = true;
                this.Hide_show_Menu.IsEnabled = true;
                this.CreateCovexGraph.Header = "Reset Convex Graph";

            }
            catch (Exception error)
            {
                MessageBox.Show(error.Report());
            }

        }
        private void ClearJGNodes_Click(object sender, RoutedEventArgs e)
        {
            foreach (Node item in JGNodes)
            {
                item.Clear();
            }
            JGNodes.Clear();
            Node.LineToUVGuide.Clear();
            foreach (JGEdge item in edges)
            {
                item.Clear();
            }
            edges.Clear();
            JGEdge.LineToEdgeGuide.Clear();

            
        }
        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            this._host = null;
            foreach (Node item in JGNodes)
            {
                item.Clear();
            }
            JGNodes.Clear();
            Node.LineToUVGuide.Clear();
            foreach (JGEdge item in edges)
            {
                item.Clear();
            }
            edges.Clear();
            JGEdge.LineToEdgeGuide.Clear();
            this.CreateCovexGraph.Click -= CreateConvexGraph_Click;
            this.Hide_show_Menu.Click -= Hide_show_Menu_Click;
            this.AddVertex.Click -= AddVertex_Click;
            this.RemoveVertex.Click -= RemoveVertex_Click;
            this.MoveVertex.Click -= MoveVertex_Click;
            this.AddEdge.Click -= AddEdge_Click;
            this.RemoveEdge.Click -= RemoveEdge_Click;
            this.DrawJG.Click -= DrawJG_Click;
            this.JustifiedGraphMenu = null;
            this.CreateCovexGraph = null;
            this.EditGraph = null;
            this.AddVertex = null;
            this.RemoveVertex = null;
            this.MoveVertex = null;
            this.AddEdge = null;
            this.RemoveEdge = null;
            this.DrawJG = null;
            this.Hide_show_Menu = null;
            if (this.JGNodes != null)
            {
                this.JGNodes.Clear();
                this.JGNodes = null;
            }
            this.jgGraph = null;
            if (this.edges != null)
            {
                this.edges.Clear();
                this.edges = null;
            }
            this.edgeLine = null;
            this.node1 = null;
            this.node2 = null;
            if (this.relatedEdges != null)
            {
                this.relatedEdges.Clear();
                this.relatedEdges = null;
            }
            this.root = null;
            if (this.JGHierarchy != null)
            {
                this.JGHierarchy.Clear();
                this.JGHierarchy = null;
            }
            Node.FloorScene = null;
            Node.Transform = null;
            JGEdge.FloorScene = null;
            JGEdge.Transform = null;
        }

        #region Adding nodes
        private void AddVertex_Click(object sender, RoutedEventArgs e)
        {
            this._host.Menues.IsEnabled = false;
            this._host.UIMessage.Text = "Click to add a new convex space";
            this._host.UIMessage.Visibility = Visibility.Visible;
            this._host.CommandReset.Click += endAddingNodes_Click;
            //this.JGraphMode = true;
            this._host.Cursor = Cursors.Pen;
            this._host.FloorScene.MouseLeftButtonDown += FloorScene_MouseLeftButtonDown;
        }
        private void endAddingNodes_Click(object sender, RoutedEventArgs e)
        {
            this._host.CommandReset.Click -= endAddingNodes_Click;

            this._host.Menues.IsEnabled = true;
            this._host.UIMessage.Visibility = Visibility.Hidden;
            //this.JGraphMode = false;
            this._host.Cursor = Cursors.Arrow;
            this._host.FloorScene.MouseLeftButtonDown -= FloorScene_MouseLeftButtonDown;
        }
        #endregion

        #region Removing nodes
        
        private void RemoveVertex_Click(object sender, RoutedEventArgs e)
        {
            Node.SelectToRemove = this.markNodeToRemove;
            nodeRemovingMode = true;
            this._host.Menues.IsEnabled = false;
            this._host.UIMessage.Text = "Click on a vertex to remove it";
            this._host.UIMessage.Visibility = Visibility.Visible;

            this._host.CommandReset.Click += endRemovingNodes_Click;
        }
        private void endRemovingNodes_Click(object sender, RoutedEventArgs e)
        {
            Node.SelectToRemove = null;
            this._host.Menues.IsEnabled = true;
            this._host.UIMessage.Visibility = Visibility.Hidden;
            this._host.CommandReset.Click -= endRemovingNodes_Click;
            this.nodeRemovingMode = false;
            #region updating the nodes
            HashSet<Node> SelectedNodes = new HashSet<Node>();
            foreach (Node node in this.JGNodes)
            {
                if (node.Mark.Visibility == Visibility.Hidden)
                {
                    SelectedNodes.Add(node);
                }
            }
            MessageBox.Show(SelectedNodes.Count.ToString() + " nodes were choosen to remove");
            foreach (Node node in SelectedNodes)
            {
                this.JGNodes.Remove(node);
                node.Clear();
            }
            #endregion
            #region
            HashSet<UV> removedPoints = new HashSet<UV>();
            foreach (Node item in SelectedNodes)
            {
                removedPoints.Add(item.Coordinates);
            }
            HashSet<JGEdge> removableEdges = new HashSet<JGEdge>();
            foreach (JGEdge edge in this.edges)
            {
                if (removedPoints.Contains(edge.P1) || removedPoints.Contains(edge.P2))
                {
                    removableEdges.Add(edge);
                }
            }
            MessageBox.Show(removableEdges.Count.ToString() + " edges were choosen to remove");
            foreach (JGEdge edge in removableEdges)
            {
                this.edges.Remove(edge);
                edge.Clear();
            }
            removableEdges = null;
            SelectedNodes = null;
            #endregion

        }
        private bool markNodeToRemove(Line line)
        {
            if (nodeRemovingMode)
            {
                line.Visibility = System.Windows.Visibility.Hidden;
            }
            return true;
        }
        #endregion

        #region Adding Edges
        
        private void DrawLine(object sender, MouseEventArgs e)
        {
            if (edgeLine != null)
            {
                int index = this._host.FloorScene.Children.IndexOf(edgeLine);
                if (index != -1)
                {
                    this._host.FloorScene.Children.RemoveAt(index);
                }
            }
            if (node1 != null)
            {
                Point p1 = this._host.Transform(node1.Coordinates);
                Point p2 = Mouse.GetPosition(this._host.FloorScene);
                edgeLine = new Line()
                {
                    X1 = p1.X,
                    Y1 = p1.Y,
                    X2 = p2.X,
                    Y2 = p2.Y,
                    Stroke = Brushes.Pink,
                    StrokeThickness = 1,
                };
                this._host.FloorScene.Children.Add(edgeLine);
            }
        }
        private void AddEdge_Click(object sender, RoutedEventArgs e)
        {
            this._host.FloorScene.MouseMove += DrawLine;
            Node.SelectForEdge = selectNodes;
            this._host.Menues.IsEnabled = false;
            this._host.UIMessage.Text = "Click on the nodes to add edges";
            this._host.UIMessage.Visibility = Visibility.Visible;
            this._host.Cursor = Cursors.Pen;
            this._host.CommandReset.Click += endAddingEdges_Click;
        }
        private void endAddingEdges_Click(object sender, RoutedEventArgs e)
        {
            this._host.FloorScene.MouseMove -= DrawLine;
            Node.SelectForEdge = null;
            this._host.Menues.IsEnabled = true;
            this._host.UIMessage.Visibility = Visibility.Hidden;
            this._host.CommandReset.Click -= endAddingEdges_Click;
            node1 = null;
            node2 = null;
            this._host.Cursor = Cursors.Arrow;
        }

        private bool selectNodes(Line nodeMark)
        {
            Node n1 = null;
            Node.LineToUVGuide.TryGetValue(nodeMark, out n1);
            if (n1 != null)
            {
                if (node1 == null)
                {
                    node1 = n1;
                    node1.Mark.Stroke = Node.WhenSelected;
                }
                else
                {
                    node2 = n1;
                    node2.Mark.Stroke = Node.WhenSelected;
                }
                if (node1 != null && node2 != null)
                {
                    if (node1.GetHashCode() == node2.GetHashCode())
                    {
                        node1.Mark.Stroke = Node.Color;
                        node2.Mark.Stroke = Node.Color;
                        node1 = null;
                        node2 = null;
                    }
                }
            }

            if (node1 != null && node2 != null)
            {
                JGEdge newEdge = new JGEdge(node1.Coordinates, node2.Coordinates);
                newEdge.Draw();
                this.edges.Add(newEdge);
                node1.Mark.Stroke = Node.Color;
                node2.Mark.Stroke = Node.Color;
                node1 = null;
                node2 = null;
            }
            return true;
        }

        #endregion

        #region Remove edge

        private void endRemovingEdges_Click(object sender, RoutedEventArgs e)
        {
            edgeRemoveMode = false;
            this._host.Menues.IsEnabled = true;
            this._host.UIMessage.Visibility = Visibility.Hidden;
            this._host.CommandReset.Click -= endRemovingEdges_Click;
            var removedEdges = new HashSet<JGEdge>();
            foreach (JGEdge edge in this.edges)
            {
                if (edge.EdgeLine.Visibility == Visibility.Hidden)
                {
                    removedEdges.Add(edge);
                }
            }
            foreach (JGEdge edge in removedEdges)
            {
                this.edges.Remove(edge);
                edge.Clear();
            }
            removedEdges = null;
            JGEdge.SelectToRemove = null;
        }
        private void RemoveEdge_Click(object sender, RoutedEventArgs e)
        {
            JGEdge.SelectToRemove = markEdgeAsRemove;
            edgeRemoveMode = true;
            this._host.Menues.IsEnabled = false;
            this._host.UIMessage.Text = "Click on an connection to remove it";
            this._host.UIMessage.Visibility = Visibility.Visible;
            this._host.CommandReset.Click += endRemovingEdges_Click;
        }
        private bool markEdgeAsRemove(Line line)
        {
            if (edgeRemoveMode)
            {
                line.Visibility = System.Windows.Visibility.Hidden;
            }
            return true;
        }
        #endregion

        #region Move Vertex space
        bool moveMode = true;
        private void MoveVertex_Click(object sender, RoutedEventArgs e)
        {
            this.node1 = null;
            this._host.Menues.IsEnabled = false;
            this._host.UIMessage.Text = "Click on a node to move it";
            this._host.UIMessage.Visibility = System.Windows.Visibility.Visible;
            this._host.CommandReset.Click += endMovingConvexSpace_Click;
            
            foreach (Node item in this.JGNodes)
            {
                item.Mark.MouseLeftButtonDown += Mark_MouseLeftButtonDown2;
            }
            moveMode = true;
        }
        private void endMovingConvexSpace_Click(object sender, RoutedEventArgs e)
        {
            this._host.Menues.IsEnabled = true;
            this._host.UIMessage.Visibility = System.Windows.Visibility.Hidden;
            this._host.CommandReset.Click -= endMovingConvexSpace_Click;
            foreach (Node item in this.JGNodes)
            {
                item.Mark.MouseLeftButtonDown -= Mark_MouseLeftButtonDown2;
            }
            node1 = null;

        }
        private void Mark_MouseLeftButtonDown2(object sender, MouseButtonEventArgs e)
        {
            if (this.moveMode)
            {
                Line line = (Line)sender;
                Node.LineToUVGuide.TryGetValue(line, out node1);
                if (node1 != null)
                {
                    this.relatedEdges = this.nodeToEdge(line);
                    this._host.FloorScene.MouseMove += FloorScene_MouseMove2;
                }
                this.moveMode = false;
                return;
            }
            else
            {
                this._host.FloorScene.MouseMove -= FloorScene_MouseMove2;
                node1 = null;
                this.moveMode = true;

                return;
            }

        }
        private void FloorScene_MouseMove2(object sender, MouseEventArgs e)
        {
            if (node1 != null)
            {
                Point p = Mouse.GetPosition(this._host.FloorScene);
                UV uvP = this._host.TransformInverse(p);
                foreach (JGEdge item in relatedEdges)
                {
                    if (item.P1 == node1.Coordinates)
                    {
                        item.P1 = uvP;
                    }
                    if (item.P2 == node1.Coordinates)
                    {
                        item.P2 = uvP;
                    }
                    item.Draw();
                }
                node1.Coordinates = uvP;
                node1.Draw();
                node1.Mark.MouseLeftButtonDown += Mark_MouseLeftButtonDown2;
            }

        }
        private HashSet<JGEdge> nodeToEdge(Line nodeMark)
        {
            Node node = null;
            if (!Node.LineToUVGuide.TryGetValue(nodeMark, out node))
            {
                return null;
            }
            HashSet<JGEdge> e = new HashSet<JGEdge>();
            foreach (JGEdge item in this.edges)
            {
                if (item.P1 == node.Coordinates || item.P2 == node.Coordinates)
                {
                    e.Add(item);
                }
            }
            if (e.Count == 0)
            {
                return null;
            }
            return e;
        }
        #endregion

        #region Creating and drawing the JGraph
        
        private void DrawJG_Click(object sender, RoutedEventArgs e)
        {
            CreateJGraph();
            this._host.Menues.IsEnabled = false;
            this._host.UIMessage.Text = "Pick a node in a convex space ";
            this._host.UIMessage.Visibility = Visibility.Visible;
            foreach (Node node in this.JGNodes)
            {
                node.Mark.MouseLeftButtonDown += Mark_MouseLeftButtonDown;
            }

        }

        private void Mark_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this._host.Menues.IsEnabled = true;
            this._host.UIMessage.Visibility = Visibility.Hidden;
            foreach (Node node in this.JGNodes)
            {
                node.Mark.MouseLeftButtonDown -= Mark_MouseLeftButtonDown;
            }
            Node n = null;
            Node.LineToUVGuide.TryGetValue((Line)sender, out n);
            if (n != null)
            {
                foreach (JGVertex item in jgGraph.Vertices)
                {
                    if (item.Point == n.Coordinates)
                    {
                        root = item;
                        break;
                    }
                }
            }
            if (root == null)
            {
                MessageBox.Show("Vertex not tracked");
                return;
            }
            n.Mark.Stroke = Brushes.DarkRed;
            #region create hierarchy
            HashSet<JGVertex> unExplored = new HashSet<JGVertex>();
            JGHierarchy = new List<HashSet<JGVertex>>();
            foreach (JGVertex item in jgGraph.Vertices)
            {
                if (item.Point != root.Point)
                {
                    unExplored.Add(item);
                }
            }
            HashSet<JGVertex> firstLevel = new HashSet<JGVertex>();
            firstLevel.Add(root);
            JGHierarchy.Add(firstLevel);
            HashSet<JGVertex> newLevel = new HashSet<JGVertex>();
            HashSet<JGVertex> level = new HashSet<JGVertex>();
            level.Add(root);
            while (unExplored.Count != 0)
            {
                foreach (JGVertex item in level)
                {
                    foreach (JGVertex vertex in item.Connections)
                    {
                        if (unExplored.Contains(vertex))
                        {
                            newLevel.Add(vertex);
                        }
                    }
                }
                foreach (JGVertex vertex in newLevel)
                {
                    unExplored.Remove(vertex);
                }
                if (newLevel.Count == 0 && unExplored.Count != 0)
                {
                    MessageBox.Show("Justified Graph is not a linked graph!\nTry adding connections or removing unconnected nodes...");
                    return;
                }
                level.Clear();
                level.UnionWith(newLevel);
                HashSet<JGVertex> thisLevel = new HashSet<JGVertex>();
                foreach (JGVertex item in level)
                {
                    thisLevel.Add(item);
                }
                JGHierarchy.Add(thisLevel);
                newLevel.Clear();
            }

            #endregion
            drawGraph();
        }

        /// <summary>
        /// Draws the graph.
        /// </summary>
        public void drawGraph()
        {
            var graph = this.jgGraph.DeepCopy();
            List<HashSet<JGVertex>> hierarchy = new List<HashSet<JGVertex>>();
            for (int i = 0; i < JGHierarchy.Count; i++)
            {
                HashSet<JGVertex> newLevel = new HashSet<JGVertex>();
                foreach (JGVertex item in JGHierarchy[i])
                {
                    newLevel.Add(graph.Find(item.Point));
                }
                hierarchy.Add(newLevel);
            }
            var visualizer = new JustifiedGraphVisualHost()
            {
                Width = this._host.Width,
                Height = this._host.Height
            };
            visualizer.JGVisualizer.SetGraph(graph, hierarchy);
            visualizer.Owner = this._host;
            visualizer.ShowDialog();
            visualizer = null;
            foreach (Node item in JGNodes)
            {
                item.Mark.Stroke = Node.Color;
            }
        }

        /// <summary>
        /// Creates the justified graph using a standard BFS algorithm.
        /// </summary>
        public void CreateJGraph()
        {
            #region setting the Nodes
            HashSet<UV> vertexPoints = new HashSet<UV>();
            foreach (Node node in this.JGNodes)
            {
                node.Clear();
            }
            Node.LineToUVGuide.Clear();
            this.JGNodes.Clear();
            foreach (JGEdge edge in this.edges)
            {
                this.JGNodes.Add(new Node(edge.P1));
                this.JGNodes.Add(new Node(edge.P2));
            }
            foreach (Node node in this.JGNodes)
            {
                vertexPoints.Add(node.Coordinates);
                node.Draw();
            }
            #endregion

            this.jgGraph = new JGGraph(vertexPoints);
            foreach (JGEdge edge in this.edges)
            {
                this.jgGraph.AddConnection(edge);
            }

        }

        #endregion

        /// <summary>
        /// Sets the host.
        /// </summary>
        /// <param name="host">The main document to which this control belongs.</param>
        public void SetHost(OSMDocument host)
        {
            this._host = host;
            this._host.Menues.Items.Add(this.JustifiedGraphMenu);

            Node.FloorScene = this;
            Node.Transform = this._host.Transform;
            JGEdge.FloorScene = this;
            JGEdge.Transform = this._host.Transform;

        }
    }


}

