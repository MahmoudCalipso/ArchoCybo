using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Net.Http.Json;
using ArchoCybo.Application.Services.AI;
using ArchoCybo.Services;

namespace ArchoCybo.Shared.Components;

public partial class AIAssistantPanel
{
    [Inject] public HttpClient Http { get; set; } = default!;
    [Inject] public ISnackbar Snackbar { get; set; } = default!;
    [Inject] public TokenProvider TokenProvider { get; set; } = default!;

    [Parameter] public EventCallback<List<EntitySuggestion>> OnEntitiesSuggested { get; set; }
    [Parameter] public EventCallback<List<RelationshipSuggestion>> OnRelationshipsSuggested { get; set; }

    private bool isOpen = false;
    private bool isLoading = false;
    private string userPrompt = "";
    private string optimizationResult = "";
    private int activeTab = 0; // 0: Entities, 1: Relationships, 2: Query Optimizer

    private List<EntitySuggestion> suggestedEntities = new();
    private List<RelationshipSuggestion> suggestedRelationships = new();

    private void TogglePanel() => isOpen = !isOpen;

    private async Task GenerateEntities()
    {
        if (string.IsNullOrWhiteSpace(userPrompt))
        {
            Snackbar.Add("Please enter a description", Severity.Warning);
            return;
        }

        isLoading = true;
        try
        {
            AttachToken();
            var response = await Http.PostAsJsonAsync("api/AIAssistant/suggest-entities", new { Description = userPrompt });
            
            if (response.IsSuccessStatusCode)
            {
                suggestedEntities = await response.Content.ReadFromJsonAsync<List<EntitySuggestion>>() ?? new();
                Snackbar.Add($"Generated {suggestedEntities.Count} entity suggestions!", Severity.Success);
                
                if (OnEntitiesSuggested.HasDelegate)
                    await OnEntitiesSuggested.InvokeAsync(suggestedEntities);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                Snackbar.Add($"AI Error: {error}", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task GenerateRelationships()
    {
        if (!suggestedEntities.Any())
        {
            Snackbar.Add("Generate entities first", Severity.Warning);
            return;
        }

        isLoading = true;
        try
        {
            AttachToken();
            // Convert EntitySuggestion to Entity (simplified for demo)
            var entities = suggestedEntities.Select(e => {
                var entity = new ArchoCybo.Domain.Entities.CodeGeneration.Entity
                {
                    Name = e.Name
                };
                foreach (var f in e.Fields)
                {
                    entity.AddField(new ArchoCybo.Domain.Entities.CodeGeneration.Field
                    {
                        Name = f.Name,
                        DataType = MapDataType(f.DataType),
                        IsNullable = !f.IsRequired
                    });
                }
                return entity;
            }).ToList();

            var response = await Http.PostAsJsonAsync("api/AIAssistant/suggest-relationships", new { Entities = entities });
            
            if (response.IsSuccessStatusCode)
            {
                suggestedRelationships = await response.Content.ReadFromJsonAsync<List<RelationshipSuggestion>>() ?? new();
                Snackbar.Add($"Generated {suggestedRelationships.Count} relationship suggestions!", Severity.Success);
                
                if (OnRelationshipsSuggested.HasDelegate)
                    await OnRelationshipsSuggested.InvokeAsync(suggestedRelationships);
            }
            else
            {
                Snackbar.Add("Failed to generate relationships", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task OptimizeQuery()
    {
        if (string.IsNullOrWhiteSpace(userPrompt))
        {
            Snackbar.Add("Please enter a query to optimize", Severity.Warning);
            return;
        }

        isLoading = true;
        try
        {
            AttachToken();
            var response = await Http.PostAsJsonAsync("api/AIAssistant/optimize-query", new { Query = userPrompt, Context = "Entity Framework Core" });
            
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<dynamic>();
                optimizationResult = result?.optimizedQuery?.ToString() ?? "No optimization available";
                Snackbar.Add("Query optimized!", Severity.Success);
            }
            else
            {
                Snackbar.Add("Failed to optimize query", Severity.Error);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
        finally
        {
            isLoading = false;
        }
    }

    private void AttachToken()
    {
        if (!string.IsNullOrEmpty(TokenProvider.Token))
            Http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenProvider.Token);
    }

    private void ClearResults()
    {
        suggestedEntities.Clear();
        suggestedRelationships.Clear();
        optimizationResult = "";
        userPrompt = "";
    }

    private ArchoCybo.Domain.Enums.FieldDataType MapDataType(string type) => type.ToLower() switch
    {
        "string" => ArchoCybo.Domain.Enums.FieldDataType.String,
        "int" => ArchoCybo.Domain.Enums.FieldDataType.Integer,
        "datetime" => ArchoCybo.Domain.Enums.FieldDataType.DateTime,
        "bool" => ArchoCybo.Domain.Enums.FieldDataType.Boolean,
        "decimal" => ArchoCybo.Domain.Enums.FieldDataType.Decimal,
        "guid" => ArchoCybo.Domain.Enums.FieldDataType.Guid,
        _ => ArchoCybo.Domain.Enums.FieldDataType.String
    };
}
