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
using SpatialAnalysis.FieldUtility;
using SpatialAnalysis.Visualization;
using System.Collections.Generic;
using System;
using SpatialAnalysis.Geometry;

namespace SpatialAnalysis.JustifiedGraph
{
    /// <summary>
    /// Documentation is under development.
    /// </summary>
    public class Node
    {
        /// <summary>
        /// Gets or sets the inverse transformation of the scene.
        /// </summary>
        /// <value>The inverse transform.</value>
        public static Func<Point, UV> InverseTransform { get; set; }
        /// <summary>
        /// The color brush for selection
        /// </summary>
        public static Brush WhenSelected = Brushes.DarkRed;
        /// <summary>
        /// The select for edge
        /// </summary>
        public static Func<Line, bool> SelectForEdge;
        /// <summary>
        /// The select to remove
        /// </summary>
        public static Func<Line, bool> SelectToRemove;
        /// <summary>
        /// The line to uv map
        /// </summary>
        public static Dictionary<Line, Node> LineToUVGuide = new Dictionary<Line, Node>();
        /// <summary>
        /// The floor scene
        /// </summary>
        public static Canvas FloorScene;
        /// <summary>
        /// The transform
        /// </summary>
        public static Func<UV, Point> Transform;
        /// <summary>
        /// Gets or sets the coordinates.
        /// </summary>
        /// <value>The coordinates.</value>
        public UV Coordinates { get; set; }
        /// <summary>
        /// Gets or sets the mark.
        /// </summary>
        /// <value>The mark.</value>
        public Line Mark { get; set; }
        /// <summary>
        /// The size
        /// </summary>
        public static double Size = 5;
        /// <summary>
        /// The color
        /// </summary>
        public static Brush Color = Brushes.Pink;
        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            if (this.Mark != null)
            {
                if (LineToUVGuide.ContainsKey(this.Mark))
                {
                    LineToUVGuide.Remove(this.Mark);
                }
                int index = FloorScene.Children.IndexOf(this.Mark);
                if (index != -1)
                {
                    FloorScene.Children.RemoveAt(index);
                }
                this.Mark = null;
            }
        }
        public void Draw()
        {
            this.Clear();
            Point p = Transform(this.Coordinates);
            this.Mark = new Line()
            {
                X1 = p.X - Size / 2,
                X2 = p.X + Size / 2,
                Y1 = p.Y,
                Y2 = p.Y,
                Stroke = Color,
                StrokeThickness = Size,
            };
            Canvas.SetZIndex(this.Mark, 5);
            FloorScene.Children.Add(this.Mark);
            LineToUVGuide.Add(this.Mark, this);
            this.Mark.MouseLeftButtonDown += Mark_MouseLeftButtonDown;
        }

