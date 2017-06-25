Occupancy Simulation Model
--------------------------

This repository includes two projects: OSM and OSM_Revit. OSM is the core library for simulation, visualization and evaluation of mandatory and optional occupancy scenarios. This library is completely independent from any CAD or BIM data scheme. OSM_Revit implements an interoperable framework between Autodesk Revit 2017 and OSM environment. OSM_Revit also includes Autodesk Revit 2017 API libraries. To participate in creating interoperable framework between OSM and other CAD or BIM data schemes you need to provide implementation for [BIM_To_OSM_Base.cs](https://github.com/zarrinmehr/OccupancySimulationModel/blob/master/OSM/Interoperability/BIM_To_OSM_Base.cs) abstract class and [I_OSM_To_BIM.cs](https://github.com/zarrinmehr/OccupancySimulationModel/blob/master/OSM/Interoperability/I_OSM_To_BIM.cs) interface and pass them to the constructor of [OSMDocument](https://github.com/zarrinmehr/OccupancySimulationModel/blob/master/OSM/OSMDocument.xaml.cs). You can check OSM_Revit project as an example.

These projects are developed in C# and uses XAML files to create the UI using  Windows Presentation Foundation (WPF). The OSM core project is designed based on complete isolation of simulation part from visualization part. This principle will make it imaginable for us to change the UI system in future without changing the simulation core of OSM. Let's follow this principle in future developments. 

Use .NET Framework  4.5.2 to compile the code.

Make sure to include the DLL dependencies of the projects. 

The project was last complied in Visual Studio 2015.