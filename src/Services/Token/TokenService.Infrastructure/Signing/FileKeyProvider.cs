using System;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using TokenService.Application.Interfaces;

namespace TokenService.Infrastructure.Signing
{
    public class FileKeyProvider : IKeyProvider
    {
        private readonly RsaSecurityKey _privateKey;
        private readonly RsaSecurityKey _publicKey;

        public FileKeyProvider(IConfiguration config, ILogger<FileKeyProvider> logger)
        {
            var configuredPath = config["VcSigning:PrivateKeyPath"]
                ?? throw new InvalidOperationException("VcSigning:PrivateKeyPath not configured");

            // Brug base directory, så relative paths virker fra bin-folderen
            var keyPath = Path.IsPathRooted(configuredPath)
                ? configuredPath
                : Path.Combine(AppContext.BaseDirectory, configuredPath);

            // Sørg for at mappen findes
            var dir = Path.GetDirectoryName(keyPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Hvis filen ikke findes ELLER er tom ? generér en ny dev-nøgle
            if (!File.Exists(keyPath) || new FileInfo(keyPath).Length == 0)
            {
                logger.LogWarning("Private key file '{Path}' is missing or empty. Generating a new development key.", keyPath);

                using var rsaGen = RSA.Create(2048);
                var privateKeyPem = ExportPrivateKeyPem(rsaGen);

                File.WriteAllText(keyPath, privateKeyPem);
                logger.LogInformation("Generated new RSA private key at {Path}", keyPath);
            }

            // Læs PEM-indhold
            var pemContent = File.ReadAllText(keyPath);

            if (!pemContent.Contains("BEGIN") || !pemContent.Contains("PRIVATE KEY"))
            {
                throw new ArgumentException(
                    $"File '{keyPath}' does not appear to contain a valid PEM-encoded private key.",
                    nameof(pemContent));
            }

            var rsa = RSA.Create();
            rsa.ImportFromPem(pemContent);

            _privateKey = new RsaSecurityKey(rsa);
            _publicKey = new RsaSecurityKey(rsa.ExportParameters(false));

            logger.LogInformation("Loaded RSA private key from {Path}", keyPath);
        }

        public RsaSecurityKey GetPrivateKey() => _privateKey;
        public RsaSecurityKey GetPublicKey() => _publicKey;

        private static string ExportPrivateKeyPem(RSA rsa)
        {
            var builder = new System.Text.StringBuilder();
            builder.AppendLine("-----BEGIN PRIVATE KEY-----");
            builder.AppendLine(
                Convert.ToBase64String(
                    rsa.ExportPkcs8PrivateKey(),
                    Base64FormattingOptions.InsertLineBreaks));
            builder.AppendLine("-----END PRIVATE KEY-----");
            return builder.ToString();
        }
    }
}
