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
            _publicKey = ConfigurationManager.AppSettings["PublicKey"];
        }

        public string EncryptValue(string value)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(_publicKey);

                var itemAsBytes = Encoding.UTF8.GetBytes(value);
                var encryptedBytesID = rsa.Encrypt(itemAsBytes, RSAEncryptionPadding.Pkcs1);

                return Convert.ToBase64String(encryptedBytesID);
            }
        }
    }
}