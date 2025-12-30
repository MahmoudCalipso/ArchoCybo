using Azure;
using Azure.AI.OpenAI;
using ArchoCybo.Domain.Entities.CodeGeneration;
using System.Text.Json;

namespace ArchoCybo.Application.Services.AI;

/// <summary>
/// OpenAI service for intelligent code generation assistance
/// </summary>
public class OpenAIService
{
    private readonly OpenAIClient _client;
    private readonly string _model;

    public OpenAIService(string apiKey, string model = "gpt-4")
    {
        _client = new OpenAIClient(apiKey);
        _model = model;
    }

    /// <summary>
    /// Generate entity suggestions from natural language description
    /// Example: "Create a blog system" -> Post, Comment, User, Category entities
    /// </summary>
    public async Task<List<EntitySuggestion>> SuggestEntitiesFromDescription(string description)
    {
        var prompt = $@"You are a database schema designer. Given the following requirement, suggest appropriate database entities with their fields.

Requirement: {description}

Respond with JSON in this exact format:
{{
  ""entities"": [
    {{
      ""name"": ""EntityName"",
      ""description"": ""Brief description"",
      ""fields"": [
        {{
          ""name"": ""FieldName"",
          ""dataType"": ""string|int|datetime|bool|decimal|guid"",
          ""isRequired"": true|false,
          ""isUnique"": true|false,
          ""description"": ""Field purpose""
        }}
      ]
    }}
  ]
}}

Provide 3-7 entities that would form a complete system. Include common fields like Id, CreatedAt, UpdatedAt.";

        var response = await CallOpenAI(prompt);
        return ParseEntitiesSuggestions(response);
    }

    /// <summary>
    /// Suggest relationships between entities based on their names and fields
    /// </summary>
    public async Task<List<RelationshipSuggestion>> SuggestRelationships(List<Entity> entities)
    {
        var entitiesInfo = string.Join("\n", entities.Select(e => 
            $"- {e.Name}: {string.Join(", ", e.Fields.Select(f => $"{f.Name} ({f.DataType})"))}"));

        var prompt = $@"You are a database design expert. Given these entities, suggest appropriate relationships.

Entities:
{entitiesInfo}

Respond with JSON in this exact format:
{{
  ""relationships"": [
    {{
      ""fromEntity"": ""EntityName"",
      ""toEntity"": ""RelatedEntityName"",
      ""type"": ""OneToMany|ManyToOne|OneToOne|ManyToMany"",
      ""foreignKeyField"": ""FieldName"",
      ""description"": ""Why this relationship makes sense""
    }}
  ]
}}

Only suggest logical relationships. Common patterns:
- User -> Posts (OneToMany)
- Post -> Category (ManyToOne)
- Post -> Tags (ManyToMany)";

        var response = await CallOpenAI(prompt);
        return ParseRelationshipSuggestions(response);
    }

    /// <summary>
    /// Recommend database indexes for performance optimization
    /// </summary>
    public async Task<List<IndexSuggestion>> SuggestIndexes(Entity entity)
    {
        var fieldsInfo = string.Join(", ", entity.Fields.Select(f => $"{f.Name} ({f.DataType})"));

        var prompt = $@"You are a database performance expert. For the entity '{entity.Name}' with fields: {fieldsInfo}

Suggest appropriate database indexes for optimal query performance.

Respond with JSON in this exact format:
{{
  ""indexes"": [
    {{
      ""fields"": [""FieldName1"", ""FieldName2""],
      ""isUnique"": true|false,
      ""reason"": ""Why this index improves performance""
    }}
  ]
}}

Consider:
- Foreign keys should be indexed
- Frequently queried fields
- Unique constraints
- Composite indexes for common query patterns";

        var response = await CallOpenAI(prompt);
        return ParseIndexSuggestions(response);
    }

    /// <summary>
    /// Optimize LINQ/SQL queries for better performance
    /// </summary>
    public async Task<string> OptimizeQuery(string query, string context)
    {
        var prompt = $@"You are a .NET and SQL optimization expert. Review this query and suggest improvements.

Context: {context}

Query:
{query}

Provide:
1. Optimized version of the query
2. Brief explanation of improvements
3. Performance considerations

Focus on:
- Avoiding N+1 queries
- Proper use of Include/ThenInclude
- Index usage
- Async/await patterns";

        return await CallOpenAI(prompt);
    }

    /// <summary>
    /// Generate comprehensive README.md for the generated project
    /// </summary>
    public async Task<string> GenerateReadmeFromSchema(string projectName, List<Entity> entities, string databaseType)
    {
        var entitiesList = string.Join("\n", entities.Select(e => $"- **{e.Name}**: {e.Fields.Count} fields"));

        var prompt = $@"Generate a professional README.md for a .NET 10 Web API project.

Project Name: {projectName}
Database: {databaseType}
Architecture: Clean Architecture with CQRS
Entities:
{entitiesList}

Include sections:
- Overview
- Features
- Tech Stack
- Prerequisites
- Installation & Setup
- Database Migration
- Running the Application
- API Documentation (Swagger)
- Project Structure
- Contributing
- License

Make it professional, clear, and ready for GitHub.";

        return await CallOpenAI(prompt);
    }

