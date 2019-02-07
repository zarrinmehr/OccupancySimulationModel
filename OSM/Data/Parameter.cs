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
using SpatialAnalysis.Optimization;
using System.CodeDom.Compiler;
using SpatialAnalysis.Interoperability;

namespace SpatialAnalysis.Data
{
    /// <summary>
    /// Enum AgentParameters
    /// </summary>
    public enum AgentParameters
    {
        /// <summary>
        /// The isovist external depth
        /// </summary>
        OPT_IsovistExternalDepth = 0,
        /// <summary>
        /// The number of destinations
        /// </summary>
        OPT_NumberOfDestinations = 1,
        /// <summary>
        /// The angle distribution lambda factor
        /// </summary>
        OPT_AngleDistributionLambdaFactor = 2,
        /// <summary>
        /// The desirability distribution lambda factor
        /// </summary>
        OPT_DesirabilityDistributionLambdaFactor = 3,
        /// <summary>
        /// The decision making period lambda factor
        /// </summary>
        OPT_DecisionMakingPeriodLambdaFactor = 4,
        /// <summary>
        /// The velocity magnitude
        /// </summary>
        GEN_VelocityMagnitude = 5,
        /// <summary>
        /// The angular velocity
        /// </summary>
        GEN_AngularVelocity = 6,
        /// <summary>
        /// The body size
        /// </summary>
        GEN_BodySize = 7,
        /// <summary>
        /// The visibility angle
        /// </summary>
        GEN_VisibilityAngle = 8,
        /// <summary>
        /// The barrier repulsion range
        /// </summary>
        GEN_BarrierRepulsionRange = 9,
        /// <summary>
        /// The maximum repulsion
        /// </summary>
        GEN_MaximumRepulsion = 10,
        /// <summary>
        /// The acceleration magnitude
        /// </summary>
        GEN_AccelerationMagnitude = 11,
        /// <summary>
        /// The barrier friction
        /// </summary>
        GEN_BarrierFriction = 12,
        /// <summary>
        /// The agent body elasticity
        /// </summary>
        GEN_AgentBodyElasticity = 13,
        /// <summary>
        /// The angular deviation cost
        /// </summary>
        MAN_AngularDeviationCost = 14,
        //MAN_GAUSSIANNEIGHBORHOODSIZE = 15,
        /// <summary>
        /// The cost of distance
        /// </summary>
        MAN_DistanceCost = 15,
    }
    /// <summary>
    /// Class Parameter.
    /// </summary>
    /// <seealso cref="SpatialAnalysis.Optimization.Variable" />
    public class Parameter: Variable
    {

        private bool _canBeDeleted = true;
        /// <summary>
        /// Gets a value indicating whether this instance can be deleted.
        /// </summary>
        /// <value><c>true</c> if this instance can be deleted; otherwise, <c>false</c>.</value>
        public bool CanBeDeleted { get { return _canBeDeleted; } }

