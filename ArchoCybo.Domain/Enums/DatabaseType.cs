// ===================================================================
// ArchoCybo.Domain/Enums/DatabaseType.cs
// ===================================================================
namespace ArchoCybo.Domain.Enums;

/// <summary>
/// Supported database types for code generation
/// </summary>
public enum DatabaseType
{
    SqlServer = 1,
    MySQL = 2,
    PostgreSQL = 3,
    MongoDB = 4,
    CosmosDB = 5,
    SQLite = 6,
    Oracle = 7,
    Redis = 8
}

/// <summary>
/// Architecture patterns for generated projects
/// </summary>
public enum ArchitectureType
{
    Monolithic = 1,
    Microservices = 2,
    CleanArchitecture = 3,
    DDD = 4
}

/// <summary>
/// Project generation status
/// </summary>
public enum ProjectStatus
{
    Draft = 1,
    InProgress = 2,
    Generated = 3,
    Deployed = 4,
    Failed = 5,
    Archived = 6
}

/// <summary>
/// Git repository providers
/// </summary>
public enum GitProvider
{
    GitHub = 1,
    GitLab = 2,
    AzureDevOps = 3,
    Bitbucket = 4
}

/// <summary>
/// Repository visibility settings
/// </summary>
public enum RepositoryVisibility
{
    Public = 1,
    Private = 2,
    Internal = 3
}

/// <summary>
/// Authentication methods for Git repositories
/// </summary>
public enum AuthenticationType
{
    Token = 1,
    SSH = 2,
    Username = 3,
    OAuth = 4
}

/// <summary>
/// Query types for custom queries
/// </summary>
public enum QueryType
{
    Select = 1,
    Insert = 2,
    Update = 3,
    Delete = 4,
    StoredProcedure = 5,
    View = 6,
    Function = 7
}

/// <summary>
/// Permission types for authorization
/// </summary>
public enum PermissionType
{
    Feature = 1,
    Data = 2,
    System = 3,
    API = 4
}

/// <summary>
/// Relationship types between entities
/// </summary>
public enum RelationshipType
{
    OneToOne = 1,
    OneToMany = 2,
    ManyToMany = 3
}

/// <summary>
/// Field data types for entity properties
/// </summary>
public enum FieldDataType
{
    String = 1,
    Integer = 2,
    Long = 3,
    Decimal = 4,
    Double = 5,
    Boolean = 6,
    DateTime = 7,
    Date = 8,
    Time = 9,
    Guid = 10,
    Json = 11,
    Binary = 12,
    Email = 13,
    Phone = 14,
    Url = 15,
    Color = 16,
    File = 17,
    Image = 18
}
