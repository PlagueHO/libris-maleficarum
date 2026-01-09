namespace LibrisMaleficarum.Infrastructure.Extensions;

using LibrisMaleficarum.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Extension methods for conditional Cosmos DB optimizations.
/// </summary>
public static class DbContextExtensions
{
    /// <summary>
    /// Checks if the database provider is Cosmos DB.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <returns>True if using Cosmos DB provider, false otherwise.</returns>
    public static bool IsCosmos(this ApplicationDbContext context)
    {
        return context.Database.ProviderName == "Microsoft.EntityFrameworkCore.Cosmos";
    }
}

/// <summary>
/// Extension methods for conditional partition key application.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Applies partition key optimization for Cosmos DB provider.
    /// For InMemoryDatabase, this is a no-op to allow unit testing.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <param name="source">The queryable to apply partition key to.</param>
    /// <param name="context">The database context.</param>
    /// <param name="partitionKey">The partition key value.</param>
    /// <returns>The queryable with partition key applied if using Cosmos DB.</returns>
    public static IQueryable<TEntity> WithPartitionKeyIfCosmos<TEntity>(
        this IQueryable<TEntity> source,
        ApplicationDbContext context,
        object partitionKey) where TEntity : class
    {
        // Only apply WithPartitionKey for Cosmos DB provider
        if (context.IsCosmos())
        {
            return source.WithPartitionKey(partitionKey);
        }

        // For InMemoryDatabase and other providers, return unchanged
        return source;
    }
}
