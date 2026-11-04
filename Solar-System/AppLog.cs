using System;
using System.Diagnostics;
using System.IO;

namespace Solar_System
{
    /// <summary>
    /// Very small logger. Writes to the Visual Studio "Output" window and to
    /// SolarSystem.log next to the .exe (the file is overwritten on every start).
    /// Logging must never crash the app, so all file errors are ignored.
    /// </summary>
    public static class AppLog
    {
        private static readonly object fileLock = new object();
        private static readonly string logPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SolarSystem.log");

        /// <summary>Call once at startup: clears the log from the previous run.</summary>
        public static void StartNewLog()
        {
            try
            {
                lock (fileLock)
                {
                    File.WriteAllText(logPath, "=== Kinect Solar System started " + DateTime.Now + " ===" + Environment.NewLine);
                }
            }
            catch
            {
                // Ignore: maybe the folder is read-only. The Output window still works.
            }
        }

        public static void Info(string message) { Write("INFO", message); }

        public static void Warning(string message) { Write("WARN", message); }

        public static void Error(string message, Exception ex = null)
        {
            Write("ERROR", ex == null ? message : message + ": " + ex);
        }

        private static void Write(string level, string message)
        {
            string line = DateTime.Now.ToString("HH:mm:ss") + " [" + level + "] " + message;
            Debug.WriteLine(line);
            try
            {
                lock (fileLock)
                {
                    File.AppendAllText(logPath, line + Environment.NewLine);
                }
            }
            catch
            {
                // Ignore (see class comment).
            }
        }
    }
}
