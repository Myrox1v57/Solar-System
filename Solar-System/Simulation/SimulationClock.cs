using System;
using Solar_System.Config;

namespace Solar_System.Simulation
{
    /// <summary>
    /// Keeps the simulation time. Planets work out where they are from this time, so
    /// pausing or changing the speed only has to change the clock.
    /// </summary>
    public sealed class SimulationClock
    {
        private double slowMotionFactor = 1;

        /// <summary>Simulation time in seconds. At speed 1 it runs like a normal clock.</summary>
        public double Time { get; private set; }

        /// <summary>1 = normal, 2 = twice as fast, 0.5 = half as fast.</summary>
        public double Speed { get; private set; } = 1;

        public bool IsPaused { get; private set; }

        /// <summary>True in Focus mode. The orbits then gently slow down until they nearly stop.</summary>
        public bool IsSlowMotion { get; set; }

        /// <summary>True while the pointer is on a planet, so a fast planet does not run away from it.</summary>
        public bool IsHoverSlowdown { get; set; }

        /// <summary>Call once per frame with the real seconds since the last frame.</summary>
        public void Advance(double realSeconds)
        {
            double seconds = Math.Min(realSeconds, SimulationConfig.MaxFrameSeconds);

            double target = 1;
            if (IsHoverSlowdown)
            {
                target = SimulationConfig.HoverSimulationSpeed;
            }
            if (IsSlowMotion)
            {
                target = SimulationConfig.FocusSimulationSpeed;
            }

            slowMotionFactor += (target - slowMotionFactor)
                                * (1 - Math.Exp(-SimulationConfig.SlowMotionSmoothing * seconds));

            if (!IsPaused)
            {
                Time += seconds * Speed * slowMotionFactor;
            }
        }

        public void TogglePause()
        {
            IsPaused = !IsPaused;
        }

        public void SpeedUp()
        {
            Speed = Math.Min(Speed * SimulationConfig.SimulationSpeedStep, SimulationConfig.MaxSimulationSpeed);
        }

        public void SlowDown()
        {
            Speed = Math.Max(Speed / SimulationConfig.SimulationSpeedStep, SimulationConfig.MinSimulationSpeed);
        }
    }
}
