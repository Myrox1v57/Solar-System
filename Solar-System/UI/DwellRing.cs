using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Solar_System.Config;

namespace Solar_System.UI
{
    /// <summary>
    /// The circle around the hand cursor that fills up while the hand stays still.
    /// It is a normal Ellipse with a dashed outline: one long dash that grows.
    /// </summary>
    public sealed class DwellRing
    {
        private readonly Ellipse ring;

        public DwellRing(Ellipse ring)
        {
            this.ring = ring;
            ring.Visibility = Visibility.Collapsed;
        }

        /// <summary>Progress runs from 0 to 1. Very low progress shows nothing.</summary>
        public void Show(Point? position, double progress)
        {
            if (!position.HasValue || progress < SimulationConfig.DwellRingShowsFrom)
            {
                ring.Visibility = Visibility.Collapsed;
                return;
            }

            // Dash lengths are counted in line widths, not in pixels.
            double middleOfLine = ring.Width - ring.StrokeThickness;
            double fullCircle = Math.PI * middleOfLine / ring.StrokeThickness;
            ring.StrokeDashArray = new DoubleCollection { Math.Min(progress, 1) * fullCircle, fullCircle };

            Canvas.SetLeft(ring, position.Value.X - ring.Width / 2);
            Canvas.SetTop(ring, position.Value.Y - ring.Height / 2);
            ring.Visibility = Visibility.Visible;
        }
    }
}
