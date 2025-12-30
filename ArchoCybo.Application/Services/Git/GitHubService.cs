using ArchoCybo.Application.Interfaces;
using ArchoCybo.Application.Interfaces.IServices;
using ArchoCybo.Domain.Entities.Security;
using Octokit;
using Microsoft.Extensions.Configuration;

namespace ArchoCybo.Application.Services.Git;

public class GitHubService : IGitService
{
    private readonly IConfiguration _configuration;
    private readonly IUnitOfWork _unitOfWork;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public GitHubService(IConfiguration configuration, IUnitOfWork unitOfWork)
    {
        _configuration = configuration;
        _unitOfWork = unitOfWork;
        _clientId = _configuration["Git:GitHub:ClientId"] ?? "";
        _clientSecret = _configuration["Git:GitHub:ClientSecret"] ?? "";
    }

    public string GetAuthorizationUrl(GitPlatform platform, string state)
    {
        if (platform != GitPlatform.GitHub) throw new NotSupportedException();

        var request = new OauthLoginRequest(_clientId)
        {
            Scopes = { "repo", "user" },
            State = state
        };

        var client = new GitHubClient(new ProductHeaderValue("ArchoCybo"));
        return client.Oauth.GetGitHubLoginUrl(request).ToString();
    }

    public async Task<UserGitConfiguration> AuthenticateAsync(Guid userId, GitPlatform platform, string code)
    {
        if (platform != GitPlatform.GitHub) throw new NotSupportedException();

        var client = new GitHubClient(new ProductHeaderValue("ArchoCybo"));
        var request = new OauthTokenRequest(_clientId, _clientSecret, code);
        var token = await client.Oauth.CreateAccessToken(request);

        client.Credentials = new Credentials(token.AccessToken);
        var gitUser = await client.User.Current();

        var repo = _unitOfWork.Repository<UserGitConfiguration>();
        var allConfigsResult = await repo.GetAllAsync();
        
        if (!allConfigsResult.Success || allConfigsResult.Data == null)
            throw new Exception("Failed to retrieve Git configurations.");

        var config = allConfigsResult.Data.FirstOrDefault(ug => ug.UserId == userId && ug.Platform == GitPlatform.GitHub);

        if (config == null)
        {
            config = new UserGitConfiguration
            {
                UserId = userId,
                Platform = GitPlatform.GitHub
            };
            await repo.AddAsync(config);
        }

        config.AccessToken = token.AccessToken;
        config.GitUsername = gitUser.Login;
        config.GitEmail = gitUser.Email;
        
        await repo.UpdateAsync(config);
        return config;
    }

    public async Task<string> CreateRepositoryAsync(Guid userId, GitPlatform platform, string repositoryName, string description, bool isPrivate)
    {
        var config = await GetConfigAsync(userId, platform);
        var client = new GitHubClient(new ProductHeaderValue("ArchoCybo"))
        {
            Credentials = new Credentials(config.AccessToken)
        };

        var newRepo = new NewRepository(repositoryName)
        {
            Description = description,
            Private = isPrivate,
            AutoInit = true
        };

        var repo = await client.Repository.Create(newRepo);
        return repo.CloneUrl;
    }

    public async Task PushCodeAsync(Guid userId, GitPlatform platform, string repositoryName, string localPath, string commitMessage)
    {
        // Implementation for pushing code using Octokit or LibGit2Sharp
        // For now, we'll focus on the API structure
        await Task.CompletedTask;
    }

    public async Task<List<string>> GetOrganizationsAsync(Guid userId, GitPlatform platform)
    {
        var config = await GetConfigAsync(userId, platform);
        var client = new GitHubClient(new ProductHeaderValue("ArchoCybo"))
        {
            Credentials = new Credentials(config.AccessToken)
        };

        var orgs = await client.Organization.GetAllForCurrent();
        return orgs.Select(o => o.Login).ToList();
    }

    private async Task<UserGitConfiguration> GetConfigAsync(Guid userId, GitPlatform platform)
    {
        var repo = _unitOfWork.Repository<UserGitConfiguration>();
        var allConfigsResult = await repo.GetAllAsync();

        if (!allConfigsResult.Success || allConfigsResult.Data == null)
            throw new Exception("Failed to retrieve Git configurations.");

        var config = allConfigsResult.Data.FirstOrDefault(ug => ug.UserId == userId && ug.Platform == platform);

        if (config == null || string.IsNullOrEmpty(config.AccessToken))
            throw new Exception("Git configuration not found or not authenticated.");

        return config;
    }
}
