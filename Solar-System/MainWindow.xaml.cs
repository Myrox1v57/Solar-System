using System;
using System.Windows;
using System.Windows.Media;
using Solar_System.Camera;
using Solar_System.Input;
using Solar_System.Scene;

namespace Solar_System
{
    /// <summary>
    /// The main window. Wires all parts of the app together.
    /// Phase 1: shows the static 3D scene and the Kinect status.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly KinectInput kinect = new KinectInput();
        private readonly CameraController cameraController = new CameraController();

        public MainWindow()
        {
            InitializeComponent();

            SceneViewport.Camera = cameraController.Camera;

            kinect.StatusChanged += OnKinectStatusChanged;
            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            SceneBuilder.Build(SceneViewport);

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
