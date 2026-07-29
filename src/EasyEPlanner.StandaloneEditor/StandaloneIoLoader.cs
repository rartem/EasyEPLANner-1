using EplanDevice;
using LuaInterface;
using System;
using System.Collections.Generic;
using System.Linq;
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

            foreach (LuaTable deviceData in devicesTable.Values.OfType<LuaTable>())
            {
                string name = GetString(deviceData["name"]) ?? "";
                string description = GetString(deviceData["descr"]) ?? "";
                string article = GetString(deviceData["article"]) ?? "";

                string errStr;
                deviceManager.AddDevice(name, description, string.Empty,
                    string.Empty, string.Empty, string.Empty, 0,
                    out errStr, article, string.Empty);

                if (!string.IsNullOrEmpty(errStr))
                {
                    Logs.AddMessage(errStr);
                }
            }

            deviceManager.Sort();
        }

        private string GetString(object value)
        {
            if (value == null)
                return null;

            return value.ToString();
        }

        private int GetInt(object value)
        {
            if (value == null)
                return 0;

            if (value is double d)
                return (int)d;

            if (int.TryParse(value.ToString(), out int result))
                return result;

            return 0;
        }

        private int GetFirstInt(LuaTable table)
        {
            foreach (var value in table.Values)
            {
                if (value is double d)
                    return (int)d;

                if (int.TryParse(value?.ToString() ?? string.Empty, out int result))
                    return result;
            }

            return 0;
        }
    }
}
