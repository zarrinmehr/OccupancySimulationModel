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
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Input;
using SpatialAnalysis.FieldUtility;
using SpatialAnalysis.CellularEnvironment;
using SpatialAnalysis.Geometry;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using SpatialAnalysis.Visualization;
using SpatialAnalysis.Events;
using SpatialAnalysis.IsovistUtility;
using SpatialAnalysis.Miscellaneous;
using SpatialAnalysis.Interoperability;

namespace SpatialAnalysis.Agents.MandatoryScenario.Visualization
{
    /// <summary>
    /// Class Activity Area Visual Host.
    /// </summary>
    /// <seealso cref="System.Windows.FrameworkElement" />
    public class ActivityAreaVisualHost : FrameworkElement
    {
        private static Color[] AreaColors = new Color[2] { Colors.Pink, Colors.DarkBlue };
        /// <summary>
        /// Gets the color through interpolation.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <param name="max">The maximum.</param>
        /// <param name="alpha">The alpha.</param>
        /// <returns>Color.</returns>
        public static Color GetColor(int count, int max, byte alpha)
        {
            float w1 = ((float)count) / max;
            Color color = Color.Add(Color.Multiply(AreaColors[0], w1), Color.Multiply(AreaColors[1], 1.0f - w1));
            color.A = alpha;
            return color;
        }
        private OSMDocument _host { get; set; }
        private VisualCollection _children { get; set; }
        private Brush _wheat { get; set; }
        private Brush _tomato { get; set; }
        private Pen _pen { get; set; }
        /// <summary>
        /// Gets or sets the thickness of curves.
        /// </summary>
        /// <value>The thickness.</value>
        public double Thickness { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityAreaVisualHost"/> class.
        /// </summary>
        public ActivityAreaVisualHost()
        {
            this._children = new VisualCollection(this);
            this._tomato = Brushes.Tomato.Clone();
            this._tomato.Opacity = 0.75d;
            this._wheat = Brushes.Wheat.Clone();
            this._wheat.Opacity = 0.75d;
            this.Thickness = 0.20d;
            Brush salmon = Brushes.Salmon.Clone();
            salmon.Opacity = 0.9d;
            this._pen = new Pen(salmon, Thickness);
        }
        /// <summary>
        /// Draws the sequence with force trajectory.
        /// </summary>
        /// <param name="sequence">The sequence.</param>
        /// <param name="applyColorCode">if set to <c>true</c> color code will be applied.</param>
        /// <param name="stepSize">Size of the step.</param>
        public void DrawSequenceWithForceTrajectory(Sequence sequence, bool applyColorCode, double stepSize = 0.3d)
        {
            this.Clear();
            this.drawSequenceActivities(sequence);
            if (sequence.ActivityNames.Count>1)
            {
                List<UV> paths = new List<UV>();
                for (int i = 1; i < sequence.ActivityNames.Count; i++)
                {
                    var origin = this._host.AllActivities[sequence.ActivityNames[i - 1]].DefaultState.Location;
                    var path = this._host.AllActivities[sequence.ActivityNames[i]].GetGradientPath(origin, stepSize,10000);
                    paths.AddRange(path);
                }
                PathGeometry pg = new PathGeometry();
                StreamGeometry sg2 = new StreamGeometry();
                using (var sgc = sg2.Open())
                {
                    Point[] pnts = new Point[paths.Count];
                    for (int i = 0; i < paths.Count; i++) pnts[i] = this.toPoint(paths[i]);
                    sgc.BeginFigure(pnts[0], false, false);
                    sgc.PolyLineTo(pnts, true, true);
                }
                DrawingVisual drawingVisual2 = new DrawingVisual();
                using (var context2 = drawingVisual2.RenderOpen())
                {
                    context2.DrawGeometry(null, this._pen, sg2);
                }
                drawingVisual2.Drawing.Freeze();
                this._children.Add(drawingVisual2);
                paths.Clear();
                paths = null;
            }
            if (sequence.HasVisualAwarenessField)
            {
                this.VisualizeVisibilityArea(sequence.VisualAwarenessField, applyColorCode);
            }
        }


        /// <summary>
        /// Draws the sequence with straight lines.
        /// </summary>
        /// <param name="sequence">The sequence.</param>
        /// <param name="applyColorCode">if set to <c>true</c> [apply color code].</param>
        public void DrawSequenceWithStraightLines(Sequence sequence, bool applyColorCode)
        {
            this.Clear();
            this.drawSequenceActivities(sequence);
            if (sequence.ActivityNames.Count > 1)
            {
                PathGeometry pg = new PathGeometry();
                StreamGeometry sg2 = new StreamGeometry();
                using (var sgc = sg2.Open())
                {
                    Point[] pnts = new Point[sequence.ActivityNames.Count];
                    for (int i = 0; i < pnts.Length; i++) pnts[i] = this.toPoint(this._host.AllActivities[sequence.ActivityNames[i]].DefaultState.Location);
                    sgc.BeginFigure(pnts[0], false, false);
                    sgc.PolyLineTo(pnts, true, true);
                }
                DrawingVisual drawingVisual2 = new DrawingVisual();
                using (var context2 = drawingVisual2.RenderOpen())
                {
                    context2.DrawGeometry(null, this._pen, sg2);
                }
                drawingVisual2.Drawing.Freeze();
                this._children.Add(drawingVisual2);
            }
            if (sequence.HasVisualAwarenessField)
            {
                this.VisualizeVisibilityArea(sequence.VisualAwarenessField, applyColorCode);
            }
        }

        /// <summary>
        /// Draws the activity.
        /// </summary>
        /// <param name="activity">The activity.</param>
        public void DrawActivity(Activity activity)
        {
            this.Clear();
            StreamGeometry sg = new StreamGeometry();
            using (var sgc = sg.Open())
            {
                Point[] pnts = new Point[activity.DestinationArea.Length];
                for (int i = 0; i < pnts.Length; i++) pnts[i] = this.toPoint(activity.DestinationArea.BoundaryPoints[i]);
                sgc.BeginFigure(pnts[0], true, true);
                sgc.PolyLineTo(pnts, false, false);
            }
            DrawingVisual drawingVisual = new DrawingVisual();
            using (var context = drawingVisual.RenderOpen())
            {
                if (!this._host.AgentMandatoryScenario.MainStations.Contains(activity.Name))
                {
                    context.DrawGeometry(this._wheat, null, sg);
                }
                else
                {
                    context.DrawGeometry(this._tomato, null, sg);
                }
            }
            drawingVisual.Drawing.Freeze();
            this._children.Add(drawingVisual);
        }

        /// <summary>
        /// Draws all activities.
        /// </summary>
        public void DrawAllActivities()
        {
            this.Clear();
            StreamGeometry sg1 = new StreamGeometry();
            using (var sgc = sg1.Open())
            {
                foreach (var activity in this._host.AllActivities.Values)
                {
                    if (!this._host.AgentMandatoryScenario.MainStations.Contains(activity.Name))
                    {
                        Point[] pnts = new Point[activity.DestinationArea.Length];
                        for (int i = 0; i < pnts.Length; i++) pnts[i] = this.toPoint(activity.DestinationArea.BoundaryPoints[i]);
                        sgc.BeginFigure(pnts[0], true, true);
                        sgc.PolyLineTo(pnts, false, false);
                    }
                }
            }
            DrawingVisual drawingVisual1 = new DrawingVisual();
            using (var context = drawingVisual1.RenderOpen())
            {
                context.DrawGeometry(this._wheat, null, sg1);
            }
            drawingVisual1.Drawing.Freeze();
            this._children.Add(drawingVisual1);
            StreamGeometry sg2 = new StreamGeometry();
            using (var sgc = sg2.Open())
            {
                foreach (var activity in this._host.AllActivities.Values)
                {
                    if (this._host.AgentMandatoryScenario.MainStations.Contains(activity.Name))
                    {
                        Point[] pnts = new Point[activity.DestinationArea.Length];
                        for (int i = 0; i < pnts.Length; i++) pnts[i] = this.toPoint(activity.DestinationArea.BoundaryPoints[i]);
                        sgc.BeginFigure(pnts[0], true, true);
                        sgc.PolyLineTo(pnts, false, false);
                    }
                }
            }
            DrawingVisual drawingVisual2 = new DrawingVisual();
            using (var context = drawingVisual2.RenderOpen())
            {
                context.DrawGeometry(this._tomato, null, sg2);
            }
            drawingVisual2.Drawing.Freeze();
            this._children.Add(drawingVisual2);
        }

        /// <summary>
        /// required override for the VisualChildrenCount property.
        /// </summary> 
        protected override int VisualChildrenCount
        {
            get { return _children.Count; }
        }
        /// <summary>
        /// Overrides <see cref="M:System.Windows.Media.Visual.GetVisualChild(System.Int32)" />, and returns a child at the specified index from a collection of child elements.
        /// </summary>
        /// <param name="index">The zero-based index of the requested child element in the collection.</param>
        /// <returns>The requested child element. This should not return null; if the provided index is out of range, an exception is thrown.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index >= _children.Count)
            {
                throw new ArgumentOutOfRangeException();
            }
            return _children[index];
        }

