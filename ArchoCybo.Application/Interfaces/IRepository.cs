using ArchoCybo.Domain.Common;
using ArchoCybo.Application.Models.Common;

namespace ArchoCybo.Application.Interfaces;

public interface IRepository<T, TFilter>
    where T : class
    where TFilter : BaseFilter
{
    // ---------- READ ----------
    Task<RepositoryResult<T>> GetByIdAsync(Guid id);

    Task<RepositoryResult<IEnumerable<T>>> GetAllAsync();

    Task<RepositoryResult<IEnumerable<T>>> GetAllAsNoTrackingAsync();

    Task<RepositoryResult<PaginatedResult<T>>> GetPagedAsync(
        TFilter filter,
        PaginationRequest pagination);

    IQueryable<T> Query();

    // ---------- WRITE ----------
    Task<RepositoryResult<T>> AddAsync(T entity);

    Task<RepositoryResult> UpdateAsync(T entity);

    Task<RepositoryResult> DeleteAsync(Guid id);
}
