using System;
using System.IO;
using System.Windows.Forms;

namespace EasyEPlanner.StandaloneEditor
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.ThreadException += (s, e) => LogException(e.Exception);
            AppDomain.CurrentDomain.UnhandledException +=
                (s, e) => LogException(e.ExceptionObject as Exception);

            string projectPath = args.Length > 0 ? args[0] : null;
            Application.Run(new MainForm(projectPath));
        }

        private static void LogException(Exception ex)
        {
            if (ex == null)
                return;

            string path = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "standalone_error.log");
            File.AppendAllText(path,
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n{ex}\n\n");
        }
    }
}
