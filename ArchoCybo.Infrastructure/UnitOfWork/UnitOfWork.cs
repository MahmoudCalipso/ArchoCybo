using ArchoCybo.Application.Interfaces;
using ArchoCybo.Infrastructure.Repositories;
using ArchoCybo.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace ArchoCybo.Infrastructure.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly DbContext _context;
    private readonly Dictionary<(Type, Type), object> _repositories = new();

    public UnitOfWork(DbContext context)
    {
        _context = context;
    }

    public IRepository<T, BaseFilter> Repository<T>() where T : class
    {
        return Repository<T, BaseFilter>();
    }

    public IRepository<T, TFilter> Repository<T, TFilter>() 
        where T : class 
        where TFilter : BaseFilter
    {
        var key = (typeof(T), typeof(TFilter));

        if (!_repositories.ContainsKey(key))
        {
            var repoType = typeof(EfRepository<T, TFilter>);
            var repo = Activator.CreateInstance(repoType, _context);
            _repositories[key] = repo!;
        }

        return (IRepository<T, TFilter>)_repositories[key]!;
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
