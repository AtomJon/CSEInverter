using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace CSEInverter.Tests
{
    public class TestTask : IProductTask
    {
        public TestTask() { }

        public string GetDescription()
        {
            throw new NotImplementedException();
        }

        public void Initiate(TaskArguments args)
        {
            throw new NotImplementedException();
        }

        public void Run(TaskArguments args)
        {
            throw new NotImplementedException();
        }
    }

    [TestClass]
    public class ConfigTests
    {
        [TestMethod]
        public void LoadEmptyConfig()
        {
            using (MemoryStream stream = new MemoryStream())
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine("<Config></Config>");
                writer.Flush();
                stream.Position = 0;

                Config config = Config.LoadConfigFromStream(stream);

                Assert.IsNotNull(config);
                Assert.IsNull(config.Name);
            }
        }

        [TestMethod]
        public void LoadConfigWithName()
        {
            using (MemoryStream stream = new MemoryStream())
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine("<Config Name=\"ConfigName\"></Config>");
                writer.Flush();
                stream.Position = 0;

                Config config = Config.LoadConfigFromStream(stream);

                Assert.IsNotNull(config);
                Assert.IsNotNull(config.Name);

                Assert.AreEqual("ConfigName", config.Name);
            }
        }

        [TestMethod]
        public void LoadConfigWithTask()
        {
            using (MemoryStream stream = new MemoryStream())
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.WriteLine("<Config><Tasks><Task type=\"CSEInverter.Tests.TestTask, ApplicationTests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null\"><TestTask/></Task></Tasks></Config>");
                writer.Flush();
                stream.Position = 0;

                Config config = Config.LoadConfigFromStream(stream);

                Assert.IsNotNull(config);
                Assert.IsNull(config.Name);

                var tasks = config.GetTasks();

                Assert.AreEqual(1, tasks.Length);
                Assert.AreEqual(typeof(TestTask), tasks[0].GetType());
            }
        }

        [TestMethod]
        public void SerializeConfig()
        {
            new XmlSerializer(typeof(Config));
        }

        [TestMethod]
        public void LoadEmptyStream()
        {
            Assert.ThrowsException<ArgumentException>(() =>
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    Config.LoadConfigFromStream(stream);
                }
            });
        }

        [TestMethod]
        public void LoadNullStream()
        {
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                Config.LoadConfigFromStream(null);
            });
        }

        [TestMethod]
        public void LoadBadStream()
        {
            Assert.ThrowsException<ArgumentException>(() =>
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        writer.WriteLine("BAD");

                        Config.LoadConfigFromStream(stream);
                    }
                }
            });
        }

        [TestMethod]
        public void LoadBadConfig()
        {
            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                using (MemoryStream stream = new MemoryStream())
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.WriteLine("BAD");
                    writer.Flush();

                    Config.LoadConfigFromStream(stream);
                }
            });
        }

        [TestMethod]
        public void SaveZeroBufferStream()
        {
            Assert.ThrowsException<NotSupportedException>(() =>
            {
                byte[] buffer = new byte[0];
                Config config = new Config();

                using (MemoryStream stream = new MemoryStream(buffer))
                {
                    config.SaveToStream(stream);
                }
            });
        }

        [TestMethod]
        public void SaveReadonlyStream()
        {
            Assert.ThrowsException<ArgumentException>(() =>
            {
                byte[] buffer = new byte[512];

                using (MemoryStream stream = new MemoryStream(buffer, false))
                {
                    new Config().SaveToStream(stream);
                }
            });
        }

        [TestMethod]
        public void SaveEmptyConfig()
        {
            using (MemoryStream stream = new MemoryStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                Config config = new Config();
                config.SaveToStream(stream);
                stream.Position = 0;

                string configXml = reader.ReadToEnd();

                string hex = Convert.ToHexString(Encoding.UTF8.GetBytes(configXml));

                Assert.AreEqual(
                    "3C3F786D6C2076657273696F6E3D22312E30223F3E0D0A3C436F6E66696720786D6C6E733A7873693D22687474703A2F2F7777772E77332E6F72672F323030312F584D4C536368656D612D696E7374616E63652220786D6C6E733A7873643D22687474703A2F2F7777772E77332E6F72672F323030312F584D4C536368656D61223E0D0A20203C5461736B73202F3E0D0A3C2F436F6E6669673E",
                    hex
                );
            }
        }

        [TestMethod]
        public void SaveConfigWithName()
        {
            using (MemoryStream stream = new MemoryStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                Config config = new Config("ConfigName");
                config.SaveToStream(stream);
                stream.Position = 0;

                string configXml = reader.ReadToEnd();

                string hex = Convert.ToHexString(Encoding.UTF8.GetBytes(configXml));

                Assert.AreEqual(
                    "3C3F786D6C2076657273696F6E3D22312E30223F3E0D0A3C436F6E66696720786D6C6E733A7873693D22687474703A2F2F7777772E77332E6F72672F323030312F584D4C536368656D612D696E7374616E63652220786D6C6E733A7873643D22687474703A2F2F7777772E77332E6F72672F323030312F584D4C536368656D6122204E616D653D22436F6E6669674E616D65223E0D0A20203C5461736B73202F3E0D0A3C2F436F6E6669673E",
                    hex
                );
            }
        }

        [TestMethod]
        public void SaveConfigWithTask()
        {
            using (MemoryStream stream = new MemoryStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                Config config = new Config();
                config.AddTask(new TestTask());

                config.SaveToStream(stream);
                stream.Position = 0;

                string configXml = reader.ReadToEnd();

                string hex = Convert.ToHexString(Encoding.UTF8.GetBytes(configXml));

                Assert.AreEqual(
                    "3C3F786D6C2076657273696F6E3D22312E30223F3E0D0A3C436F6E66696720786D6C6E733A7873693D22687474703A2F2F7777772E77332E6F72672F323030312F584D4C536368656D612D696E7374616E63652220786D6C6E733A7873643D22687474703A2F2F7777772E77332E6F72672F323030312F584D4C536368656D61223E0D0A20203C5461736B733E0D0A202020203C5461736B20747970653D22435345496E7665727465722E54657374732E546573745461736B2C204170706C69636174696F6E54657374732C2056657273696F6E3D312E302E302E302C2043756C747572653D6E65757472616C2C205075626C69634B6579546F6B656E3D6E756C6C223E0D0A2020202020203C546573745461736B202F3E0D0A202020203C2F5461736B3E0D0A20203C2F5461736B733E0D0A3C2F436F6E6669673E",
                    hex
                );
            }
        }
    }
}