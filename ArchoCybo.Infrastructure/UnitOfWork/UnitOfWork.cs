using ArchoCybo.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ArchoCybo.Infrastructure.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly DbContext _context;
    private readonly Dictionary<Type, object> _repositories = new();

    public UnitOfWork(DbContext context)
    {
        _context = context;
    }

    public IRepository<T> Repository<T>() where T : class
    {
        var type = typeof(T);
        if (!_repositories.ContainsKey(type))
        {
            var repoType = typeof(ArchoCybo.Infrastructure.Repositories.Repository<>).MakeGenericType(type);
            var repo = Activator.CreateInstance(repoType, _context);
            _repositories[type] = repo!;
        }

        return (IRepository<T>)_repositories[type]!;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
