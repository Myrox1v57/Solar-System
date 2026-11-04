using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Solar_System.Camera;
using Solar_System.Input;
using Solar_System.Scene;
using Solar_System.Simulation;

namespace Solar_System
{
    /// <summary>
    /// The main window. Wires all parts of the app together.
    /// Phase 2: moves the planets every frame and handles the simulation keys.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly KinectInput kinect = new KinectInput();
        private readonly CameraController cameraController = new CameraController();
        private readonly SimulationClock clock = new SimulationClock();
        private readonly Stopwatch frameTimer = new Stopwatch();
        private List<BodyVisual> bodies = new List<BodyVisual>();

        public MainWindow()
        {
            InitializeComponent();

            SceneViewport.Camera = cameraController.Camera;

            kinect.StatusChanged += OnKinectStatusChanged;
            Loaded += OnLoaded;
            Closed += OnClosed;
            KeyDown += OnKeyDown;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            bodies = SceneBuilder.Build(SceneViewport);

            // Runs once per screen frame. Using the real time between frames keeps the
            // speed the same on a slow PC and a fast PC.
            frameTimer.Start();
            CompositionTarget.Rendering += OnRendering;

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
            CompositionTarget.Rendering -= OnRendering;

            // Stop the sensor cleanly so the Kinect is free for the next run.
            kinect.Dispose();
        }

        private void OnRendering(object sender, EventArgs e)
        {
            double realSeconds = frameTimer.Elapsed.TotalSeconds;
            frameTimer.Restart();

            clock.Advance(realSeconds);

            foreach (BodyVisual body in bodies)
            {
                body.UpdateMotion(clock.Time);
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.IsRepeat)
            {
                return; // holding a key down counts as one press
            }

            switch (e.Key)
            {
                case Key.Space:
                    clock.TogglePause();
                    AppLog.Info(clock.IsPaused ? "Paused" : "Resumed");
                    break;
                case Key.OemPlus:
                case Key.Add:
                    clock.SpeedUp();
                    AppLog.Info("Simulation speed: x" + clock.Speed);
                    break;
                case Key.OemMinus:
                case Key.Subtract:
                    clock.SlowDown();
                    AppLog.Info("Simulation speed: x" + clock.Speed);
                    break;
            }
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
