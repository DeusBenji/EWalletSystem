using System.Net.Http.Json;
using Domain.Models;
using Microsoft.Extensions.Configuration;
using Application.Interfaces;
using Infrastructure.Blockchain.DTOs;

namespace Infrastructure.Blockchain
{
    public class FabricLookupClient : IFabricLookupClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public FabricLookupClient(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _baseUrl = config["Fabric:BaseUrl"] ?? "http://localhost:8080";
        }

        /// <summary>
        /// Resolver en DID til et DID Document fra Fabric
        /// </summary>
        public async Task<DidDocument?> ResolveDidAsync(string did, CancellationToken ct = default)
        {
            try
            {
                // Kald Go service: GET /dids/{did}
                var response = await _httpClient.GetAsync($"{_baseUrl}/dids/{did}", ct);

                if (!response.IsSuccessStatusCode)
                    return null;

                var result = await response.Content.ReadFromJsonAsync<GoDidDocumentResponse>(cancellationToken: ct);

                if (result == null)
                    return null;

                // Map fra Go response til domain model
                return new DidDocument
                {
                    Id = result.Id,
                    VerificationMethods = result.VerificationMethod?.Select(vm => vm.Id).ToList() ?? new(),
                    AssertionMethods = result.AssertionMethod ?? new()
                };
            }
            catch (HttpRequestException)
            {
                // DID ikke fundet eller netværksfejl
                return null;
            }
        }

        /// <summary>
        /// Checker om en hash eksisterer på Fabric blockchain
        /// </summary>
        public async Task<bool> HashExistsAsync(string hash, CancellationToken ct = default)
        {
            try
            {
                // Kald Go service: GET /anchors/{hash}/verify
                var response = await _httpClient.GetAsync($"{_baseUrl}/anchors/{hash}/verify", ct);

                if (!response.IsSuccessStatusCode)
                    return false;

                var result = await response.Content.ReadFromJsonAsync<GoVerifyAnchorResponse>(cancellationToken: ct);
                return result?.Exists ?? false;
            }
            catch (HttpRequestException)
            {
                return false;
            }
        }

        

       
    }
}