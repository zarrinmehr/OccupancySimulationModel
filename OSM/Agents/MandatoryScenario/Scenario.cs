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
using SpatialAnalysis.FieldUtility;
using System.Collections;
using MathNet.Numerics.Distributions;

namespace SpatialAnalysis.Agents.MandatoryScenario
{
    /// <summary>
    /// Class Scenario includes sequences and a mechanism for prioritizing them
    /// </summary>
    public class Scenario
    {
        public PartialSequence PartialSequenceToBeCompleted { get; set; }
        private Random _random { get; set; }
        private string _message;
        /// <summary>
        /// Gets the user interface message
        /// </summary>
        /// <value>The message.</value>
        public string Message { get { return _message; } }
        /// <summary>
        /// Gets or sets the names of the main stations.
        /// </summary>
        /// <value>The main stations.</value>
        public HashSet<string> MainStations { get; set; }
        /// <summary>
        /// Gets or sets the sequences
        /// </summary>
        /// <value>The sequences.</value>
        public HashSet<Sequence> Sequences { get; set; }
        /// <summary>
        /// Gets or sets the expected tasks.
        /// </summary>
        /// <value>The expected tasks.</value>
        public SortedDictionary<double, Sequence> ExpectedTasks { get; set; }
        /// <summary>
        /// Gets or sets the unexpected tasks.
        /// </summary>
        /// <value>The unexpected tasks.</value>
        public Dictionary<double, Sequence> UnexpectedTasks { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="Scenario"/> class.
        /// </summary>
        public Scenario()
        {
            this.MainStations = new HashSet<string>();
            this.Sequences = new HashSet<Sequence>();
            this.ExpectedTasks = new SortedDictionary<double, Sequence>();
            this.UnexpectedTasks = new Dictionary<double, Sequence>();
            this._random = new Random(DateTime.Now.Millisecond);
            this.PartialSequenceToBeCompleted = new PartialSequence();
        }
        /// <summary>
        /// Determines whether this scenario is ready for performance.
        /// </summary>
        /// <returns><c>true</c> if the scenario is ready for performance; otherwise, <c>false</c>.</returns>
        public bool IsReadyForPerformance()
        {
            this.PartialSequenceToBeCompleted.Reset();
            if (this.MainStations.Count == 0)
            {
                this._message = "No Destination Set!";
                return false;
            }
            if (this.Sequences.Count == 0)
            {
                this._message = "The scenario does not include any sequences!";
                return false;
            }
            return true;
        }

        /// <summary>
        /// Removes a sequence. If the operation succeeds return true otherwise returns false.
        /// </summary>
        /// <param name="seq">The Sequence</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool RemoveSequence(Sequence seq)
        {
            if (this.Sequences.Contains(seq))
            {
                this.Sequences.Remove(seq);
                if (this.UnexpectedTasks.ContainsValue(seq))
                {
                    var item = this.UnexpectedTasks.First(x => x.Value.Name == seq.Name);
                    this.UnexpectedTasks.Remove(item.Key);
                }
                if (this.ExpectedTasks.ContainsValue(seq))
                {
                    var item = this.ExpectedTasks.First(x => x.Value.Name == seq.Name);
                    this.ExpectedTasks.Remove(item.Key);
                }
                return true;
            }
            return false;
        }


        /// <summary>
        /// This method is called right after the termination of a sequence to put it in use again.
        /// </summary>
        /// <param name="sequence">The sequence.</param>
        /// <param name="startTime">The start time.</param>
        /// <param name="randomizeExpectedSequence">If true the expected sequences will be randomized with a uniform distribution.</param>
        public void ReActivate(Sequence sequence, double startTime, bool randomizeExpectedSequence = false)
        {
            var nextActivationTime = startTime + Exponential.Sample(this._random, 1.0 / sequence.ActivationLambdaFactor);
            if (sequence.HasVisualAwarenessField)
            {
                if (this.UnexpectedTasks.ContainsKey(nextActivationTime))
                {
                    while (this.UnexpectedTasks.ContainsKey(nextActivationTime))
                    {
                        nextActivationTime += 0.0000001d;
                    }
                }
                this.UnexpectedTasks.Add(nextActivationTime, sequence);
            }
            else
            {
                if(randomizeExpectedSequence)
                {
                    nextActivationTime = startTime + this._random.NextDouble() * sequence.ActivationLambdaFactor;
                }
                if (this.ExpectedTasks.ContainsKey(nextActivationTime))
                {
                    while (this.UnexpectedTasks.ContainsKey(nextActivationTime))
                    {
                        nextActivationTime += 0.0000001d;
                    }
                }
                this.ExpectedTasks.Add(nextActivationTime, sequence);
            }
        }

        /// <summary>
        /// Loads the tasks queues and makes the scenario ready for performance.
        /// </summary>
        /// <param name="activities">The activities.</param>
        /// <param name="hours">The hours.</param>
        /// <param name="startTime">The start time.</param>
        public void LoadQueues(Dictionary<string, Activity> activities, double startTime = 0.0d)
        {
            //separating the two type of sequences
            List<Sequence> mainSequences = new List<Sequence>();
            List<Sequence> visualSequence = new List<Sequence>();
            foreach (var item in this.Sequences)
            {
                if (item.HasVisualAwarenessField)
                {
                    visualSequence.Add(item);
                    
                }
                else
                {
                    mainSequences.Add(item);
                }
                //resetting the time visual detection waiting time for all sequences
                item.TimeToGetVisuallyDetected = 0.0;
            }

            //clearing the sequences
            this.ExpectedTasks.Clear();
            this.UnexpectedTasks.Clear();

            foreach (var item in this.Sequences)
            {
                ReActivate(item, startTime,true);
            }
        }
        /// <summary>
        /// Sets the first task in the queue of expected tasks to be triggered at a specific time
        /// This function is called in visualization mode to reduce the waiting time to see the movement of the agnate
        /// It has no effect in a simulation result when the simulation duration is reasonably long
        /// </summary>
        /// <param name="firstTaskActivation">The time when the fist expected task in the queue will be activated</param>
        public void AdjustFirstExpectedTaksActivation(double firstTaskActivation = 3)
        {
            if (this.ExpectedTasks.Count == 0) return;
            double actionTime = this.ExpectedTasks.First().Key;
            double adjustment = firstTaskActivation - actionTime;
            KeyValuePair<double, Sequence>[] tempCopy = new KeyValuePair<double, Sequence>[this.ExpectedTasks.Count];
            int i = 0;
            foreach (KeyValuePair<double, Sequence> item in this.ExpectedTasks)
            {
                tempCopy[i] = new KeyValuePair<double, Sequence>(item.Key + adjustment, item.Value);
                i++;
            }
            this.ExpectedTasks.Clear();
            foreach (var item in tempCopy)
            {
                this.ExpectedTasks.Add(item.Key, item.Value);
            }
            tempCopy = null;
        }
    }
}

