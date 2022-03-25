namespace Universalis.Mogboard;

public interface IMogboardTable<TEntity, in TKey> where TEntity : class
{
    public Task<TEntity?> Get(TKey id);
}