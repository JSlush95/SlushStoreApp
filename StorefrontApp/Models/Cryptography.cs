﻿using System;
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

        public string EncryptID(int keyID)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(_publicKey);

                var keyIDBytes = Encoding.UTF8.GetBytes(keyID.ToString());
                var encryptedBytesID = rsa.Encrypt(keyIDBytes, RSAEncryptionPadding.Pkcs1);

                return Convert.ToBase64String(encryptedBytesID);
            }
        }
    }
}