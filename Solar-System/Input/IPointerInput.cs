using System;
using System.Windows;

namespace Solar_System.Input
{
    /// <summary>
    /// What any pointer can do. The mouse uses this now, the Kinect hand will use it in
    /// Phase 4, and the rest of the app does not know or care which one is in use.
    /// All positions are in pixels on the 3D view.
    /// </summary>
    public interface IPointerInput
    {
        /// <summary>The pointer moved to this position (used for hover).</summary>
        event Action<Point> Moved;

        /// <summary>The pointer left the window.</summary>
        event Action Left;

        /// <summary>The user selected whatever is at this position.</summary>
        event Action<Point> Selected;

        /// <summary>The user closed a fist at this position. Only the Kinect has this.</summary>
        event Action<Point> Grabbed;

        event Action DragStarted;

        /// <summary>The pointer moved this far during a drag.</summary>
        event Action<Vector> DragMoved;

        event Action DragEnded;

        /// <summary>Zoom in (positive) or out (negative), in steps.</summary>
        event Action<double> Zoomed;

        /// <summary>True if keeping the pointer still over something should select it.</summary>
        bool UsesDwell { get; }
    }
}
