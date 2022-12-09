using CSEInverter;
using Renci.SshNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace ApplicationTests
{
    [TestClass]
    public class FTPTests 
    {
        const string AnonymousCredentials = "anonymous";

        [TestMethod]
        public void Work()
        {
            var info = new ConnectionInfo("127.0.0.1", AnonymousCredentials, new PasswordAuthenticationMethod(AnonymousCredentials, AnonymousCredentials));

            using SftpClient client = new SftpClient(info);

            client.Connect();

            foreach (var entry in client.ListDirectory("/"))
            {
                Console.WriteLine(entry.FullName);
            }
        }
    }
}
