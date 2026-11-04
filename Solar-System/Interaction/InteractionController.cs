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
    /// The rules of the app: what hover, select, fist and drag do in Overview and in Focus mode.
    /// It only talks to IPointerInput, so it works the same for the mouse and the Kinect.
    /// </summary>
    public sealed class InteractionController
    {
        private const double BackButtonExtraMargin = 20; // the hand may miss the button by this much

        private readonly Viewport3D viewport;
        private readonly List<BodyVisual> bodies;
        private readonly CameraController camera;
        private readonly SimulationClock clock;
        private readonly HoverMarker hoverMarker;
        private readonly DwellRing dwellRing;
        private readonly InfoPanel infoPanel;
        private readonly FrameworkElement backButton;
        private readonly DwellTimer dwellTimer = new DwellTimer();

        private BodyVisual focusBody;   // null means Overview mode
        private Point? pointer;         // null when no pointer is in the window
        private bool pointerUsesDwell;  // true when the last pointer to move was the Kinect hand
        private bool isDragging;

        // Focus mode spinning. The speed is in degrees per second.
        private double spinSpeed;
        private double draggedDegrees;  // how far the user dragged since the last frame

        public InteractionController(Viewport3D viewport, List<BodyVisual> bodies, CameraController camera,
            SimulationClock clock, IEnumerable<IPointerInput> inputs, HoverMarker hoverMarker, DwellRing dwellRing,
            InfoPanel infoPanel, FrameworkElement backButton)
        {
            this.viewport = viewport;
            this.bodies = bodies;
            this.camera = camera;
            this.clock = clock;
            this.hoverMarker = hoverMarker;
            this.dwellRing = dwellRing;
            this.infoPanel = infoPanel;
            this.backButton = backButton;

            foreach (IPointerInput input in inputs)
            {
                Listen(input);
            }
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
            isDragging = false;
            camera.FlyToOverview();
            clock.IsSlowMotion = false;
            infoPanel.SlideOut();
            Fade.Out(backButton, SimulationConfig.ButtonFadeSeconds);
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
            BodyVisual hovered = CanHover() ? BodyPicker.Pick(viewport, bodies, pointer.Value) : null;
            hoverMarker.Show(hovered);
            hoverMarker.Update();
            clock.IsHoverSlowdown = hovered != null;

            // The XAML lights up the button when this is set. A mouse lights it up by itself.
            bool handOverBack = pointerUsesDwell && pointer.HasValue && IsOverBackButton(pointer.Value);
            backButton.Tag = handOverBack ? "hand" : null;

            UpdateDwell(hovered, seconds);
        }

        private void Listen(IPointerInput input)
        {
            input.Moved += position =>
            {
                pointer = position;
                pointerUsesDwell = input.UsesDwell;
            };
            input.Left += () => pointer = null;
            input.Selected += OnSelected;
            input.Grabbed += OnGrabbed;
            input.DragStarted += () => isDragging = true;
            input.DragMoved += OnDragMoved;
            input.DragEnded += () => isDragging = false;
            input.Zoomed += OnZoomed;
        }

        private bool CanHover()
        {
            return IsInOverview && !camera.IsFlying && !isDragging && pointer.HasValue;
        }

        private bool IsOverBackButton(Point position)
        {
            if (backButton.Visibility != Visibility.Visible)
            {
                return false;
            }

            Point topLeft = backButton.TranslatePoint(new Point(0, 0), viewport);
            var area = new Rect(topLeft, backButton.RenderSize);
            area.Inflate(BackButtonExtraMargin, BackButtonExtraMargin);
            return area.Contains(position);
        }

        private void OnSelected(Point position)
        {
            if (camera.IsFlying)
            {
                return;
            }

            if (IsOverBackButton(position))
            {
                GoBack();
            }
            else if (IsInOverview)
            {
                EnterFocus(BodyPicker.Pick(viewport, bodies, position));
            }
        }

        /// <summary>A fist selects what is under the hand. On empty space it starts a drag.</summary>
        private void OnGrabbed(Point position)
        {
            if (camera.IsFlying)
            {
                return;
            }

            if (IsOverBackButton(position))
            {
                GoBack();
                return;
            }

            BodyVisual body = IsInOverview ? BodyPicker.Pick(viewport, bodies, position) : null;
            if (body != null)
            {
                EnterFocus(body);
            }
            else
            {
                isDragging = true;
            }
        }

        private void EnterFocus(BodyVisual body)
        {
            if (body == null)
            {
                return;
            }

            AppLog.Info("Focus on " + body.Body.Name);
            focusBody = body;
            spinSpeed = SimulationConfig.FocusSpinDegreesPerSecond;
            isDragging = false;
            camera.FlyTo(body);
            clock.IsSlowMotion = true;
            infoPanel.SlideIn(body.Body);
            Fade.In(backButton, SimulationConfig.ButtonFadeSeconds);
        }

        private void OnDragMoved(Vector movement)
        {
            if (!isDragging)
            {
                return; // for example a fist that just selected a planet and is still closed
            }

            if (!IsInOverview)
            {
                draggedDegrees += movement.X * SimulationConfig.SpinDragDegreesPerPixel;
            }
            else if (!camera.IsFlying)
            {
                camera.Drag(movement.X, movement.Y);
            }
        }

        private void OnZoomed(double steps)
        {
            if (IsInOverview && !camera.IsFlying)
            {
                camera.Zoom(steps);
            }
        }

        /// <summary>Holding the hand still on a planet or on the Back button selects it.</summary>
        private void UpdateDwell(BodyVisual hovered, double seconds)
        {
            object target = null;
            if (pointerUsesDwell && pointer.HasValue && !isDragging && !camera.IsFlying)
            {
                target = IsInOverview ? (object)hovered : (IsOverBackButton(pointer.Value) ? backButton : null);
            }

            double progress = dwellTimer.Update(target, pointer ?? new Point(), seconds);
            dwellRing.Show(pointer, progress);

            if (progress >= 1)
            {
                dwellTimer.Reset();
                dwellRing.Show(null, 0);

                if (IsInOverview)
                {
                    EnterFocus(hovered);
                }
                else
                {
                    GoBack();
                }
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
