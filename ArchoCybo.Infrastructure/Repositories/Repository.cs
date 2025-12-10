using ArchoCybo.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ArchoCybo.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    private readonly DbContext _context;
    private readonly DbSet<T> _dbSet;

    public Repository(DbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

    public async Task<T?> GetByIdAsync(Guid id) => await _dbSet.FindAsync(id);

    public async Task<IEnumerable<T>> ListAsync() => await _dbSet.ToListAsync();

    public void Remove(T entity) => _dbSet.Remove(entity);

    public void Update(T entity) => _dbSet.Update(entity);

    // New query support
    public IQueryable<T> Query() => _dbSet.AsQueryable();

    public async Task<IEnumerable<T>> AllAsync() => await _dbSet.AsNoTracking().ToListAsync();
}
