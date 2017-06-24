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
using SpatialAnalysis.Miscellaneous;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace SpatialAnalysis.Visualization
{
    internal class ColorCodePolicy
    {
        private bool _reverseCode;
        private Color[] _colors;
        public Color[] Colors
        {
            get
            {
                var colors = new Color[this._colors.Length];
                Array.Copy(this._colors, colors, this._colors.Length);
                return colors;
            }
        }
        public void SetColors(Color[] colors)
        {
            if (colors != null && colors.Length!= 0)
            {
                this._colors = colors;
                this.setInterpolatedColors();
            }
            else
            {
                throw new ArgumentException("Colors should not be null or empty");
            }
        }
        public int Steps { get; private set; }
        public void SetSteps(int steps)
        {
            if (steps>4)
            {
                this.Steps = steps;
                this.setInterpolatedColors();
            }
            else
            {
                throw new ArgumentException("Color steps should be larger than 3.");
            }
        }
        private Color[] _interpolatedColors;
        private void setInterpolatedColors()
        {
            this._interpolatedColors = new Color[this.Steps];
            float intercept = ((float)this.Colors.Length-1f) / (this.Steps - 1);
            float value = 0f;
            
            for (int i = 0; i < this.Steps; i++)
            {
                int colorIndex1 = (int)value;
                int colorIndex2 = colorIndex1 + 1;
                colorIndex2 = Math.Min(colorIndex2, this.Colors.Length - 1);
                float t1 = value - colorIndex1;
                float t2 = 1f - t1;
                float a = this._colors[colorIndex1].A * t2 + this._colors[colorIndex2].A * t1;
                float r = this._colors[colorIndex1].R * t2 + this._colors[colorIndex2].R * t1;
                float g = this._colors[colorIndex1].G * t2 + this._colors[colorIndex2].G * t1;
                float b = this._colors[colorIndex1].B * t2 + this._colors[colorIndex2].B * t1;
                this._interpolatedColors[i] = Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b);
                value += intercept;
            }
            if (this._reverseCode)
            {
                Array.Reverse(this._interpolatedColors);
            }
        }
        public void Reverse()
        {
            Array.Reverse(this._interpolatedColors);
            this._reverseCode = !this._reverseCode;
        }
        public Color GetColor(double scale)
        {
            scale = Math.Min(1d, scale);
            scale = Math.Max(0d, scale);
            int index = (int)(scale * (this.Steps-1));
            return this._interpolatedColors[index];
        }
        public ColorCodePolicy (Color[] colors, int steps)
        {
            if (colors != null && colors.Length != 0)
            {
                this._colors = colors;
            }
            else
            {
                throw new ArgumentException("Colors should not be null or empty");
            }
            if (steps > 4)
            {
                this.Steps = steps;
            }
            else
            {
                throw new ArgumentException("Color steps should be larger than 3.");
            }
            this.setInterpolatedColors();
            this._reverseCode = false;
        }
        public static ColorCodePolicy LoadOSMPolicy()
        {
            var colors = new Color[]
            {
                Color.FromRgb(173,  255,    47),
                Color.FromRgb(99,   114,    5),
                Color.FromRgb(133,  27,     0),
                Color.FromRgb(0,    27,     139 ),
            };
            ColorCodePolicy policy = new ColorCodePolicy(colors, 40);
            return policy;
        }
        #region bitwise operations for color as integer 
        /*The code has changed to use windows media color because integer colors are not always supported by WPF*/
        public static int Iteger_Color(byte r, byte g, byte b)
        {
            int rgb = ((r & 0x0ff) << 16) | ((g & 0x0ff) << 8) | (b & 0x0ff);
            return rgb;
        }
        public static byte[] GetRGB(int c)
        {
            int r = c >> 16 & 0xFF;
            int g = c >> 8 & 0xFF;
            int b = c & 0xFF;
            return new byte[] { (byte)r, (byte)g, (byte)b };
        }
        public static int Get_Red(int color)
        {
            int r = color >> 24 & 0xFF;
            return (byte)r;
        }
        public static int Get_Green(int color)
        {
            int g = color >> 16 & 0xFF;
            return (byte)g;
        }
        public static int Get_Blue(int color)
        {
            int b = color & 0xFF;
            return (byte)b;
        }
        public static Color Integer_To_Color(int color)
        {
            int r = color >> 16 & 0xFF;
            int g = color >> 8 & 0xFF;
            int b = color & 0xFF;
            return Color.FromRgb((byte)r, (byte)g, (byte)b);
        } 
        #endregion
    }
}

