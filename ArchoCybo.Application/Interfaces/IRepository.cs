namespace ArchoCybo.Application.Interfaces;

using System.Linq;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> ListAsync();
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);

    // Querying support - return an IQueryable for composing queries
    IQueryable<T> Query();

    // Returns all entities without tracking (useful for read-only operations)
    Task<IEnumerable<T>> AllAsync();
}
