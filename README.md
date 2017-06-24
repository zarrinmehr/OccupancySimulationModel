Occupancy Simulation Model
==========================

SAIED ZARRINMEHR

What is OSM
-----------

Occupancy Simulation Model (OSM) is a software package for the simulation, visualization and evaluation of occupancy scenarios. Specifically, it is composed of Mandatory Occupancy Scenario Model (MOSM) and Optional Occupancy Scenario Model (OOSM). OSM is an under development project that uses an Agent-Based Model (ABM) to simulate building occupants and their unique preferences as well as a rich 3D model (either BIM or CAD) to represent the built environment. From this perspective it is similar to force-based crowd simulation models. The difference is that OSM is not concerned with safety and mainly focuses on evaluation of occupants’ experiences during occupancy scenarios. An agent in OSM is also smarter that the agents in most crowd simulation models in several ways. Instead of merely trying to avoid collisions with barriers and other agents or becoming engaged in rudimentary scenarios, in OSM an agent can react to environmental qualities and visual events. An agent can also be parametrically tuned to mimic the behavior of a human in real buildings. OSM utilizes Discrete-Event Simulation (DES) for modeling mandatory occupancy scenarios. After training, an agent in OSM can collect information of hours of occupancy scenario. This information can then be subjected to visual, sequential and spatial query to create a scientific foundation for evaluation and comparison of design alternatives. In summary, OSM can help designers understand the impact of their design decisions on the occupants at the post-occupancy phase to make their decisions more informed.

OSM also offers functionalities for visibility analysis including efficient calculation of polygonal and cellular visibility fields (i.e. isovists) from vantage points, calculation of visibility area from specified areas, proxemics analysis, and extraction of Justified Graphs (JG) in a semi-automatic way. However, OSM *should not be considered* as an alternative to existing Space Syntax software such as Depthmap. It also provides a mechanisms for saving and loading spatial data in CSV format. With OSM, occupancy scenarios can be animated and spatial data can be visualized in both 2 and 3 dimensions.

OSM was developed purely in C\# programming language. Its programming logic is based on complete separation of simulation engine from visualization engine, which is developed in XAML format using Windows Presentation Foundation (WPF). The source-code is available from [GitHub](https://github.com/zarrinmehr/OSM) under [MIT License](https://github.com/zarrinmehr/OSM/blob/master/LICENSE). OSM lays out a fully interoperable framework with BIM and CAD software that can be achieved with providing implementation for an interface and an abstract class. The existing source-code provides examples of their implementations for Autodesk Revit 2017 using its API libraries and can be directly used.

Tutorials
---------

More information about OSM and its applications can be found in the following link:

LINK TO BE ADDED

Contributors
------------

This software was developed as a part of Ph.D. dissertation of [Saied Zarrinmehr](https://www.linkedin.com/in/saied-zarrinmehr-8b799557/) in the Department of Architecture at Texas A&M University. This project was not supported/sponsored by any external funding. OSM was developed with the intellectual support of the following colleagues of mine at[BIMSIM Group](http://bim-sim.org/) in the Ph.D. Program of Architecture and Master's program of Computer Science.

|[Mohammad Rahmani Asl](https://www.linkedin.com/in/mohammad-rahmani-asl-90440b56)|||
|:--------------------------------------------------------------------------------|:--|:--|
|[Chengde Wu](https://www.linkedin.com/in/chengde-wu-20836862/)|||

OSM also received generous advising and intellectual support from the following professors:

|Professor [Mark Clayton](https://www.arch.tamu.edu/directory/people/mclayton/)|*(Chair of Ph.D. Committee)*|CEO at [SMARTreview](https://www.smartreview.biz/home)|
|:-----------------------------------------------------------------------------|:---------------------------|:-----------------------------------------------------|
|Professor [Wei Yan](https://www.arch.tamu.edu/directory/people/wyan/)|*(Co-Chair of Ph.D. Committee)*||
|Professor [Geoffrey Booth](https://www.arch.tamu.edu/directory/people/gbooth/)|*(Member of Ph.D. Committee)*||
|Professor [Kevin Glowacki](https://www.arch.tamu.edu/directory/people/kglowacki/)|||

Copyright Acknowledgement
-------------------------

OSM is developed based on extensive use of the following libraries, which are open-source and publicly available subject to various licensing systems.

-   **[Jace.NET](https://github.com/pieterderycke/Jace)** is compiled as a DLL and is included in the references of OSM. This library is available under [MIT License](https://github.com/pieterderycke/Jace/blob/master/LICENSE.md).
-   **[Math.NET Numerics](https://numerics.mathdotnet.com/)** is compiled as a DLL and is included in the references of OSM. This library is available under [MIT/X11 License](https://numerics.mathdotnet.com/License.html).
-   **[Triangle.NET:](https://triangle.codeplex.com/)** Two additional functions were added to the *Triangle.NET* project. This library was then compiled as a DLL and is included in the references of OSM. It is available under [MIT License](https://triangle.codeplex.com/license).
-   **[WriteableBitmapEx](https://github.com/teichgraf/WriteableBitmapEx/)** is compiled as a DLL and is included in the references of OSM. This library is available under [MIT/X11 License](https://github.com/teichgraf/WriteableBitmapEx/blob/master/LICENSE).
-   **[Clipper Library](http://www.angusj.com/delphi/clipper.php)** is compiled as a DLL and is included in the references of OSM. This library is available under [Boost Software License](http://www.boost.org/LICENSE_1_0.txt).
-   **[WPF simple zoom and drag support in a ScrollViewer:](%20https://www.codeproject.com/Articles/97871/WPF-simple-zoom-and-drag-support-in-a-ScrollViewer)** This project is integrated in the OSM's source-code in the following files: OSMDocument.xaml, OSMDocument.xaml.cs, JustifiedGraphVisualHost.xaml, and JustifiedGraphVisualHost.xaml.cs. The code also includes notes (i.e. comments) to give credit to the original developers according to [The Code Project Open License (CPOL) 1.02](https://www.codeproject.com/info/cpol10.aspx) license.

