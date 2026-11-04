using System.Collections.Generic;
using System.Windows.Media;
using Solar_System.Model;

namespace Solar_System.Config
{
    /// <summary>
    /// Every number you may want to tweak, in one place.
    /// Distances and sizes are in "scene units". They are NOT realistic: at real scale the
    /// planets would be invisible. Distances are squeezed so all orbits fit on screen, and
    /// sizes are enlarged but keep the correct order (Jupiter biggest, Mercury smallest).
    /// </summary>
    public static class SimulationConfig
    {
        // ---- Camera ----
        /// <summary>Tilt above the orbit plane, in degrees (0 = from the side, 90 = from the top).</summary>
        public const double OverviewTiltDegrees = 30;
        /// <summary>Direction the camera looks from, in degrees around the Sun.</summary>
        public const double OverviewYawDegrees = -90;
        /// <summary>Distance from the camera to the Sun.</summary>
        public const double OverviewDistance = 90;
        /// <summary>
        /// The camera aims this far below the Sun. That moves the whole system up on screen,
        /// so the orbits nearest the camera are not cut off by the bottom edge.
        /// </summary>
        public const double OverviewAimHeight = -9;
        /// <summary>Horizontal field of view in degrees.</summary>
        public const double CameraFieldOfView = 60;
        public const double CameraNearPlane = 0.5;
        public const double CameraFarPlane = 3000;

        // ---- Detail of the 3D shapes ----
        /// <summary>Slices around a sphere. More = rounder, but slower.</summary>
        public const int SphereSegmentsAround = 96;
        /// <summary>Slices from pole to pole.</summary>
        public const int SphereSegmentsPoleToPole = 48;
        public const int OrbitLineSegments = 256;
        public const int RingSegments = 128;

        // ---- Star background ----
        /// <summary>Radius of the huge sphere with the stars on its inside.</summary>
        public const double SkyboxRadius = 1500;
        public static readonly Color SkyboxTint = Color.FromRgb(200, 200, 200);

        // ---- Lights ----
        public static readonly Color SunLightColor = Colors.White;
        /// <summary>Weak light everywhere, so the dark side of a planet is not 100% black.</summary>
        public static readonly Color AmbientLightColor = Color.FromRgb(38, 38, 42);

        // ---- Orbit lines ----
        public static readonly Color OrbitLineColor = Color.FromRgb(70, 88, 115);
        public const double OrbitLineThickness = 1.2;

        // ---- Saturn's ring (sizes are multiples of Saturn's radius, real proportions) ----
        public const string RingTextureFile = "2k_saturn_ring_alpha.png";
        public const double RingInnerRadiusFactor = 1.24;
        public const double RingOuterRadiusFactor = 2.27;
        public static readonly Color RingTint = Color.FromRgb(225, 215, 195);
        /// <summary>Used if the ring texture is missing. Half transparent.</summary>
        public static readonly Color RingFallbackColor = Color.FromArgb(120, 200, 180, 140);

        // ---- Star background file ----
        public const string SkyboxTextureFile = "2k_stars_milky_way.jpg";

        // ---- Motion ----
        /// <summary>
        /// Seconds for Earth to go once around the Sun. All planets use real orbit times
        /// compared to Earth, so Mercury is fastest and Neptune takes about 55 minutes.
        /// </summary>
        public const double EarthOrbitSeconds = 20;
        /// <summary>
        /// Seconds for Earth to spin once. Spins have their own time scale: on the orbit scale
        /// Earth would spin 18 times per second. Compared to each other the speeds are real.
        /// </summary>
        public const double EarthSpinSeconds = 10;
        /// <summary>
        /// Seconds for the Moon to go around Earth. This one is NOT to scale: the real
        /// ratio would be 1.5 seconds, which looks far too frantic on screen.
        /// </summary>
        public const double MoonOrbitSeconds = 4;

