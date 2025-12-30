using ArchoCybo.Domain.Entities.Security;

namespace ArchoCybo.Application.Interfaces.IServices;

public interface IGitService
{
    /// <summary>
    /// Gets the authorization URL for the specified platform
    /// </summary>
    string GetAuthorizationUrl(GitPlatform platform, string state);

    /// <summary>
    /// Exchanges a code for an access token
    /// </summary>
    Task<UserGitConfiguration> AuthenticateAsync(Guid userId, GitPlatform platform, string code);

    /// <summary>
    /// Creates a new repository on the platform
    /// </summary>
    Task<string> CreateRepositoryAsync(Guid userId, GitPlatform platform, string repositoryName, string description, bool isPrivate);

    /// <summary>
    /// Pushes the generated code to the specified repository
    /// </summary>
    Task PushCodeAsync(Guid userId, GitPlatform platform, string repositoryName, string localPath, string commitMessage = "Initial commit from ArchoCybo");

    /// <summary>
    /// Gets a list of organizations/namespaces the user has access to
    /// </summary>
    Task<List<string>> GetOrganizationsAsync(Guid userId, GitPlatform platform);
}
