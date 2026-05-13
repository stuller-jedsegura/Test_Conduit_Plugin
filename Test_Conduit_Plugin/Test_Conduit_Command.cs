using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.Geometry;
using System.Drawing;

namespace Test_Conduit_Plugin
{
    public class Test_Conduit_Command : Rhino.Commands.Command
    {
        //Here we store our geometry
        private static GeometryBase _geometry;

        private readonly DrawConduit _drawConduit = new DrawConduit();
        private static DisplayMaterial _displayMat = new DisplayMaterial
        {
            Diffuse = Color.RoyalBlue,
            Transparency = 0,
            Ambient = Color.Black,
            Shine = 0,
            Specular = Color.White
        };

        public Test_Conduit_Command()
        {
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static Test_Conduit_Command Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "Test_Conduit_Command";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            //Command just creates a little sphere
            var sphere = new Sphere(Point3d.Origin, 5);
            _geometry = sphere.ToBrep();

            //Lets you toggle the conduit to show the sphere or not, note how the sphere never hits the document but we have the geometry
            if (_drawConduit.Enabled)
            {
                _drawConduit.Enabled = false;
            }
            else
            {
                _drawConduit.Enabled = true;
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
                if (_geometry != null)
                {
                    e.IncludeBoundingBox(_geometry.GetBoundingBox(true));
                }
                base.CalculateBoundingBox(e);
            }

            protected override void PostDrawObjects(DrawEventArgs e)
            {
                if (_geometry is Brep brep)
                {
                    e.Display.DrawBrepShaded(brep, _displayMat);
                }
            }
        }
    }
}
