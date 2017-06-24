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
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace OSM_Revit.REVIT_INTEROPERABILITY
{
    /// <summary>
    /// Class CurveDrawingWarningSwallower.
    /// </summary>
    /// <seealso cref="Autodesk.Revit.DB.IFailuresPreprocessor" />
    public class CurveDrawingWarningSwallower : IFailuresPreprocessor
    {
        /// <summary>
        /// Preprocesses the failures.
        /// </summary>
        /// <param name="a">a.</param>
        /// <returns>FailureProcessingResult.</returns>
        public FailureProcessingResult PreprocessFailures(FailuresAccessor a)
        {
            // inside event handler, get all warnings
            IList<FailureMessageAccessor> failures = a.GetFailureMessages();
            foreach (FailureMessageAccessor f in failures)
            {
                a.DeleteAllWarnings();
            }
            return FailureProcessingResult.Continue;
        }
    }
}
