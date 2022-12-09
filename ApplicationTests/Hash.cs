using System;
using System.Security.Cryptography;

namespace CSEInverter.Tests
{
    class Hasher
    {
        public static string Hash(byte[] data)
        {
            return Convert.ToBase64String(new SHA1Managed().ComputeHash(data));
        }
    }
}
