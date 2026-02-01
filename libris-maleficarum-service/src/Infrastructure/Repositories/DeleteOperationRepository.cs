namespace LibrisMaleficarum.Infrastructure.Repositories;

using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.Interfaces.Repositories;
using LibrisMaleficarum.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Cosmos DB repository implementation for DeleteOperation entity.
/// </summary>
public class DeleteOperationRepository : IDeleteOperationRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteOperationRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public DeleteOperationRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public async Task<DeleteOperation> CreateAsync(DeleteOperation operation, CancellationToken cancellationToken = default)
    {
        if (operation == null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        _context.DeleteOperations.Add(operation);
        await _context.SaveChangesAsync(cancellationToken);
        return operation;
    }

    /// <inheritdoc/>
    public async Task<DeleteOperation?> GetByIdAsync(Guid worldId, Guid operationId, CancellationToken cancellationToken = default)
    {
        return await _context.DeleteOperations
            .WithPartitionKey(worldId)
            .FirstOrDefaultAsync(o => o.Id == operationId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<DeleteOperation> UpdateAsync(DeleteOperation operation, CancellationToken cancellationToken = default)
    {
        if (operation == null)
        {
            throw new ArgumentNullException(nameof(operation));
        }

        _context.DeleteOperations.Update(operation);
        await _context.SaveChangesAsync(cancellationToken);
        return operation;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DeleteOperation>> GetRecentByWorldAsync(Guid worldId, int limit = 20, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddHours(-24);
        return await _context.DeleteOperations
            .WithPartitionKey(worldId)
            .Where(o => o.CreatedAt >= cutoff)
            .OrderByDescending(o => o.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> CountActiveByUserAsync(Guid worldId, string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        return await _context.DeleteOperations
            .WithPartitionKey(worldId)
            .Where(o => o.CreatedBy == userId &&
                       (o.Status == DeleteOperationStatus.Pending || o.Status == DeleteOperationStatus.InProgress))
            .CountAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DeleteOperation>> GetPendingOperationsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DeleteOperations
            .Where(o => o.Status == DeleteOperationStatus.Pending)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DeleteOperation>> GetInProgressOperationsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DeleteOperations
            .Where(o => o.Status == DeleteOperationStatus.InProgress)
            .OrderBy(o => o.StartedAt)
            .ToListAsync(cancellationToken);
    }
}
