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

namespace SpatialAnalysis.CellularEnvironment.GetCellValue
{
    /// <summary>
    /// Class IndexNode.
    /// </summary>
    public class IndexNode
    {
        /// <summary>
        /// Gets or sets the index.
        /// </summary>
        /// <value>The index.</value>
        public Index index { get; set; }
        /// <summary>
        /// Gets or sets the connections.
        /// </summary>
        /// <value>The connections.</value>
        public HashSet<IndexNode> Connections { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="IndexNode"/> class.
        /// </summary>
        /// <param name="_index">The index.</param>
        public IndexNode(Index _index)
        {
            this.index = _index;
            this.Connections = new HashSet<IndexNode>();
        }
        /// <summary>
        /// Adds the connection.
        /// </summary>
        /// <param name="indexNode">The index node.</param>
        public void AddConnection(IndexNode indexNode)
        {
            this.Connections.Add( indexNode);
        }
        public override bool Equals(object obj)
        {
            IndexNode other = (IndexNode)obj;
            if (other == null)
            {
                return false;
            }
            return this.index.Equals(other.index);
        }
        public override int GetHashCode()
        {
            return this.index.GetHashCode();
        }
        public override string ToString()
        {
            return this.index.ToString();
        }


    }
    /// <summary>
    /// Class IndexGraph.
    /// </summary>
    public class IndexGraph
    {
        /// <summary>
        /// Gets or sets the index node map.
        /// </summary>
        /// <value>The index node map.</value>
        public Dictionary<Index, IndexNode> IndexNodeMap { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="IndexGraph"/> class.
        /// </summary>
        /// <param name="indexCollection">The index collection.</param>
        public IndexGraph(HashSet<Index> indexCollection)
        {
            this.IndexNodeMap = new Dictionary<Index, IndexNode>(new IndexComparer());
            foreach (Index item in indexCollection)
            {
                this.IndexNodeMap.Add(item, new IndexNode(item));
            }
            foreach (Index index in indexCollection)
            {
                foreach (Index item in Index.CrossNeighbors)
                {
                    var neighbor = item + index;
                    if (this.IndexNodeMap.ContainsKey(neighbor))
                    {
                        this.IndexNodeMap[index].AddConnection(this.IndexNodeMap[neighbor]);
                        this.IndexNodeMap[neighbor].AddConnection(this.IndexNodeMap[index]);
                    }
                }
            }
        }

    }
    
}

