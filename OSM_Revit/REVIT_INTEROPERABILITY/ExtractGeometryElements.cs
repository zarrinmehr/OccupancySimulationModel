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
    /// This class parse revit elements to OSM mesh elements.
    /// </summary>
    public class ParseRevitElements
    {
        /// <summary>
        /// Gets or sets the solids of BIM elements to be parsed.
        /// </summary>
        /// <value>The solids.</value>
        public List<Solid> Solids { get; set; }
        /// <summary>
        /// Gets or sets the meshes parsed from Revit solids.
        /// </summary>
        /// <value>The meshes.</value>
        public List<Mesh> Meshes { get; set; }
        /// <summary>
        /// Gets or sets the faces of BIM to be parsed.
        /// </summary>
        /// <value>The faces.</value>
        public List<Face> Faces { get; set; }
        /// <summary>
        /// Gets or sets a list of others Revit objects to be parsed
        /// </summary>
        /// <value>The others.</value>
        public List<GeometryObject> Others { get; set; }
        private Options _options { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParseRevitElements"/> class.
        /// </summary>
        /// <param name="options">The Revit options.</param>
        /// <param name="elements">The Revit elements.</param>
        public ParseRevitElements(Options options, IEnumerable<Element> elements)
        {
            this.Solids = new List<Solid>();
            this.Meshes = new List<Mesh>();
            this.Faces = new List<Face>();
            this.Others = new List<GeometryObject>();
            this._options = options;
            foreach (Element element in elements)
            {
                GeometryElement geomElem = null;
                geomElem = element.get_Geometry(this._options);
                if (typeof(FamilyInstance) == element.GetType())
                {
                    FamilyInstance familyInstance = element as FamilyInstance;
                    geomElem = familyInstance.get_Geometry(this._options).GetTransformed(Transform.Identity);
                }
                else
                {
                    geomElem = element.get_Geometry(this._options);
                }
                if (geomElem != null)
                {
                    this.GetGeometry(geomElem);
                }
            }
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="ParseRevitElements"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="doc">The Revit document.</param>
        /// <param name="elementIds">The element ids.</param>
        public ParseRevitElements(Options options, Document doc, IEnumerable<ElementId> elementIds)
        {
            this.Solids = new List<Solid>();
            this.Meshes = new List<Mesh>();
            this.Faces = new List<Face>();
            this.Others = new List<GeometryObject>();
            this._options = options;
            foreach (ElementId elementId in elementIds)
            {
                GeometryElement geomElem = null;
                var element = doc.GetElement(elementId);
                geomElem = element.get_Geometry(this._options);
                if (typeof(FamilyInstance) == element.GetType())
                {
                    FamilyInstance familyInstance = element as FamilyInstance;
                    geomElem = familyInstance.get_Geometry(this._options).GetTransformed(Transform.Identity);
                }
                else
                {
                    geomElem = element.get_Geometry(this._options);
                }
                if (geomElem != null)
                {
                    this.GetGeometry(geomElem);
                }
            }
        }
        private void GetGeometry(GeometryElement geomElement)
        {
            if (geomElement != null)
            {
                foreach (GeometryObject geomObj in geomElement)
                {
                    if (geomObj as Solid != null)
                    {
                        this.Solids.Add(geomObj as Solid);
                    }
                    else if (geomObj as Mesh != null)
                    {
                        this.Meshes.Add(geomObj as Mesh);
                    }
                    else if (geomObj as Face != null)
                    {
                        this.Faces.Add(geomObj as Face);
                    }
                    else if (geomObj as GeometryElement != null)
                    {
                        this.GetGeometry(geomObj as GeometryElement);
                    }
                    else if (geomObj as GeometryInstance != null)
                    {
                        this.GetGeometry(geomObj as GeometryInstance);
                    }
                    else
                    {
                        this.Others.Add(geomObj);
                    }
                }
            }
        }
        private void GetGeometry(GeometryInstance geomInstance)
        {
            GeometryElement geomElement = geomInstance.GetInstanceGeometry();
            this.GetGeometry(geomElement);
        }
        /// <summary>
        /// Cleans this instance.
        /// </summary>
        public void Clean()
        {
            foreach (var item in this.Solids) item.Dispose();
            this.Solids.Clear();
            this.Solids = null;
            foreach (var item in this.Faces) item.Dispose();
            this.Faces.Clear();
            this.Faces = null;
            foreach (var item in this.Meshes) item.Dispose();
            this.Meshes.Clear();
            this.Meshes = null;
            foreach (var item in this.Others) item.Dispose();
            this.Others.Clear();
            this.Others = null;
        }
    }
}

