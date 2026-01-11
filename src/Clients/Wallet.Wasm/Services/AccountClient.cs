using System.Net.Http.Json;

namespace Wallet.Wasm.Services
{
    public class AccountClient
    {
        private readonly HttpClient _http;

        // Via gateway: /account -> account-service, og gateway fjerner /account prefix
        // Så denne ender som /api/accounts inde i account-service
        private const string RegisterPath = "account/api/accounts";

        public AccountClient(HttpClient http)
        {
            _http = http;
        }

        // Matcher typisk AccountResponse fra backend (har Id)
        private sealed class RegisterResponse
        {
            public Guid Id { get; set; }

            // fallback hvis nogen har lavet camelCase
            public Guid AccountId { get; set; }
        }

        public async Task<(bool Success, Guid AccountId, string Message)> RegisterAsync(string email, string password)
        {
            try
            {
                var response = await _http.PostAsJsonAsync(RegisterPath, new
                {
                    email,
                    password
                });

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return (false, Guid.Empty, $"Error {response.StatusCode}: {error}");
                }

                // Backend returnerer AccountResponse med Id
                var payload = await response.Content.ReadFromJsonAsync<RegisterResponse>();

                var id = payload?.Id != Guid.Empty ? payload!.Id : payload?.AccountId ?? Guid.Empty;

                if (id == Guid.Empty)
                {
                    // Success, men vi kunne ikke parse Id - så fejl, fordi MitID flow kræver AccountId
                    return (false, Guid.Empty, "Account created, but could not read AccountId from response.");
                }

                return (true, id, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, Guid.Empty, ex.Message);
            }
        }
    }
}
