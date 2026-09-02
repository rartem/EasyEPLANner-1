using System;
using System.Drawing;
using System.Windows.Forms;

namespace EasyEPlanner.StandaloneEditor
{
    /// <summary>
    /// Настройка корневых папок репозиториев проектов.
    /// </summary>
    public sealed class ProjectRepositoriesForm : Form
    {
        private readonly ProjectRepositoryManager repositoryManager;
        private readonly ListBox repositoriesList;
        private readonly ListView projectsList;
        private readonly Label statusLabel;

        public ProjectRepositoriesForm(
            ProjectRepositoryManager repositoryManager)
        {
            this.repositoryManager = repositoryManager ??
                throw new ArgumentNullException(nameof(repositoryManager));

            Text = "Репозитории проектов";
            Width = 900;
            Height = 520;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;

            repositoriesList = new ListBox
            {
                Dock = DockStyle.Fill,
                HorizontalScrollbar = true,
            };

            projectsList = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
            };
            projectsList.Columns.Add("PAC_name", 160);
            projectsList.Columns.Add("Каталог проекта", 500);

            var split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 300,
            };
            split.Panel1.Controls.Add(repositoriesList);
            split.Panel2.Controls.Add(projectsList);

            var addButton = new Button
            {
                Text = "Добавить...",
                AutoSize = true,
            };
            addButton.Click += OnAddRepository;

            var removeButton = new Button
            {
                Text = "Удалить",
                AutoSize = true,
            };
            removeButton.Click += OnRemoveRepository;

            var refreshButton = new Button
            {
                Text = "Обновить",
                AutoSize = true,
            };
            refreshButton.Click += (sender, e) => RefreshData();

            var closeButton = new Button
            {
                Text = "Закрыть",
                AutoSize = true,
                DialogResult = DialogResult.OK,
            };

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 42,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(6),
            };
            buttons.Controls.Add(addButton);
            buttons.Controls.Add(removeButton);
            buttons.Controls.Add(refreshButton);
            buttons.Controls.Add(closeButton);

            statusLabel = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 32,
                Padding = new Padding(8, 6, 8, 4),
                AutoEllipsis = true,
            };
            statusLabel.Click += OnShowDiscoveryErrors;

            var repositoriesCaption = new Label
            {
                Text = "Корневые папки",
                Dock = DockStyle.Top,
                Height = 24,
                Padding = new Padding(3),
            };
            var projectsCaption = new Label
            {
                Text = "Обнаруженные проекты",
                Dock = DockStyle.Top,
                Height = 24,
                Padding = new Padding(3),
            };
            split.Panel1.Controls.Add(repositoriesCaption);
            split.Panel2.Controls.Add(projectsCaption);

            Controls.Add(split);
            Controls.Add(statusLabel);
            Controls.Add(buttons);
            AcceptButton = closeButton;
            CancelButton = closeButton;

            Shown += (sender, e) => RefreshData();
        }

        private void OnAddRepository(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog
            {
                Description = "Выберите папку, подпапки которой являются " +
                    "проектами",
                ShowNewFolderButton = true,
            })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                if (!repositoryManager.TryAdd(dialog.SelectedPath,
                    out string error))
                {
                    ShowError(error);
                    return;
                }
            }

            RefreshData();
        }

        private void OnRemoveRepository(object sender, EventArgs e)
        {
            if (!(repositoriesList.SelectedItem is string path))
            {
                return;
            }

            if (!repositoryManager.TryRemove(path, out string error))
            {
                ShowError(error);
                return;
            }

            RefreshData();
        }

        private void RefreshData()
        {
            repositoriesList.Items.Clear();
            foreach (string repository in repositoryManager.Repositories)
            {
                repositoriesList.Items.Add(repository);
            }

            projectsList.Items.Clear();
            var projects = repositoryManager.DiscoverProjects(
                out var errors);
            foreach (RepositoryProject project in projects)
            {
                var item = new ListViewItem(project.ProjectName);
                item.SubItems.Add(project.ProjectPath);
                projectsList.Items.Add(item);
            }

            statusLabel.Text = $"Проектов: {projects.Count}. " +
                $"Ошибок структуры: {errors.Count}.";
            statusLabel.ForeColor = errors.Count == 0
                ? SystemColors.ControlText
                : Color.DarkOrange;
            statusLabel.Tag = string.Join(Environment.NewLine, errors);
            statusLabel.Cursor = errors.Count == 0
                ? Cursors.Default
                : Cursors.Hand;
        }

        private void OnShowDiscoveryErrors(object sender, EventArgs e)
        {
            string errors = statusLabel.Tag as string;
            if (string.IsNullOrEmpty(errors))
            {
                return;
            }

            MessageBox.Show(this, errors, "Ошибки структуры репозиториев",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void ShowError(string error)
        {
            MessageBox.Show(this, error, "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
