using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using MudBlazor;
using System.Net.Http.Json;
using ArchoCybo.Application.DTOs;
using ArchoCybo.Shared.Dialogs;

namespace ArchoCybo.Pages.Schema;

public partial class SchemaDesigner : IAsyncDisposable
{
    [Inject] public HttpClient Http { get; set; } = default!;
    [Inject] public IDialogService DialogService { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;

    // SignalR Hub is injected via DI in typical Blazor, or created manually if strictly client-side URL.
    // The razor file had "@inject Microsoft.AspNetCore.SignalR.Client.HubConnection? Hub".
    // Usually HubConnection is not registered as singleton in basic templates, but let's assume it is or use manual creation if it was null.
    // However, the code: "hub = new HubConnectionBuilder()...Build()" implies we build it here. 
    // The inject directive likely was a mistake or placeholder in previous code if lines 94-97 build a new one into `hub` field.
    // I'll keep the field logic but remove the Inject if it's unused or conflicting.
    // Existing code line 9: @inject ... Hub.
    // Line 87: private HubConnection? hub;
    // Line 94: hub = new ...
    // So the injected one is ignored? Or maybe line 9 was just unused. I will ignore the injected one and use the internal one.

    [Parameter] public required string ProjectId { get; set; }

    private class EntityNode
    {
        public EntityDto? Entity { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
    }

    private class RelationDraw
    {
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }
    }

    private List<EntityNode> entityNodes = new();
    private List<RelationDraw> relationsToDraw = new();
    private HubConnection? hub;

    protected override async Task OnInitializedAsync()
    {
        await LoadEntities();

        // SignalR
        hub = new HubConnectionBuilder()
            .WithUrl(Nav.ToAbsoluteUri("/hubs/notifications"))
            .WithAutomaticReconnect()
            .Build();

        hub.On<Guid>("ProjectUpdated", async (Guid id) =>
        {
            if (id.ToString() == ProjectId)
            {
                await LoadEntities();
                StateHasChanged();
            }
        });

        try { await hub.StartAsync(); } catch { }
    }

    private async Task LoadEntities()
    {
        try
        {
            var dtos = await Http.GetFromJsonAsync<List<EntityDto>>($"api/projects/{ProjectId}/entities") ?? new();

            // Layout logic
            entityNodes.Clear();
            int x = 60, y = 60, col = 0;
            foreach (var e in dtos)
            {
                entityNodes.Add(new EntityNode { Entity = e, X = x, Y = y });

                x += 320;
                col++;
                if (col > 3) { col = 0; x = 60; y += 350; }
            }

            CalculateRelations();
        }
        catch (Exception)
        {
            // log
        }
    }

    private void CalculateRelations()
    {
        relationsToDraw.Clear();
        foreach (var node in entityNodes)
        {
            if (node.Entity?.Relations == null) continue;

            foreach (var rel in node.Entity.Relations)
            {
                var target = entityNodes.FirstOrDefault(n => n.Entity?.Id == rel.TargetEntityId);
                if (target != null)
                {
                    // Center of nodes (width 260)
                    relationsToDraw.Add(new RelationDraw
                    {
                        X1 = node.X + 130,
                        Y1 = node.Y + 40, // rough center
                        X2 = target.X + 130,
                        Y2 = target.Y + 40
                    });
                }
            }
        }
    }

    private string GetPath(RelationDraw r)
    {
        // Curvi-linear connector
        // Horizontal logic
        return $"M {r.X1} {r.Y1} C {r.X1 + 50} {r.Y1}, {r.X2 - 50} {r.Y2}, {r.X2} {r.Y2}";
    }

    private async Task AddEntity()
    {
        var parameters = new DialogParameters { { "ProjectId", ProjectId } };
        var dialog = DialogService.Show<AddEntityDialog>("Add Entity", parameters);
        var result = await dialog.Result;

        if (!result.Cancelled && result.Data != null)
        {
            // Actually create via API
            dynamic data = result.Data;
            var dto = new CreateEntityDto(data.EntityName, data.EntityName); // simplified
            await Http.PostAsJsonAsync($"api/projects/{ProjectId}/entities", dto);
            // SignalR will refresh
        }
    }

    private void EditEntity(EntityDto entity)
    {
        var parameters = new DialogParameters
        {
            { "EntityName", entity.Name },
            { "EntityId", entity.Id.ToString() },
            { "ProjectId", ProjectId }
        };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true };
        DialogService.Show<EditEntityDialog>("Manage Entity", parameters, options);
    }

    private void GoToQueryBuilder()
    {
        Nav.NavigateTo($"/query-builder/{ProjectId}");
    }

    public async ValueTask DisposeAsync()
    {
        if (hub != null) try { await hub.DisposeAsync(); } catch { }
    }
}
