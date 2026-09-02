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
            var allDevices = DeviceManager.GetInstance()
                .Devices
                .Select(dev => dev.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name)
                .ToList();

            foreach (string device in allDevices)
            {
                bool isChecked = selected.Contains(device,
                    StringComparer.OrdinalIgnoreCase);
                checkedListBox.Items.Add(device, isChecked);
            }

            if (checkedListBox.Items.Count > 0)
            {
                labelInfo.Text = $"Выбрано: {checkedListBox.CheckedItems.Count} " +
                    $"из {checkedListBox.Items.Count}";
            }
            else
            {
                labelInfo.Text = "Устройства не загружены";
            }
        }

        /// <summary>
        /// Выбранные устройства в виде строки через пробел.
        /// </summary>
        public string SelectedDevices
        {
            get
            {
                var items = checkedListBox.CheckedItems
                    .Cast<string>()
                    .OrderBy(name => name);
                return string.Join(" ", items);
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

        private void OnCheckedChanged(object sender, EventArgs e)
        {
            labelInfo.Text = $"Выбрано: {checkedListBox.CheckedItems.Count} " +
                $"из {checkedListBox.Items.Count}";
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
    }
}
