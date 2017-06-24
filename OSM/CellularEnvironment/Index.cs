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
using SpatialAnalysis.Geometry;

namespace SpatialAnalysis.CellularEnvironment
{
    /// <summary>
    /// Represents the coordinates of a cell in a cellular floor
    /// </summary>
    public class Index
    {

        public static readonly Index Right = new Index(1, 0);
        public static readonly Index Left = new Index(-1, 0);
        public static readonly Index Up = new Index(0, 1);
        public static readonly Index Down = new Index(0, -1);
        public static readonly Index DownRight = new Index(-1, 1);
        public static readonly Index DownLeft = new Index(-1, -1);
        public static readonly Index UpRight = new Index(1, 1);
        public static readonly Index UpLeft = new Index(1, -1);
        /// <summary>
        /// All surrounding neighbors relative indices
        /// </summary>
        public static readonly Index[] Neighbors = new Index[]
          {
              Index.Right,
              Index.UpRight,
              Index.Up,
              Index.UpLeft,
              Index.Left,
              Index.DownLeft,
              Index.Down,
              Index.DownRight,
          };
        /// <summary>
        /// All surrounding neighbors relative indices at top, buttom, right and left
        /// </summary>
        public static readonly HashSet<Index> CrossNeighbors = new HashSet<Index>
          {
            Index.Up,
            Index.Down,
            Index.Right,
            Index.Left
          };
        /// <summary>
        /// The width coordinate of a cell
        /// </summary>
        public int I { get; set; }
        /// <summary>
        /// the height coordinates of a cell
        /// </summary>
        public int J { get; set; }
        /// <summary>
        /// Creates a index based on its width and height coordinates
        /// </summary>
        /// <param name="i">Width coordinate</param>
        /// <param name="j">Height coordinate</param>
        public Index(int i, int j)
        {
            this.I = i;
            this.J = j;
        }

        public override int GetHashCode()
        {
            int hash = 7;
            hash = 71 * hash + this.I.GetHashCode();
            hash = 71 * hash + this.J.GetHashCode();
            return hash;
        }
        public override bool Equals(object obj)
        {
            Index index = obj as Index;
            if (index == null)
            {
                return false;
            }
            else
            {
                return index.I == this.I && index.J == this.J;
            }
        }
        public override string ToString()
        {
            //return string.Format("I: {0}; J: {1}", this.I.ToString(), this.J.ToString());
            return string.Format("[{0}, {1}]", this.I.ToString(), this.J.ToString());
        }
        public static Index operator +(Index A, Index B)
        {
            return new Index(A.I + B.I, A.J + B.J);
        }
        public static Index operator -(Index A, Index B)
        {
            return new Index(A.I - B.I, A.J - B.J);
        }
        public static bool operator ==(Index A, Index B)
        {
            if (Object.ReferenceEquals(A, B))
            {
                return true;
            }
            if ((object)A == null || (object)B == null)
            {
                return false;
            }
            return A.I == B.I && A.J == B.J;
        }
        public static bool operator !=(Index A, Index B)
        {
            return !(A == B);
        }
        public static Index operator *(Index A, int Integer)
        {
            return new Index(Integer * A.I, Integer * A.J);
        }
        public static Index operator *(int Integer, Index A)
        {
            return new Index(Integer * A.I, Integer * A.J);
        }
        /// <summary>
        /// Creates a deep copy of the index
        /// </summary>
        /// <returns>A new index</returns>
        public Index Copy()
        {
            return new Index(this.I, this.J);
        }
        /// <summary>
        /// Finds the collection of indices that are not aligned with the origin
        /// </summary>
        /// <param name="range">range of neighborhood</param>
        /// <returns>a collection of indices</returns>
        internal static HashSet<Index> GetPotentialFieldNeighborhood(int range)
        {
            HashSet<Index> set = new HashSet<Index>(new DirectionComparer());
            for (int i = 0; i <= range; i++)
            {
                for (int j = 0; j <= range; j++)
                {
                    if (!(i == 0 && j == 0))
                    {
                        set.Add(new Index(i, j));
                    }
                }
            }
            HashSet<Index> indices = new HashSet<Index>(set);
            foreach (Index index in set)
            {
                var second = index.Copy();
                second.I *= -1;
                indices.Add(second);
                var third = index.Copy();
                third.I *= -1;
                third.J *= -1;
                indices.Add(third);
                var fourth = index.Copy();
                fourth.J *= -1;
                indices.Add(fourth);
            }
            return indices;
        }         
    }

    /// <summary>
    /// Compares two indexes with each other for equality test.
    /// </summary>
    /// <seealso cref="System.Collections.Generic.IEqualityComparer{SpatialAnalysis.CellularEnvironment.Index}" />
    class IndexComparer : IEqualityComparer<Index>
    {
        /// <summary>
        /// Determines whether the specified objects are equal.
        /// </summary>
        /// <param name="x">The first object of type <paramref name="T" /> to compare.</param>
        /// <param name="y">The second object of type <paramref name="T" /> to compare.</param>
        /// <returns>true if the specified objects are equal; otherwise, false.</returns>
        public bool Equals(Index x, Index y)
        {
            return x.I == y.I && x.J == y.J;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <param name="obj">The <see cref="T:System.Object" /> for which a hash code is to be returned.</param>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public int GetHashCode(Index obj)
        {
            return obj.GetHashCode();
        }
    }

    /// <summary>
    /// Includes a logic for index equality that uses the proportion of the I and J for equality test.
    /// </summary>
    /// <seealso cref="System.Collections.Generic.IEqualityComparer{SpatialAnalysis.CellularEnvironment.Index}" />
    class DirectionComparer : IEqualityComparer<Index>
    {
        /// <summary>
        /// Determines whether the specified objects are equal.
        /// </summary>
        /// <param name="x">The first object of type <paramref name="T" /> to compare.</param>
        /// <param name="y">The second object of type <paramref name="T" /> to compare.</param>
        /// <returns>true if the specified objects are equal; otherwise, false.</returns>
        public bool Equals(Index x, Index y)
        {
            double direction1 = this.DirectionFactor(x);
            double direction2 = this.DirectionFactor(y);
            bool result = true;
            if (direction1 != direction2)
            {
                result = false;
            }
            else
            {
                if (!(Math.Sign(x.I) == Math.Sign(y.I) && Math.Sign(x.J) == Math.Sign(y.J)))
                {
                    return false;
                }
            }
            return result;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <param name="obj">The <see cref="T:System.Object" /> for which a hash code is to be returned.</param>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public int GetHashCode(Index obj)
        {
            return this.DirectionFactor(obj).GetHashCode();
        }

        /// <summary>
        /// Returns the direction factor
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>System.Double.</returns>
        public double DirectionFactor(Index index)
        {
            double i = (double)index.I;
            double sineSquared = i * i / (index.I * index.I + index.J * index.J);
            return sineSquared;
        }
    }
}

