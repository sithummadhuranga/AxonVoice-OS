using Axon.Contracts.Domain.Enums;

namespace Orchestrator.Audio;

/// <summary>
/// Provides pre-recorded filler audio bytes by language.
/// Implements the Strategy pattern — audio selection adapts to detected language.
///
/// Filler audio masks LLM inference latency (1.5s–2.5s) by playing a natural
/// "thinking" phrase to the caller while Semantic Kernel generates the response.
/// </summary>
public sealed class FillerAudioProvider
{
    private readonly ILogger<FillerAudioProvider> _logger;
    private readonly IReadOnlyDictionary<Language, byte[]> _audioCache;

    // Configurable paths injected via IConfiguration → set in appsettings / .env
    public FillerAudioProvider(
        IConfiguration config,
        ILogger<FillerAudioProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _audioCache = LoadAudioFiles(config);
    }

    /// <summary>
    /// Returns the PCM/WAV bytes for the filler audio matching the given language.
    /// Falls back to English if the language-specific file is missing.
    /// </summary>
    /// <param name="language">Detected caller language.</param>
    /// <returns>Audio bytes to stream to the caller's WebSocket.</returns>
    public ReadOnlyMemory<byte> GetFillerAudio(Language language)
    {
        if (_audioCache.TryGetValue(language, out var bytes))
            return bytes;

        _logger.LogWarning(
            "No filler audio found for language {Language}, falling back to English", language);

        return _audioCache.TryGetValue(Language.English, out var fallback)
            ? fallback
            : ReadOnlyMemory<byte>.Empty;
    }

    private Dictionary<Language, byte[]> LoadAudioFiles(IConfiguration config)
    {
        var cache = new Dictionary<Language, byte[]>();

        TryLoadFile(config, "Audio:FillerEnPath", Language.English, cache);
        TryLoadFile(config, "Audio:FillerSiPath", Language.Sinhala, cache);

        return cache;
    }

    private void TryLoadFile(
        IConfiguration config,
        string configKey,
        Language language,
        Dictionary<Language, byte[]> cache)
    {
        var path = config[configKey];

        if (string.IsNullOrWhiteSpace(path))
        {
            _logger.LogWarning("Filler audio path not configured for key {Key}", configKey);
            return;
        }

        if (!File.Exists(path))
        {
            _logger.LogWarning("Filler audio file not found at path {Path}", path);
            return;
        }

        try
        {
            cache[language] = File.ReadAllBytes(path);
            _logger.LogInformation("Loaded filler audio for {Language} from {Path}", language, path);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Failed to read filler audio file at {Path}", path);
        }
    }
}
