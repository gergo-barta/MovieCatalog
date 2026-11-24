using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MovieCatalog.Data;
using MovieCatalog.Hubs;
using MovieCatalog.Models;

namespace MovieCatalog.Services;

/// <summary>Pushes catalogue changes to every connected browser through SignalR.
/// Used by both the MVC controllers and the REST API, so Postman calls show up live in the browser too.</summary>
public class CatalogNotifier(IHubContext<CatalogHub> hub, AppDbContext db)
{
    public Task MovieAddedAsync(Movie movie, AppUser by) =>
        hub.Clients.All.SendAsync("MovieAdded", new
        {
            id = movie.Id,
            title = movie.Title,
            releaseYear = movie.ReleaseYear,
            by = by.Name,
            byUser = by.UserName
        });

    /// <param name="action">"posted", "updated" or "deleted"</param>
    public async Task ReviewsChangedAsync(int movieId, string action, int? rating, AppUser by)
    {
        var title = await db.Movies.Where(m => m.Id == movieId).Select(m => m.Title).FirstOrDefaultAsync() ?? "";
        var ratings = await db.Reviews.Where(r => r.MovieId == movieId).Select(r => r.Rating).ToListAsync();

        await hub.Clients.All.SendAsync("ReviewsChanged", new
        {
            movieId,
            movieTitle = title,
            average = ratings.Count == 0 ? (double?)null : Math.Round(ratings.Average(), 1),
            count = ratings.Count,
            action,
            rating,
            by = by.Name,
            byUser = by.UserName
        });
    }
}
