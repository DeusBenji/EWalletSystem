using System.Net.Http.Json;

namespace Wallet.Wasm.Services;

public class AdultVerificationResultDto
{
    public bool IsAdult { get; set; }
    public bool IsVerified { get; set; }
    public string? FailureReason { get; set; }
    public DateTime VerifiedAt { get; set; }
    public string? IssuerDid { get; set; }
}

public class AdultClient
{
    private readonly HttpClient _http;

    public AdultClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<AdultVerificationResultDto?> VerifyAdultAsync(string vcJwt)
    {
        try
        {
            // ✅ VIGTIGT:
            // Gateway matcher /validation/* (IKKE /api/validation/*)
            var response = await _http.PostAsJsonAsync(
                "validation/verify",
                new VerifyCredentialRequest
                {
                    VcJwt = vcJwt
                }
            );

            VerifyCredentialResponse? apiResult = null;

            try
            {
                apiResult = await response.Content.ReadFromJsonAsync<VerifyCredentialResponse>();
            }
            catch
            {
                // ignore JSON parse errors
            }

            if (apiResult is null)
            {
                return new AdultVerificationResultDto
                {
                    IsAdult = false,
                    IsVerified = false,
                    FailureReason = $"No/invalid response body (HTTP {(int)response.StatusCode})",
                    VerifiedAt = DateTime.UtcNow
                };
            }

            return new AdultVerificationResultDto
            {
                IsVerified = apiResult.Success,
                IsAdult = apiResult.Success, // AgeOver18 credential = verified
                FailureReason = apiResult.FailureReason,
                VerifiedAt = apiResult.VerifiedAt,
                IssuerDid = apiResult.IssuerDid
            };
        }
        catch (Exception ex)
        {
            return new AdultVerificationResultDto
            {
                IsAdult = false,
                IsVerified = false,
                FailureReason = ex.Message,
                VerifiedAt = DateTime.UtcNow
            };
        }
    }

    private sealed class VerifyCredentialRequest
    {
        public string VcJwt { get; set; } = default!;
    }

    private sealed class VerifyCredentialResponse
    {
        public bool Success { get; set; }
        public string? FailureReason { get; set; }
        public DateTime VerifiedAt { get; set; }
        public string? IssuerDid { get; set; }
    }
}
