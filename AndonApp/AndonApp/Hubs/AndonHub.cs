using AndonApp.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AndonApp.Hubs;

public class AndonHub : Hub
{
    private readonly IDbContextFactory<AndonDbContext> _dbFactory;

    public AndonHub(IDbContextFactory<AndonDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task JoinLine(string slug, string token)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var valid = await db.ProductionLines
            .AnyAsync(l => l.Slug == slug && l.AccessToken == token && l.IsActive);

        if (!valid)
            throw new HubException("Invalid line or token.");

        await Groups.AddToGroupAsync(Context.ConnectionId, $"line:{slug}");
    }

    public async Task LeaveLine(string slug)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"line:{slug}");
    }
}
