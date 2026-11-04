using System;
using System.Windows.Media.Media3D;
using Solar_System.Config;

namespace Solar_System.Camera
{
    /// <summary>
    /// Owns the 3D camera.
    /// Phase 1: a fixed camera looking at the Sun from slightly above.
    /// Dragging, zooming and fly-to animations are added in Phase 3.
    /// </summary>
    public sealed class CameraController
    {
        public CameraController()
        {
            Camera = new PerspectiveCamera
            {
                UpDirection = new Vector3D(0, 0, 1), // Z points up in this scene
                FieldOfView = SimulationConfig.CameraFieldOfView,
                NearPlaneDistance = SimulationConfig.CameraNearPlane,
                FarPlaneDistance = SimulationConfig.CameraFarPlane
            };
            LookAtSun(SimulationConfig.OverviewYawDegrees,
                      SimulationConfig.OverviewTiltDegrees,
                      SimulationConfig.OverviewDistance);
        }

        public PerspectiveCamera Camera { get; }

        /// <summary>Put the camera on an imaginary ball around the Sun and aim it near the Sun.</summary>
        private void LookAtSun(double yawDegrees, double tiltDegrees, double distance)
        {
            double yaw = yawDegrees * Math.PI / 180.0;
            double tilt = tiltDegrees * Math.PI / 180.0;

            var position = new Point3D(
                distance * Math.Cos(tilt) * Math.Cos(yaw),
                distance * Math.Cos(tilt) * Math.Sin(yaw),
                distance * Math.Sin(tilt));

            Camera.Position = position;
            Camera.LookDirection = new Point3D(0, 0, SimulationConfig.OverviewAimHeight) - position;
        }
    }
}
