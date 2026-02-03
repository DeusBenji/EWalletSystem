using System.Net.Http.Json;
using Application.Interfaces;

namespace Infrastructure.Blockchain
{
    public class FabricAnchorClient : IFabricAnchorClient
    {
        private readonly HttpClient _httpClient;

        public FabricAnchorClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task AnchorHashAsync(string attestationHash, CancellationToken ct = default)
        {
            var payload = new { hash = attestationHash };
            var response = await _httpClient.PostAsJsonAsync("/anchors", payload, ct);
            response.EnsureSuccessStatusCode();
        }
    }
}
