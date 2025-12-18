using System.Net.Http.Json;

namespace Wallet.Wasm.Services
{
    public class AccountClient
    {
        private readonly HttpClient _http;
        // API Gateway address; typically localhost:7005 for browser access if mapped.
        // Or if we run frontend on 8081, we assume gateway is accessible relative or absolute.
        // For development, we'll configure it.
        private const string BaseUrl = "http://localhost:7005/account/api/accounts";

        public AccountClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<(bool Success, string Message)> RegisterAsync(string email, string password)
        {
            try
            {
                var response = await _http.PostAsJsonAsync(BaseUrl, new 
                { 
                    email = email,
                    password = password
                });

                if (response.IsSuccessStatusCode)
                {
                    return (true, string.Empty);
                }

                var error = await response.Content.ReadAsStringAsync();
                return (false, $"Error {response.StatusCode}: {error}");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
