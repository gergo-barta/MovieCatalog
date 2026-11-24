using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieCatalog.Data;
using MovieCatalog.Dtos;
using MovieCatalog.Models;
using MovieCatalog.Services;

namespace MovieCatalog.Controllers.Api;

[ApiController, Route("api/movies"), Tags("Movies")]
public class MoviesApiController(AppDbContext db, UserManager<AppUser> users, CatalogNotifier notifier) : ControllerBase
{
    private static readonly System.Linq.Expressions.Expression<Func<Movie, MovieDto>> ToDto =
        m => new MovieDto(m.Id, m.Title, m.ReleaseYear, m.DurationMinutes, m.Description, m.PosterPath,
            m.GenreId, m.Genre!.Name, m.StudioId, m.Studio != null ? m.Studio.Name : null,
            m.Reviews.Average(r => (double?)r.Rating), m.Reviews.Count);

    /// <summary>List movies, optionally filtered by title and genre.</summary>
    [HttpGet]
    public async Task<List<MovieDto>> GetAll(string? search, int? genreId)
    {
        var query = db.Movies.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(m => EF.Functions.Like(m.Title, $"%{search}%"));
        if (genreId is > 0) query = query.Where(m => m.GenreId == genreId);
        return await query.OrderBy(m => m.Title).Select(ToDto).ToListAsync();
    }

    /// <summary>One movie with its cast and reviews.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<MovieDetailDto>> Get(int id)
    {
        var movie = await db.Movies.Where(m => m.Id == id).Select(ToDto).FirstOrDefaultAsync();
        if (movie is null) return NotFound();

        var cast = await db.MovieCast.Where(c => c.MovieId == id).OrderBy(c => c.BillingOrder)
            .Select(CastApiController.ToDto).ToListAsync();
        var reviews = await db.Reviews.Where(r => r.MovieId == id).OrderByDescending(r => r.CreatedAt)
            .Select(ReviewsApiController.ToDto).ToListAsync();
        return new MovieDetailDto(movie, cast, reviews);
    }

    [HttpPost, Authorize]
    public async Task<ActionResult<MovieDto>> Create(MovieInput input)
    {
        if (await Validate(input) is { } problem) return problem;

        var movie = new Movie
        {
            Title = input.Title, ReleaseYear = input.ReleaseYear, DurationMinutes = input.DurationMinutes,
            Description = input.Description, GenreId = input.GenreId, StudioId = input.StudioId
        };
        db.Movies.Add(movie);
        await db.SaveChangesAsync();
        await notifier.MovieAddedAsync(movie, (await users.GetUserAsync(User))!);

        var dto = await db.Movies.Where(m => m.Id == movie.Id).Select(ToDto).FirstAsync();
        return CreatedAtAction(nameof(Get), new { id = movie.Id }, dto);
    }

    [HttpPut("{id:int}"), Authorize]
    public async Task<IActionResult> Update(int id, MovieInput input)
    {
        var movie = await db.Movies.FindAsync(id);
        if (movie is null) return NotFound();
        if (await Validate(input) is { } problem) return problem;

        movie.Title = input.Title;
        movie.ReleaseYear = input.ReleaseYear;
        movie.DurationMinutes = input.DurationMinutes;
        movie.Description = input.Description;
        movie.GenreId = input.GenreId;
        movie.StudioId = input.StudioId;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}"), Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var movie = await db.Movies.FindAsync(id);
        if (movie is null) return NotFound();
        db.Movies.Remove(movie);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<ActionResult?> Validate(MovieInput input)
    {
        if (!await db.Genres.AnyAsync(g => g.Id == input.GenreId))
            return BadRequest(new { error = $"Genre {input.GenreId} does not exist." });
        if (input.StudioId is not null && !await db.Studios.AnyAsync(s => s.Id == input.StudioId))
            return BadRequest(new { error = $"Studio {input.StudioId} does not exist." });
        return null;
    }
}
