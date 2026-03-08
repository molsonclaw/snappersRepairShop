using System.Text.Json;

namespace SnappersRepairShop.Services;

/// <summary>
/// Reads license plate text from images using Azure AI Vision OCR
/// </summary>
public class LicensePlateOcrService
{
    private readonly HttpClient _httpClient;
    private readonly string? _endpoint;
    private readonly string? _apiKey;
    private readonly ILogger<LicensePlateOcrService> _logger;

    public LicensePlateOcrService(IConfiguration configuration, ILogger<LicensePlateOcrService> logger)
    {
        _httpClient = new HttpClient();
        _endpoint = configuration["AzureVision:Endpoint"];
        _apiKey = configuration["AzureVision:ApiKey"];
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrEmpty(_endpoint) && !string.IsNullOrEmpty(_apiKey);

    /// <summary>
    /// Extract license plate text from a base64-encoded image
    /// </summary>
    public async Task<string?> ReadLicensePlateAsync(string base64Image)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("Azure Vision not configured - OCR disabled");
            return null;
        }

        try
        {
            var imageBytes = Convert.FromBase64String(base64Image);

            var url = $"{_endpoint!.TrimEnd('/')}/computervision/imageanalysis:analyze?api-version=2024-02-01&features=read";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);
            request.Content = new ByteArrayContent(imageBytes);
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Azure Vision API error: {StatusCode} - {Response}", response.StatusCode, json);
                return null;
            }

            return ExtractPlateText(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Azure Vision OCR");
            return null;
        }
    }

    /// <summary>
    /// Parse the Vision API response and find the most likely license plate text
    /// </summary>
    private string? ExtractPlateText(string jsonResponse)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;

            if (!root.TryGetProperty("readResult", out var readResult))
                return null;

            if (!readResult.TryGetProperty("blocks", out var blocks))
                return null;

            var allLines = new List<string>();

            foreach (var block in blocks.EnumerateArray())
            {
                if (!block.TryGetProperty("lines", out var lines))
                    continue;

                foreach (var line in lines.EnumerateArray())
                {
                    if (line.TryGetProperty("text", out var text))
                    {
                        allLines.Add(text.GetString() ?? "");
                    }
                }
            }

            // License plates are typically 5-8 alphanumeric characters
            // Look for the best match
            foreach (var line in allLines)
            {
                var cleaned = new string(line.Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-').ToArray()).Trim();

                // US plates are typically 5-8 chars, possibly with a space or dash
                if (cleaned.Length >= 4 && cleaned.Length <= 10 && cleaned.Any(char.IsDigit) && cleaned.Any(char.IsLetter))
                {
                    _logger.LogInformation("Detected plate text: {PlateText}", cleaned);
                    return cleaned.ToUpper();
                }
            }

            // If no plate-like pattern found, return the shortest reasonable line
            var candidate = allLines
                .Select(l => new string(l.Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-').ToArray()).Trim())
                .Where(l => l.Length >= 3 && l.Length <= 12)
                .OrderBy(l => l.Length)
                .FirstOrDefault();

            if (candidate != null)
            {
                _logger.LogInformation("Best candidate plate text: {PlateText}", candidate);
                return candidate.ToUpper();
            }

            _logger.LogWarning("No plate text found in OCR results. Lines found: {Lines}", string.Join(" | ", allLines));
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing OCR response");
            return null;
        }
    }
}
