using System;
using System.IO;
using System.Windows.Forms;
using InterprojectExchange;

namespace EasyEPlanner.StandaloneEditor
{
    public class MainForm : Form
    {
        private readonly MenuStrip menuStrip;
        private readonly ToolStripMenuItem openProjectMenuItem;
        private readonly ToolStripMenuItem recentProjectsMenuItem;
        private readonly ToolStripMenuItem saveAllMenuItem;
        private readonly ToolStripMenuItem projectLogMenuItem;
        private readonly ToolStripMenuItem exitMenuItem;
        private readonly Panel editorPanel;
        private readonly StatusStrip statusStrip;
        private readonly ToolStripStatusLabel statusLabel;

        private readonly RecentProjectsManager recentProjectsManager;
        private readonly ProjectRepositoryManager repositoryManager;

        private StandaloneProjectLoader currentLoader;
        private StandaloneLog currentProjectLog;

        public MainForm(string projectPath = null)
        {
            Text = "EasyEPlanner — редактор технологических объектов";
            Width = 1200;
            Height = 800;

            recentProjectsManager = new RecentProjectsManager();
            repositoryManager = new ProjectRepositoryManager();

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
            projectLogMenuItem = new ToolStripMenuItem("Журнал проекта",
                null, OnShowProjectLog)
            {
                Enabled = false,
            };
            exitMenuItem = new ToolStripMenuItem("Выход", null,
                (s, e) => Close());

            fileMenu.DropDownItems.Add(openProjectMenuItem);
            fileMenu.DropDownItems.Add(recentProjectsMenuItem);
            fileMenu.DropDownItems.Add(saveAllMenuItem);
            fileMenu.DropDownItems.Add(projectLogMenuItem);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(exitMenuItem);
            menuStrip.Items.Add(fileMenu);

            var projectsMenu = new ToolStripMenuItem("Проекты");
            var repositoriesMenuItem = new ToolStripMenuItem(
                "Репозитории проектов...", null,
                OnShowProjectRepositories);
            var interprojectExchangeMenuItem = new ToolStripMenuItem(
                "Межпроектный обмен...", null,
                OnOpenInterprojectExchange);
            projectsMenu.DropDownItems.Add(repositoriesMenuItem);
            projectsMenu.DropDownItems.Add(interprojectExchangeMenuItem);
            menuStrip.Items.Add(projectsMenu);

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
            FormClosed += (sender, e) => currentProjectLog?.Dispose();
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
            if (currentProjectLog != null)
            {
                currentProjectLog.Changed -= OnProjectLogChanged;
                currentProjectLog.Dispose();
            }

            currentLoader = new StandaloneProjectLoader();

            if (!currentLoader.Load(projectDirectory))
            {
                SetCurrentProjectLog(currentLoader.ProjectLog);
                currentLoader = null;
                return;
            }

            SetCurrentProjectLog(currentLoader.ProjectLog);

            recentProjectsManager.Add(projectDirectory);
            UpdateRecentProjectsMenu();

            var editorControl = Editor.Editor.GetInstance().EditorForm;
            editorControl.Dock = DockStyle.Fill;
            editorPanel.Controls.Add(editorControl);

            statusLabel.Text = $"Проект: {currentLoader.ProjectName}";
        }

        private void SetCurrentProjectLog(StandaloneLog projectLog)
        {
            currentProjectLog = projectLog;
            if (currentProjectLog != null)
            {
                currentProjectLog.Changed += OnProjectLogChanged;
            }

            UpdateProjectLogMenu();
        }

        private void OnProjectLogChanged(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateProjectLogMenu));
                return;
            }

            UpdateProjectLogMenu();
        }

        private void UpdateProjectLogMenu()
        {
            projectLogMenuItem.Enabled = currentProjectLog != null;
            int count = currentProjectLog?.MessageCount ?? 0;
            projectLogMenuItem.Text = count == 0
                ? "Журнал проекта"
                : $"Журнал проекта ({count})";
        }

        private void OnShowProjectLog(object sender, EventArgs e)
        {
            currentProjectLog?.ShowLog();
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
                currentProjectLog?.AddException("Сохранение проекта", ex);
                MessageBox.Show($"Ошибка сохранения: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnShowProjectRepositories(object sender, EventArgs e)
        {
            using (var form = new ProjectRepositoriesForm(repositoryManager))
            {
                form.ShowDialog(this);
            }
        }

        private void OnOpenInterprojectExchange(object sender, EventArgs e)
        {
            RepositoryProject mainProject;
            RepositoryProject advancedProject;
            using (var form = new InterprojectProjectSelectionForm(
                repositoryManager, currentLoader?.ProjectDirectory))
            {
                if (form.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                mainProject = form.SelectedMainProject;
                advancedProject = form.SelectedAdvancedProject;
            }

            try
            {
                var starter = new InterprojectExchangeStarter(
                    standaloneMode: true);
                bool started = starter.StartStandalone(
                    mainProject.ProjectPath,
                    advancedProject.ProjectPath,
                    repositoryManager.Repositories,
                    out string error);
                if (!started)
                {
                    string message = string.IsNullOrEmpty(error)
                        ? "Не удалось загрузить выбранные проекты."
                        : error;
                    currentProjectLog?.AddMessage(
                        "Межпроектный обмен: " + message);
                    MessageBox.Show(this, message, "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                statusLabel.Text = "Межпроектный обмен: " +
                    $"{mainProject.ProjectName} ↔ " +
                    advancedProject.ProjectName;
            }
            catch (Exception ex)
            {
                currentProjectLog?.AddException(
                    "Открытие межпроектного обмена", ex);
                MessageBox.Show(this,
                    $"Ошибка открытия межпроектного обмена: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
