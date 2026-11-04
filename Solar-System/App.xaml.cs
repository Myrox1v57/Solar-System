using System.Windows;
using System.Windows.Threading;

namespace Solar_System
{
    /// <summary>
    /// Application start-up.
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppLog.StartNewLog();
            AppLog.Info("App starting");

            // Last safety net: write unexpected errors to SolarSystem.log so we can find the cause.
            DispatcherUnhandledException += OnUnhandledException;

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            AppLog.Info("App closing");
            base.OnExit(e);
        }

        private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            AppLog.Error("Unexpected error", e.Exception);
            // e.Handled stays false: during development we want to see the crash in Visual Studio.
        }
    }
}
