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
using Jace;
using MathNet.Numerics.Interpolation;
using SpatialAnalysis.CellularEnvironment;
using System.Windows;

namespace SpatialAnalysis.Data
{


    /// <summary>
    /// Represents a layer of Spatial Data Field which includes a cost function to determine the desirability and cost which is associated to the data.
    /// </summary>
    /// <seealso cref="SpatialAnalysis.Data.Function" />
    /// <seealso cref="SpatialAnalysis.Data.ISpatialData" />
    public class SpatialDataField : Function, ISpatialData
    {
        /// <summary>
        /// Gets or sets a value indicating whether to capture event outside interval.
        /// </summary>
        /// <value><c>true</c> if capture event when outside interval; otherwise, <c>false</c>.</value>
        public bool CaptureEventWhenOutsideInterval { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to use this data to capture event.
        /// </summary>
        /// <value><c>true</c> if [use to capture event]; otherwise, <c>false</c>.</value>
        public bool UseToCaptureEvent { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to use the cost function for event capturing or the value.
        /// </summary>
        /// <value><c>true</c> if use cost function for event capturing; otherwise, <c>false</c>.</value>
        public bool UseCostFunctionForEventCapturing { get; set; }
        private Dictionary<Cell, double> _data;
        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <value>The data.</value>
        public Dictionary<Cell, double> Data { get { return _data; } }
        private double _min;
        /// <summary>
        /// Gets the minimum value of the data.
        /// </summary>
        /// <value>The minimum.</value>
        public double Min { get { return _min; } }
        private double _max;
        /// <summary>
        /// Gets the maximum value of the data.
        /// </summary>
        /// <value>The maximum.</value>
        public double Max { get { return _max; } }
        /// <summary>
        /// Gets or sets the event capturing interval.
        /// </summary>
        /// <value>The event capturing interval.</value>
        public Interval EventCapturingInterval { get; set; }
        /// <summary>
        /// Gets the type of data.
        /// </summary>
        /// <value>The type.</value>
        public DataType Type { get { return DataType.SpatialData; }}
        /// <summary>
        /// Initializes a new instance of the <see cref="SpatialDataField"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="values">The values.</param>
        /// <exception cref="System.ArgumentException">No data assigned to Spatial Data Field</exception>
        public SpatialDataField(string name, Dictionary<Cell, double> values):base(name)
        {
            if (values == null || values.Count==0)
            {
                throw new ArgumentException("No data assigned to Spatial Data Field");
            }
            this._data = values;
            this._min = double.PositiveInfinity;
            this._max = double.NegativeInfinity;
            foreach (Cell cell in this.Data.Keys)
            {
                double val=this.Data[cell];
                if (this._max<val)
                {
                    this._max = val;
                }
                if (this._min>val)
                {
                    this._min = val;
                }
            }
            
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="SpatialDataField"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="values">The values.</param>
        /// <param name="hasDefaultRepulsion">if set to <c>true</c> has a default repulsion mechanism as its cost function.</param>
        /// <param name="includeInFieldGeneration">if set to <c>true</c> is included in potential field generation of activities.</param>
        /// <exception cref="System.ArgumentException">No data assigned to Spatial Data Field</exception>
        public SpatialDataField(string name, Dictionary<Cell, double> values, bool hasDefaultRepulsion, bool includeInFieldGeneration)
            : base(name, hasDefaultRepulsion, includeInFieldGeneration)
        {
            if (values == null || values.Count == 0)
            {
                throw new ArgumentException("No data assigned to Spatial Data Field");
            }
            this._data = values;
            this._min = double.PositiveInfinity;
            this._max = double.NegativeInfinity;
            foreach (Cell cell in this.Data.Keys)
            {
                double val = this.Data[cell];
                if (this._max < val)
                {
                    this._max = val;
                }
                if (this._min > val)
                {
                    this._min = val;
                }
            }
            if (this.HasBuiltInRepulsion)
            {
                string r = AgentParameters.GEN_BarrierRepulsionRange.ToString();
                string m = AgentParameters.GEN_MaximumRepulsion.ToString();
                string a = "(2*" + m + "/" + r + "^2)";
                string condition1 = "X<" + r +"/2";
                string condition2 = "X<" + r;
                string case1 = "-" + a + "*X^2 + " + m;
                string case2 = a + "*(X-" + r + ")^2";
                string inside = "if(" + condition1 + "," + case1 + "," + case2 + ")";
                //this.TextFormula = "if("+condition2+","+inside+",0)";                 This nested if is very complicated as an example
                this.TextFormula = "if(" + condition2 + "," + m + ",0)";


            }

        }
        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return base.ToString();
        }
        /// <summary>
        /// Sets the event capturing interval.
        /// </summary>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        public void SetEventCapturingInterval(double min, double max)
        {
            this.EventCapturingInterval = new Interval(min, max);
        }

        /// <summary>
        /// Determines if an event was captured.
        /// </summary>
        /// <param name="cell">The cell.</param>
        /// <returns>System.Nullable&lt;System.Double&gt;.</returns>
        public double? EventCaptured(Cell cell)
        { 
            double value = 0;
            if (!this._data.TryGetValue(cell,out value))
            {
                return null;
            }
            //switch to cost value if event targests cost
            if (this.UseCostFunctionForEventCapturing)
            {
                value = this.GetCost(value);
            }
            //checking for interval type
            if (this.CaptureEventWhenOutsideInterval)
            {
                if (!this.EventCapturingInterval.Includes(value))
                {
                    return value;
                }
            }
            else
            {
                if (this.EventCapturingInterval.Includes(value))
                {
                    return value;
                }
            }
            return null;
        }
        

    }


    
}

