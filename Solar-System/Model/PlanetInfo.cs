using System.Collections.Generic;

namespace Solar_System.Model
{
    /// <summary>
    /// The text about one body, exactly as written in Assets/planets.json.
    /// Everything is text, so numbers can be written in a nice readable way.
    /// </summary>
    public sealed class PlanetInfo
    {
        /// <summary>English name that links this entry to a CelestialBody.</summary>
        public string Id { get; set; }

        /// <summary>The name shown on screen (Bulgarian).</summary>
        public string Name { get; set; }

        public string Type { get; set; }
        public string Diameter { get; set; }
        public string Distance { get; set; }
        public string Day { get; set; }
        public string Year { get; set; }
        public string Moons { get; set; }
        public string Temperature { get; set; }
        public List<string> Facts { get; set; }
    }
}
