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
    /// A two way map data model. Hash codes of the data types should be unique.
    /// </summary>
    /// <typeparam name="T1">Data type 1</typeparam>
    /// <typeparam name="T2">Data type 2</typeparam>
    public class Map<T1, T2>
    {
        private Dictionary<T1, T2> T1_T2 { get; set; }
        private Dictionary<T2, T1> T2_T1 { get; set; }
        /// <summary>
        /// The size of the map
        /// </summary>
        public int Count { get { return this.T2_T1.Count; } }

        /// <summary>
        /// Enum Types
        /// </summary>
        public enum Types
        {
            /// <summary>
            /// Data Type1
            /// </summary>
            T1,
            /// <summary>
            /// Data Type2
            /// </summary>
            T2
        }
        /// <summary>
        /// Instantiates an empty map
        /// </summary>
        public Map()
        {
            this.T1_T2 = new Dictionary<T1, T2>();
            this.T2_T1 = new Dictionary<T2, T1>();
        }
        /// <summary>
        /// Clears the map
        /// </summary>
        public virtual void Clear()
        {
            this.T1_T2.Clear();
            this.T2_T1.Clear();
        }
        /// <summary>
        /// Adds to the map
        /// </summary>
        /// <param name="a">Data 1</param>
        /// <param name="b">Data 2</param>
        public virtual void Add(T1 a, T2 b)
        {
            this.T1_T2.Add(a, b);
            this.T2_T1.Add(b, a);
        }
        /// <summary>
        /// Adds to the map
        /// </summary>
        /// <param name="a">Data 2</param>
        /// <param name="b">Data 1</param>
        public virtual void Add(T2 a, T1 b)
        {
            this.T1_T2.Add(b, a);
            this.T2_T1.Add(a, b);
        }
        /// <summary>
        /// Tests for inclusion
        /// </summary>
        /// <param name="t">Input data</param>
        /// <returns>True if included and false if not included</returns>
        public virtual bool Contains(T1 t)
        {
            return this.T1_T2.ContainsKey(t);
        }
        /// <summary>
        /// Tests for inclusion
        /// </summary>
        /// <param name="t">Input data</param>
        /// <returns>True if included and false if not included</returns>
        public virtual bool Contains(T2 t)
        {
            return this.T2_T1.ContainsKey(t);
        }
        /// <summary>
        /// Removes a data pair
        /// </summary>
        /// <param name="t"> Data to remove</param>
        public virtual void Remove(T1 t)
        {
            T2 t2 = this.T1_T2[t];
            this.T1_T2.Remove(t);
            this.T2_T1.Remove(t2);
        }
        /// <summary>
        /// Removes a data pair
        /// </summary>
        /// <param name="t">Data to remove</param>
        public virtual void Remove(T2 t)
        {
            T1 t1 = this.T2_T1[t];
            this.T1_T2.Remove(t1);
            this.T2_T1.Remove(t);
        }
        /// <summary>
        /// Find the pair of data
        /// </summary>
        /// <param name="t">Input data</param>
        /// <returns>Corresponding data</returns>
        public virtual T1 Find(T2 t)
        {
            return this.T2_T1[t];
        }
        /// <summary>
        /// Find the pair of data
        /// </summary>
        /// <param name="t">Input data</param>
        /// <returns>Corresponding data</returns>
        public virtual T2 Find(T1 t)
        {
            return this.T1_T2[t];
        }
        /// <summary>
        /// Return all variables of type 1
        /// </summary>
        /// <returns>An Array of data</returns>
        public virtual T1[] GetVariable1()
        {
            return this.T1_T2.Keys.ToArray();
        }
        /// <summary>
        /// Returns all variables of type 2
        /// </summary>
        /// <returns>An Array of data</returns>
        public virtual T2[] GetVariable2()
        {
            return this.T2_T1.Keys.ToArray();
        }
    }
}

