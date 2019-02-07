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
using SpatialAnalysis.Events;
using SpatialAnalysis.CellularEnvironment;
using SpatialAnalysis.Interoperability;

namespace SpatialAnalysis.Agents.MandatoryScenario
{
    public enum SEQUENCE_PRIORITY_LEVEL
    {
        REGULAR = 0, //Normal Sequences
        PARTIAL = 1, //Left over of a normal sequence which was interrupted y an urgent task
        URGENT  = 2  //Tasks that are urgent and are visually detected 
    }

    /// <summary>
    /// Represents a task as an ordered list of activities 
    /// </summary>
    /// 
    public class Sequence
    {
        public virtual SEQUENCE_PRIORITY_LEVEL PriorityType
        {
            get
            {
                if(this.VisualAwarenessField == null)
                {
                    return SEQUENCE_PRIORITY_LEVEL.REGULAR;
                }
                return SEQUENCE_PRIORITY_LEVEL.URGENT;
            }
        }

        /// <summary>
        /// Gets or sets the visual awareness field from which this task can be visually detected. This property is set to null when the task does not require visual detection.
        /// </summary>
        /// <value>The visual awareness field.</value>
        public VisibilityTarget VisualAwarenessField { get; set; }

        /// <summary>
        /// Gets or sets the time to get visually detected for this task after its activation. This value is valid only if the <c>Sequence</c> has a visual awareness field. 
        /// This value must be set to zero when the sequence loaded for simulation.
        /// </summary>
        /// <value>The time to get visually detected.</value>
        public double TimeToGetVisuallyDetected { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance has visual awareness field.
        /// </summary>
        /// <value><c>true</c> if this instance has visual awareness field; otherwise, <c>false</c>.</value>
        public bool HasVisualAwarenessField { get { return this.VisualAwarenessField != null; } }

        /// <summary>
        /// Gets or sets the activation lambda factor of the exponential distribution.
        /// </summary>
        /// <value>The activation lambda factor.</value>
        public double ActivationLambdaFactor { get; set; }
        /// <summary>
        /// Gets or sets the activity names.
        /// </summary>
        /// <value>The activity names.</value>
        public List<string> ActivityNames { get; set; }

        /// <summary>
        /// Gets the number of activities that this task includes.
        /// </summary>
        /// <value>The activity count.</value>
        public int ActivityCount { get { return this.ActivityNames.Count; } }
        protected string _name;
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name  { get { return _name; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="Sequence"/> class.
        /// </summary>
        /// <param name="activityNames">The activity names.</param>
        /// <param name="name">The name.</param>
        /// <param name="lambdaFactor">The lambda factor.</param>
        public Sequence(IEnumerable<string> activityNames, string name, double lambdaFactor)
        {
            this.ActivityNames = new List<string>(activityNames);
            this._name = name;
            this.ActivationLambdaFactor = lambdaFactor;
            this.TimeToGetVisuallyDetected = 0;
        }
        /// <summary>
        /// This empty constructor is used only for PartialSequence class
        /// </summary>
        protected Sequence()
        {
            this.ActivityNames = new List<string>();
            this._name = string.Empty;
            this.ActivationLambdaFactor = 0;
            this.TimeToGetVisuallyDetected = 0;
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
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            Sequence sequence = obj as Sequence;
            if (sequence==null)
            {
                return false;
            }
            return this.Name == sequence.Name;
        }
        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return string.Format("{0}, {1} Activity", this.Name, this.ActivityNames.Count.ToString());
        }
        /// <summary>
        /// Assigns the visual event to this task to get it visually detectable. 
        /// </summary>
        /// <param name="visualAwarenessEvent">The visual awareness event.</param>
        public void AssignVisualEvent(VisibilityTarget visualAwarenessEvent)
        {
            this.VisualAwarenessField = visualAwarenessEvent;
        }

        public virtual string GetNextActivityName(int index)
        {
            if (index < this.ActivityNames.Count)
            {
                return this.ActivityNames[index];
            }
            throw new ArgumentException(string.Format("Index {0} was out of range of numer of activities in sequence {1}", index.ToString(), this.Name));
        }

        /// <summary>
        /// Gets the string representation of this task.
        /// </summary>
        /// <returns>System.String.</returns>
        public string GetStringRepresentation()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("SEQUENCE");
            sb.AppendLine(this.Name);
            sb.AppendLine(this.ActivationLambdaFactor.ToString());
            foreach (var item in this.ActivityNames)
            {
                sb.Append(item + ",");
            }
            if (this.HasVisualAwarenessField)
            {
                sb.Append("\n");
                sb.Append(this.VisualAwarenessField.SaveAsString());
            }
            string text = sb.ToString();
            sb.Clear();
            sb = null;
            return text;
        }
        /// <summary>
        /// Creates an instance of <c>Sequence</c> from its string representation.
        /// </summary>
        /// <param name="lines">The lines.</param>
        /// <param name="cellularFloor">The cellular floor.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>Sequence.</returns>
        /// <exception cref="System.ArgumentException">
        /// The name is invalid for a sequence!
        /// or
        /// The 'Activation Lambda Factor' is invalid for a sequence!
        /// or
        /// The sequence does not include any activities!
        /// </exception>
        public static Sequence FromStringRepresentation(List<string> lines, Length_Unit_Types unitType, CellularFloor cellularFloor, double tolerance = 0.0000001d )
        {
            if (string.IsNullOrWhiteSpace(lines[0]) || string.IsNullOrEmpty(lines[0]))
            {
                throw new ArgumentException("The name is invalid for a sequence!");
            }
            double lambda = 0;
            if (!double.TryParse(lines[1], out lambda))
            {
                throw new ArgumentException("The 'Activation Lambda Factor' is invalid for a sequence!");
            }
            var activities = lines[2].Split(',');
            var purged = new List<string>();
            foreach (var item in activities)
            {
                if (!(string.IsNullOrWhiteSpace(item) && string.IsNullOrEmpty(item)))
                {
                    purged.Add(item);
                }
            }
            if (purged.Count == 0)
            {
                throw new ArgumentException("The sequence does not include any activities!");
            }
            Sequence sequence = new Sequence(purged, lines[0].Trim(' '), lambda);
            if (lines.Count>3)
            {
                var visualEvent = VisibilityTarget.FromString(lines, 3, unitType, cellularFloor, tolerance);
                sequence.AssignVisualEvent(visualEvent);
            }
            return sequence;
        }

    }

