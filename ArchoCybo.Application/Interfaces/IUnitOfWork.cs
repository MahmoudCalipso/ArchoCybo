using ArchoCybo.Domain.Common;

namespace ArchoCybo.Application.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    IRepository<T, BaseFilter> Repository<T>() where T : class;
    IRepository<T, TFilter> Repository<T, TFilter>() where T : class where TFilter : BaseFilter;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
