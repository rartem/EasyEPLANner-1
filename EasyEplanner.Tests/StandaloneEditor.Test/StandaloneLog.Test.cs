using EasyEPlanner.StandaloneEditor;
using NUnit.Framework;
using System;
using System.IO;

namespace TestsStandaloneEditor
{
    class StandaloneLogTest
    {
        [Test]
        public void AddMessage_NewMessage_StoresCurrentAndProjectLog()
        {
            string directory = Path.Combine(
                TestContext.CurrentContext.WorkDirectory,
                "StandaloneLogTest_" + Guid.NewGuid());
            Directory.CreateDirectory(directory);

            try
            {
                var log = new StandaloneLog(directory);

                log.AddMessage("Ошибка проекта\n");
                log.AddMessage("Ошибка проекта\n");

                Assert.Multiple(() =>
                {
                    Assert.IsFalse(log.IsEmpty());
                    Assert.AreEqual(1, log.MessageCount);
                    StringAssert.Contains("Ошибка проекта",
                        File.ReadAllText(log.LogPath));
                });

                log.Clear();
                Assert.IsTrue(log.IsEmpty());
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