        // ---- Simulation speed ('+' and '-' keys) ----
        public const double MinSimulationSpeed = 0.25;
        public const double MaxSimulationSpeed = 8;
        /// <summary>Each key press multiplies or divides the speed by this.</summary>
        public const double SimulationSpeedStep = 2;
        /// <summary>Longest time one frame may count for, so a short freeze does not make planets jump.</summary>
        public const double MaxFrameSeconds = 0.1;

        /// <summary>
        /// All bodies. A parent must come before the bodies that circle it.
        /// Axial tilts, orbit years and spin days are real values. Venus and Uranus really
        /// are that strange: their big tilts also make them spin the "wrong" way.
        /// </summary>
        public static List<CelestialBody> CreateBodies()
        {
            var sun = new CelestialBody
            {
                Name = "Sun",
                TextureFile = "2k_sun.jpg",
                FallbackColor = Color.FromRgb(255, 190, 60),
                Radius = 3.5,
                AxialTiltDegrees = 7.25,
                SpinSeconds = 25.05 * EarthSpinSeconds,
                IsSelfLit = true
            };

            var earth = Planet(sun, "Earth", "2k_earth_daymap.jpg", Color.FromRgb(60, 110, 200), 0.8, 12.5, 200, 23.44, 1.0, 0.9973);

            return new List<CelestialBody>
            {
                sun,
                Planet(sun, "Mercury", "2k_mercury.jpg", Color.FromRgb(150, 140, 130), 0.45, 7.0, 60, 0.03, 0.2408, 58.65),
                Planet(sun, "Venus", "2k_venus_surface.jpg", Color.FromRgb(220, 180, 120), 0.75, 9.5, 130, 177.4, 0.6152, 243.02),
                earth,
                new CelestialBody
                {
                    Name = "Moon",
                    TextureFile = "2k_moon.jpg",
                    FallbackColor = Color.FromRgb(180, 180, 175),
                    Radius = 0.28,
                    Parent = earth,
                    OrbitRadius = 1.6,
                    StartAngleDegrees = 20,
                    AxialTiltDegrees = 6.7,
                    OrbitSeconds = MoonOrbitSeconds,
                    SpinSeconds = MoonOrbitSeconds // same as its orbit, so one face always looks at Earth
                },
                Planet(sun, "Mars", "2k_mars.jpg", Color.FromRgb(195, 95, 60), 0.6, 16.0, 330, 25.19, 1.8808, 1.026),
                Planet(sun, "Jupiter", "2k_jupiter.jpg", Color.FromRgb(205, 170, 135), 2.2, 21.5, 20, 3.13, 11.862, 0.4135),
                Planet(sun, "Saturn", "2k_saturn.jpg", Color.FromRgb(215, 190, 140), 1.9, 28.5, 150, 26.73, 29.457, 0.444),
                Planet(sun, "Uranus", "2k_uranus.jpg", Color.FromRgb(150, 215, 225), 1.25, 34.5, 200, 97.77, 84.02, 0.7183),
                Planet(sun, "Neptune", "2k_neptune.jpg", Color.FromRgb(70, 100, 215), 1.2, 39.5, 100, 28.32, 164.8, 0.6713)
            };
        }

        /// <summary>
        /// Shortcut for a body that circles the Sun.
        /// orbitYears: real time for one orbit, in Earth years.
        /// spinDays: real time for one spin, in Earth days.
        /// </summary>
        private static CelestialBody Planet(CelestialBody sun, string name, string texture, Color fallback,
            double radius, double orbitRadius, double startAngle, double axialTilt, double orbitYears, double spinDays)
        {
            return new CelestialBody
            {
                Name = name,
                TextureFile = texture,
                FallbackColor = fallback,
                Radius = radius,
                Parent = sun,
                OrbitRadius = orbitRadius,
                StartAngleDegrees = startAngle,
                AxialTiltDegrees = axialTilt,
                OrbitSeconds = orbitYears * EarthOrbitSeconds,
                SpinSeconds = spinDays * EarthSpinSeconds
            };
        }
    }
}
