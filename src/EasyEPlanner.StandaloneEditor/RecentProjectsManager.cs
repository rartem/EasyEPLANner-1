using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace EasyEPlanner.StandaloneEditor
{
    /// <summary>
    /// Хранение и управление списком недавно открытых проектов.
    /// </summary>
    public class RecentProjectsManager
    {
        private const int MaxCount = 10;
        private readonly string storagePath;
        private readonly List<string> projects;

        public RecentProjectsManager()
        {
            string appDataPath = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);
            string folder = Path.Combine(appDataPath, "EasyEPlanner.StandaloneEditor");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            storagePath = Path.Combine(folder, "recent_projects.txt");
            projects = new List<string>();
            Load();
        }

        /// <summary>
        /// Список путей к недавно открытым проектам.
        /// </summary>
        public IReadOnlyList<string> Projects => projects.AsReadOnly();

        /// <summary>
        /// Добавить проект в историю.
        /// </summary>
        public void Add(string projectPath)
        {
            if (string.IsNullOrWhiteSpace(projectPath))
                return;

            string normalized = projectPath.Trim();
            if (!Directory.Exists(normalized))
                return;

            projects.RemoveAll(p =>
                string.Equals(p, normalized, StringComparison.OrdinalIgnoreCase));
            projects.Insert(0, normalized);

            while (projects.Count > MaxCount)
            {
                projects.RemoveAt(projects.Count - 1);
            }

            Save();
        }

        /// <summary>
        /// Очистить историю.
        /// </summary>
        public void Clear()
        {
            projects.Clear();
            Save();
        }

        private void Load()
        {
            projects.Clear();
            if (!File.Exists(storagePath))
                return;

            try
            {
                var lines = File.ReadAllLines(storagePath)
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrEmpty(line) && Directory.Exists(line))
                    .Distinct(StringComparer.OrdinalIgnoreCase);

                projects.AddRange(lines.Take(MaxCount));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось загрузить историю проектов: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Save()
        {
            try
            {
                File.WriteAllLines(storagePath, projects);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось сохранить историю проектов: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
