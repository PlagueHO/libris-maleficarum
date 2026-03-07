namespace LibrisMaleficarum.Domain.Interfaces.Services;

using LibrisMaleficarum.Domain.Models;

/// <summary>
/// Abstraction for index synchronization operations with Azure AI Search.
/// </summary>
public interface ISearchIndexService
{
    /// <summary>
    /// Indexes a single document in the search index.
    /// </summary>
    /// <param name="document">The document to index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task IndexDocumentAsync(SearchIndexDocument document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Indexes a batch of documents in the search index.
    /// </summary>
    /// <param name="documents">The documents to index.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task IndexDocumentsBatchAsync(IEnumerable<SearchIndexDocument> documents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a document from the search index.
    /// </summary>
    /// <param name="documentId">The document identifier to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveDocumentAsync(string documentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a batch of documents from the search index.
    /// </summary>
    /// <param name="documentIds">The document identifiers to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveDocumentsBatchAsync(IEnumerable<string> documentIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensures the search index exists with the correct schema.
    /// Creates the index if it doesn't exist.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task EnsureIndexExistsAsync(CancellationToken cancellationToken = default);
}
