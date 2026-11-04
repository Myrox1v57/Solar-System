using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace Solar_System.UI
{
    /// <summary>Makes things appear and disappear gently instead of popping.</summary>
    public static class Fade
    {
        public static void In(UIElement element, double seconds)
        {
            element.Visibility = Visibility.Visible;
            Animate(element, 1, seconds, null);
        }

        /// <summary>Fades out, then removes the element from the screen.</summary>
        public static void Out(UIElement element, double seconds)
        {
            Animate(element, 0, seconds, () =>
            {
                // If it was faded back in meanwhile, leave it alone.
                if (element.Opacity < 0.01)
                {
                    element.Visibility = Visibility.Collapsed;
                }
            });
        }

        private static void Animate(UIElement element, double opacity, double seconds, Action whenFinished)
        {
            var animation = new DoubleAnimation(opacity, TimeSpan.FromSeconds(seconds));
            if (whenFinished != null)
            {
                animation.Completed += (sender, e) => whenFinished();
            }
            element.BeginAnimation(UIElement.OpacityProperty, animation);
        }
    }
}
