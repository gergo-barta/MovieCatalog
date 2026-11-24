using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace MovieCatalog.Hubs;

/// <summary>
/// Real-time hub. Clients connect from every page; the server pushes:
///  - "PresenceChanged"  {users, guests}                      when someone opens/closes the site
///  - "MovieAdded"       {id, title, releaseYear, by, byUser} when a movie is created
///  - "ReviewsChanged"   {movieId, movieTitle, average, count, action, rating, by, byUser}
/// </summary>
public class CatalogHub : Hub
{
    // connectionId -> user name ("" for guests)
    private static readonly ConcurrentDictionary<string, string> Connections = new();

    public override async Task OnConnectedAsync()
    {
        var name = Context.User?.Identity?.IsAuthenticated == true ? Context.User.Identity!.Name ?? "" : "";
        Connections[Context.ConnectionId] = name;
        await BroadcastPresence();
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Connections.TryRemove(Context.ConnectionId, out _);
        await BroadcastPresence();
        await base.OnDisconnectedAsync(exception);
    }

    private Task BroadcastPresence()
    {
        var values = Connections.Values.ToList();
        var users = values.Where(v => v != "").Distinct().Count();
        var guests = values.Count(v => v == "");
        return Clients.All.SendAsync("PresenceChanged", new { users, guests });
    }
}
