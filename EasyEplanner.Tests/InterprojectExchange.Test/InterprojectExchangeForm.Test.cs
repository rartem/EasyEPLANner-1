using System.Collections;
using System.IO;
using System.Reflection;
using System.Threading;
using InterprojectExchange;
using NUnit.Framework;

namespace EasyEplannerTests.InterprojectExchangeTest
{
    public class InterprojectExchangeFormTest
    {
        [TestCase("DO_VIRT", "DO")]
        [TestCase("DI_VIRT", "DI")]
        [TestCase("AI_VIRT", "AI")]
        [TestCase("DO", "DO")]
        public void NormalizeChannelType_VirtAndBaseTypes_ReturnsBaseChannel(
            string deviceType, string expected)
        {
            var method = typeof(InterprojectExchangeForm).GetMethod(
                "NormalizeChannelType",
                BindingFlags.NonPublic | BindingFlags.Static);

            var result = method.Invoke(null, new object[] { deviceType });

            Assert.AreEqual(expected, result);
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void LoadAdvProjData_SelectedModel_FillsSignalsList()
        {
            var exchange = (InterprojectExchange.InterprojectExchange)
                InterprojectExchange.InterprojectExchange
                    .GetStandaloneInstance();
            exchange.Clear();
            exchange.SetStandaloneMainProjectName("MAIN");
            FilterConfiguration.SetStandaloneConfigurationDirectory(
                Path.Combine(Path.GetTempPath(), "EasyEPlanner.Tests"));

            var mainModel = new CurrentProjectModel();
            mainModel.AddPLCData("MAIN");
            var advancedModel = new AdvancedProjectModel
            {
                ProjectName = "ADV",
                Loaded = true,
                Selected = true,
            };
            advancedModel.AddDeviceData("ADV1DI1", "Вход связуемого проекта");
            exchange.AddModel(mainModel);
            exchange.AddModel(advancedModel);

            using (var form = new InterprojectExchangeForm("ADV"))
            {
                var method = typeof(InterprojectExchangeForm).GetMethod(
                    "LoadAdvProjData",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                method.Invoke(form, new object[] { "ADV" });

                var field = typeof(InterprojectExchangeForm).GetField(
                    "advProjItems",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var items = (IList)field.GetValue(form);

                Assert.AreEqual(1, items.Count);
            }

            exchange.Clear();
        }
    }
}
