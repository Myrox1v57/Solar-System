using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Solar_System.Camera;
using Solar_System.Config;
using Solar_System.Input;
using Solar_System.Scene;
using Solar_System.Simulation;
using Solar_System.UI;

namespace Solar_System.Interaction
{
    /// <summary>
    /// The rules of the app: what hover, select and drag do in Overview and in Focus mode.
    /// It only talks to IPointerInput, so it works the same for the mouse and the Kinect.
    /// </summary>
    public sealed class InteractionController
    {
        private readonly Viewport3D viewport;
        private readonly List<BodyVisual> bodies;
        private readonly CameraController camera;
        private readonly SimulationClock clock;
        private readonly HoverMarker hoverMarker;
        private readonly InfoPanel infoPanel;
        private readonly UIElement backButton;

        private BodyVisual focusBody;   // null means Overview mode
        private Point? pointer;         // null when the pointer is not in the window
        private bool isDragging;

        // Focus mode spinning. The speed is in degrees per second.
        private double spinSpeed;
        private double draggedDegrees;  // how far the user dragged since the last frame

        public InteractionController(Viewport3D viewport, List<BodyVisual> bodies, CameraController camera,
            SimulationClock clock, IPointerInput input, HoverMarker hoverMarker, InfoPanel infoPanel, UIElement backButton)
        {
            this.viewport = viewport;
            this.bodies = bodies;
            this.camera = camera;
            this.clock = clock;
            this.hoverMarker = hoverMarker;
            this.infoPanel = infoPanel;
            this.backButton = backButton;

            input.Moved += position => pointer = position;
            input.Left += () => pointer = null;
            input.Selected += OnSelected;
            input.DragStarted += () => isDragging = true;
            input.DragMoved += OnDragMoved;
            input.DragEnded += () => isDragging = false;
            input.Zoomed += OnZoomed;
        }

        private bool IsInOverview
        {
            get { return focusBody == null; }
        }

        /// <summary>Back to Overview. Does nothing if we are already there.</summary>
        public void GoBack()
        {
            if (IsInOverview)
            {
                return;
            }

            AppLog.Info("Back to Overview");
            focusBody = null;
            spinSpeed = 0;
            camera.FlyToOverview();
            clock.IsSlowMotion = false;
            infoPanel.SlideOut();
            backButton.Visibility = Visibility.Collapsed;
        }


        public void ResetCamera()
        {
            if (IsInOverview)
            {
                camera.Reset();
            }
        }

        /// <summary>Call once per frame, after the planets have moved.</summary>
        public void Update(double seconds)
        {
            if (focusBody != null)
            {
                UpdateSpin(seconds);
            }

            camera.Update(seconds);

            // Check every frame, not only when the pointer moves: planets move under it too.
            hoverMarker.Show(CanHover() ? PickAtPointer() : null);
            hoverMarker.Update();
        }

        private bool CanHover()
        {
            return IsInOverview && !camera.IsFlying && !isDragging && pointer.HasValue;
        }

        private BodyVisual PickAtPointer()
        {
            return BodyPicker.Pick(viewport, bodies, pointer.Value);
        }

        private void OnSelected(Point position)
        {
            if (!IsInOverview || camera.IsFlying)
            {
                return;
            }

            BodyVisual body = BodyPicker.Pick(viewport, bodies, position);
            if (body == null)
            {
                return;
            }

            AppLog.Info("Focus on " + body.Body.Name);
            focusBody = body;
            spinSpeed = SimulationConfig.FocusSpinDegreesPerSecond;
            camera.FlyTo(body);
            clock.IsSlowMotion = true;
            infoPanel.SlideIn(body.Body);
            backButton.Visibility = Visibility.Visible;
        }

        private void OnDragMoved(Vector movement)
        {
            if (IsInOverview)
            {
                if (!camera.IsFlying)
                {
                    camera.Drag(movement.X, movement.Y);
                }
            }
            else
            {
                draggedDegrees += movement.X * SimulationConfig.SpinDragDegreesPerPixel;
            }
        }

        private void OnZoomed(double steps)
        {
            if (IsInOverview && !camera.IsFlying)
            {
                camera.Zoom(steps);
            }
        }

        /// <summary>
        /// While dragging, the planet follows the pointer. After letting go it keeps
        /// that speed, which then fades back to the normal slow rotation.
        /// </summary>
        private void UpdateSpin(double seconds)
        {
            if (seconds <= 0)
            {
                return;
            }

            if (isDragging)
            {
                focusBody.AddSpin(draggedDegrees);
                spinSpeed += (draggedDegrees / seconds - spinSpeed) * 0.3; // remember the recent speed
            }
            else
            {
                focusBody.AddSpin(spinSpeed * seconds);
                spinSpeed += (SimulationConfig.FocusSpinDegreesPerSecond - spinSpeed)
                             * (1 - Math.Exp(-SimulationConfig.SpinMomentumFade * seconds));
            }
            draggedDegrees = 0;
        }
    }
}
