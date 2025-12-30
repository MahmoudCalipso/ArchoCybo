using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ArchoCybo.Application.DTOs;
using ArchoCybo.Services;

namespace ArchoCybo.Pages.Query;

public partial class QueryBuilderAdvanced
{
    [Inject] public HttpClient Http { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;
    [Inject] public IJSRuntime JS { get; set; } = default!;
    [Inject] public CodeGenerationService CodeGen { get; set; } = default!;
    [Inject] public TokenProvider TokenProvider { get; set; } = default!;
    [Inject] public ISnackbar Snackbar { get; set; } = default!;

    [Parameter] public string? ProjectId { get; set; }

    private List<JsonElement>? entities;
    private List<JsonElement>? columns;
    private string? selectedEntity;
    private Dictionary<string, bool> selectedColumns = new();
    private List<FilterItem> filters = new();

    private string queryText = string.Empty;
    private string linqText = string.Empty;
    private List<Dictionary<string, object>>? results;

    private bool showCodeGenDialog = false;
    private string entityName = string.Empty;
    private string generatedDto = string.Empty;
    private string generatedRepository = string.Empty;
    private string generatedService = string.Empty;
    private string generatedController = string.Empty;

    private DialogOptions dialogOptions = new DialogOptions { MaxWidth = MaxWidth.Large, FullWidth = true, CloseButton = true };

    protected override async Task OnInitializedAsync()
    {
        AttachToken();
        await LoadEntities();
    }

    private void AttachToken()
    {
        if (!string.IsNullOrEmpty(TokenProvider.Token))
            Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenProvider.Token);
    }

    private async Task LoadEntities()
    {
        try
        {
            entities = await Http.GetFromJsonAsync<List<JsonElement>>($"api/projects/{ProjectId}/entities");
        }
        catch (Exception ex)
        {
            Snackbar.Add("Failed to load entities: " + ex.Message, Severity.Error);
        }
    }

    private async Task OnEntityChanged(string newValue)
    {
        selectedEntity = newValue;
        if (string.IsNullOrEmpty(selectedEntity) || entities == null) return;

        try
        {
            // Find selected entity in list
            var ent = entities.FirstOrDefault(e => e.GetProperty("Name").GetString() == selectedEntity);
            if (ent.ValueKind != JsonValueKind.Undefined)
            {
                // EntityDto has 'Fields' property
                if (ent.TryGetProperty("Fields", out var fieldsProp))
                {
                    columns = fieldsProp.EnumerateArray().ToList();
                }
                else
                {
                    columns = new List<JsonElement>();
                }
            }

            selectedColumns.Clear();
            if (columns != null)
            {
                foreach (var col in columns)
                {
                    var name = col.GetProperty("Name").GetString();
                    if (!string.IsNullOrEmpty(name)) selectedColumns[name] = true;
                }
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add("Failed to load columns: " + ex.Message, Severity.Error);
        }
    }

    public void UpdateColumnSelection(string colName, bool isChecked)
    {
        selectedColumns[colName] = isChecked;
    }

    public string SelectedEntityProperty
    {
        get => selectedEntity ?? "";
        set => _ = OnEntityChanged(value);
    }

    private void AddFilter()
    {
        filters.Add(new FilterItem());
    }

    private void RemoveFilter(FilterItem filter)
    {
        filters.Remove(filter);
    }

    private void BuildQuery()
    {
        if (string.IsNullOrEmpty(selectedEntity))
        {
            Snackbar.Add("Please select an entity", Severity.Warning);
            return;
        }

        var cols = selectedColumns.Where(x => x.Value).Select(x => x.Key).ToList();
        if (cols.Count == 0) cols = new List<string> { "*" };

        // SQL Construction
        var query = $"SELECT {string.Join(", ", cols)} FROM {selectedEntity}";

        // Linq Construction
        var linq = $"_context.{selectedEntity}";

        if (filters.Any(f => !string.IsNullOrEmpty(f.Column)))
        {
            var validFilters = filters.Where(f => !string.IsNullOrEmpty(f.Column)).ToList();

            var whereConditions = validFilters.Select(f => f.Operator == "LIKE"
                    ? $"{f.Column} LIKE '%{f.Value}%'"
                    : $"{f.Column} {f.Operator} '{f.Value}'");

            query += "\nWHERE " + string.Join(" AND ", whereConditions);

            foreach (var f in validFilters)
            {
                if (f.Operator == "=") linq += $".Where(x => x.{f.Column} == \"{f.Value}\")";
                else if (f.Operator == "!=") linq += $".Where(x => x.{f.Column} != \"{f.Value}\")";
                else if (f.Operator == "LIKE") linq += $".Where(x => x.{f.Column}.Contains(\"{f.Value}\"))";
            }
        }

        if (cols.Count > 0 && cols[0] != "*")
        {
            linq += $".Select(x => new {{ {string.Join(", ", cols.Select(c => $"x.{c}"))} }})";
        }

        queryText = query;
        linqText = linq + ".ToListAsync();";

        Snackbar.Add("Query constructed.", Severity.Info);
    }

    private async Task RunQuery()
    {
        if (string.IsNullOrWhiteSpace(queryText))
        {
            Snackbar.Add("Please build a query first", Severity.Warning);
            return;
        }

        try
        {
            AttachToken();
            var dto = new QueryDto(queryText, null, 30);
            var resp = await Http.PostAsJsonAsync("api/query/execute", dto);

            if (resp.IsSuccessStatusCode)
            {
                results = await resp.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
                Snackbar.Add($"Success. Returned {results?.Count ?? 0} rows.", Severity.Success);
            }
            else
            {
                var error = await resp.Content.ReadAsStringAsync();
                Snackbar.Add("Query Execution Failed: " + error, Severity.Error);
                results = null;
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add("Error: " + ex.Message, Severity.Error);
            results = null;
        }
    }

    private async Task CopyQuery()
    {
        if (string.IsNullOrEmpty(queryText)) return;
        await JS.InvokeVoidAsync("navigator.clipboard.writeText", queryText);
        Snackbar.Add("SQL copied.", Severity.Success);
    }

    private void OpenCodeGenDialog()
    {
        if (string.IsNullOrEmpty(selectedEntity)) return;

        entityName = selectedEntity;
        GenerateAllCode();
        showCodeGenDialog = true;
    }

    private void GenerateAllCode()
    {
        var columnInfos = columns?.Select(c => new ColumnInfo
        {
            Name = c.GetProperty("Name").GetString() ?? "",
            Type = c.GetProperty("Type").GetString() ?? ""
        }).ToList() ?? new List<ColumnInfo>();

        generatedDto = CodeGen.GenerateDto(entityName, columnInfos);
        generatedRepository = CodeGen.GenerateRepository(entityName);
        generatedService = CodeGen.GenerateService(entityName);
        generatedController = CodeGen.GenerateController(entityName);
    }

    private void CloseCodeGenDialog()
    {
        showCodeGenDialog = false;
    }

    private async Task CopyAllCode()
    {
        var allCode = $"// DTO\n{generatedDto}\n\n// Repository\n{generatedRepository}\n\n// Service\n{generatedService}\n\n// Controller\n{generatedController}";
        await JS.InvokeVoidAsync("navigator.clipboard.writeText", allCode);
        Snackbar.Add("Artifacts copied to clipboard.", Severity.Success);
    }

    public class FilterItem
    {
        public string Column { get; set; } = string.Empty;
        public string Operator { get; set; } = "=";
        public string Value { get; set; } = string.Empty;
    }
}
