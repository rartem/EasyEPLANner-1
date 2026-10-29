using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows.Forms;
using Eplan.EplApi.ApplicationFramework;

namespace EasyEPlanner
{
    /// <summary>
    /// Открыть настройки надстройки без загрузки проекта EPLAN.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class ShowSettingsAction : IEplAction
    {
        public bool Execute(ActionCallingContext context)
        {
            try
            {
                string path = Path.Combine(ProjectManager.GetInstance()
                    .OriginalAssemblyPath,
                    StaticHelper.CommonConst.ConfigFileName);
                var owner = new WindowWrapper(Process.GetCurrentProcess()
                    .MainWindowHandle);
                using (var form = new SettingsForm(path))
                {
                    if (form.ShowDialog(owner) == DialogResult.OK)
                    {
                        InterprojectExchange.FilterConfiguration
                            .ReloadIfLoaded(path);
                        InterprojectExchange.InterprojectProjectCatalog
                            .Invalidate();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось открыть настройки.\n" +
                    ex.Message, "Настройки EasyEPLANner",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public bool OnRegister(ref string name, ref int ordinal)
        {
            name = nameof(ShowSettingsAction);
            ordinal = 30;
            return true;
        }

        public void GetActionProperties(ref ActionProperties properties)
        {
        }
    }
}
