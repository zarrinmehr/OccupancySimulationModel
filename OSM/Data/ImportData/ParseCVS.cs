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
using System.Windows;
using SpatialAnalysis;
using SpatialAnalysis.FieldUtility.Visualization;
using SpatialAnalysis.FieldUtility;
using SpatialAnalysis.Geometry;
using System.Windows.Interop;
using SpatialAnalysis.CellularEnvironment;
using SpatialAnalysis.Miscellaneous;

namespace SpatialAnalysis.Data.ImportData
{
    /// <summary>
    /// Class UVData.
    /// </summary>
    /// <seealso cref="SpatialAnalysis.Geometry.UV" />
    internal class UVData :UV
    {
        private Dictionary<int, double> iDToDataField;
        /// <summary>
        /// Initializes a new instance of the <see cref="UVData"/> class.
        /// </summary>
        /// <param name="u">The u.</param>
        /// <param name="v">The v.</param>
        public UVData(double u, double v)
            :base(u,v)
        {
            this.iDToDataField = new Dictionary<int, double>();
        }
        /// <summary>
        /// Adds the data.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="dataValue">The data value.</param>
        public void AddData(int id, double dataValue)
        {
            if (!this.iDToDataField.ContainsKey(id))
            {
                this.iDToDataField.Add(id, dataValue);
            }
        }
        /// <summary>
        /// Gets the data.
        /// </summary>
        /// <param name="ID">The identifier.</param>
        /// <returns>System.Double.</returns>
        public double GetData(int ID)
        {
            return this.iDToDataField[ID];
        }
        /// <summary>
        /// Determines whether the specified identifier contains identifier.
        /// </summary>
        /// <param name="ID">The identifier.</param>
        /// <returns><c>true</c> if the specified identifier contains identifier; otherwise, <c>false</c>.</returns>
        public bool ContainsID(int ID)
        {
            return this.iDToDataField.ContainsKey(ID);
        }
        /// <summary>
        /// Gets the quality count.
        /// </summary>
        /// <value>The quality count.</value>
        public int QualityCount
        {
            get { return this.iDToDataField.Count; }
        }
        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="p">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object p)
        {
            return base.Equals(p);
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
            return base.ToString();
        }
    }
    /// <summary>
    /// Class ParseCSV.
    /// </summary>
    internal class ParseCSV
    {
        private Dictionary<string, int> NameToID{ get; set; }
        private Dictionary<int, string> IDToName { get; set; }
        private double X_min{ get; set; }
        private double X_max{ get; set; }
        private double Y_min{ get; set; }
        private double Y_max{ get; set; }
        private double dist_x{ get; set; }
        private double dist_y { get; set; }
        /// <summary>
        /// Gets or sets the data points.
        /// </summary>
        /// <value>The data points.</value>
        public UVData[,] DataPoints { get; set; }
        private int number_X { get; set; }
        private int number_Y { get; set; }
        private string[] inputData { get; set; }
        private OSMDocument _host { get; set; }
        /// <summary>
        /// Gets or sets the importer.
        /// </summary>
        /// <value>The importer.</value>
        public DataImportInterface importer { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ParseCSV"/> is succeed.
        /// </summary>
        /// <value><c>true</c> if succeed; otherwise, <c>false</c>.</value>
        public bool Succeed { get; set; } 
        /// <summary>
        /// Index of input data cell and its weighting factor
        /// </summary>
        public Dictionary<Index, double> IndexToArea { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParseCSV"/> class.
        /// </summary>
        /// <param name="host">The host.</param>
        public ParseCSV(OSMDocument host)
        {
            this._host = host;
            #region Reading data and getting data fields
            if (this.readFile())
            {
                this.importer = new DataImportInterface(this.inputData, this._host.cellularFloor);
                importer.Owner = this._host;
                this.importer.ShowDialog();
                if (importer.X_Index==-1)
                {
                    importer = null;
                    MessageBox.Show("Data was not loaded!");
                    this.Succeed = false;
                    return;
                }
                else
                {
                    this.Succeed = true;
                }
            }
            else
            {
                MessageBox.Show("Data was not loaded!");
                this.Succeed = false;
                return;
            }
            #endregion
            this.IndexToArea = new Dictionary<Index, double>();
            #region load ID map to names
            
            this.IDToName = new Dictionary<int, string>();
            this.NameToID = new Dictionary<string, int>();
            for (int i = 0; i < this.importer.Names.Length; i++)
            {
                if (i!=this.importer.X_Index && i!=this.importer.Y_Index)
                {
                    this.IDToName.Add(i, this.importer.Names[i]);
                    this.NameToID.Add(this.importer.Names[i], i);
                }
            }

            #endregion
            var x_sorted = ParseCSV.GetRange(inputData, this.importer.X_Index, this.importer.Scale);
            var y_sorted = ParseCSV.GetRange(inputData, this.importer.Y_Index, this.importer.Scale);


            this.dist_x = (x_sorted[x_sorted.Length - 1] - x_sorted[0]) / (x_sorted.Length - 1);
            this.dist_y = (y_sorted[y_sorted.Length - 1] - y_sorted[0]) / (y_sorted.Length - 1);
            this.X_min = x_sorted[0];
            this.X_max = x_sorted[x_sorted.Length - 1] + this.dist_x;
            this.Y_min = y_sorted[0];
            this.Y_max = y_sorted[y_sorted.Length - 1] + this.dist_y;
            this.number_X = x_sorted.Length;
            this.number_Y = y_sorted.Length;
            

            this.DataPoints = new UVData[this.number_X, this.number_Y];
            for (int i = 1; i < inputData.Length; i++)
            {
                this.parse(inputData[i]);
            }
            

        }

        private void parse(string data)
        {
            int I = -1, J = -1;
            string[] input;
            try
            {
                input = data.Split(',');
                double x = double.Parse(input[this.importer.X_Index]);
                x /= this.importer.Scale;
                double y = double.Parse(input[this.importer.Y_Index]);
                y /= this.importer.Scale;
                double i = (x - this.X_min) / this.dist_x + .1 * this.dist_x;//added with to account for rounding error
                double j = (y - this.Y_min) / this.dist_y + .1 * this.dist_y;//same here
                I = (int)i;
                J = (int)j;
            }
            catch (Exception)
            {
                return;
            }
            this.DataPoints[I, J] = new UVData(this.X_min + I * this.dist_x, this.Y_min + J * this.dist_y);
            for (int k = 0; k < input.Length; k++)
            {
                if (k!=this.importer.X_Index && k!=this.importer.Y_Index && importer.SelectedIndices.Contains(k))
                {
                    double value;
                    if (double.TryParse(input[k], out value))
                    {
                        this.DataPoints[I, J].AddData(k, value);
                    }
                }
            }
        }

        private Index findIndex(UV p)
        {
            double i = (p.U - this.X_min) / this.dist_x;
            int I = (int)i;
            double j = (p.V - this.Y_min) / this.dist_y;
            int J = (int)j;
            return new Index(I, J);
        }

        /// <summary>
        /// Loads the weighting factors.
        /// </summary>
        /// <param name="cellIndex">Index of the cell.</param>
        public void LoadWeightingFactors(Index cellIndex)
        {
            this.IndexToArea.Clear();
            //find the indices of the cell in input cellular data
            var inputIndex1 = this.findIndex(this._host.cellularFloor.Cells[cellIndex.I, cellIndex.J]);
            var inputIndex2 = this.findIndex(this._host.cellularFloor.Cells[cellIndex.I, cellIndex.J] + new UV(this._host.cellularFloor.CellSize, this._host.cellularFloor.CellSize));
            for (int i = inputIndex1.I; i <= inputIndex2.I; i++)
            {
                for (int j = inputIndex1.J; j <= inputIndex2.J; j++)
                {
                    Index inputIndex = new Index(i, j);
                    double area = OverlappingArea(this._host.cellularFloor, cellIndex, inputIndex);
                    this.IndexToArea.Add(inputIndex, area);
                }
            }

        }
        private double OverlappingArea(CellularFloor cellularFloor, Index cellIndex, Index inputIndex)
        {
            if (inputIndex.I<0 || inputIndex.I>=this.number_X ||inputIndex.J<0 || inputIndex.J>=this.number_Y)
            {
                return 0;
            }
            if (this.DataPoints[inputIndex.I, inputIndex.J] == null)
            {
                return 0;
            }
            if (cellularFloor.FindCell(cellIndex).VisualOverlapState != OverlapState.Outside)
            {
                return 0;
            }
            double u1 = cellularFloor.Cells[cellIndex.I, cellIndex.J].U;
            double u2 = u1 + this._host.cellularFloor.CellSize;
            Interval interval_U = new Interval(u1, u2);
            double x1 = this.DataPoints[inputIndex.I, inputIndex.J].U;
            double x2 = x1 + this.dist_x;
            Interval interval_X = new Interval(x1, x2);
            var interval_A = interval_U.Overlap(interval_X);
            if (interval_A == null)
            {
                return 0;
            }
            double v1 = cellularFloor.Cells[cellIndex.I, cellIndex.J].V;
            double v2 = v1 + this._host.cellularFloor.CellSize;
            Interval interval_V = new Interval(v1, v2);
            double y1 = this.DataPoints[inputIndex.I, inputIndex.J].V;
            double y2 = y1 + this.dist_y;
            Interval interval_Y = new Interval(y1, y2);
            var interval_B = interval_V.Overlap(interval_Y);
            if (interval_B == null)
            {
                return 0;
            }
            return interval_A.Length * interval_B.Length;

            
        }

        private bool readFile()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.Title = "Import data and load it into the grid";
            dlg.DefaultExt = ".CSV";
            dlg.Filter = "Comma Separated Values (.csv)|*.csv";
            Nullable<bool> result = dlg.ShowDialog(this._host);
            string fileAddress = "";
            if (result == true)
            {
                fileAddress = dlg.FileName;
            }
            else
            {
                return false;
            }
            try
            {
                using (System.IO.StreamReader sr = new System.IO.StreamReader(fileAddress))
                {
                    List<string> lines = new List<string>();
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        lines.Add(line);
                    }
                    sr.Close();
                    this.inputData = lines.ToArray();
                    lines.Clear();
                    lines = null;
                }
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Report());
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gets the range.
        /// </summary>
        /// <param name="inputData">The input data.</param>
        /// <param name="index">The index.</param>
        /// <returns>System.Double[].</returns>
        public static double[] GetRange(string[] inputData, int index)
        {
            var values = new HashSet<double>();
            double value;
            for (int i = 1; i < inputData.Length; i++)
            {
                string[] data = inputData[i].Split(',');
                if (double.TryParse(data[index], out value))
                {
                    values.Add(value);
                }
            }
            double[] sortedValues = values.ToArray();
            Array.Sort(sortedValues);
            values.Clear();
            values = null;
            return sortedValues;
        }
        /// <summary>
        /// Gets the range.
        /// </summary>
        /// <param name="inputData">The input data.</param>
        /// <param name="index">The index.</param>
        /// <param name="scale">The scale.</param>
        /// <returns>System.Double[].</returns>
        public static double[] GetRange(string[] inputData, int index, double scale)
        {
            var values = new HashSet<double>();
            double value;
            for (int i = 1; i < inputData.Length; i++)
            {
                string[] data = inputData[i].Split(',');
                if (double.TryParse(data[index], out value))
                {
                    values.Add(value/scale);
                }
            }
            double[] sortedValues = values.ToArray();
            Array.Sort(sortedValues);
            values.Clear();
            values = null;
            return sortedValues;
        }

    }
}

