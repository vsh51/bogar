using System;
using System.Windows;

namespace Bogar.UI
{
    internal static class WindowNavigationHelper
    {
        public static void Replace(Window currentWindow, Window nextWindow)
        {
            if (currentWindow == null)
                throw new ArgumentNullException(nameof(currentWindow));
            if (nextWindow == null)
                throw new ArgumentNullException(nameof(nextWindow));

            ApplyPlacement(currentWindow, nextWindow, matchWindowState: true);
            nextWindow.Show();
            currentWindow.Close();
        }

        public static void AlignTo(Window referenceWindow, Window targetWindow, double offsetX = 0, double offsetY = 0)
        {
            if (referenceWindow == null)
                throw new ArgumentNullException(nameof(referenceWindow));
            if (targetWindow == null)
                throw new ArgumentNullException(nameof(targetWindow));

            ApplyPlacement(referenceWindow, targetWindow, matchWindowState: false, offsetX: offsetX, offsetY: offsetY);
        }

        private static void ApplyPlacement(Window referenceWindow, Window targetWindow, bool matchWindowState, double offsetX = 0, double offsetY = 0)
        {
            var referenceBounds = referenceWindow.WindowState == WindowState.Normal
                ? new Rect(referenceWindow.Left, referenceWindow.Top, referenceWindow.Width, referenceWindow.Height)
                : referenceWindow.RestoreBounds;

            if (double.IsNaN(referenceBounds.Left) || double.IsNaN(referenceBounds.Top))
            {
                targetWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                if (matchWindowState)
                {
                    targetWindow.WindowState = WindowState.Normal;
                }
                return;
            }

            targetWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            targetWindow.Left = referenceBounds.Left + offsetX;
            targetWindow.Top = referenceBounds.Top + offsetY;

            if (matchWindowState)
            {
                var targetState = referenceWindow.WindowState == WindowState.Minimized
                    ? WindowState.Normal
                    : referenceWindow.WindowState;
                targetWindow.WindowState = targetState;
            }
        }
    }
}
