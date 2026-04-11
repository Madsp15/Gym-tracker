using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;

namespace Gym_tracker.Services;

/// <summary>
/// Calls the free wger Workout Manager API to provide exercise name suggestions.
/// Results are cached in-memory for the session to minimise network requests.
/// </summary>
public class WgerService
{
    private readonly HttpClient _http;
    private readonly Dictionary<string, List<WgerExerciseInfo>> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    public WgerService(HttpClient http) => _http = http;

    /// <summary>Returns up to 8 exercise suggestions for the given search term.</summary>
    public async Task<List<WgerExerciseInfo>> SearchExercisesAsync(string term)
    {
        if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            return new();

        if (_cache.TryGetValue(term, out var cached))
            return cached;

        try
        {
            var url = "https://wger.de/api/v2/exercise/search/" +
                      $"?term={Uri.EscapeDataString(term)}&language=english&format=json";

            // Ask the server to respond in English so category names arrive in English
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en"));

            var httpResponse = await _http.SendAsync(request);
            if (!httpResponse.IsSuccessStatusCode) return new();

            var response = await httpResponse.Content.ReadFromJsonAsync<WgerSearchResponse>();

            var results = response?.Suggestions
                .Where(s => !string.IsNullOrWhiteSpace(s.Value))
                .Select(s => new WgerExerciseInfo(s.Value, NormalizeCategory(s.Data.Category)))
                .DistinctBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .Take(8)
                .ToList() ?? new();

            _cache[term] = results;
            return results;
        }
        catch
        {
            // Silently degrade — input still works without suggestions
            return new();
        }
    }

    /// <summary>
    /// Maps wger category names (any language) to our canonical English muscle group names.
    /// wger returns category names in the user's browser locale, so we normalise here.
    /// </summary>
    private static string NormalizeCategory(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        return CategoryMap.TryGetValue(raw.Trim(), out var normalized) ? normalized : raw;
    }

    private static readonly Dictionary<string, string> CategoryMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // ── English ────────────────────────────────────
            ["Abs"]       = "Core",
            ["Arms"]      = "Biceps",
            ["Back"]      = "Back",
            ["Calves"]    = "Calves",
            ["Chest"]     = "Chest",
            ["Legs"]      = "Quads",
            ["Shoulders"] = "Shoulders",
            // ── Danish ─────────────────────────────────────
            ["Mavemuskler"] = "Core",
            ["Arme"]        = "Biceps",
            ["Ryg"]         = "Back",
            ["Lægge"]       = "Calves",
            ["Bryst"]       = "Chest",
            ["Ben"]         = "Quads",
            ["Skuldre"]     = "Shoulders",
            // ── Norwegian ──────────────────────────────────
            ["Mage"]         = "Core",
            ["Magemusklene"] = "Core",
            ["Armer"]        = "Biceps",
            ["Armene"]       = "Biceps",
            ["Rygg"]         = "Back",
            ["Ryggen"]       = "Back",
            ["Legger"]       = "Calves",
            ["Leggene"]      = "Calves",
            ["Brystet"]      = "Chest",
            ["Benene"]       = "Quads",
            ["Skuldrene"]    = "Shoulders",
            // ── Swedish ────────────────────────────────────
            ["Magen"]   = "Core",
            ["Armar"]   = "Biceps",
            ["Vader"]   = "Calves",
            ["Bröst"]   = "Chest",
            ["Axlar"]   = "Shoulders",
            // ── German ─────────────────────────────────────
            ["Bauch"]     = "Core",
            ["Rücken"]    = "Back",
            ["Waden"]     = "Calves",
            ["Brust"]     = "Chest",
            ["Beine"]     = "Quads",
            ["Schultern"] = "Shoulders",
            // ── Spanish ────────────────────────────────────
            ["Abdominales"]  = "Core",
            ["Brazos"]       = "Biceps",
            ["Espalda"]      = "Back",
            ["Pantorrillas"] = "Calves",
            ["Pecho"]        = "Chest",
            ["Piernas"]      = "Quads",
            ["Hombros"]      = "Shoulders",
            // ── French ─────────────────────────────────────
            ["Abdominaux"] = "Core",
            ["Bras"]       = "Biceps",
            ["Dos"]        = "Back",
            ["Mollets"]    = "Calves",
            ["Pectoraux"]  = "Chest",
            ["Jambes"]     = "Quads",
            ["Épaules"]    = "Shoulders",
        };
}

// ── Public result model ───────────────────────────────────────────────────────

public record WgerExerciseInfo(string Name, string Category);

// ── Internal JSON models ──────────────────────────────────────────────────────

public class WgerSearchResponse
{
    [JsonPropertyName("suggestions")]
    public List<WgerSuggestion> Suggestions { get; set; } = new();
}

public class WgerSuggestion
{
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public WgerSuggestionData Data { get; set; } = new();
}

public class WgerSuggestionData
{
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;
}

