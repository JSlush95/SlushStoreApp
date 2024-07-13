using StorefrontApp.Utilities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace StorefrontApp.Models
{
    public class Cryptography
    {
        private readonly string _publicKey;

        public Cryptography()
        {
            _publicKey = EnvironmentVariables.PublicKey;
            if (string.IsNullOrEmpty(_publicKey))
            {
                Log.Warn("Public key environment variable not set.");
                throw new ApplicationException("Public key environment variable not set.");
            }
        }

        public string EncryptValue(string value)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(_publicKey);
                Log.Info("Encrypting item...");

                var itemAsBytes = Encoding.UTF8.GetBytes(value);
                var encryptedBytesID = rsa.Encrypt(itemAsBytes, RSAEncryptionPadding.Pkcs1);

                Log.Info("Item encrypted.");
                return Convert.ToBase64String(encryptedBytesID);
            }
        }
    }
}