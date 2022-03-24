namespace Universalis.Mogboard;

public interface IMogboardTable<out TEntity, in TKey> where TEntity : class
{
    public TEntity? Get(TKey id);
}