using System;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TokenService.Application.Interfaces;

namespace TokenService.Infrastructure.Signing
{
    public class FileKeyProvider : IKeyProvider
    {
        private readonly RsaSecurityKey _privateKey;
        private readonly RsaSecurityKey _publicKey;
        
        public FileKeyProvider(IConfiguration config)
        {
            var keyPath = config["VcSigning:PrivateKeyPath"] 
                ?? throw new InvalidOperationException("PrivateKeyPath not configured");
            
            if (!File.Exists(keyPath))
            {
                 throw new FileNotFoundException($"Private key file not found at {keyPath}");
            }

            // Load RSA key from PEM file
            var pemContent = File.ReadAllText(keyPath);
            var rsa = RSA.Create();
            rsa.ImportFromPem(pemContent);
            
            _privateKey = new RsaSecurityKey(rsa);
            _publicKey = new RsaSecurityKey(rsa.ExportParameters(false));
        }
        
        public RsaSecurityKey GetPrivateKey() => _privateKey;
        public RsaSecurityKey GetPublicKey() => _publicKey;
    }
}
