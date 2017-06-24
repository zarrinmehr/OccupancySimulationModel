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
using System.ComponentModel;

namespace SpatialAnalysis.Optimization
{
    /// <summary>
    /// Class Variable.
    /// </summary>
    /// <seealso cref="System.ComponentModel.INotifyPropertyChanged" />
    public class Variable: INotifyPropertyChanged
    {
        #region implimentation of INotifyPropertyChanged
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        private void notifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        private double _value;
        /// <summary>
        /// Gets or sets the value of the variable.
        /// </summary>
        /// <value>The value.</value>
        /// <exception cref="ArgumentOutOfRangeException">New value is out of range between minimum and maximum values</exception>
        public double Value 
        {
            get { return this._value; }
            set
            {
                if (this._value != value)
                {
                    if (value>this.Maximum || value<this.Minimum)
                    {
                        throw new ArgumentOutOfRangeException("New value is out of range between minimum and maximum values");
                    }
                    this._value = value;
                    this.notifyPropertyChanged("Value");
                }
            }
        }
        private double _min;
        /// <summary>
        /// Gets or sets the minimum of the variable.
        /// </summary>
        /// <value>The minimum.</value>
        /// <exception cref="ArgumentOutOfRangeException">New 'Minimum' value cannot be larger than the 'Maximum' bound value</exception>
        public double Minimum 
        { 
            get { return this._min; }
            set
            {
                if (value == this._min)
                {
                    return;
                }
                if (value>=this.Maximum)
                {
                    throw new ArgumentOutOfRangeException("New 'Minimum' value cannot be larger than the 'Maximum' bound value");
                }
                else
                {
                    this._min = value;
                    if (this.Value<value)
                    {
                        this.Value = value;
                    }
                }
            }
        }
        private double _max;
        /// <summary>
        /// Gets or sets the maximum of the variable.
        /// </summary>
        /// <value>The maximum.</value>
        /// <exception cref="ArgumentOutOfRangeException">New 'Maximum' value cannot be smaller than the 'Minimum' bound value</exception>
        public double Maximum 
        { 
            get { return this._max; }
            set
            {
                if (value == this._max)
                {
                    return;
                }
                if (value<=this.Minimum)
                {
                    throw new ArgumentOutOfRangeException("New 'Maximum' value cannot be smaller than the 'Minumum' bound value");
                }
                else
                {
                    this._max = value;
                    if (this.Value>value)
                    {
                        this.Value = value;
                    }
                }
            }
        }
        /// <summary>
        /// Gets the range of the variability.
        /// </summary>
        /// <value>The range.</value>
        public double Range { get { return this.Maximum - this.Minimum; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="Variable"/> class.
        /// </summary>
        /// <param name="initialValue">The initial value.</param>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        /// <exception cref="ArgumentException">Invalid variability range</exception>
        /// <exception cref="ArgumentNullException">Variable initial value is out of range</exception>
        public Variable(double initialValue, double min, double max)
        {
            if (min >= max)
            {
                throw new ArgumentException("Invalid variability range");
            }
            if (max < initialValue || min > initialValue)
            {
                throw new ArgumentNullException("Variable initial value is out of range");
            }
            this._min = min;
            this._max = max;
            this.Value = initialValue;
        }
        public override string ToString()
        {
            return string.Format("Value: {0}, Min: {1}, Max: {2}", 
                this.Value.ToString(), this.Minimum.ToString(), this.Maximum.ToString());
        }

        /// <summary>
        /// Copies this instance deeply.
        /// </summary>
        /// <returns>Variable.</returns>
        public Variable Copy()
        {
            return new Variable(this.Value, this.Minimum, this.Maximum);
        }

        public override int GetHashCode()
        {
            int hash = 7;
            hash = 71 * hash + this.Minimum.GetHashCode();
            hash = 71 * hash + this.Value.GetHashCode();
            hash = 71 * hash + this.Maximum.GetHashCode();
            return hash;
        }

        public override bool Equals(object obj)
        {
            Variable variable = obj as Variable;
            if (variable == null)
            {
                return false;
            }
            return variable.Minimum == this.Minimum && 
                variable.Value == this.Value && 
                variable.Maximum == this.Maximum;
        }



        
    }
}

