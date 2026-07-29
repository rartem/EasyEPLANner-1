using EplanDevice;
using IO;
using System;
using System.IO;
using System.Windows.Forms;
using TechObject;

namespace EasyEPlanner.StandaloneEditor
{
    /// <summary>
    /// Загрузка проекта в standalone-редактор из каталога с Lua-файлами.
    /// </summary>
    public class StandaloneProjectLoader
    {
        public string ProjectDirectory { get; private set; }
        public string ProjectName { get; private set; }
        public string OriginalMainIoLua { get; private set; }

        /// <summary>
        /// Загрузить проект из указанного каталога.
        /// </summary>
        public bool Load(string projectDirectory)
        {
            if (!Directory.Exists(projectDirectory))
            {
                MessageBox.Show("Указанный каталог проекта не найден.",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            string objectsPath = Path.Combine(projectDirectory,
                ProjectDescriptionSaver.MainTechObjectsFileName);
            if (!File.Exists(objectsPath))
            {
                MessageBox.Show(
                    $"В каталоге проекта отсутствует файл " +
                    $"{ProjectDescriptionSaver.MainTechObjectsFileName}.",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            ProjectDirectory = projectDirectory;
            ProjectName = new DirectoryInfo(projectDirectory).Name;

            InitializeEnvironment();

            try
            {
                LoadIO();
                LoadTechObjects();
                LoadRestrictions();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки проекта: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            Editor.Editor.StandaloneMode = true;
            var editor = Editor.Editor.GetInstance();
            var techObjectManager = TechObjectManager.GetInstance();
            techObjectManager.AddParent(null);
            editor.EditorForm.Init(techObjectManager);

            return true;
        }

        private void InitializeEnvironment()
        {
            string assemblyPath = AppDomain.CurrentDomain.BaseDirectory
                .TrimEnd('\\');
            ProjectManager.GetInstance().OriginalAssemblyPath = assemblyPath;

            Logs.Init(new StandaloneLog());

            IOManager.GetInstance();
            DeviceManager.GetInstance();

            var baseTechObjectManager = BaseTechObjectManager.GetInstance();
            var baseTechObjectLoader = new BaseTechObjectLoader();
            baseTechObjectLoader.LoadTo(baseTechObjectManager);
        }

        private void LoadIO()
        {
            string ioPath = Path.Combine(ProjectDirectory, "main.io.lua");
            if (!File.Exists(ioPath))
            {
                OriginalMainIoLua = string.Empty;
                return;
            }

            OriginalMainIoLua = File.ReadAllText(ioPath);
            var ioLoader = new StandaloneIoLoader();
            ioLoader.Load(OriginalMainIoLua);
        }

        private void LoadTechObjects()
        {
            string objectsPath = Path.Combine(ProjectDirectory,
                ProjectDescriptionSaver.MainTechObjectsFileName);

            EncodingDetector.MainFilesEncoding =
                EncodingDetector.DetectFileEncoding(objectsPath);

            string objectsLua = File.ReadAllText(objectsPath,
                EncodingDetector.MainFilesEncoding);

            TechObjectManager.GetInstance().LoadDescription(objectsLua,
                ProjectName);
            TechObjectManager.GetInstance().GenericTechObjects
                .ForEach(obj => obj.Update());
        }

        private void LoadRestrictions()
        {
            string restrictionsPath = Path.Combine(ProjectDirectory,
                ProjectDescriptionSaver.MainRestrictionsFileName);
            if (!File.Exists(restrictionsPath))
            {
                return;
            }

            string restrictionsLua = File.ReadAllText(restrictionsPath,
                EncodingDetector.MainFilesEncoding ??
                System.Text.Encoding.UTF8);

            TechObjectManager.GetInstance().LoadRestrictions(restrictionsLua);
        }
    }
}
