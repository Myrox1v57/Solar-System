using System;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit.Controls;
using Solar_System.Config;

namespace Solar_System.Input
{
    /// <summary>What the Kinect is doing right now.</summary>
    public enum KinectState
    {
        TurnedOff,      // switched off with the K key
        NoKinect,       // no working sensor
        WaitingForUser, // sensor works, but nobody is in front of it
        UserSeen,       // somebody is there, but no hand is raised
        Tracking        // a raised hand controls the pointer
    }

    /// <summary>
    /// The Kinect as a pointer. The Toolkit's KinectRegion does the hard work (hand cursor,
    /// fist and push detection) and this class passes it on through IPointerInput.
    /// Only the main hand of the main person counts, so people walking by are ignored.
    /// The Kinect must never crash the app, so every sensor call is wrapped in try/catch.
    /// </summary>
    public sealed class KinectInput : IPointerInput, IDisposable
    {
        private readonly Dispatcher dispatcher;
        private readonly KinectRegion region;
        private readonly FrameworkElement surface;

        private KinectSensor sensor;
        private bool isEnabled = true;
        private bool listeningForChanges;

        private bool hasPosition;  // false until the hand has been seen
        private Point position;    // smoothed hand position
        private bool isGripping;

        public KinectInput(KinectRegion region, FrameworkElement surface)
        {
            // Remember the UI thread. Sensor status events can arrive on other threads.
            dispatcher = Dispatcher.CurrentDispatcher;
            this.region = region;
            this.surface = surface;

            State = KinectState.NoKinect;
            StatusMessage = "Търсене на Kinect...";

            // Tell the Toolkit that fists and pushes mean something on this element.
            KinectRegion.SetIsGripTarget(surface, true);
            KinectRegion.SetIsPressTarget(surface, true);

            KinectRegion.AddHandPointerMoveHandler(surface, OnHandMoved);
            KinectRegion.AddHandPointerGripHandler(surface, OnGrip);
            KinectRegion.AddHandPointerGripReleaseHandler(surface, OnGripRelease);
            KinectRegion.AddHandPointerPressHandler(surface, OnPress);
            KinectRegion.AddHandPointerLostCaptureHandler(surface, OnLostCapture);
            KinectRegion.AddQueryInteractionStatusHandler(surface, OnQueryInteractionStatus);
        }

        public event Action<Point> Moved;
        public event Action Left;
        public event Action<Point> Selected;
        public event Action<Point> Grabbed;
        public event Action<Vector> DragMoved;
        public event Action DragEnded;

        // Only the mouse uses these two: a fist starts a drag with Grabbed, and a hand cannot zoom.
        public event Action DragStarted { add { } remove { } }
        public event Action<double> Zoomed { add { } remove { } }

        /// <summary>Holding a hand still is a reliable way to select, because the Kinect can miss a fist.</summary>
        public bool UsesDwell
        {
            get { return true; }
        }

        /// <summary>Raised (on the UI thread) whenever State or StatusMessage changes.</summary>
        public event EventHandler StatusChanged;

        public KinectState State { get; private set; }

