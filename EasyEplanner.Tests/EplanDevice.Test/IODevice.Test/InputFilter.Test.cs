using EplanDevice;
using NUnit.Framework;
using System.Linq;

namespace Tests.EplanDevices
{
    public class InputFilterTest
    {
        [TestCase("DI", "DI", "P_DT,P_DTO")]
        [TestCase("FS", "FS", "P_DT,P_DTO")]
        [TestCase("GS", "GS", "P_DT,P_DTO")]
        [TestCase("GS", "GS_INVERSE", "P_DT,P_DTO")]
        [TestCase("LS", "LS_MIN", "P_DT,P_DTO")]
        [TestCase("LS", "LS_MAX", "P_DT,P_DTO")]
        [TestCase("LS", "LS_IOLINK_MIN", "P_DT,P_ERR,P_DTO")]
        [TestCase("LS", "LS_IOLINK_MAX", "P_DT,P_ERR,P_DTO")]
        [TestCase("PDS", "PDS", "P_DT,P_DTO")]
        [TestCase("TS", "TS", "P_DT,P_DTO")]
        [TestCase("SB", "SB", "P_DT,P_DTO")]
        public void OffDelay_NewInput_DefaultAndLuaOrderAreCompatible(
            string type, string subtype, string expectedOrder)
        {
            IODevice device = CreateDevice(type);
            device.SetSubType(subtype);

            Assert.AreEqual(-1, device.Parameters[IODevice.Parameter.P_DTO]);
            CollectionAssert.AreEqual(expectedOrder.Split(','),
                device.Parameters.Keys.Select(x => x.Name));

            device.SetParameter("P_DT", 100);
            if (device.Parameters.ContainsKey(IODevice.Parameter.P_ERR))
            {
                device.SetParameter("P_ERR", -10);
            }

            string prefix = subtype.Contains("IOLINK") ?
                "100 --[[P_DT]], -10 --[[P_ERR]], " : "100 --[[P_DT]], ";
            StringAssert.Contains("par = {" + prefix + "-1 --[[P_DTO]] }",
                device.SaveAsLuaTable(string.Empty));

            Assert.AreEqual(string.Empty, device.SetParameter("P_DTO", 250));
            StringAssert.Contains("par = {" + prefix + "250 --[[P_DTO]] }",
                device.SaveAsLuaTable(string.Empty));
            Assert.IsTrue(device.GetDeviceProperties(device.DeviceType,
                device.DeviceSubType).ContainsKey(IODevice.Parameter.P_DTO));
        }

        private static IODevice CreateDevice(string type)
        {
            switch (type)
            {
                case "DI":
                    return new DI("DI1", "+A1-DI1", "Вход", 1, "A", 1);
                case "FS":
                    return new FS("FS1", "+A1-FS1", "Расход", 1, "A", 1, "");
                case "GS":
                    return new GS("GS1", "+A1-GS1", "Положение", 1, "A", 1,
                        "");
                case "LS":
                    return new LS("LS1", "+A1-LS1", "Уровень", 1, "A", 1,
                        "IFM.LMT100");
                case "PDS":
                    return new PDS("PDS1", "+A1-PDS1", "Давление", 1, "A", 1,
                        "");
                case "TS":
                    return new TS("TS1", "+A1-TS1", "Температура", 1, "A", 1,
                        "");
                case "SB":
                    return new SB("SB1", "+A1-SB1", "Кнопка", 1, "A", 1, "");
                default:
                    throw new System.ArgumentException(type);
            }
        }
    }
}
