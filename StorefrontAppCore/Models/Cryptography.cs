using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace StorefrontAppCore.Models
{
    public class Cryptography
    {
        private readonly string _publicKey;
        private readonly ILogger<Cryptography> _logger;

        public Cryptography(IOptions<AppSettings> appSettings, ILogger<Cryptography> logger)
        {
            _publicKey = appSettings.Value.PublicKey;
            _logger = logger;

            if (string.IsNullOrEmpty(_publicKey))
            {
                _logger.LogWarning("Public key variable not set.");
                throw new ApplicationException("Public key variable not set.");
            }
        }

        public string EncryptValue(string value)
        {
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(_publicKey);
                _logger.LogInformation("Encrypting item...");

                var itemAsBytes = Encoding.UTF8.GetBytes(value);
                var encryptedBytesID = rsa.Encrypt(itemAsBytes, RSAEncryptionPadding.Pkcs1);

                _logger.LogInformation("Item encrypted.");
                return Convert.ToBase64String(encryptedBytesID);
            }
        }
    }
}