using System.Linq.Expressions;

namespace Trading.Domain.Contracts;

public interface IRepository<TEntity> where TEntity : class
{
    Task<TEntity?> FindAsync(params object[] ids);

    Task<IEnumerable<TEntity>> GetAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        params Expression<Func<TEntity, object>>[] includeProperties);

    Task<TEntity?> GetSingleOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        params Expression<Func<TEntity, object>>[] includeProperties);

    Task<TEntity> CreateAsync(TEntity entity);

    void UpdatePartially(TEntity entity, params string[] properties);

    void Update(TEntity entity);

    void UpdateRange(IEnumerable<TEntity> entities);

    void Remove(params TEntity[] entities);
}