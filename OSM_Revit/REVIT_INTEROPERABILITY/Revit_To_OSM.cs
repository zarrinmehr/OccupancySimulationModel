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
using System.Text;
using Autodesk.Revit.DB;
//Clipper library for boolean operations of the polygons: http://sourceforge.net/projects/polyclipping/?source=navbar
using ClipperLib;
//Custom class NameSpaces
using INTPolygon = System.Collections.Generic.List<ClipperLib.IntPoint>;
using INTPolygons = System.Collections.Generic.List<System.Collections.Generic.List<ClipperLib.IntPoint>>;
using SpatialAnalysis;
using System.Windows;
using SpatialAnalysis.Geometry;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Windows.Media.Media3D;
using SpatialAnalysis.Miscellaneous;
using SpatialAnalysis.Interoperability;

namespace OSM_Revit.REVIT_INTEROPERABILITY
{

    /// <summary>
    /// Class OSM_Environment.
    /// </summary>
    /// <seealso cref="SpatialAnalysis.ExternalConnection.BIM_ReaderBase" /> 
    public class Revit_To_OSM : BIM_To_OSM_Base
    {
        private double revitVisibilityHeight;
        private UnitConversion unitConversion;
        private Options _optionsNoView { get; set; }
        private Options _optionsWithView { get; set; }
        private Document document { get; set; }
        private ViewPlan FloorPlanView { get; set; }
        private INTPolygons _footPrintOfFloorWithHoles { get; set; }
        private INTPolygons _footPrintOfFloorWithOutHoles { get; set; }
        /// <summary>
        /// Gets or sets the doors to be considered as a physical and visual barriers.
        /// </summary>
        /// <value>The doors to include.</value>
        public HashSet<ElementId> DoorsToInclude { get; set; }