    public class PartialSequence: Sequence
    {
        public override SEQUENCE_PRIORITY_LEVEL PriorityType { get { return SEQUENCE_PRIORITY_LEVEL.PARTIAL; } }
        public Sequence OriginalSequence { get; set; }
        public PartialSequence(): base()
        {
            this._name = "Partial ";
            this.OriginalSequence = null;
        }

        public void Reset()
        {
            this.ActivityNames.Clear();
            this.OriginalSequence = null;
            this._name = "Partial ";
        }

        public void Assign(Sequence sequence, int activityIndex)
        {
            if(sequence.PriorityType == SEQUENCE_PRIORITY_LEVEL.PARTIAL)
            {
                if (PartialSequence.ReferenceEquals(this, sequence))
                {
                    throw new ArgumentException("the same instance mapped o itself!!!");
                }
                throw new ArgumentException("Partial to partial found!!!");

            }
            if (activityIndex < sequence.ActivityNames.Count)
            {
                this.ActivityNames = sequence.ActivityNames.GetRange(activityIndex, sequence.ActivityNames.Count - activityIndex);
                this.OriginalSequence = sequence;
                this._name = "Partial " + sequence.Name;
            }
        }
        public void Trim(int activityIndex)
        {
            this.ActivityNames = this.ActivityNames.GetRange(activityIndex, this.ActivityNames.Count - activityIndex);
        }

        public bool IsEmpty { get { return this.ActivityNames.Count == 0; } }

    }

}