        private readonly string _name;
        /// <summary>
        /// Gets the name of parameter.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get { return _name; } }
        public HashSet<Function> LinkedFunctions { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Parameter"/> class.
        /// </summary>
        /// <param name="name">The name of parameter.</param>
        /// <param name="initialValue">The initial value.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <exception cref="System.ArgumentException">Invalid identifier name for parameter</exception>
        public Parameter(string name, double initialValue, double min, double max)
            : base(initialValue, min, max)
        {
            CodeDomProvider evaluator = CodeDomProvider.CreateProvider("C#");
            if (!evaluator.IsValidIdentifier(name))
            {
                throw new ArgumentException("Invalid identifier name for parameter");
            }
            this._name = name;
            this.LinkedFunctions = new HashSet<Function>();
        }
        public override string ToString()
        {
            return string.Format("Name: {0}, Min: {1}, Max: {2}, Value: {3}", this.Name,
                this.Minimum.ToString(), this.Maximum.ToString(), this.Value.ToString());
        }

        /// <summary>
        /// Copies this parameter with the specified name.
        /// </summary>
        /// <param name="name">The name of  the copied parameter.</param>
        /// <returns>Parameter.</returns>
        public Parameter Copy(string name)
        {
            var param = new Parameter(name, this.Value, this.Minimum, this.Maximum);
            return param;
        }
        /// <summary>
        /// Copies this instance.
        /// </summary>
        /// <returns>Parameter.</returns>
        public new Parameter Copy()
        {
            return new Parameter(this.Name, this.Value, this.Minimum, this.Maximum);
        }

        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            Parameter param = obj as Parameter;
            if (param == null)
            {
                return false;
            }
            return param.Name == this.Name;
        }
        /// <summary>
        /// Creates a readonly parameter.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="initialValue">The initial value.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <returns>Parameter.</returns>
        public static Parameter CreateReadOnly(string name, double initialValue, double min, double max)
        {
            Parameter parameter = new Parameter(name, initialValue, min, max);
            parameter._canBeDeleted = false;
            return parameter;
        }
        public static void LoadDefaultParameters(Length_Unit_Types unitTypeOrigin,Length_Unit_Types unitTypeExpected) {
            UnitConversion cntr = new UnitConversion(unitTypeOrigin, unitTypeExpected);
            DefaultParameters = new Dictionary<AgentParameters, Parameter>
            {
                //{FreeNavigationAgentParameters.OPT_IsovistInternalDepth.ToString(),
                //    Parameter.CreateReadOnly(FreeNavigationAgentParameters.OPT_IsovistInternalDepth.ToString(), 5.0d, 1.0d,5.0d)},
                {AgentParameters.OPT_IsovistExternalDepth,
                    Parameter.CreateReadOnly(AgentParameters.OPT_IsovistExternalDepth.ToString(), cntr.Convert(20.0d,6), cntr.Convert(5.0d,6),cntr.Convert(25.0d,6))},
                {AgentParameters.OPT_NumberOfDestinations,
                    Parameter.CreateReadOnly(AgentParameters.OPT_NumberOfDestinations.ToString(), 100.0d,20.0d,200.0d)},
                {AgentParameters.OPT_AngleDistributionLambdaFactor,
                    Parameter.CreateReadOnly(AgentParameters.OPT_AngleDistributionLambdaFactor.ToString(),2.5d,0.001d,3.0d)},
                {AgentParameters.OPT_DesirabilityDistributionLambdaFactor,
                    Parameter.CreateReadOnly(AgentParameters.OPT_DesirabilityDistributionLambdaFactor.ToString(),1.5d,0.001d,3.0d)},
                {AgentParameters.OPT_DecisionMakingPeriodLambdaFactor,
                    Parameter.CreateReadOnly(AgentParameters.OPT_DecisionMakingPeriodLambdaFactor.ToString(), 0.88d,0.05d,2.0d)},
                {AgentParameters.GEN_VelocityMagnitude,
                    Parameter.CreateReadOnly(AgentParameters.GEN_VelocityMagnitude.ToString(),cntr.Convert(3.8,6),cntr.Convert(2.0d,6),cntr.Convert(5.0d,6))},
                {AgentParameters.GEN_AngularVelocity,
                    Parameter.CreateReadOnly(AgentParameters.GEN_AngularVelocity.ToString(), Math.PI, 0.1d,6.283185d)},
                {AgentParameters.GEN_BodySize,
                    Parameter.CreateReadOnly(AgentParameters.GEN_BodySize.ToString(),cntr.Convert(1.80d,6),cntr.Convert(1.0d,6),cntr.Convert(2.2d,6))},
                {AgentParameters.GEN_VisibilityAngle,
                    Parameter.CreateReadOnly(AgentParameters.GEN_VisibilityAngle.ToString(),160.0d, 0.0d,180.0d)},
                {AgentParameters.GEN_BarrierRepulsionRange,
                    Parameter.CreateReadOnly(AgentParameters.GEN_BarrierRepulsionRange.ToString(), cntr.Convert(1.4d,6),cntr.Convert(1.0d,6),cntr.Convert(5.0d,6))},
                {AgentParameters.GEN_MaximumRepulsion,
                        Parameter.CreateReadOnly(AgentParameters.GEN_MaximumRepulsion.ToString(), cntr.Convert(15.0d,6),cntr.Convert(1.0d,6),cntr.Convert(20.0d,6))},
                {AgentParameters.GEN_AccelerationMagnitude,
                        Parameter.CreateReadOnly(AgentParameters.GEN_AccelerationMagnitude.ToString(), cntr.Convert(15.0d,6),cntr.Convert(5.0d,6),cntr.Convert(20.0d,6))},
                {AgentParameters.GEN_BarrierFriction,
                        Parameter.CreateReadOnly(AgentParameters.GEN_BarrierFriction.ToString(), 0.1d,0.0d,1.0d)},
                {AgentParameters.GEN_AgentBodyElasticity,
                        Parameter.CreateReadOnly(AgentParameters.GEN_AgentBodyElasticity.ToString(), 0.2d,0.0d,1.0d)},
                {AgentParameters.MAN_AngularDeviationCost,
                        Parameter.CreateReadOnly(AgentParameters.MAN_AngularDeviationCost.ToString(), 3.0d,1.0d,7.0d)},
                {AgentParameters.MAN_DistanceCost,
                        Parameter.CreateReadOnly(AgentParameters.MAN_DistanceCost.ToString(),cntr.Convert(1.0d,6),cntr.Convert(0.05d,6),cntr.Convert(2.0d,6))},
                //{AgentParameters.MAN_GAUSSIANNEIGHBORHOODSIZE,
                //        Parameter.CreateReadOnly(AgentParameters.MAN_AngularDeviationCost.ToString(),7,2,20)},
            };
        }
        public static Dictionary<AgentParameters, Parameter> DefaultParameters { get; set; }

    }
}

