using System.Net.Http.Json;

namespace Wallet.Wasm.Services;

public class AdultVerificationResultDto
{
    public bool IsAdult { get; set; }
}

public class AdultClient
{
    private readonly HttpClient _http;

    public AdultClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<AdultVerificationResultDto?> VerifyAdultAsync(string tokenId)
    {
        try 
        {
            // In a real app: await _http.PostAsJsonAsync("api/adult/verify", new { tokenId });
            // For Demo, mock response:
            await Task.Delay(500);
            return new AdultVerificationResultDto { IsAdult = true };
        }
        catch 
        {
            return null;
        }
    }
}
