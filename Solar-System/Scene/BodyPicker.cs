using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using Solar_System.Config;

namespace Solar_System.Scene
{
    /// <summary>
    /// Finds the body under the pointer. Instead of testing the real sphere, every body gets
    /// an invisible, bigger "hit sphere", so small planets like Mercury are still easy to hit.
    /// </summary>
    public static class BodyPicker
    {
        /// <summary>Returns the body at this screen position, or null if there is none.</summary>
        public static BodyVisual Pick(Viewport3D viewport, IEnumerable<BodyVisual> bodies, Point position)
        {
            // An imaginary line from the camera through the pointer, into the scene.
            Ray3D ray = Viewport3DHelper.Point2DtoRay3D(viewport, position);
            if (ray == null)
            {
                return null;
            }

            Vector3D direction = ray.Direction;
            direction.Normalize();

            BodyVisual bestBody = null;
            double bestScore = 1; // above 1 means the line misses the hit sphere

            foreach (BodyVisual body in bodies)
            {
                Vector3D toBody = body.Position - ray.Origin;
                double alongLine = Vector3D.DotProduct(toBody, direction);
                if (alongLine <= 0)
                {
                    continue; // behind the camera
                }

                double missDistance = (toBody - direction * alongLine).Length;
                double hitRadius = Math.Max(body.Body.ExtentRadius * SimulationConfig.HitRadiusFactor,
                                            SimulationConfig.MinHitRadius);

                // Hit spheres can overlap (Earth and the Moon). The body whose
                // center is closest to the pointer, compared to its size, wins.
                double score = missDistance / hitRadius;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestBody = body;
                }
            }

            return bestBody;
        }
    }
}
