using System;
using System.Windows;
using System.Windows.Media;
using Solar_System.Input;

namespace Solar_System
{
    /// <summary>
    /// The main window. Wires all parts of the app together.
    /// Phase 0: shows whether a Kinect is connected.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly KinectInput kinect = new KinectInput();

        public MainWindow()
        {
            InitializeComponent();

            kinect.StatusChanged += OnKinectStatusChanged;
            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                kinect.Start();
            }
            catch (Exception ex)
            {
                // Happens only if Microsoft.Kinect.dll itself is missing. The app must still open.
                AppLog.Error("Kinect library could not be loaded", ex);
            }
            ShowKinectStatus();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            // Stop the sensor cleanly so the Kinect is free for the next run.
            kinect.Dispose();
        }

        private void OnKinectStatusChanged(object sender, EventArgs e)
        {
            ShowKinectStatus();
        }

        private void ShowKinectStatus()
        {
            KinectStatusText.Text = kinect.StatusMessage;
            KinectStatusText.Foreground = kinect.State == KinectConnectionState.Connected
                ? Brushes.LightGreen
                : Brushes.Orange;
        }
    }
}
