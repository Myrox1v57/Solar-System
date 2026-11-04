using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Solar_System.Camera;
using Solar_System.Config;
using Solar_System.Input;
using Solar_System.Interaction;
using Solar_System.Model;
using Solar_System.Scene;
using Solar_System.Simulation;
using Solar_System.UI;

namespace Solar_System
{
    /// <summary>
    /// The main window. Wires all parts of the app together, runs the
    /// per-frame update and handles the keyboard.
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly KinectInput kinect;
        private readonly CameraController cameraController = new CameraController();
        private readonly SimulationClock clock = new SimulationClock();
        private readonly Stopwatch frameTimer = new Stopwatch();
        private List<BodyVisual> bodies = new List<BodyVisual>();
        private InteractionController interaction;

        private bool isFullscreen;
        private WindowState stateBeforeFullscreen;

        public MainWindow()
        {
            InitializeComponent();

            kinect = new KinectInput(KinectArea, InputSurface);
            SceneViewport.Camera = cameraController.Camera;

            kinect.StatusChanged += OnKinectStatusChanged;
            Loaded += OnLoaded;
            Closed += OnClosed;
            KeyDown += OnKeyDown;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            bodies = SceneBuilder.Build(SceneViewport);
            PlanetDataLoader.AttachInfo(bodies.ConvertAll(visual => visual.Body));

            interaction = new InteractionController(
                SceneViewport, bodies, cameraController, clock,
                new IPointerInput[] { new MouseInput(InputSurface), kinect },
                new HoverMarker(SceneViewport, HoverRing, HoverLabel),
                new DwellRing(DwellProgressRing),
                PlanetInfoPanel, BackButton);

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

            kinect.Update();
            interaction.Update(Math.Min(realSeconds, SimulationConfig.MaxFrameSeconds));
        }

        private void OnBackClicked(object sender, RoutedEventArgs e)
        {
            interaction.GoBack();
        }

        /// <summary>Fullscreen covers the taskbar too: the border must go before maximizing.</summary>
        private void ToggleFullscreen()
        {
            isFullscreen = !isFullscreen;

            if (isFullscreen)
            {
                stateBeforeFullscreen = WindowState;
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize; // otherwise the window is a bit bigger than the screen
                WindowState = WindowState.Maximized;
            }
            else
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                ResizeMode = ResizeMode.CanResize;
                WindowState = stateBeforeFullscreen;
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
                case Key.Escape:
                case Key.Back:
                    interaction.GoBack();
                    break;
                case Key.R:
                    interaction.ResetCamera();
                    break;
                case Key.F11:
                    ToggleFullscreen();
                    break;
                case Key.K:
                    kinect.ToggleEnabled();
                    break;
            }
        }

        private void OnKinectStatusChanged(object sender, EventArgs e)
        {
            ShowKinectStatus();
        }

        private void ShowKinectStatus()
        {
            KinectStatus.Show(kinect.State, kinect.StatusMessage);

            bool waitingForHand = kinect.State == KinectState.WaitingForUser || kinect.State == KinectState.UserSeen;
            RaiseHandPrompt.Visibility = waitingForHand ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
