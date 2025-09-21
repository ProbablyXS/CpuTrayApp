using System;
using System.Windows.Forms;

namespace CpuTrayApp
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Create and initialize the tray
            Tray tray = new Tray();
            tray.InitializeTray();

            // Run the app with the tray (no form is shown)
            Application.Run();
        }
    }
}
