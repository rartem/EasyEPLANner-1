using EasyEPlanner.StandaloneEditor;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

namespace EasyEplanner.Tests.StandaloneEditor.Test
{
    [TestFixture]
    public class ProjectRepositoryManagerTest
    {
        private string testDirectory;
        private string repositoryDirectory;
        private string storagePath;

        [SetUp]
        public void SetUp()
        {
            testDirectory = Path.Combine(Path.GetTempPath(),
                "EasyEPlannerRepositoryTests", Guid.NewGuid().ToString("N"));
            repositoryDirectory = Path.Combine(testDirectory, "repository");
            storagePath = Path.Combine(testDirectory, "repositories.txt");
            Directory.CreateDirectory(repositoryDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, true);
            }
        }

        [Test]
        public void TryAdd_ExistingRepository_PersistsNormalizedPath()
        {
            var manager = new ProjectRepositoryManager(storagePath);

            bool added = manager.TryAdd(repositoryDirectory,
                out string error);
            var loadedManager = new ProjectRepositoryManager(storagePath);

            Assert.Multiple(() =>
            {
                Assert.IsTrue(added, error);
                Assert.IsNull(error);
                Assert.AreEqual(1, loadedManager.Repositories.Count);
                Assert.AreEqual(Path.GetFullPath(repositoryDirectory),
                    loadedManager.Repositories[0]);
            });
        }

        [Test]
        public void DiscoverProjects_ImmediateSubfolders_ReadsPacName()
        {
            CreateProject("folder-a", "PAC_A");
            string nestedContainer = Path.Combine(repositoryDirectory,
                "container");
            Directory.CreateDirectory(nestedContainer);
            CreateProject(Path.Combine("container", "nested"), "PAC_NESTED");

            var manager = new ProjectRepositoryManager(storagePath);
            Assert.IsTrue(manager.TryAdd(repositoryDirectory, out _));

            var projects = manager.DiscoverProjects(out var errors);

            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, projects.Count);
                Assert.AreEqual("PAC_A", projects[0].ProjectName);
                Assert.AreEqual(Path.Combine(repositoryDirectory, "folder-a"),
                    projects[0].ProjectPath);
                Assert.That(errors.Any(error =>
                    error.Contains("container")), Is.True);
            });
        }

        [Test]
        public void TryRemove_ExistingRepository_RemovesPersistedPath()
        {
            var manager = new ProjectRepositoryManager(storagePath);
            Assert.IsTrue(manager.TryAdd(repositoryDirectory, out _));

            bool removed = manager.TryRemove(repositoryDirectory,
                out string error);
            var loadedManager = new ProjectRepositoryManager(storagePath);

            Assert.Multiple(() =>
            {
                Assert.IsTrue(removed, error);
                Assert.IsEmpty(loadedManager.Repositories);
            });
        }

        private void CreateProject(string relativeFolder, string projectName)
        {
            string projectFolder = Path.Combine(repositoryDirectory,
                relativeFolder);
            Directory.CreateDirectory(projectFolder);
            File.WriteAllText(Path.Combine(projectFolder, "main.io.lua"),
                $"PAC_name = '{projectName}'");
        }
    }
}
