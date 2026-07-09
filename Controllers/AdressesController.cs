using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Application_Camion_API.Controllers;

[Route("api/adresses")]
[ApiController]
public class AdressesController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AdressesController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("autocomplete")]
    public async Task<ActionResult<List<string>>> Autocomplete([FromQuery] string texte)
    {
        texte = (texte ?? "").Trim();

        if (texte.Length < 3)
            return new List<string>();

        string url =
            "https://data.geopf.fr/geocodage/completion/" +
            $"?text={Uri.EscapeDataString(texte)}" +
            "&type=StreetAddress" +
            "&maximumResponses=5";

        using HttpClient httpClient =
            _httpClientFactory.CreateClient();

        using HttpResponseMessage response =
            await httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return StatusCode(502, "Le service d'adresses est indisponible.");

        string json = await response.Content.ReadAsStringAsync();
        using JsonDocument document = JsonDocument.Parse(json);

        List<string> adresses = new List<string>();

        if (document.RootElement.TryGetProperty("results", out JsonElement results))
        {
            foreach (JsonElement result in results.EnumerateArray())
            {
                if (result.TryGetProperty("fulltext", out JsonElement fulltext))
                {
                    string? adresse = fulltext.GetString();

                    if (!string.IsNullOrWhiteSpace(adresse))
                        adresses.Add(adresse);
                }
            }
        }

        return adresses
            .Distinct()
            .ToList();
    }
}
