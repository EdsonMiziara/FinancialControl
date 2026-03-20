using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

namespace FinancialControl.Shared.Services;

public class SicoobService
{
    private readonly string _clientId;
    private readonly X509Certificate2 _certificate;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Constructor for SicoobService that initializes the service with the provided client ID, certificate bytes, and certificate password.
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="certBytes"></param>
    /// <param name="certPassword"></param>
    
    public SicoobService(string clientId, byte[] certBytes, string certPassword)
    {
        _clientId = clientId;
        _certificate = new X509Certificate2(certBytes, certPassword);

        var handler = new HttpClientHandler();
        handler.ClientCertificates.Add(_certificate);
        _httpClient = new HttpClient(handler);
    }

    /// <summary>
    /// Obtain OFX statement from Sicoob API for the given date range. It first retrieves an access token using client credentials flow,
    /// </summary>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <returns>
    /// Returns the OFX statement as a string. The caller is responsible for parsing the OFX content.
    /// If the API call fails, an exception will be thrown.
    /// </returns>
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

    /// <summary>
    /// Obtains an access token from Sicoob's authentication server using the client credentials flow.
    /// It sends a POST request with the required parameters,
    /// </summary>
    /// <returns>
    /// Returns the access token as a string. If the request fails or the response does not contain an access token, an exception will be thrown.
    /// </returns>
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