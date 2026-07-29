using System;
using System.IO;
using System.Windows.Forms;

namespace EasyEPlanner.StandaloneEditor
{
    public class MainForm : Form
    {
        private readonly MenuStrip menuStrip;
        private readonly ToolStripMenuItem openProjectMenuItem;
        private readonly ToolStripMenuItem recentProjectsMenuItem;
        private readonly ToolStripMenuItem saveAllMenuItem;
        private readonly ToolStripMenuItem exitMenuItem;
        private readonly Panel editorPanel;
        private readonly StatusStrip statusStrip;
        private readonly ToolStripStatusLabel statusLabel;

        private readonly RecentProjectsManager recentProjectsManager;

        private StandaloneProjectLoader currentLoader;

        public MainForm(string projectPath = null)
        {
            Text = "EasyEPlanner — редактор технологических объектов";
            Width = 1200;
            Height = 800;

            recentProjectsManager = new RecentProjectsManager();

            if (!string.IsNullOrEmpty(projectPath))
            {
                Shown += (sender, e) => OpenProject(projectPath);
            }

            menuStrip = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("Файл");
            openProjectMenuItem = new ToolStripMenuItem("Открыть проект...",
                null, OnOpenProject);
            recentProjectsMenuItem = new ToolStripMenuItem("Недавние проекты");
            saveAllMenuItem = new ToolStripMenuItem("Сохранить всё",
                null, OnSaveAll);
            exitMenuItem = new ToolStripMenuItem("Выход", null,
                (s, e) => Close());

            fileMenu.DropDownItems.Add(openProjectMenuItem);
            fileMenu.DropDownItems.Add(recentProjectsMenuItem);
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

            Load += (sender, e) => UpdateRecentProjectsMenu();
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

            recentProjectsManager.Add(projectDirectory);
            UpdateRecentProjectsMenu();

            var editorControl = Editor.Editor.GetInstance().EditorForm;
            editorControl.Dock = DockStyle.Fill;
            editorPanel.Controls.Add(editorControl);

            statusLabel.Text = $"Проект: {currentLoader.ProjectName}";
        }

        private void UpdateRecentProjectsMenu()
        {
            recentProjectsMenuItem.DropDownItems.Clear();

            var projects = recentProjectsManager.Projects;
            if (projects.Count == 0)
            {
                var emptyItem = new ToolStripMenuItem("(пусто)")
                {
                    Enabled = false,
                };
                recentProjectsMenuItem.DropDownItems.Add(emptyItem);
                return;
            }

            foreach (string path in projects)
            {
                string displayPath = path;
                if (displayPath.Length > 80)
                {
                    displayPath = "..." + displayPath.Substring(displayPath.Length - 77);
                }

                var item = new ToolStripMenuItem(displayPath, null,
                    (sender, e) => OpenProject(path));
                item.ToolTipText = path;
                recentProjectsMenuItem.DropDownItems.Add(item);
            }

            recentProjectsMenuItem.DropDownItems.Add(new ToolStripSeparator());
            var clearItem = new ToolStripMenuItem("Очистить историю", null,
                (sender, e) =>
                {
                    recentProjectsManager.Clear();
                    UpdateRecentProjectsMenu();
                });
            recentProjectsMenuItem.DropDownItems.Add(clearItem);
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
