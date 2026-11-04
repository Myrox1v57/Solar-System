using System;
using System.Windows.Threading;
using Microsoft.Kinect;

namespace Solar_System.Input
{
    /// <summary>Is a Kinect ready to use?</summary>
    public enum KinectConnectionState
    {
        NoKinect,
        Connected
    }

    /// <summary>
    /// Kinect input.
    /// Phase 0: finds a Kinect, starts it and reports when it is plugged in or unplugged.
    /// Hand pointer, grip, press and dwell are added in Phase 4.
    /// The Kinect must never crash the app, so every Kinect call is wrapped in try/catch.
    /// </summary>
    public sealed class KinectInput : IDisposable
    {
        private readonly Dispatcher dispatcher;
        private KinectSensor sensor;
        private bool listeningForChanges;

        public KinectInput()
        {
            // Remember the UI thread. Kinect events can arrive on other threads.
            dispatcher = Dispatcher.CurrentDispatcher;
            State = KinectConnectionState.NoKinect;
            StatusMessage = "Търсене на Kinect...";
        }

        /// <summary>Raised (on the UI thread) whenever State or StatusMessage changes.</summary>
        public event EventHandler StatusChanged;

        public KinectConnectionState State { get; private set; }

        /// <summary>Short text for the screen, shown in Bulgarian.</summary>
        public string StatusMessage { get; private set; }

        /// <summary>Start looking for a Kinect. Safe to call when no Kinect (or no SDK) is installed.</summary>
        public void Start()
        {
            try
            {
                if (!listeningForChanges)
                {
                    KinectSensor.KinectSensors.StatusChanged += OnSensorStatusChanged;
                    listeningForChanges = true;
                }
                FindAndStartSensor();
            }
            catch (Exception ex)
            {
                // Usually means the Kinect runtime/drivers are not installed on this PC.
                AppLog.Error("Could not access the Kinect SDK", ex);
                SetStatus(KinectConnectionState.NoKinect, "Няма Kinect (инсталиран ли е Kinect SDK 1.8?)");
            }
        }

        /// <summary>Called by the SDK when any Kinect is plugged in, unplugged, loses power, etc.</summary>
        private void OnSensorStatusChanged(object sender, StatusChangedEventArgs e)
        {
            // Read the values now, then handle them on the UI thread.
            KinectSensor changedSensor = e.Sensor;
            KinectStatus newStatus = e.Status;

            dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    AppLog.Info("Kinect status changed: " + newStatus);

                    // Our sensor was unplugged or broke: let it go.
                    if (sensor != null && changedSensor == sensor && newStatus != KinectStatus.Connected)
                    {
                        StopSensor();
                    }

                    // No sensor in use: try to find one (maybe one was just plugged in).
                    if (sensor == null)
                    {
                        FindAndStartSensor();
                    }
                }
                catch (Exception ex)
                {
                    AppLog.Error("Error while handling a Kinect status change", ex);
                }
            }));
        }

        /// <summary>Use the first connected Kinect. If none, show why.</summary>
        private void FindAndStartSensor()
        {
            foreach (KinectSensor candidate in KinectSensor.KinectSensors)
            {
                if (candidate.Status != KinectStatus.Connected)
                {
                    continue;
                }

                try
                {
                    candidate.Start();
                    sensor = candidate;
                    AppLog.Info("Kinect started");
                    SetStatus(KinectConnectionState.Connected, "Kinect е свързан");
                    return;
                }
                catch (Exception ex)
                {
                    // For example: another program is already using the Kinect.
                    AppLog.Error("Kinect found but could not be started", ex);
                }
            }

            SetStatus(KinectConnectionState.NoKinect, DescribeWhyNoKinect());
        }

        /// <summary>A helpful message based on what the SDK can see.</summary>
        private static string DescribeWhyNoKinect()
        {
            foreach (KinectSensor candidate in KinectSensor.KinectSensors)
            {
                switch (candidate.Status)
                {
                    case KinectStatus.NotPowered:
                        return "Kinect няма захранване - проверете адаптера";
                    case KinectStatus.Initializing:
                        return "Kinect се стартира...";
                    case KinectStatus.InsufficientBandwidth:
                        return "USB портът е зает - опитайте друг USB порт";
                    case KinectStatus.Connected:
                        return "Kinect се използва от друга програма";
                }
            }
            return "Няма свързан Kinect";
        }

        private void StopSensor()
        {
            if (sensor == null)
            {
                return;
            }

            try
            {
                sensor.Stop();
                AppLog.Info("Kinect stopped");
            }
            catch (Exception ex)
            {
                AppLog.Error("Error while stopping the Kinect", ex);
            }
            sensor = null;
            SetStatus(KinectConnectionState.NoKinect, "Няма свързан Kinect");
        }

        private void SetStatus(KinectConnectionState state, string message)
        {
            State = state;
            StatusMessage = message;
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>Stop the sensor cleanly when the app closes.</summary>
        public void Dispose()
        {
            try
            {
                if (listeningForChanges)
                {
                    KinectSensor.KinectSensors.StatusChanged -= OnSensorStatusChanged;
                    listeningForChanges = false;
                }
            }
            catch (Exception ex)
            {
                AppLog.Error("Error while unsubscribing from Kinect events", ex);
            }
            StopSensor();
        }
    }
}
