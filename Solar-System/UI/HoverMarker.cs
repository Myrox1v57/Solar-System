using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using HelixToolkit.Wpf;
using Solar_System.Scene;

namespace Solar_System.UI
{
    /// <summary>
    /// The glowing circle and the name above the planet under the pointer.
    /// They are flat 2D shapes drawn over the 3D view, so they stay sharp and easy to read.
    /// </summary>
    public sealed class HoverMarker
    {
        private const double RingMargin = 10; // pixels between the planet and the circle
        private const double LabelGap = 6;

        private readonly Viewport3D viewport;
        private readonly Ellipse ring;
        private readonly TextBlock label;
        private BodyVisual body;

        public HoverMarker(Viewport3D viewport, Ellipse ring, TextBlock label)
        {
            this.viewport = viewport;
            this.ring = ring;
            this.label = label;
            Show(null);
        }

        /// <summary>Mark this body, or nothing if it is null.</summary>
        public void Show(BodyVisual newBody)
        {
            if (newBody == body && newBody != null)
            {
                return;
            }

            body = newBody;
            Visibility visibility = body != null ? Visibility.Visible : Visibility.Collapsed;
            ring.Visibility = visibility;
            label.Visibility = visibility;
            if (body != null)
            {
                label.Text = body.Body.DisplayName;
            }
        }

        /// <summary>Call every frame: planets move, so the marker has to follow.</summary>
        public void Update()
        {
            if (body == null)
            {
                return;
            }

            var camera = (ProjectionCamera)viewport.Camera;
            Vector3D sideways = Vector3D.CrossProduct(camera.LookDirection, camera.UpDirection);
            sideways.Normalize();

            // Project the center and an edge of the planet to find its size on screen.
            Point center = Viewport3DHelper.Point3DtoPoint2D(viewport, body.Position);
            Point edge = Viewport3DHelper.Point3DtoPoint2D(viewport, body.Position + sideways * body.Body.ExtentRadius);
            double radius = (edge - center).Length + RingMargin;

            ring.Width = radius * 2;
            ring.Height = radius * 2;
            Canvas.SetLeft(ring, center.X - radius);
            Canvas.SetTop(ring, center.Y - radius);

            // Above the circle, but never off the top of the window.
            Canvas.SetLeft(label, center.X - label.ActualWidth / 2);
            Canvas.SetTop(label, Math.Max(0, center.Y - radius - label.ActualHeight - LabelGap));
        }
    }
}