        /// <summary>Short text for the screen, in Bulgarian.</summary>
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
                SetStatus(KinectState.NoKinect, "Няма Kinect (инсталиран ли е Kinect SDK 1.8?)");
            }
        }

        /// <summary>The K key: stop using the Kinect, or start using it again.</summary>
        public void ToggleEnabled()
        {
            isEnabled = !isEnabled;
            AppLog.Info(isEnabled ? "Kinect turned on" : "Kinect turned off");

            if (isEnabled)
            {
                Start();
            }
            else
            {
                StopSensor();
                SetStatus(KinectState.TurnedOff, "Kinect е изключен (клавиш K)");
            }
        }

        /// <summary>Call once per frame: notices when the hand or the person goes away.</summary>
        public void Update()
        {
            if (sensor == null)
            {
                return;
            }

            bool somebodySeen = false;
            bool handRaised = false;
            foreach (HandPointer hand in region.HandPointers)
            {
                somebodySeen = somebodySeen || hand.IsTracked;
                handRaised = handRaised || (IsMainHand(hand) && hand.IsInteractive);
            }

            if (!handRaised && hasPosition)
            {
                hasPosition = false;
                Left?.Invoke();
            }

            if (handRaised)
            {
                SetStatus(KinectState.Tracking, "Проследяване");
            }
            else if (somebodySeen)
            {
                SetStatus(KinectState.UserSeen, "Kinect е свързан - вдигнете ръка");
            }
            else
            {
                SetStatus(KinectState.WaitingForUser, "Застанете пред камерата");
            }
        }

        private static bool IsMainHand(HandPointer hand)
        {
            return hand.IsPrimaryUser && hand.IsPrimaryHandOfUser;
        }

        private void OnHandMoved(object sender, HandPointerEventArgs e)
        {
            HandPointer hand = e.HandPointer;
            if (!IsMainHand(hand) || !hand.IsInteractive)
            {
                return;
            }

            Point rawPosition = hand.GetPosition(surface);
            Point oldPosition = hasPosition ? position : rawPosition;

            // Follow the hand only part of the way each time, which removes the shaking.
            position = oldPosition + (rawPosition - oldPosition) * SimulationConfig.KinectPointerSmoothing;
            hasPosition = true;

            // While pushing, the Toolkit pulls the cursor toward this point. Keep it right
            // under the hand, or the cursor would slide away to the middle of the window.
            KinectRegion.SetPressTargetPoint(surface,
                new Point(rawPosition.X / surface.ActualWidth, rawPosition.Y / surface.ActualHeight));

            Moved?.Invoke(position);

            if (isGripping)
            {
                DragMoved?.Invoke((position - oldPosition) * SimulationConfig.KinectDragSensitivity);
            }

            e.Handled = true;
        }

        private void OnGrip(object sender, HandPointerEventArgs e)
        {
            if (!IsMainHand(e.HandPointer) || !hasPosition)
            {
                return;
            }

            isGripping = true;
            e.HandPointer.Capture(surface); // keep getting moves until the hand opens
            Grabbed?.Invoke(position);
            e.Handled = true;
        }

        private void OnGripRelease(object sender, HandPointerEventArgs e)
        {
            if (isGripping && IsMainHand(e.HandPointer))
            {
                e.HandPointer.Capture(null); // this ends the drag, see OnLostCapture
                e.Handled = true;
            }
        }

        /// <summary>Also runs if the hand gets lost during a drag, so a drag can never get stuck.</summary>
        private void OnLostCapture(object sender, HandPointerEventArgs e)
        {
            if (isGripping)
            {
                isGripping = false;
                DragEnded?.Invoke();
            }
        }

        private void OnPress(object sender, HandPointerEventArgs e)
        {
            if (IsMainHand(e.HandPointer) && hasPosition && !isGripping)
            {
                Selected?.Invoke(position);
                e.Handled = true;
            }
        }

        /// <summary>Tells the Toolkit to show the closed-fist cursor while we are dragging.</summary>
        private void OnQueryInteractionStatus(object sender, QueryInteractionStatusEventArgs e)
        {
            if (Equals(e.HandPointer.Captured, surface))
            {
                e.IsInGripInteraction = isGripping;
                e.Handled = true;
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
                        SetStatus(KinectState.NoKinect, "Няма свързан Kinect");
                    }

                    // No sensor in use: try to find one (maybe one was just plugged in).
                    if (sensor == null && isEnabled)
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
                if (candidate.Status == KinectStatus.Connected && TryStartSensor(candidate))
                {
                    return;
                }
            }

            SetStatus(KinectState.NoKinect, DescribeWhyNoKinect());
        }

        private bool TryStartSensor(KinectSensor candidate)
        {
            try
            {
                // The hand tracking needs both depth and skeleton data. Near mode stays
                // off: it is for people very close to the sensor, and we stand 2-3 m away.
                candidate.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                candidate.SkeletonStream.Enable();
                candidate.Start();

                region.KinectSensor = candidate;
                sensor = candidate;

                AppLog.Info("Kinect started");
                SetStatus(KinectState.WaitingForUser, "Застанете пред камерата");
                return true;
            }
            catch (Exception ex)
            {
                // For example: another program is already using the Kinect.
                AppLog.Error("Kinect found but could not be started", ex);
                region.KinectSensor = null;
                StopQuietly(candidate);
                return false;
            }
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

            // Forget the hand first, so a drag or hover cannot stay active.
            if (isGripping)
            {
                isGripping = false;
                DragEnded?.Invoke();
            }
            if (hasPosition)
            {
                hasPosition = false;
                Left?.Invoke();
            }

            try
            {
                region.KinectSensor = null;
            }
            catch (Exception ex)
            {
                AppLog.Error("Error while disconnecting the hand tracking", ex);
            }

            StopQuietly(sensor);
            sensor = null;
            AppLog.Info("Kinect stopped");
        }

        /// <summary>An unplugged sensor can throw errors here, and that must not matter.</summary>
        private static void StopQuietly(KinectSensor kinectSensor)
        {
            try
            {
                kinectSensor.Stop();
                kinectSensor.DepthStream.Disable();
                kinectSensor.SkeletonStream.Disable();
            }
            catch (Exception ex)
            {
                AppLog.Error("Error while stopping the Kinect", ex);
            }
        }

        private void SetStatus(KinectState state, string message)
        {
            if (state == State && message == StatusMessage)
            {
                return;
            }

            State = state;
            StatusMessage = message;
            AppLog.Info("Kinect state: " + state);
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
