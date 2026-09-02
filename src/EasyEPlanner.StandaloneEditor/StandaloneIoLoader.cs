using EplanDevice;
using LuaInterface;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace EasyEPlanner.StandaloneEditor
{
    /// <summary>
    /// Загрузка устройств из main.io.lua.
    /// Узлы и модули не восстанавливаются — для этого требуются сборки EPLAN.
    /// Оригинальный main.io.lua сохраняется в неизменном виде.
    /// </summary>
    public class StandaloneIoLoader
    {
        public void Load(string ioLuaText)
        {
            if (string.IsNullOrWhiteSpace(ioLuaText))
                return;

            var lua = new Lua();
            try
            {
                lua.DoString(ioLuaText);

                var devicesTable = lua["devices"] as LuaTable;
                if (devicesTable != null)
                {
                    LoadDevices(devicesTable);
                }
            }
            catch (Exception ex)
            {
                Logs.AddMessage($"Ошибка загрузки main.io.lua: {ex}\n");
                MessageBox.Show($"Ошибка загрузки main.io.lua: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                lua.Dispose();
            }
        }

        private void LoadDevices(LuaTable devicesTable)
        {
            var deviceManager = DeviceManager.GetInstance();
            deviceManager.Clear();

            foreach (LuaTable deviceData in devicesTable.Values
                .OfType<LuaTable>())
            {
                string name = GetString(deviceData["name"]) ?? "";
                string description = GetString(deviceData["descr"]) ?? "";
                string article = GetString(deviceData["article"]) ?? "";
                int deviceTypeIndex = GetInt(deviceData["dtype"], -1);
                int subTypeIndex = GetInt(deviceData["subtype"]);

                string subType = GetSubTypeName(deviceTypeIndex,
                    subTypeIndex);
                string normalizedName = NormalizeDeviceName(name,
                    deviceTypeIndex);

                string errStr;
                deviceManager.AddDevice(normalizedName, description, subType,
                    string.Empty, string.Empty, string.Empty, 0,
                    out errStr, article, string.Empty);

                if (!string.IsNullOrEmpty(errStr))
                {
                    Logs.AddMessage(errStr);
                }
            }

            deviceManager.Sort();
        }

        /// <summary>
        /// Получить имя подтипа по индексам из main.io.lua.
        /// </summary>
        private string GetSubTypeName(int deviceTypeIndex, int subTypeIndex)
        {
            int value = deviceTypeIndex * DSTExt.TypeMultiplier + subTypeIndex;
            if (Enum.IsDefined(typeof(DeviceSubType), value))
            {
                return ((DeviceSubType)value).ToString();
            }

            return string.Empty;
        }

        /// <summary>
        /// Добавить разделитель перед типом устройства. Это позволяет
        /// корректно разобрать нестандартные имена, например SAFETYDI1.
        /// </summary>
        private string NormalizeDeviceName(string name, int deviceTypeIndex)
        {
            if (!Enum.IsDefined(typeof(DeviceType), deviceTypeIndex))
            {
                return name;
            }

            var deviceType = (DeviceType)deviceTypeIndex;
            if (deviceType == DeviceType.NONE)
            {
                return name;
            }

            string cleanName = name.Trim().TrimStart('+').Replace("-", "");
            string type = deviceType.ToString();
            string pattern = $@"^(?<object>.+){Regex.Escape(type)}" +
                @"(?<number>\d+)$";
            Match match = Regex.Match(cleanName, pattern,
                RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return name;
            }

            return $"+{match.Groups["object"].Value}-{type}" +
                match.Groups["number"].Value;
        }

        private string GetString(object value)
        {
            if (value == null)
            {
                return null;
            }

            return value.ToString();
        }

        private int GetInt(object value, int defaultValue = 0)
        {
            if (value == null)
            {
                return defaultValue;
            }

            if (value is double d)
            {
                return (int)d;
            }

            if (int.TryParse(value.ToString(), out int result))
            {
                return result;
            }

            return defaultValue;
        }

    }
}
