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

    public async Task<Guid> CreateCustomQueryAsync(CreateCustomQueryDto dto, Guid userId)
    {
        var q = new CustomQuery { Name = dto.Name, Sql = dto.Sql, ResultSchema = dto.ResultSchema, ProjectId = dto.ProjectId, UserId = userId };
        var result = await _uow.Repository<CustomQuery>().AddAsync(q);
        if (!result.Success) throw new Exception(result.Message);
        return q.Id;
    }

    public async Task<IEnumerable<CustomQueryDto>> GetCustomQueriesAsync(Guid projectId)
    {
        var repo = _uow.Repository<CustomQuery>();
        var list = await repo.Query().Where(q => q.ProjectId == projectId).ToListAsync();
        return list.Select(q => new CustomQueryDto(q.Id, q.Name, q.Sql, q.ResultSchema, q.ProjectId, q.CreatedAt));
    }

    public async Task UpdateCustomQueryAsync(UpdateCustomQueryDto dto)
    {
        var repo = _uow.Repository<CustomQuery>();
        var result = await repo.GetByIdAsync(dto.Id);
        if (!result.Success || result.Data == null) throw new Exception("CustomQuery not found");
        var q = result.Data;
        q.Name = dto.Name;
        q.Sql = dto.Sql;
        q.ResultSchema = dto.ResultSchema;
        await repo.UpdateAsync(q);
    }

    public async Task DeleteCustomQueryAsync(Guid id)
    {
        var repo = _uow.Repository<CustomQuery>();
        var result = await repo.DeleteAsync(id);
        if (!result.Success) throw new Exception("CustomQuery not found");
    }
}
