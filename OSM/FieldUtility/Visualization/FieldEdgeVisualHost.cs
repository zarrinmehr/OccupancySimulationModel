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
using SpatialAnalysis.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace SpatialAnalysis.FieldUtility.Visualization
{
    /// <summary>
    /// Class FieldEdgeVisualHost.
    /// </summary>
    /// <seealso cref="System.Windows.FrameworkElement" />
    public class FieldEdgeVisualHost : FrameworkElement
    {
        private Brush _brush { get; set; }
        private OSMDocument _host { get; set; }
        private VisualCollection _children;
        // Provide a required override for the VisualChildrenCount property. 
        protected override int VisualChildrenCount
        {
            get { return _children.Count; }
        }

        // Provide a required override for the GetVisualChild method. 
        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index >= _children.Count)
            {
                throw new ArgumentOutOfRangeException();
            }

            return _children[index];
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="FieldEdgeVisualHost"/> class.
        /// </summary>
        public FieldEdgeVisualHost()
        {
            this._children = new VisualCollection(this);
            this._brush = Brushes.LightSalmon.Clone();
            this._brush.Opacity = 0.50d;
        }
        /// <summary>
        /// Sets the host.
        /// </summary>
        /// <param name="host">The main document to which this control belongs.</param>
        public void SetHost(OSMDocument host)
        {
            this._host = host;
            this.RenderTransform = this._host.RenderTransformation;
        }
        private Point toPoint(UV uv)
        {
            return new Point(uv.U, uv.V);
        }
        /// <summary>
        /// Highlights the walkable field area 
        /// </summary>
        public void Highlight()
        {
            var edges = CellularEnvironment.CellUtility.GetFieldBoundary(this._host.cellularFloor);
            StreamGeometry geom = new StreamGeometry();
            using (var gc = geom.Open())
            {
                foreach (var item in edges)
                {
                    gc.BeginFigure(toPoint(item.BoundaryPoints[0]), true, true);
                    for (int i = 0; i < item.Length; i++)
                    {
                        gc.LineTo(toPoint(item.BoundaryPoints[i]), false, false);
                    }
                }
            }
            DrawingVisual drawing = new DrawingVisual();
            using (var dc = drawing.RenderOpen())
            {
                dc.DrawGeometry(this._brush, null, geom);
            }
            if (drawing.Drawing.CanFreeze) drawing.Drawing.Freeze();
            this._children.Add(drawing);
        }
        /// <summary>
        /// Clears the walkable field area
        /// </summary>
        public void Clear()
        {
            this._children.Clear();
        }

    }
}

