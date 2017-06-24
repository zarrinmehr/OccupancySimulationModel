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

namespace SpatialAnalysis.FieldUtility
{
    /// <summary>
    /// Enum InterpolationMethod
    /// </summary>
    public enum InterpolationMethod
    {
        /// <summary>
        /// uses Linear Spline Interpolation
        /// </summary>
        Linear = 0,
        /// <summary>
        /// if the number of values is larger than 1 uses CubicSpline Interpolation otherwise uses LinearSpline Interpolation
        /// </summary>
        CubicSpline = 1,
        /// <summary>
        /// barycentric polynomial interpolation
        /// </summary>
        //Barycentric = 2,
    }
    /// <summary>
    /// Enum Direction
    /// </summary>
    public enum Direction
    {
        /// <summary>
        /// Y value is constant and only X values change
        /// </summary>
        Horizontal = 0,
        /// <summary>
        /// X value is constant and only Y values change
        /// </summary>
        Vertical = 1
    }
}

