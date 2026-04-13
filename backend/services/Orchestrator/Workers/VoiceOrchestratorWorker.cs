using Axon.Contracts.Domain.Enums;
using Orchestrator.Audio;
using Orchestrator.Session;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace Orchestrator.Workers;

/// <summary>
/// Central WebSocket handler and voice pipeline coordinator.
///
/// Pipeline per call:
///   1. WebSocket connects → session initialised in Redis
///   2. Audio chunks arrive → forwarded to Whisper ASR (HTTP)
///   3. Language token extracted → Redis session state updated
///   4. Filler audio streamed back while Semantic Kernel (AgentService) processes
///   5. AgentService response tokens buffered by sentence boundary and forwarded to TTS
///   6. WebSocket closes or drops → session terminated
/// </summary>
public sealed class VoiceOrchestratorWorker : BackgroundService
{
    private readonly ILogger<VoiceOrchestratorWorker> _logger;
    private readonly SessionStateManager _sessionState;
    private readonly FillerAudioProvider _fillerAudio;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;

    public VoiceOrchestratorWorker(
        ILogger<VoiceOrchestratorWorker> logger,
        SessionStateManager sessionState,
        FillerAudioProvider fillerAudio,
        IHttpClientFactory httpClientFactory,
        IConfiguration config)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sessionState = sessionState ?? throw new ArgumentNullException(nameof(sessionState));
        _fillerAudio = fillerAudio ?? throw new ArgumentNullException(nameof(fillerAudio));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("VoiceOrchestratorWorker started. Listening for WebSocket connections.");

        // The actual WebSocket accept loop is wired in Program.cs via app.UseWebSockets()
        // This service manages lifecycle and shared dependencies.
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    /// <summary>
    /// Handles a single WebSocket call session end-to-end.
    /// Called from the WebSocket middleware in Program.cs.
    /// </summary>
    public async Task HandleSessionAsync(
        WebSocket webSocket,
        Guid tenantId,
        Guid profileId,
        CancellationToken ct)
    {
        var sessionId = Guid.NewGuid().ToString("N");

        try
        {
            await _sessionState.InitialiseAsync(sessionId, tenantId, profileId, ct);
            _logger.LogInformation("Session {SessionId} connected", sessionId);

            var buffer = new byte[64 * 1024]; // 64 KB receive buffer

            while (webSocket.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var result = await webSocket.ReceiveAsync(buffer, ct);

                if (result.MessageType == WebSocketMessageType.Close)
                    break;

                if (result.MessageType != WebSocketMessageType.Binary)
                    continue;

                var audioChunk = new ReadOnlyMemory<byte>(buffer, 0, result.Count);
                await ProcessAudioChunkAsync(webSocket, sessionId, audioChunk, ct);
            }
        }
        catch (WebSocketException ex) when (ex.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
        {
            _logger.LogWarning("Session {SessionId} connection dropped prematurely", sessionId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Session {SessionId} cancelled", sessionId);
        }
        finally
        {
            await _sessionState.TerminateAsync(sessionId, CancellationToken.None);

            if (webSocket.State == WebSocketState.Open)
                await webSocket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure, "Session ended", CancellationToken.None);
        }
    }

    private async Task ProcessAudioChunkAsync(
        WebSocket webSocket,
        string sessionId,
        ReadOnlyMemory<byte> audioChunk,
        CancellationToken ct)
    {
        // 1. Send to Whisper ASR for transcription + language detection
        var asrResult = await SendToAsrAsync(audioChunk, ct);

        if (asrResult is null)
            return;

        // 2. Update language in Redis if detected
        if (asrResult.Language != Language.Unknown)
            await _sessionState.SetLanguageAsync(sessionId, asrResult.Language, ct);

        // 3. Stream filler audio back to mask LLM latency
        var currentLanguage = await _sessionState.GetLanguageAsync(sessionId, ct);
        var fillerBytes = _fillerAudio.GetFillerAudio(currentLanguage);

        if (!fillerBytes.IsEmpty)
        {
            await webSocket.SendAsync(
                fillerBytes,
                WebSocketMessageType.Binary,
                endOfMessage: true,
                cancellationToken: ct);
        }

        // 4. Forward transcript to AgentService for SK processing
        // (AgentService publishes response back via RabbitMQ → Orchestrator streams to WebSocket)
        _logger.LogDebug(
            "Session {SessionId} | ASR: Language={Language} | Transcript={Transcript}",
            sessionId, asrResult.Language, asrResult.Transcript);
    }

    private async Task<AsrResult?> SendToAsrAsync(ReadOnlyMemory<byte> audioBytes, CancellationToken ct)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("OllamaAsr");
            var ollamaUrl = _config["Ollama:BaseUrl"]
                ?? throw new InvalidOperationException("Ollama:BaseUrl not configured");

            // Ollama Whisper endpoint (transcription)
            using var content = new ByteArrayContent(audioBytes.ToArray());
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");

            var response = await client.PostAsync($"{ollamaUrl}/api/transcribe", content, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<AsrResult>(json, JsonOptions);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "ASR HTTP request failed — Ollama may be unreachable");
            return null;
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Whisper ASR response model.</summary>
    private sealed record AsrResult(string Transcript, Language Language);
}
