using System.Windows;
using System.Windows.Controls;
using Solar_System.Config;

namespace Solar_System.UI
{
    /// <summary>
    /// The hand on screen: an open hand, or a fist while the Kinect sees a closed hand.
    /// We draw it ourselves because the Toolkit's cursor has a fixed size and always
    /// follows the hand directly, so it cannot be made smaller or less sensitive.
    /// </summary>
    public sealed class HandCursor
    {
        private const string OpenHand = "✋";
        private const string Fist = "✊";

        private readonly TextBlock symbol;

        public HandCursor(TextBlock symbol)
        {
            this.symbol = symbol;
            symbol.FontSize = SimulationConfig.KinectCursorSize;
            symbol.Visibility = Visibility.Collapsed;
        }

        /// <summary>Call every frame. A null position hides the cursor.</summary>
        public void Show(Point? position, bool isFist)
        {
            if (!position.HasValue)
            {
                symbol.Visibility = Visibility.Collapsed;
                return;
            }

            symbol.Text = isFist ? Fist : OpenHand;
            symbol.Visibility = Visibility.Visible;
            Canvas.SetLeft(symbol, position.Value.X - symbol.ActualWidth / 2);
            Canvas.SetTop(symbol, position.Value.Y - symbol.ActualHeight / 2);
        }
    }
}
