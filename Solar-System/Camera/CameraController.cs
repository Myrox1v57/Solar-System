using System;
using System.Windows.Media.Media3D;
using Solar_System.Config;
using Solar_System.Scene;

namespace Solar_System.Camera
{
    /// <summary>
    /// Owns the 3D camera. In Overview it sits on an imaginary ball around the Sun, and a drag
    /// or zoom moves it smoothly. In Focus it follows one planet. Between the two it flies.
    /// Call Update every frame.
    /// </summary>
    public sealed class CameraController
    {
        // Where the user wants the Overview camera to be...
        private double targetYaw = SimulationConfig.OverviewYawDegrees;
        private double targetTilt = SimulationConfig.OverviewTiltDegrees;
        private double targetDistance = SimulationConfig.OverviewDistance;

        // ...and where it is right now. These chase the targets, which removes all jitter.
        private double yaw = SimulationConfig.OverviewYawDegrees;
        private double tilt = SimulationConfig.OverviewTiltDegrees;
        private double distance = SimulationConfig.OverviewDistance;

        private BodyVisual focusBody;      // null in Overview
        private Vector3D focusDirection;   // from the planet toward the camera

        private double flyTime;
        private Point3D flyStartPosition;
        private Point3D flyStartLookAt;

        public CameraController()
        {
            Camera = new PerspectiveCamera
            {
                UpDirection = new Vector3D(0, 0, 1), // Z points up in this scene
                FieldOfView = SimulationConfig.CameraFieldOfView,
                NearPlaneDistance = SimulationConfig.CameraNearPlane,
                FarPlaneDistance = SimulationConfig.CameraFarPlane
            };
            Update(0);
        }

        public PerspectiveCamera Camera { get; }

        /// <summary>True while the camera flies to a planet or back.</summary>
        public bool IsFlying { get; private set; }

        /// <summary>Overview: left/right turns around the Sun, up/down changes the tilt.</summary>
        public void Drag(double pixelsX, double pixelsY)
        {
            targetYaw -= pixelsX * SimulationConfig.CameraDragDegreesPerPixel;
            targetTilt += pixelsY * SimulationConfig.CameraDragDegreesPerPixel;
            targetTilt = Clamp(targetTilt, SimulationConfig.MinTiltDegrees, SimulationConfig.MaxTiltDegrees);
        }

        /// <summary>Overview: positive steps move closer, negative steps move away.</summary>
        public void Zoom(double steps)
        {
            targetDistance *= Math.Pow(SimulationConfig.ZoomPerWheelNotch, steps);
            targetDistance = Clamp(targetDistance, SimulationConfig.MinOverviewDistance,
                                   SimulationConfig.MaxOverviewDistance);
        }

        /// <summary>Overview: go back to the start view.</summary>
        public void Reset()
        {
            // Take the short way around instead of unwinding every turn the user made.
            targetYaw = yaw + NormalizeDegrees(SimulationConfig.OverviewYawDegrees - yaw);
            targetTilt = SimulationConfig.OverviewTiltDegrees;
            targetDistance = SimulationConfig.OverviewDistance;
        }

        /// <summary>Fly to a planet and follow it.</summary>
        public void FlyTo(BodyVisual body)
        {
            focusDirection = ChooseFocusDirection(body);
            focusBody = body;
            StartFlying();
        }

        /// <summary>Fly back to the Overview view.</summary>
        public void FlyToOverview()
        {
            focusBody = null;
            StartFlying();
        }

