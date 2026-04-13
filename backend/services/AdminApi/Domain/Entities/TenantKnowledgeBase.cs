using Pgvector;

namespace AdminApi.Domain.Entities;

/// <summary>
/// EF Core entity — persisted in PostgreSQL `TenantKnowledgeBases` table.
/// Stores pgvector embedding vectors for RAG implementations.
/// </summary>
public sealed class TenantKnowledgeBase
{
    public Guid Id { get; init; } = Guid.NewGuid();
    
    /// <summary>
    /// The profile this knowledge corresponds to.
    /// </summary>
    public Guid ProfileId { get; init; }
    
    /// <summary>
    /// The raw text snippet from the document.
    /// </summary>
    public required string TextChunk { get; set; }
    
    /// <summary>
    /// The embedding vector. Dimension size matches the embedding model (e.g., 384 for all-MiniLM-L6-v2, 1024 for MTEB, 1536 for OpenAI).
    /// Let's assume 384 for a standard local embedding model for now. 
    /// This should be configured in DbContext.
    /// </summary>
    public Vector? Embedding { get; set; }

    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