        void Mark_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Node.SelectToRemove != null)
            {
                Node.SelectToRemove((Line)sender);
            }
            if (Node.SelectForEdge != null)
            {
                Node.SelectForEdge((Line)sender);
            }

        }
        public Node(UV point)
        {
            this.Coordinates = point;
        }
    }

    /// <summary>
    /// Class JGVertex.
    /// </summary>
    internal class JGVertex
    {
        /// <summary>
        /// Gets the link count.
        /// </summary>
        /// <value>The link count.</value>
        public int LinkCount
        {
            get { return Connections.Count; }
        }
        /// <summary>
        /// Gets or sets the point.
        /// </summary>
        /// <value>The point.</value>
        public UV Point { get; set; }
        /// <summary>
        /// Gets or sets the connections.
        /// </summary>
        /// <value>The connections.</value>
        public HashSet<JGVertex> Connections { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="JGVertex"/> class.
        /// </summary>
        /// <param name="point">The point.</param>
        public JGVertex(UV point)
        {
            this.Point = point;
            this.Connections = new HashSet<JGVertex>();
        }
        /// <summary>
        /// Adds to connections.
        /// </summary>
        /// <param name="newVertex">The new vertex.</param>
        public void AddToConnections(JGVertex newVertex)
        {
            this.Connections.Add(newVertex);
        }
        public override int GetHashCode()
        {
            return this.Point.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            JGVertex v = obj as JGVertex;
            if (v == null)
            {
                return false;
            }
            return this.Point == v.Point;
        }
    }

    /// <summary>
    /// Class JGEdge.
    /// </summary>
    class JGEdge
    {
        /// <summary>
        /// Select to remove
        /// </summary>
        public static Func<Line, bool> SelectToRemove;
        /// <summary>
        /// The line to edge map
        /// </summary>
        public static Dictionary<Line, JGEdge> LineToEdgeGuide = new Dictionary<Line, JGEdge>();
        /// <summary>
        /// The floor scene
        /// </summary>
        public static Canvas FloorScene;
        /// <summary>
        /// The transform
        /// </summary>
        public static Func<UV, Point> Transform;
        /// <summary>
        /// The thickness
        /// </summary>
        public static double Thickness = 1;
        /// <summary>
        /// The color
        /// </summary>
        public static Brush Color = Brushes.Green;
        /// <summary>
        /// Gets or sets the edge line.
        /// </summary>
        /// <value>The edge line.</value>
        public Line EdgeLine { get; set; }
        /// <summary>
        /// The p1
        /// </summary>
        public UV P1;
        /// <summary>
        /// The p2
        /// </summary>
        public UV P2;
        /// <summary>
        /// Initializes a new instance of the <see cref="JGEdge"/> class.
        /// </summary>
        /// <param name="p1">The p1.</param>
        /// <param name="p2">The p2.</param>
        public JGEdge(UV p1, UV p2)
        {
            this.P1 = p1;
            this.P2 = p2;
        }

        void EdgeLine_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (JGEdge.SelectToRemove != null)
            {
                SelectToRemove((Line)sender);
            }

        }
        public override int GetHashCode()
        {
            UV p = null;
            if (P1.GetLength() > P2.GetLength())
            {
                p = P1;
            }
            else
            {
                p = P2;
            }
            return p.GetHashCode();
        }
        public override string ToString()
        {
            string s = string.Empty;
            s = "P1:\n";
            s += P1.ToString() + "\n";
            s += "P2:\n";
            s += P2.ToString();
            return s;
        }
        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            if (EdgeLine != null)
            {
                //clear the guide
                if (LineToEdgeGuide.ContainsKey(EdgeLine))
                {
                    LineToEdgeGuide.Remove(EdgeLine);
                }
                //clearing the canvas
                int index = FloorScene.Children.IndexOf(EdgeLine);
                if (index != -1)
                {
                    FloorScene.Children.RemoveAt(index);
                }
                //claring the value
                EdgeLine = null;
            }
        }
        /// <summary>
        /// Draws this instance.
        /// </summary>
        public void Draw()
        {
            this.Clear();
            Point pnts1 = Transform(P1);
            Point pnts2 = Transform(P2);
            this.EdgeLine = new Line()
            {
                X1 = pnts1.X,
                X2 = pnts2.X,
                Y1 = pnts1.Y,
                Y2 = pnts2.Y,
                Stroke = Color,
                StrokeThickness = Thickness,
            };
            Canvas.SetZIndex(this.EdgeLine, 4);
            FloorScene.Children.Add(this.EdgeLine);
            LineToEdgeGuide.Add(this.EdgeLine, this);
            this.EdgeLine.MouseLeftButtonDown += new MouseButtonEventHandler(EdgeLine_MouseLeftButtonDown);
        }
    }

    /// <summary>
    /// Class JGGraph.
    /// </summary>
    class JGGraph
    {
        /// <summary>
        /// Gets the link count.
        /// </summary>
        /// <value>The link count.</value>
        public int LinkCount
        {
            get { return Vertices.Count; }
        }
        /// <summary>
        /// Gets or sets the vertices.
        /// </summary>
        /// <value>The vertices.</value>
        public HashSet<JGVertex> Vertices { get; set; }
        Dictionary<int, JGVertex> dict { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="JGGraph"/> class.
        /// </summary>
        public JGGraph()
        {
            Vertices = new HashSet<JGVertex>();
            dict = new Dictionary<int, JGVertex>();
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="JGGraph"/> class.
        /// </summary>
        /// <param name="Vertexes">The vertexes.</param>
        public JGGraph(HashSet<UV> Vertexes)
        {
            Vertices = new HashSet<JGVertex>();
            dict = new Dictionary<int, JGVertex>();
            foreach (UV item in Vertexes)
            {
                JGVertex v = new JGVertex(item);
                this.Vertices.Add(v);
                this.dict.Add(item.GetHashCode(), v);
            }
        }
        /// <summary>
        /// Finds the specified point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>JGVertex.</returns>
        public JGVertex Find(UV point)
        {
            JGVertex vertex = null;
            this.dict.TryGetValue(point.GetHashCode(), out vertex);
            return vertex;
        }
        /// <summary>
        /// Creates a deep copy of this instance.
        /// </summary>
        /// <returns>JGGraph.</returns>
        public JGGraph DeepCopy()
        {
            JGGraph copied = new JGGraph();
            //copying vertices
            foreach (JGVertex item in this.Vertices)
            {
                JGVertex newJGV = new JGVertex(item.Point.Copy());
                copied.AddVertex(newJGV);
            }
            //copying connections
            foreach (JGVertex item in this.Vertices)
            {
                JGVertex current = copied.Find(item.Point);
                foreach (JGVertex connection in item.Connections)
                {
                    JGVertex next = copied.Find(connection.Point);
                    current.AddToConnections(next);
                    next.AddToConnections(current);
                }
            }
            return copied;
        }
        /// <summary>
        /// Gets the edge lines.
        /// </summary>
        /// <returns>List&lt;UVLine&gt;.</returns>
        public List<UVLine> ToLines()
        {
            HashSet<JGVertex> counted = new HashSet<JGVertex>();
            List<UVLine> lines = new List<UVLine>();
            foreach (JGVertex item1 in this.Vertices)
            {
                foreach (JGVertex item2 in item1.Connections)
                {
                    if (!counted.Contains(item2))
                    {
                        lines.Add(new UVLine(item1.Point, item2.Point));
                    }
                }
                counted.Add(item1);
            }
            return lines;
        }

        /// <summary>
        /// Copies this instance.
        /// </summary>
        /// <returns>JGGraph.</returns>
        public JGGraph Copy()
        {
            var graph = new JGGraph();
            foreach (JGVertex item in this.Vertices)
            {
                graph.AddVertex(item);
            }
            return graph;
        }
        /// <summary>
        /// Adds a vertex.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        public void AddVertex(JGVertex vertex)
        {
            dict.Add(vertex.GetHashCode(), vertex);
            this.Vertices.Add(vertex);
        }
        /// <summary>
        /// Adds the connection between two vertices.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        public void AddConnection(JGVertex a, JGVertex b)
        {
            JGVertex A = null;
            JGVertex B = null;
            dict.TryGetValue(a.GetHashCode(), out A);
            dict.TryGetValue(b.GetHashCode(), out B);
            if (A != null && B != null)
            {
                A.AddToConnections(B);
                B.AddToConnections(A);
            }

        }
        /// <summary>
        /// Adds the connection.
        /// </summary>
        /// <param name="edge">The edge.</param>
        public void AddConnection(JGEdge edge)
        {
            JGVertex vertex1 = null;
            dict.TryGetValue(edge.P1.GetHashCode(), out vertex1);
            JGVertex vertex2 = null;
            dict.TryGetValue(edge.P2.GetHashCode(), out vertex2);
            if (vertex1 != null && vertex2 != null)
            {
                vertex1.Connections.Add(vertex2);
                vertex2.Connections.Add(vertex1);
            }
        }
        /// <summary>
        /// Adds the connection.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        public void AddConnection(UV a, UV b)
        {
            JGVertex A = null;
            JGVertex B = null;
            dict.TryGetValue(a.GetHashCode(), out A);
            dict.TryGetValue(b.GetHashCode(), out B);
            if (A != null && B != null)
            {
                A.AddToConnections(B);
                B.AddToConnections(A);
            }
        }
        /// <summary>
        /// Converts the graph to a list of edges.
        /// </summary>
        /// <returns>List&lt;JGEdge&gt;.</returns>
        public List<JGEdge> ToEdges()
        {
            List<JGEdge> edges = new List<JGEdge>();
            JGGraph g = this.Copy();
            while (g.Vertices.Count != 0)
            {
                JGVertex v = g.Vertices.FirstOrDefault<JGVertex>();
                foreach (JGVertex item in v.Connections)
                {
                    if (g.Vertices.Contains(item))
                    {
                        edges.Add(new JGEdge(item.Point, v.Point));
                    }
                }
                g.Vertices.Remove(v);
            }
            g = null;

            return edges;
        }
        /// <summary>
        /// Converts the graph to a list of nodes.
        /// </summary>
        /// <returns>HashSet&lt;Node&gt;.</returns>
        public HashSet<Node> ToNodes()
        {
            HashSet<Node> nodes = new HashSet<Node>();
            foreach (JGVertex item in this.Vertices)
            {
                nodes.Add(new Node(item.Point));
            }
            return nodes;
        }



    }
}

