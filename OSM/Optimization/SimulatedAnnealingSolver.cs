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
    /// Delegate FitnessEvaluator which demands implementation where simulated annealing solver is used.
    /// </summary>
    /// <returns>System.Double.</returns>
    public delegate double FitnessEvaluator();
    /// <summary>
    /// Class UIEventArgs.
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class UIEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public double Value { get; set; }
        public UIEventArgs(double value):base()
        {
            this.Value = value;
        }
    }
    /// <summary>
    /// Class SimulatedAnnealingSolver. This class uses Simulated Annealing algorithm for minimizing a the value of a fitness function.
    /// </summary>
    public class SimulatedAnnealingSolver
    {
        /// <summary>
        /// Occurs when ths fitness value updates.
        /// </summary>
        public event EventHandler<UIEventArgs> FitnessUpdated;
        protected virtual void OnFitnessUpdated(UIEventArgs e)
        {
            EventHandler<UIEventArgs> handler = FitnessUpdated;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Occurs when the best fitness value is updated.
        /// </summary>
        public event EventHandler<UIEventArgs> BestFitnessUpdated;
        protected virtual void OnBestFitnessUpdated(UIEventArgs e)
        {
            EventHandler<UIEventArgs> handler = BestFitnessUpdated;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Gets or sets the variables which are the inputs of the annealing process and need to be optimized.
        /// </summary>
        /// <value>The variables.</value>
        public Variable[] Variables { get; set; }
        private Random _randomizer { get; set; }
        private int _selectedVariableIndex { get; set; }
        private double _selectedVariablePreviousValue { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="SimulatedAnnealingSolver"/> class.
        /// The parameters that are passed by reference will be updated
        /// </summary>
        /// <param name="parameters"> a collection of Variable instances</param>
        public SimulatedAnnealingSolver(IEnumerable<Variable> parameters)
        {
            this._randomizer = new Random(DateTime.Now.Millisecond);
            this.Variables = parameters.ToArray();
        }
        private void randomizeValues()
        {
            this._selectedVariableIndex = this._randomizer.Next(this.Variables.Length);
            _selectedVariablePreviousValue = this.Variables[this._selectedVariableIndex].Value;
            this.Variables[this._selectedVariableIndex].Value = 
                this.Variables[this._selectedVariableIndex].Minimum + 
                this._randomizer.NextDouble() * this.Variables[this._selectedVariableIndex].Range;
        }

        /// <summary>
        /// Solves the specified minimum temperature.
        /// </summary>
        /// <param name="minimumTemperature">The minimum temperature.</param>
        /// <param name="maximumTemperature">The maximum temperature.</param>
        /// <param name="iterationCount">The iteration count.</param>
        /// <param name="fitnessEvaluator">The fitness evaluator.</param>
        public void Solve(double minimumTemperature, double maximumTemperature, int iterationCount, FitnessEvaluator fitnessEvaluator)
        {
            double bestFitness = double.PositiveInfinity;
            var bestValues = new double[this.Variables.Length];
            //providing initial value for fitness
            double Fitness = fitnessEvaluator();
            double temperatureIncrement = (maximumTemperature - minimumTemperature) / iterationCount;
            for (int i = 0; i <= iterationCount; i++)
            {
                this.randomizeValues();
                //in addition to evaluating fitness, the FitnessEvaluator delegate can also update interface
                double currentFitness = fitnessEvaluator();
                //updating the best set of variable values
                if (currentFitness < bestFitness)
                {
                    for (int j = 0; j < bestValues.Length; j++)
                    {
                        bestValues[j] = this.Variables[j].Value;
                    }
                    bestFitness = currentFitness;
                    this.OnBestFitnessUpdated(new UIEventArgs(bestFitness));
                }
                //annealing core
                if (currentFitness < Fitness)
                {
                    Fitness = currentFitness;
                    this.OnFitnessUpdated(new UIEventArgs(Fitness));
                }
                else // fitness - currentfitness < 0
                {
                    double temperature = minimumTemperature + (iterationCount - i) * temperatureIncrement;
                    double power = (Fitness - currentFitness) / temperature;
                    double p = Math.Exp(power);
                    double random = this._randomizer.NextDouble();
                    if (p > random)
                    {
                        Fitness = currentFitness;
                        this.OnFitnessUpdated(new UIEventArgs(Fitness));
                    }
                    else //revesring the changes in variable values
                    {
                        this.Variables[this._selectedVariableIndex].Value = this._selectedVariablePreviousValue;
                    }
                }
            }
            //updating the variables with the best set of variables
            for (int i = 0; i < this.Variables.Length; i++)
            {
                this.Variables[i].Value = bestValues[i];
            }
        }
    }
}

