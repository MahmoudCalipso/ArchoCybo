// ===================================================================
// ArchoCybo.Domain/ValueObjects/DatabaseConnection.cs
// ===================================================================
using ArchoCybo.Domain.Common;
using ArchoCybo.Domain.Enums;
using ArchoCybo.SharedKernel.Domain;
using System.Collections.Generic;

namespace ArchoCybo.Domain.ValueObjects
{
    /// <summary>
    /// Database connection configuration value object
    /// </summary>
    public class DatabaseConnection : ValueObject
    {
        public string Server { get; set; } = string.Empty;
        public string Database { get; set; } = string.Empty;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public int Port { get; set; }
        public bool IntegratedSecurity { get; set; } = false;
        public bool TrustServerCertificate { get; set; } = false;
        public int CommandTimeout { get; set; } = 30;
        public int ConnectionTimeout { get; set; } = 15;
        public string? AdditionalParameters { get; set; }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Server;
            yield return Database;
            yield return Username ?? string.Empty;
            yield return Port;
            yield return IntegratedSecurity;
        }

        public string GenerateConnectionString(DatabaseType databaseType)
        {
            return databaseType switch
            {
                DatabaseType.SqlServer => GenerateSqlServerConnectionString(),
                DatabaseType.MySQL => GenerateMySqlConnectionString(),
                DatabaseType.PostgreSQL => GeneratePostgreSqlConnectionString(),
                DatabaseType.SQLite => GenerateSqliteConnectionString(),
                _ => throw new NotSupportedException($"Database type {databaseType} is not supported")
            };
        }

        private string GenerateSqlServerConnectionString()
        {
            if (IntegratedSecurity)
            {
                return $"Server={Server},{Port};Database={Database};Integrated Security=true;TrustServerCertificate={TrustServerCertificate.ToString().ToLower()};Connection Timeout={ConnectionTimeout};Command Timeout={CommandTimeout};{AdditionalParameters}";
            }
            
            return $"Server={Server},{Port};Database={Database};User Id={Username};Password={Password};TrustServerCertificate={TrustServerCertificate.ToString().ToLower()};Connection Timeout={ConnectionTimeout};Command Timeout={CommandTimeout};{AdditionalParameters}";
        }

        private string GenerateMySqlConnectionString()
        {
            return $"Server={Server};Port={Port};Database={Database};Uid={Username};Pwd={Password};Connection Timeout={ConnectionTimeout};Command Timeout={CommandTimeout};{AdditionalParameters}";
        }

        private string GeneratePostgreSqlConnectionString()
        {
            return $"Host={Server};Port={Port};Database={Database};Username={Username};Password={Password};Timeout={ConnectionTimeout};Command Timeout={CommandTimeout};{AdditionalParameters}";
        }

        private string GenerateSqliteConnectionString()
        {
            return $"Data Source={Database};{AdditionalParameters}";
        }

        public bool IsValid => !string.IsNullOrEmpty(Server) && !string.IsNullOrEmpty(Database);
    }

    // ===================================================================
    // ArchoCybo.Domain/ValueObjects/GitConfiguration.cs
    // ===================================================================

    /// <summary>
    /// Git repository configuration value object
    /// </summary>
    public class GitConfiguration : ValueObject
    {
        public GitProvider Provider { get; set; }
        public string Organization { get; set; } = string.Empty;
        public string RepositoryName { get; set; } = string.Empty;
        public string DefaultBranch { get; set; } = "main";
        public RepositoryVisibility Visibility { get; set; } = RepositoryVisibility.Private;
        public string? Description { get; set; }
        public AuthenticationType AuthType { get; set; } = AuthenticationType.Token;
        public string? Token { get; set; }
        public string? SshKeyPath { get; set; }
        public bool EnableCICD { get; set; } = false;
        public string? CICDPlatform { get; set; }
        public bool AutoSync { get; set; } = false;
        public List<string> ProtectedBranches { get; set; } = new();

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Provider;
            yield return Organization;
            yield return RepositoryName;
            yield return DefaultBranch;
            yield return Visibility;
        }

        public string GetRepositoryUrl()
        {
            return Provider switch
            {
                GitProvider.GitHub => $"https://github.com/{Organization}/{RepositoryName}",
                GitProvider.GitLab => $"https://gitlab.com/{Organization}/{RepositoryName}",
                GitProvider.AzureDevOps => $"https://dev.azure.com/{Organization}/_git/{RepositoryName}",
                GitProvider.Bitbucket => $"https://bitbucket.org/{Organization}/{RepositoryName}",
                _ => string.Empty
            };
        }

        public bool IsValid => !string.IsNullOrEmpty(Organization) && !string.IsNullOrEmpty(RepositoryName);
    }

    // ===================================================================
    // ArchoCybo.Domain/ValueObjects/SecurityConfiguration.cs
    // ===================================================================

    /// <summary>
    /// Security configuration for generated projects
    /// </summary>
    public class SecurityConfiguration : ValueObject
    {
        public string JwtSecretKey { get; set; } = string.Empty;
        public string JwtIssuer { get; set; } = string.Empty;
        public string JwtAudience { get; set; } = string.Empty;
        public int AccessTokenExpiryMinutes { get; set; } = 60;
        public int RefreshTokenExpiryDays { get; set; } = 7;
        public bool EnableRefreshTokens { get; set; } = true;
        public bool EnableMultipleDevices { get; set; } = true;
        public bool EnableTwoFactor { get; set; } = false;
        public bool EnablePasswordReset { get; set; } = true;
        public bool EnableEmailConfirmation { get; set; } = true;
        public int MaxLoginAttempts { get; set; } = 5;
        public int LockoutMinutes { get; set; } = 30;
        public bool EnableCors { get; set; } = true;
        public List<string> AllowedOrigins { get; set; } = new();
        public List<string> AllowedMethods { get; set; } = new() { "GET", "POST", "PUT", "DELETE", "OPTIONS" };

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return JwtSecretKey;
            yield return JwtIssuer;
            yield return JwtAudience;
            yield return AccessTokenExpiryMinutes;
            yield return RefreshTokenExpiryDays;
        }

        public bool IsValid => !string.IsNullOrEmpty(JwtSecretKey) && 
                              !string.IsNullOrEmpty(JwtIssuer) && 
                              !string.IsNullOrEmpty(JwtAudience) &&
                              JwtSecretKey.Length >= 32;
    }
}