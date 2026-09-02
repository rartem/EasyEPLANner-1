using InterprojectExchange;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EasyEPlanner.StandaloneEditor
{
    /// <summary>
    /// Проект, обнаруженный в одной из корневых папок репозитория.
    /// </summary>
    public sealed class RepositoryProject
    {
        public RepositoryProject(string projectName, string projectPath,
            string repositoryPath)
        {
            ProjectName = projectName;
            ProjectPath = projectPath;
            RepositoryPath = repositoryPath;
        }

        public string ProjectName { get; }
        public string ProjectPath { get; }
        public string RepositoryPath { get; }

        public override string ToString()
        {
            return $"{ProjectName} — {ProjectPath}";
        }
    }

    /// <summary>
    /// Управление набором корневых папок, непосредственные подпапки которых
    /// считаются проектами EasyEPlanner.
    /// </summary>
    public class ProjectRepositoryManager
    {
        private readonly string storagePath;
        private readonly List<string> repositories = new List<string>();

        public ProjectRepositoryManager()
            : this(GetDefaultStoragePath())
        {
        }

        /// <summary>
        /// Конструктор с заданным файлом хранения. Используется в тестах.
        /// </summary>
        public ProjectRepositoryManager(string storagePath)
        {
            if (string.IsNullOrWhiteSpace(storagePath))
            {
                throw new ArgumentException("Не задан файл репозиториев.",
                    nameof(storagePath));
            }

            this.storagePath = storagePath;
            Load();
        }

        public IReadOnlyList<string> Repositories =>
            repositories.AsReadOnly();

        /// <summary>
        /// Добавить корневую папку в список репозиториев.
        /// </summary>
        public bool TryAdd(string path, out string error)
        {
            error = null;
            if (!TryNormalizeDirectory(path, out string normalized,
                out error))
            {
                return false;
            }

            if (repositories.Any(item => string.Equals(item, normalized,
                StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            repositories.Add(normalized);
            repositories.Sort(StringComparer.OrdinalIgnoreCase);
            if (!TrySave(out error))
            {
                repositories.Remove(normalized);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Удалить корневую папку из списка репозиториев.
        /// </summary>
        public bool TryRemove(string path, out string error)
        {
            error = null;
            int index = repositories.FindIndex(item => string.Equals(item,
                path, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                return true;
            }

            string removed = repositories[index];
            repositories.RemoveAt(index);
            if (!TrySave(out error))
            {
                repositories.Insert(index, removed);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Найти проекты в непосредственных подпапках всех репозиториев.
        /// </summary>
        public IReadOnlyList<RepositoryProject> DiscoverProjects(
            out IReadOnlyList<string> errors)
        {
            var projects = new List<RepositoryProject>();
            var discoveryErrors = new List<string>();

            foreach (string repository in repositories)
            {
                if (!Directory.Exists(repository))
                {
                    discoveryErrors.Add(
                        $"Репозиторий недоступен: {repository}");
                    continue;
                }

                string[] folders;
                try
                {
                    folders = Directory.GetDirectories(repository);
                }
                catch (Exception ex) when (ex is IOException ||
                    ex is UnauthorizedAccessException)
                {
                    discoveryErrors.Add(
                        $"Не удалось прочитать {repository}: {ex.Message}");
                    continue;
                }

                foreach (string folder in folders)
                {
                    try
                    {
                        if (MainIoProjectNameReader.TryReadFromFolder(folder,
                            out string projectName, out string readError))
                        {
                            projects.Add(new RepositoryProject(projectName,
                                Path.GetFullPath(folder), repository));
                        }
                        else
                        {
                            discoveryErrors.Add(
                                $"{folder}: {readError}");
                        }
                    }
                    catch (Exception ex) when (ex is IOException ||
                        ex is UnauthorizedAccessException)
                    {
                        discoveryErrors.Add(
                            $"Не удалось прочитать {folder}: {ex.Message}");
                    }
                }
            }

            projects = projects
                .OrderBy(project => project.ProjectName,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(project => project.ProjectPath,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

            InterprojectProjectCatalog.Invalidate();
            InterprojectProjectCatalog.BuildIndexFromRoots(repositories);

            errors = discoveryErrors.AsReadOnly();
            return projects.AsReadOnly();
        }

        private void Load()
        {
            repositories.Clear();
            if (!File.Exists(storagePath))
            {
                return;
            }

            try
            {
                IEnumerable<string> paths = File.ReadAllLines(storagePath)
                    .Select(path => path.Trim())
                    .Where(path => !string.IsNullOrEmpty(path))
                    .Distinct(StringComparer.OrdinalIgnoreCase);
                repositories.AddRange(paths);
                repositories.Sort(StringComparer.OrdinalIgnoreCase);
            }
            catch (IOException)
            {
                // Ошибка будет показана при следующем изменении списка.
            }
            catch (UnauthorizedAccessException)
            {
                // Ошибка будет показана при следующем изменении списка.
            }
        }

        private bool TrySave(out string error)
        {
            try
            {
                string folder = Path.GetDirectoryName(storagePath);
                if (!string.IsNullOrEmpty(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                File.WriteAllLines(storagePath, repositories);
                error = null;
                return true;
            }
            catch (Exception ex) when (ex is IOException ||
                ex is UnauthorizedAccessException)
            {
                error = "Не удалось сохранить список репозиториев: " +
                    ex.Message;
                return false;
            }
        }

        private static bool TryNormalizeDirectory(string path,
            out string normalized, out string error)
        {
            normalized = null;
            error = null;
            if (string.IsNullOrWhiteSpace(path))
            {
                error = "Не выбрана папка репозитория.";
                return false;
            }

            try
            {
                normalized = Path.GetFullPath(path.Trim());
                string root = Path.GetPathRoot(normalized);
                if (!string.Equals(normalized, root,
                    StringComparison.OrdinalIgnoreCase))
                {
                    normalized = normalized.TrimEnd(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar);
                }
            }
            catch (Exception ex) when (ex is ArgumentException ||
                ex is NotSupportedException || ex is PathTooLongException)
            {
                error = "Некорректный путь репозитория: " + ex.Message;
                return false;
            }

            if (!Directory.Exists(normalized))
            {
                error = "Папка репозитория не существует.";
                return false;
            }

            return true;
        }

        private static string GetDefaultStoragePath()
        {
            string appDataPath = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(appDataPath,
                "EasyEPlanner.StandaloneEditor", "project_repositories.txt");
        }
    }
}
