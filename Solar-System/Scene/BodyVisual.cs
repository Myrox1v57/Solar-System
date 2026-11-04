using System.Windows.Media.Media3D;
using Solar_System.Model;

namespace Solar_System.Scene
{
    /// <summary>
    /// The 3D object of one body.
    /// Phase 1: a sphere with its axis tilted, placed at a position.
    /// Spinning comes in Phase 2; highlight, label and hit sphere come in Phase 3.
    /// </summary>
    public sealed class BodyVisual
    {
        // One shared move. Saturn's ring uses it too, so the ring always stays with Saturn.
        private readonly TranslateTransform3D translation = new TranslateTransform3D();

        public BodyVisual(CelestialBody body, GeometryModel3D sphere)
        {
            Body = body;
            Visual = new ModelVisual3D { Content = sphere, Transform = CreateTransform() };
        }

        public CelestialBody Body { get; }

        /// <summary>Add this to the Viewport3D.</summary>
        public ModelVisual3D Visual { get; }

        /// <summary>Center of the body in the scene.</summary>
        public Point3D Position
        {
            get { return new Point3D(translation.OffsetX, translation.OffsetY, translation.OffsetZ); }
            set
            {
                translation.OffsetX = value.X;
                translation.OffsetY = value.Y;
                translation.OffsetZ = value.Z;
            }
        }

        /// <summary>First lean over by the axial tilt, then move to the body's position.</summary>
        public Transform3D CreateTransform()
        {
            var transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(
                new AxisAngleRotation3D(new Vector3D(1, 0, 0), Body.AxialTiltDegrees)));
            transform.Children.Add(translation);
            return transform;
        }
    }
}
