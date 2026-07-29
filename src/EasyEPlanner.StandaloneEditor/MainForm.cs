using System;
using System.IO;
using System.Windows.Forms;

namespace EasyEPlanner.StandaloneEditor
{
    public class MainForm : Form
    {
        private readonly MenuStrip menuStrip;
        private readonly ToolStripMenuItem openProjectMenuItem;
        private readonly ToolStripMenuItem saveAllMenuItem;
        private readonly ToolStripMenuItem exitMenuItem;
        private readonly Panel editorPanel;
        private readonly StatusStrip statusStrip;
        private readonly ToolStripStatusLabel statusLabel;

        private StandaloneProjectLoader currentLoader;

        public MainForm(string projectPath = null)
        {
            Text = "EasyEPlanner — редактор технологических объектов";
            Width = 1200;
            Height = 800;

            if (!string.IsNullOrEmpty(projectPath))
            {
                Shown += (sender, e) => OpenProject(projectPath);
            }

            menuStrip = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("Файл");
            openProjectMenuItem = new ToolStripMenuItem("Открыть проект...",
                null, OnOpenProject);
            saveAllMenuItem = new ToolStripMenuItem("Сохранить всё",
                null, OnSaveAll);
            exitMenuItem = new ToolStripMenuItem("Выход", null,
                (s, e) => Close());

            fileMenu.DropDownItems.Add(openProjectMenuItem);
            fileMenu.DropDownItems.Add(saveAllMenuItem);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(exitMenuItem);
            menuStrip.Items.Add(fileMenu);

            editorPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
            };

            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel("Готово");
            statusStrip.Items.Add(statusLabel);

            Controls.Add(editorPanel);
            Controls.Add(statusStrip);
            Controls.Add(menuStrip);

            MainMenuStrip = menuStrip;
        }

        private void OnOpenProject(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog
            {
                Description = "Выберите каталог проекта с Lua-файлами",
                ShowNewFolderButton = false,
            })
            {
                if (dialog.ShowDialog() != DialogResult.OK)
                    return;

                OpenProject(dialog.SelectedPath);
            }
        }

        private void OpenProject(string projectDirectory)
        {
            editorPanel.Controls.Clear();
            currentLoader = new StandaloneProjectLoader();

            if (!currentLoader.Load(projectDirectory))
            {
                currentLoader = null;
                return;
            }

            var editorControl = Editor.Editor.GetInstance().EditorForm;
            editorControl.Dock = DockStyle.Fill;
            editorPanel.Controls.Add(editorControl);

            statusLabel.Text = $"Проект: {currentLoader.ProjectName}";
        }

        private void OnSaveAll(object sender, EventArgs e)
        {
            if (currentLoader == null)
            {
                MessageBox.Show("Проект не открыт.", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var param = new ProjectDescriptionSaver.ParametersForSave(
                    currentLoader.ProjectName,
                    currentLoader.ProjectDirectory,
                    silentMode: false)
                {
                    PreservedIOFileContent = currentLoader.OriginalMainIoLua,
                };

                ProjectDescriptionSaver.Save(param);

                statusLabel.Text = $"Проект сохранён: {currentLoader.ProjectName}";
                MessageBox.Show("Сохранение завершено.", "Готово",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
