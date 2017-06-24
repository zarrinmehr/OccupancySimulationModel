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
using System.Diagnostics;

namespace SpatialAnalysis.Miscellaneous
{
    /// <summary>
    /// Adds an extension to the System.Exception class 
    /// </summary>
    static public class ExceptionReportExtension
    {
        /// <summary>
        /// Reports the specified exception details if the project is compiled in Debug mode. Otherwise, in release compile mode it will return the standard error message only.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns>System.String.</returns>
        static public string Report(this Exception exception)
        {
            string report = string.Empty;
#if DEBUG
            StackTrace st = new StackTrace(exception, true);
            StackFrame frame = st.GetFrame(0);
            string fileName = frame.GetFileName();
            string methodName = frame.GetMethod().Name;
            int line = frame.GetFileLineNumber();
            report = string.Format("Message:\t\t{0} \nFile Name:\t{1} \nMethod Name:\t{2} \nLine Number:\t{3}",
               exception.Message, fileName, methodName, line.ToString());
#else
            report = exception.Message;
#endif
            return report;
        }
    }
}