        /// <summary>
        /// Clears the scene.
        /// </summary>
        public void Clear()
        {
            this._children.Clear();
            this._host.activityVisiblArea.Source = null;
        }

        private Point toPoint(UV uv)
        {
            return new Point(uv.U, uv.V);
        }
        /// <summary>
        /// Exports the scene to PNG format
        /// </summary>
        public void Save()
        {
            double dpi = 96;
            GetNumber getNumber0 = new GetNumber("Set Image Resolution",
                "The background drawing will be exported in PGN format.", dpi);

            getNumber0.Owner = this._host;
            getNumber0.ShowDialog();
            dpi = getNumber0.NumberValue;
            getNumber0 = null;
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Title = "Save the Scene to PNG Image format";
            dlg.DefaultExt = ".png";
            dlg.Filter = "PNG documents (.png)|*.png";
            Nullable<bool> result = dlg.ShowDialog(this._host);
            string fileAddress = "";
            if (result == true)
            {
                fileAddress = dlg.FileName;
            }
            else
            {
                return;
            }
            Rect bounds = VisualTreeHelper.GetDescendantBounds(this._host.ViewHolder);
            RenderTargetBitmap main_rtb = new RenderTargetBitmap((int)(bounds.Width * dpi / 96), (int)(bounds.Height * dpi / 96), dpi, dpi, System.Windows.Media.PixelFormats.Default);
            DrawingVisual dvFloorScene = new DrawingVisual();
            using (DrawingContext dc = dvFloorScene.RenderOpen())
            {
                VisualBrush vb = new VisualBrush(this._host.ViewHolder);
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

        /// <summary>
        /// Sets the document to which this scene belongs to
        /// </summary>
        /// <param name="host">The host.</param>
        public void SetHost(OSMDocument host)
        {
            this._host = host;
            this.RenderTransform = this._host.RenderTransformation;
            this.Thickness = this._host.UnitConvertor.Convert(0.20d);
            this._pen.Thickness = this.Thickness;
        }
        private void drawSequenceActivities(Sequence sequence)
        {
            StreamGeometry sg1 = new StreamGeometry();
            using (var sgc = sg1.Open())
            {
                var activity = this._host.AllActivities[sequence.ActivityNames[0]];
                Point[] pnts = new Point[activity.DestinationArea.Length];
                for (int i = 0; i < pnts.Length; i++) pnts[i] = this.toPoint(activity.DestinationArea.BoundaryPoints[i]);
                sgc.BeginFigure(pnts[0], true, true);
                sgc.PolyLineTo(pnts, false, false);
            }
            DrawingVisual drawingVisual1 = new DrawingVisual();
            using (var context = drawingVisual1.RenderOpen())
            {
                context.DrawGeometry(this._tomato, null, sg1);
            }
            drawingVisual1.Drawing.Freeze();
            this._children.Add(drawingVisual1);
            if (sequence.ActivityNames.Count > 1)
            {
                StreamGeometry sg = new StreamGeometry();
                using (var sgc = sg.Open())
                {
                    for (int j = 1; j < sequence.ActivityNames.Count; j++)
                    {
                        var activity = this._host.AllActivities[sequence.ActivityNames[j]];
                        Point[] pnts = new Point[activity.DestinationArea.Length];
                        for (int i = 0; i < pnts.Length; i++) pnts[i] = this.toPoint(activity.DestinationArea.BoundaryPoints[i]);
                        sgc.BeginFigure(pnts[0], true, true);
                        sgc.PolyLineTo(pnts, false, false);
                    }
                }
                DrawingVisual drawingVisual = new DrawingVisual();
                using (var context = drawingVisual.RenderOpen())
                {
                    context.DrawGeometry(this._wheat, null, sg);
                }
                drawingVisual.Drawing.Freeze();
                this._children.Add(drawingVisual);
            }
        }

        private void VisualizeVisibilityArea(VisibilityTarget occupancyVisualEvent, bool applyColorCode)
        {
            double _h = ((UIElement)this.Parent).RenderSize.Height;
            double _w = ((UIElement)this.Parent).RenderSize.Width;
            this._host.activityVisiblArea.Source = null;
            WriteableBitmap _view = BitmapFactory.New((int)_w, (int)_h);
            this._host.activityVisiblArea.Source = _view;
            this._host.activityVisiblArea.Visibility = System.Windows.Visibility.Visible;
            if (applyColorCode)
            {
                using (_view.GetBitmapContext())
                {
                    int max = int.MinValue;
                    foreach (var item in occupancyVisualEvent.ReferencedVantageCells.Values)
                    {
                        if (max < item.Count) max = item.Count;
                    }
                    byte alpha = (byte)(255 * 0.4);
                    Color lightPink = Color.Add(Colors.Red, Color.Multiply(Colors.Red, 4.0f));
                    lightPink.ScA = 0.5f;
                    foreach (int cellID in occupancyVisualEvent.AllVisibleCells)
                    {
                        Cell item = this._host.cellularFloor.FindCell(cellID);
                        Point p1 = this._host.Transform(item);
                        Point p2 = this._host.Transform(item + new UV(this._host.cellularFloor.CellSize, this._host.cellularFloor.CellSize));
                        _view.FillRectangle((int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y, GetColor(occupancyVisualEvent.ReferencedVantageCells[cellID].Count, max, alpha));
                    }
                    foreach (int cellID in occupancyVisualEvent.VantageCells)
                    {
                        Cell item = this._host.cellularFloor.FindCell(cellID);
                        Point p1 = this._host.Transform(item);
                        Point p2 = this._host.Transform(item + new UV(this._host.cellularFloor.CellSize, this._host.cellularFloor.CellSize));
                        _view.FillRectangle((int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y, lightPink);
                    }
                }
            }
            else
            {
                using (_view.GetBitmapContext())
                {
                    Color pink = Colors.Pink;
                    pink.ScA = 0.4f;
                    Color lightPink = Color.Add(Colors.Red, Color.Multiply(Colors.Red, 4.0f));
                    lightPink.ScA = 0.5f;
                    foreach (int cellID in occupancyVisualEvent.AllVisibleCells)
                    {
                        Cell item = this._host.cellularFloor.FindCell(cellID);
                        Point p1 = this._host.Transform(item);
                        Point p2 = this._host.Transform(item + new UV(this._host.cellularFloor.CellSize, this._host.cellularFloor.CellSize));
                        _view.FillRectangle((int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y, pink);
                    }
                    foreach (int cellID in occupancyVisualEvent.VantageCells)
                    {
                        Cell item = this._host.cellularFloor.FindCell(cellID);
                        Point p1 = this._host.Transform(item);
                        Point p2 = this._host.Transform(item + new UV(this._host.cellularFloor.CellSize, this._host.cellularFloor.CellSize));
                        _view.FillRectangle((int)p1.X, (int)p1.Y, (int)p2.X, (int)p2.Y, lightPink);
                    }
                }
            }

        }

    }
}

