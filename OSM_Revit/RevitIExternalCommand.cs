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
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Windows;
using SpatialAnalysis;
using SpatialAnalysis.Miscellaneous;
using System.Reflection;
using SpatialAnalysis.Interoperability;
using System.Windows.Media.Imaging;
using OSM_Revit.Properties;
using System.Drawing;
using OSM_Revit.REVIT_INTEROPERABILITY;

namespace OSM_Revit
{
    /*
        For reference check
        http://help.autodesk.com/view/RVT/2016/ENU/?guid=GUID-01F579CB-AB46-4C00-86E4-D189510D3774
    */
    public class OSM_Revit_Panel : IExternalApplication
    {
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                RibbonPanel OSM_panel = application.CreateRibbonPanel
                    (
                        Tab.AddIns, "Occupancy Simulation Model");
                        string assemblyPath = Assembly.GetExecutingAssembly().Location;
                        PushButtonData buttonData = new PushButtonData("OSM", "OSM", assemblyPath,
                        "OSM_Revit.OSM_FOR_REVIT"
                    );
                PushButton OSM_button = OSM_panel.AddItem(buttonData) as PushButton;
                OSM_button.ToolTip = "Lunch OSM application";

                Bitmap iconOSM = new Bitmap(Resources.OSM_ICON);
                BitmapSource icon = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon
                    (
                        iconOSM.GetHicon(),
                        new Int32Rect(0, 0, iconOSM.Width, iconOSM.Height),
                        BitmapSizeOptions.FromEmptyOptions()
                    );

                OSM_button.LargeImage = icon;
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Report());
                return Result.Failed;
            }

            return Result.Succeeded;
        }
    }
    /// <summary>
    /// Class OSM (OCCUPANCY SIMULATION MODEL) provides implementation for IExternalCommand
    /// </summary>
    /// <seealso cref="Autodesk.Revit.UI.IExternalCommand" />
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    [Journaling(JournalingMode.NoCommandData)]

    public class OSM_FOR_REVIT : IExternalCommand
    {
        /// <summary>
        /// The Revit Document
        /// </summary>
        public static Document RevitDocument;
        /// <summary>
        /// Overload this method to implement and external command within Revit.
        /// </summary>
        /// <param name="commandData">An ExternalCommandData object which contains reference to Application and View
        /// needed by external command.</param>
        /// <param name="message">Error message can be returned by external command. This will be displayed only if the command status
        /// was "Failed".  There is a limit of 1023 characters for this message; strings longer than this will be truncated.</param>
        /// <param name="elements">Element set indicating problem elements to display in the failure dialog.  This will be used
        /// only if the command status was "Failed".</param>
        /// <returns>The result indicates if the execution fails, succeeds, or was canceled by user. If it does not
        /// succeed, Revit will undo any changes made by the external command.</returns>
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            RevitDocument = commandData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = new UIDocument(RevitDocument);

            try
            {
                OSM_ENV_Setting floorSetting = new OSM_ENV_Setting(RevitDocument);
                floorSetting.ShowDialog();

                BIM_To_OSM_Base revit_to_osm = new Revit_To_OSM(RevitDocument, floorSetting.FloorPlan,
                    floorSetting.MinimumHeight, floorSetting.CurveApproximationLength, floorSetting.MinimumCurveLength, floorSetting.DoorIds);
                I_OSM_To_BIM osm_to_Revit = new OSM_To_Revit();

                OSMDocument mainDocument = new OSMDocument(revit_to_osm, osm_to_Revit);
                mainDocument.ShowDialog();
                mainDocument = null;
               
            }
            catch (Exception er)
            {
                string message2 = er.Report();
                MessageBox.Show(message2);
                return Result.Failed;
            }
            return Result.Succeeded;
        }

    }



}
