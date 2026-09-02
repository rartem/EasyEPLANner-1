using InterprojectExchange;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Tests.InterprojectExchangeTest
{
    [TestFixture]
    public class StandaloneInterprojectExchangeTest
    {
        private string repositoriesRoot;
        private string mainProjectFolder;
        private string advancedProjectFolder;

        [SetUp]
        public void SetUp()
        {
            repositoriesRoot = Path.Combine(Path.GetTempPath(),
                "EasyEPlannerExchangeTests", Guid.NewGuid().ToString("N"));
            mainProjectFolder = Path.Combine(repositoriesRoot, "MAIN");
            advancedProjectFolder = Path.Combine(repositoriesRoot, "ADV");
            Directory.CreateDirectory(mainProjectFolder);
            Directory.CreateDirectory(advancedProjectFolder);

            WriteMainIo(mainProjectFolder, "MAIN", 14,
                "MAIN1DO1", "MAIN1DO2");
            WriteMainIo(advancedProjectFolder, "ADV", 13,
                "ADV1DI1", "ADV1DI2");
            WriteInitialSharedFiles();
        }

        [TearDown]
        public void TearDown()
        {
            InterprojectExchange.InterprojectExchange
                .GetStandaloneInstance().Clear();
            if (Directory.Exists(repositoriesRoot))
            {
                Directory.Delete(repositoriesRoot, true);
            }
        }

        [Test]
        public void LoadEditAndSave_StandalonePair_UpdatesBothSharedFiles()
        {
            var starter = new InterprojectExchangeStarter(
                standaloneMode: true);

            bool loaded = starter.LoadStandaloneProjects(
                mainProjectFolder, advancedProjectFolder,
                new[] { repositoriesRoot }, out string errors);

            var exchange = InterprojectExchange.InterprojectExchange
                .GetStandaloneInstance();
            var mainModel = exchange.GetModel("MAIN");
            var advancedModel = exchange.GetModel("ADV");

            Assert.Multiple(() =>
            {
                Assert.IsTrue(loaded, errors);
                CollectionAssert.AreEquivalent(
                    new[] { "MAIN1DO1", "MAIN1DO2" },
                    mainModel.Devices.Select(device => device.Name));
                CollectionAssert.AreEquivalent(
                    new[] { "ADV1DI1", "ADV1DI2" },
                    advancedModel.Devices.Select(device => device.Name));
            });

            exchange.ChangeEditMode(0);
            CollectionAssert.AreEqual(
                new[] { "MAIN1DO1", "ADV1DI1" },
                exchange.GetBindedSignals()["DO"].Single());

            Assert.IsTrue(exchange.BindSignals("DO", "MAIN1DO2",
                "ADV1DI2"));
            Assert.IsTrue(exchange.DeleteSignalsBind("DO", "MAIN1DO1",
                "ADV1DI1"));

            var saver = new InterprojectExchangeSaver(exchange, "shared.lua");
            saver.SaveAsync().Wait();

            string mainShared = File.ReadAllText(Path.Combine(
                mainProjectFolder, "shared.lua"));
            string advancedShared = File.ReadAllText(Path.Combine(
                advancedProjectFolder, "shared.lua"));
            Assert.Multiple(() =>
            {
                Assert.That(mainShared, Does.Contain("MAIN1DO2"));
                Assert.That(mainShared, Does.Not.Contain("MAIN1DO1"));
                Assert.That(advancedShared, Does.Contain("__ADV1DI2"));
                Assert.That(advancedShared, Does.Not.Contain("__ADV1DI1"));
            });
        }

        private static void WriteMainIo(string projectFolder,
            string projectName, int deviceType, params string[] deviceNames)
        {
            var devices = new StringBuilder();
            foreach (string deviceName in deviceNames)
            {
                devices.AppendLine("    {");
                devices.AppendLine($"        name = '{deviceName}',");
                devices.AppendLine($"        descr = '{deviceName}',");
                devices.AppendLine($"        dtype = {deviceType},");
                devices.AppendLine("        subtype = 1,");
                devices.AppendLine("    },");
            }

            string mainIo = $@"PAC_name = '{projectName}'
nodes =
{{
    {{ ntype = 201, IP = '192.168.1.10', }},
}}
devices =
{{
{devices}}}";
            File.WriteAllText(Path.Combine(projectFolder, "main.io.lua"),
                mainIo, Encoding.UTF8);
        }

        private void WriteInitialSharedFiles()
        {
            string mainShared = @"remote_gateways =
{
}
shared_devices =
{
    {
        projectName = 'ADV',
        DO =
        {
            MAIN1DO1,
        },
    },
}";
            File.WriteAllText(Path.Combine(mainProjectFolder, "shared.lua"),
                mainShared, Encoding.UTF8);

            string advancedShared = @"remote_gateways =
{
    ['MAIN'] =
    {
        ip = '192.168.1.10',
        station = 1,
        DI =
        {
            __ADV1DI1,
        },
    },
}
shared_devices =
{
}";
            File.WriteAllText(Path.Combine(advancedProjectFolder,
                "shared.lua"), advancedShared, Encoding.UTF8);
        }
    }
}
