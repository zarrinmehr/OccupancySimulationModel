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
using SpatialAnalysis.Geometry;
using System.Diagnostics;
using SpatialAnalysis.Miscellaneous;

namespace SpatialAnalysis.IsovistUtility
{
    /// <summary>
    /// Includes an array of destinations for a cell where the agent is located
    /// </summary>
    public class AgentEscapeRoutes
    {
        /// <summary>
        /// Gets or sets the error message which was generated in the creation of this instance in a parallel thread
        /// </summary>
        /// <value>The error.</value>
        public string Error { get; set; }
        private Cell _vantageCell;
        /// <summary>
        /// Gets the vantage cell.
        /// </summary>
        /// <value>The vantage cell.</value>
        public Cell VantageCell
        {
            get { return _vantageCell; }
        }
        private AgentCellDestination[] _destinations;

        /// <summary>
        /// Gets the destinations which are visible to the agent.
        /// </summary>
        /// <value>The destinations.</value>
        public AgentCellDestination[] Destinations
        {
            get { return _destinations; }
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="AgentEscapeRoutes"/> class.
        /// </summary>
        /// <param name="vantageCell">The vantage cell.</param>
        /// <param name="destinations">The destinations.</param>
        public AgentEscapeRoutes(Cell vantageCell, AgentCellDestination[] destinations)
        {
            this._destinations = destinations;
            this._vantageCell = vantageCell;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="AgentEscapeRoutes"/> class.
        /// This constructor is for instantiation only and the destinations will be computed later in a parallel thread. The results are validated according to the error messages that are received.
        /// </summary>
        /// <param name="vantageCell">The vantage cell.</param>
        public AgentEscapeRoutes(Cell vantageCell)
        {
            this._vantageCell = vantageCell;
        }
        //step 2
        /// <summary>
        /// Computes the destinations in a parallel thread
        /// </summary>
        /// <param name="externalDepth">The external depth of view.</param>
        /// <param name="desiredNumber">The desired number of destinations to keep after simplification.</param>
        /// <param name="cellularFloor">The cellular floor.</param>
        /// <param name="staticCost">The static cost.</param>
        /// <param name="Tolerance">The tolerance.</param>
        public void ComputeDestinations(double externalDepth, int desiredNumber,
            CellularFloor cellularFloor, Dictionary<Cell, double> staticCost, double Tolerance = OSMDocument.AbsoluteTolerance)
        {
            AgentEscapeRoutes agentScapeRoutes=null;
            try 
	        {	        
		        agentScapeRoutes = CellularIsovistCalculator.GetAgentEscapeRoutes(this.VantageCell, 
                    externalDepth, desiredNumber, cellularFloor, staticCost, Tolerance);
	        }
	        catch (Exception er)
	        {
                this.Error = er.Report();
	        }

            if (agentScapeRoutes==null)
            {
                this._destinations = null;
            }
            else
            {
                this._destinations = agentScapeRoutes.Destinations;
            }
        }

    }

    /// <summary>
    /// This class Contains a destination cell and the cost associated with it. 
    /// Unlike cell destination it does not include angle.
    /// </summary>
    public class AgentCellDestination
    {
        /// <summary>
        /// Gets or sets the desirability cost.
        /// </summary>
        /// <value>The desirability cost.</value>
        public double DesirabilityCost { get; set; }
        //public UV NormalizedDirection { get; set; }
        /// <summary>
        /// Gets or sets the destination.
        /// </summary>
        /// <value>The destination.</value>
        public Cell Destination { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="AgentCellDestination"/> class.
        /// </summary>
        /// <param name="cellDestination">The cell destination.</param>
        public AgentCellDestination(CellDestination cellDestination)
        {
            this.DesirabilityCost = cellDestination.DesirabilityCost;
            //this.NormalizedDirection = cellDestination.NormalizedDirection;
            this.Destination = cellDestination.CellValue;
        }
        public override string ToString()
        {
            return string.Format("Destination Cell: {0}; Cost: {1}",
                 this.Destination.ToString(), this.DesirabilityCost.ToString());
        }
        // step 4
        /// <summary>
        /// Extracts the escape route.
        /// </summary>
        /// <param name="vantageCell">The vantage cell.</param>
        /// <param name="angleIntercept">The angle intercept.</param>
        /// <param name="collection">The collection.</param>
        /// <param name="staticCosts">The static costs.</param>
        /// <returns>AgentCellDestination[].</returns>
        public static AgentCellDestination[] ExtractedEscapeRoute(Cell vantageCell, int angleIntercept, ICollection<Cell> collection, Dictionary<Cell, double> staticCosts)
        {
            CellDestination[] destinations = new CellDestination[collection.Count];
            int i = 0;
            foreach (var item in collection)
            {
                destinations[i] = new CellDestination(vantageCell, item, staticCosts[item]);
                i++;
            }
            Array.Sort(destinations, new CellDestinationComparerAngleThenCost(angleIntercept));
            //HashSet<CellDestination> SimplifiedSet = new HashSet<CellDestination>(destinations, new CellDestinationComparerAngleOnly(angleIntercept));
            
            List<CellDestination> simplified = new List<CellDestination> { destinations[0] };
            CellDestination last = destinations[0];
            double angle = (2 * Math.PI) / angleIntercept;
            for (i = 1; i < destinations.Length; i++)
            {
                int a1 = (int)(last.Angle / angle);
                int a2 = (int)(destinations[i].Angle / angle);
                if (a1 != a2)
                {
                    simplified.Add(destinations[i]);
                    last = destinations[i];
                }
            }
            /*
             * */
            bool[] keep = new bool[simplified.Count];
            for (i = 0; i < keep.Length; i++)
            {
                keep[i] = true;
            }
            for (i = 0; i < simplified.Count; i++)
            {
                if (keep[i])
                {
                    int next_Index = (i < simplified.Count - 1) ? i + 1 : 0;
                    double angleWith_next;

                    if (i < simplified.Count - 1)
                    {
                        angleWith_next = simplified[i + 1].Angle - simplified[i].Angle;
                    }
                    else
                    {
                        angleWith_next = 2 * Math.PI - simplified[i].Angle + simplified[0].Angle;
                    }
                    if (angleWith_next < angle)
                    {
                        if (simplified[i].DesirabilityCost > simplified[next_Index].DesirabilityCost)
                        {
                            keep[i] = false;
                        }
                        else
                        {
                            keep[next_Index] = false;
                        }
                    }
                    if (keep[i])
                    {
                        int before_Index = (i > 0) ? i - 1 : simplified.Count - 1;
                        double angleWith_Before;
                        if (i > 0)
                        {
                            angleWith_Before = simplified[i].Angle - simplified[i - 1].Angle;
                        }
                        else
                        {
                            angleWith_Before = 2 * Math.PI - simplified[simplified.Count - 1].Angle + simplified[0].Angle;
                        }
                        if (angleWith_Before < angle)
                        {
                            if (simplified[i].DesirabilityCost > simplified[before_Index].DesirabilityCost)
                            {
                                keep[i] = false;
                            }
                            else
                            {
                                keep[before_Index] = false;
                            }
                        }
                    }
                }
            }
            if (keep.Length==1)
            {
                keep[0] = true;
            }
            List<CellDestination> purged = new List<CellDestination>();
            for (i = 0; i < keep.Length; i++)
            {
                if (keep[i])
                {
                    purged.Add(simplified[i]);
                }
            }
            AgentCellDestination[] isovistCellDestination = new AgentCellDestination[purged.Count];
            for (i = 0; i < purged.Count; i++)
            {
                isovistCellDestination[i] = new AgentCellDestination(purged[i]);
            }
            purged.Clear();
            purged = null;
            destinations = null;
            simplified.Clear();
            simplified = null;
            keep = null;
             
            /*
            AgentCellDestination[] isovistCellDestination = new AgentCellDestination[simplified.Count];
            int count = 0;
            foreach (var item in simplified)
            {
                isovistCellDestination[count] = new AgentCellDestination(item);
                count++;
            }
             */
            return isovistCellDestination;
        }
        
    }
    /// <summary>
    /// This class includes the Equality logic of cell destinations based on the angle parameter only.
    /// </summary>
    /// <seealso cref="System.Collections.Generic.IEqualityComparer{SpatialAnalysis.IsovistUtility.CellDestination}" />
    class CellDestinationComparerAngleOnly : IEqualityComparer<CellDestination>
    {
        private readonly double _angle;
        public CellDestinationComparerAngleOnly(int angleIntercept)
        {
            this._angle = (2 * Math.PI) / angleIntercept;
        }

        public bool Equals(CellDestination x, CellDestination y)
        {
            int angle_x = this.GetHashCode(x);
            int angle_y = this.GetHashCode(y);
            bool result = angle_x == angle_y;
            return result;
        }

        public int GetHashCode(CellDestination obj)
        {
            CellDestination cellDestination = (CellDestination)obj;
            int hash = (int)(cellDestination.Angle / this._angle);
            return hash;
        }
    }

}

