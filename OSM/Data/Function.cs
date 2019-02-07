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
using SpatialAnalysis.Geometry;
using System.ComponentModel;

namespace SpatialAnalysis.Data
{
    /// <summary>
    /// Delegate CalculateCost
    /// </summary>
    /// <param name="x">The x.</param>
    /// <returns>System.Double.</returns>
    public delegate double CalculateCost(double x);
    //public delegate double CalculateCost(double x, params object[] parameters)
    /// <summary>
    /// Enum CostCalculationMethod
    /// </summary>
    public enum CostCalculationMethod
    {
        RawValue = 0,
        Interpolation = 1,
        WrittenFormula = 2,
        BuiltInRepulsion = 3,
    }
    /// <summary>
    /// This class is the base of spatial data and implements the cost function of the data.
    /// </summary>
    public class Function
    {
        private bool _hasBuiltInRepulsion;

        /// <summary>
        /// Gets a value indicating whether this instance has built in repulsion.
        /// </summary>
        /// <value><c>true</c> if this instance has built in repulsion; otherwise, <c>false</c>.</value>
        public bool HasBuiltInRepulsion
        {
            get { return _hasBuiltInRepulsion; }
        }

        private CalculateCost _defaultMethod { get; set; }
        private string _name;
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get { return _name; } }
        private CostCalculationMethod _method;
        /// <summary>
        /// Gets or sets the type of the cost calculation.
        /// </summary>
        /// <value>The type of the cost calculation.</value>
        public CostCalculationMethod CostCalculationType 
        { 
            get 
            { 
                return _method; 
            }
            set
            {
                _method = value;
            }
        }
        private string _textFormula = "X";

