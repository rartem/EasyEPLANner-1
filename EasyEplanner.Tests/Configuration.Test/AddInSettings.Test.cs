using System;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using EasyEPlanner;
using InterprojectExchange;
using NUnit.Framework;
using PInvoke;

namespace EasyEplannerTests.ConfigurationTest
{
    [TestFixture]
    public class AddInSettingsTest
    {
        private string directory;
        private string path;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(),
                "EplannerSettings-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            path = Path.Combine(directory, "configuration.ini");
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(path))
            {
                File.SetAttributes(path, FileAttributes.Normal);
            }
            Directory.Delete(directory, true);
        }

        [Test]
        public void Load_MissingFile_ReturnsRuntimeDefaultsWithoutCreatingFile()
        {
            var settings = AddInSettings.Load(path);
            Assert.That(File.Exists(path), Is.False);
            Assert.That(settings[Find("TestSettings", "ProjectTestEnabled")],
                Is.EqualTo("False"));
            foreach (string section in new[]
                { "currProjDevList", "advProjDevList" })
            {
                var enabled = AddInSettings.Definitions.Where(item =>
                    item.Section == section && settings[item] == "True");
                Assert.That(enabled.Select(item => item.Key),
                    Is.EquivalentTo(new[] { "DI", "DO", "AI", "AO" }));
                Assert.That(AddInSettings.Definitions.Where(item =>
                    item.Section == section).Select(item => item.Key),
                    Is.EquivalentTo(
                        FilterConfiguration.GetSupportedDeviceTypes()));
            }
        }

        [Test]
        public void Save_AllSettings_RoundTripsThroughRuntimeIniReader()
        {
            var settings = new AddInSettings();
            foreach (var item in AddInSettings.Definitions)
            {
                settings[item] = item.IsBoolean ? "True" : item.IsFolder
                    ? @"C:\Проекты\Макросы" : "https://help.example/база/";
            }
            settings.Save(path);

            var loaded = AddInSettings.Load(path);
            var runtimeReader = new IniFile(path);
            foreach (var item in AddInSettings.Definitions)
            {
                Assert.That(loaded[item], Is.EqualTo(settings[item]), item.Id);
                Assert.That(runtimeReader.ReadString(item.Section,
                    item.Key, "missing"), Is.EqualTo(settings[item]), item.Id);
            }
        }

        [Test]
        public void Save_LongProjectPaths_PreservesOrderAndCyrillic()
        {
            var item = Find("path", "folder_path");
            var settings = new AddInSettings();
            string[] roots = Enumerable.Range(1, 25).Select(index =>
                @"C:\Проекты и программы\Проект" + index).ToArray();
            settings[item] = string.Join(";", roots);
            settings.Save(path);
            Assert.That(new IniFile(path).ReadString("path", "folder_path",
                ""), Is.EqualTo(string.Join(";", roots)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Save_ExistingConfiguration_PreservesLatestCustomData(
            bool legacyAnsi)
        {
            Encoding encoding = legacyAnsi ? Encoding.Default : Encoding.UTF8;
            File.WriteAllText(path, "; Комментарий\r\n[path]\r\n" +
                "folder_path=\"C:\\Проекты\"\r\n" +
                "custom_path=keep\r\n[custom]\r\nanswer=old\r\n", encoding);
            var settings = AddInSettings.Load(path);
            Assert.That(settings[Find("path", "folder_path")],
                Is.EqualTo(@"C:\Проекты"));
            File.AppendAllText(path, "new_key=latest\r\n", encoding);
            settings.Save(path);

            var reader = new IniFile(path);
            Assert.That(reader.ReadString("custom", "new_key", ""),
                Is.EqualTo("latest"));
            Assert.That(reader.ReadString("path", "custom_path", ""),
                Is.EqualTo("keep"));
            Assert.That(File.ReadAllText(path), Does.Contain("; Комментарий"));
        }

        [Test]
        public void Save_QuotedPaths_NormalizesEachRoot()
        {
            var settings = new AddInSettings();
            var item = Find("path", "folder_path");
            settings[item] = " \"C:\\Проекты\" ; \"D:\\Lua\" ";
            settings.Save(path);
            Assert.That(AddInSettings.Load(path)[item],
                Is.EqualTo(@"C:\Проекты;D:\Lua"));
        }

        [Test]
        public void Load_InvalidBoolean_UsesRuntimeDefault()
        {
            File.WriteAllText(path, "[TestSettings]\r\n" +
                "ProjectTestEnabled=invalid");
            Assert.That(AddInSettings.Load(path)[
                Find("TestSettings", "ProjectTestEnabled")],
                Is.EqualTo("False"));
        }

        [TestCase("path", "folder_path", "relative")]
        [TestCase("path", "folder_path", @"C:relative")]
        [TestCase("path", "folder_path", @"C:\valid;;D:\other")]
        [TestCase("path", "wago_macros_path", "C:\\valid\n[extra]")]
        [TestCase("helpSystem", "address", "invalid")]
        [TestCase("helpSystem", "mainAddress", "file:///C:/test")]
        public void Save_InvalidValue_DoesNotChangeOriginal(
            string section, string key, string value)
        {
            File.WriteAllText(path, "; original");
            byte[] original = File.ReadAllBytes(path);
            var settings = AddInSettings.Load(path);
            settings[Find(section, key)] = value;
            Assert.Throws<ArgumentException>(() => settings.Save(path));
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
        }

        [Test]
        public void Save_ReadOnlyFile_ReportsFailureAndKeepsOriginal()
        {
            File.WriteAllText(path, "; original");
            byte[] original = File.ReadAllBytes(path);
            File.SetAttributes(path, FileAttributes.ReadOnly);
            Assert.Catch(() => new AddInSettings().Save(path));
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
            Assert.That(Directory.GetFiles(directory).Length, Is.EqualTo(1));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void SettingsForm_EditThenCancel_DoesNotModifyFile()
        {
            File.WriteAllText(path, "[path]\r\nfolder_path=C:\\Projects");
            byte[] original = File.ReadAllBytes(path);
            using (var form = new SettingsForm(path))
            {
                var editor = (TextBox)form.Controls.Find(
                    "folder_path", true).Single();
                editor.Text = @"D:\Другие проекты";
                form.Close();
            }
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(original));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void SettingsForm_Save_PersistsEditedFieldsAndDeviceFilters()
        {
            using (var form = new SettingsForm(path))
            {
                ShowOffScreen(form);
                ((TextBox)form.Controls.Find("folder_path", true).Single())
                    .Text = @"D:\Новые проекты";
                ((CheckBox)form.Controls.Find("ProjectTestEnabled", true)
                    .Single()).Checked = true;
                var devices = (CheckedListBox)form.Controls.Find(
                    "currProjDevList", true).Single();
                devices.SetItemChecked(devices.Items.IndexOf("DO_VIRT"), true);
                form.AcceptButton.PerformClick();
                Assert.That(form.DialogResult, Is.EqualTo(DialogResult.OK));
            }
            var settings = AddInSettings.Load(path);
            Assert.That(settings[Find("path", "folder_path")],
                Is.EqualTo(@"D:\Новые проекты"));
            Assert.That(settings[Find("TestSettings", "ProjectTestEnabled")],
                Is.EqualTo("True"));
            Assert.That(settings[Find("currProjDevList", "DO_VIRT")],
                Is.EqualTo("True"));
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void SettingsForm_InvalidPath_SelectsFieldWithoutSaving()
        {
            using (var form = new SettingsForm(path))
            {
                ShowOffScreen(form);
                var folder = (TextBox)form.Controls.Find("folder_path", true)
                    .Single();
                folder.Text = "invalid";
                var tabs = (TabControl)folder.Parent.Parent.Parent;
                tabs.SelectedIndex = 3;
                form.AcceptButton.PerformClick();
                Assert.That(File.Exists(path), Is.False);
                Assert.That(form.DialogResult, Is.EqualTo(DialogResult.None));
                Assert.That(tabs.SelectedIndex, Is.EqualTo(0));
                Assert.That(folder.Focused, Is.True);
            }
        }

        [Test]
        public void ReloadIfLoaded_ExistingFilter_RefreshesSelectedTypes()
        {
            new AddInSettings().Save(path);
            FilterConfiguration.SetStandaloneConfigurationDirectory(directory);
            FilterConfiguration.ResetFilter();
            try
            {
                var filter = FilterConfiguration.GetInstance();
                bool notified = false;
                filter.SignalsFilterChanged += (hard, current, advanced) =>
                    notified = hard;
                var settings = AddInSettings.Load(path);
                foreach (var item in AddInSettings.Definitions.Where(item =>
                    item.Section == "currProjDevList"))
                {
                    settings[item] = (item.Key == "DO_VIRT").ToString();
                }
                settings.Save(path);
                FilterConfiguration.ReloadIfLoaded(path);
                Assert.That(filter.CurrentProjectSelectedDevices,
                    Is.EqualTo(new[] { "DO_VIRT" }));
                Assert.That(notified, Is.True);
            }
            finally
            {
                FilterConfiguration.ResetFilter();
                FilterConfiguration.SetStandaloneConfigurationDirectory(null);
            }
        }

        private static void ShowOffScreen(Form form)
        {
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(-20000, -20000);
            form.Show();
        }

        private static AddInSetting Find(string section, string key)
        {
            return AddInSettings.Definitions.Single(item =>
                item.Section == section && item.Key == key);
        }
    }
}
