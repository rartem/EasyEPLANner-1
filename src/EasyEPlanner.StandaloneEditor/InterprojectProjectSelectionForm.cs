using InterprojectExchange;
using System;
using System.Collections.Generic;
using System.Drawing;
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
        private readonly ProjectSelector mainProjectSelector;
        private readonly ProjectSelector advancedProjectSelector;
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
            Height = 500;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            mainProjectSelector = new ProjectSelector
            {
                Dock = DockStyle.Fill,
            };
            advancedProjectSelector = new ProjectSelector
            {
                Dock = DockStyle.Fill,
            };
            mainProjectSelector.SelectedProjectChanged +=
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
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

            layout.Controls.Add(new Label
            {
                Text = "Главный проект:",
                AutoSize = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
            }, 0, 0);
            layout.Controls.Add(mainProjectSelector, 1, 0);
            layout.Controls.Add(new Label
            {
                Text = "Второй проект:",
                AutoSize = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
            }, 0, 1);
            layout.Controls.Add(advancedProjectSelector, 1, 1);

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
            mainProjectSelector.SelectedProject;

        public RepositoryProject SelectedAdvancedProject =>
            advancedProjectSelector.SelectedProject;

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

            mainProjectSelector.SetProjects(projects);
            advancedProjectSelector.SetProjects(projects);

            mainProjectSelector.SelectProject(
                previousMainPath ?? currentProjectPath);
            advancedProjectSelector.SelectProject(previousAdvancedPath);
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
            advancedProjectSelector.SelectProject(anotherProject);
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

        /// <summary>
        /// Поле ввода со списком проектов и фильтрацией по мере ввода.
        /// </summary>
        private sealed class ProjectSelector : Panel
        {
            private readonly TextBox textBox;
            private readonly ListBox listBox;
            private List<RepositoryProject> allProjects =
                new List<RepositoryProject>();
            private bool updatingText;

            public ProjectSelector()
            {
                textBox = new TextBox
                {
                    Dock = DockStyle.Top,
                    Margin = new Padding(0),
                };
                textBox.TextChanged += OnTextChanged;
                textBox.KeyDown += OnTextBoxKeyDown;

                listBox = new ListBox
                {
                    Dock = DockStyle.Fill,
                    Margin = new Padding(0),
                };
                listBox.SelectedIndexChanged += OnListBoxSelectedIndexChanged;

                Controls.Add(listBox);
                Controls.Add(textBox);
            }

            public RepositoryProject SelectedProject { get; private set; }

            public event EventHandler SelectedProjectChanged;

            public void SetProjects(IEnumerable<RepositoryProject> projects)
            {
                allProjects = projects.ToList();
                ApplyFilter();
            }

            public void SelectProject(string projectPath)
            {
                if (string.IsNullOrEmpty(projectPath))
                {
                    return;
                }

                var project = allProjects.FirstOrDefault(item =>
                    PathsEqual(item.ProjectPath, projectPath));
                SelectProject(project);
            }

            public void SelectProject(RepositoryProject project)
            {
                SelectedProject = project;
                SetText(project?.ToString() ?? string.Empty);
                SelectListItem(project);
            }

            private void SelectListItem(RepositoryProject project)
            {
                if (project == null)
                {
                    listBox.SelectedItem = null;
                    return;
                }

                for (int index = 0; index < listBox.Items.Count; index++)
                {
                    if (ReferenceEquals(listBox.Items[index], project))
                    {
                        listBox.SelectedIndex = index;
                        return;
                    }
                }
            }

            private void SetText(string text)
            {
                updatingText = true;
                textBox.Text = text;
                updatingText = false;
            }

            private void OnTextChanged(object sender, EventArgs e)
            {
                if (updatingText)
                {
                    return;
                }

                SelectedProject = null;
                ApplyFilter();
            }

            private void ApplyFilter()
            {
                string filter = textBox.Text;
                var filtered = string.IsNullOrEmpty(filter)
                    ? allProjects
                    : allProjects.Where(project =>
                        project.ToString().IndexOf(filter,
                            StringComparison.OrdinalIgnoreCase) >= 0).ToList();

                listBox.Items.Clear();
                foreach (var project in filtered)
                {
                    listBox.Items.Add(project);
                }
            }

            private void OnListBoxSelectedIndexChanged(object sender,
                EventArgs e)
            {
                if (listBox.SelectedItem is RepositoryProject project)
                {
                    SelectedProject = project;
                    SetText(project.ToString());
                    SelectedProjectChanged?.Invoke(this, EventArgs.Empty);
                }
            }

            private void OnTextBoxKeyDown(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Down && listBox.Items.Count > 0)
                {
                    listBox.SelectedIndex = 0;
                    listBox.Focus();
                }
                else if (e.KeyCode == Keys.Enter &&
                    listBox.SelectedItem is RepositoryProject project)
                {
                    SelectedProject = project;
                    SetText(project.ToString());
                    SelectedProjectChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }
}
