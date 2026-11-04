using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using Solar_System.Config;
using Solar_System.Model;

namespace Solar_System.Scene
{
    /// <summary>
    /// Creates everything in the 3D scene: lights, stars, orbit lines, bodies and Saturn's ring.
    /// </summary>
    public static class SceneBuilder
    {
        /// <summary>Fills the viewport and returns the bodies so other code can move them.</summary>
        public static List<BodyVisual> Build(Viewport3D viewport)
        {
            List<CelestialBody> bodies = SimulationConfig.CreateBodies();

            viewport.Children.Add(CreateLights());

            ModelVisual3D skybox = CreateSkybox();
            if (skybox != null)
            {
                viewport.Children.Add(skybox);
            }

            foreach (CelestialBody body in bodies)
            {
                // Only planets get a line. The Moon's orbit is too small to show well.
                if (body.Parent != null && body.Parent.Parent == null)
                {
                    viewport.Children.Add(CreateOrbitLine(body));
                }
            }

            var visuals = new List<BodyVisual>();
            var visualOfBody = new Dictionary<CelestialBody, BodyVisual>();
            foreach (CelestialBody body in bodies)
            {
                var visual = new BodyVisual(body, CreateSphere(body));
                if (body.Parent != null)
                {
                    visual.ParentVisual = visualOfBody[body.Parent];
                }
                visual.UpdateMotion(0); // go to the start position

                viewport.Children.Add(visual.Visual);
                visuals.Add(visual);
                visualOfBody[body] = visual;
            }

            // Transparent objects must be added LAST. WPF draws in order, and anything
            // added after the ring would be hidden behind the ring's see-through parts.
            BodyVisual saturn = visuals.Find(v => v.Body.Name == "Saturn");
            viewport.Children.Add(CreateRing(saturn));

            return visuals;
        }

        private static ModelVisual3D CreateLights()
        {
            var lights = new Model3DGroup();
            lights.Children.Add(new AmbientLight(SimulationConfig.AmbientLightColor));
            lights.Children.Add(new PointLight(SimulationConfig.SunLightColor, new Point3D(0, 0, 0)));
            return new ModelVisual3D { Content = lights };
        }

        /// <summary>A huge sphere with the stars on its inside. Returns null if the picture is missing.</summary>
        private static ModelVisual3D CreateSkybox()
        {
            ImageBrush stars = TextureLoader.Load(SimulationConfig.SkyboxTextureFile);
            if (stars == null)
            {
                return null; // the window background is black anyway
            }

            var builder = new MeshBuilder(false, true);
            builder.AddSphere(new Point3D(0, 0, 0), SimulationConfig.SkyboxRadius,
                SimulationConfig.SkyboxSegmentsAround, SimulationConfig.SkyboxSegmentsPoleToPole);

            // Nothing is behind the stars, so one emissive layer is enough. This fills the whole
            // screen every frame, so drawing it only once makes a real difference to the speed.
            var material = new EmissiveMaterial(stars) { Color = SimulationConfig.SkyboxTint };
            material.Freeze();

            // We look at the sphere from inside, so the picture goes on its back side.
            var model = new GeometryModel3D
            {
                Geometry = builder.ToMesh(true),
                BackMaterial = material
            };
            return new ModelVisual3D { Content = model };
        }

        /// <summary>A thin circle. Its width stays the same on screen, whatever the camera distance.</summary>
        private static LinesVisual3D CreateOrbitLine(CelestialBody body)
        {
            var line = new LinesVisual3D
            {
                Color = SimulationConfig.OrbitLineColor,
                Thickness = SimulationConfig.OrbitLineThickness
            };

            // LinesVisual3D wants pairs of points: start and end of every short piece.
            int segments = SimulationConfig.OrbitLineSegments;
            for (int i = 0; i < segments; i++)
            {
                line.Points.Add(new Point3D(0, 0, 0) + body.GetOrbitOffset(360.0 * i / segments));
                line.Points.Add(new Point3D(0, 0, 0) + body.GetOrbitOffset(360.0 * (i + 1) / segments));
            }
            return line;
        }

