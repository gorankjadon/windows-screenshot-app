using System;
using System.Threading;
using System.Windows.Forms;

namespace GstackScreenshot
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            AppLogger.Info("Application startup initiated.");
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += OnThreadException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                AppLogger.Info("Entering application message loop.");
                Application.Run(new ScreenshotApplicationContext());
                AppLogger.Info("Application exited normally.");
            }
            catch (Exception ex)
            {
                AppLogger.Error("Fatal exception escaped Main.", ex);
                MessageBox.Show(
                    "The app hit a fatal error. Details were written to:\n" + AppLogger.CurrentLogPath,
                    "Gstack Screenshot",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            AppLogger.Error("Unhandled WinForms thread exception.", e.Exception);
            MessageBox.Show(
                "The app hit an unexpected error. Details were written to:\n" + AppLogger.CurrentLogPath,
                "Gstack Screenshot",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            AppLogger.Error("Unhandled AppDomain exception. IsTerminating=" + e.IsTerminating, exception);
        }
    }
}
