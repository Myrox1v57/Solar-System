using System;
using System.Windows;
using System.Windows.Input;
using Solar_System.Config;

namespace Solar_System.Input
{
    /// <summary>
    /// The mouse as a pointer. Press and release in one spot = select.
    /// Press and move = drag. Keys are handled in MainWindow.
    /// </summary>
    public sealed class MouseInput : IPointerInput
    {
        private readonly FrameworkElement surface;
        private bool isPressed;
        private bool isDragging;
        private Point pressPosition;
        private Point lastPosition;

        public MouseInput(FrameworkElement surface)
        {
            this.surface = surface;
            surface.MouseMove += OnMouseMove;
            surface.MouseLeave += OnMouseLeave;
            surface.MouseLeftButtonDown += OnMouseDown;
            surface.MouseLeftButtonUp += OnMouseUp;
            surface.MouseWheel += OnMouseWheel;
            surface.LostMouseCapture += OnLostMouseCapture;
        }

        public event Action<Point> Moved;
        public event Action Left;
        public event Action<Point> Selected;
        public event Action DragStarted;
        public event Action<Vector> DragMoved;
        public event Action DragEnded;
        public event Action<double> Zoomed;

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            isPressed = true;
            pressPosition = e.GetPosition(surface);
            lastPosition = pressPosition;
            surface.CaptureMouse(); // keep getting moves even if the mouse leaves the window
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            Point position = e.GetPosition(surface);
            Moved?.Invoke(position);

            if (!isPressed)
            {
                return;
            }

            if (!isDragging && (position - pressPosition).Length >= SimulationConfig.DragThresholdPixels)
            {
                isDragging = true;
                DragStarted?.Invoke();
            }

            if (isDragging)
            {
                DragMoved?.Invoke(position - lastPosition);
                lastPosition = position;
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            bool wasClick = isPressed && !isDragging;
            surface.ReleaseMouseCapture(); // this also ends a drag, see OnLostMouseCapture

            if (wasClick)
            {
                Selected?.Invoke(e.GetPosition(surface));
            }
        }

        private void OnLostMouseCapture(object sender, MouseEventArgs e)
        {
            isPressed = false;
            if (isDragging)
            {
                isDragging = false;
                DragEnded?.Invoke();
            }
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (!isPressed)
            {
                Left?.Invoke();
            }
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            Zoomed?.Invoke(e.Delta / (double)Mouse.MouseWheelDeltaForOneLine);
        }
    }
}
