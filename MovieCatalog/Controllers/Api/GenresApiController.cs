using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieCatalog.Data;
using MovieCatalog.Dtos;
using MovieCatalog.Models;

namespace MovieCatalog.Controllers.Api;

[ApiController, Route("api/genres"), Tags("Genres")]
public class GenresApiController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<List<GenreDto>> GetAll() =>
        await db.Genres.OrderBy(g => g.Name).Select(g => new GenreDto(g.Id, g.Name, g.Movies.Count)).ToListAsync();

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GenreDto>> Get(int id)
    {
        var dto = await db.Genres.Where(g => g.Id == id).Select(g => new GenreDto(g.Id, g.Name, g.Movies.Count)).FirstOrDefaultAsync();
        return dto is null ? NotFound() : dto;
    }

    [HttpPost, Authorize]
    public async Task<ActionResult<GenreDto>> Create(GenreInput input)
    {
        if (await db.Genres.AnyAsync(g => g.Name == input.Name)) return Conflict(new { error = "This genre already exists." });
        var genre = new Genre { Name = input.Name };
        db.Genres.Add(genre);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = genre.Id }, new GenreDto(genre.Id, genre.Name, 0));
    }

    [HttpPut("{id:int}"), Authorize]
    public async Task<IActionResult> Update(int id, GenreInput input)
    {
        var genre = await db.Genres.FindAsync(id);
        if (genre is null) return NotFound();
        if (await db.Genres.AnyAsync(g => g.Name == input.Name && g.Id != id)) return Conflict(new { error = "This genre already exists." });
        genre.Name = input.Name;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}"), Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var genre = await db.Genres.Include(g => g.Movies).FirstOrDefaultAsync(g => g.Id == id);
        if (genre is null) return NotFound();
        if (genre.Movies.Count > 0) return Conflict(new { error = $"Genre still has {genre.Movies.Count} movie(s)." });
        db.Genres.Remove(genre);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
