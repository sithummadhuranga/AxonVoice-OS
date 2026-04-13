namespace Axon.Contracts.Domain.Enums;

/// <summary>
/// ISO 639-1 language codes supported by AxonVoice's bilingual pipeline.
/// Detected from Whisper's language token and stored in Redis session state.
/// </summary>
public enum Language
{
    /// <summary>Unknown — language detection has not yet completed.</summary>
    Unknown = 0,

    /// <summary>English — Whisper token: &lt;|en|&gt;</summary>
    English = 1,

    /// <summary>Sinhala — Whisper token: &lt;|si|&gt;</summary>
    Sinhala = 2
}
