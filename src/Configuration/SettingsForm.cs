using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace EasyEPlanner
{
    /// <summary>
    /// Редактор параметров configuration.ini с контекстной справкой.
    /// </summary>
    public sealed class SettingsForm : Form
    {
        private readonly string configurationPath;
        private readonly AddInSettings settings;
        private readonly Dictionary<AddInSetting, Control> editors =
            new Dictionary<AddInSetting, Control>();
        private readonly Dictionary<CheckedListBox,
            List<AddInSetting>> deviceLists =
            new Dictionary<CheckedListBox, List<AddInSetting>>();
        private readonly ToolTip toolTip = new ToolTip
        {
            AutoPopDelay = 20000,
            InitialDelay = 350,
            ReshowDelay = 100,
            ShowAlways = true,
        };
        private readonly ErrorProvider errors = new ErrorProvider();
        private readonly TextBox helpText = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            BackColor = SystemColors.Info,
            BorderStyle = BorderStyle.None,
            TabStop = false,
        };
        private readonly TabControl tabs = new TabControl
        {
            Dock = DockStyle.Fill,
        };

        public SettingsForm(string configurationPath)
        {
            this.configurationPath = configurationPath;
            settings = AddInSettings.Load(configurationPath);
            Text = "Настройки EasyEPLANner";
            Font = SystemFonts.MessageBoxFont;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(820, 650);
            MinimumSize = new Size(720, 580);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MinimizeBox = false;
            errors.ContainerControl = this;
            errors.BlinkStyle = ErrorBlinkStyle.NeverBlink;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 1,
                RowCount = 5,
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(new Label
            {
                Text = "Наведите указатель или выберите параметр, " +
                    "чтобы увидеть подсказку.",
                AutoSize = true,
                Margin = new Padding(3, 0, 3, 8),
            }, 0, 0);
            var pathText = new TextBox
            {
                Text = configurationPath,
                ReadOnly = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(3, 0, 3, 10),
                AccessibleName = "Файл конфигурации",
                TabStop = false,
            };
            toolTip.SetToolTip(pathText, "Файл конфигурации надстройки");
            layout.Controls.Add(pathText, 0, 1);
            layout.Controls.Add(tabs, 0, 2);

            AddSettingsTab("Каталоги", "path");
            AddSettingsTab("Проверка проектов", "TestSettings");
            AddSettingsTab("Справка", "helpSystem");
            AddFilterTab();
            tabs.Selected += (sender, e) =>
            {
                if (e.TabPage?.Tag is AddInSetting setting)
                {
                    ShowHelp(setting);
                }
            };

            var help = new GroupBox
            {
                Text = "Подсказка",
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 20, 10, 10),
            };
            help.Controls.Add(helpText);
            layout.Controls.Add(help, 0, 3);

            var buttons = new FlowLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 8, 0, 0),
                WrapContents = false,
            };
            var cancel = new Button
            {
                Text = "Отмена",
                AutoSize = true,
                DialogResult = DialogResult.Cancel,
            };
            var save = new Button { Text = "Сохранить", AutoSize = true };
            save.Click += SaveSettings;
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(save);
            layout.Controls.Add(buttons, 0, 4);
            Controls.Add(layout);
            AcceptButton = save;
            CancelButton = cancel;
            ActiveControl = editors.Values.First();
            ShowHelp(AddInSettings.Definitions[0]);
        }

        private TableLayoutPanel AddTab(string title)
        {
            var page = new TabPage(title) { AutoScroll = true };
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 3,
                Padding = new Padding(10),
            };
            table.ColumnStyles.Add(new ColumnStyle(
                SizeType.Percent, 100));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 18));
            page.Controls.Add(table);
            tabs.TabPages.Add(page);
            return table;
        }

        private void AddSettingsTab(string title, string section)
        {
            var table = AddTab(title);
            table.Parent.Tag = AddInSettings.Definitions.First(item =>
                item.Section == section);
            foreach (var setting in AddInSettings.Definitions
                .Where(item => item.Section == section))
            {
                AddEditor(table, setting);
            }
        }

        private void AddEditor(TableLayoutPanel table, AddInSetting setting)
        {
            int row = table.RowCount++;
            if (setting.IsBoolean)
            {
                var checkBox = new CheckBox
                {
                    Text = setting.Title,
                    Name = setting.Key,
                    AutoSize = true,
                    Checked = bool.Parse(settings[setting]),
                    Margin = new Padding(3, 8, 3, 8),
                };
                table.Controls.Add(checkBox, 0, row);
                table.SetColumnSpan(checkBox, 2);
                editors.Add(setting, checkBox);
                AttachHelp(checkBox, setting);
                return;
            }

            var label = new Label
            {
                Text = setting.Title,
                AutoSize = true,
                Margin = new Padding(3, 10, 3, 3),
            };
            table.Controls.Add(label, 0, row);
            table.SetColumnSpan(label, 2);
            AttachHelp(label, setting);
            row = table.RowCount++;
            var textBox = new TextBox
            {
                Text = settings[setting],
                Dock = DockStyle.Fill,
                Name = setting.Key,
                AccessibleName = setting.Title,
                MaxLength = 32000,
                Margin = new Padding(3, 3, 3, 10),
            };
            textBox.TextChanged += (sender, e) =>
                errors.SetError(textBox, string.Empty);
            table.Controls.Add(textBox, 0, row);
            editors.Add(setting, textBox);
            AttachHelp(textBox, setting);
            if (setting.IsFolder)
            {
                var browse = new Button
                {
                    Text = "Обзор…",
                    AutoSize = true,
                    AccessibleName = "Выбрать: " + setting.Title,
                };
                browse.Click += (sender, e) => BrowseFolder(setting, textBox);
                table.Controls.Add(browse, 1, row);
                AttachHelp(browse, setting);
            }
            else
            {
                table.SetColumnSpan(textBox, 2);
            }
        }

        private void AddFilterTab()
        {
            var table = AddTab("Межпроектный обмен");
            table.Parent.Tag = AddInSettings.Definitions.First(item =>
                item.Section == "bindedSignalsList");
            table.Dock = DockStyle.Fill;
            table.AutoSize = false;
            for (int row = 0; row < 3; row++)
            {
                table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            foreach (var setting in AddInSettings.Definitions
                .Where(item => item.Section == "bindedSignalsList"))
            {
                AddEditor(table, setting);
            }

            var lists = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Height = 240,
                ColumnCount = 2,
                RowCount = 1,
            };
            lists.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            lists.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            AddDeviceList(lists, "Открытый проект", "currProjDevList", 0);
            AddDeviceList(lists, "Связуемый проект", "advProjDevList", 1);
            table.Controls.Add(lists, 0, table.RowCount++);
            table.SetColumnSpan(lists, 2);
        }

        private void AddDeviceList(TableLayoutPanel table, string title,
            string section, int column)
        {
            var group = new GroupBox
            {
                Text = title + ": типы устройств",
                Dock = DockStyle.Fill,
                Padding = new Padding(8),
            };
            var list = new CheckedListBox
            {
                Dock = DockStyle.Fill,
                CheckOnClick = true,
                Name = section,
                IntegralHeight = false,
                AccessibleName = title + ": типы устройств",
            };
            var definitions = AddInSettings.Definitions
                .Where(item => item.Section == section).ToList();
            foreach (var setting in definitions)
            {
                list.Items.Add(setting.Title, bool.Parse(settings[setting]));
            }
            deviceLists.Add(list, definitions);
            list.SelectedIndexChanged += (sender, e) =>
            {
                if (list.SelectedIndex >= 0)
                {
                    ShowHelp(definitions[list.SelectedIndex]);
                }
            };
            list.MouseMove += (sender, e) =>
            {
                int index = list.IndexFromPoint(e.Location);
                if (index >= 0)
                {
                    ShowHelp(definitions[index]);
                }
            };
            AttachHelp(list, definitions[0]);
            group.Controls.Add(list);
            table.Controls.Add(group, column, 0);
        }

        private void BrowseFolder(AddInSetting setting, TextBox editor)
        {
            using (var dialog = new FolderBrowserDialog
            {
                Description = setting.Title,
                SelectedPath = editor.Text.Split(';').Last().Trim(),
            })
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    editor.Text = setting.Key == "folder_path" &&
                        !string.IsNullOrWhiteSpace(editor.Text)
                        ? editor.Text.TrimEnd(' ', ';') + ";" +
                            dialog.SelectedPath
                        : dialog.SelectedPath;
                }
            }
        }

        private void AttachHelp(Control control, AddInSetting setting)
        {
            toolTip.SetToolTip(control, setting.Description);
            control.Enter += (sender, e) => ShowHelp(setting);
            control.MouseEnter += (sender, e) => ShowHelp(setting);
        }

        private void ShowHelp(AddInSetting setting)
        {
            helpText.Text = setting.Title + Environment.NewLine +
                setting.Description + Environment.NewLine +
                "INI: [" + setting.Section + "] " + setting.Key;
        }

        private void SaveSettings(object sender, EventArgs e)
        {
            errors.Clear();
            Control firstInvalid = null;
            foreach (var pair in editors)
            {
                string value = pair.Value is CheckBox checkBox
                    ? checkBox.Checked.ToString() : pair.Value.Text;
                try
                {
                    AddInSettings.Validate(pair.Key, value);
                    settings[pair.Key] = value;
                }
                catch (ArgumentException ex)
                {
                    errors.SetError(pair.Value, ex.Message);
                    firstInvalid = firstInvalid ?? pair.Value;
                }
            }
            if (firstInvalid != null)
            {
                Control parent = firstInvalid.Parent;
                while (parent != null && !(parent is TabPage))
                {
                    parent = parent.Parent;
                }
                tabs.SelectedTab = parent as TabPage;
                firstInvalid.Focus();
                return;
            }
            foreach (var pair in deviceLists)
            {
                for (int index = 0; index < pair.Value.Count; index++)
                {
                    settings[pair.Value[index]] =
                        pair.Key.GetItemChecked(index).ToString();
                }
            }

            try
            {
                settings.Save(configurationPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Не удалось сохранить настройки.\n" +
                    ex.Message, Text, MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                toolTip.Dispose();
                errors.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
