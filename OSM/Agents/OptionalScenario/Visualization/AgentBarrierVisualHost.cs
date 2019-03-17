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
using SpatialAnalysis.Visualization;
using SpatialAnalysis.Miscellaneous;


namespace SpatialAnalysis.Agents.OptionalScenario.Visualization
{
    /// <summary>
    /// Visualize scene
    /// </summary>
    public class AgentBarrierVisualHost : FrameworkElement
    {
        private OSMDocument _host { get; set; }
        private System.Windows.Media.Geometry _geometry;
        public System.Windows.Media.Geometry Geometry
        {
            get { return _geometry; }
            set  { _geometry = value;}
        }
        private double boarderThickness { get; set; }
        private Brush boarderBrush { get; set; }
        private VisualCollection _children { get; set; }
        private Brush fillBrush { get; set; }
        private MenuItem visualizationMenu { get; set; }
        private MenuItem hide_Show_Menu { get; set; }
        private MenuItem boarderThickness_Menu { get; set; }
        private MenuItem boarderBrush_Menu { get; set; }
        private MenuItem fillBrush_Menu { get; set; }

        public AgentBarrierVisualHost()
        {
            this._children = new VisualCollection(this);
            this.boarderThickness = 1;
            this.boarderBrush = Brushes.Black;
            this.visualizationMenu = new MenuItem();
            this.boarderBrush_Menu = new MenuItem() { Header = "Boarder Brush" };
            this.boarderThickness_Menu = new MenuItem() { Header = "Boarder Thickness" };
            this.fillBrush_Menu = new MenuItem() { Header = "Fill Brush" };
            this.hide_Show_Menu = new MenuItem() { Header = "Hide" };
            this.visualizationMenu.Items.Add(this.hide_Show_Menu);
            this.visualizationMenu.Items.Add(this.boarderThickness_Menu);
            this.visualizationMenu.Items.Add(this.fillBrush_Menu);
            this.visualizationMenu.Items.Add(this.boarderBrush_Menu);
            this.boarderThickness_Menu.Click += new RoutedEventHandler(boarderThickness_Menu_Click);
            this.fillBrush_Menu.Click += new RoutedEventHandler(fillBrush_Menu_Click);
            this.boarderBrush_Menu.Click += new RoutedEventHandler(boarderBrush_Menu_Click);
            this.hide_Show_Menu.Click += new RoutedEventHandler(hide_Show_Menu_Click);
            
        }

        private void hide_Show_Menu_Click(object sender, RoutedEventArgs e)
        {
            if (this.Visibility == System.Windows.Visibility.Visible)
            {
                this.Visibility = System.Windows.Visibility.Collapsed;
                this.hide_Show_Menu.Header = "Show";
                this.boarderBrush_Menu.IsEnabled = false;
                this.fillBrush_Menu.IsEnabled = false;
                this.boarderThickness_Menu.IsEnabled = false;
            }
            else
            {
                this.Visibility = System.Windows.Visibility.Visible;
                this.hide_Show_Menu.Header = "Hide";
                this.boarderBrush_Menu.IsEnabled = true;
                this.fillBrush_Menu.IsEnabled = true;
                this.boarderThickness_Menu.IsEnabled = true;
            }
        }

        private void setMenuName(String name)
        {
            this.visualizationMenu.Header = name;
        }

        private void boarderBrush_Menu_Click(object sender, RoutedEventArgs e)
        {
            BrushPicker colorPicker = new BrushPicker(this.boarderBrush);
            colorPicker.Owner = this._host;
            colorPicker.ShowDialog();
            this.boarderBrush = colorPicker._Brush;
            this.draw();
            colorPicker = null;
        }

        private void fillBrush_Menu_Click(object sender, RoutedEventArgs e)
        {
            BrushPicker colorPicker = new BrushPicker(this.fillBrush);
            colorPicker.Owner = this._host;
            colorPicker.ShowDialog();
            this.fillBrush = colorPicker._Brush;
            this.draw();
            colorPicker = null;
        }

        private void boarderThickness_Menu_Click(object sender, RoutedEventArgs e)
        {
            GetNumber gn = new GetNumber("Enter New Thickness Value", "New thickness value will be applied to the edges of barriers", this.boarderThickness);
            gn.Owner = this._host;
            gn.ShowDialog();
            this.boarderThickness = gn.NumberValue;
            this.draw();
            gn = null;
        }

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
        private void setGeometry(BarrierPolygon[] barriers)
        {
            StreamGeometry sg = new StreamGeometry();
            using (StreamGeometryContext sgc = sg.Open())
            {
                foreach (BarrierPolygon barrier in barriers)
                {
                    sgc.BeginFigure(this.toPoint(barrier.BoundaryPoints[0]), true, true);
                    for (int i = 1; i < barrier.Length; i++)
                    {
                        sgc.LineTo(this.toPoint(barrier.BoundaryPoints[i]), true, true);
                    }
                }
            }
            sg.FillRule = FillRule.EvenOdd;
            this._geometry = sg;
        }
        private void draw()
        {
            if (this._geometry == null)
            {
                MessageBox.Show("Cannot Draw Null Geometry");
                return;
            }
            try
            {
                this._children.Clear();
                //double scale = this.RenderTransform.Value.M11 * this.RenderTransform.Value.M11 +
                //    this.RenderTransform.Value.M12 * this.RenderTransform.Value.M12;
                //scale = Math.Sqrt(scale);
                //double t = this.boarderThickness / scale;
                Pen _pen = new Pen(this.boarderBrush, this.boarderThickness);
                DrawingVisual drawingVisual = new DrawingVisual();
                using (DrawingContext drawingContext = drawingVisual.RenderOpen())
                {
                    drawingContext.DrawGeometry(this.fillBrush, _pen, this._geometry);
                }
                drawingVisual.Drawing.Freeze();
                this._children.Add(drawingVisual);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Report());
            }
        }

        public void Clear()
        {
            this._children.Clear();
            this._geometry = null;
            this._host = null;
            this.boarderBrush = null;
            this._children = null;
            this.fillBrush = null;
            this.visualizationMenu.Items.Clear();
            this.boarderThickness_Menu.Click -= boarderThickness_Menu_Click;
            this.fillBrush_Menu.Click -= fillBrush_Menu_Click;
            this.boarderBrush_Menu.Click -= boarderBrush_Menu_Click;
            this.hide_Show_Menu.Click -= hide_Show_Menu_Click;
            this.hide_Show_Menu = null;
            this.boarderThickness_Menu = null;
            this.boarderBrush_Menu = null;
            this.fillBrush_Menu = null;
            this.visualizationMenu = null;
        }

        private Point toPoint(UV uv)
        {
            return new Point(uv.U, uv.V);
        }

        public void SetHost(OSMDocument host, string menueName)
        {
            this._host = host;
            this.RenderTransform = this._host.RenderTransformation;
            this.setMenuName(menueName);
            this._host.ViewUtil.Items.Add(this.visualizationMenu);
            this.setGeometry(this._host.cellularFloor.BarrierBuffers);
            this.boarderBrush = new SolidColorBrush(Colors.Gold) { Opacity = .8 };
            this.boarderThickness = this._host.UnitConvertor.Convert(0.25d);
            this.draw();
        }
        public void ReDraw()
        {
            this._geometry = null;
            this.setGeometry(this._host.cellularFloor.BarrierBuffers);
            this.draw();
        }


    }

}

