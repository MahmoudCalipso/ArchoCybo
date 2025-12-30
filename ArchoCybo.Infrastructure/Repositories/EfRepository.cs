using ArchoCybo.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using ArchoCybo.Domain.Common;
using ArchoCybo.Application.Models.Common;
using ArchoCybo.Application.Common;

namespace ArchoCybo.Infrastructure.Repositories;

public class EfRepository<T, TFilter> : IRepository<T, TFilter>
    where T : class
    where TFilter : BaseFilter
{
    protected readonly DbContext _context;
    protected readonly DbSet<T> _dbSet;

    public EfRepository(DbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<RepositoryResult<T>> GetByIdAsync(Guid id)
    {
        var entity = await _dbSet.FindAsync(id);

        if (entity == null)
            return RepositoryResult<T>.Fail(
                RepositoryMessageBuilder.NotFound(typeof(T).Name));

        return RepositoryResult<T>.Ok(
            entity,
            RepositoryMessageBuilder.Success(
                RepositoryAction.Get,
                typeof(T).Name));
    }

    public async Task<RepositoryResult<IEnumerable<T>>> GetAllAsync()
    {
        var list = await _dbSet.ToListAsync();

        return RepositoryResult<IEnumerable<T>>.Ok(
            list,
            RepositoryMessageBuilder.Success(
                RepositoryAction.Get,
                typeof(T).Name));
    }

    public async Task<RepositoryResult<IEnumerable<T>>> GetAllAsNoTrackingAsync()
    {
        var list = await _dbSet.AsNoTracking().ToListAsync();

        return RepositoryResult<IEnumerable<T>>.Ok(
            list,
            RepositoryMessageBuilder.Success(
                RepositoryAction.Get,
                typeof(T).Name));
    }

    public async Task<RepositoryResult<T>> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();

        return RepositoryResult<T>.Ok(
            entity,
            RepositoryMessageBuilder.Success(
                RepositoryAction.Create,
                typeof(T).Name));
    }

    public async Task<RepositoryResult> UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();

        return RepositoryResult.Ok(
            RepositoryMessageBuilder.Success(
                RepositoryAction.Update,
                typeof(T).Name));
    }

    public async Task<RepositoryResult> DeleteAsync(Guid id)
    {
        var entity = await _dbSet.FindAsync(id);

        if (entity == null)
            return RepositoryResult.Fail(
                RepositoryMessageBuilder.NotFound(typeof(T).Name));

        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();

        return RepositoryResult.Ok(
            RepositoryMessageBuilder.Success(
                RepositoryAction.Delete,
                typeof(T).Name));
    }

    public IQueryable<T> Query()
        => _dbSet.AsQueryable();

    public async Task<RepositoryResult<PaginatedResult<T>>> GetPagedAsync(
        TFilter filter,
        PaginationRequest pagination)
    {
        IQueryable<T> query = _dbSet.AsQueryable();

        // Filtering logic can be overridden in derived repositories
        // Example: if (filter is SomeSpecificFilter f) { ... }

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var result = new PaginatedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize
        };

        return RepositoryResult<PaginatedResult<T>>.Ok(
            result,
            RepositoryMessageBuilder.Success(
                RepositoryAction.Get,
                typeof(T).Name));
    }
}
