namespace Orchestrator.Audio;

/// <summary>
/// Splits a continuous audio byte stream into fixed-size, non-overlapping chunks
/// suitable for sending to the Whisper ASR HTTP endpoint.
///
/// Design: Memory-efficient — uses ArrayPool or spans where available.
/// Fixed chunk size avoids oversized HTTP payloads that could increase ASR latency.
/// </summary>
public static class AudioChunker
{
    private const int DefaultChunkSizeBytes = 32 * 1024; // 32 KB

    /// <summary>
    /// Splits <paramref name="audioData"/> into sequential chunks of
    /// <paramref name="chunkSizeBytes"/> bytes.
    /// The final chunk may be smaller than <paramref name="chunkSizeBytes"/>.
    /// </summary>
    /// <param name="audioData">Raw PCM/WebM audio bytes from the WebSocket client.</param>
    /// <param name="chunkSizeBytes">Maximum bytes per chunk. Defaults to 32 KB.</param>
    /// <returns>Lazy enumerable of memory segments — no copy until iterated.</returns>
    public static IEnumerable<ReadOnlyMemory<byte>> Split(
        ReadOnlyMemory<byte> audioData,
        int chunkSizeBytes = DefaultChunkSizeBytes)
    {
        if (chunkSizeBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(chunkSizeBytes), "Must be > 0");

        if (audioData.IsEmpty)
            yield break;

        var offset = 0;
        while (offset < audioData.Length)
        {
            var length = Math.Min(chunkSizeBytes, audioData.Length - offset);
            yield return audioData.Slice(offset, length);
            offset += length;
        }
    }
}