        HashSet<BuiltInCategory> excludedCategoriesFromFurnitures = new HashSet<BuiltInCategory>
        {
            BuiltInCategory.OST_CurtainWallMullions,
            BuiltInCategory.OST_Curtain_Systems,
            BuiltInCategory.OST_CurtainGrids,
            BuiltInCategory.OST_CurtainGridsCurtaSystem,
            BuiltInCategory.OST_CurtainGridsRoof,
            BuiltInCategory.OST_CurtainGridsSystem,
            BuiltInCategory.OST_CurtainGridsWall,
            BuiltInCategory.OST_CurtainWallMullions,
            BuiltInCategory.OST_CurtainWallMullionsCut,
            BuiltInCategory.OST_CurtainWallMullionsHiddenLines,
            BuiltInCategory.OST_CurtainWallPanels,
            BuiltInCategory.OST_CurtainWallPanelsHiddenLines,
            BuiltInCategory.OST_CurtainWallPanelTags,
            BuiltInCategory.OST_CurtaSystem,
            BuiltInCategory.OST_CurtaSystemFaceManager,
            BuiltInCategory.OST_CurtaSystemHiddenLines,
            BuiltInCategory.OST_CurtaSystemTags,
            BuiltInCategory.OST_Doors,
            BuiltInCategory.OST_Windows,
            BuiltInCategory.OST_Walls,
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="Revit_To_OSM"/> class.
        /// </summary>
        /// <param name="doc">The Revit document.</param>
        /// <param name="floorPlan">The floor plan View.</param>
        /// <param name="obstacleHeight">Height of the visual obstacles.</param>
        /// <param name="curveApproximationLength">Length of the curve approximation.</param>
        /// <param name="minimumLengthOfLine">The minimum length of line.</param>
        /// <param name="doorsToInclude">The doors to include.</param>
        public Revit_To_OSM(Document doc, ViewPlan floorPlan, double obstacleHeight, double curveApproximationLength, double minimumLengthOfLine, HashSet<ElementId> doorsToInclude, Length_Unit_Types lengthUnitType)
        {
            this.document = doc;
            this.unitType = lengthUnitType;
            this.unitConversion = new UnitConversion(Length_Unit_Types.FEET, this.UnitType);
            this.FloorPlanView = floorPlan;
            this.DoorsToInclude = doorsToInclude;
            this._optionsWithView = this.document.Application.Create.NewGeometryOptions();
            this._optionsWithView.ComputeReferences = true;
            this._optionsWithView.View = this.FloorPlanView;
            this._optionsWithView.IncludeNonVisibleObjects = false;
            this._optionsNoView = this.document.Application.Create.NewGeometryOptions();
            this._optionsNoView.ComputeReferences = true;
            this._optionsNoView.IncludeNonVisibleObjects = false;
            this.VisibilityObstacleHeight = obstacleHeight;
            this.revitVisibilityHeight = UnitConversion.Convert(obstacleHeight, this.UnitType, Length_Unit_Types.FEET);
            this.PlanElevation = unitConversion.Convert(this.FloorPlanView.GenLevel.Elevation);
             
            this.PlanName = this.FloorPlanView.Name;
            this.CurveApproximationLength = curveApproximationLength;
            this.MinimumLengthOfLine = minimumLengthOfLine;
            this.MinimumLineLengthSquared = this.MinimumLengthOfLine * this.MinimumLengthOfLine;
            this.PolygonalBooleanPrecision = 5;
            this.load();
            this.setTerritory();
            this._optionsNoView.Dispose();
            this._optionsWithView.Dispose();

        }


        private List<List<SpatialAnalysis.Geometry.UV>> facesToUVPolygons(List<Face> faces)
        {
            var AllFaceBoundryXYLists = new List<List<SpatialAnalysis.Geometry.UV>>();
            foreach (Face face in faces)
            {
                List<List<SpatialAnalysis.Geometry.UV>> facXYLists = FacePoints(face);
                AllFaceBoundryXYLists.AddRange(facXYLists);
            }
            return AllFaceBoundryXYLists;
        }
        //Getting the footprint of all Furniture
        private void load()
        {
            #region stairs
            #region stairs physical
            List<Face> statirsPhysical = this.getStairsPhysicalShadows();
            var UVPolygonsPhysicalStairs = this.facesToUVPolygons(statirsPhysical);
            foreach (var item in statirsPhysical) item.Dispose();
            statirsPhysical.Clear();
            statirsPhysical = null;
            #endregion
            #region stairs Visual
            List<Face> statirsVisual = this.getStairsVisualBlocks();
            var UVPolygonsVisualStairs = this.facesToUVPolygons(statirsVisual);
            foreach (var item in statirsVisual) item.Dispose();
            statirsVisual.Clear();
            statirsVisual = null;
            #endregion
            #endregion

            #region furniture
            List<Element> furnitureElementIds = getAllFurnitures();
            #region furniture visual
            List<Face> visualFurnituresBlocks = this.getVisualFurnituresBlocksShadows(furnitureElementIds);
            var UVpolyGonsVisualFurnituresBlocks = this.facesToUVPolygons(visualFurnituresBlocks);
            foreach (Face item in visualFurnituresBlocks) item.Dispose();
            visualFurnituresBlocks.Clear();
            visualFurnituresBlocks = null;
            #endregion
            #region furniture physical
            List<Face> physicalBlocks = this.getAllFurnituresShadows(furnitureElementIds);
            var UVpolyGonsPhysicalBlocks = this.facesToUVPolygons(physicalBlocks);
            foreach (Face item in physicalBlocks) item.Dispose();
            physicalBlocks.Clear();
            physicalBlocks = null;
            #endregion
            furnitureElementIds.Clear();
            furnitureElementIds = null;
            #endregion 

            #region curtians and Mullions
            #region curtain Physical Shadows
            List<Face> curtainPhysicalShadows = this.getCurtainSystemPhysicalShadows();
            var UVpolyGonsCurtainPhysicalShadows = this.facesToUVPolygons(curtainPhysicalShadows);
            foreach (Face item in curtainPhysicalShadows) item.Dispose();
            curtainPhysicalShadows.Clear();
            curtainPhysicalShadows = null;
            #endregion
            #region curtain Visual Shadows
            List<Face> curtainVisualShadows = this.curtainSystemVisualShadows();
            var UVpolyGonsCurtainVisualShadows = this.facesToUVPolygons(curtainVisualShadows);
            foreach (Face item in curtainVisualShadows) item.Dispose();
            curtainVisualShadows.Clear();
            curtainVisualShadows = null;
            #endregion
            #endregion

            #region walls
            //finding the Ids of all of the walls on this floor
            List<ElementId> wallsElementIds = findWallsOnFloor();
            #region wall Shadows
            List<Face> wallsShadows = this.getWallsShadows(wallsElementIds);
            var UVpolyGonsWallsShadows = this.facesToUVPolygons(wallsShadows);
            foreach (Face item in wallsShadows) item.Dispose();
            wallsShadows.Clear();
            wallsShadows = null;
            #endregion

            #region Wall top faces
            List<Face> wallsBlocks = this.getWallsVisualblocks(wallsElementIds);
            var UVpolyGonsEallVisualBlocks = this.facesToUVPolygons(wallsBlocks);
            foreach (Face item in wallsBlocks) item.Dispose();
            wallsBlocks.Clear();
            wallsBlocks = null;
            #endregion
            wallsElementIds.Clear();
            wallsElementIds = null;
            #endregion

            #region Visual Barriers
            var visual = new List<List<SpatialAnalysis.Geometry.UV>>();
            visual.AddRange(UVpolyGonsEallVisualBlocks);
            visual.AddRange(UVpolyGonsVisualFurnituresBlocks);
            visual.AddRange(UVpolyGonsCurtainVisualShadows);
            visual.AddRange(UVPolygonsVisualStairs);

            this.FootPrintPolygonsOfVisualBarriers = this.getFootPrintWithOutHoles(visual);
            List<BarrierPolygons> visualBarrierList = new List<BarrierPolygons>();
            for (int i = 0; i < this.FootPrintPolygonsOfVisualBarriers.Count; i++)
            {
                BarrierPolygons brr = this.ConvertINTPolygonToBarrierPolygon(this.FootPrintPolygonsOfVisualBarriers[i]);
                visualBarrierList.Add(brr);
            }
            this.VisualBarriers = visualBarrierList.ToArray();
            visualBarrierList = null;
            #endregion

            #region physical Barriers
            List<List<SpatialAnalysis.Geometry.UV>> physical = new List<List<SpatialAnalysis.Geometry.UV>>();
            physical.AddRange(UVpolyGonsWallsShadows);
            physical.AddRange(UVpolyGonsPhysicalBlocks);
            physical.AddRange(UVpolyGonsCurtainPhysicalShadows);
            physical.AddRange(UVPolygonsPhysicalStairs);
            physical.AddRange(visual);
            
            this.FootPrintPolygonsOfPhysicalBarriers = this.getFootPrintWithOutHoles(physical);
            List<BarrierPolygons> physicalBarrierList = new List<BarrierPolygons>();
            for (int i = 0; i < this.FootPrintPolygonsOfPhysicalBarriers.Count; i++)
            {
                BarrierPolygons brr = this.ConvertINTPolygonToBarrierPolygon(this.FootPrintPolygonsOfPhysicalBarriers[i]);
                physicalBarrierList.Add(brr);
            }
            this.PhysicalBarriers = physicalBarrierList.ToArray();
            physicalBarrierList = null;
            physical.Clear();
            physical = null;
            visual.Clear();
            visual = null;
            #endregion

            #region field
            var floorFaces = this.findFloorFaces();
            var UVPolygonsOfFloor = facesToUVPolygons(floorFaces);
            foreach (Face item in floorFaces) item.Dispose();
            floorFaces.Clear();
            floorFaces = null;
            this.loadFieldWithVoids(UVPolygonsOfFloor);
            this.loadFieldWithOutVoids(UVPolygonsOfFloor);
            UVPolygonsOfFloor.Clear();
            UVPolygonsOfFloor = null;
            //this.Field = this.FieldWithoutHoles;
            #endregion

            #region load all physical, visual barriers and holes in the floor
            this.loadAllBarriers();
            #endregion
            //this.PhysicalBarriers = this.AllBarriers;
        }
        private void loadAllBarriers()
        {
            //get the all of the holes in the floor
            INTPolygons floorDifferences = new INTPolygons();
            Clipper clipper = new Clipper();
            clipper.AddPaths(this._footPrintOfFloorWithHoles, PolyType.ptClip, true);
            clipper.AddPaths(this._footPrintOfFloorWithOutHoles, PolyType.ptSubject, true);
            clipper.Execute(ClipType.ctDifference, floorDifferences);
            //XOR the holes and the physical barriers
            var allPolygons = new INTPolygons();
            allPolygons.AddRange(this.FootPrintPolygonsOfPhysicalBarriers);
            allPolygons.AddRange(floorDifferences);
            INTPolygons union = new INTPolygons();
            clipper.Clear();
            clipper.AddPaths(allPolygons, PolyType.ptSubject, true);
            clipper.Execute(ClipType.ctXor, union, PolyFillType.pftEvenOdd, PolyFillType.pftPositive);
            // union the barriers with the visual barriers to get rid of the small scaps
            INTPolygons unionWithVIsualBarriers = new INTPolygons();
            clipper.Clear();
            clipper.AddPaths(union, PolyType.ptSubject, true);
            clipper.AddPaths(this.FootPrintPolygonsOfVisualBarriers, PolyType.ptSubject, true);
            clipper.Execute(ClipType.ctUnion, unionWithVIsualBarriers, PolyFillType.pftNonZero, PolyFillType.pftNonZero);
            // remove scraps
            this.FootPrintOfAllBarriers = this.SimplifyINTPolygons(unionWithVIsualBarriers, 0.005);
            //releasing memory
            clipper.Clear();
            clipper = null;
            this._footPrintOfFloorWithHoles.Clear();
            this._footPrintOfFloorWithHoles = null;
            this._footPrintOfFloorWithOutHoles.Clear();
            this._footPrintOfFloorWithOutHoles = null;
            allPolygons.Clear();
            allPolygons = null;
            unionWithVIsualBarriers.Clear();
            unionWithVIsualBarriers = null;
        }
        private List<Face> findFloorFaces()
        {
            List<Face> faces = new List<Face>();
            var elemIds = new List<ElementId>();

            using (FilteredElementCollector floorCollectors =
                new FilteredElementCollector(this.document, this.FloorPlanView.Id).OfClass(typeof(Floor)))
            {
                foreach (Floor item in floorCollectors)
                {
                    elemIds.Add(item.Id);
                }
            }
            var geometries = new ParseRevitElements(this._optionsWithView, this.document, elemIds);
            foreach (var solid in geometries.Solids)
            {
                if (solid != null)
                {
                    foreach (Face face in solid.Faces)
                    {
                        PlanarFace pface = face as PlanarFace;
                        if (pface != null && pface.FaceNormal.IsAlmostEqualTo(XYZ.BasisZ))
                        {
                            faces.Add(face);
                        }
                    }
                }
            }
            geometries.Clean();
            geometries = null;
            return faces;
        }
        // finding the voids in the floors
        private void loadFieldWithVoids(List<List<SpatialAnalysis.Geometry.UV>> UVPolygonsOfFloor)
        {
            this._footPrintOfFloorWithHoles = this.getFootPrintWithHoles(UVPolygonsOfFloor);
            //getting the difference of floor ares and barriers
            Clipper c = new Clipper();
            c.AddPaths(_footPrintOfFloorWithHoles, PolyType.ptSubject, true);
            c.AddPaths(this.FootPrintPolygonsOfPhysicalBarriers, PolyType.ptClip, true);
            INTPolygons difference = new INTPolygons();
            c.Execute(ClipType.ctDifference, difference);
            //offstting the polygons internally to get ride of narrow scraps
            this.FootPrintPolygonsOfFieldWithVoids = this.SimplifyINTPolygons(difference, .05);
            List<BarrierPolygons> voids = new List<BarrierPolygons>();
            for (int i = 0; i < this.FootPrintPolygonsOfFieldWithVoids.Count; i++)
            {
                BarrierPolygons brr = this.ConvertINTPolygonToBarrierPolygon(this.FootPrintPolygonsOfFieldWithVoids[i]);
                if (brr.Length > 0)
                {
                    voids.Add(brr);
                }
            }
            this.FieldBarriers = voids.ToArray();
            //releasing memory
            voids = null;
            c = null;
        }
        private void loadFieldWithOutVoids(List<List<SpatialAnalysis.Geometry.UV>> UVPolygonsOfFloor)
        {
            var floorArea = this.getFootPrintWithOutHoles(UVPolygonsOfFloor);
            this._footPrintOfFloorWithOutHoles = floorArea;
            //getting the difference of floor ares and barriers
            Clipper c = new Clipper();
            c.AddPaths(floorArea, PolyType.ptSubject, true);
            c.AddPaths(this.FootPrintPolygonsOfPhysicalBarriers, PolyType.ptClip, true);
            INTPolygons difference = new INTPolygons();
            c.Execute(ClipType.ctDifference, difference);
            //offstting the polygons internally to get ride of narrow scraps
            this.FootPrintPolygonsOfFieldWithOutVoids = this.SimplifyINTPolygons(difference, .1);
            List<BarrierPolygons> voids = new List<BarrierPolygons>();
            for (int i = 0; i < this.FootPrintPolygonsOfFieldWithOutVoids.Count; i++)
            {
                BarrierPolygons brr = this.ConvertINTPolygonToBarrierPolygon(this.FootPrintPolygonsOfFieldWithOutVoids[i]);
                if (brr.Length > 0)
                {
                    voids.Add(brr);
                }
            }
            this.FieldWithoutHoles = voids.ToArray();
            //releasing memory
            voids = null;
            c = null;
            floorArea = null;
        }
        //finding all of the walls
        private List<ElementId> findWallsOnFloor()
        {
            List<ElementId> ids = new List<ElementId>();
            //UIDocument uidoc = new UIDocument(this.document);
            //SelElementSet selElements = uidoc.Selection.Elements;
            //selElements.Clear();
            using (FilteredElementCollector wallCollector = 
                new FilteredElementCollector(this.document, this.FloorPlanView.Id).OfClass(typeof(Wall)))
            {
                foreach (Wall wall in wallCollector)
                {
                    BuiltInCategory elemCategory = (BuiltInCategory)wall.Category.Id.IntegerValue;
                    if (wall.WallType.Kind != WallKind.Curtain)
                    {
                        ids.Add(wall.Id);
                    }
                }
            }
            return ids;
        }
        //search for a face in a solid that has a given normal
        private List<Face> getWallsVisualblocks(List<ElementId> wallsIDsOnFloor)
        {
            List<Face> allFaces = new List<Face>();
            var geometries = new ParseRevitElements(this._optionsWithView, this.document, wallsIDsOnFloor);
            List<Face> topFaces = new List<Face>();
            foreach (Solid solid in geometries.Solids)
            {
                if (null != solid)
                {
                    foreach (Face face in solid.Faces)
                    {
                        if (face.Reference != null && face is PlanarFace)
                        {
                            XYZ pointOnMesh = new XYZ();
                            XYZ norm = new XYZ();
                            try
                            {
                                pointOnMesh = face.Triangulate().get_Triangle(0).get_Vertex(0);
                                norm = face.ComputeNormal(face.Project(pointOnMesh).UVPoint);
                            }
                            catch (Exception)
                            {
                                pointOnMesh = face.Triangulate().get_Triangle(0).get_Vertex(1);
                                norm = face.ComputeNormal(face.Project(pointOnMesh).UVPoint);
                            }
                            if (norm.IsAlmostEqualTo(XYZ.BasisZ) && Math.Abs(pointOnMesh.Z - this.FloorPlanView.GenLevel.Elevation - this.revitVisibilityHeight) < .01)//I am also checking the hight of face
                            {
                                topFaces.Add(face);
                            }
                        }
                    }
                }
            }
            geometries.Clean();
            geometries = null;
            return topFaces;
        }
        //finding the bottom faces of all of the walls on the floor
        private List<Face> getWallsShadows(List<ElementId> wallsIDsOnFloor)
        {
            List<Face> allFaces = new List<Face>();
            var geometries = new ParseRevitElements(this._optionsWithView, this.document, wallsIDsOnFloor);
            foreach (Solid solid in geometries.Solids)
            {
                    if (null != solid)
                    {
                        foreach (Face face in solid.Faces)
                        {
                            PlanarFace pfcae = face as PlanarFace;
                            if (pfcae == null)
                            {
                                allFaces.Add(face);
                            }
                            else if (pfcae.FaceNormal.DotProduct(XYZ.BasisZ) <.001)
                            {
                                allFaces.Add(face);
                            } 
                        }
                    }
            }
            geometries.Clean();
            geometries = null;
            return allFaces;
        }
        //approximating a face with a list of XYZ points
        private List<List<SpatialAnalysis.Geometry.UV>> FacePoints(Face face)
        {
            var allPoints = new List<List<SpatialAnalysis.Geometry.UV>>();
            try
            {
                foreach (EdgeArray edgeArray in face.EdgeLoops)
                {
                    if (edgeArray != null && edgeArray.Size >= 2)
                    {
                        var points = new List<SpatialAnalysis.Geometry.UV>();
                        for (int i = 0; i < edgeArray.Size; i++)
                        {
                            points.AddRange(EdgePoints(edgeArray.get_Item(i), face));
                        }
                        //unit transformation
                        unitConversion.Transform(points);
                        allPoints.Add(points);
                        points = null;
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Report());
            }
            return allPoints;
        }
        //approximating an edge of a face with a list of XYZ points. The last point on the edge is excluded from the list. Also, a line would be approximated with its start and end points only.
        private List<SpatialAnalysis.Geometry.UV> EdgePoints(Edge e, Face face)
        {
            Curve c = e.AsCurveFollowingFace(face);
            var points = new List<SpatialAnalysis.Geometry.UV>();
            if (c is Line)
            {
                points.Add(new SpatialAnalysis.Geometry.UV(c.GetEndPoint(0).X, c.GetEndPoint(0).Y));
                return points;
            }
            else
            {
                double p0 = c.GetEndParameter(0);
                double p1 = c.GetEndParameter(1);
                double length = c.Length;
                int n = (int)(Math.Floor(length / this.CurveApproximationLength));
                if (n < 2)
                {
                    n = 2;
                }
                for (int i = 0; i < n; i++)
                {
                    XYZ xyz = c.Evaluate(p0 + i * (p1 - p0) / n, false);
                    points.Add(new SpatialAnalysis.Geometry.UV(xyz.X, xyz.Y));
                }
                return points;
            }
        }
        //finding the shadows of all of the furniture on the ground when they block view
        private List<Face> getVisualFurnituresBlocksShadows(List<Element> elements)
        {
            double visibleHeight = this.FloorPlanView.GenLevel.Elevation + this.revitVisibilityHeight;
            List<Face> faces = new List<Face>();
            using (Plane p = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ(0, 0, this.FloorPlanView.GenLevel.Elevation - 10)))
            {
                ParseRevitElements geometries = new ParseRevitElements(this._optionsNoView, elements);
                foreach (Solid solid in geometries.Solids)
                {
                    if (null != solid && solid.Faces.Size > 0)
                    {
                        double zmin = double.PositiveInfinity;
                        double zmax = double.NegativeInfinity;
                        foreach (Edge edge in solid.Edges)
                        {
                            foreach (var pnt in edge.Tessellate())
                            {
                                if (zmin > pnt.Z)
                                {
                                    zmin = pnt.Z;
                                }
                                if (zmax < pnt.Z)
                                {
                                    zmax = pnt.Z;
                                }
                            }
                        }
                        if (zmax >= visibleHeight - .1 && zmin <= visibleHeight + .1)
                        {
                            try
                            {
                                ExtrusionAnalyzer extrusionAnalyzer = null;
                                extrusionAnalyzer = ExtrusionAnalyzer.Create(solid, p, -1 * XYZ.BasisZ);
                                Face face = extrusionAnalyzer.GetExtrusionBase();
                                if (face != null)
                                {
                                    faces.Add(face);
                                }
                            }
                            catch (Exception ) { }
                        }
                    }
                }
                geometries.Clean();
                geometries = null;
            }
            return faces;
        }
        //finding the shadows of all of the furniture on the ground
        private List<Face> getAllFurnituresShadows(List<Element> elements)
        {
            double visibleHeight = this.FloorPlanView.GenLevel.Elevation + this.revitVisibilityHeight;
            List<Face> faces = new List<Face>();
            using (Plane p =  Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ(0, 0, this.FloorPlanView.GenLevel.Elevation - 10)))
            {
                ParseRevitElements geometries = new ParseRevitElements(this._optionsNoView, elements);
                foreach (Solid solid in geometries.Solids)
                {
                    if (null != solid && solid.Faces.Size > 0)
                    {
                        double zmax = double.NegativeInfinity;
                        foreach (Edge edge in solid.Edges)
                        {
                            foreach (var pnt in edge.Tessellate())
                            {
                                if (zmax < pnt.Z)
                                {
                                    zmax = pnt.Z;
                                }
                            }
                        }
                        if (zmax <= visibleHeight + .1)
                        {
                            try
                            {
                                ExtrusionAnalyzer extrusionAnalyzer = null;
                                extrusionAnalyzer = ExtrusionAnalyzer.Create(solid, p, -1 * XYZ.BasisZ);
                                Face face = extrusionAnalyzer.GetExtrusionBase();
                                if (face != null)
                                {
                                    faces.Add(face);
                                }
                            }
                            catch (Exception )
                            {
                                //MessageBox.Show("Failed to create face frome extrusion\n\t" + e0.Message);
                            }
                        }
                    }
                }
                geometries.Clean();
                geometries = null;
            }
            return faces;
        }
        private List<Element> getAllFurnitures()
        {
            List<Element> elementIds = new List<Element>();
            using (FilteredElementCollector familyInstances =
                new FilteredElementCollector(document, this.FloorPlanView.Id).OfClass(typeof(FamilyInstance)))
            {
                foreach (FamilyInstance item in familyInstances)
                {
                    BuiltInCategory elemCategory = (BuiltInCategory)item.Category.Id.IntegerValue;
                    if (!excludedCategoriesFromFurnitures.Contains(elemCategory))
                    {
                        try
                        {
                            elementIds.Add(item as Element);
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show("Failed to cast a familyinstance to an element:\n" + e.Report());
                        }
                    }
                }
            }
            using (FilteredElementCollector stairRailings = new FilteredElementCollector(this.document, this.FloorPlanView.Id).OfCategory(BuiltInCategory.OST_StairsRailing))
            {
                foreach (Element item in stairRailings)
                {
                    BuiltInCategory elemCategory = (BuiltInCategory)item.Category.Id.IntegerValue;
                    if (!excludedCategoriesFromFurnitures.Contains(elemCategory))
                    {
                        try
                        {
                            elementIds.Add(item as Element);
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show("Failed to cast a stairRailing to an element:\n" + e.Report());
                        }
                    }
                }
            }
            foreach (var item in this.DoorsToInclude)
            {
                elementIds.Add(this.document.GetElement(item));
            }
            return elementIds;

        }
        //finding the shadows of all of the furniture on the ground
        private List<Face> getCurtainSystemPhysicalShadows()
        {
            double visibleHeight = this.FloorPlanView.GenLevel.Elevation + this.revitVisibilityHeight;
            List<Element> collection = new List<Element>();
            using (FilteredElementCollector curtainWallPanels = 
                new FilteredElementCollector(document, this.FloorPlanView.Id).OfCategory(BuiltInCategory.OST_CurtainWallPanels))
            {
                foreach (Element item in curtainWallPanels)
                {
                    collection.Add(item);
                }
            }
            using (FilteredElementCollector curtainWallMullions = 
                new FilteredElementCollector(document, this.FloorPlanView.Id).OfCategory(BuiltInCategory.OST_CurtainWallMullions))
            {
                foreach (Element item in curtainWallMullions)
                {
                    collection.Add(item);
                }
            }
            List<Face> faces = new List<Face>();
            using (Plane p = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ(0, 0, this.FloorPlanView.GenLevel.Elevation - 10)))
            {
                ParseRevitElements geometry = new ParseRevitElements(this._optionsWithView, collection);
                foreach (Solid solid in geometry.Solids)
                {
                    if (null != solid && solid.Faces.Size > 0)
                    {
                        double zmin = double.PositiveInfinity;
                        foreach (Edge edge in solid.Edges)
                        {
                            foreach (var pnt in edge.Tessellate())
                            {
                                if (zmin > pnt.Z)
                                {
                                    zmin = pnt.Z;
                                }
                            }
                        }
                        if (zmin <= visibleHeight)
                        {
                            try
                            {
                                ExtrusionAnalyzer extrusionAnalyzer = null;
                                extrusionAnalyzer = ExtrusionAnalyzer.Create(solid, p, -1 * XYZ.BasisZ);
                                Face face = extrusionAnalyzer.GetExtrusionBase();
                                if (face != null)
                                {
                                    faces.Add(face);
                                }
                            }
                            catch (Exception) { }
                        }
                    }
                }
                geometry.Clean();
                geometry = null;
            }
            return faces;
        }
        //finding the shadows of all of the furniture on the ground
        private List<Face> curtainSystemVisualShadows()
        {
            double visibleHeight = this.FloorPlanView.GenLevel.Elevation + this.revitVisibilityHeight;
            List<Element> collection = new List<Element>();
            using (FilteredElementCollector curtainWallMullions =
                new FilteredElementCollector(document, this.FloorPlanView.Id).OfCategory(BuiltInCategory.OST_CurtainWallMullions))
            {
                foreach (Element item in curtainWallMullions)
                {
                    collection.Add(item);
                }
            }
            List<Face> faces = new List<Face>();
            using (Plane p = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ(0, 0, this.FloorPlanView.GenLevel.Elevation - 10)))
            {
                ParseRevitElements geometry = new ParseRevitElements(this._optionsWithView, collection);
                foreach (Solid solid in geometry.Solids)
                {
                    if (solid.Faces.Size>0)
                    {
                        foreach (Face face in solid.Faces)
                        {
                            PlanarFace pface = face as PlanarFace;
                            if (pface != null)
                            {
                                if (pface.FaceNormal.IsAlmostEqualTo(XYZ.BasisZ))
                                {
                                    if (Math.Abs(pface.Origin.Z -visibleHeight)<.01)
                                    {
                                        faces.Add(face);
                                    }
                                }
                            }
                        }
                    }
                }
                geometry.Clean();
                geometry = null;
            }
            return faces;
        }
        //Getting the footprint of a list of faces
        private List<Face> getStairsPhysicalShadows()
        {
            double visibleHeight = this.FloorPlanView.GenLevel.Elevation + this.revitVisibilityHeight;
            FilteredElementCollector collector = new FilteredElementCollector(this.document, this.FloorPlanView.Id).OfCategory(BuiltInCategory.OST_Stairs);
            List<Element> elems = new List<Element>();
            foreach (Element item in collector)
            {
                BuiltInCategory elemCategory = (BuiltInCategory)item.Category.Id.IntegerValue;
                if (elemCategory == BuiltInCategory.OST_Stairs)
                {
                    elems.Add(item);
                }
            }
            List<Face> faces = new List<Face>();
            using (Plane p = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ(0, 0, this.FloorPlanView.GenLevel.Elevation - 10)))
            {
                ParseRevitElements geometry = new ParseRevitElements(this._optionsWithView, elems);
                foreach (Solid solid in geometry.Solids)
                {
                    if (null != solid && solid.Faces.Size > 0)
                    {
                        foreach (Face face in solid.Faces)
                        {
                            PlanarFace pFace = face as PlanarFace;
                            if (pFace != null)
                            {
                                if (pFace.FaceNormal.DotProduct(XYZ.BasisZ)!=0)
                                {
                                    double zmin = double.PositiveInfinity;
                                    double zmax = double.NegativeInfinity;
                                    foreach (EdgeArray edgeArray in face.EdgeLoops)
                                    {
                                        foreach (Edge edge in edgeArray)
                                        {
                                            foreach (var pnt in edge.Tessellate())
                                            {
                                                if (zmin > pnt.Z)
                                                {
                                                    zmin = pnt.Z;
                                                }
                                                if (zmax < pnt.Z)
                                                {
                                                    zmax = pnt.Z;
                                                }
                                            }
                                        }
                                    }
                                    if (zmin >= this.FloorPlanView.GenLevel.Elevation - .01 && zmax <= visibleHeight + .01)
                                    {
                                        faces.Add(face);
                                    }
                                }
                            }
                            else
                            {
                                double zmin = double.PositiveInfinity;
                                double zmax = double.NegativeInfinity;
                                foreach (EdgeArray edgeArray in face.EdgeLoops)
                                {
                                    foreach (Edge edge in edgeArray)
                                    {
                                        foreach (var pnt in edge.Tessellate())
                                        {
                                            if (zmin > pnt.Z)
                                            {
                                                zmin = pnt.Z;
                                            }
                                            if (zmax < pnt.Z)
                                            {
                                                zmax = pnt.Z;
                                            }
                                        }
                                    }
                                }
                                if (zmin >= this.FloorPlanView.GenLevel.Elevation - .01 && zmax <= visibleHeight + .01)
                                {
                                    faces.Add(face);
                                }
                            }
                        }
                    }
                }
                geometry.Clean();
                geometry = null;
            }
            return faces;
        }
        private List<Face> getStairsVisualBlocks()
        {
            double visibleHeight = this.FloorPlanView.GenLevel.Elevation + this.revitVisibilityHeight;
            FilteredElementCollector collector = new FilteredElementCollector(this.document, this.FloorPlanView.Id).OfCategory(BuiltInCategory.OST_Stairs);
            List<Element> elems = new List<Element>();
            foreach (Element item in collector)
            {
                BuiltInCategory elemCategory = (BuiltInCategory)item.Category.Id.IntegerValue;
                if (elemCategory == BuiltInCategory.OST_Stairs)
                {
                    elems.Add(item);
                }
            }
            List<Face> faces = new List<Face>();
            using (Plane p = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ(0, 0, this.FloorPlanView.GenLevel.Elevation - 10)))
            {
                ParseRevitElements geometry = new ParseRevitElements(this._optionsWithView, elems);
                foreach (Solid solid in geometry.Solids)
                {
                    if (solid != null && solid.Faces.Size>0)
                    {
                        foreach (Face face in solid.Faces)
                        {
                            PlanarFace pFace = face as PlanarFace;
                            if (pFace != null)
                            {
                                if (pFace.FaceNormal.IsAlmostEqualTo(XYZ.BasisZ) && Math.Abs(pFace.Origin.Z-visibleHeight)<.01)
                                {
                                    faces.Add(face);
                                }
                            }
                        }
                    }
                }
                geometry.Clean();
                geometry = null;
            }
            return faces;
        }
        //gets the footprints of polygons and removes the holes
        private INTPolygons getFootPrintWithOutHoles(List<List<SpatialAnalysis.Geometry.UV>> UVPolygons)
        {
            double expandShrinkFactor = Math.Pow(10.0, this.PolygonalBooleanPrecision) * UnitConversion.Convert(0.05, Length_Unit_Types.FEET, UnitType);
            //expanding polygons to resolve tangant edges
            INTPolygons offsetedPolygons = new INTPolygons();
            ClipperOffset clipperOffset = new ClipperOffset();
            foreach (List<SpatialAnalysis.Geometry.UV> list in UVPolygons)
            {
                try
                {
                    INTPolygon ply = new INTPolygon();
                    ply = this.ConvertUVListToINTPolygon(list);
                    clipperOffset.AddPath(ply, ClipperLib.JoinType.jtMiter, EndType.etClosedPolygon);
                    INTPolygons plygns = new INTPolygons();
                    clipperOffset.Execute(ref plygns, expandShrinkFactor);
                    offsetedPolygons.AddRange(plygns);
                    clipperOffset.Clear();
                }
                catch (Exception)
                {
                }
            }
            //Unioning the contours
            Clipper c = new Clipper();
            c.AddPaths(offsetedPolygons, PolyType.ptSubject, true);
            INTPolygons results = new INTPolygons();
            c.Execute(ClipType.ctUnion, results, PolyFillType.pftNonZero, PolyFillType.pftNonZero);
            //shrinking the polygons
            INTPolygons solution = new INTPolygons();
            clipperOffset.AddPaths(results, ClipperLib.JoinType.jtMiter, EndType.etClosedPolygon);
            clipperOffset.Execute(ref solution, -expandShrinkFactor);
            results = null;
            c = null;
            offsetedPolygons = null;
            clipperOffset = null;
            return solution;
        }
        //gets the footprints of polygons and keeps the holes
        private INTPolygons getFootPrintWithHoles(List<List<SpatialAnalysis.Geometry.UV>> UVPolygons)
        {
            //expanding polygons to resolve tangant edges
            INTPolygons polygons = new INTPolygons();
            foreach (List<SpatialAnalysis.Geometry.UV> list in UVPolygons)
            {
                try
                {
                    var ply = ConvertUVListToINTPolygon(list);
                    polygons.Add(ply);

                }
                catch (Exception)
                {
                }
            }
            //Unioning the contours
            Clipper c = new Clipper();
            c.AddPaths(polygons, PolyType.ptSubject, true);
            INTPolygons results = new INTPolygons();
            c.Execute(ClipType.ctXor, results, PolyFillType.pftEvenOdd, PolyFillType.pftNegative);
            //shrinking the polygons
            INTPolygons solution = this.SimplifyINTPolygons(results, -0.1);
            results = null;
            c = null;
            polygons = null;
            return solution;
        }
        //create a contour by offseting the barriers to a given value at a given height
        #region Parse Models to WPF MeshGeometry3D
        private bool _boundingBoxIntersect(BoundingBoxXYZ boundXYZ, Rect3D rec3d_Teritory)
        {
            bool result = false;
            if (boundXYZ != null)
            {
                Point3D _location = new Point3D(boundXYZ.Min.X, boundXYZ.Min.Y, boundXYZ.Min.Z);
                XYZ difference = boundXYZ.Max - boundXYZ.Min;
                Size3D _size = new Size3D(difference.X, difference.Y, difference.Z);
                Rect3D rec3D = new Rect3D(_location, _size);
                result= rec3d_Teritory.IntersectsWith(rec3D);
            }
            return result;
        }
        public override List<object> GetSlicedMeshGeometries(SpatialAnalysis.Geometry.UV min, SpatialAnalysis.Geometry.UV max, double offset)
        {
            offset = UnitConversion.Convert(offset, UnitType, Length_Unit_Types.FEET);
            List<object> _geometryModel3Ds = new List<object>();
            Transform transform = Transform.Identity;
            var MaxMin = new SpatialAnalysis.Geometry.UV[2] { max.Copy(), min.Copy() };
            UnitConversion.Transform(MaxMin, unitType, Length_Unit_Types.FEET);
            var diagnal = MaxMin[0] - MaxMin[1];
            Size3D size = new Size3D(diagnal.U + 2*offset, diagnal.V + 2*offset, this.revitVisibilityHeight);
            Point3D location = new Point3D(MaxMin[1].U - offset, MaxMin[1].V - offset, this.FloorPlanView.GenLevel.Elevation);
            Rect3D rec3d_Teritory = new Rect3D(location, size);
            transform.Origin = new XYZ(0, 0, this.FloorPlanView.GenLevel.Elevation);
            var inverseTransform = transform.Inverse;
            List<Element> elementsWithViewCut = new List<Element>();
            List<Element> elementsWithoutViewCut = new List<Element>();
            #region floors
            using (FilteredElementCollector floorCollectors =
                new FilteredElementCollector(this.document, this.FloorPlanView.Id).OfClass(typeof(Floor)))
            {
                foreach (Element item in floorCollectors)
                {
                    if (!item.IsHidden(this.FloorPlanView))
                    {
                        using (BoundingBoxXYZ boundXYZ = item.get_BoundingBox(this.FloorPlanView))
                        {
                            if (this._boundingBoxIntersect(boundXYZ, rec3d_Teritory))
                            {
                                elementsWithViewCut.Add(item);
                            }
                        }
                    }
                }
            }
            #endregion

            #region Walls
            using (FilteredElementCollector wallCollector =
                new FilteredElementCollector(this.document, this.FloorPlanView.Id).OfClass(typeof(Wall)))
            {
                foreach (Wall wall in wallCollector)
                {
                    BuiltInCategory elemCategory = (BuiltInCategory)wall.Category.Id.IntegerValue;
                    if (!wall.IsHidden(this.FloorPlanView))
                    {
                        using (BoundingBoxXYZ boundXYZ = wall.get_BoundingBox(this.FloorPlanView))
                        {
                            if (wall.WallType.Kind != WallKind.Curtain)
                            {
                                if (this._boundingBoxIntersect(boundXYZ, rec3d_Teritory))
                                {
                                    elementsWithViewCut.Add(wall);
                                }
                            }
                        }
                    }
                }
            }
            #endregion

            #region FamilyInstance
            using (FilteredElementCollector allfamilyInstances =
                new FilteredElementCollector(document, this.FloorPlanView.Id).OfClass(typeof(FamilyInstance)))
            {
                foreach (Element item in allfamilyInstances)
                {
                    if (!item.IsHidden(this.FloorPlanView))
                    {
                        using (BoundingBoxXYZ boundXYZ = item.get_BoundingBox(this.FloorPlanView))
                        {
                            if (this._boundingBoxIntersect(boundXYZ, rec3d_Teritory))
                            {
                                BuiltInCategory category = (BuiltInCategory)item.Category.Id.IntegerValue;
                                if (!this.excludedCategoriesFromFurnitures.Contains(category) || category == BuiltInCategory.OST_Windows)
                                {
                                    elementsWithoutViewCut.Add(item);
                                }
                            }
                        }
                    }
                }
            }
            #endregion

            #region Stairs Railing
            using (FilteredElementCollector stairRailings = new FilteredElementCollector(this.document, this.FloorPlanView.Id).OfCategory(BuiltInCategory.OST_StairsRailing))
            {
                foreach (Element item in stairRailings)
                {
                    if (!item.IsHidden(this.FloorPlanView))
                    {
                        using (BoundingBoxXYZ boundXYZ = item.get_BoundingBox(this.FloorPlanView))
                        {
                            if (this._boundingBoxIntersect(boundXYZ, rec3d_Teritory))
                            {
                                BuiltInCategory category = (BuiltInCategory)item.Category.Id.IntegerValue;
                                if (!this.excludedCategoriesFromFurnitures.Contains(category))
                                {
                                    elementsWithViewCut.Add(item);
                                }
                            }
                        }
                    }
                }
            }
            #endregion
            #region Curtain Wall Panels
            /* */
            using (FilteredElementCollector curtainWallPanels =
                new FilteredElementCollector(document, this.FloorPlanView.Id).OfCategory(BuiltInCategory.OST_CurtainWallPanels))
            {
                foreach (Element item in curtainWallPanels)
                {
                    if (!item.IsHidden(this.FloorPlanView))
                    {
                        using (BoundingBoxXYZ boundXYZ = item.get_BoundingBox(this.FloorPlanView))
                        {
                            if (this._boundingBoxIntersect(boundXYZ, rec3d_Teritory))
                            {
                                elementsWithViewCut.Add(item);
                                
                            }
                        }
                    }
                }
            }
            #endregion

            #region Curtain Wall Mullions
            using (FilteredElementCollector curtainWallMullions =
                new FilteredElementCollector(document, this.FloorPlanView.Id).OfCategory(BuiltInCategory.OST_CurtainWallMullions))
            {
                foreach (Element item in curtainWallMullions)
                {
                    if (!item.IsHidden(this.FloorPlanView))
                    {
                        using (BoundingBoxXYZ boundXYZ = item.get_BoundingBox(this.FloorPlanView))
                        {
                            if (this._boundingBoxIntersect(boundXYZ, rec3d_Teritory))
                            {
                                elementsWithViewCut.Add(item);
                            }
                        }
                    }
                }
            }
            #endregion

            #region Stairs
            using (FilteredElementCollector stairsCollector =
                new FilteredElementCollector(this.document, this.FloorPlanView.Id).OfCategory(BuiltInCategory.OST_Stairs))
            {
                foreach (Element item in stairsCollector)
                {
                    if (!item.IsHidden(this.FloorPlanView))
                    {
                        using (BoundingBoxXYZ boundXYZ = item.get_BoundingBox(this.FloorPlanView))
                        {
                            if (this._boundingBoxIntersect(boundXYZ, rec3d_Teritory))
                            {
                                //elementsWithoutViewCut.Add(item);
                            }
                        }
                    }
                }
            }
            #endregion

            #region doors
            foreach (var item in this.DoorsToInclude)
            {
                elementsWithoutViewCut.Add(this.document.GetElement(item));
            }
            #endregion

            //updating the territory box
            location.Z = 0;
            rec3d_Teritory = new Rect3D(location, size);

            using (Options options = this.document.Application.Create.NewGeometryOptions())
            {
                //unit scaling
                double unitScale = UnitConversion.ConvertScale(Length_Unit_Types.FEET, this.UnitType);
                ScaleTransform3D transform3d = new ScaleTransform3D(unitScale, unitScale, unitScale);
                #region for familyInstances
                options.ComputeReferences = true;
                options.IncludeNonVisibleObjects = false;
                ExtractGeometryModel3D geoms = new ExtractGeometryModel3D(this.document, options, elementsWithoutViewCut, inverseTransform, rec3d_Teritory);
                //unit scaling
                foreach (var item in geoms.WPFGeometries) item.Transform = transform3d;
                _geometryModel3Ds.AddRange(geoms.WPFGeometries);
                geoms.Clean();
                geoms = null;
                #endregion
                #region not for familyInstances
                options.View = this.FloorPlanView;
                geoms = new ExtractGeometryModel3D(this.document,options, elementsWithViewCut, inverseTransform, rec3d_Teritory);
                foreach (var item in geoms.WPFGeometries) item.Transform = transform3d;
                _geometryModel3Ds.AddRange(geoms.WPFGeometries);
                geoms.ShowParsingReport();
                geoms.Clean();
                geoms = null;
                ExtractGeometryModel3D.ResetReport();
                #endregion
            }
            foreach (var item in elementsWithViewCut)item.Dispose();
            elementsWithViewCut.Clear();
            elementsWithViewCut = null;
            foreach (var item in elementsWithoutViewCut) item.Dispose();
            elementsWithoutViewCut.Clear();
            elementsWithoutViewCut = null;
            return _geometryModel3Ds;
        }
        public override List<object> ParseBIM(SpatialAnalysis.Geometry.UV min, SpatialAnalysis.Geometry.UV max, double offset)
        {
            offset = UnitConversion.Convert(offset, UnitType, Length_Unit_Types.FEET);
            List<object> _geometryModel3Ds = new List<object>();
            Transform transform = Transform.Identity;
            var MaxMin = new SpatialAnalysis.Geometry.UV[2] { max.Copy(), min.Copy() };
            UnitConversion.Transform(MaxMin, unitType, Length_Unit_Types.FEET);
            var diagnal = MaxMin[0] - MaxMin[1];
            Size3D size = new Size3D(diagnal.U + 2 * offset, diagnal.V + 2 * offset, this.revitVisibilityHeight + 500);
            Point3D location = new Point3D(MaxMin[1].U - offset, MaxMin[1].V - offset, this.FloorPlanView.GenLevel.Elevation);
            Rect3D rec3d_Teritory = new Rect3D(location, size);
            transform.Origin = new XYZ(0, 0, this.FloorPlanView.GenLevel.Elevation);
            var inverseTransform = transform.Inverse;
            List<Element> elements = new List<Element>();
            #region floors
            using (FilteredElementCollector floorCollectors =
                new FilteredElementCollector(this.document, this.FloorPlanView.Id).OfClass(typeof(Floor)))
            {
                foreach (Element item in floorCollectors)
                {
                    if (!item.IsHidden(this.FloorPlanView))
                    {
                        using (BoundingBoxXYZ boundXYZ = item.get_BoundingBox(this.FloorPlanView))
                        {
                            if (this._boundingBoxIntersect(boundXYZ, rec3d_Teritory))
                            {
                                elements.Add(item);
                            }
                        }
                    }
                }
            }
            #endregion

            #region Walls
            using (FilteredElementCollector wallCollector =
                new FilteredElementCollector(this.document, this.FloorPlanView.Id).OfClass(typeof(Wall)))
            {
                foreach (Wall wall in wallCollector)
                {
                    BuiltInCategory elemCategory = (BuiltInCategory)wall.Category.Id.IntegerValue;
                    if (!wall.IsHidden(this.FloorPlanView))
                    {
                        using (BoundingBoxXYZ boundXYZ = wall.get_BoundingBox(this.FloorPlanView))
                        {
                            if (wall.WallType.Kind != WallKind.Curtain)
                            {
                                if (this._boundingBoxIntersect(boundXYZ, rec3d_Teritory))
                                {
                                    elements.Add(wall);
                                }
                            }
                        }
                    }
                }
            }
            #endregion

            #region FamilyInstance
            using (FilteredElementCollector allfamilyInstances =
                new FilteredElementCollector(document, this.FloorPlanView.Id).OfClass(typeof(FamilyInstance)))
            {
                foreach (Element item in allfamilyInstances)
                {
                    if (!item.IsHidden(this.FloorPlanView))
                    {
                        using (BoundingBoxXYZ boundXYZ = item.get_BoundingBox(this.FloorPlanView))
                        {
                            if (this._boundingBoxIntersect(boundXYZ, rec3d_Teritory))
                            {
                                BuiltInCategory category = (BuiltInCategory)item.Category.Id.IntegerValue;
                                if (!this.excludedCategoriesFromFurnitures.Contains(category) || category == BuiltInCategory.OST_Windows)
                                {
                                    elements.Add(item);
                                }
                            }
                        }
                    }
                }
            }
            #endregion

            #region Stairs Railing
            using (FilteredElementCollector stairRailings = new FilteredElementCollector(this.document, this.FloorPlanView.Id).OfCategory(BuiltInCategory.OST_StairsRailing))
            {
                foreach (Element item in stairRailings)
                {
                    if (!item.IsHidden(this.FloorPlanView))
                    {
                        using (BoundingBoxXYZ boundXYZ = item.get_BoundingBox(this.FloorPlanView))
                        {
                            if (this._boundingBoxIntersect(boundXYZ, rec3d_Teritory))
                            {
                                BuiltInCategory category = (BuiltInCategory)item.Category.Id.IntegerValue;
                                if (!this.excludedCategoriesFromFurnitures.Contains(category))
                                {
                                    elements.Add(item);
                                }
                            }
                        }
                    }
                }
            }
            #endregion

            #region Curtain Wall Panels
            /* */
            using (FilteredElementCollector curtainWallPanels =
                new FilteredElementCollector(document, this.FloorPlanView.Id).OfCategory(BuiltInCategory.OST_CurtainWallPanels))
            {
                foreach (Element item in curtainWallPanels)
                {
                    if (!item.IsHidden(this.FloorPlanView))
                    {
                        using (BoundingBoxXYZ boundXYZ = item.get_BoundingBox(this.FloorPlanView))
                        {
                            if (this._boundingBoxIntersect(boundXYZ, rec3d_Teritory))
                            {
                                elements.Add(item);

                            }
                        }
                    }
                }
            }
            #endregion

            #region Curtain Wall Mullions
            using (FilteredElementCollector curtainWallMullions =
                new FilteredElementCollector(document, this.FloorPlanView.Id).OfCategory(BuiltInCategory.OST_CurtainWallMullions))
            {
                foreach (Element item in curtainWallMullions)
                {
                    if (!item.IsHidden(this.FloorPlanView))
                    {
                        using (BoundingBoxXYZ boundXYZ = item.get_BoundingBox(this.FloorPlanView))
                        {
                            if (this._boundingBoxIntersect(boundXYZ, rec3d_Teritory))
                            {
                                elements.Add(item);
                            }
                        }
                    }
                }
            }
            #endregion

            #region Stairs
            using (FilteredElementCollector stairsCollector =
                new FilteredElementCollector(this.document, this.FloorPlanView.Id).OfCategory(BuiltInCategory.OST_Stairs))
            {
                foreach (Element item in stairsCollector)
                {
                    if (!item.IsHidden(this.FloorPlanView))
                    {
                        using (BoundingBoxXYZ boundXYZ = item.get_BoundingBox(this.FloorPlanView))
                        {
                            if (this._boundingBoxIntersect(boundXYZ, rec3d_Teritory))
                            {
                                elements.Add(item);
                            }
                        }
                    }
                }
            }
            #endregion

            #region doors
            foreach (var item in this.DoorsToInclude)
            {
                elements.Add(this.document.GetElement(item));
            }
            #endregion

            //updating the territory box
            location.Z = 0;
            rec3d_Teritory = new Rect3D(location, size);

            using (Options options = this.document.Application.Create.NewGeometryOptions())
            {
                options.ComputeReferences = true;
                options.IncludeNonVisibleObjects = false;
                ExtractGeometryModel3D geoms = new ExtractGeometryModel3D(this.document, options, elements, inverseTransform, rec3d_Teritory);
                //unit scaling
                double unitScale = UnitConversion.ConvertScale(Length_Unit_Types.FEET, this.UnitType);
                ScaleTransform3D transform3d = new ScaleTransform3D(unitScale, unitScale, unitScale);
                foreach (var item in geoms.WPFGeometries) item.Transform = transform3d;
                _geometryModel3Ds.AddRange(geoms.WPFGeometries);
                geoms.Clean();
                geoms = null;
                ExtractGeometryModel3D.ResetReport();
            }
            foreach (var item in elements) item.Dispose();
            elements.Clear();
            elements = null;
            return _geometryModel3Ds;
        }
        #endregion 
    }

}



