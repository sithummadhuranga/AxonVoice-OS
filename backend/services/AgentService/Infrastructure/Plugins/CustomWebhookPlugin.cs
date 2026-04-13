using AgentService.Domain.Interfaces;
using Microsoft.SemanticKernel;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AgentService.Infrastructure.Plugins;

/// <summary>
/// Generic webhook plugin that forwards LLM-extracted parameters to any external API.
/// Mounted when <see cref="Axon.Contracts.Domain.Enums.Purposes.CustomWebhook"/> is active.
///
/// Security: Auth token is decrypted from AES-256 storage in-memory only.
/// Never logged, never persisted in plaintext.
/// </summary>
public sealed class CustomWebhookPlugin : IAxonPlugin
{
    private readonly ILogger<CustomWebhookPlugin> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _webhookUrl;
    private readonly string _authToken;

    public CustomWebhookPlugin(
        ILogger<CustomWebhookPlugin> logger,
        IHttpClientFactory httpClientFactory,
        string webhookUrl,
        string authToken)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _webhookUrl = webhookUrl ?? throw new ArgumentNullException(nameof(webhookUrl));
        _authToken = authToken ?? throw new ArgumentNullException(nameof(authToken));
    }

    public KernelPlugin BuildPlugin() =>
        KernelPluginFactory.CreateFromObject(this, pluginName: "CustomWebhook");

    /// <summary>
    /// Sends the LLM's extracted action payload to the tenant's custom external API via HTTP POST.
    /// </summary>
    [KernelFunction("invoke_custom_action")]
    [System.ComponentModel.Description(
        "Sends structured data to the tenant's custom external API. " +
        "Use when the user's intent requires an action that isn't handled by built-in plugins.")]
    public async Task<string> InvokeCustomActionAsync(
        [System.ComponentModel.Description(
            "JSON string representing the action payload to send to the external API")]
        string payloadJson,
        CancellationToken cancellationToken = default)
    {
        // Validate JSON before sending to prevent injection
        try
        {
            JsonDocument.Parse(payloadJson);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid JSON payload rejected for webhook call");
            return "Error: The action data was invalid. Please try again.";
        }

        try
        {
            var client = _httpClientFactory.CreateClient("WebhookClient");
            using var request = new HttpRequestMessage(HttpMethod.Post, _webhookUrl);

            // Auth token injected from decrypted AES-256 store — never hardcoded
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);
            request.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");

            _logger.LogInformation("Invoking custom webhook at {Url}", _webhookUrl);

            var response = await client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation(
                "Custom webhook responded: Status={Status}", (int)response.StatusCode);

            return $"Action completed successfully. Response: {body}";
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Custom webhook call failed for URL {Url}", _webhookUrl);
            return "I'm sorry, I couldn't complete that action right now. Please try again later.";
        }
    }
}
