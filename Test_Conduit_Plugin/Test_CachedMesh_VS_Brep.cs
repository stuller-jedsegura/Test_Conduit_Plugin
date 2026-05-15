using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.Drawing;

namespace Test_Conduit_Plugin
{
    /* Purpose of this command is to try and figure out if there are any differences in caching a mesh for use in the display conduit vs just using the brep
     * Right now we are storing this mesh internally, but it SEEMS that it doesn't make anything more performant? So we're just adding bloat for no reason?
     * Or perhaps we are recalcing those meshes too often, thereby foregoing any performance gains we would have otherwise gotten?
     * 
     */

    public class Test_CachedMesh_VS_Brep : Rhino.Commands.Command
    {
        //Here we store our different geometries
        private static List<Brep> _brepList = new List<Brep>();
        private static List<Mesh> _meshCache = new List<Mesh>();

        private static bool _useCachedMesh = false;
        private static double _sphereCount = 100;

        private readonly DrawConduit _drawConduit = new DrawConduit();
        private static DisplayMaterial _displayMat = new DisplayMaterial
        {
            Diffuse = Color.RoyalBlue,
            Transparency = 0,
            Ambient = Color.Black,
            Shine = 0,
            Specular = Color.White
        };

        public Test_CachedMesh_VS_Brep()
        {
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static Test_CachedMesh_VS_Brep Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "Test_CachedMesh_VS_Brep";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            _brepList.Clear();
            _meshCache.Clear();

            for (int i = 0; i < _sphereCount; i++)
            {
                //Create a bunch of spheres
                var sphere1 = new Sphere(Point3d.Origin, 1.05);
                sphere1.Translate(Vector3d.XAxis * i);
                var brepSphere1 = sphere1.ToBrep();

                //Make even more spheres
                var sphere2 = sphere1;
                sphere2.Translate(Vector3d.YAxis * i);
                var brepSphere2 = sphere2.ToBrep();

                //If we're in mesh mode we need our meshes to display
                if (_useCachedMesh)
                {
                    var currMeshes = Mesh.CreateFromBrep(brepSphere1, MeshingParameters.FastRenderMesh);
                    foreach (var currMesh in currMeshes)
                    {
                        _meshCache.Add(currMesh);
                    }

                    var currMeshes2 = Mesh.CreateFromBrep(brepSphere2, MeshingParameters.FastRenderMesh);
                    foreach (var currMesh in currMeshes2)
                    {
                        _meshCache.Add(currMesh);
                    }
                }

                _brepList.Add(brepSphere1);
                _brepList.Add(brepSphere2);
            }

            _drawConduit.Enabled = true;

            GetOption go = new GetOption();
            OptionToggle brepMeshToggle = new OptionToggle(_useCachedMesh, "Brep", "Mesh");
            go.AddOptionToggle("Use_Mesh", ref brepMeshToggle);
            go.AcceptNothing(true);
            go.SetCommandPrompt("Brep_or_Mesh");

            for(; ; )
            {
                var input = go.Get();
                if(input == Rhino.Input.GetResult.Nothing)
                {
                    break;
                }

                _useCachedMesh = brepMeshToggle.CurrentValue;

                //If we've flipped to Mesh Mode and don't have any, lets go make them
                if(_useCachedMesh && _meshCache.Count != _brepList.Count)
                {
                    _meshCache.Clear();
                    for (int i = 0; i < _brepList.Count; i++)
                    {
                        var currMeshes = Mesh.CreateFromBrep(_brepList[i], MeshingParameters.FastRenderMesh);
                        foreach (var currMesh in currMeshes)
                        {
                            _meshCache.Add(currMesh);
                        }
                    }
                }
            }

            doc.Views.Redraw();

            // ---
            return Result.Success;
        }

        //Here is the conduit where we draw our objects
        private class DrawConduit : DisplayConduit
        {
            protected override void CalculateBoundingBox(CalculateBoundingBoxEventArgs e)
            {
                //terribly large boundingBox just to include all the things in the conduit
                e.IncludeBoundingBox(new BoundingBox(-_sphereCount, -_sphereCount, -_sphereCount, _sphereCount, _sphereCount, _sphereCount));
                
                base.CalculateBoundingBox(e);
            }

            protected override void PostDrawObjects(DrawEventArgs e)
            {
                //Draw objects based on mode
                if (_useCachedMesh)
                {
                    for (int i = 0; i < _meshCache.Count; i++)
                    {
                        e.Display.DrawMeshShaded(_meshCache[i], _displayMat);
                    }
                }
                else
                {
                    for (int i = 0; i < _brepList.Count; i++)
                    {
                        e.Display.DrawBrepShaded(_brepList[i], _displayMat);

                    }
                }
            }
        }
    }
}
