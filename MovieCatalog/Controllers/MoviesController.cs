using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MovieCatalog.Data;
using MovieCatalog.Models;
using MovieCatalog.Services;
using MovieCatalog.ViewModels;

namespace MovieCatalog.Controllers;

public class MoviesController(AppDbContext db, UserManager<AppUser> users, CatalogNotifier notifier, PosterStorage posters) : Controller
{
    public static IQueryable<MovieCard> ToCards(IQueryable<Movie> movies) =>
        movies.Select(m => new MovieCard(m.Id, m.Title, m.ReleaseYear, m.DurationMinutes, m.PosterPath, m.Genre!.Name,
            m.Reviews.Average(r => (double?)r.Rating), m.Reviews.Count));

    // GET /Movies?search=&genreId=&sort=
    public async Task<IActionResult> Index(string? search, int? genreId, string sort = "rating")
    {
        var query = db.Movies.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(m => EF.Functions.Like(m.Title, $"%{search.Trim()}%")
                                     || m.Cast.Any(c => EF.Functions.Like(c.Person!.FirstName + " " + c.Person.LastName, $"%{search.Trim()}%")));
        if (genreId is > 0)
            query = query.Where(m => m.GenreId == genreId);

        // the catalogue is small, so sorting happens in memory (keeps the SQL simple)
        var cards = await ToCards(query).ToListAsync();
        cards = sort switch
        {
            "title" => cards.OrderBy(c => c.Title).ToList(),
            "newest" => cards.OrderByDescending(c => c.Year).ThenBy(c => c.Title).ToList(),
            "oldest" => cards.OrderBy(c => c.Year).ThenBy(c => c.Title).ToList(),
            _ => cards.OrderByDescending(c => c.Average ?? 0).ThenByDescending(c => c.ReviewCount).ToList()
        };

        return View(new MovieIndexViewModel
        {
            Movies = cards,
            Search = search,
            GenreId = genreId,
            Sort = sort,
            Genres = new SelectList(await db.Genres.OrderBy(g => g.Name).ToListAsync(), "Id", "Name", genreId)
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var vm = await BuildDetails(id);
        return vm is null ? NotFound() : View(vm);
    }

    /// <summary>Reviews block of the details page, re-fetched by SignalR clients when reviews change.</summary>
    public async Task<IActionResult> ReviewsPartial(int id)
    {
        var vm = await BuildDetails(id);
        return vm is null ? NotFound() : PartialView("_Reviews", vm);
    }

    private async Task<MovieDetailsViewModel?> BuildDetails(int id)
    {
        var movie = await db.Movies
            .Include(m => m.Genre).Include(m => m.Studio)
            .Include(m => m.Cast).ThenInclude(c => c.Person)
            .Include(m => m.Reviews).ThenInclude(r => r.User)
            .AsSplitQuery()
            .FirstOrDefaultAsync(m => m.Id == id);
        if (movie is null) return null;

        var userId = users.GetUserId(User);
        var cast = movie.Cast.OrderBy(c => c.BillingOrder).ToList();
        return new MovieDetailsViewModel
        {
            Movie = movie,
            Average = movie.Reviews.Count == 0 ? null : Math.Round(movie.Reviews.Average(r => r.Rating), 1),
            ReviewCount = movie.Reviews.Count,
            Directors = cast.Where(c => c.Role == CastRole.Director).ToList(),
            Actors = cast.Where(c => c.Role != CastRole.Director).ToList(),
            Reviews = movie.Reviews.OrderByDescending(r => r.CreatedAt).ToList(),
            MyReview = userId is null ? null : movie.Reviews.FirstOrDefault(r => r.UserId == userId)
        };
    }

    [Authorize]
    public async Task<IActionResult> Create()
    {
        await FillLists();
        return View("Form", new MovieFormViewModel());
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MovieFormViewModel vm)
    {
        ValidatePoster(vm);
        if (!ModelState.IsValid)
        {
            await FillLists();
            return View("Form", vm);
        }

        var movie = vm.Movie;
        movie.Id = 0;
        movie.CreatedAt = DateTime.UtcNow;
        movie.PosterPath = vm.Poster is { Length: > 0 } ? await posters.SaveAsync(vm.Poster) : null;
        db.Movies.Add(movie);
        await db.SaveChangesAsync();

        await notifier.MovieAddedAsync(movie, (await users.GetUserAsync(User))!);
        TempData["Toast"] = $"\"{movie.Title}\" was added.";
        return RedirectToAction(nameof(Details), new { id = movie.Id });
    }

    [Authorize]
    public async Task<IActionResult> Edit(int id)
    {
        var movie = await db.Movies.FindAsync(id);
        if (movie is null) return NotFound();
        await FillLists();
        return View("Form", new MovieFormViewModel { Movie = movie });
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MovieFormViewModel vm)
    {
        var movie = await db.Movies.FindAsync(id);
        if (movie is null) return NotFound();

        ValidatePoster(vm);
        if (!ModelState.IsValid)
        {
            vm.Movie.PosterPath = movie.PosterPath;
            await FillLists();
            return View("Form", vm);
        }

        movie.Title = vm.Movie.Title;
        movie.ReleaseYear = vm.Movie.ReleaseYear;
        movie.DurationMinutes = vm.Movie.DurationMinutes;
        movie.Description = vm.Movie.Description;
        movie.GenreId = vm.Movie.GenreId;
        movie.StudioId = vm.Movie.StudioId;
        if (vm.Poster is { Length: > 0 }) movie.PosterPath = await posters.SaveAsync(vm.Poster);
        else if (vm.RemovePoster) movie.PosterPath = null;

        await db.SaveChangesAsync();
        TempData["Toast"] = "Movie updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var movie = await db.Movies.FindAsync(id);
        if (movie is null) return NotFound();
        db.Movies.Remove(movie);
        await db.SaveChangesAsync();
        TempData["Toast"] = $"\"{movie.Title}\" was deleted.";
        return RedirectToAction(nameof(Index));
    }

    private void ValidatePoster(MovieFormViewModel vm)
    {
        var error = posters.Validate(vm.Poster);
        if (error is not null) ModelState.AddModelError(nameof(vm.Poster), error);
    }

    private async Task FillLists()
    {
        ViewBag.Genres = new SelectList(await db.Genres.OrderBy(g => g.Name).ToListAsync(), "Id", "Name");
        ViewBag.Studios = new SelectList(await db.Studios.OrderBy(s => s.Name).ToListAsync(), "Id", "Name");
    }
}