        private static GeometryModel3D CreateSphere(CelestialBody body)
        {
            var builder = new MeshBuilder(true, true);
            builder.AddSphere(new Point3D(0, 0, 0), body.Radius,
                SimulationConfig.SphereSegmentsAround, SimulationConfig.SphereSegmentsPoleToPole);

            return new GeometryModel3D
            {
                Geometry = builder.ToMesh(true),
                Material = CreateBodyMaterial(body)
            };
        }

        private static Material CreateBodyMaterial(CelestialBody body)
        {
            Brush brush = TextureLoader.Load(body.TextureFile);
            if (brush == null)
            {
                brush = new SolidColorBrush(body.FallbackColor);
                brush.Freeze();
            }

            if (body.IsSelfLit)
            {
                return CreateSelfLitMaterial(brush, Colors.White);
            }

            var material = new DiffuseMaterial(brush);
            material.Freeze();
            return material;
        }

        /// <summary>
        /// Glows by itself and ignores all lights (Sun, stars, ring).
        /// A WPF emissive material only ADDS light on top of what is behind it, so things
        /// behind would shine through. The first layer stops that: it is the same picture
        /// painted black, which darkens what is behind wherever the picture is not see-through.
        /// </summary>
        private static Material CreateSelfLitMaterial(Brush brush, Color tint)
        {
            var material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(brush) { Color = Colors.Black, AmbientColor = Colors.Black });
            material.Children.Add(new EmissiveMaterial(brush) { Color = tint });
            material.Freeze();
            return material;
        }

        /// <summary>
        /// A flat ring around Saturn. It is self-lit because the Sun's light hits a flat ring
        /// almost edge-on, which would leave it nearly black.
        /// </summary>
        private static ModelVisual3D CreateRing(BodyVisual saturn)
        {
            Brush brush;
            Color tint = SimulationConfig.RingTint;

            ImageBrush picture = TextureLoader.Load(SimulationConfig.RingTextureFile);
            if (picture != null)
            {
                // The strip looks the same from top to bottom. Using only its middle avoids
                // a thin line on the ring where the top and bottom edges of the picture meet.
                picture = picture.Clone();
                picture.Viewbox = new Rect(0, 0.25, 1, 0.5);
                picture.Freeze();
                brush = picture;
            }
            else
            {
                brush = new SolidColorBrush(SimulationConfig.RingFallbackColor);
                brush.Freeze();
                tint = Colors.White;
            }

            Material material = CreateSelfLitMaterial(brush, tint);

            var model = new GeometryModel3D
            {
                Geometry = CreateRingMesh(
                    saturn.Body.Radius * SimulationConfig.RingInnerRadiusFactor,
                    saturn.Body.Radius * SimulationConfig.RingOuterRadiusFactor),
                Material = material,
                BackMaterial = material // visible from above and below
            };
            return new ModelVisual3D { Content = model, Transform = saturn.CreateTiltAndMoveTransform() };
        }

        /// <summary>
        /// A flat disc with a hole. The ring picture is a thin strip: its left edge is the
        /// inside of the ring and its right edge the outside, so U runs from inner to outer.
        /// </summary>
        private static MeshGeometry3D CreateRingMesh(double innerRadius, double outerRadius)
        {
            var mesh = new MeshGeometry3D();
            int segments = SimulationConfig.RingSegments;

            for (int i = 0; i <= segments; i++)
            {
                double angle = 2 * Math.PI * i / segments;
                double x = Math.Cos(angle);
                double y = Math.Sin(angle);
                double v = (double)i / segments;

                mesh.Positions.Add(new Point3D(innerRadius * x, innerRadius * y, 0));
                mesh.Positions.Add(new Point3D(outerRadius * x, outerRadius * y, 0));
                mesh.TextureCoordinates.Add(new Point(0, v));
                mesh.TextureCoordinates.Add(new Point(1, v));

                if (i < segments)
                {
                    int inner = i * 2;
                    int outer = inner + 1;
                    int nextInner = inner + 2;
                    int nextOuter = inner + 3;

                    mesh.TriangleIndices.Add(inner);
                    mesh.TriangleIndices.Add(outer);
                    mesh.TriangleIndices.Add(nextOuter);

                    mesh.TriangleIndices.Add(inner);
                    mesh.TriangleIndices.Add(nextOuter);
                    mesh.TriangleIndices.Add(nextInner);
                }
            }

            mesh.Freeze();
            return mesh;
        }
    }
}
