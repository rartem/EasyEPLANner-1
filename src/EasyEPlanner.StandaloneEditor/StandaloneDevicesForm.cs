using EplanDevice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace EasyEPlanner.StandaloneEditor
{
    /// <summary>
    /// Окно выбора устройств в standalone-редакторе.
    /// </summary>
    public partial class StandaloneDevicesForm : Form
    {
        private readonly List<DeviceItem> allDevices = new List<DeviceItem>();
        private readonly HashSet<string> checkedDevices =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public StandaloneDevicesForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Создать окно выбора устройств.
        /// </summary>
        /// <param name="currentValue">Текущее значение — строка устройств
        /// через пробел.</param>
        public StandaloneDevicesForm(string currentValue)
            : this()
        {
            var selected = ParseDevices(currentValue);
            allDevices = DeviceManager.GetInstance()
                .Devices
                .Select(dev => new DeviceItem(dev.Name, dev.DeviceType))
                .Distinct(new DeviceItemComparer())
                .OrderBy(item => item.Name)
                .ToList();

            foreach (string device in selected)
            {
                checkedDevices.Add(device);
            }

            FillTypes();
            ApplyFilter();
        }

        /// <summary>
        /// Выбранные устройства в виде строки через пробел.
        /// </summary>
        public string SelectedDevices
        {
            get
            {
                var items = checkedDevices.OrderBy(name => name);
                return string.Join(" ", items);
            }
        }

        private void FillTypes()
        {
            var types = allDevices
                .Select(item => item.DeviceType)
                .Where(type => type != DeviceType.NONE)
                .Distinct()
                .OrderBy(type => type.ToString())
                .ToList();

            comboBoxTypes.Items.Add("(все)");
            foreach (DeviceType type in types)
            {
                comboBoxTypes.Items.Add(type.ToString());
            }

            comboBoxTypes.SelectedIndex = 0;
        }

        private void ApplyFilter()
        {
            string search = textBoxSearch.Text.Trim();
            string selectedType = comboBoxTypes.SelectedItem as string;

            var filtered = allDevices.AsEnumerable();

            if (!string.IsNullOrEmpty(search))
            {
                filtered = filtered.Where(item =>
                    item.Name.IndexOf(search,
                        StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (!string.IsNullOrEmpty(selectedType) &&
                selectedType != "(все)")
            {
                filtered = filtered.Where(item =>
                    item.DeviceType.ToString() == selectedType);
            }

            checkedListBox.BeginUpdate();
            try
            {
                checkedListBox.Items.Clear();
                foreach (DeviceItem item in filtered)
                {
                    bool isChecked = checkedDevices.Contains(item.Name);
                    checkedListBox.Items.Add(item.Name, isChecked);
                }
            }
            finally
            {
                checkedListBox.EndUpdate();
            }

            UpdateInfo();
        }

        private void UpdateInfo()
        {
            if (checkedListBox.Items.Count > 0)
            {
                labelInfo.Text = $"Выбрано: {checkedDevices.Count} " +
                    $"из {checkedListBox.Items.Count}";
            }
            else
            {
                labelInfo.Text = "Устройства не найдены";
            }
        }

        private HashSet<string> ParseDevices(string value)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(value))
                return result;

            foreach (string part in value.Split(new[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries))
            {
                result.Add(part.Trim());
            }

            return result;
        }

        private void OnCheckedChanged(object sender, ItemCheckEventArgs e)
        {
            string deviceName = checkedListBox.Items[e.Index].ToString();
            if (e.NewValue == CheckState.Checked)
            {
                checkedDevices.Add(deviceName);
            }
            else
            {
                checkedDevices.Remove(deviceName);
            }

            UpdateInfo();
        }

        private void OnFilterChanged(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        private void OnOkClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnCancelClick(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private sealed class DeviceItem
        {
            public DeviceItem(string name, DeviceType deviceType)
            {
                Name = name;
                DeviceType = deviceType;
            }

            public string Name { get; }
            public DeviceType DeviceType { get; }

            public override string ToString()
            {
                return Name;
            }
        }

        private sealed class DeviceItemComparer : IEqualityComparer<DeviceItem>
        {
            public bool Equals(DeviceItem x, DeviceItem y)
            {
                return string.Equals(x.Name, y.Name,
                    StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(DeviceItem obj)
            {
                return StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name);
            }
        }
    }
}
