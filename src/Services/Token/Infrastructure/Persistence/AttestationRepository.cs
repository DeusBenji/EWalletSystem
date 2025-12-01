using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Domain.Models;
using Domain.Repositories;

namespace Infrastructure.Persistence
{
    public class AttestationRepository : IAttestationRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<AttestationRepository> _logger;

        public AttestationRepository(IConfiguration configuration, ILogger<AttestationRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("TokenServiceConnection")
                ?? throw new InvalidOperationException("Missing connection string 'TokenServiceConnection'.");
            _logger = logger;
        }

        private SqlConnection CreateConnection()
        {
            try
            {
                return new SqlConnection(_connectionString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create SQL connection");
                throw;
            }
        }

        public async Task SaveAsync(AgeAttestation attestation, CancellationToken ct = default)
        {
            const string sql = @"
                INSERT INTO Attestations (
                    Id, AccountId, SubjectId, IsAdult, IssuedAt, ExpiresAt, Token, Hash
                ) VALUES (
                    @Id, @AccountId, @SubjectId, @IsAdult, @IssuedAt, @ExpiresAt, @Token, @Hash
                );";

            using var conn = CreateConnection();

            var parameters = new
            {
                attestation.Id,
                attestation.AccountId,
                attestation.SubjectId,
                attestation.IsAdult,
                attestation.IssuedAt,
                attestation.ExpiresAt,
                attestation.Token,
                attestation.Hash
            };

            await conn.OpenAsync(ct);

            // Brug CommandDefinition for at få cancellation token med ind i Dapper
            var command = new CommandDefinition(sql, parameters, cancellationToken: ct);
            await conn.ExecuteAsync(command);
        }

        public async Task<AgeAttestation?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            const string sql = @"
                SELECT Id, AccountId, SubjectId, IsAdult, IssuedAt, ExpiresAt, Token, Hash
                FROM Attestations
                WHERE Id = @Id";

            using var conn = CreateConnection();
            await conn.OpenAsync(ct);

            var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: ct);

            // Vi laver en lille intern DTO til mapping, fordi AgeAttestation typisk har en custom ctor
            var row = await conn.QuerySingleOrDefaultAsync<AttestationRow>(command);

            if (row is null)
                return null;

            // Tilpas ctor’en til din konkrete AgeAttestation-implementering
            return new AgeAttestation(
                row.Id,
                row.AccountId,
                row.SubjectId,
                row.IsAdult,
                row.IssuedAt,
                row.ExpiresAt,
                row.Token,
                row.Hash
                );
        }

        // Intern type til Dapper-mapping (må gerne ligge nederst i klassen)
        private sealed class AttestationRow
        {
            public Guid Id { get; set; }
            public Guid AccountId { get; set; }
            public string SubjectId { get; set; } = default!;
            public bool IsAdult { get; set; }
            public DateTime IssuedAt { get; set; }
            public DateTime ExpiresAt { get; set; }
            public string Token { get; set; } = default!;
            public string Hash { get; set; } = default!;
        }
    }
}