    /// <summary>
    /// Generate validation rules based on field types and business logic
    /// </summary>
    public async Task<List<ValidationSuggestion>> SuggestValidationRules(Entity entity)
    {
        var fieldsInfo = string.Join("\n", entity.Fields.Select(f => 
            $"- {f.Name} ({f.DataType}): Required={!f.IsNullable}"));

        var prompt = $@"You are a data validation expert. For entity '{entity.Name}' with fields:
{fieldsInfo}

Suggest appropriate validation rules (FluentValidation style).

Respond with JSON:
{{
  ""validations"": [
    {{
      ""field"": ""FieldName"",
      ""rules"": [""NotEmpty()"", ""MaximumLength(100)"", ""EmailAddress()""],
      ""errorMessage"": ""Custom error message""
    }}
  ]
}}";

        var response = await CallOpenAI(prompt);
        return ParseValidationSuggestions(response);
    }

    private async Task<string> CallOpenAI(string prompt)
    {
        try
        {
            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                DeploymentName = _model,
                Messages =
                {
                    new ChatRequestSystemMessage("You are an expert software architect and database designer. Always respond with valid JSON when requested."),
                    new ChatRequestUserMessage(prompt)
                },
                Temperature = 0.7f,
                MaxTokens = 2000
            };

            var response = await _client.GetChatCompletionsAsync(chatCompletionsOptions);
            return response.Value.Choices[0].Message.Content;
        }
        catch (Exception ex)
        {
            throw new Exception($"OpenAI API error: {ex.Message}", ex);
        }
    }

    private List<EntitySuggestion> ParseEntitiesSuggestions(string jsonResponse)
    {
        try
        {
            // Extract JSON from markdown code blocks if present
            var json = ExtractJson(jsonResponse);
            var data = JsonSerializer.Deserialize<EntitiesResponse>(json);
            return data?.Entities ?? new List<EntitySuggestion>();
        }
        catch
        {
            return new List<EntitySuggestion>();
        }
    }

    private List<RelationshipSuggestion> ParseRelationshipSuggestions(string jsonResponse)
    {
        try
        {
            var json = ExtractJson(jsonResponse);
            var data = JsonSerializer.Deserialize<RelationshipsResponse>(json);
            return data?.Relationships ?? new List<RelationshipSuggestion>();
        }
        catch
        {
            return new List<RelationshipSuggestion>();
        }
    }

    private List<IndexSuggestion> ParseIndexSuggestions(string jsonResponse)
    {
        try
        {
            var json = ExtractJson(jsonResponse);
            var data = JsonSerializer.Deserialize<IndexesResponse>(json);
            return data?.Indexes ?? new List<IndexSuggestion>();
        }
        catch
        {
            return new List<IndexSuggestion>();
        }
    }

    private List<ValidationSuggestion> ParseValidationSuggestions(string jsonResponse)
    {
        try
        {
            var json = ExtractJson(jsonResponse);
            var data = JsonSerializer.Deserialize<ValidationsResponse>(json);
            return data?.Validations ?? new List<ValidationSuggestion>();
        }
        catch
        {
            return new List<ValidationSuggestion>();
        }
    }

    private string ExtractJson(string response)
    {
        // Remove markdown code blocks if present
        var json = response.Trim();
        if (json.StartsWith("```json"))
        {
            json = json.Substring(7);
            var endIndex = json.LastIndexOf("```");
            if (endIndex > 0)
                json = json.Substring(0, endIndex);
        }
        else if (json.StartsWith("```"))
        {
            json = json.Substring(3);
            var endIndex = json.LastIndexOf("```");
            if (endIndex > 0)
                json = json.Substring(0, endIndex);
        }
        return json.Trim();
    }
}

// Response DTOs for JSON parsing
public class EntitiesResponse
{
    public List<EntitySuggestion> Entities { get; set; } = new();
}

public class RelationshipsResponse
{
    public List<RelationshipSuggestion> Relationships { get; set; } = new();
}

public class IndexesResponse
{
    public List<IndexSuggestion> Indexes { get; set; } = new();
}

public class ValidationsResponse
{
    public List<ValidationSuggestion> Validations { get; set; } = new();
}

// Suggestion Models
public class EntitySuggestion
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<FieldSuggestion> Fields { get; set; } = new();
}

public class FieldSuggestion
{
    public string Name { get; set; } = "";
    public string DataType { get; set; } = "";
    public bool IsRequired { get; set; }
    public bool IsUnique { get; set; }
    public string Description { get; set; } = "";
}

public class RelationshipSuggestion
{
    public string FromEntity { get; set; } = "";
    public string ToEntity { get; set; } = "";
    public string Type { get; set; } = ""; // OneToMany, ManyToOne, OneToOne, ManyToMany
    public string ForeignKeyField { get; set; } = "";
    public string Description { get; set; } = "";
}

public class IndexSuggestion
{
    public List<string> Fields { get; set; } = new();
    public bool IsUnique { get; set; }
    public string Reason { get; set; } = "";
}

public class ValidationSuggestion
{
    public string Field { get; set; } = "";
    public List<string> Rules { get; set; } = new();
    public string ErrorMessage { get; set; } = "";
}
