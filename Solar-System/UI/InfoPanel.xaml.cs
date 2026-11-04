using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Solar_System.Config;
using Solar_System.Model;

namespace Solar_System.UI
{
    /// <summary>The panel on the right side with the facts about one body.</summary>
    public partial class InfoPanel : UserControl
    {
        private const string NoData = "няма данни";

        public InfoPanel()
        {
            InitializeComponent();
        }

        /// <summary>Fill in the body's text and slide the panel into view.</summary>
        public void SlideIn(CelestialBody body)
        {
            ShowText(body);

            if (Visibility != Visibility.Visible)
            {
                Slide.X = ActualWidth; // start just outside the right edge of the window
                Visibility = Visibility.Visible;
            }
            AnimateTo(0, null);
        }

        public void SlideOut()
        {
            AnimateTo(ActualWidth, () => Visibility = Visibility.Hidden);
        }

        private void ShowText(CelestialBody body)
        {
            PlanetInfo info = body.Info;

            NameText.Text = body.DisplayName;
            TypeText.Text = info?.Type ?? NoData;
            DiameterText.Text = info?.Diameter ?? NoData;
            DistanceText.Text = info?.Distance ?? NoData;
            DayText.Text = info?.Day ?? NoData;
            YearText.Text = info?.Year ?? NoData;
            MoonsText.Text = info?.Moons ?? NoData;
            TemperatureText.Text = info?.Temperature ?? NoData;

            Fact1Text.Text = GetFact(info, 0);
            Fact2Text.Text = GetFact(info, 1);
        }

        private static string GetFact(PlanetInfo info, int index)
        {
            return info?.Facts != null && index < info.Facts.Count ? info.Facts[index] : string.Empty;
        }

        private void AnimateTo(double x, Action whenFinished)
        {
            var animation = new DoubleAnimation(x, TimeSpan.FromSeconds(SimulationConfig.InfoPanelSlideSeconds))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            if (whenFinished != null)
            {
                animation.Completed += (sender, e) => whenFinished();
            }

            // Starting a new animation replaces a running one, so a quick Back cannot get stuck.
            Slide.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, animation);
        }
    }
}
