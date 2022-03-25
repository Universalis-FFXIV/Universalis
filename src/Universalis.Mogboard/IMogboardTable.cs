namespace Universalis.Mogboard;

public interface IMogboardTable<TEntity, in TKey> where TEntity : class
{
    Task<TEntity?> Get(TKey id, CancellationToken cancellationToken = default);
}