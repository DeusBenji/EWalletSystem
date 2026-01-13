using System.Net.Http.Json;

namespace Wallet.Wasm.Services;

public class AuthClient
{
    private readonly HttpClient _http;

    // via gateway: /account -> account-service
    private const string LoginPath = "account/api/accounts/login";

    public AuthClient(HttpClient http) => _http = http;

    private sealed class LoginResponse
    {
        public Guid AccountId { get; set; }

        // Backend returnerer nu: { accountId, accessToken }
        public string? AccessToken { get; set; }
    }

    public async Task<(bool Success, Guid AccountId, string? Jwt, string Error)> LoginAsync(string email, string password)
    {
        var res = await _http.PostAsJsonAsync(LoginPath, new { email, password });

        if (!res.IsSuccessStatusCode)
        {
            var msg = await res.Content.ReadAsStringAsync();
            return (false, Guid.Empty, null, msg);
        }

        var payload = await res.Content.ReadFromJsonAsync<LoginResponse>();
        if (payload == null || payload.AccountId == Guid.Empty)
            return (false, Guid.Empty, null, "Login OK, but no AccountId returned.");

        // Jwt kan være null hvis backend ikke sender den (men den burde nu)
        return (true, payload.AccountId, payload.AccessToken, "");
    }
}
