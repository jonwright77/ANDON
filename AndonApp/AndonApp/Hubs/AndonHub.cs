using Microsoft.AspNetCore.SignalR;

namespace AndonApp.Hubs;

public class AndonHub : Hub
{
    public async Task JoinLine(string slug)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"line:{slug}");
    }

    public async Task LeaveLine(string slug)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"line:{slug}");
    }
}
