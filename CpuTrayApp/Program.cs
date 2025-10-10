using System;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace CpuTrayApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                string resourceName = "CpuTrayApp.Resources." + new AssemblyName(args.Name).Name + ".dll";
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    if (stream == null) return null;
                    byte[] assemblyData = new byte[stream.Length];
                    stream.Read(assemblyData, 0, assemblyData.Length);
                    return Assembly.Load(assemblyData);
                }
            };


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //------- Force LANG
            //Thread.CurrentThread.CurrentUICulture = new CultureInfo("fr-FR"); // Français
            //Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US"); // Anglais
            //MessageBox.Show(Thread.CurrentThread.CurrentUICulture.Name);


            // Create and initialize the tray
            Tray tray = new Tray();
            tray.InitializeTray();

            // Run the app with the tray (no form is shown)
            Application.Run();
        }
    }
}
