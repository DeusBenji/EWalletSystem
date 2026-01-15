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
            _baseUrl = (config["Fabric:BaseUrl"] ?? "http://localhost:8080").TrimEnd('/');
        }

        public async Task<DidDocument?> ResolveDidAsync(string did, CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(did))
                    return null;

                // ✅ encode DID
                var didEncoded = Uri.EscapeDataString(did);

                // Go service: GET /dids/{did}
                var response = await _httpClient.GetAsync($"{_baseUrl}/dids/{didEncoded}", ct);
                if (!response.IsSuccessStatusCode)
                    return null;

                var result = await response.Content.ReadFromJsonAsync<GoDidDocumentResponse>(cancellationToken: ct);
                if (result == null)
                    return null;

                var doc = new DidDocument
                {
                    Id = result.Id,
                    VerificationMethods = result.VerificationMethod?.Select(vm => vm.Id).ToList() ?? new(),
                    AssertionMethods = result.AssertionMethod ?? new()
                };

                if (result.VerificationMethod != null)
                {
                    foreach (var vm in result.VerificationMethod)
                    {
                        doc.VerificationMethodDetails.Add(new DidVerificationMethod
                        {
                            Id = vm.Id,
                            Type = vm.Type,
                            Controller = vm.Controller,

                            // ✅ DTO felter du HAR:
                            PublicKeyJwk = vm.PublicKeyJwk,

                            // ✅ Base58 bliver lagt her (så validator kan bruge det hvis den kan)
                            PublicKeyMultibase = vm.PublicKeyBase58
                        });
                    }
                }

                return doc;
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        public async Task<bool> HashExistsAsync(string hash, CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(hash))
                    return false;

                var hashEncoded = Uri.EscapeDataString(hash);

                // Go service: GET /anchors/{hash}/verify
                var response = await _httpClient.GetAsync($"{_baseUrl}/anchors/{hashEncoded}/verify", ct);
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
