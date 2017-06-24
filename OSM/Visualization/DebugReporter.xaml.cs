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
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;

namespace SpatialAnalysis.Visualization
{
    /// <summary>
    /// Interaction logic for DebugReporter.xaml
    /// </summary>
    public partial class DebugReporter : Window
    {
        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            this.Owner.WindowState = this.WindowState;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="DebugReporter"/> class.
        /// This window can be used to see a transcript of processes and can be used for debuging. It offers methods for saving the debug messages in a text file.
        /// </summary>
        public DebugReporter()
        {
            InitializeComponent();
        }
        /// <summary>
        /// Adds an indented debug message to the window.
        /// </summary>
        /// <param name="s">The s.</param>
        public void AddIndentedReport(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return;
            }
            int i = 0;
            using (StringReader sr = new StringReader(s))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (i!=0)
                    {
                        line = "\t" + line;
                    }
                    this.lines.Items.Add(line);
                    i++;
                }//while
            }//using
        }
        /// <summary>
        /// Adds a debug message to the window.
        /// </summary>
        /// <param name="s">The s.</param>
        public void AddReport(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return;
            }
            using (StringReader sr = new StringReader(s))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    this.lines.Items.Add(line);
                }//while
            }//using
        }

        /// <summary>
        /// Clears the debug message.
        /// </summary>
        public void ClearReport()
        {
            this.lines.Items.Clear();
        }
        private void _btn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void _btnSave_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in this.lines.Items)
            {
                try
                {
                    sb.AppendLine((string)item);
                }
                catch (Exception)
                {
                }
            }
            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.DefaultExt = ".txt";
            dlg.Filter = "txt documents (.txt)|*.txt";
            Nullable<bool> result = dlg.ShowDialog(this);
            string fileAddress = "";
            if (result == true)
            {
                fileAddress = dlg.FileName;
            }
            else
            {
                return;
            }
            using(var sr = new System.IO.StreamWriter(fileAddress))
	        {
		        sr.Write(sb.ToString());
                sr.Close();
	        }
            sb.Clear();
            sb=null;
        }
    }
}

