using System;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Solar_System.Model
{
    /// <summary>
    /// Data for one body (the Sun, a planet or the Moon).
    /// All values are filled in by SimulationConfig. Nothing here is drawn on screen:
    /// the 3D part lives in Scene/BodyVisual.cs.
    /// </summary>
    public sealed class CelestialBody
    {
        /// <summary>English name, used in the log. Will also link the body to planets.json.</summary>
        public string Name { get; set; }

        /// <summary>File name inside Assets/Textures.</summary>
        public string TextureFile { get; set; }

        /// <summary>Solid color used if the texture file is missing.</summary>
        public Color FallbackColor { get; set; }

        /// <summary>Size in scene units (not realistic, see SimulationConfig).</summary>
        public double Radius { get; set; }

        /// <summary>The body this one circles. Null means it stays at the center (the Sun).</summary>
        public CelestialBody Parent { get; set; }

        /// <summary>Distance from the parent in scene units.</summary>
        public double OrbitRadius { get; set; }

        /// <summary>Where on its orbit the body starts, in degrees.</summary>
        public double StartAngleDegrees { get; set; }

        /// <summary>How far the spin axis leans over, in degrees (real values).</summary>
        public double AxialTiltDegrees { get; set; }

        /// <summary>Seconds on screen for one trip around the parent, at normal speed.</summary>
        public double OrbitSeconds { get; set; }

        /// <summary>Seconds on screen for one spin around its own axis, at normal speed.</summary>
        public double SpinSeconds { get; set; }

        /// <summary>True for the Sun: it shines by itself instead of being lit.</summary>
        public bool IsSelfLit { get; set; }

        /// <summary>Position relative to the parent at the given orbit angle (orbits lie flat, Z is up).</summary>
        public Vector3D GetOrbitOffset(double angleDegrees)
        {
            double angle = angleDegrees * Math.PI / 180.0;
            return new Vector3D(OrbitRadius * Math.Cos(angle), OrbitRadius * Math.Sin(angle), 0);
        }
    }
}