        /// <summary>
        /// Gets or sets the formula as a text.
        /// </summary>
        /// <value>The text formula.</value>
        public string TextFormula
        {
            get { return _textFormula; }
            set { _textFormula = value; }
        }
        /// <summary>
        /// Gets or sets the get cost delegate.
        /// </summary>
        /// <value>The get cost delegate.</value>
        public CalculateCost GetCost { get; set; }
        public bool IncludeInActivityGeneration { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Function"/> class.
        /// </summary>
        /// <param name="name">The name of the function.</param>
        public Function(string name)
        {
            this._name = name;
            this.IncludeInActivityGeneration = false;
            _hasBuiltInRepulsion = false ;
            this.SetRawValue();
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Function"/> class.
        /// </summary>
        /// <param name="name">The name of the function.</param>
        /// <param name="hasDefaultRepulsion">if set to <c>true</c> the function has a default repulsion.</param>
        /// <param name="includeInActivityGeneration">if set to <c>true</c> the function is included in activity generation.</param>
        public Function(string name, bool hasDefaultRepulsion, bool includeInActivityGeneration)
        {
            this._name = name;
            this.IncludeInActivityGeneration = includeInActivityGeneration;
            this._hasBuiltInRepulsion = hasDefaultRepulsion;
            this.SetBuiltInRepulsion();
        }
        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            Function data = obj as Function;
            if (data == null)
            {
                return false;
            }
            return data.Name == this.Name;
        }
        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }
        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return this.Name;
        }
        //https://github.com/pieterderycke/Jace/wiki
        /// <summary>
        /// Sets the string formula.
        /// </summary>
        /// <param name="calculateCost">The calculate cost.</param>
        public void SetStringFormula(CalculateCost calculateCost)
        {
            this.GetCost = null;
            this.GetCost = calculateCost;
            this.CostCalculationType = CostCalculationMethod.WrittenFormula;
        }
        //http://numerics.mathdotnet.com/api/MathNet.Numerics.Interpolation/CubicSpline.htm
        /// <summary>
        /// Sets the interpolation formula.
        /// </summary>
        /// <param name="interpolation">The interpolation.</param>
        public void SetInterpolation(IInterpolation interpolation)
        {
            this.GetCost = null;
            this.GetCost = new CalculateCost(interpolation.Interpolate);
            interpolation = null;
            this.CostCalculationType = CostCalculationMethod.Interpolation;
        }
        //http://numerics.mathdotnet.com/api/MathNet.Numerics.Interpolation/CubicSpline.htm
        /// <summary>
        /// Sets the interpolation formula.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        public void SetInterpolation(IEnumerable<double> x, IEnumerable<double> y)
        {
            this.GetCost = null;
            IInterpolation interpolation = CubicSpline.InterpolateBoundariesSorted(x.ToArray(), y.ToArray(), 
                SplineBoundaryCondition.Natural, 0, SplineBoundaryCondition.Natural, 0);
            this.GetCost = new CalculateCost(interpolation.Interpolate);
            interpolation = null;
            this.CostCalculationType = CostCalculationMethod.Interpolation;
        }
        /// <summary>
        /// Sets the raw value of data as cost.
        /// </summary>
        public void SetRawValue()
        {
            this.CostCalculationType = CostCalculationMethod.RawValue;
            this.GetCost = null;
            this.GetCost = (x) => x;
        }
        /// <summary>
        /// Sets the built in repulsion.
        /// </summary>
        public void SetBuiltInRepulsion()
        {
            this.CostCalculationType = CostCalculationMethod.BuiltInRepulsion;
            this.GetCost = null;
            this.GetCost = Function.FieldStaticCostMethod;
        }
       
        /// <summary>
        /// A Bezier curve with three points will be used to calculate the repulsion.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <returns>System.Double.</returns>
        public static double BezierRepulsionMethod(double x)
        {
            double val = 0.0d;
            if (x==0.0)
            {
                val = Parameter.DefaultParameters[AgentParameters.GEN_MaximumRepulsion].Value;
            }
            else if (x<=Parameter.DefaultParameters[AgentParameters.GEN_BarrierRepulsionRange].Value)
            {
                double a = 1.0d - Math.Sqrt(x / Parameter.DefaultParameters[AgentParameters.GEN_BarrierRepulsionRange].Value);
                val= a * a * Parameter.DefaultParameters[AgentParameters.GEN_MaximumRepulsion].Value;
            }

            return val;
        }
        private static double FieldStaticCostMethod(double x)
        {
            double val = 0.0d;
            double r = Parameter.DefaultParameters[AgentParameters.GEN_BarrierRepulsionRange].Value;
            if (x <= r)
            {
                double m = Parameter.DefaultParameters[AgentParameters.GEN_MaximumRepulsion].Value;
                double a = 2 * m / (r * r);

                if (x < r / 2)
                {
                    val = -a * x * x + m;
                }
                else {
                    val = a * (x - r) * (x - r);
                } 
            }
            
            /*
            //using body size was a mistake that took me a long to to figure out
            if (x <= Parameter.DefaultParameters[AgentParameters.GEN_BodySize].Value)
            {
                double a = 1.0d - Math.Sqrt(x / Parameter.DefaultParameters[AgentParameters.GEN_BodySize].Value);
                val = a * a * Parameter.DefaultParameters[AgentParameters.GEN_MaximumRepulsion].Value;
            }
            */
//            if (x <= Parameter.DefaultParameters[AgentParameters.GEN_BarrierRepulsionRange].Value)
//            {
//                double a = 1.0d - Math.Sqrt(x / Parameter.DefaultParameters[AgentParameters.GEN_BarrierRepulsionRange].Value);
//                val = a * a * Parameter.DefaultParameters[AgentParameters.GEN_MaximumRepulsion].Value;
//            }

            return val;
        }
        /// <summary>
        /// calculates the roots of a quadratics function: Ax^2 + Bx + C = 0.
        /// </summary>
        /// <param name="A">a.</param>
        /// <param name="B">The b.</param>
        /// <param name="C">The c.</param>
        /// <returns>System.Double[].</returns>
        private static double[] QuadraticRoots(double A, double B, double C)
        {
            double delta = B * B - 4 * A * C;
            if (delta < 0)
            {
                return null;
            }
            else if (delta == 0)
            {
                return new double[1] { -B / (2 * A) };
            }
            {
                double sqrt = Math.Sqrt(delta);
                return new double[2] { (-B + sqrt) / (2 * A), (-B - sqrt) / (2 * A) };
            }
        }
    }

}

