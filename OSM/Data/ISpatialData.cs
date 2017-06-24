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
using SpatialAnalysis.CellularEnvironment;

namespace SpatialAnalysis.Data
{
    /// <summary>
    /// This interface represents generic data type
    /// </summary>
    public interface ISpatialData
    {
        /// <summary>
        /// Gets the name of the data.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }
        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <value>The data.</value>
        Dictionary<Cell, double> Data { get; }
        /// <summary>
        /// Gets the minimum value of the data.
        /// </summary>
        /// <value>The minimum.</value>
        double Min { get; }
        /// <summary>
        /// Gets the maximum value of the data.
        /// </summary>
        /// <value>The maximum.</value>
        double Max { get; }
        /// <summary>
        /// Gets the type of data.
        /// </summary>
        /// <value>The type.</value>
        DataType Type { get; }
    }
}

