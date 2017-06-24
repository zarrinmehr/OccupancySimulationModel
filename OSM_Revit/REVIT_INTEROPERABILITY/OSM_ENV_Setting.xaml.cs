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
using System.Windows;
using Autodesk.Revit.DB;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Controls;
using Autodesk.Revit.UI;
using SpatialAnalysis.Miscellaneous;

namespace OSM_Revit.REVIT_INTEROPERABILITY
{
    /// <summary>
    /// Interaction logic for FloorSetting.xaml
    /// </summary>
    public partial class OSM_ENV_Setting : Window
    {
        #region Hiding the close button
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        //Hiding the close button
        private void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);

        }
        #endregion
        /// <summary>
        /// The door Ids
        /// </summary>
        public HashSet<ElementId> DoorIds;
        /// <summary>
        /// The curve approximation length
        /// </summary>
        public double CurveApproximationLength;
        /// <summary>
        /// The minimum curve length
        /// </summary>
        public double MinimumCurveLength;
        /// <summary>
        /// The floor plan
        /// </summary>
        public ViewPlan FloorPlan;
        /// <summary>
        /// The minimum height
        /// </summary>
        public double MinimumHeight;
        private List<ViewPlan> floorPlanNames;
        private UIDocument uidoc;
        /// <summary>
        /// Initializes a new instance of the <see cref="OSM_ENV_Setting"/> class.
        /// </summary>
        /// <param name="document">The Revit document.</param>
        /// <exception cref="ArgumentException">There are no 'Floor Plans' in this document!</exception>
        public OSM_ENV_Setting(Document document)
        {
            InitializeComponent();

            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            #region Getting the names of the levels that have floors assosiated to them
            FilteredElementCollector floorViewCollector = new FilteredElementCollector(document).OfClass(typeof(ViewPlan));
            floorPlanNames = new List<ViewPlan>();
            foreach (ViewPlan item in floorViewCollector)
            {
                if (item.ViewType == ViewType.FloorPlan && item.IsTemplate == false && item.ViewName != "Site")
                {
                    FilteredElementCollector floorCollector = new FilteredElementCollector(document, item.Id).OfClass(typeof(Floor));
                    bool hasFloor = false;
                    foreach (Floor floor in floorCollector)
                    {
                        hasFloor = true;
                        break;
                    }
                    floorCollector.Dispose();//releasing memory
                    if (hasFloor)
                    {
                        floorPlanNames.Add(item);
                        this.LevelMenu.Items.Add(item.Name);
                    }
                }
            }

            floorViewCollector.Dispose();//releasing memory
            if (this.floorPlanNames.Count == 0)
            {
                throw new ArgumentException("There are no 'Floor Plans' in this document!");
            }
            #endregion
            this.DoorIds = new HashSet<ElementId>();
            this.uidoc = new UIDocument(document);
            this.KeyDown += new System.Windows.Input.KeyEventHandler(FloorSetting_KeyDown);
        }

        void FloorSetting_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                this.settingCompleted();
            }
        }

        private void SettingCompleted_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            this.settingCompleted();
        }

        private void settingCompleted()
        {
            if (!double.TryParse(this.ObstacleSetting.Text, out this.MinimumHeight))
            {
                MessageBox.Show("Enter a valid number for the 'Minimum Height of Visual Obstacles'!\n(Larger than zero)");
                return;
            }
            else if (this.MinimumHeight < 0)
            {
                MessageBox.Show("Enter a valid number for the 'Minimum Height of Visual Obstacles'!\n(Larger than zero)");
                return;
            }
            if (!double.TryParse(this.CurveApproximationLength_.Text, out this.CurveApproximationLength))
            {
                MessageBox.Show("Enter a valid number for 'Curve Approximation Length'!");
                return;
            }
            if (!double.TryParse(this.MinimumCurveLength_.Text, out this.MinimumCurveLength))
            {
                MessageBox.Show("Enter a valid number for 'Minimum Curve Approximation Length'!\n(This length should be smaller than curve approximation length and larger than zero)");
                return;
            }
            else if (this.MinimumCurveLength >= this.CurveApproximationLength || this.MinimumCurveLength <= 0f)
            {
                MessageBox.Show("Enter a valid number for 'Minimum Curve Approximation Length'!\n(This length should be smaller than curve approximation length and larger than zero)");
                return;
            }
            if (this.FloorPlan == null)
	        {
                MessageBox.Show("Select a floor plan to continue!");
                return;
	        }
            using (Transaction t = new Transaction(this.uidoc.Document, "Update View Range"))
            {
                t.Start();
                try
                {
                    PlanViewRange viewRange = this.FloorPlan.GetViewRange();
                    ElementId topClipPlane = viewRange.GetLevelId(PlanViewPlane.TopClipPlane);
                    if (viewRange.GetOffset(PlanViewPlane.TopClipPlane)<this.MinimumHeight)
                    {
                        viewRange.SetOffset(PlanViewPlane.CutPlane, this.MinimumHeight);
                        viewRange.SetOffset(PlanViewPlane.TopClipPlane, this.MinimumHeight);
                    }
                    else
                    {
                        viewRange.SetOffset(PlanViewPlane.CutPlane, this.MinimumHeight);
                    }
                    this.FloorPlan.SetViewRange(viewRange);
                }
                catch (Exception ex)
                {
                    t.Commit();
                    MessageBox.Show(ex.Report());
                }
                t.Commit();
            }
            uidoc.ActiveView = this.FloorPlan;
            this.floorPlanNames = null;
            this.DialogResult = true;
            this.Close();
        }

        private void _setDoors_Click(object sender, RoutedEventArgs e)
        {
            if (this.FloorPlan == null)
            {
                MessageBox.Show("Set a floor plan to proceed!");
                return;
            }
            this.Hide();
            var results = uidoc.Selection.PickObjects(Autodesk.Revit.UI.Selection.ObjectType.Element, new DoorSelectionFilter(this.uidoc.Document), "Select Doors!");
            foreach (var item in results)
            {
                this.DoorIds.Add(item.ElementId);
            }
            this.ShowDialog();
        }

        private void LevelMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.LevelMenu.SelectedIndex != -1)
            {
                try
                {
                    this.FloorPlan = this.floorPlanNames[this.LevelMenu.SelectedIndex];
                    uidoc.ActiveView = this.FloorPlan;
                    this.DoorIds.Clear();
                }
                catch (Exception error1)
                {
                    MessageBox.Show(error1.Report());
                    return;
                }
            }
            else
            {
                this.FloorPlan = null;
            }
        }
    }

    /// <summary>
    /// Filters the selection in Revit UI to instances of Door families only. 
    /// </summary>
    /// <seealso cref="Autodesk.Revit.UI.Selection.ISelectionFilter" />
    internal class DoorSelectionFilter : Autodesk.Revit.UI.Selection.ISelectionFilter
    {
        Document doc = null;
        /// <summary>
        /// Initializes a new instance of the <see cref="DoorSelectionFilter"/> class.
        /// </summary>
        /// <param name="document">The document.</param>
        public DoorSelectionFilter(Document document)
        {
            doc = document;
        }
        /// <summary>
        /// Override this pre-filter method to specify if the element should be permitted to be selected.
        /// </summary>
        /// <param name="elem">A candidate element in selection operation.</param>
        /// <returns>Return true to allow the user to select this candidate element. Return false to prevent selection of this element.</returns>
        /// <remarks><para>If prompting the user to select an element from a Revit Link instance, the element passed here will be the link instance, not the selected linked element.
        /// Access the linked element from Reference passed to the AllowReference() callback of ISelectionFilter.</para>
        /// <para>If an exception is thrown from this method, the element will not be permitted to be selected.</para></remarks>
        public bool AllowElement(Element elem)
        {
            bool select = ((BuiltInCategory)(elem.Category.Id.IntegerValue)) == BuiltInCategory.OST_Doors;
            return select;
        }

        /// <summary>
        /// Override this post-filter method to specify if a reference to a piece of geometry is permitted to be selected.
        /// </summary>
        /// <param name="reference">A candidate reference in selection operation.</param>
        /// <param name="position">The 3D position of the mouse on the candidate reference.</param>
        /// <returns>Return true to allow the user to select this candidate reference. Return false to prevent selection of this candidate.</returns>
        /// <remarks>If an exception is thrown from this method, the element will not be permitted to be selected.</remarks>
        public bool AllowReference(Reference reference, XYZ position)
        {
            bool select = ((BuiltInCategory)(this.doc.GetElement(reference).Category.Id.IntegerValue)) == BuiltInCategory.OST_Doors;
            return select;
        }
    }

}