        public void Update(double seconds)
        {
            double chase = 1 - Math.Exp(-SimulationConfig.CameraSmoothing * seconds);
            yaw += (targetYaw - yaw) * chase;
            tilt += (targetTilt - tilt) * chase;
            distance += (targetDistance - distance) * chase;

            Point3D position;
            Point3D lookAt;
            if (focusBody != null)
            {
                GetFocusView(out position, out lookAt);
            }
            else
            {
                GetOverviewView(out position, out lookAt);
            }

            if (IsFlying)
            {
                flyTime += seconds;
                double progress = Math.Min(flyTime / SimulationConfig.FlySeconds, 1);
                double eased = progress * progress * (3 - 2 * progress); // slow start, slow end

                position = flyStartPosition + (position - flyStartPosition) * eased;
                lookAt = flyStartLookAt + (lookAt - flyStartLookAt) * eased;
                IsFlying = progress < 1;
            }

            Camera.Position = position;
            Camera.LookDirection = lookAt - position;
        }

        private void StartFlying()
        {
            flyStartPosition = Camera.Position;
            flyStartLookAt = Camera.Position + Camera.LookDirection;
            flyTime = 0;
            IsFlying = true;
        }

        private void GetOverviewView(out Point3D position, out Point3D lookAt)
        {
            double yawRadians = yaw * Math.PI / 180.0;
            double tiltRadians = tilt * Math.PI / 180.0;

            position = new Point3D(
                distance * Math.Cos(tiltRadians) * Math.Cos(yawRadians),
                distance * Math.Cos(tiltRadians) * Math.Sin(yawRadians),
                distance * Math.Sin(tiltRadians));

            // The lower the camera, the flatter the orbits look and the less aim offset they need.
            double startTilt = SimulationConfig.OverviewTiltDegrees * Math.PI / 180.0;
            double aimHeight = SimulationConfig.OverviewAimHeight * Math.Sin(tiltRadians) / Math.Sin(startTilt);
            lookAt = new Point3D(0, 0, aimHeight);
        }

        /// <summary>The camera stays close to the planet, aimed to its right, so the planet sits on the left.</summary>
        private void GetFocusView(out Point3D position, out Point3D lookAt)
        {
            double size = focusBody.Body.RingOuterRadius > 0
                ? focusBody.Body.RingOuterRadius * SimulationConfig.RingFocusSize
                : focusBody.Body.Radius;
            double cameraDistance = size * SimulationConfig.FocusDistanceFactor;
            position = focusBody.Position + focusDirection * cameraDistance;

            Vector3D right = Vector3D.CrossProduct(-focusDirection, Camera.UpDirection);
            right.Normalize();

            // The field of view is measured across the width of the screen.
            double halfScreenWidth = cameraDistance * Math.Tan(SimulationConfig.CameraFieldOfView / 2 * Math.PI / 180.0);
            double offset = (0.5 - SimulationConfig.FocusScreenCenter) * 2 * halfScreenWidth;
            lookAt = focusBody.Position + right * offset;
        }

        /// <summary>Look at a planet from the Sun's side, so we see its bright face.</summary>
        private Vector3D ChooseFocusDirection(BodyVisual body)
        {
            // The Sun has no "Sun's side", so keep looking from where the camera is now.
            Vector3D flat = body.Body.Parent != null
                ? new Point3D(0, 0, 0) - body.Position
                : Camera.Position - body.Position;
            flat.Z = 0;
            flat.Normalize();

            double side = body.Body.Parent != null ? SimulationConfig.FocusSideAngleDegrees * Math.PI / 180.0 : 0;
            double height = SimulationConfig.FocusHeightDegrees * Math.PI / 180.0;

            var turned = new Vector3D(
                flat.X * Math.Cos(side) - flat.Y * Math.Sin(side),
                flat.X * Math.Sin(side) + flat.Y * Math.Cos(side),
                0);

            return turned * Math.Cos(height) + new Vector3D(0, 0, Math.Sin(height));
        }

        private static double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        /// <summary>Brings an angle into the range -180..180.</summary>
        private static double NormalizeDegrees(double degrees)
        {
            degrees %= 360;
            if (degrees > 180)
            {
                degrees -= 360;
            }
            else if (degrees < -180)
            {
                degrees += 360;
            }
            return degrees;
        }
    }
}
