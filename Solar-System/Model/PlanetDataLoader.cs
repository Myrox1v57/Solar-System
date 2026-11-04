using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

namespace Solar_System.Model
{
    /// <summary>
    /// Loads Assets/planets.json, so the text can be edited without recompiling.
    /// A missing or broken file must never crash the app: the planets then simply have no text.
    /// </summary>
    public static class PlanetDataLoader
    {
        private static readonly string dataFile =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "planets.json");

        /// <summary>Gives every body its text from planets.json.</summary>
        public static void AttachInfo(IEnumerable<CelestialBody> bodies)
        {
            List<PlanetInfo> entries = Load();

            foreach (CelestialBody body in bodies)
            {
                body.Info = entries.Find(entry => entry.Id == body.Name);
                if (body.Info == null)
                {
                    AppLog.Warning("No entry in planets.json for: " + body.Name);
                }
            }
        }

        private static List<PlanetInfo> Load()
        {
            try
            {
                string json = File.ReadAllText(dataFile, Encoding.UTF8);
                return new JavaScriptSerializer().Deserialize<List<PlanetInfo>>(json);
            }
            catch (Exception ex)
            {
                AppLog.Error("planets.json could not be read", ex);
                return new List<PlanetInfo>();
            }
        }
    }
}
