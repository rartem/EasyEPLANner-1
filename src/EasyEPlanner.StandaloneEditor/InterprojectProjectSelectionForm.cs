using InterprojectExchange;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace EasyEPlanner.StandaloneEditor
{
    /// <summary>
    /// Выбор двух проектов для редактора межпроектного обмена.
    /// </summary>
    public sealed class InterprojectProjectSelectionForm : Form
    {
        private readonly ProjectRepositoryManager repositoryManager;
        private readonly string currentProjectPath;
        private readonly ComboBox mainProjectComboBox;
        private readonly ComboBox advancedProjectComboBox;
        private readonly Label statusLabel;
        private List<RepositoryProject> projects;

        public InterprojectProjectSelectionForm(
            ProjectRepositoryManager repositoryManager,
            string currentProjectPath)
        {
            this.repositoryManager = repositoryManager ??
                throw new ArgumentNullException(nameof(repositoryManager));
            this.currentProjectPath = currentProjectPath;

            Text = "Выбор проектов межпроектного обмена";
            Width = 760;
            Height = 250;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            mainProjectComboBox = CreateProjectsComboBox();
            advancedProjectComboBox = CreateProjectsComboBox();
            mainProjectComboBox.SelectedIndexChanged +=
                OnMainProjectChanged;

            var repositoriesButton = new Button
            {
                Text = "Репозитории...",
                AutoSize = true,
            };
            repositoriesButton.Click += OnManageRepositories;

            var openButton = new Button
            {
                Text = "Открыть редактор",
                AutoSize = true,
            };
            openButton.Click += OnOpenEditor;

            var cancelButton = new Button
            {
                Text = "Отмена",
                AutoSize = true,
                DialogResult = DialogResult.Cancel,
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                Padding = new Padding(12),
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

            layout.Controls.Add(new Label
            {
                Text = "Главный проект:",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
            }, 0, 0);
            layout.Controls.Add(mainProjectComboBox, 1, 0);
            layout.Controls.Add(new Label
            {
                Text = "Второй проект:",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
            }, 0, 1);
            layout.Controls.Add(advancedProjectComboBox, 1, 1);

            statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
            };
            layout.Controls.Add(statusLabel, 0, 2);
            layout.SetColumnSpan(statusLabel, 2);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
            };
            buttons.Controls.Add(cancelButton);
            buttons.Controls.Add(openButton);
            buttons.Controls.Add(repositoriesButton);
            layout.Controls.Add(buttons, 0, 3);
            layout.SetColumnSpan(buttons, 2);

            Controls.Add(layout);
            AcceptButton = openButton;
            CancelButton = cancelButton;
            Shown += (sender, e) => RefreshProjects();
        }

        public RepositoryProject SelectedMainProject =>
            mainProjectComboBox.SelectedItem as RepositoryProject;

        public RepositoryProject SelectedAdvancedProject =>
            advancedProjectComboBox.SelectedItem as RepositoryProject;

        private static ComboBox CreateProjectsComboBox()
        {
            return new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
            };
        }

        private void OnManageRepositories(object sender, EventArgs e)
        {
            using (var form = new ProjectRepositoriesForm(repositoryManager))
            {
                form.ShowDialog(this);
            }

            RefreshProjects();
        }

        private void RefreshProjects()
        {
            string previousMainPath = SelectedMainProject?.ProjectPath;
            string previousAdvancedPath = SelectedAdvancedProject?.ProjectPath;

            projects = repositoryManager.DiscoverProjects(out var errors)
                .ToList();
            AddCurrentProjectIfNeeded(projects);

            mainProjectComboBox.DataSource = null;
            advancedProjectComboBox.DataSource = null;
            mainProjectComboBox.DataSource = projects.ToList();
            advancedProjectComboBox.DataSource = projects.ToList();

            SelectProject(mainProjectComboBox,
                previousMainPath ?? currentProjectPath);
            SelectProject(advancedProjectComboBox, previousAdvancedPath);
            SelectAnotherAdvancedProject();

            statusLabel.Text = projects.Count == 0
                ? "Добавьте репозиторий с проектами."
                : $"Найдено проектов: {projects.Count}. " +
                    $"Ошибок структуры: {errors.Count}.";
        }

        private void AddCurrentProjectIfNeeded(
            ICollection<RepositoryProject> discoveredProjects)
        {
            if (string.IsNullOrEmpty(currentProjectPath) ||
                discoveredProjects.Any(project => PathsEqual(
                    project.ProjectPath, currentProjectPath)))
            {
                return;
            }

            if (MainIoProjectNameReader.TryReadFromFolder(currentProjectPath,
                out string projectName, out _))
            {
                discoveredProjects.Add(new RepositoryProject(projectName,
                    Path.GetFullPath(currentProjectPath), null));
            }
        }

        private void OnMainProjectChanged(object sender, EventArgs e)
        {
            SelectAnotherAdvancedProject();
        }

        private void SelectAnotherAdvancedProject()
        {
            var mainProject = SelectedMainProject;
            var advancedProject = SelectedAdvancedProject;
            if (mainProject == null || (advancedProject != null &&
                !string.Equals(mainProject.ProjectName,
                    advancedProject.ProjectName,
                    StringComparison.Ordinal)))
            {
                return;
            }

            RepositoryProject anotherProject = projects?.FirstOrDefault(
                project => !string.Equals(project.ProjectName,
                    mainProject.ProjectName, StringComparison.Ordinal));
            advancedProjectComboBox.SelectedItem = anotherProject;
        }

        private void OnOpenEditor(object sender, EventArgs e)
        {
            if (SelectedMainProject == null ||
                SelectedAdvancedProject == null)
            {
                ShowSelectionError("Необходимо выбрать два проекта.");
                return;
            }

            if (string.Equals(SelectedMainProject.ProjectName,
                SelectedAdvancedProject.ProjectName,
                StringComparison.Ordinal))
            {
                ShowSelectionError("У проектов должны отличаться PAC_name.");
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private static void SelectProject(ComboBox comboBox,
            string projectPath)
        {
            if (string.IsNullOrEmpty(projectPath))
            {
                return;
            }

            for (int index = 0; index < comboBox.Items.Count; index++)
            {
                var project = comboBox.Items[index] as RepositoryProject;
                if (project != null && PathsEqual(project.ProjectPath,
                    projectPath))
                {
                    comboBox.SelectedIndex = index;
                    return;
                }
            }
        }

        private static bool PathsEqual(string first, string second)
        {
            return string.Equals(Path.GetFullPath(first)
                    .TrimEnd('\\', '/'),
                Path.GetFullPath(second).TrimEnd('\\', '/'),
                StringComparison.OrdinalIgnoreCase);
        }

        private void ShowSelectionError(string message)
        {
            MessageBox.Show(this, message, "Выбор проектов",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
