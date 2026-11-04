using System.Windows.Media.Media3D;
using Solar_System.Model;

namespace Solar_System.Scene
{
    /// <summary>
    /// The 3D object of one body: a sphere that spins, leans on its axis and moves along its orbit.
    /// Highlight, label and hit sphere come in Phase 3.
    /// </summary>
    public sealed class BodyVisual
    {
        private readonly AxisAngleRotation3D spin = new AxisAngleRotation3D(new Vector3D(0, 0, 1), 0);

        // One shared move. Saturn's ring uses it too, so the ring always stays with Saturn.
        private readonly TranslateTransform3D translation = new TranslateTransform3D();

        public BodyVisual(CelestialBody body, GeometryModel3D sphere)
        {
            Body = body;

            // Order matters: spin first, around the sphere's own axis. Then lean and move.
            var transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(spin));
            transform.Children.Add(CreateTiltAndMoveTransform());

            Visual = new ModelVisual3D { Content = sphere, Transform = transform };
        }

        public CelestialBody Body { get; }

        /// <summary>The visual of the body this one circles. Null for the Sun.</summary>
        public BodyVisual ParentVisual { get; set; }

        /// <summary>Add this to the Viewport3D.</summary>
        public ModelVisual3D Visual { get; }

        /// <summary>Center of the body in the scene.</summary>
        public Point3D Position
        {
            get { return new Point3D(translation.OffsetX, translation.OffsetY, translation.OffsetZ); }
            private set
            {
                translation.OffsetX = value.X;
                translation.OffsetY = value.Y;
                translation.OffsetZ = value.Z;
            }
        }

        /// <summary>
        /// Put the body where it belongs at this simulation time.
        /// A parent must be updated before the bodies that circle it.
        /// </summary>
        public void UpdateMotion(double simulationSeconds)
        {
            spin.Angle = 360.0 * simulationSeconds / Body.SpinSeconds % 360.0;

            if (ParentVisual != null)
            {
                double orbitAngle = Body.StartAngleDegrees + 360.0 * simulationSeconds / Body.OrbitSeconds;
                Position = ParentVisual.Position + Body.GetOrbitOffset(orbitAngle);
            }
        }

        /// <summary>
        /// Lean over by the axial tilt, then move to the body's position. There is no spin
        /// in here, because Saturn's ring uses this too and must not spin.
        /// </summary>
        public Transform3D CreateTiltAndMoveTransform()
        {
            var transform = new Transform3DGroup();
            transform.Children.Add(new RotateTransform3D(
                new AxisAngleRotation3D(new Vector3D(1, 0, 0), Body.AxialTiltDegrees)));
            transform.Children.Add(translation);
            return transform;
        }
    }
}
