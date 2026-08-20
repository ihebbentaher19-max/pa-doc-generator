using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using PADocGenerator.Api.Common;
using PADocGenerator.Api.Models.Dtos;
using PADocGenerator.Api.Services.Interfaces;

namespace PADocGenerator.Api.Services;

/// <summary>
/// Client des API documentées Power Platform. La première API retourne les
/// environnements et les flux accessibles ; la définition est ensuite lue dans
/// Dataverse, où les flux cloud sont stockés dans la table <c>workflow</c>.
/// </summary>
public sealed class PowerPlatformFlowService : IPowerPlatformFlowService
{
    private const string ApiVersion = "2024-10-01";
    private const string PowerPlatformBaseUrl = "https://api.powerplatform.com/";
    private readonly HttpClient _httpClient;
    private readonly ILogger<PowerPlatformFlowService> _logger;

    public PowerPlatformFlowService(HttpClient httpClient, ILogger<PowerPlatformFlowService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<PowerPlatformEnvironmentDto>> GetEnvironmentsAsync(
        string powerPlatformAccessToken, CancellationToken ct)
    {
        var values = await GetPowerPlatformValuesAsync(
            new Uri($"{PowerPlatformBaseUrl}environmentmanagement/environments?api-version={ApiVersion}"),
            powerPlatformAccessToken, ct);

        return values
            .Select(element => new PowerPlatformEnvironmentDto(
                ReadString(element, "id") ?? string.Empty,
                ReadString(element, "displayName") ?? ReadString(element, "name") ?? "Environnement sans nom",
                ReadString(element, "type"),
                ReadString(element, "state"),
                ReadString(element, "url"),
                ReadString(element, "tenantId")))
            .Where(environment => !string.IsNullOrWhiteSpace(environment.Id))
            .OrderBy(environment => environment.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<PowerPlatformFlowDto>> GetFlowsAsync(
        string powerPlatformAccessToken, string environmentId, CancellationToken ct)
    {
        EnsureId(environmentId, nameof(environmentId));
        var uri = new Uri($"{PowerPlatformBaseUrl}powerautomate/environments/{Uri.EscapeDataString(environmentId)}/cloudFlows?api-version={ApiVersion}");
        var values = await GetPowerPlatformValuesAsync(uri, powerPlatformAccessToken, ct);

        return values
            .Select(MapFlow)
            .Where(flow => !string.IsNullOrWhiteSpace(flow.WorkflowId))
            .OrderBy(flow => flow.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public async Task<PowerPlatformFlowDefinitionDto> GetFlowDefinitionAsync(
        string powerPlatformAccessToken,
        string dataverseAccessToken,
        string environmentId,
        string workflowId,
        CancellationToken ct)
    {
        EnsureId(environmentId, nameof(environmentId));
        if (!Guid.TryParse(workflowId, out var flowGuid))
            throw new BusinessException("L'identifiant du flux Power Automate est invalide.");

        // La liste venant du serveur Power Platform est la source de vérité pour
        // l'environnement et évite toute URL Dataverse contrôlée par le client.
        var environments = await GetEnvironmentsAsync(powerPlatformAccessToken, ct);
        var environment = environments.FirstOrDefault(item =>
            string.Equals(item.Id, environmentId, StringComparison.OrdinalIgnoreCase));
        if (environment is null)
            throw new BusinessException("Cet environnement Power Platform n'est pas accessible avec ce compte.");

        var flows = await GetFlowsAsync(powerPlatformAccessToken, environmentId, ct);
        var selectedFlow = flows.FirstOrDefault(item =>
            string.Equals(item.WorkflowId, workflowId, StringComparison.OrdinalIgnoreCase));
        if (selectedFlow is null)
            throw new BusinessException("Ce flux n'est pas accessible dans l'environnement sélectionné.");

        if (!Uri.TryCreate(environment.DataverseUrl, UriKind.Absolute, out var dataverseBaseUrl) ||
            dataverseBaseUrl.Scheme != Uri.UriSchemeHttps ||
            string.IsNullOrWhiteSpace(dataverseBaseUrl.Host))
        {
            throw new BusinessException("Cet environnement ne possède pas de base Dataverse utilisable pour lire la définition du flux.");
        }

        var endpoint = new Uri(
            dataverseBaseUrl,
            $"api/data/v9.2/workflows({flowGuid:D})?$select=clientdata,name,workflowid");
        using var definitionDocument = await GetJsonAsync(endpoint, dataverseAccessToken, ct);
        var clientData = ReadString(definitionDocument!.RootElement, "clientdata");
        if (string.IsNullOrWhiteSpace(clientData))
            throw new BusinessException("La définition de ce flux n'est pas disponible pour votre compte.");

        var definitionJson = NormalizeDefinition(clientData, selectedFlow.DisplayName);
        return new PowerPlatformFlowDefinitionDto(
            ReadString(definitionDocument.RootElement, "name") ?? selectedFlow.DisplayName,
            definitionJson,
            environment.Id,
            environment.TenantId);
    }

    private async Task<JsonDocument?> GetJsonAsync(
        Uri uri, string accessToken, CancellationToken ct, bool allowNoContent = false)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new BusinessException("La session Microsoft a expiré. Reconnectez-vous à Microsoft 365 puis réessayez.");

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", StripBearerPrefix(accessToken));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.TryAddWithoutValidation("OData-Version", "4.0");
        request.Headers.TryAddWithoutValidation("OData-MaxVersion", "4.0");

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        if (allowNoContent && response.StatusCode == HttpStatusCode.NoContent)
            return null;

        if (!response.IsSuccessStatusCode)
        {
            var providerMessage = await ReadProviderErrorAsync(response, ct);
            _logger.LogWarning("Power Platform a répondu {StatusCode} pour {Endpoint}: {ProviderMessage}",
                (int)response.StatusCode, uri.AbsolutePath, providerMessage);
            throw new BusinessException(ToUserMessage(response.StatusCode, providerMessage));
        }

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
    }

    /// <summary>
    /// L'API des environnements retourne un <c>@odata.nextLink</c> quand la
    /// liste est paginée. On le suit pour ne pas cacher des environnements ou
    /// des flux à l'utilisateur, tout en n'acceptant que le domaine Microsoft
    /// attendu et un nombre fini de pages.
    /// </summary>
    private async Task<List<JsonElement>> GetPowerPlatformValuesAsync(
        Uri initialUri, string accessToken, CancellationToken ct)
    {
        var values = new List<JsonElement>();
        Uri? currentUri = initialUri;

        for (var page = 0; currentUri is not null && page < 20; page++)
        {
            using var document = await GetJsonAsync(currentUri, accessToken, ct, allowNoContent: true);
            if (document is null) break;

            values.AddRange(GetValues(document.RootElement).Select(value => value.Clone()));
            currentUri = GetSafePowerPlatformNextLink(document.RootElement);
        }

        return values;
    }

    private static Uri? GetSafePowerPlatformNextLink(JsonElement root)
    {
        var nextLink = ReadString(root, "@odata.nextLink");
        if (string.IsNullOrWhiteSpace(nextLink) || !Uri.TryCreate(nextLink, UriKind.Absolute, out var uri))
            return null;

        return uri.Scheme == Uri.UriSchemeHttps &&
               string.Equals(uri.Host, "api.powerplatform.com", StringComparison.OrdinalIgnoreCase)
            ? uri
            : null;
    }

    private static IEnumerable<JsonElement> GetValues(JsonElement root) =>
        root.TryGetProperty("value", out var values) && values.ValueKind == JsonValueKind.Array
            ? values.EnumerateArray()
            : Enumerable.Empty<JsonElement>();

    private static PowerPlatformFlowDto MapFlow(JsonElement element)
    {
        var properties = GetProperties(element);
        var modified = ReadDateTime(element, "modifiedOn") ?? ReadDateTime(properties, "modifiedOn");
        return new PowerPlatformFlowDto(
            ReadString(element, "workflowId") ?? ReadString(properties, "workflowId") ??
            ReadString(element, "id") ?? string.Empty,
            ReadString(element, "displayName") ?? ReadString(properties, "displayName") ??
            ReadString(element, "name") ?? ReadString(properties, "name") ?? "Flux sans nom",
            ReadString(element, "state") ?? ReadString(properties, "state"),
            modified,
            ReadBoolean(element, "isManaged") ?? ReadBoolean(properties, "isManaged") ?? false);
    }

    private static JsonElement GetProperties(JsonElement element) =>
        element.TryGetProperty("properties", out var properties) && properties.ValueKind == JsonValueKind.Object
            ? properties
            : element;

    private static string? ReadString(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) &&
        value.ValueKind is JsonValueKind.String or JsonValueKind.Number
            ? value.ToString()
            : null;

    private static bool? ReadBoolean(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) &&
        value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : null;

    private static DateTime? ReadDateTime(JsonElement element, string name) =>
        DateTime.TryParse(ReadString(element, name), CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var result)
            ? result : null;

    private static string NormalizeDefinition(string clientData, string displayName)
    {
        try
        {
            using var document = JsonDocument.Parse(clientData);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw new JsonException();

            // clientdata est normalement l'enveloppe exportée. Si Dataverse ne
            // retourne que la définition, on la remet dans une enveloppe que le
            // parser existant sait déjà traiter.
            if (root.TryGetProperty("properties", out var properties) &&
                properties.TryGetProperty("definition", out _))
                return root.GetRawText();

            if (root.TryGetProperty("definition", out _) ||
                root.TryGetProperty("actions", out _) || root.TryGetProperty("triggers", out _))
            {
                return JsonSerializer.Serialize(new { name = displayName, definition = root });
            }
        }
        catch (JsonException)
        {
            // Le message fonctionnel est traité ci-dessous, sans exposer le contenu du flux.
        }

        throw new BusinessException("Power Platform a retourné une définition de flux non exploitable.");
    }

    private static string StripBearerPrefix(string token) =>
        token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? token[7..].Trim() : token.Trim();

    private static void EnsureId(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 200 || value.Contains('/') || value.Contains('\\'))
            throw new ArgumentException("Identifiant Power Platform invalide.", parameterName);
    }

    private static async Task<string?> ReadProviderErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            return document.RootElement.TryGetProperty("error", out var error)
                ? ReadString(error, "message")
                : ReadString(document.RootElement, "message");
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string ToUserMessage(HttpStatusCode statusCode, string? providerMessage) => statusCode switch
    {
        HttpStatusCode.Unauthorized => "La session Microsoft a expiré. Reconnectez-vous à Microsoft 365 puis réessayez.",
        HttpStatusCode.Forbidden => "Votre compte Microsoft n'a pas les autorisations nécessaires pour cette ressource Power Platform.",
        HttpStatusCode.NotFound => "La ressource demandée est introuvable ou cet environnement n'a pas de base Dataverse.",
        _ => string.IsNullOrWhiteSpace(providerMessage)
            ? "Power Platform n'a pas pu traiter la demande. Réessayez dans quelques instants."
            : $"Power Platform a refusé la demande : {providerMessage}"
    };
}
