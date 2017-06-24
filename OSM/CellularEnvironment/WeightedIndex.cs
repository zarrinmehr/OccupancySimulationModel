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

namespace SpatialAnalysis.CellularEnvironment
{
    /// <summary>
    /// Represents an index with a weighting factor.
    /// </summary>
    /// <seealso cref="SpatialAnalysis.CellularEnvironment.Index" />
    /// <seealso cref="System.IComparable{SpatialAnalysis.CellularEnvironment.WeightedIndex}" />
    public class WeightedIndex: Index, IComparable<WeightedIndex>
    {
        /// <summary>
        /// Gets or sets the weighting factor of the index.
        /// </summary>
        /// <value>The weighting factor.</value>
        public double WeightingFactor { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="WeightedIndex"/> class.
        /// </summary>
        /// <param name="i">The i.</param>
        /// <param name="j">The j.</param>
        /// <param name="weightingFactor">The weighting factor.</param>
        public WeightedIndex(int i, int j, double weightingFactor): base(i,j)
        {
            this.WeightingFactor = weightingFactor;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="WeightedIndex"/> class.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="weightingFactor">The weighting factor.</param>
        public WeightedIndex(Index index, double weightingFactor)
            : base(index.I, index.J)
        {
            this.WeightingFactor = weightingFactor;
        }
        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
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
            return string.Format("[{0}, {1}];\t Weight: {2}",
                this.I.ToString(), this.J.ToString(), this.WeightingFactor.ToString());
        }
        /// <summary>
        /// Copies this instance deeply.
        /// </summary>
        /// <returns>WeightedIndex.</returns>
        public new WeightedIndex Copy()
        {
            return new WeightedIndex(this.I, this.J, this.WeightingFactor);
        }

        /// <summary>
        /// Compares the current object with another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>A value that indicates the relative order of the objects being compared. The return value has the following meanings: Value Meaning Less than zero This object is less than the <paramref name="other" /> parameter.Zero This object is equal to <paramref name="other" />. Greater than zero This object is greater than <paramref name="other" />.</returns>
        public int CompareTo(WeightedIndex other)
        {
            if (this.WeightingFactor == other.WeightingFactor)
            {
                if (this.I != other.I)
                {
                    return this.I.CompareTo(other.I);
                }
                else
                {
                    return this.J.CompareTo(other.J);
                }
            }
            else
            {
                return this.WeightingFactor.CompareTo(other.WeightingFactor);
            }
        }
    }
}

