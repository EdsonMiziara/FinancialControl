using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace FinancialControl.Shared.Services;

public class SicoobService
{
    private readonly string _clientId;
    private readonly X509Certificate2 _certificate;
    private readonly HttpClient _httpClient;

    public SicoobService(string clientId, byte[] certBytes, string certPassword)
    {
        _clientId = clientId;
        _certificate = new X509Certificate2(certBytes, certPassword);

        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(_certificate);
        _httpClient = new HttpClient(handler);
    }

    public async Task<string> GetOfxStatementAsync(DateTime startDate, DateTime endDate)
    {
        // 1. Obter Token de Acesso (OAuth2)
        string token = await GetAccessTokenAsync();

        // 2. Chamar endpoint de extrato OFX
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _httpClient.DefaultRequestHeaders.Add("client_id", _clientId);

        var response = await _httpClient.GetAsync($"https://api.sicoob.com.br/extrato/v1/ofx/{startDate:yyyy-MM-dd}/{endDate:yyyy-MM-dd}");
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    private async Task<string> GetAccessTokenAsync()
    {
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("scope", "extrato_ofx")
        });

        var response = await _httpClient.PostAsync("https://auth.sicoob.com.br/auth/realms/cooperado/protocol/openid-connect/token", content);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString();
    }
}