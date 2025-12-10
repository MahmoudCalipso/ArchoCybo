using ArchoCybo.Application.DTOs;
using ArchoCybo.Application.Interfaces.IServices;
using ArchoCybo.Application.Interfaces;
using ArchoCybo.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArchoCybo.Application.Services;

public class QueryService : IQueryService
{
    private readonly IUnitOfWork _uow;

    public QueryService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Guid> CreateCustomQueryAsync(CreateCustomQueryDto dto)
    {
        var q = new CustomQuery { Name = dto.Name, Sql = dto.Sql, ProjectId = dto.ProjectId };
        await _uow.Repository<CustomQuery>().AddAsync(q);
        await _uow.SaveChangesAsync();
        return q.Id;
    }

    public async Task<IEnumerable<CustomQueryDto>> GetCustomQueriesAsync(Guid projectId)
    {
        var repo = _uow.Repository<CustomQuery>();
        var list = await repo.Query().Where(q => q.ProjectId == projectId).ToListAsync();
        return list.Select(q => new CustomQueryDto(q.Id, q.Name, q.Sql, q.ProjectId, q.CreatedAt));
    }

    public async Task UpdateCustomQueryAsync(UpdateCustomQueryDto dto)
    {
        var repo = _uow.Repository<CustomQuery>();
        var q = await repo.GetByIdAsync(dto.Id);
        if (q == null) throw new Exception("CustomQuery not found");
        q.Name = dto.Name;
        q.Sql = dto.Sql;
        repo.Update(q);
        await _uow.SaveChangesAsync();
    }

    public async Task DeleteCustomQueryAsync(Guid id)
    {
        var repo = _uow.Repository<CustomQuery>();
        var q = await repo.GetByIdAsync(id);
        if (q == null) throw new Exception("CustomQuery not found");
        repo.Remove(q);
        await _uow.SaveChangesAsync();
    }
}
