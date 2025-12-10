// ===================================================================
// ArchoCybo.Domain/Entities/QueryBuilder/QueryBuilderEntities.cs
// ===================================================================
using ArchoCybo.Domain.Common;
using ArchoCybo.Domain.Enums;
using ArchoCybo.Domain.Entities.CodeGeneration;

namespace ArchoCybo.Domain.Entities.QueryBuilder;

/// <summary>
/// Custom query definition for advanced database operations
/// </summary>
public class CustomQuery : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid ProjectId { get; set; }
    public QueryType QueryType { get; set; } = QueryType.Select;
    public string SqlQuery { get; set; } = string.Empty;
    public string? Parameters { get; set; } // JSON
    public string? ResultSchema { get; set; } // JSON
    public bool IsActive { get; set; } = true;
    public int ExecutionCount { get; set; } = 0;
    public DateTime? LastExecutedAt { get; set; }
    public string? LastExecutionResult { get; set; }
    public bool GenerateEndpoint { get; set; } = false;
    public string? EndpointPath { get; set; }
    public string? EndpointHttpMethod { get; set; }
    public string? CacheSettings { get; set; } // JSON

    // Navigation Properties
    public virtual Project Project { get; set; } = null!;
    public virtual ICollection<QueryJoin> QueryJoins { get; set; } = new List<QueryJoin>();
    public virtual ICollection<QueryFilter> QueryFilters { get; set; } = new List<QueryFilter>();
    public virtual ICollection<QueryParameter> QueryParameters { get; set; } = new List<QueryParameter>();
    public virtual ICollection<QuerySort> QuerySorts { get; set; } = new List<QuerySort>();

    // Computed Properties
    public string EndpointSignature => !string.IsNullOrEmpty(EndpointPath) ?
        $"{EndpointHttpMethod?.ToUpper()} {EndpointPath}" : string.Empty;
    public bool HasJoins => QueryJoins.Any();
    public bool HasFilters => QueryFilters.Any();
    public bool HasParameters => QueryParameters.Any();
    public bool IsExecutable => !string.IsNullOrEmpty(SqlQuery) && IsActive;
    public bool HasBeenExecuted => LastExecutedAt.HasValue;
}

// ===================================================================
// Join configuration for custom queries
// ===================================================================
public class QueryJoin : BaseAuditableEntity
{
    public Guid CustomQueryId { get; set; }
    public CustomQuery CustomQuery { get; set; } = null!;

    public Guid SourceEntityId { get; set; }
    public Entity SourceEntity { get; set; } = null!;

    public Guid TargetEntityId { get; set; }
    public Entity TargetEntity { get; set; } = null!;

    public string JoinType { get; set; } = "INNER"; // INNER, LEFT, RIGHT, FULL
    public string SourceField { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
    public string? Alias { get; set; }
    public int SortOrder { get; set; }

    // Computed Properties
    public string JoinClause => $"{JoinType} JOIN {TargetEntity.ActualTableName}" +
        (!string.IsNullOrEmpty(Alias) ? $" AS {Alias}" : "") +
        $" ON {SourceEntity.ActualTableName}.{SourceField} = " +
        $"{(!string.IsNullOrEmpty(Alias) ? Alias : TargetEntity.ActualTableName)}.{TargetField}";
}

// ===================================================================
// Filter conditions for custom queries
// ===================================================================
public class QueryFilter : BaseAuditableEntity
{
    public Guid CustomQueryId { get; set; }
    public CustomQuery CustomQuery { get; set; } = null!;

    public string FieldName { get; set; } = string.Empty;
    public string Operator { get; set; } = "="; // =, !=, <, >, <=, >=, LIKE, IN, BETWEEN, etc.
    public string? Value { get; set; }
    public string? SecondValue { get; set; } // For BETWEEN operator
    public string LogicalOperator { get; set; } = "AND"; // AND, OR
    public int GroupLevel { get; set; } = 0; // For grouping with parentheses
    public bool IsParameterized { get; set; } = false;
    public string? ParameterName { get; set; }
    public int SortOrder { get; set; }

    // Computed Properties
    public string FilterClause
    {
        get
        {
            var clause = $"{FieldName} {Operator}";

            if (IsParameterized && !string.IsNullOrEmpty(ParameterName))
            {
                clause += $" @{ParameterName}";
            }
            else
            {
                clause += Operator.ToUpper() switch
                {
                    "BETWEEN" => $" {Value} AND {SecondValue}",
                    "IN" => $" ({Value})",
                    "LIKE" => $" '%{Value}%'",
                    _ => $" '{Value}'"
                };
            }

            return clause;
        }
    }
}

// ===================================================================
// Parameters for custom queries
// ===================================================================
public class QueryParameter : BaseAuditableEntity
{
    public Guid CustomQueryId { get; set; }
    public CustomQuery CustomQuery { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = "string";
    public bool IsRequired { get; set; } = true;
    public string? DefaultValue { get; set; }
    public string? Description { get; set; }
    public string? ValidationRules { get; set; } // JSON
    public int SortOrder { get; set; }

    // For API generation
    public bool IncludeInApi { get; set; } = true;
    public string? ApiParameterType { get; set; } = "query"; // query, path, body, header
}

// ===================================================================
// Sort configuration for custom queries
// ===================================================================
public class QuerySort : BaseAuditableEntity
{
    public Guid CustomQueryId { get; set; }
    public CustomQuery CustomQuery { get; set; } = null!;

    public string FieldName { get; set; } = string.Empty;
    public string Direction { get; set; } = "ASC"; // ASC, DESC
    public int SortOrder { get; set; }

    // Computed Properties
    public string OrderByClause => $"{FieldName} {Direction}";
}

// ===================================================================
// Query execution results for caching and analysis
// ===================================================================
public class QueryResult : BaseAuditableEntity
{
    public Guid CustomQueryId { get; set; }
    public CustomQuery CustomQuery { get; set; } = null!;

    public string ExecutionId { get; set; } = string.Empty;
    public string? Parameters { get; set; } // JSON of parameter values used
    public string? ResultData { get; set; } // JSON result data
    public int ResultCount { get; set; }
    public long ExecutionTimeMs { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public string? ExecutedBy { get; set; }
    public bool IsSuccess { get; set; } = true;
    public string? ErrorMessage { get; set; }
    public string? ExecutionPlan { get; set; }

    // Computed Properties
    public TimeSpan ExecutionTime => TimeSpan.FromMilliseconds(ExecutionTimeMs);
    public bool IsSlowQuery => ExecutionTimeMs > 1000; // > 1 second
}

// ===================================================================
// User-saved queries for reuse
// ===================================================================
public class SavedQuery : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public string SqlQuery { get; set; } = string.Empty;
    public string? Parameters { get; set; } // JSON
    public bool IsPublic { get; set; } = false;
    public bool IsFavorite { get; set; } = false;
    public int UsageCount { get; set; } = 0;
    public DateTime? LastUsedAt { get; set; }

    // Navigation Properties  
    public virtual ICollection<QueryTag> QueryTags { get; set; } = new List<QueryTag>();
}

// ===================================================================
// Tags for categorizing saved queries
// ===================================================================
public class QueryTag : BaseAuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }

    public Guid SavedQueryId { get; set; }
    public SavedQuery SavedQuery { get; set; } = null!;
}
