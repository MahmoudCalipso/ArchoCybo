namespace ArchoCybo.Domain.Entities.CodeGeneration;

public class Relation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SourceEntityId { get; set; }
    public Entity SourceEntity { get; set; } = null!;

    public Guid TargetEntityId { get; set; }
    public Entity TargetEntity { get; set; } = null!;

    public string Type { get; set; } = "OneToMany"; // OneToOne, OneToMany, ManyToMany
    public string ForeignKey { get; set; } = string.Empty;
    public string NavigationProperty { get; set; } = string.Empty;
    public string? JoinTable { get; set; } // For ManyToMany
}
