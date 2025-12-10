using ArchoCybo.Domain.Common;

// ===================================================================
// ArchoCybo.Domain/Entities/Security/EndpointPermission.cs
// ===================================================================
namespace ArchoCybo.Domain.Entities.Security;

/// <summary>
/// API endpoint permission mapping
/// </summary>
public class EndpointPermission : BaseAuditableEntity
{
    public string EndpointPath { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string? Controller { get; set; }
    public string? Action { get; set; }
    public string? Description { get; set; }
    public bool IsPublic { get; set; } = false;
    public bool RequiresAuthentication { get; set; } = true;
    public Guid? RequiredPermissionId { get; set; }
    public string? AdditionalPolicies { get; set; }

    // Navigation Properties
    public virtual Permission? RequiredPermission { get; set; }

    // Computed Properties
    public string EndpointSignature => $"{HttpMethod.ToUpper()} {EndpointPath}";
    public bool HasPermissionRequirement => RequiredPermissionId.HasValue && !IsPublic;
}
