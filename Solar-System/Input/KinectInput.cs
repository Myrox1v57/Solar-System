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

        private bool hasPosition;     // false until the hand has been seen
        private Point targetPosition; // where the hand says the cursor should be
        private Point position;       // where the cursor is now: it smoothly chases the target
        private bool isGripping;
        private double handMissingSeconds; // how long the hand has been gone

        // Push and pull zoom. "Reach" is how far the hand is in front of the shoulders, in metres.
        private Skeleton[] skeletons;
        private int mainHandTrackingId;
        private HandType mainHandType;
        private bool hasReach;
        private double smoothedReach;
        private double zoomAnchor; // the reach the last zoom step ended at

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
        public event Action<double> Zoomed;

        // Only the mouse uses this: a fist starts a drag with Grabbed.
        public event Action DragStarted { add { } remove { } }

        /// <summary>Holding a hand still is a reliable way to select, because the Kinect can miss a fist.</summary>
        public bool UsesDwell
        {
            get { return true; }
        }

        /// <summary>Where to draw the hand cursor. Null when no hand is raised.</summary>
        public Point? HandPosition
        {
            get { return hasPosition ? position : (Point?)null; }
        }

        /// <summary>True while the hand is closed and held.</summary>
        public bool IsGripping
        {
            get { return isGripping; }
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

        /// <summary>Call once per frame: follows the hand and notices when the hand or the person goes away.</summary>
        public void Update(double seconds)
        {
            if (sensor == null)
            {
                return;
            }

            bool somebodySeen = false;
            HandPointer mainHand = null;
            foreach (HandPointer hand in region.HandPointers)
            {
                somebodySeen = somebodySeen || hand.IsTracked;

                // Near the edge of its zone a hand stops being "interactive" but is still
                // there, so "active" counts too. Otherwise the cursor dies at the corners.
                if (IsMainHand(hand) && hand.IsTracked && (hand.IsActive || hand.IsInteractive))
                {
                    mainHand = hand;
                }
            }

            if (mainHand != null)
            {
                FollowHand(mainHand);
            }
            else
            {
                NoticeMissingHand(seconds);
            }

            if (hasPosition)
            {
                MoveCursor(seconds);
            }

            if (hasPosition)
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

        /// <summary>
        /// We read the hand here on every frame, instead of waiting for the Toolkit's move
        /// events, because those stop as soon as the hand is outside the window.
        /// </summary>
        private void FollowHand(HandPointer hand)
        {
            handMissingSeconds = 0;
            mainHandTrackingId = hand.TrackingId;
            mainHandType = hand.HandType;

            double width = surface.ActualWidth;
            double height = surface.ActualHeight;
            if (width <= 0 || height <= 0)
            {
                return;
            }

            Point handPosition = hand.GetPosition(surface);
            targetPosition = new Point(ToCursorPosition(handPosition.X, width),
                                       ToCursorPosition(handPosition.Y, height));

            if (!hasPosition)
            {
                position = targetPosition; // a new hand starts right where it is
                hasPosition = true;
            }

            // While pushing, the Toolkit pulls the cursor toward this point. Keep it right
            // under the hand, or the cursor would slide away to the middle of the window.
            KinectRegion.SetPressTargetPoint(surface,
                new Point(Clamp(handPosition.X / width, 0, 1), Clamp(handPosition.Y / height, 0, 1)));
        }

        /// <summary>A short dropout must not lose the cursor, so wait a moment first.</summary>
        private void NoticeMissingHand(double seconds)
        {
            if (!hasPosition)
            {
                return;
            }

            handMissingSeconds += seconds;
            if (handMissingSeconds >= SimulationConfig.KinectHandLostSeconds)
            {
                hasPosition = false;
                Left?.Invoke();
            }
        }

        /// <summary>
        /// Turns a hand position into a cursor position, for one direction (left-right or up-down).
        /// The cursor is calm in the middle and speeds up near the edges, so it reaches the edge
        /// of the screen while the hand is still safely inside the area the Kinect can use.
        /// </summary>
        private static double ToCursorPosition(double handPosition, double screenSize)
        {
            // -1 is one edge, 0 the middle and 1 the other edge.
            double fromMiddle = (handPosition / screenSize - 0.5) * 2;
            double hand = Clamp(fromMiddle / SimulationConfig.KinectHandReach, -1, 1);

            // A straight line plus a curve: together they always end exactly at 1.
            double calm = SimulationConfig.KinectPointerSensitivity;
            double cursor = calm * hand + (1 - calm) * hand * hand * hand;

            return (cursor + 1) / 2 * screenSize;
        }

        private static double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        /// <summary>
        /// The Kinect sends about 30 updates per second. Chasing the latest one on every
        /// screen frame keeps the cursor smooth and removes the shaking of the hand.
        /// </summary>
        private void MoveCursor(double seconds)
        {
            Point oldPosition = position;
            double chase = 1 - Math.Exp(-SimulationConfig.KinectPointerSmoothing * seconds);
            position = oldPosition + (targetPosition - oldPosition) * chase;

            Moved?.Invoke(position);

            if (isGripping)
            {
                DragMoved?.Invoke((position - oldPosition) * SimulationConfig.KinectDragSensitivity);
            }
        }

        private void OnGrip(object sender, HandPointerEventArgs e)
        {
            if (!IsMainHand(e.HandPointer) || !hasPosition)
            {
                return;
            }

            isGripping = true;
            hasReach = false; // the zoom starts counting from where the fist closed
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

        /// <summary>
        /// The Toolkit's hand pointer has no depth, so the zoom reads it from the skeleton.
        /// Only needed while a fist is closed.
        /// </summary>
        private void OnSkeletonFrame(object sender, SkeletonFrameReadyEventArgs e)
        {
            if (!isGripping)
            {
                return;
            }

            try
            {
                using (SkeletonFrame frame = e.OpenSkeletonFrame())
                {
                    if (frame == null)
                    {
                        return;
                    }

                    if (skeletons == null || skeletons.Length != frame.SkeletonArrayLength)
                    {
                        skeletons = new Skeleton[frame.SkeletonArrayLength];
                    }
                    frame.CopySkeletonDataTo(skeletons);
                }

                foreach (Skeleton skeleton in skeletons)
                {
                    if (skeleton.TrackingState == SkeletonTrackingState.Tracked
                        && skeleton.TrackingId == mainHandTrackingId)
                    {
                        ReadReach(skeleton);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                // For example: the sensor was unplugged in the middle of a frame.
                AppLog.Error("Error while reading the hand depth", ex);
            }
        }

        private void ReadReach(Skeleton skeleton)
        {
            Joint hand = skeleton.Joints[mainHandType == HandType.Left ? JointType.HandLeft : JointType.HandRight];
            Joint shoulders = skeleton.Joints[JointType.ShoulderCenter];

            // A joint the Kinect only guessed would make the zoom jump.
            if (hand.TrackingState == JointTrackingState.Tracked
                && shoulders.TrackingState == JointTrackingState.Tracked)
            {
                // Measured from the shoulders, so swaying or stepping does not zoom. Only the arm does.
                ZoomFromReach(shoulders.Position.Z - hand.Position.Z);
            }
        }

        /// <summary>
        /// Pushing the fist forward zooms in, and pulling it back zooms out. The small dead
        /// zone means a shaking hand does nothing, and changing direction needs a real movement.
        /// </summary>
        private void ZoomFromReach(double reach)
        {
            if (!hasReach)
            {
                hasReach = true;
                smoothedReach = reach;
                zoomAnchor = reach;
                return;
            }

            smoothedReach += (reach - smoothedReach) * SimulationConfig.KinectReachSmoothing;

            double moved = smoothedReach - zoomAnchor;
            if (Math.Abs(moved) <= SimulationConfig.KinectZoomDeadZoneMeters)
            {
                return;
            }

            double zoomMeters = moved - Math.Sign(moved) * SimulationConfig.KinectZoomDeadZoneMeters;
            zoomAnchor += zoomMeters;
            Zoomed?.Invoke(zoomMeters * SimulationConfig.KinectZoomStepsPerMeter);
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
                sensor.SkeletonFrameReady += OnSkeletonFrame;

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
                sensor.SkeletonFrameReady -= OnSkeletonFrame;
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
