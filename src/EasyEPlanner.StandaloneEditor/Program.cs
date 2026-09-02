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

            LoadSystemResourceExtensions();

            Editor.StandaloneEditorCallbacks.SelectDevices =
                (owner, currentValue) =>
                {
                    using (var dialog = new StandaloneDevicesForm(currentValue))
                    {
                        if (dialog.ShowDialog(owner) == DialogResult.OK)
                        {
                            return dialog.SelectedDevices;
                        }

                        return null;
                    }
                };

            string projectPath = args.Length > 0 ? args[0] : null;
            Application.Run(new MainForm(projectPath));
        }

        /// <summary>
        /// Явная загрузка System.Resources.Extensions и его зависимостей.
        /// Требуется для работы ресурсов .NET Framework при использовании
        /// preserialized resources.
        /// </summary>
        private static void LoadSystemResourceExtensions()
        {
            string[] assemblies = new[]
            {
                "System.Buffers.dll",
                "System.Numerics.Vectors.dll",
                "System.Runtime.CompilerServices.Unsafe.dll",
                "System.Memory.dll",
                "System.Resources.Extensions.dll"
            };

            foreach (string assemblyFile in assemblies)
            {
                string path = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    assemblyFile);

                if (File.Exists(path))
                {
                    try
                    {
                        System.Reflection.Assembly.LoadFrom(path);
                    }
                    catch (Exception ex)
                    {
                        LogException(ex);
                    }
                }
            }
        }

        private static void LogException(Exception ex)
        {
            if (ex == null)
            {
                return;
            }

            try
            {
                Logs.AddMessage("Необработанное исключение standalone-" +
                    $"редактора:\n{ex}\n");
            }
            catch (Exception)
            {
                // Ошибка журналирования не должна скрывать исходную ошибку.
            }

            string path = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "standalone_error.log");
            try
            {
                File.AppendAllText(path,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n{ex}\n\n");
            }
            catch (Exception)
            {
                // В аварийном обработчике нельзя создавать новое исключение.
            }
        }
    }
}
