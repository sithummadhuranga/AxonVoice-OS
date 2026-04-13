using System.Text.RegularExpressions;
using System.Text;

namespace AgentService.Application.Pipeline;

/// <summary>
/// Buffers streaming LLM token output and fires a callback the moment a complete
/// sentence is detected (by punctuation boundary).
///
/// Pattern: Pipeline / Template Method
///
/// The sentence-boundary approach enables concurrent TTS dispatch:
/// While the LLM generates the remainder of a paragraph, TTS is already
/// speaking the first complete sentence — eliminating the perceived full-response delay.
/// </summary>
public sealed partial class SentenceBoundaryBuffer
{
    private readonly StringBuilder _buffer = new();
    private readonly Func<string, CancellationToken, Task> _onSentenceReady;
    private readonly ILogger<SentenceBoundaryBuffer> _logger;

    // Matches terminal punctuation followed by space or end-of-string
    [GeneratedRegex(@"(?<=[.!?])\s+|(?<=[.!?])$", RegexOptions.Compiled)]
    private static partial Regex SentenceDelimiter();

    public SentenceBoundaryBuffer(
        Func<string, CancellationToken, Task> onSentenceReady,
        ILogger<SentenceBoundaryBuffer> logger)
    {
        _onSentenceReady = onSentenceReady ?? throw new ArgumentNullException(nameof(onSentenceReady));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Append a streaming token from the LLM and dispatch complete sentences.</summary>
    /// <param name="token">A single token or token fragment from the SK streaming response.</param>
    /// <param name="ct">Cancellation for the TTS dispatch call.</param>
    public async Task AppendAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(token))
            return;

        _buffer.Append(token);
        var current = _buffer.ToString();

        // Find boundary positions using the compiled regex
        var matches = SentenceDelimiter().Matches(current);
        if (matches.Count == 0)
            return;

        var lastMatchEnd = 0;
        foreach (Match match in matches)
        {
            var sentence = current[lastMatchEnd..(match.Index + match.Length)].Trim();
            lastMatchEnd = match.Index + match.Length;

            if (!string.IsNullOrWhiteSpace(sentence))
            {
                _logger.LogDebug("Dispatching sentence to TTS: {Sentence}", sentence);
                await _onSentenceReady(sentence, ct);
            }
        }

        // Keep remaining incomplete sentence in buffer
        _buffer.Clear();
        _buffer.Append(current[lastMatchEnd..]);
    }

    /// <summary>Flush any remaining buffer content as a final sentence.</summary>
    public async Task FlushAsync(CancellationToken ct = default)
    {
        var remaining = _buffer.ToString().Trim();
        _buffer.Clear();

        if (!string.IsNullOrWhiteSpace(remaining))
        {
            _logger.LogDebug("Flushing remaining buffer to TTS: {Remaining}", remaining);
            await _onSentenceReady(remaining, ct);
        }
    }
}
