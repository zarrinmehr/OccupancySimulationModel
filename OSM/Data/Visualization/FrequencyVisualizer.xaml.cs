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
using SpatialAnalysis.Events;
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

namespace SpatialAnalysis.Data.Visualization
{
    /// <summary>
    /// Interaction logic for FrequencyVisualizer.xaml
    /// </summary>
    public partial class FrequencyVisualizer : UserControl
    {
        EvaluationEvent _event { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="FrequencyVisualizer"/> class.
        /// </summary>
        public FrequencyVisualizer()
        {
            InitializeComponent();
            this.SizeChanged += FrequencyVisualizer_SizeChanged;
        }

        void FrequencyVisualizer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this._event==null)
            {
                return;
            }
            this.draw();
        }
        /// <summary>
        /// Draws the frequency.
        /// </summary>
        /// <param name="event_">The event.</param>
        /// <exception cref="System.ArgumentException">The Amplitudes cannot be null and should include more than 1 frequencies</exception>
        public void DrawFrequency(EvaluationEvent event_)
        {
            if (!event_.HasFrequencies || event_.FrequencyAmplitudes.Length < 2)
            {
                throw new ArgumentException("The Amplitudes cannot be null and should include more than 1 frequencies");
            }
            this._event = event_;
            this.draw();
        }
        private void draw()
        {
            double y1 = double.NegativeInfinity;
            for (int i = 0; i < this._event.FrequencyAmplitudes.Length; i++)
            {
                if (this._event.FrequencyAmplitudes[i] > y1) y1 = this._event.FrequencyAmplitudes[i];
            }
            this._yMax.Text = y1.ToString("0.0000");
            this._graphsHost.Source = null;
            int h = (int)this.layout.RowDefinitions[0].ActualHeight;
            int w = (int)this.layout.ColumnDefinitions[1].ActualWidth;
            this._graphsHost.Width = this.layout.ColumnDefinitions[1].ActualWidth;
            this._graphsHost.Height = this.layout.RowDefinitions[0].ActualHeight;
            WriteableBitmap _view = BitmapFactory.New(w, h);
            int[] pnts = new int[this._event.FrequencyAmplitudes.Length * 2];
            double xScale = w / ((double)this._event.FrequencyAmplitudes.Length);
            double yScale = h / ((double)y1);
            for (int i = 0; i < this._event.FrequencyAmplitudes.Length; i++)
            {
                pnts[2 * i] = (int)(i * xScale);
                pnts[2 * i + 1] = h - (int)(this._event.FrequencyAmplitudes[i] * yScale);
            }
            using (_view.GetBitmapContext())
            {
                _view.DrawPolyline(pnts, Colors.Green);
            }
            this._graphsHost.Source = _view;
        }
        
    }
}

