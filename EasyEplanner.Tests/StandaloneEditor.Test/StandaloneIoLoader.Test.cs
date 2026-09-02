using EasyEPlanner.StandaloneEditor;
using EplanDevice;
using NUnit.Framework;

namespace TestsStandaloneEditor
{
    class StandaloneIoLoaderTest
    {
        [Test]
        public void Load_NonstandardDeviceName_RestoresTypeAndSubtype()
        {
            const string lua = @"
devices =
{
    {
        name = 'SAFETYDI1',
        descr = 'Безопасность',
        dtype = 13,
        subtype = 1,
    },
}";
            var deviceManager = DeviceManager.GetInstance();

            new StandaloneIoLoader().Load(lua);

            IODevice device = deviceManager.GetDevice("SAFETYDI1");
            Assert.Multiple(() =>
            {
                Assert.AreEqual("SAFETYDI1", device.Name);
                Assert.AreEqual(DeviceType.DI, device.DeviceType);
                Assert.AreEqual(DeviceSubType.DI, device.DeviceSubType);
            });
        }
    }
}
