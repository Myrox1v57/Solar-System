using System.Windows;
using Solar_System.Config;

namespace Solar_System.Interaction
{
    /// <summary>
    /// Measures how long the hand has stayed still over the same thing.
    /// Moving away, or moving too much, starts the count again.
    /// </summary>
    public sealed class DwellTimer
    {
        private object target;
        private Point startPosition;
        private double seconds;

        /// <summary>Call once per frame. Returns the progress: 0 = just started, 1 = time is up.</summary>
        public double Update(object newTarget, Point position, double elapsedSeconds)
        {
            bool sameTarget = newTarget != null && ReferenceEquals(newTarget, target);
            bool stayedStill = (position - startPosition).Length <= SimulationConfig.DwellTolerancePixels;

            if (!sameTarget || !stayedStill)
            {
                target = newTarget;
                startPosition = position;
                seconds = 0;
                return 0;
            }

            seconds += elapsedSeconds;
            return seconds / SimulationConfig.DwellSeconds;
        }

        public void Reset()
        {
            target = null;
            seconds = 0;
        }
    }
}
