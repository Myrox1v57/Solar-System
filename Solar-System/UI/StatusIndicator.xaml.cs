using System.Windows.Controls;
using System.Windows.Media;
using Solar_System.Input;

namespace Solar_System.UI
{
    /// <summary>The small Kinect status in the corner of the screen.</summary>
    public partial class StatusIndicator : UserControl
    {
        public StatusIndicator()
        {
            InitializeComponent();
        }

        public void Show(KinectState state, string message)
        {
            MessageText.Text = message;
            Dot.Fill = ChooseColor(state);
        }

        private static Brush ChooseColor(KinectState state)
        {
            switch (state)
            {
                case KinectState.Tracking:
                    return Brushes.LightGreen;
                case KinectState.WaitingForUser:
                case KinectState.UserSeen:
                    return Brushes.DeepSkyBlue;
                case KinectState.TurnedOff:
                    return Brushes.Gray;
                default:
                    return Brushes.Orange;
            }
        }
    }
}
