namespace ArchoCybo.Domain.Entities.CodeGeneration;

using ArchoCybo.Domain.Common;

/// <summary>
/// Minimal Entity metadata used by Query Builder
/// </summary>
public class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string ActualTableName => string.IsNullOrEmpty(TableName) ? Name : TableName;

    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    // Strong collection of Fields
    private readonly List<Field> _fields = new();
    public IReadOnlyList<Field> Fields => _fields.AsReadOnly();

    public ICollection<Relation> Relations { get; set; } = new List<Relation>();

    // Add field helper
    public void AddField(Field field)
    {
        if (field == null)
            throw new ArgumentNullException(nameof(field));

        field.Entity = this;
        field.EntityId = this.Id;
        _fields.Add(field);
    }

    // Remove field helper
    public void RemoveField(Field field)
    {
        if (_fields.Contains(field))
        {
            _fields.Remove(field);
        }
    }
}
